namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A<TLink>
	{
		// registers
		public ushort PCl = 0;
		public ushort PCh = 1;
		public ushort SPl = 2;
		public ushort SPh = 3;
		public ushort A = 4;
		public ushort F = 5;
		public ushort B = 6;
		public ushort C = 7;
		public ushort D = 8;
		public ushort E = 9;
		public ushort H = 10;
		public ushort L = 11;
		public ushort W = 12;
		public ushort Z = 13;
		public ushort Aim = 14; // use this indicator for RLCA etc., since the Z flag is reset on those
		public ushort Ixl = 15;
		public ushort Ixh = 16;
		public ushort Iyl = 17;
		public ushort Iyh = 18;
		public ushort Int = 19;
		public ushort R = 20;
		public ushort I = 21;
		public ushort ZERO = 22; // it is convenient to have a register that is always zero, to reuse instructions
		public ushort ALU = 23; // This will be temporary arthimatic storage
		// shadow registers
		public ushort A_s = 24;
		public ushort F_s = 25;
		public ushort B_s = 26;
		public ushort C_s = 27;
		public ushort D_s = 28;
		public ushort E_s = 29;
		public ushort H_s = 30;
		public ushort L_s = 31;
		public ushort DB = 32;
		public ushort scratch = 33;
		public ushort IRQ_V = 34; // IRQ mode 1 vector
		public ushort NMI_V = 35; // NMI vector

		public ushort[] Regs = new ushort[36];

		// IO Contention Constants. Need to distinguish port access and normal memory accesses for zx spectrum
		public const ushort BIO1 = 100;
		public const ushort BIO2 = 101;
		public const ushort BIO3 = 102;
		public const ushort BIO4 = 103;

		public const ushort WIO1 = 105;
		public const ushort WIO2 = 106;
		public const ushort WIO3 = 107;
		public const ushort WIO4 = 108;


		public bool FlagI;

		public bool FlagW; // wait flag, when set to true reads / writes will be delayed

		public bool FlagC
		{
			get => (Regs[5] & 0x01) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x01) | (value ? 0x01 : 0x00));
		}

		public bool FlagN
		{
			get => (Regs[5] & 0x02) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x02) | (value ? 0x02 : 0x00));
		}

		public bool FlagP
		{
			get => (Regs[5] & 0x04) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x04) | (value ? 0x04 : 0x00));
		}

		public bool Flag3
		{
			get => (Regs[5] & 0x08) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x08) | (value ? 0x08 : 0x00));
		}

		public bool FlagH
		{
			get => (Regs[5] & 0x10) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x10) | (value ? 0x10 : 0x00));
		}

		public bool Flag5
		{
			get => (Regs[5] & 0x20) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x20) | (value ? 0x20 : 0x00));
		}

		public bool FlagZ
		{
			get => (Regs[5] & 0x40) != 0;
			set => Regs[5] = (ushort)((Regs[5] & ~0x40) | (value ? 0x40 : 0x00));
		}

		public bool FlagS
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
			for (int i=0; i < 36; i++)
			{
				Regs[i] = 0;
			}

			// the IRQ1 vector is 0x38
			Regs[IRQ_V] = 0x38;
			// The NMI vector is constant 0x66
			Regs[NMI_V] = 0x66;

			FlagI = false;
			FlagW = false;
		}

		private bool[] TableParity;
		private void InitTableParity()
		{
			TableParity = new bool[256];
			for (int i = 0; i < 256; ++i)
			{
				int Bits = 0;
				for (int j = 0; j < 8; ++j)
				{
					Bits += (i >> j) & 1;
				}
				TableParity[i] = (Bits & 1) == 0;
			}
		}
	}
}