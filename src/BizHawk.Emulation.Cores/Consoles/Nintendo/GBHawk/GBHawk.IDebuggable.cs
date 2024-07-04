using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
			=> cpu.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value)
			=> cpu.SetCpuRegister(register, value);

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => _settings.cycle_return_setting == GBSettings.Cycle_Return.CPU ? (long)cpu.TotalExecutedCycles : CycleCount;
	}
}
