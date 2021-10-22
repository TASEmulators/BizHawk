using System.Collections.Generic;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.ARM;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public class NDSDisassembler : VerifiedDisassembler
	{
		private readonly Darm _libdarm = BizInvoker.GetInvoker<Darm>(
			new DynamicLibraryImportResolver(OSTailoredCode.IsUnixHost ? "libdarm.so" : "libdarm.dll", hasLimitedLifetime: false),
			CallingConventionAdapters.Native
		);

		public override IEnumerable<string> AvailableCpus => new[]
		{
			"ARM v4",
			"ARM v4 (Thumb)",
			"ARM v5",
			"ARM v5 (Thumb)",
		};

		public override string PCRegisterName => int.Parse(Cpu.Substring(5, 1)) == 4 ? "ARM7 r15" : "ARM9 r15";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			NDS.NDSSystemBus.UseArm9 = int.Parse(Cpu.Substring(5, 1)) == 5;
			if (Cpu.Length == 14)
			{
				addr &= unchecked((uint)~1);
				int op = m.PeekByte((int)addr) | m.PeekByte((int)addr + 1) << 8;
				string ret = _libdarm.DisassembleStuff(addr | 1, (uint)op);
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
				string ret = _libdarm.DisassembleStuff(addr, (uint)op);
				length = 4;
				return ret;
			}
		}
	}
}
