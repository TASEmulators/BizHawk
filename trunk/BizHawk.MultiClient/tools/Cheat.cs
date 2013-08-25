using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public class Cheat
	{
		public string Name { get; set; }
		public int Address { get; set; }
		public byte Value { get; set; }
		public byte? Compare { get; set; }
		public MemoryDomain Domain { get; set; }
		
		private bool enabled;

		public Cheat()
		{
			Name = "";
			Address = 0;
			Value = 0;
			Compare = null;
			enabled = false;
			Domain = new MemoryDomain("NULL", 1, Endian.Little, addr => 0, (a, v) => { });
		}

		public Cheat(Cheat c)
		{
			Name = c.Name;
			Address = c.Address;
			Value = c.Value;
			enabled = c.enabled;
			Domain = c.Domain;
			Compare = c.Compare;
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
			Name = cname;
			Address = addr;
			Value = val;
			enabled = e;
			Domain = d;
			Compare = comp;
			if (enabled)
			{
				Enable();
			}
			else
			{
				Disable();
			}
		}

		public bool IsSeparator
		{
			get
			{
				return Address == -1; //TODO: make this a nullable instead
			}
		}

		public void Enable()
		{
			enabled = true;
			if (Global.Emulator is NES && Domain == Global.Emulator.MemoryDomains[1])
			{
				(Global.Emulator as NES).ApplyGameGenie(Address, Value, Compare);
			}
			else
			{
				MemoryPulse.Add(Domain, Address, Value, Compare);
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
			if (Global.Emulator is NES && Domain == Global.Emulator.MemoryDomains[1])
			{
				(Global.Emulator as NES).RemoveGameGenie(Address);
			}
			else
			{
				MemoryPulse.Remove(Domain, Address);
			}
		}

		public bool IsEnabled
		{
			get
			{
				return enabled;
			}
		}

		~Cheat()
		{
			DisposeOfCheat();
		}
	}
}
