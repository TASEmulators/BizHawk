using System;

namespace BizHawk.Emulation.Cores.Components.MC6809
{
	public partial class MC6809
	{
		// registers
		public ushort[] Regs = new ushort[14];

		public const ushort PC = 0;
		public const ushort US = 1;
		public const ushort SP = 2;
		public const ushort X = 3;
		public const ushort Y = 4;
		public const ushort A = 5;
		public const ushort B = 6;
		public const ushort ADDR = 7; // internal
		public const ushort ALU = 8; // internal
		public const ushort ALU2 = 9; // internal
		public const ushort DP = 10;
		public const ushort CC = 11;
		public const ushort Dr = 12;
		public const ushort IDX_EA = 13;

		public ushort D
		{
			get { return (ushort)(Regs[B] | (Regs[A] << 8)); }
			set { Regs[B] = (ushort)(value & 0xFF); Regs[A] = (ushort)((value >> 8) & 0xFF); }
		}

		public bool FlagC
		{
			get { return (Regs[CC] & 0x01) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x01) | (value ? 0x01 : 0x00)); }
		}

		public bool FlagV
		{
			get { return (Regs[CC] & 0x02) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x02) | (value ? 0x02 : 0x00)); }
		}

		public bool FlagZ
		{
			get { return (Regs[CC] & 0x04) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x04) | (value ? 0x04 : 0x00)); }
		}

		public bool FlagN
		{
			get { return (Regs[CC] & 0x08) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x08) | (value ? 0x08 : 0x00)); }
		}

		public bool FlagI
		{
			get { return (Regs[CC] & 0x10) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x10) | (value ? 0x10 : 0x00)); }
		}

		public bool FlagH
		{
			get { return (Regs[CC] & 0x20) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x20) | (value ? 0x20 : 0x00)); }
		}

		public bool FlagF
		{
			get { return (Regs[CC] & 0x40) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x40) | (value ? 0x40 : 0x00)); }
		}

		public bool FlagE
		{
			get { return (Regs[CC] & 0x80) != 0; }
			set { Regs[CC] = (byte)((Regs[CC] & ~0x80) | (value ? 0x80 : 0x00)); }
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