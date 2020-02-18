using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

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
			string ret = LR35902.Disassemble((ushort)addr, a => m.PeekByte(a), out var tmp);
			length = tmp;
			return ret;
		}
	}
}
