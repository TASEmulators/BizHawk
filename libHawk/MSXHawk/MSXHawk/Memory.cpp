#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "Z80A.h"

using namespace std;

namespace MSXHawk
{
	void MemoryManager::remap_ROM_0()
	{
		// 0x0000 - 0x03FF always maps to start of ROM
		cpu_pntr->MemoryMap[0] = &rom[0];
		cpu_pntr->MemoryMapMask[0] = 0;

		for (uint32_t i = 1; i < 16; i++)
		{
			cpu_pntr->MemoryMap[i] = &rom[(reg_FFFD % rom_size) * 0x4000 + (0x400 * i)];
			cpu_pntr->MemoryMapMask[i] = 0;
		}
	}

	void MemoryManager::remap_ROM_1()
	{
		for (uint32_t i = 0; i < 16; i++)
		{
			cpu_pntr->MemoryMap[i + 16] = &rom[(reg_FFFE % rom_size) * 0x4000 + (0x400 * i)];
			cpu_pntr->MemoryMapMask[i + 16] = 0;
		}
	}

	void MemoryManager::remap_ROM_2()
	{
		if ((reg_FFFC & 0x8) > 0)
		{
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = &cart_ram[((reg_FFFC >> 2) & 0x1) * 0x4000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 32] = 0xFF;
			}
		}
		else
		{
			for (int i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = &rom[(reg_FFFF % rom_size) * 0x4000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 32] = 0;
			}
		}
	}
	
	void MemoryManager::remap_RAM()
	{
		if ((reg_FFFC & 0x10) > 0)
		{
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = &cart_ram[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 48] = 0xFF;
			}
		}
		else
		{
			for (uint32_t i = 0; i < 8; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = &ram[(0x400 * i)];
				cpu_pntr->MemoryMap[i + 48 + 8] = &ram[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 48] = 0xFF;
				cpu_pntr->MemoryMapMask[i + 48 + 8] = 0xFF;
			}
		}
	}





}