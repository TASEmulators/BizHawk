using System.Text;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX
	{
		private readonly ITraceable _tracer;

		public class GPGXTraceBuffer(
			IDebuggable debuggableCore,
			IMemoryDomains memoryDomains,
			IDisassemblable disassembler)
			: CallbackBasedTraceBuffer(debuggableCore, memoryDomains, disassembler, TRACE_HEADER)
		{
			private const string TRACE_HEADER = "M68K: PC, machine code, mnemonic, operands, registers (A0-A7, D0-D7, SR, USP), flags (XNZVC)";

			protected override void TraceFromCallback(uint addr, uint value, uint flags)
			{
				var regs = DebuggableCore.GetCpuFlagsAndRegisters();
				var pc = (uint)regs["M68K PC"].Value & 0xFFFFFF;
				var disasm = Disassembler.Disassemble(MemoryDomains.SystemBus, pc, out _);

				var sb = new StringBuilder();

				foreach (var r in regs)
				{
					if (r.Key.StartsWithOrdinal("M68K")) // drop Z80 regs until it has its own debugger/tracer
					{
						if (r.Key is not ("M68K SP" or "M68K ISP" // copies of a7
							or "M68K PC" // already present in every line start
							or "M68K IR")) // copy of last opcode, already shown in raw bytes
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

				this.Put(new(disassembly: $"{pc:X6}:  {disasm}".PadRight(50), registerInfo: sb.ToString().Trim()));
			}
		}
	}
}
