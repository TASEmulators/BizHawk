using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IDebuggable
	{
		IDictionary<string, RegisterValue> dummyGetCPUflags() { return new Dictionary<string, RegisterValue>(); }
		void setCPURegister(string register, int value) { }

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters() => dummyGetCPUflags();

		public void SetCpuRegister(string register, int value) => setCPURegister(register, value);

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;
		
		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => 0;
	}
}
