// X = don't care
/*
1. TIA   0000 00XX 0000 0000 - 0000 00XX 0001 1111
2. MARIA 0000 00XX 0010 0000 - 0000 00XX 0011 1111
3. 6532  0000 0010 1000 0000 - 0000 0010 1111 1111
PORTS
4. 6532  0000 010X 1000 0000 - 0000 010X 1111 1111
RAM(DON'T USE)
5. RAM   0001 1000 0000 0000 - 0010 0111 1111 1111
6. RAM   00X0 000A 0100 0000 - 00X0 000A 1111 1111
SHADOW
7. RAM   001X X000 0000 0000 - 001X X111 1111 1111
*/

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk
	{
		public byte ReadMemory(ushort addr)
		{
			uint flags = (uint)(Common.MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");

			if ((addr & 0xFCE0) == 0)
			{
				// return TIA registers or control register if it is still unlocked
				if ((A7800_control_register & 0x1) == 0)
				{
					return 0xFF; // TODO: what to return here?
				}

				slow_access = true;
				return tia.ReadMemory((ushort)(addr & 0x1F), false);
			}
			
			if ((addr & 0xFCE0) == 0x20)
			{
				if ((A7800_control_register & 0x2) > 0)
				{
					return Maria_regs[addr & 0x1F];
				}
				else
				{
					return 0x80; // TODO: What if Maria is off?
				}
			}
			
			if ((addr & 0xFF80) == 0x280)
			{
				slow_access = true;
				return m6532.ReadMemory(addr, false);
			}
			
			if ((addr & 0xFE80) == 0x480)
			{
				slow_access = true;
				return RAM_6532[addr & 0x7F];
			}
			
			if ((addr >= 0x1800) && (addr < 0x2800))
			{
				return RAM[addr -0x1800];
			}
			
			if ((addr >= 0x40) && (addr < 0x100))
			{
				// RAM block 0
				return RAM[addr - 0x40 + 0x840];
			}
			
			if ((addr >= 0x140) && (addr < 0x200))
			{
				// RAM block 1
				return RAM[addr - 0x140 + 0x940];
			}

			if ((addr >= 0x2800) && (addr < 0x3000))
			{
				// this mirror evidently does not exist on hardware despite being in the documentation
				return 0xFF;// RAM[(addr & 0x7FF) + 0x800];
			}

			if ((addr >= 0x3000) && (addr < 0x4000))
			{
				// could be either RAM mirror or ROM
				return mapper.ReadMemory(addr);
			}

			if ((addr >= 0x400) && (addr < 0x480))
			{
				// cartridge space available
				return mapper.ReadMemory(addr);
			}

			if ((addr >= 0x500) && (addr < 0x1800))
			{
				// cartridge space available
				return mapper.ReadMemory(addr);
			}

			return mapper.ReadMemory(addr);
		}

		public void WriteMemory(ushort addr, byte value)
		{
			uint flags = (uint)(Common.MemoryCallbackFlags.AccessWrite);
			MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");

			if ((addr & 0xFCE0) == 0)
			{
				// return TIA registers or control register if it is still unlocked
				if ((A7800_control_register & 0x1) == 0)
				{
					A7800_control_register = value;
				}
				else
				{
					slow_access = true;
					tia.WriteMemory((ushort)(addr & 0x1F), value, false);
				}
			}
			else if ((addr & 0xFCE0) == 0x20)
			{
				if ((A7800_control_register & 0x2) > 0)
				{
					// register 8 is read only and controlled by Maria
					var temp = addr & 0x1F;

					if (temp != 8)
						Maria_regs[temp] = value;

					if (temp == 4) // WSYNC
						cpu.RDY = false;
					/*
					for (int i = 0; i < 0x20; i++) 
					{
						Console.Write(Maria_regs[i]);
						Console.Write(" ");
					}
					Console.WriteLine(maria.scanline);
					*/
				}
				else
				{
					// TODO: What if Maria is off?
				}
			}
			else if ((addr & 0xFF80) == 0x280)
			{
				slow_access = true;
				m6532.WriteMemory(addr, value);
			}
			else if ((addr & 0xFE80) == 0x480)
			{
				slow_access = true;
				RAM_6532[addr & 0x7F] = value;
			}
			else if ((addr >= 0x1800) && (addr < 0x2800))
			{
				RAM[addr - 0x1800] = value;
			}
			else if ((addr >= 0x40) && (addr < 0x100))
			{
				// RAM block 0
				RAM[addr - 0x40 + 0x840] = value;
			}
			else if ((addr >= 0x140) && (addr < 0x200))
			{
				// RAM block 1
				RAM[addr - 0x140 + 0x940] = value;
			}
			else if ((addr >= 0x2800) && (addr < 0x3000))
			{
				// this mirror evidently does not exist on hardware despite being in the documentation
				//RAM[(addr & 0x7FF) + 0x800] = value;
			}
			else if ((addr >= 0x3000) && (addr < 0x4000))
			{
				// could be either RAM mirror or ROM
				mapper.WriteMemory(addr, value);
			}
			else if ((addr >= 0x400) && (addr < 0x480))
			{
				// cartridge space available
				mapper.WriteMemory(addr, value);
			}
			else if ((addr >= 0x500) && (addr < 0x1800))
			{
				// cartridge space available
				mapper.WriteMemory(addr, value);
			}
			else
			{
				mapper.WriteMemory(addr, value);
			}
		}
	}
}
