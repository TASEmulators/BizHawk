#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Z80A.h"
#include "PSG.h"
#include "VDP.h"
#include "Memory.h"

namespace MSXHawk
{
	//class Z80A;
	//class VDP;
	//class SN76489sms;
	//class MemoryManager;
	
	class MSXCore
	{
	public:
		MSXCore() 
		{
			MemMap.cpu_pntr = &cpu;
			MemMap.vdp_pntr = &vdp;
			MemMap.psg_pntr = &psg;
			cpu.mem_ctrl = &MemMap;
		};

		Z80A* aaa;
		
		void Load_ROM(uint8_t* ext_rom, uint32_t ext_rom_size, uint32_t ext_rom_mapper)
		{
			MemMap.Load_ROM(ext_rom, ext_rom_size, ext_rom_mapper);
		}

		VDP vdp;
		Z80A cpu;
		SN76489sms psg;
		MemoryManager MemMap;
	};
}

