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
					Disassembly = string.Format("{0:X}:  {1}", pc, disasm),
					RegisterInfo = ""
				};

				Buffer.Add(traceInfo);
			}
		}

	}
}