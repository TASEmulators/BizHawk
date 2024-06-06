#include "NDS.h"
#include "NDSCart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "DSi_TMD.h"
#include "GPU3D_OpenGL.h"
#include "GPU3D_Compute.h"
#include "CRC32.h"
#include "FreeBIOS.h"
#include "SPI.h"

#include "BizPlatform/BizFile.h"
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

static void ClearNandSavs(melonDS::DSi_NAND::NANDMount& mount, u32 category)
{
	std::vector<u32> titlelist;
	mount.ListTitles(category, titlelist);

	char fname[128];
	for (auto& title : titlelist)
	{
		snprintf(fname, sizeof(fname), "0:/title/%08x/%08x/data/public.sav", category, title);
		mount.RemoveFile(fname);
		snprintf(fname, sizeof(fname), "0:/title/%08x/%08x/data/private.sav", category, title);
		mount.RemoveFile(fname);
		snprintf(fname, sizeof(fname), "0:/title/%08x/%08x/data/banner.sav", category, title);
		mount.RemoveFile(fname);
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
		melonDS::DSi_NAND::DSiSerialData serialData;
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

		if (clearNand)
		{
			// clear out DSiWare
			constexpr u32 DSIWARE_CATEGORY = 0x00030004;

			std::vector<u32> titlelist;
			mount.ListTitles(DSIWARE_CATEGORY, titlelist);

			for (auto& title : titlelist)
			{
				mount.DeleteTitle(DSIWARE_CATEGORY, title);
			}

			// clear out .sav files of builtin apps / title management / system menu
			constexpr u32 BUILTIN_APP_CATEGORY = 0x00030005;
			constexpr u32 TITLE_MANAGEMENT_CATEGORY = 0x00030015;
			constexpr u32 SYSTEM_MENU_CATEGORY = 0x00030017;

			ClearNandSavs(mount, BUILTIN_APP_CATEGORY);
			ClearNandSavs(mount, TITLE_MANAGEMENT_CATEGORY);
			ClearNandSavs(mount, SYSTEM_MENU_CATEGORY);

			// clear out some other misc files
			mount.RemoveFile("0:/shared2/launcher/wrap.bin");
			mount.RemoveFile("0:/shared2/0000");
			mount.RemoveFile("0:/sys/log/product.log");
			mount.RemoveFile("0:/sys/log/sysmenu.log");
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
		std::unique_ptr<melonDS::NDSCart::CartCommon> ndsRom = nullptr;
		if (args->NdsRomData)
		{
			ndsRom = melonDS::NDSCart::ParseROM(args->NdsRomData, args->NdsRomLength, std::nullopt);

			if (!ndsRom)
			{
				throw std::runtime_error("Failed to parse NDS ROM");
			}
		}

		std::unique_ptr<melonDS::GBACart::CartCommon> gbaRom = nullptr;
		if (args->GbaRomData)
		{
			auto gbaSram = CreateBlankGbaSram(args->GbaRomData, args->GbaRomLength);
			gbaRom = melonDS::GBACart::ParseROM(args->GbaRomData, args->GbaRomLength, gbaSram.first.get(), gbaSram.second);

			if (!gbaRom)
			{
				throw std::runtime_error("Failed to parse GBA ROM");
			}
		}

		auto arm9Bios = CreateBiosImage<melonDS::ARM9BIOSImage>(args->Arm9BiosData, args->Arm9BiosLength, melonDS::bios_arm9_bin);
		auto arm7Bios = CreateBiosImage<melonDS::ARM7BIOSImage>(args->Arm7BiosData, args->Arm7BiosLength, melonDS::bios_arm7_bin);
		auto firmware = CreateFirmware(args->FirmwareData, args->FirmwareLength, args->DSi, args->FwSettings);

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
			static const u8 dsiBiosPatch[] = { 0xFE, 0xFF, 0xFF, 0xEA };
			memcpy(arm9iBios->data(), dsiBiosPatch, sizeof(dsiBiosPatch));
			memcpy(arm7iBios->data(), dsiBiosPatch, sizeof(dsiBiosPatch));

			auto nandImage = CreateNandImage(
				args->NandData, args->NandLength, arm7iBios,
				args->FwSettings, args->ClearNAND,
				args->DsiWareData, args->DsiWareLength, args->TmdData, args->TmdLength);

			melonDS::DSiArgs dsiArgs
			{
				std::move(ndsRom),
				std::move(gbaRom),
				std::move(arm9Bios),
				std::move(arm7Bios),
				std::move(firmware),
				std::nullopt,
				bitDepth,
				interpolation,
				std::nullopt,
				std::move(renderer3d),
				// dsi specific args
				std::move(arm9iBios),
				std::move(arm7iBios),
				std::move(nandImage),
				std::nullopt,
				false,
			};

			nds = std::make_unique<melonDS::DSi>(std::move(dsiArgs));
		}
		else
		{
			melonDS::NDSArgs ndsArgs
			{
				std::move(ndsRom),
				std::move(gbaRom),
				std::move(arm9Bios),
				std::move(arm7Bios),
				std::move(firmware),
				std::nullopt,
				bitDepth,
				interpolation,
				std::nullopt,
				std::move(renderer3d)
			};

			nds = std::make_unique<melonDS::NDS>(std::move(ndsArgs));
		}

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
