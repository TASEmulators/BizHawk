using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", Cpu.A },
				{ "X", Cpu.X },
				{ "Y", Cpu.Y },
				{ "PC", Cpu.PC },
				{ "S", Cpu.S },
				{ "MPR-0", Cpu.MPR[0] },
				{ "MPR-1", Cpu.MPR[1] },
				{ "MPR-2", Cpu.MPR[2] },
				{ "MPR-3", Cpu.MPR[3] },
				{ "MPR-4", Cpu.MPR[4] },
				{ "MPR-5", Cpu.MPR[5] },
				{ "MPR-6", Cpu.MPR[6] },
				{ "MPR-7", Cpu.MPR[7] }
			};
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public int TotalExecutedCycles
		{
			get { return (int)Cpu.TotalExecutedCycles; }
		}
	}
}
