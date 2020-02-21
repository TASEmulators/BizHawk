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

#include <stdafx.h>
#include <at/atcpu/breakpoints.h>
#include <at/atcpu/co6502.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atcpu/states6502.h>

#define ATCP_DUMMY_READ_BYTE(addr) ((void)(0))
#define ATCP_DEBUG_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? DebugReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? ReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_WRITE_BYTE(addr, value) ((void)(tmpaddr = (addr), tmpval = (value), tmpbase = mWriteMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? WriteByteSlow(tmpbase, tmpaddr, tmpval) : (void)(*(uint8 *)(tmpbase + tmpaddr) = tmpval))))

#define AT_CPU_READ_BYTE(addr) ATCP_READ_BYTE((addr))
#define AT_CPU_READ_BYTE_ADDR16(addr) ATCP_READ_BYTE((addr))
#define AT_CPU_DUMMY_READ_BYTE(addr) (0)
#define AT_CPU_READ_BYTE_HL(addrhi, addrlo) ATCP_READ_BYTE(((uint32)(uint8)(addrhi) << 8) + (uint8)(addrlo))
#define AT_CPU_WRITE_BYTE(addr, value) ATCP_WRITE_BYTE((addr), (value))
#define AT_CPU_WRITE_BYTE_HL(addrhi, addrlo, value) ATCP_WRITE_BYTE(((uint32)(uint8)(addrhi) << 8) + (uint8)(addrlo), (value))

#define INSN_FETCH() AT_CPU_EXT_READ_BYTE(mPC, 0); ++mPC
#define INSN_FETCH_TO(dest) AT_CPU_EXT_READ_BYTE(mPC, 0); ++mPC; (dest) = readData
#define INSN_FETCH_TO_2(dest, slowFlag) AT_CPU_EXT_READ_BYTE_2(mPC, 0, slowFlag); ++mPC; (dest) = readData
#define INSN_DUMMY_FETCH_NOINC() AT_CPU_DUMMY_EXT_READ_BYTE(mPC, 0)
#define INSN_FETCH_NOINC() AT_CPU_EXT_READ_BYTE(mPC, 0)

#define AT_CPU_DUMMY_EXT_READ_BYTE(addr, bank) (0)
#define AT_CPU_EXT_READ_BYTE(addr, bank) readData = ATCP_READ_BYTE((addr))
#define AT_CPU_EXT_READ_BYTE_2(addr, bank, slowFlag) readData = AT_CPU_EXT_READ_BYTE((addr), (bank))
#define AT_CPU_EXT_WRITE_BYTE(addr, bank, value) AT_CPU_WRITE_BYTE((addr), (value))
#define END_SUB_CYCLE() goto next_cycle;

void ATCoProc6502::Run() {
	using namespace ATCPUStates6502;

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

	if (mCyclesLeft <= 0)
		return;

	uint16 tmpaddr;
	uintptr tmpbase;
	uint8 tmpval;

	uint32		cyclesLeft = mCyclesLeft;
	const uint32 cyclesBase = mCyclesBase;

while(cyclesLeft > 0) {
	for(;;) {
		uint8 readData;

		switch(*mpNextState++) {
			case kStateNop:
				break;

			case kStateReadOpcode:
				if (mpBreakpointMap[mPC]) {
					mCyclesLeft = cyclesLeft;
					mInsnPC = mPC;

					if (CheckBreakpoint()) {
						goto force_exit;
					}
				}

				// fall through

			case kStateReadOpcodeNoBreak:
				mInsnPC = mPC;

				if (mExtraFlags)
					DoExtra();

				{
					bool slowFlag = false;
					INSN_FETCH_TO_2(mOpcode, slowFlag);
					mpNextState = mDecoderTables.mDecodeHeap + mDecoderTables.mInsnPtrs[mOpcode];
				}

				END_SUB_CYCLE();

			case kStateReadImm:
				INSN_FETCH_TO(mData);
				END_SUB_CYCLE();

			case kStateReadAddrL:
				INSN_FETCH_TO(mAddr);
				END_SUB_CYCLE();

			case kStateReadAddrH:
				INSN_FETCH();
				mAddr += (uint16)readData << 8;
				END_SUB_CYCLE();

			case kStateRead:
				mData = AT_CPU_READ_BYTE_ADDR16(mAddr);
				END_SUB_CYCLE();

			case kStateReadSetSZToA:
				{
					mA = AT_CPU_READ_BYTE_ADDR16(mAddr);

					uint8 p = mP & ~(kFlagN | kFlagZ);

					p += (mA & 0x80);	// N

					if (!mA)
						p += kFlagZ;

					mP = p;
				}
				END_SUB_CYCLE();

			case kStateReadDummyOpcode:
				INSN_FETCH_NOINC();
				END_SUB_CYCLE();

			case kStateReadAddrHX:
				INSN_FETCH();
				mAddr += (uint16)readData << 8;
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mX) & 0x00ff);
				mAddr = mAddr + mX;
				END_SUB_CYCLE();

			case kStateReadAddrHX_SHY:
				{
					uint8 hiByte;
					INSN_FETCH_TO(hiByte);
					uint32 lowSum = (uint32)mAddr + mX;

					// compute borked result from page crossing
					mData = mY & (hiByte + 1);

					// replace high byte if page crossing detected
					if (lowSum >= 0x100) {
						hiByte = mData;
						lowSum &= 0xff;
					}

					mAddr = (uint16)(lowSum + ((uint32)hiByte << 8));
				}
				END_SUB_CYCLE();

			case kStateReadAddrHY_SHA:
				{
					uint8 hiByte;
					INSN_FETCH_TO(hiByte);
					uint32 lowSum = (uint32)mAddr + mY;

					// compute borked result from page crossing
					mData = mA & mX & (hiByte + 1);

					// replace high byte if page crossing detected
					if (lowSum >= 0x100) {
						hiByte = mData;
						lowSum &= 0xff;
					}

					mAddr = (uint16)(lowSum + ((uint32)hiByte << 8));
				}
				END_SUB_CYCLE();

			case kStateReadAddrHY_SHX:
				{
					uint8 hiByte;
					INSN_FETCH_TO(hiByte);
					uint32 lowSum = (uint32)mAddr + mY;

					// compute borked result from page crossing
					mData = mX & (hiByte + 1);

					// replace high byte if page crossing detected
					if (lowSum >= 0x100) {
						hiByte = mData;
						lowSum &= 0xff;
					}

					mAddr = (uint16)(lowSum + ((uint32)hiByte << 8));
				}
				END_SUB_CYCLE();

			case kStateReadAddrHY:
				INSN_FETCH();
				mAddr += (uint16)readData << 8;
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mY) & 0x00ff);
				mAddr = mAddr + mY;
				END_SUB_CYCLE();

			case kStateReadAddX:
				mData = AT_CPU_READ_BYTE_ADDR16(mAddr);
				mAddr = (uint8)(mAddr + mX);
				END_SUB_CYCLE();

			case kStateReadAddY:
				mData = AT_CPU_READ_BYTE_ADDR16(mAddr);
				mAddr = (uint8)(mAddr + mY);
				END_SUB_CYCLE();

			case kStateReadCarry:
				mData = AT_CPU_READ_BYTE_ADDR16(mAddr2);
				if (mAddr == mAddr2)
					++mpNextState;
				END_SUB_CYCLE();

			case kStateReadCarryForced:
				mData = AT_CPU_READ_BYTE_ADDR16(mAddr2);
				END_SUB_CYCLE();

			case kStateWrite:
				AT_CPU_WRITE_BYTE(mAddr, mData);
				END_SUB_CYCLE();

			case kStateWriteA:
				AT_CPU_WRITE_BYTE(mAddr, mA);
				END_SUB_CYCLE();

			case kStateReadAbsIndAddr:
				mAddr = mData + ((uint16)AT_CPU_READ_BYTE(mAddr + 1) << 8);
				END_SUB_CYCLE();

			case kStateReadAbsIndAddrBroken:
				mAddr = mData + ((uint16)AT_CPU_READ_BYTE((mAddr & 0xff00) + ((mAddr + 1) & 0xff)) << 8);
				END_SUB_CYCLE();

			case kStateReadIndAddr:
				mAddr = mData + ((uint16)AT_CPU_READ_BYTE(0xff & (mAddr + 1)) << 8);
				END_SUB_CYCLE();

			case kStateReadIndYAddr:
				mAddr = mData + ((uint16)AT_CPU_READ_BYTE(0xff & (mAddr + 1)) << 8);
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mY) & 0x00ff);
				mAddr = mAddr + mY;
				END_SUB_CYCLE();

			case kStateReadIndYAddr_SHA:
				{
					uint32 lowSum = (uint32)mData + mY;
					uint8 hiByte = AT_CPU_READ_BYTE(0xff & (mAddr + 1));

					// compute "adjusted" high address byte
					mData = mA & mX & (hiByte + 1);

					// check for page crossing and replace high byte if so
					if (lowSum >= 0x100) {
						lowSum &= 0xff;
						hiByte = mData;
					}

					mAddr = (uint16)(lowSum + ((uint32)hiByte << 8));
				}
				END_SUB_CYCLE();

			case kStateWait:
				INSN_DUMMY_FETCH_NOINC();
				END_SUB_CYCLE();

			case kStateAtoD:
				mData = mA;
				break;

			case kStateXtoD:
				mData = mX;
				break;

			case kStateYtoD:
				mData = mY;
				break;

			case kStateStoD:
				mData = mS;
				break;

			case kStatePtoD:
				mData = mP;
				break;

			case kStatePtoD_B0:
				mData = mP & ~kFlagB;
				break;

			case kStatePtoD_B1:
				mData = mP | kFlagB;
				break;

			case kState0toD:
				mData = 0;
				break;

			case kStateDtoA:
				mA = mData;
				break;

			case kStateDtoX:
				mX = mData;
				break;

			case kStateDtoY:
				mY = mData;
				break;

			case kStateDtoS:
				mS = mData;
				break;

			case kStateDtoP:
				mP = mData | 0x30;
				break;

			case kStateDtoP_noICheck:
				mP = mData | 0x30;
				break;

			case kStateDSetSZ:
				{
					uint8 p = mP & ~(kFlagN | kFlagZ);

					p += (mData & 0x80);	// N

					if (!mData)
						p |= kFlagZ;

					mP = p;
				}
				break;

			case kStateDSetSZToA:
				{
					uint8 p = mP & ~(kFlagN | kFlagZ);

					p |= (mData & 0x80);	// copy N
					if (!mData)
						p |= kFlagZ;

					mP = p;
					mA = mData;
				}
				break;

			case kStateDSetSV:
				mP &= ~(kFlagN | kFlagV);
				mP |= (mData & 0xC0);
				break;

			case kStateAddrToPC:
				mPC = mAddr;
				break;

			case kStateNMIVecToPC:
				mPC = 0xFFFA;
				mP |= kFlagI;
				break;

			case kStateIRQVecToPC:
				mPC = 0xFFFE;
				mP |= kFlagI;
				break;

			case kStateNMIOrIRQVecToPC:
				mPC = 0xFFFE;
				mP |= kFlagI;
				break;

			case kStatePush:
				AT_CPU_WRITE_BYTE(0x100 + (uint8)mS--, mData);
				END_SUB_CYCLE();

			case kStatePushPCL:
				AT_CPU_WRITE_BYTE(0x100 + (uint8)mS--, mPC & 0xff);
				END_SUB_CYCLE();

			case kStatePushPCH:
				AT_CPU_WRITE_BYTE(0x100 + (uint8)mS--, mPC >> 8);
				END_SUB_CYCLE();

			case kStatePushPCLM1:
				AT_CPU_WRITE_BYTE(0x100 + (uint8)mS--, (mPC - 1) & 0xff);
				END_SUB_CYCLE();

			case kStatePushPCHM1:
				AT_CPU_WRITE_BYTE(0x100 + (uint8)mS--, (mPC - 1) >> 8);
				END_SUB_CYCLE();

			case kStatePop:
				mData = AT_CPU_READ_BYTE(0x100 + (uint8)++mS);
				END_SUB_CYCLE();

			case kStatePopPCL:
				mPC = AT_CPU_READ_BYTE(0x100 + (uint8)++mS);
				END_SUB_CYCLE();

			case kStatePopPCH:
				mPC += AT_CPU_READ_BYTE(0x100 + (uint8)++mS) << 8;
				END_SUB_CYCLE();

			case kStatePopPCHP1:
				mPC += AT_CPU_READ_BYTE(0x100 + (uint8)++mS) << 8;
				++mPC;
				END_SUB_CYCLE();

			case kStateAdc:
				if (mP & kFlagD) {
					// BCD
					uint8 carry = (mP & kFlagC);

					uint32 lowResult = (mA & 15) + (mData & 15) + carry;
					if (lowResult >= 10)
						lowResult += 6;

					if (lowResult >= 0x20)
						lowResult -= 0x10;

					uint32 highResult = (mA & 0xf0) + (mData & 0xf0) + lowResult;

					uint8 p = mP & ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					p += (((highResult ^ mA) & ~(mData ^ mA)) >> 1) & kFlagV;

					p += (highResult & 0x80);	// N

					if (highResult >= 0xA0)
						highResult += 0x60;

					if (highResult >= 0x100)
						p += kFlagC;

					if (!(uint8)(mA + mData + carry))
						p += kFlagZ;

					mA = (uint8)highResult;
					mP = p;
				} else {
					uint32 carry =  (mP & kFlagC);
					uint32 carry7 = (mA & 0x7f) + (mData & 0x7f) + carry;
					uint32 result = mA + mData + carry;

					uint8 p = mP & ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					p += (result & 0x80);	// N

					if (result >= 0x100)
						p += kFlagC;

					if (!(result & 0xff))
						p += kFlagZ;

					p += ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

					mP = p;
					mA = (uint8)result;
				}
				break;

			case kStateSbc:
				if (mP & kFlagD) {
					// Pole Position needs N set properly here for its passing counter
					// to stop correctly!

					mData ^= 0xff;

					// Flags set according to binary op
					uint32 carry7 = (mA & 0x7f) + (mData & 0x7f) + (mP & kFlagC);
					uint32 result = carry7 + (mA & 0x80) + (mData & 0x80);

					// BCD
					uint32 lowResult = (mA & 15) + (mData & 15) + (mP & kFlagC);
					uint32 highCarry = 0x10;
					if (lowResult < 0x10) {
						lowResult -= 6;
						highCarry = 0;
					}

					uint32 highResult = (mA & 0xf0) + (mData & 0xf0) + (lowResult & 0x0f) + highCarry;

					if (highResult < 0x100)
						highResult -= 0x60;

					uint8 p = mP & ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					p += (result & 0x80);	// N

					if (result >= 0x100)
						p += kFlagC;

					if (!(result & 0xff))
						p += kFlagZ;

					p += ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

					mP = p;
					mA = (uint8)highResult;
				} else {
					mData ^= 0xff;
					uint32 carry = (mP & kFlagC);
					uint32 carry7 = (mA & 0x7f) + (mData & 0x7f) + carry;
					uint32 result = mData + mA + carry;

					uint8 p = mP & ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					p += (result & 0x80);	// N

					if (result >= 0x100)
						p += kFlagC;

					if (!(result & 0xff))
						p += kFlagZ;

					p += ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

					mP = p;
					mA = (uint8)result;
				}
				break;

			case kStateCmp:
				{
					// must leave data alone to not break DCP
					uint32 result = mA + (mData ^ 0xff) + 1;

					uint8 p = (mP & ~(kFlagC | kFlagN | kFlagZ));

					p += (result & 0x80);	// N
					p += (result >> 8);

					if (!(result & 0xff))
						p += kFlagZ;

					mP = p;
				}
				break;

			case kStateCmpX:
				{
					mData ^= 0xff;
					uint32 result = mX + mData + 1;

					mP &= ~(kFlagC | kFlagN | kFlagZ);

					mP |= (result & 0x80);	// N

					if (result >= 0x100)
						mP |= kFlagC;

					if (!(result & 0xff))
						mP |= kFlagZ;
				}
				break;

			case kStateCmpY:
				{
					mData ^= 0xff;
					uint32 result = mY + mData + 1;

					mP &= ~(kFlagC | kFlagN | kFlagZ);

					mP |= (result & 0x80);	// N

					if (result >= 0x100)
						mP |= kFlagC;

					if (!(result & 0xff))
						mP |= kFlagZ;
				}
				break;

			case kStateInc:
				{
					++mData;

					uint8 p = mP & ~(kFlagN | kFlagZ);
					p += (mData & 0x80);	// N

					if (!mData)
						p += kFlagZ;

					mP = p;
				}
				break;

			case kStateIncXWait:
				{
					uint8 p = mP & ~(kFlagN | kFlagZ);

					++mX;
					if (!mX)
						p += kFlagZ;

					p += (mX & 0x80);	// N

					mP = p;
				}
				INSN_DUMMY_FETCH_NOINC();
				END_SUB_CYCLE();

			case kStateDec:
				{
					--mData;
					uint8 p = mP & ~(kFlagN | kFlagZ);
					p += (mData & 0x80);	// N

					if (!mData)
						p += kFlagZ;

					mP = p;
				}
				break;

			case kStateDecXWait:
				{
					uint8 p = mP & ~(kFlagN | kFlagZ);

					--mX;
					if (!mX)
						p += kFlagZ;

					p += (mX & 0x80);	// N

					mP = p;
				}
				INSN_DUMMY_FETCH_NOINC();
				END_SUB_CYCLE();

			case kStateDecC:
				--mData;
				mP |= kFlagC;
				if (!mData)
					mP &= ~kFlagC;
				break;

			case kStateAnd:
				{
					mData &= mA;

					uint8 p = mP & ~(kFlagN | kFlagZ);

					p += (mData & 0x80);	// N

					if (!mData)
						p += kFlagZ;

					mP = p;
				}
				break;

			case kStateAnd_SAX:
				mData &= mA;
				break;

			case kStateAnc:
				mData &= mA;
				mP &= ~(kFlagN | kFlagZ | kFlagC);
				if (mData & 0x80)
					mP |= kFlagN | kFlagC;
				if (!mData)
					mP |= kFlagZ;
				break;

			case kStateXaa:
				mA &= (mData & mX);
				mP &= ~(kFlagN | kFlagZ);
				if (mA & 0x80)
					mP |= kFlagN;
				if (!mA)
					mP |= kFlagZ;
				break;

			case kStateLas:
				mA = mX = mS = (mData & mS);
				mP &= ~(kFlagN | kFlagZ);
				if (mS & 0x80)
					mP |= kFlagN;
				if (!mS)
					mP |= kFlagZ;
				break;

			case kStateSbx:
				mP &= ~(kFlagN | kFlagZ | kFlagC);
				mX &= mA;
				if (mX >= mData)
					mP |= kFlagC;
				mX -= mData;
				mP |= (mX & 0x80);	// N
				if (!mX)
					mP |= kFlagZ;
				break;

			case kStateArr:
				{
					mA &= mData;

					// stash off AND result for decimal correction
					uint8 andres = mA;

					mA = (mA >> 1) + (mP << 7);
					mP &= ~(kFlagN | kFlagZ | kFlagC | kFlagV);

					switch(mA & 0x60) {
						case 0x00:	break;
						case 0x20:	mP += kFlagV; break;
						case 0x40:	mP += kFlagC | kFlagV; break;
						case 0x60:	mP += kFlagC; break;
					}

					mP += (mA & 0x80);

					if (!mA)
						mP |= kFlagZ;

					// perform BCD adjustment and correct C if in decimal mode
					if (mP & kFlagD) {
						// low adjust
						if ((andres & 15) >= 5)
							mA = (mA & 0xf0) + ((mA + 6) & 15);

						// high adjust and carry out
						mP &= ~kFlagC;
						if (andres >= 0x50) {
							mA += 0x60;
							mP |= kFlagC;
						}
					}
				}
				break;

			case kStateXas:
				mS = (mX & mA);
				mData = mS & (uint32)((mAddr >> 8) + 1);
				break;

			case kStateOr:
				mA |= mData;
				mP &= ~(kFlagN | kFlagZ);
				mP |= (mA & 0x80);	// N
				if (!mA)
					mP |= kFlagZ;
				break;

			case kStateXor:
				mA ^= mData;
				mP &= ~(kFlagN | kFlagZ);
				mP |= (mA & 0x80);	// N
				if (!mA)
					mP |= kFlagZ;
				break;

			case kStateAsl:
				mP &= ~(kFlagN | kFlagZ | kFlagC);
				if (mData & 0x80)
					mP |= kFlagC;
				mData += mData;
				mP |= (mData & 0x80);	// N
				if (!mData)
					mP |= kFlagZ;
				break;

			case kStateLsr:
				mP &= ~(kFlagN | kFlagZ | kFlagC);
				if (mData & 0x01)
					mP |= kFlagC;
				mData >>= 1;
				if (!mData)
					mP |= kFlagZ;
				break;

			case kStateRol:
				{
					uint32 result = (uint32)mData + (uint32)mData + (mP & kFlagC);
					mP &= ~(kFlagN | kFlagZ | kFlagC);
					if (result & 0x100)
						mP |= kFlagC;
					mData = (uint8)result;
					if (mData & 0x80)
						mP |= kFlagN;
					if (!mData)
						mP |= kFlagZ;
				}
				break;

			case kStateRor:
				{
					uint32 result = (mData >> 1) + ((mP & kFlagC) << 7);
					mP &= ~(kFlagN | kFlagZ | kFlagC);
					if (mData & 0x1)
						mP |= kFlagC;
					mData = (uint8)result;
					if (mData & 0x80)
						mP |= kFlagN;
					if (!mData)
						mP |= kFlagZ;
				}
				break;

			case kStateBit:
				{
					uint8 result = mData & mA;
					mP &= ~kFlagZ;
					if (!result)
						mP |= kFlagZ;
				}
				break;

			case kStateSEI:
				mP |= kFlagI;
				break;

			case kStateCLI:
				mP &= ~kFlagI;
				break;

			case kStateSEC:
				mP |= kFlagC;
				break;

			case kStateCLC:
				mP &= ~kFlagC;
				break;

			case kStateSED:
				mP |= kFlagD;
				break;

			case kStateCLD:
				mP &= ~kFlagD;
				break;

			case kStateCLV:
				mP &= ~kFlagV;
				break;

			case kStateJs:
				if (!(mP & kFlagN)) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJns:
				if (mP & kFlagN) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJc:
				if (!(mP & kFlagC)) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJnc:
				if (mP & kFlagC) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJz:
				if (!(mP & kFlagZ)) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJnz:
				if (mP & kFlagZ) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJo:
				if (!(mP & kFlagV)) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJno:
				if (mP & kFlagV) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateJccFalseRead:
				AT_CPU_READ_BYTE(mAddr);
				END_SUB_CYCLE();

			case kStateResetBit:
				mData &= ~(1 << ((mOpcode >> 4) & 7));
				break;

			case kStateSetBit:
				mData |= 1 << ((mOpcode >> 4) & 7);
				break;

			case kStateReadRel:
				INSN_FETCH_TO(mRelOffset);
				END_SUB_CYCLE();

			case kStateJ0:
				if (mData & (1 << ((mOpcode >> 4) & 7))) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mRelOffset;
				mAddr += mPC & 0xff;
				if (mAddr == mPC)
					++mpNextState;
				END_SUB_CYCLE();

			case kStateJ1:
				if (!(mData & (1 << ((mOpcode >> 4) & 7)))) {
					++mpNextState;
					break;
				}

				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mRelOffset;
				mAddr += mPC & 0xff;
				if (mAddr == mPC)
					++mpNextState;
				END_SUB_CYCLE();

			case kStateWaitForInterrupt:
				--mpNextState;
				END_SUB_CYCLE();

			case kStateStop:
				--mpNextState;
				END_SUB_CYCLE();

			case kStateJ:
				INSN_FETCH_NOINC();
				mAddr = mPC & 0xff00;
				mPC += (sint16)(sint8)mData;
				mAddr += mPC & 0xff;
				if (mAddr == mPC) {
					++mpNextState;
				}
				END_SUB_CYCLE();

			case kStateTrb:
				mP &= ~kFlagZ;
				if (!(mData & mA))
					mP |= kFlagZ;

				mData &= ~mA;
				break;

			case kStateTsb:
				mP &= ~kFlagZ;
				if (!(mData & mA))
					mP |= kFlagZ;

				mData |= mA;
				break;

			case kStateC02_Adc:
				if (mP & kFlagD) {
					uint32 lowResult = (mA & 15) + (mData & 15) + (mP & kFlagC);
					if (lowResult >= 10)
						lowResult += 6;

					if (lowResult >= 0x20)
						lowResult -= 0x10;

					uint32 highResult = (mA & 0xf0) + (mData & 0xf0) + lowResult;

					mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					mP |= (((highResult ^ mA) & ~(mData ^ mA)) >> 1) & kFlagV;

					if (highResult >= 0xA0)
						highResult += 0x60;

					if (highResult >= 0x100)
						mP |= kFlagC;

					uint8 result = (uint8)highResult;

					if (!result)
						mP |= kFlagZ;

					if (result & 0x80)
						mP |= kFlagN;

					mA = result;
				} else {
					uint32 carry7 = (mA & 0x7f) + (mData & 0x7f) + (mP & kFlagC);
					uint32 result = carry7 + (mA & 0x80) + (mData & 0x80);

					mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					if (result & 0x80)
						mP |= kFlagN;

					if (result >= 0x100)
						mP |= kFlagC;

					if (!(result & 0xff))
						mP |= kFlagZ;

					mP |= ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

					mA = (uint8)result;

					// No extra cycle unless decimal mode is on.
					++mpNextState;
				}
				break;

			case kStateC02_Sbc:
				if (mP & kFlagD) {
					// Pole Position needs N set properly here for its passing counter
					// to stop correctly!

					mData ^= 0xff;

					// Flags set according to binary op
					uint32 carry7 = (mA & 0x7f) + (mData & 0x7f) + (mP & kFlagC);
					uint32 result = carry7 + (mA & 0x80) + (mData & 0x80);

					// BCD
					uint32 lowResult = (mA & 15) + (mData & 15) + (mP & kFlagC);
					if (lowResult < 0x10)
						lowResult -= 6;

					uint32 highResult = (mA & 0xf0) + (mData & 0xf0) + (lowResult & 0x1f);

					if (highResult < 0x100)
						highResult -= 0x60;

					mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					uint8 bcdresult = (uint8)highResult;

					if (bcdresult & 0x80)
						mP |= kFlagN;

					if (result >= 0x100)
						mP |= kFlagC;

					if (!(bcdresult & 0xff))
						mP |= kFlagZ;

					mP |= ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

					mA = bcdresult;
				} else {
					mData ^= 0xff;
					uint32 carry7 = (mA & 0x7f) + (mData & 0x7f) + (mP & kFlagC);
					uint32 result = carry7 + (mA & 0x80) + (mData & 0x80);

					mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

					if (result & 0x80)
						mP |= kFlagN;

					if (result >= 0x100)
						mP |= kFlagC;

					if (!(result & 0xff))
						mP |= kFlagZ;

					mP |= ((result >> 2) ^ (carry7 >> 1)) & kFlagV;

					mA = (uint8)result;

					// No extra cycle unless decimal mode is on.
					++mpNextState;
				}
				break;

			case kStateAddToHistory:
				{
					ATCPUHistoryEntry * VDRESTRICT he = &mpHistory[mHistoryIndex++ & 131071];

					he->mCycle = cyclesBase - cyclesLeft;
					he->mUnhaltedCycle = cyclesBase - cyclesLeft;
					he->mEA = 0xFFFFFFFFUL;
					he->mPC = mPC - 1;
					he->mP = mP;
					he->mA = mA;
					he->mX = mX;
					he->mY = mY;
					he->mS = mS;
					he->mbIRQ = false;
					he->mbNMI = false;
					he->mSubCycle = 0;
					he->mbEmulation = false;
					he->mGlobalPCBase = 0;
					he->mB = 0;
					he->mK = 0;
					he->mD = 0;

					for(int i=0; i<3; ++i) {
						uint16 pc = mPC - 1 + i;
						he->mOpcode[i] = ATCP_DEBUG_READ_BYTE(pc);
					}
				}
				break;

			case kStateAddEAToHistory:
				{
					ATCPUHistoryEntry& he = mpHistory[(mHistoryIndex - 1) & 131071];

					he.mEA = mAddr;
				}
				break;

			case kStateBreakOnUnsupportedOpcode:
				--mpNextState;
				END_SUB_CYCLE();

			case kStateRegenerateDecodeTables:
				mpNextState = RegenerateDecodeTables();
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
	mCyclesLeft = (sint32)cyclesLeft;
}
