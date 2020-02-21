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

bool ATCPUEmulator::Decode6502Ill(uint8 opcode) {
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
