using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.MC6809;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				
				["A"] = cpu.Regs[MC6809.A],
				["B"] = cpu.Regs[MC6809.B],
				["X"] = cpu.Regs[MC6809.X],
				["Y"] = cpu.Regs[MC6809.Y],
				["US"] = cpu.Regs[MC6809.US],
				["SP"] = cpu.Regs[MC6809.SP],
				["PC"] = cpu.Regs[MC6809.PC],
				["Flag E"] = cpu.FlagE,
				["Flag F"] = cpu.FlagF,
				["Flag H"] = cpu.FlagH,
				["Flag I"] = cpu.FlagI,
				["Flag N"] = cpu.FlagN,
				["Flag Z"] = cpu.FlagZ,
				["Flag V"] = cpu.FlagV,
				["Flag C"] = cpu.FlagC
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					cpu.Regs[MC6809.A] = (byte)value;
					break;
				case "B":
					cpu.Regs[MC6809.B] = (byte)value;
					break;
				case "X":
					cpu.Regs[MC6809.X] = (byte)value;
					break;
				case "Y":
					cpu.Regs[MC6809.Y] = (ushort)value;
					break;
				case "US":
					cpu.Regs[MC6809.US] = (ushort)value;
					break;
				case "SP":
					cpu.Regs[MC6809.SP] = (ushort)value;
					break;
				case "PC":
					cpu.Regs[MC6809.PC] = (ushort)value;
					break;
				case "Flag E":
					cpu.FlagE = value > 0;
					break;
				case "Flag F":
					cpu.FlagF = value > 0;
					break;
				case "Flag H":
					cpu.FlagH = value > 0;
					break;
				case "Flag I":
					cpu.FlagI = value > 0;
					break;
				case "Flag N":
					cpu.FlagN = value > 0;
					break;
				case "Flag Z":
					cpu.FlagZ = value > 0;
					break;
				case "Flag V":
					cpu.FlagV = value > 0;
					break;
				case "Flag C":
					cpu.FlagC = value > 0;
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
