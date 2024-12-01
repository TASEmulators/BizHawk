using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		// registers

		public const ushort PCl = 0;
		public const ushort PCh = 1;
		public const ushort SPl = 2;
		public const ushort SPh = 3;
		public const ushort A = 4;
		public const ushort F = 5;
		public const ushort B = 6;
		public const ushort C = 7;
		public const ushort D = 8;
		public const ushort E = 9;
		public const ushort H = 10;
		public const ushort L = 11;
		public const ushort W = 12;
		public const ushort Z = 13;
		public const ushort Aim = 14; // use this indicator for RLCA etc., since the Z flag is reset on those

		public ushort[] Regs = new ushort[14];

		public bool was_FlagI, FlagI;

		public bool FlagC
		{
			get => (Regs[5] & 0x10) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x10) | (value ? 0x10 : 0x00));
		}

		public bool FlagH
		{
			get => (Regs[5] & 0x20) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x20) | (value ? 0x20 : 0x00));
		}

		public bool FlagN
		{
			get => (Regs[5] & 0x40) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x40) | (value ? 0x40 : 0x00));
		}

		public bool FlagZ
		{
			get => (Regs[5] & 0x80) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x80) | (value ? 0x80 : 0x00));
		}

		public ushort RegPC
		{
			get => (ushort)(Regs[0] | (Regs[1] << 8));
			set
			{
				Regs[0] = (ushort)(value & 0xFF);
				Regs[1] = (ushort)((value >> 8) & 0xFF);
			}
		}

		private void ResetRegisters()
		{
			for (int i=0; i < 14; i++)
			{
				Regs[i] = 0;
			}
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				[nameof(PCl)] = Regs[PCl],
				[nameof(PCh)] = Regs[PCh],
				[nameof(SPl)] = Regs[SPl],
				[nameof(SPh)] = Regs[SPh],
				[nameof(A)] = Regs[A],
				[nameof(F)] = Regs[F],
				[nameof(B)] = Regs[B],
				[nameof(C)] = Regs[C],
				[nameof(D)] = Regs[D],
				[nameof(E)] = Regs[E],
				[nameof(H)] = Regs[H],
				[nameof(L)] = Regs[L],
				[nameof(W)] = Regs[W],
				[nameof(Z)] = Regs[Z],
				["PC"] = RegPC,
				["Flag I"] = FlagI,
				["Flag C"] = FlagC,
				["Flag H"] = FlagH,
				["Flag N"] = FlagN,
				["Flag Z"] = FlagZ
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case nameof(PCl):
					Regs[PCl] = (ushort)value;
					break;
				case nameof(PCh):
					Regs[PCh] = (ushort)value;
					break;
				case nameof(SPl):
					Regs[SPl] = (ushort)value;
					break;
				case nameof(SPh):
					Regs[SPh] = (ushort)value;
					break;
				case nameof(A):
					Regs[A] = (ushort)value;
					break;
				case nameof(F):
					Regs[F] = (ushort)value;
					break;
				case nameof(B):
					Regs[B] = (ushort)value;
					break;
				case nameof(C):
					Regs[C] = (ushort)value;
					break;
				case nameof(D):
					Regs[D] = (ushort)value;
					break;
				case nameof(E):
					Regs[E] = (ushort)value;
					break;
				case nameof(H):
					Regs[H] = (ushort)value;
					break;
				case nameof(L):
					Regs[L] = (ushort)value;
					break;
				case nameof(W):
					Regs[W] = (ushort)value;
					break;
				case nameof(Z):
					Regs[Z] = (ushort)value;
					break;
				case "PC":
					RegPC = (ushort) value;
					break;
			}
		}
	}
}
