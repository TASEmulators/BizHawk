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
			Console.WriteLine("EXCEPTION");
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
							oper, (ushort)(reg + RB));

			IRQS = 4;
		}


		public void OP_A_R(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							oper, A, (ushort)(reg + RB));

			IRQS = 4;
		}

		public void OP_IR(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							oper, (ushort)(reg + RB));

			IRQS = 4;
		}

		public void OP_A_IR(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							MEM_ALU, (ushort)(reg + RB),
							oper, A, ALU);

			IRQS = 4;
		}

		public void OP_DIR_IR(ushort oper, ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD, ALU, PC,
							INC11, PC,
							IDLE,
							IDLE,
							IDLE,
							oper, (ushort)(reg + RB), ALU);

			IRQS = 9;
		}

		public void IN_OUT_A(ushort oper, ushort port)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE, 
							IDLE,
							IDLE,
							IDLE,
							oper, A, port);

			IRQS = 9;
		}

		public void MOV_R(ushort dest, ushort src)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							MOV, dest, src);

			IRQS = 4;
		}

		public void BUS_PORT_IN()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							//IDLE);
							RD_P, A, 0);

			IRQS = 9;
			// Console.WriteLine("IN "+ TotalExecutedCycles);
		}

		public void BUS_PORT_OUT()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							WR_P, 0, A);

			IRQS = 9;
			Console.WriteLine("OUT");
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
							WR_P, port, A);

			IRQS = 9;

		}

		public void RET()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							PULL_PC,
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
							PULL,
							EM,
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
							TR, ALU, PC,
							IDLE,
							SET_ADDR_8, ALU, A,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							RD, A, ALU);

			IRQS = 9;
		}

		public void MOV3_A_A()
		{
			PopulateCURINSTR(IDLE,
							TR, ALU, PC,
							IDLE,
							SET_ADDR_8, ALU, A,
							IDLE,
							SET_ADDR_M3,
							IDLE,
							IDLE,
							RD, A, ALU);

			IRQS = 9;
		}

		public void MOVX_A_R(ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							EEA,
							WR_P, 0, (ushort)(reg + RB),
							DEA,
							IDLE,
							IDLE,
							IDLE,
							RD_P, A, 0);

			IRQS = 9;
		}

		public void MOVX_R_A(ushort reg)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							EEA,
							WR_P, 0, (ushort)(reg + RB),
							DEA,
							IDLE,
							IDLE,
							IDLE,
							WR_P, 0, A);

			IRQS = 9;
		}

		public void OP_A_DIR(ushort oper)
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD, ALU, PC,
							INC11, PC,
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
							INC11, PC,
							IDLE,
							IDLE,
							IDLE,
							oper, (ushort)(reg + RB), ALU);

			IRQS = 9;
		}

		// TODO: This should only write back to the port destination if directly wired, otherwise we should wait for a write pulse
		// TODO: for O2, P1 is tied direct to CTRL outputs so this is ok, BUS and P2 should do something else though
		public void OP_PB_DIR(ushort oper, ushort reg)
		{
			if (reg == 1)
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								RD, ALU, PC,
								INC11, PC,
								oper, (ushort)(reg + PX), ALU,
								IDLE,
								IDLE,
								WR_P, reg, (ushort)(reg + PX));
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								RD, ALU, PC,
								INC11, PC,
								oper, (ushort)(reg + PX), ALU,
								IDLE,
								IDLE,
								IDLE);
			}
			
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
							RD, ALU, PC,
							INC11, PC,
							IDLE,
							PUSH,
							IDLE,
							SET_ADDR, PC, ALU, dest_h);

			IRQS = 9;
		}

		public void DJNZ(ushort reg)
		{
			if ((Regs[reg + RB] - 1) == 0)
			{		
				PopulateCURINSTR(IDLE,
								IDLE,
								DEC8, (ushort)(reg + RB),
								RD, ALU, PC,
								INC11, PC,
								IDLE,
								IDLE,
								IDLE,
								IDLE);
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								DEC8, (ushort)(reg + RB),
								RD, ALU, PC,
								INC11, PC,
								IDLE,
								IDLE,
								IDLE,
								SET_ADDR_8, PC, ALU);
			}
			
			IRQS = 9;
		}

		public void JP_A()
		{
			PopulateCURINSTR(IDLE,
							IDLE,
							SET_ADDR_8, PC, A,
							RD, ALU, PC,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							SET_ADDR_8, PC, ALU);

			IRQS = 9;
		}

		public void JPB(ushort Tebit)
		{
			if (Regs[A].Bit(Tebit))
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								RD, ALU, PC,
								INC11, PC,
								IDLE,
								IDLE,
								IDLE,
								SET_ADDR_8, PC, ALU);
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								RD, ALU, PC,
								INC11, PC,
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
								RD, ALU, PC,
								INC11, PC,
								IDLE,
								IDLE,
								IDLE,
								SET_ADDR_8, PC, ALU);
			}
			else
			{
				PopulateCURINSTR(IDLE,
								IDLE,
								IDLE,
								RD, ALU, PC,
								INC11, PC,
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
							RD, ALU, PC,
							INC11, PC,
							IDLE,
							IDLE,
							IDLE,
							SET_ADDR, PC, ALU, high_addr);

			IRQS = 9;
		}
	}
}
