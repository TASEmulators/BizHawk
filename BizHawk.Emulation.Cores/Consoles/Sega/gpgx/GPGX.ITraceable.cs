using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Text;

using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX
	{
		public TraceBuffer Tracer { get; private set; }

		private LibGPGX.trace_cb _tracecb;

		private const string TraceHeader = "M68K: PC, machine code, mnemonic, operands, registers (D0-D7, A0-A7, SR, USP), flags (XNZVC)";

		private void MakeTrace()
		{
			var regs = GetCpuFlagsAndRegisters();
			uint pc = (uint)regs["M68K PC"].Value;
			var length = 0;
			var disasm = Disassemble(MemoryDomains.SystemBus, pc, out length).PadRight(40);

			var traceInfo = new TraceInfo
			{
				Disassembly = string.Format("{0:X6}:  {1}", pc, disasm)
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
						sb.Append(
							string.Format("{0}:{1} ",
							r.Key.Replace("M68K", string.Empty).Trim(),
							r.Value.Value.ToHexString(r.Value.BitSize / 4)));
					}
				}
			}
			var sr = regs["M68K SR"].Value;
			sb.Append(
				string.Format("{0}{1}{2}{3}{4}",
				(sr & 16) > 0 ? "X" : "x",
				(sr &  8) > 0 ? "N" : "n",
				(sr &  4) > 0 ? "Z" : "z",
				(sr &  2) > 0 ? "V" : "v",
				(sr &  1) > 0 ? "C" : "c"));

			traceInfo.RegisterInfo = sb.ToString().Trim();

			Tracer.Put(traceInfo);
		}

		private void ConnectTracer()
		{
			Tracer = new TraceBuffer { Header = TraceHeader };
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
			_tracecb = new LibGPGX.trace_cb(MakeTrace);
		}
	}
}
