using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause
	{
		public TraceBuffer Tracer { get; private set; }

		public static string TraceHeader = "SH2: core, PC, machine code, mnemonic, operands, registers (GPRs, PR, SR, MAC, GBR, VBR)";

		LibYabause.TraceCallback trace_cb;

		public void YabauseTraceCallback(string dis, string regs)
		{
			Tracer.Put(new TraceInfo
			{
				Disassembly = dis,
				RegisterInfo = regs
			});
		}

		private void ConnectTracer()
		{
			trace_cb = new LibYabause.TraceCallback(YabauseTraceCallback);
			Tracer = new TraceBuffer() { Header = TraceHeader };
			ServiceProvider = new BasicServiceProvider(this);
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
		}
	}
}
