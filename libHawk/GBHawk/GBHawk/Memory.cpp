#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "LR35902.h"
#include "PPU_Base.h"
#include "GBAudio.h"

using namespace std;

namespace GBHawk
{
	/*
	$FFFF          Interrupt Enable Flag
	$FF80-$FFFE    Zero Page - 127 bytes
	$FF00-$FF7F    Hardware I/O Registers
	$FEA0-$FEFF    Unusable Memory
	$FE00-$FE9F    OAM - Object Attribute Memory
	$E000-$FDFF    Echo RAM - Reserved, Do Not Use
	$D000-$DFFF    Internal RAM - Bank 1-7 (switchable - CGB only)
	$C000-$CFFF    Internal RAM - Bank 0 (fixed)
	$A000-$BFFF    Cartridge RAM (If Available)
	$9C00-$9FFF    BG Map Data 2
	$9800-$9BFF    BG Map Data 1
	$8000-$97FF    Character RAM
	$4000-$7FFF    Cartridge ROM - Switchable Banks 1-xx
	$0150-$3FFF    Cartridge ROM - Bank 0 (fixed)
	$0100-$014F    Cartridge Header Area
	$0000-$00FF    Restart and Interrupt Vectors
	*/

	/*
	* VRAM is arranged as:
	* 0x1800 Tiles
	* 0x400 BG Map 1
	* 0x400 BG Map 2
	* 0x1800 Tiles
	* 0x400 CA Map 1
	* 0x400 CA Map 2
	* Only the top set is available in GB (i.e. VRAM_Bank = 0)
	*/
	
	uint8_t MemoryManager::ReadMemory(uint32_t addr)
	{
		//uint flags = (uint)(MemoryCallbackFlags.AccessRead);
		//MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		addr_access = addr;

		if (ppu_pntr->DMA_start)
		{
			// some of gekkio's tests require these to be accessible during DMA
			if (addr < 0x8000)
			{
				if (ppu_pntr->DMA_addr < 0x80)
				{
					return 0xFF;
				}
				else
				{
					return mapper.ReadMemory(addr);
				}
			}
			else if ((addr >= 0xE000) && (addr < 0xF000))
			{
				return RAM[addr - 0xE000];
			}
			else if ((addr >= 0xF000) && (addr < 0xFE00))
			{
				return RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)];
			}
			else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu.DMA_OAM_access)
			{
				return OAM[addr - 0xFE00];
			}
			else if ((addr >= 0xFF00) && (addr < 0xFF80)) // The game GOAL! Requires Hardware Regs to be accessible
			{
				return Read_Registers(addr);
			}
			else if ((addr >= 0xFF80))
			{
				return ZP_RAM[addr - 0xFF80];
			}

			return 0xFF;
		}

		if (addr < 0x900)
		{
			if (addr < 0x100)
			{
				// return Either BIOS ROM or Game ROM
				if ((GB_bios_register & 0x1) == 0)
				{
					return bios_rom[addr]; // Return BIOS
				}
				else
				{
					return mapper.ReadMemory(addr);
				}
			}
			else if (addr >= 0x200)
			{
				// return Either BIOS ROM or Game ROM
				if (((GB_bios_register & 0x1) == 0) && is_GBC)
				{
					return bios_rom[addr]; // Return BIOS
				}
				else
				{
					return mapper.ReadMemory(addr);
				}
			}
			else
			{
				return mapper.ReadMemory(addr);
			}
		}
		else if (addr < 0x8000)
		{
			return mapper.ReadMemory(addr);
		}
		else if (addr < 0xA000)
		{
			if (ppu_pntr->VRAM_access_read) { return VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)]; }
			else { return 0xFF; }
		}
		else if (addr < 0xC000)
		{
			return mapper.ReadMemory(addr);
		}
		else if (addr < 0xD000)
		{
			return RAM[addr - 0xC000];
		}
		else if (addr < 0xE000)
		{
			return RAM[(RAM_Bank * 0x1000) + (addr - 0xD000)];
		}
		else if (addr < 0xF000)
		{
			return RAM[addr - 0xE000];
		}
		else if (addr < 0xFE00)
		{
			return RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)];
		}
		else if (addr < 0xFEA0)
		{
			if (ppu_pntr->OAM_access_read) { return OAM[addr - 0xFE00]; }
			else { return 0xFF; }
		}
		else if (addr < 0xFF00)
		{
			// unmapped memory, returns 0xFF
			return 0xFF;
		}
		else if (addr < 0xFF80)
		{
			return Read_Registers(addr);
		}
		else if (addr < 0xFFFF)
		{
			return ZP_RAM[addr - 0xFF80];
		}
		else
		{
			return Read_Registers(addr);
		}

	}
	
	void MemoryManager::WriteMemory(uint32_t addr, uint8_t value)
	{
		//uint flags = (uint)(MemoryCallbackFlags.AccessWrite);
		//MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
		addr_access = addr;

		if (ppu_pntr->DMA_start)
		{
			// some of gekkio's tests require this to be accessible during DMA
			if ((addr >= 0xE000) && (addr < 0xF000))
			{
				RAM[addr - 0xE000] = value;
			}
			else if ((addr >= 0xF000) && (addr < 0xFE00))
			{
				RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)] = value;
			}
			else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu.DMA_OAM_access)
			{
				OAM[addr - 0xFE00] = value;
			}
			else if ((addr >= 0xFF00) && (addr < 0xFF80)) // The game GOAL! Requires Hardware Regs to be accessible
			{
				Write_Registers(addr, value);
			}
			else if ((addr >= 0xFF80))
			{
				ZP_RAM[addr - 0xFF80] = value;
			}
			return;
		}

		if (addr < 0x900)
		{
			if (addr < 0x100)
			{
				if ((GB_bios_register & 0x1) == 0)
				{
					// No Writing to BIOS
				}
				else
				{
					mapper.WriteMemory(addr, value);
				}
			}
			else if (addr >= 0x200)
			{
				if (((GB_bios_register & 0x1) == 0) && is_GBC)
				{
					// No Writing to BIOS
				}
				else
				{
					mapper.WriteMemory(addr, value);
				}
			}
			else
			{
				mapper.WriteMemory(addr, value);
			}
		}
		else if (addr < 0x8000)
		{
			mapper.WriteMemory(addr, value);
		}
		else if (addr < 0xA000)
		{
			if (ppu_pntr->VRAM_access_write) { VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)] = value; }
		}
		else if (addr < 0xC000)
		{
			mapper.WriteMemory(addr, value);
		}
		else if (addr < 0xD000)
		{
			RAM[addr - 0xC000] = value;
		}
		else if (addr < 0xE000)
		{
			RAM[(RAM_Bank * 0x1000) + (addr - 0xD000)] = value;
		}
		else if (addr < 0xF000)
		{
			RAM[addr - 0xE000] = value;
		}
		else if (addr < 0xFE00)
		{
			RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)] = value;
		}
		else if (addr < 0xFEA0)
		{
			if (ppu_pntr->OAM_access_write) { OAM[addr - 0xFE00] = value; }
		}
		else if (addr < 0xFF00)
		{
			// unmapped, writing has no effect
		}
		else if (addr < 0xFF80)
		{
			Write_Registers(addr, value);
		}
		else if (addr < 0xFFFF)
		{
			ZP_RAM[addr - 0xFF80] = value;
		}
		else
		{
			Write_Registers(addr, value);
		}
	}

	uint8_t MemoryManager::PeekMemory(uint32_t addr)
	{
		if (ppu_pntr->DMA_start)
		{
			// some of gekkio's tests require these to be accessible during DMA
			if (addr < 0x8000)
			{
				if (ppu_pntr->DMA_addr < 0x80)
				{
					return 0xFF;
				}
				else
				{
					return mapper.ReadMemory(addr);
				}
			}
			else if ((addr >= 0xE000) && (addr < 0xF000))
			{
				return RAM[addr - 0xE000];
			}
			else if ((addr >= 0xF000) && (addr < 0xFE00))
			{
				return RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)];
			}
			else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu.DMA_OAM_access)
			{
				return OAM[addr - 0xFE00];
			}
			else if ((addr >= 0xFF00) && (addr < 0xFF80)) // The game GOAL! Requires Hardware Regs to be accessible
			{
				return Read_Registers(addr);
			}
			else if ((addr >= 0xFF80))
			{
				return ZP_RAM[addr - 0xFF80];
			}

			return 0xFF;
		}

		if (addr < 0x900)
		{
			if (addr < 0x100)
			{
				// return Either BIOS ROM or Game ROM
				if ((GB_bios_register & 0x1) == 0)
				{
					return bios_rom[addr]; // Return BIOS
				}
				else
				{
					return mapper.ReadMemory(addr);
				}
			}
			else if (addr >= 0x200)
			{
				// return Either BIOS ROM or Game ROM
				if (((GB_bios_register & 0x1) == 0) && is_GBC)
				{
					return bios_rom[addr]; // Return BIOS
				}
				else
				{
					return mapper.ReadMemory(addr);
				}
			}
			else
			{
				return mapper.ReadMemory(addr);
			}
		}
		else if (addr < 0x8000)
		{
			return mapper.PeekMemory(addr);
		}
		else if (addr < 0xA000)
		{
			if (ppu_pntr->VRAM_access_read) { return VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)]; }
			else { return 0xFF; }
		}
		else if (addr < 0xC000)
		{
			return mapper.PeekMemory(addr);
		}
		else if (addr < 0xD000)
		{
			return RAM[addr - 0xC000];
		}
		else if (addr < 0xE000)
		{
			return RAM[(RAM_Bank * 0x1000) + (addr - 0xD000)];
		}
		else if (addr < 0xF000)
		{
			return RAM[addr - 0xE000];
		}
		else if (addr < 0xFE00)
		{
			return RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)];
		}
		else if (addr < 0xFEA0)
		{
			if (ppu_pntr->OAM_access_read) { return OAM[addr - 0xFE00]; }
			else { return 0xFF; }
		}
		else if (addr < 0xFF00)
		{
			// unmapped memory, returns 0xFF
			return 0xFF;
		}
		else if (addr < 0xFF80)
		{
			return Read_Registers(addr);
		}
		else if (addr < 0xFFFF)
		{
			return ZP_RAM[addr - 0xFF80];
		}
		else
		{
			return Read_Registers(addr);
		}
	}
}