#include <src/types.h>
#include <src/mednafen.h>
#include <src/psx/psx.h>
#include <src/psx/spu.h>
#include <src/psx/frontio.h>
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
	extern FrontIO *FIO;
	extern MultiAccessSizeMem<512 * 1024, false> *BIOSROM;
	extern MultiAccessSizeMem<65536, false> *PIOMem;
}

static void AccessSystemBus(uint8_t* buffer, int64_t address, int64_t count, bool write)
{
	if (write)
	{
		while (count--)
			PSX_MemPoke8(address++, *buffer++);
	}
	else
	{
		while (count--)
			*buffer++ = PSX_MemPeek8(address++);
	}
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
	AddMemoryDomain("SPURAM", SPU->SPURAM, 512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("BiosROM", BIOSROM->data8, 512*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("PIOMem", PIOMem ? PIOMem->data8 : NULL, 64*1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("DCache", CPU->ScratchRAM.data8, 1024, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	for (int j = 0; j < 8; j++) {
		std::string s = "Memcard ";
		s += (j + 1);
		if (FIO->MCDevices[j]->GetNVSize())
		{
			AddMemoryDomain(s.c_str(), (void*)FIO->MCDevices[j]->ReadNV(), FIO->MCDevices[j]->GetNVSize(), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_SAVERAMMABLE);
		}
	}
	AddMemoryDomain("System Bus", (void*)AccessSystemBus, 1ull << 32, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_FUNCTIONHOOK);
}
