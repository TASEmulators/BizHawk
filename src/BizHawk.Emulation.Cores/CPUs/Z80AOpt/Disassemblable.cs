using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A; // reuse the shared Z80ADisassembler tables

namespace BizHawk.Emulation.Cores.Components.Z80AOpt
{
	// IDisassemblable is implemented by delegating to the shared Z80ADisassembler
	// (the mnemonic tables live in the original Z80A core and are not perf-critical,
	// so they are reused rather than forked).
	public sealed partial class Z80AOpt<TLink> : IDisassemblable
	{
		public string Cpu
		{
			get => "Z80";
			set { }
		}

		public string PCRegisterName => "PC";

		public IEnumerable<string> AvailableCpus { get; } = [ "Z80" ];

		public string Disassemble(MemoryDomain m, uint addr, out int length)
			=> Z80ADisassembler.Disassemble((ushort)addr, a => m.PeekByte(a), out length);
	}
}
