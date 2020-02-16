using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters() => subnes.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value) => subnes.SetCpuRegister(register, value);

		public bool CanStep(StepType type) => false;

		public IMemoryCallbackSystem MemoryCallbacks => subnes.MemoryCallbacks;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => subnes.cpu.TotalExecutedCycles;
	}
}
