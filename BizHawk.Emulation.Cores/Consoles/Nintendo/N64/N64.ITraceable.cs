using System;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64
	{
		private readonly ITraceable Tracer;

		public class m64pTraceBuffer : CallbackBasedTraceBuffer
		{
			public m64pTraceBuffer(IDebuggable debuggableCore, IMemoryDomains memoryDomains, IDisassemblable disassembler)
				: base(debuggableCore, memoryDomains, disassembler)
			{
				Header = "r3400: PC, mnemonic, arguments";
			}

			public override void TraceFromCallback()
			{
				var regs = DebuggableCore.GetCpuFlagsAndRegisters();
				uint pc = (uint)regs["PC"].Value;
				var length = 0;
				var disasm = Disassembler.Disassemble(MemoryDomains.SystemBus, pc, out length);

				var traceInfo = new TraceInfo
				{
					Disassembly = string.Format("{0:X}:  {1}", pc, disasm.PadRight(32))
				};

				var sb = new StringBuilder();

				for (int i = 1; i < 32; i++) // r0 is always zero
				{
					UInt64 val = (regs["REG" + i + "_hi"].Value << 32) | regs["REG" + i + "_lo"].Value;
					string name = GPRnames[i];
					sb.Append(string.Format("{0}:{1:X16} ", name, val));
				}

				sb.Append(string.Format("LL:{0:X8} ", regs["LL"].Value));
				sb.Append(string.Format("LO:{0:X8}{1:X8} ", regs["LO_hi"].Value, regs["LO_lo"].Value));
				sb.Append(string.Format("HI:{0:X8}{1:X8} ", regs["HI_hi"].Value, regs["HI_lo"].Value));
				sb.Append(string.Format("FCR0:{0:X8} ", regs["FCR0"].Value));
				sb.Append(string.Format("FCR31:{0:X8} ", regs["FCR31"].Value));
				// drop co-processor regs for now

				traceInfo.RegisterInfo = sb.ToString().Trim();

				Put(traceInfo);
			}

			private string[] GPRnames = new string[32]
			{
				"r0",
				"at",
				"v0", "v1",
				"a0", "a1", "a2", "a3",
				"t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
				"s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
				"t8", "t9",
				"k0", "k1",
				"gp",
				"sp",
				"s8",
				"ra"
			};
		}

	}
}