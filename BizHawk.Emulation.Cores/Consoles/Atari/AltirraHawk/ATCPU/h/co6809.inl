//	Altirra - Atari 800/800XL/5200 emulator
//	Coprocessor library - Z80 state machine
//	Copyright (C) 2009-2016 Avery Lee
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
#define AT_CPU_READ_BYTE_HL(addrhi, addrlo) ATCP_READ_BYTE(((uint32)(uint8)(addrhi) << 8) + (uint8)(addrlo))
#define AT_CPU_WRITE_BYTE(addr, value) ATCP_WRITE_BYTE((addr), (value))
#define AT_CPU_WRITE_BYTE_HL(addrhi, addrlo, value) ATCP_WRITE_BYTE(((uint32)(uint8)(addrhi) << 8) + (uint8)(addrlo), (value))

#define INSN_FETCH() AT_CPU_READ_BYTE(mPC++)
#define INSN_FETCH_TO(dest) readData = AT_CPU_READ_BYTE(mPC); ++mPC; (dest) = readData
#define INSN_FETCH_NOINC() AT_CPU_READ_BYTE(mPC)

#define AT_SET_PARITY(val) if ((0x9669U >> ((val ^ (val >> 4)) & 15)) & 1) mF |= 0x04

void ATCoProc6809::Run() {
	using namespace ATCPUStates6809;

	ATCP_MEMORY_CONTEXT;

	if (mCyclesLeft <= 0)
		return;

	static constexpr uint8 E = 0x80;
	static constexpr uint8 F = 0x40;
	static constexpr uint8 H = 0x20;
	static constexpr uint8 I = 0x10;
	static constexpr uint8 N = 0x08;
	static constexpr uint8 Z = 0x04;
	static constexpr uint8 V = 0x02;
	static constexpr uint8 C = 0x01;
	static constexpr uint8 HNZVC = H|N|Z|V|C;
	static constexpr uint8 HNZC = H|N|Z|C;
	static constexpr uint8 NZVC = N|Z|V|C;
	static constexpr uint8 NZV = N|Z|V;
	static constexpr uint8 NZC = N|Z|C;

	while(mCyclesLeft) {
		uint8 readData;

		switch(*mpNextState++) {

			case k6809StateNop:
				break;

			case k6809StateWait_1:
				--mCyclesLeft;
				break;

			case k6809StateReadOpcode:
				if (mpBreakpointMap[mPC]) {
					mInsnPC = mPC;
					bool shouldExit = CheckBreakpoint();

					if (shouldExit) {
						goto force_exit;
					}
				}
			case k6809StateReadOpcodeNoBreak:
				mInsnPC = mPC;

				if (mbIntAttention) {
					if (mbNmiArmed && mbNmiAsserted) {
						mbNmiAsserted = false;
						mpNextState = &mDecoderTables.mDecodeHeap[mDecoderTables.mNmiSequence];
						break;
					} else if (mbFirqAsserted && !(mCC & F)) {
						mpNextState = &mDecoderTables.mDecodeHeap[mDecoderTables.mFirqSequence];
						break;
					} else if (mbIrqAsserted && !(mCC & I)) {
						mpNextState = &mDecoderTables.mDecodeHeap[mDecoderTables.mIrqSequence];
						break;
					}

					mbIntAttention = false;
				}

				{
					uint8 opcode;
					INSN_FETCH_TO(opcode);
					//VDDEBUG("Executing opcode AF=%02X%02X BC=%02X%02X DE=%02X%02x HL=%02X%02X %04X: %02X\n", mA, mF, mB, mC, mD, mE, mH, mL, mInsnPC, opcode);
					mpNextState = mDecoderTables.mDecodeHeap + mDecoderTables.mInsns[opcode];
				}

				--mCyclesLeft;
				break;

			case k6809StateReadOpcode10_1:
				{
					uint8 opcode;
					INSN_FETCH_TO(opcode);
					mpNextState = mDecoderTables.mDecodeHeap + mDecoderTables.mInsns10[opcode];
				}
				break;

			case k6809StateReadOpcode11_1:
				{
					uint8 opcode;
					INSN_FETCH_TO(opcode);
					mpNextState = mDecoderTables.mDecodeHeap + mDecoderTables.mInsns11[opcode];
				}
				break;

			case k6809StateReadAddrDirect_1:
				mAddr = (uint16)(((uint32)mDP << 8) + INSN_FETCH());
				--mCyclesLeft;
				break;

			case k6809StateReadAddrExtHi:
				mAddr = (uint16)((uint32)INSN_FETCH() << 8);
				--mCyclesLeft;
				break;

			case k6809StateReadAddrExtLo:
				mAddr = (uint16)(mAddr + (uint32)INSN_FETCH());
				--mCyclesLeft;
				break;

			case k6809StateReadImm_1:
				mData = INSN_FETCH();
				--mCyclesLeft;
				break;

			case k6809StateReadImmHi_1:
				mData = (uint32)INSN_FETCH() << 8;
				--mCyclesLeft;
				break;

			case k6809StateReadImmLo_1:
				mData += INSN_FETCH();
				--mCyclesLeft;
				break;

			case k6809StateReadByte_1:
				mData = AT_CPU_READ_BYTE(mAddr);
				--mCyclesLeft;
				break;

			case k6809StateReadByteHi_1:
				mData = (uint32)AT_CPU_READ_BYTE(mAddr) << 8;
				--mCyclesLeft;
				break;

			case k6809StateReadByteLo_1:
				mData += (uint32)AT_CPU_READ_BYTE(mAddr + 1);
				--mCyclesLeft;
				break;

			case k6809StateWriteByte_1:
				AT_CPU_WRITE_BYTE(mAddr, mData);
				--mCyclesLeft;
				break;

			case k6809StateWriteByteHi_1:
				AT_CPU_WRITE_BYTE(mAddr, (uint8)(mData >> 8));
				--mCyclesLeft;
				break;

			case k6809StateWriteByteLo_1:
				AT_CPU_WRITE_BYTE(mAddr + 1, mData);
				--mCyclesLeft;
				break;

			case k6809StateIndexed_2p:
				mpSavedState = mpNextState;
				mData = INSN_FETCH();

				switch((mData >> 5) & 3) {
					case 0: mAddr = mX; break;
					case 1: mAddr = mY; break;
					case 2: mAddr = mU; break;
					case 3: mAddr = mS; break;
				}

				if (mData & 0x80)
					mpNextState = mDecoderTables.mDecodeHeap + mDecoderTables.mIndexedModes[mData & 31];
				else
					mpNextState = mDecoderTables.mDecodeHeap + mDecoderTables.mIndexedModes[32];

				--mCyclesLeft;
				break;

			case k6809StateIndexedPostInc:
				switch((mData >> 5) & 3) {
					case 0: ++mX; break;
					case 1: ++mY; break;
					case 2: ++mU; break;
					case 3:
						++mS;
						ArmNmi();
						break;
				}
				--mCyclesLeft;
				break;

			case k6809StateIndexedPreDec:
				switch((mData >> 5) & 3) {
					case 0: mAddr = --mX; break;
					case 1: mAddr = --mY; break;
					case 2: mAddr = --mU; break;
					case 3:
						mAddr = --mS;
						ArmNmi();
						break;
				}
				--mCyclesLeft;
				break;

			case k6809StateIndexedA:
				mAddr += mA;
				--mCyclesLeft;
				break;

			case k6809StateIndexedB:
				mAddr += mB;
				--mCyclesLeft;
				break;

			case k6809StateIndexedD:
				mAddr += (uint32)mA << 8;
				mAddr += mB;
				--mCyclesLeft;
				break;

			case k6809StateIndexed5BitOffset:
				mAddr += ((mData & 0x1F) - 0x20) ^ (UINT32_C(0) - 0x20);
				--mCyclesLeft;
				break;

			case k6809StateIndexed8BitOffset:
				mAddr += (uint32)(sint8)INSN_FETCH();
				--mCyclesLeft;
				break;

			case k6809StateIndexed16BitOffsetHi:
				mAddr += (uint32)(sint16)((uint32)INSN_FETCH() << 8);
				--mCyclesLeft;
				break;

			case k6809StateIndexed16BitOffsetLo_1:
				mAddr += INSN_FETCH();
				--mCyclesLeft;
				break;

			case k6809StateIndexed8BitOffsetPCR_1:
				mAddr = mPC + 1;
				mAddr += (uint32)(sint8)INSN_FETCH();
				--mCyclesLeft;
				break;

			case k6809StateIndexed16BitOffsetPCRHi_1:
				mAddr = mPC + 2;
				mAddr += (uint32)(sint16)((uint32)INSN_FETCH() << 8);
				--mCyclesLeft;
				break;

			case k6809StateIndexedIndirectLo:
				mAddr = mData + AT_CPU_READ_BYTE(mAddr + 1);
				break;

			case k6809StateIndexedRts:
				mpNextState = mpSavedState;
				--mCyclesLeft;
				break;

			case k6809StateClrData_NZVC_1:
				mData = 0;
				mCC &= 0xF0;
				mCC |= 0x04;
				--mCyclesLeft;
				break;

			case k6809StateAToData:
				mData = mA;
				break;

			case k6809StateBToData:
				mData = mB;
				break;

			case k6809StateDToData:
				mData = ((uint32)mA << 8) + mB;
				break;

			case k6809StateXToData:
				mData = mX;
				break;

			case k6809StateYToData:
				mData = mY;
				break;

			case k6809StateSToData:
				mData = mS;
				break;

			case k6809StateUToData:
				mData = mU;
				break;

			case k6809StatePCToData:
				mData = mPC;
				break;

			case k6809StateDataToA:
				mA = (uint8)mData;
				break;

			case k6809StateDataToA_NZV:
				mA = (uint8)mData;
				mCC &= ~NZV;
				if (!mA)
					mCC += Z;
				if (mA & 0x80)
					mCC += N;
				break;

			case k6809StateDataToB:
				mB = (uint8)mData;
				break;

			case k6809StateDataToB_NZV:
				mB = (uint8)mData;
				mCC &= ~NZV;
				if (!mB)
					mCC += Z;
				if (mB & 0x80)
					mCC += N;
				break;

			case k6809StateDataToD:
				mA = (uint8)(mData >> 8);
				mB = (uint8)mData;
				break;

			case k6809StateDataToD_NZV:
				mA = (uint8)(mData >> 8);
				mB = (uint8)mData;
				mCC &= ~NZV;
				if (!(uint16)mData)
					mCC += Z;
				if (mA & 0x80)
					mCC += N;
				break;

			case k6809StateDataToX_NZV:
				mX = mData;
				mCC &= ~NZV;
				if (!mX)
					mCC += Z;
				if (mX & 0x8000)
					mCC += N;
				break;

			case k6809StateDataToY_NZV:
				mY = mData;
				mCC &= ~NZV;
				if (!mY)
					mCC += Z;
				if (mY & 0x8000)
					mCC += N;
				break;

			case k6809StateDataToS_NZV:
				mS = mData;
				mCC &= ~NZV;
				if (!mS)
					mCC += Z;
				if (mS & 0x8000)
					mCC += N;
				ArmNmi();
				break;

			case k6809StateDataToU_NZV:
				mU = mData;
				mCC &= ~NZV;
				if (!mU)
					mCC += Z;
				if (mU & 0x80)
					mCC += N;
				break;

			case k6809StateDataToCC:
				SetCC((uint8)mData);
				break;

			case k6809StateDataToPC:
				mPC = (uint16)mData;
				break;

			case k6809StateAddrToX:
				mX = mAddr;
				break;

			case k6809StateAddrToX_Z:
				mX = mAddr;
				mCC &= ~Z;
				if (!mX)
					mCC |= Z;
				break;

			case k6809StateAddrToY:
				mY = mAddr;
				break;

			case k6809StateAddrToY_Z:
				mY = mAddr;
				mCC &= ~Z;
				if (!mY)
					mCC |= Z;
				break;

			case k6809StateAddrToS:
				mS = mAddr;
				break;

			case k6809StateAddrToU:
				mU = mAddr;
				break;

			case k6809StateAddrToPC:
				mPC = mAddr;
				break;

			case k6809StateExg:
				{
					uint32 v1 = 0xFFFF;
					uint32 v2 = 0xFFFF;

					switch((mData >> 4) & 15) {
						case 0:
							v1 = ((uint32)mA << 8) + mB;
							break;
						case 1:		v1 = mX; break;
						case 2:		v1 = mY; break;
						case 3:		v1 = mU; break;
						case 4:		v1 = mS; break;
						case 5:		v1 = mPC; break;
						case 8:		v1 = mA; break;
						case 9:		v1 = mB; break;
						case 10:	v1 = mCC; break;
						case 11:	v1 = mDP; break;
						default:
							break;
					}

					switch(mData & 15) {
						case 0:
							v2 = ((uint32)mA << 8) + mB;
							break;
						case 1:		v2 = mX; break;
						case 2:		v2 = mY; break;
						case 3:		v2 = mU; break;
						case 4:		v2 = mS; break;
						case 5:		v2 = mPC; break;
						case 8:		v2 = mA; break;
						case 9:		v2 = mB; break;
						case 10:	v2 = mCC; break;
						case 11:	v2 = mDP; break;
						default:
							break;
					}

					switch((mData >> 4) & 15) {
						case 0:
							mA = (uint8)(v2 >> 8);
							mB = (uint8)v2;
							break;
						case 1:		mX = (uint16)v2; break;
						case 2:		mY = (uint16)v2; break;
						case 3:		mU = (uint16)v2; break;
						case 4:		mS = (uint16)v2; ArmNmi(); break;
						case 5:		mPC = (uint16)v2; break;
						case 8:		mA = (uint8)v2; break;
						case 9:		mB = (uint8)v2; break;
						case 10:	SetCC((uint8)v2); break;
						case 11:	mDP = (uint8)v2; break;
						default:
							break;
					}

					switch(mData & 15) {
						case 0:
							mA = (uint8)(v1 >> 8);
							mB = (uint8)v1;
							break;
						case 1:		mX = (uint16)v1; break;
						case 2:		mY = (uint16)v1; break;
						case 3:		mU = (uint16)v1; break;
						case 4:		mS = (uint16)v1; ArmNmi(); break;
						case 5:		mPC = (uint16)v1; break;
						case 8:		mA = (uint8)v1; break;
						case 9:		mB = (uint8)v1; break;
						case 10:	SetCC((uint8)v1); break;
						case 11:	mDP = (uint8)v1; break;
						default:
							break;
					}
				}
				--mCyclesLeft;
				break;

			case k6809StateTfr:
				{
					uint32 v = 0xFFFF;

					switch((mData >> 4) & 15) {
						case 0:
							v = ((uint32)mA << 8) + mB;
							break;
						case 1:		v = mX; break;
						case 2:		v = mY; break;
						case 3:		v = mU; break;
						case 4:		v = mS; break;
						case 5:		v = mPC; break;
						case 8:		v = mA; break;
						case 9:		v = mB; break;
						case 10:	v = mCC; break;
						case 11:	v = mDP; break;
						default:
							break;
					}

					switch(mData & 15) {
						case 0:
							mA = (uint8)(v >> 8);
							mB = (uint8)v;
							break;
						case 1:		mX = (uint16)v; break;
						case 2:		mY = (uint16)v; break;
						case 3:		mU = (uint16)v; break;
						case 4:		mS = (uint16)v; ArmNmi(); break;
						case 5:		mPC = (uint16)v; break;
						case 8:		mA = (uint8)v; break;
						case 9:		mB = (uint8)v; break;
						case 10:	SetCC((uint8)v); break;
						case 11:	mDP = (uint8)v; break;
						default:
							break;
					}
				}
				--mCyclesLeft;
				break;

			case k6809StatePushHiS_1:
				AT_CPU_WRITE_BYTE(--mS, (uint8)(mData >> 8));
				ArmNmi();
				--mCyclesLeft;
				break;

			case k6809StatePushLoS_1:
				AT_CPU_WRITE_BYTE(--mS, (uint8)mData);
				ArmNmi();
				--mCyclesLeft;
				break;

			case k6809StatePopHiS_1:
				mData = (uint32)AT_CPU_READ_BYTE(mS++) << 8;
				ArmNmi();
				--mCyclesLeft;
				break;

			case k6809StatePopLoS_1:
				mData += AT_CPU_READ_BYTE(mS++);
				ArmNmi();
				--mCyclesLeft;
				break;

			case k6809StatePopS_1:
				mData = AT_CPU_READ_BYTE(mS++);
				ArmNmi();
				--mCyclesLeft;
				break;

			case k6809StatePushRegMaskS_1: {
				if (!mData)
					break;

				uint8 v;
				if (mData & 0x8000) {
					if (mData & 0x80) {
						v = (uint8)(mPC >> 8);
						mData -= 0x8080;
					} else if (mData & 0x40) {
						v = (uint8)(mU >> 8);
						mData -= 0x8040;
					} else if (mData & 0x20) {
						v = (uint8)(mY >> 8);
						mData -= 0x8020;
					} else {
						v = (uint8)(mX >> 8);
						mData -= 0x8010;
					}
				} else {
					if (mData & 0x80) {
						mData += 0x8000;
						v = (uint8)mPC;
					} else if (mData & 0x40) {
						mData += 0x8000;
						v = (uint8)mU;
					} else if (mData & 0x20) {
						mData += 0x8000;
						v = (uint8)mY;
					} else if (mData & 0x10) {
						mData += 0x8000;
						v = (uint8)mX;
					} else if (mData & 0x08) {
						mData -= 0x08;
						v = (uint8)mDP;
					} else if (mData & 0x04) {
						mData -= 0x04;
						v = (uint8)mB;
					} else if (mData & 0x02) {
						mData -= 0x02;
						v = (uint8)mA;
					} else {
						mData -= 0x01;
						v = (uint8)mCC;
					}
				}

				AT_CPU_WRITE_BYTE(--mS, v);
				ArmNmi();

				--mCyclesLeft;
				if (mData)
					--mpNextState;
				break;
			}

			case k6809StatePullRegMaskS_1: {
				if (!mData)
					break;
			
				const uint8 v = AT_CPU_READ_BYTE(mS++);
				ArmNmi();

				if (mData & 0x8000) {
					if (mData & 0x10) {
						mX += v;
						mData -= 0x8010;
					} else if (mData & 0x20) {
						mY += v;
						mData -= 0x8020;
					} else if (mData & 0x40) {
						mU += v;
						mData -= 0x8040;
					} else if (mData & 0x80) {
						mPC += v;
						mData -= 0x8080;
					}
				} else {
					if (mData & 0x01) {
						mData -= 0x01;
						SetCC(v);
					} else if (mData & 0x02) {
						mData -= 0x02;
						mA = v;
					} else if (mData & 0x04) {
						mData -= 0x04;
						mB = v;
					} else if (mData & 0x08) {
						mData -= 0x08;
						mDP= v;
					} else if (mData & 0x10) {
						mData += 0x8000;
						mX = (uint16)((uint32)v << 8);
					} else if (mData & 0x20) {
						mData += 0x8000;
						mY = (uint16)((uint32)v << 8);
					} else if (mData & 0x40) {
						mData += 0x8000;
						mU = (uint16)((uint32)v << 8);
					} else {
						mData += 0x8000;
						mPC = (uint16)((uint32)v << 8);
					}
				}

				--mCyclesLeft;
				if (mData)
					--mpNextState;
				break;
			}

			case k6809StatePushRegMaskU_1: {
				if (!mData)
					break;

				uint8 v;
				if (mData & 0x8000) {
					if (mData & 0x80) {
						v = (uint8)(mPC >> 8);
						mData -= 0x8080;
					} else if (mData & 0x40) {
						v = (uint8)(mU >> 8);
						mData -= 0x8040;
					} else if (mData & 0x20) {
						v = (uint8)(mY >> 8);
						mData -= 0x8020;
					} else {
						v = (uint8)(mX >> 8);
						mData -= 0x8010;
					}
				} else {
					if (mData & 0x80) {
						mData += 0x8000;
						v = (uint8)mPC;
					} else if (mData & 0x40) {
						mData += 0x8000;
						v = (uint8)mU;
					} else if (mData & 0x20) {
						mData += 0x8000;
						v = (uint8)mY;
					} else if (mData & 0x10) {
						mData += 0x8000;
						v = (uint8)mX;
					} else if (mData & 0x08) {
						mData -= 0x08;
						v = (uint8)mDP;
					} else if (mData & 0x04) {
						mData -= 0x04;
						v = (uint8)mB;
					} else if (mData & 0x02) {
						mData -= 0x02;
						v = (uint8)mA;
					} else {
						mData -= 0x01;
						v = (uint8)mCC;
					}
				}

				AT_CPU_WRITE_BYTE(--mU, v);
				ArmNmi();

				--mCyclesLeft;
				if (mData)
					--mpNextState;
				break;
			}

			case k6809StatePullRegMaskU_1: {
				if (!mData)
					break;
			
				const uint8 v = AT_CPU_READ_BYTE(mU++);
				ArmNmi();

				if (mData & 0x8000) {
					if (mData & 0x10) {
						mX += v;
						mData -= 0x8010;
					} else if (mData & 0x20) {
						mY += v;
						mData -= 0x8020;
					} else if (mData & 0x40) {
						mU += v;
						mData -= 0x8040;
					} else if (mData & 0x80) {
						mPC += v;
						mData -= 0x8080;
					}
				} else {
					if (mData & 0x01) {
						mData -= 0x01;
						SetCC(v);
					} else if (mData & 0x02) {
						mData -= 0x02;
						mA = v;
					} else if (mData & 0x04) {
						mData -= 0x04;
						mB = v;
					} else if (mData & 0x08) {
						mData -= 0x08;
						mDP= v;
					} else if (mData & 0x10) {
						mData += 0x8000;
						mX = (uint16)((uint32)v << 8);
					} else if (mData & 0x20) {
						mData += 0x8000;
						mY = (uint16)((uint32)v << 8);
					} else if (mData & 0x40) {
						mData += 0x8000;
						mU = (uint16)((uint32)v << 8);
					} else {
						mData += 0x8000;
						mPC = (uint16)((uint32)v << 8);
					}
				}

				--mCyclesLeft;
				if (mData)
					--mpNextState;
				break;
			}

			case k6809StateSwapAddrPC_1:
				mData = mPC;
				mPC = mAddr;
				--mCyclesLeft;
				break;

			case k6809StateRti_TestE:
				mData = (mCC & E) ? 0xFE : 0x80;
				break;

			case k6809StateCwai_SetE:
				mCC &= mData;
				mCC |= E;
				mData = 0xFF;
				break;

			case k6809StateCwai_Wait:
				--mCyclesLeft;

				if (mbIntAttention) {
					if (mbNmiArmed && mbNmiAsserted) {
						mbNmiAsserted = false;
						mpNextState = &mDecoderTables.mDecodeHeap[mDecoderTables.mCwaiNmiSequence];
						break;
					} else if (mbFirqAsserted) {
						mpNextState = &mDecoderTables.mDecodeHeap[mDecoderTables.mCwaiFirqSequence];
						break;
					} else if (mbIrqAsserted) {
						mpNextState = &mDecoderTables.mDecodeHeap[mDecoderTables.mCwaiIrqSequence];
						break;
					}

					mbIntAttention = false;
				}

				--mpNextState;
				break;

			case k6809StateSync:
				--mCyclesLeft;

				// We can't use the IntAttention flag here as SYNC needs to ignore F/I.
				// Strangely, the 6809 datasheet doesn't show the NMI armed state being
				// checked by SYNC.
				if (mbNmiAsserted || mbFirqAsserted || mbIrqAsserted)
					break;

				--mpNextState;
				break;

			case k6809StateIntEntryE0:
				mCC &= ~E;
				mData = 0x81;		// push PC, CC
				break;

			case k6809StateIntEntryE1:
				mCC |= E;
				mData = 0xFF;		// push all
				break;

			case k6809StateBeginIrq:
				RecordInterrupt(true, false);
				mAddr = 0xFFF8;
				mCC |= I|F;
				break;

			case k6809StateBeginFirq:
				RecordInterrupt(true, true);
				mAddr = 0xFFF6;
				mCC |= I|F;
				break;

			case k6809StateBeginNmi:
				RecordInterrupt(false, true);
				mAddr = 0xFFFC;
				mCC |= I|F;
				break;

			case k6809StateBeginSwi:
				mAddr = 0xFFFA;
				mCC |= I|F;
				break;

			case k6809StateBeginSwi2:
				mAddr = 0xFFF4;
				break;

			case k6809StateBeginSwi3:
				mAddr = 0xFFF2;
				break;

			case k6809StateTest_NZV_1:
				mCC &= ~NZV;
				if (mData & 0x80)
					mCC += N;
				if (!(uint8)mData)
					mCC += Z;

				--mCyclesLeft;
				break;

			case k6809StateAndA_NZV:
				mData &= mA;
				mCC &= ~NZV;
				if (mData & 0x80)
					mCC += N;
				if (!(uint8)mData)
					mCC += Z;
				break;

			case k6809StateAndB_NZV:
				mData &= mB;
				mCC &= ~NZV;
				if (mData & 0x80)
					mCC += N;
				if (!(uint8)mData)
					mCC += Z;
				break;

			case k6809StateAndCC_1:
				SetCC(mCC & (uint8)mData);
				--mCyclesLeft;
				break;

			case k6809StateOrCC_1:
				SetCC(mCC | (uint8)mData);
				--mCyclesLeft;
				break;

			case k6809StateOrToA_NZV:
				mA |= mData;
				mCC &= ~NZV;
				if (mA & 0x80)
					mCC += N;
				if (!(uint8)mA)
					mCC += Z;
				break;

			case k6809StateOrToB_NZV:
				mB |= mData;
				mCC &= ~NZV;
				if (mB & 0x80)
					mCC += N;
				if (!(uint8)mB)
					mCC += Z;
				break;

			case k6809StateXorToA_NZV:
				mA ^= mData;
				mCC &= ~NZV;
				if (mA & 0x80)
					mCC += N;
				if (!(uint8)mA)
					mCC += Z;
				break;

			case k6809StateXorToB_NZV:
				mB ^= mData;
				mCC &= ~NZV;
				if (mB & 0x80)
					mCC += N;
				if (!(uint8)mB)
					mCC += Z;
				break;

			case k6809StateAddA_HNZVC: {
				mCC &= ~HNZVC;

				const uint8 arg = (uint8)mData;
				const uint32 v = mA + arg;
			
				if (v & 0x100)
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mA) & ~(arg ^ mA) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateAddB_HNZVC: {
				mCC &= ~HNZVC;

				const uint8 arg = (uint8)mData;
				const uint32 v = mB + arg;
			
				if (v & 0x100)
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mB) & ~(arg ^ mB) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateAddD_NZVC_1: {
				mCC &= ~NZVC;

				const uint32 arg1 = ((uint32)mA << 8) + mB;
				const uint32 arg2 = mData;
				const uint32 v = arg1 + arg2;
			
				if (v & 0x10000)
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ arg1) & ~(arg1 ^ arg2) & 0x8000)
					mCC += V;
			
				if (!(uint16)v)
					mCC += Z;
			
				if (v & 0x8000)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateAdcA_HNZVC: {
				const uint8 arg = (uint8)mData;
				const uint32 v = mA + arg + (mCC & C);

				mCC &= ~HNZVC;
			
				if (v & 0x100)
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mA) & ~(arg ^ mA) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateAdcB_HNZVC: {
				const uint8 arg = (uint8)mData;
				const uint32 v = mB + arg + (mCC & C);

				mCC &= ~HNZVC;
			
				if (v & 0x100)
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mB) & ~(arg ^ mB) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateSubA_HNZVC: {
				mCC &= ~HNZVC;

				const uint8 arg = (uint8)~mData;
				const uint32 v = mA + arg + 1;
			
				if (!(v & 0x100))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mA) & ~(arg ^ mA) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateSubB_HNZVC: {
				mCC &= ~HNZVC;

				const uint8 arg = (uint8)~mData;
				const uint32 v = mB + arg + 1;
			
				if (!(v & 0x100))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mB) & ~(arg ^ mB) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateSubD_NZVC_1: {
				mCC &= ~NZVC;

				const uint32 d = ((uint32)mA << 8) + mB;
				const uint16 arg = (uint16)~mData;
				const uint32 v = d + arg + 1;
			
				if (!(v & 0x10000))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ d) & ~(arg ^ d) & 0x8000)
					mCC += V;
			
				if (!(uint16)v)
					mCC += Z;
			
				if (v & 0x8000)
					mCC += N;

				mData = v;
				--mCyclesLeft;
				break;
			}

			case k6809StateSbcA_HNZVC: {
				const uint8 arg = (uint8)~mData;
				const uint32 v = mA + arg + (mCC & C ? 0 : 1);

				mCC &= ~HNZVC;
			
				if (!(v & 0x100))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mA) & ~(arg ^ mA) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateSbcB_HNZVC: {
				const uint8 arg = (uint8)~mData;
				const uint32 v = mB + arg + (mCC & C ? 0 : 1);

				mCC &= ~HNZVC;
			
				if (!(v & 0x100))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mB) & ~(arg ^ mB) & 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				break;
			}

			case k6809StateCmpX_NZVC_1: {
				mCC &= ~NZVC;

				const uint16 arg = (uint16)~mData;
				const uint32 v = mX + arg + 1;
			
				if (!(v & 0x10000))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mX) & ~(arg ^ mX) & 0x8000)
					mCC += V;
			
				if (!(uint16)v)
					mCC += Z;
			
				if (v & 0x8000)
					mCC += N;

				--mCyclesLeft;
				break;
			}

			case k6809StateCmpY_NZVC_1: {
				mCC &= ~NZVC;

				const uint16 arg = (uint16)~mData;
				const uint32 v = mY + arg + 1;
			
				if (!(v & 0x10000))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mY) & ~(arg ^ mY) & 0x8000)
					mCC += V;
			
				if (!(uint16)v)
					mCC += Z;
			
				if (v & 0x8000)
					mCC += N;

				--mCyclesLeft;
				break;
			}

			case k6809StateCmpU_NZVC_1: {
				mCC &= ~NZVC;

				const uint16 arg = (uint16)~mData;
				const uint32 v = mU + arg + 1;
			
				if (!(v & 0x10000))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mU) & ~(arg ^ mU) & 0x8000)
					mCC += V;
			
				if (!(uint16)v)
					mCC += Z;
			
				if (v & 0x8000)
					mCC += N;

				--mCyclesLeft;
				break;
			}

			case k6809StateCmpS_NZVC_1: {
				mCC &= ~NZVC;

				const uint16 arg = (uint16)~mData;
				const uint32 v = mS + arg + 1;
			
				if (!(v & 0x10000))
					mCC += C;

				// overflow if arguments had same sign and result is different
				if ((v ^ mS) & ~(arg ^ mS) & 0x8000)
					mCC += V;
			
				if (!(uint16)v)
					mCC += Z;
			
				if (v & 0x8000)
					mCC += N;

				--mCyclesLeft;
				break;
			}

			case k6809StateNeg_HNZVC_1: {
				mCC &= ~HNZVC;

				const uint8 v = (uint8)(0U - mData);
			
				if (v)
					mCC += C;

				if (v == 0x80)
					mCC += V;
			
				if (!(uint8)v)
					mCC += Z;
			
				if (v & 0x80)
					mCC += N;

				mData = v;
				--mCyclesLeft;
				break;
			}

			case k6809StateDec_NZV_1:
				--mData;
				mCC &= ~NZV;
				if (mData & 0x80)
					mCC += N;
				if (!(uint8)mData)
					mCC += Z;
				if ((uint8)mData == 0x7F)
					mCC += V;
				--mCyclesLeft;
				break;

			case k6809StateInc_NZV_1:
				++mData;
				mCC &= ~NZV;
				if (mData & 0x80)
					mCC += N;
				if (!(uint8)mData)
					mCC += Z;
				if ((uint8)mData == 0x80)
					mCC += V;
				--mCyclesLeft;
				break;

			case k6809StateAsl_HNZVC_1:
				mCC &= ~HNZVC;

				if ((mData + 0x40) & 0x80)
					mCC += V;

				if (mData & 0x80)
					mCC += C;

				mData = (uint8)(mData + mData);

				if (mData & 0x80)
					mCC += N;

				if (!mData)
					mCC += Z;

				--mCyclesLeft;
				break;

			case k6809StateAsr_HNZC_1:
				mCC &= ~HNZC;

				if (mData & 0x01)
					mCC += C;

				mData = (uint8)((sint8)mData >> 1);

				if (!mData)
					mCC += Z;

				--mCyclesLeft;
				break;

			case k6809StateLsr_NZC_1:
				mCC &= ~NZC;

				if (mData & 0x01)
					mCC += C;

				mData = (uint8)mData >> 1;

				if (!mData)
					mCC += Z;

				--mCyclesLeft;
				break;

			case k6809StateRol_NZVC_1: {
				const uint8 carryIn = mCC & C;

				mCC &= ~NZVC;

				if (mData & 0x80)
					mCC += C;

				if ((mData + 0x40) & 0x80)
					mCC += V;

				mData = ((uint8)mData << 1) + carryIn;

				if (mData & 0x80)
					mCC += N;

				if (!mData)
					mCC += Z;

				break;
			}

			case k6809StateRor_NZC_1: {
				const uint8 carryIn = mCC & C;

				mCC &= ~NZC;

				if (mData & 0x01)
					mCC += C;

				mData = ((uint8)mData >> 1) + (carryIn << 7);

				if (mData & 0x80)
					mCC += N;

				if (!mData)
					mCC += Z;

				--mCyclesLeft;
				break;
			}

			case k6809StateCom_NZVC_1:
				mData = ~mData;
				mCC &= ~NZV;
				mCC |= C;

				if (!(uint8)mData)
					mCC |= Z;

				if (mData & 0x80)
					mCC |= N;

				--mCyclesLeft;
				break;

			case k6809StateMul_ZC_1:
				{
					const uint32 v = (uint32)mA * mB;

					mA = (uint8)(v >> 8);
					mB = (uint8)v;

					mCC &= ~(Z|C);
					if (!v)
						mCC |= Z;
					if (mB & 0x80)
						mCC |= C;
				}
				--mCyclesLeft;
				break;

			case k6809StateDaa_NZVC_1: {
				uint8 correct = 0;

				if ((mCC & H) || (mA & 0x0F) > 9)
					correct = 6;

				if ((mCC & C) || mA >= 0x89)
					correct += 0x60;

				const uint32 v = (uint32)mA + correct;
				mA = (uint8)v;

				mCC &= ~NZVC;

				if (mA & 0x80)
					mCC |= N;

				if (!mA)
					mCC |= Z;

				if (v >= 0x100)
					mCC |= C;

				--mCyclesLeft;
				break;
			}

			case k6809StateSex_NZV_1:	// sign extend B -> A
				mCC &= ~NZV;

				mA = (uint8)((sint8)mB >> 7);

				if (!mA)
					mCC |= Z;
				else
					mCC |= N;

				--mCyclesLeft;
				break;

			case k6809StateAbx_1:
				mX += mB;
				break;

			case k6809StateBra_1:		// branch always
				mPC = mAddr;
				--mCyclesLeft;
				break;

			case k6809StateBhi_1:		// branch on (C or Z)=0
				if (!(mCC & (C|Z)))
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBls_1:		// branch on (C or Z)=1
				if (mCC & (C|Z))
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBcc_1:		// branch on C=0
				if (!(mCC & C))
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBcs_1:		// branch on C=1
				if (mCC & C)
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBne_1:		// branch on Z=0
				if (!(mCC & Z))
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBeq_1:		// branch on Z=1
				if (mCC & Z)
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBvc_1:		// branch on V=0
				if (!(mCC & V))
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBvs_1:		// branch on V=1
				if (mCC & V)
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBpl_1:		// branch on N=0
				if (!(mCC & N))
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBmi_1:		// branch on N=1
				if (mCC & N)
					mPC = mAddr;
				else
					++mpNextState;
				--mCyclesLeft;
				break;

			case k6809StateBge_1:		// branch on N^V=0
				switch(mCC & (N|V)) {
					case 0:
					case N|V:
						mPC = mAddr;
						break;
					default:
						++mpNextState;
						break;
				}
				--mCyclesLeft;
				break;

			case k6809StateBlt_1:		// branch on N^V=1
				switch(mCC & (N|V)) {
					case N:
					case V:
						mPC = mAddr;
						break;
					default:
						++mpNextState;
						break;
				}
				--mCyclesLeft;
				break;

			case k6809StateBgt_1:		// branch on (Z or N^V) = 0
				switch(mCC & (N|Z|V)) {
					case 0:
					case N|V:
						mPC = mAddr;
						break;
					default:
						++mpNextState;
						break;
				}
				--mCyclesLeft;
				break;

			case k6809StateBle_1:		// branch on (Z or N^V) = 1
				switch(mCC & (N|Z|V)) {
					default:
						mPC = mAddr;
						break;
					case 0:
					case N|V:
						++mpNextState;
						break;
				}
				--mCyclesLeft;
				break;

			case k6809StateRegenerateDecodeTables:
				mpNextState = RegenerateDecodeTables();
				break;

			case k6809StateAddToHistory:
				{
					ATCPUHistoryEntry * VDRESTRICT he = &mpHistory[mHistoryIndex++ & 131071];

					he->mCycle = mCyclesBase - mCyclesLeft;
					he->mUnhaltedCycle = mCyclesBase - mCyclesLeft;
					he->mEA = 0xFFFFFFFFUL;
					he->mPC = mPC - 1;
					he->mP = mCC;
					he->mA = mA;
					he->mExt.mAH = mB;
					he->mX = (uint8)mX;
					he->mExt.mXH = (uint8)(mX >> 8);
					he->mY = (uint8)mY;
					he->mExt.mYH = (uint8)(mY >> 8);
					he->mS = (uint8)mS;
					he->mExt.mSH = (uint8)(mS >> 8);
					he->mbIRQ = false;
					he->mbNMI = false;
					he->mSubCycle = 0;
					he->mbEmulation = true;
					he->mK = mDP;
					he->mD = mU;

					for(int i=0; i<4; ++i) {
						uint16 pc = mPC - 1 + i;
						he->mOpcode[i] = ATCP_DEBUG_READ_BYTE(pc);
					}

					he->mB = ATCP_DEBUG_READ_BYTE(mPC + 3);
				}
				break;

			case k6809StateBreakOnUnsupportedOpcode:
				--mpNextState;
				--mCyclesLeft;
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

	force_exit:
		;
}

///////////////////////////////////////////////////////////////////

#undef AT_CPU_READ_BYTE
#undef AT_CPU_READ_BYTE_ADDR16
#undef AT_CPU_READ_BYTE_HL
#undef AT_CPU_WRITE_BYTE
#undef AT_CPU_WRITE_BYTE_HL

#undef INSN_FETCH
#undef INSN_FETCH_TO
#undef INSN_FETCH_NOINC
