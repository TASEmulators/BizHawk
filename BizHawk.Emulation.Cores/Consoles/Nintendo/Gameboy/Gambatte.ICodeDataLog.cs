using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	partial class Gameboy
	{
		void ICodeDataLogger.SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
			if(cdl == null)
				LibGambatte.gambatte_setcdcallback(GambatteState, null);
			else
				LibGambatte.gambatte_setcdcallback(GambatteState, CDCallback);
		}

		void ICodeDataLogger.NewCDL(ICodeDataLog cdl)
		{
			cdl["ROM"] = new byte[MemoryDomains["ROM"].Size];

			//cdl["HRAM"] = new byte[_memoryDomains["HRAM"].Size]; //this is probably useless, but it's here if someone needs it
			cdl["WRAM"] = new byte[MemoryDomains["WRAM"].Size];

			if (MemoryDomains.Has("CartRAM"))
				cdl["CartRAM"] = new byte[MemoryDomains["CartRAM"].Size];

			cdl.SubType = "GB";
			cdl.SubVer = 0;
		}

		//not supported
		void ICodeDataLogger.DisassembleCDL(Stream s, ICodeDataLog cdl) { }

		ICodeDataLog CDL;
		LibGambatte.CDCallback CDCallback;
		void CDCallbackProc(int addr, LibGambatte.CDLog_AddrType addrtype, LibGambatte.CDLog_Flags flags)
		{
			if (CDL == null) return;
			if (!CDL.Active) return;
			string key;
			switch (addrtype)
			{
				case LibGambatte.CDLog_AddrType.ROM: key = "ROM"; break;
				case LibGambatte.CDLog_AddrType.HRAM: key = "HRAM"; break;
				case LibGambatte.CDLog_AddrType.WRAM: key = "WRAM"; break;
				case LibGambatte.CDLog_AddrType.CartRAM: key = "CartRAM"; break;
				default: throw new InvalidOperationException("Juniper lightbulb proxy");
			}
			CDL[key][addr] |= (byte)flags;
		}

	}
}