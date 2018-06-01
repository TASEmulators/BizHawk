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
		}

		private void EXX_()
		{
			cur_instr = new ushort[]
						{EXX,
						WAIT,
						OP_F,
						OP };
		}

		// this exchanges 2 16 bit registers
		private void EXCH_16_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{EXCH_16, dest_l, dest_h, src_l, src_h,
						WAIT,
						OP_F,
						OP };
		}

		private void INC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{INC16,  src_l, src_h,
						IDLE,
						IDLE,
						WAIT,					
						OP_F,
						OP };
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
		}

		private void REG_OP(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{operation, dest, src,
						WAIT,
						OP_F,
						OP };
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
			}
		}

		private void HALT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						HALT };
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
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh,
							IDLE,
							IDLE,
							WAIT,					
							OP_F,
							OP };
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
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							WAIT,
							OP_F,
							OP };
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
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh,
							DEC16, SPl, SPh,
							WAIT,
							WR, SPl, SPh, PCh,
							DEC16, SPl, SPh,
							WAIT,
							WR, SPl, SPh, PCl,
							TR16, PCl, PCh, Z, W,
							WAIT,
							OP_F,
							OP };
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
			}
		}

		private void INT_OP(ushort operation, ushort src)
		{
			cur_instr = new ushort[]
						{operation, src,
						WAIT,
						OP_F,
						OP };
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			cur_instr = new ushort[]
						{operation, bit, src,
						WAIT,
						OP_F,
						OP };
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						DEC16, SPl, SPh,
						WAIT,
						WR, SPl, SPh, src_h,
						DEC16, SPl, SPh,
						WAIT,
						WR, SPl, SPh, src_l,
						IDLE,
						WAIT,
						OP_F,
						OP };
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
		}

		private void RST_(ushort n)
		{
			cur_instr = new ushort[]
						{IDLE,
						DEC16, SPl, SPh,
						WAIT,
						WR, SPl, SPh, PCh,
						DEC16, SPl, SPh,
						WAIT,
						WR, SPl, SPh, PCl,
						RST, n,
						WAIT,						
						OP_F,
						OP };
		}

		private void PREFIX_(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						OP_F,
						PREFIX, src};
		}

		private void PREFETCH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{ADDS, Z, W, ALU, ZERO,
						WAIT,
						OP_F,
						PREFIX, IXYprefetch };
		}

		private void DI_()
		{
			cur_instr = new ushort[]
						{DI,
						WAIT,
						OP_F,
						OP };
		}

		private void EI_()
		{
			cur_instr = new ushort[]
						{EI,
						WAIT,
						OP_F,
						OP };
		}

		private void JP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR16, PCl, PCh, src_l, src_h,
						WAIT,
						OP_F,
						OP };
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
		}

		private void OUT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						TR, W, A,
						OUT, Z, W, A,
						INC16, Z, W,
						IDLE,
						WAIT,
						OP_F,
						OP};
		}

		private void OUT_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, C, B,
						OUT, Z, W, src,
						INC16, Z, W,
						IDLE,					
						WAIT,
						OP_F,
						OP};
		}

		private void IN_()
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						TR, W, A,
						IN, A, Z, W,
						INC16, Z, W,
						IDLE,
						WAIT,
						OP_F,
						OP};
		}

		private void IN_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IN, dest, src, B,
						TR16, Z, W, C, B,
						INC16, Z, W,
						IDLE,
						WAIT,
						OP_F,
						OP};
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
		}

		private void INT_MODE_(ushort src)
		{
			cur_instr = new ushort[]
						{INT_MODE, src,
						WAIT,
						OP_F,
						OP };
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
						WR, Z, W, ALU,
						INC16, Z, W,
						WAIT,
						OP_F,
						OP };
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
						WR, Z, W, ALU,
						INC16, Z, W,
						WAIT,
						OP_F,
						OP };
		}
	}
}
