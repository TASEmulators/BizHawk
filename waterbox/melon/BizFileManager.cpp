#include "NDS.h"
#include "NDSCart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "DSi_TMD.h"
#include "CRC32.h"
#include "FreeBIOS.h"
#include "SPI.h"

#include "BizFileManager.h"

// need to peek at these internals
namespace DSi_BPTWL
{
	extern u8 Registers[0x100];
}

namespace FileManager
{

constexpr u32 DSIWARE_CATEGORY = 0x00030004;
static u8 DSiWareID[8];

static std::optional<std::pair<std::unique_ptr<u8[]>, size_t>> GetFileData(std::string path)
{
	auto file = Platform::OpenFile(path, Platform::FileMode::Read);
	if (!file)
	{
		return std::nullopt;
	}

	size_t size = Platform::FileLength(file);
	auto data = std::make_unique<u8[]>(size);

	Platform::FileRewind(file);
	Platform::FileRead(data.get(), size, 1, file);
	Platform::CloseFile(file);

	return std::make_pair(std::move(data), size);
}

static bool LoadBIOS(const char* path, u8* buffer, size_t len)
{
	auto bios = Platform::OpenFile(path, Platform::FileMode::Read);
	if (!bios)
	{
		return false;
	}

	if (Platform::FileLength(bios) != len)
	{
		Platform::CloseFile(bios);
		return false;
	}

	Platform::FileRewind(bios);
	auto read = Platform::FileRead(buffer, len, 1, bios);
	Platform::CloseFile(bios);
	return read == len;
}

// Inits NDS BIOS7 and BIOS9
const char* InitNDSBIOS()
{
	if (NDS::ConsoleType == 1 || Platform::GetConfigBool(Platform::ExternalBIOSEnable))
	{
		if (!LoadBIOS("bios7.bin", NDS::ARM7BIOS, sizeof(NDS::ARM7BIOS)))
		{
			return "Failed to load BIOS7!";
		}

		if (!LoadBIOS("bios9.bin", NDS::ARM9BIOS, sizeof(NDS::ARM9BIOS)))
		{
			return "Failed to load BIOS9!";
		}
	}
	else
	{
		memcpy(NDS::ARM7BIOS, bios_arm7_bin, sizeof(bios_arm7_bin));
		memcpy(NDS::ARM9BIOS, bios_arm9_bin, sizeof(bios_arm9_bin));
	}

	return nullptr;
}

static void SanitizeExternalFirmware(SPI_Firmware::Firmware& firmware)
{
	auto& header = firmware.Header();

	const bool isDSiFw = header.ConsoleType == SPI_Firmware::FirmwareConsoleType::DSi;
	const auto defaultHeader = SPI_Firmware::FirmwareHeader(isDSiFw);

	// the user data offset won't necessarily be 0x7FE00 & Mask, DSi/iQue use 0x7FC00 & Mask instead
	// but we don't want to crash due to an invalid offset
	const auto maxUserDataOffset = 0x7FE00 & firmware.Mask();
	if (firmware.UserDataOffset() > maxUserDataOffset)
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

	auto& aps = firmware.AccessPoints();
	aps[0] = SPI_Firmware::WifiAccessPoint(isDSiFw);
	aps[1] = SPI_Firmware::WifiAccessPoint();
	aps[2] = SPI_Firmware::WifiAccessPoint();

	if (isDSiFw)
	{
		auto& exAps = firmware.ExtendedAccessPoints();
		exAps[0] = SPI_Firmware::ExtendedWifiAccessPoint();
		exAps[1] = SPI_Firmware::ExtendedWifiAccessPoint();
		exAps[2] = SPI_Firmware::ExtendedWifiAccessPoint();
	}
}

static void FixFirmwareTouchscreenCalibration(SPI_Firmware::UserData& userData)
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

static void SetFirmwareSettings(SPI_Firmware::UserData& userData, FirmwareSettings& fwSettings)
{
	memset(userData.Bytes, 0, 0x74);

	userData.Version = 5;
	FixFirmwareTouchscreenCalibration(userData);

	userData.NameLength = fwSettings.UsernameLength;
	memcpy(userData.Nickname, fwSettings.Username, sizeof(fwSettings.Username));
	userData.Settings = fwSettings.Language | SPI_Firmware::BacklightLevel::Max | 0xEC00;
	userData.BirthdayMonth = fwSettings.BirthdayMonth;
	userData.BirthdayDay = fwSettings.BirthdayDay;
	userData.FavoriteColor = fwSettings.Color;
	userData.MessageLength = fwSettings.MessageLength;
	memcpy(userData.Message, fwSettings.Message, sizeof(fwSettings.Message));

	if (userData.ExtendedSettings.Unknown0 == 1)
	{
		userData.ExtendedSettings.ExtendedLanguage = static_cast<SPI_Firmware::Language>(fwSettings.Language & SPI_Firmware::Language::Reserved);
		memset(userData.ExtendedSettings.Unused0, 0, sizeof(userData.ExtendedSettings.Unused0));

		if (!((1 << static_cast<u8>(userData.ExtendedSettings.ExtendedLanguage)) & userData.ExtendedSettings.SupportedLanguageMask))
		{
			userData.ExtendedSettings.ExtendedLanguage = SPI_Firmware::Language::English;
			userData.Settings &= ~SPI_Firmware::Language::Reserved;
			userData.Settings |= SPI_Firmware::Language::English;
		}
	}
	else
	{
		memset(userData.Unused3, 0xFF, sizeof(userData.Unused3));
	}

	// only extended settings should have Chinese / Korean
	// note that SPI_Firmware::Language::Reserved is Korean, so it's valid to have language set to that
	if ((userData.Settings & SPI_Firmware::Language::Reserved) >= SPI_Firmware::Language::Chinese)
	{
		userData.Settings &= ~SPI_Firmware::Language::Reserved;
		userData.Settings |= SPI_Firmware::Language::English;
	}
}

// Inits NDS firmware
const char* InitFirmware(FirmwareSettings& fwSettings)
{
	auto firmware = SPI_Firmware::Firmware(NDS::ConsoleType);

	if (Platform::GetConfigBool(Platform::ExternalBIOSEnable))
	{
		auto fw = Platform::OpenFile("firmware.bin", Platform::FileMode::Read);
		if (!fw)
		{
			return "Failed to obtain firmware!";
		}

		firmware = SPI_Firmware::Firmware(fw);

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
		return "Failed to load firmware!";
	}

	for (SPI_Firmware::UserData& userData : firmware.UserData())
	{
		FixFirmwareTouchscreenCalibration(userData);
		userData.UpdateChecksum();
	}

	if (fwSettings.OverrideSettings)
	{
		for (SPI_Firmware::UserData& userData : firmware.UserData())
		{
			SetFirmwareSettings(userData, fwSettings);
			userData.UpdateChecksum();
		}

		SPI_Firmware::MacAddress mac;
		Platform::GetConfigArray(Platform::Firm_MAC, &mac);
		auto& header = firmware.Header();
		header.MacAddress = mac;
		header.UpdateChecksum();
	}

	if (!SPI_Firmware::InstallFirmware(std::move(firmware)))
	{
		return "Failed to install firmware!";
	}

	return nullptr;
}

// Inits DSi BIOS7i and BIOS9i
const char* InitDSiBIOS()
{
	if (NDS::ConsoleType == 0)
	{
		return "Tried to init DSi BIOSes in NDS mode";
	}

	if (!LoadBIOS("bios7i.bin", DSi::ARM7iBIOS, sizeof(DSi::ARM7iBIOS)))
	{
		return "Failed to load BIOS7i!";
	}

	if (!LoadBIOS("bios9i.bin", DSi::ARM9iBIOS, sizeof(DSi::ARM9iBIOS)))
	{
		return "Failed to load BIOS9i!";
	}

	if (!Platform::GetConfigBool(Platform::DSi_FullBIOSBoot))
	{
		// upstream applies this patch, for whatever reason
		static const u8 patch[] = { 0xFE, 0xFF, 0xFF, 0xEA };
		memcpy(DSi::ARM7iBIOS, patch, sizeof(patch));
		memcpy(DSi::ARM9iBIOS, patch, sizeof(patch));
	}

	return nullptr;
}

static u8 GetDefaultCountryCode(DSi_NAND::ConsoleRegion region)
{
	// TODO: CountryCode probably should be configurable
	// these defaults are also completely arbitrary
	switch (region)
	{
		case DSi_NAND::ConsoleRegion::Japan: return 0x01; // Japan
		case DSi_NAND::ConsoleRegion::USA: return 0x31; // United States
		case DSi_NAND::ConsoleRegion::Europe: return 0x6E; // United Kingdom
		case DSi_NAND::ConsoleRegion::Australia: return 0x41; // Australia
		case DSi_NAND::ConsoleRegion::China: return 0xA0; // China
		case DSi_NAND::ConsoleRegion::Korea: return 0x88; // Korea
		default: return 0x31; // ???
	}
}

static void SanitizeNANDSettings(DSi_NAND::DSiFirmwareSystemSettings& settings, DSi_NAND::ConsoleRegion region)
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

const char* InitNAND(FirmwareSettings& fwSettings, bool clearNand, bool dsiWare)
{
	auto nand = DSi_NAND::NANDImage(Platform::OpenFile("nand.bin", Platform::FileMode::ReadWrite), &DSi::ARM7iBIOS[0x8308]);
	if (!nand)
	{
		return "Failed to parse DSi NAND!";
	}

	{
		auto mount = DSi_NAND::NANDMount(nand);
		if (!mount)
		{
			return "Failed to mount DSi NAND!";
		}

		DSi_NAND::DSiFirmwareSystemSettings settings{};
		if (!mount.ReadUserData(settings))
		{
			return "Failed to read DSi NAND user data";
		}

		// serial data will contain the NAND's region
		DSi_NAND::DSiSerialData serialData;
		if (!mount.ReadSerialData(serialData))
		{
			return "Failed to obtain serial data!";
		}

		if (fwSettings.OverrideSettings)
		{
			SanitizeNANDSettings(settings, serialData.Region);
			memset(settings.Nickname, 0, sizeof(settings.Nickname));
			memcpy(settings.Nickname, fwSettings.Username, fwSettings.UsernameLength * 2);
			settings.Language = static_cast<SPI_Firmware::Language>(fwSettings.Language & SPI_Firmware::Language::Reserved);
			settings.FavoriteColor = fwSettings.Color;
			settings.BirthdayMonth = fwSettings.BirthdayMonth;
			settings.BirthdayDay = fwSettings.BirthdayDay;
			memset(settings.Message, 0, sizeof(settings.Message));
			memcpy(settings.Message, fwSettings.Message, fwSettings.MessageLength * 2);

			if (!((1 << static_cast<u8>(settings.Language)) & serialData.SupportedLanguages))
			{
				// English is valid among all NANDs
				settings.Language = SPI_Firmware::Language::English;
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
			return "Failed to write DSi NAND user data";
		}

		if (clearNand)
		{
			std::vector<u32> titlelist;
			mount.ListTitles(DSIWARE_CATEGORY, titlelist);

			for (auto& title : titlelist)
			{
				mount.DeleteTitle(DSIWARE_CATEGORY, title);
			}
		}

		if (dsiWare)
		{
			auto rom = GetFileData("dsiware.rom");
			if (!rom)
			{
				return "Failed to obtain DSiWare ROM!";
			}

			auto tmdData = GetFileData("tmd.rom");
			if (!tmdData)
			{
				return "Failed to obtain TMD!";
			}

			if (tmdData->second != sizeof(DSi_TMD::TitleMetadata))
			{
				return "TMD is the wrong size!";
			}

			DSi_TMD::TitleMetadata tmd;
			memcpy(&tmd, tmdData->first.get(), sizeof(DSi_TMD::TitleMetadata));

			if (!mount.ImportTitle(rom->first.get(), rom->second, tmd, false))
			{
				return "Loading DSiWare failed!";
			}

			// verify that the imported title is supported by this NAND
			// it will not actually appear otherwise
			auto regionFlags = rom->second > 0x1B0 ? rom->first[0x1B0] : 0;
			if (!(regionFlags & (1 << static_cast<u8>(serialData.Region))))
			{
				return "Loaded NAND region does not support this DSiWare title!";
			}

			if (rom->second >= 0x238)
			{
				memcpy(&DSiWareID, &rom->first[0x230], sizeof(DSiWareID));
			}
		}
	}

	DSi::NANDImage = std::make_unique<DSi_NAND::NANDImage>(std::move(nand));
	return nullptr;
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
	u32 crc = CRC32(gbaRomData, gbaRomSize);
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

const char* InitCarts(bool gba)
{
	auto ndsRom = GetFileData("nds.rom");
	if (!ndsRom)
	{
		return "Failed to obtain NDS ROM!";
	}

	if (!NDS::LoadCart(ndsRom->first.get(), ndsRom->second, nullptr, 0))
	{
		return "Failed to load NDS ROM!";
	}

	if (ndsRom->second >= 0x15C && NDS::IsLoadedARM9BIOSBuiltIn())
	{
		// copy logo to the ARM9 bios
		// this is only needed for the builtin bios, which omits the logo
		memcpy(&NDS::ARM9BIOS[0x20], &ndsRom->first[0xC0], 0x9C);
	}

	if (gba)
	{
		auto gbaRom = GetFileData("gba.rom");
		if (!gbaRom)
		{
			return "Failed to obtain GBA ROM!";
		}

		auto gbaSram = CreateBlankGbaSram(gbaRom->first.get(), gbaRom->second);
		if (!NDS::LoadGBACart(gbaRom->first.get(), gbaRom->second, gbaSram.first.get(), gbaSram.second))
		{
			return "Failed to load GBA ROM!";
		}
	}

	return nullptr;
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

void SetupDirectBoot()
{
	if (NDSCart::Cart)
	{
		NDS::SetupDirectBoot("nds.rom");
	}
	else
	{
		// set warm boot flag
		DSi_BPTWL::Registers[0x70] |= 1;

		// setup "auto-load" feature
		DSiAutoLoad dsiAutoLoad;
		memset(&dsiAutoLoad, 0, sizeof(dsiAutoLoad));
		memcpy(dsiAutoLoad.ID, "TLNC", sizeof(dsiAutoLoad.ID));
		dsiAutoLoad.Unknown1 = 0x01;
		dsiAutoLoad.Length = 0x18;
		memcpy(dsiAutoLoad.NewTitleID, DSiWareID, sizeof(DSiWareID));
		dsiAutoLoad.Flags |= (0x03 << 1) | 0x01;
		dsiAutoLoad.Flags |= (1 << 4); // unknown bit, seems to be required to boot into games (errors otherwise?)
		dsiAutoLoad.CRC16 = SPI_Firmware::CRC16((u8*)&dsiAutoLoad.PrevTitleID, dsiAutoLoad.Length, 0xFFFF);
		memcpy(&NDS::MainRAM[0x300], &dsiAutoLoad, sizeof(dsiAutoLoad));
	}
}

}
