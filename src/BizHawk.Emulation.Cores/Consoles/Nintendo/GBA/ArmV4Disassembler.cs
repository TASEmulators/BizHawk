using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.ARM;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public class ArmV4Disassembler : VerifiedDisassembler
	{
		public override IEnumerable<string> AvailableCpus => new[]
		{
			"ARM v4",
			"ARM v4 (Thumb)"
		};

		public override string PCRegisterName => "R15";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			if (Cpu == "ARM v4 (Thumb)")
			{
				addr &= unchecked((uint)~1);
				int op = m.PeekByte((int)addr) | m.PeekByte((int)addr + 1) << 8;
				string ret = Darm.DisassembleStuff(addr | 1, (uint)op);
				length = 2;
				return ret;
			}
			else
			{
				addr &= unchecked((uint)~3);
				int op = m.PeekByte((int)addr)
					| m.PeekByte((int)addr + 1) << 8
					| m.PeekByte((int)addr + 2) << 16
					| m.PeekByte((int)addr + 3) << 24;
				string ret = Darm.DisassembleStuff(addr, (uint)op);
				length = 4;
				return ret;
			}
		}
	}
}
