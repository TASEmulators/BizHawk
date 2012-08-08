using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class STIC
	{
		private ushort[] Registers = new ushort[64];
		private bool FGBG = false;

		public ushort? ReadSTIC(ushort addr)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x0021)
							FGBG = false;
						return Registers[addr];
					}
					else if (addr <= 0x007F)
						// TODO: OK only during VBlank Period 2.
						return Registers[addr - 0x0040];
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x4021)
							FGBG = false;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x8021)
							FGBG = false;
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0xC021)
							FGBG = false;
					}
					break;
			}
			return null;
		}

		public bool WriteSTIC(ushort addr, ushort value)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x0021)
							FGBG = true;
						Registers[addr] = value;
						return true;
					}
					else if (addr <= 0x007F)
						// Read-only STIC.
						break;
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x4021)
							FGBG = true;
						Registers[addr - 0x4000] = value;
						return true;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x8021)
							FGBG = true;
						Registers[addr & 0x003F] = value;
						return true;
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0xC021)
							FGBG = true;
						Registers[addr - 0xC000] = value;
						return true;
					}
					break;
			}
			return false;
		}
	}
}
