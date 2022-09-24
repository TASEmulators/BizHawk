using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class VirtualJaguar
	{
		private ITraceable Tracer { get; }
		private readonly LibVirtualJaguar.M68KTraceCallback _cpuTraceCallback;
		private readonly LibVirtualJaguar.RISCTraceCallback _gpuTraceCallback;
		private readonly LibVirtualJaguar.RISCTraceCallback _dspTraceCallback;

		private unsafe void MakeCPUTrace(IntPtr r)
		{
			uint* regs = (uint*)r;
			var pc = regs[16] & 0xFFFFFF;
			var disasm = _disassembler.DisassembleM68K(this.AsMemoryDomains().SystemBus, pc, out _);
			var regInfo = string.Empty;
			for (int i = 0; i < 8; i++)
			{
				regInfo += $"D{i}:{regs[i]:X8} ";
			}
			for (int i = 0; i < 8; i++)
			{
				regInfo += $"A{i}:{regs[i + 8]:X8} ";
			}
			regInfo += $"SR:{regs[17]:X8} ";
			var sr = regs[17];
			regInfo += string.Concat(
				(sr & 16) > 0 ? "X" : "x",
				(sr & 8) > 0 ? "N" : "n",
				(sr & 4) > 0 ? "Z" : "z",
				(sr & 2) > 0 ? "V" : "v",
				(sr & 1) > 0 ? "C" : "c");
			regInfo += " (M68K)";

			Tracer.Put(new(disassembly: $"{pc:X6}:  {disasm}".PadRight(30), registerInfo: regInfo));
		}

		private unsafe void MakeGPUTrace(uint pc, IntPtr r)
		{
			uint* regs = (uint*)r;
			pc &= 0xFFFFFF;
			var disasm = _disassembler.DisassembleRISC(true, this.AsMemoryDomains().SystemBus, pc, out _);
			var regInfo = string.Empty;
			for (int i = 0; i < 32; i++)
			{
				regInfo += $"r{i}:{regs[i]:X8} ";
			}
			regInfo += "(GPU)";

			Tracer.Put(new(disassembly: $"{pc:X6}:  {disasm}".PadRight(50), registerInfo: regInfo));
		}

		private unsafe void MakeDSPTrace(uint pc, IntPtr r)
		{
			uint* regs = (uint*)r;
			pc &= 0xFFFFFF;
			var disasm = _disassembler.DisassembleRISC(false, this.AsMemoryDomains().SystemBus, pc, out _);
			var regInfo = string.Empty;
			for (int i = 0; i < 32; i++)
			{
				regInfo += $"r{i}:{regs[i]:X8} ";
			}
			regInfo += "(DSP)";

			Tracer.Put(new(disassembly: $"{pc:X6}:  {disasm}".PadRight(50), registerInfo: regInfo));
		}
	}
}
