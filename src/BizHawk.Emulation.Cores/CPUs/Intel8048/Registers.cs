using System.Collections.Generic;
using BizHawk.Emulation.Common;

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

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["R0"] = Regs[0 + RB],
				["R1"] = Regs[1 + RB],
				["R2"] = Regs[2 + RB],
				["R3"] = Regs[3 + RB],
				["R4"] = Regs[4 + RB],
				["R5"] = Regs[5 + RB],
				["R6"] = Regs[6 + RB],
				["R7"] = Regs[7 + RB],
				["PC"] = Regs[PC],
				["Flag C"] = FlagC,
				["Flag AC"] = FlagAC,
				["Flag BS"] = FlagBS,
				["Flag F0"] = FlagF0,
				["Flag F1"] = F1,
				["Flag T0"] = T0,
				["Flag T1"] = T1
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "R0":
					Regs[0 + RB] = (byte)value;
					break;
				case "R1":
					Regs[1 + RB] = (byte)value;
					break;
				case "R2":
					Regs[2 + RB] = (byte)value;
					break;
				case "R3":
					Regs[3 + RB] = (byte)value;
					break;
				case "R4":
					Regs[4 + RB] = (byte)value;
					break;
				case "R5":
					Regs[5 + RB] = (byte)value;
					break;
				case "R6":
					Regs[6 + RB] = (byte)value;
					break;
				case "R7":
					Regs[7 + RB] = (byte)value;
					break;
				case "PC":
					Regs[PC] = (ushort)value;
					break;
				case "Flag C":
					FlagC = value > 0;
					break;
				case "Flag AC":
					FlagAC = value > 0;
					break;
				case "Flag BS":
					FlagBS = value > 0;
					break;
				case "Flag F0":
					FlagF0 = value > 0;
					break;
				case "Flag F1":
					F1 = value > 0;
					break;
				case "Flag T0":
					T0 = value > 0;
					break;
				case "Flag T1":
					T1 = value > 0;
					break;
			}
		}

		private void ResetRegisters()
		{
			for (int i = 0; i < 78; i++)
			{
				Regs[i] = 0;
			}

			F1 = false;

			T0 = T1 = T1_old = false;

			Flag3 = true;

			EA = false;

			TF = false;
			timer_en = false;
			counter_en = false;
			timer_prescale = 0;

			RB = MB = 0;
		}
	}
}