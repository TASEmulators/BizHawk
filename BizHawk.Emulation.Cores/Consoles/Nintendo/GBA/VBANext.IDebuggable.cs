using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return regs.GetAllRegisters();
		}

		public void SetCpuRegister(string register, int value)
		{
			regs.SetRegister(register, value);
		}


		public bool CanStep(StepType type)
		{
			return false;
		}

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem();
		public IMemoryCallbackSystem MemoryCallbacks { get { return _memorycallbacks; } }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public int TotalExecutedCycles {  get { throw new NotImplementedException(); } }
	}
}
