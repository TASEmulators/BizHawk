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
						{IDLE,
						IDLE,
						IDLE,
						OP };
		}

		// NOTE: In a real Z80, this operation just flips a switch to choose between 2 registers
		// but it's simpler to emulate just by exchanging the register with it's shadow
		private void EXCH_()
		{
			cur_instr = new ushort[]
						{EXCH,
						IDLE,
						IDLE,
						OP };
		}

		private void EXX_()
		{
			cur_instr = new ushort[]
						{EXX,
						IDLE,
						IDLE,
						OP };
		}

		// this exchanges 2 16 bit registers
		private void EXCH_16_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{EXCH_16, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE,
						OP };
		}

		private void INC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						INC16,  src_l, src_h,					
						IDLE,
						OP };
		}


		private void DEC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						DEC16, src_l, src_h,
						IDLE,
						IDLE,
						OP };
		}

		// this is done in two steps technically, but the flags don't work out using existing funcitons
		// so let's use a different function since it's an internal operation anyway
		private void ADD_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						TR16, Z, W, dest_l, dest_h,
						INC16, Z, W,
						IDLE,
						IDLE,
						ADD16, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE,
						OP };
		}

		private void REG_OP(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{operation, dest, src,
						IDLE,
						IDLE,
						OP };
		}

		// Operations using the I and R registers take one T-cycle longer
		private void REG_OP_IR(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{operation, dest, src,
						IDLE,
						IDLE,
						SET_FL_IR, dest,
						OP };
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
							IDLE,
							RD, Z, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							ASGN, W, 0,
							IDLE,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							IDLE,
							RD, ALU, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
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
							IDLE,
							RD, Z, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,							
							IDLE,
							ASGN, W, 0,
							IDLE,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh,						
							IDLE,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							RD, ALU, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							OP };
			}
		}

		private void JP_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							RD, Z, PCl, PCh,
							INC16, PCl, PCh,
							RD, W, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							TR16, PCl, PCh, Z, W,
							IDLE,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							RD, Z, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,
							RD, W, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,						
							IDLE,
							OP };
			}
		}

		private void RET_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, Z, SPl, SPh,
						INC16, SPl, SPh,
						IDLE,						
						IDLE,
						RD, W, SPl, SPh,
						INC16, SPl, SPh,
						TR16, PCl, PCh, Z, W,						
						OP };
		}

		private void RETI_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, Z, SPl, SPh,
						INC16, SPl, SPh,
						IDLE,						
						IDLE,
						RD, W, SPl, SPh,
						INC16, SPl, SPh,
						TR16, PCl, PCh, Z, W,
						OP };
		}

		private void RETN_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, Z, SPl, SPh,
						INC16, SPl, SPh,
						IDLE,
						RD, W, SPl, SPh,
						INC16, SPl, SPh,
						EI_RETN,
						TR16, PCl, PCh, Z, W,
						OP };
		}


		private void RET_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							RD, Z, SPl, SPh,
							INC16, SPl, SPh,
							IDLE,						
							IDLE,
							RD, W, SPl, SPh,
							INC16, SPl, SPh,
							IDLE,							
							TR16, PCl, PCh, Z, W,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							IDLE,
							OP };
			}
		}

		private void CALL_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							RD, Z, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,							
							RD, W, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,
							DEC16, SPl, SPh,
							IDLE,
							WR, SPl, SPh, PCh,						
							DEC16, SPl, SPh,
							WR, SPl, SPh, PCl,
							IDLE,
							TR, PCl, Z,
							TR, PCh, W,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							RD, Z, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							RD, W, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							OP };
			}
		}

		private void INT_OP(ushort operation, ushort src)
		{
			cur_instr = new ushort[]
						{operation, src,
						IDLE,
						IDLE,
						OP };
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			cur_instr = new ushort[]
						{operation, bit, src,
						IDLE,
						IDLE,
						OP };
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, src_h,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, src_l,
						IDLE,
						OP };
		}


		private void POP_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, src_l, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						RD, src_h, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						OP };
		}

		private void RST_(ushort n)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						DEC16, SPl, SPh,
						WR, SPl, SPh, PCh,
						DEC16, SPl, SPh,
						WR, SPl, SPh, PCl,
						IDLE,
						ASGN, Z, n,
						ASGN, W, 0,						
						TR16, PCl, PCh, Z, W,
						OP };
		}

		private void PREFIX_(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						PREFIX, src};
		}

		private void PREFETCH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR16, Z, W, src_l, src_h,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						PREFIX, IXYprefetch };
		}

		private void DI_()
		{
			cur_instr = new ushort[]
						{DI,
						IDLE,
						IDLE,
						OP };
		}

		private void EI_()
		{
			cur_instr = new ushort[]
						{EI,
						IDLE,
						IDLE,
						OP };
		}

		private void JP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR, PCl, src_l,
						IDLE,
						TR, PCh, src_h,
						OP };
		}

		private void LD_SP_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,						
						IDLE,
						TR, SPl, src_l,
						TR, SPh, src_h,
						IDLE,
						OP };
		}

		private void OUT_()
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, ALU, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						TR, W, A,
						OUT, ALU, A,
						TR, Z, ALU,
						INC16, Z, ALU,
						IDLE,
						IDLE,
						OP};
		}

		private void OUT_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						OUT, dest, src,
						IDLE,
						TR16, Z, W, C, B,
						INC16, Z, W,
						IDLE,
						OP};
		}

		private void IN_()
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, ALU, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						TR, W, A,
						IN, A, ALU,
						TR, Z, ALU,
						INC16, Z, W,
						IDLE,
						IDLE,
						OP};
		}

		private void IN_REG_(ushort dest, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IN, dest, src,
						IDLE,
						TR16, Z, W, C, B,
						INC16, Z, W,
						IDLE,
						OP};
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
						op, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE,
						OP};
		}

		private void INT_MODE_(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						INT_MODE, src,
						OP };
		}

		private void RRD_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, Z, W, L, H,
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						RRD, ALU, A,
						IDLE,
						WR, Z, W, ALU,
						IDLE,
						INC16, Z, W,
						IDLE,
						IDLE,
						OP };
		}

		private void RLD_()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, Z, W, L, H,
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						RLD, ALU, A,
						IDLE,
						WR, Z, W, ALU,
						IDLE,
						INC16, Z, W,
						IDLE,
						IDLE,
						OP };
		}
	}
}
