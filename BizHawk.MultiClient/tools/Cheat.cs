using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public class Cheat
	{
		//TODO: compare value (for NES)
		public string name { get; set; }
		public int address { get; set; }
		public byte value { get; set; }
		public MemoryDomain domain { get; set; }
		private bool enabled;

		public Cheat()
		{
			name = "";
			address = 0;
			value = 0;
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
		}

		public Cheat(string cname, int addr, byte val, bool e, MemoryDomain d)
		{
			name = cname;
			address = addr;
			value = val;
			enabled = e;
			domain = d;
		}

		public void Enable()
		{
			enabled = true;
			domain.SetFreeze(address, new MemoryDomain.FreezeData(MemoryDomain.FreezeData.Flag.Frozen, value));
		}

		public void Disable()
		{
			enabled = false;
			domain.SetFreeze(address, MemoryDomain.FreezeData.Empty);
		}

		public bool IsEnabled()
		{
			return enabled;
		}

	}
}
