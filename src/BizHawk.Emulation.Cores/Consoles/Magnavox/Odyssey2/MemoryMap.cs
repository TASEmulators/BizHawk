﻿using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

/*
	$0400-$0FFF    Cartridge (Only 2K accessible, bit 10 not mapped to cart)
	$0000-$03FF    BIOS
*/

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk
	{
		public byte ReadMemory(ushort addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessRead;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}

			if (addr < 0x400)
			{
				return _bios[addr];
			}

			return mapper.ReadMemory((ushort)((addr - 0x400) + bank_size * rom_bank));
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessWrite;
				MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			}

			if (addr < 0x400)
			{
				// no-op (BIOS)
			}
			else
			{
				mapper.WriteMemory(addr, value);
			}
		}

		public byte PeekMemory(ushort addr)
		{
			if (addr < 0x400)
			{
				return _bios[addr];
			}

			return mapper.PeekMemory((ushort)((addr - 0x400) + bank_size * rom_bank));
		}

		public byte ReadPort(ushort port)
		{
			if (port == 0)
			{
				// BUS, used with external memory and ppu
				if (cpu.EA)
				{
					return addr_latch;
				}

				if (RAM_en)
				{
					if (addr_latch < 0x80)
					{
						return RAM[addr_latch & 0x7F];
					}

					// voice module would return here
					return 0;
				}

				if (ppu_en && !copy_en)
				{
					return ppu.ReadReg(addr_latch);
				}

				if (vpp_en && is_G7400)
				{
					return ppu.ReadRegVPP(addr_latch);
				}

				if (cart_b1 && is_XROM)
				{
					return _rom[((kb_byte & 3) << 8) + addr_latch];
				}

				// if neither RAM or PPU is enabled, then a RD pulse from instruction IN A,BUS will latch controller
				// onto the bus, but only if they are enabled correctly using port 2
				if (kybrd_en)
				{
					_islag = false;
					if ((kb_byte & 1) == 1)
					{
						return controller_state_1;
					}
					if ((kb_byte & 1) == 0)
					{
						return controller_state_2;
					}
				}

				// not sure what happens if this case is reached, probably whatever the last value on the bus is
				// Console.WriteLine("Bad read: " + addr_latch + " " + cpu.TotalExecutedCycles);
				return 0;
			}

			if (port == 1)
			{
				// various control pins
				return unchecked((byte) ((ppu.lum_en ? 0x80 : 0)
					| (copy_en ? 0x40 : 0)
					| (!vpp_en ? 0x20 : 0)
					| (!RAM_en ? 0x10 : 0)
					| (!ppu_en ? 0x08 : 0)
					| (!kybrd_en ? 0x04 : 0)
					| (cart_b1 ? 0x02 : 0)
					| (cart_b0 ? 0x01 : 0)));
			}

			// keyboard
			_islag = false;
			return kb_byte;
		}

		public void WritePort(ushort port, byte value)
		{
			if (port == 0)
			{
				// BUS, used with external memory and ppu
				if (cpu.EA)
				{
					addr_latch = value;
				}
				else
				{
					if (RAM_en && !copy_en)
					{
						if (addr_latch < 0x80)
						{
							RAM[addr_latch] = value;
						}
						else
						{
							// voice module goes here
						}
					}

					if (ppu_en)
					{
						ppu.WriteReg(addr_latch, value);
						//Console.WriteLine((addr_latch) + " " + value);
					}

					if (vpp_en)
					{
						ppu.WriteRegVPP(addr_latch, value);
						//Console.WriteLine((addr_latch) + " " + value);
					}
				}
			}
			else if (port == 1)
			{
				// various control pins
				ppu.lum_en = value.Bit(7);
				copy_en = value.Bit(6);
				vpp_en = !value.Bit(5) && is_G7400;
				RAM_en = !value.Bit(4);
				ppu_en = !value.Bit(3);
				kybrd_en = !value.Bit(2);
				cart_b1 = value.Bit(1);
				cart_b0 = value.Bit(0);

				rom_bank = (ushort)(cart_b0 ? 1 : 0);
				rom_bank |= (ushort)(cart_b1 ? 2 : 0);
				//rom_bank = (ushort)(rom_bank << 12);

				// XROM uses cart_b1 for read enable, not bank switch
				if (is_XROM) { rom_bank = 0; }

				ppu.bg_brightness = !ppu.lum_en ? 8 : 0;
				ppu.grid_brightness = (!ppu.lum_en | ppu.VDC_color.Bit(6)) ? 8 : 0;

				//Console.WriteLine("main ctrl: " + value + " " + ppu.lum_en + " " + ppu_en + " " + RAM_en + " " + cpu.TotalExecutedCycles + " " + ppu.LY + " " + rom_bank);
			}
			else
			{
				// keyboard
				kb_byte = (byte)(value & 7);
				KB_Scan();
			}
		}
	}
}
