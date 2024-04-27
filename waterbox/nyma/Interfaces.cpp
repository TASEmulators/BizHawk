#include "mednafen/src/mednafen.h"
#include "mednafen/src/general.h"
#include "mednafen/src/state.h"
#include "mednafen/src/settings.h"
#include "mednafen/src/mempatcher.h"
#include "mednafen/src/mednafen-driver.h"
#include "mednafen/src/player.h"
#include "mednafen/src/Time.h"

#include <stdio.h>
#include <stdarg.h>
#include <emulibc.h>
#include "nyma.h"
#include <unordered_map>

enum { SETTING_VALUE_MAX_LENGTH = 256 };

static void (*FrontendSettingQuery)(const char* setting, char* dest);
ECL_EXPORT void SetFrontendSettingQuery(void (*q)(const char* setting, char* dest))
{
	FrontendSettingQuery = q;
}

static std::unordered_map<uint32_t, CheatArea> CheatAreas;

CheatArea* FindCheatArea(uint32_t address)
{
	auto kvp = CheatAreas.find(address);
	if (kvp != CheatAreas.end())
	{
		return &kvp->second;
	}
	else
	{
		return nullptr;
	}
}

static void (*FrontendFirmwareNotify)(const char* name);
ECL_EXPORT void SetFrontendFirmwareNotify(void (*cb)(const char* name))
{
	FrontendFirmwareNotify = cb;
}

namespace Mednafen
{
	MDFNGI *MDFNGameInfo = NULL;
	NativeVFS NVFS;

	std::string MDFN_MakeFName(MakeFName_Type type, int id1, const char *cd1)
	{
		std::string ret;
		switch (type)
		{
			case MDFNMKF_STATE: ret += "STATE:"; break;
			case MDFNMKF_SNAP: ret += "SNAP:"; break;
			case MDFNMKF_SAV: ret += "SAV:"; break;
			case MDFNMKF_SAVBACK: ret += "SAVBACK:"; break;
			case MDFNMKF_CHEAT: ret += "CHEAT:"; break;
			case MDFNMKF_PALETTE: ret += "PALETTE:"; break;
			case MDFNMKF_PATCH: ret += "PATCH:"; break;
			case MDFNMKF_MOVIE: ret += "MOVIE:"; break;
			case MDFNMKF_SNAP_DAT: ret += "SNAP_DAT:"; break;
			case MDFNMKF_CHEAT_TMP: ret += "CHEAT_TMP:"; break;
			case MDFNMKF_FIRMWARE: ret += "FIRMWARE:"; break;
			case MDFNMKF_PGCONFIG: ret += "PGCONFIG:"; break;
			default: ret += "UNKNOWN:"; break;
		}
		ret += cd1;
		if (type == MDFNMKF_FIRMWARE)
			FrontendFirmwareNotify(ret.c_str());
		return ret;
	}

	// mednafen-driver.h
	static int curindent = 0;
	void MDFN_indent(int indent)
	{
		curindent += indent;
		if(curindent < 0)
		{
			fprintf(stderr, "MDFN_indent negative!\n");
			curindent = 0;
		}
	}
	void MDFN_printf(const char *format, ...) noexcept
	{
		for (int i = 0; i < curindent; i++)
			putchar('\t');
		va_list argp;
		va_start(argp, format);
		vprintf(format, argp);
   		va_end(argp);		
	}
	void MDFND_OutputNotice(MDFN_NoticeType t, const char* s) noexcept
	{
		fputs(s, t == MDFN_NOTICE_ERROR ? stderr : stdout);
		fputc('\n', t == MDFN_NOTICE_ERROR ? stderr : stdout);
	}
	void MDFND_OutputInfo(const char* s) noexcept
	{
		puts(s);
	}
	void MDFN_Notify(MDFN_NoticeType t, const char* format, ...) noexcept
	{
		va_list argp;
		va_start(argp, format);
		vfprintf(t == MDFN_NOTICE_ERROR ? stderr : stdout, format, argp);
	}

	void MDFN_MidSync(EmulateSpecStruct *espec, const unsigned flags)
	{}
	void MDFN_MidLineUpdate(EmulateSpecStruct *espec, int y)
	{}

	bool MDFNSS_StateAction(StateMem *sm, const unsigned load, const bool data_only, const SFORMAT *sf, const char *sname, const bool optional) noexcept
	{
		abort();
	}
	void MDFNSS_SaveSM(Stream *st, bool data_only, const MDFN_Surface *surface, const MDFN_Rect *DisplayRect, const int32 *LineWidths)
	{
		abort();
	}
	void MDFNSS_LoadSM(Stream *st, bool data_only, const bool fuzz)
	{
		abort();
	}


	static const MDFNSetting* GetSetting(const char* name)
	{
		const MDFNSetting* s;
		for (int i = 0; s = &MDFNGameInfo->Settings[i], s->name; i++)
		{
			if (strcmp(s->name, name) == 0)
				return s;
		}
		return nullptr;
	}

	uint64 MDFN_GetSettingUI(const char *name)
	{
		auto s = GetSetting(name);
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		if (s && s->type == MDFNST_ENUM)
		{
			for (int i = 0; s->enum_list[i].string; i++)
			{
				if (strcmp(s->enum_list[i].string, tmp) == 0)
					return s->enum_list[i].number;
			}
			for (int i = 0; s->enum_list[i].string; i++)
			{
				if (strcmp(s->enum_list[i].string, s->default_value) == 0)
					return s->enum_list[i].number;
			}
			return 0;
		}
		else
		{
			return strtoul(tmp, nullptr, 10);
		}
	}
	int64 MDFN_GetSettingI(const char *name)
	{
		auto s = GetSetting(name);
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		if (s && s->type == MDFNST_ENUM)
		{
			for (int i = 0; s->enum_list[i].string; i++)
			{
				if (strcmp(s->enum_list[i].string, tmp) == 0)
					return s->enum_list[i].number;
			}
			for (int i = 0; s->enum_list[i].string; i++)
			{
				if (strcmp(s->enum_list[i].string, s->default_value) == 0)
					return s->enum_list[i].number;
			}
			return 0;
		}
		else
		{
			return strtol(tmp, nullptr, 10);
		}
	}
	double MDFN_GetSettingF(const char *name)
	{
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		return strtod(tmp, nullptr);
	}
	bool MDFN_GetSettingB(const char *name)
	{
		return (bool)MDFN_GetSettingUI(name);
	}
	std::string MDFN_GetSettingS(const char *name)
	{
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		return std::string(tmp);
	}
	std::vector<uint64> MDFN_GetSettingMultiUI(const char *name)
	{
		// only used in some demo code it seems?
		return std::vector<uint64>();
	}
	std::vector<int64> MDFN_GetSettingMultiI(const char *name)
	{
		// not used for anything it seems?
		return std::vector<int64>();
	}
	uint64 MDFN_GetSettingMultiM(const char *name)
	{
		return MDFN_GetSettingUI(name);
	}
	void MDFNMP_Init(uint32 ps, uint32 numpages)
	{}
	void MDFNMP_AddRAM(uint32 size, uint32 address, uint8 *RAM, bool use_in_search) // Deprecated
	{
		CheatAreas.insert(std::pair<uint32_t, CheatArea>({ address, CheatArea({ (void*)RAM, size }) }));
	}
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
	std::vector<SUBCHEAT> SubCheats[8];
	bool SubCheatsOn;

	// player.h
	void Player_Init(int tsongs, const std::string &album, const std::string &artist, const std::string &copyright, const std::vector<std::string> &snames, bool override_gi)
	{}
	void Player_Draw(MDFN_Surface *surface, MDFN_Rect *dr, int CurrentSong, int16 *samples, int32 sampcount)
	{}

	namespace Time
	{
		void Time_Init()
		{}
		int64 EpochTime()
		{
			return FrontendTime;
		}
		int64 MonoUS()
		{
			return FrontendTime;
		}
		struct tm LocalTime(const int64 ept)
		{
			// musl's localtime_r gets into a lot of unfun syscalls, and we wouldn't allow changable timezone anyway
			return UTCTime(ept);
		}
		struct tm UTCTime(const int64 ept)
		{
			struct tm tout;
			time_t tt = (time_t)ept;
			if(!gmtime_r(&tt, &tout))
			{
				ErrnoHolder ene(errno);
				throw MDFN_Error(ene.Errno(), _("%s failed: %s"), "gmtime_r()", ene.StrError());
			}
			return tout;
		}
	}
}
