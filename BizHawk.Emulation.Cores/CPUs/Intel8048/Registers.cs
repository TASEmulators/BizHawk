using System;

namespace BizHawk.Emulation.Common.Components.I8048
{
	public partial class I8048
	{
		// registers
		public ushort[] Regs = new ushort[21];

		// 64 bytes of onboard ram
		public ushort[] RAM = new ushort[64];

		// The 8048 has 2 flags that can be used for conditionals
		// F0 is on the PSW, F1 is seperate
		public bool F1;

		// The 8048 has 2 test lines which can be used for conditionals, T0 can be used as an output
		public bool T0, T1;

		public const ushort PC = 0;
		public const ushort PSW = 1;
		public const ushort BUS = 2;
		public const ushort A = 3;
		public const ushort R0 = 4;
		public const ushort R1 = 5;
		public const ushort R2 = 6;
		public const ushort R3 = 7;
		public const ushort R4 = 8;
		public const ushort R5 = 9;
		public const ushort R6 = 10;
		public const ushort R7 = 11;
		public const ushort ADDR = 12; // internal
		public const ushort ALU = 13; // internal
		public const ushort ALU2 = 14; // internal
		public const ushort P1 = 15;
		public const ushort P2 = 16;
		public const ushort P4 = 17;
		public const ushort P5 = 18;
		public const ushort P6 = 19;
		public const ushort P7 = 20;

		public bool Flag3
		{
			get { return (Regs[PSW] & 0x08) != 0; }
			set { Regs[PSW] = (byte)((Regs[PSW] & ~0x08) | 0x08); }
		}

		public bool FlagBS
		{
			get { return (Regs[PSW] & 0x10) != 0; }
			set { Regs[PSW] = (byte)((Regs[PSW] & ~0x10) | (value ? 0x10 : 0x00)); }
		}

		public bool FlagF0
		{
			get { return (Regs[PSW] & 0x20) != 0; }
			set { Regs[PSW] = (byte)((Regs[PSW] & ~0x20) | (value ? 0x20 : 0x00)); }
		}

		public bool FlagAC
		{
			get { return (Regs[PSW] & 0x40) != 0; }
			set { Regs[PSW] = (byte)((Regs[PSW] & ~0x40) | (value ? 0x40 : 0x00)); }
		}

		public bool FlagC
		{
			get { return (Regs[PSW] & 0x80) != 0; }
			set { Regs[PSW] = (byte)((Regs[PSW] & ~0x80) | (value ? 0x80 : 0x00)); }
		}

		private void ResetRegisters()
		{
			for (int i = 0; i < 21; i++)
			{
				Regs[i] = 0;
			}

			F1 = false;

			T0 = T1 = false;

			Flag3 = true;
		}
	}
}