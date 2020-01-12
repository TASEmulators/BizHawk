using System;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.MC6800
{
	public partial class MC6800
	{
		// this contains the vectors of instrcution operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		private void NOP_()
		{
			PopulateCURINSTR(IDLE);

			IRQS = 1;
		}

		private void ILLEGAL()
		{
			//throw new Exception("Encountered illegal instruction");
			PopulateCURINSTR(IDLE);

			IRQS = 1;
		}

		private void REG_OP(ushort oper, ushort src)
		{
			PopulateCURINSTR(oper, src);

			IRQS = 1;
		}

		private void REG_OP_16(ushort oper, ushort src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							oper, src);

			IRQS = 3;
		}

		private void DIRECT_MEM(ushort oper)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							SET_ADDR, ADDR, DP, ALU,
							RD, ALU, ADDR,
							oper, ALU,
							WR, ADDR, ALU);

			IRQS = 5;
		}

		private void DIRECT_ST_4(ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							IDLE,
							WR, ADDR, dest);

			IRQS = 3;
		}

		private void DIRECT_MEM_4(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							IDLE,			
							RD_INC_OP, ALU, ADDR, oper, dest, ALU);

			IRQS = 3;
		}

		private void EXT_MEM(ushort oper)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC, ALU2, PC,
							SET_ADDR, ADDR, ALU, ALU2,
							RD, ALU, ADDR,
							oper, ALU,
							WR, ADDR, ALU);

			IRQS = 6;
		}

		private void EXT_REG(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							RD, ALU, ADDR,
							oper, dest, ALU);

			IRQS = 4;
		}

		private void EXT_ST(ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							IDLE,
							WR, ADDR, dest);

			IRQS = 4;
		}

		private void REG_OP_IMD(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, oper, dest, ALU);

			IRQS = 1;
		}

		private void DIR_CMP_16(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							RD_INC, ALU, ADDR,
							RD, ALU2, ADDR,
							SET_ADDR, ADDR, ALU, ALU2,
							oper, dest, ADDR);

			IRQS = 5;
		}

		private void IMD_CMP_16(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							oper, dest, ADDR);

			IRQS = 3;
		}

		private void REG_OP_LD_16(ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, LD_16, dest, ALU, ALU2);

			IRQS = 2;
		}

		private void DIR_OP_LD_16(ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							IDLE,
							RD_INC, ALU, ADDR,
							RD_INC_OP, ALU2, ADDR, LD_16, dest, ALU, ALU2);

			IRQS = 4;
		}

		private void DIR_OP_ST_16(ushort src)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							IDLE,
							WR_HI_INC, ADDR, src, 
							WR_DEC_LO, ADDR, src);

			IRQS = 4;
		}

		private void EXT_OP_LD_16(ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							RD_INC, ALU, ADDR,
							RD_INC_OP, ALU2, ADDR, LD_16, dest, ALU, ALU2);

			IRQS = 4;
		}

		private void EXT_OP_ST_16(ushort src)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC, ALU2, PC, 
							SET_ADDR, ADDR, ALU, ALU2,
							WR_HI_INC, ADDR, src,
							WR_DEC_LO, ADDR, src);

			IRQS = 5;
		}

		private void EXT_CMP_16(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							RD_INC, ALU, ADDR,
							RD, ALU2, ADDR,
							SET_ADDR, ADDR, ALU, ALU2,
							oper, dest, ADDR);

			IRQS = 6;
		}

		private void JMP_EXT_()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC, ALU2, PC,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 3;
		}

		private void JSR_EXT()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC, ALU2, PC, 
							SET_ADDR, ADDR, ALU, ALU2,
							TR, ALU, PC,
							IDLE,
							TR, PC, ADDR,
							WR_DEC_LO, SP, ALU,
							WR_DEC_HI, SP, ALU);

			IRQS = 8;
		}

		private void BR_(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR(RD_INC, ALU, PC,
								ADD8BR, PC, ALU);

				IRQS = 2;
			}
			else
			{
				PopulateCURINSTR(RD_INC, ALU, PC,
								IDLE);

				IRQS = 2;
			}
		}

		private void BSR_()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							TR, ADDR, PC,
							ADD8BR, PC, ALU,
							IDLE,
							IDLE,
							WR_DEC_LO, SP, ADDR,
							WR_DEC_HI, SP, ADDR);

			IRQS = 7;
		}

		private void RTS()
		{
			PopulateCURINSTR(INC16, SP,
							RD_INC, ALU, SP,
							RD, ALU2, SP,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 4;
		}

		private void RTI()
		{
			PopulateCURINSTR(INC16, SP,
							RD_INC, CC, SP,
							RD_INC, B, SP,
							RD_INC, A, SP,
							RD_INC, ALU, SP,
							RD_INC_OP, ALU2, SP, SET_ADDR, X, ALU, ALU2,
							RD_INC, ALU, SP,
							RD, ALU2, SP, 
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 9;
		}

		private void PSH_(ushort src)
		{
			PopulateCURINSTR(WR, SP, src,
							IDLE,
							DEC16, SP);

			IRQS = 3;
		}

		private void PUL_(ushort src)
		{
			PopulateCURINSTR(INC16, SP,
							IDLE,
							RD, src, SP);

			IRQS = 3;
		}

		private void SWI1()
		{
			Regs[ADDR] = 0xFFFA;
			PopulateCURINSTR(IDLE,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, A,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, CC,
							SET_I,
							RD_INC, ALU, ADDR,
							RD_INC_OP, ALU2, ADDR, SET_ADDR, PC, ALU, ALU2);

			IRQS = 11;
		}

		private void WAI_()
		{
			PopulateCURINSTR(WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, A,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, CC,
							WAI);

			IRQS = 8;
		}
	}
}
