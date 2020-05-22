#include "mednafen/src/mednafen.h"
#include "mednafen/src/general.h"
#include "mednafen/src/state.h"
#include "mednafen/src/settings.h"
#include "mednafen/src/mempatcher.h"
#include "mednafen/src/mednafen-driver.h"
#include "mednafen/src/player.h"

namespace Mednafen
{
	MDFNGI *MDFNGameInfo = NULL;
	NativeVFS NVFS;

	std::string MDFN_MakeFName(MakeFName_Type type, int id1, const char *cd1)
	{
		return "";
	}

	// mednafen-driver.h
	void MDFN_indent(int indent)
	{}
	void MDFN_printf(const char *format, ...) noexcept
	{}
	void MDFND_OutputNotice(MDFN_NoticeType t, const char* s) noexcept
	{}
	void MDFND_OutputInfo(const char* s) noexcept
	{}
	void MDFN_Notify(MDFN_NoticeType t, const char* format, ...) noexcept
	{}
	void MDFND_MidSync(EmulateSpecStruct *espec, const unsigned flags)
	{}

	bool MDFNSS_StateAction(StateMem *sm, const unsigned load, const bool data_only, const SFORMAT *sf, const char *sname, const bool optional) noexcept
	{
		abort();
	}

	uint64 MDFN_GetSettingUI(const char *name)
	{
		return 0;
	}
	int64 MDFN_GetSettingI(const char *name)
	{
		return 0;
	}
	double MDFN_GetSettingF(const char *name)
	{
		return 0;
	}
	bool MDFN_GetSettingB(const char *name)
	{
		return false;
	}
	std::string MDFN_GetSettingS(const char *name)
	{
		return "";
	}

	void MDFNMP_Init(uint32 ps, uint32 numpages)
	{}
	void MDFNMP_AddRAM(uint32 size, uint32 address, uint8 *RAM, bool use_in_search) // Deprecated
	{}
	void MDFNMP_RegSearchable(uint32 addr, uint32 size)
	{}
	void MDFNMP_Kill(void)
	{}

	void MDFN_LoadGameCheats(Stream* override)
	{}
	void MDFNMP_InstallReadPatches(void)
	{}
	void MDFNMP_RemoveReadPatches(void)
	{}
	void MDFNMP_ApplyPeriodicCheats(void)
	{}

	// player.h
	void Player_Init(int tsongs, const std::string &album, const std::string &artist, const std::string &copyright, const std::vector<std::string> &snames, bool override_gi)
	{}
	void Player_Draw(MDFN_Surface *surface, MDFN_Rect *dr, int CurrentSong, int16 *samples, int32 sampcount)
	{}
}
