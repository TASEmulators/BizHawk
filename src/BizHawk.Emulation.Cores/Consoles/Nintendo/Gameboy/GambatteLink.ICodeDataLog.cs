using System.IO;

using BizHawk.Emulation.Common;

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
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
			throw new NotImplementedException();
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