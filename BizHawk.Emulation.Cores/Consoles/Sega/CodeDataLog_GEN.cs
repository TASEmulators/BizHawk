using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.H6280;

namespace BizHawk.Emulation.Cores.Consoles.Sega
{
	public class CodeDataLog_GEN : CodeDataLog
	{
		public static CodeDataLog_GEN Create(IMemoryDomains memdomains)
		{
			var t = new CodeDataLog_GEN();
			
			t["MD CART"] = new byte[memdomains["MD CART"].Size];
			t["68K RAM"] = new byte[memdomains["68K RAM"].Size];
			t["Z80 RAM"] = new byte[memdomains["Z80 RAM"].Size];

			if(memdomains.Has("SRAM"))
				t["SRAM"] = new byte[memdomains["SRAM"].Size];

			return t;
		}

		public override string SubType { get { return "GEN"; } }
		public override int SubVer { get { return 0; } }

		//todo - this could be base classed
		public bool CheckConsistency(IMemoryDomains memdomains)
		{
			if (memdomains["MD CART"].Size != this["MD CART"].Length) return false;
			if (memdomains["68K RAM"].Size != this["68K RAM"].Length) return false;
			if (memdomains["Z80 RAM"].Size != this["Z80 RAM"].Length) return false;
			if (memdomains.Has("SRAM") != this.ContainsKey("SRAM")) return false;
			if (memdomains.Has("SRAM"))
				if (memdomains["SRAM"].Size != this["SRAM"].Length) 
					return false;
			return true;
		}
	}
}