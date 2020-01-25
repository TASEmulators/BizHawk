using System;

namespace BizHawk.Emulation.Cores.Components.I8048
{
	public partial class I8048
	{
		// registers
		public ushort[] Regs = new ushort[78];

		// EA gets set to true on external memory address latch
		public bool EA;
		
		// The 8048 has 2 flags that can be used for conditionals
		// F0 is on the PSW, F1 is seperate
		public bool F1;

		// The timer flag is set if the timer overflows, testing it resets it to zero
		public bool TF;
		public bool timer_en;
		public bool counter_en;
		public int timer_prescale;

		// The 8048 has 2 test lines which can be used for conditionals, T0 can be used as an output
		public bool T0, T1, T1_old;

		// 8 'registers' but really they point to locations in RAM
		public const ushort R0 = 0;
		public const ushort R1 = 1;
		public const ushort R2 = 2;
		public const ushort R3 = 3;
		public const ushort R4 = 4;
		public const ushort R5 = 5;
		public const ushort R6 = 6;
		public const ushort R7 = 7;

		// offset for port regs
		public const ushort PX = 70;

		// the location pointed to by the registers is controlled by the RAM bank
		public ushort RB = 0;

		// high PC address bit is controlled by instruction bank
		// only changes on JMP and CALL instructions
		public ushort MB = 0;

		//RAM occupies registers 0-63
		public const ushort PC = 64;
		public const ushort PSW = 65;
		public const ushort A = 66;
		public const ushort ADDR = 67; // internal
		public const ushort ALU = 68; // internal
		public const ushort ALU2 = 69; // internal
		public const ushort BUS = 70;
		public const ushort P1 = 71;
		public const ushort P2 = 72;
		public const ushort P4 = 73;
		public const ushort P5 = 74;
		public const ushort P6 = 75;
		public const ushort P7 = 76;
		public const ushort TIM = 77;

		public bool Flag3
		{
			get => (Regs[PSW] & 0x08) != 0;
			set => Regs[PSW] = (byte)((Regs[PSW] & ~0x08) | 0x08);
		}

		public bool FlagBS
		{
			get => (Regs[PSW] & 0x10) != 0;
			set
			{
				// change register bank also
				Regs[PSW] = (byte)((Regs[PSW] & ~0x10) | (value ? 0x10 : 0x00));
				if (value & 0x10 > 0)
				{
					RB = 24;
				}
				else
				{
					RB = 0;
				}

			}
		}

		public bool FlagF0
		{
			get => (Regs[PSW] & 0x20) != 0;
			set => Regs[PSW] = (byte)((Regs[PSW] & ~0x20) | (value ? 0x20 : 0x00));
		}

		public bool FlagAC
		{
			get => (Regs[PSW] & 0x40) != 0;
			set => Regs[PSW] = (byte)((Regs[PSW] & ~0x40) | (value ? 0x40 : 0x00));
		}

		public bool FlagC
		{
			get => (Regs[PSW] & 0x80) != 0;
			set => Regs[PSW] = (byte)((Regs[PSW] & ~0x80) | (value ? 0x80 : 0x00));
		}

		private void ResetRegisters()
		{
			for (int i = 0; i < 78; i++)
			{
				Regs[i] = 0;
			}

			F1 = false;

			T0 = T1 = false;

			Flag3 = true;
		}
	}
}