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

bool ATCPUEmulator::Decode65C02(uint8 opcode) {
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
			*mpDstState++ = kStateBitSetSV;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJ0AddToPath : kStateJ0;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJ1AddToPath : kStateJ1;
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
			*mpDstState++ = kStateBitSetSV;
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
			*mpDstState++ = mbPathfindingEnabled ? kStateJAddToPath : kStateJ;
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

