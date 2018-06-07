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
						{ IDLE,
						  WAIT,
						  OP_F,
						  OP };

			BUSRQ = new ushort[] {PCh, 0, 0, 0 };
		}

		// NOTE: In a real Z80, this operation just flips a switch to choose between 2 registers
		// but it's simpler to emulate just by exchanging the register with it's shadow
		private void EXCH_()
		{
			cur_instr = new ushort[]
						{EXCH,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void EXX_()
		{
			cur_instr = new ushort[]
						{EXX,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		// this exchanges 2 16 bit registers
		private void EXCH_16_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{EXCH_16, dest_l, dest_h, src_l, src_h,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void INC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{INC16, src_l, src_h,
						IDLE,
						IDLE,
						WAIT,					
						OP_F,
						OP };

			BUSRQ = new ushort[] {I, I, PCh, 0, 0, 0};
		}


		private void DEC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{DEC16, src_l, src_h,
						IDLE,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] {I, I, PCh, 0, 0, 0};
		}

		// this is done in two steps technically, but the flags don't work out using existing funcitons
		// so let's use a different function since it's an internal operation anyway
		private void ADD_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR16, Z, W, dest_l, dest_h,
						IDLE,
						INC16, Z, W,
						IDLE,
						ADD16, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] {I, I, I, I, I, I, I, PCh, 0, 0, 0 };
		}

		private void REG_OP(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{operation, dest, src,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] {PCh, 0, 0, 0 };
		}

		// Operations using the I and R registers take one T-cycle longer
		private void REG_OP_IR(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{operation, dest, src,
						SET_FL_IR, dest,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { I, PCh, 0, 0, 0 };
		}

		// note: do not use DEC here since no flags are affected by this operation
		private void DJNZ_()
		{
			if ((Regs[B] - 1) != 0)
			{
				cur_instr = new ushort[]
							{IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							IDLE,
							ASGN, W, 0,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh,
							IDLE,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] {I, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, PCh, 0, 0, 0};
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							WAIT,
							RD_INC, ALU, PCl, PCh,
							IDLE,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] {I, PCh, 0, 0, PCh, 0, 0, 0};
			}
		}

		private void HALT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						HALT };

			BUSRQ = new ushort[] {PCh, 0, 0, 0 };
		}

		private void JR_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							ASGN, W, 0,
							IDLE,
							IDLE,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh,
							WAIT,					
							OP_F,
							OP };

				BUSRQ = new ushort[] {PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, PCh, 0, 0, 0};
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							WAIT,
							RD_INC, ALU, PCl, PCh,
							IDLE,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] {PCh, 0, 0, PCh, 0, 0, 0};
			}
		}

		private void JP_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							TR16, PCl, PCh, Z, W,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] {PCh, 0, 0, PCh, 0, 0, W, 0, 0, 0};
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,						
							OP_F,
							OP };

				BUSRQ = new ushort[] {PCh, 0, 0, PCh, 0, 0, PCh, 0, 0, 0};
			}
		}

		private void RET_()
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC, W, SPl, SPh,
						TR16, PCl, PCh, Z, W,
						WAIT,
						OP_F,						
						OP };

			BUSRQ = new ushort[] {SPh, 0, 0, SPh, 0, 0, W, 0, 0, 0};
		}

		private void RETI_()
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC, W, SPl, SPh,
						TR16, PCl, PCh, Z, W,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] {SPh, 0, 0, SPh, 0, 0, W, 0, 0, 0};
		}

		private void RETN_()
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						EI_RETN,
						WAIT,
						RD_INC, W, SPl, SPh,
						TR16, PCl, PCh, Z, W,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] {SPh, 0, 0, SPh, 0, 0, W, 0, 0, 0};
		}


		private void RET_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, SPl, SPh,	
							IDLE,						
							WAIT,
							RD_INC, W, SPl, SPh,
							TR16, PCl, PCh, Z, W,
							WAIT,							
							OP_F,
							OP };

				BUSRQ = new ushort[] {I, SPh, 0, 0, SPh, 0, 0, W, 0, 0, 0};
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] {I, PCh, 0, 0, 0};
			}
		}

		private void CALL_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							DEC16, SPl, SPh,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							IDLE,
							WAIT,
							WR, SPl, SPh, PCl,
							TR16, PCl, PCh, Z, W,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] {PCh, 0, 0, PCh, 0, 0, PCh, SPh, 0, 0, SPh, 0, 0, W, 0, 0, 0};
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							IDLE,
							WAIT,
							OP_F,
							OP };

				BUSRQ = new ushort[] { PCh, 0, 0, PCh, 0, 0, PCh, 0, 0, 0 };
			}
		}

		private void INT_OP(ushort operation, ushort src)
		{
			cur_instr = new ushort[]
						{operation, src,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			cur_instr = new ushort[]
						{operation, bit, src,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, src_h,
						IDLE,
						WAIT,
						WR, SPl, SPh, src_l,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { I, SPh, 0, 0, SPh, 0, 0, PCh, 0, 0, 0 };
		}


		private void POP_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, src_l, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC, src_h, SPl, SPh,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { SPh, 0, 0, SPh, 0, 0, PCh, 0, 0, 0 };
		}

		private void RST_(ushort n)
		{
			cur_instr = new ushort[]
						{DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						RST, n,
						WAIT,
						WR, SPl, SPh, PCl,	
						TR16, PCl, PCh, Z, W,
						WAIT,						
						OP_F,
						OP };

			BUSRQ = new ushort[] { I, SPh, 0, 0, SPh, 0, 0, W, 0, 0, 0 };
		}

		private void PREFIX_(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						OP_F,
						PREFIX, src};

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
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
						WAIT,
						RD_INC, ALU, PCl, PCh,
						ADDS, Z, W, ALU, ZERO,
						WAIT,
						OP_F,
						IDLE,
						PREFIX, src,};

			BUSRQ = new ushort[] { PCh, 0, 0, PCh, 0, 0, PCh, PCh };
		}

		private void DI_()
		{
			cur_instr = new ushort[]
						{DI,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void EI_()
		{
			cur_instr = new ushort[]
						{EI,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void JP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR16, PCl, PCh, src_l, src_h,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { src_h, 0, 0, 0 };
		}

		private void LD_SP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,						
						IDLE,
						TR16, SPl, SPh, src_l, src_h,
						WAIT, 
						OP_F,
						OP };

			BUSRQ = new ushort[] { I, I, PCh, 0, 0, 0 };
		}

		private void OUT_()
		{
			cur_instr = new ushort[]
						{TR, W, A,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						WAIT,
						OUT, Z, W, A,
						INC16, Z, W,					
						WAIT,
						OP_F,
						OP};

			BUSRQ = new ushort[] { PCh, 0, 0, WIO1, WIO2, WIO3, WIO4, PCh, 0, 0, 0};
		}

		private void OUT_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, Z, W, C, B,
						OUT, Z, W, src,
						INC16, Z, W,					
						WAIT,
						OP_F,
						OP};

			BUSRQ = new ushort[] { BIO1, BIO2, BIO3, BIO4, PCh, 0, 0, 0 };
		}

		private void IN_()
		{
			cur_instr = new ushort[]
						{TR, W, A,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						WAIT,
						IN, A, Z, W,
						INC16, Z, W,
						WAIT,
						OP_F,
						OP};

			BUSRQ = new ushort[] { PCh, 0, 0, WIO1, WIO2, WIO3, WIO4, PCh, 0, 0, 0 };
		}

		private void IN_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{TR16, Z, W, C, B,
						WAIT,
						WAIT,
						IN, dest, Z, W,						
						INC16, Z, W,
						WAIT,
						OP_F,
						OP};

			BUSRQ = new ushort[] { BIO1, BIO2, BIO3, BIO4, PCh, 0, 0, 0 };
		}

		private void REG_OP_16_(ushort op, ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, Z, W, dest_l, dest_h,
						INC16, Z, W,
						IDLE,
						IDLE,
						op, dest_l, dest_h, src_l, src_h,
						IDLE,
						WAIT,
						OP_F,
						OP};

			BUSRQ = new ushort[] { I, I, I, I, I, I, I, PCh, 0, 0, 0 };
		}

		private void INT_MODE_(ushort src)
		{
			cur_instr = new ushort[]
						{INT_MODE, src,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
		}

		private void RRD_()
		{
			cur_instr = new ushort[]
						{TR16, Z, W, L, H,
						WAIT,
						RD, ALU, Z, W,
						IDLE,
						RRD, ALU, A,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						WR_INC, Z, W, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { H, 0, 0, H, H, H, H, W, 0, 0, PCh, 0, 0, 0 };
		}

		private void RLD_()
		{
			cur_instr = new ushort[]
						{TR16, Z, W, L, H,
						WAIT,
						RD, ALU, Z, W,
						IDLE,
						RLD, ALU, A,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						WR_INC, Z, W, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };

			BUSRQ = new ushort[] { H, 0, 0, H, H, H, H, W, 0, 0, PCh, 0, 0, 0 };
		}
	}
}
