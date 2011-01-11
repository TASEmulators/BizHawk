using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		private void Bcc() // Branch conditionally
		{
			if ((sbyte)this.m_IR == 0)
			{
				if (TestCondition((this.m_IR >> 8) & 0xF))
				{
					this.m_PC += FetchW();
					this.m_Cycles += 10;
				}
				else
				{
					this.m_PC += 2;
					this.m_Cycles += 12;
				}
			}
			else
			{
				if (TestCondition((this.m_IR >> 8) & 0xF))
				{
					this.m_PC += (sbyte)this.m_IR;
					this.m_Cycles += 10;
				}
				else
				{
					this.m_Cycles += 8;
				}
			}
		}

		private void DBcc() // Test condition, decrement, branch
		{
			if (TestCondition((this.m_IR >> 8) & 0xF))
			{
				// Need to move PC on...
				this.m_PC += 2;
				this.m_Cycles += 12;
			}
			else
			{
				short counter = (short)this.m_D[this.m_IR & 0x7];
				Helpers.Inject(ref this.m_D[this.m_IR & 0x7], --counter);

				if (counter == -1)
				{
					this.m_PC += 2;
					this.m_Cycles += 14;
				}
				else
				{
					this.m_PC += FetchW();
					this.m_Cycles += 10;
				}
			}
		}

		private void Scc() // Set according to condition
		{
			int cCode = (this.m_IR >> 8) & 0xF;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			if (TestCondition(cCode))
			{
				// Set all the bits
				SetOperandB(mode, register, -1);
				this.m_Cycles += (mode == 0) ? 6 : 8 + Helpers.EACalcTimeBW(mode, register);
			}
			else
			{
				// Clear all the bits
				SetOperandB(mode, register, 0);
				this.m_Cycles += (mode == 0) ? 4 : 8 + Helpers.EACalcTimeBW(mode, register);
			}
		}

		private void BRA() // Branch Always
		{
			this.m_Cycles += 10;

			if ((sbyte)this.m_IR == 0)
			{
				this.m_PC += PeekW();
			}
			else
			{
				this.m_PC += (sbyte)this.m_IR;
			}
		}

		private void BSR() // Branch to subroutine
		{
			this.SP -= 4;

			// 16-bit displacement
			if ((sbyte)this.m_IR == 0)
			{
				WriteL(this.SP, this.m_PC + 2);
				this.m_PC += PeekW();
			}

			// 8-bit displacement
			else
			{
				WriteL(this.SP, this.m_PC);
				this.m_PC += (sbyte)this.m_IR;
			}

			this.m_Cycles += 18;
		}

		private void JMP() // Jump
		{
			this.m_PC = FetchAddress((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);

			switch ((this.m_IR >> 3) & 0x7)
			{
				case 0x2: this.m_Cycles += 8; break;
				case 0x5: this.m_Cycles += 10; break;
				case 0x6: this.m_Cycles += 14; break;
				case 0x7:
					switch (this.m_IR & 0x7)
					{
						case 0x0: this.m_Cycles += 10; break;
						case 0x1: this.m_Cycles += 12; break;
						case 0x2: this.m_Cycles += 10; break;
						case 0x3: this.m_Cycles += 14; break;
					}
					break;
			}
		}

		private void JSR() // Jump to subroutine
		{
			int address = FetchAddress((this.m_IR >> 3) & 0x7, this.m_IR & 0x7) & 0x00FFFFFF;
			this.SP -= 4;
			WriteL(this.SP, this.m_PC);
			this.m_PC = address;

			switch ((this.m_IR >> 3) & 0x7)
			{
				case 0x2: this.m_Cycles += 16; break;
				case 0x5: this.m_Cycles += 18; break;
				case 0x6: this.m_Cycles += 22; break;
				case 0x7:
					switch (this.m_IR & 0x7)
					{
						case 0x0: this.m_Cycles += 18; break;
						case 0x1: this.m_Cycles += 20; break;
						case 0x2: this.m_Cycles += 18; break;
						case 0x3: this.m_Cycles += 22; break;
					}
					break;
			}
		}

		private void NOP() // No operation
		{
			// Doesn't do anything, it's there to help flush the integer pipeline
			this.m_Cycles += 4;
		}

		private void RTS() // Return from Subroutine
		{
			this.m_PC = ReadL(this.SP);
			this.SP += 4;
			this.m_Cycles += 16;
		}

		private void TST() // Test an operand
		{
			// Use an integer operand, it gets sign-extended and we can check it afterwards
			int operand = 0;
			switch ((this.m_IR >> 6) & 0x3)
			{
				case 0: // B
					{
						operand = FetchOperandB((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);
						break;
					}
				case 1: // W
					{
						operand = FetchOperandW((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);
						this.m_Cycles += 4 + Helpers.EACalcTimeBW((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);
						break;
					}
				case 2: // L
					{
						operand = FetchOperandL((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);
						this.m_Cycles += 4 + Helpers.EACalcTimeL((this.m_IR >> 3) & 0x7, this.m_IR & 0x7);
						break;
					}
			}

			this.V = this.C = false;
			this.N = (operand < 0);
			this.Z = (operand == 0);
		}
	}
}