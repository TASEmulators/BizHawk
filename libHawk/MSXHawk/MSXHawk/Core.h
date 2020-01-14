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
			cpu.HW_Read = &HardwareRead;
			cpu.HW_Write = &HardwareWrite;
		};
		
		static void Load_ROM(uint8_t* ext_rom, uint32_t ext_rom_size, uint32_t ext_rom_mapper)
		{
			rom = ext_rom;
			rom_size = ext_rom_size / 0x4000;
			rom_mapper = ext_rom_mapper;

			// default memory map setup
			reg_FFFC = 0;
			reg_FFFD = 0;
			reg_FFFE = 0;
			reg_FFFF = 0;
			remap_ROM_0();
			remap_ROM_1();
			remap_ROM_2();
			remap_RAM();
		}

		static VDP vdp;
		static Z80A cpu;
		static SN76489sms psg;

		static uint8_t* rom;
		static uint32_t rom_size;
		static uint32_t rom_mapper;
		static uint8_t ram[0x2000];

		static uint8_t cart_ram[0x8000];

		static uint8_t HardwareRead(uint32_t value)
		{
			return 0;
		}

		static void HardwareWrite(uint32_t addr, uint8_t value) 
		{

		}

		static void MemoryWrite(uint32_t addr, uint8_t value)
		{
			switch (addr) 
			{
			case 0xFFFC:
				reg_FFFC = value;
				remap_ROM_2();
				remap_RAM();
				break;
			case 0xFFFD:
				reg_FFFD = value;
				remap_ROM_0();
				break;
			case 0xFFFE:
				reg_FFFE = value;
				remap_ROM_1();
				break;
			case 0xFFFF:
				reg_FFFF = value;
				remap_ROM_2();
				break;
			}
		}

		static uint8_t reg_FFFC, reg_FFFD, reg_FFFE, reg_FFFF;

		static inline void remap_ROM_0()
		{
			// 0x0000 - 0x03FF always maps to start of ROM
			cpu.MemoryMap[0] = &rom[0];
			cpu.MemoryMapMask[0] = 0;

			for (uint32_t i = 1; i < 16; i++)
			{
				cpu.MemoryMap[i] = &rom[(reg_FFFD % rom_size) * 0x4000 + (0x400 * i)];
				cpu.MemoryMapMask[i] = 0;
			}
		}

		static inline void remap_ROM_1()
		{
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu.MemoryMap[i + 16] = &rom[(reg_FFFE % rom_size) * 0x4000 + (0x400 * i)];
				cpu.MemoryMapMask[i + 16] = 0;
			}
		}

		static inline void remap_ROM_2()
		{
			if ((reg_FFFC & 0x8) > 0) 
			{
				for (uint32_t i = 0; i < 16; i++)
				{
					cpu.MemoryMap[i + 32] = &cart_ram[((reg_FFFC >> 2) & 0x1) * 0x4000 + (0x400 * i)];
					cpu.MemoryMapMask[i + 32] = 0xFF;
				}
			}
			else 
			{
				for (int i = 0; i < 16; i++)
				{
					cpu.MemoryMap[i + 32] = &rom[(reg_FFFF % rom_size) * 0x4000 + (0x400 * i)];
					cpu.MemoryMapMask[i + 32] = 0;
				}
			}
		}

		static inline void remap_RAM()
		{
			if ((reg_FFFC & 0x10) > 0)
			{
				for (uint32_t i = 0; i < 16; i++)
				{
					cpu.MemoryMap[i + 48] = &cart_ram[(0x400 * i)];
					cpu.MemoryMapMask[i + 48] = 0xFF;
				}
			}
			else
			{
				for (uint32_t i = 0; i < 8; i++)
				{
					cpu.MemoryMap[i + 48] = &ram[(0x400 * i)];
					cpu.MemoryMap[i + 48 + 8] = &ram[(0x400 * i)];
					cpu.MemoryMapMask[i + 48] = 0xFF;
					cpu.MemoryMapMask[i + 48 + 8] = 0xFF;
				}
			}
		}
	};
}

