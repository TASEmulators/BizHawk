using System;
using System.Text;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	partial class N64
	{
		public TraceBuffer Tracer { get; private set; }

		private mupen64plusApi.TraceCallback _tracecb;

		public void MakeTrace()
		{
			var regs = GetCpuFlagsAndRegisters();
			uint pc = (uint)regs["PC"].Value;
			var length = 0;
			var disasm = Disassemble(MemoryDomains.SystemBus, pc, out length);

			var traceInfo = new TraceInfo
			{
				Disassembly = $"{pc:X}:  {disasm.PadRight(32)}"
			};

			var sb = new StringBuilder();

			for (int i = 1; i < 32; i++) // r0 is always zero
			{
				UInt64 val = (regs[GPRnames[i] + "_hi"].Value << 32) | regs[GPRnames[i] + "_lo"].Value;
				string name = GPRnames[i];
				sb.Append($"{name}:{val:X16} ");
			}

			sb.Append($"LL:{regs["LL"].Value:X8} ");
			sb.Append($"LO:{regs["LO_hi"].Value:X8}{regs["LO_lo"].Value:X8} ");
			sb.Append($"HI:{regs["HI_hi"].Value:X8}{regs["HI_lo"].Value:X8} ");
			sb.Append($"FCR0:{regs["FCR0"].Value:X8} ");
			sb.Append($"FCR31:{regs["FCR31"].Value:X8} ");

			for (int i = 0; i < 32; i++) // r0 is always zero
			{
				UInt64 val = (regs["CP1 FGR REG" + i + "_hi"].Value << 32) | regs["CP1 FGR REG" + i + "_lo"].Value;
				sb.Append($"f{i}:{val:X16} ");
			}

			// drop MMU co-processor regs for now

			traceInfo.RegisterInfo = sb.ToString().Trim();

			Tracer.Put(traceInfo);
		}

		private const string TraceHeader = "r3400: PC, mnemonic, operands, registers (GPRs, Load/Link Bit, MultHI, MultLO, Implementation/Revision, Control/Status, FGRs)";

		private void ConnectTracer()
		{
			Tracer = new TraceBuffer { Header = TraceHeader };
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
			_tracecb = new mupen64plusApi.TraceCallback(MakeTrace);
		}
	}
}