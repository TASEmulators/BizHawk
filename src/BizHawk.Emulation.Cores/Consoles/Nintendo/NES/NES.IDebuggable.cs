using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters() => cpu.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value) => cpu.SetCpuRegister(register, value);

		public bool CanStep(StepType type) => false;

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => cpu.TotalExecutedCycles;
	}
}
