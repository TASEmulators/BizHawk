using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision
	{
		private ushort[] STIC_Registers = new ushort[64];
		private ushort[] Scratchpad_RAM = new ushort[240];
		private ushort[] PSG_Registers = new ushort[16];
		private ushort[] System_RAM = new ushort[352];
		private ushort[] Executive_ROM = new ushort[4096];
		private ushort[] Graphics_ROM = new ushort[2048];
		private ushort[] Graphics_RAM = new ushort[512];

		public ushort ReadMemory(ushort addr)
		{
			ushort? cart = ReadCart(addr);
			if (cart != null)
				return (ushort)cart;
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr >= 0x0100 && addr <= 0x01EF)
						return Scratchpad_RAM[addr & 0x00EF];
					if (addr >= 0x01F0 && addr <= 0x01FF)
						return PSG_Registers[addr & 0x000F];
					if (addr >= 0x0200 && addr <= 0x035F)
						return System_RAM[addr & 0x015F];
					break;
				case 0x1000:
					return Executive_ROM[addr & 0x0FFF];
				case 0x3000:
					if (addr >= 0x3000 && addr <= 0x37FF)
						return Graphics_ROM[addr & 0x07FF];
					if (addr >= 0x3800 && addr <= 0x39FF)
						return Graphics_RAM[addr & 0x01FF];
					break;
			}
			throw new NotImplementedException();
		}

		public void WriteMemory(ushort addr, ushort value)
		{
			bool cart = WriteCart(addr, value);
			if (cart)
				return;
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr >= 0x0100 && addr <= 0x01EF)
					{
						Scratchpad_RAM[addr & 0x00EF] = value;
						return;
					}
					if (addr >= 0x01F0 && addr <= 0x01FF)
					{
						PSG_Registers[addr & 0x000F] = value;
						return;
					}
					if (addr >= 0x0200 && addr <= 0x035F)
					{
						System_RAM[addr & 0x015F] = value;
						return;
					}
					break;
				case 0x3000:
					if (addr >= 0x3800 && addr <= 0x39FF)
					{
						Graphics_RAM[addr & 0x01FF] = value;
						return;
					}
					break;
			}
			throw new NotImplementedException();
		}
	}
}
