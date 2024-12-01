using System.Text;

namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	public sealed partial class F3850<TLink>
	{
		public const int MaxInstructionLength = 48;

		public long TotalExecutedCycles;

		public int instr_pntr = 0;
		public byte[] cur_instr = new byte[MaxInstructionLength];		// fixed size - do not change at runtime
		public byte[] cur_romc = new byte[MaxInstructionLength];        // fixed size - do not change at runtime
		public byte opcode;

		public long[] dLog = new long[0xFF];
		private string debug = "";
		private void UpdateDebug()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 255; i++)
			{
				if (dLog[i] > 0)
					sb.AppendLine(i.ToString() + "\t" + dLog[i]);
				else
					sb.AppendLine();
			}

			debug = sb.ToString();
		}

		public void FetchInstruction()
		{
			switch (opcode)
			{
				case 0x00: LR_A_KU(); break;					// LR A, (KU) 
				case 0x01: LR_A_KL(); break;					// LR A, (KL) 
				case 0x02: LR_A_QU(); break;					// LR A, (QU) 
				case 0x03: LR_A_QL(); break;					// LR A, (QL) 
				case 0x04: LR_KU_A(); break;					// LR KU, (A) 
				case 0x05: LR_KL_A(); break;					// LR KL, (A) 
				case 0x06: LR_QU_A(); break;					// LR QU, (A) 
				case 0x07: LR_QL_A(); break;					// LR QL, (A) 
				case 0x08: LR_K_P(); break;						// LR K, (P) 
				case 0x09: LR_P_K(); break;						// LR P, (K) 
				case 0x0A: LR_A_IS(); break;					// LR A, (ISAR) 
				case 0x0B: LR_IS_A(); break;					// LR ISAR, (A) 
				case 0x0C: PK(); break;							// LR PC1, (PC0); LR PC0l <- (r13); LR PC0h, (r12)
				case 0x0D: LR_P0_Q(); break;					// LR PC0l, (r15); LR PC0h <- (r14)
				case 0x0E: LR_Q_DC(); break;					// LR r14, (DC0h); r15 <- (DC0l)
				case 0x0F: LR_DC_Q(); break;					// LR DC0h, (r14); DC0l <- (r15)
				case 0x10: LR_DC_H(); break;					// LR DC0h, (r10); DC0l <- (r11)
				case 0x11: LR_H_DC(); break;					// LR r10, (DC0h); r11 <- (DC0l)
				case 0x12: SR(1); break;						// Shift (A) right one bit position (zero fill)
				case 0x13: SL(1); break;						// Shift (A) left one bit position (zero fill)
				case 0x14: SR(4); break;						// Shift (A) right four bit positions (zero fill)
				case 0x15: SL(4); break;						// Shift (A) left four bit positions (zero fill)
				case 0x16: LM(); break;							// A <- ((DC0))
				case 0x17: ST(); break;							// (DC) <- (A)
				case 0x18: COM(); break;						// A <- A XOR 255 (complement A)
				case 0x19: LNK(); break;						// A <- (A) + (C)
				case 0x1A: DI(); break;							// Clear ICB
				case 0x1B: EI(); break;							// Set ICB
				case 0x1C: POP(); break;						// PC0 <- PC1
				case 0x1D: LR_W_J(); break;						// W <- (r9)
				case 0x1E: LR_J_W(); break;						// r9 <- (W)
				case 0x1F: INC(); break;						// A <- (A) + 1
				case 0x20: LI(); break;							// A <- H'aa'
				case 0x21: NI(); break;							// A <- (A) AND H'aa'
				case 0x22: OI(); break;							// A <- (A) OR H'aa'
				case 0x23: XI(); break;							// A <- (A) XOR H'aa'
				case 0x24: AI(); break;							// A <- (A) + H'aa'
				case 0x25: CI(); break;							// H'aa' + (A) + 1 (modify flags without saving result)
				case 0x26: IN(); break;							// DB <- PP; A <- (I/O Port PP)
				case 0x27: OUT(); break;						// DB <- PP; I/O Port PP <- (A)
				case 0x28: PI(); break;							// A <- H'ii'; PC1 <- (PC0) + 1; PC0l <- H'jj'; PC0h <- (A)
				case 0x29: JMP(); break;						// A <- H'ii'; PC0l <- H'jj'; PC0h <- (A)
				case 0x2A: DCI(); break;						// DC0h <- ii; increment PC0; DC0l <- jj; increment PC0
				case 0x2B: NOP(); break;						// No operation (4 cycles - fetch next opcode)
				case 0x2C: XDC(); break;                        // DC0 <-> DC1
				case 0x2D: ILLEGAL(); break;                    // No instruction - do a NOP
				case 0x2E: ILLEGAL(); break;                    // No instruction - do a NOP
				case 0x2F: ILLEGAL(); break;                    // No instruction - do a NOP


				case 0x30: DS(0); break;						// SR <- (SR) + H'FF'
				case 0x31: DS(1); break;						// SR <- (SR) + H'FF'
				case 0x32: DS(2); break;						// SR <- (SR) + H'FF'
				case 0x33: DS(3); break;						// SR <- (SR) + H'FF'
				case 0x34: DS(4); break;						// SR <- (SR) + H'FF'
				case 0x35: DS(5); break;						// SR <- (SR) + H'FF'
				case 0x36: DS(6); break;						// SR <- (SR) + H'FF'
				case 0x37: DS(7); break;						// SR <- (SR) + H'FF'
				case 0x38: DS(8); break;						// SR <- (SR) + H'FF'
				case 0x39: DS(9); break;						// SR <- (SR) + H'FF'
				case 0x3A: DS(10); break;						// SR <- (SR) + H'FF'
				case 0x3B: DS(11); break;						// SR <- (SR) + H'FF'
				case 0x3C: DS_ISAR(); break;					// SR <- (SR) + H'FF' (SR pointed to by the ISAR)
				case 0x3D: DS_ISAR_INC(); break;				// SR <- (SR) + H'FF' (SR pointed to by the ISAR); ISAR incremented
				case 0x3E: DS_ISAR_DEC(); break;                // SR <- (SR) + H'FF' (SR pointed to by the ISAR); ISAR decremented
				case 0x3F: ILLEGAL(); break;					// No instruction - do a NOP
				
				case 0x40: LR_A_R(0); break;					// A <- (SR)
				case 0x41: LR_A_R(1); break;					// A <- (SR)
				case 0x42: LR_A_R(2); break;					// A <- (SR)
				case 0x43: LR_A_R(3); break;					// A <- (SR)
				case 0x44: LR_A_R(4); break;					// A <- (SR)
				case 0x45: LR_A_R(5); break;					// A <- (SR)
				case 0x46: LR_A_R(6); break;					// A <- (SR)
				case 0x47: LR_A_R(7); break;					// A <- (SR)
				case 0x48: LR_A_R(8); break;					// A <- (SR)
				case 0x49: LR_A_R(9); break;					// A <- (SR)
				case 0x4A: LR_A_R(10); break;					// A <- (SR)
				case 0x4B: LR_A_R(11); break;					// A <- (SR)
				case 0x4C: LR_A_ISAR(); break;					// A <- (SR) (SR pointed to by the ISAR)
				case 0x4D: LR_A_ISAR_INC(); break;				// A <- (SR) (SR pointed to by the ISAR); ISAR incremented
				case 0x4E: LR_A_ISAR_DEC(); break;              // A <- (SR) (SR pointed to by the ISAR); ISAR decremented
				case 0x4F: ILLEGAL(); break;                    // No instruction - do a NOP

				case 0x50: LR_R_A(0); break;					// SR <- (A)
				case 0x51: LR_R_A(1); break;					// SR <- (A)
				case 0x52: LR_R_A(2); break;					// SR <- (A)
				case 0x53: LR_R_A(3); break;					// SR <- (A)
				case 0x54: LR_R_A(4); break;					// SR <- (A)
				case 0x55: LR_R_A(5); break;					// SR <- (A)
				case 0x56: LR_R_A(6); break;					// SR <- (A)
				case 0x57: LR_R_A(7); break;					// SR <- (A)
				case 0x58: LR_R_A(8); break;					// SR <- (A)
				case 0x59: LR_R_A(9); break;					// SR <- (A)
				case 0x5A: LR_R_A(10); break;					// SR <- (A)
				case 0x5B: LR_R_A(11); break;					// SR <- (A)
				case 0x5C: LR_ISAR_A(); break;					// SR <- (A) (SR pointed to by the ISAR)
				case 0x5D: LR_ISAR_A_INC(); break;				// SR <- (A) (SR pointed to by the ISAR); ISAR incremented
				case 0x5E: LR_ISAR_A_DEC(); break;              // SR <- (A) (SR pointed to by the ISAR); ISAR decremented
				case 0x5F: ILLEGAL(); break;                    // No instruction - do a NOP

				case 0x60: LISU(0x00); break;					// ISARU <- 0'e' (octal)
				case 0x61: LISU(0x08); break;					// ISARU <- 0'e' (octal)
				case 0x62: LISU(0x10); break;					// ISARU <- 0'e' (octal)
				case 0x63: LISU(0x18); break;					// ISARU <- 0'e' (octal)
				case 0x64: LISU(0x20); break;					// ISARU <- 0'e' (octal)
				case 0x65: LISU(0x28); break;					// ISARU <- 0'e' (octal)
				case 0x66: LISU(0x30); break;					// ISARU <- 0'e' (octal)
				case 0x67: LISU(0x38); break;					// ISARU <- 0'e' (octal)
				case 0x68: LISL(0); break;						// ISARL <- 0'e' (octal)
				case 0x69: LISL(1); break;						// ISARL <- 0'e' (octal)
				case 0x6A: LISL(2); break;						// ISARL <- 0'e' (octal)
				case 0x6B: LISL(3); break;						// ISARL <- 0'e' (octal)
				case 0x6C: LISL(4); break;						// ISARL <- 0'e' (octal)
				case 0x6D: LISL(5); break;						// ISARL <- 0'e' (octal)
				case 0x6E: LISL(6); break;						// ISARL <- 0'e' (octal)
				case 0x6F: LISL(7); break;						// ISARL <- 0'e' (octal)

				case 0x70: LIS(0); break;						// A <- H'0a'	(CLR)
				case 0x71: LIS(1); break;						// A <- H'0a'
				case 0x72: LIS(2); break;						// A <- H'0a'
				case 0x73: LIS(3); break;						// A <- H'0a'
				case 0x74: LIS(4); break;						// A <- H'0a'
				case 0x75: LIS(5); break;						// A <- H'0a'
				case 0x76: LIS(6); break;						// A <- H'0a'
				case 0x77: LIS(7); break;						// A <- H'0a'
				case 0x78: LIS(8); break;						// A <- H'0a'
				case 0x79: LIS(9); break;						// A <- H'0a'
				case 0x7a: LIS(10); break;						// A <- H'0a'
				case 0x7b: LIS(11); break;						// A <- H'0a'
				case 0x7c: LIS(12); break;						// A <- H'0a'
				case 0x7d: LIS(13); break;						// A <- H'0a'
				case 0x7e: LIS(14); break;						// A <- H'0a'
				case 0x7f: LIS(15); break;						// A <- H'0a'

				case 0x80: BT(0); break;                        // BTN		-	Branch on true - no branch (3 cycle effective NOP)
				case 0x81: BT(1); break;                        // BP		-	Branch if positive (sign bit is set)
				case 0x82: BT(2); break;                        // BC		-	Branch on carry (carry bit is set)
				case 0x83: BT(3); break;						// BT_CS	-	Branch on carry or positive
				case 0x84: BT(4); break;                        // BZ		-	Branch on zero (zero bit is set)
				case 0x85: BT(5); break;						// BT_ZS	-	Branch on zero or positive
				case 0x86: BT(6); break;						// BT_ZC	-	Branch if zero or on carry
				case 0x87: BT(7); break;						// BTZ_CS	-	Branch if zero or positive or on carry

				case 0x88: AM(); break;							// A <- (A) + ((DC0))Binary; DC0 <- (DC0) + 1
				case 0x89: AMD(); break;						// A <- (A) + ((DC0))Decimal; DC0 <- (DC0) + 1
				case 0x8A: NM(); break;							// A <- (A) AND ((DC0)); DC0 <- (DC0) + 1
				case 0x8B: OM(); break;							// A <- (A) OR ((DC0)); DC0 <- (DC0) + 1
				case 0x8C: XM(); break;							// A <- (A) XOR ((DC0)); DC0 <- (DC0) + 1
				case 0x8D: CM(); break;							// Set status flags on basis of: ((DC)) + (A) + 1; DC0 <- (DC0) + 1; DC <- (DC) + (A)
				case 0x8E: ADC(); break;						// DC <- (DC) + (A)

				case 0x8F: BR7(); break;						// Branch on ISAR (any of the low 3 bits of ISAR are reset)			
																// 	
				case 0x90: BF(0); break;						// BR		-	Unconditional branch relative (always)				
				case 0x91: BF(1); break;                        // BM		-	Branch on negative (sign bit is reset)				
				case 0x92: BF(2); break;                        // BNC		-	Branch if no carry (carry bit is reset)	
				case 0x93: BF(3); break;						// BF_CS	-	Branch on false - negative and no carry				
				case 0x94: BF(4); break;                        // BNZ		-	Branch on not zero (zero bit is reset)				
				case 0x95: BF(5); break;						// BF_ZS	-	Branch on false - negative and not zero				
				case 0x96: BF(6); break;						// BF_ZC	-	Branch on false - no carry and not zero				
				case 0x97: BF(7); break;						// BF_ZCS	-	Branch on false - no carry and not zero and negative				
				case 0x98: BF(8); break;                        // BNO		-	Branch if no overflow (OVF bit is reset)				
				case 0x99: BF(9); break;						// BF_OS	-	Branch on false - no overflow and negative				
				case 0x9A: BF(10); break;						// BF_OC	-	Branch on false - no overflow and no carry				
				case 0x9B: BF(11); break;						// BF_OCS	-	Branch on false - no overflow and no carry and negative				
				case 0x9C: BF(12); break;						// BF_OZ	-	Branch on false - no overflow and not zero				
				case 0x9D: BF(13); break;						// BF_OZS	-	Branch on false - no overflow and not zero and negative				
				case 0x9E: BF(14); break;						// BF_OZC	-	Branch on false - no overflow and not zero and no carry				
				case 0x9F: BF(15); break;						// BF_OZCS	-	Branch on false - no overflow and not zero and no carry and negative

				case 0xA0: INS_0(0); break;						// A <- (I/O Port 0 or 1)
				case 0xA1: INS_0(1); break;                     // A <- (I/O Port 0 or 1)

				case 0xA2: ILLEGAL(); break;                    // F8 Guide To Programming suggests port 3 cannot be read
				case 0xA3: ILLEGAL(); break;                    // F8 Guide To Programming suggests port 4 cannot be read

				case 0xA4: INS_1(4); break;						// DB <- Port Address (4 thru 15)
				case 0xA5: INS_1(5); break;						// DB <- Port Address (4 thru 15)
				case 0xA6: INS_1(6); break;						// DB <- Port Address (4 thru 15)
				case 0xA7: INS_1(7); break;						// DB <- Port Address (4 thru 15)
				case 0xA8: INS_1(8); break;						// DB <- Port Address (4 thru 15)
				case 0xA9: INS_1(9); break;						// DB <- Port Address (4 thru 15)
				case 0xAA: INS_1(10); break;					// DB <- Port Address (4 thru 15)
				case 0xAB: INS_1(11); break;					// DB <- Port Address (4 thru 15)
				case 0xAC: INS_1(12); break;					// DB <- Port Address (4 thru 15)
				case 0xAD: INS_1(13); break;					// DB <- Port Address (4 thru 15)
				case 0xAE: INS_1(14); break;					// DB <- Port Address (4 thru 15)
				case 0xAF: INS_1(15); break;					// DB <- Port Address (4 thru 15)

				case 0xB0: OUTS_0(0); break;					// I/O Port 0 or 1 <- (A)
				case 0xB1: OUTS_0(1); break;                    // I/O Port 0 or 1 <- (A)

				case 0xB2: ILLEGAL(); break;                    // F8 Guide To Programming suggests port 3 cannot be written to
				case 0xB3: ILLEGAL(); break;                    // F8 Guide To Programming suggests port 4 cannot be written to

				case 0xB4: OUTS_1(4); break;					// DB <- Port Address (4 thru 15)
				case 0xB5: OUTS_1(5); break;					// DB <- Port Address (4 thru 15)
				case 0xB6: OUTS_1(6); break;					// DB <- Port Address (4 thru 15)
				case 0xB7: OUTS_1(7); break;					// DB <- Port Address (4 thru 15)
				case 0xB8: OUTS_1(8); break;					// DB <- Port Address (4 thru 15)
				case 0xB9: OUTS_1(9); break;					// DB <- Port Address (4 thru 15)
				case 0xBA: OUTS_1(10); break;					// DB <- Port Address (4 thru 15)
				case 0xBB: OUTS_1(11); break;					// DB <- Port Address (4 thru 15)
				case 0xBC: OUTS_1(12); break;					// DB <- Port Address (4 thru 15)
				case 0xBD: OUTS_1(13); break;					// DB <- Port Address (4 thru 15)
				case 0xBE: OUTS_1(14); break;					// DB <- Port Address (4 thru 15)
				case 0xBF: OUTS_1(15); break;					// DB <- Port Address (4 thru 15)

				case 0xC0: AS(0); break;						// A <- (A) + (r) Binary
				case 0xC1: AS(1); break;						// A <- (A) + (r) Binary
				case 0xC2: AS(2); break;						// A <- (A) + (r) Binary
				case 0xC3: AS(3); break;						// A <- (A) + (r) Binary
				case 0xC4: AS(4); break;						// A <- (A) + (r) Binary
				case 0xC5: AS(5); break;						// A <- (A) + (r) Binary
				case 0xC6: AS(6); break;						// A <- (A) + (r) Binary
				case 0xC7: AS(7); break;						// A <- (A) + (r) Binary
				case 0xC8: AS(8); break;						// A <- (A) + (r) Binary
				case 0xC9: AS(9); break;						// A <- (A) + (r) Binary
				case 0xCA: AS(10); break;						// A <- (A) + (r) Binary
				case 0xCB: AS(11); break;						// A <- (A) + (r) Binary
				case 0xCC: AS_IS(); break;						// A <- (A) + (r addressed via ISAR) Binary
				case 0xCD: AS_IS_INC(); break;					// A <- (A) + (r addressed via ISAR) Binary; Increment ISAR
				case 0xCE: AS_IS_DEC(); break;                  // A <- (A) + (r addressed via ISAR) Binary; Decrement ISAR
				case 0xCF: ILLEGAL(); break;                    // No instruction - do a NOP

				case 0xD0: ASD(0); break;						// A <- (A) + (r) Decimal
				case 0xD1: ASD(1); break;						// A <- (A) + (r) Decimal
				case 0xD2: ASD(2); break;						// A <- (A) + (r) Decimal
				case 0xD3: ASD(3); break;						// A <- (A) + (r) Decimal
				case 0xD4: ASD(4); break;						// A <- (A) + (r) Decimal
				case 0xD5: ASD(5); break;						// A <- (A) + (r) Decimal
				case 0xD6: ASD(6); break;						// A <- (A) + (r) Decimal
				case 0xD7: ASD(7); break;						// A <- (A) + (r) Decimal
				case 0xD8: ASD(8); break;						// A <- (A) + (r) Decimal
				case 0xD9: ASD(9); break;						// A <- (A) + (r) Decimal
				case 0xDA: ASD(10); break;						// A <- (A) + (r) Decimal
				case 0xDB: ASD(11); break;						// A <- (A) + (r) Decimal
				case 0xDC: ASD_IS(); break;						// A <- (A) + (r addressed via ISAR) Decimal
				case 0xDD: ASD_IS_INC(); break;					// A <- (A) + (r addressed via ISAR) Decimal; Increment ISAR
				case 0xDE: ASD_IS_DEC(); break;                 // A <- (A) + (r addressed via ISAR) Decimal; Decrement ISAR
				case 0xDF: ILLEGAL(); break;                    // No instruction - do a NOP

				case 0xE0: XS(0); break;						// A <- (A) XOR (r)
				case 0xE1: XS(1); break;						// A <- (A) XOR (r)
				case 0xE2: XS(2); break;						// A <- (A) XOR (r)
				case 0xE3: XS(3); break;						// A <- (A) XOR (r)
				case 0xE4: XS(4); break;						// A <- (A) XOR (r)
				case 0xE5: XS(5); break;						// A <- (A) XOR (r)
				case 0xE6: XS(6); break;						// A <- (A) XOR (r)
				case 0xE7: XS(7); break;						// A <- (A) XOR (r)
				case 0xE8: XS(8); break;						// A <- (A) XOR (r)
				case 0xE9: XS(9); break;						// A <- (A) XOR (r)
				case 0xEA: XS(10); break;						// A <- (A) XOR (r)
				case 0xEB: XS(11); break;						// A <- (A) XOR (r)
				case 0xEC: XS_IS(); break;						// A <- (A) XOR (r addressed via ISAR)
				case 0xED: XS_IS_INC(); break;					// A <- (A) XOR (r addressed via ISAR); Increment ISAR
				case 0xEE: XS_IS_DEC(); break;                  // A <- (A) XOR (r addressed via ISAR); Decrement ISAR
				case 0xEF: ILLEGAL(); break;                    // No instruction - do a NOP

				case 0xF0: NS(0); break;						// A <- (A) AND (r)
				case 0xF1: NS(1); break;                        // A <- (A) AND (r)
				case 0xF2: NS(2); break;                        // A <- (A) AND (r)
				case 0xF3: NS(3); break;                        // A <- (A) AND (r)
				case 0xF4: NS(4); break;                        // A <- (A) AND (r)
				case 0xF5: NS(5); break;                        // A <- (A) AND (r)
				case 0xF6: NS(6); break;                        // A <- (A) AND (r)
				case 0xF7: NS(7); break;                        // A <- (A) AND (r)
				case 0xF8: NS(8); break;                        // A <- (A) AND (r)
				case 0xF9: NS(9); break;                        // A <- (A) AND (r)
				case 0xFA: NS(10); break;                       // A <- (A) AND (r)
				case 0xFB: NS(11); break;                       // A <- (A) AND (r)
				case 0xFC: NS_IS(); break;                      // A <- (A) AND (r addressed via ISAR)
				case 0xFD: NS_IS_INC(); break;                  // A <- (A) AND (r addressed via ISAR); Increment ISAR
				case 0xFE: NS_IS_DEC(); break;                  // A <- (A) AND (r addressed via ISAR); Decrement ISAR
				case 0xFF: ILLEGAL(); break;                    // No instruction - do a NOP
			}
		}
	}
}
