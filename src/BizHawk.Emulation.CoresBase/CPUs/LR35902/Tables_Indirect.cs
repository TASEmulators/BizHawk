namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		private void INT_OP_IND(ushort operation, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, Z, src_l, src_h,
						IDLE,
						operation, Z,
						IDLE,
						WR, src_l, src_h, Z,
						IDLE,					
						IDLE,
						HALT_CHK,
						OP };
		}

		private void BIT_OP_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,	
						RD, Z, src_l, src_h,
						IDLE,
						operation, bit, Z,
						IDLE,
						WR, src_l, src_h, Z,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void BIT_TE_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, Z, src_l, src_h,
						IDLE,
						operation, bit, Z,
						HALT_CHK,
						OP };
		}

		private void REG_OP_IND_INC(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,					
						IDLE,					
						IDLE,
						RD, Z, src_l, src_h,
						operation, dest, Z,
						INC16, src_l, src_h,
						HALT_CHK,
						OP };
		}

		private void REG_OP_IND(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, Z, src_l, src_h,
						IDLE,
						operation, dest, Z,
						HALT_CHK,
						OP };
		}

		private void LD_R_IM(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						IDLE,
						RD, Z, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						IDLE,
						WR, W, Z, dest_l,
						IDLE,
						INC16, W, Z,
						IDLE,
						WR, W, Z, dest_h,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_8_IND_INC(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						WR, dest_l, dest_h, src,
						IDLE,
						INC16, dest_l, dest_h,
						HALT_CHK,
						OP };
		}

		private void LD_8_IND_DEC(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						WR, dest_l, dest_h, src,
						IDLE,
						DEC16, dest_l, dest_h,
						HALT_CHK,
						OP };
		}

		private void LD_8_IND(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						WR, dest_l, dest_h, src,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_8_IND_IND(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, Z, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						IDLE,
						WR, dest_l, dest_h, Z,
						IDLE,
						IDLE,
						HALT_CHK,
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
						HALT_CHK,
						OP };
		}

		private void LD_IND_8_INC_HL(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, dest, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						HALT_CHK,
						OP };
		}

		private void LD_IND_8_DEC_HL(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, dest, src_l, src_h,
						IDLE,
						DEC16, src_l, src_h,
						HALT_CHK,
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
						IDLE,
						RD, dest_h, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						HALT_CHK,
						OP };
		}

		private void INC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,					
						RD, Z, src_l, src_h,
						IDLE,
						INC8, Z,
						IDLE,
						WR,  src_l, src_h, Z,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void DEC_8_IND(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,	
						RD, Z, src_l, src_h,
						IDLE,
						DEC8, Z,
						IDLE,
						WR, src_l, src_h, Z,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}


		private void LD_8_IND_FF(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,					
						IDLE,					
						IDLE,
						RD, W, src_l, src_h,
						INC16, src_l, src_h,
						IDLE,
						ASGN, Z , 0xFF,
						RD, dest, W, Z,
						IDLE,					
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_FF_IND_8(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, dest_l, dest_h,
						INC16, dest_l, dest_h,
						IDLE,
						ASGN, Z , 0xFF,
						WR, W, Z, src,
						IDLE,		
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_8_IND_FFC(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						ASGN, Z , 0xFF,
						RD, dest, C, Z,
						IDLE,	
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_FFC_IND_8(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						ASGN, Z , 0xFF,
						WR, C, Z, src,
						IDLE,					
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_16_IND_FF(ushort dest, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						IDLE,
						RD, Z, src_l, src_h,
						IDLE,
						INC16, src_l, src_h,
						IDLE,
						RD, dest, W, Z,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_FF_IND_16(ushort dest_l, ushort dest_h, ushort src)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, dest_l, dest_h,
						IDLE,
						INC16, dest_l, dest_h,
						IDLE,
						RD, Z, dest_l, dest_h,
						IDLE,
						INC16, dest_l, dest_h,
						IDLE,
						WR, W, Z, src,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}
	}
}
