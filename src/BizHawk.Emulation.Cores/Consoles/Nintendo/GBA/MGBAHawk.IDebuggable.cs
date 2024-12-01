using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var values = new int[RegisterNames.Length];
			LibmGBA.BizGetRegisters(Core, values);
			var ret = new Dictionary<string, RegisterValue>();
			for (var i = 0; i < RegisterNames.Length; i++)
			{
				ret[RegisterNames[i]] = new(values[i]);
			}

			return ret;
		}

		public void SetCpuRegister(string register, int value)
		{
			int index = register?.ToUpperInvariant() switch
			{
				"R0" => 0,
				"R1" => 1,
				"R2" => 2,
				"R3" => 3,
				"R4" => 4,
				"R5" => 5,
				"R6" => 6,
				"R7" => 7,
				"R8" => 8,
				"R9" => 9,
				"R10" => 10,
				"R11" => 11,
				"R12" => 12,
				"R13" => 13,
				"R14" => 14,
				"R15" => 15,
				"CPSR" => 16,
				"SPSR" => 17,
				_ => -1
			};

			if (index != -1)
			{
				LibmGBA.BizSetRegister(Core, index, value);
			}
		}

		private readonly MGBAMemoryCallbackSystem _memoryCallbacks;

		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => (long)LibmGBA.BizGetGlobalTime(Core);

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
