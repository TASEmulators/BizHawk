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

bool ATCPUEmulator::Decode65C816(uint8 opcode, bool unalignedDP, bool emu, bool mode16, bool index16) {
	// Decode group I instructions (119 opcodes)
	static const bool kIsGroupI[32]={
		false,	// 00
		true,	// 01: (dp,X)
		false,	// 02
		true,	// 03: sr,S
		false,	// 04
		true,	// 05: dp
		false,	// 06
		true,	// 07: [dp]
		false,	// 08
		true,	// 09: imm
		false,	// 0A
		false,	// 0B
		false,	// 0C
		true,	// 0D: abs
		false,	// 0E
		true,	// 0F: long
		false,	// 10
		true,	// 11: (dp),Y
		true,	// 12: (dp)
		true,	// 13: (sr,S),Y
		false,	// 14
		true,	// 15: dp,X
		false,	// 16
		true,	// 17: [dp],Y
		false,	// 18
		true,	// 19: abs,Y
		false,	// 1A
		false,	// 1B
		false,	// 1C
		true,	// 1D: abs,X
		false,	// 1E
		true,	// 1F: long,X
	};

	// 89 is a special case (STA #imm)
	if (kIsGroupI[opcode & 0x1F] && opcode != 0x89) {
		bool is_write = (opcode & 0xE0) == 0x80;
		bool is_dp = false;

		switch(opcode & 0x1F) {
			case 0x01:
				Decode65816AddrDpIndX(unalignedDP, emu);
				break;

			case 0x03:
				Decode65816AddrStackRel();
				break;

			case 0x05:
				Decode65816AddrDp(unalignedDP);
				is_dp = true;
				break;

			case 0x07:
				Decode65816AddrDpLongInd(unalignedDP);
				break;

			case 0x09:
				if (mode16) {
					*mpDstState++ = kStateReadImmL16;
					*mpDstState++ = kStateReadImmH16;
				} else
					*mpDstState++ = kStateReadImm;
				break;

			case 0x0D:
				Decode65816AddrAbs();
				break;

			case 0x0F:
				Decode65816AddrAbsLong();
				break;

			case 0x11:
				Decode65816AddrDpIndY(unalignedDP, emu, index16 || is_write);
				break;

			case 0x12:
				Decode65816AddrDpInd(unalignedDP);
				break;

			case 0x13:
				Decode65816AddrStackRelInd();
				break;

			case 0x15:
				Decode65816AddrDpX(unalignedDP, emu);
				is_dp = true;
				break;

			case 0x17:
				Decode65816AddrDpLongIndY(unalignedDP);
				break;

			case 0x19:
				Decode65816AddrAbsY(index16 || is_write);
				break;

			case 0x1D:
				Decode65816AddrAbsX(index16 || is_write);
				break;

			case 0x1F:
				Decode65816AddrAbsLongX();
				break;
		}

		if (!is_write && ((opcode & 0x1f) != 0x09)) {
			if (mode16) {
				*mpDstState++ = kStateReadL16;

				if (is_dp)
					*mpDstState++ = kStateReadH16_DpBank;
				else
					*mpDstState++ = kStateReadH16;
			} else {
				*mpDstState++ = kState816ReadByte;
			}
		}

		switch(opcode & 0xE0) {
			case 0x00:	// ORA
				if (mode16)
					*mpDstState++ = kStateOr16;
				else
					*mpDstState++ = kStateOr;
				break;

			case 0x20:	// AND
				if (mode16) {
					*mpDstState++ = kStateAnd16;
					*mpDstState++ = kStateDtoA16;
				} else {
					*mpDstState++ = kStateAnd;
					*mpDstState++ = kStateDtoA;
				}
				break;

			case 0x40:	// EOR
				if (mode16)
					*mpDstState++ = kStateXor16;
				else
					*mpDstState++ = kStateXor;
				break;

			case 0x60:	// ADC
				if (mode16)
					*mpDstState++ = kStateAdc16;
				else {
					*mpDstState++ = kStateC02_Adc;
					*mpDstState++ = kStateNop;		// 65C816 doesn't take extra clock for decimal flags.
				}
				break;

			case 0x80:	// STA
				if (mode16) {
					*mpDstState++ = kStateAtoD16;
					*mpDstState++ = kStateWriteL16;
					*mpDstState++ = kStateWriteH16;
				} else {
					*mpDstState++ = kStateAtoD;
					*mpDstState++ = kState816WriteByte;
				}
				break;

			case 0xA0:	// LDA
				if (mode16) {
					*mpDstState++ = kStateDSetSZ16;
					*mpDstState++ = kStateDtoA16;
				} else {
					*mpDstState++ = kStateDSetSZ;
					*mpDstState++ = kStateDtoA;
				}
				break;

			case 0xC0:	// CMP
				if (mode16)
					*mpDstState++ = kStateCmp16;
				else
					*mpDstState++ = kStateCmp;
				break;

			case 0xE0:	// SBC
				if (mode16)
					*mpDstState++ = kStateSbc16;
				else {
					*mpDstState++ = kStateC02_Sbc;
					*mpDstState++ = kStateNop;		// 65C816 doesn't take extra clock for decimal flags.
				}
				break;

		}

		return true;
	}

	switch(opcode) {
		case 0x00:	// BRK
			if (mbStopOnBRK)
				*mpDstState++ = kStateBreakOnUnsupportedOpcode;

			*mpDstState++ = kStateReadAddrL;	// 1
			*mpDstState++ = kStateReadAddrH;	// 2

			if (!emu)
				*mpDstState++ = kStatePushPBKNative;	// 3*

			*mpDstState++ = emu ? kStatePushPCHM1 : kStatePushPCHM1Native;	// 3
			*mpDstState++ = emu ? kStatePushPCLM1 : kStatePushPCLM1Native;	// 4

			if (emu) 
				*mpDstState++ = kStatePtoD_B1;
			else
				*mpDstState++ = kStatePtoD;

			*mpDstState++ = kStatePush;			// 5
			*mpDstState++ = kState816_SetI_ClearD;

			if (emu)
				*mpDstState++ = kStateIRQVecToPC;
			else
				*mpDstState++ = kState816_NatBRKVecToPC;

			*mpDstState++ = kStateReadAddrL;	// 6
			*mpDstState++ = kStateReadAddrH;	// 7
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x02:	// COP
			*mpDstState++ = kStateReadImm;			// 2: read signature

			if (!emu)
				*mpDstState++ = kStatePushPBKNative;	// 3: push program bank

			*mpDstState++ = kStatePushPCHNative;	// 4: push PCH
			*mpDstState++ = kStatePushPCLNative;	// 5: push PCL
			*mpDstState++ = kStatePtoD;				//
			*mpDstState++ = kStatePushNative;		// 6: push P
			*mpDstState++ = kState816_SetI_ClearD;	//

			if (emu)
				*mpDstState++ = kState816_EmuCOPVecToPC;	//
			else
				*mpDstState++ = kState816_NatCOPVecToPC;	//

			*mpDstState++ = kStateReadAddrL;		// 7: read vector low
			*mpDstState++ = kStateReadAddrH;		// 8: read vector high
			*mpDstState++ = kStateAddrToPC;			//
			if (mbStepOver)
				*mpDstState++ = kStateStepOver;
			break;

		case 0x04:	// TSB zp
			Decode65816AddrDp(unalignedDP);				// 2

			if (mode16) {
				*mpDstState++ = kStateReadL16;			// 3
				*mpDstState++ = kStateReadH16_DpBank;	// 4
				*mpDstState++ = kStateTsb16;			// 5
				*mpDstState++ = kStateWait;				//
				*mpDstState++ = kStateWriteH16_DpBank;	// 6
				*mpDstState++ = kStateWriteL16;			// 7
			} else {
				*mpDstState++ = kState816ReadByte;		// 3
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateTsb;				// 4
				*mpDstState++ = kState816WriteByte;			// 5
			}
			break;

		case 0x06:	// ASL zp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateAsl16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateAsl;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x08:	// PHP
			*mpDstState++ = kStatePtoD;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePushNative;
			break;

		case 0x0A:	// ASL A
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateAsl16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateAsl;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA;
			}
			break;

		case 0x0B:	// PHD
			*mpDstState++ = kStateDPtoD16;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePushH16;
			*mpDstState++ = kStatePushL16;
			break;

		case 0x0C:	// TSB abs
			Decode65816AddrAbs();						// 2, 3

			if (mode16) {
				*mpDstState++ = kStateReadL16;			// 4
				*mpDstState++ = kStateReadH16;			// 5
				*mpDstState++ = kStateTsb16;			// 6
				*mpDstState++ = kStateWait;				//
				*mpDstState++ = kStateWriteH16;			// 7
				*mpDstState++ = kStateWriteL16;			// 8
			} else {
				*mpDstState++ = kState816ReadByte;		// 4
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateTsb;				// 5
				*mpDstState++ = kState816WriteByte;			// 6
			}
			break;

		case 0x0E:	// ASL abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateAsl16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateAsl;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x10:	// BPL rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJnsAddToPath : kStateJns;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x14:	// TRB dp
			Decode65816AddrDp(unalignedDP);				// 2

			if (mode16) {
				*mpDstState++ = kStateReadL16;			// 3
				*mpDstState++ = kStateReadH16_DpBank;	// 4
				*mpDstState++ = kStateTrb16;			// 5
				*mpDstState++ = kStateWait;				//
				*mpDstState++ = kStateWriteH16_DpBank;	// 6
				*mpDstState++ = kStateWriteL16;			// 7
			} else {
				*mpDstState++ = kState816ReadByte;		// 3
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateTrb;				// 4
				*mpDstState++ = kState816WriteByte;			// 5
			}
			break;

		case 0x16:	// ASL dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateAsl16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateAsl;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x18:	// CLC
			*mpDstState++ = kStateCLC;
			*mpDstState++ = kStateWait;
			break;

		case 0x1A:	// INC A
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA;
			}
			break;

		case 0x1B:	// TCS
			*mpDstState++ = kStateAtoD16;
			*mpDstState++ = kStateDtoS16;
			*mpDstState++ = kStateWait;
			break;

		case 0x1C:	// TRB abs
			Decode65816AddrAbs();						// 2, 3

			if (mode16) {
				*mpDstState++ = kStateReadL16;			// 4
				*mpDstState++ = kStateReadH16;			// 5
				*mpDstState++ = kStateTrb16;			// 6
				*mpDstState++ = kStateWait;				//
				*mpDstState++ = kStateWriteH16;			// 7
				*mpDstState++ = kStateWriteL16;			// 8
			} else {
				*mpDstState++ = kState816ReadByte;		// 4
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateTrb;				// 5
				*mpDstState++ = kState816WriteByte;			// 6
			}
			break;

		case 0x1E:	// ASL abs,X
			Decode65816AddrAbsX(true);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateAsl16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateAsl;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x20:	// JSR
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;

			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;

			*mpDstState++ = emu ? kStatePushPCHM1 : kStatePushPCHM1Native;
			*mpDstState++ = emu ? kStatePushPCLM1 : kStatePushPCLM1Native;
			*mpDstState++ = kStateAddrToPC;
			*mpDstState++ = kStateWait;

			if (mbStepOver)
				*mpDstState++ = kStateStepOver;

			if (mbPathfindingEnabled)
				*mpDstState++ = kStateAddAsPathStart;
			break;

		case 0x22:	// JSL long
			*mpDstState++ = kStateReadAddrL;			// 2: read jump address low
			*mpDstState++ = kStateReadAddrH;			// 3: read jump address high
			*mpDstState++ = kStatePushPBKNative;		// 4: push program bank
			*mpDstState++ = kStateWait;					// 5: internal operation
			*mpDstState++ = kStateReadAddrB;			// 6: read jump address bank
			*mpDstState++ = kStatePushPCHM1Native;		// 7: push PCH
			*mpDstState++ = kStatePushPCLM1Native;		// 8: push PCL
			*mpDstState++ = kState816_LongAddrToPC;

			if (mbStepOver)
				*mpDstState++ = kStateStepOver;

			if (mbPathfindingEnabled)
				*mpDstState++ = kStateAddAsPathStart;
			break;

		case 0x24:	// BIT dp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateBitSetSV16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateBitSetSV;
			}
			break;

		case 0x26:	// ROL dp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateRol16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRol;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x28:	// PLP
			*mpDstState++ = emu ? kStatePop : kStatePopNative;

			if (emu)
				*mpDstState++ = kStateDtoP;
			else
				*mpDstState++ = kStateDtoPNative;

			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			break;

		case 0x2A:	// ROL A
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateRol16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateRol;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA;
			}
			break;

		case 0x2B:	// PLD
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePopL16;
			*mpDstState++ = kStatePopH16;
			*mpDstState++ = kStateDtoDP16;
			break;

		case 0x2C:	// BIT abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateBitSetSV16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateBitSetSV;
			}
			break;

		case 0x2E:	// ROL abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateRol16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRol;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x30:	// BMI rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJsAddToPath : kStateJs;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x34:	// BIT dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateBitSetSV16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateBitSetSV;
			}
			break;

		case 0x36:	// ROL dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateRol16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kStateWait;
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRol;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x38:	// SEC
			*mpDstState++ = kStateSEC;
			*mpDstState++ = kStateWait;
			break;

		case 0x3A:	// DEC A
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA;
			}
			break;

		case 0x3B:	// TSC
			*mpDstState++ = kStateStoD16;
			*mpDstState++ = kStateDSetSZ16;
			*mpDstState++ = kStateDtoA16;
			*mpDstState++ = kStateWait;
			break;

		case 0x3C:	// BIT abs,X
			Decode65816AddrAbsX(index16);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateBitSetSV16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateBitSetSV;
			}
			break;

		case 0x3E:	// ROL abs,X
			Decode65816AddrAbsX(true);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateRol16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRol;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x40:	// RTI
			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;

			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = emu ? kStatePop : kStatePopNative;

			if (emu)
				*mpDstState++ = kStateDtoP_noICheck;
			else
				*mpDstState++ = kStateDtoPNative_noICheck;

			*mpDstState++ = kStatePopPCLNative;
			*mpDstState++ = kStatePopPCHNative;

			if (!emu)
				*mpDstState++ = kStatePopPBKNative;
			break;

		case 0x42:	// WDM
			*mpDstState++ = kStateWait;
			break;

		case 0x44:	// MVP
			*mpDstState++ = kStateReadImm;		// 2: DBA
			*mpDstState++ = kStateDtoB;			//
			*mpDstState++ = kStateReadAddrB;	// 3: SBA
			*mpDstState++ = kState816_MoveRead;	// 4: Source data
			*mpDstState++ = kState816_MoveWriteP;// 5: Dest data
			*mpDstState++ = kState816ReadByte;	// 6: False read of destination
			*mpDstState++ = kState816ReadByte;	// 7: False read of destination
			break;

		case 0x46:	// LSR dp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateLsr16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateLsr;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x48:	// PHA
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStatePushH16;
				*mpDstState++ = kStatePushL16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStatePushNative;
			}
			break;

		case 0x4A:	// LSR
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateLsr16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateLsr;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA;
			}
			break;

		case 0x4B:	// PHK
			*mpDstState++ = kStateKtoD;
			*mpDstState++ = emu ? kStatePush : kStatePushNative;
			break;

		case 0x4C:	// JMP abs
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;

			if (mpVerifier)
				*mpDstState++ = kStateVerifyInsn;

			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x4E:	// LSR abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateLsr16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateLsr;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x50:	// BVC
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJnoAddToPath : kStateJno;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x54:	// MVN
			*mpDstState++ = kStateReadImm;		// 2: DBA
			*mpDstState++ = kStateDtoB;			//
			*mpDstState++ = kStateReadAddrB;	// 3: SBA
			*mpDstState++ = kState816_MoveRead;	// 4: Source data
			*mpDstState++ = kState816_MoveWriteN;// 5: Dest data
			*mpDstState++ = kState816ReadByte;	// 6: False read of destination
			*mpDstState++ = kState816ReadByte;	// 7: False read of destination
			break;

		case 0x56:	// LSR dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateLsr16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateLsr;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x58:	// CLI
			*mpDstState++ = kStateCLI;
			*mpDstState++ = kStateWait;
			break;

		case 0x5A:	// PHY
			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStatePushH16;
				*mpDstState++ = kStatePushL16;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStatePushNative;
			}
			break;

		case 0x5B:	// TCD
			*mpDstState++ = kStateAtoD16;
			*mpDstState++ = kStateDtoDP16;
			*mpDstState++ = kStateDSetSZ16;
			*mpDstState++ = kStateWait;
			break;

		case 0x5C:	// JMP al
			*mpDstState++ = kStateReadAddrL;
			*mpDstState++ = kStateReadAddrH;
			*mpDstState++ = kStateReadAddrB;
			*mpDstState++ = kState816_LongAddrToPC;
			break;

		case 0x5E:	// LSR abs,X
			Decode65816AddrAbsX(true);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateLsr16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateLsr;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x60:	// RTS
			if (emu) {
				*mpDstState++ = kStatePopPCL;
				*mpDstState++ = kStatePopPCHP1;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStatePopPCLNative;
				*mpDstState++ = kStatePopPCHP1Native;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x62:	// PER
			*mpDstState++ = kStateReadImmL16;	// 2
			*mpDstState++ = kStateReadImmH16;	// 3
			*mpDstState++ = kState816_Per;		// 4
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStatePushH16;		// 5
			*mpDstState++ = kStatePushL16;		// 6

			break;

		case 0x64:	// STZ zp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kState0toD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kState0toD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x66:	// ROR dp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateRor16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRor;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x68:	// PLA
			if (mode16) {
				*mpDstState++ = kStatePopL16;
				*mpDstState++ = kStatePopH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoA16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = emu ? kStatePop : kStatePopNative;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoA;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x6A:	// ROR A
			if (mode16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateRor16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA16;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateRor;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDtoA;
			}
			break;

		case 0x6B:	// RTL
			*mpDstState++ = kStateWait;				// 2
			*mpDstState++ = kStateWait;				// 3
			*mpDstState++ = kStatePopPCLNative;		// 4
			*mpDstState++ = kStatePopPCHP1Native;	// 5
			*mpDstState++ = kStatePopPBKNative;		// 6
			break;

		case 0x6C:	// JMP (abs)
			Decode65816AddrAbs();
			*mpDstState++ = kState816_SetBank0;
			*mpDstState++ = kState816ReadByte;
			*mpDstState++ = kState816ReadAddrAbsInd;
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x6E:	// ROR abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateRor16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRor;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x70:	// BVS
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJoAddToPath : kStateJo;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x74:	// STZ zp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kState0toD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kState0toD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x76:	// ROR dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateRor16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateRor;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x78:	// SEI
			*mpDstState++ = kStateSEI;
			*mpDstState++ = kStateWait;
			break;

		case 0x7A:	// PLY
			if (index16) {
				*mpDstState++ = kStatePopL16;
				*mpDstState++ = kStatePopH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = emu ? kStatePop : kStatePopNative;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x7B:	// TDC
			*mpDstState++ = kStateDPtoD16;
			*mpDstState++ = kStateDSetSZ16;
			*mpDstState++ = kStateDtoA16;
			*mpDstState++ = kStateWait;
			break;

		case 0x7C:	// JMP (abs,X)
			*mpDstState++ = kState816ReadAddrL;
			*mpDstState++ = kState816ReadAddrHX;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kState816_SetBankPBR;
			*mpDstState++ = kState816ReadByte;
			*mpDstState++ = kState816ReadAddrAbsInd;
			*mpDstState++ = kStateAddrToPC;
			break;

		case 0x7E:	// ROR abs,X
			Decode65816AddrAbsX(true);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateRor16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateRor;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x80:	// BRA rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJAddToPath : kStateJ;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x82:	// BRL rel16
			*mpDstState++ = kStateReadImmL16;
			*mpDstState++ = kStateReadImmH16;
			*mpDstState++ = mbPathfindingEnabled ? kStateJ16AddToPath : kStateJ16;
			break;

		case 0x84:	// STY zp
			Decode65816AddrDp(unalignedDP);

			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x86:	// STX zp
			Decode65816AddrDp(unalignedDP);

			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kState816WriteByte;	
			}
			break;

		case 0x88:	// DEY
			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateDtoY16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kStateDtoY;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x89:	// BIT #imm
			if (mode16) {
				*mpDstState++ = kStateReadImmL16;
				*mpDstState++ = kStateReadImmH16;
				*mpDstState++ = kStateTsb16;
			} else {
				*mpDstState++ = kStateReadImm;
				*mpDstState++ = kStateTsb;
			}
			break;

		case 0x8A:	// TXA
			if (mode16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoA16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoA;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x8B:	// PHB
			*mpDstState++ = kStateBtoD;
			*mpDstState++ = emu ? kStatePush : kStatePushNative;
			break;

		case 0x8C:	// STY abs
			Decode65816AddrAbs();

			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x8E:	// STX abs
			Decode65816AddrAbs();

			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x90:	// BCC rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJncAddToPath : kStateJnc;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0x94:	// STY zp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x96:	// STX zp,Y
			Decode65816AddrDpY(unalignedDP, emu);

			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0x98:	// TYA
			if (mode16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoA16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoA;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x9A:	// TXS
			if (emu) {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kStateDtoS;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateDtoS16;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0x9B:	// TXY
			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
			}
			*mpDstState++ = kStateWait;
			break;

		case 0x9C:	// STZ abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kState0toD16;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kState0toD;
				*mpDstState++ = kState816WriteByte;			// 4
			}
			break;

		case 0x9E:	// STZ abs,X
			Decode65816AddrAbsX(index16);

			if (mode16) {
				*mpDstState++ = kState0toD16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteL16;
				*mpDstState++ = kStateWriteH16;
			} else {
				*mpDstState++ = kState0toD;
				*mpDstState++ = kStateWait;				// 4
				*mpDstState++ = kState816WriteByte;			// 5
			}
			break;

		case 0xA0:	// LDY imm
			if (index16) {
				*mpDstState++ = kStateReadImmL16;
				*mpDstState++ = kStateReadImmH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
			} else {
				*mpDstState++ = kStateReadImm;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
			}
			break;

		case 0xA2:	// LDX imm
			if (index16) {
				*mpDstState++ = kStateReadImmL16;
				*mpDstState++ = kStateReadImmH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
			} else {
				*mpDstState++ = kStateReadImm;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
			}
			break;

		case 0xA4:	// LDY dp
			Decode65816AddrDp(unalignedDP);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
			}
			break;

		case 0xA6:	// LDX dp
			Decode65816AddrDp(unalignedDP);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
			}
			break;

		case 0xA8:	// TAY
			if (index16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xAA:	// TAX
			if (index16) {
				*mpDstState++ = kStateAtoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateAtoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xAB:	// PLB
			*mpDstState++ = kStatePopNative;	//** doesn't wrap even in emu mode
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateWait;
			*mpDstState++ = kStateDtoB;
			break;

		case 0xAC:	// LDY abs
			Decode65816AddrAbs();

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
			}
			break;

		case 0xAE:	// LDX abs
			Decode65816AddrAbs();

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
			}
			break;

		case 0xB0:	// BCS rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJcAddToPath : kStateJc;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xB4:	// LDY dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
			}
			break;

		case 0xB6:	// LDX dp,Y
			Decode65816AddrDpY(unalignedDP, emu);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
			}
			break;

		case 0xB8:	// CLV
			*mpDstState++ = kStateCLV;
			*mpDstState++ = kStateWait;
			break;

		case 0xBA:	// TSX
			if (index16) {
				*mpDstState++ = kStateStoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateStoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xBB:	// TYX
			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
			}
			*mpDstState++ = kStateWait;
			break;

		case 0xBC:	// LDY abs,X
			Decode65816AddrAbsX(index16);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoY16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoY;
			}
			break;

		case 0xBE:	// LDX abs,Y
			Decode65816AddrAbsY(index16);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
			}
			break;

		case 0xC0:	// CPY imm
			if (index16) {
				*mpDstState++ = kStateReadImmL16;
				*mpDstState++ = kStateReadImmH16;
				*mpDstState++ = kStateCmpY16;
			} else {
				*mpDstState++ = kStateReadImm;
				*mpDstState++ = kStateCmpY;
			}
			break;

		case 0xC2:	// REP
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateRep;
			break;

		case 0xC4:	// CPY dp
			Decode65816AddrDp(unalignedDP);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateCmpY16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateCmpY;
			}
			break;

		case 0xC6:	// DEC dp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xC8:	// INY
			if (index16) {
				*mpDstState++ = kStateYtoD16;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateDtoY16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateYtoD;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kStateDtoY;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xCA:	// DEX
			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateDtoX16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kStateDtoX;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xCB:	// WAI
			*mpDstState++ = kStateWaitForInterrupt;
			break;

		case 0xCC:	// CPY abs
			Decode65816AddrAbs();

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateCmpY16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateCmpY;
			}
			break;

		case 0xCE:	// DEC abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xD0:	// BNE rel
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJnzAddToPath : kStateJnz;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xD4:	// PEI (dp)
			Decode65816AddrDp(unalignedDP);
			*mpDstState++ = kStateReadL16;
			*mpDstState++ = kStateReadH16_DpBank;
			*mpDstState++ = kStatePushH16;
			*mpDstState++ = kStatePushL16;
			break;

		case 0xD6:	// DEC dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xD8:	// CLD
			*mpDstState++ = kStateCLD;
			*mpDstState++ = kStateWait;
			break;

		case 0xDA:	// PHX
			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStatePushH16;
				*mpDstState++ = kStatePushL16;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStatePushNative;
			}
			break;

		case 0xDB:	// STP
			*mpDstState++ = kStateStop;
			break;

		case 0xDC:	// JML [abs]
			Decode65816AddrAbs();
			*mpDstState++ = kState816_SetBank0;
			*mpDstState++ = kStateRead816AddrAbsLongL;
			*mpDstState++ = kStateRead816AddrAbsLongH;
			*mpDstState++ = kStateRead816AddrAbsLongB;
			*mpDstState++ = kState816_LongAddrToPC;
			break;

		case 0xDE:	// DEC abs,X
			Decode65816AddrAbsX(true);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateDec16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateDec;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xE0:	// CPX imm
			if (index16) {
				*mpDstState++ = kStateReadImmL16;
				*mpDstState++ = kStateReadImmH16;
				*mpDstState++ = kStateCmpX16;
			} else {
				*mpDstState++ = kStateReadImm;
				*mpDstState++ = kStateCmpX;
			}
			break;

		case 0xE2:	// SEP
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = kStateSep;
			break;

		case 0xE4:	// CPX dp
			Decode65816AddrDp(unalignedDP);

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateCmpX16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateCmpX;
			}
			break;

		case 0xE6:	// INC dp
			Decode65816AddrDp(unalignedDP);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xE8:	// INX
			if (index16) {
				*mpDstState++ = kStateXtoD16;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateDtoX16;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = kStateXtoD;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kStateDtoX;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xEA:	// NOP
			*mpDstState++ = kStateWait;		// 2
			break;

		case 0xEB:	// XBA
			*mpDstState++ = kStateXba;
			*mpDstState++ = kStateWait;		// 2
			*mpDstState++ = kStateWait;		// 3
			break;

		case 0xEC:	// CPX abs
			Decode65816AddrAbs();

			if (index16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateCmpX16;
			} else {
				*mpDstState++ = kState816ReadByte;
				*mpDstState++ = kStateCmpX;
			}
			break;

		case 0xEE:	// INC abs
			Decode65816AddrAbs();

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xF0:	// BEQ rel8
			*mpDstState++ = kStateReadImm;
			*mpDstState++ = mbPathfindingEnabled ? kStateJzAddToPath : kStateJz;
			*mpDstState++ = kStateJccFalseRead;
			break;

		case 0xF4:	// PEA abs
			*mpDstState++ = kStateReadImmL16;
			*mpDstState++ = kStateReadImmH16;
			*mpDstState++ = kStatePushH16;
			*mpDstState++ = kStatePushL16;
			break;

		case 0xF6:	// INC dp,X
			Decode65816AddrDpX(unalignedDP, emu);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16_DpBank;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16_DpBank;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		case 0xF8:	// SED
			*mpDstState++ = kStateSED;
			*mpDstState++ = kStateWait;
			break;

		case 0xFA:	// PLX
			if (index16) {
				*mpDstState++ = kStatePopL16;
				*mpDstState++ = kStatePopH16;
				*mpDstState++ = kStateDSetSZ16;
				*mpDstState++ = kStateDtoX16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			} else {
				*mpDstState++ = emu ? kStatePop : kStatePopNative;
				*mpDstState++ = kStateDSetSZ;
				*mpDstState++ = kStateDtoX;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWait;
			}
			break;

		case 0xFB:	// XCE
			*mpDstState++ = kStateXce;
			*mpDstState++ = kStateWait;
			break;

		case 0xFC:	// JSR (abs,X)
			// The timing is weird on this one.
			*mpDstState++ = kState816ReadAddrL;		// 2: read abs low
			*mpDstState++ = kStatePushPCHNative;	// 3: push PCH
			*mpDstState++ = kStatePushPCLNative;	// 4: push PCL
			*mpDstState++ = kState816ReadAddrHX;	// 5: read abs high
			*mpDstState++ = kStateWait;				// 6: internal operation
			*mpDstState++ = kState816_SetBankPBR;
			*mpDstState++ = kState816ReadByte;		// 7: read new PCL
			*mpDstState++ = kState816ReadAddrAbsInd;// 8: read new PCH
			*mpDstState++ = kStateAddrToPC;

			if (mbStepOver)
				*mpDstState++ = kStateStepOver;

			if (mbPathfindingEnabled)
				*mpDstState++ = kStateAddAsPathStart;
			break;

		case 0xFE:	// INC abs,X
			Decode65816AddrAbsX(true);

			if (mode16) {
				*mpDstState++ = kStateReadL16;
				*mpDstState++ = kStateReadH16;
				*mpDstState++ = kStateInc16;
				*mpDstState++ = kStateWait;
				*mpDstState++ = kStateWriteH16;
				*mpDstState++ = kStateWriteL16;
			} else {
				*mpDstState++ = kState816ReadByte;
				if (emu)
					*mpDstState++ = kState816WriteByte;
				else
					*mpDstState++ = kStateWait;
				*mpDstState++ = kStateInc;
				*mpDstState++ = kState816WriteByte;
			}
			break;

		default:
			return false;
	}

	return true;
}

void ATCPUEmulator::Decode65816AddrDp(bool unalignedDP) {
	*mpDstState++ = kStateReadAddrDp;

	if (unalignedDP)
		*mpDstState++ = kStateWait;
}

void ATCPUEmulator::Decode65816AddrDpX(bool unalignedDP, bool emu) {
	*mpDstState++ = unalignedDP || !emu ? kStateReadAddrDpX : kStateReadAddrDpXInPage;
	*mpDstState++ = kStateWait;

	if (unalignedDP)
		*mpDstState++ = kStateWait;
}

void ATCPUEmulator::Decode65816AddrDpY(bool unalignedDP, bool emu) {
	*mpDstState++ = unalignedDP || !emu ? kStateReadAddrDpY : kStateReadAddrDpYInPage;
	*mpDstState++ = kStateWait;

	if (unalignedDP)
		*mpDstState++ = kStateWait;
}

void ATCPUEmulator::Decode65816AddrDpInd(bool unalignedDP) {
	*mpDstState++ = kStateReadAddrDp;
	
	if (unalignedDP)
		*mpDstState++ = kStateWait;

	*mpDstState++ = kState816ReadByte;	
	*mpDstState++ = kStateReadIndAddrDp;
}

void ATCPUEmulator::Decode65816AddrDpIndX(bool unalignedDP, bool emu) {
	*mpDstState++ = unalignedDP || !emu ? kStateReadAddrDpX : kStateReadAddrDpXInPage;
	
	if (unalignedDP)
		*mpDstState++ = kStateWait;

	*mpDstState++ = kStateWait;
	*mpDstState++ = kState816ReadByte;	
	*mpDstState++ = unalignedDP || !emu ? kStateReadIndAddrDp : kState816ReadIndAddrDpInPage;
}

void ATCPUEmulator::Decode65816AddrDpIndY(bool unalignedDP, bool emu, bool forceCycle) {
	*mpDstState++ = kStateReadAddrDp;
	
	if (unalignedDP)
		*mpDstState++ = kStateWait;

	*mpDstState++ = kState816ReadByte;	
	*mpDstState++ = unalignedDP || !emu ? kStateReadIndAddrDp : kState816ReadIndAddrDpInPage;
	*mpDstState++ = forceCycle ? kState816ReadAddrAbsYAlways : kState816ReadAddrAbsYSpec;
}

void ATCPUEmulator::Decode65816AddrDpLongInd(bool unalignedDP) {
	*mpDstState++ = kStateReadAddrDp;

	if (unalignedDP)
		*mpDstState++ = kStateWait;

	*mpDstState++ = kState816ReadByte;	
	*mpDstState++ = kStateReadIndAddrDpLongH;	
	*mpDstState++ = kStateReadIndAddrDpLongB;	
}

void ATCPUEmulator::Decode65816AddrDpLongIndY(bool unalignedDP) {
	*mpDstState++ = kStateReadAddrDp;

	if (unalignedDP)
		*mpDstState++ = kStateWait;

	*mpDstState++ = kState816ReadByte;	
	*mpDstState++ = kStateReadIndAddrDpLongH;
	*mpDstState++ = kStateReadIndAddrDpLongB;
	*mpDstState++ = kStateReadAddrAddY;
}

void ATCPUEmulator::Decode65816AddrAbs() {
	*mpDstState++ = kState816ReadAddrL;
	*mpDstState++ = kStateReadAddrH;
}

void ATCPUEmulator::Decode65816AddrAbsX(bool forceCycle) {
	*mpDstState++ = kState816ReadAddrL;
	*mpDstState++ = kState816ReadAddrH;
	*mpDstState++ = forceCycle ? kState816ReadAddrAbsXAlways : kState816ReadAddrAbsXSpec;
}

void ATCPUEmulator::Decode65816AddrAbsY(bool forceCycle) {
	*mpDstState++ = kState816ReadAddrL;
	*mpDstState++ = kState816ReadAddrH;
	*mpDstState++ = forceCycle ? kState816ReadAddrAbsYAlways : kState816ReadAddrAbsYSpec;
}

void ATCPUEmulator::Decode65816AddrAbsLong() {
	*mpDstState++ = kStateReadAddrL;
	*mpDstState++ = kStateReadAddrH;
	*mpDstState++ = kStateReadAddrB;
}

void ATCPUEmulator::Decode65816AddrAbsLongX() {
	*mpDstState++ = kStateReadAddrL;
	*mpDstState++ = kStateReadAddrH;
	*mpDstState++ = kStateReadAddrBX;
}

void ATCPUEmulator::Decode65816AddrStackRel() {
	*mpDstState++ = kStateReadAddrSO;
	*mpDstState++ = kStateWait;
}

void ATCPUEmulator::Decode65816AddrStackRelInd() {
	*mpDstState++ = kStateReadAddrSO;	// 2
	*mpDstState++ = kStateWait;			// 3
	*mpDstState++ = kStateRead816AddrAbsLongL;	// 4
	*mpDstState++ = kStateRead816AddrAbsLongH;	// 5
	*mpDstState++ = kState816ReadAddrSO_AddY;	// 6
}
