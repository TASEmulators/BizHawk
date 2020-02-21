//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include <vd2/system/hash.h>
#include "debugger.h"
#include "console.h"

class ATDebuggerCmdAssemble : public vdrefcounted<IATDebuggerActiveCommand> {
public:
	ATDebuggerCmdAssemble(uint32 address);

	virtual bool IsBusy() const { return false; }
	virtual const char *GetPrompt();
	virtual void BeginCommand(IATDebugger *debugger);
	virtual void EndCommand();
	virtual bool ProcessSubCommand(const char *s);

protected:
	struct OperandInfo {
		sint32	mOpValue;
		uint32	mOpModes;
		int		mPostIncX;
		int		mPostIncY;
	};

	struct OperandInfoImplied : public OperandInfo {
		OperandInfoImplied() {
			mOpValue = 0;
			mOpModes = kModeFlag_Imp;
			mPostIncX = 0;
			mPostIncY = 0;
		}
	};

	struct OperandInfoImm : public OperandInfo {
		OperandInfoImm(uint8 imm) {
			mOpValue = imm;
			mOpModes = kModeFlag_Imm;
			mPostIncX = 0;
			mPostIncY = 0;
		}
	};

	struct OperandInfoBranchTarget : public OperandInfo {
		OperandInfoBranchTarget(uint32 addr) {
			mOpValue = addr;
			mOpModes = kModeFlag_Rel | kModeFlag_RelLong;
			mPostIncX = 0;
			mPostIncY = 0;
		}
	};

	struct OpcodeEntry {
		uint8 mEncoding;
		uint8 mFlags;
	};

	const char *ParseOperand(const char *s, OperandInfo& opinfo);
	const char *ParseExpression(const char *s, sint32& value);

	void MakeOperandShiftable(const char *loc, OperandInfo& opinfo, uint32 len);
	void ShiftOperand(const OperandInfo& opinfo, OperandInfo& opdst, uint32 offset);
	void EmitCompositeINCDEC(const char *loc, const OpcodeEntry *opcode, const OperandInfo& opinfo, uint32 len);
	const char *EmitCompositeADx(const char *loc, const OpcodeEntry *flagsOpcode, const OpcodeEntry *aluOpcode, uint32 len);
	const char *EmitCompositeMVx(const char *loc, const OpcodeEntry *loadOpcode, const OpcodeEntry *storeOpcode);
	const char *EmitCompositeMWx(const char *loc, const OpcodeEntry *loadOpcode, const OpcodeEntry *storeOpcode);
	const char *EmitCompositeCPx(const char *loc, uint32 len);
	void EmitOpcode(const char *loc, const OpcodeEntry *opcodes, const OperandInfo& opinfo, bool allowImpliedFallback, bool doPostIncrements = true);
	void EmitPostIncrements(const OperandInfo& opinfo, bool do2x);

	static bool IsIdentStart(char c);
	static bool IsIdentNext(char c);

	struct ParseException {
		ParseException(const char *pos, const char *msg = NULL) : mpPos(pos), mpMsg(msg) {}

		const char *const mpPos;
		const char *const mpMsg;
	};

	struct HashedStringSpanAI : public VDStringSpanA {
		HashedStringSpanAI()
			: VDStringSpanA()
			, mHash(VDHashString32I(mpBegin, (uint32)(mpEnd - mpBegin)))
		{
		}

		HashedStringSpanAI(const char *s)
			: VDStringSpanA(s)
			, mHash(VDHashString32I(mpBegin, (uint32)(mpEnd - mpBegin)))
		{
		}

		HashedStringSpanAI(const char *s, const char *t)
			: VDStringSpanA(s, t)
			, mHash(VDHashString32I(s, (uint32)(t-s)))
		{
		}

		HashedStringSpanAI(const VDStringSpanA& sp)
			: VDStringSpanA(sp)
			, mHash(VDHashString32I(mpBegin, (uint32)(mpEnd - mpBegin)))
		{
		}

		const uint32 mHash;
	};

	struct HashedStringSpanHash {
		bool operator()(const HashedStringSpanAI& s, const HashedStringSpanAI& t) const {
			return s.mHash == t.mHash && s.comparei(t) == 0;
		}

		size_t operator()(const HashedStringSpanAI& s) const {
			return s.mHash;
		}
	};

	enum {
		kModeImp		= 0x00,		// none/A
		kModeImm		= 0x01,		// #imm
		kModeIVec		= 0x02,		// ivec
		kModeZp			= 0x03,		// zp
		kModeZpX		= 0x04,		// zp,X
		kModeZpY		= 0x05,		// zp,Y
		kModeZpI		= 0x06,		// (zp)
		kModeAbs		= 0x07,		// abs
		kModeAbsX		= 0x08,		// abs,X
		kModeAbsY		= 0x09,		// abs,Y
		kModeAbsI		= 0x0A,		// (abs)
		kModeAbsIX		= 0x0B,		// (abs,X)
		kModeIndX		= 0x0C,		// (zp,X)
		kModeIndY		= 0x0D,		// (zp),Y
		kModeRel		= 0x0E,		// branch target
		kModeAbsLong	= 0x0F,		// al
		kModeAbsLongX	= 0x10,		// al,X
		kModeIndLong	= 0x11,		// [dp]
		kModeIndLongY	= 0x12,		// [dp],Y
		kModeStack		= 0x13,		// dp,S
		kModeStackIndY	= 0x14,		// (dp,S),Y
		kModeRelLong	= 0x15,		// branch target (16-bit)
		kModeAbsLongI	= 0x16,		// [abs]
		kModeMove		= 0x17,		// src,dest
		kModeMask		= 0x1F,

		kFlagNone	= 0x00,
		kFlagEnd	= 0x80,
		kFlagAll	= 0xFF
	};

	enum : uint32 {
		kModeFlag_Imp		= (uint32)1 << kModeImp,
		kModeFlag_Imm		= (uint32)1 << kModeImm,
		kModeFlag_IVec		= (uint32)1 << kModeIVec,
		kModeFlag_Zp		= (uint32)1 << kModeZp,
		kModeFlag_ZpX		= (uint32)1 << kModeZpX,
		kModeFlag_ZpY		= (uint32)1 << kModeZpY,
		kModeFlag_ZpI		= (uint32)1 << kModeZpI,
		kModeFlag_Abs		= (uint32)1 << kModeAbs,
		kModeFlag_AbsX		= (uint32)1 << kModeAbsX,
		kModeFlag_AbsY		= (uint32)1 << kModeAbsY,
		kModeFlag_AbsI		= (uint32)1 << kModeAbsI,
		kModeFlag_AbsIX		= (uint32)1 << kModeAbsIX,
		kModeFlag_IndX		= (uint32)1 << kModeIndX,
		kModeFlag_IndY		= (uint32)1 << kModeIndY,
		kModeFlag_Rel		= (uint32)1 << kModeRel,
		kModeFlag_AbsLong	= (uint32)1 << kModeAbsLong,
		kModeFlag_AbsLongX	= (uint32)1 << kModeAbsLongX,
		kModeFlag_IndLong	= (uint32)1 << kModeIndLong,
		kModeFlag_IndLongY	= (uint32)1 << kModeIndLongY,
		kModeFlag_Stack		= (uint32)1 << kModeStack,
		kModeFlag_StackIndY	= (uint32)1 << kModeStackIndY,
		kModeFlag_RelLong	= (uint32)1 << kModeRelLong,
		kModeFlag_AbsLongI	= (uint32)1 << kModeAbsLongI,
		kModeFlag_Move		= (uint32)1 << kModeMove,
	};

	enum {
		kPsuedoOp_None,
		kPsuedoOp_REQ,
		kPsuedoOp_RNE,
		kPsuedoOp_RPL,
		kPsuedoOp_RMI,
		kPsuedoOp_RCC,
		kPsuedoOp_RCS,
		kPsuedoOp_RVC,
		kPsuedoOp_RVS,
		kPsuedoOp_SEQ,
		kPsuedoOp_SNE,
		kPsuedoOp_SPL,
		kPsuedoOp_SMI,
		kPsuedoOp_SCC,
		kPsuedoOp_SCS,
		kPsuedoOp_SVC,
		kPsuedoOp_SVS,
		kPsuedoOp_JEQ,
		kPsuedoOp_JNE,
		kPsuedoOp_JPL,
		kPsuedoOp_JMI,
		kPsuedoOp_JCC,
		kPsuedoOp_JCS,
		kPsuedoOp_JVC,
		kPsuedoOp_JVS,
		kPsuedoOp_ADD,
		kPsuedoOp_SUB,
		kPsuedoOp_PHR,
		kPsuedoOp_PLR,
		kPsuedoOp_INW,
		kPsuedoOp_INL,
		kPsuedoOp_IND,
		kPsuedoOp_DEW,
		kPsuedoOp_DEL,
		kPsuedoOp_DED,

		kPsuedoOp_OperandParsingLimit,

		kPsuedoOp_ADB,
		kPsuedoOp_SBB,
		kPsuedoOp_ADW,
		kPsuedoOp_SBW,
		kPsuedoOp_MVA,
		kPsuedoOp_MVX,
		kPsuedoOp_MVY,
		kPsuedoOp_MWA,
		kPsuedoOp_MWX,
		kPsuedoOp_MWY,
		kPsuedoOp_CPB,
		kPsuedoOp_CPW,
		kPsuedoOp_CPL,
		kPsuedoOp_CPD,

		kPsuedoOp_ORG,
		kPsuedoOp_LONGA,
		kPsuedoOp_LONGI
	};

	struct CommandEntry {
		const char *mpName;
		const OpcodeEntry *mpOpcodes;
		uint8 mPsuedoOp;
	};

	IATDebugger *mpDebugger;
	uint32	mAddress;
	bool	mbLongM;
	bool	mbLongX;
	VDStringA	mPrompt;

	vdfastvector<uint8> mEmittedBytes;

	typedef vdhashmap<HashedStringSpanAI, const CommandEntry *, HashedStringSpanHash, HashedStringSpanHash> CommandLookup;
	CommandLookup mCommandLookup;

	static const CommandEntry kCommands[];

#define X_OPCODE_DECL(name) static const ATDebuggerCmdAssemble::OpcodeEntry kOpcodes_##name[];
	X_OPCODE_DECL(ADC)
	X_OPCODE_DECL(AND)
	X_OPCODE_DECL(ASL)
	X_OPCODE_DECL(BCC)
	X_OPCODE_DECL(BCS)
	X_OPCODE_DECL(BEQ)
	X_OPCODE_DECL(BIT)
	X_OPCODE_DECL(BMI)
	X_OPCODE_DECL(BNE)
	X_OPCODE_DECL(BPL)
	X_OPCODE_DECL(BRA)
	X_OPCODE_DECL(BRK)
	X_OPCODE_DECL(BRL)
	X_OPCODE_DECL(BVC)
	X_OPCODE_DECL(BVS)
	X_OPCODE_DECL(CLC)
	X_OPCODE_DECL(CLD)
	X_OPCODE_DECL(CLI)
	X_OPCODE_DECL(CLV)
	X_OPCODE_DECL(CMP)
	X_OPCODE_DECL(COP)
	X_OPCODE_DECL(CPX)
	X_OPCODE_DECL(CPY)
	X_OPCODE_DECL(DEA)
	X_OPCODE_DECL(DEC)
	X_OPCODE_DECL(DEX)
	X_OPCODE_DECL(DEY)
	X_OPCODE_DECL(EOR)
	X_OPCODE_DECL(INA)
	X_OPCODE_DECL(INC)
	X_OPCODE_DECL(INX)
	X_OPCODE_DECL(INY)
	X_OPCODE_DECL(JML)
	X_OPCODE_DECL(JMP)
	X_OPCODE_DECL(JSL)
	X_OPCODE_DECL(JSR)
	X_OPCODE_DECL(LDA)
	X_OPCODE_DECL(LDX)
	X_OPCODE_DECL(LDY)
	X_OPCODE_DECL(LSR)
	X_OPCODE_DECL(MVN)
	X_OPCODE_DECL(MVP)
	X_OPCODE_DECL(NOP)
	X_OPCODE_DECL(ORA)
	X_OPCODE_DECL(PEA)
	X_OPCODE_DECL(PEI)
	X_OPCODE_DECL(PER)
	X_OPCODE_DECL(PHA)
	X_OPCODE_DECL(PHB)
	X_OPCODE_DECL(PHD)
	X_OPCODE_DECL(PHK)
	X_OPCODE_DECL(PHP)
	X_OPCODE_DECL(PHX)
	X_OPCODE_DECL(PHY)
	X_OPCODE_DECL(PLA)
	X_OPCODE_DECL(PLB)
	X_OPCODE_DECL(PLD)
	X_OPCODE_DECL(PLP)
	X_OPCODE_DECL(PLX)
	X_OPCODE_DECL(PLY)
	X_OPCODE_DECL(REP)
	X_OPCODE_DECL(ROL)
	X_OPCODE_DECL(ROR)
	X_OPCODE_DECL(RTI)
	X_OPCODE_DECL(RTL)
	X_OPCODE_DECL(RTS)
	X_OPCODE_DECL(SBC)
	X_OPCODE_DECL(SEC)
	X_OPCODE_DECL(SED)
	X_OPCODE_DECL(SEI)
	X_OPCODE_DECL(SEP)
	X_OPCODE_DECL(STA)
	X_OPCODE_DECL(STP)
	X_OPCODE_DECL(STX)
	X_OPCODE_DECL(STY)
	X_OPCODE_DECL(STZ)
	X_OPCODE_DECL(SWA)
	X_OPCODE_DECL(TAD)
	X_OPCODE_DECL(TAS)
	X_OPCODE_DECL(TAX)
	X_OPCODE_DECL(TAY)
	X_OPCODE_DECL(TCD)
	X_OPCODE_DECL(TCS)
	X_OPCODE_DECL(TDA)
	X_OPCODE_DECL(TDC)
	X_OPCODE_DECL(TRB)
	X_OPCODE_DECL(TSB)
	X_OPCODE_DECL(TSA)
	X_OPCODE_DECL(TSC)
	X_OPCODE_DECL(TSX)
	X_OPCODE_DECL(TXA)
	X_OPCODE_DECL(TXS)
	X_OPCODE_DECL(TXY)
	X_OPCODE_DECL(TYA)
	X_OPCODE_DECL(TYX)
	X_OPCODE_DECL(WAI)
	X_OPCODE_DECL(WDM)
	X_OPCODE_DECL(XBA)
	X_OPCODE_DECL(XCE)
};

//////////////////////////////////////////////////////////////////////////

#define X_OPCODE_BEGIN(name) const ATDebuggerCmdAssemble::OpcodeEntry ATDebuggerCmdAssemble::kOpcodes_##name[]={
#define X_OPCODE_END() { 0, kFlagEnd } };

X_OPCODE_BEGIN(AND)
	{ 0x29, kModeImm },
	{ 0x25, kModeZp },
	{ 0x35, kModeZpX },
	{ 0x2D, kModeAbs },
	{ 0x3D, kModeAbsX },
	{ 0x39, kModeAbsY },
	{ 0x21, kModeIndX },
	{ 0x31, kModeIndY },
	{ 0x32, kModeZpI },
	{ 0x23, kModeStack },
	{ 0x33, kModeStackIndY },
	{ 0x27, kModeIndLong },
	{ 0x37, kModeIndLongY },
	{ 0x2F, kModeAbsLong },
	{ 0x3F, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(ADC)
	{ 0x69, kModeImm },
	{ 0x65, kModeZp },
	{ 0x75, kModeZpX },
	{ 0x6D, kModeAbs },
	{ 0x7D, kModeAbsX },
	{ 0x79, kModeAbsY },
	{ 0x61, kModeIndX },
	{ 0x71, kModeIndY },
	{ 0x72, kModeZpI },
	{ 0x63, kModeStack },
	{ 0x73, kModeStackIndY },
	{ 0x67, kModeIndLong },
	{ 0x77, kModeIndLongY },
	{ 0x6F, kModeAbsLong },
	{ 0x7F, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(ASL)
	{ 0x0A, kModeImp },
	{ 0x06, kModeZp },
	{ 0x16, kModeZpX },
	{ 0x0E, kModeAbs },
	{ 0x1E, kModeAbsX },
X_OPCODE_END()

X_OPCODE_BEGIN(BCC)
	{ 0x90, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BCS)
	{ 0xB0, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BEQ)
	{ 0xF0, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BIT)
	{ 0x24, kModeZp },
	{ 0x2C, kModeAbs },
	{ 0x34, kModeZpX },
	{ 0x3C, kModeAbsX },
	{ 0x89, kModeImm },
X_OPCODE_END()

X_OPCODE_BEGIN(BMI)
	{ 0x30, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BNE)
	{ 0xD0, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BPL)
	{ 0x10, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BRA)
	{ 0x80, kModeRel },
	{ 0x82, kModeRelLong },
X_OPCODE_END()

X_OPCODE_BEGIN(BRK)
	{ 0x00, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(BRL)
	{ 0x82, kModeRelLong },
X_OPCODE_END()

X_OPCODE_BEGIN(BVC)
	{ 0x50, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(BVS)
	{ 0x70, kModeRel },
X_OPCODE_END()

X_OPCODE_BEGIN(CLC)
	{ 0x18, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(CLD)
	{ 0xD8, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(CLI)
	{ 0x58, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(CLV)
	{ 0xB8, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(CMP)
	{ 0xC9, kModeImm },
	{ 0xC5, kModeZp },
	{ 0xD5, kModeZpX },
	{ 0xCD, kModeAbs },
	{ 0xDD, kModeAbsX },
	{ 0xD9, kModeAbsY },
	{ 0xC1, kModeIndX },
	{ 0xD1, kModeIndY },
	{ 0xD2, kModeZpI },
	{ 0xC3, kModeStack },
	{ 0xD3, kModeStackIndY },
	{ 0xC7, kModeIndLong },
	{ 0xD7, kModeIndLongY },
	{ 0xCF, kModeAbsLong },
	{ 0xDF, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(COP)
	{ 0x02, kModeIVec },
X_OPCODE_END()

X_OPCODE_BEGIN(CPX)
	{ 0xE0, kModeImm },
	{ 0xE4, kModeZp },
	{ 0xEC, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(CPY)
	{ 0xC0, kModeImm },
	{ 0xC4, kModeZp },
	{ 0xCC, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(DEA)
	{ 0x3A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(DEC)
	{ 0xC6, kModeZp },
	{ 0xD6, kModeZpX },
	{ 0xCE, kModeAbs },
	{ 0xDE, kModeAbsX },
	{ 0x3A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(DEX)
	{ 0xCA, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(DEY)
	{ 0x88, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(EOR)
	{ 0x49, kModeImm },
	{ 0x45, kModeZp },
	{ 0x55, kModeZpX },
	{ 0x4D, kModeAbs },
	{ 0x5D, kModeAbsX },
	{ 0x59, kModeAbsY },
	{ 0x41, kModeIndX },
	{ 0x51, kModeIndY },
	{ 0x52, kModeZpI },
	{ 0x43, kModeStack },
	{ 0x53, kModeStackIndY },
	{ 0x47, kModeIndLong },
	{ 0x57, kModeIndLongY },
	{ 0x4F, kModeAbsLong },
	{ 0x5F, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(INA)
	{ 0x1A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(INC)
	{ 0xE6, kModeZp },
	{ 0xF6, kModeZpX },
	{ 0xEE, kModeAbs },
	{ 0xFE, kModeAbsX },
	{ 0x1A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(INX)
	{ 0xE8, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(INY)
	{ 0xC8, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(JML)
	{ 0x5C, kModeAbsLong },
	{ 0xDC, kModeAbsLongI },
X_OPCODE_END()

X_OPCODE_BEGIN(JMP)
	{ 0x4C, kModeAbs },
	{ 0x6C, kModeAbsI },
	{ 0x7C, kModeAbsIX },
	{ 0x5C, kModeAbsLong },
	{ 0xDC, kModeAbsLongI },
X_OPCODE_END()

X_OPCODE_BEGIN(JSL)
	{ 0x22, kModeAbsLong },
X_OPCODE_END()

X_OPCODE_BEGIN(JSR)
	{ 0x20, kModeAbs },
	{ 0x22, kModeAbsLong },
	{ 0xFC, kModeAbsIX },
X_OPCODE_END()

X_OPCODE_BEGIN(LDA)
	{ 0xA9, kModeImm },
	{ 0xA5, kModeZp },
	{ 0xB5, kModeZpX },
	{ 0xAD, kModeAbs },
	{ 0xBD, kModeAbsX },
	{ 0xB9, kModeAbsY },
	{ 0xA1, kModeIndX },
	{ 0xB1, kModeIndY },
	{ 0xB2, kModeZpI },
	{ 0xA3, kModeStack },
	{ 0xB3, kModeStackIndY },
	{ 0xA7, kModeIndLong },
	{ 0xB7, kModeIndLongY },
	{ 0xAF, kModeAbsLong },
	{ 0xBF, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(LDX)
	{ 0xA2, kModeImm },
	{ 0xA6, kModeZp },
	{ 0xB6, kModeZpY },
	{ 0xAE, kModeAbs },
	{ 0xBE, kModeAbsY },
X_OPCODE_END()

X_OPCODE_BEGIN(LDY)
	{ 0xA0, kModeImm },
	{ 0xA4, kModeZp },
	{ 0xB4, kModeZpX },
	{ 0xAC, kModeAbs },
	{ 0xBC, kModeAbsX },
X_OPCODE_END()

X_OPCODE_BEGIN(LSR)
	{ 0x4A, kModeImp },
	{ 0x46, kModeZp },
	{ 0x56, kModeZpX },
	{ 0x4E, kModeAbs },
	{ 0x5E, kModeAbsX },
X_OPCODE_END()

X_OPCODE_BEGIN(MVN)
	{ 0x54, kModeMove },
X_OPCODE_END()

X_OPCODE_BEGIN(MVP)
	{ 0x44, kModeMove },
X_OPCODE_END()

X_OPCODE_BEGIN(NOP)
	{ 0xEA, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(ORA)
	{ 0x09, kModeImm },
	{ 0x05, kModeZp },
	{ 0x15, kModeZpX },
	{ 0x0D, kModeAbs },
	{ 0x1D, kModeAbsX },
	{ 0x19, kModeAbsY },
	{ 0x01, kModeIndX },
	{ 0x11, kModeIndY },
	{ 0x12, kModeZpI },
	{ 0x03, kModeStack },
	{ 0x13, kModeStackIndY },
	{ 0x07, kModeIndLong },
	{ 0x17, kModeIndLongY },
	{ 0x0F, kModeAbsLong },
	{ 0x1F, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(PEA)
	{ 0xF4, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(PEI)
	{ 0xD4, kModeZpI },
X_OPCODE_END()

X_OPCODE_BEGIN(PER)
	{ 0x62, kModeRelLong },
X_OPCODE_END()

X_OPCODE_BEGIN(PHA)
	{ 0x48, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PHB)
	{ 0x8B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PHD)
	{ 0x0B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PHK)
	{ 0x4B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PHP)
	{ 0x08, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PHX)
	{ 0xDA, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PHY)
	{ 0x5A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PLA)
	{ 0x68, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PLB)
	{ 0xAB, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PLD)
	{ 0x2B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PLP)
	{ 0x28, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PLX)
	{ 0xFA, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(PLY)
	{ 0x7A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(REP)
	{ 0xC2, kModeImm },
X_OPCODE_END()

X_OPCODE_BEGIN(ROL)
	{ 0x2A, kModeImp },
	{ 0x26, kModeZp },
	{ 0x36, kModeZpX },
	{ 0x2E, kModeAbs },
	{ 0x3E, kModeAbsX },
X_OPCODE_END()

X_OPCODE_BEGIN(ROR)
	{ 0x6A, kModeImp },
	{ 0x66, kModeZp },
	{ 0x76, kModeZpX },
	{ 0x6E, kModeAbs },
	{ 0x7E, kModeAbsX },
X_OPCODE_END()

X_OPCODE_BEGIN(RTI)
	{ 0x40, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(RTL)
	{ 0x6B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(RTS)
	{ 0x60, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(SBC)
	{ 0xE9, kModeImm },
	{ 0xE5, kModeZp },
	{ 0xF5, kModeZpX },
	{ 0xED, kModeAbs },
	{ 0xFD, kModeAbsX },
	{ 0xF9, kModeAbsY },
	{ 0xE1, kModeIndX },
	{ 0xF1, kModeIndY },
	{ 0xF2, kModeZpI },
	{ 0xE3, kModeStack },
	{ 0xF3, kModeStackIndY },
	{ 0xE7, kModeIndLong },
	{ 0xF7, kModeIndLongY },
	{ 0xEF, kModeAbsLong },
	{ 0xFF, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(SEC)
	{ 0x38, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(SED)
	{ 0xF8, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(SEI)
	{ 0x78, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(SEP)
	{ 0xE2, kModeImm },
X_OPCODE_END()

X_OPCODE_BEGIN(STA)
	{ 0x85, kModeZp },
	{ 0x95, kModeZpX },
	{ 0x8D, kModeAbs },
	{ 0x9D, kModeAbsX },
	{ 0x99, kModeAbsY },
	{ 0x81, kModeIndX },
	{ 0x91, kModeIndY },
	{ 0x92, kModeZpI },
	{ 0x83, kModeStack },
	{ 0x93, kModeStackIndY },
	{ 0x87, kModeIndLong },
	{ 0x97, kModeIndLongY },
	{ 0x8F, kModeAbsLong },
	{ 0x9F, kModeAbsLongX },
X_OPCODE_END()

X_OPCODE_BEGIN(STP)
	{ 0xDB, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(STX)
	{ 0x86, kModeZp },
	{ 0x96, kModeZpY },
	{ 0x8E, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(STY)
	{ 0x84, kModeZp },
	{ 0x94, kModeZpX },
	{ 0x8C, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(STZ)
	{ 0x64, kModeZp },
	{ 0x74, kModeZpX },
	{ 0x9C, kModeAbs },
	{ 0x9E, kModeAbsX },
X_OPCODE_END()

X_OPCODE_BEGIN(SWA)
	{ 0xEB, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TAD)
	{ 0x5B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TAS)
	{ 0x1B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TAX)
	{ 0xAA, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TAY)
	{ 0xA8, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TCD)
	{ 0x5B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TCS)
	{ 0x1B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TDA)
	{ 0x7B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TDC)
	{ 0x7B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TRB)
	{ 0x14, kModeZp },
	{ 0x1C, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(TSA)
	{ 0x3B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TSB)
	{ 0x04, kModeZp },
	{ 0x0C, kModeAbs },
X_OPCODE_END()

X_OPCODE_BEGIN(TSC)
	{ 0x3B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TSX)
	{ 0xBA, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TXA)
	{ 0x8A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TXS)
	{ 0x9A, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TXY)
	{ 0x9B, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TYA)
	{ 0x98, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(TYX)
	{ 0xBB, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(WAI)
	{ 0xCB, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(WDM)
	{ 0x42, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(XBA)
	{ 0xEB, kModeImp },
X_OPCODE_END()

X_OPCODE_BEGIN(XCE)
	{ 0xFB, kModeImp },
X_OPCODE_END()

//////////////////////////////////////////////////////////////////////////

const ATDebuggerCmdAssemble::CommandEntry ATDebuggerCmdAssemble::kCommands[] = {
	// psuedo-opcodes (operand friendly)
	{ "REQ", NULL, kPsuedoOp_REQ },
	{ "RNE", NULL, kPsuedoOp_RNE },
	{ "RPL", NULL, kPsuedoOp_RPL },
	{ "RMI", NULL, kPsuedoOp_RMI },
	{ "RCC", NULL, kPsuedoOp_RCC },
	{ "RCS", NULL, kPsuedoOp_RCS },
	{ "RVC", NULL, kPsuedoOp_RVC },
	{ "RVS", NULL, kPsuedoOp_RCS },
	{ "SEQ", NULL, kPsuedoOp_SEQ },
	{ "SNE", NULL, kPsuedoOp_SNE },
	{ "SPL", NULL, kPsuedoOp_SPL },
	{ "SMI", NULL, kPsuedoOp_SMI },
	{ "SCC", NULL, kPsuedoOp_SCC },
	{ "SCS", NULL, kPsuedoOp_SCS },
	{ "SVC", NULL, kPsuedoOp_SVC },
	{ "SVS", NULL, kPsuedoOp_SCS },
	{ "JEQ", NULL, kPsuedoOp_JEQ },
	{ "JNE", NULL, kPsuedoOp_JNE },
	{ "JPL", NULL, kPsuedoOp_JPL },
	{ "JMI", NULL, kPsuedoOp_JMI },
	{ "JCC", NULL, kPsuedoOp_JCC },
	{ "JCS", NULL, kPsuedoOp_JCS },
	{ "JVC", NULL, kPsuedoOp_JVC },
	{ "JVS", NULL, kPsuedoOp_JCS },
	{ "ADD", NULL, kPsuedoOp_ADD },
	{ "SUB", NULL, kPsuedoOp_SUB },
	{ "PHR", NULL, kPsuedoOp_PHR },
	{ "PLR", NULL, kPsuedoOp_PLR },
	{ "INW", NULL, kPsuedoOp_INW },
	{ "INL", NULL, kPsuedoOp_INL },
	{ "IND", NULL, kPsuedoOp_IND },
	{ "DEW", NULL, kPsuedoOp_DEW },
	{ "DEL", NULL, kPsuedoOp_DEL },
	{ "DED", NULL, kPsuedoOp_DED },

	// psuedo-opcodes (custom operand)
	{ "ADB", NULL, kPsuedoOp_ADB },
	{ "SBB", NULL, kPsuedoOp_SBB },
	{ "ADW", NULL, kPsuedoOp_ADW },
	{ "SBW", NULL, kPsuedoOp_SBW },
	{ "MVA", NULL, kPsuedoOp_MVA },
	{ "MVX", NULL, kPsuedoOp_MVX },
	{ "MVY", NULL, kPsuedoOp_MVY },
	{ "MWA", NULL, kPsuedoOp_MWA },
	{ "MWX", NULL, kPsuedoOp_MWX },
	{ "MWY", NULL, kPsuedoOp_MWY },
	{ "CPB", NULL, kPsuedoOp_CPB },
	{ "CPW", NULL, kPsuedoOp_CPW },
	{ "CPL", NULL, kPsuedoOp_CPL },
	{ "CPD", NULL, kPsuedoOp_CPD },

	// directives
	{ "ORG", NULL, kPsuedoOp_ORG },
	{ "LONGA", NULL, kPsuedoOp_LONGA },
	{ "LONGI", NULL, kPsuedoOp_LONGI },

	// 6502
	{ "ADC", kOpcodes_ADC },
	{ "AND", kOpcodes_AND },
	{ "ASL", kOpcodes_ASL },
	{ "BCC", kOpcodes_BCC },
	{ "BCS", kOpcodes_BCS },
	{ "BEQ", kOpcodes_BEQ },
	{ "BIT", kOpcodes_BIT },
	{ "BMI", kOpcodes_BMI },
	{ "BNE", kOpcodes_BNE },
	{ "BPL", kOpcodes_BPL },
	{ "BRA", kOpcodes_BRA },
	{ "BRL", kOpcodes_BRL },
	{ "BRK", kOpcodes_BRK },
	{ "BVC", kOpcodes_BVC },
	{ "BVS", kOpcodes_BVS },
	{ "CLC", kOpcodes_CLC },
	{ "CLD", kOpcodes_CLD },
	{ "CLI", kOpcodes_CLI },
	{ "CLV", kOpcodes_CLV },
	{ "CMP", kOpcodes_CMP },
	{ "COP", kOpcodes_COP },
	{ "CPX", kOpcodes_CPX },
	{ "CPY", kOpcodes_CPY },
	{ "DEA", kOpcodes_DEA },
	{ "DEC", kOpcodes_DEC },
	{ "DEX", kOpcodes_DEX },
	{ "DEY", kOpcodes_DEY },
	{ "EOR", kOpcodes_EOR },
	{ "INA", kOpcodes_INA },
	{ "INC", kOpcodes_INC },
	{ "INX", kOpcodes_INX },
	{ "INY", kOpcodes_INY },
	{ "JML", kOpcodes_JML },
	{ "JMP", kOpcodes_JMP },
	{ "JSL", kOpcodes_JSL },
	{ "JSR", kOpcodes_JSR },
	{ "LDA", kOpcodes_LDA },
	{ "LDX", kOpcodes_LDX },
	{ "LDY", kOpcodes_LDY },
	{ "LSR", kOpcodes_LSR },
	{ "MVN", kOpcodes_MVN },
	{ "MVP", kOpcodes_MVP },
	{ "NOP", kOpcodes_NOP },
	{ "ORA", kOpcodes_ORA },
	{ "PEA", kOpcodes_PEA },
	{ "PEI", kOpcodes_PEI },
	{ "PER", kOpcodes_PER },
	{ "PHA", kOpcodes_PHA },
	{ "PHB", kOpcodes_PHB },
	{ "PHD", kOpcodes_PHD },
	{ "PHP", kOpcodes_PHP },
	{ "PHK", kOpcodes_PHK },
	{ "PHX", kOpcodes_PHX },
	{ "PHY", kOpcodes_PHY },
	{ "PLA", kOpcodes_PLA },
	{ "PLB", kOpcodes_PLB },
	{ "PLD", kOpcodes_PLD },
	{ "PLP", kOpcodes_PLP },
	{ "PLX", kOpcodes_PLX },
	{ "PLY", kOpcodes_PLY },
	{ "REP", kOpcodes_REP },
	{ "ROL", kOpcodes_ROL },
	{ "ROR", kOpcodes_ROR },
	{ "RTI", kOpcodes_RTI },
	{ "RTL", kOpcodes_RTL },
	{ "RTS", kOpcodes_RTS },
	{ "SBC", kOpcodes_SBC },
	{ "SEC", kOpcodes_SEC },
	{ "SED", kOpcodes_SED },
	{ "SEI", kOpcodes_SEI },
	{ "SEP", kOpcodes_SEP },
	{ "STA", kOpcodes_STA },
	{ "STP", kOpcodes_STP },
	{ "STX", kOpcodes_STX },
	{ "STY", kOpcodes_STY },
	{ "STZ", kOpcodes_STZ },
	{ "TAD", kOpcodes_TAD },
	{ "TAX", kOpcodes_TAX },
	{ "TAY", kOpcodes_TAY },
	{ "TCD", kOpcodes_TCD },
	{ "TCS", kOpcodes_TCS },
	{ "TDA", kOpcodes_TDA },
	{ "TDC", kOpcodes_TDC },
	{ "TRB", kOpcodes_TRB },
	{ "TSA", kOpcodes_TSA },
	{ "TSB", kOpcodes_TSB },
	{ "TSC", kOpcodes_TSC },
	{ "TSX", kOpcodes_TSX },
	{ "TXA", kOpcodes_TXA },
	{ "TXS", kOpcodes_TXS },
	{ "TXY", kOpcodes_TXY },
	{ "TYA", kOpcodes_TYA },
	{ "TYX", kOpcodes_TYX },
	{ "WAI", kOpcodes_WAI },
	{ "WDM", kOpcodes_WDM },
	{ "XBA", kOpcodes_XBA },
	{ "XCE", kOpcodes_XCE },
};

//////////////////////////////////////////////////////////////////////////

ATDebuggerCmdAssemble::ATDebuggerCmdAssemble(uint32 address)
	: mAddress(address)
	, mbLongM(false)
	, mbLongX(false)
{
	for(size_t i=0; i<sizeof(kCommands)/sizeof(kCommands[0]); ++i) {
		const CommandEntry& e = kCommands[i];

		mCommandLookup.insert(HashedStringSpanAI(e.mpName)).first->second = &e;
	}
}

const char *ATDebuggerCmdAssemble::GetPrompt() {
	if (mAddress >= 0x10000)
		mPrompt.sprintf("%02X:%04X", mAddress >> 16, mAddress & 0xffff);
	else
		mPrompt.sprintf("%04X", mAddress);

	if (mbLongM || mbLongX)
		mPrompt.append_sprintf("/%s%s", mbLongM ? "M" : "", mbLongX ? "X" : "");

	return mPrompt.c_str();
}

void ATDebuggerCmdAssemble::BeginCommand(IATDebugger *debugger) {
	mpDebugger = debugger;
}

void ATDebuggerCmdAssemble::EndCommand() {
	if (mAddress >= 0x10000)
		ATConsolePrintf("%02X:%04X: end\n", mAddress >> 16, mAddress & 0xffff);
	else
		ATConsolePrintf("%04X: end\n", mAddress);
}

bool ATDebuggerCmdAssemble::ProcessSubCommand(const char *s) {
	const char *const s0 = s;

	mEmittedBytes.clear();

	try {
		// skip whitespace
		while(*s == ' ')
			++s;

		// check for comment or EOL
		if (*s == ';')
			return true;

		if (!*s)
			return false;

		// obtain label/Command
		const char *opstart = s;
		if (!IsIdentStart(*s))
			throw ParseException(s);

		while(IsIdentNext(*s))
			++s;

		const char *opend = s;
		const char *labstart = NULL;
		const char *labend = NULL;

		// check if this is a valid Command
		CommandLookup::const_iterator it(mCommandLookup.find(HashedStringSpanAI(opstart, opend)));
		if (it == mCommandLookup.end()) {
			// nope -- assume it's a label
			labstart = opstart;
			labend = opend;

			// skip colon if present
			bool haveColon = false;

			if (*s == ':') {
				++s;
				haveColon = true;
			}

			// skip whitespace
			while(*s == ' ')
				++s;

			opstart = NULL;
			opend = NULL;

			if (!*s || *s == ';') {
				if (!haveColon && it == mCommandLookup.end())
					throw ParseException(s, "Unknown command or opcode");

			} else {
				opstart = s;

				if (!IsIdentStart(*s))
					throw ParseException(s, "Expected command or opcode");

				++s;

				while(IsIdentNext(*s))
					++s;

				opend = s;

				it = mCommandLookup.find(HashedStringSpanAI(opstart, opend));

				if (it == mCommandLookup.end())
					throw ParseException(s, "Unknown command or opcode");
			}
		}

		bool allowMulti = false;

		typedef vdfastvector<const CommandEntry *> Commands;
		Commands commands;

		if (it != mCommandLookup.end()) {
			allowMulti = it->second->mPsuedoOp < kPsuedoOp_OperandParsingLimit;

			// push entry and check for follow-up commands
			for(;;) {
				commands.push_back(it->second);

				while(*s == ' ')
					++s;

				if (*s != ':')
					break;

				++s;

				const char *opstart2 = s;
				if (!IsIdentStart(*s))
					throw ParseException(s, "Expected command or opcode");

				++s;
				while(IsIdentNext(*s))
					++s;
				const char *opend2 = s;

				it = mCommandLookup.find(HashedStringSpanAI(opstart2, opend2));

				if (it == mCommandLookup.end())
					throw ParseException(opstart2, "Unknown command or opcode");

				if (!allowMulti || it->second->mPsuedoOp > kPsuedoOp_OperandParsingLimit)
					throw ParseException(opstart2, "Stacking not allowed with command");
			}
		}

		if (labstart)
			mpDebugger->AddCustomSymbol(mAddress, 1, VDStringA(labstart, labend).c_str(), kATSymbol_Any);

		if (!allowMulti && !commands.empty()) {
			const CommandEntry& cmd = *commands.front();
			sint32 newAddr = -1;

			switch(cmd.mPsuedoOp) {
				case kPsuedoOp_ADB:
					s = EmitCompositeADx(s, kOpcodes_CLC, kOpcodes_ADC, 1);
					break;
				case kPsuedoOp_SBB:
					s = EmitCompositeADx(s, kOpcodes_SEC, kOpcodes_SBC, 1);
					break;
				case kPsuedoOp_ADW:
					s = EmitCompositeADx(s, kOpcodes_CLC, kOpcodes_ADC, 2);
					break;
				case kPsuedoOp_SBW:
					s = EmitCompositeADx(s, kOpcodes_SEC, kOpcodes_SBC, 2);
					break;

				case kPsuedoOp_MVA:
					s = EmitCompositeMVx(s, kOpcodes_LDA, kOpcodes_STA);
					break;

				case kPsuedoOp_MVX:
					s = EmitCompositeMVx(s, kOpcodes_LDX, kOpcodes_STX);
					break;

				case kPsuedoOp_MVY:
					s = EmitCompositeMVx(s, kOpcodes_LDY, kOpcodes_STY);
					break;

				case kPsuedoOp_MWA:
					s = EmitCompositeMWx(s, kOpcodes_LDA, kOpcodes_STA);
					break;

				case kPsuedoOp_MWX:
					s = EmitCompositeMWx(s, kOpcodes_LDX, kOpcodes_STX);
					break;

				case kPsuedoOp_MWY:
					s = EmitCompositeMWx(s, kOpcodes_LDY, kOpcodes_STY);
					break;

				case kPsuedoOp_CPB:
					s = EmitCompositeCPx(s, 1);
					break;

				case kPsuedoOp_CPW:
					s = EmitCompositeCPx(s, 2);
					break;

				case kPsuedoOp_CPL:
					s = EmitCompositeCPx(s, 3);
					break;

				case kPsuedoOp_CPD:
					s = EmitCompositeCPx(s, 4);
					break;

				case kPsuedoOp_ORG:
					{
						sint32 addr;

						s = ParseExpression(s, addr);

						newAddr = addr;
					}
					break;

				case kPsuedoOp_LONGA:
				case kPsuedoOp_LONGI:
					while(*s == ' ')
						++s;

					if ((s[0] & 0xdf) == 'O' && (s[1] & 0xdf) == 'N') {
						if (cmd.mPsuedoOp == kPsuedoOp_LONGA)
							mbLongM = true;
						else
							mbLongX = true;

						s += 2;
					} else if ((s[0] & 0xdf) == 'O' && (s[1] & 0xdf) == 'F' && (s[2] & 0xdf) == 'F') {
						if (cmd.mPsuedoOp == kPsuedoOp_LONGA)
							mbLongM = false;
						else
							mbLongX = false;

						s += 3;
					} else
						throw ParseException(s, "Expected 'on' or 'off'");
					break;

				default:
					VDASSERT(false);
			}

			// we shouldn't have anything but a comment left
			while(*s == ' ')
				++s;

			if (*s && *s != ';')
				throw ParseException(s, "Expected end of line");

			if (newAddr >= 0)
				mAddress = newAddr;
		} else if (!commands.empty()) {
			// parse the operand, if present
			OperandInfo operand;
			s = ParseOperand(s, operand);

			// we shouldn't have anything but a comment left
			while(*s == ' ')
				++s;

			if (*s && *s != ';')
				throw ParseException(s, "Expected end of line");

			// process and emit each command or opcode
			const bool multicmd = &commands.front() != &commands.back();
			OperandInfoImplied implied;
			int skipPatchOffset = -1;
			for(Commands::const_iterator itC(commands.begin()), itCEnd(commands.end()); itC != itCEnd; ++itC) {
				const CommandEntry& cmd = **itC;
				uint32 pc = mAddress + (uint32)mEmittedBytes.size();

				static const OpcodeEntry *const kBranchOpcodes[]={
					kOpcodes_BEQ,
					kOpcodes_BNE,
					kOpcodes_BPL,
					kOpcodes_BMI,
					kOpcodes_BCC,
					kOpcodes_BCS,
					kOpcodes_BVC,
					kOpcodes_BCS,
				};

				static const uint8 kBranchOpVals[]={
					0xF0,
					0xD0,
					0x10,
					0x30,
					0x90,
					0xB0,
					0x50,
					0x70
				};

				switch(cmd.mPsuedoOp) {
					// Repeat psuedo-opcodes
					case kPsuedoOp_REQ:
					case kPsuedoOp_RNE:
					case kPsuedoOp_RPL:
					case kPsuedoOp_RMI:
					case kPsuedoOp_RCC:
					case kPsuedoOp_RCS:
					case kPsuedoOp_RVC:
					case kPsuedoOp_RVS:
						EmitOpcode(s, kBranchOpcodes[cmd.mPsuedoOp - kPsuedoOp_REQ], OperandInfoBranchTarget(mAddress), false);
						break;

					// skip pseudo-opcodes
					case kPsuedoOp_SEQ:
					case kPsuedoOp_SNE:
					case kPsuedoOp_SPL:
					case kPsuedoOp_SMI:
					case kPsuedoOp_SCC:
					case kPsuedoOp_SCS:
					case kPsuedoOp_SVC:
					case kPsuedoOp_SVS:
						if (skipPatchOffset >= 0)
							throw ParseException(s, "Cannot skip another skip pseudo-op");

						mEmittedBytes.push_back(kBranchOpVals[cmd.mPsuedoOp - kPsuedoOp_SEQ]);
						mEmittedBytes.push_back(0);

						skipPatchOffset = (uint32)mEmittedBytes.size();
						continue;

					case kPsuedoOp_JEQ:
					case kPsuedoOp_JNE:
					case kPsuedoOp_JPL:
					case kPsuedoOp_JMI:
					case kPsuedoOp_JCC:
					case kPsuedoOp_JCS:
					case kPsuedoOp_JVC:
					case kPsuedoOp_JVS:
						if (((operand.mOpValue - (pc + 2) + 0x80) & 0xffff) < 0x100)
							EmitOpcode(s, kBranchOpcodes[cmd.mPsuedoOp - kPsuedoOp_JEQ], operand, false);
						else {
							EmitOpcode(s, kBranchOpcodes[(cmd.mPsuedoOp - kPsuedoOp_JEQ) ^ 1], OperandInfoBranchTarget(pc + 5), false);
							EmitOpcode(s, kOpcodes_JMP, operand, false);
						}
						break;

					case kPsuedoOp_ADD:
						EmitOpcode(s, kOpcodes_CLC, implied, false);
						EmitOpcode(s, kOpcodes_ADC, operand, multicmd);
						break;
					case kPsuedoOp_SUB:
						EmitOpcode(s, kOpcodes_SEC, implied, false);
						EmitOpcode(s, kOpcodes_ADC, operand, multicmd);
						break;
					case kPsuedoOp_PHR:
						EmitOpcode(s, kOpcodes_PHA, implied, false);
						EmitOpcode(s, kOpcodes_TXA, implied, false);
						EmitOpcode(s, kOpcodes_PHA, implied, false);
						EmitOpcode(s, kOpcodes_TYA, implied, false);
						EmitOpcode(s, kOpcodes_PHA, implied, false);
						break;
					case kPsuedoOp_PLR:
						EmitOpcode(s, kOpcodes_PLA, implied, false);
						EmitOpcode(s, kOpcodes_TAY, implied, false);
						EmitOpcode(s, kOpcodes_PLA, implied, false);
						EmitOpcode(s, kOpcodes_TAX, implied, false);
						EmitOpcode(s, kOpcodes_PLA, implied, false);
						break;
					case kPsuedoOp_INW:
						EmitCompositeINCDEC(s, kOpcodes_INC, operand, 2);
						break;
					case kPsuedoOp_INL:
						EmitCompositeINCDEC(s, kOpcodes_INC, operand, 3);
						break;
					case kPsuedoOp_IND:
						EmitCompositeINCDEC(s, kOpcodes_INC, operand, 4);
						break;
					case kPsuedoOp_DEW:
						EmitCompositeINCDEC(s, kOpcodes_DEC, operand, 2);
						break;
					case kPsuedoOp_DEL:
						EmitCompositeINCDEC(s, kOpcodes_DEC, operand, 3);
						break;
					case kPsuedoOp_DED:
						EmitCompositeINCDEC(s, kOpcodes_DEC, operand, 4);
						break;

					default:
						EmitOpcode(s, cmd.mpOpcodes, operand, multicmd);
						break;
				}

				if (skipPatchOffset >= 0) {
					mEmittedBytes[skipPatchOffset - 1] = (uint8)(mEmittedBytes.size() - skipPatchOffset);
					skipPatchOffset = -1;
				}
			}

			if (skipPatchOffset >= 0)
				throw ParseException(s, "Cannot end with skip pseudo-op");
		}

		VDStringA output;

		if (mAddress >= 0x10000)
			output.sprintf("%02X:%04X:", mAddress >> 16, mAddress & 0xffff);
		else
			output.sprintf("%04X:", mAddress);

		uint32 n = (uint32)mEmittedBytes.size();
		for(uint32 i=0; i<n; ++i) {
			if (i == 4)
				break;

			output.append_sprintf(" %02X", mEmittedBytes[i]);
		}

		if (n > 4)
			output += '+';
		else {
			for(int i=13 - 3*n; i; --i)
				output += ' ';
		}

		output.append_sprintf("   %s\n", s0);
		ATConsoleWrite(output.c_str());

		if (!mEmittedBytes.empty())
			mpDebugger->WriteGlobalMemory(mAddress, mEmittedBytes.data(), (uint32)mEmittedBytes.size());

		mAddress += n;
		mAddress &= 0xFFFFFF;
	} catch(const ParseException& e) {
		if (e.mpMsg) {
			ATConsolePrintf("ERROR: %s\n       %s\n", e.mpMsg, s0);
			ATConsolePrintf("%*c^\n", 7 + (e.mpPos - s0), ' ');
		} else {
			ATConsolePrintf("ERROR: %s\n", s0);
			ATConsolePrintf("%*c^\n", 7 + (e.mpPos - s0), ' ');
		}
	}

	return true;
}

const char *ATDebuggerCmdAssemble::ParseOperand(const char *s, OperandInfo& opinfo) {
	opinfo.mOpValue = 0;
	opinfo.mOpModes = 0;
	opinfo.mPostIncX = 0;
	opinfo.mPostIncY = 0;

	while(*s == ' ')
		++s;

	if (!*s || *s == ';') {
		opinfo.mOpModes = kModeFlag_Imp;
	} else if (*s == '#') {
		++s;
		while(*s == ' ')
			++s;

		s = ParseExpression(s, opinfo.mOpValue);

		opinfo.mOpModes = kModeFlag_Imm;
	} else if (*s == '(') {	// (zp); (abs); (zp,X); (abs,X); (zp),Y; (d,S),Y
		s = ParseExpression(s+1, opinfo.mOpValue);

		bool zpAllowed = (uint32)opinfo.mOpValue < 0x100;

		while(*s == ' ')
			++s;

		if (*s == ',') {	// (abs/zp,X) or (d,S),Y
			++s;

			if (*s == 's' || *s == 'S') {
				if (!zpAllowed)
					throw ParseException(s, "Operand exceeds range for stack-relative addressing");

				if (s[1] != ')' || s[2] != ',' || (s[3] != 'Y' && s[3] != 'y'))
					throw ParseException(s, "Expected (d,S),Y syntax");

				s += 4;

				opinfo.mOpModes = kModeFlag_StackIndY;
			} else if (*s == 'x' || *s == 'X') {
				++s;

				if (*s == '+') {
					++s;
					++opinfo.mPostIncX;
					if (*s == '+') {
						++s;
						++opinfo.mPostIncX;
					}
				}

				while(*s == ' ')
					++s;

				if (*s != ')')
					throw ParseException(s, "')' expected");

				++s;

				opinfo.mOpModes = kModeFlag_AbsIX;

				if (zpAllowed)
					opinfo.mOpModes |= kModeFlag_IndX;
			} else {
				throw ParseException(s, "S or X index register expected");
			}
		} else if (*s == ')') {
			++s;

			while(*s == ' ')
				++s;

			if (*s == ',') {
				++s;

				while(*s == ' ')
					++s;

				if (*s != 'y' && *s != 'Y')
					throw ParseException(s, "Y index register expected");

				++s;

				if (*s == '+') {
					++s;
					++opinfo.mPostIncY;
					if (*s == '+') {
						++s;
						++opinfo.mPostIncY;
					}
				}

				if (!zpAllowed)
					throw ParseException(s, "Operand exceeds range for zero page addressing");

				opinfo.mOpModes = kModeFlag_IndY;
			} else if (!*s || *s == ';') {
				opinfo.mOpModes = kModeFlag_AbsI;

				if (zpAllowed)
					opinfo.mOpModes |= kModeFlag_ZpI;
			} else
				throw ParseException(s);
		}
	} else if (*s == '[') {	// [dp]; [dp],Y; [abs]
		s = ParseExpression(s+1, opinfo.mOpValue);

		const bool zpAllowed = (uint32)opinfo.mOpValue < 0x100;

		while(*s == ' ')
			++s;

		if (*s != ']')
			throw ParseException(s, "']' expected");

		++s;

		// check for [dp],Y
		if (s[0] == ',' && (s[1] == 'Y' || s[1] == 'y')) {
			s += 2;

			if (!zpAllowed)
				throw ParseException(s, "Operand exceeds range for direct page long indexed addressing");

			opinfo.mOpModes = kModeFlag_IndLongY;

			if (*s == '+') {
				++s;
				++opinfo.mPostIncY;

				if (*s == '+') {
					++s;
					++opinfo.mPostIncY;
				}
			}
		} else {
			opinfo.mOpModes = kModeFlag_AbsLongI;

			if (zpAllowed)
				opinfo.mOpModes |= kModeFlag_IndLong;
		}
	} else {
		s = ParseExpression(s, opinfo.mOpValue);

		bool zpAllowed = ((uint32)opinfo.mOpValue) < 0x100;
		bool absAllowed = ((uint32)opinfo.mOpValue) < 0x10000;

		while(*s == ' ')
			++s;

		if (*s == ',') {
			++s;
			while(*s == ' ')
				++s;

			if (*s == 'x' || *s == 'X') {
				++s;

				if (*s == '+') {
					++s;
					++opinfo.mPostIncX;
					if (*s == '+') {
						++s;
						++opinfo.mPostIncX;
					}
				}

				opinfo.mOpModes = kModeFlag_AbsLongX;

				if (absAllowed) {
					opinfo.mOpModes |= kModeFlag_AbsX;

					if (zpAllowed)
						opinfo.mOpModes |= kModeFlag_ZpX;
				}
			} else if (*s == 'y' || *s == 'Y') {
				++s;

				if (*s == '+') {
					++s;
					++opinfo.mPostIncY;
					if (*s == '+') {
						++s;
						++opinfo.mPostIncY;
					}
				}

				opinfo.mOpModes = kModeFlag_AbsY;

				if (zpAllowed)
					opinfo.mOpModes |= kModeFlag_ZpY;
			} else if (*s == 's' || *s == 'S') {
				++s;

				if (!zpAllowed)
					throw ParseException(s, "Operand exceeds range for stack-relative addressing");

				opinfo.mOpModes = kModeFlag_Stack;
			} else {
				if (!zpAllowed)
					throw ParseException(s, "Source bank out of range");

				sint32 dstBank;
				s = ParseExpression(s, dstBank);

				if ((uint32)dstBank >= 0x100)
					throw ParseException(s, "Destination bank out of range");

				opinfo.mOpModes = kModeFlag_Move;
				opinfo.mOpValue += dstBank << 8;
			}
		} else {
			opinfo.mOpModes = kModeFlag_AbsLong;

			if (absAllowed) {
				opinfo.mOpModes |= kModeFlag_Abs;

				if (zpAllowed)
					opinfo.mOpModes |= kModeFlag_Zp | kModeFlag_IVec;
			}

			opinfo.mOpModes |= kModeFlag_Rel | kModeFlag_RelLong;
		}
	}

	return s;
}

const char *ATDebuggerCmdAssemble::ParseExpression(const char *s, sint32& value) {
	enum {
		kOpAdd,
		kOpSubtract,
		kOpMultiply,
		kOpDivide,
		kOpNegate,
		kOpTakeLowByte,
		kOpTakeHighByte
	};

	enum {
		kPrecAdd = 0x100,
		kPrecMul = 0x200,
		kPrecUnary = 0x300
	};

	vdfastvector<sint32> valstack;
	vdfastvector<uint32> opstack;
	uint32 parenLevel = 0;
	bool expectingValue = true;

	opstack.push_back(0);

	for(;;) {
		char c = *s++;

		if (c == ' ')
			continue;

		if (expectingValue) {
			if (c == '+') {
				// just ignore unary +
			} else if (c == '-') {
				opstack.push_back(kOpNegate + kPrecUnary + parenLevel);
			} else if (c == '<') {
				opstack.push_back(kOpTakeLowByte + kPrecUnary + parenLevel);
			} else if (c == '>') {
				opstack.push_back(kOpTakeHighByte + kPrecUnary + parenLevel);
			} else if (c == '(') {
				parenLevel += 0x10000;
			} else if (c == '*') {
				valstack.push_back(mAddress);
				expectingValue = false;
			} else if (c == '$') {
				c = *s++;

				if (!isxdigit((unsigned char)c))
					throw ParseException(s-1);

				sint32 val = 0;
				do {
					uint8 v = (uint8)((uint8)c - '0');
					if (v >= 10)
						v = (c & 0xdf) - 'A' + 10;

					val = (val << 4) + (sint32)v;

					c = *s++;
				} while(isxdigit((unsigned char)c));

				--s;

				valstack.push_back(val);
				expectingValue = false;
			} else if ((uint8)(c - '0') < 10) {
				sint32 val = (uint8)(c - '0');

				for(;;) {
					c = *s++;

					uint8 v = (uint8)((uint8)c - '0');

					if (v >= 10)
						break;

					val = (val * 10) + v;
				}

				--s;

				valstack.push_back(val);
				expectingValue = false;
			} else if (IsIdentStart(c)) {
				const char *identStart = s-1;

				while(IsIdentNext(*++s))
					;

				const char *identEnd = s;

				sint32 val = mpDebugger->ResolveSymbol(VDStringA(identStart, identEnd).c_str(), false, false, false);

				if (val < 0)
					throw ParseException(identStart, "Unknown symbol");

				valstack.push_back(val);
				expectingValue = false;
			} else
				throw ParseException(s-1, "Value expected");
		} else {
			if (c == ')') {
				if (parenLevel) {
					parenLevel -= 0x10000;
					continue;
				}
			}

			uint32 opcode = 0;

			if (c == '+') {
				opcode = kOpAdd + kPrecAdd + parenLevel;
			} else if (c == '-') {
				opcode = kOpSubtract + kPrecAdd + parenLevel;
			} else if (c == '*') {
				opcode = kOpMultiply + kPrecMul + parenLevel;
			} else if (c == '/') {
				opcode = kOpDivide + kPrecMul + parenLevel;
			} else {
				if (parenLevel)
					throw ParseException(s-1, "Missing ')'");
			}

			uint32 opprec = (opcode & 0xffffff00);
			while((opstack.back() & 0xffffff00) > opprec) {
				sint32 *sp = &valstack.back();
				uint32 reduceOp = opstack.back() & 0xff;

				opstack.pop_back();

				switch(reduceOp) {
					case kOpAdd:
						sp[-1] += *sp;
						valstack.pop_back();
						break;
					case kOpSubtract:
						sp[-1] -= *sp;
						valstack.pop_back();
						break;
					case kOpMultiply:
						sp[-1] *= *sp;
						valstack.pop_back();
						break;
					case kOpDivide:
						if (!*sp)
							throw ParseException(s-1, "Division by zero");

						// -0x80000000 / -1 throws an overflow exception... which we don't care about
						if (sp[-1] != INT_MIN || *sp != -1)
							sp[-1] /= *sp;

						valstack.pop_back();
						break;
					case kOpNegate:
						*sp = -*sp;
						break;
					case kOpTakeLowByte:
						*sp &= 0xff;
						break;
					case kOpTakeHighByte:
						*sp = ((uint32)*sp >> 8) & 0xff;
						break;
				}
			}

			if (!opcode)
				break;

			opstack.push_back(opcode);

			expectingValue = true;
		}
	}

	VDASSERT(valstack.size() == 1);
	VDASSERT(opstack.size() == 1);

	value = valstack.back();
	return s-1;
}

void ATDebuggerCmdAssemble::MakeOperandShiftable(const char *loc, OperandInfo& opinfo, uint32 len) {
	const uint32 kZpModes = kModeFlag_Zp | kModeFlag_ZpX | kModeFlag_ZpY;

	if (opinfo.mOpModes & kZpModes) {
		if (((opinfo.mOpValue + len) & 0xffff) > 0x100)
			opinfo.mOpModes &= ~kZpModes;
	}

	const uint32 kIncModes = kModeFlag_Imm | kModeFlag_Abs | kModeFlag_AbsX | kModeFlag_AbsY | kModeFlag_AbsLong | kModeFlag_AbsLongX;

	opinfo.mOpModes &= kZpModes | kIncModes;

	if (!opinfo.mOpModes)
		throw ParseException(loc, "Operand must be incrementable");
}

void ATDebuggerCmdAssemble::ShiftOperand(const OperandInfo& opinfo, OperandInfo& opdst, uint32 offset) {
	opdst = opinfo;

	if (opinfo.mOpModes & kModeFlag_Imm)
		opdst.mOpValue = (opinfo.mOpValue >> (offset * 8)) & 0xff;
	else
		opdst.mOpValue += offset;
}

void ATDebuggerCmdAssemble::EmitCompositeINCDEC(const char *loc, const OpcodeEntry *opcode, const OperandInfo& opinfo, uint32 len) {
	VDASSERT(len <= 4);

	OperandInfo optemp(opinfo);

	MakeOperandShiftable(loc, optemp, len);

	size_t patchloc[3];

	EmitOpcode(loc, opcode, optemp, false);
	for(uint32 i=1; i<len; ++i) {
		mEmittedBytes.push_back(0xD0);		// BNE
		mEmittedBytes.push_back(0);
		patchloc[i-1] = mEmittedBytes.size();
		++optemp.mOpValue;
		EmitOpcode(loc, opcode, optemp, false);
	}

	size_t n = mEmittedBytes.size();
	for(uint32 i=1; i<len; ++i)
		mEmittedBytes[patchloc[i-1] - 1] = (uint8)(n - patchloc[i-1]);
}

const char *ATDebuggerCmdAssemble::EmitCompositeADx(const char *s, const OpcodeEntry *flagsOpcode, const OpcodeEntry *aluOpcode, uint32 len) {
	OperandInfo op1;
	OperandInfo op2;
	OperandInfo op3;

	s = ParseOperand(s, op1);
	s = ParseOperand(s, op2);
	s = ParseOperand(s, op3);

	EmitOpcode(s, flagsOpcode, OperandInfoImplied(), false);

	if (len == 1) {
		EmitOpcode(s, kOpcodes_LDA, op1, false, false);
		EmitOpcode(s, aluOpcode, op2, false, false);
		EmitOpcode(s, kOpcodes_STA, op3, false, false);
	} else {
		for(uint32 i=0; i<len; ++i) {
			OperandInfo op1a;
			OperandInfo op2a;
			OperandInfo op3a;
			ShiftOperand(op1, op1a, i);
			ShiftOperand(op2, op2a, i);
			ShiftOperand(op3, op3a, i);
			EmitOpcode(s, kOpcodes_LDA, op1a, false, false);
			EmitOpcode(s, aluOpcode, op2a, false, false);
			EmitOpcode(s, kOpcodes_STA, op3a, false, false);
		}
	}

	op1.mPostIncX *= len;
	op1.mPostIncY *= len;
	op2.mPostIncX *= len;
	op2.mPostIncY *= len;
	op3.mPostIncX *= len;
	op3.mPostIncY *= len;

	EmitPostIncrements(op1, false);
	EmitPostIncrements(op2, false);
	EmitPostIncrements(op3, false);

	return s;
}

const char *ATDebuggerCmdAssemble::EmitCompositeMVx(const char *s, const OpcodeEntry *loadOpcode, const OpcodeEntry *storeOpcode) {
	OperandInfo op1;
	OperandInfo op2;

	s = ParseOperand(s, op1);
	s = ParseOperand(s, op2);
	EmitOpcode(s, loadOpcode, op1, false, false);
	EmitOpcode(s, storeOpcode, op2, false, false);
	EmitPostIncrements(op1, false);
	EmitPostIncrements(op2, false);

	return s;
}

const char *ATDebuggerCmdAssemble::EmitCompositeMWx(const char *s, const OpcodeEntry *loadOpcode, const OpcodeEntry *storeOpcode) {
	OperandInfo op1;
	OperandInfo op2;
	OperandInfo op3;

	s = ParseOperand(s, op1);
	s = ParseOperand(s, op2);
	MakeOperandShiftable(s, op1, 2);
	MakeOperandShiftable(s, op2, 2);
	ShiftOperand(op1, op3, 0);
	EmitOpcode(s, loadOpcode, op3, false, false);
	ShiftOperand(op2, op3, 0);
	EmitOpcode(s, storeOpcode, op3, false, false);
	ShiftOperand(op1, op3, 1);
	EmitOpcode(s, loadOpcode, op3, false, false);
	ShiftOperand(op2, op3, 1);
	EmitOpcode(s, storeOpcode, op3, false, false);
	EmitPostIncrements(op1, true);
	EmitPostIncrements(op2, true);

	return s;
}

const char *ATDebuggerCmdAssemble::EmitCompositeCPx(const char *s, uint32 len) {
	VDASSERT(len <= 4);

	OperandInfo op1;
	OperandInfo op2;
	OperandInfo op3;
	OperandInfo op4;

	s = ParseOperand(s, op1);
	s = ParseOperand(s, op2);

	if (op1.mPostIncX | op1.mPostIncY | op2.mPostIncX | op2.mPostIncY)
		throw ParseException(s, "Cannot use post-increment with CPB/CPW/CPL/CPD");

	if (len == 1) {
		EmitOpcode(s, kOpcodes_LDA, op1, false, false);
		EmitOpcode(s, kOpcodes_CMP, op2, false, false);
		EmitPostIncrements(op1, false);
		EmitPostIncrements(op2, false);
	} else {
		MakeOperandShiftable(s, op1, len);
		MakeOperandShiftable(s, op2, len);

		size_t patchlocs[4];

		// compare high bytes
		for(uint32 i = len; i; --i) {
			ShiftOperand(op1, op3, i - 1);
			ShiftOperand(op2, op4, i - 1);
			EmitOpcode(s, kOpcodes_LDA, op3, false, false);
			EmitOpcode(s, kOpcodes_CMP, op4, false, false);

			// emit branch
			if (i > 1) {
				mEmittedBytes.push_back(0xD0);		// BNE
				mEmittedBytes.push_back(0);
				patchlocs[i-1] = mEmittedBytes.size();
			}
		}

		// patch branches
		size_t n = mEmittedBytes.size();
		for(uint32 i = len - 1; i; --i) {
			mEmittedBytes[patchlocs[i] - 1] = (uint8)(n - patchlocs[i]);
		}
	}

	return s;
}

void ATDebuggerCmdAssemble::EmitOpcode(const char *loc, const OpcodeEntry *opcodes, const OperandInfo& opinfo, bool allowImpliedFallback, bool doPostIncrements) {
	// look up opcode with addressing mode
	const OpcodeEntry *opBest = NULL;
	uint8 opBestMode;
	for(const OpcodeEntry *op = opcodes; !(op->mFlags & kFlagEnd); ++op) {
		uint8 mode = op->mFlags & kModeMask;

		if (opinfo.mOpModes & ((uint32)1 << mode)) {
			opBest = op;
			opBestMode = mode;
			break;
		}

		// For a multi-command line, we drop back to implied if we can't find a matching
		// addressing mode.
		if (allowImpliedFallback && mode == kModeImp) {
			opBest = op;
			opBestMode = mode;
		}
	}

	if (!opBest)
		throw ParseException(loc, "Addressing mode not supported for instruction");

	mEmittedBytes.push_back(opBest->mEncoding);

	switch(opBestMode) {
		case kModeImp:
			break;

		case kModeImm:
			{
				bool imm16 = false;

				// Here we do a cheat and look up the opcode to see whether it is M or X sensitive.
				switch(opBest->mEncoding) {
					case 0x09:	// ORA #imm
					case 0x29:	// AND #imm
					case 0x49:	// EOR #imm
					case 0x69:	// ADC #imm
					case 0x89:	// BIT #imm
					case 0xA9:	// LDA #imm
					case 0xC9:	// CMP #imm
					case 0xE9:	// SBC #imm
						imm16 = mbLongM;
						break;

					case 0xA0:	// LDY #imm
					case 0xA2:	// LDX #imm
					case 0xC0:	// CPY #imm
					case 0xE0:	// CPX #imm
						imm16 = mbLongX;
						break;
				}

				if (imm16) {
					if ((uint32)opinfo.mOpValue >= 0x10000)
						throw ParseException(loc, "Immediate value out of range (16-bit mode)");

					mEmittedBytes.push_back((uint8)opinfo.mOpValue);
					mEmittedBytes.push_back((uint8)((uint32)opinfo.mOpValue >> 8));
				} else {
					if ((uint32)opinfo.mOpValue >= 0x100)
						throw ParseException(loc, "Immediate value out of range (8-bit mode)");

					mEmittedBytes.push_back((uint8)opinfo.mOpValue);
				}
			}
			break;

		case kModeIVec:
			if ((uint32)opinfo.mOpValue >= 0x100)
				throw ParseException(loc, "Immediate value out of range (8-bit mode)");

			mEmittedBytes.push_back((uint8)opinfo.mOpValue);
			break;

		case kModeZp:
		case kModeZpX:
		case kModeZpY:
		case kModeZpI:
		case kModeIndX:
		case kModeIndY:
		case kModeIndLong:
		case kModeIndLongY:
		case kModeStack:
		case kModeStackIndY:
			mEmittedBytes.push_back((uint8)opinfo.mOpValue);
			break;

		case kModeAbs:
		case kModeAbsX:
		case kModeAbsY:
		case kModeAbsI:
		case kModeAbsIX:
		case kModeAbsLongI:
			mEmittedBytes.push_back((uint8)opinfo.mOpValue);
			mEmittedBytes.push_back((uint8)((uint32)opinfo.mOpValue >> 8));
			break;
		case kModeMove:
			mEmittedBytes.push_back((uint8)((uint32)opinfo.mOpValue >> 8));
			mEmittedBytes.push_back((uint8)opinfo.mOpValue);
			break;

		case kModeAbsLong:
		case kModeAbsLongX:
			mEmittedBytes.push_back((uint8)opinfo.mOpValue);
			mEmittedBytes.push_back((uint8)((uint32)opinfo.mOpValue >> 8));
			mEmittedBytes.push_back((uint8)((uint32)opinfo.mOpValue >> 16));
			break;

		case kModeRel:
			{
				uint32 offset = (opinfo.mOpValue - (mAddress + mEmittedBytes.size() + 1)) & 0xffff;

				if ((opinfo.mOpValue ^ mAddress) & 0xff0000)
					throw ParseException(loc, "Branch crosses bank boundary");

				if (offset >= 0x80 && offset < 0xFF80) {
					// Check if we had relative long mode also available.
					// If so, use that instead of failing.
					for(const OpcodeEntry *op = opcodes; !(op->mFlags & kFlagEnd); ++op) {
						const uint8 mode = op->mFlags & kModeMask;

						if (mode == kModeRelLong) {
							mEmittedBytes.back() = op->mEncoding;
							goto use_rel_long;
						}
					}

					throw ParseException(loc, "Branch target out of range");
				} else {
					mEmittedBytes.push_back((uint8)offset);
				}
			}
			break;

		case kModeRelLong:
use_rel_long:
			if ((opinfo.mOpValue ^ mAddress) & 0xff0000)
				throw ParseException(loc, "Long branch crosses bank boundary");

			{
				uint32 offset = opinfo.mOpValue - (mAddress + (uint32)mEmittedBytes.size() + 2);

				mEmittedBytes.push_back((uint8)offset);
				mEmittedBytes.push_back((uint8)(offset >> 8));
			}
			break;
	}

	// emit postincrements
	if (doPostIncrements)
		EmitPostIncrements(opinfo, false);
}

void ATDebuggerCmdAssemble::EmitPostIncrements(const OperandInfo& opinfo, bool do2x) {
	int x = opinfo.mPostIncX;
	int y = opinfo.mPostIncY;

	if (do2x) {
		x += x;
		y += y;
	}

	for(int i=0; i<x; ++i)
		mEmittedBytes.push_back(0xE8);

	for(int i=0; i<y; ++i)
		mEmittedBytes.push_back(0xC8);
}

bool ATDebuggerCmdAssemble::IsIdentStart(char c) {
	return	(unsigned char)((c & 0xdf) - 'A') < 26 ||
			c == '.' ||
			c == '_' ||
			c == '?';
}

bool ATDebuggerCmdAssemble::IsIdentNext(char c) {
	return	(unsigned char)((c & 0xdf) - 'A') < 26 ||
			(unsigned char)(c - '0') < 10 ||
			c == '.' ||
			c == '_' ||
			c == '?';
}

//////////////////////////////////////////////////////////////////////////

void ATCreateDebuggerCmdAssemble(uint32 address, IATDebuggerActiveCommand **ppcmd) {
	IATDebuggerActiveCommand *cmd = new ATDebuggerCmdAssemble(address);
	cmd->AddRef();

	*ppcmd = cmd;
}
