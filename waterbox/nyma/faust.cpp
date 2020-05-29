#include <src/types.h>
#include "nyma.h"
#include <emulibc.h>
#include "mednafen/src/snes_faust/snes.h"
#include <waterboxcore.h>
#include "mednafen/src/snes_faust/ppu.h"
#include "mednafen/src/snes_faust/input.h"
#include "mednafen/src/snes_faust/cart.h"

using namespace MDFN_IEN_SNES_FAUST;

extern Mednafen::MDFNGI EmulatedSNES_Faust;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedSNES_Faust;
}

// ECL_EXPORT bool GetSaveRam()
// {
// 	try
// 	{
// 		FLASH_SaveNV();
// 		return true;
// 	}
// 	catch(...)
// 	{
// 		return false;
// 	}
// }
// ECL_EXPORT bool PutSaveRam()
// {
// 	try
// 	{
// 		FLASH_LoadNV();
// 		return true;
// 	}
// 	catch(...)
// 	{
// 		return false;
// 	}
// }

// namespace MDFN_IEN_NGP
// {
// 	extern uint8 CPUExRAM[16384];
// }

#define MemoryDomainFunctions(N,R,W)\
static void Access##N(uint8_t* buffer, int64_t address, int64_t count, bool write)\
{\
	if (write)\
	{\
		while (count--)\
			W(address++, *buffer++);\
	}\
	else\
	{\
		while (count--)\
			*buffer++ = R(address++);\
	}\
}

MemoryDomainFunctions(WRAM, PeekWRAM, PokeWRAM);

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	int i = 0;
	// m[0].Data = Memory.SRAM; // sram, or sufami A sram
	// m[0].Name = "CARTRAM";
	// m[0].Size = (unsigned)(Memory.SRAMSize ? (1 << (Memory.SRAMSize + 3)) * 128 : 0);
	// if (m[0].Size > 0x20000)
	// 	m[0].Size = 0x20000;
	// m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	// m[1].Data = Multi.sramB; // sufami B sram
	// m[1].Name = "CARTRAM B";
	// m[1].Size = (unsigned)(Multi.cartType && Multi.sramSizeB ? (1 << (Multi.sramSizeB + 3)) * 128 : 0);
	// m[1].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	// m[2].Data = RTCData.reg;
	// m[2].Name = "RTC";
	// m[2].Size = (Settings.SRTC || Settings.SPC7110RTC) ? 20 : 0;
	// m[2].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[i].Data = (void*)(MemoryFunctionHook)AccessWRAM;
	m[i].Name = "WRAM";
	m[i].Size = 128 * 1024;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	// m[4].Data = Memory.VRAM;
	// m[4].Name = "VRAM";
	// m[4].Size = 64 * 1024;
	// m[4].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2;

	// m[5].Data = Memory.ROM;
	// m[5].Name = "CARTROM";
	// m[5].Size = Memory.CalculatedSize;
	// m[5].Flags = MEMORYAREA_FLAGS_WORDSIZE2;
}




// stub ppu_mt since we can't support it
namespace MDFN_IEN_SNES_FAUST
{
static MDFN_COLD uint32 DummyEventHandler(uint32 timestamp)
{
	return SNES_EVENT_MAXTS;
}
namespace PPU_MT
{
void PPU_Init(const bool IsPAL, const bool IsPALPPUBit, const bool WantFrameBeginVBlank, const uint64 affinity){}
void PPU_SetGetVideoParams(MDFNGI* gi, const unsigned caspect, const unsigned hfilter, const unsigned sls, const unsigned sle){}
snes_event_handler PPU_GetEventHandler(void){ return DummyEventHandler; }
snes_event_handler PPU_GetLineIRQEventHandler(void){ return DummyEventHandler; }
void PPU_Kill(void){}
void PPU_StartFrame(EmulateSpecStruct* espec){}
void PPU_SyncMT(void){}
void PPU_Reset(bool powering_up){}
void PPU_ResetTS(void){}
void PPU_StateAction(StateMem* sm, const unsigned load, const bool data_only){}
uint16 PPU_PeekVRAM(uint32 addr){ return 0; }
uint16 PPU_PeekCGRAM(uint32 addr){ return 0; }
uint8 PPU_PeekOAM(uint32 addr){ return 0; }
uint8 PPU_PeekOAMHI(uint32 addr){ return 0; }
uint32 PPU_GetRegister(const unsigned id, char* const special, const uint32 special_len){ return 0; }
}
// and msu1 because it uses MT readers
void MSU1_Init(GameFile* gf, double* IdealSoundRate, uint64 affinity_audio, uint64 affinity_data){}
void MSU1_Kill(void){}
void MSU1_Reset(bool powering_up){}
void MSU1_StateAction(StateMem* sm, const unsigned load, const bool data_only){}
void MSU1_StartFrame(double master_clock, double rate, int32 apu_clock_multiplier, int32 resamp_num, int32 resamp_denom, bool resamp_clear_buf){}
void MSU1_EndFrame(int16* SoundBuf, int32 SoundBufSize){}
void MSU1_AdjustTS(const int32 delta){}
snes_event_handler MSU1_GetEventHandler(void){ return DummyEventHandler; }
}
