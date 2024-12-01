using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = L.Cpu.Regs[L.Cpu.A],
				["AF"] = L.Cpu.Regs[L.Cpu.F] + (L.Cpu.Regs[L.Cpu.A] << 8),
				["B"] = L.Cpu.Regs[L.Cpu.B],
				["BC"] = L.Cpu.Regs[L.Cpu.C] + (L.Cpu.Regs[L.Cpu.B] << 8),
				["C"] = L.Cpu.Regs[L.Cpu.C],
				["D"] = L.Cpu.Regs[L.Cpu.D],
				["DE"] = L.Cpu.Regs[L.Cpu.E] + (L.Cpu.Regs[L.Cpu.D] << 8),
				["E"] = L.Cpu.Regs[L.Cpu.E],
				["F"] = L.Cpu.Regs[L.Cpu.F],
				["H"] = L.Cpu.Regs[L.Cpu.H],
				["HL"] = L.Cpu.Regs[L.Cpu.L] + (L.Cpu.Regs[L.Cpu.H] << 8),
				["I"] = L.Cpu.Regs[L.Cpu.I],
				["IX"] = L.Cpu.Regs[L.Cpu.Ixl] + (L.Cpu.Regs[L.Cpu.Ixh] << 8),
				["IY"] = L.Cpu.Regs[L.Cpu.Iyl] + (L.Cpu.Regs[L.Cpu.Iyh] << 8),
				["L"] = L.Cpu.Regs[L.Cpu.L],
				["PC"] = L.Cpu.Regs[L.Cpu.PCl] + (L.Cpu.Regs[L.Cpu.PCh] << 8),
				["R"] = L.Cpu.Regs[L.Cpu.R],
				["Shadow AF"] = L.Cpu.Regs[L.Cpu.F_s] + (L.Cpu.Regs[L.Cpu.A_s] << 8),
				["Shadow BC"] = L.Cpu.Regs[L.Cpu.C_s] + (L.Cpu.Regs[L.Cpu.B_s] << 8),
				["Shadow DE"] = L.Cpu.Regs[L.Cpu.E_s] + (L.Cpu.Regs[L.Cpu.D_s] << 8),
				["Shadow HL"] = L.Cpu.Regs[L.Cpu.L_s] + (L.Cpu.Regs[L.Cpu.H_s] << 8),
				["SP"] = L.Cpu.Regs[L.Cpu.SPl] + (L.Cpu.Regs[L.Cpu.SPh] << 8),
				["Flag C"] = L.Cpu.FlagC,
				["Flag N"] = L.Cpu.FlagN,
				["Flag P/V"] = L.Cpu.FlagP,
				["Flag 3rd"] = L.Cpu.Flag3,
				["Flag H"] = L.Cpu.FlagH,
				["Flag 5th"] = L.Cpu.Flag5,
				["Flag Z"] = L.Cpu.FlagZ,
				["Flag S"] = L.Cpu.FlagS
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					L.Cpu.Regs[L.Cpu.A] = (ushort)value;
					break;
				case "AF":
					L.Cpu.Regs[L.Cpu.F] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.A] = (ushort)(value & 0xFF00);
					break;
				case "B":
					L.Cpu.Regs[L.Cpu.B] = (ushort)value;
					break;
				case "BC":
					L.Cpu.Regs[L.Cpu.C] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.B] = (ushort)(value & 0xFF00);
					break;
				case "C":
					L.Cpu.Regs[L.Cpu.C] = (ushort)value;
					break;
				case "D":
					L.Cpu.Regs[L.Cpu.D] = (ushort)value;
					break;
				case "DE":
					L.Cpu.Regs[L.Cpu.E] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.D] = (ushort)(value & 0xFF00);
					break;
				case "E":
					L.Cpu.Regs[L.Cpu.E] = (ushort)value;
					break;
				case "F":
					L.Cpu.Regs[L.Cpu.F] = (ushort)value;
					break;
				case "H":
					L.Cpu.Regs[L.Cpu.H] = (ushort)value;
					break;
				case "HL":
					L.Cpu.Regs[L.Cpu.L] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.H] = (ushort)(value & 0xFF00);
					break;
				case "I":
					L.Cpu.Regs[L.Cpu.I] = (ushort)value;
					break;
				case "IX":
					L.Cpu.Regs[L.Cpu.Ixl] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.Ixh] = (ushort)(value & 0xFF00);
					break;
				case "IY":
					L.Cpu.Regs[L.Cpu.Iyl] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.Iyh] = (ushort)(value & 0xFF00);
					break;
				case "L":
					L.Cpu.Regs[L.Cpu.L] = (ushort)value;
					break;
				case "PC":
					L.Cpu.Regs[L.Cpu.PCl] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.PCh] = (ushort)(value & 0xFF00);
					break;
				case "R":
					L.Cpu.Regs[L.Cpu.R] = (ushort)value;
					break;
				case "Shadow AF":
					L.Cpu.Regs[L.Cpu.F_s] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.A_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow BC":
					L.Cpu.Regs[L.Cpu.C_s] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.B_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow DE":
					L.Cpu.Regs[L.Cpu.E_s] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.D_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow HL":
					L.Cpu.Regs[L.Cpu.L_s] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.H_s] = (ushort)(value & 0xFF00);
					break;
				case "SP":
					L.Cpu.Regs[L.Cpu.SPl] = (ushort)(value & 0xFF);
					L.Cpu.Regs[L.Cpu.SPh] = (ushort)(value & 0xFF00);
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

		public long TotalExecutedCycles => L.Cpu.TotalExecutedCycles;
	}
}
