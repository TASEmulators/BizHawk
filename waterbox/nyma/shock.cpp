#include <src/types.h>
#include <src/mednafen.h>
#include <src/psx/psx.h>
#include "nyma.h"
#include <emulibc.h>
#include <waterboxcore.h>

using namespace MDFN_IEN_PSX;

extern Mednafen::MDFNGI EmulatedPSX;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedPSX;
}

namespace MDFN_IEN_PSX
{
	extern MultiAccessSizeMem<2048 * 1024, false> MainRAM;
	//extern MultiAccessSizeMem<512 * 1024, false> *BIOSROM;
	//extern MultiAccessSizeMem<65536, false> *PIOMem;
	extern PS_GPU GPU;
	//extern PS_SPU *SPU;
	extern PS_CPU *CPU;
}

static void SysBusAccess(uint8* buffer, int64 address, int64 count, bool write)
{
	if (write)
		while (count--) PSX_MemPoke8(address++, *buffer++);
	else
		while (count--) *buffer++ = PSX_MemPeek8(address++);
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	int i = 0;
	#define AddMemoryDomain(name,data,size,flags) do\
	{\
		m[i].Data = data;\
		m[i].Name = name;\
		m[i].Size = size;\
		m[i].Flags = flags;\
		i++;\
	}\
	while (0)
	AddMemoryDomain("MainRAM", MainRAM.data8, 2048*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_PRIMARY);
	AddMemoryDomain("GPURAM", GPU.GPURAM, 2*512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	//AddMemoryDomain("SPURAM", SPU->SPURAM, 512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	//AddMemoryDomain("BiosROM", BIOSROM->data8, 512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	//AddMemoryDomain("PIOMem", PIOMem->data8, 64*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("DCache", CPU->ScratchRAM.data8, 1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("System Bus", (void*)SysBusAccess, 1ull << 32, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_FUNCTIONHOOK);
}
