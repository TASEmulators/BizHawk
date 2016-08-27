using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public partial class Octoshock : IDisassemblable
	{
		public string Cpu
		{
			get { return "R3000A"; }
			set { }
		}

		public IEnumerable<string> AvailableCpus
		{
			get
			{
				yield return "R3000A";
			}
		}

		public string PCRegisterName
		{
			get { return "pc"; }
		}

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			length = 4;
			//var result = OctoshockDll.shock_Util_DisassembleMIPS();
			return "";
		}
	}
}
