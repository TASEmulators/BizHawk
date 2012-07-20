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
			ushort core;
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr >= 0x0100 && addr <= 0x01EF)
						core = Scratchpad_RAM[addr & 0x00EF];
					else if (addr >= 0x01F0 && addr <= 0x01FF)
						core = PSG_Registers[addr & 0x000F];
					else if (addr >= 0x0200 && addr <= 0x035F)
						core = System_RAM[addr & 0x015F];
					else
						throw new NotImplementedException();
					break;
				case 0x1000:
					core = Executive_ROM[addr & 0x0FFF];
					break;
				case 0x3000:
					if (addr >= 0x3000 && addr <= 0x37FF)
						core = Graphics_ROM[addr & 0x07FF];
					else if (addr >= 0x3800 && addr <= 0x39FF)
						core = Graphics_RAM[addr & 0x01FF];
					else
						throw new NotImplementedException();
					break;
				default:
					throw new NotImplementedException();
			}
			/*
			TODO: Fix Intellicart hook.
			if (cart != null)
				return (ushort)cart;
			*/
			return core;
		}

		public void WriteMemory(ushort addr, ushort value)
		{
			WriteCart(addr, value);
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr >= 0x0100 && addr <= 0x01EF)
					{
						Scratchpad_RAM[addr & 0x00EF] = value;
						break;
					}
					else if (addr >= 0x01F0 && addr <= 0x01FF)
					{
						PSG_Registers[addr & 0x000F] = value;
						break;
					}
					else if (addr >= 0x0200 && addr <= 0x035F)
					{
						System_RAM[addr & 0x015F] = value;
						break;
					}
					else
						throw new NotImplementedException();
				case 0x3000:
					if (addr >= 0x3800 && addr <= 0x39FF)
					{
						Graphics_RAM[addr & 0x01FF] = value;
						break;
					}
					else
						throw new NotImplementedException();
				default:
					throw new NotImplementedException();
			}
		}
	}
}
