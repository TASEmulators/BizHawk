using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters() => regs.GetAllRegisters();

		public void SetCpuRegister(string register, int value) => regs.SetRegister(register, value);

		public bool CanStep(StepType type) => false;

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { "System Bus" });
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();
	}
}
