using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.Z80GB;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public class GBDisassembler : VerifiedDisassembler
	{
		public override IEnumerable<string> AvailableCpus
		{
			get { yield return "Z80GB"; }
		}

		public override string PCRegisterName => "PC";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			ushort tmp;
			string ret = NewDisassembler.Disassemble((ushort)addr, a => m.PeekByte(a), out tmp);
			length = tmp;
			return ret;
		}
	}
}
