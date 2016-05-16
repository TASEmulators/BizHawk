using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	partial class GambatteLink
	{
		void ICodeDataLogger.SetCDL(CodeDataLog cdl)
		{
			((ICodeDataLogger)L).SetCDL(cdl);
		}

		void ICodeDataLogger.NewCDL(CodeDataLog cdl)
		{
			((ICodeDataLogger)L).NewCDL(cdl);
		}

		void ICodeDataLogger.DisassembleCDL(Stream s, CodeDataLog cdl) { ((ICodeDataLogger)L).DisassembleCDL(s, cdl); }

	}
}