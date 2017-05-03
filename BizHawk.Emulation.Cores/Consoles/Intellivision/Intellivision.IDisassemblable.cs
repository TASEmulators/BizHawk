using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IDisassemblable
	{
		public string Cpu
		{
			get { return "CP1610"; }
			set { }
		}

		public string PCRegisterName
		{
			get { return "PC"; }
		}

		public IEnumerable<string> AvailableCpus
		{
			get { yield return "CP1610"; }
		}

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			// Note: currently this core can only disassemble the SystemBus
			// The CP1610 disassembler would need some refactoring to support anything else
			string ret = _cpu.Disassemble((ushort)addr, out length);
			return ret;
		}
	}
}
