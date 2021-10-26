using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS : IDebuggable
	{
		[FeatureNotImplemented]
		public IMemoryCallbackSystem MemoryCallbacks => throw new NotImplementedException(); // https://github.com/TASEmulators/BizHawk/issues/2585

		public long TotalExecutedCycles => CycleCount;

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			uint[] regs = new uint[2 * 16];
			_core.GetRegs(regs);

			var ret = new Dictionary<string, RegisterValue>();
			for (int i = 0; i < 2; i++)
			{
				int ncpu = i == 0 ? 9 : 7;
				for (int j = 0; j < 16; j++)
				{
					ret["ARM" + ncpu + " r" + j] = regs[i * 16 + j];
				}
			}
			return ret;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.Length != 7 && register.Length != 8)
			{
				throw new InvalidOperationException("Wrong String Length???");
			}
			int ncpu = int.Parse(register.Substring(3, 1));
			if (ncpu != 9 && ncpu != 7)
			{
				throw new InvalidOperationException("Invalid CPU???");
			}
			int index = int.Parse(register.Substring(6, register.Length - 6));
			if (index < 0 || index > 15)
			{
				throw new InvalidOperationException("Invalid Reg Index???");
			}
			_core.SetReg(ncpu == 9 ? 0 : 1, index, value);
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();
	}
}
