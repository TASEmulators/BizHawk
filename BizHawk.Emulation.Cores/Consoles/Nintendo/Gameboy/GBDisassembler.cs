using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public class GBDisassembler : VerifiedDisassembler
	{
		public override IEnumerable<string> AvailableCpus
		{
			get { yield return "Z80GB"; }
		}

		public override string PCRegisterName
		{
			get { return "PC"; }
		}

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			ushort tmp;
			string ret = Common.Components.Z80GB.NewDisassembler.Disassemble((ushort)addr, (a) => m.PeekByte(a), out tmp);
			length = tmp;
			return ret;
		}
	}
}
