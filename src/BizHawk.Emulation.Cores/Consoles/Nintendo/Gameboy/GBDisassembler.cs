using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public class GBDisassembler : VerifiedDisassembler
	{
		public bool UseRGBDSSyntax;

		public override IEnumerable<string> AvailableCpus { get; } = new[] { "LR35902" };

		public override string PCRegisterName => "PC";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			var ret = LR35902.Disassemble((ushort) addr, a => m.PeekByte(a), UseRGBDSSyntax, out var tmp);
			length = tmp;
			return ret;
		}
	}
}
