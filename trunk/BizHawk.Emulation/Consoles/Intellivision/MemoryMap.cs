using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision
	{
		private ushort[] STIC_Registers;
		private ushort[] Scratchpad_RAM;
		private ushort[] PSG_Registers;
		private ushort[] System_RAM;
		private ushort[] Executive_ROM;
		private ushort[] Graphics_ROM;
		private ushort[] Graphics_RAM;

		public ushort ReadMemory(ushort addr)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr >= 0x0100 && addr <= 0x01EF)
						return Scratchpad_RAM[addr];
					if (addr >= 0x01F0 && addr <= 0x01FF)
						return PSG_Registers[addr];
					if (addr >= 0x0200 && addr <= 0x035F)
						return System_RAM[addr];
					break;
				case 0x1000:
					return Executive_ROM[addr];
				case 0x3000:
					if (addr >= 0x3000 && addr <= 0x37FF)
						return Graphics_ROM[addr];
					if (addr >= 0x3800 && addr <= 0x39FF)
						return Graphics_RAM[addr];
					break;
			}
			throw new NotImplementedException();
		}

		public void WriteMemory(ushort addr, ushort value)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr >= 0x0100 && addr <= 0x01EF)
					{
						Scratchpad_RAM[addr] = value;
						return;
					}
					if (addr >= 0x01F0 && addr <= 0x01FF)
					{
						PSG_Registers[addr] = value;
						return;
					}
					if (addr >= 0x0200 && addr <= 0x035F)
					{
						System_RAM[addr] = value;
						return;
					}
					break;
				case 0x1000:
					Executive_ROM[addr] = value;
					return;
				case 0x3000:
					if (addr >= 0x3000 && addr <= 0x37FF)
					{
						Graphics_ROM[addr] = value;
						return;
					}
					if (addr >= 0x3800 && addr <= 0x39FF)
					{
						Graphics_RAM[addr] = value;
						return;
					}
					break;
			}
			throw new NotImplementedException();
		}
	}
}
