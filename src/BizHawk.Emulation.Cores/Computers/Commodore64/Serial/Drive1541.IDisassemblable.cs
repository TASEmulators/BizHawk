using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public sealed partial class Drive1541 : IDisassemblable
	{
		public IEnumerable<string> AvailableCpus { get; } = [ "Disk Drive 6502" ];

		string IDisassemblable.Cpu
		{
			get => "Disk Drive 6502";

			set
			{
			}
		}

		string IDisassemblable.PCRegisterName => "PC";

		string IDisassemblable.Disassemble(MemoryDomain m, uint addr, out int length)
		{
			return Components.M6502.MOS6502X.Disassemble((ushort)addr, out length, CpuPeek);
		}
	}
}
