using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public class Ares64Disassembler : VerifiedDisassembler
	{
		private readonly LibAres64 _core;
		private readonly byte[] _disasmbuf = new byte[100]; // todo: is this big enough?

		public Ares64Disassembler(LibAres64 core)
		{
			_core = core;
		}

		public override IEnumerable<string> AvailableCpus => new[] { "R4300", };

		public override string PCRegisterName => "PC";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			_core.GetDisassembly(addr, m.PeekUint(addr, true), _disasmbuf);
			length = 4;
			var ret = Encoding.UTF8.GetString(_disasmbuf);
			var z = ret.IndexOf('\0');
			if (z > -1)
			{
				ret = ret.Substring(0, z); // remove garbage past null terminator
			}
			ret = Regex.Replace(ret, @"\u001b?\[[0-9]{1,2}m", ""); // remove ANSI escape sequences
			ret = Regex.Replace(ret, @"\{.*\}", ""); // remove any {*} patterns
			return ret;
		}
	}
}
