using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M68000;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class JaguarDisassembler : VerifiedDisassembler
	{
		private readonly MC68000 _disassembler = new();

		public override string PCRegisterName => "PC";

		public override IEnumerable<string> AvailableCpus => new[] { "M68000" };

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			_disassembler.ReadByte = a => (sbyte)m.PeekByte(a);
			_disassembler.ReadWord = a => (short)m.PeekUshort(a, true);
			_disassembler.ReadLong = a => (int)m.PeekUint(a, true);
			var info = _disassembler.Disassemble((int)addr);
			length = info.Length;
			return $"{info.RawBytes.Substring(0, 4):X4}  {info.Mnemonic,-7} {info.Args}";
		}
	}
}
