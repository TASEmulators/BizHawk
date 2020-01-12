using System;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.MC6809
{
	public partial class MC6809
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

		private void SYNC_()
		{
			PopulateCURINSTR(IDLE,
							SYNC);

			IRQS = -1;
		}

		private void REG_OP(ushort oper, ushort src)
		{
			PopulateCURINSTR(oper, src);

			IRQS = 1;
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
							ST_8, dest,
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
							ST_8, dest,
							WR, ADDR, dest);

			IRQS = 4;
		}

		private void REG_OP_IMD_CC(ushort oper)
		{
			Regs[ALU2] = Regs[CC];
			PopulateCURINSTR(RD_INC_OP, ALU, PC, oper, ALU2, ALU,
							TR, CC, ALU2);

			IRQS = 2;
		}

		private void REG_OP_IMD(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, oper, dest, ALU);

			IRQS = 1;
		}

		private void IMD_OP_D(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							oper, ADDR);

			IRQS = 3;
		}

		private void DIR_OP_D(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							RD_INC, ALU, ADDR,
							RD, ALU2, ADDR, 
							SET_ADDR, ADDR, ALU, ALU2,
							oper, ADDR);

			IRQS = 5;
		}

		private void EXT_OP_D(ushort oper, ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							RD_INC, ALU, ADDR,
							RD, ALU2, ADDR,
							SET_ADDR, ADDR, ALU, ALU2,
							oper, ADDR);

			IRQS = 6;
		}

		private void REG_OP_LD_16D()
		{
			PopulateCURINSTR(RD_INC, A, PC,
							RD_INC_OP, B, PC, LD_16, ADDR, A, B);

			IRQS = 2;
		}

		private void DIR_OP_LD_16D()
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							IDLE,
							RD_INC, A, ADDR,
							RD_INC_OP, B, ADDR, LD_16, ADDR, A, B);

			IRQS = 4;
		}

		private void DIR_OP_ST_16D()
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							ST_16, Dr,
							WR_LO_INC, ADDR, A,
							WR, ADDR, B);

			IRQS = 4;
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
							ST_16, src,
							WR_HI_INC, ADDR, src, 
							WR_DEC_LO, ADDR, src);

			IRQS = 4;
		}

		private void EXT_OP_LD_16(ushort dest)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							IDLE,
							RD_INC, ALU, ADDR,
							RD_INC_OP, ALU2, ADDR, LD_16, dest, ALU, ALU2);

			IRQS = 5;
		}

		private void EXT_OP_ST_16(ushort src)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							ST_16, src,
							WR_HI_INC, ADDR, src,
							WR_DEC_LO, ADDR, src);

			IRQS = 5;
		}

		private void EXT_OP_LD_16D()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							IDLE,
							RD_INC, A, ADDR,
							RD_INC_OP, B, ADDR, LD_16, ADDR, A, B);

			IRQS = 5;
		}

		private void EXT_OP_ST_16D()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							ST_16, Dr,
							WR_LO_INC, ADDR, A,
							WR, ADDR, B);

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

		private void EXG_()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							EXG, ALU,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 7;
		}

		private void TFR_()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							TFR, ALU,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 5;
		}

		private void JMP_DIR_()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							SET_ADDR, PC, DP, ALU);

			IRQS = 2;
		}

		private void JMP_EXT_()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC, ALU2, PC,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 3;
		}

		private void JSR_()
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, SET_ADDR, ADDR, DP, ALU,
							TR, ALU, PC,
							DEC16, SP,
							TR, PC, ADDR,
							WR_DEC_LO, SP, ALU,
							WR_HI, SP, ALU);

			IRQS = 6;
		}

		private void JSR_EXT()
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							RD_INC_OP, ALU2, PC, SET_ADDR, ADDR, ALU, ALU2,
							TR, ALU, PC,
							DEC16, SP,
							TR, PC, ADDR,
							WR_DEC_LO, SP, ALU,
							WR_HI, SP, ALU);

			IRQS = 7;
		}

		private void LBR_(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR(RD_INC, ALU, PC,
								RD_INC, ALU2, PC,
								SET_ADDR, ADDR, ALU, ALU2,
								ADD16BR, PC, ADDR);

				IRQS = 4;
			}
			else
			{
				PopulateCURINSTR(RD_INC, ALU, PC,
								RD_INC, ALU2, PC,
								SET_ADDR, ADDR, ALU, ALU2);

				IRQS = 3;
			}			
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
							DEC16, SP,
							WR_DEC_LO, SP, ADDR,
							WR_HI, SP, ADDR);

			IRQS = 6;
		}

		private void LBSR_()
		{
				PopulateCURINSTR(RD_INC, ALU, PC,
								RD_INC, ALU2, PC,
								SET_ADDR, ADDR, ALU, ALU2,
								TR, ALU, PC,
								ADD16BR, PC, ADDR,
								DEC16, SP,
								WR_DEC_LO, SP, ALU,
								WR_HI, SP, ALU);

			IRQS = 8;
		}

		private void PAGE_2()
		{
			PopulateCURINSTR(OP_PG_2);

			IRQS = -1;
		}

		private void PAGE_3()
		{
			PopulateCURINSTR(OP_PG_3);

			IRQS = -1;
		}

		private void ABX_()
		{
			PopulateCURINSTR(ABX,
							IDLE);

			IRQS = 2;
		}

		private void MUL_()
		{
			PopulateCURINSTR(MUL,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 10;
		}

		private void RTS()
		{
			PopulateCURINSTR(IDLE,
							RD_INC, ALU, SP,
							RD_INC, ALU2, SP,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 4;
		}

		private void RTI()
		{
			PopulateCURINSTR(IDLE,
							RD_INC_OP, CC, SP, JPE,
							RD_INC, A, SP,
							RD_INC, B, SP,
							RD_INC, DP, SP,
							RD_INC, ALU, SP,
							RD_INC_OP, ALU2, SP, SET_ADDR, X, ALU, ALU2,
							RD_INC, ALU, SP,
							RD_INC_OP, ALU2, SP, SET_ADDR, Y, ALU, ALU2,
							RD_INC, ALU, SP,
							RD_INC_OP, ALU2, SP, SET_ADDR, US, ALU, ALU2,
							RD_INC, ALU, SP,
							RD_INC_OP, ALU2, SP, SET_ADDR, PC, ALU, ALU2);

			IRQS = 14;
		}

		private void PSH(ushort src)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							IDLE,
							DEC16, src,
							PSH_n, src);

			IRQS = -1;
		}

		// Post byte info is in ALU
		// mask out bits until the end
		private void PSH_n_BLD(ushort src)
		{
			if (Regs[ALU].Bit(7))
			{
				PopulateCURINSTR(WR_DEC_LO, src, PC,
								WR_DEC_HI_OP, src, PC, PSH_n, src);

				Regs[ALU] &= 0x7F;
			}
			else if (Regs[ALU].Bit(6))
			{
				if (src == US)
				{
					PopulateCURINSTR(WR_DEC_LO, src, SP,
									WR_DEC_HI_OP, src, SP, PSH_n, src);
				}
				else
				{
					PopulateCURINSTR(WR_DEC_LO, src, US,
									WR_DEC_HI_OP, src, US, PSH_n, src);
				}
				
				Regs[ALU] &= 0x3F;
			}
			else if (Regs[ALU].Bit(5))
			{
				PopulateCURINSTR(WR_DEC_LO, src, Y,
								WR_DEC_HI_OP, src, Y, PSH_n, src);

				Regs[ALU] &= 0x1F;
			}
			else if (Regs[ALU].Bit(4))
			{
				PopulateCURINSTR(WR_DEC_LO, src, X,
								WR_DEC_HI_OP, src, X, PSH_n, src);

				Regs[ALU] &= 0xF;
			}
			else if (Regs[ALU].Bit(3))
			{
				PopulateCURINSTR(WR_DEC_LO_OP, src, DP, PSH_n, src);

				Regs[ALU] &= 0x7;
			}
			else if (Regs[ALU].Bit(2))
			{
				PopulateCURINSTR(WR_DEC_LO_OP, src, B, PSH_n, src);

				Regs[ALU] &= 0x3;
			}
			else if (Regs[ALU].Bit(1))
			{
				PopulateCURINSTR(WR_DEC_LO_OP, src, A, PSH_n, src);

				Regs[ALU] &= 0x1;
			}
			else if (Regs[ALU].Bit(0))
			{
				PopulateCURINSTR(WR_DEC_LO_OP, src, CC, PSH_n, src);

				Regs[ALU] = 0;
			}
			else
			{
				Regs[src] += 1; // we decremented an extra time overall, regardless of what was run

				IRQS = irq_pntr + 1;
			}

			instr_pntr = 0;
		}

		private void PUL(ushort src)
		{
			PopulateCURINSTR(RD_INC, ALU, PC,
							IDLE,
							PUL_n, src);

			IRQS = -1;
		}

		// Post byte info is in ALU
		// mask out bits until the end
		private void PUL_n_BLD(ushort src)
		{
			if (Regs[ALU].Bit(0))
			{
				PopulateCURINSTR(RD_INC_OP, CC, src, PUL_n, src);

				Regs[ALU] &= 0xFE;
			}
			else if (Regs[ALU].Bit(1))
			{
				PopulateCURINSTR(RD_INC_OP, A, src, PUL_n, src);

				Regs[ALU] &= 0xFC;
			}
			else if (Regs[ALU].Bit(2))
			{
				PopulateCURINSTR(RD_INC_OP, B, src, PUL_n, src);

				Regs[ALU] &= 0xF8;
			}
			else if (Regs[ALU].Bit(3))
			{
				PopulateCURINSTR(RD_INC_OP, DP, src, PUL_n, src);

				Regs[ALU] &= 0xF0;
			}
			else if (Regs[ALU].Bit(4))
			{
				PopulateCURINSTR(RD_INC, ALU2, src,
								RD_INC_OP, ADDR, src, SET_ADDR_PUL, X, src);

				Regs[ALU] &= 0xE0;
			}
			else if (Regs[ALU].Bit(5))
			{
				PopulateCURINSTR(RD_INC, ALU2, src,
								RD_INC_OP, ADDR, src, SET_ADDR_PUL, Y, src);

				Regs[ALU] &= 0xC0;
			}
			else if (Regs[ALU].Bit(6))
			{
				if (src == US)
				{
					PopulateCURINSTR(RD_INC, ALU2, src,
									RD_INC_OP, ADDR, src, SET_ADDR_PUL, SP, src);
				}
				else
				{
					PopulateCURINSTR(RD_INC, ALU2, src,
									RD_INC_OP, ADDR, src, SET_ADDR_PUL, US, src);
				}

				Regs[ALU] &= 0x80;
			}
			else if (Regs[ALU].Bit(7))
			{
				PopulateCURINSTR(RD_INC, ALU2, src,
								RD_INC_OP, ADDR, src, SET_ADDR_PUL, PC, src);

				Regs[ALU] = 0;
			}
			else
			{
				// extra end cycle
				PopulateCURINSTR(IDLE);

				IRQS = irq_pntr + 2;
			}

			instr_pntr = 0;
		}

		private void SWI1()
		{
			Regs[ADDR] = 0xFFFA;
			PopulateCURINSTR(SET_E,
							DEC16, SP,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, US,
							WR_DEC_HI, SP, US,
							WR_DEC_LO, SP, Y,
							WR_DEC_HI, SP, Y,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, DP,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, A,
							WR, SP, CC,
							SET_F_I,
							RD_INC, ALU, ADDR,
							RD_INC, ALU2, ADDR,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 18;
		}

		private void SWI2_3(ushort pick)
		{
			Regs[ADDR] = (ushort)((pick == 3) ? 0xFFF2 : 0xFFF4);
			PopulateCURINSTR(SET_E,
							DEC16, SP,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, US,
							WR_DEC_HI, SP, US,
							WR_DEC_LO, SP, Y,
							WR_DEC_HI, SP, Y,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, DP,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, A,
							WR, SP, CC,
							IDLE,
							RD_INC, ALU, ADDR,
							RD_INC, ALU2, ADDR,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 18;
		}

		private void CWAI_()
		{
			PopulateCURINSTR(RD_INC_OP, ALU, PC, ANDCC, ALU,
							SET_E,
							DEC16, SP,
							WR_DEC_LO, SP, PC,
							WR_DEC_HI, SP, PC,
							WR_DEC_LO, SP, US,
							WR_DEC_HI, SP, US,
							WR_DEC_LO, SP, Y,
							WR_DEC_HI, SP, Y,
							WR_DEC_LO, SP, X,
							WR_DEC_HI, SP, X,
							WR_DEC_LO, SP, DP,
							WR_DEC_LO, SP, B,
							WR_DEC_LO, SP, A,
							WR, SP, CC,
							CWAI);

			IRQS = 16;
		}
	}
}
