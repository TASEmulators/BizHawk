using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = cpu.A,
				["X"] = cpu.X,
				["Y"] = cpu.Y,
				["S"] = cpu.S,
				["PC"] = cpu.PC,
				["Flag C"] = cpu.FlagC,
				["Flag Z"] = cpu.FlagZ,
				["Flag I"] = cpu.FlagI,
				["Flag D"] = cpu.FlagD,
				["Flag B"] = cpu.FlagB,
				["Flag V"] = cpu.FlagV,
				["Flag N"] = cpu.FlagN,
				["Flag T"] = cpu.FlagT
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					cpu.A = (byte)value;
					break;
				case "X":
					cpu.X = (byte)value;
					break;
				case "Y":
					cpu.Y = (byte)value;
					break;
				case "S":
					cpu.S = (byte)value;
					break;
				case "PC":
					cpu.PC = (ushort)value;
					break;
				case "Flag I":
					cpu.FlagI = value > 0;
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

		public long TotalExecutedCycles => cpu.TotalExecutedCycles;
	}
}
