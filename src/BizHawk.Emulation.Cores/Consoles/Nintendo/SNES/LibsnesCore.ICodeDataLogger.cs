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
			void AddIfExists(string name, string addAs = null)
			{
				var found = _memoryDomains[name];
				if (found is not null) cdl[addAs ?? name] = new byte[found.Size];
			}

			cdl["CARTROM"] = new byte[_memoryDomains["CARTROM"]!.Size];
			cdl["CARTROM-DB"] = new byte[_memoryDomains["CARTROM"]!.Size];
			cdl["CARTROM-D"] = new byte[_memoryDomains["CARTROM"]!.Size*2];
			cdl["WRAM"] = new byte[_memoryDomains["WRAM"]!.Size];
			cdl["APURAM"] = new byte[_memoryDomains["APURAM"]!.Size];
			AddIfExists("CARTRAM");

			if (IsSGB)
			{
				cdl["SGB_CARTROM"] = new byte[_memoryDomains["SGB CARTROM"]!.Size];
				cdl["SGB_HRAM"] = new byte[_memoryDomains["SGB HRAM"]!.Size];
				cdl["SGB_WRAM"] = new byte[_memoryDomains["SGB WRAM"]!.Size];
				AddIfExists("SGB CARTRAM", addAs: "SGB_CARTRAM");
			}

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
