namespace BizHawk.Emulation.Cores.Components.M68000
{
	partial class MC68000
	{
		// TODO, the timing on AND variants is wrong. IE, and.w w/ immediate should be 8 cycles, but I cant figure out how that should work.
		private void AND0() // AND <ea>, Dn
		{
			int dstReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int srcMode = (op >> 3) & 0x07;
			int srcReg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					D[dstReg].s8 &= ReadValueB(srcMode, srcReg);
					PendingCycles -= (srcMode == 0) ? 4 : 8 + EACyclesBW[srcMode, srcReg];
					N = (D[dstReg].s8 & 0x80) != 0;
					Z = (D[dstReg].s8 == 0);
					return;
				case 1: // Word
					D[dstReg].s16 &= ReadValueW(srcMode, srcReg);
					PendingCycles -= (srcMode == 0) ? 4 : 8 + EACyclesBW[srcMode, srcReg];
					N = (D[dstReg].s16 & 0x8000) != 0;
					Z = (D[dstReg].s16 == 0);
					return;
				case 2: // Long
					D[dstReg].s32 &= ReadValueL(srcMode, srcReg);
					PendingCycles -= (srcMode == 0) ? 8 : 12 + EACyclesL[srcMode, srcReg];
					N = (D[dstReg].s32 & 0x80000000) != 0;
					Z = (D[dstReg].s32 == 0);
					return;
			}
		}

		private void AND0_Disasm(DisassemblyInfo info)
		{
			int dstReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int srcMode = (op >> 3) & 0x07;
			int srcReg = op & 0x07;

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					info.Mnemonic = "and.b";
					info.Args = $"{DisassembleValue(srcMode, srcReg, 1, ref pc)}, D{dstReg}";
					break;
				case 1: // Word
					info.Mnemonic = "and.w";
					info.Args = $"{DisassembleValue(srcMode, srcReg, 2, ref pc)}, D{dstReg}";
					break;
				case 2: // Long
					info.Mnemonic = "and.l";
					info.Args = $"{DisassembleValue(srcMode, srcReg, 4, ref pc)}, D{dstReg}";
					break;
			}

			info.Length = pc - info.PC;
		}

		private void AND1() // AND Dn, <ea>
		{
			int srcReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					{
						sbyte dest = PeekValueB(dstMode, dstReg);
						sbyte value = (sbyte)(dest & D[srcReg].s8);
						WriteValueB(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 4 : 8 + EACyclesBW[dstMode, dstReg];
						N = (value & 0x80) != 0;
						Z = (value == 0);
						return;
					}
				case 1: // Word
					{
						short dest = PeekValueW(dstMode, dstReg);
						short value = (short)(dest & D[srcReg].s16);
						WriteValueW(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 4 : 8 + EACyclesBW[dstMode, dstReg];
						N = (value & 0x8000) != 0;
						Z = (value == 0);
						return;
					}
				case 2: // Long
					{
						int dest = PeekValueL(dstMode, dstReg);
						int value = dest & D[srcReg].s32;
						WriteValueL(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesL[dstMode, dstReg];
						N = (value & 0x80000000) != 0;
						Z = (value == 0);
						return;
					}
			}
		}

		private void AND1_Disasm(DisassemblyInfo info)
		{
			int srcReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					info.Mnemonic = "and.b";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 1, ref pc)}";
					break;
				case 1: // Word
					info.Mnemonic = "and.w";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 2, ref pc)}";
					break;
				case 2: // Long
					info.Mnemonic = "and.l";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 4, ref pc)}";
					break;
			}

			info.Length = pc - info.PC;
		}

		private void ANDI() // ANDI #<data>, <ea>
		{
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					{
						sbyte imm = (sbyte)ReadWord(PC); PC += 2;
						sbyte arg = PeekValueB(dstMode, dstReg);
						sbyte result = (sbyte)(imm & arg);
						WriteValueB(dstMode, dstReg, result);
						PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesBW[dstMode, dstReg];
						N = (result & 0x80) != 0;
						Z = (result == 0);
						return;
					}
				case 1: // Word
					{
						short imm = ReadWord(PC); PC += 2;
						short arg = PeekValueW(dstMode, dstReg);
						short result = (short)(imm & arg);
						WriteValueW(dstMode, dstReg, result);
						PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesBW[dstMode, dstReg];
						N = (result & 0x8000) != 0;
						Z = (result == 0);
						return;
					}
				case 2: // Long
					{
						int imm = ReadLong(PC); PC += 4;
						int arg = PeekValueL(dstMode, dstReg);
						int result = imm & arg;
						WriteValueL(dstMode, dstReg, result);
						PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesL[dstMode, dstReg];
						N = (result & 0x80000000) != 0;
						Z = (result == 0);
						return;
					}
			}
		}

		private void ANDI_Disasm(DisassemblyInfo info)
		{
			int size = ((op >> 6) & 0x03);
			int dstMode = ((op >> 3) & 0x07);
			int dstReg = (op & 0x07);

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					{
						info.Mnemonic = "andi.b";
						sbyte imm = (sbyte)ReadWord(pc); pc += 2;
						info.Args = $"#${imm:X}, ";
						info.Args += DisassembleValue(dstMode, dstReg, 1, ref pc);
						break;
					}
				case 1: // Word
					{
						info.Mnemonic = "andi.w";
						short imm = ReadWord(pc); pc += 2;
						info.Args = $"#${imm:X}, ";
						info.Args += DisassembleValue(dstMode, dstReg, 2, ref pc);
						break;
					}
				case 2: // Long
					{
						info.Mnemonic = "andi.l";
						int imm = ReadLong(pc); pc += 4;
						info.Args = $"#${imm:X}, ";
						info.Args += DisassembleValue(dstMode, dstReg, 4, ref pc);
						break;
					}
			}

			info.Length = pc - info.PC;
		}

		private void EOR() // EOR Dn, <ea>
		{
			int srcReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					{
						sbyte dest = PeekValueB(dstMode, dstReg);
						sbyte value = (sbyte)(dest ^ D[srcReg].s8);
						WriteValueB(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 4 : 8 + EACyclesBW[dstMode, dstReg];
						N = (value & 0x80) != 0;
						Z = (value == 0);
						return;
					}
				case 1: // Word
					{
						short dest = PeekValueW(dstMode, dstReg);
						short value = (short)(dest ^ D[srcReg].s16);
						WriteValueW(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 4 : 8 + EACyclesBW[dstMode, dstReg];
						N = (value & 0x8000) != 0;
						Z = (value == 0);
						return;
					}
				case 2: // Long
					{
						int dest = PeekValueL(dstMode, dstReg);
						int value = dest ^ D[srcReg].s32;
						WriteValueL(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesL[dstMode, dstReg];
						N = (value & 0x80000000) != 0;
						Z = (value == 0);
						return;
					}
			}
		}

		private void EOR_Disasm(DisassemblyInfo info)
		{
			int srcReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					info.Mnemonic = "eor.b";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 1, ref pc)}";
					break;
				case 1: // Word
					info.Mnemonic = "eor.w";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 2, ref pc)}";
					break;
				case 2: // Long
					info.Mnemonic = "eor.l";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 4, ref pc)}";
					break;
			}

			info.Length = pc - info.PC;
		}

		private void EORI()
		{
			int size = (op >> 6) & 3;
			int mode = (op >> 3) & 7;
			int reg = (op >> 0) & 7;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					{
						sbyte immed = (sbyte)ReadWord(PC); PC += 2;
						sbyte value = (sbyte)(PeekValueB(mode, reg) ^ immed);
						WriteValueB(mode, reg, value);
						N = (value & 0x80) != 0;
						Z = value == 0;
						PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
						return;
					}
				case 1: // word
					{
						short immed = ReadWord(PC); PC += 2;
						short value = (short)(PeekValueW(mode, reg) ^ immed);
						WriteValueW(mode, reg, value);
						N = (value & 0x8000) != 0;
						Z = value == 0;
						PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
						return;
					}
				case 2: // long
					{
						int immed = ReadLong(PC); PC += 4;
						int value = PeekValueL(mode, reg) ^ immed;
						WriteValueL(mode, reg, value);
						N = (value & 0x80000000) != 0;
						Z = value == 0;
						PendingCycles -= mode == 0 ? 16 : 20 + EACyclesL[mode, reg];
						return;
					}
			}
		}

		private void EORI_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int size = (op >> 6) & 3;
			int mode = (op >> 3) & 7;
			int reg = (op >> 0) & 7;

			switch (size)
			{
				case 0: // byte
					{
						info.Mnemonic = "eori.b";
						sbyte immed = (sbyte)ReadWord(pc); pc += 2;
						info.Args = $"#${immed:X}, {DisassembleValue(mode, reg, 1, ref pc)}";
						break;
					}
				case 1: // word
					{
						info.Mnemonic = "eori.w";
						short immed = ReadWord(pc); pc += 2;
						info.Args = $"#${immed:X}, {DisassembleValue(mode, reg, 2, ref pc)}";
						break;
					}
				case 2: // long
					{
						info.Mnemonic = "eori.l";
						int immed = ReadLong(pc); pc += 4;
						info.Args = $"#${immed:X}, {DisassembleValue(mode, reg, 4, ref pc)}";
						break;
					}
			}

			info.Length = pc - info.PC;
		}

		private void OR0() // OR <ea>, Dn
		{
			int dstReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int srcMode = (op >> 3) & 0x07;
			int srcReg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					D[dstReg].s8 |= ReadValueB(srcMode, srcReg);
					PendingCycles -= (srcMode == 0) ? 4 : 8 + EACyclesBW[srcMode, srcReg];
					N = (D[dstReg].s8 & 0x80) != 0;
					Z = (D[dstReg].s8 == 0);
					return;
				case 1: // Word
					D[dstReg].s16 |= ReadValueW(srcMode, srcReg);
					PendingCycles -= (srcMode == 0) ? 4 : 8 + EACyclesBW[srcMode, srcReg];
					N = (D[dstReg].s16 & 0x8000) != 0;
					Z = (D[dstReg].s16 == 0);
					return;
				case 2: // Long
					D[dstReg].s32 |= ReadValueL(srcMode, srcReg);
					PendingCycles -= (srcMode == 0) ? 8 : 12 + EACyclesL[srcMode, srcReg];
					N = (D[dstReg].s32 & 0x80000000) != 0;
					Z = (D[dstReg].s32 == 0);
					return;
			}
		}

		private void OR0_Disasm(DisassemblyInfo info)
		{
			int dstReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int srcMode = (op >> 3) & 0x07;
			int srcReg = op & 0x07;

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					info.Mnemonic = "or.b";
					info.Args = $"{DisassembleValue(srcMode, srcReg, 1, ref pc)}, D{dstReg}";
					break;
				case 1: // Word
					info.Mnemonic = "or.w";
					info.Args = $"{DisassembleValue(srcMode, srcReg, 2, ref pc)}, D{dstReg}";
					break;
				case 2: // Long
					info.Mnemonic = "or.l";
					info.Args = $"{DisassembleValue(srcMode, srcReg, 4, ref pc)}, D{dstReg}";
					break;
			}

			info.Length = pc - info.PC;
		}

		private void OR1() // OR Dn, <ea>
		{
			int srcReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					{
						sbyte dest = PeekValueB(dstMode, dstReg);
						sbyte value = (sbyte)(dest | D[srcReg].s8);
						WriteValueB(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 4 : 8 + EACyclesBW[dstMode, dstReg];
						N = (value & 0x80) != 0;
						Z = (value == 0);
						return;
					}
				case 1: // Word
					{
						short dest = PeekValueW(dstMode, dstReg);
						short value = (short)(dest | D[srcReg].s16);
						WriteValueW(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 4 : 8 + EACyclesBW[dstMode, dstReg];
						N = (value & 0x8000) != 0;
						Z = (value == 0);
						return;
					}
				case 2: // Long
					{
						int dest = PeekValueL(dstMode, dstReg);
						int value = dest | D[srcReg].s32;
						WriteValueL(dstMode, dstReg, value);
						PendingCycles -= (dstMode == 0) ? 8 : 12 + EACyclesL[dstMode, dstReg];
						N = (value & 0x80000000) != 0;
						Z = (value == 0);
						return;
					}
			}
		}

		private void OR1_Disasm(DisassemblyInfo info)
		{
			int srcReg = (op >> 9) & 0x07;
			int size = (op >> 6) & 0x03;
			int dstMode = (op >> 3) & 0x07;
			int dstReg = op & 0x07;

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					info.Mnemonic = "or.b";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 1, ref pc)}";
					break;
				case 1: // Word
					info.Mnemonic = "or.w";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 2, ref pc)}";
					break;
				case 2: // Long
					info.Mnemonic = "or.l";
					info.Args = $"D{srcReg}, {DisassembleValue(dstMode, dstReg, 4, ref pc)}";
					break;
			}

			info.Length = pc - info.PC;
		}

		private void ORI()
		{
			int size = (op >> 6) & 3;
			int mode = (op >> 3) & 7;
			int reg = (op >> 0) & 7;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					{
						sbyte immed = (sbyte)ReadWord(PC); PC += 2;
						sbyte value = (sbyte)(PeekValueB(mode, reg) | immed);
						WriteValueB(mode, reg, value);
						N = (value & 0x80) != 0;
						Z = value == 0;
						PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
						return;
					}
				case 1: // word
					{
						short immed = ReadWord(PC); PC += 2;
						short value = (short)(PeekValueW(mode, reg) | immed);
						WriteValueW(mode, reg, value);
						N = (value & 0x8000) != 0;
						Z = value == 0;
						PendingCycles -= mode == 0 ? 8 : 12 + EACyclesBW[mode, reg];
						return;
					}
				case 2: // long
					{
						int immed = ReadLong(PC); PC += 4;
						int value = PeekValueL(mode, reg) | immed;
						WriteValueL(mode, reg, value);
						N = (value & 0x80000000) != 0;
						Z = value == 0;
						PendingCycles -= mode == 0 ? 16 : 20 + EACyclesL[mode, reg];
						return;
					}
			}
		}

		private void ORI_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int size = (op >> 6) & 3;
			int mode = (op >> 3) & 7;
			int reg = (op >> 0) & 7;

			switch (size)
			{
				case 0: // byte
					{
						info.Mnemonic = "ori.b";
						sbyte immed = (sbyte)ReadWord(pc); pc += 2;
						info.Args = $"#${immed:X}, {DisassembleValue(mode, reg, 1, ref pc)}";
						break;
					}
				case 1: // word
					{
						info.Mnemonic = "ori.w";
						short immed = ReadWord(pc); pc += 2;
						info.Args = $"#${immed:X}, {DisassembleValue(mode, reg, 2, ref pc)}";
						break;
					}
				case 2: // long
					{
						info.Mnemonic = "ori.l";
						int immed = ReadLong(pc); pc += 4;
						info.Args = $"#${immed:X}, {DisassembleValue(mode, reg, 4, ref pc)}";
						break;
					}
			}

			info.Length = pc - info.PC;
		}

		private void NOT()
		{
			int size = (op >> 6) & 0x03;
			int mode = (op >> 3) & 0x07;
			int reg = op & 0x07;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // Byte
					{
						sbyte value = PeekValueB(mode, reg);
						value = (sbyte)~value;
						WriteValueB(mode, reg, value);
						PendingCycles -= (mode == 0) ? 4 : 8 + EACyclesBW[mode, reg];
						N = (value & 0x80) != 0;
						Z = (value == 0);
						return;
					}
				case 1: // Word
					{
						short value = PeekValueW(mode, reg);
						value = (short)~value;
						WriteValueW(mode, reg, value);
						PendingCycles -= (mode == 0) ? 4 : 8 + EACyclesBW[mode, reg];
						N = (value & 0x8000) != 0;
						Z = (value == 0);
						return;
					}
				case 2: // Long
					{
						int value = PeekValueL(mode, reg);
						value = ~value;
						WriteValueL(mode, reg, value);
						PendingCycles -= (mode == 0) ? 8 : 12 + EACyclesL[mode, reg];
						N = (value & 0x80000000) != 0;
						Z = (value == 0);
						return;
					}
			}
		}

		private void NOT_Disasm(DisassemblyInfo info)
		{
			int size = (op >> 6) & 0x03;
			int mode = (op >> 3) & 0x07;
			int reg = op & 0x07;

			int pc = info.PC + 2;

			switch (size)
			{
				case 0: // Byte
					info.Mnemonic = "not.b";
					info.Args = DisassembleValue(mode, reg, 1, ref pc);
					break;
				case 1: // Word
					info.Mnemonic = "not.w";
					info.Args = DisassembleValue(mode, reg, 2, ref pc);
					break;
				case 2: // Long
					info.Mnemonic = "not.l";
					info.Args = DisassembleValue(mode, reg, 4, ref pc);
					break;
			}

			info.Length = pc - info.PC;
		}

		private void LSLd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						C = X = (D[reg].u8 & 0x80) != 0;
						D[reg].u8 <<= 1;
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].u8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						C = X = (D[reg].u16 & 0x8000) != 0;
						D[reg].u16 <<= 1;
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].u16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						C = X = (D[reg].u32 & 0x80000000) != 0;
						D[reg].u32 <<= 1;
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].u32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void LSLd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "lsl.b"; break;
				case 1: info.Mnemonic = "lsl.w"; break;
				case 2: info.Mnemonic = "lsl.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void LSRd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						C = X = (D[reg].u8 & 1) != 0;
						D[reg].u8 >>= 1;
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].u8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						C = X = (D[reg].u16 & 1) != 0;
						D[reg].u16 >>= 1;
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].u16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						C = X = (D[reg].u32 & 1) != 0;
						D[reg].u32 >>= 1;
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].u32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void LSRd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "lsr.b"; break;
				case 1: info.Mnemonic = "lsr.w"; break;
				case 2: info.Mnemonic = "lsr.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void ASLd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						bool msb = D[reg].s8 < 0;
						C = X = (D[reg].u8 & 0x80) != 0;
						D[reg].s8 <<= 1;
						V |= (D[reg].s8 < 0) != msb;
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].u8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						bool msb = D[reg].s16 < 0;
						C = X = (D[reg].u16 & 0x8000) != 0;
						D[reg].s16 <<= 1;
						V |= (D[reg].s16 < 0) != msb;
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].u16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						bool msb = D[reg].s32 < 0;
						C = X = (D[reg].u32 & 0x80000000) != 0;
						D[reg].s32 <<= 1;
						V |= (D[reg].s32 < 0) != msb;
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].u32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void ASLd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "asl.b"; break;
				case 1: info.Mnemonic = "asl.w"; break;
				case 2: info.Mnemonic = "asl.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void ASRd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						bool msb = D[reg].s8 < 0;
						C = X = (D[reg].u8 & 1) != 0;
						D[reg].s8 >>= 1;
						V |= (D[reg].s8 < 0) != msb;
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].u8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						bool msb = D[reg].s16 < 0;
						C = X = (D[reg].u16 & 1) != 0;
						D[reg].s16 >>= 1;
						V |= (D[reg].s16 < 0) != msb;
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].u16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						bool msb = D[reg].s32 < 0;
						C = X = (D[reg].u32 & 1) != 0;
						D[reg].s32 >>= 1;
						V |= (D[reg].s32 < 0) != msb;
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].u32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void ASRd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "asr.b"; break;
				case 1: info.Mnemonic = "asr.w"; break;
				case 2: info.Mnemonic = "asr.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void ROLd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u8 & 0x80) != 0;
						D[reg].u8 = (byte)((D[reg].u8 << 1) | (D[reg].u8 >> 7));
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].u8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u16 & 0x8000) != 0;
						D[reg].u16 = (ushort)((D[reg].u16 << 1) | (D[reg].u16 >> 15));
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].u16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u32 & 0x80000000) != 0;
						D[reg].u32 = ((D[reg].u32 << 1) | (D[reg].u32 >> 31));
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].u32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void ROLd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "rol.b"; break;
				case 1: info.Mnemonic = "rol.w"; break;
				case 2: info.Mnemonic = "rol.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void RORd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			V = false;
			C = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u8 & 1) != 0;
						D[reg].u8 = (byte)((D[reg].u8 >> 1) | (D[reg].u8 << 7));
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].u8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u16 & 1) != 0;
						D[reg].u16 = (ushort)((D[reg].u16 >> 1) | (D[reg].u16 << 15));
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].u16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u32 & 1) != 0;
						D[reg].u32 = ((D[reg].u32 >> 1) | (D[reg].u32 << 31));
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].u32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void RORd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "ror.b"; break;
				case 1: info.Mnemonic = "ror.w"; break;
				case 2: info.Mnemonic = "ror.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void ROXLd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			C = X;
			V = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u8 & 0x80) != 0;
						D[reg].u8 = (byte)((D[reg].u8 << 1) | (X ? 1 : 0));
						X = C;
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].s8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u16 & 0x8000) != 0;
						D[reg].u16 = (ushort)((D[reg].u16 << 1) | (X ? 1 : 0));
						X = C;
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].s16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].s32 & 0x80000000) != 0;
						D[reg].s32 = ((D[reg].s32 << 1) | (X ? 1 : 0));
						X = C;
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].s32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void ROXLd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "roxl.b"; break;
				case 1: info.Mnemonic = "roxl.w"; break;
				case 2: info.Mnemonic = "roxl.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void ROXRd()
		{
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;
			else if (m == 1) rot = D[rot].s32 & 63;

			C = X;
			V = false;

			switch (size)
			{
				case 0: // byte
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u8 & 1) != 0;
						D[reg].u8 = (byte)((D[reg].u8 >> 1) | (X ? 0x80 : 0));
						X = C;
					}
					N = (D[reg].s8 & 0x80) != 0;
					Z = D[reg].s8 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 1: // word
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].u16 & 1) != 0;
						D[reg].u16 = (ushort)((D[reg].u16 >> 1) | (X ? 0x8000 : 0));
						X = C;
					}
					N = (D[reg].s16 & 0x8000) != 0;
					Z = D[reg].s16 == 0;
					PendingCycles -= 6 + (rot * 2);
					return;
				case 2: // long
					for (int i = 0; i < rot; i++)
					{
						C = (D[reg].s32 & 1) != 0;
						D[reg].u32 = ((D[reg].u32 >> 1) | (X ? 0x80000000 : 0));
						X = C;
					}
					N = (D[reg].s32 & 0x80000000) != 0;
					Z = D[reg].s32 == 0;
					PendingCycles -= 8 + (rot * 2);
					return;
			}
		}

		private void ROXRd_Disasm(DisassemblyInfo info)
		{
			int pc = info.PC + 2;
			int rot = (op >> 9) & 7;
			int size = (op >> 6) & 3;
			int m = (op >> 5) & 1;
			int reg = op & 7;

			if (m == 0 && rot == 0) rot = 8;

			switch (size)
			{
				case 0: info.Mnemonic = "roxr.b"; break;
				case 1: info.Mnemonic = "roxr.w"; break;
				case 2: info.Mnemonic = "roxr.l"; break;
			}
			if (m == 0) info.Args = rot + ", D" + reg;
			else info.Args = "D" + rot + ", D" + reg;

			info.Length = pc - info.PC;
		}

		private void SWAP()
		{
			int reg = op & 7;
			D[reg].u32 = (D[reg].u32 << 16) | (D[reg].u32 >> 16);
			V = C = false;
			Z = D[reg].u32 == 0;
			N = (D[reg].s32 & 0x80000000) != 0;
			PendingCycles -= 4;
		}

		private void SWAP_Disasm(DisassemblyInfo info)
		{
			int reg = op & 7;
			info.Mnemonic = "swap";
			info.Args = "D" + reg;
		}
	}
}
