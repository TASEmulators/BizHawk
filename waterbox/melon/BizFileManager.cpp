#include "NDS.h"
#include "DSi_NAND.h"
#include "DSi_TMD.h"
#include "CRC32.h"

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

const char* InitNAND(bool clearNand, bool dsiWare)
{
	auto dsiBios7Path = Platform::GetConfigString(Platform::ConfigEntry::DSi_BIOS7Path);
	auto bios7i = Platform::OpenFile(dsiBios7Path, Platform::FileMode::Read);
	if (!bios7i)
	{
		return "Failed to obtain BIOS7i!";
	}

	u8 es_keyY[16]{};
	Platform::FileSeek(bios7i, 0x8308, Platform::FileSeekOrigin::Start);
	Platform::FileRead(es_keyY, 16, 1, bios7i);
	Platform::CloseFile(bios7i);

	if (!DSi_NAND::Init(es_keyY))
	{
		return "Failed to init DSi NAND!";
	}

	if (clearNand)
	{
		std::vector<u32> titlelist;
		DSi_NAND::ListTitles(DSIWARE_CATEGORY, titlelist);

		for (auto& title : titlelist)
		{
			DSi_NAND::DeleteTitle(DSIWARE_CATEGORY, title);
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

		if (tmdData->second < sizeof(DSi_TMD::TitleMetadata))
		{
			return "TMD is too small!";
		}

		DSi_TMD::TitleMetadata tmd;
		memcpy(&tmd, tmdData->first.get(), sizeof(DSi_TMD::TitleMetadata));
		if (!DSi_NAND::ImportTitle(rom->first.get(), rom->second, tmd, false))
		{
			DSi_NAND::DeInit();
			return "Loading DSiWare failed!";
		}
	}

	DSi_NAND::DeInit();
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
