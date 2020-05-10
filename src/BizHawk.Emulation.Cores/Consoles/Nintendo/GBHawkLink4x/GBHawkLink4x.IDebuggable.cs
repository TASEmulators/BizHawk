using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var a = A.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("A " + reg.Key, reg.Value));

			var b = B.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("B " + reg.Key, reg.Value));

			var c = C.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("C " + reg.Key, reg.Value));

			var d = D.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("D " + reg.Key, reg.Value));

			return a.Union(b).Union(c).Union(d).ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.StartsWith("A "))
			{
				A.SetCpuRegister(register.Replace("A ", ""), value);
			}
			else if (register.StartsWith("B "))
			{
				B.SetCpuRegister(register.Replace("B ", ""), value);
			}
			else if (register.StartsWith("C "))
			{
				C.SetCpuRegister(register.Replace("C ", ""), value);
			}
			else if (register.StartsWith("D "))
			{
				C.SetCpuRegister(register.Replace("D ", ""), value);
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => (long)A.cpu.TotalExecutedCycles;
	}
}
