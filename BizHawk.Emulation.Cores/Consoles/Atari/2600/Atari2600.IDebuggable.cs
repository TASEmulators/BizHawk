using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = Cpu.A,
				["X"] = Cpu.X,
				["Y"] = Cpu.Y,
				["S"] = Cpu.S,
				["PC"] = Cpu.PC,

				["Flag C"] = Cpu.FlagC,
				["Flag Z"] = Cpu.FlagZ,
				["Flag I"] = Cpu.FlagI,
				["Flag D"] = Cpu.FlagD,

				["Flag B"] = Cpu.FlagB,
				["Flag V"] = Cpu.FlagV,
				["Flag N"] = Cpu.FlagN,
				["Flag T"] = Cpu.FlagT
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Cpu.A = (byte)value;
					break;
				case "X":
					Cpu.X = (byte)value;
					break;
				case "Y":
					Cpu.Y = (byte)value;
					break;
				case "S":
					Cpu.S = (byte)value;
					break;
				case "PC":
					Cpu.PC = (ushort)value;
					break;
				case "Flag I":
					Cpu.FlagI = value > 0;
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

		public long TotalExecutedCycles => Cpu.TotalExecutedCycles;
	}
}
