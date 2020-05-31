#include <src/types.h>
#include "nyma.h"
#include <emulibc.h>
#include "mednafen/src/snes_faust/snes.h"
#include <waterboxcore.h>
#include "mednafen/src/snes_faust/ppu.h"
#include "mednafen/src/snes_faust/input.h"
#include "mednafen/src/snes_faust/cart.h"
#include "mednafen/src/snes_faust/cart-private.h"
#include "mednafen/src/snes_faust/apu.h"
#include "mednafen/src/snes_faust/cart/sa1cpu.h"

using namespace MDFN_IEN_SNES_FAUST;

extern Mednafen::MDFNGI EmulatedSNES_Faust;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedSNES_Faust;
}

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
#define MemoryDomainFunctions16(N,R,W)\
static void Access##N(uint8_t* buffer, int64_t address, int64_t count, bool write)\
{\
	auto address16 = address >> 1;\
	if (address & 1 && count)\
	{\
		auto scratch = R(address16);\
		if (write)\
		{\
			scratch = scratch & 0xff | buffer[0] << 8;\
			W(address16, scratch);\
		}\
		else\
		{\
			buffer[0] = scratch >> 8;\
		}\
		buffer++;\
		address16++;\
		count--;\
	}\
	auto buffer16 = (uint16_t*)buffer;\
	if (write)\
	{\
		for (; count > 1; count -= 2)\
			W(address16++, *buffer16++);\
	}\
	else\
	{\
		for (; count > 1; count -= 2)\
			*buffer16++ = R(address16++);\
	}\
	if (count)\
	{\
		buffer = (uint8_t*)buffer16;\
		auto scratch = R(address16);\
		if (write)\
		{\
			scratch = scratch & 0xff00 | buffer[0];\
			W(address16, scratch);\
		}\
		else\
		{\
			buffer[0] = scratch;\
		}\
	}\
}

MemoryDomainFunctions(WRAM, PeekWRAM, PokeWRAM);
MemoryDomainFunctions(SRAM, CART_PeekRAM, CART_PokeRAM);

MemoryDomainFunctions16(VRAM, PPU_ST::PPU_PeekVRAM, PPU_ST::PPU_PokeVRAM);
MemoryDomainFunctions16(CGRAM, PPU_ST::PPU_PeekCGRAM, PPU_ST::PPU_PokeCGRAM);

MemoryDomainFunctions(OAMLO, PPU_ST::PPU_PeekOAM, PPU_ST::PPU_PokeOAM);
MemoryDomainFunctions(OAMHI, PPU_ST::PPU_PeekOAMHI, PPU_ST::PPU_PokeOAMHI);

MemoryDomainFunctions(APU, APU_PeekRAM, APU_PokeRAM);

namespace MDFN_IEN_SNES_FAUST::SA1CPU
{
	extern CPU_Misc CPUM;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	int i = 0;

	// Sufami not supported on this core
	// m[i].Name = "CARTRAM B";
	// m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	// spc7110 not supported on this core
	// m[i].Name = "RTC";
	// m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[i].Data = (void*)(MemoryFunctionHook)AccessWRAM;
	m[i].Name = "WRAM";
	m[i].Size = 128 * 1024;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = Cart.ROM;
	m[i].Name = "CARTROM";
	m[i].Size = Cart.ROM_Size;
	m[i].Flags = MEMORYAREA_FLAGS_WORDSIZE2;
	i++;

	if (CART_GetRAMSize())
	{
		m[i].Data = (void*)(MemoryFunctionHook)AccessSRAM;
		m[i].Name = "CARTRAM";
		m[i].Size = CART_GetRAMSize();
		m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_FUNCTIONHOOK | MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_SAVERAMMABLE;
		i++;
	}

	m[i].Data = (void*)(MemoryFunctionHook)AccessVRAM;
	m[i].Name = "VRAM";
	m[i].Size = 64 * 1024;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = (void*)(MemoryFunctionHook)AccessCGRAM;
	m[i].Name = "CGRAM";
	m[i].Size = 512;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = (void*)(MemoryFunctionHook)AccessOAMLO;
	m[i].Name = "OAMLO";
	m[i].Size = 512;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = (void*)(MemoryFunctionHook)AccessOAMHI;
	m[i].Name = "OAMHI";
	m[i].Size = 32;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = (void*)(MemoryFunctionHook)AccessAPU;
	m[i].Name = "APURAM";
	m[i].Size = 64 * 1024;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	if (SA1CPU::CPUM.ReadFuncs[0])
	{
		m[i].Data = SA1CPU::CPUM.IRAM;
		m[i].Name = "SA1 IRAM";
		m[i].Size = sizeof(SA1CPU::CPUM.IRAM);
		m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2;
		i++;
	}

	// TODO: "System Bus"
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
