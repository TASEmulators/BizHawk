//	Altirra - Atari 800/800XL emulator
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
#include <at/atcore/scheduler.h>
#include "cpu.h"
#include "cpumemory.h"
#include "cputracer.h"
#include "trace.h"
#include "tracecpu.h"

namespace {
	struct ATBytemap256 {
		uint8_t v[256];

		constexpr void set(uint8_t v0, std::initializer_list<uint8> indices) {
			for(uint8 index : indices)
				v[index] = v0;
		}

		constexpr uint8 operator[](size_t index) const {
			return v[index];
		}
	};

	constexpr ATBytemap256 MakeIdleInsnMap() {
		ATBytemap256 bytemap = {};

		// branch instructions
		bytemap.set(1, { 0x10, 0x30, 0x50, 0x70, 0x90, 0xB0, 0xD0, 0xF0 });
		
		// JMP abs
		bytemap.set(1, { 0x4C });

		// LDA zp, abs
		bytemap.set(1, { 0xA5, 0xAD } );

		// LDX zp, abs
		bytemap.set(1, { 0xA6, 0xAE } );

		// LDY zp, abs
		bytemap.set(1, { 0xA4, 0xAC } );

		// BIT zp, abs
		bytemap.set(1, { 0x24, 0x2C } );

		// CMP imm/zp/abs
		bytemap.set(1, { 0xC9, 0xC5, 0xCD } );

		// CPX imm/zp/abs
		bytemap.set(1, { 0xE0, 0xE4, 0xEC } );

		// CPY imm/zp/abs
		bytemap.set(1, { 0xC0, 0xC4, 0xCC } );

		// LDA/LDX/LDY imm
		bytemap.set(1, { 0xA9, 0xA2, 0xA0 } );

		// STA/STX/STY abs, conditional on addr=WSYNC
		bytemap.v[0x8D] = 2;
		bytemap.v[0x8E] = 2;
		bytemap.v[0x8C] = 2;

		return bytemap;
	}

	constexpr ATBytemap256 kATIdleInsnTable = MakeIdleInsnMap();

	static constexpr const wchar_t *kThreadChannelNames[] = {
		L"Idle",
		L"Main",
		L"CIO",
		L"SIO",
		L"IRQ",
		L"VBI",
		L"DLI",
	};
}

///////////////////////////////////////////////////////////////////////////

ATCPUTracer::ATCPUTracer() {
}

ATCPUTracer::~ATCPUTracer() {
}

void ATCPUTracer::Init(ATCPUEmulator *cpu, ATScheduler *scheduler, ATScheduler *slowScheduler, IATCPUTimestampDecoderProvider *tsdprovider, ATTraceContext *traceContext, bool traceInsns, bool traceBasic) {
	mpTSDProvider = tsdprovider;
	mpCPU = cpu;
	mpCPU->SetTracingEnabled(true);
	mpScheduler = scheduler;
	mpSlowScheduler = slowScheduler;
	mbTraceBasic = traceBasic;

	ATTraceGroup *traceGroup = traceContext->mpCollection->AddGroup(L"CPU");

	static_assert(vdcountof(kThreadChannelNames) == vdcountof(mpTraceChannels), "");
	for(size_t i=0; i<vdcountof(mpTraceChannels); ++i)
		mpTraceChannels[i] = traceGroup->AddSimpleChannel(traceContext->mBaseTime, traceContext->mBaseTickScale, kThreadChannelNames[i]);

	if (traceBasic) {
		mpTraceChannelBasic = traceGroup->AddFormattedChannel(traceContext->mBaseTime, traceContext->mBaseTickScale, L"BASIC");
	}

	if (traceInsns) {
		ATTraceGroup *traceGroupHistory = traceContext->mpCollection->AddGroup(L"CPU History", kATTraceGroupType_CPUHistory);
		mpTraceChannelHistory = new ATTraceChannelCPUHistory(traceContext->mBaseTime, traceContext->mBaseTickScale, L"History", cpu->GetDisasmMode(), cpu->GetSubCycles(), &traceContext->mMemTracker);
		traceGroupHistory->AddChannel(mpTraceChannelHistory);
	}

	mLastHistoryCounter = mpCPU->GetHistoryCounter() + 1;
	mbAdjustStackNext = false;
	mLastS = mpCPU->GetS();
	mThreadContext = kThreadContext_Main;
	mThreadContextStartTime = traceContext->mBaseTime;
	mIdleCounter = 0;

	Reschedule();

	std::fill(std::begin(mStackTable), std::end(mStackTable), StackEntry { -1, 0 } );
}

void ATCPUTracer::Shutdown() {
	if (mpCPU) {
		Update();
		mpCPU->SetTracingEnabled(false);
		mpCPU = nullptr;
	}

	for(ATTraceChannelSimple *&ch : mpTraceChannels)
		ch = nullptr;

	if (mpSlowScheduler) {
		mpSlowScheduler->UnsetEvent(mpUpdateEvent);
		mpSlowScheduler = NULL;
	}
}

void ATCPUTracer::OnScheduledEvent(uint32 id) {
	Reschedule();

	Update();
}

void ATCPUTracer::Reschedule() {
	mpUpdateEvent = mpSlowScheduler->AddEvent(mbTraceBasic ? 2 : 32, this, 1);
}

void ATCPUTracer::Update() {
	uint32 nextHistoryCounter = mpCPU->GetHistoryCounter();
	uint32 hcDelta = nextHistoryCounter - mLastHistoryCounter;

	if (hcDelta >= UINT32_C(0x80000000))
		return;

	uint32 count = hcDelta & (mpCPU->GetHistoryLength() - 1);
	mLastHistoryCounter = nextHistoryCounter;

	if (!count)
		return;

	const auto& tsdecoder = mpTSDProvider->GetTimestampDecoder();

	const ATCPUHistoryEntry *hentp = &mpCPU->GetHistory(count);
	uint32 pos = count;

	const uint64 currentTime = mpScheduler->GetTick64();
	const auto extendTime64 = [threshold = (uint32)(currentTime + 0x10000), tb0 = currentTime - (uint32)currentTime](uint32 t32) {
		return (uint32)(t32 - threshold) < UINT32_C(0x80000000) ? tb0 + t32 - UINT64_C(0x100000000) : tb0 + t32;
	};

	if (mThreadContext < 0) {
		mThreadContext = kThreadContext_Main;
		mThreadContextStartTime = extendTime64(hentp->mCycle);
	}

	if (mIdleCounter > 0x800000)
		mIdleCounter = 0x800000;

	while(pos) {
		const ATCPUHistoryEntry *hentn = &mpCPU->GetHistory(--pos);

		if (mpTraceChannelHistory) {
			const uint64 eventTime = currentTime + (uint64)(sint64)(sint32)(hentp->mCycle - (uint32)currentTime);

			mpTraceChannelHistory->AddEvent(eventTime, *hentp);
		}

		uint32 extpc = hentp->mPC + (hentp->mK << 16);
		uint32 addr = extpc;

		bool adjustStack = mbAdjustStackNext || hentp->mbIRQ || hentp->mbNMI;
		mbAdjustStackNext = false;

		uint8 opcode = hentp->mOpcode[0];
		switch(opcode) {
			case 0x20:		// JSR
			case 0x60:		// RTS
			case 0x40:		// RTI
				mbAdjustStackNext = true;
				break;
		}

		if (mbTraceBasic) {
			switch(opcode) {
				case 0xB1:	// LDA (zp),Y
					if (hentp->mOpcode[1] == 0x8A) {	// STMCUR
						uint16 addr = (uint16)(hentp->mEA - hentp->mY);

						if (mBasicLineAddr != addr) {
							mBasicLineAddr = addr;

							ATCPUEmulatorMemory *mem = mpCPU->GetMemory();
							const uint8 lo = mem->DebugReadByte(addr);
							const uint8 hi = mem->DebugReadByte(addr+1);

							sint32 line = lo + ((sint32)hi << 8);

							if (mBasicLineNo != line) {
								const uint64 t = extendTime64(hentp->mCycle);

								if (mBasicLineStartTime) {
									mpTraceChannelBasic->AddTickEvent(mBasicLineStartTime, t,
										[line16 = (uint16)line](VDStringW& ev) {
											ev.sprintf(L"%u", line16);
										}, kATTraceColor_Default);
								}

								mBasicLineNo = line;
								mBasicLineStartTime = t;
							}
						}
					}

					break;
			}
		}

		int newContext = mThreadContext;

		if (!(hentp->mP & AT6502::kFlagI) && !hentp->mbIRQ && !hentp->mbNMI) {
			switch(newContext){
				case kThreadContext_Idle:
				case kThreadContext_Main:
				case kThreadContext_CIOIdle:
				case kThreadContext_CIO:
				case kThreadContext_SIOIdle:
				case kThreadContext_SIO:
					break;

				case kThreadContext_VBI:
					newContext = kThreadContext_VBIDeferred;
					break;

				case kThreadContext_VBIDeferred:
					break;

				default: 
					newContext = kThreadContext_Main;
					break;
			}
		}

		bool forceContextSplit = false;
		if (adjustStack) {
			sint8 sdir = hentp->mS - mLastS;
			if (sdir > 0) {
				// pop
				do {
					const StackEntry& se = mStackTable[mLastS];

					if (se.mContext >= 0) {
						newContext = se.mContext;
						mIdleCounter = se.mIdleCounter;
						mIdleStartTime = 0;
						mStackTable[mLastS].mContext = -1;
					}
				} while(++mLastS != hentp->mS);
			} else {
				if (sdir < 0) {
					// push
					while(--mLastS != hentp->mS) {
						mStackTable[mLastS].mContext = -1;
					}

					mStackTable[mLastS] = StackEntry { (sint8)mThreadContext, mIdleCounter > 0xFF ? (uint8)0xFF : (uint8)mIdleCounter };
				}
			}

			if (hentp->mbNMI) {
				if (!hentp->mbIRQ) {
					if (tsdecoder.IsInterruptPositionVBI(hentp->mCycle))
						newContext = kThreadContext_VBI;
					else
						newContext = kThreadContext_DLI;

					forceContextSplit = true;
					mIdleCounter = 0;
				}
			} else if (hentp->mbIRQ) {
				newContext = kThreadContext_IRQ;
				forceContextSplit = true;
			} else if (opcode == 0x4C) {
				switch(hentp->mPC) {
					case 0xE456:
						newContext = kThreadContext_CIO;
						forceContextSplit = true;
						break;

					case 0xE459:
						newContext = kThreadContext_SIO;
						forceContextSplit = true;
						break;
				}
			}

			if (forceContextSplit)
				mIdleCounter = 0;
		}

		const uint8 idleCode = kATIdleInsnTable[opcode];
		bool isIdleInsn = idleCode != 0;

		if (idleCode == 2) {
			// check for write to WSYNC
			if (hentp->mOpcode[1] != 0x0A || hentp->mOpcode[2] != 0xD4) {
				isIdleInsn = false;
			}
		}

		switch(newContext) {
			case kThreadContext_Idle:
			case kThreadContext_SIOIdle:
			case kThreadContext_CIOIdle:
				if (!isIdleInsn)
					++newContext;
				break;

			case kThreadContext_Main:
			case kThreadContext_SIO:
			case kThreadContext_CIO:
				if (isIdleInsn) {
					if (!mIdleCounter++) {
						mIdleStartTime = extendTime64(hentp->mCycle);
					}

					if (mIdleCounter == 24) {
						--newContext;
					}
				} else {
					mIdleCounter = 0;
				}
				break;
		}

		if (mThreadContext != newContext || forceContextSplit) {
			uint64 t = extendTime64(hentp->mCycle);
			
			switch(newContext) {
				case kThreadContext_Idle:
				case kThreadContext_CIOIdle:
				case kThreadContext_SIOIdle:
					if (mIdleStartTime)
						t = mIdleStartTime;
					break;

				default:
					break;
			}

			if (t > mThreadContextStartTime) {
				static constexpr uint32 kThreadContextColors[] = {
					kATTraceColor_CPUThread_Idle,
					kATTraceColor_CPUThread_Main,
					kATTraceColor_CPUThread_Idle,
					kATTraceColor_CPUThread_Main,
					kATTraceColor_CPUThread_Idle,
					kATTraceColor_CPUThread_Main,
					kATTraceColor_CPUThread_IRQ,
					kATTraceColor_CPUThread_VBI,
					kATTraceColor_CPUThread_VBIDeferred,
					kATTraceColor_CPUThread_DLI,
				};

				static constexpr uint32 kThreadContextToChannel[] = {
					0, 1, 2, 2, 3, 3, 4, 5, 5, 6
				};

				static constexpr const wchar_t *kThreadContextNames[] = {
					L"Idle",
					L"Main",
					L"Idle",
					L"CIO",
					L"Idle",
					L"SIO",
					L"IRQ",
					L"Imm", L"Def",
					L"DLI",
				};

				static_assert(vdcountof(kThreadContextColors) == vdcountof(kThreadContextToChannel), "Trace channel mismatch");

				mpTraceChannels[kThreadContextToChannel[mThreadContext]]->AddTickEvent(mThreadContextStartTime, t, kThreadContextNames[mThreadContext], kThreadContextColors[mThreadContext]);
			}

			mThreadContext = newContext;
			mThreadContextStartTime = t;
			mIdleCounter = 0;
		}

		// tally here

		hentp = hentn;
	}
}
