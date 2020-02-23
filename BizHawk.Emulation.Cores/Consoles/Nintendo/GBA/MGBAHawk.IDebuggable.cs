using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var values = new int[RegisterNames.Length];
			LibmGBA.BizGetRegisters(_core, values);
			var ret = new Dictionary<string, RegisterValue>();
			for (var i = 0; i < RegisterNames.Length; i++)
			{
				ret[RegisterNames[i]] = new RegisterValue(values[i]);
			}

			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		[FeatureNotImplemented]
		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MGBAMemoryCallbackSystem();

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		private static readonly string[] RegisterNames =
		{
			"R0",
			"R1",
			"R2",
			"R3",
			"R4",
			"R5",
			"R6",
			"R7",
			"R8",
			"R9",
			"R10",
			"R11",
			"R12",
			"R13",
			"R14",
			"R15",
			"CPSR",
			"SPSR"
		};
	}
}
