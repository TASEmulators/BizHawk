#include <src/types.h>
#include <src/mednafen.h>
#include <src/psx/psx.h>
#include <src/psx/spu.h>
#include "nyma.h"
#include <emulibc.h>
#include <waterboxcore.h>

using namespace MDFN_IEN_PSX;

extern Mednafen::MDFNGI EmulatedPSX;

void SetupMDFNGameInfo()
{
	EmulatedPSX.LayerNames = NULL; // SetLayerEnableMask is null but not this for w/e reason so this is useless
	Mednafen::MDFNGameInfo = &EmulatedPSX;
}

namespace MDFN_IEN_PSX
{
	extern MultiAccessSizeMem<2048 * 1024, false> MainRAM;
	extern PS_GPU GPU;
	extern PS_SPU *SPU;
	extern PS_CPU *CPU;
}

#define MemoryDomainFunctions(N,R,W,O)\
static void Access##N(uint8_t* buffer, int64_t address, int64_t count, bool write)\
{\
	if (write)\
	{\
		while (count--)\
			W(O + address++, *buffer++);\
	}\
	else\
	{\
		while (count--)\
			*buffer++ = R(O + address++);\
	}\
}

MemoryDomainFunctions(BIOSROM, PSX_MemPeek8, PSX_MemPoke8, 0x1FC00000);
MemoryDomainFunctions(PIOMem, PSX_MemPeek8, PSX_MemPoke8, 0x1F000000);
MemoryDomainFunctions(SystemBus, PSX_MemPeek8, PSX_MemPoke8, 0);

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
	AddMemoryDomain("SPURAM", SPU->SPURAM, 512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("BiosROM", (void*)AccessBIOSROM, 512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_FUNCTIONHOOK);
	AddMemoryDomain("PIOMem", (void*)AccessPIOMem, 64*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_FUNCTIONHOOK);
	AddMemoryDomain("DCache", CPU->ScratchRAM.data8, 1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("System Bus", (void*)AccessSystemBus, 1ull << 32, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_FUNCTIONHOOK);
}
