/******************************************************************************/
/* Mednafen Virtual Boy Emulation Module                                      */
/******************************************************************************/
/* vb.cpp:
**  Copyright (C) 2010-2017 Mednafen Team
**
** This program is free software; you can redistribute it and/or
** modify it under the terms of the GNU General Public License
** as published by the Free Software Foundation; either version 2
** of the License, or (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software Foundation, Inc.,
** 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

#include "vb.h"
#include <emulibc.h>
#define EXPORT extern "C" ECL_EXPORT

namespace MDFN_IEN_VB
{
struct NativeSettings
{
	int InstantReadHack;
	int DisableParallax;
	int ThreeDeeMode;
	int SwapViews;
	int AnaglyphPreset;
	int AnaglyphCustomLeftColor;
	int AnaglyphCustomRightColor;
	int NonAnaglyphColor;
	int LedOnScale;
	int InterlacePrescale;
	int SideBySideSeparation;
};

static void (*input_callback)();
static bool lagged;

enum
{
	ANAGLYPH_PRESET_DISABLED = 0,
	ANAGLYPH_PRESET_RED_BLUE,
	ANAGLYPH_PRESET_RED_CYAN,
	ANAGLYPH_PRESET_RED_ELECTRICCYAN,
	ANAGLYPH_PRESET_RED_GREEN,
	ANAGLYPH_PRESET_GREEN_MAGENTA,
	ANAGLYPH_PRESET_YELLOW_BLUE,
};

static const uint32 AnaglyphPreset_Colors[][2] =
{
	{0, 0},
	{0xFF0000, 0x0000FF},
	{0xFF0000, 0x00B7EB},
	{0xFF0000, 0x00FFFF},
	{0xFF0000, 0x00FF00},
	{0x00FF00, 0xFF00FF},
	{0xFFFF00, 0x0000FF},
};

int32 VB_InDebugPeek;

static uint32 VB3DMode;

static uint8 *WRAM = NULL;

static uint8 *GPRAM = NULL;
static const uint32 GPRAM_Mask = 0xFFFF;

static uint8 *GPROM = NULL;
static uint32 GPROM_Mask;

V810 *VB_V810 = NULL;

VSU *VB_VSU = NULL;
static uint32 VSU_CycleFix;

static uint8 WCR;

static int32 next_vip_ts, next_timer_ts, next_input_ts;

static uint32 IRQ_Asserted;

static INLINE void RecalcIntLevel(void)
{
	int ilevel = -1;

	for (int i = 4; i >= 0; i--)
	{
		if (IRQ_Asserted & (1 << i))
		{
			ilevel = i;
			break;
		}
	}

	VB_V810->SetInt(ilevel);
}

void VBIRQ_Assert(int source, bool assert)
{
	assert(source >= 0 && source <= 4);

	IRQ_Asserted &= ~(1 << source);

	if (assert)
		IRQ_Asserted |= 1 << source;

	RecalcIntLevel();
}

static MDFN_FASTCALL uint8 HWCTRL_Read(v810_timestamp_t &timestamp, uint32 A)
{
	uint8 ret = 0;

	if (A & 0x3)
	{
		//puts("HWCtrl Bogus Read?");
		return (ret);
	}

	switch (A & 0xFF)
	{
	default: //printf("Unknown HWCTRL Read: %08x\n", A);
		break;

	case 0x18:
	case 0x1C:
	case 0x20:
		ret = TIMER_Read(timestamp, A);
		break;

	case 0x24:
		ret = WCR | 0xFC;
		break;

	case 0x10:
	case 0x14:
	case 0x28:
		lagged = false;
		if (input_callback)
			input_callback();
		ret = VBINPUT_Read(timestamp, A);
		break;
	}

	return (ret);
}

static MDFN_FASTCALL void HWCTRL_Write(v810_timestamp_t &timestamp, uint32 A, uint8 V)
{
	if (A & 0x3)
	{
		puts("HWCtrl Bogus Write?");
		return;
	}

	switch (A & 0xFF)
	{
	default: //printf("Unknown HWCTRL Write: %08x %02x\n", A, V);
		break;

	case 0x18:
	case 0x1C:
	case 0x20:
		TIMER_Write(timestamp, A, V);
		break;

	case 0x24:
		WCR = V & 0x3;
		break;

	case 0x10:
	case 0x14:
	case 0x28:
		VBINPUT_Write(timestamp, A, V);
		break;
	}
}

uint8 MDFN_FASTCALL MemRead8(v810_timestamp_t &timestamp, uint32 A)
{
	uint8 ret = 0;
	A &= (1 << 27) - 1;

	//if((A >> 24) <= 2)
	// printf("Read8: %d %08x\n", timestamp, A);

	switch (A >> 24)
	{
	case 0:
		ret = VIP_Read8(timestamp, A);
		break;

	case 1:
		break;

	case 2:
		ret = HWCTRL_Read(timestamp, A);
		break;

	case 3:
		break;
	case 4:
		break;

	case 5:
		ret = WRAM[A & 0xFFFF];
		break;

	case 6:
		if (GPRAM)
			ret = GPRAM[A & GPRAM_Mask];
		break;

	case 7:
		ret = GPROM[A & GPROM_Mask];
		break;
	}
	return (ret);
}

uint16 MDFN_FASTCALL MemRead16(v810_timestamp_t &timestamp, uint32 A)
{
	uint16 ret = 0;

	A &= (1 << 27) - 1;

	//if((A >> 24) <= 2)
	// printf("Read16: %d %08x\n", timestamp, A);

	switch (A >> 24)
	{
	case 0:
		ret = VIP_Read16(timestamp, A);
		break;

	case 1:
		break;

	case 2:
		ret = HWCTRL_Read(timestamp, A);
		break;

	case 3:
		break;

	case 4:
		break;

	case 5:
		ret = MDFN_de16lsb<true>(&WRAM[A & 0xFFFF]);
		break;

	case 6:
		if (GPRAM)
			ret = MDFN_de16lsb<true>(&GPRAM[A & GPRAM_Mask]);
		break;

	case 7:
		ret = MDFN_de16lsb<true>(&GPROM[A & GPROM_Mask]);
		break;
	}
	return ret;
}

void MDFN_FASTCALL MemWrite8(v810_timestamp_t &timestamp, uint32 A, uint8 V)
{
	A &= (1 << 27) - 1;

	//if((A >> 24) <= 2)
	// printf("Write8: %d %08x %02x\n", timestamp, A, V);

	switch (A >> 24)
	{
	case 0:
		VIP_Write8(timestamp, A, V);
		break;

	case 1:
		VB_VSU->Write((timestamp + VSU_CycleFix) >> 2, A, V);
		break;

	case 2:
		HWCTRL_Write(timestamp, A, V);
		break;

	case 3:
		break;

	case 4:
		break;

	case 5:
		WRAM[A & 0xFFFF] = V;
		break;

	case 6:
		if (GPRAM)
			GPRAM[A & GPRAM_Mask] = V;
		break;

	case 7: // ROM, no writing allowed!
		break;
	}
}

void MDFN_FASTCALL MemWrite16(v810_timestamp_t &timestamp, uint32 A, uint16 V)
{
	A &= (1 << 27) - 1;

	//if((A >> 24) <= 2)
	// printf("Write16: %d %08x %04x\n", timestamp, A, V);

	switch (A >> 24)
	{
	case 0:
		VIP_Write16(timestamp, A, V);
		break;

	case 1:
		VB_VSU->Write((timestamp + VSU_CycleFix) >> 2, A, V);
		break;

	case 2:
		HWCTRL_Write(timestamp, A, V);
		break;

	case 3:
		break;

	case 4:
		break;

	case 5:
		MDFN_en16lsb<true>(&WRAM[A & 0xFFFF], V);
		break;

	case 6:
		if (GPRAM)
			MDFN_en16lsb<true>(&GPRAM[A & GPRAM_Mask], V);
		break;

	case 7: // ROM, no writing allowed!
		break;
	}
}

static void FixNonEvents(void)
{
	if (next_vip_ts & 0x40000000)
		next_vip_ts = VB_EVENT_NONONO;

	if (next_timer_ts & 0x40000000)
		next_timer_ts = VB_EVENT_NONONO;

	if (next_input_ts & 0x40000000)
		next_input_ts = VB_EVENT_NONONO;
}

static void EventReset(void)
{
	next_vip_ts = VB_EVENT_NONONO;
	next_timer_ts = VB_EVENT_NONONO;
	next_input_ts = VB_EVENT_NONONO;
}

static INLINE int32 CalcNextTS(void)
{
	int32 next_timestamp = next_vip_ts;

	if (next_timestamp > next_timer_ts)
		next_timestamp = next_timer_ts;

	if (next_timestamp > next_input_ts)
		next_timestamp = next_input_ts;

	return (next_timestamp);
}

static void RebaseTS(const v810_timestamp_t timestamp)
{
	//printf("Rebase: %08x %08x %08x\n", timestamp, next_vip_ts, next_timer_ts);

	assert(next_vip_ts > timestamp);
	assert(next_timer_ts > timestamp);
	assert(next_input_ts > timestamp);

	next_vip_ts -= timestamp;
	next_timer_ts -= timestamp;
	next_input_ts -= timestamp;
}

void VB_SetEvent(const int type, const v810_timestamp_t next_timestamp)
{
	//assert(next_timestamp > VB_V810->v810_timestamp);

	if (type == VB_EVENT_VIP)
		next_vip_ts = next_timestamp;
	else if (type == VB_EVENT_TIMER)
		next_timer_ts = next_timestamp;
	else if (type == VB_EVENT_INPUT)
		next_input_ts = next_timestamp;

	if (next_timestamp < VB_V810->GetEventNT())
		VB_V810->SetEventNT(next_timestamp);
}

static int32 MDFN_FASTCALL EventHandler(const v810_timestamp_t timestamp)
{
	if (timestamp >= next_vip_ts)
		next_vip_ts = VIP_Update(timestamp);

	if (timestamp >= next_timer_ts)
		next_timer_ts = TIMER_Update(timestamp);

	if (timestamp >= next_input_ts)
		next_input_ts = VBINPUT_Update(timestamp);

	return (CalcNextTS());
}

// Called externally from debug.cpp in some cases.
void ForceEventUpdates(const v810_timestamp_t timestamp)
{
	next_vip_ts = VIP_Update(timestamp);
	next_timer_ts = TIMER_Update(timestamp);
	next_input_ts = VBINPUT_Update(timestamp);

	VB_V810->SetEventNT(CalcNextTS());
	//printf("FEU: %d %d %d\n", next_vip_ts, next_timer_ts, next_input_ts);
}

static void VB_Power(void)
{
	memset(WRAM, 0, 65536);

	VIP_Power();
	VB_VSU->Power();
	TIMER_Power();
	VBINPUT_Power();

	EventReset();
	IRQ_Asserted = 0;
	RecalcIntLevel();
	VB_V810->Reset();

	VSU_CycleFix = 0;
	WCR = 0;

	ForceEventUpdates(0); //VB_V810->v810_timestamp);
}

/*struct VB_HeaderInfo
{
	char game_title[256];
	uint32 game_code;
	uint16 manf_code;
	uint8 version;
};*/

/*static void ReadHeader(const uint8 *const rom_data, const uint64 rom_size, VB_HeaderInfo *hi)
{
        iconv_t sjis_ict = iconv_open("UTF-8", "shift_jis");

        if (sjis_ict != (iconv_t)-1)
        {
                char *in_ptr, *out_ptr;
                size_t ibl, obl;

                ibl = 20;
                obl = sizeof(hi->game_title) - 1;

                in_ptr = (char *)rom_data + (0xFFFFFDE0 & (rom_size - 1));
                out_ptr = hi->game_title;

                iconv(sjis_ict, (ICONV_CONST char **)&in_ptr, &ibl, &out_ptr, &obl);
                iconv_close(sjis_ict);

                *out_ptr = 0;

                MDFN_zapctrlchars(hi->game_title);
                MDFN_trim(hi->game_title);
        }
        else
                hi->game_title[0] = 0;

        hi->game_code = MDFN_de32lsb(rom_data + (0xFFFFFDFB & (rom_size - 1)));
        hi->manf_code = MDFN_de16lsb(rom_data + (0xFFFFFDF9 & (rom_size - 1)));
        hi->version = rom_data[0xFFFFFDFF & (rom_size - 1)];
}*/

void VB_ExitLoop(void)
{
	VB_V810->Exit();
}


/*MDFNGI EmulatedVB =
    {

        PortInfo,
        Load,
        TestMagic,
        NULL,
        NULL,
        CloseGame,

        SetLayerEnableMask,
        NULL, // Layer names, null-delimited

        NULL,
        NULL,

        VIP_CPInfo,
        1 << 0,

        CheatInfo_Empty,

        false,
        StateAction,
        Emulate,
        NULL,
        VBINPUT_SetInput,
        NULL,
        DoSimpleCommand,
        NULL,
        VBSettings,
        MDFN_MASTERCLOCK_FIXED(VB_MASTER_CLOCK),
        0,
        false, // Multires possible?

        0,    // lcm_width
        0,    // lcm_height
        NULL, // Dummy

        384, // Nominal width
        224, // Nominal height

        384, // Framebuffer width
        256, // Framebuffer height

        2, // Number of output sound channels
};*/
}

using namespace MDFN_IEN_VB;

EXPORT int Load(const uint8 *rom, int length, const NativeSettings* settings)
{
	const uint64 rom_size = length;
	V810_Emu_Mode cpu_mode = V810_EMU_MODE_ACCURATE;

	VB_InDebugPeek = 0;

	if (rom_size != round_up_pow2(rom_size))
	{
		return 0;
		// throw MDFN_Error(0, _("VB ROM image size is not a power of 2."));
	}

	if (rom_size < 256)
	{
		return 0;
		//throw MDFN_Error(0, _("VB ROM image size is too small."));
	}

	if (rom_size > (1 << 24))
	{
		return 0;
		//throw MDFN_Error(0, _("VB ROM image size is too large."));
	}

	VB_V810 = new V810();
	VB_V810->Init(cpu_mode, true);

	VB_V810->SetMemReadHandlers(MemRead8, MemRead16, NULL);
	VB_V810->SetMemWriteHandlers(MemWrite8, MemWrite16, NULL);

	VB_V810->SetIOReadHandlers(MemRead8, MemRead16, NULL);
	VB_V810->SetIOWriteHandlers(MemWrite8, MemWrite16, NULL);

	for (int i = 0; i < 256; i++)
	{
		VB_V810->SetMemReadBus32(i, false);
		VB_V810->SetMemWriteBus32(i, false);
	}

	std::vector<uint32> Map_Addresses;

	for (uint64 A = 0; A < 1ULL << 32; A += (1 << 27))
	{
		for (uint64 sub_A = 5 << 24; sub_A < (6 << 24); sub_A += 65536)
		{
			Map_Addresses.push_back(A + sub_A);
		}
	}

	WRAM = VB_V810->SetFastMap(alloc_plain, &Map_Addresses[0], 65536, Map_Addresses.size(), "WRAM");
	Map_Addresses.clear();

	// Round up the ROM size to 65536(we mirror it a little later)
	GPROM_Mask = (rom_size < 65536) ? (65536 - 1) : (rom_size - 1);

	for (uint64 A = 0; A < 1ULL << 32; A += (1 << 27))
	{
		for (uint64 sub_A = 7 << 24; sub_A < (8 << 24); sub_A += GPROM_Mask + 1)
		{
			Map_Addresses.push_back(A + sub_A);
			//printf("%08x\n", (uint32)(A + sub_A));
		}
	}

	GPROM = VB_V810->SetFastMap(alloc_sealed, &Map_Addresses[0], GPROM_Mask + 1, Map_Addresses.size(), "Cart ROM");
	Map_Addresses.clear();

	memcpy(GPROM, rom, rom_size);

	// Mirror ROM images < 64KiB to 64KiB
	for (uint64 i = rom_size; i < 65536; i += rom_size)
	{
		memcpy(GPROM + i, GPROM, rom_size);
	}

	/*VB_HeaderInfo hinfo;

	ReadHeader(GPROM, rom_size, &hinfo);

	MDFN_printf(_("Title:     %s\n"), hinfo.game_title);
	MDFN_printf(_("Game ID Code: %u\n"), hinfo.game_code);
	MDFN_printf(_("Manufacturer Code: %d\n"), hinfo.manf_code);
	MDFN_printf(_("Version:   %u\n"), hinfo.version);

	MDFN_printf(_("ROM:       %uKiB\n"), (unsigned)(rom_size / 1024));
	MDFN_printf(_("ROM MD5:   0x%s\n"), md5_context::asciistr(MDFNGameInfo->MD5, 0).c_str());*/

	/*MDFN_printf("\n");

	MDFN_printf(_("V810 Emulation Mode: %s\n"), (cpu_mode == V810_EMU_MODE_ACCURATE) ? _("Accurate") : _("Fast"));*/

	for (uint64 A = 0; A < 1ULL << 32; A += (1 << 27))
	{
		for (uint64 sub_A = 6 << 24; sub_A < (7 << 24); sub_A += GPRAM_Mask + 1)
		{
			//printf("GPRAM: %08x\n", A + sub_A);
			Map_Addresses.push_back(A + sub_A);
		}
	}

	GPRAM = VB_V810->SetFastMap(alloc_plain, &Map_Addresses[0], GPRAM_Mask + 1, Map_Addresses.size(), "Cart RAM");
	Map_Addresses.clear();

	memset(GPRAM, 0, GPRAM_Mask + 1);

	VIP_Init();
	VB_VSU = new VSU();
	VBINPUT_Init();

	VB3DMode = settings->ThreeDeeMode;
	uint32 prescale = settings->InterlacePrescale;
	uint32 sbs_separation = settings->SideBySideSeparation;
	bool reverse = settings->SwapViews;

	VIP_Set3DMode(VB3DMode, reverse, prescale, sbs_separation);

	VIP_SetParallaxDisable(settings->DisableParallax);

	{
		auto presetColor = settings->AnaglyphPreset;

		uint32 lcolor = settings->AnaglyphCustomLeftColor;
		uint32 rcolor = settings->AnaglyphCustomRightColor;

		if (presetColor != ANAGLYPH_PRESET_DISABLED)
		{
			lcolor = AnaglyphPreset_Colors[presetColor][0];
			rcolor = AnaglyphPreset_Colors[presetColor][1];
		}
		VIP_SetAnaglyphColors(lcolor, rcolor);
		VIP_SetDefaultColor(settings->NonAnaglyphColor);
	}

	VBINPUT_SetInstantReadHack(settings->InstantReadHack);

	VIP_SetLEDOnScale(settings->LedOnScale / 1000.0);

	VB_Power();

	/*switch (VB3DMode)
                {
                default:
                        break;

                case VB3DMODE_VLI:
                        MDFNGameInfo->nominal_width = 768 * prescale;
                        MDFNGameInfo->nominal_height = 224;
                        MDFNGameInfo->fb_width = 768 * prescale;
                        MDFNGameInfo->fb_height = 224;
                        break;

                case VB3DMODE_HLI:
                        MDFNGameInfo->nominal_width = 384;
                        MDFNGameInfo->nominal_height = 448 * prescale;
                        MDFNGameInfo->fb_width = 384;
                        MDFNGameInfo->fb_height = 448 * prescale;
                        break;

                case VB3DMODE_CSCOPE:
                        MDFNGameInfo->nominal_width = 512;
                        MDFNGameInfo->nominal_height = 384;
                        MDFNGameInfo->fb_width = 512;
                        MDFNGameInfo->fb_height = 384;
                        break;

                case VB3DMODE_SIDEBYSIDE:
                        MDFNGameInfo->nominal_width = 384 * 2 + sbs_separation;
                        MDFNGameInfo->nominal_height = 224;
                        MDFNGameInfo->fb_width = 384 * 2 + sbs_separation;
                        MDFNGameInfo->fb_height = 224;
                        break;
                }
                MDFNGameInfo->lcm_width = MDFNGameInfo->fb_width;
                MDFNGameInfo->lcm_height = MDFNGameInfo->fb_height;*/

	VB_VSU->SetSoundRate(44100);

	return 1;
}

EXPORT void GetMemoryArea(int which, void **ptr, int *size)
{
	switch (which)
	{
	case 0:
		*ptr = WRAM;
		*size = 65536;
		break;
	case 1:
		*ptr = GPRAM;
		*size = GPRAM_Mask + 1;
		break;
	case 2:
		*ptr = GPROM;
		*size = GPROM_Mask + 1;
		break;
	default:
		*ptr = nullptr;
		*size = 0;
		break;
	}
}

EXPORT void Emulate(EmulateSpecStruct *espec)
{
	v810_timestamp_t v810_timestamp;
	lagged = true;

	VBINPUT_Frame(&espec->Buttons);

	VIP_StartFrame(espec);

	v810_timestamp = VB_V810->Run(EventHandler);

	FixNonEvents();
	ForceEventUpdates(v810_timestamp);

	espec->SoundBufSize = VB_VSU->EndFrame((v810_timestamp + VSU_CycleFix) >> 2, espec->SoundBuf, espec->SoundBufMaxSize);

	VSU_CycleFix = (v810_timestamp + VSU_CycleFix) & 3;

	espec->MasterCycles = v810_timestamp;
	espec->Lagged = lagged;

	TIMER_ResetTS();
	VBINPUT_ResetTS();
	VIP_ResetTS();

	RebaseTS(v810_timestamp);

	VB_V810->ResetTS(0);
}

EXPORT void HardReset()
{
	VB_Power();
}

EXPORT void SetInputCallback(void (*callback)())
{
	input_callback = callback;
}

int main()
{
	return 0;
}
