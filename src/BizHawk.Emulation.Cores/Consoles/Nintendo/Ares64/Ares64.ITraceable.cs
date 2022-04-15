using System;
using System.Text.RegularExpressions;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public partial class Ares64
	{
		private ITraceable Tracer { get; }
		private readonly LibAres64.TraceCallback _tracecb;

		private void MakeTrace(IntPtr disasm)
		{
			var regs = GetCpuFlagsAndRegisters();
			var regsStr = "";
			foreach (var r in regs)
			{
				if (r.Key is not "PC")
				{
					regsStr += r.Key + $":{r.Value.Value:X16} ";
				}
			}

			regsStr = regsStr.Remove(regsStr.Length - 1, 1);

			var disasmStr = Mershul.PtrToStringUtf8(disasm);
			disasmStr = disasmStr.Remove(0, 5); // remove "CPU  "
			disasmStr = disasmStr.Replace("\n", ""); // remove newlines
			disasmStr = Regex.Replace(disasmStr, @"\{.*\}", ""); // remove any {*} patterns

			Tracer.Put(new(
				disassembly: disasmStr,
				registerInfo: regsStr));
		}
	}
}
