using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public partial class Octoshock
	{
		public TraceBuffer Tracer { get; private set; }

		public static string TraceHeader = "R3000A: PC, machine code, mnemonic, operands, registers (GPRs, lo, hi, sr, cause, epc)";

		OctoshockDll.ShockCallback_Trace trace_cb;

		public void ShockTraceCallback(IntPtr opaque, uint PC, uint inst, string dis)
		{
			var regs = GetCpuFlagsAndRegisters();
			StringBuilder sb = new StringBuilder();

			foreach (var r in regs)
			{
				if (r.Key != "pc")
					sb.Append(
						string.Format("{0}:{1} ",
						r.Key,
						r.Value.Value.ToHexString(r.Value.BitSize / 4)));
			}

			Tracer.Put(new TraceInfo
			{
				Disassembly = string.Format("{0:X8}:  {1:X8}  {2}", PC, inst, dis.PadRight(30)),
				RegisterInfo = sb.ToString().Trim()
			});
		}

		private void ConnectTracer()
		{
			trace_cb = new OctoshockDll.ShockCallback_Trace(ShockTraceCallback);
			Tracer = new TraceBuffer() { Header = TraceHeader };
			ServiceProvider = new BasicServiceProvider(this);
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
		}
	}
}
