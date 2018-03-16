namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A
	{
		private void INT_OP_IND(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, ALU, src_l, src_h,
						IDLE,
						operation, ALU,
						IDLE,
						WR, src_l, src_h, ALU,
						IDLE,					
						IDLE,						
						OP };
		}

		private void BIT_OP_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,	
						RD, ALU, src_l, src_h,
						IDLE,
						operation, bit, ALU,
						IDLE,
						WR, src_l, src_h, ALU,
						IDLE,
						IDLE,
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
						IDLE,
						RD, ALU, src_l, src_h,
						IDLE,
						I_BIT, bit, ALU,
						IDLE,
						OP };
		}

		private void REG_OP_IND_INC(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,					
						IDLE,					
						RD, ALU, src_l, src_h,
						IDLE,
						operation, dest, ALU,
						INC16, src_l, src_h,
						OP };
		}

		private void REG_OP_IND(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, Z, W, src_l, src_h,
						RD, ALU, Z, W,
						INC16, Z, W,
						operation, dest, ALU,									
						OP };
		}

		private void LD_16_IND_nn(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, Z, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						WR, Z, W, src_l,
						IDLE,
						INC16, Z, W,
						IDLE,
						WR, Z, W, src_h,
						IDLE,
						OP };
		}

		private void LD_IND_16_nn(ushort dest_l, ushort dest_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, Z, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, dest_l, Z, W,
						IDLE,
						INC16, Z, W,
						IDLE,
						RD, dest_h, Z, W,
						IDLE,
						OP };
		}

		private void LD_8_IND_nn(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, Z, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						WR, Z, W, src,
						INC16, Z, W,
						TR, W, A,
						OP };
		}

		private void LD_IND_8_nn(ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, Z, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, dest, Z, W,
						IDLE,
						INC16, Z, W,
						OP };
		}

		private void LD_8_IND(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						TR16, Z, W, dest_l, dest_h,
						WR, Z, W, src,
						INC16, Z, W,
						TR,	W, A,					
						OP };
		}

		private void LD_8_IND_IND(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, ALU, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						IDLE,
						WR, dest_l, dest_h, ALU,
						IDLE,
						OP };
		}

		private void LD_IND_8_INC(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, dest, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						OP };
		}

		private void LD_IND_8_DEC(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, dest, src_l, src_h,
						IDLE,
						DEC16, src_l, src_h,
						IDLE,
						OP };
		}

		private void LD_IND_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, dest_l, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						RD, dest_h, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						OP };
		}

		private void INC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,					
						RD, ALU, src_l, src_h,
						IDLE,
						INC8, ALU,
						IDLE,
						WR,  src_l, src_h, ALU,
						IDLE,
						IDLE,
						OP };
		}

		private void DEC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,	
						RD, ALU, src_l, src_h,
						IDLE,
						DEC8, ALU,
						IDLE,
						WR, src_l, src_h, ALU,
						IDLE,
						IDLE,
						OP };
		}

		// NOTE: WZ implied for the wollowing 3 functions
		private void I_INT_OP(ushort operation, ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						operation, ALU,
						IDLE,
						WR, Z, W, ALU,
						IDLE,
						TR, dest, ALU,
						IDLE,
						OP };
		}

		private void I_BIT_OP(ushort operation, ushort bit, ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						operation, bit, ALU,
						IDLE,
						WR, Z, W, ALU,
						IDLE,
						TR, dest, ALU,
						IDLE,
						OP };
		}

		private void I_BIT_TE(ushort bit)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						I_BIT, bit, ALU,
						IDLE,
						OP };
		}

		private void I_OP_n(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, ALU, PCl, PCh,
						INC16, PCl, PCh,
						IDLE,
						TR16, Z, W, src_l, src_h,
						IDLE,
						ADDS, Z, W, ALU, ZERO,					
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						IDLE,
						operation, ALU,
						IDLE,
						IDLE,
						IDLE,
						WR, Z, W, ALU,
						IDLE,
						OP };
		}

		private void I_OP_n_n(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, ALU, PCl, PCh,
						INC16, PCl, PCh,
						IDLE,
						TR16, Z, W, src_l, src_h,
						IDLE,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						RD, ALU, PCl, PCh,
						INC16, PCl, PCh,
						IDLE,
						WR, Z, W, ALU,
						IDLE,
						OP };
		}

		private void I_REG_OP_IND_n(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, ALU, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						TR16, Z, W, src_l, src_h,
						IDLE,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						RD, ALU, Z, W,
						IDLE,
						operation, dest, ALU,
						IDLE,
						OP };
		}

		private void I_LD_8_IND_n(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						RD, ALU, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,					
						TR16, Z, W, dest_l, dest_h,
						IDLE,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						WR, Z, W, src,						
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						OP };
		}

		private void LD_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{RD, ALU, L, H,
						IDLE,
						WR, E, D, ALU,
						IDLE,
						operation, L, H,
						IDLE,
						operation, E, D,
						IDLE,
						DEC16, C, B,
						SET_FL_LD, 
						IDLE,
						OP_R, 0, operation, repeat_instr };
		}

		private void CP_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,						
						IDLE,
						RD, ALU, L, H,
						operation, L, H,
						IDLE,						
						IDLE,
						DEC16, C, B,
						SET_FL_CP,
						IDLE,
						operation, Z, W,
						IDLE,
						OP_R, 1, operation, repeat_instr };
		}

		private void IN_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IN, ALU, C, B,
						IDLE,
						WR, L, H, ALU,
						IDLE,
						operation, L, H,
						IDLE,
						TR16, Z, W, C, B,
						operation, Z, W,					
						IDLE,
						DEC8, B,
						IDLE,
						OP_R, 2, operation, repeat_instr };
		}

		private void OUT_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{RD, ALU, L, H,
						IDLE,
						OUT, C, B, ALU,
						IDLE,
						IDLE,
						operation, L, H,
						DEC8, B,
						IDLE,
						TR16, Z, W, C, B,
						operation, Z, W,							
						IDLE,
						OP_R, 3, operation, repeat_instr };
		}

		// this is an indirect change of a a 16 bit register with memory
		private void EXCH_16_IND_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						RD, Z, dest_l, dest_h,
						IDLE,				
						IDLE,
						I_RD, W, dest_l, dest_h, 1,
						IDLE,
						IDLE,
						WR, dest_l, dest_h, src_l,						
						IDLE,
						IDLE,
						I_WR, dest_l, dest_h, 1, src_h,
						IDLE,						
						IDLE,
						TR16, src_l, src_h, Z, W,
						IDLE,
						IDLE,						
						IDLE,
						OP };
		}
	}
}
