using System;

namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		// this contains the vectors of instrcution operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		private void NOP_()
		{
			cur_instr = new ushort[]
						{IDLE,					
						IDLE,
						HALT_CHK,
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
						IDLE,
						HALT_CHK,
						OP };
		}


		private void DEC_16(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						DEC16, src_l, src_h,
						IDLE,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void ADD_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
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
			cur_instr = new ushort[]
						{operation, dest, src,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void STOP_()
		{
			cur_instr = new ushort[]
						{RD, Z, PCl, PCh,
						INC16, PCl, PCh,
						IDLE,
						STOP };
		}

		private void HALT_()
		{
			if (FlagI && (EI_pending == 0) && !interrupts_enabled)
			{
				if (is_GBC)
				{
					// in GBC mode, the HALT bug is worked around by simply adding a NOP
					// so it just takes 4 cycles longer to reach the next instruction
					cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							OP_G};
				}
				else
				{	// if interrupts are disabled,
					// a glitchy decrement to the program counter happens
					{
						cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							OP_G};
					}
				}
			}
			else
			{
				cur_instr = new ushort[]
						{
						IDLE,						
						HALT_CHK,
						IDLE,
						HALT, 0 };

				if (!is_GBC) { skip_once = true; }
				// If the interrupt flag is not currently set, but it does get set in the first check
				// then a bug is triggered 
				// With interrupts enabled, this runs the halt command twice 
				// when they are disabled, it reads the next byte twice
				if (!FlagI ||(FlagI && !interrupts_enabled)) { Halt_bug_2 = true; }
				
			}
		}

		private void JR_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							RD, W, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							ASGN, Z, 0,
							IDLE,
							ADDS, PCl, PCh, W, Z,
							HALT_CHK,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							RD, Z, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							HALT_CHK,
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
							IDLE,
							RD, W, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							RD, Z, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							TR, PCl, W,
							IDLE,
							TR, PCh, Z,
							HALT_CHK,
							OP };
			}
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							RD, W, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							RD, Z, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							HALT_CHK,
							OP };
			}
		}

		private void RET_()
		{
			cur_instr = new ushort[]
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
			cur_instr = new ushort[]
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


		private void RET_COND(bool cond)
		{
			if (cond)
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
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
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							HALT_CHK,
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
							IDLE,
							RD, W, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,							
							IDLE,
							RD, Z, PCl, PCh,
							INC16, PCl, PCh,
							IDLE,
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
			else
			{
				cur_instr = new ushort[]
							{IDLE,
							IDLE,
							IDLE,
							RD, W, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							IDLE,
							RD, Z, PCl, PCh,
							IDLE,
							INC16, PCl, PCh,
							HALT_CHK,
							OP };
			}
		}

		private void INT_OP(ushort operation, ushort src)
		{
			cur_instr = new ushort[]
						{operation, src,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			cur_instr = new ushort[]
						{operation, bit, src,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			cur_instr = new ushort[]
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
				cur_instr = new ushort[]
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
				cur_instr = new ushort[]
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
			cur_instr = new ushort[]
						{PREFIX,
						IDLE,
						IDLE,
						OP };
		}

		private void DI_()
		{
			cur_instr = new ushort[]
						{DI,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void EI_()
		{
			cur_instr = new ushort[]
						{EI,
						IDLE,
						HALT_CHK,
						OP };
		}

		private void JP_HL()
		{
			cur_instr = new ushort[]
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
			cur_instr = new ushort[]
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
			cur_instr = new ushort[]
						{JAM,
						IDLE,
						IDLE,
						IDLE };
		}
	}
}
