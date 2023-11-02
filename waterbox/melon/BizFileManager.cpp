#include "NDS.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "DSi_TMD.h"
#include "CRC32.h"
#include "FreeBIOS.h"
#include "SPI.h"

#include "BizFileManager.h"

namespace FileManager
{

constexpr u32 DSIWARE_CATEGORY = 0x00030004;

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

// Inits NDS BIOS7 and BIOS9
const char* InitNDSBIOS()
{
	if (NDS::ConsoleType == 1 || Platform::GetConfigBool(Platform::ExternalBIOSEnable))
	{
		auto bios7 = Platform::OpenFile("bios7.bin", Platform::FileMode::Read);
		if (!bios7)
		{
			return "Failed to obtain BIOS7!";
		}

		if (Platform::FileLength(bios7) != sizeof(NDS::ARM7BIOS))
		{
			Platform::CloseFile(bios7);
			return "Incorrectly sized BIOS7!";
		}

		Platform::FileRewind(bios7);
		Platform::FileRead(NDS::ARM7BIOS, sizeof(NDS::ARM7BIOS), 1, bios7);
		Platform::CloseFile(bios7);

		auto bios9 = Platform::OpenFile("bios9.bin", Platform::FileMode::Read);
		if (!bios9)
		{
			return "Failed to obtain BIOS9!";
		}

		if (Platform::FileLength(bios9) != sizeof(NDS::ARM9BIOS))
		{
			Platform::CloseFile(bios9);
			return "Incorrectly sized BIOS9!";
		}

		Platform::FileRewind(bios9);
		Platform::FileRead(NDS::ARM9BIOS, sizeof(NDS::ARM9BIOS), 1, bios9);
		Platform::CloseFile(bios9);
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

	header.UserSettingsOffset = (0x7FE00 & firmware.Mask()) >> 3;

	if (isDSiFw)
	{
		memset(&header.Unknown0[0], 0, (uintptr_t)&header.Unused2[0] - (uintptr_t)&header.Unknown0[0]);
		header.Unused2[0] = header.Unused2[1] = 0xFF;
	}

	memcpy(&header.WifiConfigLength, &defaultHeader.WifiConfigLength, (uintptr_t)&header.Unknown4 - (uintptr_t)&header.WifiConfigLength);
	memset(&header.Unknown4, 0xFF, (uintptr_t)&header.Unused7 - (uintptr_t)&header.Unknown4 + 1);

	if (isDSiFw)
	{
		header.WifiBoard = defaultHeader.WifiBoard;
		header.WifiFlash = defaultHeader.WifiFlash;
	}

	header.UpdateChecksum();

	auto& aps = firmware.AccessPoints();
	aps[0] = SPI_Firmware::WifiAccessPoint(NDS::ConsoleType);
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

static void SetFirmwareSettings(SPI_Firmware::UserData& userData, FirmwareSettings& fwSettings, bool dsiFw)
{
	memset(userData.Bytes, 0, 0x74);
	userData.Version = 5;
	FixFirmwareTouchscreenCalibration(userData);

	userData.NameLength = fwSettings.UsernameLength;
	memcpy(userData.Nickname, fwSettings.Username, sizeof(fwSettings.Username));
	userData.Settings = fwSettings.Language | SPI_Firmware::BacklightLevel::Max | 0xFC00;
	userData.BirthdayMonth = fwSettings.BirthdayMonth;
	userData.BirthdayDay = fwSettings.BirthdayDay;
	userData.FavoriteColor = fwSettings.Color;
	userData.MessageLength = fwSettings.MessageLength;
	memcpy(userData.Message, fwSettings.Message, sizeof(fwSettings.Message));

	if (userData.ExtendedSettings.Unknown0 == 1)
	{
		userData.ExtendedSettings.ExtendedLanguage = static_cast<SPI_Firmware::Language>(fwSettings.Language & SPI_Firmware::Language::Reserved);
		memset(userData.ExtendedSettings.Unused0, dsiFw ? 0x00 : 0xFF, sizeof(userData.ExtendedSettings.Unused0));
	}
	else
	{
		memset(userData.Unused3, 0xFF, sizeof(userData.Unused3));
	}

	// only extended settings should have Chinese
	if ((userData.Settings & SPI_Firmware::Language::Reserved) == SPI_Firmware::Language::Chinese)
	{
		userData.Settings &= SPI_Firmware::Language::Reserved;
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
			// Fix header and AP points
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
			SetFirmwareSettings(userData, fwSettings, firmware.Header().ConsoleType == SPI_Firmware::FirmwareConsoleType::DSi);
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

	auto bios7i = Platform::OpenFile("bios7i.bin", Platform::FileMode::Read);
	if (!bios7i)
	{
		return "Failed to obtain BIOS7i!";
	}

	if (Platform::FileLength(bios7i) != sizeof(DSi::ARM7iBIOS))
	{
		Platform::CloseFile(bios7i);
		return "Incorrectly sized BIOS7i!";
	}

	Platform::FileRewind(bios7i);
	Platform::FileRead(DSi::ARM7iBIOS, sizeof(DSi::ARM7iBIOS), 1, bios7i);
	Platform::CloseFile(bios7i);

	auto bios9i = Platform::OpenFile("bios9i.bin", Platform::FileMode::Read);
	if (!bios9i)
	{
		return "Failed to obtain BIOS9i!";
	}

	if (Platform::FileLength(bios9i) != sizeof(DSi::ARM9iBIOS))
	{
		Platform::CloseFile(bios9i);
		return "Incorrectly sized BIOS9i!";
	}

	Platform::FileRewind(bios9i);
	Platform::FileRead(DSi::ARM9iBIOS, sizeof(DSi::ARM9iBIOS), 1, bios9i);
	Platform::CloseFile(bios9i);

	if (!Platform::GetConfigBool(Platform::DSi_FullBIOSBoot))
	{
		static const u8 branch[] = { 0xFE, 0xFF, 0xFF, 0xEA };
		memcpy(DSi::ARM7iBIOS, branch, sizeof(branch));
		memcpy(DSi::ARM9iBIOS, branch, sizeof(branch));
	}

	return nullptr;
}

static void SanitizeNANDSettings(DSi_NAND::DSiFirmwareSystemSettings& settings)
{
	memset(settings.Zero00, 0, sizeof(settings.Zero00));
	settings.Version = 1;
	settings.UpdateCounter = 0;
	memset(settings.Zero01, 0, sizeof(settings.Zero01));
	settings.BelowRAMAreaSize = 0x128;
	settings.ConfigFlags = 0x0100000F;
	settings.Zero02 = 0;
	settings.CountryCode = 49; // United States
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

		if (fwSettings.OverrideSettings)
		{
			SanitizeNANDSettings(settings);
			memset(settings.Nickname, 0, sizeof(settings.Nickname));
			memcpy(settings.Nickname, fwSettings.Username, fwSettings.UsernameLength);
			settings.Language = static_cast<SPI_Firmware::Language>(fwSettings.Language & SPI_Firmware::Language::Reserved);
			settings.FavoriteColor = fwSettings.Color;
			settings.BirthdayMonth = fwSettings.BirthdayMonth;
			settings.BirthdayDay = fwSettings.BirthdayDay;
			memset(settings.Message, 0, sizeof(settings.Message));
			memcpy(settings.Message, fwSettings.Message, fwSettings.MessageLength);
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
			auto tmdData = GetFileData("tmd.rom");
			if (!tmdData)
			{
				return "Failed to obtain TMD!";
			}

			if (tmdData->second < sizeof(DSi_TMD::TitleMetadata))
			{
				return "TMD is too small!";
			}

			DSi_TMD::TitleMetadata tmd;
			memcpy(&tmd, tmdData->first.get(), sizeof(DSi_TMD::TitleMetadata));

			if (!mount.ImportTitle("dsiware.rom", tmd, false))
			{
				return "Loading DSiWare failed!";
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

}
