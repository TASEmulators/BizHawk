//	Altirra - Atari 800/800XL/5200 emulator
//	Coprocessor library - 65802 emulator
//	Copyright (C) 2009-2015 Avery Lee
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

///////////////////////////////////////////////////////////////////////////

#define AT_CPU_READ_BYTE(addr) ATCP_READ_BYTE((addr))
#define AT_CPU_READ_BYTE_ADDR16(addr) ATCP_READ_BYTE((addr))
#define AT_CPU_DUMMY_READ_BYTE(addr) (0)
#define AT_CPU_READ_BYTE_HL(addrhi, addrlo) ATCP_READ_BYTE(((uint32)(uint8)(addrhi) << 8) + (uint8)(addrlo))
#define AT_CPU_WRITE_BYTE(addr, value) ATCP_WRITE_BYTE((addr), (value))
#define AT_CPU_WRITE_BYTE_HL(addrhi, addrlo, value) ATCP_WRITE_BYTE(((uint32)(uint8)(addrhi) << 8) + (uint8)(addrlo), (value))

#define INSN_FETCH() AT_CPU_EXT_READ_BYTE(rPC, rK); ++rPC
#define INSN_FETCH_TO(dest) AT_CPU_EXT_READ_BYTE(rPC, rK); ++rPC; (dest) = readData
#define INSN_FETCH_TO_2(dest, slowFlag) AT_CPU_EXT_READ_BYTE_2(rPC, rK, slowFlag); ++rPC; (dest) = readData
#define INSN_DUMMY_FETCH_NOINC() AT_CPU_DUMMY_EXT_READ_BYTE(rPC, rK)
#define INSN_FETCH_NOINC() AT_CPU_EXT_READ_BYTE(rPC, rK)

#define AT_CPU_DUMMY_EXT_READ_BYTE(addr, bank) (0)
#define AT_CPU_EXT_READ_BYTE(addr, bank) readData = ATCP_READ_BYTE((addr))
#define AT_CPU_EXT_READ_BYTE_2(addr, bank, slowFlag) readData = AT_CPU_EXT_READ_BYTE((addr), (bank))
#define AT_CPU_EXT_WRITE_BYTE(addr, bank, value) AT_CPU_WRITE_BYTE((addr), (value))
#define END_SUB_CYCLE() goto next_cycle;

#define ATCP_CYCLES(n) cyclesLeft -= (n)

#define ATCP_SWITCH_DECODE_TABLES()	\
	mSH = rSH;	\
	mXH = rXH;	\
	mYH = rYH;	\
	mP = rP;	\
	UpdateDecodeTable();	\
	rP = mP;	\
	rYH = mYH;	\
	rXH = mXH;	\
	rSH = mSH


// hoist all registers to locals
uint8	rA	= mA;
uint8	rAH	= mAH;
uint8	rP	= mP;
uint8	rX	= mX;
uint8	rXH	= mXH;
uint8	rY	= mY;
uint8	rYH	= mYH;
uint8	rS	= mS;
uint8	rSH	= mSH;
uint16	rDP	= mDP;
uint8	rB	= mB;
uint8	rK	= mK;
uint16	rPC	= mPC;
uint8	rData = mData;
uint16	rData16 = mData16;
uint16	rAddr = mAddr;
uint16	rAddr2 = mAddr2;
uint8	rAddrBank = mAddrBank;

enum : uint8 {
	kFlagN = 0x80,
	kFlagV = 0x40,
	kFlagM = 0x20,
	kFlagX = 0x10,
	kFlagB = 0x10,
	kFlagD = 0x08,
	kFlagI = 0x04,
	kFlagZ = 0x02,
	kFlagC = 0x01,
};

while(cyclesLeft > 0) {

///////////////////////////////////////////////////////////////////////////

for(;;) {
	uint8 readData;

	switch(*nextState++) {
		case kStateNop:
			break;

		case kStateReadOpcode:
			if (mpBreakpointMap[rPC]) {
				mA	= rA;
				mAH	= rAH;
				mB	= rB;
				mP	= rP;
				mX	= rX;
				mXH	= rXH;
				mY	= rY;
				mYH	= rYH;
				mS	= rS;
				mSH	= rSH;
				mDP	= rDP;
				mB	= rB;
				mK	= rK;
				mPC	= rPC;
				mInsnPC = rPC;
				mCyclesLeft = cyclesLeft;

				bool shouldExit = CheckBreakpoint();

				rA	= mA;
				rAH	= mAH;
				rP	= mP;
				rX	= mX;
				rXH	= mXH;
				rY	= mY;
				rYH	= mYH;
				rS	= mS;
				rSH	= mSH;
				rDP	= mDP;
				rB	= mB;
				rK	= mK;
				rPC	= mPC;

				if (shouldExit) {
					nextState = mpNextState;
					goto force_exit;
				}
			}
		case kStateReadOpcodeNoBreak:
			mInsnPC = rPC;

			if (mExtraFlags) {
				mP = rP;
				DoExtra();
				rP = mP;
			}

			{
				bool slowFlag = false;
				uint8 opcode;
				INSN_FETCH_TO_2(opcode, slowFlag);
				nextState = mDecoderTables.mDecodeHeap + mpDecodePtrs[opcode];

#if 0
			VDDEBUG("Executing opcode: PC=%04X A=%02X%02X X=%02X%02X Y=%02X%02X P=%02X (%c%c%c%c%c%c%c%c) S=%02X%02X %02X %02X %02X\n",
				mInsnPC,
				rAH,
				rA,
				rXH,
				rX,
				rYH,
				rY,
				rP,
				rP & 0x80 ? 'N' : '-',
				rP & 0x40 ? 'V' : '-',
				rP & 0x20 ? 'M' : '-',
				rP & 0x10 ? 'X' : '-',
				rP & 0x08 ? 'D' : '-',
				rP & 0x04 ? 'I' : '-',
				rP & 0x02 ? 'Z' : '-',
				rP & 0x01 ? 'C' : '-',
				rSH,
				rS, opcode, ATCP_READ_BYTE(rPC), ATCP_READ_BYTE(rPC+1));
#endif
			}

			END_SUB_CYCLE();

		case kStateReadImm:
			INSN_FETCH_TO(rData);
			END_SUB_CYCLE();

		case kStateReadAddrL:
			INSN_FETCH_TO(rAddr);
			END_SUB_CYCLE();

		case kStateReadAddrH:
			INSN_FETCH();
			rAddr += (uint16)readData << 8;
			END_SUB_CYCLE();

		case kStateReadDummyOpcode:
			INSN_FETCH_NOINC();
			END_SUB_CYCLE();

		case kStateWait:
			INSN_DUMMY_FETCH_NOINC();
			END_SUB_CYCLE();

		case kStateAtoD:
			rData = rA;
			break;

		case kStateXtoD:
			rData = rX;
			break;

		case kStateYtoD:
			rData = rY;
			break;

		case kStateStoD:
			rData = rS;
			break;

		case kStatePtoD:
			rData = rP;
			break;

		case kStatePtoD_B0:
			rData = rP & ~kFlagB;
			break;

		case kStatePtoD_B1:
			rData = rP | kFlagB;
			break;

		case kState0toD:
			rData = 0;
			break;

		case kStateDtoA:
			rA = rData;
			break;

		case kStateDtoX:
			rX = rData;
			break;

		case kStateDtoY:
			rY = rData;
			break;

		case kStateDtoS:
			rS = rData;
			break;

		case kStateDtoP:
			rP = rData | 0x30;
			break;

		case kStateDSetSZ:
			{
				uint8 p = rP & ~(kFlagN | kFlagZ);

				p += (rData & 0x80);	// N

				if (!rData)
					p |= kFlagZ;

				rP = p;
			}
			break;

		case kStateDSetSZToA:
			{
				uint8 p = rP & ~(kFlagN | kFlagZ);

				p |= (rData & 0x80);	// copy N
				if (!rData)
					p |= kFlagZ;

				rP = p;
				rA = rData;
			}
			break;

		case kStateDSetSV:
			rP &= ~(kFlagN | kFlagV);
			rP |= (rData & 0xC0);
			break;

		case kStateAddrToPC:
			rPC = rAddr;
			break;

		case kStateNMIVecToPC:
			rPC = 0xFFFA;
			break;

		case kStateIRQVecToPC:
			rPC = 0xFFFE;
			break;

		case kStatePush:
			AT_CPU_WRITE_BYTE(0x100 + (uint8)rS--, rData);
			END_SUB_CYCLE();

		case kStatePushPCL:
			AT_CPU_WRITE_BYTE(0x100 + (uint8)rS--, rPC & 0xff);
			END_SUB_CYCLE();

		case kStatePushPCH:
			AT_CPU_WRITE_BYTE(0x100 + (uint8)rS--, rPC >> 8);
			END_SUB_CYCLE();

		case kStatePop:
			rData = AT_CPU_READ_BYTE(0x100 + (uint8)++rS);
			END_SUB_CYCLE();

		case kStatePopPCL:
			rPC = AT_CPU_READ_BYTE(0x100 + (uint8)++rS);
			END_SUB_CYCLE();

		case kStatePopPCH:
			rPC += AT_CPU_READ_BYTE(0x100 + (uint8)++rS) << 8;
			END_SUB_CYCLE();

		case kStatePopPCHP1:
			rPC += AT_CPU_READ_BYTE(0x100 + (uint8)++rS) << 8;
			++rPC;
			END_SUB_CYCLE();

		case kStateCmp:
			{
				// must leave data alone to not break DCP
				uint32 result = rA + (rData ^ 0xff) + 1;

				uint8 p = (rP & ~(kFlagC | kFlagN | kFlagZ));

				p += (result & 0x80);	// N
				p += (result >> 8);

				if (!(result & 0xff))
					p += kFlagZ;

				rP = p;
			}
			break;

		case kStateCmpX:
			{
				rData ^= 0xff;
				uint32 result = rX + rData + 1;

				rP &= ~(kFlagC | kFlagN | kFlagZ);

				rP |= (result & 0x80);	// N

				if (result >= 0x100)
					rP |= kFlagC;

				if (!(result & 0xff))
					rP |= kFlagZ;
			}
			break;

		case kStateCmpY:
			{
				rData ^= 0xff;
				uint32 result = rY + rData + 1;

				rP &= ~(kFlagC | kFlagN | kFlagZ);

				rP |= (result & 0x80);	// N

				if (result >= 0x100)
					rP |= kFlagC;

				if (!(result & 0xff))
					rP |= kFlagZ;
			}
			break;

		case kStateInc:
			{
				++rData;

				uint8 p = rP & ~(kFlagN | kFlagZ);
				p += (rData & 0x80);	// N

				if (!rData)
					p += kFlagZ;

				rP = p;
			}
			break;

		case kStateDec:
			{
				--rData;
				uint8 p = rP & ~(kFlagN | kFlagZ);
				p += (rData & 0x80);	// N

				if (!rData)
					p += kFlagZ;

				rP = p;
			}
			break;

		case kStateAnd:
			{
				rData &= rA;

				uint8 p = rP & ~(kFlagN | kFlagZ);

				p += (rData & 0x80);	// N

				if (!rData)
					p += kFlagZ;

				rP = p;
			}
			break;

		case kStateOr:
			rA |= rData;
			rP &= ~(kFlagN | kFlagZ);
			rP |= (rA & 0x80);	// N
			if (!rA)
				rP |= kFlagZ;
			break;

		case kStateXor:
			rA ^= rData;
			rP &= ~(kFlagN | kFlagZ);
			rP |= (rA & 0x80);	// N
			if (!rA)
				rP |= kFlagZ;
			break;

		case kStateAsl:
			rP &= ~(kFlagN | kFlagZ | kFlagC);
			if (rData & 0x80)
				rP |= kFlagC;
			rData += rData;
			rP |= (rData & 0x80);	// N
			if (!rData)
				rP |= kFlagZ;
			break;

		case kStateLsr:
			rP &= ~(kFlagN | kFlagZ | kFlagC);
			if (rData & 0x01)
				rP |= kFlagC;
			rData >>= 1;
			if (!rData)
				rP |= kFlagZ;
			break;

		case kStateRol:
			{
				uint32 result = (uint32)rData + (uint32)rData + (rP & kFlagC);
				rP &= ~(kFlagN | kFlagZ | kFlagC);
				if (result & 0x100)
					rP |= kFlagC;
				rData = (uint8)result;
				if (rData & 0x80)
					rP |= kFlagN;
				if (!rData)
					rP |= kFlagZ;
			}
			break;

		case kStateRor:
			{
				uint32 result = (rData >> 1) + ((rP & kFlagC) << 7);
				rP &= ~(kFlagN | kFlagZ | kFlagC);
				if (rData & 0x1)
					rP |= kFlagC;
				rData = (uint8)result;
				if (rData & 0x80)
					rP |= kFlagN;
				if (!rData)
					rP |= kFlagZ;
			}
			break;

		case kStateBit:
			{
				uint8 result = rData & rA;
				rP &= ~kFlagZ;
				if (!result)
					rP |= kFlagZ;
			}
			break;

		case kStateSEI:
			rP |= kFlagI;
			break;

		case kStateCLI:
			rP &= ~kFlagI;
			break;

		case kStateSEC:
			rP |= kFlagC;
			break;

		case kStateCLC:
			rP &= ~kFlagC;
			break;

		case kStateSED:
			rP |= kFlagD;
			break;

		case kStateCLD:
			rP &= ~kFlagD;
			break;

		case kStateCLV:
			rP &= ~kFlagV;
			break;

		case kStateJs:
			if (!(rP & kFlagN)) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJns:
			if (rP & kFlagN) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJc:
			if (!(rP & kFlagC)) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJnc:
			if (rP & kFlagC) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJz:
			if (!(rP & kFlagZ)) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJnz:
			if (rP & kFlagZ) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJo:
			if (!(rP & kFlagV)) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateJno:
			if (rP & kFlagV) {
				++nextState;
				break;
			}

			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		/////////
		case kStateJccFalseRead:
			AT_CPU_DUMMY_READ_BYTE(rAddr);
			END_SUB_CYCLE();

		case kStateWaitForInterrupt:
			--nextState;
			END_SUB_CYCLE();

		case kStateStop:
			--nextState;
			END_SUB_CYCLE();

		case kStateJ:
			INSN_FETCH_NOINC();
			rAddr = rPC & 0xff00;
			rPC += (sint16)(sint8)rData;
			rAddr += rPC & 0xff;
			if (rAddr == rPC) {
				++nextState;
			}
			END_SUB_CYCLE();

		case kStateTrb:
			rP &= ~kFlagZ;
			if (!(rData & rA))
				rP |= kFlagZ;

			rData &= ~rA;
			break;

		case kStateTsb:
			rP &= ~kFlagZ;
			if (!(rData & rA))
				rP |= kFlagZ;

			rData |= rA;
			break;

		case kStateC02_Adc:
			if (rP & kFlagD) {
				uint32 lowResult = (rA & 15) + (rData & 15) + (rP & kFlagC);
				if (lowResult >= 10)
					lowResult += 6;

				if (lowResult >= 0x20)
					lowResult -= 0x10;

				uint32 highResult = (rA & 0xf0) + (rData & 0xf0) + lowResult;

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				rP |= (((highResult ^ rA) & ~(rData ^ rA)) >> 1) & kFlagV;

				if (highResult >= 0xA0)
					highResult += 0x60;

				if (highResult >= 0x100)
					rP |= kFlagC;

				uint8 result = (uint8)highResult;

				if (!result)
					rP |= kFlagZ;

				if (result & 0x80)
					rP |= kFlagN;

				rA = result;
			} else {
				uint32 carry7 = (rA & 0x7f) + (rData & 0x7f) + (rP & kFlagC);
				uint32 result = carry7 + (rA & 0x80) + (rData & 0x80);

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (result & 0x80)
					rP |= kFlagN;

				if (result >= 0x100)
					rP |= kFlagC;

				if (!(result & 0xff))
					rP |= kFlagZ;

				rP |= ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

				rA = (uint8)result;

				// No extra cycle unless decimal mode is on.
				++nextState;
			}
			break;

		case kStateC02_Sbc:
			if (rP & kFlagD) {
				// Pole Position needs N set properly here for its passing counter
				// to stop correctly!

				rData ^= 0xff;

				// Flags set according to binary op
				uint32 carry7 = (rA & 0x7f) + (rData & 0x7f) + (rP & kFlagC);
				uint32 result = carry7 + (rA & 0x80) + (rData & 0x80);

				// BCD
				uint32 lowResult = (rA & 15) + (rData & 15) + (rP & kFlagC);
				if (lowResult < 0x10)
					lowResult -= 6;

				uint32 highResult = (rA & 0xf0) + (rData & 0xf0) + (lowResult & 0x1f);

				if (highResult < 0x100)
					highResult -= 0x60;

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				uint8 bcdresult = (uint8)highResult;

				if (bcdresult & 0x80)
					rP |= kFlagN;

				if (result >= 0x100)
					rP |= kFlagC;

				if (!(bcdresult & 0xff))
					rP |= kFlagZ;

				rP |= ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

				rA = bcdresult;
			} else {
				rData ^= 0xff;
				uint32 carry7 = (rA & 0x7f) + (rData & 0x7f) + (rP & kFlagC);
				uint32 result = carry7 + (rA & 0x80) + (rData & 0x80);

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (result & 0x80)
					rP |= kFlagN;

				if (result >= 0x100)
					rP |= kFlagC;

				if (!(result & 0xff))
					rP |= kFlagZ;

				rP |= ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

				rA = (uint8)result;

				// No extra cycle unless decimal mode is on.
				++nextState;
			}
			break;

		////////// 65C816 states

		case kStateReadImmL16:
			INSN_FETCH_TO(rData16);
			END_SUB_CYCLE();

		case kStateReadImmH16:
			INSN_FETCH();
			rData16 += (uint32)readData << 8;
			END_SUB_CYCLE();

		case kStateReadAddrDp:
			INSN_FETCH();
			rAddr = (uint32)readData + rDP;
			rAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpX:
			INSN_FETCH();
			rAddr = (uint32)readData + rDP + rX + ((uint32)rXH << 8);
			rAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpXInPage:
			INSN_FETCH();
			rAddr = rDP + (uint8)(readData + rX);
			rAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpY:
			INSN_FETCH();
			rAddr = (uint32)readData + rDP + rY + ((uint32)rYH << 8);
			rAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpYInPage:
			INSN_FETCH();
			rAddr = rDP + (uint8)(readData + rY);
			rAddrBank = 0;
			END_SUB_CYCLE();

		case kState816ReadIndAddrDpInPage:
			rAddr = rData + ((uint16)AT_CPU_READ_BYTE((rAddr & 0xff00) + (0xff & (rAddr + 1))) << 8);
			END_SUB_CYCLE();

		case kStateReadIndAddrDp:
			rAddr = rData + ((uint16)AT_CPU_READ_BYTE(rAddr + 1) << 8);
			rAddrBank = rB;
			END_SUB_CYCLE();

		case kStateReadIndAddrDpY:
			rAddr = rData + ((uint16)AT_CPU_READ_BYTE(rAddr + 1) << 8) + rY + ((uint32)rYH << 8);
			rAddrBank = rB;
			END_SUB_CYCLE();

		case kStateReadIndAddrDpLongH:
			rData16 = rData + ((uint16)AT_CPU_READ_BYTE(rAddr + 1) << 8);
			END_SUB_CYCLE();

		case kStateReadIndAddrDpLongB:
			rAddrBank = AT_CPU_READ_BYTE(rAddr + 2);
			rAddr = rData16;
			END_SUB_CYCLE();

		case kStateReadAddrAddY:
			{
				uint32 addr32 = (uint32)rAddr + rY + ((uint32)rYH << 8);

				rAddr = (uint16)addr32;
				rAddrBank = (uint8)(rAddrBank + (addr32 >> 16));
			}
			break;

		case kState816ReadAddrL:
			INSN_FETCH_TO(rAddr);
			rAddrBank = rB;
			END_SUB_CYCLE();

		case kState816ReadAddrH:
			INSN_FETCH();
			rAddrBank = rB;
			rAddr = (uint16)(rAddr + ((uint32)readData << 8));
			END_SUB_CYCLE();

		case kState816ReadAddrHX:
			INSN_FETCH();
			rAddrBank = rB;
			rAddr = (uint16)(rAddr + ((uint32)readData << 8) + rX + ((uint32)rXH << 8));
			END_SUB_CYCLE();

		case kState816ReadAddrAbsXSpec:
			{
				rAddr2 = (rAddr & 0xff00) + ((rAddr + rX) & 0x00ff);

				const uint32 addr32 = (uint32)rAddr + rX + ((uint32)rXH << 8);
				const uint16 newAddr = (uint16)addr32;
				const uint8 newAddrBank = (uint8)(rAddrBank + (addr32 >> 16));

				if (newAddr != rAddr2) {
					AT_CPU_DUMMY_EXT_READ_BYTE(rAddr2, rAddrBank);

					rAddr = newAddr;
					rAddrBank = newAddrBank;
					END_SUB_CYCLE();
				}

				rAddr = newAddr;
				rAddrBank = newAddrBank;
			}
			break;

		case kState816ReadAddrAbsXAlways:
			{
				rAddr2 = (rAddr & 0xff00) + ((rAddr + rX) & 0x00ff);
				AT_CPU_DUMMY_EXT_READ_BYTE(rAddr2, rAddrBank);

				const uint32 addr32 = (uint32)rAddr + rX + ((uint32)rXH << 8);
				rAddr = (uint16)addr32;
				rAddrBank = (uint8)(rAddrBank + (addr32 >> 16));

				END_SUB_CYCLE();
			}

		case kState816ReadAddrAbsYSpec:
			{
				rAddr2 = (rAddr & 0xff00) + ((rAddr + rY) & 0x00ff);

				const uint32 addr32 = (uint32)rAddr + rY + ((uint32)rYH << 8);
				const uint16 newAddr = (uint16)addr32;
				const uint8 newAddrBank = (uint8)(rAddrBank + (addr32 >> 16));

				if (newAddr != rAddr2) {
					AT_CPU_DUMMY_EXT_READ_BYTE(rAddr2, rAddrBank);

					rAddr = newAddr;
					rAddrBank = newAddrBank;
					END_SUB_CYCLE();
				}

				rAddr = newAddr;
				rAddrBank = newAddrBank;
			}
			break;

		case kState816ReadAddrAbsYAlways:
			{
				rAddr2 = (rAddr & 0xff00) + ((rAddr + rY) & 0x00ff);
				AT_CPU_DUMMY_EXT_READ_BYTE(rAddr2, rAddrBank);

				const uint32 addr32 = (uint32)rAddr + rY + ((uint32)rYH << 8);
				rAddr = (uint16)addr32;
				rAddrBank = (uint8)(rAddrBank + (addr32 >> 16));

				END_SUB_CYCLE();
			}

		case kState816ReadAddrAbsInd:
			AT_CPU_EXT_READ_BYTE(rAddr+1, rAddrBank);
			rAddr = (uint16)((uint32)rData + ((uint32)readData << 8));
			END_SUB_CYCLE();

		case kStateRead816AddrAbsLongL:
			AT_CPU_EXT_READ_BYTE(rAddr, rAddrBank);
			rData = readData;
			END_SUB_CYCLE();

		case kStateRead816AddrAbsLongH:
			AT_CPU_EXT_READ_BYTE(rAddr + 1, rAddrBank);
			rData16 = rData + ((uint32)readData << 8);
			END_SUB_CYCLE();

		case kStateRead816AddrAbsLongB:
			AT_CPU_EXT_READ_BYTE(rAddr + 2, rAddrBank);
			rAddrBank = readData;
			rAddr = rData16;
			END_SUB_CYCLE();

		case kStateReadAddrB:
			INSN_FETCH_TO(rAddrBank);
			END_SUB_CYCLE();

		case kStateReadAddrBX:
			INSN_FETCH_TO(rAddrBank);
			{
				uint32 ea = (uint32)rAddr + rX + ((uint32)rXH << 8);

				if (ea >= 0x10000)
					++rAddrBank;

				rAddr = (uint16)ea;
			}
			END_SUB_CYCLE();

		case kStateReadAddrSO:
			INSN_FETCH();
			rAddrBank = 0;
			rAddr = rS + ((uint32)rSH << 8) + readData;
			END_SUB_CYCLE();

		case kState816ReadAddrSO_AddY:
			{
				AT_CPU_DUMMY_EXT_READ_BYTE(rAddr+1, rAddrBank);

				uint32 addr32 = (uint32)rData16 + rY + ((uint32)rYH << 8);
				rAddr = (uint16)addr32;
				rAddrBank = (uint8)(rB + (addr32 >> 16));
			}
			END_SUB_CYCLE();

		case kStateBtoD:
			rData = rB;
			break;

		case kStateKtoD:
			rData = rK;
			break;

		case kState0toD16:
			rData16 = 0;
			break;

		case kStateAtoD16:
			rData16 = ((uint32)rAH << 8) + rA;
			break;

		case kStateXtoD16:
			rData16 = ((uint32)rXH << 8) + rX;
			break;

		case kStateYtoD16:
			rData16 = ((uint32)rYH << 8) + rY;
			break;

		case kStateStoD16:
			rData16 = ((uint32)rSH << 8) + rS;
			break;

		case kStateDPtoD16:
			rData16 = rDP;
			break;

		case kStateDtoB:
			rB = rData;
			break;

		case kStateDtoA16:
			rA = (uint8)rData16;
			rAH = (uint8)(rData16 >> 8);
			break;

		case kStateDtoX16:
			rX = (uint8)rData16;
			rXH = (uint8)(rData16 >> 8);
			break;

		case kStateDtoY16:
			rY = (uint8)rData16;
			rYH = (uint8)(rData16 >> 8);
			break;

		case kStateDtoPNative:
			{
				const uint8 delta = rP ^ rData;

				rP = rData;
				if (!mbEmulationFlag && (delta & 0x30)) {
					ATCP_SWITCH_DECODE_TABLES();
				}
			}
			break;

		case kStateDtoS16:
			rS = (uint8)rData16;
			if (!mbEmulationFlag)
				rSH = (uint8)(rData16 >> 8);
			break;

		case kStateDtoDP16:
			{
				const uint16 prevDP = rDP;
				rDP = rData16;

				// check if are changing DL=0 state, which requires a redecode
				if (((uint8)rDP == 0) ^ ((uint8)prevDP == 0)) {
					ATCP_SWITCH_DECODE_TABLES();
				}
			}
			break;

		case kStateDSetSZ16:
			rP &= ~(kFlagN | kFlagZ);
			if (rData16 & 0x8000)
				rP |= kFlagN;
			if (!rData16)
				rP |= kFlagZ;
			break;

		case kStateDSetSV16:
			rP &= ~(kFlagN | kFlagV);
			rP |= ((uint8)(rData16 >> 8) & 0xC0);
			break;

		case kState816WriteByte:
			AT_CPU_EXT_WRITE_BYTE(rAddr, rAddrBank, rData);
			END_SUB_CYCLE();

		case kStateWriteL16:
			AT_CPU_EXT_WRITE_BYTE(rAddr, rAddrBank, (uint8)rData16);
			END_SUB_CYCLE();

		case kStateWriteH16:
			AT_CPU_EXT_WRITE_BYTE(rAddr + 1, (uint8)((((uint32)rAddr + 1) >> 16) + rAddrBank), (uint8)(rData16 >> 8));
			END_SUB_CYCLE();

		case kStateWriteH16_DpBank:
			if (mbEmulationFlag && !(uint8)rDP) {
				AT_CPU_EXT_WRITE_BYTE(rAddr & 0xff00 + ((rAddr + 1) & 0xff), rAddrBank, (uint8)(rData16 >> 8));
			} else {
				AT_CPU_EXT_WRITE_BYTE(rAddr + 1, rAddrBank, (uint8)(rData16 >> 8));
			}

			END_SUB_CYCLE();

		case kState816ReadByte:
			AT_CPU_EXT_READ_BYTE(rAddr, rAddrBank);
			rData = readData;
			END_SUB_CYCLE();

		case kState816ReadByte_PBK:
			AT_CPU_EXT_READ_BYTE(rAddr, rK);
			rData = readData;
			END_SUB_CYCLE();

		case kStateReadL16:
			AT_CPU_EXT_READ_BYTE(rAddr, rAddrBank);
			rData16 = readData;
			END_SUB_CYCLE();

		case kStateReadH16:
			AT_CPU_EXT_READ_BYTE(rAddr + 1, (uint8)((((uint32)rAddr + 1) >> 16) + rAddrBank));
			rData16 += ((uint32)readData << 8);
			END_SUB_CYCLE();

		case kStateReadH16_DpBank:
			if (mbEmulationFlag && !(uint8)rDP) {
				AT_CPU_EXT_READ_BYTE((rAddr & 0xff00) + ((rAddr + 1) & 0xff), rAddrBank);
				rData16 += ((uint32)readData << 8);
			} else {
				AT_CPU_EXT_READ_BYTE(rAddr + 1, rAddrBank);
				rData16 += ((uint32)readData << 8);
			}

			END_SUB_CYCLE();

		case kStateAnd16:
			rData16 &= (rA + ((uint32)rAH << 8));
			rP &= ~(kFlagN | kFlagZ);
			if (rData16 & 0x8000)
				rP |= kFlagN;
			if (!rData16)
				rP |= kFlagZ;
			break;

		case kStateOr16:
			rA |= (uint8)rData16;
			rAH |= (uint8)(rData16 >> 8);
			rP &= ~(kFlagN | kFlagZ);
			if (rAH & 0x80)
				rP |= kFlagN;
			if (!(rA | rAH))
				rP |= kFlagZ;
			break;

		case kStateXor16:
			rA ^= (uint8)rData16;
			rAH ^= (uint8)(rData16 >> 8);
			rP &= ~(kFlagN | kFlagZ);
			if (rAH & 0x80)
				rP |= kFlagN;
			if (!(rA | rAH))
				rP |= kFlagZ;
			break;

		case kStateAdc16:
			if (rP & kFlagD) {
				uint32 lowResult = (rA & 15) + (rData16 & 15) + (rP & kFlagC);
				if (lowResult >= 10)
					lowResult += 6;

				uint32 midResult = (rA & 0xf0) + (rData16 & 0xf0) + lowResult;
				if (midResult >= 0xA0)
					midResult += 0x60;

				uint32 acchi = (uint32)rAH << 8;
				uint32 midHiResult = (acchi & 0xf00) + (rData16 & 0xf00) + midResult;
				if (midHiResult >= 0xA00)
					midHiResult += 0x600;

				uint32 highResult = (acchi & 0xf000) + (rData16 & 0xf000) + midHiResult;
				if (highResult >= 0xA000)
					highResult += 0x6000;

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (highResult >= 0x10000)
					rP |= kFlagC;

				if (!(highResult & 0xffff))
					rP |= kFlagZ;

				if (highResult & 0x8000)
					rP |= kFlagN;

				rA = (uint8)highResult;
				rAH = (uint8)(highResult >> 8);
			} else {
				uint32 data = rData16;
				uint32 acc = rA + ((uint32)rAH << 8);
				uint32 carry15 = (acc & 0x7fff) + (data & 0x7fff) + (rP & kFlagC);
				uint32 result = carry15 + (acc & 0x8000) + (data & 0x8000);

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (result & 0x8000)
					rP |= kFlagN;

				if (result >= 0x10000)
					rP |= kFlagC;

				if (!(result & 0xffff))
					rP |= kFlagZ;

				rP |= ((result >> 10) ^ (carry15 >> 9)) & kFlagV;

				rA = (uint8)result;
				rAH = (uint8)(result >> 8);
			}
			break;

		case kStateSbc16:
			if (rP & kFlagD) {
				uint32 data = (uint32)rData ^ 0xffff;
				uint32 acc = rA + ((uint32)rAH << 8);

				// BCD
				uint32 lowResult = (acc & 15) + (data & 15) + (rP & kFlagC);
				if (lowResult < 0x10)
					lowResult -= 6;

				uint32 midResult = (acc & 0xf0) + (data & 0xf0) + lowResult;
				if (midResult < 0x100)
					midResult -= 0x60;

				uint32 midHiResult = (acc & 0xf00) + (data & 0xf00) + midResult;
				if (midHiResult < 0x1000)
					midHiResult -= 0x600;

				uint32 highResult = (acc & 0xf000) + (data & 0xf000) + midHiResult;
				if (highResult < 0x10000)
					highResult -= 0x6000;

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (highResult & 0x8000)
					rP |= kFlagN;

				if (highResult >= 0x10000)
					rP |= kFlagC;

				if (!(highResult & 0xffff))
					rP |= kFlagZ;

				rA = (uint8)highResult;
				rAH = (uint8)(highResult >> 8);
			} else {
				uint32 acc = ((uint32)rAH << 8) + rA;
				
				uint32 d16 = (uint32)rData16 ^ 0xffff;
				uint32 carry15 = (acc & 0x7fff) + (d16 & 0x7fff) + (rP & kFlagC);
				uint32 result = carry15 + (acc & 0x8000) + (d16 & 0x8000);

				rP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (result & 0x8000)
					rP |= kFlagN;

				if (result >= 0x10000)
					rP |= kFlagC;

				if (!(result & 0xffff))
					rP |= kFlagZ;

				rP |= ((result >> 10) ^ (carry15 >> 9)) & kFlagV;

				rA = (uint8)result;
				rAH = (uint8)(result >> 8);
			}
			break;

		case kStateCmp16:
			{
				uint32 acc = ((uint32)rAH << 8) + rA;
				uint32 d16 = (uint32)rData16 ^ 0xffff;
				uint32 result = acc + d16 + 1;

				rP &= ~(kFlagC | kFlagN | kFlagZ);

				if (result & 0x8000)
					rP |= kFlagN;

				if (result >= 0x10000)
					rP |= kFlagC;

				if (!(result & 0xffff))
					rP |= kFlagZ;
			}
			break;

		case kStateInc16:
			++rData16;
			rP &= ~(kFlagN | kFlagZ);
			if (rData16 & 0x8000)
				rP |= kFlagN;
			if (!rData16)
				rP |= kFlagZ;
			break;

		case kStateDec16:
			--rData16;
			rP &= ~(kFlagN | kFlagZ);
			if (rData16 & 0x8000)
				rP |= kFlagN;
			if (!rData16)
				rP |= kFlagZ;
			break;

		case kStateRol16:
			{
				uint32 result = (uint32)rData16 + (uint32)rData16 + (rP & kFlagC);
				rP &= ~(kFlagN | kFlagZ | kFlagC);
				if (result & 0x10000)
					rP |= kFlagC;
				rData16 = (uint16)result;
				if (rData16 & 0x8000)
					rP |= kFlagN;
				if (!rData16)
					rP |= kFlagZ;
			}
			break;

		case kStateRor16:
			{
				uint32 result = ((uint32)rData16 >> 1) + ((uint32)(rP & kFlagC) << 15);
				rP &= ~(kFlagN | kFlagZ | kFlagC);
				if (rData16 & 1)
					rP |= kFlagC;
				rData16 = (uint16)result;
				if (result & 0x8000)
					rP |= kFlagN;
				if (!result)
					rP |= kFlagZ;
			}
			break;

		case kStateAsl16:
			rP &= ~(kFlagN | kFlagZ | kFlagC);
			if (rData16 & 0x8000)
				rP |= kFlagC;
			rData16 += rData16;
			if (rData16 & 0x8000)
				rP |= kFlagN;
			if (!rData16)
				rP |= kFlagZ;
			break;

		case kStateLsr16:
			rP &= ~(kFlagN | kFlagZ | kFlagC);
			if (rData16 & 0x01)
				rP |= kFlagC;
			rData16 >>= 1;
			if (!rData16)
				rP |= kFlagZ;
			break;

		case kStateBit16:
			{
				uint32 acc = rA + ((uint32)rAH << 8);
				uint16 result = rData16 & acc;

				rP &= ~kFlagZ;
				if (!result)
					rP |= kFlagZ;
			}
			break;

		case kStateTrb16:
			{
				uint32 acc = rA + ((uint32)rAH << 8);

				rP &= ~kFlagZ;
				if (!(rData16 & acc))
					rP |= kFlagZ;

				rData16 &= ~acc;
			}
			break;

		case kStateTsb16:
			{
				uint32 acc = rA + ((uint32)rAH << 8);

				rP &= ~kFlagZ;
				if (!(rData16 & acc))
					rP |= kFlagZ;

				rData16 |= acc;
			}
			break;

		case kStateCmpX16:
			{
				uint32 data = (uint32)rData16 ^ 0xffff;
				uint32 result = rX + ((uint32)rXH << 8) + data + 1;

				rP &= ~(kFlagC | kFlagN | kFlagZ);

				if (result & 0x8000)
					rP |= kFlagN;

				if (result >= 0x10000)
					rP |= kFlagC;

				if (!(result & 0xffff))
					rP |= kFlagZ;
			}
			break;

		case kStateCmpY16:
			{
				uint32 data = (uint32)rData16 ^ 0xffff;
				uint32 result = rY + ((uint32)rYH << 8) + data + 1;

				rP &= ~(kFlagC | kFlagN | kFlagZ);

				if (result & 0x8000)
					rP |= kFlagN;

				if (result >= 0x10000)
					rP |= kFlagC;

				if (!(result & 0xffff))
					rP |= kFlagZ;
			}
			break;

		case kStateXba:
			{
				uint8 t = rAH;
				rAH = rA;
				rA = t;

				rP &= ~(kFlagN | kFlagZ);

				rP |= (t & kFlagN);

				if (!t)
					rP |= kFlagZ;
			}
			break;

		case kStateXce:
			{
				bool newEmuFlag = ((rP & kFlagC) != 0);

				if (newEmuFlag != mbEmulationFlag) {
					rP &= ~kFlagC;
					if (mbEmulationFlag)
						rP |= kFlagC | kFlagM | kFlagX;

					mbEmulationFlag = newEmuFlag;
					ATCP_SWITCH_DECODE_TABLES();
				}
			}
			break;

		case kStatePushNative:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, rData);
			if (!rS-- && !mbEmulationFlag)
				--rSH;
			END_SUB_CYCLE();

		case kStatePushL16:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, (uint8)rData16);
			if (!rS--)
				--rSH;

			if (mbEmulationFlag)
				rSH = 1;
			END_SUB_CYCLE();

		case kStatePushH16:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, (uint8)(rData16 >> 8));
			if (!rS--) {
				// We intentionally allow this to underflow in emulation mode. This is a quirk of
				// the 65C816 for 16-bit non-PC pushes. This will be corrected after the low byte
				// is pushed.
				--rSH;
			}
			END_SUB_CYCLE();

		case kStatePushPBKNative:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, rK);
			if (!rS-- && !mbEmulationFlag)
				--rSH;
			END_SUB_CYCLE();

		case kStatePushPCLNative:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, rPC & 0xff);
			if (!rS--)
				--rSH;
			END_SUB_CYCLE();

		case kStatePushPCHNative:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, rPC >> 8);
			if (!rS--)
				--rSH;
			END_SUB_CYCLE();

		case kStatePushPCLM1Native:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, (rPC - 1) & 0xff);
			if (!rS-- && !mbEmulationFlag)
				--rSH;
			END_SUB_CYCLE();

		case kStatePushPCHM1Native:
			AT_CPU_WRITE_BYTE_HL(rSH, rS, (rPC - 1) >> 8);
			if (!rS-- && !mbEmulationFlag)
				--rSH;
			END_SUB_CYCLE();

		case kStatePopNative:
			if (!++rS)
				++rSH;
			rData = AT_CPU_READ_BYTE_HL(rSH, rS);
			END_SUB_CYCLE();

		case kStatePopL16:
			if (!++rS) {
				// We intentionally allow this to overflow in emulation modes to emulate a 65C816
				// quirk. It will be fixed up after we read the high byte.
				++rSH;
			}

			rData16 = AT_CPU_READ_BYTE_HL(rSH, rS);
			END_SUB_CYCLE();

		case kStatePopH16:
			if (!++rS)
				++rSH;

			rData16 += (uint32)AT_CPU_READ_BYTE_HL(rSH, rS) << 8;

			if (mbEmulationFlag)
				rSH = 1;
			END_SUB_CYCLE();

		case kStatePopPCLNative:
			if (!++rS && !mbEmulationFlag)
				++rSH;
			rPC = AT_CPU_READ_BYTE_HL(rSH, rS);
			END_SUB_CYCLE();

		case kStatePopPCHNative:
			if (!++rS && !mbEmulationFlag)
				++rSH;
			rPC += (uint32)AT_CPU_READ_BYTE_HL(rSH, rS) << 8;
			END_SUB_CYCLE();

		case kStatePopPCHP1Native:
			if (!++rS && !mbEmulationFlag)
				++rSH;
			rPC += ((uint32)AT_CPU_READ_BYTE_HL(rSH, rS) << 8) + 1;
			END_SUB_CYCLE();

		case kStatePopPBKNative:
			if (!++rS && !mbEmulationFlag)
				++rSH;
			rK = AT_CPU_READ_BYTE_HL(rSH, rS);
			END_SUB_CYCLE();

		case kStateRep:
			if (mbEmulationFlag)
				rP &= ~(rData & 0xcf);		// m and x are off-limits
			else
				rP &= ~rData;

			ATCP_SWITCH_DECODE_TABLES();
			END_SUB_CYCLE();

		case kStateSep:
			if (mbEmulationFlag)
				rP |= rData & 0xcf;		// m and x are off-limits
			else
				rP |= rData;

			ATCP_SWITCH_DECODE_TABLES();
			END_SUB_CYCLE();

		case kStateJ16:
			rPC += rData16;
			END_SUB_CYCLE();

		case kState816_NatCOPVecToPC:
			rPC = 0xFFE4;
			rK = 0;
			break;

		case kState816_EmuCOPVecToPC:
			rPC = 0xFFF4;
			break;

		case kState816_NatNMIVecToPC:
			rPC = 0xFFEA;
			rK = 0;
			break;

		case kState816_NatIRQVecToPC:
			rPC = 0xFFEE;
			rK = 0;
			break;

		case kState816_NatBRKVecToPC:
			rPC = 0xFFE6;
			rK = 0;
			break;

		case kState816_SetI_ClearD:
			rP |= kFlagI;
			rP &= ~kFlagD;
			break;

		case kState816_LongAddrToPC:
			rPC = rAddr;
			rK = rAddrBank;
			break;

		case kState816_MoveRead:
			AT_CPU_EXT_READ_BYTE(rX + ((uint32)rXH << 8), rAddrBank);
			rData = readData;
			END_SUB_CYCLE();

		case kState816_MoveWriteP:
			rAddr = rY + ((uint32)rYH << 8);
			AT_CPU_EXT_WRITE_BYTE(rAddr, rB, rData);

			if (!mbEmulationFlag && !(rP & kFlagX)) {
				if (!rX)
					--rXH;

				if (!rY)
					--rYH;
			}

			--rX;
			--rY;

			if (rA-- || rAH--)
					rPC -= 3;

			END_SUB_CYCLE();

		case kState816_MoveWriteN:
			rAddr = rY + ((uint32)rYH << 8);
			AT_CPU_EXT_WRITE_BYTE(rAddr, rB, rData);

			++rX;
			++rY;
			if (!mbEmulationFlag && !(rP & kFlagX)) {
				if (!rX)
					++rXH;

				if (!rY)
					++rYH;
			}

			if (rA-- || rAH--)
					rPC -= 3;

			END_SUB_CYCLE();

		case kState816_Per:
			rData16 += rPC;
			break;

		case kState816_SetBank0:
			rAddrBank = 0;
			break;

		case kState816_SetBankPBR:
			rAddrBank = rK;
			break;

		case kStateAddToHistory:
			{
				ATCPUHistoryEntry * VDRESTRICT he = &mpHistory[mHistoryIndex++ & 131071];

				he->mCycle = cyclesBase - cyclesLeft;
				he->mUnhaltedCycle = cyclesBase - cyclesLeft;
				he->mEA = 0xFFFFFFFFUL;
				he->mPC = rPC - 1;
				he->mP = rP;
				he->mA = rA;
				he->mX = rX;
				he->mY = rY;
				he->mS = rS;
				he->mbIRQ = false;
				he->mbNMI = false;
				he->mSubCycle = 0;
				he->mbEmulation = mbEmulationFlag;
				he->mExt.mSH = rSH;
				he->mExt.mAH = rAH;
				he->mExt.mXH = rXH;
				he->mExt.mYH = rYH;
				he->mB = rB;
				he->mK = rK;
				he->mD = rDP;

				for(int i=0; i<4; ++i) {
					uint16 pc = rPC - 1 + i;
					he->mOpcode[i] = ATCP_DEBUG_READ_BYTE(pc);
				}
			}
			break;

		case kStateRegenerateDecodeTables:
			nextState = RegenerateDecodeTables();
			break;

#ifdef _DEBUG
		default:
			VDASSERT(!"Invalid CPU state detected.");
			break;
#else
		default:
			VDNEVERHERE;
#endif
	}
}
next_cycle:
	--cyclesLeft;

}

force_exit:
// write locals back to registers
mA	= rA;
mAH	= rAH;
mB	= rB;
mP	= rP;
mX	= rX;
mXH	= rXH;
mY	= rY;
mYH	= rYH;
mS	= rS;
mSH	= rSH;
mDP	= rDP;
mB	= rB;
mK	= rK;
mPC	= rPC;
mData = rData;
mData16 = rData16;
mAddr = rAddr;
mAddr2 = rAddr2;
mAddrBank = rAddrBank;

///////////////////////////////////////////////////////////////////

#undef AT_CPU_READ_BYTE
#undef AT_CPU_READ_BYTE_ADDR16
#undef AT_CPU_DUMMY_READ_BYTE
#undef AT_CPU_READ_BYTE_HL
#undef AT_CPU_EXT_READ_BYTE
#undef AT_CPU_EXT_READ_BYTE_2
#undef AT_CPU_DUMMY_EXT_READ_BYTE
#undef AT_CPU_WRITE_BYTE
#undef AT_CPU_WRITE_BYTE_HL
#undef AT_CPU_EXT_WRITE_BYTE

#undef END_SUB_CYCLE
#undef INSN_FETCH
#undef INSN_FETCH_TO
#undef INSN_FETCH_TO_2
#undef INSN_FETCH_NOINC
#undef INSN_DUMMY_FETCH_NOINC
