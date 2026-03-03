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
			var pc = regs["R15"].Value - wordSize;
			var sb = new StringBuilder();

			foreach (var registerName in RegisterNames)
			{
				sb.Append($" {registerName}:{regs[registerName].Value:X8}");
			}

			sb.Append($" Cy:{TotalExecutedCycles}");

			Tracer.Put(new(
				disassembly: $"{pc:X8}: { machineCode }  { instruction }".PadRight(54),
				registerInfo: sb.ToString()));
		}
	}
}
