using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class STIC
	{
		private bool Fgbg = false;
		private ushort[] Register = new ushort[64];

		public Func<bool> GetSr1;
		public Func<bool> GetSr2;
		public Func<bool> GetSst;
		public Action<bool> SetSr1;
		public Action<bool> SetSr2;

		private bool Sr1 { set { SetSr1(value); } }
		private bool Sr2 { set { SetSr2(value); } }
		private bool Sst { get { return GetSst(); } }
		
		public void Reset()
		{
			Sr1 = true;
			Sr2 = true;
		}

		public ushort? ReadSTIC(ushort addr)
		{
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x0021)
							Fgbg = false;
						return Register[addr];
					}
					else if (addr <= 0x007F)
						// TODO: OK only during VBlank Period 2.
						return Register[addr - 0x0040];
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x4021)
							Fgbg = false;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x8021)
							Fgbg = false;
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0xC021)
							Fgbg = false;
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
							Fgbg = true;
						Register[addr] = value;
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
							Fgbg = true;
						Register[addr - 0x4000] = value;
						return true;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0x8021)
							Fgbg = true;
						Register[addr & 0x003F] = value;
						return true;
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: OK only during VBlank Period 1.
						if (addr == 0xC021)
							Fgbg = true;
						Register[addr - 0xC000] = value;
						return true;
					}
					break;
			}
			return false;
		}
	}
}
