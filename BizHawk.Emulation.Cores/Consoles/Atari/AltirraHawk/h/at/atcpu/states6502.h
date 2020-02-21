//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
//	CPU emulation library - state definitions
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

#ifndef f_AT_ATCPU_STATES6502_H
#define f_AT_ATCPU_STATES6502_H

#include <vd2/system/vdtypes.h>

namespace ATCPUStates6502 {
	enum ATCPUState6502 : uint8 {
		kStateNop,
		kStateReadOpcode,
		kStateReadOpcodeNoBreak,
		kStateAddToHistory,
		kStateAddEAToHistory,
		kStateReadDummyOpcode,
		kStateBreakOnUnsupportedOpcode,
		kStateReadImm,
		kStateReadAddrL,
		kStateReadAddrH,
		kStateReadAddrHX,
		kStateReadAddrHY,
		kStateReadAddrHX_SHY,
		kStateReadAddrHY_SHA,
		kStateReadAddrHY_SHX,
		kStateRead,
		kStateReadSetSZToA,
		kStateReadAddX,
		kStateReadAddY,
		kStateReadCarry,
		kStateReadCarryForced,
		kStateReadAbsIndAddr,
		kStateReadAbsIndAddrBroken,
		kStateReadIndAddr,					// Read high byte of indirect address into address register, wrapped in zero page.
		kStateReadIndYAddr,
		kStateReadIndYAddr_SHA,
		kStateWrite,
		kStateWriteA,
		kStateWait,
		kStateAtoD,
		kStateXtoD,
		kStateYtoD,
		kStateStoD,
		kStatePtoD,
		kStatePtoD_B0,
		kStatePtoD_B1,
		kState0toD,
		kStateDtoA,
		kStateDtoX,
		kStateDtoY,
		kStateDtoS,
		kStateDtoP,
		kStateDtoP_noICheck,
		kStateDSetSZ,
		kStateDSetSZToA,
		kStateDSetSV,
		kStateAddrToPC,
		kStateNMIVecToPC,
		kStateIRQVecToPC,
		kStateNMIOrIRQVecToPC,
		kStatePush,
		kStatePushPCL,
		kStatePushPCH,
		kStatePushPCLM1,
		kStatePushPCHM1,
		kStatePop,
		kStatePopPCL,
		kStatePopPCH,
		kStatePopPCHP1,
		kStateAdc,
		kStateSbc,
		kStateCmp,
		kStateCmpX,
		kStateCmpY,
		kStateInc,
		kStateIncXWait,
		kStateDec,
		kStateDecXWait,
		kStateDecC,
		kStateAnd,
		kStateAnd_SAX,
		kStateAnc,
		kStateXaa,
		kStateLas,
		kStateSbx,
		kStateArr,
		kStateXas,
		kStateOr,
		kStateXor,
		kStateAsl,
		kStateLsr,
		kStateRol,
		kStateRor,
		kStateBit,
		kStateSEI,
		kStateCLI,
		kStateSEC,
		kStateCLC,
		kStateSED,
		kStateCLD,
		kStateCLV,
		kStateJs,
		kStateJns,
		kStateJc,
		kStateJnc,
		kStateJz,
		kStateJnz,
		kStateJo,
		kStateJno,
		kStateJccFalseRead,
		kStateStepOver,
		kStateRegenerateDecodeTables,

		// 65C02 states
		kStateResetBit,
		kStateSetBit,
		kStateReadRel,
		kStateJ0,
		kStateJ1,
		kStateJ,
		kStateWaitForInterrupt,
		kStateStop,
		kStateTrb,
		kStateTsb,		// also used by BIT #imm
		kStateC02_Adc,
		kStateC02_Sbc,

		kStateCount
	};
}

#endif
