using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	partial class GambatteLink
	{
		void ICodeDataLogger.SetCDL(ICodeDataLog cdl)
		{
			((ICodeDataLogger)L).SetCDL(cdl);
		}

		void ICodeDataLogger.NewCDL(ICodeDataLog cdl)
		{
			((ICodeDataLogger)L).NewCDL(cdl);
		}

		void ICodeDataLogger.DisassembleCDL(Stream s, ICodeDataLog cdl) { ((ICodeDataLogger)L).DisassembleCDL(s, cdl); }

	}
}