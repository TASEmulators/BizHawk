using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.H6280;

//TODO - refactor into different files

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
			t["CartRAM"] = new byte[memdomains["Cart RAM"].Size];
			return t;
		}

		public override string SubType { get { return "GB"; } }
		public override int SubVer { get { return 0; } }
	}
}