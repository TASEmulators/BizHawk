using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		private void Btst(int mode, int register, int mask)
		{
			switch (mode)
			{
				case 0: // Destination is data register
					{
						int operand = FetchOperandL(mode, register);
						this.Z = ((operand & mask) == 0);
						return;
					}
				default:
					{
						byte operand = (byte)FetchOperandB(mode, register);
						this.Z = ((operand & mask) == 0);
						return;
					}
			}
		}

		private void BTST_Dynamic()
		{
			int bitNumberRegister = (this.m_IR >> 9) & 0x7;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = this.m_D[bitNumberRegister];
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Btst(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 6 : 4 + Helpers.EACalcTimeBW(mode, register);
		}

		private void BTST_Static()
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = (byte)(FetchW() & 0x00FF);
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Btst(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 10 : 8 + Helpers.EACalcTimeBW(mode, register);
		}

		private void Bset(int mode, int register, int mask)
		{
			switch (mode)
			{
				case 0: // Destination is data register
					{
						int operand = PeekOperandL(mode, register);
						this.Z = ((operand & mask) == 0);
						SetOperandL(mode, register, (operand | mask));
						return;
					}
				default:
					{
						byte operand = (byte)PeekOperandB(mode, register);
						this.Z = ((operand & mask) == 0);
						SetOperandB(mode, register, (sbyte)(operand | mask));
						return;
					}
			}
		}

		private void BSET_Dynamic()
		{
			int bitNumberRegister = (this.m_IR >> 9) & 0x7;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = this.m_D[bitNumberRegister];
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Bset(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 8 : 8 + Helpers.EACalcTimeBW(mode, register);
		}

		private void BSET_Static()
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = (byte)(FetchW() & 0x00FF);
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Bset(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 12 : 12 + Helpers.EACalcTimeBW(mode, register);
		}

		private void Bclr(int mode, int register, int mask)
		{
			switch (mode)
			{
				case 0: // Destination is data register
					{
						int operand = PeekOperandL(mode, register);
						this.Z = ((operand & mask) > 0);
						SetOperandL(mode, register, (operand & ~mask));
						return;
					}
				default:
					{
						byte operand = (byte)PeekOperandB(mode, register);
						this.Z = ((operand & mask) > 0);
						SetOperandB(mode, register, (sbyte)(operand & ~mask));
						return;
					}
			}
		}

		private void BCLR_Dynamic()
		{
			int bitNumberRegister = (this.m_IR >> 9) & 0x7;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = this.m_D[bitNumberRegister];
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Bclr(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 10 : 8 + Helpers.EACalcTimeBW(mode, register);
		}

		private void BCLR_Static()
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = (byte)(FetchW() & 0x00FF);
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Bclr(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 14 : 12 + Helpers.EACalcTimeBW(mode, register);
		}

		private void Bchg(int mode, int register, int mask)
		{
			switch (mode)
			{
				case 0: // Destination is data register
					{
						int operand = PeekOperandL(mode, register);
						this.Z = ((operand & mask) > 0);
						SetOperandL(mode, register, (operand ^ mask));
						return;
					}
				default:
					{
						byte operand = (byte)PeekOperandB(mode, register);
						this.Z = ((operand & mask) > 0);
						SetOperandB(mode, register, (sbyte)(operand ^ mask));
						return;
					}
			}
		}

		private void BCHG_Dynamic()
		{
			int bitNumberRegister = (this.m_IR >> 9) & 0x7;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = this.m_D[bitNumberRegister];
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Bchg(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 8 : 8 + Helpers.EACalcTimeBW(mode, register);
		}

		private void BCHG_Static()
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Need to convert bit number into a mask
			int bitNumber = (byte)(FetchW() & 0x00FF);
			bitNumber %= (mode == 0) ? 32 : 8;
			int mask = 1 << bitNumber;

			Bchg(mode, register, mask);
			this.m_Cycles += (mode == 0) ? 12 : 12 + Helpers.EACalcTimeBW(mode, register);
		}
	}
}
