using BizHawk.Emulation.Common;

using System.IO;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	partial class GambatteLink
	{
		void ICodeDataLogger.SetCDL(ICodeDataLog cdl)
		{
			((ICodeDataLogger)_linkedCores[P1]).SetCDL(cdl);
		}

		void ICodeDataLogger.NewCDL(ICodeDataLog cdl)
		{
			((ICodeDataLogger)_linkedCores[P1]).NewCDL(cdl);
		}

		void ICodeDataLogger.DisassembleCDL(Stream s, ICodeDataLog cdl) { ((ICodeDataLogger)_linkedCores[P1]).DisassembleCDL(s, cdl); }

	}
}