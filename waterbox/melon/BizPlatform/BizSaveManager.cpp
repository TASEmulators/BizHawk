#include "NDS.h"
#include "NDSCart.h"
#include "GBACart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "Platform.h"

#include "BizUserData.h"

namespace melonDS::Platform
{

ECL_EXPORT void PutSaveRam(melonDS::NDS* nds, u8* data, u32 len)
{
	const u32 ndsSaveLen = nds->GetNDSSaveLength();
	const u32 gbaSaveLen = nds->GetGBASaveLength();
	auto* bizUserData = static_cast<BizUserData*>(nds->UserData);

	if (len >= ndsSaveLen)
	{
		nds->SetNDSSave(data, len);
		bizUserData->NdsSaveRamIsDirty = false;

		if (gbaSaveLen && len >= (ndsSaveLen + gbaSaveLen))
		{
			// don't use SetGBASave! it will re-allocate the save buffer (bad!)
			// SetNDSSave is fine (and should be used)
			memcpy(nds->GetGBASave(), data + ndsSaveLen, gbaSaveLen);
			bizUserData->GbaSaveRamIsDirty = false;
		}
	}
}

ECL_EXPORT void GetSaveRam(melonDS::NDS* nds, u8* data, bool clearDirty)
{
	const u32 ndsSaveLen = nds->GetNDSSaveLength();
	const u32 gbaSaveLen = nds->GetGBASaveLength();
	auto* bizUserData = static_cast<BizUserData*>(nds->UserData);

	if (ndsSaveLen)
	{
		memcpy(data, nds->GetNDSSave(), ndsSaveLen);
		if (clearDirty)
		{
			bizUserData->NdsSaveRamIsDirty = false;
		}
	}

	if (gbaSaveLen)
	{
		memcpy(data + ndsSaveLen, nds->GetGBASave(), gbaSaveLen);
		if (clearDirty)
		{
			bizUserData->GbaSaveRamIsDirty = false;
		}
	}
}

ECL_EXPORT u32 GetSaveRamLength(melonDS::NDS* nds)
{
	return nds->GetNDSSaveLength() + nds->GetGBASaveLength();
}

ECL_EXPORT bool SaveRamIsDirty(melonDS::NDS* nds)
{
	auto* bizUserData = static_cast<BizUserData*>(nds->UserData);
	return bizUserData->NdsSaveRamIsDirty || bizUserData->GbaSaveRamIsDirty;
}

ECL_EXPORT void ImportDSiWareSavs(melonDS::DSi* dsi, u64 titleId)
{
	if (auto& nand = dsi->GetNAND())
	{
		if (auto mount = melonDS::DSi_NAND::NANDMount(nand))
		{
			mount.ImportTitleData(titleId >> 32, titleId & 0xFFFFFFFF, melonDS::DSi_NAND::TitleData_PublicSav, "public.sav");
			mount.ImportTitleData(titleId >> 32, titleId & 0xFFFFFFFF, melonDS::DSi_NAND::TitleData_PrivateSav, "private.sav");
			mount.ImportTitleData(titleId >> 32, titleId & 0xFFFFFFFF, melonDS::DSi_NAND::TitleData_BannerSav, "banner.sav");
		}
	}
}

ECL_EXPORT void ExportDSiWareSavs(melonDS::DSi* dsi, u64 titleId)
{
	if (auto& nand = dsi->GetNAND())
	{
		if (auto mount = melonDS::DSi_NAND::NANDMount(nand))
		{
			mount.ExportTitleData(titleId >> 32, titleId & 0xFFFFFFFF, melonDS::DSi_NAND::TitleData_PublicSav, "public.sav");
			mount.ExportTitleData(titleId >> 32, titleId & 0xFFFFFFFF, melonDS::DSi_NAND::TitleData_PrivateSav, "private.sav");
			mount.ExportTitleData(titleId >> 32, titleId & 0xFFFFFFFF, melonDS::DSi_NAND::TitleData_BannerSav, "banner.sav");
		}
	}
}

ECL_EXPORT void DSiWareSavsLength(melonDS::DSi* dsi, u64 titleId, u32* publicSavSize, u32* privateSavSize, u32* bannerSavSize)
{
	*publicSavSize = *privateSavSize = *bannerSavSize = 0;

	if (auto& nand = dsi->GetNAND())
	{
		if (auto mount = melonDS::DSi_NAND::NANDMount(nand))
		{
			u32 version;
			melonDS::NDSHeader header{};

			mount.GetTitleInfo(titleId >> 32, titleId & 0xFFFFFFFF, version, &header, nullptr);
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

void WriteNDSSave(const u8* savedata, u32 savelen, u32 writeoffset, u32 writelen, void* userdata)
{
	auto* bizUserData = static_cast<BizUserData*>(userdata);
	bizUserData->NdsSaveRamIsDirty = true;
}

void WriteGBASave(const u8* savedata, u32 savelen, u32 writeoffset, u32 writelen, void* userdata)
{
	auto* bizUserData = static_cast<BizUserData*>(userdata);
	bizUserData->GbaSaveRamIsDirty = true;
}

void WriteFirmware(const melonDS::Firmware& firmware, u32 writeoffset, u32 writelen, void* userdata)
{
}

void WriteDateTime(int year, int month, int day, int hour, int minute, int second, void* userdata)
{
}

}
