//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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
#include <vd2/system/binary.h>
#include <at/atcore/address.h>
#include <at/atcpu/execstate.h>
#include <at/atdebugger/target.h>
#include "simulator.h"
#include "console.h"
#include "debugger.h"
#include "symbols.h"
#include "disasm.h"

extern ATSimulator g_sim;

namespace {
	enum {
		kModeInvalid,
		kModeImplied,		// implied
		kModeRel,			// beq rel-offset
		kModeRel16,			// brl rel16-offset
		kModeImm,			// lda #$00
		kModeImmMode16,		// lda #$0100
		kModeImmIndex16,	// ldx #$0100
		kModeImm16,			// pea #$2000
		kModeIVec,			// cop 0
		kModeZp,			// lda $00
		kModeZpX,			// lda $00,x
		kModeZpY,			// lda $00,y
		kModeAbs,			// lda $0100
		kModeAbsX,			// lda $0100,x
		kModeAbsY,			// lda $0100,y
		kModeIndA,			// jmp ($0100)
		kModeIndAL,			// jmp [$0100]
		kModeIndX,			// lda ($00,x)
		kModeIndY,			// lda ($00),y
		kModeInd,			// lda ($00)
		kModeIndAX,			// jmp ($0100,x)
		kModeBit,			// smb0 $04
		kModeBBit,			// bbr0 $04,rel-offset
		kModeLong,			// lda $010000
		kModeLongX,			// lda $010000,x
		kModeStack,			// lda 1,s
		kModeStackIndY,		// lda (1,s),y
		kModeDpIndLong,		// lda [$00]
		kModeDpIndLongY,	// lda [$00],y
		kModeMove,			// mvp #$01,#$02
	};

									//	inv	imp	rel	r16	imm	imM	imX	im6	ivc	zp	zpX	zpY	abs	abX	abY	(a)	[a]	(zX	(zY	(z)	(aX	bz	z,r	al	alX	o,S	S)Y	[d]	[dY	#sd
	static const uint8 kBPM_M8_X8[]={	1,	1,	2,	3,	2,	2,	2,	3,	1,	2,	2,	2,	3,	3,	3,	3,	3,	2,	2,	2,	3,	2,	3,	4,	4,	2,	2,	2,	2,	3	};
	static const uint8 kBPM_M8_X16[]={	1,	1,	2,	3,	2,	2,	3,	3,	1,	2,	2,	2,	3,	3,	3,	3,	3,	2,	2,	2,	3,	2,	3,	4,	4,	2,	2,	2,	2,	3	};
	static const uint8 kBPM_M16_X8[]={	1,	1,	2,	3,	2,	3,	2,	3,	1,	2,	2,	2,	3,	3,	3,	3,	3,	2,	2,	2,	3,	2,	3,	4,	4,	2,	2,	2,	2,	3	};
	static const uint8 kBPM_M16_X16[]={	1,	1,	2,	3,	2,	3,	3,	3,	1,	2,	2,	2,	3,	3,	3,	3,	3,	2,	2,	2,	3,	2,	3,	4,	4,	2,	2,	2,	2,	3	};

	static const uint8 *const kBytesPerModeTables[]={
		kBPM_M8_X8,		// 6502
		kBPM_M8_X8,		// 65C02
		kBPM_M8_X8,		// 65C816 emulation
		kBPM_M16_X16,	// 65C816 native M=0 X=0
		kBPM_M16_X8,	// 65C816 native M=0 X=1
		kBPM_M8_X16,	// 65C816 native M=1 X=0
		kBPM_M8_X8,		// 65C816 native M=1 X=1
	};

#define PROCESS_OPCODES			\
		PROCESS_OPCODE(bad, None),	\
		PROCESS_OPCODE(ADC, M),	\
		PROCESS_OPCODE(ANC, M),	\
		PROCESS_OPCODE(AND, M),	\
		PROCESS_OPCODE(ANE, M),	\
		PROCESS_OPCODE(ARR, M),	\
		PROCESS_OPCODE(ASL, M),	\
		PROCESS_OPCODE(ASR, M),	\
		PROCESS_OPCODE(BCC, None),	\
		PROCESS_OPCODE(BCS, None),	\
		PROCESS_OPCODE(BEQ, None),	\
		PROCESS_OPCODE(BIT, M),	\
		PROCESS_OPCODE(BMI, None),	\
		PROCESS_OPCODE(BNE, None),	\
		PROCESS_OPCODE(BPL, None),	\
		PROCESS_OPCODE(BRK, None),	\
		PROCESS_OPCODE(BVC, None),	\
		PROCESS_OPCODE(BVS, None),	\
		PROCESS_OPCODE(CLC, None),	\
		PROCESS_OPCODE(CLD, None),	\
		PROCESS_OPCODE(CLI, None),	\
		PROCESS_OPCODE(CLV, None),	\
		PROCESS_OPCODE(CMP, M),	\
		PROCESS_OPCODE(CPX, X),	\
		PROCESS_OPCODE(CPY, X),	\
		PROCESS_OPCODE(DCP, M),	\
		PROCESS_OPCODE(DEC, M),	\
		PROCESS_OPCODE(DEX, X),	\
		PROCESS_OPCODE(DEY, X),	\
		PROCESS_OPCODE(EOR, M),	\
		PROCESS_OPCODE(INC, M),	\
		PROCESS_OPCODE(INX, X),	\
		PROCESS_OPCODE(INY, X),	\
		PROCESS_OPCODE(ISB, M),	\
		PROCESS_OPCODE(JMP, None),	\
		PROCESS_OPCODE(JSR, None),	\
		PROCESS_OPCODE(KIL, None),	\
		PROCESS_OPCODE(LAS, M),	\
		PROCESS_OPCODE(LAX, M),	\
		PROCESS_OPCODE(LDA, M),	\
		PROCESS_OPCODE(LDX, X),	\
		PROCESS_OPCODE(LDY, X),	\
		PROCESS_OPCODE(LSR, M),	\
		PROCESS_OPCODE(LXA, M),	\
		PROCESS_OPCODE(NOP, M),	\
		PROCESS_OPCODE(ORA, M),	\
		PROCESS_OPCODE(PHA, M),	\
		PROCESS_OPCODE(PHP, 8),	\
		PROCESS_OPCODE(PLA, M),	\
		PROCESS_OPCODE(PLP, 8),	\
		PROCESS_OPCODE(RLA, M),	\
		PROCESS_OPCODE(ROL, M),	\
		PROCESS_OPCODE(ROR, M),	\
		PROCESS_OPCODE(RRA, M),	\
		PROCESS_OPCODE(RTI, None),	\
		PROCESS_OPCODE(RTS, None),	\
		PROCESS_OPCODE(SAX, M),	\
		PROCESS_OPCODE(SBC, M),	\
		PROCESS_OPCODE(SBX, M),	\
		PROCESS_OPCODE(SEC, None),	\
		PROCESS_OPCODE(SED, None),	\
		PROCESS_OPCODE(SEI, None),	\
		PROCESS_OPCODE(SHA, M),	\
		PROCESS_OPCODE(SHS, M),	\
		PROCESS_OPCODE(SHX, X),	\
		PROCESS_OPCODE(SHY, X),	\
		PROCESS_OPCODE(SLO, M),	\
		PROCESS_OPCODE(SRE, M),	\
		PROCESS_OPCODE(STA, M),	\
		PROCESS_OPCODE(STX, X),	\
		PROCESS_OPCODE(STY, X),	\
		PROCESS_OPCODE(TAX, None),	\
		PROCESS_OPCODE(TAY, None),	\
		PROCESS_OPCODE(TSX, None),	\
		PROCESS_OPCODE(TXA, None),	\
		PROCESS_OPCODE(TXS, None),	\
		PROCESS_OPCODE(TYA, None),	\
								\
		/* 65C02 */				\
		PROCESS_OPCODE(BRA, None),	\
		PROCESS_OPCODE(TSB, M),	\
		PROCESS_OPCODE(RMB, M),	\
		PROCESS_OPCODE(BBR, None),	\
		PROCESS_OPCODE(TRB, M),	\
		PROCESS_OPCODE(STZ, M),	\
		PROCESS_OPCODE(SMB, M),	\
		PROCESS_OPCODE(BBS, None),	\
		PROCESS_OPCODE(WAI, None),	\
		PROCESS_OPCODE(STP, None),	\
		PROCESS_OPCODE(PLX, None),	\
		PROCESS_OPCODE(PLY, None),	\
		PROCESS_OPCODE(PHX, None),	\
		PROCESS_OPCODE(PHY, None),	\
								\
		/* 65C816 */			\
		PROCESS_OPCODE(BRL, None),	\
		PROCESS_OPCODE(COP, None),	\
		PROCESS_OPCODE(JML, None),	\
		PROCESS_OPCODE(JSL, None),	\
		PROCESS_OPCODE(MVN, None),	\
		PROCESS_OPCODE(MVP, None),	\
		PROCESS_OPCODE(PEA, None),	\
		PROCESS_OPCODE(PEI, 16),	\
		PROCESS_OPCODE(PER, None),	\
		PROCESS_OPCODE(PHB, 8),	\
		PROCESS_OPCODE(PHD, 16),	\
		PROCESS_OPCODE(PHK, 8),	\
		PROCESS_OPCODE(PLB, 8),	\
		PROCESS_OPCODE(PLD, 16),	\
		PROCESS_OPCODE(REP, None),	\
		PROCESS_OPCODE(RTL, None),	\
		PROCESS_OPCODE(SEP, None),	\
		PROCESS_OPCODE(TCD, None),	\
		PROCESS_OPCODE(TCS, None),	\
		PROCESS_OPCODE(TDC, None),	\
		PROCESS_OPCODE(TSC, None),	\
		PROCESS_OPCODE(TXY, None),	\
		PROCESS_OPCODE(TYX, None),	\
		PROCESS_OPCODE(XBA, None),	\
		PROCESS_OPCODE(XCE, None),	\
		PROCESS_OPCODE(WDM, None),	\

	enum {
#define PROCESS_OPCODE(name, mode) kOpcode##name
		PROCESS_OPCODES
#undef PROCESS_OPCODE
	};

	static const char *const kOpcodes[]={
#define PROCESS_OPCODE(name, mode) #name
		PROCESS_OPCODES
#undef PROCESS_OPCODE
	};

	enum MemoryAccessMode {
		kMemoryAccessMode_None,
		kMemoryAccessMode_8,
		kMemoryAccessMode_16,
		kMemoryAccessMode_M,
		kMemoryAccessMode_X
	};

	static const uint8 kOpcodeMemoryAccessModes[]={
#define PROCESS_OPCODE(name, mode) kMemoryAccessMode_##mode
		PROCESS_OPCODES
#undef PROCESS_OPCODE
	};

#undef PROCESS_OPCODES

	#define xx(op) { kModeInvalid, 0 }
	#define Ip(op) { kModeImplied, kOpcode##op }
	#define Re(op) { kModeRel, kOpcode##op }
	#define Rl(op) { kModeRel16, kOpcode##op }
	#define Im(op) { kModeImm, kOpcode##op }
	#define ImM(op) { kModeImmMode16, kOpcode##op }
	#define ImX(op) { kModeImmIndex16, kOpcode##op }
	#define I2(op) { kModeImm16, kOpcode##op }
	#define Iv(op) { kModeIVec, kOpcode##op }
	#define Zp(op) { kModeZp, kOpcode##op }
	#define Zx(op) { kModeZpX, kOpcode##op }
	#define Zy(op) { kModeZpY, kOpcode##op }
	#define Ab(op) { kModeAbs, kOpcode##op }
	#define Ax(op) { kModeAbsX, kOpcode##op }
	#define Ay(op) { kModeAbsY, kOpcode##op }
	#define Ia(op) { kModeIndA, kOpcode##op }
	#define Il(op) { kModeIndAL, kOpcode##op }
	#define Ix(op) { kModeIndX, kOpcode##op }
	#define Iy(op) { kModeIndY, kOpcode##op }
	#define Iz(op) { kModeInd, kOpcode##op }
	#define It(op) { kModeIndAX, kOpcode##op }
	#define Bz(op) { kModeBit, kOpcode##op }
	#define Bb(op) { kModeBBit, kOpcode##op }

	#define Lg(op) { kModeLong, kOpcode##op }
	#define Lx(op) { kModeLongX, kOpcode##op }
	#define Sr(op) { kModeStack, kOpcode##op }
	#define Sy(op) { kModeStackIndY, kOpcode##op }
	#define Xd(op) { kModeDpIndLong, kOpcode##op }
	#define Xy(op) { kModeDpIndLongY, kOpcode##op }
	#define Mv(op) { kModeMove, kOpcode##op }

	const uint8 kModeTbl_6502[256][2]={
		//			   0,       1,       2,       3,       4,       5,       6,       7,       8,       9,       A,       B,       C,       D,       E,       F
		/* 00 */	Ip(BRK), Ix(ORA), Ip(KIL), Ix(SLO), Zp(NOP), Zp(ORA), Zp(ASL), Zp(SLO), Ip(PHP), Im(ORA), Ip(ASL), Im(ANC), Ab(NOP), Ab(ORA), Ab(ASL), Ab(SLO), 
		/* 10 */	Re(BPL), Iy(ORA), Ip(KIL), Iy(SLO), Zx(NOP), Zx(ORA), Zx(ASL), Zx(SLO), Ip(CLC), Ay(ORA), Ip(NOP), Ay(SLO), Ax(NOP), Ax(ORA), Ax(ASL), Ax(SLO), 
		/* 20 */	Ab(JSR), Ix(AND), Ip(KIL), Ix(RLA), Zp(BIT), Zp(AND), Zp(ROL), Zp(RLA), Ip(PLP), Im(AND), Ip(ROL), Im(ANC), Ab(BIT), Ab(AND), Ab(ROL), Ab(RLA), 
		/* 30 */	Re(BMI), Iy(AND), Ip(KIL), Iy(RLA), Zx(NOP), Zx(AND), Zx(ROL), Zx(RLA), Ip(SEC), Ay(AND), Ip(NOP), Ay(RLA), Ax(NOP), Ax(AND), Ax(ROL), Ax(RLA), 
		/* 40 */	Ip(RTI), Ix(EOR), Ip(KIL), Ix(SRE), Zp(NOP), Zp(EOR), Zp(LSR), Zp(SRE), Ip(PHA), Im(EOR), Ip(LSR), Im(ASR), Ab(JMP), Ab(EOR), Ab(LSR), Ab(SRE), 
		/* 50 */	Re(BVC), Iy(EOR), Ip(KIL), Iy(SRE), Zx(NOP), Zx(EOR), Zx(LSR), Zx(SRE), Ip(CLI), Ay(EOR), Ip(NOP), Ay(SRE), Ax(NOP), Ax(EOR), Ax(LSR), Ax(SRE), 
		/* 60 */	Ip(RTS), Ix(ADC), Ip(KIL), Ix(RRA), Zp(NOP), Zp(ADC), Zp(ROR), Zp(RRA), Ip(PLA), Im(ADC), Ip(ROR), Im(ARR), Ia(JMP), Ab(ADC), Ab(ROR), Ab(RRA), 
		/* 70 */	Re(BVS), Iy(ADC), Ip(KIL), Iy(RRA), Zx(NOP), Zx(ADC), Zx(ROR), Zx(RRA), Ip(SEI), Ay(ADC), Ip(NOP), Ay(RRA), Ax(NOP), Ax(ADC), Ax(ROR), Ax(RRA), 
		/* 80 */	Im(NOP), Ix(STA), Im(NOP), Ix(SAX), Zp(STY), Zp(STA), Zp(STX), Zp(SAX), Ip(DEY), Im(NOP), Ip(TXA), Im(ANE), Ab(STY), Ab(STA), Ab(STX), Ab(SAX), 
		/* 90 */	Re(BCC), Iy(STA), Ip(KIL), Iy(SHA), Zx(STY), Zx(STA), Zy(STX), Zy(SAX), Ip(TYA), Ay(STA), Ip(TXS), Ay(SHS), Ax(SHY), Ax(STA), Ay(SHX), Ay(SHA), 
		/* A0 */	Im(LDY), Ix(LDA), Im(LDX), Ix(LAX), Zp(LDY), Zp(LDA), Zp(LDX), Zp(LAX), Ip(TAY), Im(LDA), Ip(TAX), Im(LXA), Ab(LDY), Ab(LDA), Ab(LDX), Ab(LAX), 
		/* B0 */	Re(BCS), Iy(LDA), Ip(KIL), Iy(LAX), Zx(LDY), Zx(LDA), Zy(LDX), Zy(LAX), Ip(CLV), Ay(LDA), Ip(TSX), Ab(LAS), Ax(LDY), Ax(LDA), Ay(LDX), Ay(LAX), 
		/* C0 */	Im(CPY), Ix(CMP), Im(NOP), Ix(DCP), Zp(CPY), Zp(CMP), Zp(DEC), Zp(DCP), Ip(INY), Im(CMP), Ip(DEX), Im(SBX), Ab(CPY), Ab(CMP), Ab(DEC), Ab(DCP), 
		/* D0 */	Re(BNE), Iy(CMP), Ip(KIL), Iy(DCP), Zx(NOP), Zx(CMP), Zx(DEC), Zx(DCP), Ip(CLD), Ay(CMP), Ip(NOP), Ay(DCP), Ax(NOP), Ax(CMP), Ax(DEC), Ax(DCP), 
		/* E0 */	Im(CPX), Ix(SBC), Im(NOP), Ix(ISB), Zp(CPX), Zp(SBC), Zp(INC), Zp(ISB), Ip(INX), Im(SBC), Ip(NOP), Im(SBC), Ab(CPX), Ab(SBC), Ab(INC), Ab(ISB), 
		/* F0 */	Re(BEQ), Iy(SBC), Ip(KIL), Iy(ISB), Zx(NOP), Zx(SBC), Zx(INC), Zx(ISB), Ip(SED), Ay(SBC), Ip(NOP), Ay(ISB), Ax(NOP), Ax(SBC), Ax(INC), Ax(ISB),
	};

	const uint8 kModeTbl_65C02[256][2]={
		//			   0,       1,       2,       3,       4,       5,       6,       7,       8,       9,       A,       B,       C,       D,       E,       F
		/* 00 */	Ip(BRK), Ix(ORA), xx(bad), xx(bad), Zp(TSB), Zp(ORA), Zp(ASL), Bz(RMB), Ip(PHP), Im(ORA), Ip(ASL), xx(bad), Ab(TSB), Ab(ORA), Ab(ASL), Bb(BBR), 
		/* 10 */	Re(BPL), Iy(ORA), Iz(ORA), xx(bad), Zp(TRB), Zx(ORA), Zx(ASL), Bz(RMB), Ip(CLC), Ay(ORA), Ip(INC), xx(bad), Ab(TRB), Ax(ORA), Ax(ASL), Bb(BBR), 
		/* 20 */	Ab(JSR), Ix(AND), xx(bad), xx(bad), Zp(BIT), Zp(AND), Zp(ROL), Bz(RMB), Ip(PLP), Im(AND), Ip(ROL), xx(bad), Ab(BIT), Ab(AND), Ab(ROL), Bb(BBR), 
		/* 30 */	Re(BMI), Iy(AND), Iz(AND), xx(bad), Zx(BIT), Zx(AND), Zx(ROL), Bz(RMB), Ip(SEC), Ay(AND), Ip(DEC), xx(bad), Ax(BIT), Ax(AND), Ax(ROL), Bb(BBR), 
		/* 40 */	Ip(RTI), Ix(EOR), xx(bad), xx(bad), Zp(NOP), Zp(EOR), Zp(LSR), Bz(RMB), Ip(PHA), Im(EOR), Ip(LSR), xx(bad), Ab(JMP), Ab(EOR), Ab(LSR), Bb(BBR), 
		/* 50 */	Re(BVC), Iy(EOR), Iz(EOR), xx(bad), Zx(NOP), Zx(EOR), Zx(LSR), Bz(RMB), Ip(CLI), Ay(EOR), Ip(PHY), xx(bad), Ax(NOP), Ax(EOR), Ax(LSR), Bb(BBR), 
		/* 60 */	Ip(RTS), Ix(ADC), xx(bad), xx(bad), Zp(STZ), Zp(ADC), Zp(ROR), Bz(RMB), Ip(PLA), Im(ADC), Ip(ROR), xx(bad), Ia(JMP), Ab(ADC), Ab(ROR), Bb(BBR), 
		/* 70 */	Re(BVS), Iy(ADC), Iz(ADC), xx(bad), Zx(STZ), Zx(ADC), Zx(ROR), Bz(RMB), Ip(SEI), Ay(ADC), Ip(PLY), xx(bad), It(JMP), Ax(ADC), Ax(ROR), Bb(BBR), 
		/* 80 */	Re(BRA), Ix(STA), xx(bad), xx(bad), Zp(STY), Zp(STA), Zp(STX), Bz(SMB), Ip(DEY), Im(BIT), Ip(TXA), xx(bad), Ab(STY), Ab(STA), Ab(STX), Bb(BBS), 
		/* 90 */	Re(BCC), Iy(STA), Iz(STA), xx(bad), Zx(STY), Zx(STA), Zy(STX), Bz(SMB), Ip(TYA), Ay(STA), Ip(TXS), xx(bad), Ab(STZ), Ax(STA), Ax(STZ), Bb(BBS), 
		/* A0 */	Im(LDY), Ix(LDA), Im(LDX), xx(bad), Zp(LDY), Zp(LDA), Zp(LDX), Bz(SMB), Ip(TAY), Im(LDA), Ip(TAX), xx(bad), Ab(LDY), Ab(LDA), Ab(LDX), Bb(BBS), 
		/* B0 */	Re(BCS), Iy(LDA), Iz(LDA), xx(bad), Zx(LDY), Zx(LDA), Zy(LDX), Bz(SMB), Ip(CLV), Ay(LDA), Ip(TSX), xx(bad), Ax(LDY), Ax(LDA), Ay(LDX), Bb(BBS), 
		/* C0 */	Im(CPY), Ix(CMP), xx(bad), xx(bad), Zp(CPY), Zp(CMP), Zp(DEC), Bz(SMB), Ip(INY), Im(CMP), Ip(DEX), Ip(WAI), Ab(CPY), Ab(CMP), Ab(DEC), Bb(BBS), 
		/* D0 */	Re(BNE), Iy(CMP), Iz(CMP), xx(bad), xx(bad), Zx(CMP), Zx(DEC), Bz(SMB), Ip(CLD), Ay(CMP), Ip(PHX), Ip(STP), Ax(NOP), Ax(CMP), Ax(DEC), Bb(BBS), 
		/* E0 */	Im(CPX), Ix(SBC), xx(bad), xx(bad), Zp(CPX), Zp(SBC), Zp(INC), Bz(SMB), Ip(INX), Im(SBC), Ip(NOP), xx(bad), Ab(CPX), Ab(SBC), Ab(INC), Bb(BBS), 
		/* F0 */	Re(BEQ), Iy(SBC), Iz(SBC), xx(bad), Zx(NOP), Zx(SBC), Zx(INC), Bz(SMB), Ip(SED), Ay(SBC), Ip(PLX), xx(bad), Ax(NOP), Ax(SBC), Ax(INC), Bb(BBS),
	};

	const uint8 kModeTbl_65C816[256][2]={
		//			   0,       1,       2,       3,       4,       5,       6,       7,       8,       9,       A,       B,       C,       D,       E,       F
		/* 00 */	Ip(BRK), Ix(ORA), Iv(COP), Sr(ORA), Zp(TSB), Zp(ORA), Zp(ASL), Xd(ORA), Ip(PHP),ImM(ORA), Ip(ASL), Ip(PHD), Ab(TSB), Ab(ORA), Ab(ASL), Lg(ORA), 
		/* 10 */	Re(BPL), Iy(ORA), Iz(ORA), Sy(ORA), Zp(TRB), Zx(ORA), Zx(ASL), Xy(ORA), Ip(CLC), Ay(ORA), Ip(INC), Ip(TCS), Ab(TRB), Ax(ORA), Ax(ASL), Lx(ORA), 
		/* 20 */	Ab(JSR), Ix(AND), Lg(JSL), Sr(AND), Zp(BIT), Zp(AND), Zp(ROL), Xd(AND), Ip(PLP),ImM(AND), Ip(ROL), Ip(PLD), Ab(BIT), Ab(AND), Ab(ROL), Lg(AND), 
		/* 30 */	Re(BMI), Iy(AND), Iz(AND), Sy(AND), Zx(BIT), Zx(AND), Zx(ROL), Xy(AND), Ip(SEC), Ay(AND), Ip(DEC), Ip(TSC), Ax(BIT), Ax(AND), Ax(ROL), Lx(AND), 
		/* 40 */	Ip(RTI), Ix(EOR), Ip(WDM), Sr(EOR), Mv(MVP), Zp(EOR), Zp(LSR), Xd(EOR), Ip(PHA),ImM(EOR), Ip(LSR), Ip(PHK), Ab(JMP), Ab(EOR), Ab(LSR), Lg(EOR), 
		/* 50 */	Re(BVC), Iy(EOR), Iz(EOR), Sy(EOR), Mv(MVN), Zx(EOR), Zx(LSR), Xy(EOR), Ip(CLI), Ay(EOR), Ip(PHY), Ip(TCD), Lg(JMP), Ax(EOR), Ax(LSR), Lx(EOR), 
		/* 60 */	Ip(RTS), Ix(ADC), Rl(PER), Sr(ADC), Zp(STZ), Zp(ADC), Zp(ROR), Xd(ADC), Ip(PLA),ImM(ADC), Ip(ROR), Ip(RTL), Ia(JMP), Ab(ADC), Ab(ROR), Lg(ADC), 
		/* 70 */	Re(BVS), Iy(ADC), Iz(ADC), Sy(ADC), Zx(STZ), Zx(ADC), Zx(ROR), Xy(ADC), Ip(SEI), Ay(ADC), Ip(PLY), Ip(TDC), It(JMP), Ax(ADC), Ax(ROR), Lx(ADC), 
		/* 80 */	Re(BRA), Ix(STA), Rl(BRL), Sr(STA), Zp(STY), Zp(STA), Zp(STX), Xd(STA), Ip(DEY),ImM(BIT), Ip(TXA), Ip(PHB), Ab(STY), Ab(STA), Ab(STX), Lg(STA), 
		/* 90 */	Re(BCC), Iy(STA), Iz(STA), Sy(STA), Zx(STY), Zx(STA), Zy(STX), Xy(STA), Ip(TYA), Ay(STA), Ip(TXS), Ip(TXY), Ab(STZ), Ax(STA), Ax(STZ), Lx(STA), 
		/* A0 */   ImX(LDY), Ix(LDA),ImX(LDX), Sr(LDA), Zp(LDY), Zp(LDA), Zp(LDX), Xd(LDA), Ip(TAY),ImM(LDA), Ip(TAX), Ip(PLB), Ab(LDY), Ab(LDA), Ab(LDX), Lg(LDA), 
		/* B0 */	Re(BCS), Iy(LDA), Iz(LDA), Sy(LDA), Zx(LDY), Zx(LDA), Zy(LDX), Xy(LDA), Ip(CLV), Ay(LDA), Ip(TSX), Ip(TYX), Ax(LDY), Ax(LDA), Ay(LDX), Lx(LDA), 
		/* C0 */   ImX(CPY), Ix(CMP), Im(REP), Sr(CMP), Zp(CPY), Zp(CMP), Zp(DEC), Xd(CMP), Ip(INY),ImM(CMP), Ip(DEX), Ip(WAI), Ab(CPY), Ab(CMP), Ab(DEC), Lg(CMP), 
		/* D0 */	Re(BNE), Iy(CMP), Iz(CMP), Sy(CMP), Iz(PEI), Zx(CMP), Zx(DEC), Xy(CMP), Ip(CLD), Ay(CMP), Ip(PHX), Ip(STP), Il(JML), Ax(CMP), Ax(DEC), Lx(CMP), 
		/* E0 */   ImX(CPX), Ix(SBC), Im(SEP), Sr(SBC), Zp(CPX), Zp(SBC), Zp(INC), Xd(SBC), Ip(INX),ImM(SBC), Ip(NOP), Ip(XBA), Ab(CPX), Ab(SBC), Ab(INC), Lg(SBC), 
		/* F0 */	Re(BEQ), Iy(SBC), Iz(SBC), Sy(SBC), Ab(PEA), Zx(SBC), Zx(INC), Xy(SBC), Ip(SED), Ay(SBC), Ip(PLX), Ip(XCE), It(JSR), Ax(SBC), Ax(INC), Lx(SBC),
	};

	#undef xx
	#undef Ip
	#undef Re
	#undef Rl
	#undef Im
	#undef ImM
	#undef ImX
	#undef I2
	#undef Iv
	#undef Zp
	#undef Zx
	#undef Zy
	#undef Ab
	#undef Ax
	#undef Ay
	#undef Ia
	#undef Il
	#undef Ix
	#undef Iy
	#undef Iz
	#undef It
	#undef Bz
	#undef Bb

	#undef Lg
	#undef Lx
	#undef Sr
	#undef Sy
	#undef Xd
	#undef Xy
	#undef Mv

	const uint8 (*kModeTbl[8])[2]={
		kModeTbl_6502,
		kModeTbl_65C02,
		kModeTbl_65C816,
		kModeTbl_65C816,
		kModeTbl_65C816,
		kModeTbl_65C816,
		kModeTbl_65C816,
	};

	enum ATZ80TokenMode : uint8 {
		kATZ80TokenMode_None,
		kATZ80TokenMode_PCRel8,
		kATZ80TokenMode_PCAbs16,
		kATZ80TokenMode_Abs,
		kATZ80TokenMode_Imm8,
		kATZ80TokenMode_Imm16,
		kATZ80TokenMode_IOPort,
		kATZ80TokenMode_Index,
		kATZ80TokenMode_Indirect,
	};

	const uint8 kZ80TokenModeBytes[]={
		0, 1, 2, 2, 1, 2, 1, 0, 1
	};

	static_assert(vdcountof(kZ80TokenModeBytes) == kATZ80TokenMode_Indirect + 1, "token mode byte table missized");

	constexpr ATZ80TokenMode ParseInsnToken(const char *s) {
		return *s != '$' ? kATZ80TokenMode_None
			: s[1] == 'r' ? kATZ80TokenMode_PCRel8
			: s[1] == 'p' ? kATZ80TokenMode_PCAbs16
			: s[1] == 'a' ? kATZ80TokenMode_Abs
			: s[1] == 'i' ? kATZ80TokenMode_Imm8
			: s[1] == 'I' ? kATZ80TokenMode_Imm16
			: s[1] == 't' ? kATZ80TokenMode_IOPort
			: s[1] == 'x' ? kATZ80TokenMode_Index
			: s[1] == 'X' ? kATZ80TokenMode_Indirect
			: throw;
	}

	constexpr int FindNextInsnToken(const char *s) {
		return *s == '$' || !*s
			? 0
			: FindNextInsnToken(s+1) + 1;
	}

	struct Z80Insn {
		const char *s = nullptr;
		uint8 prefixLen = 0;
		ATZ80TokenMode token1 = kATZ80TokenMode_None;
		uint8 midLen = 0;
		ATZ80TokenMode token2 = kATZ80TokenMode_None;

		constexpr Z80Insn() = default;

		constexpr Z80Insn(const char *s_, uint8 prefixLen_, ATZ80TokenMode token1_, uint8 midLen_)
			: s(s_)
			, prefixLen(prefixLen_)
			, token1(token1_)
			, midLen(midLen_)
			, token2(ParseInsnToken(s_ + prefixLen_ + (token1 ? 2 : 0) + midLen_))
		{
		}

		constexpr Z80Insn(const char *s, uint8 prefixLen, ATZ80TokenMode token1)
			: Z80Insn(s, prefixLen, token1, (uint8)FindNextInsnToken(s + prefixLen + (token1 ? 2 : 0)))
		{
		}

		constexpr Z80Insn(const char *s, uint8 prefixLen)
			: Z80Insn(s, prefixLen, ParseInsnToken(s + prefixLen))
		{
		}

		constexpr Z80Insn(const char *s) : Z80Insn(s, (uint8)FindNextInsnToken(s)) {}

		uint32 GetOpcodeLen() const {
			return s ? kZ80TokenModeBytes[token1] + kZ80TokenModeBytes[token2] + 1 : 0;
		}
	};

	constexpr Z80Insn kZ80Insns[] = {
/*00*/	"nop",			"ld bc,$I",		"ld (bc),a",	"inc bc",		"inc b",		"dec b",		"ld b,$i",		"rlca",
/*08*/	"ex af,af'",	"add hl,bc",	"ld a,(bc)",	"dec bc",		"inc c",		"dec c",		"ld c,$i",		"rrca",
/*10*/	"djnz $r",		"ld de,$I",		"ld (de),a",	"inc de",		"inc d",		"dec d",		"ld d,$i",		"rla",
/*18*/	"jr $r",		"add hl,de",	"ld a,(de)",	"dec de",		"inc e",		"dec e",		"ld e,$i",		"rra",
/*20*/	"jr nz,$r",		"ld hl,$I",		"ld ($a),hl",	"inc hl",		"inc h",		"dec h",		"ld h,$i",		"daa",
/*28*/	"jr z,$r",		"add hl,hl",	"ld hl,($a)",	"dec hl",		"inc l",		"dec l",		"ld l,$i",		"cpl",
/*30*/	"jr nc,$r",		"ld sp,$I",		"ld ($a),a",	"inc sp",		"inc (hl)",		"dec (hl)",		"ld (hl),$i",	"scf",
/*38*/	"jr c,$r",		"add hl,sp",	"ld a,($a)",	"dec sp",		"inc a",		"dec a",		"ld a,$i",		"ccf",
/*40*/	"ld b,b",		"ld b,c",		"ld b,d",		"ld b,e",		"ld b,h",		"ld b,l",		"ld b,(hl)",	"ld b,a",
/*48*/	"ld c,b",		"ld c,c",		"ld c,d",		"ld c,e",		"ld c,h",		"ld c,l",		"ld c,(hl)",	"ld c,a",
/*50*/	"ld d,b",		"ld d,c",		"ld d,d",		"ld d,e",		"ld d,h",		"ld d,l",		"ld d,(hl)",	"ld d,a",
/*58*/	"ld e,b",		"ld e,c",		"ld e,d",		"ld e,e",		"ld e,h",		"ld e,l",		"ld e,(hl)",	"ld e,a",
/*60*/	"ld h,b",		"ld h,c",		"ld h,d",		"ld h,e",		"ld h,h",		"ld h,l",		"ld h,(hl)",	"ld h,a",
/*68*/	"ld l,b",		"ld l,c",		"ld l,d",		"ld l,e",		"ld l,h",		"ld l,l",		"ld l,(hl)",	"ld l,a",
/*70*/	"ld (hl),b",	"ld (hl),c",	"ld (hl),d",	"ld (hl),e",	"ld (hl),h",	"ld (hl),l",	"halt",			"ld (hl),a",
/*78*/	"ld a,b",		"ld a,c",		"ld a,d",		"ld a,e",		"ld a,h",		"ld a,l",		"ld a,(hl)",	"ld a,a",
/*80*/	"add a,b",		"add a,c",		"add a,d",		"add a,e",		"add a,h",		"add a,l",		"add a,(hl)",	"add a,a",
/*88*/	"adc a,b",		"adc a,c",		"adc a,d",		"adc a,e",		"adc a,h",		"adc a,l",		"adc a,(hl)",	"adc a,a",
/*90*/	"sub a,b",		"sub a,c",		"sub a,d",		"sub a,e",		"sub a,h",		"sub a,l",		"sub a,(hl)",	"sub a,a",
/*98*/	"sbc a,b",		"sbc a,c",		"sbc a,d",		"sbc a,e",		"sbc a,h",		"sbc a,l",		"sbc a,(hl)",	"sbc a,a",
/*A0*/	"and a,b",		"and a,c",		"and a,d",		"and a,e",		"and a,h",		"and a,l",		"and a,(hl)",	"and a,a",
/*A8*/	"xor a,b",		"xor a,c",		"xor a,d",		"xor a,e",		"xor a,h",		"xor a,l",		"xor a,(hl)",	"xor a,a",
/*B0*/	"or a,b",		"or a,c",		"or a,d",		"or a,e",		"or a,h",		"or a,l",		"or a,(hl)",	"or a,a",
/*B8*/	"cp a,b",		"cp a,c",		"cp a,d",		"cp a,e",		"cp a,h",		"cp a,l",		"cp a,(hl)",	"cp a,a",
/*C0*/	"ret nz",		"pop bc",		"jp nz,$p",		"jp $p",		"call nz,$p",	"push bc",		"add a,$i",		"rst 00h",
/*C8*/	"ret z",		"ret",			"jp z,$p",		{},				"call z,$p",	"call $p",		"adc a,$i",		"rst 08h",
/*D0*/	"ret nc",		"pop de",		"jp nc,$p",		"out ($t),a",	"call nc,$p",	"push de",		"sub $i",		"rst 10h",
/*D8*/	"ret c",		"exx",			"jp c,$p",		"in a,($t)",	"call c,$p",	{},				"sbc a,$i",		"rst 18h",
/*E0*/	"ret po",		"pop hl",		"jp po,$p",		"ex (sp),hl",	"call po,$p",	"push hl",		"and $i",		"rst 20h",
/*E8*/	"ret pe",		"jp (hl)",		"jp pe,$p",		"ex de,hl",		"call pe,$p",	{},				"xor $i",		"rst 28h",
/*F0*/	"ret p",		"pop af",		"jp p,$p",		"di",			"call p,$p",	"push af",		"or $i",		"rst 30h",
/*F8*/	"ret m",		"ld sp,hl",		"jp m,$p",		"ei",			"call m,$p",	{},				"cp $i",		"rst 38h",
	};

	constexpr Z80Insn kZ80InsnsED[] = {
/*00*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*10*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*20*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*30*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},

/*40*/	"in b,(c)",		"out (c),b",	"sbc hl,bc",	"ld ($a),bc",	"neg",			"retn",			"im 0",			"ld i,a",
/*48*/	"in c,(c)",		"out (c),c",	"adc hl,bc",	"ld bc,($a)",	"neg",			"reti",			{},				"ld r,a",
/*50*/	"in d,(c)",		"out (c),d",	"sbc hl,de",	"ld ($a),de",	"neg",			"retn",			"im 1",			"ld a,i",
/*58*/	"in e,(c)",		"out (c),e",	"adc hl,de",	"ld de,($a)",	"neg",			"retn",			"im 2",			"ld a,r",
/*60*/	"in h,(c)",		"out (c),h",	"sbc hl,hl",	"ld ($a),hl",	"neg",			"retn",			"im 0",			"rrd",
/*68*/	"in l,(c)",		"out (c),l",	"adc hl,hl",	"ld hl,($a)",	"neg",			"retn",			{},				"rld",
/*70*/	"in (c)",		"out (c),0",	"sbc hl,sp",	"ld ($a),sp",	"neg",			"retn",			"im 1",			{},
/*78*/	"in a,(c)",		"out (c),a",	"adc hl,sp",	"ld sp,($a)",	"neg",			"retn",			"im 2",			{},

/*80*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*90*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*A0*/	"ldi",			"cpi",			"ini",			"outi",			{},				{},				{},				{},
/*A8*/	"ldd",			"cpd",			"ind",			"outd",			{},				{},				{},				{},
/*B0*/	"ldir",			"cpir",			"inir",			"otir",			{},				{},				{},				{},
/*B8*/	"lddr",			"cpdr",			"indr",			"otdr",			{},				{},				{},				{},

/*C0*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*D0*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*E0*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
/*F0*/	{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},{},
	};

	constexpr Z80Insn kZ80InsnsCB[] = {
/*00*/	"rlc b",		"rlc c",		"rlc d",		"rlc e",		"rlc h",		"rlc l",		"rlc (hl)",		"rlc a",
/*08*/	"rrc b",		"rrc c",		"rrc d",		"rrc e",		"rrc h",		"rrc l",		"rrc (hl)",		"rrc a",
/*10*/	"rl b",			"rl c",			"rl d",			"rl e",			"rl h",			"rl l",			"rl (hl)",		"rl a",
/*18*/	"rr b",			"rr c",			"rr d",			"rr e",			"rr h",			"rr l",			"rr (hl)",		"rr a",
/*20*/	"sla b",		"sla c",		"sla d",		"sla e",		"sla h",		"sla l",		"sla (hl)",		"sla a",
/*28*/	"sra b",		"sra c",		"sra d",		"sra e",		"sra h",		"sra l",		"sra (hl)",		"sra a",
/*30*/	"sll b",		"sll c",		"sll d",		"sll e",		"sll h",		"sll l",		"sll (hl)",		"sll a",
/*38*/	"srl b",		"srl c",		"srl d",		"srl e",		"srl h",		"srl l",		"srl (hl)",		"srl a",
/*40*/	"bit 0,b",		"bit 0,c",		"bit 0,d",		"bit 0,e",		"bit 0,h",		"bit 0,l",		"bit 0,(hl)",	"bit 0,a",
/*48*/	"bit 1,b",		"bit 1,c",		"bit 1,d",		"bit 1,e",		"bit 1,h",		"bit 1,l",		"bit 1,(hl)",	"bit 1,a",
/*50*/	"bit 2,b",		"bit 2,c",		"bit 2,d",		"bit 2,e",		"bit 2,h",		"bit 2,l",		"bit 2,(hl)",	"bit 2,a",
/*58*/	"bit 3,b",		"bit 3,c",		"bit 3,d",		"bit 3,e",		"bit 3,h",		"bit 3,l",		"bit 3,(hl)",	"bit 3,a",
/*60*/	"bit 4,b",		"bit 4,c",		"bit 4,d",		"bit 4,e",		"bit 4,h",		"bit 4,l",		"bit 4,(hl)",	"bit 4,a",
/*68*/	"bit 5,b",		"bit 5,c",		"bit 5,d",		"bit 5,e",		"bit 5,h",		"bit 5,l",		"bit 5,(hl)",	"bit 5,a",
/*70*/	"bit 6,b",		"bit 6,c",		"bit 6,d",		"bit 6,e",		"bit 6,h",		"bit 6,l",		"bit 6,(hl)",	"bit 6,a",
/*78*/	"bit 7,b",		"bit 7,c",		"bit 7,d",		"bit 7,e",		"bit 7,h",		"bit 7,l",		"bit 7,(hl)",	"bit 7,a",
/*80*/	"res 0,b",		"res 0,c",		"res 0,d",		"res 0,e",		"res 0,h",		"res 0,l",		"res 0,(hl)",	"res 0,a",
/*88*/	"res 1,b",		"res 1,c",		"res 1,d",		"res 1,e",		"res 1,h",		"res 1,l",		"res 1,(hl)",	"res 1,a",
/*90*/	"res 2,b",		"res 2,c",		"res 2,d",		"res 2,e",		"res 2,h",		"res 2,l",		"res 2,(hl)",	"res 2,a",
/*98*/	"res 3,b",		"res 3,c",		"res 3,d",		"res 3,e",		"res 3,h",		"res 3,l",		"res 3,(hl)",	"res 3,a",
/*A0*/	"res 4,b",		"res 4,c",		"res 4,d",		"res 4,e",		"res 4,h",		"res 4,l",		"res 4,(hl)",	"res 4,a",
/*A8*/	"res 5,b",		"res 5,c",		"res 5,d",		"res 5,e",		"res 5,h",		"res 5,l",		"res 5,(hl)",	"res 5,a",
/*B0*/	"res 6,b",		"res 6,c",		"res 6,d",		"res 6,e",		"res 6,h",		"res 6,l",		"res 6,(hl)",	"res 6,a",
/*B8*/	"res 7,b",		"res 7,c",		"res 7,d",		"res 7,e",		"res 7,h",		"res 7,l",		"res 7,(hl)",	"res 7,a",
/*C0*/	"set 0,b",		"set 0,c",		"set 0,d",		"set 0,e",		"set 0,h",		"set 0,l",		"set 0,(hl)",	"set 0,a",
/*C8*/	"set 1,b",		"set 1,c",		"set 1,d",		"set 1,e",		"set 1,h",		"set 1,l",		"set 1,(hl)",	"set 1,a",
/*D0*/	"set 2,b",		"set 2,c",		"set 2,d",		"set 2,e",		"set 2,h",		"set 2,l",		"set 2,(hl)",	"set 2,a",
/*D8*/	"set 3,b",		"set 3,c",		"set 3,d",		"set 3,e",		"set 3,h",		"set 3,l",		"set 3,(hl)",	"set 3,a",
/*E0*/	"set 4,b",		"set 4,c",		"set 4,d",		"set 4,e",		"set 4,h",		"set 4,l",		"set 4,(hl)",	"set 4,a",
/*E8*/	"set 5,b",		"set 5,c",		"set 5,d",		"set 5,e",		"set 5,h",		"set 5,l",		"set 5,(hl)",	"set 5,a",
/*F0*/	"set 6,b",		"set 6,c",		"set 6,d",		"set 6,e",		"set 6,h",		"set 6,l",		"set 6,(hl)",	"set 6,a",
/*F8*/	"set 7,b",		"set 7,c",		"set 7,d",		"set 7,e",		"set 7,h",		"set 7,l",		"set 7,(hl)",	"set 7,a",
	};

	constexpr Z80Insn kZ80InsnsDDFD[] = {
/*00*/	{},				{},				{},				{},				{},				{},				{},				{},
/*08*/	{},				"add $x,bc",	{},				{},				{},				{},				{},				{},
/*10*/	{},				{},				{},				{},				{},				{},				{},				{},
/*18*/	{},				"add $x,de",	{},				{},				{},				{},				{},				{},
/*20*/	{},				"ld $x,$I",		"ld ($a),$x",	"inc $x",		"inc $xh",		"dec $xh",		"ld $xh,$i",	{},
/*28*/	{},				"add $x,$x",	"ld $x,($a)",	"dec $x",		"inc $xl",		"dec $xl",		"ld $xl,$i",	{},
/*30*/	{},				{},				{},				{},				"inc $X",		"dec $X",		"ld $X,$i",		{},
/*38*/	{},				"add $x,sp",	{},				{},				{},				{},				{},				{},
/*40*/	{},				{},				{},				{},				"ld b,$xh",		"ld b,$xl",		"ld b,$X",		{},
/*48*/	{},				{},				{},				{},				"ld c,$xh",		"ld c,$xl",		"ld c,$X",		{},
/*50*/	{},				{},				{},				{},				"ld d,$xh",		"ld d,$xl",		"ld d,$X",		{},
/*58*/	{},				{},				{},				{},				"ld e,$xh",		"ld e,$xl",		"ld e,$X",		{},
/*60*/	"ld $xh,b",		"ld $xh,c",		"ld $xh,d",		"ld $xh,e",		"ld $xh,$xh",	"ld $xh,$xl",	"ld h,$X",		"ld $xh,a",
/*68*/	"ld $xl,b",		"ld $xl,c",		"ld $xl,d",		"ld $xl,e",		"ld $xl,$xh",	"ld $xl,$xl",	"ld l,$X",		"ld $xl,a",
/*70*/	"ld $X,b",		"ld $X,c",		"ld $X,d",		"ld $X,e",		"ld $X,$xh",	"ld $X,$xl",	{},				"ld $X,a",
/*78*/	{},				{},				{},				{},				"ld a,$xh",		"ld a,$xl",		"ld a,$X",		{},
/*80*/	{},				{},				{},				{},				"add a,$xh",	"add a,$xl",	"add a,$X",		{},
/*88*/	{},				{},				{},				{},				"adc a,$xh",	"adc a,$xl",	"adc a,$X",		{},
/*90*/	{},				{},				{},				{},				"sub a,$xh",	"sub a,$xl",	"sub a,$X",		{},
/*98*/	{},				{},				{},				{},				"sbc a,$xh",	"sbc a,$xl",	"sbc a,$X",		{},
/*A0*/	{},				{},				{},				{},				"and a,$xh",	"and a,$xl",	"and a,$X",		{},
/*A8*/	{},				{},				{},				{},				"xor a,$xh",	"xor a,$xl",	"xor a,$X",		{},
/*B0*/	{},				{},				{},				{},				"or a,$xh",		"or a,$xl",		"or a,$X",		{},
/*B8*/	{},				{},				{},				{},				"cp a,$xh",		"cp a,$xl",		"cp a,$X",		{},
/*C0*/	{},				{},				{},				{},				{},				{},				{},				{},
/*C8*/	{},				{},				{},				{},				{},				{},				{},				{},
/*D0*/	{},				{},				{},				{},				{},				{},				{},				{},
/*D8*/	{},				{},				{},				{},				{},				{},				{},				{},
/*E0*/	{},				"pop $x",		{},				"ex (sp),$x",	{},				"push $x",		{},				{},
/*E8*/	{},				"jp ($x)",		{},				{},				{},				{},				{},				{},
/*F0*/	{},				{},				{},				{},				{},				{},				{},				{},
/*F8*/	{},				"ld sp,$x",		{},				{},				{},				{},				{},				{},
	};

	constexpr Z80Insn kZ80InsnsDDFDCB[] = {
/*00*/	"rlc $X,b",		"rlc $X,c",		"rlc $X,d",		"rlc $X,e",		"rlc $X,h",		"rlc $X,l",		"rlc $X",		"rlc $X,a",
/*08*/	"rrc $X,b",		"rrc $X,c",		"rrc $X,d",		"rrc $X,e",		"rrc $X,h",		"rrc $X,l",		"rrc $X",		"rrc $X,a",
/*10*/	"rl $X,b",		"rl $X,c",		"rl $X,d",		"rl $X,e",		"rl $X,h",		"rl $X,l",		"rl ($X)",		"rl $X,a",
/*18*/	"rr $X,b",		"rr $X,c",		"rr $X,d",		"rr $X,e",		"rr $X,h",		"rr $X,l",		"rr ($X)",		"rr $X,a",
/*20*/	"sla $X,b",		"sla $X,c",		"sla $X,d",		"sla $X,e",		"sla $X,h",		"sla $X,l",		"sla $X",		"sla $X,a",
/*28*/	"sra $X,b",		"sra $X,c",		"sra $X,d",		"sra $X,e",		"sra $X,h",		"sra $X,l",		"sra $X",		"sra $X,a",
/*30*/	"sll $X,b",		"sll $X,c",		"sll $X,d",		"sll $X,e",		"sll $X,h",		"sll $X,l",		"sll $X",		"sll $X,a",
/*38*/	"srl $X,b",		"srl $X,c",		"srl $X,d",		"srl $X,e",		"srl $X,h",		"srl $X,l",		"srl $X",		"srl $X,a",
/*40*/	"bit 0,$X",		"bit 0,$X",		"bit 0,$X",		"bit 0,$X",		"bit 0,$X",		"bit 0,$X",		"bit 0,$X",		"bit 0,$X",
/*48*/	"bit 1,$X",		"bit 1,$X",		"bit 1,$X",		"bit 1,$X",		"bit 1,$X",		"bit 1,$X",		"bit 1,$X",		"bit 1,$X",
/*50*/	"bit 2,$X",		"bit 2,$X",		"bit 2,$X",		"bit 2,$X",		"bit 2,$X",		"bit 2,$X",		"bit 2,$X",		"bit 2,$X",
/*58*/	"bit 3,$X",		"bit 3,$X",		"bit 3,$X",		"bit 3,$X",		"bit 3,$X",		"bit 3,$X",		"bit 3,$X",		"bit 3,$X",
/*60*/	"bit 4,$X",		"bit 4,$X",		"bit 4,$X",		"bit 4,$X",		"bit 4,$X",		"bit 4,$X",		"bit 4,$X",		"bit 4,$X",
/*68*/	"bit 5,$X",		"bit 5,$X",		"bit 5,$X",		"bit 5,$X",		"bit 5,$X",		"bit 5,$X",		"bit 5,$X",		"bit 5,$X",
/*70*/	"bit 6,$X",		"bit 6,$X",		"bit 6,$X",		"bit 6,$X",		"bit 6,$X",		"bit 6,$X",		"bit 6,$X",		"bit 6,$X",
/*78*/	"bit 7,$X",		"bit 7,$X",		"bit 7,$X",		"bit 7,$X",		"bit 7,$X",		"bit 7,$X",		"bit 7,$X",		"bit 7,$X",
/*80*/	"res 0,$X",		"res 0,$X",		"res 0,$X",		"res 0,$X",		"res 0,$X",		"res 0,$X",		"res 0,$X",		"res 0,$X",
/*88*/	"res 1,$X",		"res 1,$X",		"res 1,$X",		"res 1,$X",		"res 1,$X",		"res 1,$X",		"res 1,$X",		"res 1,$X",
/*90*/	"res 2,$X",		"res 2,$X",		"res 2,$X",		"res 2,$X",		"res 2,$X",		"res 2,$X",		"res 2,$X",		"res 2,$X",
/*98*/	"res 3,$X",		"res 3,$X",		"res 3,$X",		"res 3,$X",		"res 3,$X",		"res 3,$X",		"res 3,$X",		"res 3,$X",
/*A0*/	"res 4,$X",		"res 4,$X",		"res 4,$X",		"res 4,$X",		"res 4,$X",		"res 4,$X",		"res 4,$X",		"res 4,$X",
/*A8*/	"res 5,$X",		"res 5,$X",		"res 5,$X",		"res 5,$X",		"res 5,$X",		"res 5,$X",		"res 5,$X",		"res 5,$X",
/*B0*/	"res 6,$X",		"res 6,$X",		"res 6,$X",		"res 6,$X",		"res 6,$X",		"res 6,$X",		"res 6,$X",		"res 6,$X",
/*B8*/	"res 7,$X",		"res 7,$X",		"res 7,$X",		"res 7,$X",		"res 7,$X",		"res 7,$X",		"res 7,$X",		"res 7,$X",
/*C0*/	"set 0,$X",		"set 0,$X",		"set 0,$X",		"set 0,$X",		"set 0,$X",		"set 0,$X",		"set 0,$X",		"set 0,$X",
/*C8*/	"set 1,$X",		"set 1,$X",		"set 1,$X",		"set 1,$X",		"set 1,$X",		"set 1,$X",		"set 1,$X",		"set 1,$X",
/*D0*/	"set 2,$X",		"set 2,$X",		"set 2,$X",		"set 2,$X",		"set 2,$X",		"set 2,$X",		"set 2,$X",		"set 2,$X",
/*D8*/	"set 3,$X",		"set 3,$X",		"set 3,$X",		"set 3,$X",		"set 3,$X",		"set 3,$X",		"set 3,$X",		"set 3,$X",
/*E0*/	"set 4,$X",		"set 4,$X",		"set 4,$X",		"set 4,$X",		"set 4,$X",		"set 4,$X",		"set 4,$X",		"set 4,$X",
/*E8*/	"set 5,$X",		"set 5,$X",		"set 5,$X",		"set 5,$X",		"set 5,$X",		"set 5,$X",		"set 5,$X",		"set 5,$X",
/*F0*/	"set 6,$X",		"set 6,$X",		"set 6,$X",		"set 6,$X",		"set 6,$X",		"set 6,$X",		"set 6,$X",		"set 6,$X",
/*F8*/	"set 7,$X",		"set 7,$X",		"set 7,$X",		"set 7,$X",		"set 7,$X",		"set 7,$X",		"set 7,$X",		"set 7,$X",
	};

	static_assert(vdcountof(kZ80Insns) == 256, "Z80 base table wrong length");
	static_assert(vdcountof(kZ80InsnsED) == 256, "Z80 ED table wrong length");
	static_assert(vdcountof(kZ80InsnsCB) == 256, "Z80 CB table wrong length");
	static_assert(vdcountof(kZ80InsnsDDFD) == 256, "Z80 DD/FD table wrong length");
	static_assert(vdcountof(kZ80InsnsDDFDCB) == 256, "Z80 DDCB/FDCB table wrong length");

	enum ATMCS48TokenMode : uint8 {
		kATMCS48TokenMode_None,
		kATMCS48TokenMode_PCAbs8,
		kATMCS48TokenMode_PCAbs16,
		kATMCS48TokenMode_Imm,
	};

	const uint8 kMCS48TokenModeBytes[]={
		0, 1, 1, 1
	};

	static_assert(vdcountof(kMCS48TokenModeBytes) == kATMCS48TokenMode_Imm + 1, "token mode byte table missized");

	constexpr ATMCS48TokenMode MCS48ParseInsnToken(const char *s) {
		return *s != '$' ? kATMCS48TokenMode_None
			: s[1] == 'r' ? kATMCS48TokenMode_PCAbs8
			: s[1] == 'p' ? kATMCS48TokenMode_PCAbs16
			: s[1] == 'i' ? kATMCS48TokenMode_Imm
			: throw;
	}

	constexpr int MCS48FindNextInsnToken(const char *s) {
		return *s == '$' || !*s
			? 0
			: MCS48FindNextInsnToken(s+1) + 1;
	}

	struct MCS48Insn {
		const char *s = nullptr;
		uint8 prefixLen = 0;
		ATMCS48TokenMode token = kATMCS48TokenMode_None;

		constexpr MCS48Insn() = default;

		constexpr MCS48Insn(const char *s_, uint8 prefixLen_)
			: s(s_)
			, prefixLen(prefixLen_)
			, token(MCS48ParseInsnToken(s_ + prefixLen_))
		{
		}

		constexpr MCS48Insn(const char *s) : MCS48Insn(s, (uint8)FindNextInsnToken(s)) {}

		uint32 GetOpcodeLen() const {
			return s ? kMCS48TokenModeBytes[token] + 1 : 0;
		}
	};

	constexpr MCS48Insn kMCS48Insns[] = {
/*00*/	"NOP",			{},				{},				"ADD A,#$i",				"JMP $p",		"EN I",			{},				"DEC A",
/*08*/	"IN A,BUS",		"IN A,P1",		"IN A,P2",		{},				{},				{},				{},				{},
/*10*/	"INC @R0",		"INC @R1",		"JB0 $r",		"ADDC A,#$i",	"CALL $p",		"DIS I",		"JTF $r",		"INC A",
/*18*/	"INC R0",		"INC R1",		"INC R2",		"INC R3",		"INC R4",		"INC R5",		"INC R6",		"INC R7",
/*20*/	"XCH A,@R0",	"XCH A,@R1",	{},				"MOV A,#$i",	"JMP $p",		"EN TCNTI",		"JNT0 $r",		"CLR A",
/*28*/	"XCH A,R0",		"XCH A,R1",		"XCH A,R2",		"XCH A,R3",		"XCH A,R4",		"XCH A,R5",		"XCH A,R6",		"XCH A,R7",
/*30*/	{},				{},				"JB1 $r",		{},				"CALL $p",		"DIS TCNTI",	"JT0 $r",		"CPL A",
/*38*/	"OUTL BUS,A",	"OUTL P1,A",	"OUTL P2,A",	{},				{},				{},				{},				{},
/*40*/	"ORL A,@R0",	"ORL A,@R1",	"MOV A,T",		"ORL A,#$i",	"JMP $p",		"STRT TCNT",	"JNT1 $r",		"SWAP A",
/*48*/	"ORL A,R0",		"ORL A,R1",		"ORL A,R2",		"ORL A,R3",		"ORL A,R4",		"ORL A,R5",		"ORL A,R6",		"ORL A,R7",
/*50*/	"ANL A,@R0",	"ANL A,@R1",	"JB2 $r",		"ANL A,#$i",	"CALL $p",		"STRT T",		"JT1 $r",		"DA A",
/*58*/	"ANL A,R0",		"ANL A,R1",		"ANL A,R2",		"ANL A,R3",		"ANL A,R4",		"ANL A,R5",		"ANL A,R6",		"ANL A,R7",
/*60*/	"ADD A,@R0",	"ADD A,@R1",	"MOV T,A",		{},				"JMP $p",		"STOP TCNT",	{},				"RRC A",
/*68*/	"ADD A,R0",		"ADD A,R1",		"ADD A,R2",		"ADD A,R3",		"ADD A,R4",		"ADD A,R5",		"ADD A,R6",		"ADD A,R7",
/*70*/	"ADDC A,@R0",	"ADDC A,@R1",	"JB3 $r",		{},				"CALL $p",		{},				"JF1 $r",		"RR A",
/*78*/	"ADDC A,R0",	"ADDC A,R1",	"ADDC A,R2",	"ADDC A,R3",	"ADDC A,R4",	"ADDC A,R5",	"ADDC A,R6",	"ADDC A,R7",
/*80*/	"MOVX A,@R0",	"MOVX A,@R1",	{},				"RET",			"JMP $p",		"CLR F0",		"JNI $r",		{},
/*88*/	"ORL BUS,#$i",	"ORL P1,#$i",	"ORL P2,#$i",	{},				{},				{},				{},				{},
/*90*/	"MOVX @R0,A",	"MOVX @R1,A",	"JB4 $r",		"RETR",			"CALL $p",		"CPL F0",		"JNZ $r",		"CLR C",
/*98*/	"ANL BUS,#$i",	"ANL P1,#$i",	"ANL P2,#$i",	{},				{},				{},				{},				{},
/*A0*/	"MOV @R0,A",	"MOV @R1,A",	{},				"MOVP A,@A",	"JMP $p",		"CLR F1",		{},				"CPL C",
/*A8*/	"MOV R0,A",		"MOV R1,A",		"MOV R2,A",		"MOV R3,A",		"MOV R4,A",		"MOV R5,A",		"MOV R6,A",		"MOV R7,A",
/*B0*/	"MOV @R0,#$i",	"MOV @R1,#$i",	"JB5 $r",		"JMPP @A",		"CALL $p",		"CPL F1",		"JF0 $r",		{},
/*B8*/	"MOV R0,#$i",	"MOV R1,#$i",	"MOV R2,#$i",	"MOV R3,#$i",	"MOV R4,#$i",	"MOV R5,#$i",	"MOV R6,#$i",	"MOV R7,#$i",
/*C0*/	{},				{},				{},				{},				"JMP $p",		"SEL RB0",		"JZ $r",		"MOV A,PSW",
/*C8*/	"DEC R0",		"DEC R1",		"DEC R2",		"DEC R3",		"DEC R4",		"DEC R5",		"DEC R6",		"DEC R7",
/*D0*/	"XRL A,@R0",	"XRL A,@R1",	"JB6 $r",		"XRL A,#$i",	"CALL $p",		"SEL RB1",		{},				"MOV PSW,A",
/*D8*/	"XRL A,R0",		"XRL A,R1",		"XRL A,R2",		"XRL A,R3",		"XRL A,R4",		"XRL A,R5",		"XRL A,R6",		"XRL A,R7",
/*E0*/	{},				{},				{},				"MOVP3 A,@A",	"JMP $p",		"SEL MB0",		"JNC $r",		"RL A",
/*E8*/	"DJNZ R0,$r",	"DJNZ R1,$r",	"DJNZ R2,$r",	"DJNZ R3,$r",	"DJNZ R4,$r",	"DJNZ R5,$r",	"DJNZ R6,$r",	"DJNZ R7,$r",
/*F0*/	"MOV A,@R0",	"MOV A,@R1",	"JB7 $r",		{},				"CALL $p",		"SEL MB1",		"JC $r",		"RLC A",
/*F8*/	"MOV A,R0",		"MOV A,R1",		"MOV A,R2",		"MOV A,R3",		"MOV A,R4",		"MOV A,R5",		"MOV A,R6",		"MOV A,R7",
	};

	static_assert(vdcountof(kMCS48Insns) == 256, "MCS-48 base table wrong length");
}

namespace AT6809Dsm {
	enum class NameCode : uint8;

	struct Insn {
		uint8 mName;
		uint8 mMode;

		constexpr Insn()
			: mName(0)
			, mMode(0)
		{
		}

		constexpr Insn(NameCode nameCode)
			: mName((uint8)nameCode)
			, mMode(0)
		{
		}

		constexpr Insn(NameCode nameCode, uint8 mode)
			: mName((uint8)nameCode)
			, mMode(mode)
		{
		}
	};

	constexpr Insn operator-(NameCode name, uint8 mode) {
		return Insn((NameCode)name, mode);
	}

	static const char *const kNames[] = {
		nullptr,
		"ABX",	
		"ADCA",
		"ADCB",
		"ADDA",
		"ADDB",
		"ADDD",
		"ANDA",
		"ANDB",
		"ANDCC",
		"ASL",	
		"ASLA",
		"ASLB",
		"ASR",	
		"ASRA",
		"ASRB",
		"BCC",	
		"BCS",	
		"BEQ",	
		"BGE",	
		"BGT",	
		"BHI",	
		"BITA",
		"BITB",
		"BLE",	
		"BLS",	
		"BLT",	
		"BMI",	
		"BNE",	
		"BPL",	
		"BRA",	
		"BRN",	
		"BSR",	
		"BVC",	
		"BVS",	
		"CLR",	
		"CLRA",
		"CLRB",
		"CMPA",
		"CMPB",
		"CMPD",
		"CMPS",
		"CMPU",
		"CMPX",
		"CMPY",
		"COM",	
		"COMA",
		"COMB",
		"CWAI",
		"DAA",	
		"DEC",	
		"DECA",
		"DECB",
		"EORA",
		"EORB",
		"EXG",	
		"INC",	
		"INCA",
		"INCB",
		"JMP",	
		"JSR",	
		"LBCC",
		"LBCS",
		"LBEQ",
		"LBGE",
		"LBGT",
		"LBHI",
		"LBLE",
		"LBLS",
		"LBLT",
		"LBMI",
		"LBNE",
		"LBPL",
		"LBRA",
		"LBRN",
		"LBSR",
		"LBVC",
		"LBVS",
		"LDA",	
		"LDB",	
		"LDD",	
		"LDS",	
		"LDU",	
		"LDX",	
		"LDY",	
		"LEAS",
		"LEAU",
		"LEAX",
		"LEAY",
		"LSR",	
		"LSRA",
		"LSRB",
		"MUL",	
		"NEG",	
		"NEGA",
		"NEGB",
		"NOP",	
		"ORA",	
		"ORB",	
		"ORCC",
		"PSHS",
		"PSHU",
		"PULS",
		"PULU",
		"ROL",	
		"ROLA",
		"ROLB",
		"ROR",	
		"RORA",
		"RORB",
		"RTI",	
		"RTS",	
		"SBCA",
		"SBCB",
		"SEX",	
		"STA",	
		"STB",	
		"STD",	
		"STS",	
		"STU",	
		"STX",	
		"STY",	
		"SUBA",
		"SUBB",
		"SUBD",
		"SWI",	
		"SWI2",
		"SWI3",
		"SYNC",
		"TFR",	
		"TST",	
		"TSTA",
		"TSTB",
	};

	const uint8 i = 1;
	const uint8 il = 2;
	const uint8 d = 3;
	const uint8 e = 4;
	const uint8 x = 5;
	const uint8 r = 6;
	const uint8 rl = 7;
	const uint8 m = 8;
	const uint8 t = 9;

	constexpr NameCode ABX	= NameCode(  1);
	constexpr NameCode ADCA	= NameCode(  2);
	constexpr NameCode ADCB	= NameCode(  3);
	constexpr NameCode ADDA	= NameCode(  4);
	constexpr NameCode ADDB	= NameCode(  5);
	constexpr NameCode ADDD	= NameCode(  6);
	constexpr NameCode ANDA	= NameCode(  7);
	constexpr NameCode ANDB	= NameCode(  8);
	constexpr NameCode ANDCC= NameCode(  9);
	constexpr NameCode ASL	= NameCode( 10);
	constexpr NameCode ASLA	= NameCode( 11);
	constexpr NameCode ASLB	= NameCode( 12);
	constexpr NameCode ASR	= NameCode( 13);
	constexpr NameCode ASRA	= NameCode( 14);
	constexpr NameCode ASRB	= NameCode( 15);
	constexpr NameCode BCC	= NameCode( 16);
	constexpr NameCode BCS	= NameCode( 17);
	constexpr NameCode BEQ	= NameCode( 18);
	constexpr NameCode BGE	= NameCode( 19);
	constexpr NameCode BGT	= NameCode( 20);
	constexpr NameCode BHI	= NameCode( 21);
	constexpr NameCode BITA	= NameCode( 22);
	constexpr NameCode BITB	= NameCode( 23);
	constexpr NameCode BLE	= NameCode( 24);
	constexpr NameCode BLS	= NameCode( 25);
	constexpr NameCode BLT	= NameCode( 26);
	constexpr NameCode BMI	= NameCode( 27);
	constexpr NameCode BNE	= NameCode( 28);
	constexpr NameCode BPL	= NameCode( 29);
	constexpr NameCode BRA	= NameCode( 30);
	constexpr NameCode BRN	= NameCode( 31);
	constexpr NameCode BSR	= NameCode( 32);
	constexpr NameCode BVC	= NameCode( 33);
	constexpr NameCode BVS	= NameCode( 34);
	constexpr NameCode CLR	= NameCode( 35);
	constexpr NameCode CLRA	= NameCode( 36);
	constexpr NameCode CLRB	= NameCode( 37);
	constexpr NameCode CMPA	= NameCode( 38);
	constexpr NameCode CMPB	= NameCode( 39);
	constexpr NameCode CMPD	= NameCode( 40);
	constexpr NameCode CMPS	= NameCode( 41);
	constexpr NameCode CMPU	= NameCode( 42);
	constexpr NameCode CMPX	= NameCode( 43);
	constexpr NameCode CMPY	= NameCode( 44);
	constexpr NameCode COM	= NameCode( 45);
	constexpr NameCode COMA	= NameCode( 46);
	constexpr NameCode COMB	= NameCode( 47);
	constexpr NameCode CWAI	= NameCode( 48);
	constexpr NameCode DAA	= NameCode( 49);
	constexpr NameCode DEC	= NameCode( 50);
	constexpr NameCode DECA	= NameCode( 51);
	constexpr NameCode DECB	= NameCode( 52);
	constexpr NameCode EORA	= NameCode( 53);
	constexpr NameCode EORB	= NameCode( 54);
	constexpr NameCode EXG	= NameCode( 55);
	constexpr NameCode INC	= NameCode( 56);
	constexpr NameCode INCA	= NameCode( 57);
	constexpr NameCode INCB	= NameCode( 58);
	constexpr NameCode JMP	= NameCode( 59);
	constexpr NameCode JSR	= NameCode( 60);
	constexpr NameCode LBCC	= NameCode( 61);
	constexpr NameCode LBCS	= NameCode( 62);
	constexpr NameCode LBEQ	= NameCode( 63);
	constexpr NameCode LBGE	= NameCode( 64);
	constexpr NameCode LBGT	= NameCode( 65);
	constexpr NameCode LBHI	= NameCode( 66);
	constexpr NameCode LBLE	= NameCode( 67);
	constexpr NameCode LBLS	= NameCode( 68);
	constexpr NameCode LBLT	= NameCode( 69);
	constexpr NameCode LBMI	= NameCode( 70);
	constexpr NameCode LBNE	= NameCode( 71);
	constexpr NameCode LBPL	= NameCode( 72);
	constexpr NameCode LBRA	= NameCode( 73);
	constexpr NameCode LBRN	= NameCode( 74);
	constexpr NameCode LBSR	= NameCode( 75);
	constexpr NameCode LBVC	= NameCode( 76);
	constexpr NameCode LBVS	= NameCode( 77);
	constexpr NameCode LDA	= NameCode( 78);
	constexpr NameCode LDB	= NameCode( 79);
	constexpr NameCode LDD	= NameCode( 80);
	constexpr NameCode LDS	= NameCode( 81);
	constexpr NameCode LDU	= NameCode( 82);
	constexpr NameCode LDX	= NameCode( 83);
	constexpr NameCode LDY	= NameCode( 84);
	constexpr NameCode LEAS	= NameCode( 85);
	constexpr NameCode LEAU	= NameCode( 86);
	constexpr NameCode LEAX	= NameCode( 87);
	constexpr NameCode LEAY	= NameCode( 88);
	constexpr NameCode LSR	= NameCode( 89);
	constexpr NameCode LSRA	= NameCode( 90);
	constexpr NameCode LSRB	= NameCode( 91);
	constexpr NameCode MUL	= NameCode( 92);
	constexpr NameCode NEG	= NameCode( 93);
	constexpr NameCode NEGA	= NameCode( 94);
	constexpr NameCode NEGB	= NameCode( 95);
	constexpr NameCode NOP	= NameCode( 96);
	constexpr NameCode ORA	= NameCode( 97);
	constexpr NameCode ORB	= NameCode( 98);
	constexpr NameCode ORCC	= NameCode( 99);
	constexpr NameCode PSHS	= NameCode(100);
	constexpr NameCode PSHU	= NameCode(101);
	constexpr NameCode PULS	= NameCode(102);
	constexpr NameCode PULU	= NameCode(103);
	constexpr NameCode ROL	= NameCode(104);
	constexpr NameCode ROLA	= NameCode(105);
	constexpr NameCode ROLB	= NameCode(106);
	constexpr NameCode ROR	= NameCode(107);
	constexpr NameCode RORA	= NameCode(108);
	constexpr NameCode RORB	= NameCode(109);
	constexpr NameCode RTI	= NameCode(110);
	constexpr NameCode RTS	= NameCode(111);
	constexpr NameCode SBCA	= NameCode(112);
	constexpr NameCode SBCB	= NameCode(113);
	constexpr NameCode SEX	= NameCode(114);
	constexpr NameCode STA	= NameCode(115);
	constexpr NameCode STB	= NameCode(116);
	constexpr NameCode STD	= NameCode(117);
	constexpr NameCode STS	= NameCode(118);
	constexpr NameCode STU	= NameCode(119);
	constexpr NameCode STX	= NameCode(120);
	constexpr NameCode STY	= NameCode(121);
	constexpr NameCode SUBA	= NameCode(122);
	constexpr NameCode SUBB	= NameCode(123);
	constexpr NameCode SUBD	= NameCode(124);
	constexpr NameCode SWI	= NameCode(125);
	constexpr NameCode SWI2	= NameCode(126);
	constexpr NameCode SWI3	= NameCode(127);
	constexpr NameCode SYNC	= NameCode(128);
	constexpr NameCode TFR	= NameCode(129);
	constexpr NameCode TST	= NameCode(130);
	constexpr NameCode TSTA	= NameCode(131);
	constexpr NameCode TSTB	= NameCode(132);

	constexpr Insn kInsns[] = {
/*00*/	NEG-d,	{},		{},		COM-d,	LSR-d,	{},		ROR-d,	ASR-d,	ASL-d,	ROL-d,	DEC-d,	{},		INC-d,	TST-d,	JMP-d,	CLR-d,
/*10*/	{},		{},		NOP,	SYNC,	{},		{},		LBRA-rl,LBSR-rl,{},		DAA,	ORCC-i,	{},		ANDCC-i,SEX,	EXG-t,	TFR-t,
/*20*/	BRA-r,	BRN-r,	BHI-r,	BLS-r,	BCC-r,	BCS-r,	BNE-r,	BEQ-r,	BVC-r,	BVS-r,	BPL-r,	BMI-r,	BGE-r,	BLT-r,	BGT-r,	BLE-r,
/*30*/	LEAX-x,	LEAY-x,	LEAS-x,	LEAU-x,	PSHS-m,	PULS-m,	PSHU-m,	PULU-m,	{},		RTS,	ABX,	RTI,	CWAI-i,	MUL,	{},		SWI,
/*40*/	NEGA,	{},		{},		COMA,	LSRA,	{},		RORA,	ASRA,	ASLA,	ROLA,	DECA,	{},		INCA,	TSTA,	{},		CLRA,
/*50*/	NEGB,	{},		{},		COMB,	LSRB,	{},		RORB,	ASRB,	ASLB,	ROLB,	DECB,	{},		INCB,	TSTB,	{},		CLRB,
/*60*/	NEG-x,	{},		{},		COM-x,	LSR-x,	{},		ROR-x,	ASR-x,	ASL-x,	ROL-x,	DEC-x,	{},		INC-x,	TST-x,	JMP-x,	CLR-x,
/*70*/	NEG-e,	{},		{},		COM-e,	LSR-e,	{},		ROR-e,	ASR-e,	ASL-e,	ROL-e,	DEC-e,	{},		INC-e,	TST-e,	JMP-e,	CLR-e,
/*80*/	SUBA-i,	CMPA-i,	SBCA-i,	SUBD-il,ANDA-i,	BITA-i,	LDA-i,	{},		EORA-i,	ADCA-i,	ORA-i,	ADDA-i,	CMPX-i,	BSR-r,	LDX-il,	{},
/*90*/	SUBA-d,	CMPA-d,	SBCA-d,	SUBD-d,	ANDA-d,	BITA-d,	LDA-d,	STA-d,	EORA-d,	ADCA-d,	ORA-d,	ADDA-d,	CMPX-d,	JSR-d,	LDX-d,	STX-d,
/*A0*/	SUBA-x,	CMPA-x,	SBCA-x,	SUBD-x,	ANDA-x,	BITA-x,	LDA-x,	STA-x,	EORA-x,	ADCA-x,	ORA-x,	ADDA-x,	CMPX-x,	JSR-x,	LDX-x,	STX-x,
/*B0*/	SUBA-e,	CMPA-e,	SBCA-e,	SUBD-e,	ANDA-e,	BITA-e,	LDA-e,	STA-e,	EORA-e,	ADCA-e,	ORA-e,	ADDA-e,	CMPX-e,	JSR-e,	LDX-e,	STX-e,
/*C0*/	SUBB-i,	CMPB-i,	SBCB-i,	ADDD-il,ANDB-i,	BITB-i,	LDB-i,	{},		EORB-i,	ADCB-i,	ORB-i,	ADDB-i,	LDD-il,	{},		LDU-il,	{},
/*D0*/	SUBB-d,	CMPB-d,	SBCB-d,	ADDD-d,	ANDB-d,	BITB-d,	LDB-d,	STB-d,	EORB-d,	ADCB-d,	ORB-d,	ADDB-d,	LDD-d,	STD-d,	LDU-d,	STU-d,
/*E0*/	SUBB-x,	CMPB-x,	SBCB-x,	ADDD-x,	ANDB-x,	BITB-x,	LDB-x,	STB-x,	EORB-x,	ADCB-x,	ORB-x,	ADDB-x,	LDD-x,	STD-x,	LDU-x,	STU-x,
/*F0*/	SUBB-e,	CMPB-e,	SBCB-e,	ADDD-e,	ANDB-e,	BITB-e,	LDB-e,	STB-e,	EORB-e,	ADCB-e,	ORB-e,	ADDB-e,	LDD-e,	STD-e,	LDU-e,	STU-e,
	};

	constexpr Insn kInsns10[] = {
/*00*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*10*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*20*/	{},		LBRN-rl,LBHI-rl,LBLS-rl,LBCC-rl,LBCS-rl,LBNE-rl,LBEQ-rl,LBVC-rl,LBVS-rl,LBPL-rl,LBMI-rl,LBGE-rl,LBLT-rl,LBGT-rl,LBLE-rl,
/*30*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		SWI2-rl,
/*40*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*50*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*60*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*70*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*80*/	{},		{},		{},		CMPD-il,{},		{},		{},		{},		{},		{},		{},		{},		CMPY-il,{},		LDY-il,	{},
/*90*/	{},		{},		{},		CMPD-d,	{},		{},		{},		{},		{},		{},		{},		{},		CMPY-d,	{},		LDY-d,	STY-d,
/*A0*/	{},		{},		{},		CMPD-x,	{},		{},		{},		{},		{},		{},		{},		{},		CMPY-x,	{},		LDY-x,	STY-x,
/*B0*/	{},		{},		{},		CMPD-e,	{},		{},		{},		{},		{},		{},		{},		{},		CMPY-e,	{},		LDY-e,	STY-e,
/*C0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		LDS-il,	{},
/*D0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		LDS-d,	STS-d,
/*E0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		LDS-x,	STS-x,
/*F0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		LDS-e,	STS-e,
	};

	constexpr Insn kInsns11[] = {
/*00*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*10*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*20*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*30*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		SWI3,
/*40*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*50*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*60*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*70*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*80*/	{},		{},		{},		CMPU-il,{},		{},		{},		{},		{},		{},		{},		{},		CMPS-il,{},		{},		{},
/*90*/	{},		{},		{},		CMPU-d,	{},		{},		{},		{},		{},		{},		{},		{},		CMPS-d,	{},		{},		{},
/*A0*/	{},		{},		{},		CMPU-x,	{},		{},		{},		{},		{},		{},		{},		{},		CMPS-x,	{},		{},		{},
/*B0*/	{},		{},		{},		CMPU-e,	{},		{},		{},		{},		{},		{},		{},		{},		CMPS-e,	{},		{},		{},
/*C0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*D0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*E0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
/*F0*/	{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},		{},
	};
}

const char *ATGetSymbolName(uint32 addr, bool write) {
	IATDebuggerSymbolLookup *symlookup = ATGetDebuggerSymbolLookup();

	ATSymbol sym;
	if (!symlookup->LookupSymbol(addr, write ? kATSymbol_Write : kATSymbol_Read | kATSymbol_Execute, sym))
		return NULL;

	if (sym.mOffset != addr)
		return NULL;

	return sym.mpName;
}

const char *ATGetSymbolNameOffset(uint32 addr, bool write, sint32& offset) {
	IATDebuggerSymbolLookup *symlookup = ATGetDebuggerSymbolLookup();

	ATSymbol sym;
	if (!symlookup->LookupSymbol(addr, write ? kATSymbol_Write : kATSymbol_Read | kATSymbol_Execute, sym))
		return NULL;

	offset = addr - (sint32)sym.mOffset;
	return sym.mpName;
}

void ATDisassembleCaptureRegisterContext(ATCPUHistoryEntry& hent) {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	hent.mP = cpu.GetP();
	hent.mX = cpu.GetX();
	hent.mExt.mXH = cpu.GetXH();
	hent.mY = cpu.GetY();
	hent.mExt.mYH = cpu.GetYH();
	hent.mD = cpu.GetD();
	hent.mS = cpu.GetS();
	hent.mExt.mSH = cpu.GetSH();
	hent.mB = cpu.GetB();
	hent.mK = cpu.GetK();
	hent.mbEmulation = cpu.GetEmulationFlag();
}

void ATDisassembleCaptureRegisterContext(IATDebugTarget *target, ATCPUHistoryEntry& hent) {
	ATCPUExecState state;

	target->GetExecState(state);

	ATDisassembleCaptureRegisterContext(hent, state, target->GetDisasmMode());
}

void ATDisassembleCaptureRegisterContext(ATCPUHistoryEntry& hent, const ATCPUExecState& execState, ATDebugDisasmMode execMode) {
	switch(execMode) {
		case kATDebugDisasmMode_8048: {
			const ATCPUExecState8048& state8048 = execState.m8048;

			hent.mA = state8048.mA;
			hent.mP = state8048.mPSW;
			break;
		}
								  
		case kATDebugDisasmMode_Z80: {
			const ATCPUExecStateZ80& stateZ80 = execState.mZ80;
			hent.mZ80_A		= stateZ80.mA;
			hent.mZ80_F		= stateZ80.mF;
			hent.mZ80_B		= stateZ80.mB;
			hent.mZ80_C		= stateZ80.mC;
			hent.mZ80_D		= stateZ80.mD;
			hent.mExt.mZ80_E		= stateZ80.mE;
			hent.mExt.mZ80_H		= stateZ80.mH;
			hent.mExt.mZ80_L		= stateZ80.mL;
			hent.mZ80_SP	= stateZ80.mSP;
			hent.mbEmulation = true;
			break;
		}

		case kATDebugDisasmMode_6809: {
			const ATCPUExecState6809& state6809 = execState.m6809;
			hent.mP		= state6809.mCC;
			hent.mA		= state6809.mA;
			hent.mExt.mAH	= state6809.mB;
			hent.mX		= (uint8)state6809.mX;
			hent.mExt.mXH	= (uint8)(state6809.mX >> 8);
			hent.mY		= (uint8)state6809.mY;
			hent.mExt.mYH	= (uint8)(state6809.mY >> 8);
			hent.mD		= state6809.mDP;
			hent.mS		= (uint8)state6809.mS;
			hent.mExt.mSH	= (uint8)(state6809.mS >> 8);
			hent.mB		= state6809.mB;
			hent.mbEmulation = true;
			break;
		}

		default: {
			const ATCPUExecState6502& state6502 = execState.m6502;
			hent.mP		= state6502.mP;
			hent.mX		= state6502.mX;
			hent.mExt.mXH	= state6502.mXH;
			hent.mY		= state6502.mY;
			hent.mExt.mYH	= state6502.mYH;
			hent.mD		= state6502.mDP;
			hent.mS		= state6502.mS;
			hent.mExt.mSH	= state6502.mSH;
			hent.mB		= state6502.mB;
			hent.mK		= state6502.mK;
			hent.mbEmulation = state6502.mbEmulationFlag;
			break;
		}
	}
}

void ATDisassembleCaptureInsnContext(uint16 addr, uint8 bank, ATCPUHistoryEntry& hent) {
	uint32 addr24 = addr + ((uint32)bank << 16);
	uint8 opcode = g_sim.DebugGlobalReadByte(addr24);
	uint8 byte1 = g_sim.DebugGlobalReadByte((addr24+1) & 0xffffff);
	uint8 byte2 = g_sim.DebugGlobalReadByte((addr24+2) & 0xffffff);
	uint8 byte3 = g_sim.DebugGlobalReadByte((addr24+3) & 0xffffff);

	hent.mPC = addr;
	hent.mK = bank;
	hent.mOpcode[0] = opcode;
	hent.mOpcode[1] = byte1;
	hent.mOpcode[2] = byte2;
	hent.mOpcode[3] = byte3;
}

void ATDisassembleCaptureInsnContext(uint32 globalAddr, ATCPUHistoryEntry& hent) {
	const uint32 bankSpace = globalAddr & 0xFFFF0000;
	uint8 opcode = g_sim.DebugGlobalReadByte(globalAddr);
	uint8 byte1 = g_sim.DebugGlobalReadByte(bankSpace + ((globalAddr+1) & 0xffff));
	uint8 byte2 = g_sim.DebugGlobalReadByte(bankSpace + ((globalAddr+2) & 0xffff));
	uint8 byte3 = g_sim.DebugGlobalReadByte(bankSpace + ((globalAddr+3) & 0xffff));

	hent.mPC = (uint16)globalAddr;
	hent.mK = (uint8)(globalAddr >> 16);
	hent.mGlobalPCBase = bankSpace;
	hent.mOpcode[0] = opcode;
	hent.mOpcode[1] = byte1;
	hent.mOpcode[2] = byte2;
	hent.mOpcode[3] = byte3;
}

void ATDisassembleCaptureInsnContext(IATDebugTarget *target, uint16 addr, uint8 bank, ATCPUHistoryEntry& hent) {
	hent.mPC = addr;
	hent.mK = bank;

	// Instructions don't wrap across banks on the 65C816.
	const uint32 bank24 = ((uint32)bank << 16);
	for(uint32 i=0; i<4; ++i)
		hent.mOpcode[i] = target->DebugReadByte(bank24 + ((addr + i) & 0xffff));
}

void ATDisassembleCaptureInsnContext(IATDebugTarget *target, uint32 globalAddr, ATCPUHistoryEntry& hent) {
	hent.mPC = globalAddr & 0xFFFF;
	hent.mK = 0;
	hent.mGlobalPCBase = globalAddr & 0xFFFF0000;

	// Instructions don't wrap across banks on the 65C816.
	for(uint32 i=0; i<4; ++i)
		hent.mOpcode[i] = target->DebugReadByte(hent.mGlobalPCBase + ((globalAddr + i) & 0xffff));
}

uint16 ATDisassembleInsn(char *buf, uint16 addr, bool decodeReferences) {
	VDStringA line;

	addr = ATDisassembleInsn(line, addr, decodeReferences);

	line += '\n';
	line += (char)0;
	line.copy(buf, VDStringA::npos);

	return addr;
}

uint16 ATDisassembleInsn(VDStringA& line, uint16 addr, bool decodeReferences) {
	ATCPUHistoryEntry hent;
	ATDisassembleCaptureRegisterContext(hent);
	ATDisassembleCaptureInsnContext(addr, hent.mK, hent);

	const ATCPUEmulator& cpu = g_sim.GetCPU();
	return ATDisassembleInsn(line, g_sim.GetDebugTarget(), cpu.GetDisasmMode(), hent, decodeReferences, false, true, true, true);
}

namespace {
	const char kHexDigits[16] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

	void WriteHex8(VDStringA& line, size_t offsetFromEnd, uint8 v) {
		char *p = &*(line.end() - offsetFromEnd);

		p[0] = kHexDigits[v >> 4];
		p[1] = kHexDigits[v & 15];
	}

	void WriteHex16(VDStringA& line, size_t offsetFromEnd, uint16 v) {
		char *p = &*(line.end() - offsetFromEnd);

		p[0] = kHexDigits[v >> 12];
		p[1] = kHexDigits[(v >> 8) & 15];
		p[2] = kHexDigits[(v >> 4) & 15];
		p[3] = kHexDigits[v & 15];
	}

	void AppendIntelHex8(VDStringA& line, uint8 v, bool lowercase) {
		char buf[4];

		buf[0] = '0';
		buf[1] = kHexDigits[v >> 4];
		buf[2] = kHexDigits[v & 15];
		buf[3] = lowercase ? 'h' : 'H';

		line.append(v < 0xA0 ? buf + 1 : buf, buf + 4);
	}

	void AppendIntelHex16(VDStringA& line, uint16 v, bool lowercase) {
		char buf[6];

		buf[0] = '0';
		buf[1] = kHexDigits[v >> 12];
		buf[2] = kHexDigits[(v >> 8) & 15];
		buf[3] = kHexDigits[(v >> 4) & 15];
		buf[4] = kHexDigits[v & 15];
		buf[5] = lowercase ? 'h' : 'H';

		line.append(v < 0xA000 ? buf + 1 : buf, buf + 6);
	}
}

uint16 ATDisassembleInsnZ80(VDStringA& line, const ATCPUHistoryEntry& hent, bool showCodeBytes, bool lowercaseOps) {
	// Valid patterns:
	//	xx
	//	DD/FD xx
	//	CB xx
	//	ED xx
	//	DD/FD CB xx
	//
	// DD/FD must be the first; only the last one is used. We have to validate
	// this due to the possibility of endless prefixes.

	const uint8 op0 = hent.mOpcode[0];
	const Z80Insn *insn = &kZ80Insns[op0];
	unsigned oplen = 1;

	if (op0 == 0xDD || op0 == 0xFD) {
		const uint8 op1 = hent.mOpcode[1];
		
		// Okay, we have an IX/IY-based instruction. This is invalid with DD/ED/FD
		// prefixes, for which we just emit a byte. However, it is valid with the CB
		// prefix.
		//
		// An additional complication for DDCB or FDCB is that if (IX+d) or (IY+d) is
		// also used, the displacement comes before the final opcode byte, i.e. DDCBddoo.
		// How can we tell if indexing is used, though, if the opcode byte comes after
		// the displacement? That's easy -- all DDCB or FDCB opcodes do.

		if (op1 != 0xDD && op1 != 0xFD && op1 != 0xED) {
			if (op1 == 0xCB) {
				oplen = 3;

				insn = &kZ80InsnsDDFDCB[hent.mOpcode[3]];
			} else {
				oplen = 2;
				insn = &kZ80InsnsDDFD[op1];
			}
		}
	} else if (op0 == 0xED) {
		insn = &kZ80InsnsED[hent.mOpcode[1]];
		oplen = 2;
	} else if (op0 == 0xCB) {
		insn = &kZ80InsnsCB[hent.mOpcode[1]];
		oplen = 2;
	}

	const char *s = insn->s;
	if (!s)
		oplen = 1;
	else {
		oplen += kZ80TokenModeBytes[insn->token1] + kZ80TokenModeBytes[insn->token2];
	}

	if (showCodeBytes) {
		line.append(20, ' ');
		WriteHex16(line, 20, hent.mPC);
		line.end()[-16] = ':';
		WriteHex8(line, 14, op0);

		if (oplen >= 2) {
			WriteHex8(line, 11, hent.mOpcode[1]);

			if (oplen >= 3) {
				WriteHex8(line, 8, hent.mOpcode[2]);

				if (oplen >= 4) {
					WriteHex8(line, 5, hent.mOpcode[3]);
				}
			}
		}
	} else {
		line.append(8, ' ');
		WriteHex16(line, 8, hent.mPC);
		line.end()[-4] = ':';
	}

	if (!s) {
		line.append(lowercaseOps ? "defb    " : "DEFB    ");
		AppendIntelHex8(line, op0, lowercaseOps);
	} else {
		for(int i=0; i<2; ++i) {
			const size_t firstLen = i ? insn->midLen : insn->prefixLen;
			const char *split = (const char *)memchr(s, ' ', firstLen);

			const auto startLen = line.size();

			if (split) {
				size_t splitLen = (size_t)(split - s);
				line.append(s, s + splitLen);
				line.append(splitLen < 8 ? 8 - splitLen : 1, ' ');
				line.append(s + splitLen + 1, s + firstLen);
			} else {
				line.append(s, s + firstLen);
			}

			if (!lowercaseOps) {
				std::transform(line.begin() + startLen, line.end(), line.begin() + startLen,
					[](char c) { return (unsigned)((unsigned)c - 0x61) < 26 ? (char)(c - 0x20) : c; });
			}

			s += firstLen;

			const auto token = i ? insn->token2 : insn->token1;

			if (token) {
				s += 2;

				switch(token) {
					case kATZ80TokenMode_None:
						break;

					case kATZ80TokenMode_PCRel8:
						AppendIntelHex16(line, (uint16)(hent.mPC + oplen + (sint8)hent.mOpcode[oplen - 1]), lowercaseOps);
						break;

					case kATZ80TokenMode_PCAbs16:
					case kATZ80TokenMode_Imm16:
					case kATZ80TokenMode_Abs:
						AppendIntelHex16(line, VDReadUnalignedLEU16(&hent.mOpcode[oplen - 2]), lowercaseOps);
						break;

					case kATZ80TokenMode_Imm8:
					case kATZ80TokenMode_IOPort:
						AppendIntelHex8(line, hent.mOpcode[oplen - 1], lowercaseOps);
						break;

					case kATZ80TokenMode_Index:
						if (lowercaseOps)
							line.append(op0 == 0xDD ? "ix" : "iy");
						else
							line.append(op0 == 0xDD ? "IX" : "IY");
						break;

					case kATZ80TokenMode_Indirect:
						{
							// The displacement is always the third opcode byte. This is important
							// for shift/rotate opcodes of the form DDCBddoo or FDCBddoo.
							const uint8 disp = hent.mOpcode[2];

							if (lowercaseOps)
								line.append(op0 == 0xDD ? "(ix+00h)" : "(iy+00h)");
							else
								line.append(op0 == 0xDD ? "(IX+00h)" : "(IY+00h)");

							if (disp & 0x80)
								line.end()[-5] = '-';

							WriteHex8(line, 4, disp & 0x80 ? 0-disp : disp);
						}
						break;
				}
			}
		}

		line += s;
	}

	return (uint16)(hent.mPC + oplen);
}

uint16 ATDisassembleInsn8048(VDStringA& line, const ATCPUHistoryEntry& hent, bool showCodeBytes, bool lowercaseOps) {
	const uint8 op0 = hent.mOpcode[0];
	const MCS48Insn *insn = &kMCS48Insns[op0];
	unsigned oplen = 1;

	const char *s = insn->s;
	if (!s)
		oplen = 1;
	else {
		oplen += kMCS48TokenModeBytes[insn->token];
	}

	if (showCodeBytes) {
		line.append(20, ' ');
		WriteHex16(line, 20, hent.mPC);
		line.end()[-16] = ':';
		WriteHex8(line, 14, op0);

		if (oplen >= 2) {
			WriteHex8(line, 11, hent.mOpcode[1]);

			if (oplen >= 3) {
				WriteHex8(line, 8, hent.mOpcode[2]);

				if (oplen >= 4) {
					WriteHex8(line, 5, hent.mOpcode[3]);
				}
			}
		}
	} else {
		line.append(8, ' ');
		WriteHex16(line, 8, hent.mPC);
		line.end()[-4] = ':';
	}

	if (!s) {
		line.append(lowercaseOps ? "defb    " : "DEFB    ");
		AppendIntelHex8(line, op0, lowercaseOps);
	} else {
		const size_t firstLen = insn->prefixLen;
		const char *split = (const char *)memchr(s, ' ', firstLen);

		const auto startLen = line.size();

		if (split) {
			size_t splitLen = (size_t)(split - s);
			line.append(s, s + splitLen);
			line.append(splitLen < 8 ? 8 - splitLen : 1, ' ');
			line.append(s + splitLen + 1, s + firstLen);
		} else {
			line.append(s, s + firstLen);
		}

		if (lowercaseOps) {
			std::transform(line.begin() + startLen, line.end(), line.begin() + startLen,
				[](char c) { return (unsigned)((unsigned)c - 0x41) < 26 ? (char)(c + 0x20) : c; });
		}

		s += firstLen;

		if (insn->token) {
			s += 2;

			switch(insn->token) {
				case kATMCS48TokenMode_None:
					break;

				case kATMCS48TokenMode_PCAbs8:
					AppendIntelHex16(line, (uint16)(((hent.mPC + 1) & 0xF00) + hent.mOpcode[1]), lowercaseOps);
					break;

				case kATMCS48TokenMode_PCAbs16:
					AppendIntelHex16(line, (uint16)((hent.mPC & 0x800) + ((uint32)(hent.mOpcode[0] & 0xE0) << 3) + hent.mOpcode[1]), lowercaseOps);
					break;

				case kATMCS48TokenMode_Imm:
					AppendIntelHex8(line, hent.mOpcode[oplen - 1], lowercaseOps);
					break;
			}
		}

		line += s;
	}

	return (uint16)(hent.mPC + oplen);
}

uint16 ATDisassembleInsn6809(VDStringA& line, const ATCPUHistoryEntry& hent, bool decodeRefsHistory, bool showCodeBytes, bool lowercaseOps) {
	using AT6809Dsm::Insn;

	const uint8 insnBuf[5]={
		hent.mOpcode[0],
		hent.mOpcode[1],
		hent.mOpcode[2],
		hent.mOpcode[3],
		hent.mB,
	};

	const size_t lineStart = line.size();

	const uint8 op0 = insnBuf[0];
	const uint8 *arg = &insnBuf[1];
	const Insn *insn;
	unsigned oplen = 1;

	if (op0 == 0x10) {
		insn = &AT6809Dsm::kInsns10[insnBuf[1]];
		++arg;
		++oplen;
	} else if (op0 == 0x11) {
		insn = &AT6809Dsm::kInsns11[insnBuf[1]];
		++arg;
		++oplen;
	} else
		insn = &AT6809Dsm::kInsns[op0];

	// decode length by addressing mode
	switch(insn->mMode) {
		case AT6809Dsm::i:		// immediate
		case AT6809Dsm::r:		// relative
		case AT6809Dsm::d:		// direct
		case AT6809Dsm::m:		// register mask (push/pull)
		case AT6809Dsm::t:		// register transfer (TFR)
			++oplen;
			break;

		case AT6809Dsm::il:		// immediate 16-bit
		case AT6809Dsm::rl:		// relative 16-bit
		case AT6809Dsm::e:		// extended
			oplen += 2;
			break;

		case AT6809Dsm::x:		// indexed
			switch(arg[0] & 0b0'10011111) {
				case 0b1'00'01000:		// 8-bit offset
				case 0b1'00'01100:		// 8-bit PC relative
					oplen += 2;
					break;

				case 0b1'00'01001:		// 16-bit register relative
				case 0b1'00'01101:		// 16-bit PC relative
					oplen += 3;
					break;

				default:
					++oplen;
					break;
			}
			break;
	}

	const char *s = AT6809Dsm::kNames[insn->mName];

	if (showCodeBytes) {
		line.append(24, ' ');
		WriteHex16(line, 24, hent.mPC);
		line.end()[-20] = ':';
		WriteHex8(line, 18, insnBuf[0]);

		if (oplen >= 2) {
			WriteHex8(line, 16, insnBuf[1]);

			if (oplen >= 3) {
				WriteHex8(line, 14, insnBuf[2]);

				if (oplen >= 4) {
					WriteHex8(line, 12, insnBuf[3]);

					if (oplen >= 5)
						WriteHex8(line, 10, insnBuf[4]);
				}
			}
		}
	} else {
		line.append(8, ' ');
		WriteHex16(line, 8, hent.mPC);
		line.end()[-4] = ':';
	}

	if (!s) {
		line.append(lowercaseOps ? "fcb     $  " : "FCB     $  ");
		WriteHex8(line, 2, op0);
	} else {
		const auto startLen = line.size();

		line.append(s);

		if (lowercaseOps) {
			std::transform(line.begin() + startLen, line.end(), line.begin() + startLen,
				[](char c) { return (unsigned)((unsigned)c - 0x41) < 26 ? (char)(c + 0x20) : c; });
		} else

		if (insn->mMode) {
			sint32 ea = -1;
			line.append(8-strlen(s), ' ');

			switch(insn->mMode) {
				case AT6809Dsm::i:
					line.append_sprintf("#$%02X", arg[0]);
					break;

				case AT6809Dsm::il:
					line.append_sprintf("#$%04X", VDReadUnalignedBEU16(arg));
					break;

				case AT6809Dsm::d:
					line.append_sprintf("$%02X", arg[0]);
					if (hent.mK)
						ea = ((uint32)hent.mK << 8) + arg[0];
					break;

				case AT6809Dsm::e:
					line.append_sprintf("$%04X", VDReadUnalignedBEU16(arg));
					break;

				case AT6809Dsm::r:
					line.append_sprintf("$%04X", hent.mPC + oplen + (sint8)arg[0]);
					break;

				case AT6809Dsm::rl:
					line.append_sprintf("$%04X", (hent.mPC + oplen + VDReadUnalignedBEU16(arg)) & 0xFFFF);
					break;
				case AT6809Dsm::x: {
					const int regIdx = (arg[0] >> 5) & 3;
					const char reg = "XYUS"[regIdx];
					uint32 rval = 0;

					switch(regIdx) {
						case 0:
							rval = hent.mX + ((uint32)hent.mExt.mXH << 8);
							break;
						case 1:
							rval = hent.mY + ((uint32)hent.mExt.mYH << 8);
							break;
						case 2:
							rval = hent.mD;
							break;
						case 3:
							rval = hent.mS + ((uint32)hent.mExt.mSH << 8);
							break;
					}

					if (arg[0] < 0x80) {
						int offset = ((arg[0] & 0x1F) - 0x10) ^ -0x10;
						line.append_sprintf("%d,%c", offset, reg);
						ea = (rval + offset) & 0xffff;
					} else {
						switch(arg[0] & 0x1F) {
							case 0b000'00000:
								line.append_sprintf(",%c+", reg);
								ea = rval;
								break;
							case 0b000'00001:
								line.append_sprintf(",%c++", reg);
								ea = rval;
								break;
							case 0b000'00010:
								line.append_sprintf(",-%c", reg);
								ea = (rval - 1) & 0xffff;
								break;
							case 0b000'00011:
								line.append_sprintf(",--%c", reg);
								ea = (rval - 2) & 0xffff;
								break;
							case 0b000'00100:
								line.append_sprintf(",%c", reg);
								ea = rval;
								break;
							case 0b000'00101:
								line.append_sprintf("B,%c", reg);
								ea = rval + hent.mExt.mAH;
								break;
							case 0b000'00110:
								line.append_sprintf("A,%c", reg);
								ea = rval + hent.mA;
								break;
							case 0b000'01000:
								line.append_sprintf("%d,%c", (sint8)arg[1], reg);
								ea = (rval + (sint8)arg[1]) & 0xffff;
								break;
							case 0b000'01001:
								line.append_sprintf("$%04X,%c", VDReadUnalignedBEU16(arg+1), reg);
								ea = (rval + VDReadUnalignedBEU16(arg+1)) & 0xffff;
								break;
							case 0b000'01011:
								line.append_sprintf("D,%c", reg);
								ea = (rval + hent.mA + ((uint32)hent.mExt.mAH << 8)) & 0xffff;
								break;
							case 0b000'01100:
								line.append_sprintf("%d,PCR", (sint8)arg[1]);
								ea = ((sint8)rval + hent.mPC + oplen) & 0xffff;
								break;
							case 0b000'01101:
								line.append_sprintf("$%04X,PCR", VDReadUnalignedBEU16(arg+1));
								ea = (VDReadUnalignedBEU16(arg+1) + hent.mPC + oplen) & 0xffff;
								break;
							case 0b000'10001:
								line.append_sprintf("[,%c++]", reg);
								break;
							case 0b000'10011:
								line.append_sprintf("[,--%c]", reg);
								break;
							case 0b000'10100:
								line.append_sprintf("[,%c]", reg);
								break;
							case 0b000'10101:
								line.append_sprintf("[B,%c]", reg);
								break;
							case 0b000'10110:
								line.append_sprintf("[A,%c]", reg);
								break;
							case 0b000'11000:
								line.append_sprintf("[%d,%c]", (sint8)arg[1], reg);
								break;
							case 0b000'11001:
								line.append_sprintf("[$%04X,%c]", VDReadUnalignedBEU16(arg+1), reg);
								break;
							case 0b000'11011:
								line.append_sprintf("[D,%c]", reg);
								break;
							case 0b000'11100:
								line.append_sprintf("[%d,PCR]", (sint8)arg[1]);
								break;
							case 0b000'11101:
								line.append_sprintf("[$%04X,PCR]", VDReadUnalignedBEU16(arg+1));
								break;
							case 0b000'11111:
								line.append_sprintf("[$%04X]", VDReadUnalignedBEU16(arg+1));
								break;
							default:
								line.append_sprintf("<invalid:$%02X>", arg[0]);
								break;
						}
					}
					break;
				}

				case AT6809Dsm::m: {
					static constexpr const char *const kRegisters[8] = {
						"CCR", "A", "B", "DPR", "X", "Y", "U", "PC"
					};

					bool first = true;
					const uint8 regMask = arg[0];
					for(int i=0; i<8; ++i) {
						const int j = op0 & 1 ? i^7 : i;
						if (!(regMask & (1 << j)))
							continue;

						const char *reg = kRegisters[j];

						// check for PSHU/POPU needing to push/pop S instead of U
						if (j == 1 && (op0 & 2))
							reg = "S";

						if (first)
							first = false;
						else
							line += ',';

						line.append(reg);
					}

					break;
				}

				case AT6809Dsm::t: {
					static constexpr const char *const kTransferRegisters[16]={
						"D", "X", "Y", "U", "S", "PC", "6", "7",
						"A", "B", "CCR", "DPR", "12", "13", "14", "15"
					};

					line.append_sprintf("%s,%s", kTransferRegisters[arg[0] >> 4], kTransferRegisters[arg[0] & 15]);
					break;
				}
			}

			if (decodeRefsHistory && ea >= 0) {
				line.append(2 + 42 - std::min<size_t>(42, line.size() - lineStart), ' ');
				line.append_sprintf(";$%04X", ea);
			}
		}
	}

	return (uint16)(hent.mPC + oplen);
}

uint16 ATDisassembleInsn(VDStringA& line,
	IATDebugTarget *target,
	ATDebugDisasmMode disasmMode,
	const ATCPUHistoryEntry& hent,
	bool decodeReferences,
	bool decodeRefsHistory,
	bool showPCAddress,
	bool showCodeBytes,
	bool showLabels,
	bool lowercaseOps,
	bool wideOpcode,
	bool showLabelNamespaces,
	bool showSymbols,
	bool showGlobalPC)
{
	if (disasmMode == kATDebugDisasmMode_8048)
		return ATDisassembleInsn8048(line, hent, showCodeBytes, lowercaseOps);

	if (disasmMode == kATDebugDisasmMode_Z80)
		return ATDisassembleInsnZ80(line, hent, showCodeBytes, lowercaseOps);

	if (disasmMode == kATDebugDisasmMode_6809)
		return ATDisassembleInsn6809(line, hent, decodeRefsHistory, showCodeBytes, lowercaseOps);

	ATCPUSubMode subMode = kATCPUSubMode_6502;

	switch(disasmMode) {
		case kATDebugDisasmMode_6502:
		default:
			subMode = kATCPUSubMode_6502;
			break;

		case kATDebugDisasmMode_65C02:
			subMode = kATCPUSubMode_65C02;
			break;

		case kATDebugDisasmMode_65C816:
			if (hent.mbEmulation)
				subMode = kATCPUSubMode_65C816_Emulation;
			else switch(hent.mP & (AT6502::kFlagM | AT6502::kFlagX)) {
				case 0:
				default:
					subMode = kATCPUSubMode_65C816_NativeM16X16;
					break;
				case AT6502::kFlagM:
					subMode = kATCPUSubMode_65C816_NativeM8X16;
					break;
				case AT6502::kFlagX:
					subMode = kATCPUSubMode_65C816_NativeM16X8;
					break;
				case AT6502::kFlagM | AT6502::kFlagX:
					subMode = kATCPUSubMode_65C816_NativeM8X8;
					break;
			}
			break;
	}

	const uint8 opcode = hent.mOpcode[0];
	const uint8 byte1 = hent.mOpcode[1];
	const uint8 byte2 = hent.mOpcode[2];
	const uint8 byte3 = hent.mOpcode[3];

	const bool is816 = (disasmMode == kATDebugDisasmMode_65C816);
	const uint8 pbk = is816 ? hent.mK : 0;
	const uint32 d = hent.mD;
	const uint32 dpmask = !hent.mbEmulation || (uint8)d ? 0xffff : 0xff;
	const uint32 x = hent.mX + (is816 ? (uint32)hent.mExt.mXH << 8 : 0);
	const uint32 y = hent.mY + (is816 ? (uint32)hent.mExt.mYH << 8 : 0);
	const uint32 s16 = ((uint32)hent.mExt.mSH << 8) + hent.mS;
	
	const uint8 (*const tbl)[2] = kModeTbl[subMode];
	const uint8 mode = tbl[opcode][0];
	const uint8 opid = tbl[opcode][1];
	const uint16 addr = hent.mPC;

	uint32 xpc = addr;

	if (showGlobalPC && disasmMode == kATDebugDisasmMode_6502) {
		if (hent.mGlobalPCBase)
			xpc = hent.mGlobalPCBase + hent.mPC;

		if (showPCAddress) {
			if ((xpc & kATAddressSpaceMask) == kATAddressSpace_PORTB) {
				static const char kXPCTemplate[]=" 00'0000: ";

				line += kXPCTemplate;
				WriteHex8(line, 9, (uint8)(xpc >> 16));
				WriteHex16(line, 6, (uint16)(xpc & 0xFFFF));
			} else if ((xpc & kATAddressSpaceMask) == kATAddressSpace_CB) {
				static const char kCBPCTemplate[]="t:00'0000: ";

				line += kCBPCTemplate;
				WriteHex8(line, 9, (uint8)(xpc >> 16));
				WriteHex16(line, 6, (uint16)(xpc & 0xFFFF));
			} else {
				const auto addrStart = line.size();

				line += ATAddressGetSpacePrefix(xpc);

				const auto prefixLen = line.size() - addrStart;
				if (prefixLen < 4)
					line.append(4 - prefixLen, ' ');
				
				static const char kGPCTemplate[]="    : ";
				line += kGPCTemplate;

				// if address is above bank 0 or non-CPU address space is more than 16-bit,
				// go to 6 digits
				if (xpc >= 0x10000 && ATAddressGetSpaceSize(xpc) > 0x10000) {
					WriteHex8(line, 8, (uint8)(xpc >> 16));
				}

				WriteHex16(line, 6, (uint16)xpc);
			}
		}
	} else {
		xpc += ((uint32)pbk << 16);

		if (showPCAddress) {
			static const char kPCTemplate[]="  :    : ";

			if (disasmMode == kATDebugDisasmMode_65C816) {
				line += kPCTemplate;
				WriteHex8(line, 9, hent.mK);
			} else {
				line += kPCTemplate + 3;
			}

			WriteHex16(line, 6, hent.mPC);
		}
	}

	int opsize = kBytesPerModeTables[subMode][mode];

	if (showCodeBytes) {
		line.append(10, ' ');

		switch(opsize) {
			case 3:
				WriteHex8(line, 4, byte2);
			case 2:
				WriteHex8(line, 7, byte1);
			case 1:
				WriteHex8(line, 10, opcode);
				break;

			case 4:
				WriteHex8(line, 10, opcode);
				WriteHex8(line, 8, byte1);
				WriteHex8(line, 6, byte2);
				WriteHex8(line, 4, byte3);
				break;
		}
	}

	size_t startPos = line.size();

	const ATCPUEmulator& cpu = g_sim.GetCPU();

	if (showLabels) {
		VDStringA tempLabel;

		const char *label = NULL;
		
		label = ATGetSymbolName(xpc, false);

		if (!label && cpu.IsPathfindingEnabled() && cpu.IsPathStart(addr)) {
			tempLabel.sprintf("L%04X", addr);
			label = tempLabel.c_str();
		}

		size_t len = 0;
		
		if (label) {
			if (!showLabelNamespaces) {
				const char *dot = strchr(label, '.');

				if (dot && dot[1])
					label = dot;
			}

			len = strlen(label);
			line.append(label);
		}

		line.append(len < 7 ? (uint32)(8 - len) : 1, ' ');
	}

	const char *opname = kOpcodes[opid];
	int opnamepad = (wideOpcode ? 7 : 3) - (int)strlen(opname);
	if (lowercaseOps) {
		for(;;) {
			char c = *opname++;

			if (!c)
				break;

			line += (char)tolower((unsigned char)c);
		}
	} else
		line += opname;

	if (mode == kModeBit || mode == kModeBBit) {
		line += (char)('0' + ((opcode >> 4) & 7));
		if (opnamepad)
			--opnamepad;
	}

	while(opnamepad-- > 0)
		line += ' ';

	if (mode == kModeImm) {
		line.append(" #$FF");
		WriteHex8(line, 2, byte1);
	} else if (mode == kModeImmMode16 || mode == kModeImmIndex16) {
		if (opsize == 3)
			line.append_sprintf(" #$%02X%02X", byte2, byte1);
		else 
			line.append_sprintf(" #$%02X", byte1);
	} else if (mode == kModeImm16) {
		line.append_sprintf(" #$%02X%02X", byte2, byte1);
	} else if (mode == kModeIVec) {
		line.append_sprintf(" $%02X", byte1);
	} else if (mode == kModeMove) {
		line.append_sprintf(" $%02X,$%02X", byte2, byte1);
	} else if (mode != kModeInvalid && mode != kModeImplied) {
		line += ' ';

		switch(mode) {
			case kModeIndA:
			case kModeIndX:
			case kModeIndY:
			case kModeInd:
			case kModeIndAX:
			case kModeStackIndY:
				line += '(';
				break;
			case kModeDpIndLong:
			case kModeDpIndLongY:
			case kModeIndAL:
				line += '[';
				break;
		}

		// Determine bank to use for the base address (operand).
		//
		//	JMP abs			-> PBK
		//	JMP (abs)		-> 0
		//	JMP (abs,X)		-> PBK
		//	JML [dp]		-> 0
		//	JSR abs			-> PBK
		//	JSR (abs,X)		-> PBK
		//	All branch		-> PBK
		uint8 eaBank = 0;
		
		if (disasmMode == kATDebugDisasmMode_65C816) {
			eaBank = hent.mB;

			switch(opid) {
			case kOpcodeBCC:
			case kOpcodeBCS:
			case kOpcodeBEQ:
			case kOpcodeBMI:
			case kOpcodeBNE:
			case kOpcodeBPL:
			case kOpcodeBRA:
			case kOpcodeBVC:
			case kOpcodeBVS:
			case kOpcodeJSR:
			case kOpcodeJSL:
				eaBank = hent.mK;
				break;

			case kOpcodeJMP:
			case kOpcodeJML:
				if (mode == kModeIndA || mode == kModeIndAL)
					eaBank = 0;
				else
					eaBank = hent.mK;
				break;

			}
		}

		uint32 base;			// base address argument in instruction, i.e. addr,X -> addr
		uint32 ea;				// effective address, i.e. addr,X -> DBK:addr+X
		uint32 ea2;				// second effective address (bit branch insns only)
		bool addr16 = false;	// show operand as 16-bit (word)
		bool addr24 = false;	// show operand as 24-bit (long)
		bool ea16 = false;		// show effective address in comment as 16-bit instead of 8-bit
		bool dolabel = showSymbols;

		switch(mode) {
			case kModeRel:
				base = ea = addr + 2 + (sint8)byte1;
				addr16 = true;
				ea16 = true;
				break;

			case kModeRel16:
				base = ea = addr + 3 + (sint16)((uint32)byte1 + ((uint32)byte2 << 8));
				addr16 = true;
				ea16 = true;
				break;

			case kModeZp:
				if (d)
					dolabel = false;
			case kModeBit:
				base = byte1;
				ea = (d + byte1) & 0xffff;
				break;

			case kModeZpX:
				if (d)
					dolabel = false;

				base = byte1;
				ea = (d + ((byte1 + x) & dpmask)) & 0xffff;

				if (dpmask >= 0x100)
					ea16 = true;
				break;

			case kModeZpY:
				if (d)
					dolabel = false;

				base = byte1;
				ea = (d + ((byte1 + y) & dpmask)) & 0xffff;

				if (dpmask >= 0x100)
					ea16 = true;
				break;

			case kModeAbs:
				base = ea = byte1 + (byte2 << 8);

				if (disasmMode == kATDebugDisasmMode_65C816)
					ea += ((uint32)eaBank << 16);

				addr16 = true;
				ea16 = true;
				break;

			case kModeAbsX:
				base = byte1 + (byte2 << 8);

				if (disasmMode == kATDebugDisasmMode_65C816)
					ea = (base + x + ((uint32)hent.mB << 16)) & 0xffffff;
				else
					ea = (base + x) & 0xffff;

				addr16 = true;
				ea16 = true;
				break;

			case kModeAbsY:
				base = byte1 + (byte2 << 8);

				if (disasmMode == kATDebugDisasmMode_65C816)
					ea = (base + y + ((uint32)hent.mB << 16)) & 0xffffff;
				else
					ea = (base + y) & 0xffff;

				addr16 = true;
				ea16 = true;
				break;

			case kModeIndA:
				base = byte1 + (byte2 << 8);

				if (decodeRefsHistory)
					ea = hent.mEA;
				else
					ea = target->DebugReadByte(base) + 256*target->DebugReadByte(base+1) + ((uint32)eaBank << 16);

				addr16 = true;
				ea16 = true;
				break;

			case kModeIndAL:
				base = byte1 + (byte2 << 8);

				if (decodeRefsHistory)
					ea = hent.mEA;
				else
					ea = target->DebugReadByte(base)
						+ 256 * target->DebugReadByte(base+1)
						+ 65536 * target->DebugReadByte(base+2);

				addr16 = true;
				ea16 = true;
				break;

			case kModeIndX:
				base = byte1;

				if (decodeRefsHistory)
					ea = hent.mEA;
				else
					ea = target->DebugReadByte((uint8)(base + x)) + 256*target->DebugReadByte((uint8)(base + ((x + 1) & dpmask)));

				ea16 = true;
				break;

			case kModeIndY:
				base = byte1;

				if (decodeRefsHistory)
					ea = hent.mEA;
				else
					ea = target->DebugReadByte(d + base) + 256*target->DebugReadByte(d + ((base + 1) & dpmask)) + y;

				ea16 = true;
				break;

			case kModeInd:
				base = byte1;

				if (decodeRefsHistory)
					ea = hent.mEA;
				else
					ea = target->DebugReadByte(d + base) + 256*target->DebugReadByte(d + ((base+1) & dpmask));

				ea16 = true;
				break;

			case kModeIndAX:
				base = byte1 + (byte2 << 8);

				if (decodeRefsHistory)
					ea = hent.mEA;
				else
					ea = target->DebugReadByte(base+x) + 256*target->DebugReadByte(base+1+x);

				addr16 = true;
				ea16 = true;
				break;

			case kModeBBit:
				base = ea = byte1;
				ea2 = addr + 3 + (sint8)byte2;
				break;

			case kModeLong:
				base = ea = (uint32)byte1 + ((uint32)byte2 << 8) + ((uint32)byte3 << 16);
				addr16 = true;
				addr24 = true;
				ea16 = true;
				break;

			case kModeLongX:
				base = (uint32)byte1 + ((uint32)byte2 << 8) + ((uint32)byte3 << 16);
				ea = base + x;
				addr16 = true;
				addr24 = true;
				ea16 = true;
				break;

			case kModeStack:
				dolabel = false;
				base = byte1;
				ea = (base + s16) & 0xffff;
				break;

			case kModeStackIndY:
				dolabel = false;
				base = byte1;

				if (decodeRefsHistory)
					ea = hent.mEA;
				else {
					ea = target->DebugReadByte(base + s16);
					ea += 256 * target->DebugReadByte(base + s16+1);
					ea += y;
				}

				break;

			case kModeDpIndLong:
				if (d)
					dolabel = false;

				base = byte1;

				if (decodeRefsHistory)
					ea = hent.mEA;
				else {
					uint16 dpaddr = d + byte1;

					ea = (uint32)target->DebugReadByte(dpaddr++);
					ea += (uint32)target->DebugReadByte(dpaddr++) << 8;
					ea += (uint32)target->DebugReadByte(dpaddr) << 16;
				}
				break;

			case kModeDpIndLongY:
				if (d)
					dolabel = false;

				base = byte1;

				if (decodeRefsHistory)
					ea = hent.mEA;
				else {
					uint16 dpaddr = d + byte1;

					ea = (uint32)target->DebugReadByte(dpaddr++);
					ea += (uint32)target->DebugReadByte(dpaddr++) << 8;
					ea += (uint32)target->DebugReadByte(dpaddr) << 16;
					ea += y;
				}
				break;
		}

		bool write = false;
		switch(opid) {
		case kOpcodeASL:
		case kOpcodeDCP:
		case kOpcodeDEC:
		case kOpcodeINC:
		case kOpcodeISB:
		case kOpcodeLSR:
		case kOpcodeROL:
		case kOpcodeROR:
		case kOpcodeSAX:
		case kOpcodeSTA:
		case kOpcodeSTX:
		case kOpcodeSTY:
		case kOpcodeSTZ:
		case kOpcodeTRB:
		case kOpcodeTSB:
			write = true;
			break;
		}

		const char *name = NULL;
		sint32 offset;
		
		if (dolabel) {
			uint32 symAddr = base;

			if (!addr24)
				symAddr += (uint32)eaBank << 16;

			name = ATGetSymbolNameOffset(symAddr, write, offset);
		}

		if (name) {
			uint32 absOffset = abs(offset);
			if (!absOffset)
				line.append(name);
			else if (absOffset < 10)
				line.append_sprintf("%s%+d", name, offset);
			else
				line.append_sprintf("%s%c$%02X", name, offset < 0 ? '-' : '+', absOffset);
		} else if (addr24)
			line.append_sprintf("$%06X", base & 0xffffff);
		else if (addr16) {
			line.append(5, '$');
			WriteHex16(line, 4, (uint16)base);
		} else {
			line.append(3, '$');
			WriteHex8(line, 2, (uint8)base);
		}

		switch(mode) {
			case kModeZpX:
			case kModeAbsX:
			case kModeLongX:
				line.append(",X");
				break;

			case kModeZpY:
			case kModeAbsY:
				line.append(",Y");
				break;

			case kModeInd:
			case kModeIndA:
				line.append(")");
				break;

			case kModeIndX:
			case kModeIndAX:
				line.append(",X)");
				break;

			case kModeIndY:
				line.append("),Y");
				break;

			case kModeBBit:
				{
					const char *name2 = ATGetSymbolName(ea2, false);

					line.append(",");

					if (name2)
						line.append(name2);
					else
						line.append_sprintf("$%04X", ea2);
				}
				break;

			case kModeStack:
				line.append(",S");
				break;

			case kModeStackIndY:
				line.append(",S),Y");
				break;

			case kModeIndAL:
			case kModeDpIndLong:
				line.append("]");
				break;

			case kModeDpIndLongY:
				line.append("],Y");
				break;
		}

		if (decodeRefsHistory && ea != 0xFFFFFFFFUL) {
			switch(mode) {
				case kModeZpX:			// bank 0
				case kModeZpY:			// bank 0
				case kModeAbsX:			// DBK
				case kModeAbsY:			// DBK
				case kModeIndA:			// bank 0 -> PBK
				case kModeIndAL:		// bank 0 -> any
				case kModeIndX:			// DBK
				case kModeIndY:			// DBK
				case kModeInd:			// bank 0 -> DBK
				case kModeIndAX:		// PBK
				case kModeStackIndY:	// bank 0 -> PBK
				case kModeDpIndLong:	// bank 0 -> any
				case kModeDpIndLongY:	// bank 0 -> any
					size_t padLen = startPos + 20;

					if (line.size() < padLen)
						line.resize((uint32)padLen, ' ');

					if (disasmMode == kATDebugDisasmMode_65C816 &&
						mode != kModeZpX &&
						mode != kModeZpY)
					{
						line.append_sprintf(" ;$%02X:%04X", (ea >> 16) & 0xff, ea & 0xffff);
						break;
					}

					if (ea16)
						line.append_sprintf(" ;$%04X", ea);
					else
						line.append_sprintf(" ;$%02X", ea);
					break;
			}
		} else if (decodeReferences) {
			if (mode != kModeRel && mode != kModeRel16) {
				size_t padLen = startPos + 20;

				if (line.size() < padLen)
					line.resize((uint32)padLen, ' ');

				if (disasmMode == kATDebugDisasmMode_65C816)
					line.append_sprintf(" [$%02X:%04X]", (ea >> 16) & 0xff, ea & 0xffff);
				else if (ea16)
					line.append_sprintf(" [$%04X]", ea);
				else
					line.append_sprintf(" [$%02X]", ea);

				if (!write) {
					bool access16 = false;

					switch(kOpcodeMemoryAccessModes[opid]) {
						case kMemoryAccessMode_None:
						case kMemoryAccessMode_8:
						default:
							break;

						case kMemoryAccessMode_16:
							access16 = true;
							break;

						case kMemoryAccessMode_M:
							switch(subMode) {
								case kATCPUSubMode_65C816_NativeM16X16:
								case kATCPUSubMode_65C816_NativeM16X8:
									access16 = true;
									break;
							}
							break;

						case kMemoryAccessMode_X:
							switch(subMode) {
								case kATCPUSubMode_65C816_NativeM16X16:
								case kATCPUSubMode_65C816_NativeM8X16:
									access16 = true;
									break;
							}
							break;
					}

					if (access16) {
						switch(mode) {
							case kModeZp:
							case kModeZpX:
							case kModeZpY:
							case kModeIndA:
							case kModeStack:
								line.append_sprintf(" = $%02X%02X",
									target->DebugReadByte((ea+1) & 0xffff),
									target->DebugReadByte(ea));
								break;

							default:
								line.append_sprintf(" = $%04X"
									, target->DebugReadByte(ea & 0xffffff) + 256*target->DebugReadByte((ea + 1) & 0xffffff)
									);
								break;
						}
					} else
						line.append_sprintf(" = $%02X", target->DebugReadByte(ea));
				}
			}
		}
	}

	return addr + opsize;
}

uint16 ATDisassembleInsn(uint16 addr, uint8 bank) {
	ATCPUHistoryEntry hent;
	ATDisassembleCaptureRegisterContext(hent);
	ATDisassembleCaptureInsnContext(addr, bank, hent);

	const ATCPUEmulator& cpu = g_sim.GetCPU();

	VDStringA buf;
	addr = ATDisassembleInsn(buf, g_sim.GetDebugTarget(), cpu.GetDisasmMode(), hent, true, false, true, true, true);
	buf += '\n';
	ATConsoleWrite(buf.c_str());

	return addr;
}

uint16 ATDisassembleGetFirstAnchor(IATDebugTarget *target, uint16 addr, uint16 targetAddr, uint32 addrBank) {
	ATCPUSubMode subMode = kATCPUSubMode_6502;

	const auto disasmMode = target->GetDisasmMode();

	switch(disasmMode) {
		case kATDebugDisasmMode_6502:
		default:
			subMode = kATCPUSubMode_6502;
			break;

		case kATDebugDisasmMode_65C02:
			subMode = kATCPUSubMode_65C02;
			break;

		case kATDebugDisasmMode_65C816:
			{
				ATCPUExecState execState;
				target->GetExecState(execState);

				if (execState.m6502.mbEmulationFlag)
					subMode = kATCPUSubMode_65C816_Emulation;
				else switch(execState.m6502.mP & (AT6502::kFlagM | AT6502::kFlagX)) {
					case 0:
					default:
						subMode = kATCPUSubMode_65C816_NativeM16X16;
						break;
					case AT6502::kFlagM:
						subMode = kATCPUSubMode_65C816_NativeM8X16;
						break;
					case AT6502::kFlagX:
						subMode = kATCPUSubMode_65C816_NativeM16X8;
						break;
					case AT6502::kFlagM | AT6502::kFlagX:
						subMode = kATCPUSubMode_65C816_NativeM8X8;
						break;
				}
			}
			break;
	}

	const uint8 (*const tbl)[2] = kModeTbl[subMode];
	const uint8 *const modetbl = kBytesPerModeTables[subMode];

	vdfastvector<uint8> results;

	uint16 testbase = addr;
	for(int i=0; i<4; ++i) {
		uint16 ip = testbase;
		for(;;) {
			if (ip == targetAddr)
				return testbase;

			uint32 offset = (uint16)(ip - addr);
			if (offset < results.size() && results[offset])
				break;

			uint8 opcode = target->DebugReadByte(ip + addrBank);
			uint8 mode = tbl[opcode][0];

			uint8 oplen = modetbl[mode];
			if (mode == kModeInvalid || (uint16)(targetAddr - ip) < oplen) {
				if (offset >= results.size())
					results.resize(offset+1, false);
				results[offset] = true;
				break;
			}

			ip += oplen;
		}

		++testbase;
		if (testbase == targetAddr)
			break;
	}

	return testbase;
}

void ATDisassemblePredictContext(ATCPUHistoryEntry& hent, ATDebugDisasmMode execMode) {
	if (execMode == kATDebugDisasmMode_65C816) {
		switch(hent.mOpcode[0]) {
			case 0x18:	// CLC
				hent.mP &= ~AT6502::kFlagC;
				break;

			case 0x38:	// SEC
				hent.mP |= AT6502::kFlagC;
				break;

			case 0xC2:	// REP
				if (hent.mbEmulation)
					hent.mP &= ~hent.mOpcode[1] | 0x30;
				else
					hent.mP &= ~hent.mOpcode[1];
				break;

			case 0xE2:	// SEP
				if (hent.mbEmulation)
					hent.mP |= hent.mOpcode[1] & 0xcf;
				else
					hent.mP |= hent.mOpcode[1];
				break;

			case 0xFB:	// XCE
				{
					uint8 e = hent.mbEmulation ? 1 : 0;
					uint8 xorv = (hent.mP ^ e) & 1;

					if (xorv) {
						e ^= xorv;
						hent.mP ^= xorv;

						hent.mbEmulation = (e & 1) != 0;

						if (hent.mbEmulation)
							hent.mP |= 0x30;
					}
				}
				break;
		}
	}
}

int ATGetOpcodeLength(uint8 opcode) {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	ATCPUSubMode subMode = cpu.GetCPUSubMode();
	const uint8 (*const tbl)[2] = kModeTbl[subMode];

	return kBytesPerModeTables[subMode][tbl[opcode][0]];
}

int ATGetOpcodeLength(uint8 opcode, uint8 p, bool emuMode) {
	ATCPUEmulator& cpu = g_sim.GetCPU();

	return ATGetOpcodeLength(opcode, p, emuMode, cpu.GetDisasmMode());
}

int ATGetOpcodeLength(uint8 opcode, uint8 p, bool emuMode, ATDebugDisasmMode disasmMode) {
	ATCPUSubMode subMode;

	switch(disasmMode) {
		case kATDebugDisasmMode_6502:
		default:
			subMode = kATCPUSubMode_6502;
			break;

		case kATDebugDisasmMode_65C02:
			subMode = kATCPUSubMode_65C02;
			break;

		case kATDebugDisasmMode_65C816:
			subMode = kATCPUSubMode_65C816_Emulation;
			if (!emuMode)
				subMode = (ATCPUSubMode)(kATCPUSubMode_65C816_NativeM16X16 + ((p >> 4) & 3));

			break;
	}

	const uint8 (*const tbl)[2] = kModeTbl[subMode];

	return kBytesPerModeTables[subMode][tbl[opcode][0]];
}

bool ATIsValidOpcode(uint8 opcode) {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	ATCPUSubMode subMode = cpu.GetCPUSubMode();
	const uint8 (*const tbl)[2] = kModeTbl[subMode];

	return tbl[opcode][1] != kOpcodebad;
}

uint32 ATGetOpcodeLengthZ80(uint8 opcode) {
	return kZ80Insns[opcode].GetOpcodeLen();
}

uint32 ATGetOpcodeLengthZ80ED(uint8 opcode) {
	return kZ80InsnsED[opcode].GetOpcodeLen();
}

uint32 ATGetOpcodeLengthZ80CB(uint8 opcode) {
	return kZ80InsnsCB[opcode].GetOpcodeLen();
}

uint32 ATGetOpcodeLengthZ80DDFD(uint8 opcode) {
	return kZ80InsnsDDFD[opcode].GetOpcodeLen();
}

uint32 ATGetOpcodeLengthZ80DDFDCB(uint8 opcode) {
	return kZ80InsnsDDFDCB[opcode].GetOpcodeLen();
}
