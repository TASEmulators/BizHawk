namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		public ulong TotalExecutedCycles;

		private int EI_pending;
		private bool interrupts_enabled;

		// variables for executing instructions
		public int instr_pntr = 0;
		public ushort[] cur_instr = new ushort [60];
		public ushort[] instr_table = new ushort[256 * 2 * 60 + 60 * 8];
		public int opcode;
		public bool CB_prefix;
		public bool halted;
		public bool stopped;
		public bool jammed;
		public int LY;

		// unsaved variables
		public bool checker;
		byte interrupt_src_reg, interrupt_enable_reg;

		public void BuildInstructionTable()
		{
			for (int i = 0; i < 256; i++)
			{
				switch (i)
				{
					case 0x00: NOP_();									break; // NOP
					case 0x01: LD_IND_16(C, B, PCl, PCh);				break; // LD BC, nn
					case 0x02: LD_8_IND(C, B, A);						break; // LD (BC), A
					case 0x03: INC_16(C, B);							break; // INC BC
					case 0x04: INT_OP(INC8, B);							break; // INC B
					case 0x05: INT_OP(DEC8, B);							break; // DEC B
					case 0x06: LD_IND_8_INC(B, PCl, PCh);				break; // LD B, n
					case 0x07: INT_OP(RLC, Aim);						break; // RLCA
					case 0x08: LD_R_IM(SPl, SPh, PCl, PCh);				break; // LD (imm), SP
					case 0x09: ADD_16(L, H, C, B);						break; // ADD HL, BC
					case 0x0A: REG_OP_IND(TR, A, C, B);					break; // LD A, (BC)
					case 0x0B: DEC_16(C, B);							break; // DEC BC
					case 0x0C: INT_OP(INC8, C);							break; // INC C
					case 0x0D: INT_OP(DEC8, C);							break; // DEC C
					case 0x0E: LD_IND_8_INC(C, PCl, PCh);				break; // LD C, n
					case 0x0F: INT_OP(RRC, Aim);						break; // RRCA
					case 0x10: STOP_();									break; // STOP
					case 0x11: LD_IND_16(E, D, PCl, PCh);				break; // LD DE, nn
					case 0x12: LD_8_IND(E, D, A);						break; // LD (DE), A
					case 0x13: INC_16(E, D);							break; // INC DE
					case 0x14: INT_OP(INC8, D);							break; // INC D
					case 0x15: INT_OP(DEC8, D);							break; // DEC D
					case 0x16: LD_IND_8_INC(D, PCl, PCh);				break; // LD D, n
					case 0x17: INT_OP(RL, Aim);							break; // RLA
					case 0x18: JR_COND(ALWAYS_T);						break; // JR, r8
					case 0x19: ADD_16(L, H, E, D);						break; // ADD HL, DE
					case 0x1A: REG_OP_IND(TR, A, E, D);					break; // LD A, (DE)
					case 0x1B: DEC_16(E, D);							break; // DEC DE
					case 0x1C: INT_OP(INC8, E);							break; // INC E
					case 0x1D: INT_OP(DEC8, E);							break; // DEC E
					case 0x1E: LD_IND_8_INC(E, PCl, PCh);				break; // LD E, n
					case 0x1F: INT_OP(RR, Aim);							break; // RRA
					case 0x20: JR_COND(FLAG_NZ);						break; // JR NZ, r8
					case 0x21: LD_IND_16(L, H, PCl, PCh);				break; // LD HL, nn
					case 0x22: LD_8_IND_INC(L, H, A);					break; // LD (HL+), A
					case 0x23: INC_16(L, H);							break; // INC HL
					case 0x24: INT_OP(INC8, H);							break; // INC H
					case 0x25: INT_OP(DEC8, H);							break; // DEC H
					case 0x26: LD_IND_8_INC(H, PCl, PCh);				break; // LD H, n
					case 0x27: INT_OP(DA, A);							break; // DAA
					case 0x28: JR_COND(FLAG_Z);							break; // JR Z, r8
					case 0x29: ADD_16(L, H, L, H);						break; // ADD HL, HL
					case 0x2A: LD_IND_8_INC_HL(A, L, H);				break; // LD A, (HL+)
					case 0x2B: DEC_16(L, H);							break; // DEC HL
					case 0x2C: INT_OP(INC8, L);							break; // INC L
					case 0x2D: INT_OP(DEC8, L);							break; // DEC L
					case 0x2E: LD_IND_8_INC(L, PCl, PCh);				break; // LD L, n
					case 0x2F: INT_OP(CPL, A);							break; // CPL
					case 0x30: JR_COND(FLAG_NC);						break; // JR NC, r8
					case 0x31: LD_IND_16(SPl, SPh, PCl, PCh);			break; // LD SP, nn
					case 0x32: LD_8_IND_DEC(L, H, A);					break; // LD (HL-), A
					case 0x33: INC_16(SPl, SPh);						break; // INC SP
					case 0x34: INC_8_IND(L, H);							break; // INC (HL)
					case 0x35: DEC_8_IND(L, H);							break; // DEC (HL)
					case 0x36: LD_8_IND_IND(L, H, PCl, PCh);			break; // LD (HL), n
					case 0x37: INT_OP(SCF, A);							break; // SCF
					case 0x38: JR_COND(FLAG_C);							break; // JR C, r8
					case 0x39: ADD_16(L, H, SPl, SPh);					break; // ADD HL, SP
					case 0x3A: LD_IND_8_DEC_HL(A, L, H);				break; // LD A, (HL-)
					case 0x3B: DEC_16(SPl, SPh);						break; // DEC SP
					case 0x3C: INT_OP(INC8, A);							break; // INC A
					case 0x3D: INT_OP(DEC8, A);							break; // DEC A
					case 0x3E: LD_IND_8_INC(A, PCl, PCh);				break; // LD A, n
					case 0x3F: INT_OP(CCF, A);							break; // CCF
					case 0x40: REG_OP(TR, B, B);						break; // LD B, B
					case 0x41: REG_OP(TR, B, C);						break; // LD B, C
					case 0x42: REG_OP(TR, B, D);						break; // LD B, D
					case 0x43: REG_OP(TR, B, E);						break; // LD B, E
					case 0x44: REG_OP(TR, B, H);						break; // LD B, H
					case 0x45: REG_OP(TR, B, L);						break; // LD B, L
					case 0x46: REG_OP_IND(TR, B, L, H);					break; // LD B, (HL)
					case 0x47: REG_OP(TR, B, A);						break; // LD B, A
					case 0x48: REG_OP(TR, C, B);						break; // LD C, B
					case 0x49: REG_OP(TR, C, C);						break; // LD C, C
					case 0x4A: REG_OP(TR, C, D);						break; // LD C, D
					case 0x4B: REG_OP(TR, C, E);						break; // LD C, E
					case 0x4C: REG_OP(TR, C, H);						break; // LD C, H
					case 0x4D: REG_OP(TR, C, L);						break; // LD C, L
					case 0x4E: REG_OP_IND(TR, C, L, H);					break; // LD C, (HL)
					case 0x4F: REG_OP(TR, C, A);						break; // LD C, A
					case 0x50: REG_OP(TR, D, B);						break; // LD D, B
					case 0x51: REG_OP(TR, D, C);						break; // LD D, C
					case 0x52: REG_OP(TR, D, D);						break; // LD D, D
					case 0x53: REG_OP(TR, D, E);						break; // LD D, E
					case 0x54: REG_OP(TR, D, H);						break; // LD D, H
					case 0x55: REG_OP(TR, D, L);						break; // LD D, L
					case 0x56: REG_OP_IND(TR, D, L, H);					break; // LD D, (HL)
					case 0x57: REG_OP(TR, D, A);						break; // LD D, A
					case 0x58: REG_OP(TR, E, B);						break; // LD E, B
					case 0x59: REG_OP(TR, E, C);						break; // LD E, C
					case 0x5A: REG_OP(TR, E, D);						break; // LD E, D
					case 0x5B: REG_OP(TR, E, E);						break; // LD E, E
					case 0x5C: REG_OP(TR, E, H);						break; // LD E, H
					case 0x5D: REG_OP(TR, E, L);						break; // LD E, L
					case 0x5E: REG_OP_IND(TR, E, L, H);					break; // LD E, (HL)
					case 0x5F: REG_OP(TR, E, A);						break; // LD E, A
					case 0x60: REG_OP(TR, H, B);						break; // LD H, B
					case 0x61: REG_OP(TR, H, C);						break; // LD H, C
					case 0x62: REG_OP(TR, H, D);						break; // LD H, D
					case 0x63: REG_OP(TR, H, E);						break; // LD H, E
					case 0x64: REG_OP(TR, H, H);						break; // LD H, H
					case 0x65: REG_OP(TR, H, L);						break; // LD H, L
					case 0x66: REG_OP_IND(TR, H, L, H);					break; // LD H, (HL)
					case 0x67: REG_OP(TR, H, A);						break; // LD H, A
					case 0x68: REG_OP(TR, L, B);						break; // LD L, B
					case 0x69: REG_OP(TR, L, C);						break; // LD L, C
					case 0x6A: REG_OP(TR, L, D);						break; // LD L, D
					case 0x6B: REG_OP(TR, L, E);						break; // LD L, E
					case 0x6C: REG_OP(TR, L, H);						break; // LD L, H
					case 0x6D: REG_OP(TR, L, L);						break; // LD L, L
					case 0x6E: REG_OP_IND(TR, L, L, H);					break; // LD L, (HL)
					case 0x6F: REG_OP(TR, L, A);						break; // LD L, A
					case 0x70: LD_8_IND(L, H, B);						break; // LD (HL), B
					case 0x71: LD_8_IND(L, H, C);						break; // LD (HL), C
					case 0x72: LD_8_IND(L, H, D);						break; // LD (HL), D
					case 0x73: LD_8_IND(L, H, E);						break; // LD (HL), E
					case 0x74: LD_8_IND(L, H, H);						break; // LD (HL), H
					case 0x75: LD_8_IND(L, H, L);						break; // LD (HL), L
					case 0x76: HALT_();									break; // HALT
					case 0x77: LD_8_IND(L, H, A);						break; // LD (HL), A
					case 0x78: REG_OP(TR, A, B);						break; // LD A, B
					case 0x79: REG_OP(TR, A, C);						break; // LD A, C
					case 0x7A: REG_OP(TR, A, D);						break; // LD A, D
					case 0x7B: REG_OP(TR, A, E);						break; // LD A, E
					case 0x7C: REG_OP(TR, A, H);						break; // LD A, H
					case 0x7D: REG_OP(TR, A, L);						break; // LD A, L
					case 0x7E: REG_OP_IND(TR, A, L, H);					break; // LD A, (HL)
					case 0x7F: REG_OP(TR, A, A);						break; // LD A, A
					case 0x80: REG_OP(ADD8, A, B);						break; // ADD A, B
					case 0x81: REG_OP(ADD8, A, C);						break; // ADD A, C
					case 0x82: REG_OP(ADD8, A, D);						break; // ADD A, D
					case 0x83: REG_OP(ADD8, A, E);						break; // ADD A, E
					case 0x84: REG_OP(ADD8, A, H);						break; // ADD A, H
					case 0x85: REG_OP(ADD8, A, L);						break; // ADD A, L
					case 0x86: REG_OP_IND(ADD8, A, L, H);				break; // ADD A, (HL)
					case 0x87: REG_OP(ADD8, A, A);						break; // ADD A, A
					case 0x88: REG_OP(ADC8, A, B);						break; // ADC A, B
					case 0x89: REG_OP(ADC8, A, C);						break; // ADC A, C
					case 0x8A: REG_OP(ADC8, A, D);						break; // ADC A, D
					case 0x8B: REG_OP(ADC8, A, E);						break; // ADC A, E
					case 0x8C: REG_OP(ADC8, A, H);						break; // ADC A, H
					case 0x8D: REG_OP(ADC8, A, L);						break; // ADC A, L
					case 0x8E: REG_OP_IND(ADC8, A, L, H);				break; // ADC A, (HL)
					case 0x8F: REG_OP(ADC8, A, A);						break; // ADC A, A
					case 0x90: REG_OP(SUB8, A, B);						break; // SUB A, B
					case 0x91: REG_OP(SUB8, A, C);						break; // SUB A, C
					case 0x92: REG_OP(SUB8, A, D);						break; // SUB A, D
					case 0x93: REG_OP(SUB8, A, E);						break; // SUB A, E
					case 0x94: REG_OP(SUB8, A, H);						break; // SUB A, H
					case 0x95: REG_OP(SUB8, A, L);						break; // SUB A, L
					case 0x96: REG_OP_IND(SUB8, A, L, H);				break; // SUB A, (HL)
					case 0x97: REG_OP(SUB8, A, A);						break; // SUB A, A
					case 0x98: REG_OP(SBC8, A, B);						break; // SBC A, B
					case 0x99: REG_OP(SBC8, A, C);						break; // SBC A, C
					case 0x9A: REG_OP(SBC8, A, D);						break; // SBC A, D
					case 0x9B: REG_OP(SBC8, A, E);						break; // SBC A, E
					case 0x9C: REG_OP(SBC8, A, H);						break; // SBC A, H
					case 0x9D: REG_OP(SBC8, A, L);						break; // SBC A, L
					case 0x9E: REG_OP_IND(SBC8, A, L, H);				break; // SBC A, (HL)
					case 0x9F: REG_OP(SBC8, A, A);						break; // SBC A, A
					case 0xA0: REG_OP(AND8, A, B);						break; // AND A, B
					case 0xA1: REG_OP(AND8, A, C);						break; // AND A, C
					case 0xA2: REG_OP(AND8, A, D);						break; // AND A, D
					case 0xA3: REG_OP(AND8, A, E);						break; // AND A, E
					case 0xA4: REG_OP(AND8, A, H);						break; // AND A, H
					case 0xA5: REG_OP(AND8, A, L);						break; // AND A, L
					case 0xA6: REG_OP_IND(AND8, A, L, H);				break; // AND A, (HL)
					case 0xA7: REG_OP(AND8, A, A);						break; // AND A, A
					case 0xA8: REG_OP(XOR8, A, B);						break; // XOR A, B
					case 0xA9: REG_OP(XOR8, A, C);						break; // XOR A, C
					case 0xAA: REG_OP(XOR8, A, D);						break; // XOR A, D
					case 0xAB: REG_OP(XOR8, A, E);						break; // XOR A, E
					case 0xAC: REG_OP(XOR8, A, H);						break; // XOR A, H
					case 0xAD: REG_OP(XOR8, A, L);						break; // XOR A, L
					case 0xAE: REG_OP_IND(XOR8, A, L, H);				break; // XOR A, (HL)
					case 0xAF: REG_OP(XOR8, A, A);						break; // XOR A, A
					case 0xB0: REG_OP(OR8, A, B);						break; // OR A, B
					case 0xB1: REG_OP(OR8, A, C);						break; // OR A, C
					case 0xB2: REG_OP(OR8, A, D);						break; // OR A, D
					case 0xB3: REG_OP(OR8, A, E);						break; // OR A, E
					case 0xB4: REG_OP(OR8, A, H);						break; // OR A, H
					case 0xB5: REG_OP(OR8, A, L);						break; // OR A, L
					case 0xB6: REG_OP_IND(OR8, A, L, H);				break; // OR A, (HL)
					case 0xB7: REG_OP(OR8, A, A);						break; // OR A, A
					case 0xB8: REG_OP(CP8, A, B);						break; // CP A, B
					case 0xB9: REG_OP(CP8, A, C);						break; // CP A, C
					case 0xBA: REG_OP(CP8, A, D);						break; // CP A, D
					case 0xBB: REG_OP(CP8, A, E);						break; // CP A, E
					case 0xBC: REG_OP(CP8, A, H);						break; // CP A, H
					case 0xBD: REG_OP(CP8, A, L);						break; // CP A, L
					case 0xBE: REG_OP_IND(CP8, A, L, H);				break; // CP A, (HL)
					case 0xBF: REG_OP(CP8, A, A);						break; // CP A, A
					case 0xC0: RET_COND(FLAG_NZ);						break; // Ret NZ
					case 0xC1: POP_(C, B);								break; // POP BC
					case 0xC2: JP_COND(FLAG_NZ);						break; // JP NZ
					case 0xC3: JP_COND(ALWAYS_T);						break; // JP
					case 0xC4: CALL_COND(FLAG_NZ);						break; // CALL NZ
					case 0xC5: PUSH_(C, B);								break; // PUSH BC
					case 0xC6: REG_OP_IND_INC(ADD8, A, PCl, PCh);		break; // ADD A, n
					case 0xC7: RST_(0);									break; // RST 0
					case 0xC8: RET_COND(FLAG_Z);						break; // RET Z
					case 0xC9: RET_();									break; // RET
					case 0xCA: JP_COND(FLAG_Z);							break; // JP Z
					case 0xCB: PREFIX_();								break; // PREFIX
					case 0xCC: CALL_COND(FLAG_Z);						break; // CALL Z
					case 0xCD: CALL_COND(ALWAYS_T);						break; // CALL
					case 0xCE: REG_OP_IND_INC(ADC8, A, PCl, PCh);		break; // ADC A, n
					case 0xCF: RST_(0x08);								break; // RST 0x08
					case 0xD0: RET_COND(FLAG_NC);						break; // Ret NC
					case 0xD1: POP_(E, D);								break; // POP DE
					case 0xD2: JP_COND(FLAG_NC);						break; // JP NC
					case 0xD3: JAM_();									break; // JAM
					case 0xD4: CALL_COND(FLAG_NC);						break; // CALL NC
					case 0xD5: PUSH_(E, D);								break; // PUSH DE
					case 0xD6: REG_OP_IND_INC(SUB8, A, PCl, PCh);		break; // SUB A, n
					case 0xD7: RST_(0x10);								break; // RST 0x10
					case 0xD8: RET_COND(FLAG_C);						break; // RET C
					case 0xD9: RETI_();									break; // RETI
					case 0xDA: JP_COND(FLAG_C);							break; // JP C
					case 0xDB: JAM_();									break; // JAM
					case 0xDC: CALL_COND(FLAG_C);						break; // CALL C
					case 0xDD: JAM_();									break; // JAM
					case 0xDE: REG_OP_IND_INC(SBC8, A, PCl, PCh);		break; // SBC A, n
					case 0xDF: RST_(0x18);								break; // RST 0x18
					case 0xE0: LD_FF_IND_8(PCl, PCh, A);				break; // LD(n), A
					case 0xE1: POP_(L, H);								break; // POP HL
					case 0xE2: LD_FFC_IND_8(PCl, PCh, A);				break; // LD(C), A
					case 0xE3: JAM_();									break; // JAM
					case 0xE4: JAM_();                                  break; // JAM
					case 0xE5: PUSH_(L, H);								break; // PUSH HL
					case 0xE6: REG_OP_IND_INC(AND8, A, PCl, PCh);		break; // AND A, n
					case 0xE7: RST_(0x20);								break; // RST 0x20
					case 0xE8: ADD_SP();								break; // ADD SP,n
					case 0xE9: JP_HL();									break; // JP (HL)
					case 0xEA: LD_FF_IND_16(PCl, PCh, A);				break; // LD(nn), A
					case 0xEB: JAM_();									break; // JAM
					case 0xEC: JAM_();									break; // JAM
					case 0xED: JAM_();									break; // JAM
					case 0xEE: REG_OP_IND_INC(XOR8, A, PCl, PCh);		break; // XOR A, n
					case 0xEF: RST_(0x28);								break; // RST 0x28
					case 0xF0: LD_8_IND_FF(A, PCl, PCh);				break; // A, LD(n)
					case 0xF1: POP_(F, A);								break; // POP AF
					case 0xF2: LD_8_IND_FFC(A, PCl, PCh);				break; // A, LD(C)
					case 0xF3: DI_();									break; // DI
					case 0xF4: JAM_();									break; // JAM
					case 0xF5: PUSH_(F, A);								break; // PUSH AF
					case 0xF6: REG_OP_IND_INC(OR8, A, PCl, PCh);		break; // OR A, n
					case 0xF7: RST_(0x30);								break; // RST 0x30
					case 0xF8: LD_HL_SPn();								break; // LD HL, SP+n
					case 0xF9: LD_SP_HL();								break; // LD, SP, HL
					case 0xFA: LD_16_IND_FF(A, PCl, PCh);				break; // A, LD(nn)
					case 0xFB: EI_();									break; // EI
					case 0xFC: JAM_();									break; // JAM
					case 0xFD: JAM_();									break; // JAM
					case 0xFE: REG_OP_IND_INC(CP8, A, PCl, PCh);		break; // XOR A, n
					case 0xFF: RST_(0x38);								break; // RST 0x38
				}

				for (int j = 0; j < cur_instr.Length; j++)
				{
					instr_table[i * 60 + j] = cur_instr[j];
				}

				switch (i)
				{
					case 0x00: INT_OP(RLC, B);							break; // RLC B
					case 0x01: INT_OP(RLC, C);							break; // RLC C
					case 0x02: INT_OP(RLC, D);							break; // RLC D
					case 0x03: INT_OP(RLC, E);							break; // RLC E
					case 0x04: INT_OP(RLC, H);							break; // RLC H
					case 0x05: INT_OP(RLC, L);							break; // RLC L
					case 0x06: INT_OP_IND(RLC, L, H);					break; // RLC (HL)
					case 0x07: INT_OP(RLC, A);							break; // RLC A
					case 0x08: INT_OP(RRC, B);							break; // RRC B
					case 0x09: INT_OP(RRC, C);							break; // RRC C
					case 0x0A: INT_OP(RRC, D);							break; // RRC D
					case 0x0B: INT_OP(RRC, E);							break; // RRC E
					case 0x0C: INT_OP(RRC, H);							break; // RRC H
					case 0x0D: INT_OP(RRC, L);							break; // RRC L
					case 0x0E: INT_OP_IND(RRC, L, H);					break; // RRC (HL)
					case 0x0F: INT_OP(RRC, A);							break; // RRC A
					case 0x10: INT_OP(RL, B);							break; // RL B
					case 0x11: INT_OP(RL, C);							break; // RL C
					case 0x12: INT_OP(RL, D);							break; // RL D
					case 0x13: INT_OP(RL, E);							break; // RL E
					case 0x14: INT_OP(RL, H);							break; // RL H
					case 0x15: INT_OP(RL, L);							break; // RL L
					case 0x16: INT_OP_IND(RL, L, H);					break; // RL (HL)
					case 0x17: INT_OP(RL, A);							break; // RL A
					case 0x18: INT_OP(RR, B);							break; // RR B
					case 0x19: INT_OP(RR, C);							break; // RR C
					case 0x1A: INT_OP(RR, D);							break; // RR D
					case 0x1B: INT_OP(RR, E);							break; // RR E
					case 0x1C: INT_OP(RR, H);							break; // RR H
					case 0x1D: INT_OP(RR, L);							break; // RR L
					case 0x1E: INT_OP_IND(RR, L, H);					break; // RR (HL)
					case 0x1F: INT_OP(RR, A);							break; // RR A
					case 0x20: INT_OP(SLA, B);							break; // SLA B
					case 0x21: INT_OP(SLA, C);							break; // SLA C
					case 0x22: INT_OP(SLA, D);							break; // SLA D
					case 0x23: INT_OP(SLA, E);							break; // SLA E
					case 0x24: INT_OP(SLA, H);							break; // SLA H
					case 0x25: INT_OP(SLA, L);							break; // SLA L
					case 0x26: INT_OP_IND(SLA, L, H);					break; // SLA (HL)
					case 0x27: INT_OP(SLA, A);							break; // SLA A
					case 0x28: INT_OP(SRA, B);							break; // SRA B
					case 0x29: INT_OP(SRA, C);							break; // SRA C
					case 0x2A: INT_OP(SRA, D);							break; // SRA D
					case 0x2B: INT_OP(SRA, E);							break; // SRA E
					case 0x2C: INT_OP(SRA, H);							break; // SRA H
					case 0x2D: INT_OP(SRA, L);							break; // SRA L
					case 0x2E: INT_OP_IND(SRA, L, H);					break; // SRA (HL)
					case 0x2F: INT_OP(SRA, A);							break; // SRA A
					case 0x30: INT_OP(SWAP, B);							break; // SWAP B
					case 0x31: INT_OP(SWAP, C);							break; // SWAP C
					case 0x32: INT_OP(SWAP, D);							break; // SWAP D
					case 0x33: INT_OP(SWAP, E);							break; // SWAP E
					case 0x34: INT_OP(SWAP, H);							break; // SWAP H
					case 0x35: INT_OP(SWAP, L);							break; // SWAP L
					case 0x36: INT_OP_IND(SWAP, L, H);					break; // SWAP (HL)
					case 0x37: INT_OP(SWAP, A);							break; // SWAP A
					case 0x38: INT_OP(SRL, B);							break; // SRL B
					case 0x39: INT_OP(SRL, C);							break; // SRL C
					case 0x3A: INT_OP(SRL, D);							break; // SRL D
					case 0x3B: INT_OP(SRL, E);							break; // SRL E
					case 0x3C: INT_OP(SRL, H);							break; // SRL H
					case 0x3D: INT_OP(SRL, L);							break; // SRL L
					case 0x3E: INT_OP_IND(SRL, L, H);					break; // SRL (HL)
					case 0x3F: INT_OP(SRL, A);							break; // SRL A
					case 0x40: BIT_OP(BIT, 0, B);						break; // BIT 0, B
					case 0x41: BIT_OP(BIT, 0, C);						break; // BIT 0, C
					case 0x42: BIT_OP(BIT, 0, D);						break; // BIT 0, D
					case 0x43: BIT_OP(BIT, 0, E);						break; // BIT 0, E
					case 0x44: BIT_OP(BIT, 0, H);						break; // BIT 0, H
					case 0x45: BIT_OP(BIT, 0, L);						break; // BIT 0, L
					case 0x46: BIT_TE_IND(BIT, 0, L, H);				break; // BIT 0, (HL)
					case 0x47: BIT_OP(BIT, 0, A);						break; // BIT 0, A
					case 0x48: BIT_OP(BIT, 1, B);						break; // BIT 1, B
					case 0x49: BIT_OP(BIT, 1, C);						break; // BIT 1, C
					case 0x4A: BIT_OP(BIT, 1, D);						break; // BIT 1, D
					case 0x4B: BIT_OP(BIT, 1, E);						break; // BIT 1, E
					case 0x4C: BIT_OP(BIT, 1, H);						break; // BIT 1, H
					case 0x4D: BIT_OP(BIT, 1, L);						break; // BIT 1, L
					case 0x4E: BIT_TE_IND(BIT, 1, L, H);				break; // BIT 1, (HL)
					case 0x4F: BIT_OP(BIT, 1, A);						break; // BIT 1, A
					case 0x50: BIT_OP(BIT, 2, B);						break; // BIT 2, B
					case 0x51: BIT_OP(BIT, 2, C);						break; // BIT 2, C
					case 0x52: BIT_OP(BIT, 2, D);						break; // BIT 2, D
					case 0x53: BIT_OP(BIT, 2, E);						break; // BIT 2, E
					case 0x54: BIT_OP(BIT, 2, H);						break; // BIT 2, H
					case 0x55: BIT_OP(BIT, 2, L);						break; // BIT 2, L
					case 0x56: BIT_TE_IND(BIT, 2, L, H);				break; // BIT 2, (HL)
					case 0x57: BIT_OP(BIT, 2, A);						break; // BIT 2, A
					case 0x58: BIT_OP(BIT, 3, B);						break; // BIT 3, B
					case 0x59: BIT_OP(BIT, 3, C);						break; // BIT 3, C
					case 0x5A: BIT_OP(BIT, 3, D);						break; // BIT 3, D
					case 0x5B: BIT_OP(BIT, 3, E);						break; // BIT 3, E
					case 0x5C: BIT_OP(BIT, 3, H);						break; // BIT 3, H
					case 0x5D: BIT_OP(BIT, 3, L);						break; // BIT 3, L
					case 0x5E: BIT_TE_IND(BIT, 3, L, H);				break; // BIT 3, (HL)
					case 0x5F: BIT_OP(BIT, 3, A);						break; // BIT 3, A
					case 0x60: BIT_OP(BIT, 4, B);						break; // BIT 4, B
					case 0x61: BIT_OP(BIT, 4, C);						break; // BIT 4, C
					case 0x62: BIT_OP(BIT, 4, D);						break; // BIT 4, D
					case 0x63: BIT_OP(BIT, 4, E);						break; // BIT 4, E
					case 0x64: BIT_OP(BIT, 4, H);						break; // BIT 4, H
					case 0x65: BIT_OP(BIT, 4, L);						break; // BIT 4, L
					case 0x66: BIT_TE_IND(BIT, 4, L, H);				break; // BIT 4, (HL)
					case 0x67: BIT_OP(BIT, 4, A);						break; // BIT 4, A
					case 0x68: BIT_OP(BIT, 5, B);						break; // BIT 5, B
					case 0x69: BIT_OP(BIT, 5, C);						break; // BIT 5, C
					case 0x6A: BIT_OP(BIT, 5, D);						break; // BIT 5, D
					case 0x6B: BIT_OP(BIT, 5, E);						break; // BIT 5, E
					case 0x6C: BIT_OP(BIT, 5, H);						break; // BIT 5, H
					case 0x6D: BIT_OP(BIT, 5, L);						break; // BIT 5, L
					case 0x6E: BIT_TE_IND(BIT, 5, L, H);				break; // BIT 5, (HL)
					case 0x6F: BIT_OP(BIT, 5, A);						break; // BIT 5, A
					case 0x70: BIT_OP(BIT, 6, B);						break; // BIT 6, B
					case 0x71: BIT_OP(BIT, 6, C);						break; // BIT 6, C
					case 0x72: BIT_OP(BIT, 6, D);						break; // BIT 6, D
					case 0x73: BIT_OP(BIT, 6, E);						break; // BIT 6, E
					case 0x74: BIT_OP(BIT, 6, H);						break; // BIT 6, H
					case 0x75: BIT_OP(BIT, 6, L);						break; // BIT 6, L
					case 0x76: BIT_TE_IND(BIT, 6, L, H);				break; // BIT 6, (HL)
					case 0x77: BIT_OP(BIT, 6, A);						break; // BIT 6, A
					case 0x78: BIT_OP(BIT, 7, B);						break; // BIT 7, B
					case 0x79: BIT_OP(BIT, 7, C);						break; // BIT 7, C
					case 0x7A: BIT_OP(BIT, 7, D);						break; // BIT 7, D
					case 0x7B: BIT_OP(BIT, 7, E);						break; // BIT 7, E
					case 0x7C: BIT_OP(BIT, 7, H);						break; // BIT 7, H
					case 0x7D: BIT_OP(BIT, 7, L);						break; // BIT 7, L
					case 0x7E: BIT_TE_IND(BIT, 7, L, H);				break; // BIT 7, (HL)
					case 0x7F: BIT_OP(BIT, 7, A);						break; // BIT 7, A
					case 0x80: BIT_OP(RES, 0, B);						break; // RES 0, B
					case 0x81: BIT_OP(RES, 0, C);						break; // RES 0, C
					case 0x82: BIT_OP(RES, 0, D);						break; // RES 0, D
					case 0x83: BIT_OP(RES, 0, E);						break; // RES 0, E
					case 0x84: BIT_OP(RES, 0, H);						break; // RES 0, H
					case 0x85: BIT_OP(RES, 0, L);						break; // RES 0, L
					case 0x86: BIT_OP_IND(RES, 0, L, H);				break; // RES 0, (HL)
					case 0x87: BIT_OP(RES, 0, A);						break; // RES 0, A
					case 0x88: BIT_OP(RES, 1, B);						break; // RES 1, B
					case 0x89: BIT_OP(RES, 1, C);						break; // RES 1, C
					case 0x8A: BIT_OP(RES, 1, D);						break; // RES 1, D
					case 0x8B: BIT_OP(RES, 1, E);						break; // RES 1, E
					case 0x8C: BIT_OP(RES, 1, H);						break; // RES 1, H
					case 0x8D: BIT_OP(RES, 1, L);						break; // RES 1, L
					case 0x8E: BIT_OP_IND(RES, 1, L, H);				break; // RES 1, (HL)
					case 0x8F: BIT_OP(RES, 1, A);						break; // RES 1, A
					case 0x90: BIT_OP(RES, 2, B);						break; // RES 2, B
					case 0x91: BIT_OP(RES, 2, C);						break; // RES 2, C
					case 0x92: BIT_OP(RES, 2, D);						break; // RES 2, D
					case 0x93: BIT_OP(RES, 2, E);						break; // RES 2, E
					case 0x94: BIT_OP(RES, 2, H);						break; // RES 2, H
					case 0x95: BIT_OP(RES, 2, L);						break; // RES 2, L
					case 0x96: BIT_OP_IND(RES, 2, L, H);				break; // RES 2, (HL)
					case 0x97: BIT_OP(RES, 2, A);						break; // RES 2, A
					case 0x98: BIT_OP(RES, 3, B);						break; // RES 3, B
					case 0x99: BIT_OP(RES, 3, C);						break; // RES 3, C
					case 0x9A: BIT_OP(RES, 3, D);						break; // RES 3, D
					case 0x9B: BIT_OP(RES, 3, E);						break; // RES 3, E
					case 0x9C: BIT_OP(RES, 3, H);						break; // RES 3, H
					case 0x9D: BIT_OP(RES, 3, L);						break; // RES 3, L
					case 0x9E: BIT_OP_IND(RES, 3, L, H);				break; // RES 3, (HL)
					case 0x9F: BIT_OP(RES, 3, A);						break; // RES 3, A
					case 0xA0: BIT_OP(RES, 4, B);						break; // RES 4, B
					case 0xA1: BIT_OP(RES, 4, C);						break; // RES 4, C
					case 0xA2: BIT_OP(RES, 4, D);						break; // RES 4, D
					case 0xA3: BIT_OP(RES, 4, E);						break; // RES 4, E
					case 0xA4: BIT_OP(RES, 4, H);						break; // RES 4, H
					case 0xA5: BIT_OP(RES, 4, L);						break; // RES 4, L
					case 0xA6: BIT_OP_IND(RES, 4, L, H);				break; // RES 4, (HL)
					case 0xA7: BIT_OP(RES, 4, A);						break; // RES 4, A
					case 0xA8: BIT_OP(RES, 5, B);						break; // RES 5, B
					case 0xA9: BIT_OP(RES, 5, C);						break; // RES 5, C
					case 0xAA: BIT_OP(RES, 5, D);						break; // RES 5, D
					case 0xAB: BIT_OP(RES, 5, E);						break; // RES 5, E
					case 0xAC: BIT_OP(RES, 5, H);						break; // RES 5, H
					case 0xAD: BIT_OP(RES, 5, L);						break; // RES 5, L
					case 0xAE: BIT_OP_IND(RES, 5, L, H);				break; // RES 5, (HL)
					case 0xAF: BIT_OP(RES, 5, A);						break; // RES 5, A
					case 0xB0: BIT_OP(RES, 6, B);						break; // RES 6, B
					case 0xB1: BIT_OP(RES, 6, C);						break; // RES 6, C
					case 0xB2: BIT_OP(RES, 6, D);						break; // RES 6, D
					case 0xB3: BIT_OP(RES, 6, E);						break; // RES 6, E
					case 0xB4: BIT_OP(RES, 6, H);						break; // RES 6, H
					case 0xB5: BIT_OP(RES, 6, L);						break; // RES 6, L
					case 0xB6: BIT_OP_IND(RES, 6, L, H);				break; // RES 6, (HL)
					case 0xB7: BIT_OP(RES, 6, A);						break; // RES 6, A
					case 0xB8: BIT_OP(RES, 7, B);						break; // RES 7, B
					case 0xB9: BIT_OP(RES, 7, C);						break; // RES 7, C
					case 0xBA: BIT_OP(RES, 7, D);						break; // RES 7, D
					case 0xBB: BIT_OP(RES, 7, E);						break; // RES 7, E
					case 0xBC: BIT_OP(RES, 7, H);						break; // RES 7, H
					case 0xBD: BIT_OP(RES, 7, L);						break; // RES 7, L
					case 0xBE: BIT_OP_IND(RES, 7, L, H);				break; // RES 7, (HL)
					case 0xBF: BIT_OP(RES, 7, A);						break; // RES 7, A
					case 0xC0: BIT_OP(SET, 0, B);						break; // SET 0, B
					case 0xC1: BIT_OP(SET, 0, C);						break; // SET 0, C
					case 0xC2: BIT_OP(SET, 0, D);						break; // SET 0, D
					case 0xC3: BIT_OP(SET, 0, E);						break; // SET 0, E
					case 0xC4: BIT_OP(SET, 0, H);						break; // SET 0, H
					case 0xC5: BIT_OP(SET, 0, L);						break; // SET 0, L
					case 0xC6: BIT_OP_IND(SET, 0, L, H);				break; // SET 0, (HL)
					case 0xC7: BIT_OP(SET, 0, A);						break; // SET 0, A
					case 0xC8: BIT_OP(SET, 1, B);						break; // SET 1, B
					case 0xC9: BIT_OP(SET, 1, C);						break; // SET 1, C
					case 0xCA: BIT_OP(SET, 1, D);						break; // SET 1, D
					case 0xCB: BIT_OP(SET, 1, E);						break; // SET 1, E
					case 0xCC: BIT_OP(SET, 1, H);						break; // SET 1, H
					case 0xCD: BIT_OP(SET, 1, L);						break; // SET 1, L
					case 0xCE: BIT_OP_IND(SET, 1, L, H);				break; // SET 1, (HL)
					case 0xCF: BIT_OP(SET, 1, A);						break; // SET 1, A
					case 0xD0: BIT_OP(SET, 2, B);						break; // SET 2, B
					case 0xD1: BIT_OP(SET, 2, C);						break; // SET 2, C
					case 0xD2: BIT_OP(SET, 2, D);						break; // SET 2, D
					case 0xD3: BIT_OP(SET, 2, E);						break; // SET 2, E
					case 0xD4: BIT_OP(SET, 2, H);						break; // SET 2, H
					case 0xD5: BIT_OP(SET, 2, L);						break; // SET 2, L
					case 0xD6: BIT_OP_IND(SET, 2, L, H);				break; // SET 2, (HL)
					case 0xD7: BIT_OP(SET, 2, A);						break; // SET 2, A
					case 0xD8: BIT_OP(SET, 3, B);						break; // SET 3, B
					case 0xD9: BIT_OP(SET, 3, C);						break; // SET 3, C
					case 0xDA: BIT_OP(SET, 3, D);						break; // SET 3, D
					case 0xDB: BIT_OP(SET, 3, E);						break; // SET 3, E
					case 0xDC: BIT_OP(SET, 3, H);						break; // SET 3, H
					case 0xDD: BIT_OP(SET, 3, L);						break; // SET 3, L
					case 0xDE: BIT_OP_IND(SET, 3, L, H);				break; // SET 3, (HL)
					case 0xDF: BIT_OP(SET, 3, A);						break; // SET 3, A
					case 0xE0: BIT_OP(SET, 4, B);						break; // SET 4, B
					case 0xE1: BIT_OP(SET, 4, C);						break; // SET 4, C
					case 0xE2: BIT_OP(SET, 4, D);						break; // SET 4, D
					case 0xE3: BIT_OP(SET, 4, E);						break; // SET 4, E
					case 0xE4: BIT_OP(SET, 4, H);						break; // SET 4, H
					case 0xE5: BIT_OP(SET, 4, L);						break; // SET 4, L
					case 0xE6: BIT_OP_IND(SET, 4, L, H);				break; // SET 4, (HL)
					case 0xE7: BIT_OP(SET, 4, A);						break; // SET 4, A
					case 0xE8: BIT_OP(SET, 5, B);						break; // SET 5, B
					case 0xE9: BIT_OP(SET, 5, C);						break; // SET 5, C
					case 0xEA: BIT_OP(SET, 5, D);						break; // SET 5, D
					case 0xEB: BIT_OP(SET, 5, E);						break; // SET 5, E
					case 0xEC: BIT_OP(SET, 5, H);						break; // SET 5, H
					case 0xED: BIT_OP(SET, 5, L);						break; // SET 5, L
					case 0xEE: BIT_OP_IND(SET, 5, L, H);				break; // SET 5, (HL)
					case 0xEF: BIT_OP(SET, 5, A);						break; // SET 5, A
					case 0xF0: BIT_OP(SET, 6, B);						break; // SET 6, B
					case 0xF1: BIT_OP(SET, 6, C);						break; // SET 6, C
					case 0xF2: BIT_OP(SET, 6, D);						break; // SET 6, D
					case 0xF3: BIT_OP(SET, 6, E);						break; // SET 6, E
					case 0xF4: BIT_OP(SET, 6, H);						break; // SET 6, H
					case 0xF5: BIT_OP(SET, 6, L);						break; // SET 6, L
					case 0xF6: BIT_OP_IND(SET, 6, L, H);				break; // SET 6, (HL)
					case 0xF7: BIT_OP(SET, 6, A);						break; // SET 6, A
					case 0xF8: BIT_OP(SET, 7, B);						break; // SET 7, B
					case 0xF9: BIT_OP(SET, 7, C);						break; // SET 7, C
					case 0xFA: BIT_OP(SET, 7, D);						break; // SET 7, D
					case 0xFB: BIT_OP(SET, 7, E);						break; // SET 7, E
					case 0xFC: BIT_OP(SET, 7, H);						break; // SET 7, H
					case 0xFD: BIT_OP(SET, 7, L);						break; // SET 7, L
					case 0xFE: BIT_OP_IND(SET, 7, L, H);				break; // SET 7, (HL)
					case 0xFF: BIT_OP(SET, 7, A);						break; // SET 7, A
				}

				for (int j = 0; j < cur_instr.Length; j++)
				{
					instr_table[256 * 60 + i * 60 + j] = cur_instr[j];
				}
			}

			// other miscellaneous vectors

			// reset
			instr_table[256 * 60 * 2] = IDLE;
			instr_table[256 * 60 * 2 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 3] = IDLE;
			instr_table[256 * 60 * 2 + 4] = IDLE;
			instr_table[256 * 60 * 2 + 5] = IDLE;
			instr_table[256 * 60 * 2 + 6] = IDLE;
			instr_table[256 * 60 * 2 + 7] = IDLE;
			instr_table[256 * 60 * 2 + 8] = IDLE;
			instr_table[256 * 60 * 2 + 9] = IDLE;
			instr_table[256 * 60 * 2 + 10] = HALT_CHK;
			instr_table[256 * 60 * 2 + 11] = OP;

			// exit halt / stop loop
			instr_table[256 * 60 * 2 + 60] = IDLE;
			instr_table[256 * 60 * 2 + 60 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 + 2] = HALT_CHK;
			instr_table[256 * 60 * 2 + 60 + 3] = OP;

			// skipped loop
			instr_table[256 * 60 * 2 + 60 * 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 2 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 2 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 2 + 3] = HALT;
			instr_table[256 * 60 * 2 + 60 * 2 + 4] = (ushort)0;

			// GBC Halt loop
			instr_table[256 * 60 * 2 + 60 * 3] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 3 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 3 + 2] = HALT_CHK;
			instr_table[256 * 60 * 2 + 60 * 3 + 3] = HALT;
			instr_table[256 * 60 * 2 + 60 * 3 + 4] = (ushort)0;

			// spec Halt loop
			instr_table[256 * 60 * 2 + 60 * 4] = HALT_CHK;
			instr_table[256 * 60 * 2 + 60 * 4 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 4 + 2] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 4 + 3] = HALT;
			instr_table[256 * 60 * 2 + 60 * 4 + 4] = (ushort)0;

			// Stop loop
			instr_table[256 * 60 * 2 + 60 * 5] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 5 + 1] = IDLE;
			instr_table[256 * 60 * 2 + 60 * 5 + 2] = HALT_CHK;
			instr_table[256 * 60 * 2 + 60 * 5 + 3] = STOP;

			// interrupt vectors
			INTERRUPT_();

			for (int i = 0; i < cur_instr.Length; i++)
			{
				instr_table[256 * 60 * 2 + 60 * 6 + i] = cur_instr[i];
			}

			INTERRUPT_GBC_NOP();

			for (int i = 0; i < cur_instr.Length; i++)
			{
				instr_table[256 * 60 * 2 + 60 * 7 + i] = cur_instr[i];
			}

		}
	}
}