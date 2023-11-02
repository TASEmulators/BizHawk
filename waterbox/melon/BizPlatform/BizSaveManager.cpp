#include "NDS.h"
#include "NDSCart.h"
#include "GBACart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "Platform.h"

namespace Platform
{

constexpr u32 DSIWARE_CATEGORY = 0x00030004;

static bool NdsSaveRamIsDirty = false;
static bool GbaSaveRamIsDirty = false;

ECL_EXPORT void PutSaveRam(u8* data, u32 len)
{
	const u32 ndsSaveLen = NDSCart::GetSaveMemoryLength();
	const u32 gbaSaveLen = GBACart::GetSaveMemoryLength();

	if (len >= ndsSaveLen)
	{
		NDS::LoadSave(data, len);
		NdsSaveRamIsDirty = false;

		data += ndsSaveLen;
		len -= ndsSaveLen;

		if (gbaSaveLen && len >= gbaSaveLen)
		{
			GBACart::LoadSave(data, gbaSaveLen);
			GbaSaveRamIsDirty = false;
		}
	}
}

ECL_EXPORT void GetSaveRam(u8* data)
{
	const u32 ndsSaveLen = NDSCart::GetSaveMemoryLength();
	const u32 gbaSaveLen = GBACart::GetSaveMemoryLength();

	if (ndsSaveLen)
	{
		memcpy(data, NDSCart::GetSaveMemory(), ndsSaveLen);
		NdsSaveRamIsDirty = false;
	}

	if (gbaSaveLen)
	{
		memcpy(data + ndsSaveLen, GBACart::GetSaveMemory(), gbaSaveLen);
		GbaSaveRamIsDirty = false;
	}
}

ECL_EXPORT u32 GetSaveRamLength()
{
	return NDSCart::GetSaveMemoryLength() + GBACart::GetSaveMemoryLength();
}

ECL_EXPORT bool SaveRamIsDirty()
{
	return NdsSaveRamIsDirty || GbaSaveRamIsDirty;
}

ECL_EXPORT void ImportDSiWareSavs(u32 titleId)
{
	auto& nand = DSi::NANDImage;
	if (nand && *nand)
	{
		if (auto mount = DSi_NAND::NANDMount(*nand))
		{
			mount.ImportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PublicSav, "public.sav");
			mount.ImportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PrivateSav, "private.sav");
			mount.ImportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_BannerSav, "banner.sav");
		}
	}
}

ECL_EXPORT void ExportDSiWareSavs(u32 titleId)
{
	auto& nand = DSi::NANDImage;
	if (nand && *nand)
	{
		if (auto mount = DSi_NAND::NANDMount(*nand))
		{
			mount.ExportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PublicSav, "public.sav");
			mount.ExportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PrivateSav, "private.sav");
			mount.ExportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_BannerSav, "banner.sav");
		}
	}
}

ECL_EXPORT void DSiWareSavsLength(u32 titleId, u32* publicSavSize, u32* privateSavSize, u32* bannerSavSize)
{
	*publicSavSize = *privateSavSize = *bannerSavSize = 0;

	auto& nand = DSi::NANDImage;
	if (nand && *nand)
	{
		if (auto mount = DSi_NAND::NANDMount(*nand))
		{
			u32 version;
			NDSHeader header{};

			mount.GetTitleInfo(DSIWARE_CATEGORY, titleId, version, &header, nullptr);
			*publicSavSize = header.DSiPublicSavSize;
			*privateSavSize = header.DSiPrivateSavSize;
			*bannerSavSize = (header.AppFlags & 0x04) ? 0x4000 : 0;
		}
	}
}

// TODO - I don't like this approach with NAND
// Perhaps instead it would be better to use FileFlush to write to disk
// (guarded by frontend determinism switch, of course) 

ECL_EXPORT u32 GetNANDSize()
{
	auto& nand = DSi::NANDImage;
	if (nand && *nand)
	{
		return nand->GetLength();
	}

	return 0;
}

ECL_EXPORT void GetNANDData(u8* buf)
{
	auto& nand = DSi::NANDImage;
	if (nand && *nand)
	{
		auto len = nand->GetLength();
		auto file = nand->GetFile();
		Platform::FileRewind(file);
		Platform::FileRead(buf, 1, len, file);
	}
}

void WriteNDSSave(const u8* savedata, u32 savelen, u32 writeoffset, u32 writelen)
{
	NdsSaveRamIsDirty = true;
}

void WriteGBASave(const u8* savedata, u32 savelen, u32 writeoffset, u32 writelen)
{
	GbaSaveRamIsDirty = true;
}

void WriteFirmware(const SPI_Firmware::Firmware& firmware, u32 writeoffset, u32 writelen)
{	
}

void WriteDateTime(int year, int month, int day, int hour, int minute, int second)
{
}

}
