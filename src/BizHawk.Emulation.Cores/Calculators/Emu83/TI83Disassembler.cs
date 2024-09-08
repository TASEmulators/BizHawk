using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public class TI83Disassembler : VerifiedDisassembler
	{
		public override IEnumerable<string> AvailableCpus { get; } = new[] { "Z80" };

		public override string PCRegisterName => "PC";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			var ret = Z80ADisassembler.Disassemble((ushort) addr, a => m.PeekByte(a), out var tmp);
			length = tmp;
			return ret;
		}
	}
}
