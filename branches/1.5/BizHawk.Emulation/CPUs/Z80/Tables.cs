namespace BizHawk.Emulation.CPUs.Z80 
{
	public partial class Z80A 
    {
		private void InitialiseTables() 
        {
			InitTableInc();
			InitTableDec();
			InitTableParity();
			InitTableALU();
			InitTableRotShift();
			InitTableHalfBorrow();
			InitTableHalfCarry();
			InitTableNeg();
			InitTableDaa();
		}

		private byte[] TableInc;
		private void InitTableInc() 
        {
			TableInc = new byte[256];
			for (int i = 0; i < 256; ++i)
				TableInc[i] = FlagByte(false, false, i == 0x80, UndocumentedX(i), (i & 0xF) == 0x0, UndocumentedY(i), i == 0, i > 127);
		}

		private byte[] TableDec;
		private void InitTableDec() 
        {
			TableDec = new byte[256];
			for (int i = 0; i < 256; ++i)
				TableDec[i] = FlagByte(false, true, i == 0x7F, UndocumentedX(i), (i & 0xF) == 0xF, UndocumentedY(i), i == 0, i > 127);
		}

		private bool[] TableParity;
		private void InitTableParity() 
        {
			TableParity = new bool[256];
			for (int i = 0; i < 256; ++i) 
            {
				int Bits = 0;
				for (int j = 0; j < 8; ++j) 
                {
					Bits += (i >> j) & 1;
				}
				TableParity[i] = (Bits & 1) == 0;
			}
		}

		private ushort[, , ,] TableALU;
		private void InitTableALU() 
        {
			TableALU = new ushort[8, 256, 256, 2]; // Class, OP1, OP2, Carry

			for (int i = 0; i < 8; ++i) 
            {
				for (int op1 = 0; op1 < 256; ++op1) 
                {
					for (int op2 = 0; op2 < 256; ++op2) 
                    {
						for (int c = 0; c < 2; ++c) 
                        {

							int ac = (i == 1 || i == 3) ? c : 0;

							bool S = false;
							bool Z = false;
							bool C = false;
							bool H = false;
							bool N = false;
							bool P = false;

							byte result_b = 0;
							int result_si = 0;
							int result_ui = 0;

							// Fetch result
							switch (i) 
                            {
								case 0:
								case 1:
									result_si = (sbyte)op1 + (sbyte)op2 + ac;
									result_ui = op1 + op2 + ac;
									break;
								case 2:
								case 3:
								case 7:
									result_si = (sbyte)op1 - (sbyte)op2 - ac;
									result_ui = op1 - op2 - ac;
									break;
								case 4:
									result_si = op1 & op2;
									break;
								case 5:
									result_si = op1 ^ op2;
									break;
								case 6:
									result_si = op1 | op2;
									break;
							}

							result_b = (byte)result_si;

							// Parity/Carry

							switch (i) 
                            {
								case 0:
								case 1:
								case 2:
								case 3:
								case 7:
									P = result_si < -128 || result_si > 127;
									C = result_ui < 0 || result_ui > 255;
									break;
								case 4:
								case 5:
								case 6:
									P = TableParity[result_b];
									C = false;
									break;
							}

							// Subtraction
							N = i == 2 || i == 3 || i == 7;

							// Half carry
							switch (i) 
                            {
								case 0:
								case 1:
									H = ((op1 & 0xF) + (op2 & 0xF) + (ac & 0xF)) > 0xF;
									break;
								case 2:
								case 3:
								case 7:
									H = ((op1 & 0xF) - (op2 & 0xF) - (ac & 0xF)) < 0x0;
									break;
								case 4:
									H = true;
									break;
								case 5:
								case 6:
									H = false;
									break;
							}

							// Undocumented
							byte UndocumentedFlags = (byte)(result_b & 0x28);
							if (i == 7) UndocumentedFlags = (byte)(op2 & 0x28);

							S = result_b > 127;
							Z = result_b == 0;

							if (i == 7) result_b = (byte)op1;

							TableALU[i, op1, op2, c] = (ushort)(
								result_b * 256 +
								((C ? 0x01 : 0) + (N ? 0x02 : 0) + (P ? 0x04 : 0) + (H ? 0x10 : 0) + (Z ? 0x40 : 0) + (S ? 0x80 : 0)) +
								(UndocumentedFlags));

						}
					}
				}
			}	
		}

		private bool[,] TableHalfBorrow;
		private void InitTableHalfBorrow() 
        {
			TableHalfBorrow = new bool[256, 256];
			for (int i = 0; i < 256; i++) 
            {
				for (int j = 0; j < 256; j++) 
                {
					TableHalfBorrow[i, j] = ((i & 0xF) - (j & 0xF)) < 0;
				}
			}
		}

		private bool[,] TableHalfCarry;
		private void InitTableHalfCarry() 
        {
			TableHalfCarry = new bool[256, 256];
			for (int i = 0; i < 256; i++) 
            {
				for (int j = 0; j < 256; j++) 
                {
					TableHalfCarry[i, j] = ((i & 0xF) + (j & 0xF)) > 0xF;
				}
			}
		}

		private ushort[, ,] TableRotShift;
		private void InitTableRotShift() 
        {
			TableRotShift = new ushort[2, 8, 65536]; // All, operation, AF
			for (int all = 0; all < 2; all++) 
            {
				for (int y = 0; y < 8; ++y) 
                {
					for (int af = 0; af < 65536; af++) 
                    {
						byte Old = (byte)(af >> 8);
						bool OldCarry = (af & 0x01) != 0;

						ushort newAf = (ushort)(af & ~(0x13)); // Clear HALF-CARRY, SUBTRACT and CARRY flags

						byte New = Old;
						if ((y & 1) == 0) 
                        {
							if ((Old & 0x80) != 0) ++newAf;

							New <<= 1;

							if ((y & 0x04) == 0) {
								if (((y & 0x02) == 0) ? ((newAf & 0x01) != 0) : OldCarry) New |= 0x01;
							} else {
								if ((y & 0x02) != 0) New |= 0x01;
							}

						} else {

							if ((Old & 0x01) != 0) ++newAf;

							New >>= 1;

							if ((y & 0x04) == 0) {
								if (((y & 0x02) == 0) ? ((newAf & 0x01) != 0) : OldCarry) New |= 0x80;
							} else {
								if ((y & 0x02) == 0) New |= (byte)(Old & 0x80);
							}
						}

						newAf &= 0xFF;
						newAf |= (ushort)(New * 256);

						if (all == 1) 
                        {
							newAf &= unchecked((ushort)~0xC4); // Clear S, Z, P
							if (New > 127) newAf |= 0x80;
							if (New == 0) newAf |= 0x40;
							if (TableParity[New]) newAf |= 0x04;
						}

						TableRotShift[all, y, af] = (ushort)((newAf & ~0x28) | ((newAf >> 8) & 0x28));
					}
				}
			}
		}

		private ushort[] TableNeg;
		private void InitTableNeg() 
        {
			TableNeg = new ushort[65536];
			for (int af = 0; af < 65536; af++) 
            {
				ushort raf = 0;
				byte b = (byte)(af >> 8);
				byte a = (byte)-b;
				raf |= (ushort)(a * 256);
				raf |= FlagByte(b != 0x00, true, b == 0x80, UndocumentedX(a), TableHalfCarry[a, b], UndocumentedY(a), a == 0, a > 127);
				TableNeg[af] = raf;
			}
		}

		private ushort[] TableDaa;
		private void InitTableDaa() 
        {
			TableDaa = new ushort[65536];
			for (int af = 0; af < 65536; ++af) 
            {
				byte a = (byte)(af >> 8);
				byte tmp = a;

				if (IsN(af)) 
                {
					if (IsH(af) || ((a & 0x0F) > 0x09)) tmp -= 0x06;
					if (IsC(af) || a > 0x99) tmp -= 0x60;
				} else {
					if (IsH(af) || ((a & 0x0F) > 0x09)) tmp += 0x06;
					if (IsC(af) || a > 0x99) tmp += 0x60;
				}

				TableDaa[af] = (ushort)((tmp * 256) + FlagByte(IsC(af) || a > 0x99, IsN(af), TableParity[tmp], UndocumentedX(tmp), ((a ^ tmp) & 0x10) != 0, UndocumentedY(tmp), tmp == 0, tmp > 127));
			}
		}

		private byte FlagByte(bool C, bool N, bool P, bool X, bool H, bool Y, bool Z, bool S) 
        {
			return (byte)(
				(C ? 0x01 : 0) +
				(N ? 0x02 : 0) +
				(P ? 0x04 : 0) +
				(X ? 0x08 : 0) +
				(H ? 0x10 : 0) +
				(Y ? 0x20 : 0) +
				(Z ? 0x40 : 0) +
				(S ? 0x80 : 0)
			 );
		}

		private bool UndocumentedX(int value) 
        {
			return (value & 0x08) != 0;
		}

		private bool UndocumentedY(int value) 
        {
			return (value & 0x20) != 0;
		}

		private bool IsC(int value) { return (value & 0x01) != 0; }
		private bool IsN(int value) { return (value & 0x02) != 0; }
		private bool IsP(int value) { return (value & 0x04) != 0; }
		private bool IsX(int value) { return (value & 0x08) != 0; }
		private bool IsH(int value) { return (value & 0x10) != 0; }
		private bool IsY(int value) { return (value & 0x20) != 0; }
		private bool IsZ(int value) { return (value & 0x40) != 0; }
		private bool IsS(int value) { return (value & 0x80) != 0; }
	}
}
