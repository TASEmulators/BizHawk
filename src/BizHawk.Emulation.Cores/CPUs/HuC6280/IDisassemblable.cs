using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.H6280
{
	partial class HuC6280 : IDisassemblable
	{
		public string Cpu
		{
			get => "6280";
			set { }
		}

		public string PCRegisterName => "PC";

		public IEnumerable<string> AvailableCpus { get; } = [ "6280" ];

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			return DisassembleExt((ushort)addr, out length,
				a => m.PeekByte(a),
				a => (ushort)(m.PeekByte(a) | m.PeekByte(a + 1) << 8));
		}
	}
}
