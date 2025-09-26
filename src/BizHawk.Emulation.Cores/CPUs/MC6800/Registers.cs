namespace BizHawk.Emulation.Cores.Components.MC6800
{
	public partial class MC6800
	{
		// registers
		[CLSCompliant(false)]
		public ushort[] Regs = new ushort[11];

		[CLSCompliant(false)]
		public const ushort PC = 0;

		[CLSCompliant(false)]
		public const ushort SP = 1;

		[CLSCompliant(false)]
		public const ushort X = 2;

		[CLSCompliant(false)]
		public const ushort A = 3;

		[CLSCompliant(false)]
		public const ushort B = 4;

		[CLSCompliant(false)]
		public const ushort ADDR = 5; // internal

		[CLSCompliant(false)]
		public const ushort ALU = 6; // internal

		[CLSCompliant(false)]
		public const ushort ALU2 = 7; // internal

		[CLSCompliant(false)]
		public const ushort DP = 8; // always zero

		[CLSCompliant(false)]
		public const ushort CC = 9;

		[CLSCompliant(false)]
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