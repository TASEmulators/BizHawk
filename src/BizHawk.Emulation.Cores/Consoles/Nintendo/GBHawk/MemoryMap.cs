using BizHawk.Emulation.Common;

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

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk
	{
		//public byte[] RAM_read = new byte[0x8000];
		//public byte[] ZP_RAM_read = new byte[0x80];

		public byte ReadMemory(ushort addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessRead;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
			
			addr_access = addr;
			
			if (ppu.DMA_bus_control)
			{
				// some of gekkio's tests require these to be accessible during DMA
				if (addr < 0x8000)
				{
					if (ppu.DMA_addr < 0x80)
					{
						return ppu.DMA_byte;
					}

					bus_value = mapper.ReadMemoryLow(addr);
					bus_access_time = cpu.TotalExecutedCycles;
					return bus_value;
				}

				if (addr >= 0xA000 && addr < 0xC000 && is_GBC)
				{
					// on GBC only, cart is accessible during DMA
					bus_value = mapper.ReadMemoryHigh(addr);
					bus_access_time = cpu.TotalExecutedCycles;
					return bus_value;
				}

				if (addr >= 0xE000 && addr < 0xF000)
				{
					return RAM[addr - 0xE000];
				}

				if (addr >= 0xF000 && addr < 0xFE00)
				{
					//if (RAM_read[(RAM_Bank * 0x1000) + (addr - 0xF000)] == 0) { Console.WriteLine("RAM: " + addr + " " + cpu.TotalExecutedCycles); }
					return RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)];
				}

				if (addr >= 0xFE00 && addr < 0xFEA0)
				{
					if (ppu.DMA_OAM_access)
					{
						return OAM[addr - 0xFE00];
					}	
					else 
					{
						return 0xFF;
					}				
				}

				if (addr >= 0xFF00 && addr < 0xFF80) // The game GOAL! Requires Hardware Regs to be accessible
				{
					return Read_Registers(addr);
				}

				if (addr >= 0xFF80)
				{
					if (addr != 0xFFFF)
					{
						//if (ZP_RAM_read[addr - 0xFF80] == 0) { Console.WriteLine("ZP: " + (addr - 0xFF80) + " " + cpu.TotalExecutedCycles); }
						return ZP_RAM[addr - 0xFF80];
					}
					else
					{
						return Read_Registers(addr);
					}
				}

				return ppu.DMA_byte;
			}
			
			if (addr < 0x8000)
			{
				if (addr >= 0x900)
				{
					bus_value = mapper.ReadMemoryLow(addr);
					bus_access_time = cpu.TotalExecutedCycles;
					return bus_value;
				}

				if (addr < 0x100)
				{
					// return Either BIOS ROM or Game ROM
					if ((GB_bios_register & 0x1) == 0)
					{
						return _bios[addr]; // Return BIOS
					}

					bus_value = mapper.ReadMemoryLow(addr);
					bus_access_time = cpu.TotalExecutedCycles;
					return bus_value;
				}

				if (addr >= 0x200)
				{
					// return Either BIOS ROM or Game ROM
					if (((GB_bios_register & 0x1) == 0) && is_GBC)
					{
						return _bios[addr]; // Return BIOS
					}

					bus_value = mapper.ReadMemoryLow(addr);
					bus_access_time = cpu.TotalExecutedCycles;
					return bus_value;
				}

				bus_value = mapper.ReadMemoryLow(addr);
				bus_access_time = cpu.TotalExecutedCycles;
				return bus_value;
			}

			if (addr < 0xA000)
			{
				if (ppu.VRAM_access_read)
				{
					return VRAM[VRAM_Bank * 0x2000 + (addr - 0x8000)];
				}

				if (!HDMA_transfer)
				{
					if (ppu.pixel_counter == 160)
					{
						Console.WriteLine("VRAM Glitch " + cpu.TotalExecutedCycles + " " + ppu.bus_address + " " +
						VRAM[ppu.bus_address] + " " + ppu.read_case_prev + " " + (ppu.internal_cycle & 1) + " " +
						(VRAM_Bank * 0x2000 + (addr - 0x8000)) + " " + VRAM[VRAM_Bank * 0x2000 + (addr - 0x8000)]);

						// TODO: This is a complicated case because the PPU is accessing 2 areas of VRAM at the same time.
						if (is_GBC && ((ppu.read_case_prev == 0) || (ppu.read_case_prev == 4)))
						{
							if ((VRAM_Bank * 0x2000 + (addr - 0x8000)) < 0x3800)
							{
								return VRAM[VRAM_Bank * 0x2000 + (addr - 0x8000)];
							}
							return VRAM[ppu.bus_address];
						}

						// TODO: What is returned when the ppu isn't accessing VRAM?
						//if ((ppu.read_case_prev == 3) || (ppu.read_case_prev == 7))
						//{
						//	return VRAM[VRAM_Bank * 0x2000 + (addr - 0x8000)];
						//}
						return VRAM[ppu.bus_address];
					}
					return 0xFF;
				}
				else
				{
					return 0xFF;
				}				
			}

			if (addr < 0xC000)
			{
				bus_value = mapper.ReadMemoryHigh(addr);
				bus_access_time = cpu.TotalExecutedCycles;
				return bus_value;
			}

			if (addr < 0xFE00)
			{
				addr = (ushort)(RAM_Bank * (addr & 0x1000) + (addr & 0xFFF));
				//if (RAM_read[addr] == 0) { Console.WriteLine("RAM: " + addr + " " + cpu.TotalExecutedCycles); }
				return RAM[addr];
			}

			if (addr < 0xFF00)
			{
				if (addr < 0xFEA0)
				{
					if (ppu.OAM_access_read)
					{
						return OAM[addr - 0xFE00];
					}

					return 0xFF;
				}

				// unmapped memory, return depends on console and rendering
				if (is_GBC)
				{
					if (_syncSettings.GBACGB)
					{
						// in GBA mode, it returns a reflection of the address somehow
						if (ppu.OAM_access_read)
						{
							return (byte)((addr & 0xF0) | ((addr & 0xF0) >> 4));
						}

						return 0xFF;
					}
					else
					{
						// otherwise the return value is revision dependent. Assume CGB-E is same as GBA mode consoles for now
						if (ppu.OAM_access_read)
						{
							return (byte)((addr & 0xF0) | ((addr & 0xF0) >> 4));
						}

						return 0xFF;
					}
				}
				else
				{
					if (ppu.OAM_access_read)
					{
						return 0;
					}

					return 0xFF;
				}
			}

			if (addr < 0xFF80)
			{
				return Read_Registers(addr);
			}

			if (addr < 0xFFFF)
			{
				//if (ZP_RAM_read[addr - 0xFF80] == 0) { Console.WriteLine("ZP: " + (addr - 0xFF80) + " " + cpu.TotalExecutedCycles); }
				return ZP_RAM[addr - 0xFF80];
			}

			return Read_Registers(addr);
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessWrite;
				MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			}
			
			addr_access = addr;

			if (ppu.DMA_bus_control)
			{
				// some of gekkio's tests require this to be accessible during DMA
				if (addr >= 0xA000 && addr < 0xC000 && is_GBC)
				{
					// on GBC only, cart is accessible during DMA
					bus_value = value;
					bus_access_time = cpu.TotalExecutedCycles;
					mapper.WriteMemory(addr, value);
				}

				if (addr >= 0xE000 && addr < 0xF000)
				{
					//RAM_read[addr - 0xE000] = 1;
					RAM[addr - 0xE000] = value;
				}
				else if (addr >= 0xF000 && addr < 0xFE00)
				{
					//RAM_read[RAM_Bank * 0x1000 + (addr - 0xF000)] = 1;
					RAM[RAM_Bank * 0x1000 + (addr - 0xF000)] = value;
				}
				else if (addr >= 0xFE00 && addr < 0xFEA0 && ppu.DMA_OAM_access)
				{
					OAM[addr - 0xFE00] = value; 
				}
				else if (addr >= 0xFF00 && addr < 0xFF80) // The game GOAL! Requires Hardware Regs to be accessible
				{
					Write_Registers(addr, value);
				}
				else if (addr >= 0xFF80)
				{
					if (addr != 0xFFFF)
					{
						//ZP_RAM_read[addr - 0xFF80] = 1;
						ZP_RAM[addr - 0xFF80] = value;
					}
					else
					{
						Write_Registers(addr, value);
					}
				}

				return;
			}
		
			// Writes are more likely from the top down
			if (addr >= 0xFF00)
			{
				if (addr < 0xFF80)
				{
					Write_Registers(addr, value);
				}
				else if (addr < 0xFFFF)
				{
					//ZP_RAM_read[addr - 0xFF80] = 1;
					ZP_RAM[addr - 0xFF80] = value;
				}
				else
				{
					Write_Registers(addr, value);
				}
			}
			else if (addr >= 0xFE00)
			{
				if (addr < 0xFEA0)
				{
					if (ppu.OAM_access_write) { OAM[addr - 0xFE00] = value; }
				}
				// unmapped memory writes depend on console		
				else
				{
					if (is_GBC)
					{
						if (_syncSettings.GBACGB)
						{
							// in GBA mode, writes have no effect as far as tested, might need more thorough tests
						}
						else
						{
							// otherwise it's revision dependent. Assume CGB-E is same as GBA mode consoles for now
						}
					}
					else
					{
						if (ppu.OAM_access_write) { OAM[addr - 0xFEA0 + 0x40] = 0; }
					}
				}
			}
			else if (addr >= 0xC000)
			{
				addr = (ushort)(RAM_Bank * (addr & 0x1000) + (addr & 0xFFF));
				//RAM_read[addr] = 1;
				RAM[addr] = value;
			}
			else if (addr >= 0xA000)
			{
				bus_value = value;
				bus_access_time = cpu.TotalExecutedCycles;
				mapper.WriteMemory(addr, value);
			}
			else if (addr >= 0x8000)
			{
				if (ppu.VRAM_access_write) 
				{ 
					VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)] = value; 
				}
			}
			else
			{
				if (addr >= 0x900)
				{
					bus_value = value;
					bus_access_time = cpu.TotalExecutedCycles;
					mapper.WriteMemory(addr, value);
				}
				else
				{
					if (addr < 0x100)
					{
						if ((GB_bios_register & 0x1) == 0)
						{
							// No Writing to BIOS
						}
						else
						{
							bus_value = value;
							bus_access_time = cpu.TotalExecutedCycles;
							mapper.WriteMemory(addr, value);
						}
					}
					else if (addr >= 0x200)
					{
						if ((GB_bios_register & 0x1) == 0 && is_GBC)
						{
							// No Writing to BIOS
						}
						else
						{
							bus_value = value;
							bus_access_time = cpu.TotalExecutedCycles;
							mapper.WriteMemory(addr, value);
						}
					}
					else
					{
						bus_value = value;
						bus_access_time = cpu.TotalExecutedCycles;
						mapper.WriteMemory(addr, value);
					}
				}
			}
		}

		public byte PeekMemory(ushort addr)
		{
			if (ppu.DMA_bus_control)
			{
				// some of gekkio's tests require these to be accessible during DMA
				if (addr < 0x8000)
				{
					if (ppu.DMA_addr < 0x80)
					{
						return ppu.DMA_byte;
					}

					return mapper.PeekMemoryLow(addr);
				}

				if (addr >= 0xA000 && addr < 0xC000 && is_GBC)
				{
					// on GBC only, cart is accessible during DMA
					return mapper.PeekMemoryHigh(addr);
				}

				if (addr >= 0xE000 && addr < 0xF000)
				{
					return RAM[addr - 0xE000];
				}

				if (addr >= 0xF000 && addr < 0xFE00)
				{
					return RAM[(RAM_Bank * 0x1000) + (addr - 0xF000)];
				}

				if (addr >= 0xFE00 && addr < 0xFEA0)
				{
					if (ppu.DMA_OAM_access)
					{
						return OAM[addr - 0xFE00];
					}
					else
					{
						return 0xFF;
					}
				}

				if (addr >= 0xFF00 && addr < 0xFF80) // The game GOAL! Requires Hardware Regs to be accessible
				{
					return Read_Registers(addr);
				}

				if (addr >= 0xFF80)
				{
					if (addr != 0xFFFF)
					{
						return ZP_RAM[addr - 0xFF80];
					}
					else
					{
						return Read_Registers(addr);
					}
				}

				return ppu.DMA_byte;
			}

			if (addr < 0x8000)
			{
				if (addr >= 0x900)
				{
					return mapper.PeekMemoryLow(addr);
				}

				if (addr < 0x100)
				{
					// return Either BIOS ROM or Game ROM
					if ((GB_bios_register & 0x1) == 0)
					{
						return _bios[addr]; // Return BIOS
					}

					return mapper.PeekMemoryLow(addr);
				}

				if (addr >= 0x200)
				{
					// return Either BIOS ROM or Game ROM
					if (((GB_bios_register & 0x1) == 0) && is_GBC)
					{
						return _bios[addr]; // Return BIOS
					}

					return mapper.PeekMemoryLow(addr);
				}

				return mapper.PeekMemoryLow(addr);
			}

			if (addr < 0xA000)
			{
				if (ppu.VRAM_access_read)
				{
					return VRAM[(VRAM_Bank * 0x2000) + (addr - 0x8000)];
				}

				return 0xFF;
			}

			if (addr < 0xC000)
			{
				return mapper.PeekMemoryHigh(addr);
			}

			if (addr < 0xFE00)
			{
				addr = (ushort)(RAM_Bank * (addr & 0x1000) + (addr & 0xFFF));
				return RAM[addr];
			}

			if (addr < 0xFF00)
			{
				if (addr < 0xFEA0)
				{
					if (ppu.OAM_access_read)
					{
						return OAM[addr - 0xFE00];
					}

					return 0xFF;
				}

				// unmapped memory, return depends on console and rendering
				if (is_GBC)
				{
					if (_syncSettings.GBACGB)
					{
						// in GBA mode, it returns a reflection of the address somehow
						if (ppu.OAM_access_read)
						{
							return (byte)((addr & 0xF0) | ((addr & 0xF0) >> 4));
						}

						return 0xFF;
					}
					else
					{
						// otherwise the return value is revision dependent. Assume CGB-E is same as GBA mode consoles for now
						if (ppu.OAM_access_read)
						{
							return (byte)((addr & 0xF0) | ((addr & 0xF0) >> 4));
						}

						return 0xFF;
					}
				}
				else
				{
					if (ppu.OAM_access_read)
					{
						return 0;
					}

					return 0xFF;
				}
			}

			if (addr < 0xFF80)
			{
				return Read_Registers(addr);
			}

			if (addr < 0xFFFF)
			{
				return ZP_RAM[addr - 0xFF80];
			}

			return Read_Registers(addr);
		}
	}
}
