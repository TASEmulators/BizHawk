//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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

#if !defined(AT_CPU_MACHINE_65C816)
int ATCPUEmulator::Advance6502() {
#elif !defined(AT_CPU_MACHINE_65C816_HISPEED)
int ATCPUEmulator::Advance65816() {
#else
int ATCPUEmulator::Advance65816HiSpeed(bool dma) {
	if (dma) {
		if (!--mSubCyclesLeft) {
			mSubCyclesLeft = mSubCycles;
			return kATSimEvent_None;
		}
	}

	for(;;) {
#endif

#define AT_CPU_READ_BYTE(addr) ((mpMemory->mBusValue) = (mpMemory->ReadByte((addr))))
#define AT_CPU_READ_BYTE_ADDR16(addr) ((mpMemory->mBusValue) = (mpMemory->ReadByteAddr16((addr))))
#define AT_CPU_DUMMY_READ_BYTE(addr) ((mpMemory->mBusValue) = (mpMemory->ReadByte((addr))))
#define AT_CPU_READ_BYTE_HL(addrhi, addrlo) ((mpMemory->mBusValue) = (mpMemory->ReadByte((((uint32)addrhi) << 8) + (addrlo))))
#define AT_CPU_WRITE_BYTE(addr, value) (mpMemory->WriteByte((addr), (mpMemory->mBusValue) = ((value))))
#define AT_CPU_WRITE_BYTE_HL(addrhi, addrlo, value) (mpMemory->WriteByte(((uint32)(addrhi) << 8) + (addrlo), (mpMemory->mBusValue) = ((value))))

#ifdef AT_CPU_MACHINE_65C816
	#define INSN_FETCH() AT_CPU_EXT_READ_BYTE(mPC, mK); ++mPC
	#define INSN_FETCH_TO(dest) AT_CPU_EXT_READ_BYTE(mPC, mK); ++mPC; (dest) = readData
	#define INSN_FETCH_TO_2(dest, slowFlag) AT_CPU_EXT_READ_BYTE_2(mPC, mK, slowFlag); ++mPC; (dest) = readData
	#define INSN_DUMMY_FETCH_NOINC() AT_CPU_DUMMY_EXT_READ_BYTE(mPC, mK)
	#define INSN_FETCH_NOINC() AT_CPU_EXT_READ_BYTE(mPC, mK)
#else
	uint32 tpc;

	#define INSN_FETCH() (void)((tpc = mPC), (mPC = (uint16)(tpc + 1)), (readData = AT_CPU_READ_BYTE_ADDR16(tpc)))
	#define INSN_FETCH_TO(dest) ((tpc = mPC), (mPC = (uint16)(tpc + 1)), ((dest) = AT_CPU_READ_BYTE_ADDR16(tpc)))
	#define INSN_FETCH_TO_2(dest, slowFlag) INSN_FETCH_TO(dest)
	#define INSN_DUMMY_FETCH_NOINC() AT_CPU_DUMMY_READ_BYTE(mPC)
	#define INSN_FETCH_NOINC() AT_CPU_READ_BYTE(mPC)
#endif

#ifdef AT_CPU_MACHINE_65C816_HISPEED
	// Dummy reads have to be routed too, as the ANTIC emulation gets pretty unhappy and
	// misdecodes bytes if the 65C816 manages to somehow do a memory cycle while ANTIC
	// has the bus. I imagine the real ANTIC chip would be unhappy, too.
	#define AT_CPU_DUMMY_EXT_READ_BYTE(addr, bank)	\
		busResult = mpMemory->ExtReadByteAccel((addr), (bank), mSubCycles == mSubCyclesLeft);	\
		if (busResult < 0) { \
			if (busResult == -256) { \
				--mpNextState;\
				goto wait_slow_cycle;	\
			}	\
			mSubCyclesLeft = 1;	\
		}

	#define AT_CPU_EXT_READ_BYTE(addr, bank)	\
		busResult = mpMemory->ExtReadByteAccel((addr), (bank), mSubCycles == mSubCyclesLeft);	\
		if (busResult < 0) { \
			if (busResult == -256) { \
				--mpNextState;\
				goto wait_slow_cycle;	\
			}	\
			mSubCyclesLeft = 1;	\
		}	\
		readData = (uint8)busResult
	
	#define AT_CPU_EXT_READ_BYTE_2(addr, bank, slowFlag)	\
		busResult = mpMemory->ExtReadByteAccel((addr), (bank), mSubCycles == mSubCyclesLeft);	\
		if (busResult < 0) { \
			slowFlag = true; \
			if (busResult == -256) { \
				--mpNextState;\
				goto wait_slow_cycle;	\
			}	\
			mSubCyclesLeft = 1;	\
		}	\
		readData = (uint8)busResult

	#define AT_CPU_EXT_WRITE_BYTE(addr, bank, value) \
		writeData = (value);	\
		busResult = mpMemory->ExtWriteByteAccel((addr), (bank), writeData, mSubCycles == mSubCyclesLeft);	\
		if (busResult < 0) { \
			if (busResult == -256) {	\
				--mpNextState;	\
				goto wait_slow_cycle;	\
			}	\
			\
			mSubCyclesLeft = 1;	\
		}

	#define END_SUB_CYCLE() goto end_sub_cycle;
#else
	#define AT_CPU_DUMMY_EXT_READ_BYTE(addr, bank) ((mpMemory->mBusValue) = (mpMemory->ExtReadByte((addr), (bank))))
	#define AT_CPU_EXT_READ_BYTE(addr, bank) (void)(readData = (mpMemory->mBusValue) = (mpMemory->ExtReadByte((addr), (bank))))
	#define AT_CPU_EXT_READ_BYTE_2(addr, bank, slowFlag) AT_CPU_EXT_READ_BYTE(addr, bank)
	#define AT_CPU_EXT_WRITE_BYTE(addr, bank, value) (mpMemory->ExtWriteByte((addr), (bank), (mpMemory->mBusValue) = ((value))))
	#define END_SUB_CYCLE() return kATSimEvent_None
#endif

///////////////////////////////////////////////////////////////////////////

for(;;) {
#ifdef AT_CPU_MACHINE_65C816_HISPEED
	sint32 busResult;
	uint8 writeData;
#endif

	uint8 readData;

	switch(*mpNextState++) {
		case kStateNop:
			break;

		case kStateReadOpcode:
			mInsnPC = mPC;

			if (mDebugFlags) {
				uint8 stat = ProcessDebugging();

				if (stat)
					return stat;
			}

			// fall through

		case kStateReadOpcodeNoBreak:
			if (mIntFlags) {
#ifdef AT_CPU_MACHINE_65C816_HISPEED
				if (ProcessInterrupts<true>())
					continue;
#else
				if (ProcessInterrupts<false>())
					continue;
#endif
			}

			// prevent hooks from running in bank >0 or in native mode
			{
				uint8 iflags = mInsnFlags[mPC];

				if (iflags & kInsnFlagHook) {
#ifdef AT_CPU_MACHINE_65C816
					uint8 op = ProcessHook816();
#else
					uint8 op = ProcessHook();
#endif

					if (op) {
						if (mbHistoryActive) {
#ifdef AT_CPU_MACHINE_65C816
#ifdef AT_CPU_MACHINE_65C816_HISPEED
							AddHistoryEntry<true, true>(false);
#else
							AddHistoryEntry<true, false>(false);
#endif
#else
							AddHistoryEntry<false, false>(false);
#endif
						}

						// if a jump is requested, loop around again
						if (op == 0x4C)
							break;

						END_SUB_CYCLE();
					}
				}
			}

			{
				bool slowFlag = false;
				INSN_FETCH_TO_2(mOpcode, slowFlag);
				mpNextState = mDecodeHeap + mDecodePtrs[mOpcode];

				if (mbHistoryActive) {
#ifdef AT_CPU_MACHINE_65C816
#ifdef AT_CPU_MACHINE_65C816_HISPEED
					AddHistoryEntry<true, true>(slowFlag);
#else
					AddHistoryEntry<true, false>(false);
#endif
#else
					AddHistoryEntry<false, false>(false);
#endif
				}
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
			if ((mP ^ mData) & kFlagI) {
				if (mData & kFlagI)
					mIntFlags |= kIntFlag_IRQSetPending;
				else
					mIntFlags |= kIntFlag_IRQReleasePending;
			}

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

		case kStateDSetSZToX:
			{
				uint8 p = mP & ~(kFlagN | kFlagZ);

				p |= (mData & 0x80);	// copy N
				if (!mData)
					p |= kFlagZ;

				mP = p;
				mX = mData;
			}
			break;

		case kStateDSetSZToY:
			{
				uint8 p = mP & ~(kFlagN | kFlagZ);

				p |= (mData & 0x80);	// copy N
				if (!mData)
					p |= kFlagZ;

				mP = p;
				mY = mData;
			}
			break;

		case kStateAddrToPC:
			mPC = mAddr;
			break;

		case kStateCheckNMIBlocked:
			mbNMIForced = false;

			if (mIntFlags & kIntFlag_NMIPending) {
				mIntFlags &= ~kIntFlag_NMIPending;

				if (mNMIAssertTime != mpCallbacks->CPUGetUnhaltedCycle())
					mbNMIForced = true;
			}
			break;

		case kStateNMIVecToPC:
			mPC = 0xFFFA;

#ifdef AT_CPU_MACHINE_65C816
			mK = 0;
#endif

			mP |= kFlagI;
			mIntFlags &= ~kIntFlag_IRQSetPending;
			break;

		case kStateIRQVecToPC:
			mPC = 0xFFFE;

#ifdef AT_CPU_MACHINE_65C816
			mK = 0;
#endif

			mP |= kFlagI;
			mIntFlags &= ~kIntFlag_IRQSetPending;
			break;

		case kStateIRQVecToPCBlockNMIs:
			if (mNMIAssertTime + 1 == mpCallbacks->CPUGetUnhaltedCycle())
				mIntFlags &= ~kIntFlag_NMIPending;

			mPC = 0xFFFE;
			break;

		case kStateNMIOrIRQVecToPC:
			if (mIntFlags & kIntFlag_NMIPending) {
				mPC = 0xFFFA;
				mIntFlags &= ~kIntFlag_NMIPending;
			} else
				mPC = 0xFFFE;
			mP |= kFlagI;
			mIntFlags &= ~kIntFlag_IRQSetPending;
			break;

		case kStateNMIOrIRQVecToPCBlockable:
			if (mbNMIForced)
				mPC = 0xFFFA;
			else
				mPC = 0xFFFE;
			mP |= kFlagI;
			mIntFlags &= ~kIntFlag_IRQSetPending;
			break;

		case kStateDelayInterrupts:
			if (mIntFlags & kIntFlag_NMIPending)
				mNMIAssertTime = mpCallbacks->CPUGetUnhaltedCycle();

			if (mIntFlags & kIntFlag_IRQPending)
				mIRQAssertTime = mpCallbacks->CPUGetUnhaltedCycle();
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

		case kStateBitSetSV:
			{
				mP &= ~(kFlagN | kFlagV | kFlagZ);
				mP |= (mData & 0xC0);

				uint8 result = mData & mA;
				if (!result)
					mP |= kFlagZ;
			}
			break;

		case kStateSEI:
			if (!(mP & kFlagI)) {
				mP |= kFlagI;

				mIntFlags |= kIntFlag_IRQSetPending;
			}
			break;

		case kStateCLI:
			if (mP & kFlagI) {
				mP &= ~kFlagI;
				mIntFlags |= kIntFlag_IRQReleasePending;
			}
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		/////////
		case kStateJsAddToPath:
			if (!(mP & kFlagN)) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJnsAddToPath:
			if (mP & kFlagN) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJcAddToPath:
			if (!(mP & kFlagC)) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJncAddToPath:
			if (mP & kFlagC) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJzAddToPath:
			if (!(mP & kFlagZ)) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJnzAddToPath:
			if (mP & kFlagZ) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJoAddToPath:
			if (!(mP & kFlagV)) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJnoAddToPath:
			if (mP & kFlagV) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJccFalseRead:
			AT_CPU_READ_BYTE(mAddr);
			END_SUB_CYCLE();

		case kStateStepOver:
			ProcessStepOver();
			break;

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

		case kStateJ0AddToPath:
			if (mData & (1 << ((mOpcode >> 4) & 7))) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mRelOffset;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC)
				++mpNextState;
			END_SUB_CYCLE();

		case kStateJ1AddToPath:
			if (!(mData & (1 << ((mOpcode >> 4) & 7)))) {
				++mpNextState;
				break;
			}

			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mRelOffset;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC)
				++mpNextState;
			END_SUB_CYCLE();

		case kStateWaitForInterrupt:
			switch(mIntFlags & (kIntFlag_IRQPending | kIntFlag_IRQActive)) {
				case kIntFlag_IRQPending:
				case kIntFlag_IRQActive:
					UpdatePendingIRQState();
					break;
			}

			if (mIntFlags & kIntFlag_NMIPending) {
				mNMIAssertTime -= 3;
			} else if (!(mIntFlags & kIntFlag_IRQPending) || mpCallbacks->CPUGetUnhaltedCycle() - mIRQAcknowledgeTime <= 0)
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
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
				++mpNextState;
			}
			END_SUB_CYCLE();

		case kStateJAddToPath:
			INSN_FETCH_NOINC();
			mAddr = mPC & 0xff00;
			mPC += (sint16)(sint8)mData;
			mAddr += mPC & 0xff;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			if (mAddr == mPC) {
				mNMIIgnoreUnhaltedCycle = mpCallbacks->CPUGetUnhaltedCycle() + 1;
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

		case kStateAddEAToHistory:
			{
				HistoryEntry& he = mHistory[(mHistoryIndex - 1) & 131071];

				he.mEA = mAddr;
			}
			break;

		case kStateAddAsPathStart:
			if (!(mInsnFlags[mPC] & kInsnFlagPathStart)) {
				mInsnFlags[mPC] |= kInsnFlagPathStart;
			}
			break;

		case kStateAddToPath:
			{
				uint16 adjpc = mPC - 1;
				if (!(mInsnFlags[adjpc] & kInsnFlagPathExecuted)) {
					mInsnFlags[adjpc] |= kInsnFlagPathExecuted;

					if (mbPathBreakEnabled) {
						mbUnusedCycle = true;
						return kATSimEvent_CPUNewPath;
					}
				}
			}
			break;

		case kStateBreakOnUnsupportedOpcode:
			mbUnusedCycle = true;
			return kATSimEvent_CPUIllegalInsn;

		////////// 65C816 states

#ifdef AT_CPU_MACHINE_65C816
		case kStateReadImmL16:
			INSN_FETCH_TO(mData16);
			END_SUB_CYCLE();

		case kStateReadImmH16:
			INSN_FETCH();
			mData16 += (uint32)readData << 8;
			END_SUB_CYCLE();

		case kStateReadAddrDp:
			INSN_FETCH();
			mAddr = (uint32)readData + mDP;
			mAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpX:
			INSN_FETCH();
			mAddr = (uint32)readData + mDP + mX + ((uint32)mXH << 8);
			mAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpXInPage:
			INSN_FETCH();
			mAddr = mDP + (uint8)(readData + mX);
			mAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpY:
			INSN_FETCH();
			mAddr = (uint32)readData + mDP + mY + ((uint32)mYH << 8);
			mAddrBank = 0;
			END_SUB_CYCLE();

		case kStateReadAddrDpYInPage:
			INSN_FETCH();
			mAddr = mDP + (uint8)(readData + mY);
			mAddrBank = 0;
			END_SUB_CYCLE();

		case kState816ReadIndAddrDpInPage:
			mAddr = mData + ((uint16)AT_CPU_READ_BYTE((mAddr & 0xff00) + (0xff & (mAddr + 1))) << 8);
			END_SUB_CYCLE();

		case kStateReadIndAddrDp:
			mAddr = mData + ((uint16)AT_CPU_READ_BYTE(mAddr + 1) << 8);
			mAddrBank = mB;
			END_SUB_CYCLE();

		case kStateReadIndAddrDpY:
			mAddr = mData + ((uint16)AT_CPU_READ_BYTE(mAddr + 1) << 8) + mY + ((uint32)mYH << 8);
			mAddrBank = mB;
			END_SUB_CYCLE();

		case kStateReadIndAddrDpLongH:
			mData16 = mData + ((uint16)AT_CPU_READ_BYTE(mAddr + 1) << 8);
			END_SUB_CYCLE();

		case kStateReadIndAddrDpLongB:
			mAddrBank = AT_CPU_READ_BYTE(mAddr + 2);
			mAddr = mData16;
			END_SUB_CYCLE();

		case kStateReadAddrAddY:
			{
				uint32 addr32 = (uint32)mAddr + mY + ((uint32)mYH << 8);

				mAddr = (uint16)addr32;
				mAddrBank = (uint8)(mAddrBank + (addr32 >> 16));
			}
			break;

		case kState816ReadAddrL:
			INSN_FETCH_TO(mAddr);
			mAddrBank = mB;
			END_SUB_CYCLE();

		case kState816ReadAddrH:
			INSN_FETCH();
			mAddrBank = mB;
			mAddr = (uint16)(mAddr + ((uint32)readData << 8));
			END_SUB_CYCLE();

		case kState816ReadAddrHX:
			INSN_FETCH();
			mAddrBank = mB;
			mAddr = (uint16)(mAddr + ((uint32)readData << 8) + mX + ((uint32)mXH << 8));
			END_SUB_CYCLE();

		case kState816ReadAddrAbsXSpec:
			{
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mX) & 0x00ff);

				const uint32 addr32 = (uint32)mAddr + mX + ((uint32)mXH << 8);
				const uint16 newAddr = (uint16)addr32;
				const uint8 newAddrBank = (uint8)(mAddrBank + (addr32 >> 16));

				if (newAddr != mAddr2) {
					AT_CPU_DUMMY_EXT_READ_BYTE(mAddr2, mAddrBank);

					mAddr = newAddr;
					mAddrBank = newAddrBank;
					END_SUB_CYCLE();
				}

				mAddr = newAddr;
				mAddrBank = newAddrBank;
			}
			break;

		case kState816ReadAddrAbsXAlways:
			{
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mX) & 0x00ff);
				AT_CPU_DUMMY_EXT_READ_BYTE(mAddr2, mAddrBank);

				const uint32 addr32 = (uint32)mAddr + mX + ((uint32)mXH << 8);
				mAddr = (uint16)addr32;
				mAddrBank = (uint8)(mAddrBank + (addr32 >> 16));

				END_SUB_CYCLE();
			}

		case kState816ReadAddrAbsYSpec:
			{
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mY) & 0x00ff);

				const uint32 addr32 = (uint32)mAddr + mY + ((uint32)mYH << 8);
				const uint16 newAddr = (uint16)addr32;
				const uint8 newAddrBank = (uint8)(mAddrBank + (addr32 >> 16));

				if (newAddr != mAddr2) {
					AT_CPU_DUMMY_EXT_READ_BYTE(mAddr2, mAddrBank);

					mAddr = newAddr;
					mAddrBank = newAddrBank;
					END_SUB_CYCLE();
				}

				mAddr = newAddr;
				mAddrBank = newAddrBank;
			}
			break;

		case kState816ReadAddrAbsYAlways:
			{
				mAddr2 = (mAddr & 0xff00) + ((mAddr + mY) & 0x00ff);
				AT_CPU_DUMMY_EXT_READ_BYTE(mAddr2, mAddrBank);

				const uint32 addr32 = (uint32)mAddr + mY + ((uint32)mYH << 8);
				mAddr = (uint16)addr32;
				mAddrBank = (uint8)(mAddrBank + (addr32 >> 16));

				END_SUB_CYCLE();
			}

		case kState816ReadAddrAbsInd:
			AT_CPU_EXT_READ_BYTE(mAddr+1, mAddrBank);
			mAddr = (uint16)((uint32)mData + ((uint32)readData << 8));
			END_SUB_CYCLE();

		case kStateRead816AddrAbsLongL:
			AT_CPU_EXT_READ_BYTE(mAddr, mAddrBank);
			mData = readData;
			END_SUB_CYCLE();

		case kStateRead816AddrAbsLongH:
			AT_CPU_EXT_READ_BYTE(mAddr + 1, mAddrBank);
			mData16 = mData + ((uint32)readData << 8);
			END_SUB_CYCLE();

		case kStateRead816AddrAbsLongB:
			AT_CPU_EXT_READ_BYTE(mAddr + 2, mAddrBank);
			mAddrBank = readData;
			mAddr = mData16;
			END_SUB_CYCLE();

		case kStateReadAddrB:
			INSN_FETCH_TO(mAddrBank);
			END_SUB_CYCLE();

		case kStateReadAddrBX:
			INSN_FETCH_TO(mAddrBank);
			{
				uint32 ea = (uint32)mAddr + mX + ((uint32)mXH << 8);

				if (ea >= 0x10000)
					++mAddrBank;

				mAddr = (uint16)ea;
			}
			END_SUB_CYCLE();

		case kStateReadAddrSO:
			INSN_FETCH();
			mAddrBank = 0;
			mAddr = mS + ((uint32)mSH << 8) + readData;
			END_SUB_CYCLE();

		case kState816ReadAddrSO_AddY:
			{
				AT_CPU_DUMMY_EXT_READ_BYTE(mAddr+1, mAddrBank);

				uint32 addr32 = (uint32)mData16 + mY + ((uint32)mYH << 8);
				mAddr = (uint16)addr32;
				mAddrBank = (uint8)(mB + (addr32 >> 16));
			}
			END_SUB_CYCLE();

		case kStateBtoD:
			mData = mB;
			break;

		case kStateKtoD:
			mData = mK;
			break;

		case kState0toD16:
			mData16 = 0;
			break;

		case kStateAtoD16:
			mData16 = ((uint32)mAH << 8) + mA;
			break;

		case kStateXtoD16:
			mData16 = ((uint32)mXH << 8) + mX;
			break;

		case kStateYtoD16:
			mData16 = ((uint32)mYH << 8) + mY;
			break;

		case kStateStoD16:
			mData16 = ((uint32)mSH << 8) + mS;
			break;

		case kStateDPtoD16:
			mData16 = mDP;
			break;

		case kStateDtoB:
			mB = mData;
			break;

		case kStateDtoA16:
			mA = (uint8)mData16;
			mAH = (uint8)(mData16 >> 8);
			break;

		case kStateDtoX16:
			mX = (uint8)mData16;
			mXH = (uint8)(mData16 >> 8);
			break;

		case kStateDtoY16:
			mY = (uint8)mData16;
			mYH = (uint8)(mData16 >> 8);
			break;

		case kStateDtoPNative:
			if ((mP & ~mData) & kFlagI)
				mIntFlags |= kIntFlag_IRQReleasePending;
			mP = mData;
			Update65816DecodeTable();
			break;

		case kStateDtoPNative_noICheck:
			mP = mData;
			Update65816DecodeTable();
			break;

		case kStateDtoS16:
			mS = (uint8)mData16;
			if (!mbEmulationFlag)
				mSH = (uint8)(mData16 >> 8);
			break;

		case kStateDtoDP16:
			{
				const uint16 prevDP = mDP;
				mDP = mData16;

				// check if are changing DL=0 state, which requires a redecode
				if (((uint8)mDP == 0) ^ ((uint8)prevDP == 0))
					Update65816DecodeTable();
			}
			break;

		case kStateDSetSZ16:
			mP &= ~(kFlagN | kFlagZ);
			if (mData16 & 0x8000)
				mP |= kFlagN;
			if (!mData16)
				mP |= kFlagZ;
			break;

		case kStateBitSetSV16:
			{
				uint32 acc = mA + ((uint32)mAH << 8);
				uint16 result = mData16 & acc;

				mP &= ~(kFlagN | kFlagV | kFlagZ);
				mP |= ((uint8)(mData16 >> 8) & 0xC0);
				if (!result)
					mP |= kFlagZ;
			}
			break;

		case kState816WriteByte:
			AT_CPU_EXT_WRITE_BYTE(mAddr, mAddrBank, mData);
			END_SUB_CYCLE();

		case kStateWriteL16:
			AT_CPU_EXT_WRITE_BYTE(mAddr, mAddrBank, (uint8)mData16);
			END_SUB_CYCLE();

		case kStateWriteH16:
			AT_CPU_EXT_WRITE_BYTE(mAddr + 1, (uint8)((((uint32)mAddr + 1) >> 16) + mAddrBank), (uint8)(mData16 >> 8));
			END_SUB_CYCLE();

		case kStateWriteH16_DpBank:
			if (mbEmulationFlag && !(uint8)mDP) {
				AT_CPU_EXT_WRITE_BYTE(mAddr & 0xff00 + ((mAddr + 1) & 0xff), mAddrBank, (uint8)(mData16 >> 8));
			} else {
				AT_CPU_EXT_WRITE_BYTE(mAddr + 1, mAddrBank, (uint8)(mData16 >> 8));
			}

			END_SUB_CYCLE();

		case kState816ReadByte:
			AT_CPU_EXT_READ_BYTE(mAddr, mAddrBank);
			mData = readData;
			END_SUB_CYCLE();

		case kState816ReadByte_PBK:
			AT_CPU_EXT_READ_BYTE(mAddr, mK);
			mData = readData;
			END_SUB_CYCLE();

		case kStateReadL16:
			AT_CPU_EXT_READ_BYTE(mAddr, mAddrBank);
			mData16 = readData;
			END_SUB_CYCLE();

		case kStateReadH16:
			AT_CPU_EXT_READ_BYTE(mAddr + 1, (uint8)((((uint32)mAddr + 1) >> 16) + mAddrBank));
			mData16 += ((uint32)readData << 8);
			END_SUB_CYCLE();

		case kStateReadH16_DpBank:
			if (mbEmulationFlag && !(uint8)mDP) {
				AT_CPU_EXT_READ_BYTE((mAddr & 0xff00) + ((mAddr + 1) & 0xff), mAddrBank);
				mData16 += ((uint32)readData << 8);
			} else {
				AT_CPU_EXT_READ_BYTE(mAddr + 1, mAddrBank);
				mData16 += ((uint32)readData << 8);
			}

			END_SUB_CYCLE();

		case kStateAnd16:
			mData16 &= (mA + ((uint32)mAH << 8));
			mP &= ~(kFlagN | kFlagZ);
			if (mData16 & 0x8000)
				mP |= kFlagN;
			if (!mData16)
				mP |= kFlagZ;
			break;

		case kStateOr16:
			mA |= (uint8)mData16;
			mAH |= (uint8)(mData16 >> 8);
			mP &= ~(kFlagN | kFlagZ);
			if (mAH & 0x80)
				mP |= kFlagN;
			if (!(mA | mAH))
				mP |= kFlagZ;
			break;

		case kStateXor16:
			mA ^= (uint8)mData16;
			mAH ^= (uint8)(mData16 >> 8);
			mP &= ~(kFlagN | kFlagZ);
			if (mAH & 0x80)
				mP |= kFlagN;
			if (!(mA | mAH))
				mP |= kFlagZ;
			break;

		case kStateAdc16:
			if (mP & kFlagD) {
				uint32 lowResult = (mA & 15) + (mData16 & 15) + (mP & kFlagC);
				if (lowResult >= 10)
					lowResult += 6;

				uint32 midResult = (mA & 0xf0) + (mData16 & 0xf0) + lowResult;
				if (midResult >= 0xA0)
					midResult += 0x60;

				uint32 acchi = (uint32)mAH << 8;
				uint32 midHiResult = (acchi & 0xf00) + (mData16 & 0xf00) + midResult;
				if (midHiResult >= 0xA00)
					midHiResult += 0x600;

				uint32 highResult = (acchi & 0xf000) + (mData16 & 0xf000) + midHiResult;
				if (highResult >= 0xA000)
					highResult += 0x6000;

				mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (highResult >= 0x10000)
					mP |= kFlagC;

				if (!(highResult & 0xffff))
					mP |= kFlagZ;

				if (highResult & 0x8000)
					mP |= kFlagN;

				mA = (uint8)highResult;
				mAH = (uint8)(highResult >> 8);
			} else {
				uint32 data = mData16;
				uint32 acc = mA + ((uint32)mAH << 8);
				uint32 carry15 = (acc & 0x7fff) + (data & 0x7fff) + (mP & kFlagC);
				uint32 result = carry15 + (acc & 0x8000) + (data & 0x8000);

				mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (result & 0x8000)
					mP |= kFlagN;

				if (result >= 0x10000)
					mP |= kFlagC;

				if (!(result & 0xffff))
					mP |= kFlagZ;

				mP |= ((result >> 10) ^ (carry15 >> 9)) & kFlagV;

				mA = (uint8)result;
				mAH = (uint8)(result >> 8);
			}
			break;

		case kStateSbc16:
			if (mP & kFlagD) {
				uint32 data = (uint32)mData ^ 0xffff;
				uint32 acc = mA + ((uint32)mAH << 8);

				// BCD
				uint32 lowResult = (acc & 15) + (data & 15) + (mP & kFlagC);
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

				mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (highResult & 0x8000)
					mP |= kFlagN;

				if (highResult >= 0x10000)
					mP |= kFlagC;

				if (!(highResult & 0xffff))
					mP |= kFlagZ;

				mA = (uint8)highResult;
				mAH = (uint8)(highResult >> 8);
			} else {
				uint32 acc = ((uint32)mAH << 8) + mA;
				
				uint32 d16 = (uint32)mData16 ^ 0xffff;
				uint32 carry15 = (acc & 0x7fff) + (d16 & 0x7fff) + (mP & kFlagC);
				uint32 result = carry15 + (acc & 0x8000) + (d16 & 0x8000);

				mP &= ~(kFlagC | kFlagN | kFlagZ | kFlagV);

				if (result & 0x8000)
					mP |= kFlagN;

				if (result >= 0x10000)
					mP |= kFlagC;

				if (!(result & 0xffff))
					mP |= kFlagZ;

				mP |= ((result >> 10) ^ (carry15 >> 9)) & kFlagV;

				mA = (uint8)result;
				mAH = (uint8)(result >> 8);
			}
			break;

		case kStateCmp16:
			{
				uint32 acc = ((uint32)mAH << 8) + mA;
				uint32 d16 = (uint32)mData16 ^ 0xffff;
				uint32 result = acc + d16 + 1;

				mP &= ~(kFlagC | kFlagN | kFlagZ);

				if (result & 0x8000)
					mP |= kFlagN;

				if (result >= 0x10000)
					mP |= kFlagC;

				if (!(result & 0xffff))
					mP |= kFlagZ;
			}
			break;

		case kStateInc16:
			++mData16;
			mP &= ~(kFlagN | kFlagZ);
			if (mData16 & 0x8000)
				mP |= kFlagN;
			if (!mData16)
				mP |= kFlagZ;
			break;

		case kStateDec16:
			--mData16;
			mP &= ~(kFlagN | kFlagZ);
			if (mData16 & 0x8000)
				mP |= kFlagN;
			if (!mData16)
				mP |= kFlagZ;
			break;

		case kStateRol16:
			{
				uint32 result = (uint32)mData16 + (uint32)mData16 + (mP & kFlagC);
				mP &= ~(kFlagN | kFlagZ | kFlagC);
				if (result & 0x10000)
					mP |= kFlagC;
				mData16 = (uint16)result;
				if (mData16 & 0x8000)
					mP |= kFlagN;
				if (!mData16)
					mP |= kFlagZ;
			}
			break;

		case kStateRor16:
			{
				uint32 result = ((uint32)mData16 >> 1) + ((uint32)(mP & kFlagC) << 15);
				mP &= ~(kFlagN | kFlagZ | kFlagC);
				if (mData16 & 1)
					mP |= kFlagC;
				mData16 = (uint16)result;
				if (result & 0x8000)
					mP |= kFlagN;
				if (!result)
					mP |= kFlagZ;
			}
			break;

		case kStateAsl16:
			mP &= ~(kFlagN | kFlagZ | kFlagC);
			if (mData16 & 0x8000)
				mP |= kFlagC;
			mData16 += mData16;
			if (mData16 & 0x8000)
				mP |= kFlagN;
			if (!mData16)
				mP |= kFlagZ;
			break;

		case kStateLsr16:
			mP &= ~(kFlagN | kFlagZ | kFlagC);
			if (mData16 & 0x01)
				mP |= kFlagC;
			mData16 >>= 1;
			if (!mData16)
				mP |= kFlagZ;
			break;

		case kStateTrb16:
			{
				uint32 acc = mA + ((uint32)mAH << 8);

				mP &= ~kFlagZ;
				if (!(mData16 & acc))
					mP |= kFlagZ;

				mData16 &= ~acc;
			}
			break;

		case kStateTsb16:
			{
				uint32 acc = mA + ((uint32)mAH << 8);

				mP &= ~kFlagZ;
				if (!(mData16 & acc))
					mP |= kFlagZ;

				mData16 |= acc;
			}
			break;

		case kStateCmpX16:
			{
				uint32 data = (uint32)mData16 ^ 0xffff;
				uint32 result = mX + ((uint32)mXH << 8) + data + 1;

				mP &= ~(kFlagC | kFlagN | kFlagZ);

				if (result & 0x8000)
					mP |= kFlagN;

				if (result >= 0x10000)
					mP |= kFlagC;

				if (!(result & 0xffff))
					mP |= kFlagZ;
			}
			break;

		case kStateCmpY16:
			{
				uint32 data = (uint32)mData16 ^ 0xffff;
				uint32 result = mY + ((uint32)mYH << 8) + data + 1;

				mP &= ~(kFlagC | kFlagN | kFlagZ);

				if (result & 0x8000)
					mP |= kFlagN;

				if (result >= 0x10000)
					mP |= kFlagC;

				if (!(result & 0xffff))
					mP |= kFlagZ;
			}
			break;

		case kStateXba:
			{
				uint8 t = mAH;
				mAH = mA;
				mA = t;

				mP &= ~(kFlagN | kFlagZ);

				mP |= (t & kFlagN);

				if (!t)
					mP |= kFlagZ;
			}
			break;

		case kStateXce:
			{
				bool newEmuFlag = ((mP & kFlagC) != 0);
				mP &= ~kFlagC;
				if (mbEmulationFlag)
					mP |= kFlagC | kFlagM | kFlagX;

				mbEmulationFlag = newEmuFlag;
				Update65816DecodeTable();
			}
			break;

		case kStatePushNative:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, mData);
			if (!mS-- && !mbEmulationFlag)
				--mSH;
			END_SUB_CYCLE();

		case kStatePushL16:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, (uint8)mData16);
			if (!mS--)
				--mSH;

			if (mbEmulationFlag)
				mSH = 1;
			END_SUB_CYCLE();

		case kStatePushH16:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, (uint8)(mData16 >> 8));
			if (!mS--) {
				// We intentionally allow this to underflow in emulation mode. This is a quirk of
				// the 65C816 for 16-bit non-PC pushes. This will be corrected after the low byte
				// is pushed.
				--mSH;
			}
			END_SUB_CYCLE();

		case kStatePushPBKNative:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, mK);
			if (!mS--)
				--mSH;
			END_SUB_CYCLE();

		case kStatePushPCLNative:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, mPC & 0xff);
			if (!mS-- && !mbEmulationFlag)
				--mSH;
			END_SUB_CYCLE();

		case kStatePushPCHNative:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, mPC >> 8);
			if (!mS-- && !mbEmulationFlag)
				--mSH;
			END_SUB_CYCLE();

		case kStatePushPCLM1Native:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, (mPC - 1) & 0xff);
			if (!mS--)
				--mSH;
			// last cycle -- must force SH=1 in emu mode
			if (mbEmulationFlag)
				mSH = 1;
			END_SUB_CYCLE();

		case kStatePushPCHM1Native:
			AT_CPU_WRITE_BYTE_HL(mSH, mS, (mPC - 1) >> 8);
			if (!mS--)
				--mSH;
			END_SUB_CYCLE();

		case kStatePopNative:
			if (!++mS)
				++mSH;
			mData = AT_CPU_READ_BYTE_HL(mSH, mS);
			if (mbEmulationFlag)
				mSH = 1;
			END_SUB_CYCLE();

		case kStatePopL16:
			if (!++mS) {
				// We intentionally allow this to overflow in emulation modes to emulate a 65C816
				// quirk. It will be fixed up after we read the high byte.
				++mSH;
			}

			mData16 = AT_CPU_READ_BYTE_HL(mSH, mS);
			END_SUB_CYCLE();

		case kStatePopH16:
			if (!++mS)
				++mSH;

			mData16 += (uint32)AT_CPU_READ_BYTE_HL(mSH, mS) << 8;

			if (mbEmulationFlag)
				mSH = 1;
			END_SUB_CYCLE();

		case kStatePopPCLNative:
			if (!++mS && !mbEmulationFlag)
				++mSH;
			mPC = AT_CPU_READ_BYTE_HL(mSH, mS);
			END_SUB_CYCLE();

		case kStatePopPCHNative:
			if (!++mS && !mbEmulationFlag)
				++mSH;
			mPC += (uint32)AT_CPU_READ_BYTE_HL(mSH, mS) << 8;
			END_SUB_CYCLE();

		case kStatePopPCHP1Native:
			if (!++mS && !mbEmulationFlag)
				++mSH;
			mPC += ((uint32)AT_CPU_READ_BYTE_HL(mSH, mS) << 8) + 1;
			END_SUB_CYCLE();

		case kStatePopPBKNative:
			if (!++mS && !mbEmulationFlag)
				++mSH;
			mK = AT_CPU_READ_BYTE_HL(mSH, mS);
			END_SUB_CYCLE();

		case kStateRep:
			if (mP & mData & kFlagI)
				mIntFlags |= kIntFlag_IRQReleasePending;

			if (mbEmulationFlag)
				mP &= ~(mData & 0xcf);		// m and x are off-limits
			else
				mP &= ~mData;

			Update65816DecodeTable();
			END_SUB_CYCLE();

		case kStateSep:
			if (~mP & mData & kFlagI)
				mIntFlags |= kIntFlag_IRQSetPending;

			if (mbEmulationFlag)
				mP |= mData & 0xcf;		// m and x are off-limits
			else
				mP |= mData;

			Update65816DecodeTable();
			END_SUB_CYCLE();

		case kStateJ16:
			mPC += mData16;
			END_SUB_CYCLE();

		case kStateJ16AddToPath:
			mPC += mData16;
			mInsnFlags[mPC] |= kInsnFlagPathStart;
			END_SUB_CYCLE();

		case kState816_NatCOPVecToPC:
			mPC = 0xFFE4;
			mK = 0;
			break;

		case kState816_EmuCOPVecToPC:
			mPC = 0xFFF4;
			break;

		case kState816_NatNMIVecToPC:
			mPC = 0xFFEA;
			mK = 0;
			break;

		case kState816_NatIRQVecToPC:
			mPC = 0xFFEE;
			mK = 0;
			break;

		case kState816_NatBRKVecToPC:
			mPC = 0xFFE6;
			mK = 0;
			break;

		case kState816_ABORT:
			// prepare to push PC of faulting instruction (NOT current PC)
			mData16 = mInsnPC;

			// load instruction vector
			mPC = mbEmulationFlag ? 0xFFF8 : 0xFFE8;

			// force PBK=0
			mK = 0;
			break;

		case kState816_SetI_ClearD:
			mP |= kFlagI;
			mP &= ~kFlagD;
			break;

		case kState816_LongAddrToPC:
			mPC = mAddr;
			mK = mAddrBank;
			break;

		case kState816_MoveRead:
			AT_CPU_EXT_READ_BYTE(mX + ((uint32)mXH << 8), mAddrBank);
			mData = readData;
			END_SUB_CYCLE();

		case kState816_MoveWriteP:
			mAddr = mY + ((uint32)mYH << 8);
			AT_CPU_EXT_WRITE_BYTE(mAddr, mB, mData);

			if (!mbEmulationFlag && !(mP & kFlagX)) {
				if (!mX)
					--mXH;

				if (!mY)
					--mYH;
			}

			--mX;
			--mY;

			if (mA-- || mAH--)
					mPC -= 3;

			END_SUB_CYCLE();

		case kState816_MoveWriteN:
			mAddr = mY + ((uint32)mYH << 8);
			AT_CPU_EXT_WRITE_BYTE(mAddr, mB, mData);

			++mX;
			++mY;
			if (!mbEmulationFlag && !(mP & kFlagX)) {
				if (!mX)
					++mXH;

				if (!mY)
					++mYH;
			}

			if (mA-- || mAH--)
					mPC -= 3;

			END_SUB_CYCLE();

		case kState816_Per:
			mData16 += mPC;
			break;

		case kState816_SetBank0:
			mAddrBank = 0;
			break;

		case kState816_SetBankPBR:
			mAddrBank = mK;
			break;
#endif

		case kStateUpdateHeatMap:
			if (mpHeatMap)
				mpHeatMap->ProcessInsn(*this, mOpcode, mAddr, mInsnPC);
			break;

		case kStateVerifyInsn:
			if (mpVerifier)
				mpVerifier->VerifyInsn(*this, mOpcode, mAddr);
			break;

		case kStateVerifyIRQEntry:
			if (mpVerifier)
				mpVerifier->OnIRQEntry();
			break;

		case kStateVerifyNMIEntry:
			if (mpVerifier)
				mpVerifier->OnNMIEntry();
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

#ifdef AT_CPU_MACHINE_65C816_HISPEED
end_sub_cycle:
		if (!--mSubCyclesLeft) {
wait_slow_cycle:
			if (mbForceNextCycleSlow) {
				mbForceNextCycleSlow = false;
				mSubCyclesLeft = 1;
			} else {
				mSubCyclesLeft = mSubCycles;
			}
			break;
		}
	}

	return kATSimEvent_None;
#endif

}
