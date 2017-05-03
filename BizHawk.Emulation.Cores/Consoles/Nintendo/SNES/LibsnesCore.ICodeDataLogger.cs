using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : ICodeDataLogger
	{
		public void SetCDL(ICodeDataLog cdl)
		{
			_currCdl?.Unpin();
			_currCdl = cdl;
			_currCdl?.Pin();

			// set it no matter what. if its null, the cdl will be unhooked from libsnes internally
			Api.QUERY_set_cdl(_currCdl);
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["CARTROM"] = new byte[_memoryDomains["CARTROM"].Size];

			if (_memoryDomains.Has("CARTRAM"))
			{
				cdl["CARTRAM"] = new byte[_memoryDomains["CARTRAM"].Size];
			}

			cdl["WRAM"] = new byte[_memoryDomains["WRAM"].Size];
			cdl["APURAM"] = new byte[_memoryDomains["APURAM"].Size];

			cdl.SubType = "SNES";
			cdl.SubVer = 0;
		}

		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
		{
			// TODO: should this throw a NotImplementedException?
			// not supported yet
		}

		private ICodeDataLog _currCdl;
	}
}
