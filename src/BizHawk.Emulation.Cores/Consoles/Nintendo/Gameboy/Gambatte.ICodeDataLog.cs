using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl) => SetCDL(cdl, "");

		internal void SetCDL(ICodeDataLog cdl, string which)
		{
			_cdl = cdl;
			_which = which;
			LibGambatte.gambatte_setcdcallback(GambatteState, cdl == null ? null : _cdCallback);
		}

		public void NewCDL(ICodeDataLog cdl) => NewCDL(cdl, "");

		internal void NewCDL(ICodeDataLog cdl, string which)
		{
			cdl[which + "ROM"] = new byte[MemoryDomains["ROM"]!.Size];

			// cdl["HRAM"] = new byte[_memoryDomains["HRAM"]!.Size]; //this is probably useless, but it's here if someone needs it
			cdl[which + "WRAM"] = new byte[MemoryDomains["WRAM"]!.Size];

			var found = MemoryDomains["CartRAM"];
			if (found is not null) cdl[which + "CartRAM"] = new byte[found.Size];

			cdl.SubType = "GB";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		private ICodeDataLog _cdl;
		private string _which;
		private readonly LibGambatte.CDCallback _cdCallback;

		private void CDCallbackProc(int addr, LibGambatte.CDLog_AddrType addrtype, LibGambatte.CDLog_Flags flags)
		{
			if (_cdl is not { Active: true })
			{
				return;
			}

			var key = addrtype switch
			{
				LibGambatte.CDLog_AddrType.ROM => "ROM",
				LibGambatte.CDLog_AddrType.HRAM => "HRAM",
				LibGambatte.CDLog_AddrType.WRAM => "WRAM",
				LibGambatte.CDLog_AddrType.CartRAM => "CartRAM",
				_ => throw new InvalidOperationException("Juniper lightbulb proxy"),
			};

			_cdl[_which + key][addr] |= (byte)flags;
		}

	}
}