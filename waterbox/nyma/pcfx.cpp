#include "mednafen/src/types.h"
#include <src/pcfx/pcfx.h>
#include "nyma.h"
#include <emulibc.h>
#include <waterboxcore.h>

using namespace MDFN_IEN_PCFX;

extern Mednafen::MDFNGI EmulatedPCFX;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedPCFX;
}

namespace MDFN_IEN_PCFX
{
	extern uint8 BackupRAM[0x8000];
	extern uint8 ExBackupRAM[0x20000];
	extern uint8 *BIOSROM; 	// 1MB
	extern uint8 *RAM; 	// 2MB
	extern uint8 *FXSCSIROM;	// 512KiB
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = BackupRAM;
	m[0].Name = "Backup RAM";
	m[0].Size = sizeof(BackupRAM);
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[1].Data = ExBackupRAM;
	m[1].Name = "Extra Backup RAM";
	m[1].Size = sizeof(ExBackupRAM);
	m[1].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[2].Data = BIOSROM;
	m[2].Name = "BIOS ROM";
	m[2].Size = 1024 * 1024;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE4;

	m[3].Data = RAM;
	m[3].Name = "Main RAM";
	m[3].Size = 2 * 1024 * 1024;
	m[3].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_PRIMARY;

	m[4].Data = FXSCSIROM;
	m[4].Name = "Scsi Rom";
	m[4].Size = 512 * 1024;
	m[4].Flags = MEMORYAREA_FLAGS_WORDSIZE4;

	for (int i = 0; i < 2; i++)
	{
		m[i + 5].Data = fx_vdc_chips[i]->VRAM;
		m[i + 5].Name = i == 0 ? "VDC A VRAM" : "VDC B VRAM";
		m[i + 5].Size = fx_vdc_chips[i]->VRAM_Size;
		m[i + 5].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2;
	}
}
