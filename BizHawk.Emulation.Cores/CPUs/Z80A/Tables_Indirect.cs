namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A
	{
		private void INT_OP_IND(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						IDLE,
						operation, ALU,
						WAIT,
						WR, src_l, src_h, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, src_h, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void BIT_OP_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,	
						RD, ALU, src_l, src_h,
						operation, bit, ALU,
						IDLE,
						WAIT,
						WR, src_l, src_h, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, src_h, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
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
						I_BIT, bit, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, src_h };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 1 };
		}

		private void REG_OP_IND_INC(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,					
						WAIT,					
						RD_OP, 1, ALU, src_l, src_h, operation, dest, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 1};
		}

		private void REG_OP_IND(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, src_l, src_h,
						WAIT,
						RD_OP, 1, ALU, Z, W, operation, dest, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 1 };
		}

		// different because HL doesn't effect WZ
		private void REG_OP_IND_HL(ushort operation, ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_OP, 0, ALU, L, H, operation, dest, ALU };

			BUSRQ = new ushort[] { 0, H, 0, 0 };
			MEMRQ = new ushort[] { 0, H, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 1 };
		}

		private void LD_16_IND_nn(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, W, PCl, PCh,
						IDLE,
						WAIT,
						WR_INC, Z, W, src_l,
						IDLE,
						WAIT,
						WR, Z, W, src_h };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void LD_IND_16_nn(ushort dest_l, ushort dest_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
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
						RD, dest_h, Z, W };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void LD_8_IND_nn(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, W, PCl, PCh,
						IDLE,
						WAIT,
						WR_INC_WA, Z, W, src };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void LD_IND_8_nn(ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, W, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, dest, Z, W };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void LD_8_IND(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, dest_l, dest_h,
						WAIT,
						WR_INC_WA, Z, W, src };

			BUSRQ = new ushort[] { 0, dest_h, 0, 0 };
			MEMRQ = new ushort[] { 0, dest_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 1 };
		}

		// seperate HL needed since it doesn't effect the WZ pair
		private void LD_8_IND_HL(ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						WR, L, H, src };

			BUSRQ = new ushort[] { 0, H, 0, 0 };
			MEMRQ = new ushort[] { 0, H, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 1 };
		}

		private void LD_8_IND_IND(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, ALU, src_l, src_h,
						IDLE,
						WAIT,
						WR, dest_l, dest_h, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, dest_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, dest_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
		}

		private void LD_IND_8_INC(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, dest, src_l, src_h };

			BUSRQ = new ushort[] { 0, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 1 };
		}

		private void LD_IND_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, dest_l, src_l, src_h,
						IDLE,
						WAIT,
						RD_INC, dest_h, src_l, src_h };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 1 };
		}

		private void INC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,					
						RD, ALU, src_l, src_h,
						INC8, ALU,
						IDLE,
						WAIT,
						WR,  src_l, src_h, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, src_h, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void DEC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,	
						RD, ALU, src_l, src_h,
						DEC8, ALU,
						IDLE,
						WAIT,
						WR, src_l, src_h, ALU };

			BUSRQ = new ushort[] { 0, src_h, 0, 0, src_h, src_h, 0, 0 };
			MEMRQ = new ushort[] { 0, src_h, 0, 0, 0, src_h, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
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
						TR, dest, ALU,
						WAIT,
						WR, Z, W, ALU };

			BUSRQ = new ushort[] { 0, W, 0, 0, W, W, 0, 0 };
			MEMRQ = new ushort[] { 0, W, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void I_BIT_OP(ushort operation, ushort bit, ushort dest)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, bit, ALU,
						TR, dest, ALU,
						WAIT,
						WR, Z, W, ALU };

			BUSRQ = new ushort[] { 0, W, 0, 0, W, W, 0, 0 };
			MEMRQ = new ushort[] { 0, W, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 1};
		}

		private void I_BIT_TE(ushort bit)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						I_BIT, bit, ALU };

			BUSRQ = new ushort[] { 0, W, 0, 0, W };
			MEMRQ = new ushort[] { 0, W, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 1 };
		}

		private void I_OP_n(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, PCl, PCh,
						IDLE,
						IDLE,
						TR16, Z, W, src_l, src_h,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						INC16, PCl, PCh,
						WAIT,
						RD, ALU, Z, W,
						operation, ALU,
						IDLE,
						WAIT,
						WR, Z, W, ALU };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, W, 0, 0, W, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, 0, 0, 0, 0, 0, W, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void I_OP_n_n(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						TR16, Z, W, src_l, src_h,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						ADDS, Z, W, ALU, ZERO,
						WAIT,
						RD, ALU, PCl, PCh,
						IDLE,
						IDLE,
						INC16, PCl, PCh,
						WAIT,
						WR, Z, W, ALU };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, PCh, PCh, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, PCh, 0, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void I_REG_OP_IND_n(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, PCl, PCh,
						IDLE,
						TR16, Z, W, src_l, src_h,
						IDLE,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						INC16, PCl, PCh,
						WAIT,
						RD_OP, 0, ALU, Z, W, operation, dest, ALU };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, W, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, 0, 0, 0, 0, 0, W, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void I_LD_8_IND_n(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, PCl, PCh,
						IDLE,
						IDLE,					
						TR16, Z, W, dest_l, dest_h,
						ADDS, Z, W, ALU, ZERO,
						IDLE,
						INC16, PCl, PCh,
						WAIT,
						WR, Z, W, src };

			BUSRQ = new ushort[] { 0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, Z, 0, 0 };
			MEMRQ = new ushort[] { 0, PCh, 0, 0, 0, 0, 0, 0, 0, Z, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void LD_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
					{IDLE,
					IDLE,
					WAIT,
					RD, ALU, L, H,
					operation, L, H,
					WAIT,
					WR, E, D, ALU,
					IDLE,
					SET_FL_LD_R, 0, operation, repeat_instr};

			BUSRQ = new ushort[] { 0, H, 0, 0, D, 0, 0, D, D };
			MEMRQ = new ushort[] { 0, H, 0, 0, D, 0, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void CP_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD, ALU, L, H,
						IDLE,
						DEC16, C, B,
						operation, Z, W,
						IDLE,
						SET_FL_CP_R, 1, operation, repeat_instr};

			BUSRQ = new ushort[] { 0, H, 0, 0, H, H, H, H, H };
			MEMRQ = new ushort[] { 0, H, 0, 0, 0, 0, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void IN_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						WAIT,
						WAIT,
						IN, ALU, C, B,
						IDLE,
						WAIT,
						REP_OP_I, L, H, ALU, operation, 2, operation, repeat_instr };

			BUSRQ = new ushort[] { 0, I, BIO1, BIO2, BIO3, BIO4, H, 0, 0};
			MEMRQ = new ushort[] { 0, 0, BIO1, BIO2, BIO3, BIO4, H, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		private void OUT_OP_R(ushort operation, ushort repeat_instr)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,						
						IDLE,
						WAIT,
						RD, ALU, L, H,
						IDLE,
						WAIT,
						WAIT, 
						REP_OP_O, C, B, ALU, operation, 3, operation, repeat_instr };

			BUSRQ = new ushort[] { 0, I, H, 0, 0, BIO1, BIO2, BIO3, BIO4 };
			MEMRQ = new ushort[] { 0, 0, H, 0, 0, BIO1, BIO2, BIO3, BIO4 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}

		// this is an indirect change of a a 16 bit register with memory
		private void EXCH_16_IND_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, dest_l, dest_h,
						IDLE,				
						WAIT,
						RD, W, dest_l, dest_h,
						IDLE,
						IDLE,
						WAIT,
						WR_DEC, dest_l, dest_h, src_h,
						IDLE,						
						WAIT,
						WR, dest_l, dest_h, src_l,
						IDLE,
						TR16, src_l, src_h, Z, W };

			BUSRQ = new ushort[] { 0, dest_h, 0, 0, dest_h, 0, 0, dest_h, dest_h, 0, 0, dest_h, 0, 0, dest_h, dest_h };
			MEMRQ = new ushort[] { 0, dest_h, 0, 0, dest_h, 0, 0, 0, dest_h, 0, 0, dest_h, 0, 0, 0, 0 };
			IRQS = new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
		}
	}
}
