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
			string disasm = msg.Split('|')[1];
			string[] split = disasm.Split(':');
			string machineCode = split[0].PadLeft(8);
			string instruction = split[1].Trim();
			var regs = GetCpuFlagsAndRegisters();
			ulong wordSize = (regs["CPSR"].Value & 32) == 0 ? 4UL : 2UL;
			ulong pc = regs["R15"].Value - wordSize * 2;
			StringBuilder sb = new();

			for (int i = 0; i < RegisterNames.Length; i++)
			{
				sb.Append($" { RegisterNames[i] }:{ regs[RegisterNames[i]].Value:X8}");
			}

			Tracer.Put(new(
				disassembly: $"{pc:X8}: { machineCode }  { instruction }".PadRight(50),
				registerInfo: sb.ToString()));
		}
	}
}
