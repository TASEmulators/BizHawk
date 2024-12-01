using System.IO;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : ICodeDataLogger
	{
		private ICodeDataLog _cdl;

		public void SetCDL(ICodeDataLog cdl)
		{
			_cdl = cdl;
			if (cdl == null)
				this.cpu.CDLCallback = null;
			else this.cpu.CDLCallback = CDLCpuCallback;
		}

		public void NewCDL(ICodeDataLog cdl)
		{
			cdl["ROM"] = new byte[MemoryDomains["ROM"]!.Size];
			cdl["HRAM"] = new byte[MemoryDomains["Zero Page RAM"]!.Size];

			cdl["WRAM"] = new byte[MemoryDomains["Main RAM"]!.Size];

			var found = MemoryDomains["Cart RAM"];
			if (found is not null) cdl["CartRAM"] = new byte[found.Size];

			cdl.SubType = "O2";
			cdl.SubVer = 0;
		}

		[FeatureNotImplemented]
		public void DisassembleCDL(Stream s, ICodeDataLog cdl)
			=> throw new NotImplementedException();

		public void SetCDL(I8048.eCDLogMemFlags flags, string type, int cdladdr)
		{
			if (type == null) return;
			byte val = (byte)flags;
			_cdl[type][cdladdr] |= (byte)flags;
		}

		private void CDLCpuCallback(ushort addr, I8048.eCDLogMemFlags flags)
		{

			if (addr < 0x400)
			{

			}
			else
			{
				mapper.MapCDL(addr, flags);
				return;
			}
		}	
	}
}