//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2009 Avery Lee
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
#include <at/atcpu/decode6502.h>
#include <at/atcpu/states6502.h>

void ATCPUDecoderGenerator6502::RebuildTables(ATCPUDecoderTables6502& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints, bool isC02) {
	using namespace ATCPUStates6502;

	mbStopOnBRK = stopOnBRK;
	mbHistoryEnabled = historyTracing;
	mpDstState = dst.mDecodeHeap;

	const auto stateReadOpcode = enableBreakpoints ? kStateReadOpcode : kStateReadOpcodeNoBreak;

	for(int j=0; j<256; ++j) {
		dst.mInsnPtrs[j] = mpDstState - dst.mDecodeHeap;

		uint8 c = (uint8)j;

		if (historyTracing)
			*mpDstState++ = kStateAddToHistory;

		if (!isC02 || !DecodeInsn65C02(c)) {
			if (!DecodeInsn6502(c) && !DecodeInsn6502Ill(c))
				*mpDstState++ = kStateBreakOnUnsupportedOpcode;
		}

		*mpDstState++ = stateReadOpcode;
	}

	// predecode NMI sequence
	dst.mInsnPtrs[256] = mpDstState - dst.mDecodeHeap;
	*mpDstState++ = kStateReadDummyOpcode;
	*mpDstState++ = kStateReadDummyOpcode;
	*mpDstState++ = kStatePushPCH;
	*mpDstState++ = kStatePushPCL;
	*mpDstState++ = kStatePtoD_B0;

	if (isC02)
		*mpDstState++ = kStateCLD;

	*mpDstState++ = kStatePush;
	*mpDstState++ = kStateSEI;
	*mpDstState++ = kStateNMIVecToPC;
	*mpDstState++ = kStateReadAddrL;
	*mpDstState++ = kStateReadAddrH;
	*mpDstState++ = kStateAddrToPC;
	*mpDstState++ = stateReadOpcode;

	// predecode IRQ sequence
	dst.mInsnPtrs[257] = mpDstState - dst.mDecodeHeap;
	*mpDstState++ = kStateReadDummyOpcode;
	*mpDstState++ = kStateReadDummyOpcode;
	*mpDstState++ = kStatePushPCH;
	*mpDstState++ = kStatePushPCL;
	*mpDstState++ = kStatePtoD_B0;

	if (isC02)
		*mpDstState++ = kStateCLD;

	*mpDstState++ = kStatePush;
	*mpDstState++ = kStateSEI;
	*mpDstState++ = kStateIRQVecToPC;
	*mpDstState++ = kStateReadAddrL;
	*mpDstState++ = kStateReadAddrH;
	*mpDstState++ = kStateAddrToPC;
	*mpDstState++ = stateReadOpcode;
}

bool ATCPUDecoderGenerator6502::DecodeInsn6502(uint8 opcode) {
	using namespace ATCPUStates6502;

	switch(opcode) {
		case 0x00:	// BRK
			if (mbStopOnBRK)
				*mpDstState++ = kStateBreakOnUnsupportedOpcode;

			*mpDstState++ = kStateReadAddrL;	// 2
			*mpDstState++ = kStatePushPCH;		// 3
			*mpDstState++ = kStatePushPCL;		// 4
			*mpDstState++ = kStatePtoD_B1;
			*mpDstState++ = kStatePush;			// 5
			*mpDstState++ = kStateSEI;
			*mpDstState++ = kStateNMIOrIRQVecToPC;
			*mpDstState++ = kStateReadAddrL;	// 6
			*mpDstState++ = kStateReadAddrH;	// 7
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x01:	// ORA (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateOr;
			break;

		case 0x05:	// ORA zp
			DecodeReadZp();
			*mpDstState++ = kStateOr;
			break;

		case 0x06:	// ASL zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			break;

		case 0x08:	// PHP
			*mpDstState++ = kStatePtoD;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePush;
			break;

		case 0x09:	// ORA imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateOr;
			break;

		case 0x0A:	// ASL A
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x0D:	// ORA abs
			DecodeReadAbs();
			*mpDstState++ = kStateOr;
			break;

		case 0x0E:	// ASL abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			break;

		case 0x10:	// BPL rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJns;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x11:	// ORA (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateOr;
			break;

		case 0x15:	// ORA zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateOr;
			break;

		case 0x16:	// ASL zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			break;

		case 0x18:	// CLC
			*mpDstState++ = kStateCLC;
			*mpDstState++ = kStateWait;
			break;

		case 0x19:	// ORA abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateOr;
			break;

		case 0x1D:	// ORA abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateOr;
			break;

		case 0x1E:	// ASL abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateAsl;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x20:	// JSR abs
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStatePushPCHM1;
			*mpDstState++ = kStatePushPCLM1;
			*mpDstState++ = kStateAddrToPC;
			*mpDstState++ = kStateWait;
			break;

		case 0x21:	// AND (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x24:	// BIT zp
			DecodeReadZp();
			*mpDstState++ = kStateDSetSV;
			*mpDstState++ = kStateBit;
			break;

		case 0x25:	// AND zp
			DecodeReadZp();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x26:	// ROL zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			break;

		case 0x28:	// PLP
			*mpDstState++ = kStatePop;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoP;
			*mpDstState++ = kStateWait;
			break;

		case 0x29:	// AND imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x2A:	// ROL A
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x2C:	// BIT abs
			DecodeReadAbs();
			*mpDstState++ = kStateDSetSV;
			*mpDstState++ = kStateBit;
			break;

		case 0x2D:	// AND abs
			DecodeReadAbs();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x2E:	// ROL abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			break;

		case 0x30:	// BMI rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJs;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x31:	// AND (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x35:	// AND zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x36:	// ROL zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			break;

		case 0x38:	// SEC
			*mpDstState++ = kStateSEC;
			*mpDstState++ = kStateWait;
			break;

		case 0x39:	// AND abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x3D:	// AND abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x3E:	// ROL abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateRol;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x40:	// RTI
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePop;
			*mpDstState++ = kStateDtoP_noICheck;
			*mpDstState++ = kStatePopPCL;
			*mpDstState++ = kStatePopPCH;
			break;

		case 0x41:	// EOR (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateXor;
			break;

		case 0x45:	// EOR zp
			DecodeReadZp();
			*mpDstState++ = kStateXor;
			break;

		case 0x46:	// LSR zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			break;

		case 0x48:	// PHA
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePush;
			break;

		case 0x49:	// EOR imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateXor;
			break;

		case 0x4A:	// LSR A
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x4C:	// JMP abs
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x4D:	// EOR abs
			DecodeReadAbs();
			*mpDstState++ = kStateXor;
			break;

		case 0x4E:	// LSR abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			break;

		case 0x50:	// BVC
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJno;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x51:	// EOR (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateXor;
			break;

		case 0x55:	// EOR zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateXor;
			break;

		case 0x56:	// LSR zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			break;

		case 0x58:	// CLI
			*mpDstState++ = kStateCLI;
			*mpDstState++ = kStateWait;
			break;

		case 0x59:	// EOR abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateXor;
			break;

		case 0x5D:	// EOR abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateXor;
			break;

		case 0x5E:	// LSR abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateLsr;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x60:	// RTS
			*mpDstState++ = kStatePopPCL;
			*mpDstState++ = kStatePopPCHP1;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0x61:	// ADC (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateAdc;
			break;

		case 0x65:	// ADC zp
			DecodeReadZp();
			*mpDstState++ = kStateAdc;
			break;

		case 0x66:	// ROR zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWrite;
			break;

		case 0x68:	// PLA
			*mpDstState++ = kStatePop;
			*mpDstState++ = kStateDSetSZToA;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0x69:	// ADC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateAdc;
			break;

		case 0x6A:	// ROR A
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x6C:	// JMP (abs)
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateReadAbsIndAddrBroken;
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x6D:	// ADC abs
			DecodeReadAbs();
			*mpDstState++ = kStateAdc;
			break;

		case 0x6E:	// ROR abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWrite;
			break;

		case 0x70:	// BVS
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJo;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x71:	// ADC (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateAdc;
			break;

		case 0x75:	// ADC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateAdc;
			break;

		case 0x76:	// ROR zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWrite;
			break;

		case 0x78:	// SEI
			*mpDstState++ = kStateSEI;
			*mpDstState++ = kStateWait;
			break;

		case 0x79:	// ADC abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateAdc;
			break;

		case 0x7D:	// ADC abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateAdc;
			break;

		case 0x7E:	// ROR abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateRor;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x81:	// STA (zp,X)
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddX;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateReadIndAddr;
			*mpDstState++ = kStateWriteA;

			if (mbHistoryEnabled)
				*mpDstState++ = kStateAddEAToHistory;
			break;

		case 0x84:	// STY zp
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateWrite;
			break;

		case 0x85:	// STA zp
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateWriteA;	
			break;

		case 0x86:	// STX zp
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateWrite;	
			break;

		case 0x88:	// DEY
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateDtoY;
			*mpDstState++ = kStateWait;
			break;

		case 0x8A:	// TXA
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateDSetSZToA;
			*mpDstState++ = kStateWait;
			break;

		case 0x8C:	// STY abs
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateWrite;
			break;

		case 0x8D:	// STA abs
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateWriteA;	
			break;

		case 0x8E:	// STX abs
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateWrite;
			break;

		case 0x90:	// BCC rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJnc;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x91:	// STA (zp),Y
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateReadIndYAddr;
			*mpDstState++ = kStateReadCarryForced;
			*mpDstState++ = kStateWriteA;

			if (mbHistoryEnabled)
				*mpDstState++ = kStateAddEAToHistory;
			break;

		case 0x94:	// STY zp,X
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddX;
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateWrite;
			break;

		case 0x95:	// STA zp,X
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddX;
			*mpDstState++ = kStateWriteA;	
			break;

		case 0x96:	// STX zp,Y
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddY;
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateWrite;
			break;

		case 0x98:	// TYA
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateDSetSZToA;
			*mpDstState++ = kStateWait;
			break;

		case 0x99:	// STA abs,Y
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrHY;
			*mpDstState++ = kStateReadCarryForced;
			*mpDstState++ = kStateWriteA;
			break;

		case 0x9A:	// TXS
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateDtoS;
			*mpDstState++ = kStateWait;
			break;

		case 0x9D:	// STA abs,X
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrHX;
			*mpDstState++ = kStateReadCarryForced;
			*mpDstState++ = kStateWriteA;
			break;

		case 0xA0:	// LDY imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			break;

		case 0xA1:	// LDA (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xA2:	// LDX imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			break;

		case 0xA4:	// LDY zp
			DecodeReadZp();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			break;

		case 0xA5:	// LDA zp
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadSetSZToA;
			break;

		case 0xA6:	// LDX zp
			DecodeReadZp();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			break;

		case 0xA8:	// TAY
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			*mpDstState++ = kStateWait;
			break;

		case 0xA9:	// LDA imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xAA:	// TAX
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateWait;
			break;

		case 0xAC:	// LDY abs
			DecodeReadAbs();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			break;

		case 0xAD:	// LDA abs
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrH;		// 3
			*mpDstState++ = kStateReadSetSZToA;		// 4
			break;

		case 0xAE:	// LDX abs
			DecodeReadAbs();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			break;

		case 0xB0:	// BCS rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJc;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xB1:	// LDA (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xB4:	// LDY zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			break;

		case 0xB5:	// LDA zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xB6:	// LDX zp,Y
			DecodeReadZpY();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			break;

		case 0xB8:	// CLV
			*mpDstState++ = kStateCLV;
			*mpDstState++ = kStateWait;
			break;

		case 0xB9:	// LDA abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xBA:	// TSX
			*mpDstState++ = kStateStoD;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateWait;
			break;

		case 0xBC:	// LDY abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			break;

		case 0xBD:	// LDA abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xBE:	// LDX abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			break;

		case 0xC0:	// CPY imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateCmpY;
			break;

		case 0xC1:	// CMP (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateCmp;
			break;

		case 0xC4:	// CPY zp
			DecodeReadZp();
			*mpDstState++ = kStateCmpY;
			break;

		case 0xC5:	// CMP zp
			DecodeReadZp();
			*mpDstState++ = kStateCmp;
			break;

		case 0xC6:	// DEC zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWrite;
			break;

		case 0xC8:	// INY
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateDtoY;
			*mpDstState++ = kStateWait;
			break;

		case 0xC9:	// CMP imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateCmp;
			break;

		case 0xCA:	// DEX
			*mpDstState++ = kStateDecXWait;
			break;

		case 0xCC:	// CPY abs
			DecodeReadAbs();
			*mpDstState++ = kStateCmpY;
			break;

		case 0xCD:	// CMP abs
			DecodeReadAbs();
			*mpDstState++ = kStateCmp;
			break;

		case 0xCE:	// DEC abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWrite;
			break;

		case 0xD0:	// BNE rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJnz;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xD1:	// CMP (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateCmp;
			break;

		case 0xD5:	// CMP zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateCmp;
			break;

		case 0xD6:	// DEC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWrite;
			break;

		case 0xD8:	// CLD
			*mpDstState++ = kStateCLD;
			*mpDstState++ = kStateWait;
			break;

		case 0xD9:	// CMP abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateCmp;
			break;

		case 0xDD:	// CMP abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateCmp;
			break;

		case 0xDE:	// DEC abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateDec;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0xE0:	// CPX imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateCmpX;
			break;

		case 0xE1:	// SBC (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateSbc;
			break;

		case 0xE4:	// CPX zp
			DecodeReadZp();
			*mpDstState++ = kStateCmpX;
			break;

		case 0xE5:	// SBC zp
			DecodeReadZp();
			*mpDstState++ = kStateSbc;
			break;

		case 0xE6:	// INC zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			break;

		case 0xE8:	// INX
			*mpDstState++ = kStateIncXWait;
			break;

		case 0xE9:	// SBC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateSbc;
			break;

		case 0xEA:	// NOP
			*mpDstState++ = kStateWait;
			break;

		case 0xEC:	// CPX abs
			DecodeReadAbs();
			*mpDstState++ = kStateCmpX;
			break;

		case 0xED:	// SBC abs
			DecodeReadAbs();
			*mpDstState++ = kStateSbc;
			break;

		case 0xEE:	// INC abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			break;

		case 0xF0:	// BEQ rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJz;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xF1:	// SBC (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateSbc;
			break;

		case 0xF5:	// SBC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateSbc;
			break;

		case 0xF6:	// INC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			break;

		case 0xF8:	// SED
			*mpDstState++ = kStateSED;
			*mpDstState++ = kStateWait;
			break;

		case 0xF9:	// SBC abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateSbc;
			break;

		case 0xFD:	// SBC abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateSbc;
			break;

		case 0xFE:	// INC abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateInc;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		default:
			return false;
	}

	return true;
}

bool ATCPUDecoderGenerator6502::DecodeInsn6502Ill(uint8 opcode) {
	using namespace ATCPUStates6502;

	switch(opcode) {
		case 0x03:	// SLO (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateOr;
			break;

		case 0x04:	// NOP zp
			DecodeReadZp();
			break;

		case 0x07:	// SLO zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateOr;
			break;

		case 0x0B:	// AAC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateAnc;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x0C:	// NOP abs
			DecodeReadAbs();
			break;

		case 0x0F:	// SLO abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateOr;
			break;

		case 0x13:	// SLO (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateOr;
			break;

		case 0x14:	// NOP zp,X
			DecodeReadZpX();
			break;

		case 0x17:	// SLO zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateOr;
			break;

		case 0x1A:	// NOP*
			*mpDstState++ = kStateWait;
			break;

		case 0x1B:	// SLO abs,Y
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHY;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateAsl;				//
			*mpDstState++ = kStateWrite;			// 7
			*mpDstState++ = kStateOr;
			break;

		case 0x1C:	// NOP abs,X
			DecodeReadAbsX();
			break;

		case 0x1F:	// SLO abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateAsl;				//
			*mpDstState++ = kStateWrite;			// 7
			*mpDstState++ = kStateOr;
			break;

		case 0x23:	// RLA (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x27:	// RLA zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x2B:	// AAC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateAnc;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x2F:	// RLA abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x33:	// RLA (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x34:	// NOP zp,X
			DecodeReadZpX();
			break;

		case 0x37:	// RLA Zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x3A:	// NOP*
			*mpDstState++ = kStateWait;
			break;

		case 0x3B:	// RLA abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x3C:	// NOP abs,X
			DecodeReadAbsX();
			break;

		case 0x3F:	// RLA abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x43:	// SRE (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateXor;
			break;

		case 0x44:	// NOP zp
			DecodeReadZp();
			break;

		case 0x47:	// SRE zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateXor;
			break;

		case 0x4B:	// ASR #imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x4F:	// SRE abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateXor;
			break;

		case 0x53:	// SRE (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateXor;
			break;

		case 0x54:	// NOP zp,X
			DecodeReadZpX();
			break;

		case 0x57:	// SRE zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateXor;
			break;

		case 0x5A:	// NOP*
			*mpDstState++ = kStateWait;
			break;

		case 0x5B:	// SRE abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateXor;
			break;

		case 0x5C:	// NOP abs,X
			DecodeReadAbsX();
			break;

		case 0x5F:	// SRE abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;			// 7
			*mpDstState++ = kStateXor;
			break;

		case 0x63:	// RRA (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x64:	// NOP zp
			DecodeReadZp();
			break;

		case 0x67:	// RRA zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x6B:	// ARR #imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateArr;
			*mpDstState++ = kStateWait;
			break;

		case 0x6F:	// RRA abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x73:	// RRA (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x74:	// NOP zp,X
			DecodeReadZpX();
			break;

		case 0x77:	// RRA zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x7A:	// NOP*
			*mpDstState++ = kStateWait;
			break;

		case 0x7B:	// RRA abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x7C:	// NOP abs,X
			DecodeReadAbsX();
			break;

		case 0x7F:	// RRA abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateAdc;
			*mpDstState++ = kStateWrite;
			break;

		case 0x80:	// NOP #imm
			*mpDstState++ = kStateReadImm;
			break;

		case 0x82:	// NOP #imm
			*mpDstState++ = kStateReadImm;
			break;

		case 0x83:	// SAX (zp,X)
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddX;			// 3
			*mpDstState++ = kStateRead;				// 4
			*mpDstState++ = kStateReadIndAddr;		// 5
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateAnd_SAX;
			*mpDstState++ = kStateWrite;			// 6
			break;

		case 0x87:	// SAX zp
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateAnd_SAX;
			*mpDstState++ = kStateWrite;
			break;

		case 0x89:	// NOP #imm
			*mpDstState++ = kStateReadImm;
			break;

		case 0x8B:	// ANE #imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateXaa;
			break;

		case 0x8F:	// SAX abs
			*mpDstState++ = kStateReadAddrL;		
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateAnd_SAX;
			*mpDstState++ = kStateWrite;
			break;

		case 0x93:	// SHA (zp),Y
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateRead;				// 3
			*mpDstState++ = kStateReadIndYAddr_SHA;	// 4
			*mpDstState++ = kStateWait;				// 5
			*mpDstState++ = kStateWrite;			// 6
			break;

		case 0x97:	// SAX zp,Y
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddY;
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateAnd_SAX;
			*mpDstState++ = kStateWrite;
			break;

		case 0x9B:	// SHS abs,Y
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHY;		// 3
			*mpDstState++ = kStateXas;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWrite;
			break;

		case 0x9C:	// SHY abs,X
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrHX_SHY;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWrite;
			break;

		case 0x9E:	// SHX abs,Y
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrHY_SHX;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWrite;
			break;

		case 0x9F:	// SHA abs,Y
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHY_SHA;	// 3
			*mpDstState++ = kStateWait;				// 4
			*mpDstState++ = kStateWrite;			// 5
			break;

		case 0xA3:	// LAX (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateDtoA;
			break;

		case 0xA7:	// LAX zp
			DecodeReadZp();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateDtoA;
			break;

		case 0xAB:	// LXA #imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			*mpDstState++ = kStateDtoX;
			break;

		case 0xAF:	// LAX abs
			DecodeReadAbs();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateDtoA;
			break;

		case 0xB3:	// LAX (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateDtoA;
			break;

		case 0xB7:	// LAX zp,Y
			DecodeReadZpY();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateDtoA;
			break;

		case 0xBB:	// LAS abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateLas;
			break;

		case 0xBF:	// LAX abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateDtoA;
			break;

		case 0xC2:	// NOP #arg
			*mpDstState++ = kStateReadImm;
			break;

		case 0xC3:	// DCP (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;
			break;

		case 0xC7:	// DCP zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;
			break;

		case 0xCB:	// SBX #arg
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateSbx;
			break;

		case 0xCF:	// DCP abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;
			break;

		case 0xD3:	// DCP (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;
			break;

		case 0xD4:	// NOP zp,X
			DecodeReadZpX();
			break;

		case 0xD7:	// DCP zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;
			break;

		case 0xDA:	// NOP*
			*mpDstState++ = kStateWait;
			break;

		case 0xDB:	// DCP abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;
			break;

		case 0xDC:	// NOP abs,X
			DecodeReadAbsX();
			break;

		case 0xDF:	// DCP abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateCmp;
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0xE2:	// NOP #arg
			*mpDstState++ = kStateReadImm;
			break;

		case 0xE3:	// ISB (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateSbc;
			break;

		case 0xE7:	// ISB zp
			DecodeReadZp();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateSbc;
			break;

		case 0xEB:	// SBC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateSbc;
			break;

		case 0xEF:	// ISB abs
			DecodeReadAbs();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateSbc;
			break;

		case 0xF3:	// ISB (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateSbc;
			break;

		case 0xF4:	// NOP zp,X
			DecodeReadZpX();
			break;

		case 0xF7:	// ISB zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateSbc;
			break;

		case 0xFA:	// NOP*
			*mpDstState++ = kStateWait;
			break;

		case 0xFB:	// ISB abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			*mpDstState++ = kStateSbc;
			break;

		case 0xFC:	// NOP abs,X
			DecodeReadAbsX();
			break;

		case 0xFF:	// ISB abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;			// 7
			*mpDstState++ = kStateSbc;
			break;

		default:
			return false;
	}

	return true;
}

bool ATCPUDecoderGenerator6502::DecodeInsn65C02(uint8 opcode) {
	using namespace ATCPUStates6502;

	switch(opcode) {
		// New (zp) mode
		case 0x12:	// ORA (zp)
			DecodeReadInd();
			*mpDstState++ = kStateOr;
			break;

		case 0x32:	// AND (zp)
			DecodeReadInd();
			*mpDstState++ = kStateAnd;
			break;

		case 0x52:	// EOR (zp)
			DecodeReadInd();
			*mpDstState++ = kStateXor;
			break;

		case 0x61:	// ADC (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x65:	// ADC zp
			DecodeReadZp();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x69:	// ADC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x6D:	// ADC abs
			DecodeReadAbs();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x71:	// ADC (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x72:	// ADC (zp)
			DecodeReadInd();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x75:	// ADC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x79:	// ADC abs,Y
			DecodeReadAbsY();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x7D:	// ADC abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateC02_Adc;
			*mpDstState++ = kStateWait;
			break;

		case 0x92:	// STA (zp)
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateRead;				// 3
			*mpDstState++ = kStateReadIndAddr;		// 4
			*mpDstState++ = kStateWrite;			// 5
			break;

		case 0xB2:	// LDA (zp)
			DecodeReadInd();
			*mpDstState++ = kStateDtoA;
			break;

		case 0xD2:	// CMP (zp)
			DecodeReadInd();
			*mpDstState++ = kStateCmp;
			break;

		case 0xE1:	// SBC (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		case 0xE5:	// SBC zp
			DecodeReadZp();
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		case 0xE9:	// SBC imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		case 0xED:	// SBC abs
			DecodeReadAbs();
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		case 0xF1:	// SBC (zp),Y
			DecodeReadIndY();
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		case 0xF2:	// SBC (zp)
			DecodeReadInd();
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		case 0xF5:	// SBC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateC02_Sbc;
			*mpDstState++ = kStateWait;
			break;

		// TRB/TSB
		case 0x04:	// TSB zp
			DecodeReadZp();							// 2, 3
			*mpDstState++ = kStateTsb;				// 4
			*mpDstState++ = kStateWait;				//
			*mpDstState++ = kStateWrite;			// 5
			break;

		case 0x14:	// TRB zp
			DecodeReadZp();							// 2, 3
			*mpDstState++ = kStateTrb;				// 4
			*mpDstState++ = kStateWait;				//
			*mpDstState++ = kStateWrite;			// 5
			break;

		case 0x0C:	// TSB abs
			DecodeReadAbs();						// 2, 3, 4
			*mpDstState++ = kStateTsb;				// 5
			*mpDstState++ = kStateWait;				//
			*mpDstState++ = kStateWrite;			// 6
			break;

		case 0x1C:	// TRB abs
			DecodeReadAbs();						// 2, 3, 4
			*mpDstState++ = kStateTrb;				// 5
			*mpDstState++ = kStateWait;				//
			*mpDstState++ = kStateWrite;			// 6
			break;

		// STZ
		case 0x64:	// STZ zp
			*mpDstState++ = kState0toD;
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateWrite;
			break;

		case 0x74:	// STZ zp,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddX;			// 3
			*mpDstState++ = kState0toD;
			*mpDstState++ = kStateWrite;			// 4
			break;

		case 0x9C:	// STZ abs
			*mpDstState++ = kState0toD;
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrH;		// 3
			*mpDstState++ = kStateWrite;			// 4
			break;

		case 0x9E:	// STZ abs,X
			*mpDstState++ = kState0toD;
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateWait;				// 4
			*mpDstState++ = kStateWrite;			// 5
			break;

		// BIT
		case 0x3C:	// BIT abs,X
			DecodeReadAbsX();
			*mpDstState++ = kStateDSetSV;
			*mpDstState++ = kStateBit;
			break;

		// RMB/SMB
		case 0x07:	// RMB0 zp (5 cycles)
		case 0x17:	// RMB1 zp
		case 0x27:	// RMB2 zp
		case 0x37:	// RMB3 zp
		case 0x47:	// RMB4 zp
		case 0x57:	// RMB5 zp
		case 0x67:	// RMB6 zp
		case 0x77:	// RMB7 zp
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateResetBit;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWrite;
			break;

		case 0x87:	// SMB0 zp (5 cycles)
		case 0x97:	// SMB1 zp
		case 0xA7:	// SMB2 zp
		case 0xB7:	// SMB3 zp
		case 0xC7:	// SMB4 zp
		case 0xD7:	// SMB5 zp
		case 0xE7:	// SMB6 zp
		case 0xF7:	// SMB7 zp
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateSetBit;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWrite;
			break;

		case 0x0F:	// BBR0 zp,rel
		case 0x1F:	// BBR1 zp,rel
		case 0x2F:	// BBR2 zp,rel
		case 0x3F:	// BBR3 zp,rel
		case 0x4F:	// BBR4 zp,rel
		case 0x5F:	// BBR5 zp,rel
		case 0x6F:	// BBR6 zp,rel
		case 0x7F:	// BBR7 zp,rel
			DecodeReadZp();
			*mpDstState++ = kStateReadRel;
			*mpDstState++ = kStateJ0;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x8F:	// BBS0 zp,rel
		case 0x9F:	// BBS1 zp,rel
		case 0xAF:	// BBS2 zp,rel
		case 0xBF:	// BBS3 zp,rel
		case 0xCF:	// BBS4 zp,rel
		case 0xDF:	// BBS5 zp,rel
		case 0xEF:	// BBS6 zp,rel
		case 0xFF:	// BBS7 zp,rel
			DecodeReadZp();
			*mpDstState++ = kStateReadRel;
			*mpDstState++ = kStateJ1;
			*mpDstState++ = kStateJccFalseRead;
			break;

		// Misc
		case 0x1A:	// INC A
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x34:	// BIT zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateDSetSV;
			*mpDstState++ = kStateBit;
			break;

		case 0x3A:	// DEC A
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x6C:	// JMP (abs)	(takes 6 cycles on 65C02)
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateReadAbsIndAddr;
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x7C:	// JMP (abs,X)
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrHX;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateReadAbsIndAddr;
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0xCB:	// WAI
			*mpDstState++ = kStateWaitForInterrupt;
			break;

		case 0xDB:	// STP
			*mpDstState++ = kStateStop;
			break;

		case 0x5A:	// PHY
			*mpDstState++ = kStateYtoD;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePush;
			break;

		case 0x7A:	// PLY
			*mpDstState++ = kStatePop;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoY;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0xDA:	// PHX
			*mpDstState++ = kStateXtoD;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePush;
			break;

		case 0xFA:	// PLX
			*mpDstState++ = kStatePop;
			*mpDstState++ = kStateDSetSZ;
			*mpDstState++ = kStateDtoX;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0x80:	// BRA rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateJ;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x89:	// BIT #imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateTsb;
			break;

		//================= RMW behavior ==================
		//
		// Changes from 6502:
		// - ALU cycle is a read cycle, not a write cycle
		// - ASL/ROL/ASL/ROR abs,X take 6 cycles if no page crossing (INC/DEC NOT affected)

		case 0x06:	// ASL zp
			DecodeReadZp();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			break;

		case 0x0E:	// ASL abs
			DecodeReadAbs();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			break;

		case 0x16:	// ASL zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateAsl;
			*mpDstState++ = kStateWrite;
			break;

		case 0x1E:	// ASL abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarry;		// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateRead;				// 6
			*mpDstState++ = kStateAsl;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x26:	// ROL zp
			DecodeReadZp();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			break;

		case 0x2E:	// ROL abs
			DecodeReadAbs();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			break;

		case 0x36:	// ROL zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateRol;
			*mpDstState++ = kStateWrite;
			break;
		case 0x3E:	// ROL abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarry;		// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateRead;				// 6
			*mpDstState++ = kStateRol;				//
			*mpDstState++ = kStateWrite;			// 7
			break;
		case 0x46:	// LSR zp
			DecodeReadZp();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			break;
		case 0x4E:	// LSR abs
			DecodeReadAbs();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			break;
		case 0x56:	// LSR zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateLsr;
			*mpDstState++ = kStateWrite;
			break;
		case 0x5E:	// LSR abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarry;		// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateRead;				// 6
			*mpDstState++ = kStateLsr;				//
			*mpDstState++ = kStateWrite;			// 7
			break;
		case 0x66:	// ROR zp
			DecodeReadZp();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWrite;
			break;
		case 0x6E:	// ROR abs
			DecodeReadAbs();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWrite;
			break;
		case 0x76:	// ROR zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateRor;
			*mpDstState++ = kStateWrite;
			break;
		case 0x7E:	// ROR abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarry;		// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateRead;				// 6
			*mpDstState++ = kStateRor;				//
			*mpDstState++ = kStateWrite;			// 7
			break;
		case 0xC6:	// DEC zp
			DecodeReadZp();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWrite;
			break;
		case 0xCE:	// DEC abs
			DecodeReadAbs();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWrite;
			break;
		case 0xD6:	// DEC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateDec;
			*mpDstState++ = kStateWrite;
			break;
		case 0xDE:	// DEC abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateRead;				// 6
			*mpDstState++ = kStateDec;				//
			*mpDstState++ = kStateWrite;			// 7
			break;
		case 0xE6:	// INC zp
			DecodeReadZp();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			break;
		case 0xEE:	// INC abs
			DecodeReadAbs();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			break;
		case 0xF6:	// INC zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateInc;
			*mpDstState++ = kStateWrite;
			break;
		case 0xFE:	// INC abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateRead;				// 6
			*mpDstState++ = kStateInc;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		//================= reserved NOPs ========================

		case 0x02:	// Reserved NOP (2 bytes, 2 cycles)
		case 0x22:
		//case 0x42:	We use this as an escape.
		case 0x62:
		case 0x82:
		case 0xC2:
		case 0xE2:
			*mpDstState++ = kStateReadImm;
			break;

		case 0x44:	// Reserved NOP (2 bytes, 3 cycles)
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateWait;
			break;

		case 0x54:	// Reserved NOP (2 bytes, 4 cycles)
		case 0xD4:
		case 0xF4:
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0x5C:	// Reserved NOP (3 bytes, 8 cycles)
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0xDC:	// Reserved NOP (3 bytes, 4 cycles)
		case 0xFC:
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateWait;
			break;

		default:
			if ((opcode & 0x03) == 0x03) {
				// Reserved NOP (1 byte, 1 cycle)
				return true;
			}

			return false;
	}

	return true;
}

void ATCPUDecoderGenerator6502::DecodeReadZp() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;
	*mpDstState++ = kStateRead;
}

void ATCPUDecoderGenerator6502::DecodeReadZpX() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateReadAddX;			// 3
	*mpDstState++ = kStateRead;				// 4
}

void ATCPUDecoderGenerator6502::DecodeReadZpY() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateReadAddY;			// 3
	*mpDstState++ = kStateRead;				// 4
}

void ATCPUDecoderGenerator6502::DecodeReadAbs() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateReadAddrH;		// 3
	*mpDstState++ = kStateRead;				// 4
}

void ATCPUDecoderGenerator6502::DecodeReadAbsX() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateReadAddrHX;		// 3
	*mpDstState++ = kStateReadCarry;		// 4
	*mpDstState++ = kStateRead;				// (5)
}

void ATCPUDecoderGenerator6502::DecodeReadAbsY() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateReadAddrHY;		// 3
	*mpDstState++ = kStateReadCarry;		// 4
	*mpDstState++ = kStateRead;				// (5)
}

void ATCPUDecoderGenerator6502::DecodeReadIndX() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateReadAddX;			// 3
	*mpDstState++ = kStateRead;				// 4
	*mpDstState++ = kStateReadIndAddr;		// 5
	*mpDstState++ = kStateRead;				// 6

	if (mbHistoryEnabled)
		*mpDstState++ = kStateAddEAToHistory;
}

void ATCPUDecoderGenerator6502::DecodeReadIndY() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateRead;				// 3
	*mpDstState++ = kStateReadIndYAddr;		// 4
	*mpDstState++ = kStateReadCarry;		// 5
	*mpDstState++ = kStateRead;				// (6)

	if (mbHistoryEnabled)
		*mpDstState++ = kStateAddEAToHistory;
}

void ATCPUDecoderGenerator6502::DecodeReadInd() {
	using namespace ATCPUStates6502;

	*mpDstState++ = kStateReadAddrL;		// 2
	*mpDstState++ = kStateRead;				// 3
	*mpDstState++ = kStateReadIndAddr;		// 4
	*mpDstState++ = kStateRead;				// 5

	if (mbHistoryEnabled)
		*mpDstState++ = kStateAddEAToHistory;
}
