using System;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Common.Components.I8048
{
	public partial class I8048
	{
		// this contains the vectors of instrcution operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle
		public void ILLEGAL()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 4;
		}

		public void OP_IMP(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							oper);

			IRQS = 4;
		}

		public void OP_R_IMP(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							oper, reg);

			IRQS = 4;
		}


		public void OP_A_R(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							oper, A, reg);

			IRQS = 4;
		}

		public void IN_OUT_A(ushort oper, ushort port)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							oper, A, port);

			IRQS = 4;
		}

		public void MOV_R(ushort dest, ushort src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							MOV, dest, src);

			IRQS = 4;
		}

		public void JP_A()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void IN_OUT_BUS(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							oper, A);

			IRQS = 9;
		}

		public void OUT_P(ushort port)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							port, A);

			IRQS = 9;
		}

		public void RET()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void RETR()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void MOV_A_P4(ushort port)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void MOV_P4_A(ushort port)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void MOV_A_A()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void MOV3_A_A()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void MOVX_A_R(ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void MOVX_R_A(ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}

		public void OP_A_DIR(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD, ALU, PC,
							INC16, PC,
							IDLE,
							IDLE,
							IDLE,
							oper, A, ALU);

			IRQS = 9;
		}

		public void OP_R_DIR(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD, ALU, PC,
							INC16, PC,
							IDLE,
							IDLE,
							IDLE,
							oper, reg, ALU);

			IRQS = 9;
		}

		public void OP_PB_DIR(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD, ALU, PC,
							INC16, PC,
							IDLE,
							IDLE,
							IDLE,
							oper, reg, ALU);

			IRQS = 9;
		}

		public void OP_EXP_A(ushort oper, ushort reg)
		{
			// Lower 4 bits only		
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							TR, ALU, A,
							IDLE,
							IDLE,
							MSK, ALU,
							IDLE,
							oper, reg, ALU);

			IRQS = 9;
		}

		public void CALL(ushort dest_h)
		{
			// Lower 4 bits only		
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							TR, ALU, A,
							IDLE,
							IDLE,
							MSK, ALU,
							IDLE,
							ALU);

			IRQS = 9;
		}

		public void DJNZ(ushort reg)
		{
			if ((Regs[reg] - 1) == 0)
			{		
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE);
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE);
			}
			
			IRQS = 9;
		}

		public void JPB(ushort Tebit)
		{
			if (Regs[A].Bit(Tebit))
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE);
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE);
			}

			IRQS = 9;
		}

		public void JP_COND(bool cond, ushort SPEC)
		{
			if (cond)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE);
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								IDLE,
								IDLE,
								SPEC,
								IDLE,
								IDLE,
								IDLE);
			}

			IRQS = 9;
		}

		public void JP_2k(ushort high_addr)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 9;
		}
	}
}
