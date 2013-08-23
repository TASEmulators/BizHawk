using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public class Cheat
	{
		public string name { get; set; }
		public int address { get; set; }
		public byte value { get; set; }
		public byte? compare { get; set; }
		public MemoryDomain domain { get; set; }
		private bool enabled;

		public Cheat()
		{
			name = "";
			address = 0;
			value = 0;
			compare = null;
			enabled = false;
			domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
		}

		public Cheat(Cheat c)
		{
			name = c.name;
			address = c.address;
			value = c.value;
			enabled = c.enabled;
			domain = c.domain;
			compare = c.compare;
			if (enabled)
			{
				Enable();
			}
			else
			{
				Disable();
			}
		}

		public Cheat(string cname, int addr, byte val, bool e, MemoryDomain d, byte? comp = null)
		{
			name = cname;
			address = addr;
			value = val;
			enabled = e;
			domain = d;
			compare = comp;
			if (enabled)
			{
				Enable();
			}
			else
			{
				Disable();
			}
		}

		public void Enable()
		{
			enabled = true;
			if (Global.Emulator is NES && domain == Global.Emulator.MemoryDomains[1])
			{
				(Global.Emulator as NES).ApplyGameGenie(address, value, compare);
			}
			else
			{
				MemoryPulse.Add(domain, address, value, compare);
			}

			Global.MainForm.UpdateCheatStatus();
		}

		public void Disable()
		{
			enabled = false;
			DisposeOfCheat();
			Global.MainForm.UpdateCheatStatus();
		}

		public void DisposeOfCheat()
		{
			if (Global.Emulator is NES && domain == Global.Emulator.MemoryDomains[1])
			{
				(Global.Emulator as NES).RemoveGameGenie(address);
			}
			else
			{
				MemoryPulse.Remove(domain, address);
			}
		}

		public bool IsEnabled()
		{
			return enabled;
		}

		~Cheat()
		{
			DisposeOfCheat();
		}
	}
}
