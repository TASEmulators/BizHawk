using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk
	{
		private ITraceable Tracer { get; }

		private readonly LibmGBA.TraceCallback _tracecb;

		private void MakeTrace(string msg)
		{
			var disasm = msg.Split('|')[1];
			var split = disasm.Split(':');
			var machineCode = split[0].PadLeft(8);
			var instruction = split[1].Trim();
			var regs = GetCpuFlagsAndRegisters();
			var wordSize = (regs["CPSR"].Value & 32) == 0 ? 4UL : 2UL;
			var pc = regs["R15"].Value - wordSize * 2;
			var sb = new StringBuilder();

			for (var i = 0; i < RegisterNames.Length; i++)
			{
				sb.Append($" { RegisterNames[i] }:{ regs[RegisterNames[i]].Value:X8}");
			}

			Tracer.Put(new(
				disassembly: $"{pc:X8}: { machineCode }  { instruction }".PadRight(50),
				registerInfo: sb.ToString()));
		}
	}
}
