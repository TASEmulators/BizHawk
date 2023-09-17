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
	if (DSi_NAND::Init(&DSi::ARM7iBIOS[0x8308]))
	{
		DSi_NAND::ImportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PublicSav, "public.sav");
		DSi_NAND::ImportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PrivateSav, "private.sav");
		DSi_NAND::ImportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_BannerSav, "banner.sav");
		DSi_NAND::DeInit();
	}
}

ECL_EXPORT void ExportDSiWareSavs(u32 titleId)
{
	if (DSi_NAND::Init(&DSi::ARM7iBIOS[0x8308]))
	{
		DSi_NAND::ExportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PublicSav, "public.sav");
		DSi_NAND::ExportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_PrivateSav, "private.sav");
		DSi_NAND::ExportTitleData(DSIWARE_CATEGORY, titleId, DSi_NAND::TitleData_BannerSav, "banner.sav");
		DSi_NAND::DeInit();
	}
}

ECL_EXPORT void DSiWareSavsLength(u32 titleId, u32* publicSavSize, u32* privateSavSize, u32* bannerSavSize)
{
	*publicSavSize = *privateSavSize = *bannerSavSize = 0;
	if (DSi_NAND::Init(&DSi::ARM7iBIOS[0x8308]))
	{
		u32 version;
		NDSHeader header{};

		DSi_NAND::GetTitleInfo(DSIWARE_CATEGORY, titleId, version, &header, nullptr);
		*publicSavSize = header.DSiPublicSavSize;
		*privateSavSize = header.DSiPrivateSavSize;
		*bannerSavSize = (header.AppFlags & 0x04) ? 0x4000 : 0;
		DSi_NAND::DeInit();
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

}
