#include <src/types.h>
#include <src/mednafen.h>
#include <src/ss/ss.h>
#include "nyma.h"
#include <emulibc.h>
#include <waterboxcore.h>
#include <src/ss/cart.h>
namespace MDFN_IEN_SS
{
	#include <src/ss/scsp.h>
}

using namespace MDFN_IEN_SS;

extern Mednafen::MDFNGI EmulatedSS;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedSS;
}

namespace MDFN_IEN_SS
{
	extern SS_SCSP SCSP;
	extern uint16 BIOSROM[524288 / sizeof(uint16)];
	extern uint16 WorkRAML[1024 * 1024 / sizeof(uint16)];
	extern uint16 WorkRAMH[1024 * 1024 / sizeof(uint16)];	// Effectively 32-bit in reality, but 16-bit here because of CPU interpreter design(regarding fastmap).
	extern uint8 BackupRAM[32768];
	namespace VDP1
	{
		extern uint16 VRAM[0x40000];
		extern uint16 FB[2][0x20000];
	}
	namespace VDP2
	{
		extern uint16 VRAM[262144];
		extern uint16 CRAM[2048];
	}
	extern uint8 ExtBackupRAM[0x80000];
	extern uint16* CS1RAM;
	extern uint16 ExtRAM[0x200000];
	extern uint16 ROM[0x100000];

	extern int ActiveCartType;
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
	AddMemoryDomain("Sound Ram", SCSP.GetRAMPtr(), 0x100000, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Backup Ram", BackupRAM, sizeof(BackupRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Boot Rom", BIOSROM, 524288, MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Work Ram Low", WorkRAML, sizeof(WorkRAML), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("Work Ram High", WorkRAMH, sizeof(WorkRAMH), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY);
	AddMemoryDomain("VDP1 Ram", VDP1::VRAM, sizeof(VDP1::VRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("VDP1 Framebuffer", VDP1::FB, sizeof(VDP1::FB), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("VDP2 Ram", VDP2::VRAM, sizeof(VDP2::VRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	AddMemoryDomain("VDP2 CRam", VDP2::CRAM, sizeof(VDP2::CRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_BACKUP_MEM)
		AddMemoryDomain("Backup Cart", ExtBackupRAM, sizeof(ExtBackupRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_CS1RAM_16M)
		AddMemoryDomain("CS1 Cart", CS1RAM, 0x1000000, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_EXTRAM_4M)
		AddMemoryDomain("Ram Cart", ExtRAM, sizeof(ExtRAM), MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_EXTRAM_1M)
		AddMemoryDomain("Ram Cart", ExtRAM, sizeof(ExtRAM) / 4, MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
	if (ActiveCartType == CART_KOF95 || ActiveCartType == CART_ULTRAMAN)
	AddMemoryDomain("Rom Cart", ROM, sizeof(ROM), MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_WORDSIZE2);
}
