using System;

namespace BizHawk.Emulation.Cores.Components.MC6800
{
	public partial class MC6800
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
				case 0x00: ILLEGAL();								break; // ILLEGAL
				case 0x01: NOP_();									break; // NOP				(Inherent)
				case 0x02: ILLEGAL();								break; // ILLEGAL
				case 0x03: ILLEGAL();								break; // ILLEGAL
				case 0x04: ILLEGAL();								break; // ILLEGAL
				case 0x05: ILLEGAL();								break; // ILLEGAL
				case 0x06: REG_OP(TAP, A);							break; // TAP				(Inherent)
				case 0x07: REG_OP(TPA, A);							break; // TPA				(Inherent)
				case 0x08: REG_OP_16(INX, X);						break; // INX				(Inherent)
				case 0x09: REG_OP_16(DEX, X);						break; // DEX				(Inherent)
				case 0x0A: REG_OP(CLV, CC);							break; // CLV				(Inherent)
				case 0x0B: REG_OP(SEV, CC);							break; // SEV				(Inherent)
				case 0x0C: REG_OP(CLC, CC);							break; // CLC				(Inherent)
				case 0x0D: REG_OP(SEC, CC);							break; // SEC				(Inherent)
				case 0x0E: REG_OP(CLI, CC);							break; // CLI				(Inherent)
				case 0x0F: REG_OP(SEI, CC);							break; // SEI				(Inherent)
				case 0x10: REG_OP(SBA, A);							break; // SBA				(Inherent)
				case 0x11: REG_OP(CBA, A);							break; // CBA				(Inherent)
				case 0x12: ILLEGAL();								break; // ILLEGAL
				case 0x13: ILLEGAL();								break; // ILLEGAL
				case 0x14: ILLEGAL();								break; // ILLEGAL
				case 0x15: ILLEGAL();								break; // ILLEGAL
				case 0x16: REG_OP(TAB, A);							break; // TAB				(Inherent)
				case 0x17: REG_OP(TBA, A);							break; // TBA				(Inherent)
				case 0x18: ILLEGAL();								break; // ILLEGAL
				case 0x19: REG_OP(DA, A);							break; // DAA				(Inherent)
				case 0x1A: ILLEGAL();								break; // ILLEGAL
				case 0x1B: REG_OP(ABA, A);							break; // ABA				(Inherent)
				case 0x1C: ILLEGAL();								break; // ILLEGAL
				case 0x1D: ILLEGAL();								break; // ILLEGAL
				case 0x1E: ILLEGAL();								break; // ILLEGAL
				case 0x1F: ILLEGAL();								break; // ILLEGAL
				case 0x20: BR_(true);								break; // BRA				(Relative)
				case 0x21: ILLEGAL();								break; // ILLEGAL
				case 0x22: BR_(!(FlagC | FlagZ));					break; // BHI				(Relative)
				case 0x23: BR_(FlagC | FlagZ);						break; // BLS				(Relative)
				case 0x24: BR_(!FlagC);								break; // BHS , BCC			(Relative)
				case 0x25: BR_(FlagC);								break; // BLO , BCS			(Relative)
				case 0x26: BR_(!FlagZ);								break; // BNE				(Relative)
				case 0x27: BR_(FlagZ);								break; // BEQ				(Relative)
				case 0x28: BR_(!FlagV);								break; // BVC				(Relative)
				case 0x29: BR_(FlagV);								break; // BVS				(Relative)
				case 0x2A: BR_(!FlagN);								break; // BPL				(Relative)
				case 0x2B: BR_(FlagN);								break; // BMI				(Relative)
				case 0x2C: BR_(FlagN == FlagV);						break; // BGE				(Relative)
				case 0x2D: BR_(FlagN ^ FlagV);						break; // BLT				(Relative)
				case 0x2E: BR_((!FlagZ) & (FlagN == FlagV));		break; // BGT				(Relative)
				case 0x2F: BR_(FlagZ | (FlagN ^ FlagV));			break; // BLE				(Relative)
				case 0x30: REG_OP_16(TSX, X);						break; // TSX				(Inherent)
				case 0x31: REG_OP_16(INS, SP);						break; // INS				(Inherent)
				case 0x32: PUL_(A);									break; // PULA				(Inherent)
				case 0x33: PUL_(B);									break; // PULB				(Inherent)
				case 0x34: REG_OP_16(DES, SP);						break; // DES				(Inherent)
				case 0x35: REG_OP_16(TXS, SP);						break; // TXS				(Inherent)
				case 0x36: PSH_(A);									break; // PSHA				(Inherent)
				case 0x37: PSH_(B);									break; // PSHB				(Inherent)
				case 0x38: ILLEGAL();								break; // ILLEGAL
				case 0x39: RTS();									break; // RTS				(Inherent)
				case 0x3A: ILLEGAL();								break; // ILLEGAL
				case 0x3B: RTI();									break; // RTI				(Inherent)
				case 0x3C: ILLEGAL();								break; // ILLEGAL
				case 0x3D: ILLEGAL();								break; // ILLEGAL
				case 0x3E: WAI_();									break; // WAI				(Inherent)
				case 0x3F: SWI1();									break; // SWI				(Inherent)
				case 0x40: REG_OP(NEG, A);							break; // NEGA				(Inherent)
				case 0x41: ILLEGAL();								break; // ILLEGAL
				case 0x42: ILLEGAL();								break; // ILLEGAL
				case 0x43: REG_OP(COM, A);							break; // COMA				(Inherent)
				case 0x44: REG_OP(LSR, A);							break; // LSRA				(Inherent)
				case 0x45: ILLEGAL();								break; // ILLEGAL
				case 0x46: REG_OP(ROR, A);							break; // RORA				(Inherent)
				case 0x47: REG_OP(ASR, A);							break; // ASRA				(Inherent)
				case 0x48: REG_OP(ASL, A);							break; // ASLA , LSLA		(Inherent)
				case 0x49: REG_OP(ROL, A);							break; // ROLA				(Inherent)
				case 0x4A: REG_OP(DEC8, A);							break; // DECA				(Inherent)
				case 0x4B: ILLEGAL();								break; // ILLEGAL
				case 0x4C: REG_OP(INC8, A);							break; // INCA				(Inherent)
				case 0x4D: REG_OP(TST, A);							break; // TSTA				(Inherent)
				case 0x4E: ILLEGAL();								break; // ILLEGAL
				case 0x4F: REG_OP(CLR, A);							break; // CLRA				(Inherent)
				case 0x50: REG_OP(NEG, B);							break; // NEGB				(Inherent)
				case 0x51: ILLEGAL();								break; // ILLEGAL
				case 0x52: ILLEGAL();							    break; // ILLEGAL
				case 0x53: REG_OP(COM, B);							break; // COMB				(Inherent)
				case 0x54: REG_OP(LSR, B);							break; // LSRB				(Inherent)
				case 0x55: ILLEGAL();								break; // ILLEGAL
				case 0x56: REG_OP(ROR, B);							break; // RORB				(Inherent)
				case 0x57: REG_OP(ASR, B);							break; // ASRB				(Inherent)
				case 0x58: REG_OP(ASL, B);							break; // ASLB , LSLB		(Inherent)
				case 0x59: REG_OP(ROL, B);							break; // ROLB				(Inherent)
				case 0x5A: REG_OP(DEC8, B);							break; // DECB				(Inherent)
				case 0x5B: ILLEGAL();								break; // ILLEGAL
				case 0x5C: REG_OP(INC8, B);							break; // INCB				(Inherent)
				case 0x5D: REG_OP(TST, B);							break; // TSTB				(Inherent)
				case 0x5E: ILLEGAL();								break; // ILLEGAL
				case 0x5F: REG_OP(CLR, B);							break; // CLRB				(Inherent)
				case 0x60: INDEX_OP(I_NEG);							break; // NEG				(Indexed)
				case 0x61: ILLEGAL();								break; // ILLEGAL
				case 0x62: ILLEGAL();								break; // ILLEGAL
				case 0x63: INDEX_OP(I_COM);							break; // COM				(Indexed)
				case 0x64: INDEX_OP(I_LSR);							break; // LSR				(Indexed)
				case 0x65: ILLEGAL();								break; // ILLEGAL
				case 0x66: INDEX_OP(I_ROR);							break; // ROR				(Indexed)
				case 0x67: INDEX_OP(I_ASR);							break; // ASR				(Indexed)
				case 0x68: INDEX_OP(I_ASL);							break; // ASL , LSL			(Indexed)
				case 0x69: INDEX_OP(I_ROL);							break; // ROL				(Indexed)
				case 0x6A: INDEX_OP(I_DEC);							break; // DEC				(Indexed)
				case 0x6B: ILLEGAL();								break; // ILLEGAL
				case 0x6C: INDEX_OP(I_INC);							break; // INC				(Indexed)
				case 0x6D: INDEX_OP(I_TST);							break; // TST				(Indexed)
				case 0x6E: INDEX_OP(I_JMP);							break; // JMP				(Indexed)
				case 0x6F: INDEX_OP(I_CLR);							break; // CLR				(Indexed)
				case 0x70: EXT_MEM(NEG);							break; // NEG				(Extended)
				case 0x71: ILLEGAL();								break; // ILLEGAL
				case 0x72: ILLEGAL();								break; // ILLEGAL
				case 0x73: EXT_MEM(COM);							break; // COM				(Extended)
				case 0x74: EXT_MEM(LSR);							break; // LSR				(Extended)
				case 0x75: ILLEGAL();								break; // ILLEGAL
				case 0x76: EXT_MEM(ROR);							break; // ROR				(Extended)
				case 0x77: EXT_MEM(ASR);							break; // ASR				(Extended)
				case 0x78: EXT_MEM(ASL);							break; // ASL , LSL			(Extended)
				case 0x79: EXT_MEM(ROL);							break; // ROL				(Extended)
				case 0x7A: EXT_MEM(DEC8);							break; // DEC				(Extended)
				case 0x7B: ILLEGAL();								break; // ILLEGAL
				case 0x7C: EXT_MEM(INC8);							break; // INC				(Extended)
				case 0x7D: EXT_MEM(TST);							break; // TST				(Extended)
				case 0x7E: JMP_EXT_();								break; // JMP				(Extended)
				case 0x7F: EXT_MEM(CLR);							break; // CLR				(Extended)
				case 0x80: REG_OP_IMD(SUB8, A);						break; // SUBA				(Immediate)
				case 0x81: REG_OP_IMD(CMP8, A);						break; // CMPA				(Immediate)
				case 0x82: REG_OP_IMD(SBC8, A);						break; // SBCA				(Immediate)
				case 0x83: ILLEGAL();								break; // ILLEGAL
				case 0x84: REG_OP_IMD(AND8, A);						break; // ANDA				(Immediate)
				case 0x85: REG_OP_IMD(BIT, A);						break; // BITA				(Immediate)
				case 0x86: REG_OP_IMD(LD_8, A);						break; // LDA				(Immediate)
				case 0x87: ILLEGAL();								break; // ILLEGAL
				case 0x88: REG_OP_IMD(XOR8, A);						break; // EORA				(Immediate)
				case 0x89: REG_OP_IMD(ADC8, A);						break; // ADCA				(Immediate)
				case 0x8A: REG_OP_IMD(OR8, A);						break; // ORA				(Immediate)
				case 0x8B: REG_OP_IMD(ADD8, A);						break; // ADDA				(Immediate)
				case 0x8C: IMD_CMP_16(CMP16, X);					break; // CMPX				(Immediate)
				case 0x8D: BSR_();									break; // BSR				(Relative)
				case 0x8E: REG_OP_LD_16(SP);						break; // LDS				(Immediate)
				case 0x8F: ILLEGAL();								break; // ILLEGAL
				case 0x90: DIRECT_MEM_4(SUB8, A);					break; // SUBA				(Direct)
				case 0x91: DIRECT_MEM_4(CMP8, A);					break; // CMPA				(Direct)
				case 0x92: DIRECT_MEM_4(SBC8, A);					break; // SBCA				(Direct)
				case 0x93: ILLEGAL();								break; // ILLEGAL
				case 0x94: DIRECT_MEM_4(AND8, A);					break; // ANDA				(Direct)
				case 0x95: DIRECT_MEM_4(BIT, A);					break; // BITA				(Direct)
				case 0x96: DIRECT_MEM_4(LD_8, A);					break; // LDA				(Direct)
				case 0x97: DIRECT_ST_4(A);							break; // STA				(Direct)
				case 0x98: DIRECT_MEM_4(XOR8, A);					break; // EORA				(Direct)
				case 0x99: DIRECT_MEM_4(ADC8, A);					break; // ADCA				(Direct)
				case 0x9A: DIRECT_MEM_4(OR8, A);					break; // ORA				(Direct)
				case 0x9B: DIRECT_MEM_4(ADD8, A);					break; // ADDA				(Direct)
				case 0x9C: DIR_CMP_16(CMP16, X);					break; // CMPX				(Direct)
				case 0x9D: ILLEGAL();								break; // ILLEGAL
				case 0x9E: DIR_OP_LD_16(SP);						break; // LDS				(Direct)
				case 0x9F: DIR_OP_ST_16(SP);						break; // STS				(Direct)
				case 0xA0: INDEX_OP_REG(I_SUB, A);					break; // SUBA				(Indexed)
				case 0xA1: INDEX_OP_REG(I_CMP, A);					break; // CMPA				(Indexed)
				case 0xA2: INDEX_OP_REG(I_SBC, A);					break; // SBCA				(Indexed)
				case 0xA3: ILLEGAL();								break; // ILLEGAL
				case 0xA4: INDEX_OP_REG(I_AND, A);					break; // ANDA				(Indexed)
				case 0xA5: INDEX_OP_REG(I_BIT, A);					break; // BITA				(Indexed)
				case 0xA6: INDEX_OP_REG(I_LD, A);					break; // LDA				(Indexed)
				case 0xA7: INDEX_OP_REG(I_ST, A);					break; // STA				(Indexed)
				case 0xA8: INDEX_OP_REG(I_XOR, A);					break; // EORA				(Indexed)
				case 0xA9: INDEX_OP_REG(I_ADC, A);					break; // ADCA				(Indexed)
				case 0xAA: INDEX_OP_REG(I_OR, A);					break; // ORA				(Indexed)
				case 0xAB: INDEX_OP_REG(I_ADD, A);					break; // ADDA				(Indexed)
				case 0xAC: INDEX_OP_REG(I_CMP16, X);				break; // CMPX				(Indexed)
				case 0xAD: INDEX_OP(I_JSR);							break; // JSR				(Indexed)
				case 0xAE: INDEX_OP_REG(I_LD16, SP);				break; // LDS				(Indexed)
				case 0xAF: INDEX_OP_REG(I_ST16, SP);				break; // STS				(Indexed)
				case 0xB0: EXT_REG(SUB8, A);						break; // SUBA				(Extended)
				case 0xB1: EXT_REG(CMP8, A);						break; // CMPA				(Extended)
				case 0xB2: EXT_REG(SBC8, A);						break; // SBCA				(Extended)
				case 0xB3: ILLEGAL();								break; // ILLEGAL
				case 0xB4: EXT_REG(AND8, A);						break; // ANDA				(Extended)
				case 0xB5: EXT_REG(BIT, A);							break; // BITA				(Extended)
				case 0xB6: EXT_REG(LD_8, A);						break; // LDA				(Extended)
				case 0xB7: EXT_ST(A);								break; // STA				(Extended)
				case 0xB8: EXT_REG(XOR8, A);						break; // EORA				(Extended)
				case 0xB9: EXT_REG(ADC8, A);						break; // ADCA				(Extended)
				case 0xBA: EXT_REG(OR8, A);							break; // ORA				(Extended)
				case 0xBB: EXT_REG(ADD8, A);						break; // ADDA				(Extended)
				case 0xBC: EXT_CMP_16(CMP16, X);					break; // CMPX				(Extended)
				case 0xBD: JSR_EXT();								break; // JSR				(Extended)
				case 0xBE: EXT_OP_LD_16(SP);						break; // LDS				(Extended)
				case 0xBF: EXT_OP_ST_16(SP);						break; // STS				(Extended)
				case 0xC0: REG_OP_IMD(SUB8, B);						break; // SUBB				(Immediate)
				case 0xC1: REG_OP_IMD(CMP8, B);						break; // CMPB				(Immediate)
				case 0xC2: REG_OP_IMD(SBC8, B);						break; // SBCB				(Immediate)
				case 0xC3: ILLEGAL();								break; // ILLEGAL
				case 0xC4: REG_OP_IMD(AND8, B);						break; // ANDB				(Immediate)
				case 0xC5: REG_OP_IMD(BIT, B);						break; // BITB				(Immediate)
				case 0xC6: REG_OP_IMD(LD_8, B);						break; // LDB				(Immediate)
				case 0xC7: ILLEGAL();								break; // ILLEGAL
				case 0xC8: REG_OP_IMD(XOR8, B);						break; // EORB				(Immediate)
				case 0xC9: REG_OP_IMD(ADC8, B);						break; // ADCB				(Immediate)
				case 0xCA: REG_OP_IMD(OR8, B);						break; // ORB				(Immediate)
				case 0xCB: REG_OP_IMD(ADD8, B);						break; // ADDB				(Immediate)
				case 0xCC: ILLEGAL();								break; // ILLEGAL
				case 0xCD: ILLEGAL();								break; // ILLEGAL
				case 0xCE: REG_OP_LD_16(X);							break; // LDX				(Immediate)
				case 0xCF: ILLEGAL();								break; // ILLEGAL
				case 0xD0: DIRECT_MEM_4(SUB8, B);					break; // SUBB				(Direct)
				case 0xD1: DIRECT_MEM_4(CMP8, B);					break; // CMPB				(Direct)
				case 0xD2: DIRECT_MEM_4(SBC8, B);					break; // SBCB				(Direct)
				case 0xD3: ILLEGAL();								break; // ILLEGAL
				case 0xD4: DIRECT_MEM_4(AND8, B);					break; // ANDB				(Direct)
				case 0xD5: DIRECT_MEM_4(BIT, B);					break; // BITB				(Direct)
				case 0xD6: DIRECT_MEM_4(LD_8, B);					break; // LDB				(Direct)
				case 0xD7: DIRECT_ST_4(B);							break; // STB				(Direct)
				case 0xD8: DIRECT_MEM_4(XOR8, B);					break; // EORB				(Direct)
				case 0xD9: DIRECT_MEM_4(ADC8, B);					break; // ADCB				(Direct)
				case 0xDA: DIRECT_MEM_4(OR8, B);					break; // ORB				(Direct)
				case 0xDB: DIRECT_MEM_4(ADD8, B);					break; // ADDB				(Direct)
				case 0xDC: ILLEGAL();								break; // ILLEGAL
				case 0xDD: ILLEGAL();								break; // ILLEGAL
				case 0xDE: DIR_OP_LD_16(X);							break; // LDX				(Direct)
				case 0xDF: DIR_OP_ST_16(X);							break; // STX				(Direct)
				case 0xE0: INDEX_OP_REG(I_SUB, B);					break; // SUBB				(Indexed)
				case 0xE1: INDEX_OP_REG(I_CMP, B);					break; // CMPB				(Indexed)
				case 0xE2: INDEX_OP_REG(I_SBC, B);					break; // SBCB				(Indexed)
				case 0xE3: ILLEGAL();								break; // ILLEGAL
				case 0xE4: INDEX_OP_REG(I_AND, B);					break; // ANDB				(Indexed)
				case 0xE5: INDEX_OP_REG(I_BIT, B);					break; // BITB				(Indexed)
				case 0xE6: INDEX_OP_REG(I_LD, B);					break; // LDB				(Indexed)
				case 0xE7: INDEX_OP_REG(I_ST, B);					break; // STB				(Indexed)
				case 0xE8: INDEX_OP_REG(I_XOR, B);					break; // EORB				(Indexed)
				case 0xE9: INDEX_OP_REG(I_ADC, B);					break; // ADCB				(Indexed)
				case 0xEA: INDEX_OP_REG(I_OR, B);					break; // ORB				(Indexed)
				case 0xEB: INDEX_OP_REG(I_ADD, B);					break; // ADDB				(Indexed)
				case 0xEC: ILLEGAL();								break; // ILLEGAL
				case 0xED: ILLEGAL();								break; // ILLEGAL
				case 0xEE: INDEX_OP_REG(I_LD16, X);					break; // LDX				(Indexed)
				case 0xEF: INDEX_OP_REG(I_ST16, X);					break; // STX				(Indexed)
				case 0xF0: EXT_REG(SUB8, B);						break; // SUBB				(Extended)
				case 0xF1: EXT_REG(CMP8, B);						break; // CMPB				(Extended)
				case 0xF2: EXT_REG(SBC8, B);						break; // SBCB				(Extended)
				case 0xF3: ILLEGAL();								break; // ILLEGAL
				case 0xF4: EXT_REG(AND8, B);						break; // ANDB				(Extended)
				case 0xF5: EXT_REG(BIT, B);							break; // BITB				(Extended)
				case 0xF6: EXT_REG(LD_8, B);						break; // LDB				(Extended)
				case 0xF7: EXT_ST(B);								break; // STB				(Extended)
				case 0xF8: EXT_REG(XOR8, B);						break; // EORB				(Extended)
				case 0xF9: EXT_REG(ADC8, B);						break; // ADCB				(Extended)
				case 0xFA: EXT_REG(OR8, B);							break; // ORB				(Extended)
				case 0xFB: EXT_REG(ADD8, B);						break; // ADDB				(Extended)
				case 0xFC: ILLEGAL();								break; // ILLEGAL
				case 0xFD: ILLEGAL();								break; // ILLEGAL
				case 0xFE: EXT_OP_LD_16(X);							break; // LDX				(Extended)
				case 0xFF: EXT_OP_ST_16(X);							break; // STX				(Extended)
			}
		}
	}
}