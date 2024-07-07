using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters() => Cpu.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value) => Cpu.SetCpuRegister(register, value);

		public bool CanStep(StepType type) => false;

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => Cpu.TotalExecutedCycles;
	}
}
