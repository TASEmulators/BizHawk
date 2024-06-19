#include "NDS.h"
#include "NDSCart.h"
#include "GBACart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "Platform.h"

namespace melonDS::Platform
{

constexpr u32 DSIWARE_CATEGORY = 0x00030004;

static bool NdsSaveRamIsDirty = false;
static bool GbaSaveRamIsDirty = false;

ECL_EXPORT void PutSaveRam(melonDS::NDS* nds, u8* data, u32 len)
{
	const u32 ndsSaveLen = nds->GetNDSSaveLength();
	const u32 gbaSaveLen = nds->GetGBASaveLength();

	if (len >= ndsSaveLen)
	{
		nds->SetNDSSave(data, len);
		NdsSaveRamIsDirty = false;

		if (gbaSaveLen && len >= (ndsSaveLen + gbaSaveLen))
		{
			// don't use SetGBASave! it will re-allocate the save buffer (bad!)
			// SetNDSSave is fine (and should be used)
			memcpy(nds->GetGBASave(), data + ndsSaveLen, gbaSaveLen);
			GbaSaveRamIsDirty = false;
		}
	}
}

ECL_EXPORT void GetSaveRam(melonDS::NDS* nds, u8* data)
{
	const u32 ndsSaveLen = nds->GetNDSSaveLength();
	const u32 gbaSaveLen = nds->GetGBASaveLength();

	if (ndsSaveLen)
	{
		memcpy(data, nds->GetNDSSave(), ndsSaveLen);
		NdsSaveRamIsDirty = false;
	}

	if (gbaSaveLen)
	{
		memcpy(data + ndsSaveLen, nds->GetGBASave(), gbaSaveLen);
		GbaSaveRamIsDirty = false;
	}
}

ECL_EXPORT u32 GetSaveRamLength(melonDS::NDS* nds)
{
	return nds->GetNDSSaveLength() + nds->GetGBASaveLength();
}

ECL_EXPORT bool SaveRamIsDirty()
{
	return NdsSaveRamIsDirty || GbaSaveRamIsDirty;
}

ECL_EXPORT void ImportDSiWareSavs(melonDS::DSi* dsi, u32 titleId)
{
	if (auto& nand = dsi->GetNAND())
	{
		if (auto mount = melonDS::DSi_NAND::NANDMount(nand))
		{
			mount.ImportTitleData(DSIWARE_CATEGORY, titleId, melonDS::DSi_NAND::TitleData_PublicSav, "public.sav");
			mount.ImportTitleData(DSIWARE_CATEGORY, titleId, melonDS::DSi_NAND::TitleData_PrivateSav, "private.sav");
			mount.ImportTitleData(DSIWARE_CATEGORY, titleId, melonDS::DSi_NAND::TitleData_BannerSav, "banner.sav");
		}
	}
}

ECL_EXPORT void ExportDSiWareSavs(melonDS::DSi* dsi, u32 titleId)
{
	if (auto& nand = dsi->GetNAND())
	{
		if (auto mount = melonDS::DSi_NAND::NANDMount(nand))
		{
			mount.ExportTitleData(DSIWARE_CATEGORY, titleId, melonDS::DSi_NAND::TitleData_PublicSav, "public.sav");
			mount.ExportTitleData(DSIWARE_CATEGORY, titleId, melonDS::DSi_NAND::TitleData_PrivateSav, "private.sav");
			mount.ExportTitleData(DSIWARE_CATEGORY, titleId, melonDS::DSi_NAND::TitleData_BannerSav, "banner.sav");
		}
	}
}

ECL_EXPORT void DSiWareSavsLength(melonDS::DSi* dsi, u32 titleId, u32* publicSavSize, u32* privateSavSize, u32* bannerSavSize)
{
	*publicSavSize = *privateSavSize = *bannerSavSize = 0;

	if (auto& nand = dsi->GetNAND())
	{
		if (auto mount = melonDS::DSi_NAND::NANDMount(nand))
		{
			u32 version;
			melonDS::NDSHeader header{};

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

ECL_EXPORT u32 GetNANDSize(melonDS::DSi* dsi)
{
	if (auto& nand = dsi->GetNAND())
	{
		return nand.GetLength();
	}

	return 0;
}

ECL_EXPORT void GetNANDData(melonDS::DSi* dsi, u8* buf)
{
	if (auto& nand = dsi->GetNAND())
	{
		auto len = nand.GetLength();
		auto file = nand.GetFile();
		melonDS::Platform::FileRewind(file);
		melonDS::Platform::FileRead(buf, 1, len, file);
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

void WriteFirmware(const melonDS::Firmware& firmware, u32 writeoffset, u32 writelen)
{
}

void WriteDateTime(int year, int month, int day, int hour, int minute, int second)
{
}

}
