using System;

namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A
	{
		// this contains the vectors of instrcution operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		private void NOP_()
		{
			cur_instr = new ushort[]
						{IDLE };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		// NOTE: In a real Z80, this operation just flips a switch to choose between 2 registers
		// but it's simpler to emulate just by exchanging the register with it's shadow
		private void EXCH_()
		{
			cur_instr = new ushort[]
						{EXCH };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void EXX_()
		{
			cur_instr = new ushort[]
						{EXX };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		// this exchanges 2 16 bit registers
		private void EXCH_16_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{EXCH_16, dest_l, dest_h, src_l, src_h };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void INC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{INC16, src_l, src_h,
						IDLE,
						IDLE };

			BUSRQ = new ushort[] { 0, I, I };
			MEMRQ = new ushort[] { 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 1 };
		}


		private void DEC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{DEC16, src_l, src_h,
						IDLE,
						IDLE };

			BUSRQ = new ushort[] { 0, I, I };
			MEMRQ = new ushort[] { 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 1 };
		}

		// this is done in two steps technically, but the flags don't work out using existing funcitons
		// so let's use a different function since it's an internal operation anyway
		private void ADD_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, dest_l, dest_h,
						IDLE,
						INC16, Z, W,
						IDLE,
						ADD16, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE};

			BUSRQ = new ushort[] { 0, I, I, I, I, I, I, I };
			MEMRQ = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1};
		}

		private void REG_OP(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{operation, dest, src };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		// Operations using the I and R registers take one T-cycle longer
		private void REG_OP_IR(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						SET_FL_IR, dest, src };

			BUSRQ = new ushort[] { 0, I };
			MEMRQ = new ushort[] { 0, 0 };
			IRQS = new ushort[] { 0, 1 };
		}

		// note: do not use DEC here since no flags are affected by this operation
		private void DJNZ_()
		{
			if ((Regs[B] - 1) != 0)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							IDLE,
							ASGN, W, 0,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh };

				BUSRQ = new ushort[] { 0, I, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh };
				MEMRQ = new ushort[] { 0, 0, PCh, 0, 0, 0, 0, 0, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							WAIT,
							RD_INC, ALU, PCl, PCh };

				BUSRQ = new ushort[] { 0, I, PCh, 0, 0 };
				MEMRQ = new ushort[] { 0, 0, PCh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 1 };
			}
		}

		private void HALT_()
		{
			cur_instr = new ushort[]
						{ HALT };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void JR_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							ASGN, W, 0,
							IDLE,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh };

				BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh };
				MEMRQ = new ushort[] { 0, PCh, 0, 0, 0, 0, 0, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, ALU, PCl, PCh };

				BUSRQ = new ushort[] { 0, PCh, 0, 0 };
				MEMRQ = new ushort[] { 0, PCh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 1 };
			}
		}

		private void JP_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC_TR_PC, Z, W, PCl, PCh};

				BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0 };
				MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh };

				BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0 };
				MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
			}
		}

		private void RET_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC_TR_PC, Z, W, SPl, SPh };

			BUSRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			MEMRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
		}

		private void RETI_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC_TR_PC, Z, W, SPl, SPh };

			BUSRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			MEMRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
		}

		private void RETN_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						EI_RETN,
						WAIT,
						RD_INC_TR_PC, Z, W, SPl, SPh };

			BUSRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			MEMRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
		}


		private void RET_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, SPl, SPh,	
							IDLE,						
							WAIT,
							RD_INC_TR_PC, Z, W, SPl, SPh};

				BUSRQ = new ushort[] { 0, I, SPh, 0, 0, SPh, 0, 0 };
				MEMRQ = new ushort[] { 0, 0, SPh, 0, 0, SPh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE };

				BUSRQ = new ushort[] { 0, I };
				MEMRQ = new ushort[] { 0, 0 };
				IRQS = new ushort[] { 0, 1 };
			}
		}

		private void CALL_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD, W, PCl, PCh,
							INC16, PCl, PCh,
							DEC16, SPl, SPh,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							IDLE,
							WAIT,
							WR_TR_PC, SPl, SPh, PCl };

				BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, PCh, SPh, 0, 0, SPh, 0, 0 };
				MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, 0, SPh, 0, 0, SPh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh};

				BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0 };
				MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0 };
				IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
			}
		}

		private void INT_OP(ushort operation, ushort src)
		{
			cur_instr = new ushort[]
						{operation, src };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			cur_instr = new ushort[]
						{operation, bit, src };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, src_h,
						IDLE,
						WAIT,
						WR, SPl, SPh, src_l };

			BUSRQ = new ushort[] { 0, I, SPh, 0, 0, SPh, 0, 0 };
			MEMRQ = new ushort[] { 0, 0, SPh, 0, 0, SPh, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}


		private void POP_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, src_l, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC, src_h, SPl, SPh };

			BUSRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			MEMRQ = new ushort[] { 0, SPh, 0, 0, SPh, 0, 0 };
			IRQS = new ushort[] {0, 0, 0, 0, 0, 0, 1 };
		}

		private void RST_(ushort n)
		{
			cur_instr = new ushort[]
						{IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						RST, n,
						WAIT,
						WR_TR_PC, SPl, SPh, PCl };

			BUSRQ = new ushort[] { 0, I, SPh, 0, 0, SPh, 0, 0 };
			MEMRQ = new ushort[] { 0, 0, SPh, 0, 0, SPh, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void PREFIX_(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						PREFIX };

			PRE_SRC = src;

			BUSRQ = new ushort[] { 0, PCh, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0 }; // prefix does not get interrupted
		}

		private void PREFETCH_(ushort src)
		{
			if (src == IXCBpre)
			{
				Regs[W] = Regs[Ixh];
				Regs[Z] = Regs[Ixl];
			}
			else
			{
				Regs[W] = Regs[Iyh];
				Regs[Z] = Regs[Iyl];
			}

			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						ADDS, Z, W, ALU, ZERO,
						WAIT,
						IDLE,
						PREFIX};

			PRE_SRC = src;

			//Console.WriteLine(TotalExecutedCycles);

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, PCh };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 }; // prefetch does not get interrupted
		}

		private void DI_()
		{
			cur_instr = new ushort[]
						{DI };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void EI_()
		{
			cur_instr = new ushort[]
						{EI };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void JP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR16, PCl, PCh, src_l, src_h };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void LD_SP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, SPl, SPh, src_l, src_h };

			BUSRQ = new ushort[] { 0, I, I };
			MEMRQ = new ushort[] { 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 1 };
		}

		private void OUT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						TR, W, A,
						WAIT,
						RD_INC, Z, PCl, PCh,
						TR, ALU, A,						
						WAIT,					
						WAIT,
						OUT_INC, Z, ALU, A };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1};
		}

		private void OUT_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, C, B,					
						IDLE,					
						IDLE,
						OUT_INC, Z, W, src };

			BUSRQ = new ushort[] { 0, BIO1, BIO2, BIO3, BIO4 };
			MEMRQ = new ushort[] { 0, BIO1, BIO2, BIO3, BIO4 };
			IRQS = new ushort[] { 0, 0, 0, 0, 1 };
		}

		private void IN_()
		{
			cur_instr = new ushort[]
						{IDLE,
						TR, W, A,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						WAIT,
						IN_A_N_INC, A, Z, W };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void IN_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, C, B,
						WAIT,
						WAIT,
						IN_INC, dest, Z, W };

			BUSRQ = new ushort[] { 0, BIO1, BIO2, BIO3, BIO4 };
			MEMRQ = new ushort[] { 0, BIO1, BIO2, BIO3, BIO4 };
			IRQS = new ushort[] { 0, 0, 0, 0, 1 };
		}

		private void REG_OP_16_(ushort op, ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						TR16, Z, W, dest_l, dest_h,
						INC16, Z, W,
						IDLE,
						IDLE,
						op, dest_l, dest_h, src_l, src_h };

			BUSRQ = new ushort[] { 0, I, I, I, I, I, I, I };
			MEMRQ = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void INT_MODE_(ushort src)
		{
			cur_instr = new ushort[]
						{INT_MODE, src };

			BUSRQ = new ushort[] { 0 };
			MEMRQ = new ushort[] { 0 };
			IRQS = new ushort[] { 1 };
		}

		private void RRD_()
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, L, H,
						WAIT,
						RD, ALU, Z, W,
						IDLE,
						RRD, ALU, A,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						WR_INC, Z, W, ALU };

			BUSRQ = new ushort[] { 0, H, 0, 0, H, H, H, H, W, 0, 0 };
			MEMRQ = new ushort[] { 0, H, 0, 0, 0, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void RLD_()
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, L, H,
						WAIT,
						RD, ALU, Z, W,
						IDLE,
						RLD, ALU, A,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						WR_INC, Z, W, ALU };

			BUSRQ = new ushort[] { 0, H, 0, 0, H, H, H, H, W, 0, 0 };
			MEMRQ = new ushort[] { 0, H, 0, 0, 0, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}
	}
}
