using BizHawk.Emulation.Common;

using System.IO;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	partial class GambatteLink : ICodeDataLogger
	{
		void ICodeDataLogger.SetCDL(ICodeDataLog cdl)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].SetCDL(cdl, $"P{i + 1} ");
			}
		}

		void ICodeDataLogger.NewCDL(ICodeDataLog cdl)
		{
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].NewCDL(cdl, $"P{i + 1} ");
			}
		}

		[FeatureNotImplemented]
		void ICodeDataLogger.DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
			// this doesn't actually do anything
			/*
			for (int i = 0; i < _numCores; i++)
			{
				_linkedCores[i].DisassembleCDL(s, cdl);
			}
			*/
		}
	}
}