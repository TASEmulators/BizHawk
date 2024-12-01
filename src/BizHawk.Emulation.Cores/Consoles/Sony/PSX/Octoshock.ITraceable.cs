using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public partial class Octoshock
	{
		public TraceBuffer Tracer { get; private set; }

		public const string TraceHeader = "R3000A: PC, machine code, mnemonic, operands, registers (GPRs, lo, hi, sr, cause, epc)";

		private OctoshockDll.ShockCallback_Trace trace_cb;

		public void ShockTraceCallback(IntPtr opaque, uint PC, uint inst, string dis)
		{
			var regs = GetCpuFlagsAndRegisters();
			StringBuilder sb = new StringBuilder();

			foreach (var r in regs)
			{
				if (r.Key != "pc")
					sb.Append($"{r.Key}:{r.Value.Value.ToHexString(r.Value.BitSize / 4)} ");
			}

			Tracer.Put(new(disassembly: $"{PC:X8}:  {inst:X8}  {dis.PadRight(30)}", registerInfo: sb.ToString().Trim()));
		}

		private void ConnectTracer()
		{
			trace_cb = new OctoshockDll.ShockCallback_Trace(ShockTraceCallback);
			Tracer = new TraceBuffer(TraceHeader);
			ServiceProvider = new BasicServiceProvider(this);
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
		}
	}
}
