using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk
	{
		public byte ReadMemory(ushort addr)
		{
			MemoryCallbacks.CallReads(addr);

			if (addr < 0x0400) {
				if ((addr & 0xFF) < 0x20)
				{
					// return TIA registers or control register if it is still unlocked
					if ((A7800_control_register & 0x1) == 0 && (addr < 0x20))
					{
						return 0xFF; // TODO: what to return here?
					}
					else
					{
						return TIA_regs[addr]; // TODO: what to return here?
					}
				}
				else if ((addr & 0xFF) < 0x40)
				{
					if ((A7800_control_register & 0x2) > 0)
					{
						return Maria_regs[(addr & 0x3F) - 0x20];
					}
					else
					{
						return 0xFF;
					}
				}
				else if (addr < 0x100)
				{
					// RAM block 0
					return RAM[addr - 0x40 + 0x840];
				}
				else if (addr < 0x200)
				{
					// RAM block 1
					return RAM[addr - 0x140 + 0x940];
				}
				else if (addr < 0x300)
				{
					return regs_6532[addr - 0x240];
				}
				else
				{
					return 0xFF; // what is mapped here?
				}
			}
			else if (addr < 0x480)
			{
				// cartridge space available
				return mapper.ReadMemory(addr);
			}
			else if (addr < 0x500)
			{
				// this is where RAM for the 6532 resides for use in 2600 mode
				return 0xFF; 
			}
			else if (addr < 0x1800)
			{
				// cartridge space available
				return mapper.ReadMemory(addr);
			}
			else if (addr < 0x2800)
			{
				return RAM[addr - 0x1800];
			}
			else if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				return mapper.ReadMemory(addr);
			}
			else
			{
				return mapper.ReadMemory(addr);
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			MemoryCallbacks.CallWrites(addr);

			if (addr < 0x0400)
			{
				if ((addr & 0xFF) < 0x20)
				{
					// return TIA registers or control register if it is still unlocked
					if ((A7800_control_register & 0x1) == 0 && (addr < 0x20))
					{
						A7800_control_register = value;
					}
					else
					{
						TIA_regs[addr] = value; 
					}
				}
				else if ((addr & 0xFF) < 0x40)
				{
					if ((A7800_control_register & 0x2) > 0)
					{
						var temp = (addr & 0x3F) - 0x20;

						// register 8 is read only and controlled by Maria
						if (temp != 8)
							Maria_regs[temp] = value;

						if (temp==4) // WSYNC
							cpu.RDY = false;
					}
				}
				else if (addr < 0x100)
				{
					// RAM block 0
					RAM[addr - 0x40 + 0x840] = value;
				}
				else if (addr < 0x200)
				{
					// RAM block 1
					RAM[addr - 0x140 + 0x940] = value;
				}
				else if (addr < 0x300)
				{
					regs_6532[addr - 0x240] = value;
				}
				else
				{
					// what is mapped here?
				}
			}
			else if (addr < 0x480)
			{
				// cartridge space available
				mapper.WriteMemory(addr, value);
			}
			else if (addr < 0x500)
			{
				// this is where RAM for the 6532 resides for use in 2600 mode
				// is it accessible in 7800 mode?
			}
			else if (addr < 0x1800)
			{
				// cartridge space available
				mapper.WriteMemory(addr, value);
			}
			else if (addr < 0x2800)
			{
				RAM[addr - 0x1800] = value;
			}
			else if (addr < 0x4000)
			{
				// could be either RAM mirror or ROM
				mapper.WriteMemory(addr, value);
			}
			else
			{
				// cartridge and other OPSYS
				mapper.WriteMemory(addr, value);
			}
		}


	}
}
