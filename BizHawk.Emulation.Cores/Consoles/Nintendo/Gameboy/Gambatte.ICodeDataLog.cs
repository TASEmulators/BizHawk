using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			_cdl = cdl;
			LibGambatte.gambatte_setcdcallback(GambatteState, cdl == null ? null : _cdCallback);
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["ROM"] = new byte[MemoryDomains["ROM"].Size];

			// cdl["HRAM"] = new byte[_memoryDomains["HRAM"].Size]; //this is probably useless, but it's here if someone needs it
			cdl["WRAM"] = new byte[MemoryDomains["WRAM"].Size];

			if (MemoryDomains.Has("CartRAM"))
			{
				cdl["CartRAM"] = new byte[MemoryDomains["CartRAM"].Size];
			}

			cdl.SubType = "GB";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		void ICodeDataLogger.DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
		}

		private ICodeDataLog _cdl;
		private readonly LibGambatte.CDCallback _cdCallback;

		private void CDCallbackProc(int addr, LibGambatte.CDLog_AddrType addrtype, LibGambatte.CDLog_Flags flags)
		{
			if (_cdl == null)
			{
				return;
			}

			if (!_cdl.Active)
			{
				return;
			}

			string key;
			switch (addrtype)
			{
				case LibGambatte.CDLog_AddrType.ROM:
					key = "ROM";
					break;
				case LibGambatte.CDLog_AddrType.HRAM:
					key = "HRAM";
					break;
				case LibGambatte.CDLog_AddrType.WRAM:
					key = "WRAM";
					break;
				case LibGambatte.CDLog_AddrType.CartRAM:
					key = "CartRAM";
					break;
				default:
					throw new InvalidOperationException("Juniper lightbulb proxy");
			}

			_cdl[key][addr] |= (byte)flags;
		}

	}
}