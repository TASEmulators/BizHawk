using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = subnes.cpu.A,
				["X"] = subnes.cpu.X,
				["Y"] = subnes.cpu.Y,
				["S"] = subnes.cpu.S,
				["PC"] = subnes.cpu.PC,
				["Flag C"] = subnes.cpu.FlagC,
				["Flag Z"] = subnes.cpu.FlagZ,
				["Flag I"] = subnes.cpu.FlagI,
				["Flag D"] = subnes.cpu.FlagD,
				["Flag B"] = subnes.cpu.FlagB,
				["Flag V"] = subnes.cpu.FlagV,
				["Flag N"] = subnes.cpu.FlagN,
				["Flag T"] = subnes.cpu.FlagT
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					subnes.cpu.A = (byte)value;
					break;
				case "X":
					subnes.cpu.X = (byte)value;
					break;
				case "Y":
					subnes.cpu.Y = (byte)value;
					break;
				case "S":
					subnes.cpu.S = (byte)value;
					break;
				case "PC":
					subnes.cpu.PC = (ushort)value;
					break;
				case "Flag I":
					subnes.cpu.FlagI = value > 0;
					break;
			}
		}

		public bool CanStep(StepType type)
		{
			return false;
		}

		public IMemoryCallbackSystem MemoryCallbacks => subnes.MemoryCallbacks;

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public long TotalExecutedCycles => subnes.cpu.TotalExecutedCycles;
	}
}
