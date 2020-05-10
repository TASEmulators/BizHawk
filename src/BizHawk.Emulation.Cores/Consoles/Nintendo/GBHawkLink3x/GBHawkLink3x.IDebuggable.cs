using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x
{
	public partial class GBHawkLink3x : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var left = L.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("Left " + reg.Key, reg.Value));

			var center = C.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("Center " + reg.Key, reg.Value));

			var right = R.GetCpuFlagsAndRegisters()
				.Select(reg => new KeyValuePair<string, RegisterValue>("Right " + reg.Key, reg.Value));

			return left.Union(center).Union(right).ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.StartsWith("Left "))
			{
				L.SetCpuRegister(register.Replace("Left ", ""), value);
			}
			else if (register.StartsWith("Center "))
			{
				C.SetCpuRegister(register.Replace("Center ", ""), value);
			}
			else if (register.StartsWith("Right "))
			{
				R.SetCpuRegister(register.Replace("Right ", ""), value);
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => (long)L.cpu.TotalExecutedCycles;
	}
}
