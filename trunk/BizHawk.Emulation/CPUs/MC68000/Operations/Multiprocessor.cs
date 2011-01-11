using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public partial class MC68K
	{
		private void TAS() // Test and set
		{
			int mode = (this.m_IR >> 3) & 0x7;
			int register = this.m_IR & 0x7;

			sbyte operand = PeekOperandB(mode, register);

			this.N = (operand < 0);
			this.Z = (operand == 0);
			this.V = false;
			this.C = false;

			this.m_Cycles += (mode == 0) ? 4 : 14 + Helpers.EACalcTimeBW(mode, register);

			// Set the 7th bit
			byte uOperand = (byte)operand;
			uOperand |= 0x80;
			SetOperandB(mode, register, (sbyte)uOperand);
		}
	}
}
