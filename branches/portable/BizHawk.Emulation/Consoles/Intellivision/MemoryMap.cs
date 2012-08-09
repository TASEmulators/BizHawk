using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision
	{
		private const ushort UNMAPPED = 0xFFFF;

		private ushort[] Scratchpad_RAM = new ushort[240];
		private ushort[] System_RAM = new ushort[352];
		private ushort[] Executive_ROM = new ushort[4096]; // TODO: Intellivision II support?
		private ushort[] Graphics_ROM = new ushort[2048];
		private ushort[] Graphics_RAM = new ushort[512];

		public ushort ReadMemory(ushort addr)
		{
			ushort? cart = Cart.ReadCart(addr);
			ushort? stic = Stic.ReadSTIC(addr);
			ushort? psg = Psg.ReadPSG(addr);
			ushort? core = null;
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x007F)
						// STIC.
						break;
					else if (addr <= 0x00FF)
						// Unoccupied.
						break;
					else if (addr <= 0x01EF)
						core = Scratchpad_RAM[addr - 0x0100];
					else if (addr <= 0x01FF)
						// PSG.
						break;
					else if (addr <= 0x035F)
						core = System_RAM[addr - 0x0200];
					else if (addr <= 0x03FF)
						// TODO: Garbage values for Intellivision II.
						break;
					else if (addr <= 0x04FF)
						// TODO: Additional EXEC ROM for Intellivision II.
						break;
					break;
				case 0x1000:
					core = Executive_ROM[addr - 0x1000];
					break;
				case 0x3000:
					if (addr <= 0x37FF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_ROM[addr - 0x3000];
					else if (addr <= 0x39FF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr - 0x3800];
					else if (addr <= 0x3BFF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr - 0x3A00];
					else if (addr <= 0x3DFF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr - 0x3C00];
					else
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr - 0x3E00];
					break;
				case 0x7000:
					if (addr <= 0x77FF)
						// Available to cartridges.
						break;
					else if (addr <= 0x79FF)
						// Write-only Graphics RAM.
						break;
					else if (addr <= 0x7BFF)
						// Write-only Graphics RAM.
						break;
					else if (addr <= 0x7DFF)
						// Write-only Graphics RAM.
						break;
					else
						// Write-only Graphics RAM.
						break;
				case 0xB000:
					if (addr <= 0xB7FF)
						// Available to cartridges.
						break;
					else if (addr <= 0xB9FF)
						// Write-only Graphics RAM.
						break;
					else if (addr <= 0xBBFF)
						// Write-only Graphics RAM.
						break;
					else if (addr <= 0xBDFF)
						// Write-only Graphics RAM.
						break;
					else
						// Write-only Graphics RAM.
						break;
				case 0xF000:
					if (addr <= 0xF7FF)
						// Available to cartridges.
						break;
					else if (addr <= 0xF9FF)
						// Write-only Graphics RAM.
						break;
					else if (addr <= 0xFBFF)
						// Write-only Graphics RAM.
						break;
					else if (addr <= 0xFDFF)
						// Write-only Graphics RAM.
						break;
					else
						// Write-only Graphics RAM.
						break;
			}
			if (cart != null)
				return (ushort)cart;
			else if (stic != null)
				return (ushort)stic;
			else if (psg != null)
				return (ushort)psg;
			else if (core != null)
				return (ushort)core;
			return UNMAPPED;
		}

		public bool WriteMemory(ushort addr, ushort value)
		{
			bool cart = Cart.WriteCart(addr, value);
			bool stic = Stic.WriteSTIC(addr, value);
			bool psg = Psg.WritePSG(addr, value);
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x007F)
						// STIC.
						break;
					else if (addr <= 0x00FF)
						// Unoccupied.
						break;
					else if (addr <= 0x01EF)
					{
						Scratchpad_RAM[addr - 0x0100] = value;
						return true;
					}
					else if (addr <= 0x01FF)
					{
						// PSG.
						break;
					}
					else if (addr <= 0x035F)
					{
						System_RAM[addr - 0x0200] = value;
						return true;
					}
					else if (addr <= 0x03FF)
						// Read-only garbage values for Intellivision II.
						break;
					else if (addr <= 0x04FF)
						// Read-only additional EXEC ROM for Intellivision II.
						break;
					break;
				case 0x1000:
					// Read-only Executive ROM.
					break;
				case 0x3000:
					if (addr <= 0x37FF)
						// Read-only Graphics ROM.
						break;
					else if (addr <= 0x39FF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0x3800] = value;
						return true;
					}
					else if (addr <= 0x3BFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0x3A00] = value;
						return true;
					}
					else if (addr <= 0x3DFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0x3C00] = value;
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0x3E00] = value;
						return true;
					}
				case 0x7000:
					if (addr <= 0x77FF)
						// Available to cartridges.
						break;
					else if (addr <= 0x79FF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0x7BFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0x7DFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
						// Available to cartridges.
						break;
					else if (addr <= 0xB9FF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xB800] = value;
						return true;
					}
					else if (addr <= 0xBBFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xBA00] = value;
						return true;
					}
					else if (addr <= 0xBDFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xBC00] = value;
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xBE00] = value;
						return true;
					}
				case 0xF000:
					if (addr <= 0xF7FF)
						// Available to cartridges.
						break;
					else if (addr <= 0xF9FF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xF800] = value;
						return true;
					}
					else if (addr <= 0xFBFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xFA00] = value;
						return true;
					}
					else if (addr <= 0xFDFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xFC00] = value;
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr - 0xFE00] = value;
						return true;
					}
			}
			return (cart || stic || psg);
		}
	}
}
