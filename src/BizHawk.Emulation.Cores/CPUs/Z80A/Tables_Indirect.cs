namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A<TLink>
	{
		private void INT_OP_IND(ushort operation, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						IDLE,
						operation, ALU,
						WAIT,
						WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		}

		private void BIT_OP_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						operation, bit, ALU,
						IDLE,
						WAIT,
						WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		}

		// Note that this operation uses I_BIT, same as indexed BIT.
		// This is where the strange behaviour in Flag bits 3 and 5 come from.
		// normally WZ contain I* + n when doing I_BIT ops, but here we use that code path 
		// even though WZ is not assigned to, letting it's value from other operations show through
		private void BIT_TE_IND(ushort operation, ushort bit, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						I_BIT, bit, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h);
			PopulateMEMRQ(0, src_h, 0, 0, 0);
			IRQS = 5;
		}

		private void REG_OP_IND_INC(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_OP, 1, ALU, src_l, src_h, operation, dest, ALU);

			PopulateBUSRQ(0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0);
			IRQS = 4;
		}

		private void REG_OP_IND(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						TR16, Z, W, src_l, src_h,
						WAIT,
						RD_OP, 1, ALU, Z, W, operation, dest, ALU);

			PopulateBUSRQ(0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0);
			IRQS = 4;
		}

		// different because HL doesn't effect WZ
		private void REG_OP_IND_HL(ushort operation, ushort dest)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_OP, 0, ALU, L, H, operation, dest, ALU);

			PopulateBUSRQ(0, H, 0, 0);
			PopulateMEMRQ(0, H, 0, 0);
			IRQS = 4;
		}

		private void LD_16_IND_nn(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
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
						WR, Z, W, src_h);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			IRQS = 13;
		}

		private void LD_IND_16_nn(ushort dest_l, ushort dest_h)
		{
			PopulateCURINSTR
					(IDLE,
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
						RD, dest_h, Z, W);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0, W, 0, 0);
			IRQS = 13;
		}

		private void LD_8_IND_nn(ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, W, PCl, PCh,
						IDLE,
						WAIT,
						WR_INC_WA, Z, W, src);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			IRQS = 10;
		}

		private void LD_IND_8_nn(ushort dest)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, W, PCl, PCh,
						IDLE,
						WAIT,
						RD_INC, dest, Z, W);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, W, 0, 0);
			IRQS = 10;
		}

		private void LD_8_IND(ushort dest_l, ushort dest_h, ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						TR16, Z, W, dest_l, dest_h,
						WAIT,
						WR_INC_WA, Z, W, src);

			PopulateBUSRQ(0, dest_h, 0, 0);
			PopulateMEMRQ(0, dest_h, 0, 0);
			IRQS = 4;
		}

		// seperate HL needed since it doesn't effect the WZ pair
		private void LD_8_IND_HL(ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						WR, L, H, src);

			PopulateBUSRQ(0, H, 0, 0);
			PopulateMEMRQ(0, H, 0, 0);
			IRQS = 4;
		}

		private void LD_8_IND_IND(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, ALU, src_l, src_h,
						IDLE,
						WAIT,
						WR, dest_l, dest_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, dest_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, dest_h, 0, 0);
			IRQS = 7;
		}

		private void LD_IND_8_INC(ushort dest, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, dest, src_l, src_h);

			PopulateBUSRQ(0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0);
			IRQS = 4;
		}

		private void LD_IND_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, dest_l, src_l, src_h,
						IDLE,
						WAIT,
						RD_INC, dest_h, src_l, src_h);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, src_h, 0, 0);
			IRQS = 7;
		}

		private void INC_8_IND(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						INC8, ALU,
						IDLE,
						WAIT,
						WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		}

		private void DEC_8_IND(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, src_l, src_h,
						DEC8, ALU,
						IDLE,
						WAIT,
						WR, src_l, src_h, ALU);

			PopulateBUSRQ(0, src_h, 0, 0, src_h, src_h, 0, 0);
			PopulateMEMRQ(0, src_h, 0, 0, 0, src_h, 0, 0);
			IRQS = 8;
		}

		// NOTE: WZ implied for the wollowing 3 functions
		private void I_INT_OP(ushort operation, ushort dest)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, ALU,
						TR, dest, ALU,
						WAIT,
						WR, Z, W, ALU);

			PopulateBUSRQ(0, W, 0, 0, W, W, 0, 0);
			PopulateMEMRQ(0, W, 0, 0, 0, W, 0, 0);
			IRQS = 8;
		}

		private void I_BIT_OP(ushort operation, ushort bit, ushort dest)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						operation, bit, ALU,
						TR, dest, ALU,
						WAIT,
						WR, Z, W, ALU);

			PopulateBUSRQ(0, W, 0, 0, W, W, 0, 0);
			PopulateMEMRQ(0, W, 0, 0, 0, W, 0, 0);
			IRQS = 8;
		}

		private void I_BIT_TE(ushort bit)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, Z, W,
						I_BIT, bit, ALU);

			PopulateBUSRQ(0, W, 0, 0, W);
			PopulateMEMRQ(0, W, 0, 0, 0);
			IRQS = 5;
		}

		private void I_OP_n(ushort operation, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
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
						WR, Z, W, ALU);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, W, 0, 0, W, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0, W, 0, 0, 0, W, 0, 0);
			IRQS = 16;
		}

		private void I_OP_n_n(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
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
						WR, Z, W, ALU);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, PCh, PCh, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, 0, 0, W, 0, 0);
			IRQS = 12;
		}

		private void I_REG_OP_IND_n(ushort operation, ushort dest, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
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
						RD_OP, 0, ALU, Z, W, operation, dest, ALU);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, W, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0, W, 0, 0);
			IRQS = 12;
		}

		private void I_LD_8_IND_n(ushort dest_l, ushort dest_h, ushort src)
		{
			PopulateCURINSTR
					(IDLE,
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
						WR, Z, W, src);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh, Z, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0, Z, 0, 0);
			IRQS = 12;
		}

		private void LD_OP_R(ushort operation, ushort repeat_instr)
		{
			PopulateCURINSTR
					(IDLE,
					IDLE,
					WAIT,
					RD, ALU, L, H,
					operation, L, H,
					WAIT,
					WR, E, D, ALU,
					IDLE,
					SET_FL_LD_R, 0, operation, repeat_instr);

			PopulateBUSRQ(0, H, 0, 0, D, 0, 0, D, D);
			PopulateMEMRQ(0, H, 0, 0, D, 0, 0, 0, 0);
			IRQS = 9;
		}

		private void CP_OP_R(ushort operation, ushort repeat_instr)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD, ALU, L, H,
						IDLE,
						DEC16, C, B,
						operation, Z, W,
						IDLE,
						SET_FL_CP_R, 1, operation, repeat_instr);

			PopulateBUSRQ(0, H, 0, 0, H, H, H, H, H);
			PopulateMEMRQ(0, H, 0, 0, 0, 0, 0, 0, 0);
			IRQS = 9;
		}

		private void IN_OP_R(ushort operation, ushort repeat_instr)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						IN, ALU, C, B,
						IDLE,
						WAIT,
						REP_OP_I, L, H, ALU, operation, 2, operation, repeat_instr);

			PopulateBUSRQ(0, I, BIO1, BIO2, BIO3, BIO4, H, 0, 0);
			PopulateMEMRQ(0, 0, BIO1, BIO2, BIO3, BIO4, H, 0, 0);
			IRQS = 9;
		}

		private void OUT_OP_R(ushort operation, ushort repeat_instr)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IDLE,
						WAIT,
						RD, ALU, L, H,
						IDLE,
						IDLE,
						WAIT,
						REP_OP_O, C, B, ALU, operation, 3, operation, repeat_instr);

			PopulateBUSRQ(0, I, H, 0, 0, BIO1, BIO2, BIO3, BIO4);
			PopulateMEMRQ(0, 0, H, 0, 0, BIO1, BIO2, BIO3, BIO4);
			IRQS = 9;
		}

		// this is an indirect change of a a 16 bit register with memory
		private void EXCH_16_IND_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
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
						TR16, src_l, src_h, Z, W);

			PopulateBUSRQ(0, dest_h, 0, 0, dest_h, 0, 0, dest_h, dest_h, 0, 0, dest_h, 0, 0, dest_h, dest_h);
			PopulateMEMRQ(0, dest_h, 0, 0, dest_h, 0, 0, 0, dest_h, 0, 0, dest_h, 0, 0, 0, 0);
			IRQS = 16;
		}
	}
}
