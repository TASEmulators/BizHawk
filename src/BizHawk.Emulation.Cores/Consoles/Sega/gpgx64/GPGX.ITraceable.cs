using System.Text;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX
	{
		private readonly ITraceable _tracer;

		public class GPGXTraceBuffer : CallbackBasedTraceBuffer
		{
			public GPGXTraceBuffer(IDebuggable debuggableCore, IMemoryDomains memoryDomains, IDisassemblable disassembler)
				: base(debuggableCore, memoryDomains, disassembler)
			{
				Header = "M68K: PC, machine code, mnemonic, operands, registers (D0-D7, A0-A7, SR, USP), flags (XNZVC)";
			}

			protected override void TraceFromCallback(uint addr, uint value, uint flags)
			{
				var regs = DebuggableCore.GetCpuFlagsAndRegisters();
				uint pc = (uint)regs["M68K PC"].Value;
				var disasm = Disassembler.Disassemble(MemoryDomains.SystemBus, pc & 0xFFFFFF, out _);

				var traceInfo = new TraceInfo
				{
					Disassembly = $"{pc:X6}:  {disasm}".PadRight(50)
				};

				var sb = new StringBuilder();

				foreach (var r in regs)
				{
					if (r.Key.StartsWith("M68K")) // drop Z80 regs until it has its own debugger/tracer
					{
						if (r.Key != "M68K SP" && r.Key != "M68K ISP" && // copies of a7
							r.Key != "M68K PC" && // already present in every line start
							r.Key != "M68K IR") // copy of last opcode, already shown in raw bytes
						{
							sb.Append($"{r.Key.Replace("M68K", "").Trim()}:{r.Value.Value.ToHexString(r.Value.BitSize / 4)} ");
						}
					}
				}
				var sr = regs["M68K SR"].Value;
				sb.Append(string.Concat(
					(sr & 16) > 0 ? "X" : "x",
					(sr &  8) > 0 ? "N" : "n",
					(sr &  4) > 0 ? "Z" : "z",
					(sr &  2) > 0 ? "V" : "v",
					(sr &  1) > 0 ? "C" : "c"));

				traceInfo.RegisterInfo = sb.ToString().Trim();

				Put(traceInfo);
			}
		}
	}
}
