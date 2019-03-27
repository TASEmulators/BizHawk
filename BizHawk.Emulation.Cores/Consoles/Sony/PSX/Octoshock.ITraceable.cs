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
			Tracer.Put(new TraceInfo
			{
				Disassembly = $"{PC:X8}:  {inst:X8}  {dis.PadRight(30)}",
				RegisterInfo = string.Join(" ", GetCpuFlagsAndRegisters().Where(r => r.Key != "pc")
					.Select(r => $"{r.Key}:{r.Value.Value.ToHexString(r.Value.BitSize / 4)}"))
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
