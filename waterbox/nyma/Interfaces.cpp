#include "mednafen/src/mednafen.h"
#include "mednafen/src/general.h"
#include "mednafen/src/state.h"
#include "mednafen/src/settings.h"
#include "mednafen/src/mempatcher.h"
#include "mednafen/src/mednafen-driver.h"
#include "mednafen/src/player.h"

#include <stdio.h>
#include <stdarg.h>
#include <emulibc.h>

enum { SETTING_VALUE_MAX_LENGTH = 256 };

static void (*FrontendSettingQuery)(const char* setting, char* dest);
ECL_EXPORT void SetFrontendSettingQuery(void (*q)(const char* setting, char* dest))
{
	FrontendSettingQuery = q;
}

namespace Mednafen
{
	MDFNGI *MDFNGameInfo = NULL;
	NativeVFS NVFS;

	std::string MDFN_MakeFName(MakeFName_Type type, int id1, const char *cd1)
	{
		return "";
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

	bool MDFNSS_StateAction(StateMem *sm, const unsigned load, const bool data_only, const SFORMAT *sf, const char *sname, const bool optional) noexcept
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
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		return strtol(tmp, nullptr, 10);
	}
	double MDFN_GetSettingF(const char *name)
	{
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		return strtod(tmp, nullptr);
	}
	bool MDFN_GetSettingB(const char *name)
	{
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		return strtol(tmp, nullptr, 10) != 0;
	}
	std::string MDFN_GetSettingS(const char *name)
	{
		char tmp[SETTING_VALUE_MAX_LENGTH];
		FrontendSettingQuery(name, tmp);
		return std::string(tmp);
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
	std::vector<SUBCHEAT> SubCheats[8];
	bool SubCheatsOn;

	// player.h
	void Player_Init(int tsongs, const std::string &album, const std::string &artist, const std::string &copyright, const std::vector<std::string> &snames, bool override_gi)
	{}
	void Player_Draw(MDFN_Surface *surface, MDFN_Rect *dr, int CurrentSong, int16 *samples, int32 sampcount)
	{}
}
