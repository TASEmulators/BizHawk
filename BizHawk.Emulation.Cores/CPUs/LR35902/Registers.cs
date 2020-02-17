namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		// registers

		public static ushort PCl = 0;
		public static ushort PCh = 1;
		public static ushort SPl = 2;
		public static ushort SPh = 3;
		public static ushort A = 4;
		public static ushort F = 5;
		public static ushort B = 6;
		public static ushort C = 7;
		public static ushort D = 8;
		public static ushort E = 9;
		public static ushort H = 10;
		public static ushort L = 11;
		public static ushort W = 12;
		public static ushort Z = 13;
		public static ushort Aim = 14; // use this indicator for RLCA etc., since the Z flag is reset on those

		public ushort[] Regs = new ushort[14];

		public bool FlagI;

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

	}
}