using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.MC6809
{
	public partial class MC6809
	{
		// registers
		[CLSCompliant(false)]
		public ushort[] Regs = new ushort[14];

		[CLSCompliant(false)]
		public const ushort PC = 0;

		[CLSCompliant(false)]
		public const ushort US = 1;

		[CLSCompliant(false)]
		public const ushort SP = 2;

		[CLSCompliant(false)]
		public const ushort X = 3;

		[CLSCompliant(false)]
		public const ushort Y = 4;

		[CLSCompliant(false)]
		public const ushort A = 5;

		[CLSCompliant(false)]
		public const ushort B = 6;

		[CLSCompliant(false)]
		public const ushort ADDR = 7; // internal

		[CLSCompliant(false)]
		public const ushort ALU = 8; // internal

		[CLSCompliant(false)]
		public const ushort ALU2 = 9; // internal

		[CLSCompliant(false)]
		public const ushort DP = 10;

		[CLSCompliant(false)]
		public const ushort CC = 11;

		[CLSCompliant(false)]
		public const ushort Dr = 12;

		[CLSCompliant(false)]
		public const ushort IDX_EA = 13;

		[CLSCompliant(false)]
		public ushort D
		{
			get => (ushort)(Regs[B] | (Regs[A] << 8));
			set { Regs[B] = (ushort)(value & 0xFF); Regs[A] = (ushort)((value >> 8) & 0xFF); }
		}

		public bool FlagC
		{
			get => (Regs[CC] & 0x01) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x01) | (value ? 0x01 : 0x00));
		}

		public bool FlagV
		{
			get => (Regs[CC] & 0x02) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x02) | (value ? 0x02 : 0x00));
		}

		public bool FlagZ
		{
			get => (Regs[CC] & 0x04) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x04) | (value ? 0x04 : 0x00));
		}

		public bool FlagN
		{
			get => (Regs[CC] & 0x08) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x08) | (value ? 0x08 : 0x00));
		}

		public bool FlagI
		{
			get => (Regs[CC] & 0x10) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x10) | (value ? 0x10 : 0x00));
		}

		public bool FlagH
		{
			get => (Regs[CC] & 0x20) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x20) | (value ? 0x20 : 0x00));
		}

		public bool FlagF
		{
			get => (Regs[CC] & 0x40) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x40) | (value ? 0x40 : 0x00));
		}

		public bool FlagE
		{
			get => (Regs[CC] & 0x80) != 0;
			set => Regs[CC] = (byte)((Regs[CC] & ~0x80) | (value ? 0x80 : 0x00));
		}

		private void ResetRegisters()
		{
			for (int i = 0; i < 14; i++)
			{
				Regs[i] = 0;
			}

			FlagI = true;
		}

		[CLSCompliant(false)]
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = Regs[A],
				["B"] = Regs[B],
				["X"] = Regs[X],
				["Y"] = Regs[Y],
				["US"] = Regs[US],
				["SP"] = Regs[SP],
				["PC"] = Regs[PC],
				["Flag E"] = FlagE,
				["Flag F"] = FlagF,
				["Flag H"] = FlagH,
				["Flag I"] = FlagI,
				["Flag N"] = FlagN,
				["Flag Z"] = FlagZ,
				["Flag V"] = FlagV,
				["Flag C"] = FlagC
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Regs[A] = (byte)value;
					break;
				case "B":
					Regs[B] = (byte)value;
					break;
				case "X":
					Regs[X] = (byte)value;
					break;
				case "Y":
					Regs[Y] = (ushort)value;
					break;
				case "US":
					Regs[US] = (ushort)value;
					break;
				case "SP":
					Regs[SP] = (ushort)value;
					break;
				case "PC":
					Regs[PC] = (ushort)value;
					break;
				case "Flag E":
					FlagE = value > 0;
					break;
				case "Flag F":
					FlagF = value > 0;
					break;
				case "Flag H":
					FlagH = value > 0;
					break;
				case "Flag I":
					FlagI = value > 0;
					break;
				case "Flag N":
					FlagN = value > 0;
					break;
				case "Flag Z":
					FlagZ = value > 0;
					break;
				case "Flag V":
					FlagV = value > 0;
					break;
				case "Flag C":
					FlagC = value > 0;
					break;
			}
		}
	}
}