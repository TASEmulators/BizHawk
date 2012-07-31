using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision
	{
		private const string INVALID = "Invalid memory address.";

		private ushort[] STIC_Registers = new ushort[64];
		private ushort[] Scratchpad_RAM = new ushort[240];
		private ushort[] PSG_Registers = new ushort[16];
		private ushort[] System_RAM = new ushort[352];
		private ushort[] Executive_ROM = new ushort[4096]; // TODO: Intellivision II support?
		private ushort[] Graphics_ROM = new ushort[2048];
		private ushort[] Graphics_RAM = new ushort[512];

		public ushort ReadMemory(ushort addr)
		{
			ushort? cart = Cart.Read(addr);
			ushort? core = null;
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
						// TODO: OK only during VBlank Period 1.
						core = STIC_Registers[addr];
					else if (addr <= 0x007F)
						// TODO: OK only during VBlank Period 2.
						core = STIC_Registers[addr & 0x003F];
					else if (addr <= 0x00FF)
						// Unoccupied.
						break;
					else if (addr <= 0x01EF)
						core = Scratchpad_RAM[addr & 0x00EF];
					else if (addr <= 0x01FF)
						core = PSG_Registers[addr & 0x000F];
					else if (addr <= 0x035F)
						core = System_RAM[addr & 0x015F];
					else if (addr <= 0x03FF)
						// TODO: Garbage values for Intellivision II.
						break;
					else if (addr <= 0x04FF)
						// TODO: Additional EXEC ROM for Intellivision II.
						break;
					break;
				case 0x1000:
					core = Executive_ROM[addr & 0x0FFF];
					break;
				case 0x3000:
					if (addr <= 0x37FF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_ROM[addr & 0x07FF];
					else if (addr <= 0x39FF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					else if (addr <= 0x3BFF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					else if (addr <= 0x3DFF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					else
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						if (addr == 0x4021)
							// TODO: OK only during VBlank Period 1.
							// TODO: Switch into Color Stack mode.
							core = STIC_Registers[0x0021];
					}
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
				case 0x8000:
					if (addr <= 0x803F)
					{
						if (addr == 0x8021)
							// TODO: OK only during VBlank Period 1.
							// TODO: Switch into Color Stack mode.
							core = STIC_Registers[0x0021];
					}
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
				case 0xC000:
					if (addr <= 0xC03F)
					{
						if (addr == 0xC021)
							// TODO: OK only during VBlank Period 1.
							// TODO: Switch into Color Stack mode.
							core = STIC_Registers[0x0021];
					}
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
			else if (core == null)
				throw new ArgumentException(INVALID);
			return (ushort)core;
		}

		public bool WriteMemory(ushort addr, ushort value)
		{
			bool cart = Cart.Write(addr, value);
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
					{
						// TODO: OK only during VBlank Period 1.
						STIC_Registers[addr] = value;
						return true;
					}
					else if (addr <= 0x007F)
						// Read-only STIC.
						break;
					else if (addr <= 0x00FF)
						// Unoccupied.
						break;
					else if (addr <= 0x01EF)
					{
						Scratchpad_RAM[addr & 0x00EF] = value;
						return true;
					}
					else if (addr <= 0x01FF)
					{
						PSG_Registers[addr & 0x000F] = value;
						return true;
					}
					else if (addr <= 0x035F)
					{
						System_RAM[addr & 0x015F] = value;
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
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0x3BFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0x3DFF)
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
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: OK only during VBlank Period 1.
						STIC_Registers[addr & 0x003F] = value;
						return true;
					}
					break;
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
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						STIC_Registers[addr & 0x003F] = value;
						return true;
					}
					break;
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
						// Available to cartridges.
						break;
					else if (addr <= 0xB9FF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0xBBFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0xBDFF)
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
				case 0xC000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						STIC_Registers[addr & 0x003F] = value;
						return true;
					}
					break;
				case 0xF000:
					if (addr <= 0xF7FF)
						// Available to cartridges.
						break;
					else if (addr <= 0xF9FF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0xFBFF)
					{
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
						return true;
					}
					else if (addr <= 0xFDFF)
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
			}
			return cart;
		}
	}
}
