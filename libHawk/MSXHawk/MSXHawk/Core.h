#pragma once

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Z80A.h"
#include "PSG.h"
#include "VDP.h"

namespace MSXHawk
{
	class MSXCore
	{
	public:
		MSXCore() 
		{
			cpu.HW_Write = &HardwareWrite;
		};
				
		VDP vdp;
		Z80A cpu;
		SN76489sms psg;

		unsigned char* rom;
		unsigned int rom_size;
		unsigned char ram[0x2000];

		static void HardwareWrite(uint8_t value) 
		{

		}

		// memory map
		unsigned char* Memory_Map[8];

		void Load_ROM(unsigned char* ext_rom, unsigned int ext_rom_size) 
		{
			rom = ext_rom;
			rom_size = ext_rom_size;

			Memory_Map[0] = &rom[0];
		}
	};
}

