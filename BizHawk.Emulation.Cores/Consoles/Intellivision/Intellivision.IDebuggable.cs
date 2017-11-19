using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["R0"] = _cpu.Register[0],
				["R1"] = _cpu.Register[1],
				["R2"] = _cpu.Register[2],
				["R3"] = _cpu.Register[3],
				["R4"] = _cpu.Register[4],
				["R5"] = _cpu.Register[5],
				["R6"] = _cpu.Register[6],
				["PC"] = _cpu.Register[7],

				["FlagS"] = _cpu.FlagS,
				["FlagC"] = _cpu.FlagC,
				["FlagZ"] = _cpu.FlagZ,
				["FlagO"] = _cpu.FlagO,
				["FlagI"] = _cpu.FlagI,
				["FlagD"] = _cpu.FlagD
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();

				case "R0":
					_cpu.Register[0] = (ushort)value;
					break;
				case "R1":
					_cpu.Register[1] = (ushort)value;
					break;
				case "R2":
					_cpu.Register[2] = (ushort)value;
					break;
				case "R3":
					_cpu.Register[3] = (ushort)value;
					break;
				case "R4":
					_cpu.Register[4] = (ushort)value;
					break;
				case "R5":
					_cpu.Register[5] = (ushort)value;
					break;
				case "R6":
					_cpu.Register[6] = (ushort)value;
					break;
				case "PC":
					_cpu.Register[7] = (ushort)value;
					break;

				case "FlagS":
					_cpu.FlagS = value > 0;
					break;
				case "FlagC":
					_cpu.FlagC = value > 0;
					break;
				case "FlagZ":
					_cpu.FlagZ = value > 0;
					break;
				case "FlagO":
					_cpu.FlagO = value > 0;
					break;
				case "FlagI":
					_cpu.FlagI = value > 0;
					break;
				case "FlagD":
					_cpu.FlagD = value > 0;
					break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type)
		{
			return false;
		}

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}

		public int TotalExecutedCycles => _cpu.TotalExecutedCycles;
	}
}
