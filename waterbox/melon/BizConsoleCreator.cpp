#include "NDS.h"
#include "NDSCart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "DSi_TMD.h"
#include "FATIO.h"
#include "GPU3D_OpenGL.h"
#include "GPU3D_Compute.h"
#include "CRC32.h"
#include "FreeBIOS.h"
#include "SPI.h"

#include "fatfs/diskio.h"
#include "fatfs/ff.h"

#include "BizPlatform/BizFile.h"
#include "BizPlatform/BizUserData.h"
#include "BizTypes.h"

extern melonDS::NDS* CurrentNDS;

namespace ConsoleCreator
{

struct FirmwareSettings
{
	bool OverrideSettings;
	int UsernameLength;
	char16_t Username[10];
	int Language;
	int BirthdayMonth;
	int BirthdayDay;
	int Color;
	int MessageLength;
	char16_t Message[26];
	u8 MacAddress[6];
};

template <typename T>
static std::unique_ptr<T> CreateBiosImage(u8* biosData, u32 biosLength, std::optional<T> biosFallback = std::nullopt)
{
	auto bios = std::make_unique<T>();
	if (biosData)
	{
		if (biosLength != bios->size())
		{
			throw std::runtime_error("Invalid BIOS size");
		}

		memcpy(bios->data(), biosData, bios->size());
	}
	else
	{
		if (!biosFallback)
		{
			throw std::runtime_error("Failed to load BIOS");
		}

		memcpy(bios->data(), biosFallback->data(), bios->size());
	}

	return std::move(bios);
}

static void SanitizeExternalFirmware(melonDS::Firmware& firmware)
{
	auto& header = firmware.GetHeader();

	const bool isDSiFw = header.ConsoleType == melonDS::Firmware::FirmwareConsoleType::DSi;
	const auto defaultHeader = melonDS::Firmware::FirmwareHeader{isDSiFw};

	// the user data offset won't necessarily be 0x7FE00 & Mask, DSi/iQue use 0x7FC00 & Mask instead
	// but we don't want to crash due to an invalid offset
	const auto maxUserDataOffset = 0x7FE00 & firmware.Mask();
	if (firmware.GetUserDataOffset() > maxUserDataOffset)
	{
		header.UserSettingsOffset = maxUserDataOffset >> 3;
	}

	if (isDSiFw)
	{
		memset(&header.Bytes[0x22], 0x00, 6);
		memset(&header.Bytes[0x28], 0xFF, 2);
	}

	memcpy(&header.Bytes[0x2C], &defaultHeader.Bytes[0x2C], 0x136);
	memset(&header.Bytes[0x162], 0xFF, 0x9E);

	if (isDSiFw)
	{
		header.WifiBoard = defaultHeader.WifiBoard;
		header.WifiFlash = defaultHeader.WifiFlash;
	}

	header.UpdateChecksum();

	auto& aps = firmware.GetAccessPoints();
	aps[0] = melonDS::Firmware::WifiAccessPoint{isDSiFw};
	aps[1] = melonDS::Firmware::WifiAccessPoint{};
	aps[2] = melonDS::Firmware::WifiAccessPoint{};

	if (isDSiFw)
	{
		auto& exAps = firmware.GetExtendedAccessPoints();
		exAps[0] = melonDS::Firmware::ExtendedWifiAccessPoint{};
		exAps[1] = melonDS::Firmware::ExtendedWifiAccessPoint{};
		exAps[2] = melonDS::Firmware::ExtendedWifiAccessPoint{};
	}
}

static void FixFirmwareTouchscreenCalibration(melonDS::Firmware::UserData& userData)
{
	userData.TouchCalibrationADC1[0] = 0;
	userData.TouchCalibrationADC1[1] = 0;
	userData.TouchCalibrationPixel1[0] = 0;
	userData.TouchCalibrationPixel1[1] = 0;
	userData.TouchCalibrationADC2[0] = 255 << 4;
	userData.TouchCalibrationADC2[1] = 191 << 4;
	userData.TouchCalibrationPixel2[0] = 255;
	userData.TouchCalibrationPixel2[1] = 191;
}

static void SetFirmwareSettings(melonDS::Firmware::UserData& userData, FirmwareSettings& fwSettings)
{
	memset(userData.Bytes, 0, 0x74);

	userData.Version = 5;
	FixFirmwareTouchscreenCalibration(userData);

	userData.NameLength = fwSettings.UsernameLength;
	memcpy(userData.Nickname, fwSettings.Username, sizeof(fwSettings.Username));
	userData.Settings = fwSettings.Language | melonDS::Firmware::BacklightLevel::Max | 0xEC00;
	userData.BirthdayMonth = fwSettings.BirthdayMonth;
	userData.BirthdayDay = fwSettings.BirthdayDay;
	userData.FavoriteColor = fwSettings.Color;
	userData.MessageLength = fwSettings.MessageLength;
	memcpy(userData.Message, fwSettings.Message, sizeof(fwSettings.Message));

	if (userData.ExtendedSettings.Unknown0 == 1)
	{
		userData.ExtendedSettings.ExtendedLanguage = static_cast<melonDS::Firmware::Language>(fwSettings.Language & melonDS::Firmware::Language::Reserved);
		memset(userData.ExtendedSettings.Unused0, 0, sizeof(userData.ExtendedSettings.Unused0));

		if (!((1 << static_cast<u8>(userData.ExtendedSettings.ExtendedLanguage)) & userData.ExtendedSettings.SupportedLanguageMask))
		{
			// Use the first supported language
			for (int i = 0; i <= melonDS::Firmware::Language::Reserved; i++)
			{
				if ((1 << i) & userData.ExtendedSettings.SupportedLanguageMask)
				{
					userData.ExtendedSettings.ExtendedLanguage = static_cast<melonDS::Firmware::Language>(i);
					break;
				}
			}

			userData.Settings &= ~melonDS::Firmware::Language::Reserved;
			userData.Settings |= userData.ExtendedSettings.ExtendedLanguage;
		}
	}
	else
	{
		memset(userData.Unused3, 0xFF, sizeof(userData.Unused3));
	}

	// only extended settings should have Chinese / Korean
	// note that melonDS::Firmware::Language::Reserved is Korean, so it's valid to have language set to that
	if ((userData.Settings & melonDS::Firmware::Language::Reserved) >= melonDS::Firmware::Language::Chinese)
	{
		userData.Settings &= ~melonDS::Firmware::Language::Reserved;
		userData.Settings |= melonDS::Firmware::Language::English;
	}
}

static melonDS::Firmware CreateFirmware(u8* fwData, u32 fwLength, bool dsi, FirmwareSettings& fwSettings)
{
	melonDS::Firmware firmware{dsi};

	if (fwData)
	{
		firmware = melonDS::Firmware{fwData, fwLength};

		if (firmware.Buffer())
		{
			// sanitize header, wifi calibration, and AP points
			SanitizeExternalFirmware(firmware);
		}
	}
	else
	{
		fwSettings.OverrideSettings = true;
	}

	if (!firmware.Buffer())
	{
		throw std::runtime_error("Failed to load firmware!");
	}

	for (auto& userData : firmware.GetUserData())
	{
		FixFirmwareTouchscreenCalibration(userData);
		userData.UpdateChecksum();
	}

	if (fwSettings.OverrideSettings)
	{
		for (auto& userData : firmware.GetUserData())
		{
			SetFirmwareSettings(userData, fwSettings);
			userData.UpdateChecksum();
		}

		melonDS::MacAddress mac;
		static_assert(mac.size() == sizeof(fwSettings.MacAddress));
		memcpy(mac.data(), fwSettings.MacAddress, mac.size());
		auto& header = firmware.GetHeader();
		header.MacAddr = mac;
		header.UpdateChecksum();
	}

	return firmware;
}

static u8 GetDefaultCountryCode(melonDS::DSi_NAND::ConsoleRegion region)
{
	// TODO: CountryCode probably should be configurable
	// these defaults are also completely arbitrary
	switch (region)
	{
		case melonDS::DSi_NAND::ConsoleRegion::Japan: return 0x01; // Japan
		case melonDS::DSi_NAND::ConsoleRegion::USA: return 0x31; // United States
		case melonDS::DSi_NAND::ConsoleRegion::Europe: return 0x6E; // United Kingdom
		case melonDS::DSi_NAND::ConsoleRegion::Australia: return 0x41; // Australia
		case melonDS::DSi_NAND::ConsoleRegion::China: return 0xA0; // China
		case melonDS::DSi_NAND::ConsoleRegion::Korea: return 0x88; // Korea
		default: return 0x31; // ???
	}
}

static void SanitizeNandSettings(melonDS::DSi_NAND::DSiFirmwareSystemSettings& settings, melonDS::DSi_NAND::ConsoleRegion region)
{
	memset(settings.Zero00, 0, sizeof(settings.Zero00));
	settings.Version = 1;
	settings.UpdateCounter = 0;
	memset(settings.Zero01, 0, sizeof(settings.Zero01));
	settings.BelowRAMAreaSize = 0x128;
	// bit 0-1 are unknown (but usually 1)
	// bit 2 indicates language set (?)
	// bit 3 is wifi enable (really wifi LED enable; usually set)
	// bit 24 set will indicate EULA is "agreed" to
	settings.ConfigFlags = 0x0100000F;
	settings.Zero02 = 0;
	settings.CountryCode = GetDefaultCountryCode(region);
	settings.RTCYear = 0;
	settings.RTCOffset = 0;
	memset(settings.Zero3, 0, sizeof(settings.Zero3));
	settings.EULAVersion = 1;
	memset(settings.Zero04, 0, sizeof(settings.Zero04));
	settings.AlarmHour = 0;
	settings.AlarmMinute = 0;
	memset(settings.Zero05, 0, sizeof(settings.Zero05));
	settings.AlarmEnable = false;
	memset(settings.Zero06, 0, sizeof(settings.Zero06));
	settings.Unknown0 = 0;
	settings.Unknown1 = 3; // apparently 2 or 3
	memset(settings.Zero07, 0, sizeof(settings.Zero07));
	settings.SystemMenuMostRecentTitleID.fill(0);
	settings.Unknown2[0] = 0x9C;
	settings.Unknown2[1] = 0x20;
	settings.Unknown2[2] = 0x01;
	settings.Unknown2[3] = 0x02;
	memset(settings.Zero08, 0, sizeof(settings.Zero08));
	settings.Zero09 = 0;
	settings.ParentalControlsFlags = 0;
	memset(settings.Zero10, 0, sizeof(settings.Zero10));
	settings.ParentalControlsRegion = 0;
	settings.ParentalControlsYearsOfAgeRating = 0;
	settings.ParentalControlsSecretQuestion = 0;
	settings.Unknown3 = 0; // apparently 0 or 6 or 7
	memset(settings.Zero11, 0, sizeof(settings.Zero11));
	memset(settings.ParentalControlsPIN, 0, sizeof(settings.ParentalControlsPIN));
	memset(settings.ParentalControlsSecretAnswer, 0, sizeof(settings.ParentalControlsSecretAnswer));
}

#pragma pack(push, 1)

struct VolumeBootRecord
{
	u8 JumpOpcode[3];
	u8 OemName[8];
	u16 BytesPerSector;
	u8 SectorsPerCluster;
	u16 NumReservedSectors;
	u8 NumFATs;
	u16 MaxRootDirectoryEntries;
	u16 NumSectorsU16;
	u8 MediaDescriptor;
	u16 SectorsPerFAT;
	u16 SectorsPerTrack;
	u16 NumHeads;
	u32 NumHiddenSectors;
	u32 NumSectorsU32;
	u8 DriveNumber;
	u8 Reserved;
	u8 ExBootSignature;
	u32 VolumeID;
	u8 VolumeLabel[11];
	u8 FileSystemType[8];
	u8 BootCode[448];
	u16 BootSignature;
};

#pragma pack(pop)

static_assert(sizeof(VolumeBootRecord) == 0x200, "VolumeBootRecord wrong size");

#pragma pack(push, 1)

struct PitHeader
{
	u8 ID[8];
	u16 NumEntries;
	u16 Unknown; // reported on gbatek as 0x0001, freshly formatted dump has 0x0000
	u16 NextPhotoFolderNum;
	u16 NextPhotoFileNum;
	u16 NextFrameFolderNum;
	u16 NextFrameFileNum;
	u16 CRC16;
	u16 HeaderSize;
};

#pragma pack(pop)

static_assert(sizeof(PitHeader) == 0x18, "PitHeader wrong size");

static const char* const BaseNandDirs[] =
{
	"0:/import", "0:/progress", "0:/shared1", "0:/shared2", "0:/sys", "0:/ticket", "0:/title", "0:/tmp",
	"0:/shared2/launcher",
	"0:/sys/log",
	"0:/ticket/0003000f", "0:/ticket/00030004", "0:/ticket/00030005", "0:/ticket/00030015", "0:/ticket/00030017",
	"0:/title/0003000f", "0:/title/00030004", "0:/title/00030005", "0:/title/00030015", "0:/title/00030017",
	"0:/title/0003000f/484e4341", "0:/title/0003000f/484e4841",
	"0:/title/0003000f/484e4341/content", "0:/title/0003000f/484e4341/data",
	"0:/title/0003000f/484e4841/content", "0:/title/0003000f/484e4841/data",
	"0:/tmp/es",
	"0:/tmp/es/write",
};

static const char* const RegionalNandDirs[] =
{
	"0:/title/0003000f/484e4c%x", "0:/title/00030015/484e42%x", "0:/title/00030017/484e41%x",
	"0:/title/0003000f/484e4c%x/content", "0:/title/0003000f/484e4c%x/data",
	"0:/title/00030015/484e42%x/content", "0:/title/00030015/484e42%x/data",
	"0:/title/00030017/484e41%x/content", "0:/title/00030017/484e41%x/data",
};

static const std::pair<const char*, const u32> BaseNandFiles[] =
{
	std::make_pair("0:/shared1/TWLCFG0.dat", 0x4000),
	std::make_pair("0:/shared1/TWLCFG1.dat", 0x4000),
	std::make_pair("0:/sys/HWID.sgn", 0x100),
	std::make_pair("0:/sys/HWINFO_N.dat", 0x4000),
	std::make_pair("0:/sys/HWINFO_S.dat", 0x4000),
	std::make_pair("0:/ticket/0003000f/484e4341.tik", 0x2C4),
	std::make_pair("0:/ticket/0003000f/484e4841.tik", 0x2C4),
};

static const std::pair<const char*, const u32> RegionalNandFiles[] =
{
	std::make_pair("0:/ticket/0003000f/484e4c%x.tik", 0x2C4),
	std::make_pair("0:/ticket/00030015/484e42%x.tik", 0x2C4),
	std::make_pair("0:/ticket/00030017/484e41%x.tik", 0x2C4),
};

static const std::pair<const char*, const char*> BaseNandTitlePaths[] =
{
	std::make_pair("0:/title/0003000f/484e4341/content/title.tmd", "0:/title/0003000f/484e4341/content/%08x.app"),
	std::make_pair("0:/title/0003000f/484e4841/content/title.tmd", "0:/title/0003000f/484e4841/content/%08x.app"),
};

static const std::pair<const char*, const char*> RegionalNandTitlePaths[] =
{
	std::make_pair("0:/title/0003000f/484e4c%x/content/title.tmd", "0:/title/0003000f/484e4c%x/content/%08x.app"),
	std::make_pair("0:/title/00030015/484e42%x/content/title.tmd", "0:/title/00030015/484e42%x/content/%08x.app"),
};

static const char* const PhotoNandDirs[] =
{
	"0:/photo",
	"0:/photo/private",
	"0:/photo/private/ds",
	"0:/photo/private/ds/app",
	"0:/photo/private/ds/app/484E494A",
};

static u8 GetRegionIdChar(melonDS::DSi_NAND::ConsoleRegion region)
{
	switch (region)
	{
		case melonDS::DSi_NAND::ConsoleRegion::Japan: return 'J';
		case melonDS::DSi_NAND::ConsoleRegion::USA: return 'E';
		case melonDS::DSi_NAND::ConsoleRegion::Europe: return 'P';
		case melonDS::DSi_NAND::ConsoleRegion::Australia: return 'U';
		case melonDS::DSi_NAND::ConsoleRegion::China: return 'C';
		case melonDS::DSi_NAND::ConsoleRegion::Korea: return 'K';
		default: return 0;
	}
}

static melonDS::DSi_NAND::NANDImage CreateNandImage(
	u8* nandData, u32 nandLength, std::unique_ptr<melonDS::DSiBIOSImage>& arm7Bios,
	FirmwareSettings& fwSettings, bool clearNand,
	u8* dsiWareData, u32 dsiWareLength, u8* tmdData, u32 tmdLength)
{
	auto nand = melonDS::DSi_NAND::NANDImage{melonDS::Platform::CreateMemoryFile(nandData, nandLength), &arm7Bios->data()[0x8308]};
	if (!nand)
	{
		throw std::runtime_error("Failed to parse DSi NAND!");
	}

	// To properly "clear" the NAND, we'll want to just recreate the NAND entirely
	// Even tiny differences within the NAND structure can cause potential sync issues
	// This can happen even if the entire directory structure and every file is identical!
	if (clearNand)
	{
		// first required files need to be obtained (only one file system can be mounted at a time with fatfs)
		std::vector<std::pair<std::vector<u8>, std::string>> nandFiles;
		char nandPath[128];
		u8 regionIdChar;
		{
			auto mount = melonDS::DSi_NAND::NANDMount(nand);
			if (!mount)
			{
				throw std::runtime_error("Failed to mount DSi NAND!");
			}

			// serial data will contain the NAND's region
			melonDS::DSi_NAND::DSiSerialData serialData{};
			if (!mount.ReadSerialData(serialData))
			{
				throw std::runtime_error("Failed to obtain serial data!");
			}

			regionIdChar = GetRegionIdChar(serialData.Region);
			if (!regionIdChar)
			{
				throw std::runtime_error("Unknown NAND region");
			}

			for (auto baseNandFile : BaseNandFiles)
			{
				std::vector<u8> nandFile;
				if (!mount.ExportFile(baseNandFile.first, nandFile)
					|| nandFile.size() != baseNandFile.second)
				{
					throw std::runtime_error("Failed to export NAND file");
				}

				nandFiles.push_back(std::make_pair(std::move(nandFile), std::string(baseNandFile.first)));
			}

			for (auto regionalNandFile : RegionalNandFiles)
			{
				snprintf(nandPath, sizeof(nandPath), regionalNandFile.first, regionIdChar);
				std::vector<u8> nandFile;
				if (!mount.ExportFile(nandPath, nandFile)
					|| nandFile.size() != regionalNandFile.second)
				{
					throw std::runtime_error("Failed to export NAND file");
				}

				nandFiles.push_back(std::make_pair(std::move(nandFile), std::string(nandPath)));
			}

			for (auto baseNandTitlePath : BaseNandTitlePaths)
			{
				std::vector<u8> tmdFile;
				if (!mount.ExportFile(baseNandTitlePath.first, tmdFile)
					|| tmdFile.size() != sizeof(melonDS::DSi_TMD::TitleMetadata))
				{
					throw std::runtime_error("Failed to export NAND TMD");
				}

				auto* tmd = reinterpret_cast<melonDS::DSi_TMD::TitleMetadata*>(tmdFile.data());
				u32 version = tmd->Contents.GetVersion();
				u64 contentSize = (u64)tmd->Contents.ContentSize[0] << 56 | (u64)tmd->Contents.ContentSize[1] << 48
					| (u64)tmd->Contents.ContentSize[2] << 40 | (u64)tmd->Contents.ContentSize[3] << 32 | (u64)tmd->Contents.ContentSize[4] << 24
					| (u64)tmd->Contents.ContentSize[5] << 16 | (u64)tmd->Contents.ContentSize[6] << 8 | (u64)tmd->Contents.ContentSize[7] << 0;

				nandFiles.push_back(std::make_pair(std::move(tmdFile), std::string(baseNandTitlePath.first)));

				snprintf(nandPath, sizeof(nandPath), baseNandTitlePath.second, version);
				std::vector<u8> appFile;
				if (!mount.ExportFile(nandPath, appFile)
					|| appFile.size() != contentSize)
				{
					throw std::runtime_error("Failed to export NAND app file");
				}

				nandFiles.push_back(std::make_pair(std::move(appFile), std::string(nandPath)));
			}

			for (auto regionalNandTitlePath : RegionalNandTitlePaths)
			{
				snprintf(nandPath, sizeof(nandPath), regionalNandTitlePath.first, regionIdChar);
				std::vector<u8> tmdFile;
				if (!mount.ExportFile(nandPath, tmdFile)
					|| tmdFile.size() != sizeof(melonDS::DSi_TMD::TitleMetadata))
				{
					throw std::runtime_error("Failed to export NAND TMD");
				}

				auto* tmd = reinterpret_cast<melonDS::DSi_TMD::TitleMetadata*>(tmdFile.data());
				u32 version = tmd->Contents.GetVersion();
				u64 contentSize = (u64)tmd->Contents.ContentSize[0] << 56 | (u64)tmd->Contents.ContentSize[1] << 48
					| (u64)tmd->Contents.ContentSize[2] << 40 | (u64)tmd->Contents.ContentSize[3] << 32 | (u64)tmd->Contents.ContentSize[4] << 24
					| (u64)tmd->Contents.ContentSize[5] << 16 | (u64)tmd->Contents.ContentSize[6] << 8 | (u64)tmd->Contents.ContentSize[7] << 0;

				nandFiles.push_back(std::make_pair(std::move(tmdFile), std::string(nandPath)));

				snprintf(nandPath, sizeof(nandPath), regionalNandTitlePath.second, regionIdChar, version);
				std::vector<u8> appFile;
				if (!mount.ExportFile(nandPath, appFile)
					|| appFile.size() != contentSize)
				{
					throw std::runtime_error("Failed to export NAND app file");
				}

				nandFiles.push_back(std::make_pair(std::move(appFile), std::string(nandPath)));
			}

			// Special logic for the System Menu app
			// Under most circumstances, this is just a standard regional nand title
			// However, Unlaunch's exploit will make the title.tmd much larger than normal
			{
				snprintf(nandPath, sizeof(nandPath), "0:/title/00030017/484e41%x/content/title.tmd", regionIdChar);
				std::vector<u8> tmdFile;
				if (!mount.ExportFile(nandPath, tmdFile)
					|| tmdFile.size() < sizeof(melonDS::DSi_TMD::TitleMetadata))
				{
					throw std::runtime_error("Failed to export NAND TMD");
				}

				// shrink tmd down to its normal size if this happens to be an unlaunch exploit tmd
				// (this also happens to undo an unlaunch exploited NAND back to its original state)
				tmdFile.resize(sizeof(melonDS::DSi_TMD::TitleMetadata));

				auto* tmd = reinterpret_cast<melonDS::DSi_TMD::TitleMetadata*>(tmdFile.data());
				u32 version = tmd->Contents.GetVersion();
				u64 contentSize = (u64)tmd->Contents.ContentSize[0] << 56 | (u64)tmd->Contents.ContentSize[1] << 48
					| (u64)tmd->Contents.ContentSize[2] << 40 | (u64)tmd->Contents.ContentSize[3] << 32 | (u64)tmd->Contents.ContentSize[4] << 24
					| (u64)tmd->Contents.ContentSize[5] << 16 | (u64)tmd->Contents.ContentSize[6] << 8 | (u64)tmd->Contents.ContentSize[7] << 0;

				nandFiles.push_back(std::make_pair(std::move(tmdFile), std::string(nandPath)));

				snprintf(nandPath, sizeof(nandPath), "0:/title/00030017/484e41%x/content/%08x.app", regionIdChar, version);
				std::vector<u8> appFile;
				if (!mount.ExportFile(nandPath, appFile)
					|| appFile.size() != contentSize)
				{
					throw std::runtime_error("Failed to export NAND app file");
				}

				nandFiles.push_back(std::make_pair(std::move(appFile), std::string(nandPath)));
			}

			// sys/cert.sys also needs to be transferred over, but its size varies depending on if the DSi ever connected to the DSi Shop
			// sys/dev.kp doesn't need to be present, as it isn't present DSis that have never connected to the DSi Shop
			// This does make the shop unusable (well, it already is in multiple different ways) and Title Management won't be available
			// But not like there's any need to use Title Management with NAND cleared
			{
				std::vector<u8> nandFile;
				if (!mount.ExportFile("0:/sys/cert.sys", nandFile))
				{
					throw std::runtime_error("Failed to export cert.sys");
				}

				// DSi connected to the DSi Shop, remove those latter certificates
				if (nandFile.size() == 0xF40 || nandFile.size() == 0xC40)
				{
					nandFile.resize(0xA00);
				}

				if (nandFile.size() != 0xA00)
				{
					throw std::runtime_error("Wrong cert.sys size");
				}

				nandFiles.push_back(std::make_pair(std::move(nandFile), std::string("0:/sys/cert.sys")));
			}

			// sys/TWLFontTable.dat also needs to be transferred over, although its size will vary depending on region
			{
				std::vector<u8> nandFile;
				if (!mount.ExportFile("0:/sys/TWLFontTable.dat", nandFile))
				{
					throw std::runtime_error("Failed to export TWLFontTable.dat");
				}

				u32 twlFontTableSize;
				switch (serialData.Region)
				{
					case melonDS::DSi_NAND::ConsoleRegion::China:
						twlFontTableSize = 0x8E020;
						break;
					case melonDS::DSi_NAND::ConsoleRegion::Korea:
						twlFontTableSize = 0x27B80;
						break;
					default:
						twlFontTableSize = 0xD2C40;
						break;
				}

				if (nandFile.size() != twlFontTableSize)
				{
					throw std::runtime_error("Wrong TWLFontTable.dat size");
				}

				nandFiles.push_back(std::make_pair(std::move(nandFile), std::string("0:/sys/TWLFontTable.dat")));
			}
		}

		constexpr u32 MAIN_PARTITION_SIZE = 0xCDF1200;
		auto mainPartition = std::make_unique<u8[]>(MAIN_PARTITION_SIZE);
		// unused data within a partition is just unencrypted 0s
		memset(mainPartition.get(), 0, MAIN_PARTITION_SIZE);
		melonDS::ff_disk_open(
			[&](BYTE* buf, LBA_t sector, UINT num) -> UINT
			{
				u32 addr = sector * 0x200U;
				if (addr >= MAIN_PARTITION_SIZE)
				{
					return 0;
				}

				u32 len = num * 0x200U;
				if ((addr + len) > MAIN_PARTITION_SIZE)
				{
					len = MAIN_PARTITION_SIZE - addr;
				}

				u32 ctr = (0x10EE00U + addr) >> 4;
				AES_ctx ctx;
				nand.SetupFATCrypto(&ctx, ctr);

				for (u32 i = 0; i < len; i += 16)
				{
					u8 tmp[16];
					melonDS::Bswap128(tmp, &mainPartition[addr + i]);
					AES_CTR_xcrypt_buffer(&ctx, tmp, sizeof(tmp));
					melonDS::Bswap128(&buf[i], tmp);
				}

				return len / 0x200U;
			},
			[&](const BYTE* buf, LBA_t sector, UINT num) -> UINT
			{
				u32 addr = sector * 0x200U;
				if (addr >= MAIN_PARTITION_SIZE)
				{
					return 0;
				}

				u32 len = num * 0x200U;
				if ((addr + len) > MAIN_PARTITION_SIZE)
				{
					len = MAIN_PARTITION_SIZE - addr;
				}

				u32 ctr = (0x10EE00U + addr) >> 4;
				AES_ctx ctx;
				nand.SetupFATCrypto(&ctx, ctr);

				for (u32 i = 0; i < len; i += 16)
				{
					u8 tmp[16];
					melonDS::Bswap128(tmp, &buf[i]);
					AES_CTR_xcrypt_buffer(&ctx, tmp, sizeof(tmp));
					melonDS::Bswap128(&mainPartition[addr + i], tmp);
				}

				return len / 0x200U;
			},
			(LBA_t)(MAIN_PARTITION_SIZE / 0x200)
		);

		constexpr u32 INIT_FAT16 = 0xFFFFFFF8;
		constexpr u32 INIT_FAT12 = 0x00FFFFF8;
		u8 sectorBuffer[0x200];
		disk_initialize(0);

		// create and write VBR
		auto* vbr = reinterpret_cast<VolumeBootRecord*>(&sectorBuffer[0]);
		vbr->JumpOpcode[0] = 0xE9;
		vbr->JumpOpcode[1] = 0x00;
		vbr->JumpOpcode[2] = 0x00;
		memcpy(vbr->OemName, "TWL     ", sizeof(vbr->OemName));
		vbr->BytesPerSector = 0x200;
		vbr->SectorsPerCluster = 32;
		vbr->NumReservedSectors = 1;
		vbr->NumFATs = 2;
		vbr->MaxRootDirectoryEntries = 512;
		vbr->NumSectorsU16 = 0;
		vbr->MediaDescriptor = 0xF8;
		vbr->SectorsPerFAT = 52;
		vbr->SectorsPerTrack = 32;
		vbr->NumHeads = 16;
		vbr->NumHiddenSectors = 0x10EE00 / 0x200;
		vbr->NumSectorsU32 = MAIN_PARTITION_SIZE / 0x200;
		vbr->DriveNumber = 0;
		vbr->Reserved = 0;
		vbr->ExBootSignature = 0x29;
		vbr->VolumeID = 0x12345678;
		memset(vbr->VolumeLabel, 0x20, sizeof(vbr->VolumeLabel));
		memset(vbr->FileSystemType, 0x00, sizeof(vbr->FileSystemType));
		memset(vbr->BootCode, 0x00, sizeof(vbr->BootCode));
		vbr->BootSignature = 0xAA55;

		disk_write(0, sectorBuffer, 0, 1);

		// init both FATs
		memset(sectorBuffer, 0, sizeof(sectorBuffer));
		memcpy(sectorBuffer, &INIT_FAT16, sizeof(INIT_FAT16));
		disk_write(0, sectorBuffer, 1, 1);
		disk_write(0, sectorBuffer, 1 + 52, 1);

		memset(sectorBuffer, 0, sizeof(INIT_FAT16));
		for (u32 i = 1; i < 52; i++)
		{
			disk_write(0, sectorBuffer, 1 + i, 1);
			disk_write(0, sectorBuffer, 1 + 52 + i, 1);
		}

		// init root directory entries
		for (u32 i = 0; i < 32; i++)
		{
			disk_write(0, sectorBuffer, 1 + 52 + 52 + i, 1);
		}

		auto fs = std::make_unique<FATFS>();
		FRESULT res = f_mount(fs.get(), "0:", 0);
		if (res != FR_OK)
		{
			f_unmount("0:");
			melonDS::ff_disk_close();
			throw std::runtime_error("Failed to mount temporary main partition");
		}

		// create base directory structure
		for (auto baseNandDir : BaseNandDirs)
		{
			res = f_mkdir(baseNandDir);
			if (res != FR_OK)
			{
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed to create NAND directory");
			}
		}

		// add regional folders
		for (auto regionalNandDir : RegionalNandDirs)
		{
			snprintf(nandPath, sizeof(nandPath), regionalNandDir, regionIdChar);
			res = f_mkdir(nandPath);
			if (res != FR_OK)
			{
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed to create NAND directory");
			}
		}

		// a few more files need to be added to complete the main partition
		{
			std::vector<u8> sysMenuLog;
			sysMenuLog.resize(0x4000);
			memset(sysMenuLog.data(), 0, sysMenuLog.size());
			nandFiles.push_back(std::make_pair(std::move(sysMenuLog), std::string("0:/sys/log/sysmenu.log")));

			// sys/log/shop.log won't be present on DSis that have never connected to the DSi Shop (this is what we target)

			// sys/log/product.log is written at the factory, and variable length
			// nothing seems to need it, so best not bother writing it in

			// sys/shared2/launcher/wrap.bin gets recreated if missing automatically by the system menu on startup
			// it's rather complex, so don't bother trying to create a prepared one
			// (not much point other than maybe a slightly shorter boot time)

			// sys/shared2/0000 is a 2MiB FAT12 blob, and doesn't appear to be recreated automatically?
			// It just contains sound recordings and is empty on first bootup
			// Therefore an empty FAT12 blob can replace it

			std::vector<u8> shared2Sound;
			shared2Sound.resize(0x200000);
			memset(shared2Sound.data(), 0, shared2Sound.size());

			// write VBR
			vbr = reinterpret_cast<VolumeBootRecord*>(&shared2Sound[0]);
			vbr->JumpOpcode[0] = 0xE9;
			vbr->JumpOpcode[1] = 0x00;
			vbr->JumpOpcode[2] = 0x00;
			memcpy(vbr->OemName, "E       ", sizeof(vbr->OemName));
			vbr->BytesPerSector = 0x200;
			vbr->SectorsPerCluster = 4;
			vbr->NumReservedSectors = 1;
			vbr->NumFATs = 2;
			vbr->MaxRootDirectoryEntries = 512;
			vbr->NumSectorsU16 = 0x200000 / 0x200;
			vbr->MediaDescriptor = 0xF8;
			vbr->SectorsPerFAT = 3;
			vbr->SectorsPerTrack = 16;
			vbr->NumHeads = 16;
			vbr->NumHiddenSectors = 0;
			vbr->NumSectorsU32 = 0;
			vbr->DriveNumber = 2;
			vbr->Reserved = 0;
			vbr->ExBootSignature = 0x29;
			vbr->VolumeID = 0x12345678;
			memcpy(vbr->VolumeLabel, "V          ", sizeof(vbr->VolumeLabel));
			memset(vbr->FileSystemType, 0x00, sizeof(vbr->FileSystemType));
			memset(vbr->BootCode, 0x00, sizeof(vbr->BootCode));
			vbr->BootSignature = 0xAA55;

			// init both FATs
			memcpy(&shared2Sound[(1 + 0) * 0x200], &INIT_FAT12, sizeof(INIT_FAT12));
			memcpy(&shared2Sound[(1 + 3) * 0x200], &INIT_FAT12, sizeof(INIT_FAT12));

			nandFiles.push_back(std::make_pair(std::move(shared2Sound), std::string("0:/shared2/0000")));
		}

		// add in a blank sav file for the System Menu (this isn't created automatically)
		{
			std::vector<u8> nandSav;
			nandSav.resize(0x4000);
			memset(nandSav.data(), 0, nandSav.size());

			// write VBR
			vbr = reinterpret_cast<VolumeBootRecord*>(&nandSav[0]);
			vbr->JumpOpcode[0] = 0xE9;
			vbr->JumpOpcode[1] = 0x00;
			vbr->JumpOpcode[2] = 0x00;
			memcpy(vbr->OemName, "E       ", sizeof(vbr->OemName));
			vbr->BytesPerSector = 0x200;
			vbr->SectorsPerCluster = 1;
			vbr->NumReservedSectors = 1;
			vbr->NumFATs = 2;
			vbr->MaxRootDirectoryEntries = 32;
			vbr->NumSectorsU16 = 0x1B;
			vbr->MediaDescriptor = 0xF8;
			vbr->SectorsPerFAT = 1;
			vbr->SectorsPerTrack = 3;
			vbr->NumHeads = 3;
			vbr->NumHiddenSectors = 0;
			vbr->NumSectorsU32 = 0;
			vbr->DriveNumber = 2;
			vbr->Reserved = 0;
			vbr->ExBootSignature = 0x29;
			vbr->VolumeID = 0x12345678;
			memcpy(vbr->VolumeLabel, "V          ", sizeof(vbr->VolumeLabel));
			memset(vbr->FileSystemType, 0x00, sizeof(vbr->FileSystemType));
			memset(vbr->BootCode, 0x00, sizeof(vbr->BootCode));
			vbr->BootSignature = 0xAA55;

			// init both FATs
			memcpy(&nandSav[(1 + 0) * 0x200], &INIT_FAT12, sizeof(INIT_FAT12));
			memcpy(&nandSav[(1 + 1) * 0x200], &INIT_FAT12, sizeof(INIT_FAT12));

			snprintf(nandPath, sizeof(nandPath), "0:/title/00030017/484e41%x/data/private.sav", regionIdChar);
			nandFiles.push_back(std::make_pair(std::move(nandSav), std::string(nandPath)));
		}

		// import in all the old files
		for (auto& nandFile : nandFiles)
		{
			FF_FIL file;
			res = f_open(&file, nandFile.second.c_str(), FA_CREATE_NEW | FA_WRITE);
			if (res != FR_OK)
			{
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed to open NAND file");
			}

			u32 nwrite;
			res = f_write(&file, nandFile.first.data(), nandFile.first.size(), &nwrite);
			if (res != FR_OK || nwrite != nandFile.first.size())
			{
				f_close(&file);
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed to write NAND file");
			}

			f_close(&file);
		}

		f_unmount("0:");
		melonDS::ff_disk_close();

		auto* nandFileHandle = nand.GetFile();
		melonDS::Platform::FileSeek(nandFileHandle, 0x10EE00, melonDS::Platform::FileSeekOrigin::Start);
		melonDS::Platform::FileWrite(mainPartition.get(), 1, MAIN_PARTITION_SIZE, nandFileHandle);

		// the photo partition also needs to be recreated
		constexpr u32 PHOTO_PARTITION_SIZE = 0x20B6600;
		auto photoPartition = std::make_unique<u8[]>(PHOTO_PARTITION_SIZE);
		memset(photoPartition.get(), 0, PHOTO_PARTITION_SIZE);
		melonDS::ff_disk_open(
			[&](BYTE* buf, LBA_t sector, UINT num) -> UINT
			{
				u32 addr = sector * 0x200U;
				if (addr >= PHOTO_PARTITION_SIZE)
				{
					return 0;
				}

				u32 len = num * 0x200U;
				if ((addr + len) > PHOTO_PARTITION_SIZE)
				{
					len = PHOTO_PARTITION_SIZE - addr;
				}

				u32 ctr = (0xCF09A00U + addr) >> 4;
				AES_ctx ctx;
				nand.SetupFATCrypto(&ctx, ctr);

				for (u32 i = 0; i < len; i += 16)
				{
					u8 tmp[16];
					melonDS::Bswap128(tmp, &photoPartition[addr + i]);
					AES_CTR_xcrypt_buffer(&ctx, tmp, sizeof(tmp));
					melonDS::Bswap128(&buf[i], tmp);
				}

				return len / 0x200U;
			},
			[&](const BYTE* buf, LBA_t sector, UINT num) -> UINT
			{
				u32 addr = sector * 0x200U;
				if (addr >= PHOTO_PARTITION_SIZE)
				{
					return 0;
				}

				u32 len = num * 0x200U;
				if ((addr + len) > PHOTO_PARTITION_SIZE)
				{
					len = PHOTO_PARTITION_SIZE - addr;
				}

				u32 ctr = (0xCF09A00U + addr) >> 4;
				AES_ctx ctx;
				nand.SetupFATCrypto(&ctx, ctr);

				for (u32 i = 0; i < len; i += 16)
				{
					u8 tmp[16];
					melonDS::Bswap128(tmp, &buf[i]);
					AES_CTR_xcrypt_buffer(&ctx, tmp, sizeof(tmp));
					melonDS::Bswap128(&photoPartition[addr + i], tmp);
				}

				return len / 0x200U;
			},
			(LBA_t)(PHOTO_PARTITION_SIZE / 0x200)
		);

		disk_initialize(0);

		// create and write VBR
		vbr = reinterpret_cast<VolumeBootRecord*>(&sectorBuffer[0]);
		vbr->JumpOpcode[0] = 0xE9;
		vbr->JumpOpcode[1] = 0x00;
		vbr->JumpOpcode[2] = 0x00;
		memcpy(vbr->OemName, "TWL     ", sizeof(vbr->OemName));
		vbr->BytesPerSector = 0x200;
		vbr->SectorsPerCluster = 32;
		vbr->NumReservedSectors = 1;
		vbr->NumFATs = 2;
		vbr->MaxRootDirectoryEntries = 512;
		vbr->NumSectorsU16 = 0;
		vbr->MediaDescriptor = 0xF8;
		vbr->SectorsPerFAT = 9;
		vbr->SectorsPerTrack = 32;
		vbr->NumHeads = 16;
		vbr->NumHiddenSectors = 0xCF09A00 / 0x200;
		vbr->NumSectorsU32 = PHOTO_PARTITION_SIZE / 0x200;
		vbr->DriveNumber = 1;
		vbr->Reserved = 0;
		vbr->ExBootSignature = 0x29;
		vbr->VolumeID = 0x12345678;
		memset(vbr->VolumeLabel, 0x20, sizeof(vbr->VolumeLabel));
		memset(vbr->FileSystemType, 0x00, sizeof(vbr->FileSystemType));
		memset(vbr->BootCode, 0x00, sizeof(vbr->BootCode));
		vbr->BootSignature = 0xAA55;

		disk_write(0, sectorBuffer, 0, 1);

		// init both FATs
		memset(sectorBuffer, 0, sizeof(sectorBuffer));
		memcpy(sectorBuffer, &INIT_FAT12, sizeof(INIT_FAT12));
		disk_write(0, sectorBuffer, 1, 1);
		disk_write(0, sectorBuffer, 1 + 9, 1);

		memset(sectorBuffer, 0, sizeof(INIT_FAT12));
		for (u32 i = 1; i < 9; i++)
		{
			disk_write(0, sectorBuffer, 1 + i, 1);
			disk_write(0, sectorBuffer, 1 + 9 + i, 1);
		}

		// init root directory entries
		for (u32 i = 0; i < 32; i++)
		{
			disk_write(0, sectorBuffer, 1 + 9 + 9 + i, 1);
		}

		res = f_mount(fs.get(), "0:", 0);
		if (res != FR_OK)
		{
			f_unmount("0:");
			melonDS::ff_disk_close();
			throw std::runtime_error("Failed to mount temporary photo partition");
		}

		// create photo directory structure
		for (auto photoNandDir : PhotoNandDirs)
		{
			res = f_mkdir(photoNandDir);
			if (res != FR_OK)
			{
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed to create NAND directory");
			}
		}

		// create pit.bin
		{
			constexpr u32 PIT_SIZE = 0x1F60;
			auto pit = std::make_unique<u8[]>(PIT_SIZE);
			memset(pit.get(), 0, PIT_SIZE);

			auto* pitHeader = reinterpret_cast<PitHeader*>(&pit[0]);
			memcpy(pitHeader->ID, "0TIP00_1", sizeof(pitHeader->ID));
			pitHeader->NumEntries = 500;
			pitHeader->Unknown = 0;
			pitHeader->NextPhotoFolderNum = 0;
			pitHeader->NextPhotoFileNum = 2;
			pitHeader->NextFrameFolderNum = 0;
			pitHeader->NextFrameFileNum = 0;
			pitHeader->CRC16 = 0x68D9;
			pitHeader->HeaderSize = sizeof(PitHeader);

			FF_FIL file;
			res = f_open(&file, "0:/photo/private/ds/app/484E494A/pit.bin", FA_CREATE_NEW | FA_WRITE);
			if (res != FR_OK)
			{
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed to open pit.bin");
			}

			u32 nwrite;
			res = f_write(&file, pit.get(), PIT_SIZE, &nwrite);
			if (res != FR_OK || nwrite != PIT_SIZE)
			{
				f_close(&file);
				f_unmount("0:");
				melonDS::ff_disk_close();
				throw std::runtime_error("Failed write pit.bin");
			}

			f_close(&file);
		}

		f_unmount("0:");
		melonDS::ff_disk_close();

		melonDS::Platform::FileSeek(nandFileHandle, 0xCF09A00, melonDS::Platform::FileSeekOrigin::Start);
		melonDS::Platform::FileWrite(photoPartition.get(), 1, PHOTO_PARTITION_SIZE, nandFileHandle);
	}

	{
		auto mount = melonDS::DSi_NAND::NANDMount(nand);
		if (!mount)
		{
			throw std::runtime_error("Failed to mount DSi NAND!");
		}

		melonDS::DSi_NAND::DSiFirmwareSystemSettings settings{};
		if (!mount.ReadUserData(settings))
		{
			throw std::runtime_error("Failed to read DSi NAND user data");
		}

		// serial data will contain the NAND's region
		melonDS::DSi_NAND::DSiSerialData serialData{};
		if (!mount.ReadSerialData(serialData))
		{
			throw std::runtime_error("Failed to obtain serial data!");
		}

		if (fwSettings.OverrideSettings)
		{
			SanitizeNandSettings(settings, serialData.Region);
			memset(settings.Nickname, 0, sizeof(settings.Nickname));
			memcpy(settings.Nickname, fwSettings.Username, fwSettings.UsernameLength * 2);
			settings.Language = static_cast<melonDS::Firmware::Language>(fwSettings.Language & melonDS::Firmware::Language::Reserved);
			settings.FavoriteColor = fwSettings.Color;
			settings.BirthdayMonth = fwSettings.BirthdayMonth;
			settings.BirthdayDay = fwSettings.BirthdayDay;
			memset(settings.Message, 0, sizeof(settings.Message));
			memcpy(settings.Message, fwSettings.Message, fwSettings.MessageLength * 2);

			if (!((1 << static_cast<u8>(settings.Language)) & serialData.SupportedLanguages))
			{
				// Use the first supported language
				for (int i = 0; i <= melonDS::Firmware::Language::Reserved; i++)
				{
					if ((1 << i) & serialData.SupportedLanguages)
					{
						settings.Language = static_cast<melonDS::Firmware::Language>(i);
						break;
					}
				}
			}
		}

		settings.TouchCalibrationADC1[0] = 0;
		settings.TouchCalibrationADC1[1] = 0;
		settings.TouchCalibrationPixel1[0] = 0;
		settings.TouchCalibrationPixel1[1] = 0;
		settings.TouchCalibrationADC2[0] = 255 << 4;
		settings.TouchCalibrationADC2[1] = 191 << 4;
		settings.TouchCalibrationPixel2[0] = 255;
		settings.TouchCalibrationPixel2[1] = 191;

		settings.UpdateHash();

		if (!mount.ApplyUserData(settings))
		{
			throw std::runtime_error("Failed to write DSi NAND user data");
		}

		if (dsiWareData)
		{
			if (tmdLength != sizeof(melonDS::DSi_TMD::TitleMetadata))
			{
				throw std::runtime_error("TMD is the wrong size!");
			}

			melonDS::DSi_TMD::TitleMetadata tmd;
			memcpy(&tmd, tmdData, sizeof(melonDS::DSi_TMD::TitleMetadata));

			if (!mount.ImportTitle(dsiWareData, dsiWareLength, tmd, false))
			{
				throw std::runtime_error("Loading DSiWare failed!");
			}

			// verify that the imported title is supported by this NAND
			// it will not actually appear otherwise
			auto regionFlags = dsiWareLength > 0x1B0 ? dsiWareData[0x1B0] : 0;
			if (!(regionFlags & (1 << static_cast<u8>(serialData.Region))))
			{
				throw std::runtime_error("Loaded NAND region does not support this DSiWare title!");
			}
		}
	}

	return nand;
}

enum class GBASaveType
{
	NONE,
	SRAM,
	EEPROM512,
	EEPROM,
	FLASH512,
	FLASH1M,
};

#include "GBASaveOverrides.h"

static GBASaveType FindGbaSaveType(const u8* gbaRomData, size_t gbaRomSize)
{
	u32 crc = melonDS::CRC32(gbaRomData, gbaRomSize);
	if (auto saveOverride = GbaCrcSaveTypeOverrides.find(crc); saveOverride != GbaCrcSaveTypeOverrides.end())
	{
		return saveOverride->second;
	}

	if (gbaRomSize >= 0xB0)
	{
		char gameId[4];
		std::memcpy(gameId, &gbaRomData[0xAC], 4);
		if (auto saveOverride = GbaGameIdSaveTypeOverrides.find(std::string(gameId, 4)); saveOverride != GbaGameIdSaveTypeOverrides.end())
		{
			return saveOverride->second;
		}
	}

	if (memmem(gbaRomData, gbaRomSize, "EEPROM_V", strlen("EEPROM_V")))
	{
		return GBASaveType::EEPROM512;
	}

	if (memmem(gbaRomData, gbaRomSize, "SRAM_V", strlen("SRAM_V")))
	{
		return GBASaveType::SRAM;
	}

	if (memmem(gbaRomData, gbaRomSize, "FLASH_V", strlen("FLASH_V"))
		|| memmem(gbaRomData, gbaRomSize, "FLASH512_V", strlen("FLASH512_V")))
	{
		return GBASaveType::FLASH512;
	}

	if (memmem(gbaRomData, gbaRomSize, "FLASH1M_V", strlen("FLASH1M_V")))
	{
		return GBASaveType::FLASH1M;
	}

	return GBASaveType::NONE;
}

static std::pair<std::unique_ptr<u8[]>, size_t> CreateBlankGbaSram(const u8* gbaRomData, size_t gbaRomSize)
{
	auto saveType = FindGbaSaveType(gbaRomData, gbaRomSize);

	if (saveType == GBASaveType::NONE)
	{
		return std::make_pair(nullptr, 0);
	}

	size_t size;
	switch (saveType)
	{
		case GBASaveType::SRAM:
			size = 0x8000;
			break;
		case GBASaveType::EEPROM512:
			size = 0x200;
			break;
		case GBASaveType::EEPROM:
			size = 0x2000;
			break;
		case GBASaveType::FLASH512:
			size = 0x10000;
			break;
		case GBASaveType::FLASH1M:
			size = 0x20000;
			break;
		default:
			__builtin_unreachable();
	}

	auto data = std::make_unique<u8[]>(size);
	memset(data.get(), 0xFF, size);
	return std::make_pair(std::move(data), size);
}

#pragma pack(push, 1)

struct DSiAutoLoad
{
	u8 ID[4]; // "TLNC"
	u8 Unknown1; // "usually 01h"
	u8 Length; // starting from PrevTitleId
	u16 CRC16; // covering Length bytes ("18h=norm")
	u8 PrevTitleID[8]; // can be 0 ("anonymous")
	u8 NewTitleID[8];
	u32 Flags; // bit 0: is valid, bit 1-3: boot type ("01h=Cartridge, 02h=Landing, 03h=DSiware"), other bits unknown/unused
	u32 Unused1; // this part is typically still checksummed
	u8 Unused2[0xE0]; // this part isn't checksummed, but is 0 filled on erasing autoload data
};

#pragma pack(pop)

static_assert(sizeof(DSiAutoLoad) == 0x100, "DSiAutoLoad wrong size");

ECL_EXPORT void ResetConsole(melonDS::NDS* nds, bool skipFw, u64 dsiTitleId)
{
	nds->Reset();

	if (skipFw || nds->NeedsDirectBoot())
	{
		if (nds->GetNDSCart())
		{
			nds->SetupDirectBoot("nds.rom");
		}
		else
		{
			auto* dsi = static_cast<melonDS::DSi*>(nds);

			// set warm boot flag
			dsi->I2C.GetBPTWL()->SetBootFlag(true);

			// setup "auto-load" feature
			DSiAutoLoad dsiAutoLoad;
			memset(&dsiAutoLoad, 0, sizeof(dsiAutoLoad));
			memcpy(dsiAutoLoad.ID, "TLNC", sizeof(dsiAutoLoad.ID));
			dsiAutoLoad.Unknown1 = 0x01;
			dsiAutoLoad.Length = 0x18;

			for (int i = 0; i < 8; i++)
			{
				dsiAutoLoad.NewTitleID[i] = dsiTitleId & 0xFF;
				dsiTitleId >>= 8;
			}

			dsiAutoLoad.Flags |= (0x03 << 1) | 0x01;
			dsiAutoLoad.Flags |= (1 << 4); // unknown bit, seems to be required to boot into games (errors otherwise?)
			dsiAutoLoad.CRC16 = melonDS::CRC16((u8*)&dsiAutoLoad.PrevTitleID, dsiAutoLoad.Length, 0xFFFF);
			memcpy(&nds->MainRAM[0x300], &dsiAutoLoad, sizeof(dsiAutoLoad));
		}
	}

	nds->Start();
}

struct ConsoleCreationArgs
{
	u8* NdsRomData;
	u32 NdsRomLength;

	u8* GbaRomData;
	u32 GbaRomLength;

	u8* Arm9BiosData;
	u32 Arm9BiosLength;

	u8* Arm7BiosData;
	u32 Arm7BiosLength;

	u8* FirmwareData;
	u32 FirmwareLength;

	u8* Arm9iBiosData;
	u32 Arm9iBiosLength;

	u8* Arm7iBiosData;
	u32 Arm7iBiosLength;

	u8* NandData;
	u32 NandLength;

	u8* DsiWareData;
	u32 DsiWareLength;

	u8* TmdData;
	u32 TmdLength;

	bool DSi;
	bool ClearNAND;
	bool SkipFW;
	bool FullDSiBIOSBoot;
	bool EnableDLDI;
	bool EnableDSiSDCard;
	bool DSiDSPHLE;

	bool EnableJIT;
	u32 MaxBlockSize;
	bool LiteralOptimizations;
	bool BranchOptimizations;

	int BitDepth;
	int Interpolation;

	int ThreeDeeRenderer;
	bool Threaded3D;
	int ScaleFactor;
	bool BetterPolygons;
	bool HiResCoordinates;

	int StartYear; // 0-99
	int StartMonth; // 1-12
	int StartDay; // 1-(28/29/30/31 depending on month/year)
	int StartHour; // 0-23
	int StartMinute; // 0-59
	int StartSecond; // 0-59

	FirmwareSettings FwSettings;
};

ECL_EXPORT melonDS::NDS* CreateConsole(ConsoleCreationArgs* args, char* error)
{
	try
	{
		// SD Cards are set to be 256MiB always
		constexpr u32 SD_CARD_SIZE = 256 * 1024 * 1024;

		auto bizUserData = std::make_unique<melonDS::Platform::BizUserData>();
		memset(bizUserData.get(), 0, sizeof(melonDS::Platform::BizUserData));

		std::unique_ptr<melonDS::NDSCart::CartCommon> ndsRom = nullptr;
		if (args->NdsRomData)
		{
			melonDS::NDSCart::NDSCartArgs cartArgs{};
			if (args->EnableDLDI)
			{
				cartArgs.SDCard = melonDS::FATStorageArgs
				{
					"dldi.bin",
					SD_CARD_SIZE,
					false,
					std::nullopt,
				};
			}

			ndsRom = melonDS::NDSCart::ParseROM(args->NdsRomData, args->NdsRomLength, bizUserData.get(), std::move(cartArgs));

			if (!ndsRom)
			{
				throw std::runtime_error("Failed to parse NDS ROM");
			}
		}

		std::unique_ptr<melonDS::GBACart::CartCommon> gbaRom = nullptr;
		if (args->GbaRomData)
		{
			auto gbaSram = CreateBlankGbaSram(args->GbaRomData, args->GbaRomLength);
			gbaRom = melonDS::GBACart::ParseROM(args->GbaRomData, args->GbaRomLength, gbaSram.first.get(), gbaSram.second, bizUserData.get());

			if (!gbaRom)
			{
				throw std::runtime_error("Failed to parse GBA ROM");
			}
		}

		auto arm9Bios = CreateBiosImage<melonDS::ARM9BIOSImage>(args->Arm9BiosData, args->Arm9BiosLength, melonDS::bios_arm9_bin);
		auto arm7Bios = CreateBiosImage<melonDS::ARM7BIOSImage>(args->Arm7BiosData, args->Arm7BiosLength, melonDS::bios_arm7_bin);
		auto firmware = CreateFirmware(args->FirmwareData, args->FirmwareLength, args->DSi, args->FwSettings);

		std::optional<melonDS::JITArgs> jitArgs = std::nullopt;
		if (args->EnableJIT)
		{
			jitArgs = melonDS::JITArgs
			{
				args->MaxBlockSize,
				args->LiteralOptimizations,
				args->BranchOptimizations,
				false,
			};
		}

		auto bitDepth = static_cast<melonDS::AudioBitDepth>(args->BitDepth);
		auto interpolation = static_cast<melonDS::AudioInterpolation>(args->Interpolation);

		std::unique_ptr<melonDS::Renderer3D> renderer3d;
		switch (args->ThreeDeeRenderer)
		{
			case 0:
			{
				auto softRenderer = std::make_unique<melonDS::SoftRenderer>();
				// SetThreaded needs the nds GPU field, so can't do this now
				// softRenderer->SetThreaded(args->Threaded3D, nds->GPU);
				renderer3d = std::move(softRenderer);
				break;
			}
			case 1:
			{
				auto glRenderer = melonDS::GLRenderer::New();
				glRenderer->SetRenderSettings(args->BetterPolygons, args->ScaleFactor);
				renderer3d = std::move(glRenderer);
				break;
			}
			case 2:
			{
				auto computeRenderer = melonDS::ComputeRenderer::New();
				computeRenderer->SetRenderSettings(args->ScaleFactor, args->HiResCoordinates);
				renderer3d = std::move(computeRenderer);
				break;
			}
			default:
				throw std::runtime_error("Unknown 3DRenderer!");
		}

		int currentShader, shadersCount;
		while (renderer3d->NeedsShaderCompile())
		{
			renderer3d->ShaderCompileStep(currentShader, shadersCount);
		}

		std::unique_ptr<melonDS::NDS> nds;

		if (args->DSi)
		{
			auto arm9iBios = CreateBiosImage<melonDS::DSiBIOSImage>(args->Arm9iBiosData, args->Arm9iBiosLength);
			auto arm7iBios = CreateBiosImage<melonDS::DSiBIOSImage>(args->Arm7iBiosData, args->Arm7iBiosLength);

			// upstream applies this patch to overwrite the reset vector for non-full boots
			if (!args->FullDSiBIOSBoot)
			{
				static const u8 dsiBiosPatch[] = { 0xFE, 0xFF, 0xFF, 0xEA };
				memcpy(arm9iBios->data(), dsiBiosPatch, sizeof(dsiBiosPatch));
				memcpy(arm7iBios->data(), dsiBiosPatch, sizeof(dsiBiosPatch));
			}

			auto nandImage = CreateNandImage(
				args->NandData, args->NandLength, arm7iBios,
				args->FwSettings, args->ClearNAND,
				args->DsiWareData, args->DsiWareLength, args->TmdData, args->TmdLength);

			std::optional<melonDS::FATStorage> dsiSdCard = std::nullopt;
			if (args->EnableDSiSDCard)
			{
				dsiSdCard = melonDS::FATStorage("dsisd.bin", SD_CARD_SIZE, false, std::nullopt);
			}

			melonDS::DSiArgs dsiArgs
			{
				std::move(arm9Bios),
				std::move(arm7Bios),
				std::move(firmware),
				std::move(jitArgs),
				bitDepth,
				interpolation,
				44100.0f,
				std::nullopt,
				std::move(renderer3d),
				// dsi specific args
				std::move(arm9iBios),
				std::move(arm7iBios),
				std::move(nandImage),
				std::move(dsiSdCard),
				args->FullDSiBIOSBoot,
				args->DSiDSPHLE,
			};

			nds = std::make_unique<melonDS::DSi>(std::move(dsiArgs), bizUserData.get());
		}
		else
		{
			melonDS::NDSArgs ndsArgs
			{
				std::move(arm9Bios),
				std::move(arm7Bios),
				std::move(firmware),
				std::move(jitArgs),
				bitDepth,
				interpolation,
				44100.0f,
				std::nullopt,
				std::move(renderer3d)
			};

			nds = std::make_unique<melonDS::NDS>(std::move(ndsArgs), bizUserData.get());
		}

		nds->SetNDSCart(std::move(ndsRom));
		nds->SetGBACart(std::move(gbaRom));

		if (args->ThreeDeeRenderer == 0)
		{
			auto& softRenderer = static_cast<melonDS::SoftRenderer&>(nds->GetRenderer3D());
			softRenderer.SetThreaded(args->Threaded3D, nds->GPU);
		}

		nds->RTC.SetDateTime(args->StartYear, args->StartMonth, args->StartDay,
			args->StartHour, args->StartMinute, args->StartSecond);

		u64 dsiWareId = 0;
		if (args->DsiWareLength >= 0x238)
		{
			for (int i = 0; i < 8; i++)
			{
				dsiWareId <<= 8;
				dsiWareId |= args->DsiWareData[0x237 - i];
			}
		}

		ResetConsole(nds.get(), args->SkipFW, dsiWareId);

		bizUserData.release();
		CurrentNDS = nds.release();
		return CurrentNDS;
	}
	catch (const std::exception& e)
	{
		strncpy(error, e.what(), 1024);
		return nullptr;
	}
}

}
