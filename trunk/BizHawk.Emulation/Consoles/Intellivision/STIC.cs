using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed class STIC
	{
		private bool Sr1, Sr2, Sst, Fgbg = false;
		private ushort[] Register = new ushort[64];

		public int TotalExecutedCycles;
		public int PendingCycles;
		
		public void Reset()
		{
			Sr1 = true;
			Sr2 = true;
		}

		public bool GetSr1()
		{
			return Sr1;
		}

		public bool GetSr2()
		{
			return Sr2;
		}

		public void SetSst(bool value)
		{
			Sst = value;
		}

		public int GetPendingCycles()
		{
			return PendingCycles;
		}

		public void AddPendingCycles(int cycles)
		{
			PendingCycles += cycles;
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

		public void Execute(int cycles)
		{
			if (PendingCycles <= 0)
			{
				Sr1 = !Sr1;
				if (Sr1)
					AddPendingCycles(14394 - 3791 + 530);
				else
					AddPendingCycles(3791);
			}
			PendingCycles -= cycles;
			TotalExecutedCycles += cycles;
		}
	}
}
