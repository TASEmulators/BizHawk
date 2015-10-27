using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.H6280;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public class CodeDataLog_GB : CodeDataLog
	{
		public static CodeDataLog_GB Create(IMemoryDomains memdomains)
		{
			var t = new CodeDataLog_GB();
			
			t["ROM"] = new byte[memdomains["ROM"].Size];
			
			//t["HRAM"] = new byte[memdomains["HRAM"].Size]; //this is probably useless, but it's here if someone needs it
			t["WRAM"] = new byte[memdomains["WRAM"].Size];

			if(memdomains.Has("CartRAM"))
				t["CartRAM"] = new byte[memdomains["WRAM"].Size];

			return t;
		}

		public override string SubType { get { return "GB"; } }
		public override int SubVer { get { return 0; } }

		//todo - this could be base classed
		public bool CheckConsistency(IMemoryDomains memdomains)
		{
			if (memdomains["ROM"].Size != this["ROM"].Length) return false;
			if (memdomains["WRAM"].Size != this["WRAM"].Length) return false;
			if (memdomains.Has("CartRAM") != this.ContainsKey("CartRAM")) return false;
			if(memdomains.Has("CartRAM"))
				if (memdomains["CartRAM"].Size != this["CartRAM"].Length) 
					return false;
			return true;
		}
	}
}