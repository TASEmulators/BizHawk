using System;

namespace BizHawk.Emulation.Cores.Components.MC6800
{
	public partial class MC6800
	{
		// registers
		public ushort[] Regs = new ushort[11];

		public const ushort PC = 0;
		public const ushort SP = 1;
		public const ushort X = 2;
		public const ushort A = 3;
		public const ushort B = 4;
		public const ushort ADDR = 5; // internal
		public const ushort ALU = 6; // internal
		public const ushort ALU2 = 7; // internal
		public const ushort DP = 8; // always zero
		public const ushort CC = 9;
		public const ushort IDX_EA = 10;

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

		private void ResetRegisters()
		{
			for (int i = 0; i < 14; i++)
			{
				Regs[i] = 0;
			}

			FlagI = true;
		}
	}
}