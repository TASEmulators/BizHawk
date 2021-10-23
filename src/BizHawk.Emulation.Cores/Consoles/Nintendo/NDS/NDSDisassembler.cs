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
			"ARM v5",
			"ARM v5 (Thumb)",
			"ARM v4",
			"ARM v4 (Thumb)",
		};

		public override string PCRegisterName => int.Parse(Cpu.Substring(5, 1)) == 5 ? "ARM9 r15" : "ARM7 r15";

		public override string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			if (m is NDS.NDSSystemBus NdsSysBus)
			{
				NdsSysBus.UseArm9 = int.Parse(Cpu.Substring(5, 1)) == 5;
				m = NdsSysBus;
			}

			if (Cpu.Length == 14)
			{
				addr &= ~1u;
				int op = m.PeekByte(addr) | m.PeekByte(addr + 1) << 8;
				string ret = _libdarm.DisassembleStuff(addr | 1, (uint)op);
				length = 2;
				return ret;
			}
			else
			{
				addr &= ~3u;
				int op = m.PeekByte(addr)
					| m.PeekByte(addr + 1) << 8
					| m.PeekByte(addr + 2) << 16
					| m.PeekByte(addr + 3) << 24;
				string ret = _libdarm.DisassembleStuff(addr, (uint)op);
				length = 4;
				return ret;
			}
		}

		public string Trace(uint pc, uint op, bool isthumb)
		{
			// easier to handle prefetch here
			if (isthumb)
			{
				pc -= 2;
				pc &= ~1u;
				return _libdarm.DisassembleStuff(pc | 1, op);
			}
			else
			{
				pc -= 4;
				pc &= ~3u;
				return _libdarm.DisassembleStuff(pc, op);
			}
		}
	}
}
