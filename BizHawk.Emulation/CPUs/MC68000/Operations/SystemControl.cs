using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		private void ANDI_to_CCR()
		{
			Helpers.Inject(ref this.m_SR, (byte)(this.m_SR & FetchW()));
			this.m_Cycles += 20;
		}

		private void ANDI_to_SR()
		{
			if (this.S)
			{
				this.m_SR &= FetchW();

				// Might not be in supervisor mode any more...
				if (!this.S)
				{
					this.m_Ssp = this.SP;
					this.SP = this.m_Usp;
				}

				this.m_Cycles += 20;
			}
			else
			{
				// TODO - cycle counter
				Trap(8);
			}
		}

		private void CHK()
		{
			int registerToCheck = (this.m_IR >> 9) & 0x7;
			int size = (this.m_IR >> 7) & 0x3;
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			// Only word size is legal on the 68000

			if ((short)this.m_D[registerToCheck] < 0)
			{
				this.N = true;
				Trap(6);
			}
			else if ((short)this.m_D[registerToCheck] > FetchOperandW(mode, size))
			{
				this.N = false;
				Trap(6);
			}
			else
			{
				this.m_Cycles += 10 + Helpers.EACalcTimeBW(mode, register);
			}
		}

		private void EORI_to_CCR()
		{
			Helpers.Inject(ref this.m_SR, (byte)(this.m_SR ^ FetchW()));
			this.m_Cycles += 20;
		}

		private void EORI_to_SR()
		{
			if (this.S)
			{
				this.m_SR ^= FetchW();

				// Might not be in supervisor mode any more...
				if (!this.S)
				{
					this.m_Ssp = this.SP;
					this.SP = this.m_Usp;
				}

				this.m_Cycles += 20;
			}
			else
			{
				// TODO - cycle counter
				Trap(8);
			}
		}

		private void ILLEGAL()
		{
			Trap(4);
		}

		private void MOVE_from_SR()
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			SetOperandW(mode, register, (short)this.m_SR);
			this.m_Cycles += (mode == 0) ? 6 : 8 + Helpers.EACalcTimeBW(mode, register);
		}

		private void MOVE_to_CCR()
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			this.m_SR &= 0xFF00;
			this.m_SR |= (ushort)(FetchOperandW(mode, register) & 0x00FF);
			this.m_Cycles += (mode == 0) ? 12 : 12 + Helpers.EACalcTimeBW(mode, register);
		}

		private void MOVE_to_SR() // Move to the Status Register
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			if (this.S)
			{
				this.m_SR = (ushort)FetchOperandW(mode, register);
				this.m_Cycles += (mode == 0) ? 12 : 12 + Helpers.EACalcTimeBW(mode, register);

				// Might not be in supervisor mode now...
				if (!this.S)
				{
					this.m_Ssp = this.SP;
					this.SP = this.m_Usp;
				}
			}
			else
			{
				Trap(8);
			}
		}

		private void MOVE_USP() // Move User Stack Pointer
		{
			if (this.S)
			{
				if (((this.m_IR >> 3) & 0x1) == 0)
				{
					this.m_Usp = this.m_A[this.m_IR & 0x7];
				}
				else
				{
					this.m_A[this.m_IR & 0x7] = this.m_Usp;
				}

				this.m_Cycles += 4;
			}
			else
			{
				// TODO Cycles
				Trap(8);
			}
		}

		private void ORI_to_CCR()
		{
			Helpers.Inject(ref this.m_SR, (byte)(this.m_SR | (ushort)FetchW()));
			this.m_Cycles += 20;
		}

		private void ORI_to_SR()
		{
			if (this.S)
			{
				this.m_SR |= (ushort)FetchW();

				// Might not be in supervisor mode any more...
				if (!this.S)
				{
					this.m_Ssp = this.SP;
					this.SP = this.m_Usp;
				}

				this.m_Cycles += 20;
			}
			else
			{
				// TODO - TRAP, cycle counter
				throw new NotImplementedException();
			}
		}

		private void RESET()
		{
			this.m_Cycles += 132;
			throw new NotImplementedException();
		}

		private void RTE()
		{
			if (this.S)
			{
				this.m_SR = (ushort)ReadW(this.SP);
				this.SP += 2;
				this.m_PC = ReadL(this.SP);
				this.SP += 4;

				// Might not be in supervisor mode any more...
				if (!this.S)
				{
					this.m_Ssp = this.SP;
					this.SP = this.m_Usp;
				}

				this.m_Cycles += 20;
			}
			else
			{
				// Privilege exception
				Trap(8);
			}
		}

		private void RTR()
		{
			// Seems a bit like RTE, but only affects condition codes
			this.m_SR = (ushort)((0x00FF & ReadW(this.m_Ssp)) | (0xFF00 & this.m_SR));
			this.SP += 2;
			this.m_PC = ReadL(this.SP);
			this.SP += 4;
			this.m_Cycles += 20;
		}

		private void STOP()
		{
			this.m_Cycles += 4;
			throw new NotImplementedException();
		}

		private void Trap(int trapVector)
		{
			// Make sure we're in supervisor mode
			if (!this.S)
			{
				this.m_Usp = this.SP;
				this.SP = this.m_Ssp;
			}

			// Add stack frame
			this.SP -= 4;
			WriteL(this.SP, this.m_PC);
			this.SP -= 2;
			WriteW(this.SP, (short)this.m_SR);

			// Enter supervisor mode
			this.S = true;

			// Get vector address from ROM header
			this.m_PC = ReadL(trapVector * 4);
		}

		private void TRAP()
		{
			int trapVector = (this.m_IR & 0x000F);
			Trap(trapVector + 32);
		}

		private void TRAPV()
		{
			this.m_Cycles += 4;
			if (this.V)
			{
				Trap(7);
			}
		}
	}
}
