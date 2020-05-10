namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		// this contains the vectors of instruction operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		private void NOP_()
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void INC_16(ushort srcL, ushort srcH)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						INC16,
						srcL,
						srcH,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void DEC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						DEC16,
						src_l,
						src_h,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void ADD_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						ADD16, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void REG_OP(ushort operation, ushort dest, ushort src)
		{
			cur_instr = new[]
						{operation, dest, src,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void STOP_()
		{
			cur_instr = new[]
						{RD, Z, PCl, PCh,
						INC16, PCl, PCh,
						IDLE,
						STOP };
		}

		private void HALT_()
		{
			cur_instr = new ushort[]
						{HALT_FUNC,
						IDLE,
						IDLE,
						OP_G,
						HALT_CHK,
						IDLE,
						HALT, 0};
		}

		private void JR_COND(ushort cond)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, PCl, PCh,
						INC16, PCl, PCh,
						COND_CHECK, cond, (ushort)0,							
						IDLE,
						ASGN, Z, 0,
						IDLE,
						ADDS, PCl, PCh, W, Z,
						HALT_CHK,
						OP };
		}

		private void JP_COND(ushort cond)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						RD, Z, PCl, PCh,
						INC16, PCl, PCh,
						COND_CHECK, cond, (ushort)1,
						IDLE,
						TR, PCl, W,
						IDLE,
						TR, PCh, Z,
						HALT_CHK,
						OP };
		}

		private void RET_()
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						RD, PCl, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						RD, PCh, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void RETI_()
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						RD, PCl, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						RD, PCh, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						EI_RETI,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}


		private void RET_COND(ushort cond)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						COND_CHECK, cond, (ushort)2,
						IDLE,
						RD, PCl, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						RD, PCh, SPl, SPh,
						IDLE,
						INC16, SPl, SPh,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void CALL_COND(ushort cond)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, PCl, PCh,
						INC16, PCl, PCh,
						IDLE,							
						IDLE,
						RD, Z, PCl, PCh,
						INC16, PCl, PCh,
						COND_CHECK, cond, (ushort)3,
						DEC16, SPl, SPh,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						WR, SPl, SPh, PCh,
						IDLE,							
						IDLE,
						DEC16, SPl, SPh,
						WR, SPl, SPh, PCl,				
						TR, PCl, W,
						TR, PCh, Z,
						HALT_CHK,
						OP };
		}

		private void INT_OP(ushort operation, ushort src)
		{
			cur_instr = new[]
						{operation, src,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			cur_instr = new[]
						{operation, bit, src,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, src_h,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WR, SPl, SPh, src_l,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		// NOTE: this is the only instruction that can write to F
		// but the bottom 4 bits of F are always 0, so instead of putting a special check for every read op
		// let's just put a special operation here specifically for F
		private void POP_(ushort src_l, ushort src_h)
		{
			if (src_l != F)
			{
				cur_instr = new[]
							{IDLE,
							IDLE,
							IDLE,
							RD, src_l, SPl, SPh,
							IDLE,
							INC16, SPl, SPh,
							IDLE,
							RD, src_h, SPl, SPh,
							IDLE,
							INC16, SPl, SPh,
							HALT_CHK,
							OP };
			}
			else
			{
				cur_instr = new[]
							{IDLE,
							IDLE,
							IDLE,
							RD_F, src_l, SPl, SPh,
							IDLE,
							INC16, SPl, SPh,
							IDLE,
							RD, src_h, SPl, SPh,
							IDLE,
							INC16, SPl, SPh,
							HALT_CHK,
							OP };
			} 
		}

		private void RST_(ushort n)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						DEC16, SPl, SPh,
						IDLE,						
						IDLE,					
						IDLE,
						WR, SPl, SPh, PCh,
						DEC16, SPl, SPh,
						IDLE,				
						IDLE,
						WR, SPl, SPh, PCl,
						ASGN, PCh, 0,
						ASGN, PCl, n,
						HALT_CHK,
						OP };
		}

		private void PREFIX_()
		{
			cur_instr = new[]
						{PREFIX,
						IDLE,
						IDLE,
						OP };
		}

		private void DI_()
		{
			cur_instr = new[]
						{DI,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void EI_()
		{
			cur_instr = new[]
						{EI,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void JP_HL()
		{
			cur_instr = new[]
						{TR, PCl, L,
						TR, PCh, H,
						HALT_CHK,
						OP };
		}

		private void ADD_SP()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						IDLE,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						IDLE,
						ASGN, Z, 0,
						IDLE,
						ADDS, SPl, SPh, W, Z,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void LD_SP_HL()
		{
			cur_instr = new[]
						{IDLE,
						IDLE,
						IDLE,
						TR, SPl, L,
						IDLE,
						TR, SPh, H,
						HALT_CHK,
						OP };
		}

		private void LD_HL_SPn()
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						RD, W, PCl, PCh,
						IDLE,
						INC16, PCl, PCh,
						TR, H, SPh,
						TR, L, SPl,
						ASGN, Z, 0,
						ADDS, L, H, W, Z,
						HALT_CHK,
						OP };
		}

		private void JAM_()
		{
			cur_instr = new[]
						{JAM,
						IDLE,
						IDLE,
						IDLE };
		}
	}
}
