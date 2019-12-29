using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.I8048;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["R0"] = cpu.Regs[0 + cpu.RB],
				["R1"] = cpu.Regs[1 + cpu.RB],
				["R2"] = cpu.Regs[2 + cpu.RB],
				["R3"] = cpu.Regs[3 + cpu.RB],
				["R4"] = cpu.Regs[4 + cpu.RB],
				["R5"] = cpu.Regs[5 + cpu.RB],
				["R6"] = cpu.Regs[6 + cpu.RB],
				["R7"] = cpu.Regs[7 + cpu.RB],
				["PC"] = cpu.Regs[I8048.PC],
				["Flag C"] = cpu.FlagC,
				["Flag AC"] = cpu.FlagAC,
				["Flag BS"] = cpu.FlagBS,
				["Flag F0"] = cpu.FlagF0,
				["Flag F1"] = cpu.F1,
				["Flag T0"] = cpu.T0,
				["Flag T1"] = cpu.T1
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "R0":
					cpu.Regs[0 + cpu.RB] = (byte)value;
					break;
				case "R1":
					cpu.Regs[1 + cpu.RB] = (byte)value;
					break;
				case "R2":
					cpu.Regs[2 + cpu.RB] = (byte)value;
					break; ;
				case "R3":
					cpu.Regs[3 + cpu.RB] = (byte)value;
					break;
				case "R4":
					cpu.Regs[4 + cpu.RB] = (byte)value;
					break;
				case "R5":
					cpu.Regs[5 + cpu.RB] = (byte)value;
					break;
				case "R6":
					cpu.Regs[6 + cpu.RB] = (byte)value;
					break; ;
				case "R7":
					cpu.Regs[7 + cpu.RB] = (byte)value;
					break;
				case "PC":
					cpu.Regs[I8048.PC] = (ushort)value;
					break;
				case "Flag C":
					cpu.FlagC = value > 0;
					break;
				case "Flag AC":
					cpu.FlagAC = value > 0;
					break;
				case "Flag BS":
					cpu.FlagBS = value > 0;
					break;
				case "Flag F0":
					cpu.FlagF0 = value > 0;
					break;
				case "Flag F1":
					cpu.F1 = value > 0;
					break;
				case "Flag T0":
					cpu.T0 = value > 0;
					break;
				case "Flag T1":
					cpu.T1 = value > 0;
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

		public long TotalExecutedCycles
		{
			get { return (long)cpu.TotalExecutedCycles; }
		}
	}
}
