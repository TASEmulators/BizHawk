#include "mednafen/src/types.h"
#include "nyma.h"
#include <emulibc.h>
#include "mednafen/src/ngp/neopop.h"
#include <src/ngp/flash.h>
#include <waterboxcore.h>

using namespace MDFN_IEN_NGP;

extern Mednafen::MDFNGI EmulatedNGP;

void SetupMDFNGameInfo()
{
	Mednafen::MDFNGameInfo = &EmulatedNGP;
}

ECL_EXPORT bool GetSaveRam()
{
	try
	{
		FLASH_SaveNV();
		return true;
	}
	catch(...)
	{
		return false;
	}
}
ECL_EXPORT bool PutSaveRam()
{
	try
	{
		FLASH_LoadNV();
		return true;
	}
	catch(...)
	{
		return false;
	}
}

namespace MDFN_IEN_NGP
{
	extern uint8 CPUExRAM[16384];
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	m[0].Data = CPUExRAM;
	m[0].Name = "RAM";
	m[0].Size = 16384;
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_WORDSIZE4;

	m[1].Data = ngpc_rom.data;
	m[1].Name = "ROM";
	m[1].Size = ngpc_rom.length;
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE4;

	m[2].Data = ngpc_rom.orig_data;
	m[2].Name = "ORIGINAL ROM";
	m[2].Size = ngpc_rom.length;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE4;
}
