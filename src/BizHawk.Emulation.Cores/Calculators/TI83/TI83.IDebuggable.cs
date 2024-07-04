using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.TI83
{
	public partial class TI83 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters() => _cpu.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value) => _cpu.SetCpuRegister(register, value);

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public bool CanStep(StepType type) => false;

		public long TotalExecutedCycles => _cpu.TotalExecutedCycles;
	}
}
