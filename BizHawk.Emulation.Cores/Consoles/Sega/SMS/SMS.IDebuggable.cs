using System;
using System.Collections.Generic;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = Cpu.Regs[Cpu.A],
				["AF"] = Cpu.Regs[Cpu.F] + (Cpu.Regs[Cpu.A] << 8),
				["B"] = Cpu.Regs[Cpu.B],
				["BC"] = Cpu.Regs[Cpu.C] + (Cpu.Regs[Cpu.B] << 8),
				["C"] = Cpu.Regs[Cpu.C],
				["D"] = Cpu.Regs[Cpu.D],
				["DE"] = Cpu.Regs[Cpu.E] + (Cpu.Regs[Cpu.D] << 8),
				["E"] = Cpu.Regs[Cpu.E],
				["F"] = Cpu.Regs[Cpu.F],
				["H"] = Cpu.Regs[Cpu.H],
				["HL"] = Cpu.Regs[Cpu.L] + (Cpu.Regs[Cpu.H] << 8),
				["I"] = Cpu.Regs[Cpu.I],
				["IX"] = Cpu.Regs[Cpu.Ixl] + (Cpu.Regs[Cpu.Ixh] << 8),
				["IY"] = Cpu.Regs[Cpu.Iyl] + (Cpu.Regs[Cpu.Iyh] << 8),
				["L"] = Cpu.Regs[Cpu.L],
				["PC"] = Cpu.Regs[Cpu.PCl] + (Cpu.Regs[Cpu.PCh] << 8),
				["R"] = Cpu.Regs[Cpu.R],
				["Shadow AF"] = Cpu.Regs[Cpu.F_s] + (Cpu.Regs[Cpu.A_s] << 8),
				["Shadow BC"] = Cpu.Regs[Cpu.C_s] + (Cpu.Regs[Cpu.B_s] << 8),
				["Shadow DE"] = Cpu.Regs[Cpu.E_s] + (Cpu.Regs[Cpu.D_s] << 8),
				["Shadow HL"] = Cpu.Regs[Cpu.L_s] + (Cpu.Regs[Cpu.H_s] << 8),
				["SP"] = Cpu.Regs[Cpu.SPl] + (Cpu.Regs[Cpu.SPh] << 8),
				["Flag C"] = Cpu.FlagC,
				["Flag N"] = Cpu.FlagN,
				["Flag P/V"] = Cpu.FlagP,
				["Flag 3rd"] = Cpu.Flag3,
				["Flag H"] = Cpu.FlagH,
				["Flag 5th"] = Cpu.Flag5,
				["Flag Z"] = Cpu.FlagZ,
				["Flag S"] = Cpu.FlagS
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Cpu.Regs[Cpu.A] = (ushort)value;
					break;
				case "AF":
					Cpu.Regs[Cpu.F] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.A] = (ushort)(value & 0xFF00);
					break;
				case "B":
					Cpu.Regs[Cpu.B] = (ushort)value;
					break;
				case "BC":
					Cpu.Regs[Cpu.C] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.B] = (ushort)(value & 0xFF00);
					break;
				case "C":
					Cpu.Regs[Cpu.C] = (ushort)value;
					break;
				case "D":
					Cpu.Regs[Cpu.D] = (ushort)value;
					break;
				case "DE":
					Cpu.Regs[Cpu.E] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.D] = (ushort)(value & 0xFF00);
					break;
				case "E":
					Cpu.Regs[Cpu.E] = (ushort)value;
					break;
				case "F":
					Cpu.Regs[Cpu.F] = (ushort)value;
					break;
				case "H":
					Cpu.Regs[Cpu.H] = (ushort)value;
					break;
				case "HL":
					Cpu.Regs[Cpu.L] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.H] = (ushort)(value & 0xFF00);
					break;
				case "I":
					Cpu.Regs[Cpu.I] = (ushort)value;
					break;
				case "IX":
					Cpu.Regs[Cpu.Ixl] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.Ixh] = (ushort)(value & 0xFF00);
					break;
				case "IY":
					Cpu.Regs[Cpu.Iyl] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.Iyh] = (ushort)(value & 0xFF00);
					break;
				case "L":
					Cpu.Regs[Cpu.L] = (ushort)value;
					break;
				case "PC":
					Cpu.Regs[Cpu.PCl] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.PCh] = (ushort)(value & 0xFF00);
					break;
				case "R":
					Cpu.Regs[Cpu.R] = (ushort)value;
					break;
				case "Shadow AF":
					Cpu.Regs[Cpu.F_s] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.A_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow BC":
					Cpu.Regs[Cpu.C_s] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.B_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow DE":
					Cpu.Regs[Cpu.E_s] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.D_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow HL":
					Cpu.Regs[Cpu.L_s] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.H_s] = (ushort)(value & 0xFF00);
					break;
				case "SP":
					Cpu.Regs[Cpu.SPl] = (ushort)(value & 0xFF);
					Cpu.Regs[Cpu.SPh] = (ushort)(value & 0xFF00);
					break;
			}
		}

		public bool CanStep(StepType type) { return false; }

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}

		public long TotalExecutedCycles
		{
			get { return Cpu.TotalExecutedCycles; }
		}
	}
}
