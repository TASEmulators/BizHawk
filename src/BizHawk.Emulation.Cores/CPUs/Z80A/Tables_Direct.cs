namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A<TLink>
	{
		// this contains the vectors of instrcution operations
		// NOTE: This list is NOT confirmed accurate for each individual cycle

		private void NOP_()
		{
			PopulateCURINSTR
				(IDLE);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		// NOTE: In a real Z80, this operation just flips a switch to choose between 2 registers
		// but it's simpler to emulate just by exchanging the register with it's shadow
		private void EXCH_()
		{
			PopulateCURINSTR
				(EXCH);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void EXX_()
		{
			PopulateCURINSTR
				(EXX);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		// this exchanges 2 16 bit registers
		private void EXCH_16_(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
				(EXCH_16, dest_l, dest_h, src_l, src_h);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void INC_16(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
				(INC16, src_l, src_h,
						IDLE,
						IDLE);

			PopulateBUSRQ(0, I, I);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
		}


		private void DEC_16(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
				(DEC16, src_l, src_h,
						IDLE,
						IDLE);

			PopulateBUSRQ(0, I, I);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
		}

		// this is done in two steps technically, but the flags don't work out using existing funcitons
		// so let's use a different function since it's an internal operation anyway
		private void ADD_16(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
				(IDLE,
						TR16, Z, W, dest_l, dest_h,
						IDLE,
						INC16, Z, W,
						IDLE,
						ADD16, dest_l, dest_h, src_l, src_h,
						IDLE,
						IDLE);

			PopulateBUSRQ(0, I, I, I, I, I, I, I);
			PopulateMEMRQ(0, 0, 0, 0, 0, 0, 0, 0);
			IRQS = 8;
		}

		private void REG_OP(ushort operation, ushort dest, ushort src)
		{
			PopulateCURINSTR
				(operation, dest, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		// Operations using the I and R registers take one T-cycle longer
		private void REG_OP_IR(ushort operation, ushort dest, ushort src)
		{
			PopulateCURINSTR
				(IDLE,
						SET_FL_IR, dest, src);

			PopulateBUSRQ(0, I);
			PopulateMEMRQ(0, 0);
			IRQS = 2;
		}

		// note: do not use DEC here since no flags are affected by this operation
		private void DJNZ_()
		{
			if ((Regs[B] - 1) != 0)
			{
				PopulateCURINSTR
				(IDLE,
							IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							IDLE,
							ASGN, W, 0,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh);

				PopulateBUSRQ(0, I, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh);
				PopulateMEMRQ(0, 0, PCh, 0, 0, 0, 0, 0, 0, 0);
				IRQS = 10;
			}
			else
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							ASGN, B, (ushort)((Regs[B] - 1) & 0xFF),
							WAIT,
							RD_INC, ALU, PCl, PCh);

				PopulateBUSRQ(0, I, PCh, 0, 0);
				PopulateMEMRQ(0, 0, PCh, 0, 0);
				IRQS = 5;
			}
		}

		private void HALT_()
		{
			PopulateCURINSTR
					(HALT);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void JR_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							ASGN, W, 0,
							IDLE,
							ADDS, PCl, PCh, Z, W,
							TR16, Z, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, PCh, PCh, PCh, PCh);
				PopulateMEMRQ(0, PCh, 0, 0, 0, 0, 0, 0, 0);
				IRQS = 9;
			}
			else
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							WAIT,
							RD_INC, ALU, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0);
				IRQS = 4;
			}
		}

		private void JP_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC_TR_PC, Z, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0);
				IRQS = 7;
			}
			else
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0);
				IRQS = 7;
			}
		}

		private void RET_()
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC_TR_PC, Z, W, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		}

		private void RETI_()
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC_TR_PC, Z, W, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		}

		private void RETN_()
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, Z, SPl, SPh,
						EI_RETN,
						WAIT,
						RD_INC_TR_PC, Z, W, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		}


		private void RET_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, SPl, SPh,
							IDLE,
							WAIT,
							RD_INC_TR_PC, Z, W, SPl, SPh);

				PopulateBUSRQ(0, I, SPh, 0, 0, SPh, 0, 0);
				PopulateMEMRQ(0, 0, SPh, 0, 0, SPh, 0, 0);
				IRQS = 8;
			}
			else
			{
				PopulateCURINSTR
					(IDLE,
							IDLE);

				PopulateBUSRQ(0, I);
				PopulateMEMRQ(0, 0);
				IRQS = 2;
			}
		}

		private void CALL_COND(bool cond)
		{
			if (cond)
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD, W, PCl, PCh,
							INC16, PCl, PCh,
							DEC16, SPl, SPh,
							WAIT,
							WR_DEC, SPl, SPh, PCh,
							IDLE,
							WAIT,
							WR_TR_PC, SPl, SPh, PCl);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, PCh, SPh, 0, 0, SPh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, 0, SPh, 0, 0, SPh, 0, 0);
				IRQS = 14;
			}
			else
			{
				PopulateCURINSTR
					(IDLE,
							IDLE,
							WAIT,
							RD_INC, Z, PCl, PCh,
							IDLE,
							WAIT,
							RD_INC, W, PCl, PCh);

				PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0);
				PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0);
				IRQS = 7;
			}
		}

		private void INT_OP(ushort operation, ushort src)
		{
			PopulateCURINSTR
					(operation, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void BIT_OP(ushort operation, ushort bit, ushort src)
		{
			PopulateCURINSTR
					(operation, bit, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void PUSH_(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, src_h,
						IDLE,
						WAIT,
						WR, SPl, SPh, src_l);

			PopulateBUSRQ(0, I, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 8;
		}


		private void POP_(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, src_l, SPl, SPh,
						IDLE,
						WAIT,
						RD_INC, src_h, SPl, SPh);

			PopulateBUSRQ(0, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 7;
		}

		private void RST_(ushort n)
		{
			PopulateCURINSTR
					(IDLE,
						DEC16, SPl, SPh,
						IDLE,
						WAIT,
						WR_DEC, SPl, SPh, PCh,
						RST, n,
						WAIT,
						WR_TR_PC, SPl, SPh, PCl);

			PopulateBUSRQ(0, I, SPh, 0, 0, SPh, 0, 0);
			PopulateMEMRQ(0, 0, SPh, 0, 0, SPh, 0, 0);
			IRQS = 8;
		}

		private void PREFIX_(ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						PREFIX);

			PRE_SRC = src;

			PopulateBUSRQ(0, PCh, 0, 0);
			PopulateMEMRQ(0, PCh, 0, 0);
			IRQS = -1; // prefix does not get interrupted
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

			PopulateCURINSTR
					(IDLE,
						IDLE,
						WAIT,
						RD_INC, ALU, PCl, PCh,
						ADDS, Z, W, ALU, ZERO,
						WAIT,
						IDLE,
						PREFIX);

			PRE_SRC = src;

			//Console.WriteLine(TotalExecutedCycles);

			PopulateBUSRQ(0, PCh, 0, 0, PCh, 0, 0, PCh);
			PopulateMEMRQ(0, PCh, 0, 0, PCh, 0, 0, 0);
			IRQS = -1; // prefetch does not get interrupted
		}

		private void DI_()
		{
			PopulateCURINSTR
					(DI);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void EI_()
		{
			PopulateCURINSTR
					(EI);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void JP_16(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(TR16, PCl, PCh, src_l, src_h);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void LD_SP_16(ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						TR16, SPl, SPh, src_l, src_h);

			PopulateBUSRQ(0, I, I);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
		}

		private void OUT_()
		{
			PopulateCURINSTR
					(IDLE,
						TR, W, A,
						WAIT,
						RD_INC, Z, PCl, PCh,
						TR, ALU, A,
						IDLE,
						WAIT,
						OUT_INC, Z, ALU, A);

			PopulateBUSRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			PopulateMEMRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			IRQS = 8;
		}

		private void OUT_REG_(ushort dest, ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						TR16, Z, W, C, B,
						IDLE,
						WAIT,
						OUT_INC, Z, W, src);

			PopulateBUSRQ(0, BIO1, BIO2, BIO3, BIO4);
			PopulateMEMRQ(0, BIO1, BIO2, BIO3, BIO4);
			IRQS = 5;
		}

		private void IN_()
		{
			PopulateCURINSTR
					(IDLE,
						TR, W, A,
						WAIT,
						RD_INC, Z, PCl, PCh,
						IDLE,
						IDLE,
						WAIT,
						IN_A_N_INC, A, Z, W);

			PopulateBUSRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			PopulateMEMRQ(0, PCh, 0, 0, WIO1, WIO2, WIO3, WIO4);
			IRQS = 8;
		}

		private void IN_REG_(ushort dest, ushort src)
		{
			PopulateCURINSTR
					(IDLE,
						TR16, Z, W, C, B,
						IDLE,
						WAIT,
						IN_INC, dest, Z, W);

			PopulateBUSRQ(0, BIO1, BIO2, BIO3, BIO4);
			PopulateMEMRQ(0, BIO1, BIO2, BIO3, BIO4);
			IRQS = 5;
		}

		private void REG_OP_16_(ushort op, ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			PopulateCURINSTR
					(IDLE,
						IDLE,
						IDLE,
						TR16, Z, W, dest_l, dest_h,
						INC16, Z, W,
						IDLE,
						IDLE,
						op, dest_l, dest_h, src_l, src_h);

			PopulateBUSRQ(0, I, I, I, I, I, I, I);
			PopulateMEMRQ(0, 0, 0, 0, 0, 0, 0, 0);
			IRQS = 8;
		}

		private void INT_MODE_(ushort src)
		{
			PopulateCURINSTR
					(INT_MODE, src);

			PopulateBUSRQ(0);
			PopulateMEMRQ(0);
			IRQS = 1;
		}

		private void RRD_()
		{
			PopulateCURINSTR
					(IDLE,
						TR16, Z, W, L, H,
						WAIT,
						RD, ALU, Z, W,
						IDLE,
						RRD, ALU, A,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						WR_INC, Z, W, ALU);

			PopulateBUSRQ(0, H, 0, 0, H, H, H, H, W, 0, 0);
			PopulateMEMRQ(0, H, 0, 0, 0, 0, 0, 0, W, 0, 0);
			IRQS = 11;
		}

		private void RLD_()
		{
			PopulateCURINSTR
					(IDLE,
						TR16, Z, W, L, H,
						WAIT,
						RD, ALU, Z, W,
						IDLE,
						RLD, ALU, A,
						IDLE,
						IDLE,
						IDLE,
						WAIT,
						WR_INC, Z, W, ALU);

			PopulateBUSRQ(0, H, 0, 0, H, H, H, H, W, 0, 0);
			PopulateMEMRQ(0, H, 0, 0, 0, 0, 0, 0, W, 0, 0);
			IRQS = 11;
		}
	}
}
