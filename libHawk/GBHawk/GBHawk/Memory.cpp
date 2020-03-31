#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "LR35902.h"
#include "PPU.h"
#include "GBAudio.h"
#include "Mappers.h"
#include "SerialPort.h"
#include "Timer.h"

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
					return mapper_pntr->ReadMemory(addr);
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
			else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu_pntr->DMA_OAM_access)
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
					return mapper_pntr->ReadMemory(addr);
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
					return mapper_pntr->ReadMemory(addr);
				}
			}
			else
			{
				return mapper_pntr->ReadMemory(addr);
			}
		}
		else if (addr < 0x8000)
		{
			return mapper_pntr->ReadMemory(addr);
		}
		else if (addr < 0xA000)
		{
			if (ppu_pntr->VRAM_access_read) { return VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)]; }
			else { return 0xFF; }
		}
		else if (addr < 0xC000)
		{
			return mapper_pntr->ReadMemory(addr);
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
			else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu_pntr->DMA_OAM_access)
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
					mapper_pntr->WriteMemory(addr, value);
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
					mapper_pntr->WriteMemory(addr, value);
				}
			}
			else
			{
				mapper_pntr->WriteMemory(addr, value);
			}
		}
		else if (addr < 0x8000)
		{
			mapper_pntr->WriteMemory(addr, value);
		}
		else if (addr < 0xA000)
		{
			if (ppu_pntr->VRAM_access_write) { VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)] = value; }
		}
		else if (addr < 0xC000)
		{
			mapper_pntr->WriteMemory(addr, value);
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
					return mapper_pntr->ReadMemory(addr);
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
			else if ((addr >= 0xFE00) && (addr < 0xFEA0) && ppu_pntr->DMA_OAM_access)
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
					return mapper_pntr->ReadMemory(addr);
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
					return mapper_pntr->ReadMemory(addr);
				}
			}
			else
			{
				return mapper_pntr->ReadMemory(addr);
			}
		}
		else if (addr < 0x8000)
		{
			return mapper_pntr->PeekMemory(addr);
		}
		else if (addr < 0xA000)
		{
			if (ppu_pntr->VRAM_access_read) { return VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)]; }
			else { return 0xFF; }
		}
		else if (addr < 0xC000)
		{
			return mapper_pntr->PeekMemory(addr);
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

	uint8_t MemoryManager::Read_Registers(uint32_t addr)
	{
		uint8_t ret = 0;

		switch (addr)
		{
			// Read Input
		case 0xFF00:
			lagged = false;
			ret = input_register;
			break;

			// Serial data port
		case 0xFF01:
			ret = serialport_pntr->ReadReg(addr);
			break;

			// Serial port control
		case 0xFF02:
			ret = serialport_pntr->ReadReg(addr);
			break;

			// Timer Registers
		case 0xFF04:
		case 0xFF05:
		case 0xFF06:
		case 0xFF07:
			ret = timer_pntr->ReadReg(addr);
			break;

			// Interrupt flags
		case 0xFF0F:
			ret = REG_FF0F_OLD;
			break;

			// audio regs
		case 0xFF10:
		case 0xFF11:
		case 0xFF12:
		case 0xFF13:
		case 0xFF14:
		case 0xFF16:
		case 0xFF17:
		case 0xFF18:
		case 0xFF19:
		case 0xFF1A:
		case 0xFF1B:
		case 0xFF1C:
		case 0xFF1D:
		case 0xFF1E:
		case 0xFF20:
		case 0xFF21:
		case 0xFF22:
		case 0xFF23:
		case 0xFF24:
		case 0xFF25:
		case 0xFF26:
		case 0xFF30:
		case 0xFF31:
		case 0xFF32:
		case 0xFF33:
		case 0xFF34:
		case 0xFF35:
		case 0xFF36:
		case 0xFF37:
		case 0xFF38:
		case 0xFF39:
		case 0xFF3A:
		case 0xFF3B:
		case 0xFF3C:
		case 0xFF3D:
		case 0xFF3E:
		case 0xFF3F:
			ret = psg_pntr->ReadReg(addr);
			break;

			// PPU Regs
		case 0xFF40:
		case 0xFF41:
		case 0xFF42:
		case 0xFF43:
		case 0xFF44:
		case 0xFF45:
		case 0xFF46:
		case 0xFF47:
		case 0xFF48:
		case 0xFF49:
		case 0xFF4A:
		case 0xFF4B:
			ret = ppu_pntr->ReadReg(addr);
			break;

			// Speed Control for GBC
		case 0xFF4D:
			if (GBC_compat)
			{
				ret = (uint8_t)(((double_speed ? 1 : 0) << 7) + ((speed_switch ? 1 : 0)));
			}
			else
			{
				ret = 0xFF;
			}
			break;

		case 0xFF4F: // VBK
			if (GBC_compat)
			{
				ret = (uint8_t)(0xFE | VRAM_Bank);
			}
			else
			{
				ret = 0xFF;
			}
			break;

			// Bios control register. Not sure if it is readable
		case 0xFF50:
			ret = 0xFF;
			break;

			// PPU Regs for GBC
		case 0xFF51:
		case 0xFF52:
		case 0xFF53:
		case 0xFF54:
		case 0xFF55:
			if (GBC_compat)
			{
				ret = ppu_pntr->ReadReg(addr);
			}
			else
			{
				ret = 0xFF;
			}
			break;

		case 0xFF56:
			if (GBC_compat)
			{
				// can receive data
				if ((IR_reg & 0xC0) == 0xC0)
				{
					ret = IR_reg;
				}
				else
				{
					ret = (uint8_t)(IR_reg | 2);
				}
			}
			else
			{
				ret = 0xFF;
			}
			break;

		case 0xFF68:
		case 0xFF69:
		case 0xFF6A:
		case 0xFF6B:
			if (GBC_compat)
			{
				ret = ppu_pntr->ReadReg(addr);
			}
			else
			{
				ret = 0xFF;
			}
			break;

			// Speed Control for GBC
		case 0xFF70:
			if (GBC_compat)
			{
				ret = (uint8_t)RAM_Bank;
			}
			else
			{
				ret = 0xFF;
			}
			break;

		case 0xFF6C:
			if (GBC_compat) { ret = undoc_6C; }
			else { ret = 0xFF; }
			break;

		case 0xFF72:
			if (is_GBC) { ret = undoc_72; }
			else { ret = 0xFF; }
			break;

		case 0xFF73:
			if (is_GBC) { ret = undoc_73; }
			else { ret = 0xFF; }
			break;

		case 0xFF74:
			if (GBC_compat) { ret = undoc_74; }
			else { ret = 0xFF; }
			break;

		case 0xFF75:
			if (is_GBC) { ret = undoc_75; }
			else { ret = 0xFF; }
			break;

		case 0xFF76:
			if (is_GBC) { ret = undoc_76; }
			else { ret = 0xFF; }
			break;

		case 0xFF77:
			if (is_GBC) { ret = undoc_77; }
			else { ret = 0xFF; }
			break;

			// interrupt control register
		case 0xFFFF:
			ret = REG_FFFF;
			break;

		default:
			ret = 0xFF;
			break;

		}
		return ret;
	}

	void MemoryManager::Write_Registers(uint32_t addr, uint8_t value)
	{
		// check for high to low transitions that trigger IRQs
		uint8_t contr_prev = input_register;
		
		switch (addr)
		{
			// select input
		case 0xFF00:
			input_register &= 0xCF;
			input_register |= (uint8_t)(value & 0x30); // top 2 bits always 1

			input_register &= 0xF0;
			if ((input_register & 0x30) == 0x20)
			{
				input_register |= (uint8_t)(controller_state & 0xF);
			}
			else if ((input_register & 0x30) == 0x10)
			{
				input_register |= (uint8_t)((controller_state & 0xF0) >> 4);
			}
			else if ((input_register & 0x30) == 0x00)
			{
				// if both polls are set, then a bit is zero if either or both pins are zero
				uint8_t temp = (uint8_t)((controller_state & 0xF) & ((controller_state & 0xF0) >> 4));
				input_register |= temp;
			}
			else
			{
				input_register |= 0xF;
			}

			// check for interrupts
			if (((contr_prev & 8) > 0) && ((input_register & 8) == 0) ||
				((contr_prev & 4) > 0) && ((input_register & 4) == 0) ||
				((contr_prev & 2) > 0) && ((input_register & 2) == 0) ||
				((contr_prev & 1) > 0) && ((input_register & 1) == 0))
			{
				if (((REG_FFFF & 0x10) > 0)) { cpu_pntr->FlagI = true; }
				REG_FF0F |= 0x10;
			}

			break;

			// Serial data port
		case 0xFF01:
			serialport_pntr->WriteReg(addr, value);
			break;

			// Serial port control
		case 0xFF02:
			serialport_pntr->WriteReg(addr, value);
			break;

			// Timer Registers
		case 0xFF04:
		case 0xFF05:
		case 0xFF06:
		case 0xFF07:
			timer_pntr->WriteReg(addr, value);
			break;

			// Interrupt flags
		case 0xFF0F:
			REG_FF0F = (uint8_t)(0xE0 | value);

			// check if enabling any of the bits triggered an IRQ
			for (int i = 0; i < 5; i++)
			{
				if (((REG_FFFF & (1 << i)) > 0) && ((REG_FF0F & (1 << i)) > 0))
				{
					cpu_pntr->FlagI = true;
				}
			}

			// if no bits are in common between flags and enables, de-assert the IRQ
			if (((REG_FF0F & 0x1F) & REG_FFFF) == 0) { cpu_pntr->FlagI = false; }
			break;

			// audio regs
		case 0xFF10:
		case 0xFF11:
		case 0xFF12:
		case 0xFF13:
		case 0xFF14:
		case 0xFF16:
		case 0xFF17:
		case 0xFF18:
		case 0xFF19:
		case 0xFF1A:
		case 0xFF1B:
		case 0xFF1C:
		case 0xFF1D:
		case 0xFF1E:
		case 0xFF20:
		case 0xFF21:
		case 0xFF22:
		case 0xFF23:
		case 0xFF24:
		case 0xFF25:
		case 0xFF26:
		case 0xFF30:
		case 0xFF31:
		case 0xFF32:
		case 0xFF33:
		case 0xFF34:
		case 0xFF35:
		case 0xFF36:
		case 0xFF37:
		case 0xFF38:
		case 0xFF39:
		case 0xFF3A:
		case 0xFF3B:
		case 0xFF3C:
		case 0xFF3D:
		case 0xFF3E:
		case 0xFF3F:
			psg_pntr->WriteReg(addr, value);
			break;

			// PPU Regs
		case 0xFF40:
		case 0xFF41:
		case 0xFF42:
		case 0xFF43:
		case 0xFF44:
		case 0xFF45:
		case 0xFF46:
			ppu_pntr->WriteReg(addr, value);
			break;
		case 0xFF47:
		case 0xFF48:
		case 0xFF49:
			ppu_pntr->WriteReg(addr, value);
			compute_palettes();
			break;
		case 0xFF4A:
		case 0xFF4B:
			ppu_pntr->WriteReg(addr, value);
			break;

			// GBC compatibility register (I think)
		case 0xFF4C:
			if ((value != 0xC0) && (value != 0x80))// && (value != 0xFF) && (value != 0x04))
			{
				GBC_compat = false;

				// cpu operation is a function of hardware only
				//cpu.is_GBC = GBC_compat;
			}
			break;

			// Speed Control for GBC
		case 0xFF4D:
			if (GBC_compat)
			{
				speed_switch = (value & 1) > 0;
			}
			break;

			// VBK
		case 0xFF4F:
			if (GBC_compat && !ppu_pntr->HDMA_active)
			{
				VRAM_Bank = (uint8_t)(value & 1);
			}
			break;

			// Bios control register. Writing 1 permanently disables BIOS until a power cycle occurs
		case 0xFF50:
			// Console.WriteLine(value);
			if (GB_bios_register == 0)
			{
				GB_bios_register = value;
			}
			break;

			// PPU Regs for GBC
		case 0xFF51:
		case 0xFF52:
		case 0xFF53:
		case 0xFF54:
		case 0xFF55:
			if (GBC_compat)
			{
				ppu_pntr->WriteReg(addr, value);
			}
			break;

		case 0xFF56:
			if (is_GBC)
			{
				IR_reg = (uint8_t)((value & 0xC1) | (IR_reg & 0x3E));

				// send IR signal out
				if ((IR_reg & 0x1) == 0x1) { IR_signal = (uint8_t)(0 | IR_mask); }
				else { IR_signal = 2; }

				// receive own signal if IR on and receive on
				if ((IR_reg & 0xC1) == 0xC1) { IR_self = (uint8_t)(0 | IR_mask); }
				else { IR_self = 2; }

				IR_write = 8;
			}
			break;

		case 0xFF68:
		case 0xFF69:
		case 0xFF6A:
		case 0xFF6B:
			//if (GBC_compat)
			//{
			ppu_pntr->WriteReg(addr, value);
			//}
			break;

			// RAM Bank in GBC mode
		case 0xFF70:
			//Console.WriteLine(value);
			if (GBC_compat)
			{
				RAM_Bank = value & 7;
				if (RAM_Bank == 0) { RAM_Bank = 1; }
			}
			break;

		case 0xFF6C:
			if (GBC_compat) { undoc_6C |= (uint8_t)(value & 1); }
			break;

		case 0xFF72:
			if (is_GBC) { undoc_72 = value; }
			break;

		case 0xFF73:
			if (is_GBC) { undoc_73 = value; }
			break;

		case 0xFF74:
			if (GBC_compat) { undoc_74 = value; }
			break;

		case 0xFF75:
			if (is_GBC) { undoc_75 |= (uint8_t)(value & 0x70); }
			break;

		case 0xFF76:
			// read only
			break;

		case 0xFF77:
			// read only
			break;

			// interrupt control register
		case 0xFFFF:
			REG_FFFF = value;

			// check if enabling any of the bits triggered an IRQ
			for (int i = 0; i < 5; i++)
			{
				if (((REG_FFFF & (1 << i)) > 0) && ((REG_FF0F & (1 << i)) > 0))
				{
					cpu_pntr->FlagI = true;
				}
			}

			// if no bits are in common between flags and enables, de-assert the IRQ
			if (((REG_FF0F & 0x1F) & REG_FFFF) == 0) { cpu_pntr->FlagI = false; }
			break;

		default:
			//Console.Write(addr);
			//Console.Write(" ");
			//Console.WriteLine(value);
			break;
		}
	}

	void MemoryManager::compute_palettes()
	{
		for (int i = 0; i < 4; i++)
		{
			color_palette_OBJ[i] = color_palette[(ppu_pntr->obj_pal_0 >> (i * 2)) & 3];
			color_palette_OBJ[i + 4] = color_palette[(ppu_pntr->obj_pal_1 >> (i * 2)) & 3];
			color_palette_BG[i] = color_palette[(ppu_pntr->BGP >> (i * 2)) & 3];
		}
	}
}