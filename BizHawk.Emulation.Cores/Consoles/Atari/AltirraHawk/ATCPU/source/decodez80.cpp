//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2016 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>
#include <at/atcpu/decodez80.h>
#include <at/atcpu/statesz80.h>

void ATCPUDecoderGeneratorZ80::RebuildTables(ATCPUDecoderTablesZ80& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints) {
	using namespace ATCPUStatesZ80;

	mbStopOnBRK = stopOnBRK;
	mpDstState = dst.mDecodeHeap;

	dst.mIrqSequence = 0;

	*mpDstState++ = kZ80StatePCToData;
	*mpDstState++ = kZ80StateWait_3T;
	*mpDstState++ = kZ80StatePush;
	*mpDstState++ = kZ80StateWait_3T;
	*mpDstState++ = kZ80StatePush;
	*mpDstState++ = kZ80StateRstIntVec;
	*mpDstState++ = kZ80StateReadL;
	*mpDstState++ = kZ80StateReadH;
	*mpDstState++ = kZ80StateJP;
	*mpDstState++ = enableBreakpoints ? kZ80StateReadOpcode : kZ80StateReadOpcodeNoBreak;

	dst.mNmiSequence = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = kZ80StatePCToData;
	*mpDstState++ = kZ80StateWait_3T;
	*mpDstState++ = kZ80StatePush;
	*mpDstState++ = kZ80StateWait_3T;
	*mpDstState++ = kZ80StatePush;
	*mpDstState++ = kZ80StateRst66;
	*mpDstState++ = enableBreakpoints ? kZ80StateReadOpcode : kZ80StateReadOpcodeNoBreak;

	DecodeInsns(dst, dst.mInsns, &ATCPUDecoderGeneratorZ80::DecodeInsn, historyTracing, enableBreakpoints);
	DecodeInsns(dst, dst.mInsnsCB, &ATCPUDecoderGeneratorZ80::DecodeInsnCB<false>, false, enableBreakpoints);
	DecodeInsns(dst, dst.mInsnsED, &ATCPUDecoderGeneratorZ80::DecodeInsnED, false, enableBreakpoints);
	DecodeInsns(dst, dst.mInsnsDDFD, &ATCPUDecoderGeneratorZ80::DecodeInsnDDFD, false, enableBreakpoints);
	DecodeInsns(dst, dst.mInsnsDDFDCB, &ATCPUDecoderGeneratorZ80::DecodeInsnCB<true>, false, enableBreakpoints);
}

void ATCPUDecoderGeneratorZ80::DecodeInsns(ATCPUDecoderTablesZ80& dst, uint16 *p, bool (ATCPUDecoderGeneratorZ80::*pfn)(uint8), bool historyTracing, bool enableBreakpoints) {
	using namespace ATCPUStatesZ80;

	const auto stateReadOpcode = enableBreakpoints ? kZ80StateReadOpcode : kZ80StateReadOpcodeNoBreak;

	for(int i=0; i<256; ++i) {
		*p++ = (uint16)(mpDstState - dst.mDecodeHeap);

		if (historyTracing)
			*mpDstState++ = kZ80StateAddToHistory;

		if (!(this->*pfn)((uint8)i))
			*mpDstState++ = kZ80StateBreakOnUnsupportedOpcode;

		*mpDstState++ = kZ80StateWait_4T;
		*mpDstState++ = stateReadOpcode;
	}
}

bool ATCPUDecoderGeneratorZ80::DecodeInsn(uint8 opcode) {
	using namespace ATCPUStatesZ80;

	switch(opcode) {
		case 0x00:		// NOP (4T)
			return true;

		case 0x01:		// LD BC,imm16 (10T)
		case 0x11:		// LD DE,imm16 (10T)
		case 0x21:		// LD HL,imm16 (10T)
		case 0x31:		// LD SP,imm16 (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			DecodeDataToArg16(opcode);
			return true;

		case 0x02:		// LD (BC),A (7T)
		case 0x12:		// LD (DE),A (7T)
			DecodeArgToAddr16(opcode);
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateAToData;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x03:		// INC BC (6T)
		case 0x13:		// INC DE (6T)
		case 0x23:		// INC HL (6T)
		case 0x33:		// INC SP (6T)
			*mpDstState++ = kZ80StateWait_2T;
			DecodeArgToData16(opcode);
			*mpDstState++ = kZ80StateInc16;
			DecodeDataToArg16(opcode);
			return true;

		case 0x04:		// INC B (4T)
		case 0x0C:		// INC C (4T)
		case 0x14:		// INC D (4T)
		case 0x1C:		// INC E (4T)
		case 0x24:		// INC H (4T)
		case 0x2C:		// INC L (4T)
		case 0x3C:		// INC A (4T)
			DecodeArgToData(opcode >> 3, false);
			*mpDstState++ = kZ80StateInc;
			DecodeDataToArg(opcode >> 3, false);
			return true;

		case 0x05:		// DEC B (4T)
		case 0x0D:		// DEC C (4T)
		case 0x15:		// DEC D (4T)
		case 0x1D:		// DEC E (4T)
		case 0x25:		// DEC H (4T)
		case 0x2D:		// DEC L (4T)
		case 0x3D:		// DEC A (4T)
			DecodeArgToData(opcode >> 3, false);
			*mpDstState++ = kZ80StateDec;
			DecodeDataToArg(opcode >> 3, false);
			return true;

		case 0x06:		// LD B,imm (7T)
		case 0x0E:		// LD C,imm (7T)
		case 0x16:		// LD D,imm (7T)
		case 0x1E:		// LD E,imm (7T)
		case 0x26:		// LD H,imm (7T)
		case 0x2E:		// LD L,imm (7T)
		case 0x36:		// LD (HL),imm (10T)
		case 0x3E:		// LD A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			DecodeDataToArg(opcode >> 3, false);
			return true;

		case 0x07:		// RLCA (4T)
			*mpDstState++ = kZ80StateRlca;
			return true;

		case 0x08:		// EX AF,AF' (4T)
			*mpDstState++ = kZ80StateExaf;
			return true;

		case 0x09:		// ADD HL,BC (11T)
		case 0x19:		// ADD HL,DE (11T)
		case 0x29:		// ADD HL,HL (11T)
		case 0x39:		// ADD HL,SP (11T)
			*mpDstState++ = kZ80StateWait_7T;
			switch(opcode & 0x30) {
				case 0x00: *mpDstState++ = kZ80StateBCToData; break;
				case 0x10: *mpDstState++ = kZ80StateDEToData; break;
				case 0x20: *mpDstState++ = kZ80StateHLToData; break;
				case 0x30: *mpDstState++ = kZ80StateSPToData; break;
			}

			*mpDstState++ = kZ80StateAddToHL;
			return true;

		case 0x0A:		// LD A,(BC) (7T)
		case 0x1A:		// LD A,(DE) (7T)
			DecodeArgToAddr16(opcode);
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateDataToA;
			return true;

		case 0x0B:		// DEC BC (6T)
		case 0x1B:		// DEC DE (6T)
		case 0x2B:		// DEC HL (6T)
		case 0x3B:		// DEC SP (6T)
			*mpDstState++ = kZ80StateWait_2T;
			DecodeArgToData16(opcode);
			*mpDstState++ = kZ80StateDec16;
			DecodeDataToArg16(opcode);
			return true;

		case 0x0F:		// RRCA (4T)
			*mpDstState++ = kZ80StateRrca;
			return true;

		case 0x10:		// DJNZ r8 (13/8T)
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateDjnz;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateJR;
			return true;

		case 0x17:		// RLA (4T)
			*mpDstState++ = kZ80StateRla;
			return true;

		case 0x18:		// JR r8 (12T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateJR;
			return true;

		case 0x1F:		// RRA (4T)
			*mpDstState++ = kZ80StateRra;
			return true;

		case 0x20:		// JR NZ,r8 (12/8T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessNZ;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateJR;
			return true;

		case 0x22:		// LD (abs),HL (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToData;
			*mpDstState++ = kZ80StateWriteL;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateWriteH;
			return true;

		case 0x27:		// DAA
			*mpDstState++ = kZ80StateDaa;
			return true;

		case 0x28:		// JR Z,r8 (7/12T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessZ;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateJR;
			return true;

		case 0x2A:		// LD HL,(abs) (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadL;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadH;
			*mpDstState++ = kZ80StateDataToHL;
			return true;

		case 0x2F:		// CPL (4T)
			*mpDstState++ = kZ80StateCplToA;
			return true;

		case 0x30:		// JR NC,r8 (7/12T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessNC;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateJR;
			return true;

		case 0x32:		// LD (abs),A (13T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateAToData;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x34:		// INC (HL) (11T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateInc;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x35:		// DEC (HL) (11T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateDec;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x37:		// SCF (4T)
			*mpDstState++ = kZ80StateSCF;
			return true;

		case 0x38:		// JR C,r8 (7/12T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessC;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateJR;
			return true;

		case 0x3A:		// LD A,(abs) (13T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateDataToA;
			return true;

		case 0x3F:		// CCF (4T)
			*mpDstState++ = kZ80StateCCF;
			return true;

		case 0x40:		// LD B,B (4T)
		case 0x41:		// LD B,C (4T)
		case 0x42:		// LD B,D (4T)
		case 0x43:		// LD B,E (4T)
		case 0x44:		// LD B,H (4T)
		case 0x45:		// LD B,L (4T)
		case 0x46:		// LD B,(HL) (7T)
		case 0x47:		// LD B,A (4T)
		case 0x48:		// LD C,B (4T)
		case 0x49:		// LD C,C (4T)
		case 0x4A:		// LD C,D (4T)
		case 0x4B:		// LD C,E (4T)
		case 0x4C:		// LD C,H (4T)
		case 0x4D:		// LD C,L (4T)
		case 0x4E:		// LD C,(HL) (7T)
		case 0x4F:		// LD C,A (4T)
		case 0x50:		// LD D,B (4T)
		case 0x51:		// LD D,C (4T)
		case 0x52:		// LD D,D (4T)
		case 0x53:		// LD D,E (4T)
		case 0x54:		// LD D,H (4T)
		case 0x55:		// LD D,L (4T)
		case 0x56:		// LD D,(HL) (7T)
		case 0x57:		// LD D,A (4T)
		case 0x58:		// LD E,B (4T)
		case 0x59:		// LD E,C (4T)
		case 0x5A:		// LD E,D (4T)
		case 0x5B:		// LD E,E (4T)
		case 0x5C:		// LD E,H (4T)
		case 0x5D:		// LD E,L (4T)
		case 0x5E:		// LD E,(HL) (7T)
		case 0x5F:		// LD E,A (4T)
		case 0x60:		// LD H,B (4T)
		case 0x61:		// LD H,C (4T)
		case 0x62:		// LD H,D (4T)
		case 0x63:		// LD H,E (4T)
		case 0x64:		// LD H,H (4T)
		case 0x65:		// LD H,L (4T)
		case 0x66:		// LD H,(HL) (7T)
		case 0x67:		// LD H,A (4T)
		case 0x68:		// LD L,B (4T)
		case 0x69:		// LD L,C (4T)
		case 0x6A:		// LD L,D (4T)
		case 0x6B:		// LD L,E (4T)
		case 0x6C:		// LD L,H (4T)
		case 0x6D:		// LD L,L (4T)
		case 0x6E:		// LD L,(HL) (7T)
		case 0x6F:		// LD L,A (4T)
		case 0x70:		// LD (HL),B (4T)
		case 0x71:		// LD (HL),C (4T)
		case 0x72:		// LD (HL),D (4T)
		case 0x73:		// LD (HL),E (4T)
		case 0x74:		// LD (HL),H (4T)
		case 0x75:		// LD (HL),L (4T)
		case 0x77:		// LD (HL),A (4T)
		case 0x78:		// LD A,B (4T)
		case 0x79:		// LD A,C (4T)
		case 0x7A:		// LD A,D (4T)
		case 0x7B:		// LD A,E (4T)
		case 0x7C:		// LD A,H (4T)
		case 0x7D:		// LD A,L (4T)
		case 0x7E:		// LD A,(HL) (7T)
		case 0x7F:		// LD A,A (4T)
			DecodeArgToData(opcode, false);
			DecodeDataToArg(opcode >> 3, false);
			return true;

		case 0x76:		// HALT
			*mpDstState++ = kZ80StateHaltEnter;
			*mpDstState++ = kZ80StateHalt;
			return true;

		case 0x80:		// ADD A,B (4T)
		case 0x81:		// ADD A,C (4T)
		case 0x82:		// ADD A,D (4T)
		case 0x83:		// ADD A,E (4T)
		case 0x84:		// ADD A,H (4T)
		case 0x85:		// ADD A,L (4T)
		case 0x86:		// ADD A,(HL) (7T)
		case 0x87:		// ADD A,A (4T)
		case 0x88:		// ADC A,B (4T)
		case 0x89:		// ADC A,C (4T)
		case 0x8A:		// ADC A,D (4T)
		case 0x8B:		// ADC A,E (4T)
		case 0x8C:		// ADC A,H (4T)
		case 0x8D:		// ADC A,L (4T)
		case 0x8E:		// ADC A,(HL) (7T)
		case 0x8F:		// ADC A,A (4T)
		case 0x90:		// SUB A,B (4T)
		case 0x91:		// SUB A,C (4T)
		case 0x92:		// SUB A,D (4T)
		case 0x93:		// SUB A,E (4T)
		case 0x94:		// SUB A,H (4T)
		case 0x95:		// SUB A,L (4T)
		case 0x96:		// SUB A,(HL) (7T)
		case 0x97:		// SUB A,A (4T)
		case 0x98:		// SBC A,B (4T)
		case 0x99:		// SBC A,C (4T)
		case 0x9A:		// SBC A,D (4T)
		case 0x9B:		// SBC A,E (4T)
		case 0x9C:		// SBC A,H (4T)
		case 0x9D:		// SBC A,L (4T)
		case 0x9E:		// SBC A,(HL) (7T)
		case 0x9F:		// SBC A,A (4T)
		case 0xA0:		// AND A,B (4T)
		case 0xA1:		// AND A,C (4T)
		case 0xA2:		// AND A,D (4T)
		case 0xA3:		// AND A,E (4T)
		case 0xA4:		// AND A,H (4T)
		case 0xA5:		// AND A,L (4T)
		case 0xA6:		// AND A,(HL) (7T)
		case 0xA7:		// AND A,A (4T)
		case 0xA8:		// XOR B (4T)
		case 0xA9:		// XOR C (4T)
		case 0xAA:		// XOR D (4T)
		case 0xAB:		// XOR E (4T)
		case 0xAC:		// XOR H (4T)
		case 0xAD:		// XOR L (4T)
		case 0xAE:		// XOR (HL) (7T)
		case 0xAF:		// XOR A (4T)
		case 0xB0:		// OR A,B (4T)
		case 0xB1:		// OR A,C (4T)
		case 0xB2:		// OR A,D (4T)
		case 0xB3:		// OR A,E (4T)
		case 0xB4:		// OR A,H (4T)
		case 0xB5:		// OR A,L (4T)
		case 0xB6:		// OR A,(HL) (7T)
		case 0xB7:		// OR A,A (4T)
		case 0xB8:		// CP B (4T)
		case 0xB9:		// CP C (4T)
		case 0xBA:		// CP D (4T)
		case 0xBB:		// CP E (4T)
		case 0xBC:		// CP H (4T)
		case 0xBD:		// CP L (4T)
		case 0xBE:		// CP (HL) (7T)
		case 0xBF:		// CP A (4T)
			DecodeArgToData(opcode, false);

			switch(opcode & 0xF8) {
				case 0x80: *mpDstState++ = kZ80StateAddToA; break;
				case 0x88: *mpDstState++ = kZ80StateAdcToA; break;
				case 0x90: *mpDstState++ = kZ80StateSubToA; break;
				case 0x98: *mpDstState++ = kZ80StateSbcToA; break;
				case 0xA0: *mpDstState++ = kZ80StateAndToA; break;
				case 0xA8: *mpDstState++ = kZ80StateXorToA; break;
				case 0xB0: *mpDstState++ = kZ80StateOrToA; break;
				case 0xB8: *mpDstState++ = kZ80StateCpToA; break;
			}
			return true;

		case 0xC0:		// RET NZ (11/5T)
		case 0xC8:		// RET Z (11/5T)
		case 0xD0:		// RET NC (11/5T)
		case 0xD8:		// RET C (11/5T)
		case 0xE0:		// RET PO (11/5T)
		case 0xE8:		// RET PE (11/5T)
		case 0xF0:		// RET P (11/5T)
		case 0xF8:		// RET M (11/5T)
			*mpDstState++ = kZ80StateWait_1T;

			switch((opcode >> 3) & 7) {
				case 0: *mpDstState++ = kZ80StateSkipUnlessNZ; break;
				case 1: *mpDstState++ = kZ80StateSkipUnlessZ; break;
				case 2: *mpDstState++ = kZ80StateSkipUnlessNC; break;
				case 3: *mpDstState++ = kZ80StateSkipUnlessC; break;
				case 4: *mpDstState++ = kZ80StateSkipUnlessPO; break;
				case 5: *mpDstState++ = kZ80StateSkipUnlessPE; break;
				case 6: *mpDstState++ = kZ80StateSkipUnlessP; break;
				case 7: *mpDstState++ = kZ80StateSkipUnlessM; break;
			}

			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToPC;
			return true;

		case 0xC1:		// POP BC (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToBC;
			return true;

		case 0xC2:		// JP NZ,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessNZ;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xC3:		// JP (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xC4:		// CALL NZ,abs (17/10T)
		case 0xCC:		// CALL Z,abs (17/10T)
		case 0xD4:		// CALL NC,abs (17/10T)
		case 0xDC:		// CALL C,abs (17/10T)
		case 0xE4:		// CALL PO,abs (17/10T)
		case 0xEC:		// CALL PE,abs (17/10T)
		case 0xF4:		// CALL P,abs (17/10T)
		case 0xFC:		// CALL M,abs (17/10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;

			switch(opcode) {
				case 0xC4: *mpDstState++ = kZ80StateSkipUnlessNZ; break;
				case 0xCC: *mpDstState++ = kZ80StateSkipUnlessZ; break;
				case 0xD4: *mpDstState++ = kZ80StateSkipUnlessNC; break;
				case 0xDC: *mpDstState++ = kZ80StateSkipUnlessC; break;
				case 0xE4: *mpDstState++ = kZ80StateSkipUnlessPO; break;
				case 0xEC: *mpDstState++ = kZ80StateSkipUnlessPE; break;
				case 0xF4: *mpDstState++ = kZ80StateSkipUnlessP; break;
				case 0xFC: *mpDstState++ = kZ80StateSkipUnlessM; break;
			}

			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StatePCToData;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateAddrToPC;
			return true;

		case 0xC5:		// PUSH BC (10T)
			*mpDstState++ = kZ80StateBCToData;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			return true;

		case 0xC6:		// ADD A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateAddToA;
			return true;

		case 0xC7:		// RST 00h
		case 0xCF:		// RST 08h
		case 0xD7:		// RST 10h
		case 0xDF:		// RST 18h
		case 0xE7:		// RST 20h
		case 0xEF:		// RST 28h
		case 0xF7:		// RST 30h
		case 0xFF:		// RST 38h
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePCToData;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;

			switch((opcode >> 3) & 7) {
				case 0: *mpDstState++ = kZ80StateRst00; break;
				case 1: *mpDstState++ = kZ80StateRst08; break;
				case 2: *mpDstState++ = kZ80StateRst10; break;
				case 3: *mpDstState++ = kZ80StateRst18; break;
				case 4: *mpDstState++ = kZ80StateRst20; break;
				case 5: *mpDstState++ = kZ80StateRst28; break;
				case 6: *mpDstState++ = kZ80StateRst30; break;
				case 7: *mpDstState++ = kZ80StateRst38; break;
			}
			return true;

		case 0xC9:		// RET (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xCA:		// JP Z,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessZ;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xCB:		// Bit opcode extension
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadOpcodeCB;
			return true;

		case 0xCD:		// CALL abs (17T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePCp2ToData;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xCE:		// ADC A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateAdcToA;
			return true;

		case 0xD1:		// POP DE (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToDE;
			return true;

		case 0xD2:		// JP NC,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessNC;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xD3:		// OUT (port),A
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateAToData;
			*mpDstState++ = kZ80StateWritePort;
			return true;

		case 0xD5:		// PUSH DE (10T)
			*mpDstState++ = kZ80StateDEToData;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			return true;

		case 0xD6:		// SUB A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSubToA;
			return true;

		case 0xD9:		// EXX (4T)
			*mpDstState++ = kZ80StateExx;
			return true;

		case 0xDA:		// JP C,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessC;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xDB:		// IN A,(port)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadPort;
			*mpDstState++ = kZ80StateDataToA;
			return true;

		case 0xDD:		// IX opcode
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadOpcodeDD;
			return true;

		case 0xDE:		// SBC A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSbcToA;
			return true;

		case 0xE1:		// POP HL (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToHL;
			return true;

		case 0xE2:		// JP PO,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessPO;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xE3:		// EX (SP),HL (19T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateSPToAddr;
			*mpDstState++ = kZ80StateReadL;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadH;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateExHLData;
			*mpDstState++ = kZ80StateWriteH;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateWriteL;
			return true;

		case 0xE5:		// PUSH HL (10T)
			*mpDstState++ = kZ80StateHLToData;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			return true;

		case 0xE6:		// AND A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateAndToA;
			return true;

		case 0xE9:		// JP (HL) (4T)
			*mpDstState++ = kZ80StateHLToData;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xEA:		// JP PE,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessPE;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xEB:		// EX DE,HL (4T)
			*mpDstState++ = kZ80StateExDEHL;
			return true;

		case 0xED:
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadOpcodeED;
			return true;

		case 0xEE:		// XOR A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateXorToA;
			return true;

		case 0xF1:		// POP AF (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToAF;
			return true;

		case 0xF2:		// JP P,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessP;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xF3:		// DI (4T)
			*mpDstState++ = kZ80StateDI;
			return true;

		case 0xF5:		// PUSH AF (10T)
			*mpDstState++ = kZ80StateAFToData;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			return true;

		case 0xF6:		// OR A,imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateOrToA;
			return true;

		case 0xF9:		// LD SP,HL (6T)
			*mpDstState++ = kZ80StateWait_2T;
			*mpDstState++ = kZ80StateHLToData;
			*mpDstState++ = kZ80StateDataToSP;
			return true;

		case 0xFA:		// JP M,abs (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateSkipUnlessM;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xFB:		// EI (4T)
			*mpDstState++ = kZ80StateEI;
			return true;

		case 0xFD:		// IY opcode
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadOpcodeFD;
			return true;

		case 0xFE:		// CP imm (7T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateCpToA;
			return true;
	}

	return false;
}

bool ATCPUDecoderGeneratorZ80::DecodeInsnED(uint8 opcode) {
	using namespace ATCPUStatesZ80;

	switch(opcode) {
		case 0x40:		// IN B,(C) (12T)
		case 0x48:		// IN C,(C) (12T)
		case 0x50:		// IN D,(C) (12T)
		case 0x58:		// IN E,(C) (12T)
		case 0x60:		// IN H,(C) (12T)
		case 0x68:		// IN L,(C) (12T)
		case 0x78:		// IN A,(C) (12T)
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadPortC;
			DecodeDataToArg(opcode >> 3, false);
			return true;

		case 0x70:		// IN (C) (12T)
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadPortC;
			return true;

		case 0x41:		// OUT (C),B (12T)
		case 0x49:		// OUT (C),C (12T)
		case 0x51:		// OUT (C),D (12T)
		case 0x59:		// OUT (C),E (12T)
		case 0x61:		// OUT (C),H (12T)
		case 0x69:		// OUT (C),L (12T)
		case 0x79:		// OUT (C),A (12T)
			DecodeArgToData(opcode >> 3, false);
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWritePortC;
			return true;

		case 0x71:		// OUT (C),0 (12T)
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80State0ToData;
			*mpDstState++ = kZ80StateWritePortC;
			return true;

		case 0x42:		// SBC HL,BC (15T)
		case 0x52:		// SBC HL,DE (15T)
		case 0x62:		// SBC HL,HL (15T)
		case 0x72:		// SBC HL,SP (15T)
			DecodeArgToData16(opcode);
			*mpDstState++ = kZ80StateWait_11T;
			*mpDstState++ = kZ80StateSbcToHL;
			return true;

		case 0x43:		// LD (abs),BC (20T)
		case 0x53:		// LD (abs),DE (20T)
		case 0x63:		// LD (abs),HL (20T)
		case 0x73:		// LD (abs),SP (20T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			DecodeArgToData16(opcode);
			*mpDstState++ = kZ80StateWriteL;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateWriteH;
			return true;

		case 0x44:		// NEG (8T)
		case 0x4C:		// NEG (8T)
		case 0x54:		// NEG (8T)
		case 0x5C:		// NEG (8T)
		case 0x64:		// NEG (8T)
		case 0x6C:		// NEG (8T)
		case 0x74:		// NEG (8T)
		case 0x7C:		// NEG (8T)
			*mpDstState++ = kZ80StateNegA;
			return true;

		case 0x45:		// RETN (14T)
		case 0x55:		// RETN (14T)
		case 0x5D:		// RETN (14T)
		case 0x65:		// RETN (14T)
		case 0x6D:		// RETN (14T)
		case 0x75:		// RETN (14T)
		case 0x7D:		// RETN (14T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateRetn;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0x46:		// IM 0 (8T)
		case 0x66:		// IM 0 (8T)
			*mpDstState++ = kZ80StateIM0_4T;
			return true;

		case 0x4E:		// IM 0/1 (8T)
		case 0x6E:		// IM 0/1 (8T)
			// stubbed for now
			*mpDstState++ = kZ80StateWait_4T;
			return true;

		case 0x56:		// IM 1 (8T)
		case 0x76:		// IM 1 (8T)
			*mpDstState++ = kZ80StateIM1_4T;
			return true;

		case 0x5E:		// IM 2 (8T)
		case 0x7E:		// IM 2 (8T)
			*mpDstState++ = kZ80StateIM2_4T;
			return true;

		case 0x47:		// LD I,A (9T)
			*mpDstState++ = kZ80StateAToI_1T;
			return true;

		case 0x4A:		// ADC HL,BC (15T)
		case 0x5A:		// ADC HL,DE (15T)
		case 0x6A:		// ADC HL,HL (15T)
		case 0x7A:		// ADC HL,SP (15T)
			DecodeArgToData16(opcode);
			*mpDstState++ = kZ80StateWait_11T;
			*mpDstState++ = kZ80StateAdcToHL;
			return true;

		case 0x4B:		// LD BC,(abs) (20T)
		case 0x5B:		// LD DE,(abs) (20T)
		case 0x6B:		// LD HL,(abs) (20T)
		case 0x7B:		// LD SP,(abs) (20T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadL;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadH;
			DecodeDataToArg16(opcode);
			return true;

		case 0x4D:		// RETI (14T)
			*mpDstState++ = kZ80StateReti;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToPC;
			return true;

		case 0x4F:		// LD R,A (9T)
			*mpDstState++ = kZ80StateAToR_1T;
			return true;

		case 0x57:		// LD A,I (9T)
			*mpDstState++ = kZ80StateIToA_1T;
			return true;

		case 0x5F:		// LD A,R (9T)
			*mpDstState++ = kZ80StateRToA_1T;
			return true;

		case 0x67:		// RRD (18T)
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateRrd;
			*mpDstState++ = kZ80StateWait_7T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x6F:		// RLD (18T)
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateRld;
			*mpDstState++ = kZ80StateWait_7T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0xA0:		// LDI (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateDEToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep2I;
			return true;

		case 0xA1:		// CPI (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateCpToA;
			*mpDstState++ = kZ80StateStep1I;
			return true;

		case 0xA2:		// INI (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadPortC;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep1I_IO;
			return true;

		case 0xA3:		// OUTI (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateWritePortC;
			*mpDstState++ = kZ80StateStep1I_IO;
			return true;

		case 0xA8:		// LDD (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateDEToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep2D;
			return true;

		case 0xA9:		// CPD (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateCpToA;
			*mpDstState++ = kZ80StateStep1D;
			return true;

		case 0xAA:		// IND (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadPortC;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep1D_IO;
			return true;

		case 0xAB:		// OUTD (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateWritePortC;
			*mpDstState++ = kZ80StateStep1D_IO;
			return true;

		case 0xB0:		// LDIR (21/16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateDEToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep2I;
			*mpDstState++ = kZ80StateRep;
			return true;

		case 0xB1:		// CPIR (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateCpToA;
			*mpDstState++ = kZ80StateStep1I;
			*mpDstState++ = kZ80StateRepNZ;
			return true;

		case 0xB2:		// INIR (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadPortC;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep1I_IO;
			*mpDstState++ = kZ80StateRep_IO;
			return true;

		case 0xB3:		// OTIR (21/16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateWritePortC;
			*mpDstState++ = kZ80StateStep1I_IO;
			*mpDstState++ = kZ80StateRep_IO;
			return true;

		case 0xB8:		// LDDR (21/16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateDEToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep2D;
			*mpDstState++ = kZ80StateRep;
			return true;

		case 0xB9:		// CPDR (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateCpToA;
			*mpDstState++ = kZ80StateStep1D;
			*mpDstState++ = kZ80StateRepNZ;
			return true;

		case 0xBA:		// INDR (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadPortC;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			*mpDstState++ = kZ80StateStep1D_IO;
			*mpDstState++ = kZ80StateRep_IO;
			return true;

		case 0xBB:		// OTDR (16T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateWritePortC;
			*mpDstState++ = kZ80StateStep1D_IO;
			*mpDstState++ = kZ80StateRep_IO;
			return true;
	}

	return false;
}

template<bool T_UseIXIY>
bool ATCPUDecoderGeneratorZ80::DecodeInsnCB(uint8 opcode) {
	using namespace ATCPUStatesZ80;

	switch(opcode) {
		case 0x00:		// RLC B (8T)
		case 0x01:		// RLC C (8T)
		case 0x02:		// RLC D (8T)
		case 0x03:		// RLC E (8T)
		case 0x04:		// RLC H (8T)
		case 0x05:		// RLC L (8T)
		case 0x07:		// RLC A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateRlc;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x06:		// RLC (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateRlc;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x08:		// RRC B (8T)
		case 0x09:		// RRC C (8T)
		case 0x0A:		// RRC D (8T)
		case 0x0B:		// RRC E (8T)
		case 0x0C:		// RRC H (8T)
		case 0x0D:		// RRC L (8T)
		case 0x0F:		// RRC A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateRrc;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x0E:		// RRC (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateRrc;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x10:		// RL B (8T)
		case 0x11:		// RL C (8T)
		case 0x12:		// RL D (8T)
		case 0x13:		// RL E (8T)
		case 0x14:		// RL H (8T)
		case 0x15:		// RL L (8T)
		case 0x17:		// RL A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateRl;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x16:		// RL (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateRl;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x18:		// RR B (8T)
		case 0x19:		// RR C (8T)
		case 0x1A:		// RR D (8T)
		case 0x1B:		// RR E (8T)
		case 0x1C:		// RR H (8T)
		case 0x1D:		// RR L (8T)
		case 0x1F:		// RR A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateRr;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x1E:		// RR (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateRr;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x20:		// SLA B (8T)
		case 0x21:		// SLA C (8T)
		case 0x22:		// SLA D (8T)
		case 0x23:		// SLA E (8T)
		case 0x24:		// SLA H (8T)
		case 0x25:		// SLA L (8T)
		case 0x27:		// SLA A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateSla;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x26:		// SLA (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateSla;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x28:		// SRA B (8T)
		case 0x29:		// SRA C (8T)
		case 0x2A:		// SRA D (8T)
		case 0x2B:		// SRA E (8T)
		case 0x2C:		// SRA H (8T)
		case 0x2D:		// SRA L (8T)
		case 0x2F:		// SRA A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateSra;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x2E:		// SRA (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateSra;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x30:		// SLL B (8T)
		case 0x31:		// SLL C (8T)
		case 0x32:		// SLL D (8T)
		case 0x33:		// SLL E (8T)
		case 0x34:		// SLL H (8T)
		case 0x35:		// SLL L (8T)
		case 0x37:		// SLL A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateSla;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x36:		// SLL (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateSla;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x38:		// SRL B (8T)
		case 0x39:		// SRL C (8T)
		case 0x3A:		// SRL D (8T)
		case 0x3B:		// SRL E (8T)
		case 0x3C:		// SRL H (8T)
		case 0x3D:		// SRL L (8T)
		case 0x3F:		// SRL A (8T)
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateSrl;
			DecodeDataToArg(opcode, false);
			return true;

		case 0x3E:		// SRL (HL) (15T)
			*mpDstState++ = kZ80StateWait_3T;
			if (T_UseIXIY) {
				*mpDstState++ = kZ80StateReadIXdToAddr;
				*mpDstState++ = kZ80StateWait_5T;
			} else {
				*mpDstState++ = kZ80StateHLToAddr;
			}
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateSrl;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		// BIT n,r
		case 0x40:		case 0x41:		case 0x42:		case 0x43:		case 0x44:		case 0x45:		case 0x46:		case 0x47:
		case 0x48:		case 0x49:		case 0x4A:		case 0x4B:		case 0x4C:		case 0x4D:		case 0x4E:		case 0x4F:
		case 0x50:		case 0x51:		case 0x52:		case 0x53:		case 0x54:		case 0x55:		case 0x56:		case 0x57:
		case 0x58:		case 0x59:		case 0x5A:		case 0x5B:		case 0x5C:		case 0x5D:		case 0x5E:		case 0x5F:
		case 0x60:		case 0x61:		case 0x62:		case 0x63:		case 0x64:		case 0x65:		case 0x66:		case 0x67:
		case 0x68:		case 0x69:		case 0x6A:		case 0x6B:		case 0x6C:		case 0x6D:		case 0x6E:		case 0x6F:
		case 0x70:		case 0x71:		case 0x72:		case 0x73:		case 0x74:		case 0x75:		case 0x76:		case 0x77:
		case 0x78:		case 0x79:		case 0x7A:		case 0x7B:		case 0x7C:		case 0x7D:		case 0x7E:		case 0x7F:
			DecodeArgToData(opcode, T_UseIXIY);
			*mpDstState++ = kZ80StateBit0 + ((opcode >> 3) & 7);
			if ((opcode & 7) == 6 && !T_UseIXIY)
				*mpDstState++ = kZ80StateWait_1T;
			return true;

		// SET n,r
		case 0x80:		case 0x81:		case 0x82:		case 0x83:		case 0x84:		case 0x85:		case 0x86:		case 0x87:
		case 0x88:		case 0x89:		case 0x8A:		case 0x8B:		case 0x8C:		case 0x8D:		case 0x8E:		case 0x8F:
		case 0x90:		case 0x91:		case 0x92:		case 0x93:		case 0x94:		case 0x95:		case 0x96:		case 0x97:
		case 0x98:		case 0x99:		case 0x9A:		case 0x9B:		case 0x9C:		case 0x9D:		case 0x9E:		case 0x9F:
		case 0xA0:		case 0xA1:		case 0xA2:		case 0xA3:		case 0xA4:		case 0xA5:		case 0xA6:		case 0xA7:
		case 0xA8:		case 0xA9:		case 0xAA:		case 0xAB:		case 0xAC:		case 0xAD:		case 0xAE:		case 0xAF:
		case 0xB0:		case 0xB1:		case 0xB2:		case 0xB3:		case 0xB4:		case 0xB5:		case 0xB6:		case 0xB7:
		case 0xB8:		case 0xB9:		case 0xBA:		case 0xBB:		case 0xBC:		case 0xBD:		case 0xBE:		case 0xBF:
			DecodeArgToData(opcode, T_UseIXIY);
			*mpDstState++ = kZ80StateRes0 + ((opcode >> 3) & 7);
			if ((opcode & 7) == 6 && !T_UseIXIY)
				*mpDstState++ = kZ80StateWait_1T;
			DecodeDataToArg(opcode, T_UseIXIY);
			return true;

		// SET n,r
		case 0xC0:		case 0xC1:		case 0xC2:		case 0xC3:		case 0xC4:		case 0xC5:		case 0xC6:		case 0xC7:
		case 0xC8:		case 0xC9:		case 0xCA:		case 0xCB:		case 0xCC:		case 0xCD:		case 0xCE:		case 0xCF:
		case 0xD0:		case 0xD1:		case 0xD2:		case 0xD3:		case 0xD4:		case 0xD5:		case 0xD6:		case 0xD7:
		case 0xD8:		case 0xD9:		case 0xDA:		case 0xDB:		case 0xDC:		case 0xDD:		case 0xDE:		case 0xDF:
		case 0xE0:		case 0xE1:		case 0xE2:		case 0xE3:		case 0xE4:		case 0xE5:		case 0xE6:		case 0xE7:
		case 0xE8:		case 0xE9:		case 0xEA:		case 0xEB:		case 0xEC:		case 0xED:		case 0xEE:		case 0xEF:
		case 0xF0:		case 0xF1:		case 0xF2:		case 0xF3:		case 0xF4:		case 0xF5:		case 0xF6:		case 0xF7:
		case 0xF8:		case 0xF9:		case 0xFA:		case 0xFB:		case 0xFC:		case 0xFD:		case 0xFE:		case 0xFF:
			DecodeArgToData(opcode, T_UseIXIY);
			*mpDstState++ = kZ80StateSet0 + ((opcode >> 3) & 7);
			if ((opcode & 7) == 6 && !T_UseIXIY)
				*mpDstState++ = kZ80StateWait_1T;
			DecodeDataToArg(opcode, T_UseIXIY);
			return true;
	}

	return false;
}

bool ATCPUDecoderGeneratorZ80::DecodeInsnDDFD(uint8 opcode) {
	using namespace ATCPUStatesZ80;

	switch(opcode) {
		case 0x09:		// ADD IX,BC (15T)
		case 0x19:		// ADD IX,DE (15T)
		case 0x29:		// ADD IX,HL (15T)
		case 0x39:		// ADD IX,SP (15T)
			*mpDstState++ = kZ80StateWait_7T;
			switch(opcode & 0x30) {
				case 0x00: *mpDstState++ = kZ80StateBCToData; break;
				case 0x10: *mpDstState++ = kZ80StateDEToData; break;
				case 0x20: *mpDstState++ = kZ80StateHLToData; break;
				case 0x30: *mpDstState++ = kZ80StateSPToData; break;
			}

			*mpDstState++ = kZ80StateAddToIX;
			return true;

		case 0x21:		// LD IX,imm16 (10T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateDataToIX;
			return true;

		case 0x22:		// LD (abs),IX (20T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateIXToData;
			*mpDstState++ = kZ80StateWriteL;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateWriteH;
			return true;

		case 0x23:		// INC IX (10T)
			*mpDstState++ = kZ80StateWait_2T;
			*mpDstState++ = kZ80StateIXToData;
			*mpDstState++ = kZ80StateInc16;
			*mpDstState++ = kZ80StateDataToIX;
			return true;

		case 0x2A:		// LD IX,(abs) (20T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadImmAddr;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadL;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadH;
			*mpDstState++ = kZ80StateDataToIX;
			return true;

		case 0x2B:		// INC IX (10T)
			*mpDstState++ = kZ80StateWait_2T;
			*mpDstState++ = kZ80StateIXToData;
			*mpDstState++ = kZ80StateDec16;
			*mpDstState++ = kZ80StateDataToIX;
			return true;

		case 0x36:		// LD (IX+d),imm (19T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadIXdToAddr;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateReadImm;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x46:		// LD B,(IX+d) (19T)
		case 0x4E:		// LD C,(IX+d) (19T)
		case 0x56:		// LD D,(IX+d) (19T)
		case 0x5E:		// LD E,(IX+d) (19T)
		case 0x66:		// LD H,(IX+d) (19T)
		case 0x6E:		// LD L,(IX+d) (19T)
		case 0x7E:		// LD A,(IX+d) (19T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadIXdToAddr;
			*mpDstState++ = kZ80StateWait_8T;
			*mpDstState++ = kZ80StateRead;
			DecodeDataToArg(opcode >> 3, false);
			return true;

		case 0x70:		// LD (IX+d),B (19T)
		case 0x71:		// LD (IX+d),C (19T)
		case 0x72:		// LD (IX+d),D (19T)
		case 0x73:		// LD (IX+d),E (19T)
		case 0x74:		// LD (IX+d),H (19T)
		case 0x75:		// LD (IX+d),L (19T)
		case 0x77:		// LD (IX+d),A (19T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadIXdToAddr;
			*mpDstState++ = kZ80StateWait_8T;
			DecodeArgToData(opcode, false);
			*mpDstState++ = kZ80StateWrite;
			return true;

		case 0x84:		// ADD A,IXH (8T)
		case 0x85:		// ADD A,IXL (8T)
		case 0x8C:		// ADC A,IXH (8T)
		case 0x8D:		// ADC A,IXL (8T)
		case 0x94:		// SUB A,IXH (8T)
		case 0x95:		// SUB A,IXL (8T)
		case 0x9C:		// SBC A,IXH (8T)
		case 0x9D:		// SBC A,IXL (8T)
		case 0xA4:		// AND A,IXH (8T)
		case 0xA5:		// AND A,IXL (8T)
		case 0xAC:		// XOR IXH (8T)
		case 0xAD:		// XOR IXL (8T)
		case 0xB4:		// OR A,IXH (8T)
		case 0xB5:		// OR A,IXL (8T)
		case 0xBC:		// CP IXH (8T)
		case 0xBD:		// CP IXL (8T)
			if (opcode & 1)
				*mpDstState++ = kZ80StateIXLToData;
			else
				*mpDstState++ = kZ80StateIXHToData;

			switch(opcode & 0xF8) {
				case 0x80: *mpDstState++ = kZ80StateAddToA; break;
				case 0x88: *mpDstState++ = kZ80StateAdcToA; break;
				case 0x90: *mpDstState++ = kZ80StateSubToA; break;
				case 0x98: *mpDstState++ = kZ80StateSbcToA; break;
				case 0xA0: *mpDstState++ = kZ80StateAndToA; break;
				case 0xA8: *mpDstState++ = kZ80StateXorToA; break;
				case 0xB0: *mpDstState++ = kZ80StateOrToA; break;
				case 0xB8: *mpDstState++ = kZ80StateCpToA; break;
			}
			return true;

		case 0x86:		// ADD A,(IX+d) (19T)
		case 0x8E:		// ADC A,(IX+d) (19T)
		case 0x96:		// SUB A,(IX+d) (19T)
		case 0x9E:		// SBC A,(IX+d) (19T)
		case 0xA6:		// AND A,(IX+d) (19T)
		case 0xAE:		// XOR (IX+d) (19T)
		case 0xB6:		// OR A,(IX+d) (19T)
		case 0xBE:		// CP (IX+d) (19T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadIXdToAddr;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateRead;
			*mpDstState++ = kZ80StateWait_3T;

			switch(opcode & 0xF8) {
				case 0x80: *mpDstState++ = kZ80StateAddToA; break;
				case 0x88: *mpDstState++ = kZ80StateAdcToA; break;
				case 0x90: *mpDstState++ = kZ80StateSubToA; break;
				case 0x98: *mpDstState++ = kZ80StateSbcToA; break;
				case 0xA0: *mpDstState++ = kZ80StateAndToA; break;
				case 0xA8: *mpDstState++ = kZ80StateXorToA; break;
				case 0xB0: *mpDstState++ = kZ80StateOrToA; break;
				case 0xB8: *mpDstState++ = kZ80StateCpToA; break;
			}
			return true;

		case 0xCB:		// Bit opcodes with (IX+d) or (IY+d)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateReadIXdToAddr;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadOpcodeDDFDCB;
			return true;

		case 0xE1:		// POP IX (14T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePop;
			*mpDstState++ = kZ80StateDataToIX;
			return true;

		case 0xE3:		// EX (SP),IY (23T)
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateSPToAddr;
			*mpDstState++ = kZ80StateReadL;
			*mpDstState++ = kZ80StateWait_4T;
			*mpDstState++ = kZ80StateReadH;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StateExIXData;
			*mpDstState++ = kZ80StateWriteH;
			*mpDstState++ = kZ80StateWait_5T;
			*mpDstState++ = kZ80StateWriteL;
			return true;

		case 0xE5:		// PUSH IX (14T)
			*mpDstState++ = kZ80StateIXToData;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			*mpDstState++ = kZ80StateWait_3T;
			*mpDstState++ = kZ80StatePush;
			return true;

		case 0xE9:		// JP (IX) (8T)
			*mpDstState++ = kZ80StateIXToData;
			*mpDstState++ = kZ80StateJP;
			return true;

		case 0xF9:		// LD SP,IX (10T)
			*mpDstState++ = kZ80StateWait_2T;
			*mpDstState++ = kZ80StateIXToData;
			*mpDstState++ = kZ80StateDataToSP;
			return true;
	}

	return false;
}

void ATCPUDecoderGeneratorZ80::DecodeArgToData(uint8 index, bool skipHLAddr) {
	using namespace ATCPUStatesZ80;

	switch(index & 7) {
		case 0:	*mpDstState++ = kZ80StateBToData; break;
		case 1:	*mpDstState++ = kZ80StateCToData; break;
		case 2:	*mpDstState++ = kZ80StateDToData; break;
		case 3:	*mpDstState++ = kZ80StateEToData; break;
		case 4:	*mpDstState++ = kZ80StateHToData; break;
		case 5:	*mpDstState++ = kZ80StateLToData; break;
		case 6:
			*mpDstState++ = kZ80StateWait_3T;
			if (!skipHLAddr)
				*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateRead;
			break;
		case 7:	*mpDstState++ = kZ80StateAToData; break;
	}
}

void ATCPUDecoderGeneratorZ80::DecodeDataToArg(uint8 index, bool skipHLAddr) {
	using namespace ATCPUStatesZ80;

	switch(index & 7) {
		case 0:	*mpDstState++ = kZ80StateDataToB; break;
		case 1:	*mpDstState++ = kZ80StateDataToC; break;
		case 2:	*mpDstState++ = kZ80StateDataToD; break;
		case 3:	*mpDstState++ = kZ80StateDataToE; break;
		case 4:	*mpDstState++ = kZ80StateDataToH; break;
		case 5:	*mpDstState++ = kZ80StateDataToL; break;
		case 6:
			*mpDstState++ = kZ80StateWait_3T;
			if (!skipHLAddr)
				*mpDstState++ = kZ80StateHLToAddr;
			*mpDstState++ = kZ80StateWrite;
			break;
		case 7:	*mpDstState++ = kZ80StateDataToA; break;
	}
}

void ATCPUDecoderGeneratorZ80::DecodeArgToData16(uint8 index) {
	using namespace ATCPUStatesZ80;

	switch(index & 0x30) {
		case 0x00: *mpDstState++ = kZ80StateBCToData; break;
		case 0x10: *mpDstState++ = kZ80StateDEToData; break;
		case 0x20: *mpDstState++ = kZ80StateHLToData; break;
		case 0x30: *mpDstState++ = kZ80StateSPToData; break;
	}
}

void ATCPUDecoderGeneratorZ80::DecodeArgToAddr16(uint8 index) {
	using namespace ATCPUStatesZ80;

	switch(index & 0x30) {
		case 0x00: *mpDstState++ = kZ80StateBCToAddr; break;
		case 0x10: *mpDstState++ = kZ80StateDEToAddr; break;
		case 0x20: *mpDstState++ = kZ80StateHLToAddr; break;
		case 0x30: *mpDstState++ = kZ80StateSPToAddr; break;
	}
}

void ATCPUDecoderGeneratorZ80::DecodeDataToArg16(uint8 index) {
	using namespace ATCPUStatesZ80;

	switch(index & 0x30) {
		case 0x00: *mpDstState++ = kZ80StateDataToBC; break;
		case 0x10: *mpDstState++ = kZ80StateDataToDE; break;
		case 0x20: *mpDstState++ = kZ80StateDataToHL; break;
		case 0x30: *mpDstState++ = kZ80StateDataToSP; break;
	}
}
