using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx64
{
	public partial class GPGX : ICodeDataLogger
	{
		public void SetCDL(CodeDataLog cdl)
		{
			CDL = cdl;
			if (cdl == null) Core.gpgx_set_cd_callback(null);
			else Core.gpgx_set_cd_callback(CDCallback);
		}

		public void NewCDL(CodeDataLog cdl)
		{
			cdl["MD CART"] = new byte[MemoryDomains["MD CART"].Size];
			cdl["68K RAM"] = new byte[MemoryDomains["68K RAM"].Size];
			cdl["Z80 RAM"] = new byte[MemoryDomains["Z80 RAM"].Size];

			if (MemoryDomains.Has("SRAM"))
				cdl["SRAM"] = new byte[MemoryDomains["SRAM"].Size];

			cdl.SubType = "GEN";
			cdl.SubVer = 0;
		}

		// TODO: we have Disassembling now
		// not supported
		public void DisassembleCDL(Stream s, CodeDataLog cdl) { }

		private CodeDataLog CDL;
		private void CDCallbackProc(int addr, LibGPGX.CDLog_AddrType addrtype, LibGPGX.CDLog_Flags flags)
		{
			//TODO - hard reset makes CDL go nuts.

			if (CDL == null) return;
			if (!CDL.Active) return;
			string key;
			switch (addrtype)
			{
				case LibGPGX.CDLog_AddrType.MDCART: key = "MD CART"; break;
				case LibGPGX.CDLog_AddrType.RAM68k: key = "68K RAM"; break;
				case LibGPGX.CDLog_AddrType.RAMZ80: key = "Z80 RAM"; break;
				case LibGPGX.CDLog_AddrType.SRAM: key = "SRAM"; break;
				default: throw new InvalidOperationException("Lagrangian earwax incident");
			}
			CDL[key][addr] |= (byte)flags;
		}
	}
}
