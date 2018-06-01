namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A
	{
		private void INT_OP_IND(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						IDLE,
						operation, ALU,
						WAIT,
						WR, src_l, src_h, ALU,
						IDLE,
						WAIT,					
						OP_F,						
						OP };
		}

		private void BIT_OP_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,	
						RD, ALU, src_l, src_h,
						operation, bit, ALU,
						WAIT,
						WR, src_l, src_h, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		// Note that this operation uses I_BIT, same as indexed BIT.
		// This is where the strange behaviour in Flag bits 3 and 5 come from.
		// normally WZ contain I* + n when doing I_BIT ops, but here we use that code path 
		// even though WZ is not assigned to, letting it's value from other operations show through
		private void BIT_TE_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						I_BIT, bit, ALU,
						WAIT,
						OP_F,
						OP };
		}

		private void REG_OP_IND_INC(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,					
						WAIT,					
						RD_INC, ALU, src_l, src_h,
						operation, dest, ALU,
						WAIT,
						OP_F,
						OP };
		}

		private void REG_OP_IND(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{TR16, Z, W, src_l, src_h,
						WAIT,
						RD_INC, ALU, Z, W,
						operation, dest, ALU,
						WAIT,
						OP_F,									
						OP };
		}

		private void LD_16_IND_nn(ushort src_l, ushort src_h)
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
						WR, Z, W, src_l,
						INC16, Z, W,
						WAIT,
						WR, Z, W, src_h,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_IND_16_nn(ushort dest_l, ushort dest_h)
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
						RD_INC, dest_l, Z, W,
						IDLE,
						WAIT,
						RD, dest_h, Z, W,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_8_IND_nn(ushort src)
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
						WR_INC, Z, W, src,
						TR, W, A,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_IND_8_nn(ushort dest)
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
						RD_INC, dest, Z, W,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_8_IND(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{TR16, Z, W, dest_l, dest_h,
						WAIT,
						WR_INC, Z, W, src,
						TR, W, A,
						WAIT,
						OP_F,				
						OP };
		}

		private void LD_8_IND_IND(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, ALU, src_l, src_h,
						IDLE,
						WAIT,
						WR, dest_l, dest_h, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_IND_8_INC(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, dest, src_l, src_h,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_IND_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, dest_l, src_l, src_h,
						IDLE,
						WAIT,
						RD_INC, dest_h, src_l, src_h,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void INC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,					
						RD, ALU, src_l, src_h,
						INC8, ALU,
						WAIT,
						WR,  src_l, src_h, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void DEC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,	
						RD, ALU, src_l, src_h,
						DEC8, ALU,
						WAIT,
						WR, src_l, src_h, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		// NOTE: WZ implied for the wollowing 3 functions
		private void I_INT_OP(ushort operation, ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, ALU,
						WAIT,
						WR, Z, W, ALU,
						TR, dest, ALU,
						WAIT,
						OP_F,
						OP };
		}

		private void I_BIT_OP(ushort operation, ushort bit, ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, bit, ALU,
						WAIT,
						WR, Z, W, ALU,
						TR, dest, ALU,
						WAIT,
						OP_F,
						OP };
		}

		private void I_BIT_TE(ushort bit)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						I_BIT, bit, ALU,
						WAIT,
						OP_F,
						OP };
		}

		private void I_OP_n(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						IDLE,
						IDLE,
						TR16, Z, W, src_l, src_h,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, ALU,
						WAIT,
						WR, Z, W, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void I_OP_n_n(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						IDLE,
						TR16, Z, W, src_l, src_h,
						ADDS, Z, W, ALU, ZERO,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						IDLE,
						WAIT,
						WR, Z, W, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void I_REG_OP_IND_n(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						IDLE,
						TR16, Z, W, src_l, src_h,
						IDLE,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, dest, ALU,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void I_LD_8_IND_n(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						IDLE,
						IDLE,					
						TR16, Z, W, dest_l, dest_h,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						IDLE,
						WAIT,
						WR, Z, W, src,
						IDLE,
						WAIT,
						OP_F,
						OP };
		}

		private void LD_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD, ALU, L, H,
						IDLE,
						WAIT,
						WR, E, D, ALU,
						operation, L, H,
						operation, E, D,
						SET_FL_LD, // BC gets decremented in here
						WAIT, 
						OP_F,
						OP_R, 0, operation, repeat_instr };
		}

		private void CP_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,						
						WAIT,
						RD, ALU, L, H,
						operation, L, H,
						DEC16, C, B,
						SET_FL_CP,
						operation, Z, W,
						IDLE,
						IDLE,
						WAIT,
						OP_F,
						OP_R, 1, operation, repeat_instr };
		}

		private void IN_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						IN, ALU, C, B,
						IDLE,
						WAIT,
						WR, L, H, ALU,
						REP_OP_I, operation,
						IDLE,
						WAIT,
						OP_F,
						OP_R, 2, operation, repeat_instr };
		}

		private void OUT_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD, ALU, L, H,
						IDLE,
						IDLE,
						WAIT,
						OUT, C, B, ALU,
						REP_OP_O, operation,
						IDLE,
						WAIT,
						OP_F,
						OP_R, 3, operation, repeat_instr };
		}

		// this is an indirect change of a a 16 bit register with memory
		private void EXCH_16_IND_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						WAIT,
						RD, Z, dest_l, dest_h,
						IDLE,				
						IDLE,
						WAIT,
						I_RD, W, dest_l, dest_h, 1,
						IDLE,
						WAIT,
						WR, dest_l, dest_h, src_l,
						IDLE,
						IDLE,
						IDLE,						
						WAIT,
						I_WR, dest_l, dest_h, 1, src_h,
						TR16, src_l, src_h, Z, W,
						WAIT,						
						OP_F,
						OP };
		}
	}
}
