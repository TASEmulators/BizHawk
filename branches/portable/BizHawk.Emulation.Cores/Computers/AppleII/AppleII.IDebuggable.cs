using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IDebuggable
	{
		[FeatureNotImplemented]
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var regs = _machine.GetCpuFlagsAndRegisters();

			var dic = new Dictionary<string, RegisterValue>();

			foreach (var reg in regs)
			{
				dic.Add(
					reg.Key,
					reg.Key.Contains("Flag")
						? reg.Value > 0
						: (RegisterValue)reg.Value);
			}

			return dic;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}
	}
}
