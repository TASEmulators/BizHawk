#include <src/types.h>
#include <src/mednafen.h>
#include <src/vb/vb.h>
#include "nyma.h"
#include <emulibc.h>
#include <waterboxcore.h>

using namespace MDFN_IEN_VB;

extern Mednafen::MDFNGI EmulatedVB;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedVB;
}

namespace MDFN_IEN_VB
{
	extern uint8* WRAM;
	extern uint8* GPRAM;
	extern uint32 GPRAM_Mask;
	extern uint8* GPROM;
	extern uint32 GPROM_Mask;
	extern uint8 FB[2][2][0x6000];
	extern uint16 CHR_RAM[0x8000 / sizeof(uint16)];
	extern uint16 DRAM[0x20000 / sizeof(uint16)];
}

// todo
/*static void AccessSystemBus(uint8_t* buffer, int64_t address, int64_t count, bool write)
{
	if (write)
	{
		while (count--)
		{
			uint32_t addr = address++;
			uint8_t* ret = buffer++;
		}
	}
	else
	{
		while (count--)
		{
			uint32_t addr = address++;
			uint8_t* ret = buffer++;
		}
	}
}*/

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
	AddMemoryDomain("WRAM", WRAM, 65536, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_PRIMARY);
	AddMemoryDomain("CARTRAM", GPRAM, GPRAM_Mask + 1, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_SAVERAMMABLE);
	AddMemoryDomain("ROM", GPROM, GPROM_Mask + 1, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("CHR RAM", CHR_RAM, sizeof(CHR_RAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("DRAM", DRAM, sizeof(DRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	AddMemoryDomain("Framebuffer", FB, sizeof(FB), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4);
	//AddMemoryDomain("System Bus", (void*)AccessSystemBus, 1ull << 32, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_FUNCTIONHOOK);
}
