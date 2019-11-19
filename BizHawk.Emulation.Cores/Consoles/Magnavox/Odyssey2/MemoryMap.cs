using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Common.NumberExtensions;
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
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");

			if (addr < 0x400)
			{
				return _bios[addr];
			}
			else
			{
				return mapper.ReadMemory((ushort)(addr - 0x400));
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessWrite);
			MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");

			if (addr < 0x400)
			{

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
			else
			{
				return mapper.PeekMemory((ushort)(addr - 0x400));
			}
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
				else
				{
					if (RAM_en)
					{
						if (addr_latch < 0x80) 
						{
							return RAM[addr_latch & 0x7F];
						}
						else
						{
							// voice module would return here
							return 0;
						}
					}
					if (ppu_en && !copy_en)
					{
						if ((addr_latch >= 0xA7) || (addr_latch <= 0xAA))
						{
							return audio.ReadReg(addr_latch);
						}
						return ppu.ReadReg(addr_latch);
					}

					// not sure what happens if this case is reached, probably whatever the last value on the bus is
					return 0;
				}
			}
			else if (port == 1)
			{
				// various control pins
				return (byte)((lum_en ? 0x80 : 0) |
				(copy_en ? 0x40 : 0) |
				(0x20) |
				(!RAM_en ? 0x10 : 0) |
				(!ppu_en ? 0x08 : 0) |
				(!kybrd_en ? 0x04 : 0) |
				(cart_b1 ? 0x02 : 0) |
				(cart_b0 ? 0x01 : 0));
			}
			else
			{
				// keyboard
				return 0;
			}
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
						if ((addr_latch >= 0xA7) || (addr_latch <= 0xAA))
						{
							audio.WriteReg(addr_latch, value);
						}
						else
						{
							ppu.WriteReg(addr_latch, value);
						}			
					}					
				}
			}
			else if (port == 1)
			{
				// various control pins
				lum_en = value.Bit(7);
				copy_en = value.Bit(6);
				RAM_en = !value.Bit(4);
				ppu_en = !value.Bit(3);
				kybrd_en = !value.Bit(2);
				cart_b1 = value.Bit(1);
				cart_b0 = value.Bit(0);
			}
			else
			{
				// keyboard
			}
		}
	}
}
