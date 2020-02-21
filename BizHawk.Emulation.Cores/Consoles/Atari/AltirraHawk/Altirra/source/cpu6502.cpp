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
#include "cpu.h"
#include "cpustates.h"

bool ATCPUEmulator::Decode6502(uint8 opcode) {
	switch(opcode) {
		case 0x00:	// BRK
			if (mbStopOnBRK)
				*mpDstState++ = kStateBreakOnUnsupportedOpcode;

			*mpDstState++ = kStateReadAddrL;	// 2
			*mpDstState++ = kStatePushPCH;		// 3
			*mpDstState++ = kStatePushPCL;		// 4
			*mpDstState++ = kStatePtoD_B1;

			if (mbAllowBlockedNMIs)
				*mpDstState++ = kStateCheckNMIBlocked;

			*mpDstState++ = kStatePush;			// 5
			*mpDstState++ = kStateSEI;

			if (mbAllowBlockedNMIs)
				*mpDstState++ = kStateNMIOrIRQVecToPCBlockable;
			else
				*mpDstState++ = kStateNMIOrIRQVecToPC;

			*mpDstState++ = kStateReadAddrL;	// 6
			*mpDstState++ = kStateDelayInterrupts;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJnsAddToPath : kStateJns;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x11:	// ORA (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateOr;
			break;

		case 0x1D:	// ORA abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateOr;
			break;

		case 0x1E:	// ASL abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateAsl;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x20:	// JSR abs
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;

			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;

			*mpDstState++ = kStatePushPCHM1;
			*mpDstState++ = kStatePushPCLM1;
			*mpDstState++ = kStateAddrToPC;
			*mpDstState++ = kStateWait;

			if (mbStepOver)
				*mpDstState++ = kStateStepOver;

			if (mbPathfindingEnabled)
				*mpDstState++ = kStateAddAsPathStart;
			break;

		case 0x21:	// AND (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x24:	// BIT zp
			DecodeReadZp();
			*mpDstState++ = kStateBitSetSV;
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
			*mpDstState++ = kStateBitSetSV;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJsAddToPath : kStateJs;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x31:	// AND (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x3D:	// AND abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateAnd;
			*mpDstState++ = kStateDtoA;
			break;

		case 0x3E:	// ROL abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateReadCarryForced;	// 4
			*mpDstState++ = kStateRead;				// 5
			*mpDstState++ = kStateWrite;			// 6
			*mpDstState++ = kStateRol;				//
			*mpDstState++ = kStateWrite;			// 7
			break;

		case 0x40:	// RTI
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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

			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;

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
			*mpDstState++ = mbPathfindingEnabled ? kStateJnoAddToPath : kStateJno;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x51:	// EOR (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateXor;
			break;

		case 0x5D:	// EOR abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateXor;
			break;

		case 0x5E:	// LSR abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJoAddToPath : kStateJo;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x71:	// ADC (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateAdc;
			break;

		case 0x7D:	// ADC abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateAdc;
			break;

		case 0x7E:	// ROR abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJncAddToPath : kStateJnc;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x91:	// STA (zp),Y
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateRead;
			*mpDstState++ = kStateReadIndYAddr;
			*mpDstState++ = kStateReadCarryForced;
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateReadCarryForced;
			*mpDstState++ = kStateWriteA;
			break;

		case 0xA0:	// LDY imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateDSetSZToY;
			break;

		case 0xA1:	// LDA (zp,X)
			DecodeReadIndX();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xA2:	// LDX imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateDSetSZToX;
			break;

		case 0xA4:	// LDY zp
			DecodeReadZp();
			*mpDstState++ = kStateDSetSZToY;
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			break;

		case 0xA5:	// LDA zp
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadSetSZToA;
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			break;

		case 0xA6:	// LDX zp
			DecodeReadZp();
			*mpDstState++ = kStateDSetSZToX;
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			break;

		case 0xA8:	// TAY
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateDSetSZToY;
			*mpDstState++ = kStateWait;
			break;

		case 0xA9:	// LDA imm
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xAA:	// TAX
			*mpDstState++ = kStateAtoD;
			*mpDstState++ = kStateDSetSZToX;
			*mpDstState++ = kStateWait;
			break;

		case 0xAC:	// LDY abs
			DecodeReadAbs();
			*mpDstState++ = kStateDSetSZToY;
			break;

		case 0xAD:	// LDA abs
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrH;		// 3
			*mpDstState++ = kStateReadSetSZToA;		// 4
			break;

		case 0xAE:	// LDX abs
			DecodeReadAbs();
			*mpDstState++ = kStateDSetSZToX;
			break;

		case 0xB0:	// BCS rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJcAddToPath : kStateJc;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xB1:	// LDA (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xB4:	// LDY zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateDSetSZToY;
			break;

		case 0xB5:	// LDA zp,X
			DecodeReadZpX();
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xB6:	// LDX zp,Y
			DecodeReadZpY();
			*mpDstState++ = kStateDSetSZToX;
			break;

		case 0xB8:	// CLV
			*mpDstState++ = kStateCLV;
			*mpDstState++ = kStateWait;
			break;

		case 0xB9:	// LDA abs,Y
			DecodeReadAbsY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xBA:	// TSX
			*mpDstState++ = kStateStoD;
			*mpDstState++ = kStateDSetSZToX;
			*mpDstState++ = kStateWait;
			break;

		case 0xBC:	// LDY abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateDSetSZToY;
			break;

		case 0xBD:	// LDA abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateDSetSZToA;
			break;

		case 0xBE:	// LDX abs,Y
			DecodeReadAbsY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateDSetSZToX;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJnzAddToPath : kStateJnz;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xD1:	// CMP (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateCmp;
			break;

		case 0xDD:	// CMP abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateCmp;
			break;

		case 0xDE:	// DEC abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJzAddToPath : kStateJz;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xF1:	// SBC (zp),Y
			DecodeReadIndY();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateSbc;
			break;

		case 0xFD:	// SBC abs,X
			DecodeReadAbsX();
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
			*mpDstState++ = kStateSbc;
			break;

		case 0xFE:	// INC abs,X
			*mpDstState++ = kStateReadAddrL;		// 2
			*mpDstState++ = kStateReadAddrHX;		// 3
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;
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
