using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
			=> CPU.GetCpuFlagsAndRegisters();

		public void SetCpuRegister(string register, int value)
			=> CPU.SetCpuRegister(register, value);

		public IMemoryCallbackSystem MemoryCallbacks { get; }

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => CPU.TotalExecutedCycles;
	}
}
