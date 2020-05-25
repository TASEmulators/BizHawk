#include "mednafen/src/types.h"
#include "nyma.h"
#include <emulibc.h>
#include "mednafen/src/pce/pce.h"
#include <waterboxcore.h>
#include "mednafen/src/pce/pcecd.h"
#include "mednafen/src/pce/huc.h"
#include "mednafen/src/hw_misc/arcade_card/arcade_card.h"

using namespace MDFN_IEN_PCE;

extern Mednafen::MDFNGI EmulatedPCE;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedPCE;
}

static bool IsSgx()
{
	return strcmp(EmulatedPCE.LayerNames, "BG0") == 0;
}

// template<auto ReadFunc, auto WriteFunc>
// static void AccessFunc(uint8_t* buffer, int64_t address, int64_t count, bool write)
// {
// 	if (write)
// 	{
// 		while (count--)
// 			WriteFunc(address++, *buffer++);
// 	}
// 	else
// 	{
// 		while (count--)
// 			*buffer++ = ReadFunc(address++);
// 	}
// }

#define DEFUN(N,R,W)\
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
#define DEFRG(N,R,W)\
static void Access##N(uint8_t* buffer, int64_t address, int64_t count, bool write)\
{\
	if (write)\
	{\
		W(address, count, buffer);\
	}\
	else\
	{\
		R(address, count, buffer);\
	}\
}

namespace MDFN_IEN_PCE
{
	extern ArcadeCard* arcade_card;
}

DEFUN(ShortBus, HuCPU.PeekLogical, HuCPU.PokeLogical);
DEFUN(LongBus, HuCPU.PeekPhysical, HuCPU.PokePhysical);
DEFUN(MainMemory, PCE_PeekMainRAM, PCE_PokeMainRAM);
DEFUN(BRAM, HuC_PeekBRAM, HuC_PokeBRAM);
DEFRG(ADPCM, ADPCM_PeekRAM, ADPCM_PokeRAM);
DEFRG(Arcade, arcade_card->PeekRAM, arcade_card->PokeRAM);

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	int i = 0;

	m[i].Data = (void*)(MemoryFunctionHook)AccessLongBus;
	m[i].Name = "System Bus (21 bit)";
	m[i].Size = 1 << 21;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = (void*)(MemoryFunctionHook)AccessShortBus;
	m[i].Name = "System Bus";
	m[i].Size = 1 << 16;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
	i++;

	m[i].Data = (void*)(MemoryFunctionHook)AccessMainMemory;
	m[i].Name = "Main Memory";
	m[i].Size = IsSgx() ? 32768 : 8192;
	m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK | MEMORYAREA_FLAGS_PRIMARY;
	i++;

	// TODO: "ROM"

	if (HuC_IsBRAMAvailable())
	{
		m[i].Data = (void*)(MemoryFunctionHook)AccessBRAM;
		m[i].Name = "Battery RAM";
		m[i].Size = 2048;
		m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_ONEFILLED | MEMORYAREA_FLAGS_FUNCTIONHOOK | MEMORYAREA_FLAGS_SAVERAMMABLE;
		i++;
	}

	if (PCE_IsCD)
	{
		// TODO: "TurboCD RAM" (var CDRAM)

		m[i].Data = (void*)(MemoryFunctionHook)AccessADPCM;
		m[i].Name = "ADPCM RAM";
		m[i].Size = 1 << 16;
		m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
		i++;

		// TODO: "Super System Card RAM"
		// if (HuCPU.ReadMap[0x68] == SysCardRAMRead)
		// {}

		if (arcade_card)
		{
			m[i].Data = (void*)(MemoryFunctionHook)AccessArcade;
			m[i].Name = "Arcade Card RAM";
			m[i].Size = 1 << 16;
			m[i].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_FUNCTIONHOOK;
			i++;
		}
	}

	// TODO: "Cart Battery RAM" 
	// if (IsPopulous)
	// {}
}
