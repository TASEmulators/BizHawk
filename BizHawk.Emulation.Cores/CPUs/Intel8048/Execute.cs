using System;

namespace BizHawk.Emulation.Common.Components.I8048
{
	public partial class I8048
	{
		public ulong TotalExecutedCycles;

		// variables for executing instructions
		public int instr_pntr = 0;
		public ushort[] cur_instr = new ushort[60];
		public int opcode_see;

		public int IRQS;
		public int irq_pntr;

		ushort reg_d_ad;
		ushort reg_h_ad;
		ushort reg_l_ad;

		public void FetchInstruction(byte opcode)
		{
			opcode_see = opcode;
			switch (opcode)
			{
				case 0x00: OP_IMP(IDLE);							break; // NOP
				case 0x01: ILLEGAL();								break; // ILLEGAL
				case 0x02: IN_OUT_BUS(OUT);							break; // OUT BUS,A
				case 0x03: OP_A_DIR(ADD8);							break; // ADD A,#
				case 0x04: JP_2k(0);								break; // JP 2K 0
				case 0x05: OP_IMP(EI);								break; // EI
				case 0x06: ILLEGAL();								break; // ILLEGAL
				case 0x07: OP_IMP(DECA);							break; // DEC A
				case 0x08: IN_OUT_BUS(IN);							break; // IN A,BUS
				case 0x09: IN_OUT_A(IN, 1);							break; // IN A,1
				case 0x0A: IN_OUT_A(IN, 2);							break; // IN A,2
				case 0x0B: ILLEGAL();								break; // ILLEGAL
				case 0x0C: MOV_A_P4(4);								break; // MOV A,P4
				case 0x0D: MOV_A_P4(5);								break; // MOV A,P5
				case 0x0E: MOV_A_P4(6);								break; // MOV A,P6
				case 0x0F: MOV_A_P4(7);								break; // MOV A,P7
				case 0x10: OP_IR(INC_RAM, R0);						break; // INC @R0
				case 0x11: OP_IR(INC_RAM, R1);						break; // INC @R1
				case 0x12: JPB(0);									break; // JPB 0
				case 0x13: OP_A_DIR(ADC8);							break; // ADC A,#
				case 0x14: CALL(0);									break; // CALL
				case 0x15: OP_IMP(DI);								break; // DI
				case 0x16: JP_COND(TF, RES_TF);						break; // JP TF
				case 0x17: OP_IMP(INCA);							break; // INC A
				case 0x18: OP_R_IMP(INC8, R0);						break; // INC R0
				case 0x19: OP_R_IMP(INC8, R1);						break; // INC R1
				case 0x1A: OP_R_IMP(INC8, R2);						break; // INC R2
				case 0x1B: OP_R_IMP(INC8, R3);						break; // INC R3
				case 0x1C: OP_R_IMP(INC8, R4);						break; // INC R4
				case 0x1D: OP_R_IMP(INC8, R5);						break; // INC R5
				case 0x1E: OP_R_IMP(INC8, R6);						break; // INC R6
				case 0x1F: OP_R_IMP(INC8, R7);						break; // INC R7
				case 0x20: OP_IR(XCH_RAM, R0);						break; // XCH A,@R0
				case 0x21: OP_IR(XCH_RAM, R1);						break; // XCH A,@R1
				case 0x22: ILLEGAL();								break; // ILLEGAL
				case 0x23: OP_A_DIR(MOV);							break; // MOV A,#
				case 0x24: JP_2k(1);								break; // JP 2K 1
				case 0x25: OP_IMP(EN);								break; // EN
				case 0x26: JP_COND(!T0, IDLE);						break; // JP NT0
				case 0x27: OP_IMP(CLRA);							break; // CLR A
				case 0x28: OP_A_R(XCH, R0);							break; // XCH A,R0
				case 0x29: OP_A_R(XCH, R1);							break; // XCH A,R1
				case 0x2A: OP_A_R(XCH, R2);							break; // XCH A,R2
				case 0x2B: OP_A_R(XCH, R3);							break; // XCH A,R3
				case 0x2C: OP_A_R(XCH, R4);							break; // XCH A,R4
				case 0x2D: OP_A_R(XCH, R5);							break; // XCH A,R5
				case 0x2E: OP_A_R(XCH, R6);							break; // XCH A,R6
				case 0x2F: OP_A_R(XCH, R7);							break; // XCH A,R7
				case 0x30: OP_IR(XCHD_RAM, R0);						break; // XCHD A,@R0
				case 0x31: OP_IR(XCHD_RAM, R1);						break; // XCHD A,@R1
				case 0x32: JPB(1);									break; // JPB 1
				case 0x33: ILLEGAL();								break; // ILLEGAL
				case 0x34: CALL(1);									break; // CALL
				case 0x35: OP_IMP(DN);								break; // DN
				case 0x36: JP_COND(T0, IDLE);						break; // JP T0
				case 0x37: OP_IMP(COMA);							break; // COM A
				case 0x38: ILLEGAL();								break; // ILLEGAL
				case 0x39: OUT_P(1);								break; // OUT P1,A
				case 0x3A: OUT_P(2);								break; // OUT P2,A
				case 0x3B: ILLEGAL();								break; // ILLEGAL
				case 0x3C: MOV_P4_A(4);								break; // MOV P4,A
				case 0x3D: MOV_P4_A(5);								break; // MOV P5,A
				case 0x3E: MOV_P4_A(6);								break; // MOV P6,A
				case 0x3F: MOV_P4_A(7);								break; // MOV P7,A
				case 0x40: OP_A_IR(OR8, R0);						break; // OR A,@R0
				case 0x41: OP_A_IR(OR8, R1);						break; // OR A,@R1
				case 0x42: MOV_R(A, TIM);							break; // MOV A,TIM
				case 0x43: OP_A_DIR(OR8);							break; // OR A,#
				case 0x44: JP_2k(2);								break; // JP 2K 2
				case 0x45: OP_IMP(ST_CNT);							break; // START CNT
				case 0x46: JP_COND(!T1, IDLE);						break; // JP NT1
				case 0x47: OP_IMP(SWP);								break; // SWP
				case 0x48: OP_A_R(OR8, R0);							break; // OR A,R0
				case 0x49: OP_A_R(OR8, R1);							break; // OR A,R1
				case 0x4A: OP_A_R(OR8, R2);							break; // OR A,R2
				case 0x4B: OP_A_R(OR8, R3);							break; // OR A,R3
				case 0x4C: OP_A_R(OR8, R4);							break; // OR A,R4
				case 0x4D: OP_A_R(OR8, R5);							break; // OR A,R5
				case 0x4E: OP_A_R(OR8, R6);							break; // OR A,R6
				case 0x4F: OP_A_R(OR8, R7);							break; // OR A,R7
				case 0x50: OP_A_IR(AND8, R0);						break; // AND A,@R0
				case 0x51: OP_A_IR(AND8, R1);						break; // AND A,@R1
				case 0x52: JPB(2);									break; // JPB 2
				case 0x53: OP_A_DIR(AND8);							break; // AND A,#
				case 0x54: CALL(2);									break; // CALL
				case 0x55: OP_IMP(ST_T);							break; // START TIMER
				case 0x56: JP_COND(T1, IDLE);						break; // JP T1
				case 0x57: OP_IMP(DA);								break; // DA A
				case 0x58: OP_A_R(AND8, R0);						break; // AND A,R0
				case 0x59: OP_A_R(AND8, R1);						break; // AND A,R1
				case 0x5A: OP_A_R(AND8, R2);						break; // AND A,R2
				case 0x5B: OP_A_R(AND8, R3);						break; // AND A,R3
				case 0x5C: OP_A_R(AND8, R4);						break; // AND A,R4
				case 0x5D: OP_A_R(AND8, R5);						break; // AND A,R5
				case 0x5E: OP_A_R(AND8, R6);						break; // AND A,R6
				case 0x5F: OP_A_R(AND8, R7);						break; // AND A,R7
				case 0x60: OP_A_IR(ADD8, R0);						break; // ADD A,@R0
				case 0x61: OP_A_IR(ADD8, R1);						break; // ADD A,@R1
				case 0x62: MOV_R(TIM, A);							break; // MOV TIM,A
				case 0x63: ILLEGAL();								break; // ILLEGAL
				case 0x64: JP_2k(3);								break; // JP 2K 3
				case 0x65: OP_IMP(STP_CNT);							break; // STOP CNT
				case 0x66: ILLEGAL();								break; // ILLEGAL
				case 0x67: OP_IMP(RRC);								break; // RRC
				case 0x68: OP_A_R(ADD8, R0);						break; // ADD A,R0
				case 0x69: OP_A_R(ADD8, R1);						break; // ADD A,R1
				case 0x6A: OP_A_R(ADD8, R2);						break; // ADD A,R2
				case 0x6B: OP_A_R(ADD8, R3);						break; // ADD A,R3
				case 0x6C: OP_A_R(ADD8, R4);						break; // ADD A,R4
				case 0x6D: OP_A_R(ADD8, R5);						break; // ADD A,R5
				case 0x6E: OP_A_R(ADD8, R6);						break; // ADD A,R6
				case 0x6F: OP_A_R(ADD8, R7);						break; // ADD A,R7
				case 0x70: OP_A_IR(ADC8, R0);						break; // ADC A,@R0
				case 0x71: OP_A_IR(ADC8, R1);						break; // ADC A,@R1
				case 0x72: JPB(3);									break; // JPB 3
				case 0x73: ILLEGAL();								break; // ILLEGAL
				case 0x74: CALL(3);									break; // CALL
				case 0x75: OP_IMP(CLK_OUT);							break; // ENT0 CLK
				case 0x76: JP_COND(F1, IDLE);						break; // JP F1
				case 0x77: OP_IMP(ROR);								break; // ROR
				case 0x78: OP_A_R(ADC8, R0);						break; // ADC A,R0
				case 0x79: OP_A_R(ADC8, R1);						break; // ADC A,R1
				case 0x7A: OP_A_R(ADC8, R2);						break; // ADC A,R2
				case 0x7B: OP_A_R(ADC8, R3);						break; // ADC A,R3
				case 0x7C: OP_A_R(ADC8, R4);						break; // ADC A,R4
				case 0x7D: OP_A_R(ADC8, R5);						break; // ADC A,R5
				case 0x7E: OP_A_R(ADC8, R6);						break; // ADC A,R6
				case 0x7F: OP_A_R(ADC8, R7);						break; // ADC A,R7
				case 0x80: MOVX_A_R(0);								break; // MOVX A,@R0
				case 0x81: MOVX_A_R(1);								break; // MOVX A,@R1
				case 0x82: ILLEGAL();								break; // ILLEGAL
				case 0x83: RET();									break; // RET
				case 0x84: JP_2k(4);								break; // JP 2K 4
				case 0x85: OP_IMP(CL0);								break; // CLR F0
				case 0x86: JP_COND(!IRQPending, IDLE);				break; // JP !IRQ
				case 0x87: ILLEGAL();								break; // ILLEGAL
				case 0x88: OP_PB_DIR(OR8, 0);						break; // OR BUS,#
				case 0x89: OP_PB_DIR(OR8, 1);						break; // OR P1,#
				case 0x8A: OP_PB_DIR(OR8, 2);						break; // OR P2,#
				case 0x8B: ILLEGAL();								break; // ILLEGAL
				case 0x8C: OP_EXP_A(OR8, P4);						break; // OR P4,A
				case 0x8D: OP_EXP_A(OR8, P5);						break; // OR P5,A
				case 0x8E: OP_EXP_A(OR8, P6);						break; // OR P6,A
				case 0x8F: OP_EXP_A(OR8, P7);						break; // OR P7,A
				case 0x90: MOVX_R_A(0);								break; // MOVX @R0,A
				case 0x91: MOVX_R_A(1);								break; // MOVX @R1,A
				case 0x92: JPB(4);									break; // JPB 4
				case 0x93: RETR();									break; //RETR
				case 0x94: CALL(4);									break; // CALL
				case 0x95: OP_IMP(CM0);								break; // COM F0
				case 0x96: JP_COND(Regs[A] != 0, IDLE);				break; // JP (A != 0)
				case 0x97: OP_IMP(CLC);								break; // CLR C
				case 0x98: OP_PB_DIR(AND8, 0);						break; // AND BUS,#
				case 0x99: OP_PB_DIR(AND8, 1);						break; // AND P1,#
				case 0x9A: OP_PB_DIR(AND8, 2);						break; // AND P2,#
				case 0x9B: ILLEGAL();								break; // ILLEGAL
				case 0x9C: OP_EXP_A(AND8, P4);						break; // AND P4,A
				case 0x9D: OP_EXP_A(AND8, P5);						break; // AND P5,A
				case 0x9E: OP_EXP_A(AND8, P6);						break; // AND P6,A
				case 0x9F: OP_EXP_A(AND8, P7);						break; // AND P7,A
				case 0xA0: OP_IR(MOVT_RAM, R0);						break; // MOV @R0,A
				case 0xA1: OP_IR(MOVT_RAM, R1);						break; // MOV @R1,A
				case 0xA2: ILLEGAL();								break; // ILLEGAL
				case 0xA3: MOV_A_A();								break; // MOV A,@A
				case 0xA4: JP_2k(5);								break; // JP 2K 5
				case 0xA5: OP_IMP(CL1);								break; // CLR F1
				case 0xA6: ILLEGAL();								break; // ILLEGAL
				case 0xA7: OP_IMP(CMC);								break; // COM C
				case 0xA8: OP_R_IMP(MOVAR, R0);						break; // MOV R0,A
				case 0xA9: OP_R_IMP(MOVAR, R1);						break; // MOV R1,A
				case 0xAA: OP_R_IMP(MOVAR, R2);						break; // MOV R2,A
				case 0xAB: OP_R_IMP(MOVAR, R3);						break; // MOV R3,A
				case 0xAC: OP_R_IMP(MOVAR, R4);						break; // MOV R4,A
				case 0xAD: OP_R_IMP(MOVAR, R5);						break; // MOV R5,A
				case 0xAE: OP_R_IMP(MOVAR, R6);						break; // MOV R6,A
				case 0xAF: OP_R_IMP(MOVAR, R7);						break; // MOV R7,A
				case 0xB0: OP_DIR_IR(MOVT_RAM, R0);					break; // MOV @R0,#
				case 0xB1: OP_DIR_IR(MOVT_RAM, R1);					break; // MOV @R1,#
				case 0xB2: JPB(5);									break; // JPB 5
				case 0xB3: JP_A();									break; // JPP A
				case 0xB4: CALL(5);									break; // CALL
				case 0xB5: OP_IMP(CM1);								break; // COM F1
				case 0xB6: JP_COND(FlagF0, IDLE);					break; // JP F0
				case 0xB7: ILLEGAL();								break; // ILLEGAL
				case 0xB8: OP_R_DIR(MOVT, R0);						break; // MOV R0,#
				case 0xB9: OP_R_DIR(MOVT, R1);						break; // MOV R1,#
				case 0xBA: OP_R_DIR(MOVT, R2);						break; // MOV R2,#
				case 0xBB: OP_R_DIR(MOVT, R3);						break; // MOV R3,#
				case 0xBC: OP_R_DIR(MOVT, R4);						break; // MOV R4,#
				case 0xBD: OP_R_DIR(MOVT, R5);						break; // MOV R5,#
				case 0xBE: OP_R_DIR(MOVT, R6);						break; // MOV R6,#
				case 0xBF: OP_R_DIR(MOVT, R7);						break; // MOV R7,#
				case 0xC0: ILLEGAL();								break; // ILLEGAL
				case 0xC1: ILLEGAL();								break; // ILLEGAL
				case 0xC2: ILLEGAL();								break; // ILLEGAL
				case 0xC3: ILLEGAL();								break; // ILLEGAL
				case 0xC4: JP_2k(6);								break; // JP 2K 6
				case 0xC5: OP_IMP(SEL_RB0);							break; // SEL RB 0
				case 0xC6: JP_COND(Regs[A] == 0, IDLE);				break; // JP (A == 0)
				case 0xC7: MOV_R(A, PSW);							break; // MOV A,PSW
				case 0xC8: OP_R_IMP(DEC8, R0);						break; // DEC R0
				case 0xC9: OP_R_IMP(DEC8, R1);						break; // DEC R1
				case 0xCA: OP_R_IMP(DEC8, R2);						break; // DEC R2
				case 0xCB: OP_R_IMP(DEC8, R3);						break; // DEC R3
				case 0xCC: OP_R_IMP(DEC8, R4);						break; // DEC R4
				case 0xCD: OP_R_IMP(DEC8, R5);						break; // DEC R5
				case 0xCE: OP_R_IMP(DEC8, R6);						break; // DEC R6
				case 0xCF: OP_R_IMP(DEC8, R7);						break; // DEC R7
				case 0xD0: OP_A_IR(XOR8, R0);						break; // XOR A,@R0
				case 0xD1: OP_A_IR(XOR8, R1);						break; // XOR A,@R1
				case 0xD2: JPB(6);									break; // JPB 6
				case 0xD3: OP_A_DIR(XOR8);							break; // XOR A,#
				case 0xD4: CALL(6);									break; // CALL
				case 0xD5: OP_IMP(SEL_RB1);							break; // SEL RB 1
				case 0xD6: ILLEGAL();								break; // ILLEGAL
				case 0xD7: MOV_R(PSW, A);							break; // MOV PSW,A
				case 0xD8: OP_A_R(XOR8, R0);						break; // XOR A,R0
				case 0xD9: OP_A_R(XOR8, R1);						break; // XOR A,R1
				case 0xDA: OP_A_R(XOR8, R2);						break; // XOR A,R2
				case 0xDB: OP_A_R(XOR8, R3);						break; // XOR A,R3
				case 0xDC: OP_A_R(XOR8, R4);						break; // XOR A,R4
				case 0xDD: OP_A_R(XOR8, R5);						break; // XOR A,R5
				case 0xDE: OP_A_R(XOR8, R6);						break; // XOR A,R6
				case 0xDF: OP_A_R(XOR8, R7);						break; // XOR A,R7
				case 0xE0: ILLEGAL();								break; // ILLEGAL
				case 0xE1: ILLEGAL();								break; // ILLEGAL
				case 0xE2: ILLEGAL();								break; // ILLEGAL
				case 0xE3: MOV3_A_A();								break; // MOV3 A,@A
				case 0xE4: JP_2k(7);								break; // JP 2K 7
				case 0xE5: OP_IMP(SEL_MB0);							break; // SEL MB 0
				case 0xE6: JP_COND(!FlagC, IDLE);					break; // JP NC
				case 0xE7: OP_IMP(ROL);								break; // ROL
				case 0xE8: DJNZ(R0);								break; // DJNZ R0
				case 0xE9: DJNZ(R1);								break; // DJNZ R1
				case 0xEA: DJNZ(R2);								break; // DJNZ R2
				case 0xEB: DJNZ(R3);								break; // DJNZ R3
				case 0xEC: DJNZ(R4);								break; // DJNZ R4
				case 0xED: DJNZ(R5);								break; // DJNZ R5
				case 0xEE: DJNZ(R6);								break; // DJNZ R6
				case 0xEF: DJNZ(R7);								break; // DJNZ R7
				case 0xF0: OP_A_IR(MOV, R0);						break; // MOV A,@R0
				case 0xF1: OP_A_IR(MOV, R1);						break; // MOV A,@R1
				case 0xF2: JPB(7);									break; // JPB 7
				case 0xF3: ILLEGAL();								break; // ILLEGAL
				case 0xF4: CALL(7);									break; // CALL
				case 0xF5: OP_IMP(SEL_MB1);							break; // SEL MB 1
				case 0xF6: JP_COND(FlagC, IDLE);					break; // JP C
				case 0xF7: OP_IMP(RLC);								break; // RLC
				case 0xF8: OP_A_R(MOV, R0);							break; // MOV A,R0
				case 0xF9: OP_A_R(MOV, R1);							break; // MOV A,R1
				case 0xFA: OP_A_R(MOV, R2);							break; // MOV A,R2
				case 0xFB: OP_A_R(MOV, R3);							break; // MOV A,R3
				case 0xFC: OP_A_R(MOV, R4);							break; // MOV A,R4
				case 0xFD: OP_A_R(MOV, R5);							break; // MOV A,R5
				case 0xFE: OP_A_R(MOV, R6);							break; // MOV A,R6
				case 0xFF: OP_A_R(MOV, R7);							break; // MOV A,R7
			}
		}
	}
}