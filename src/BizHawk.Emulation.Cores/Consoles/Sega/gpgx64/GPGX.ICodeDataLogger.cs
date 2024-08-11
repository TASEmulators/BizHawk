using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			CDL = cdl;
			Core.gpgx_set_cd_callback(cdl == null ? null : CDCallback);
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["MD CART"] = new byte[_memoryDomains["MD CART"]!.Size];
			cdl["68K RAM"] = new byte[_memoryDomains["68K RAM"]!.Size];
			cdl["Z80 RAM"] = new byte[_memoryDomains["Z80 RAM"]!.Size];

			var found = _memoryDomains["SRAM"];
			if (found is not null)
			{
				cdl["SRAM"] = new byte[found.Size];
			}

			cdl.SubType = "GEN";
			cdl.SubVer = 0;
		}

		// TODO: we have Disassembling now
		// not supported
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
		}

		private ICodeDataLog CDL;
		private void CDCallbackProc(int addr, LibGPGX.CDLog_AddrType addrtype, LibGPGX.CDLog_Flags flags)
		{
			// TODO - hard reset makes CDL go nuts.

			if (CDL is not { Active: true })
			{
				return;
			}

			var key = addrtype switch
			{
				LibGPGX.CDLog_AddrType.MDCART => "MD CART",
				LibGPGX.CDLog_AddrType.RAM68k => "68K RAM",
				LibGPGX.CDLog_AddrType.RAMZ80 => "Z80 RAM",
				LibGPGX.CDLog_AddrType.SRAM => "SRAM",
				_ => throw new InvalidOperationException("Lagrangian earwax incident")
			};

			CDL[key][addr] |= (byte)flags;
		}
	}
}
