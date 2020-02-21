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
#include <at/atcore/scheduler.h>
#include "antic.h"
#include "gtia.h"
#include "console.h"
#include "savestate.h"
#include "simeventmanager.h"
#include "trace.h"

#if VD_CPU_X86 || VD_CPU_X64
#include "antic_sse2.inl"
#endif

namespace {
	const uint8 kModeToFetchRate[16]={
	//  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F
		0, 0, 3, 3, 3, 3, 2, 2, 1, 1, 2, 2, 2, 3, 3, 3
	};

	const uint8 kClockPattern[4][8]={
		{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },	// rate 0: no fetching
		{ 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 },	// rate 1: every 8 cycles
		{ 0x11, 0x22, 0x44, 0x88, 0x11, 0x22, 0x44, 0x88 },	// rate 2: every 4 cycles
		{ 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA },	// rate 3: every 2 cycles
	};
}

enum {
	kATAnticEvent_UpdateRegisters = 1,
	kATAnticEvent_WSYNC = 2
};

ATAnticEmulator::ATAnticEmulator() {
	SetPALMode(false);
}

ATAnticEmulator::~ATAnticEmulator() {
}

void ATAnticEmulator::Init(ATAnticEmulatorConnections *mem, ATGTIAEmulator *gtia, ATScheduler *sch, ATSimulatorEventManager *simevmgr) {
	mpConn = mem;
	mpGTIA = gtia;
	mpScheduler = sch;
	mpSimEventMgr = simevmgr;

	memset(mActivityMap, 0, sizeof mActivityMap);
}

void ATAnticEmulator::SetPALMode(bool pal) {
	mScanlineLimit = pal ? 312 : 262;
	mScanlineMax = mScanlineLimit - 1;
	mVSyncStart = pal ? 275 : 251;
}

void ATAnticEmulator::ColdReset() {
	mPFRowDMAPtrBase = 0;
	mPFRowDMAPtrOffset = 0;
	mDLControlPrev = 0;
	mDLControl = 0;
	mRowCounter = 0;
	mbRowStopUseVScroll = false;
	mbDLActive = false;
	mbPFDMAEnabled = false;
	mbPFDMAActive = false;
	mCHACTL = 0;
	mDLIST = 0;
	mHSCROL = 0;
	mVSCROL = 0;
	mPMBASE = 0;
	mCHBASE = 0;
	mPENH = 0;
	mPENV = 0xFF;				// This powers up as $FF because it is output inverted from the register.
	mCharBaseAddr128 = 0;
	mCharBaseAddr64 = 0;
	mCharInvert = 0;
	mCharBlink = 0xff;
	mPFWidth = kPFDisabled;
	mPFWidthShift = 0;
	mPFDMAPatternCacheKey = 0xFFFFFFFF;
	mbHScrollEnabled = false;
	mbInBuggedVBlank = false;
	mbPhantomPMDMA = false;
	mbPhantomPlayerDMA = false;
	mAbnormalDMAPattern = 0;
	mEndingDMAPattern = 0;
	mAbnormalDecodePattern = 0;

	mNMIST = 0x1F;
	mWSYNCPending = 0;
	mbWSYNCActive = false;

	WarmReset();
}

void ATAnticEmulator::WarmReset() {
	// What is reset by a warm reset:
	//
	// - Vertical counter
	// - Horizontal counter
	// - Refresh address (which we don't care about, as it isn't visible)
	// - DMACTL
	// - DMA clock
	// - NMIEN
	//
	// Notable items that are NOT reset:
	// - WSYNC
	// - HSCROL
	// - VSCROL
	// - PMBASE
	// - CHBASE
	// - PENH
	// - PENV
	// - CHACTL
	// - DLISTL/H
	// - Memory scan counter
	// - NMIST

	++mFrame;
	mX = 0;
	mY = 0;
	mFrameStart = ATSCHEDULER_GETTIME(mpScheduler);

	mDMACTL = 0;
	mNMIEN = 0;
	mbDLExtraLoadsPending = false;
	mbMissileDMADisabledLate = false;
	mAbnormalDMAPattern = 0;
	mEndingDMAPattern = 0;
	mAbnormalDecodePattern = 0;

	UpdatePlayfieldTiming();
	UpdateDMAPattern();

	mRegisterUpdates.clear();
	mRegisterUpdateHeadIdx = 0;

	if (mpRegisterUpdateEvent) {
		mpScheduler->RemoveEvent(mpRegisterUpdateEvent);
		mpRegisterUpdateEvent = NULL;
	}

	mpScheduler->UnsetEvent(mpEventWSYNC);
	mWSYNCPending = 0;

	mpPFDataWrite = mPFDataBuffer;
	mpPFDataRead = mPFDataBuffer;
	mpPFCharFetchPtr = mPFCharBuffer;
	mPFDecodeOffset = 0;
	mPFDecodeCharOffset = 0;
}

void ATAnticEmulator::RequestNMI() {
	mNMIST |= 0x20;
	mpConn->AnticAssertNMI_RES();
}

void ATAnticEmulator::SetLightPenPosition(bool phase) {
	SetLightPenPosition(mX * 2 + phase, mY);
}

void ATAnticEmulator::SetLightPenPosition(int x, int y) {
	mPENH = x;
	mPENV = y >> 1;
}

uint8 ATAnticEmulator::AdvanceSpecial() {
	uint8 busActive = false;

	if (mX == 114) {
		AdvanceScanline();

		uint8 fetchModeX0 = mDMAPattern[0];

		if (!(fetchModeX0 & 0x80))
			return fetchModeX0;
	}

	// Check if we are in the special DMA region.
	if (mX < 8) {
		// Check for abnormal DMA (bit 5); this means we have abnormal DMA going on during
		// cycles 0-7 and need to compute a composite address mask in case ANTIC tries to
		// do more than one DMA cycle at a time. When this happens, the address that is
		// fetched is the AND of all requested addresses. Note that this happens *even if
		// playfield DMA is supressed by DMACTL*.
		uint32 addressMask = 0xFFFF;

		const uint8 dmaPat = mDMAPattern[mX];
		if ((dmaPat & 0x06) && mX) {
			// bitmap graphic or character name fetch
			if (dmaPat & 0x02)
				addressMask &= mPFRowDMAPtrBase + (mPFRowDMAPtrOffset & 0x0fff);

			// character data fetch
			if (dmaPat & 0x04) {
				const uint8 c = (uint8)(uintptr_t)mpPFDataRead >= 48 ? 0xFF : *mpPFDataRead;
				addressMask &= mPFCharFetchPtr + ((uint32)(c & mPFCharMask) << 3);
			}
		}

		if (mX == 0) {		// missile DMA
			if ((mbMissileDMADisabledLate || (mDMACTL & 0x0C)) && (uint32)(mY - 8) < 240) {	// player DMA also forces missile DMA (Run For the Money requires this).
				mbMissileDMADisabledLate = false;

				if (mDMACTL & 0x10) {
					uint8 byte = mpConn->AnticReadByte(addressMask & (((mPMBASE & 0xf8) << 8) + 0x0300 + mY));
					busActive = true;
					mpGTIA->UpdateMissile((mY & 1) != 0, byte);
				} else {
					// DMA occurs every scanline even in half-height mode.
					uint8 byte = mpConn->AnticReadByte(addressMask & (((mPMBASE & 0xfc) << 8) + 0x0180 + (mY >> 1)));
					busActive = true;
					mpGTIA->UpdateMissile((mY & 1) != 0, byte);
				}

				// If player DMA is not enabled, do phantom DMA on regular (unshifted) timing.
				if (!(mDMACTL & 0x08)) {
					// If we were still doing missile DMA due to a late disable but
					// player DMA is now turned off, GTIA has already seen the missile
					// DMA and may start reading player DMA. Enable early fetch.
					mbPhantomPlayerDMA = true;
				}
			}

			mbDLDMAEnabledInTime = (mDMACTL & 0x20) != 0;
			mDLISTLatch = mDLIST;
		} else if (mX == 1) {
			mbPFDMAEnabled = false;
			mbPFDMAActive = false;
			mPFDMALastCheckX = 0;
			mPFDMALatchedStart = 0;
			mPFDMALatchedEnd = 0;
			mPFDMALatchedVEnd = 0;
			mPFDisplayStart = 110;
			mPFDisplayEnd = 110;
			mDLHistory[mY].mbValid = false;

			// Display start is at scanline 8.
			if (mY == 8) {
				mbDLActive = true;
				mRowCounter = 0;
				mRowCount = 1;
				mbRowStopUseVScroll = false;
				mbRowAdvance = false;

				// Note that we MUST NOT clear mDLControlPrev here. If the display list extends all
				// the way to scan line 248, ANTIC still remembers the status of the vertical scroll
				// bit.
				mDLControl = mDLControlPrev;
			}

			// compute stop line
			uint32 rowStop = mbRowStopUseVScroll ? mLatchedVScroll : ((mRowCount - 1) & 15);
			mLatchedVScroll = mVSCROL;

			if (mRowCounter != rowStop) {
				mRowCounter = (mRowCounter + 1) & 15;

				mbPFDMAActive = true;

				// Most mode lines only load on scan 0, but jumps are an exception.
				if ((mDLControl & 15) != 1)
					mbDLExtraLoadsPending = false;
			} else {
				mRowCounter = 0;
				
				if (mbDLActive) {
					mbDLExtraLoadsPending = false;
					mDLControlPrev = mDLControl;

					DLHistoryEntry& ent = mDLHistory[mY];
					ent.mDLAddress = mDLISTLatch;
					ent.mPFAddress = mPFRowDMAPtrBase + mPFRowDMAPtrOffset;
					ent.mHVScroll = mHSCROL + (mVSCROL << 4);
					ent.mDMACTL = mDMACTL;
					ent.mCHBASE = mCHBASE >> 1;
					ent.mbValid = true;

					if (mbDLDMAEnabledInTime) {
						mDLControl = mpConn->AnticReadByte(mDLISTLatch & addressMask);

						busActive = true;
						mDLIST = (mDLIST & 0xFC00) + ((mDLIST + 1) & 0x03FF);

						if (mbPhantomPMDMA) {
							mbPhantomPMDMAActive = true;

							mpGTIA->UpdateMissile((mY & 1) != 0, mDLControl);
						}
					}

					ent.mControl = mDLControl;

					uint8 mode = mDLControl & 0x0f;
					if (mode == 1 || (mode >= 2 && (mDLControl & 0x40)))
						mbDLExtraLoadsPending = true;

					mRowCounter = 0;
					mPFPushCycleMask = 0;
					mPFWidthShift = 0;

					mPFPushMode = k160;
					mPFHiresMode = false;

					switch(mode) {
					case 0:
						mRowCount = ((mDLControl >> 4) & 7) + 1;
						mPFPushMode = kBlank;
						break;
					case 1:
						mRowCount = 1;
						mPFPushMode = kBlank;
						break;
					case 2:						// IR mode 2: 40x8 characters, 2 colors/1 lum
						mRowCount = 8;
						mPFPushCycleMask = 1;	// 320 pixels normal
						mPFPushMode = k320;
						mPFWidthShift = 2;
						mPFHiresMode = true;
						break;
					case 3:						// IR mode 3: 40x10 characters, 2 colors/1 lum
						mRowCount = 10;
						mPFPushCycleMask = 1;	// 320 pixels normal
						mPFPushMode = k320;
						mPFWidthShift = 2;
						mPFHiresMode = true;
						break;
					case 4:						// IR mode 4: 40x8 characters, 5 colors
						mRowCount = 8;
						mPFPushCycleMask = 1;	// 160 pixels normal
						mPFWidthShift = 2;
						break;
					case 5:						// IR mode 5: 40x16 characters, 5 colors
						mRowCount = 16;
						mPFPushCycleMask = 1;	// 160 pixels normal
						mPFWidthShift = 2;
						break;
					case 6:						// IR mode 6: 20x8 characters, 5 colors
						mRowCount = 8;
						mPFPushCycleMask = 3;	// 160 pixels normal
						mPFWidthShift = 1;
						break;
					case 7:						// IR mode 7: 20x16 characters, 5 colors
						mRowCount = 16;
						mPFPushCycleMask = 3;	// 160 pixels normal
						mPFWidthShift = 1;
						break;
					case 8:						// IR mode 8: 40x8 graphics, 4 colors
						mRowCount = 8;
						mPFPushCycleMask = 7;	// 40 pixels normal
						mPFWidthShift = 0;
						break;
					case 9:						// IR mode 9: 80x4 graphics, 2 colors
						mRowCount = 4;
						mPFPushCycleMask = 7;	// 40 pixels normal
						mPFWidthShift = 0;
						break;
					case 10:					// IR mode A: 80x4 graphics, 4 colors
						mRowCount = 4;
						mPFPushCycleMask = 3;	// 80 pixels normal
						mPFWidthShift = 1;
						break;
					case 11:					// IR mode B: 160x2 graphics, 2 colors
						mRowCount = 2;
						mPFPushCycleMask = 3;	// 80 pixels normal
						mPFWidthShift = 1;
						break;
					case 12:					// IR mode C: 160x1 graphics, 2 colors
						mRowCount = 1;
						mPFPushCycleMask = 3;	// 160 pixels normal
						mPFWidthShift = 1;
						break;
					case 13:					// IR mode D: 160x2 graphics, 4 colors
						mRowCount = 2;
						mPFPushCycleMask = 1;	// 160 pixels normal
						mPFWidthShift = 2;
						break;
					case 14:					// IR mode E: 160x1 graphics, 4 colors
						mRowCount = 1;
						mPFPushCycleMask = 1;	// 160 pixels normal
						mPFWidthShift = 2;
						break;
					case 15:					// IR mode F: 320x1 graphics, 2 colors/1 lum
						mRowCount = 1;
						mPFPushCycleMask = 1;	// 320 pixels normal
						mPFPushMode = k320;
						mPFWidthShift = 2;
						mPFHiresMode = true;
						break;
					}

					// Check for changes in abnormal DMA pattern.
					if (mode < 2) {
						// On a blank line -- IR modes 0 and 1 -- the DMA clock is unconditionally cleared, ending
						// any abnormal DMA condition.
						mAbnormalDMAPattern = 0;
					} else {
						// At this point we change the length of the DMA clock depending on the fetch rate
						// required for the new mode. Two fun things can happen here:
						//
						// 1) Modes using the slowest fetch rate -- IR modes 8 and 9 -- can capture latent clock
						//    bits. We use the mEndingDMAPattern field to do this.
						//
						// 2) Modes using the medium and fast fetch rates will alter any existing abnormal DMA
						//    fetch pattern. This may add or remove effective bits from the pattern.
						//
						switch(kModeToFetchRate[mode]) {
							case 1:
								mAbnormalDMAPattern |= mEndingDMAPattern;
								break;

							case 2:
								mAbnormalDMAPattern = (mAbnormalDMAPattern & 15) * 0x11;
								break;

							case 3:
								mAbnormalDMAPattern = (mAbnormalDMAPattern & 3) * 0x55;
								break;
						}
					}

					// check for vertical scrolling
					uint8 scrollPrev = mDLControlPrev;
					uint8 scrollCur = mDLControl;
					if ((scrollPrev & 15) < 2)
						scrollPrev = 0;
					if ((scrollCur & 15) < 2)
						scrollCur = 0;

					mbRowStopUseVScroll = false;
					if ((scrollCur ^ scrollPrev) & 0x20) {
						if (scrollCur & 0x20)
							mRowCounter = mVSCROL;
						else
							mbRowStopUseVScroll = true;
					}

					// check for horizontal scrolling
					mbHScrollEnabled = false;
					if (mode != 1 && (scrollCur & 0x10))
						mbHScrollEnabled = true;

					mbPFDMAEnabled = true;
					mbPFDMAActive = true;
				}
			}

			mPFHScrollDMAOffset = 0;
			mbHScrollDelay = false;

			if (mbHScrollEnabled) {
				mPFHScrollDMAOffset = (mHSCROL & 14) >> 1;
				mbHScrollDelay = (mHSCROL & 1) != 0;
			}

			UpdateCurrentCharRow();
			UpdatePlayfieldTiming();

			mAbnormalDecodeShifter = 0;
		} else if (mX >= 2 && mX <= 5) {		// player DMA
			if ((mDMACTL & 0x08) && (uint32)(mY - 8) < 240) {
				uint32 index = mX - 2;
				uint8 byte;
				if (mDMACTL & 0x10) {
					byte = mpConn->AnticReadByte(addressMask & (((mPMBASE & 0xf8) << 8) + 0x400 + (0x0100 * index) + mY));
				} else {
					// DMA occurs every scanline even in half-height mode.
					byte = mpConn->AnticReadByte(addressMask & (((mPMBASE & 0xfc) << 8) + 0x200 + (0x0080 * index) + (mY >> 1)));
				}

				mpGTIA->UpdatePlayer((mY & 1) != 0, index, byte);
				busActive = true;
			} else if (mbPhantomPMDMAActive && mX > 3) {
				// We need to read the result of the _previous_ cycle due to the CPU executing after us.
				mpGTIA->UpdatePlayer((mY & 1) != 0, mX - 4, *mpConn->mpAnticBusData);
			} else if (mbPhantomPlayerDMA && mX > 2) {
				mpGTIA->UpdatePlayer((mY & 1) != 0, mX - 3, *mpConn->mpAnticBusData);
			}
		} else if (mX == 6) {		// address DMA (low)
			if (mbPhantomPMDMAActive)
				mpGTIA->UpdatePlayer((mY & 1) != 0, 2, *mpConn->mpAnticBusData);
			else if (mbPhantomPlayerDMA)
				mpGTIA->UpdatePlayer((mY & 1) != 0, 3, *mpConn->mpAnticBusData);

			if (mbDLExtraLoadsPending && (mDMACTL & 0x20)) {
				mDLNext = mpConn->AnticReadByte(mDLIST & addressMask);
				busActive = true;
				mDLIST = (mDLIST & 0xFC00) + ((mDLIST + 1) & 0x03FF);
			}
				mLatchedVScroll2 = mVSCROL;
		} else if (mX == 7) {		// address DMA (high) + NMIST change
			if (mbPhantomPMDMAActive)
				mpGTIA->UpdatePlayer((mY & 1) != 0, 3, *mpConn->mpAnticBusData);

			if (mbDLExtraLoadsPending && (mDMACTL & 0x20)) {
				uint8 b = mpConn->AnticReadByte(mDLIST & addressMask);
				busActive = true;
				mDLIST = (mDLIST & 0xFC00) + ((mDLIST + 1) & 0x03FF);

				uint16 addr = mDLNext + ((uint16)b << 8);
				if ((mDLControl & 0x0f) == 1) {
					mDLIST = addr;

					if (mDLControl & 0x40) {
						mbDLActive = false;

						// We only clear this for the JVB instruction, because it's left on for the jump
						// instruction -- if it is stretched by vertical scrolling, ANTIC will continue
						// to fetch display list addresses for each scan line!
						mbDLExtraLoadsPending = false;

						// We have to preserve the DLI bit here, because Race In Space does a DLI on
						// the waitvbl command!
						mDLControl &= ~0x4f;
						mRowCount = 1;
					}
				} else {
					// correct display list history with new address
					DLHistoryEntry& ent = mDLHistory[mY];
					ent.mPFAddress = addr;

					mPFRowDMAPtrBase = addr & 0xf000;
					mPFRowDMAPtrOffset = addr & 0x0fff;
					mbDLExtraLoadsPending = false;
				}
			}

			mEarlyNMIEN = mNMIEN;

			// Note that we set these regardless of NMIEN bits. We also use a separate variable
			// from NMIST as NMIRES does NOT affect pending NMIs.
			mPendingNMIs = 0;

			if (mY == 248) {
				mPendingNMIs = 0x40;
				mNMIST |= 0x40;
				mNMIST &= ~0x80;

				// Note that we need to preserve the vertical scroll bit here to handle oversize DLs
				// properly.
				mDLControlPrev = mDLControl;
				mDLControl &= 0x20;

				memset(mPFDataBuffer, 0, sizeof mPFDataBuffer);
			} else {
				uint32 rowStop = mbRowStopUseVScroll ? mLatchedVScroll2 : ((mRowCount - 1) & 15);

				if ((mDLControl & 0x80) && mRowCounter == rowStop) {
					mPendingNMIs = 0x80;
					mNMIST &= ~0x40;
					mNMIST |= 0x80;
				}
			}
			mpPFDataWrite = mPFDataBuffer;
			mpPFDataRead = mPFDataBuffer;
			mpPFCharFetchPtr = mPFCharBuffer;
			mPFDecodeOffset = 0;
			mPFDecodeCharOffset = 0;
		}

		// Check again if phantom DMA is active. If so, and we did a DMA fetch already, we flip
		// on the phantom bit so the actual fetch uses the already latched data. Otherwise, we
		// let the normal path fetch.
		if ((dmaPat & 0x20) && busActive)
			busActive |= 0x10;

		if (mAnalysisMode) {
			switch(mAnalysisMode) {
			case kAnalyzeDMATiming:
				if (busActive)
					mActivityMap[mY][mX] |= 1;
				break;
			}
		}

		return busActive | (dmaPat & 0x3f);
	} else {
		if (mX == 8) {
			mEarlyNMIEN2 = mNMIEN;
			mbLateNMI = false;

			uint8 cumulativeNMIEN = mPendingNMIs & mEarlyNMIEN;
			uint8 cumulativeNMIENLate = mPendingNMIs & mEarlyNMIEN2 & ~mEarlyNMIEN;

			if (cumulativeNMIEN) {
				if (mY == 248)
					mpConn->AnticAssertNMI_VBI();
				else
					mpConn->AnticAssertNMI_DLI();
			} else if (cumulativeNMIENLate) {
				mbLateNMI = true;
			}
		} else if (mX == 9) {
			if (mbLateNMI) {
				if (mY == 248)
					mpConn->AnticAssertNMI_VBI();
				else
					mpConn->AnticAssertNMI_DLI();
			}
		} else if (mX == 10) {
			if (((unsigned)(mDLControl & 15) - 2) < 6)
				memset(mPFCharBuffer, 0, sizeof mPFCharBuffer);

			memset(mPFDecodeBuffer, 0x00, sizeof mPFDecodeBuffer);

			if ((unsigned)(mY - 8) >= 240) {
				if (mPFPushMode != k320 || !(mDMACTL & 3)) {
					mpGTIA->SetVBLANK(ATGTIAEmulator::kVBlankModeOn);
				} else {
					mpGTIA->SetVBLANK(ATGTIAEmulator::kVBlankModeBugged);

					memset(mPFDecodeBuffer, 0x0a, sizeof mPFDecodeBuffer);
				}

				if (mY == 248) {
					if (mPFPushMode == k320) {
						mbInBuggedVBlank = true;
						mVSyncShiftTime = 0;
					} else {
						mbInBuggedVBlank = false;
					}
				}
			} else {
				mpGTIA->SetVBLANK(ATGTIAEmulator::kVBlankModeOff);
				mbInBuggedVBlank = false;
			}

			mPFDecodeCounter = 1;
			mPFDisplayCounter = 0;
			mPFDMALastCheckX = 0;
		} else if (mX == 16) {
			mpGTIA->BeginScanline(mY, mPFPushMode == k320);
			mbPFRendered = false;
		} else if (mX == 105) {
			mbWSYNCActive = false;
		} else if (mX == 112) {
			// Check if we have hit the end of the mode line. We need to detect this now in case
			// abnormal DMA is active so that playfield DMA can be re-enabled for cycles 0-7
			// of the next scan line. In particular, this needs to be able to hit the display
			// list instruction fetch.
			uint32 rowStop = mbRowStopUseVScroll ? mLatchedVScroll : ((mRowCount - 1) & 15);
			if (mRowCounter == rowStop) {
				bool recomp = !mbPFDMAEnabled;

				mbPFDMAEnabled = true;

				if (recomp) {
					// HACK: We need to compute this pointer based on DMA cycles that didn't
					// occur...
					mpPFDataWrite = mPFDataBuffer + 48;

					UpdateDMAPattern();
				}
			}
		}

		if (mAnalysisMode) {
			switch(mAnalysisMode) {
			case kAnalyzeDMATiming:
				if (busActive)
					mActivityMap[mY][mX] |= 1;
				break;
			}
		}

		return busActive | (mDMAPattern[mX] & 0x3f);
	}
}

void ATAnticEmulator::AdvanceScanline() {
	SyncWithGTIA(0);
	mpGTIA->EndScanline(mDLControl, mbPFRendered);

	mX = 0;

	if (mbWSYNCActive)
		*mpConn->mpAnticBusData = mWSYNCHoldValue;

	if (++mY >= mScanlineLimit) {
		AdvanceFrame();
	} else if (mY >= 248) {
		mbDLActive = false;		// needed because The Empire Strikes Back has a 259-line display list (!)

		// Don't allow jumps to continue into vertical blank; needed because Spindizzy
		// wraps a dlist with jump instructions.
		mbDLExtraLoadsPending = false;

		// The DMA clock is unconditionally cleared during VBLANK, ending any abnormal DMA condition.
		mAbnormalDMAPattern = 0;
		
		if (mpConn)
			mpConn->AnticOnVBlank();
	} else {
		// Update abnormal DMA pattern.
		const int mode = mDLControl & 15;

		mEndingDMAPattern = 0;

		if (mode >= 2) {
			int dmaStart = mPFDMALatchedStart ? mPFDMALatchedStart : mPFDMAStart;
			int dmaVEnd = mPFDMALatchedVEnd ? mPFDMALatchedVEnd : mPFDMAVEnd;

			if (mode >= 8) {
				dmaStart -= 2;
				dmaVEnd -= 2;
			}

			const int fetchRate = kModeToFetchRate[mode];

			// Check for if we need to track the ending DMA pattern. This is true whenever a character name
			// fetch cycle would occur on cycle 107+ is potentially a situation where latent DMA clock bits
			// can be captured by a switch to a slower DMA rate. For simplicity we just use the last cycle
			// before the virtual end. We thus end up making a mask in mEndingDMAPattern as follows:
			//
			//  D7  D6  D5  D4  D3  D2  D1  D0
			// (0) (0) (0) 108 107 (0) (0) (0)
			//
			// This is then used by the IR mode switching code on cycle 1 to determine whether to activate
			// abnormal DMA. Note that IR modes 8 and 9 are the only modes that can exhibit this effect and
			// those modes cannot normally themselves cause DMA cycles late enough to trigger the problem.
			// IR modes 6, 7, A, B, and C use medium fetch rate and can trigger this bug via HSCROL 14-15;
			// modes 2, 3, 4, 5, D, E, and F use fast fetch rate and cause it for HSCROL 12-15.
			//

			if (mPFDMAVEnd > mPFDMAStart && fetchRate) {
				const int dangerOffset = dmaVEnd - (16 >> fetchRate) - 109;

				if (dangerOffset >= 0)
					mEndingDMAPattern = (kClockPattern[fetchRate][7] >> (4 - dangerOffset)) & 0x38;
			}

			// Update abnormal DMA pattern.
			const uint8 origPattern = mAbnormalDMAPattern;
			mAbnormalDMAPattern |= kClockPattern[fetchRate][dmaStart & 7];
			mAbnormalDMAPattern &= ~kClockPattern[fetchRate][dmaVEnd & 7];

			// Rotate the abnormal DMA pattern, due to 114 cycles not being divisible by 8.
			mAbnormalDMAPattern = (uint8)((mAbnormalDMAPattern << 6) + (mAbnormalDMAPattern >> 2));

			if (origPattern | mAbnormalDMAPattern) {

				// Grr... we're going to immediately have a new DMA cycle with a shifted abnormal DMA pattern,
				// so we need to recompute the DMA pattern now.
				UpdateDMAPattern();
			}
		}
	}

	mpConn->AnticEndScanline();

	mbPhantomPMDMA = (mDMACTL & 0x2C) == 0x20 && (uint32)(mY - 8) < 240;
	mbPhantomPMDMAActive = false;
	mbPhantomPlayerDMA = false;

	mpPFDataWrite = mPFDataBuffer;
	mpPFDataRead = mPFDataBuffer;
	mpPFCharFetchPtr = mPFCharBuffer;
	mPFDecodeOffset = 0;
	mPFDecodeCharOffset = 0;

	if (mAbnormalDMAPattern) {
		if (mpSimEventMgr)
			mpSimEventMgr->NotifyEvent(kATSimEvent_AbnormalDMA);
	}
}

void ATAnticEmulator::AdvanceFrame() {
	mY = 0;
	mbDLActive = false;		// necessary when DL DMA disabled for Joyride ptB

	mpConn->AnticEndFrame();

	// tell GTIA if the next field is an odd field
	mpGTIA->SetFieldPolarity(!mbInBuggedVBlank || mVSyncShiftTime < 20);

	if (mAnalysisMode)
		memset(mActivityMap, 0, sizeof mActivityMap);

	++mFrame;
	mFrameStart = ATSCHEDULER_GETTIME(mpScheduler);

	if (mpTraceChannelFrames) {
		const uint64 t64 = mpScheduler->GetTick64();

		mpTraceChannelFrames->AddTickEvent(t64, t64 + mScanlineLimit * 114, L"Frame", 0xFFFFFF);
	}

	if (mpTraceChannelDisplayList) {
		const uint64 frameStart64 = mpScheduler->GetTick64() - mScanlineLimit * 114;
		int y = 8;

		while(y < 248) {
			if (!mDLHistory[y].mbValid) {
				++y;
				continue;
			}

			const DLHistoryEntry& dle = mDLHistory[y++];
			const uint8 mode = dle.mControl & 15;

			if (mode < 2)
				continue;

			sint32 startY = y - 1;

			while(y < 248) {
				const DLHistoryEntry& dle2 = mDLHistory[y];

				if (dle2.mbValid && ((dle2.mControl ^ mode) & 15))
					break;

				++y;
			}

			static constexpr const wchar_t *kModeNames[14]={
				L"2",
				L"3",
				L"4",
				L"5",
				L"6",
				L"7",
				L"8",
				L"9",
				L"A",
				L"B",
				L"C",
				L"D",
				L"E",
				L"F",
			};

			mpTraceChannelDisplayList->AddTickEvent(frameStart64 + startY * 114, frameStart64 + y * 114, kModeNames[mode - 2], kATTraceColor_Default);
		}
	}
}

void ATAnticEmulator::SyncWithGTIA(int offset) {
	Decode(offset);

	int x = mX + offset + 1;

	if (mPFDisplayEnd <= mPFDisplayStart) {
		if (mPFDisplayCounter < x)
			mPFDisplayCounter = x;

		return;
	}

	if (x > (int)mPFDisplayEnd)
		x = (int)mPFDisplayEnd;

	int limit = x;
	int xoff2 = mPFDisplayCounter;

	if (xoff2 < (int)mPFDisplayStart)
		xoff2 = (int)mPFDisplayStart;

	if (xoff2 >= limit)
		return;

	if (mPFWidth == kPFDisabled) {
		xoff2 = limit;
	} else if (!mbPFDMAActive) {
		mbPFRendered = true;

		if (mPFHiresMode) {
			for(; xoff2 < limit; ++xoff2)
				mpGTIA->UpdatePlayfield320(xoff2*2, 0);
		} else {
			for(; xoff2 < limit; ++xoff2)
				mpGTIA->UpdatePlayfield160(xoff2, 0);
		}
	} else {
		mbPFRendered = true;

		const uint8 *src = &mPFDecodeBuffer[xoff2];
		if (mPFHiresMode) {
			if (mbHScrollDelay) {
				for(; xoff2 < limit; ++xoff2) {
					uint8 a = src[-1];
					uint8 b = src[0];
					++src;
					mpGTIA->UpdatePlayfield320(xoff2*2, ((a << 2) + (b >> 2)) & 15);
				}
			} else {
				mpGTIA->UpdatePlayfield320(xoff2*2, src, limit - xoff2);
				xoff2 = limit;
			}
		} else {
			if (mbHScrollDelay) {
				for(; xoff2 < limit; ++xoff2) {
					uint8 a = src[-1];
					uint8 b = src[0];
					++src;

					mpGTIA->UpdatePlayfield160(xoff2, (a << 4) + (b >> 4));
				}
			} else {
				mpGTIA->UpdatePlayfield160(xoff2, src, limit - xoff2);
				xoff2 = limit;
			}
		}
	}

	mPFDisplayCounter = xoff2;
}

void ATAnticEmulator::Decode(int offset) {
	int limit = (int)mX + offset + 0;

	if (!(mDLControl & 8))
		limit -= 2;

	if (limit > (int)mPFDMAVEnd)
		limit = (int)mPFDMAVEnd;

	static const uint8 kExpand160[16]={
		0x00, 0x01, 0x02, 0x04,
		0x10, 0x11, 0x12, 0x14,
		0x20, 0x21, 0x22, 0x24,
		0x40, 0x41, 0x42, 0x44,
	};

	static const uint8 kExpand160Alt[16]={
		0x00, 0x01, 0x02, 0x08,
		0x10, 0x11, 0x12, 0x18,
		0x20, 0x21, 0x22, 0x28,
		0x80, 0x81, 0x82, 0x88,
	};

	static const uint16 kExpandMode6[4][16]={
		{ 0x0000, 0x0100, 0x1000, 0x1100, 0x0001, 0x0101, 0x1001, 0x1101, 0x0010, 0x0110, 0x1010, 0x1110, 0x0011, 0x0111, 0x1011, 0x1111 },
		{ 0x0000, 0x0200, 0x2000, 0x2200, 0x0002, 0x0202, 0x2002, 0x2202, 0x0020, 0x0220, 0x2020, 0x2220, 0x0022, 0x0222, 0x2022, 0x2222 },
		{ 0x0000, 0x0400, 0x4000, 0x4400, 0x0004, 0x0404, 0x4004, 0x4404, 0x0040, 0x0440, 0x4040, 0x4440, 0x0044, 0x0444, 0x4044, 0x4444 },
		{ 0x0000, 0x0800, 0x8000, 0x8800, 0x0008, 0x0808, 0x8008, 0x8808, 0x0080, 0x0880, 0x8080, 0x8880, 0x0088, 0x0888, 0x8088, 0x8888 },
	};

	static const uint32 kExpandMode8[16]={
		0x00000000,	0x11110000,	0x22220000,	0x44440000,
		0x00001111,	0x11111111,	0x22221111,	0x44441111,
		0x00002222,	0x11112222,	0x22222222,	0x44442222,
		0x00004444,	0x11114444,	0x22224444,	0x44444444,
	};

	static const uint32 kExpandMode9[16]={
		0x00000000, 0x11000000, 0x00110000, 0x11110000,
		0x00001100, 0x11001100, 0x00111100, 0x11111100,
		0x00000011, 0x11000011, 0x00110011, 0x11110011,
		0x00001111, 0x11001111, 0x00111111, 0x11111111,
	};

	static const uint16 kExpandModeA[16]={
		0x0000,	0x1100,	0x2200,	0x4400,
		0x0011,	0x1111,	0x2211,	0x4411,
		0x0022,	0x1122,	0x2222,	0x4422,
		0x0044,	0x1144,	0x2244,	0x4444,
	};

	static const uint16 kExpandModeB[16]={
		0x0000, 0x0100, 0x1000, 0x1100, 0x0001, 0x0101, 0x1001, 0x1101, 0x0010, 0x0110, 0x1010, 0x1110, 0x0011, 0x0111, 0x1011, 0x1111,
	};

	static const uint8 kExpandModeAb8[4]={ 0x00, 0x11, 0x22, 0x44 };
	static const uint8 kExpandModeAbB[4]={ 0x00, 0x01, 0x10, 0x11 };

	static const uint8 kBits[16]={
		0x01,
		0x02,
		0x04,
		0x08,
		0x10,
		0x20,
		0x40,
		0x80,
		0x01,
		0x02,
		0x04,
		0x08,
		0x10,
		0x20,
		0x40,
		0x80,
	};

	int x = mPFDecodeCounter;
	const uint8 mode = mDLControl & 15;

	if (mAbnormalDMAPattern) {
		if (x < (int)mPFDMAStart) {
			if (mPFDMAEnd <= mPFDMAStart)
				return;

			int xlim = (int)mPFDMAStart > limit ? limit : (int)mPFDMAStart;
			switch(mode) {
				case 8:	// 40x4: shift two bits every four color clocks
				case 9:	// 80x2: shift one bit every two color clocks
					// shift two bits every two machine cycles (four color clocks)
					for(; x < xlim; ++x) {
						const int x2 = x + 6;
						const int bit = x2 & 7;

						if (mAbnormalDMAPattern & (0x55 << (x2 & 1)))
							mAbnormalDecodeShifter = (mAbnormalDecodeShifter << 2);

						if (mAbnormalDMAPattern & kBits[bit])
							mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

						if (x + 2 >= (int)mPFDMAStart ? mAbnormalDecodePattern & kBits[(x + 2 - mPFDMAStart) & 7] : mAbnormalDMAPattern & kBits[bit+2]) {
							if (x >= 5) {
								if (++mPFDecodeOffset >= 63)
									mPFDecodeOffset = 0;
							}
						}
					}
					break;

				case 6:		// 20x8: shift two bits every two color clocks
				case 7:		// 20x8: shift two bits every two color clocks
				case 10:	// 80x2: shift two bits every two color clocks
				case 11:	// 160x1: shift one bit every color clock
				case 12:	// 160x1: shift one bit every color clock
					// shift two bits every machine cycle
					for(; x < xlim; ++x) {
						const int x2 = x + 6;
						const int bit = x2 & 7;

						mAbnormalDecodeShifter <<= 2;

						if (mAbnormalDMAPattern & kBits[bit])
							mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

						if (x + 2 >= (int)mPFDMAStart ? mAbnormalDecodePattern & kBits[(x + 2 - mPFDMAStart) & 7] : mAbnormalDMAPattern & kBits[bit+2]) {
							if (x >= 5) {
								if (++mPFDecodeOffset >= 63)
									mPFDecodeOffset = 0;
							}
						}
					}
					break;

				case 2:		// 40x8: shift two bits every color clock
				case 3:		// 40x8: shift two bits every color clock
				case 4:		// 40x8: shift two bits every color clock
				case 5:		// 40x8: shift two bits every color clock
				case 13:	// 160x2: shift two bits every color clock
				case 14:	// 160x2: shift two bits every color clock
				case 15:	// 320x1: shift two bits every color clock
					// shift four bits every machine cycle (two bits every color clock)
					for(; x < xlim; ++x) {
						const int x2 = x + 6;
						const int bit = x2 & 7;

						mAbnormalDecodeShifter <<= 4;

						if (mAbnormalDMAPattern & kBits[bit])
							mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

						if (x + 2 >= (int)mPFDMAStart ? mAbnormalDecodePattern & kBits[(x + 2 - mPFDMAStart) & 7] : mAbnormalDMAPattern & kBits[bit+2]) {
							if (x >= 5) {
								if (++mPFDecodeOffset >= 63)
									mPFDecodeOffset = 0;
							}
						}
					}
					break;
			}

			if (xlim == mPFDMAStart)
				mPFDecodeOffset = (mPFDecodeOffset + 62) % 63;

			mPFDecodeCounter = xlim;
		}
	} else {
		if (x < (int)mPFDMAStart) {
			if (mPFDMAEnd <= mPFDMAStart)
				return;

			x = mPFDMAStart;
		}
	}

	if (x >= limit)
		return;

	int xoffset = x - mPFDMAStart;

	switch(mode) {
		case 2:
		case 3:
		case 4:
		case 5:
		case 13:
		case 14:
		case 15:
			xoffset >>= 1;
			break;

		case 6:
		case 7:
		case 10:
		case 11:
		case 12:
			xoffset >>= 2;
			break;

		case 8:
		case 9:
			xoffset >>= 3;
			break;
	}

	const uint8 *src = &mPFDataBuffer[xoffset];
	const uint8 *chdata = &mPFCharBuffer[xoffset];

	uint8 *dst = &mPFDecodeBuffer[x];

	// In text modes, data is fetched 3 clocks in advance.
	// In graphics modes, data is fetched 4 clocks in advance.
	if (mDLControl & 8)
		dst += 4;
	else
		dst += 6;

	if (!mAbnormalDecodePattern) {
		switch(mode) {
			case 2:		// 40 column text, 1.5 colors, 8 scanlines
				// scan lines 8 and 9 are blanked in mode 2 for $00-5F line in mode 3
				// (but they can stll be inverted afterward!)
				if ((mRowCounter & 14) == 8) {
					for(; x < limit; x += 2) {
						uint8 c = *src++;
						uint8 d = *chdata++;

						uint8 himask = (c & 128) ? 0xff : 0;
						uint8 inv = himask & mCharInvert;

						if ((c & 0x60) != 0x60)
							d = 0;

						d &= (~himask | mCharBlink);
						d ^= inv;
					
						dst[0] = d >> 4;
						dst[1] = d & 15;
						dst += 2;
					}
				} else {
#if VD_CPU_X86 || VD_CPU_X64
					x = ATAnticDecodeMode2_SSE2(dst, src, chdata, x, limit, mCharInvert, mCharBlink);
#else
					for(; x < limit; x += 2) {
						uint8 c = *src++;
						uint8 d = *chdata++;

						uint8 himask = (c & 128) ? 0xff : 0;
						uint8 inv = himask & mCharInvert;

						d &= (~himask | mCharBlink);
						d ^= inv;
					
						dst[0] = d >> 4;
						dst[1] = d & 15;
						dst += 2;
					}
#endif
				}
				break;

			case 3:		// 40 column text, 1.5 colors, 10 scanlines
				for(; x < limit; ++x) {
					uint8 c = *src++;
					uint8 d = *chdata++;

					uint8 himask = (c & 128) ? 0xff : 0;
					uint8 inv = himask & mCharInvert;
					uint8 mask = mRowCounter >= 2 ? 0xff : 0x00;

					if ((mRowCounter & 6) == 0) {
						if ((c & 0x60) != 0x60)
							mask ^= 0xff;
					}

					d &= (~himask | mCharBlink);

					d = inv ^ (mask & d); 
					
					dst[0] = d >> 4;
					dst[1] = d & 15;
					dst += 2;
				}
				break;

			case 4:		// 40 column text, 5 colors, 8 scanlines
			case 5:		// 40 column text, 5 colors, 16 scanlines
				for(; x < limit; x += 2) {
					uint8 c = *src++;
					uint8 d = *chdata++;

					if (c >= 128) {
						dst[1] = kExpand160Alt[d & 15];
						dst[0] = kExpand160Alt[d >> 4];
					} else {
						dst[1] = kExpand160[d & 15];
						dst[0] = kExpand160[d >> 4];
					}

					dst += 2;
				}
				break;

			case 6:		// 20 column text, 5 colors, 8 scanlines
			case 7:		// 20 column text, 5 colors, 16 scanlines
				for(; x < limit; x += 4) {
					uint8 c = *src++;
					uint8 d = *chdata++;

					const uint16 *tbl = kExpandMode6[c >> 6];
					*(uint16 *)(dst+0) = tbl[d >> 4];
					*(uint16 *)(dst+2) = tbl[d & 15];
					dst += 4;
				}
				break;

			case 8:
				for(; x < limit; x += 8) {
					uint8 c = *src++;

					*(uint32 *)(dst + 0) = kExpandMode8[c >> 4];
					*(uint32 *)(dst + 4) = kExpandMode8[c & 15];
					dst += 8;
				}
				break;

			case 9:
				for(; x < limit; x += 8) {
					uint8 c = *src++;

					*(uint32 *)(dst + 0) = kExpandMode9[c >> 4];
					*(uint32 *)(dst + 4) = kExpandMode9[c & 15];
					dst += 8;
				}
				break;

			case 10:
				for(; x < limit; x += 4) {
					uint8 c = *src++;

					*(uint16 *)(dst+0) = kExpandModeA[c >> 4];
					*(uint16 *)(dst+2) = kExpandModeA[c & 15];
					dst += 4;
				}
				break;

			case 11:
			case 12:
				for(; x < limit; x += 4) {
					uint8 c = *src++;

					*(uint16 *)(dst+0) = kExpandModeB[c >> 4];
					*(uint16 *)(dst+2) = kExpandModeB[c & 15];
					dst += 4;
				}
				break;

			case 13:
			case 14:
				for(; x < limit; x += 2) {
					uint8 c = *src++;

					dst[0] = kExpand160[c >> 4];
					dst[1] = kExpand160[c & 15];
					dst += 2;
				}
				break;

			case 15:
				for(; x < limit; x += 2) {
					uint8 c = *src++;

					dst[0] = c >> 4;
					dst[1] = c & 15;
					dst += 2;
				}
				break;
		}
	} else {
		uint8 decodePattern2 = mAbnormalDecodePattern & ~kClockPattern[kModeToFetchRate[mode]][0];

		switch(mode) {
			case 2:	{	// 40 column text, 1.5 colors, 8 scanlines
				uint8 globalMask = 0xFF;

				// scan lines 8 and 9 are always blanked in mode 2
				if ((mRowCounter & 14) == 8)
					globalMask = 0;

				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 4;

					if (mAbnormalDecodePattern & kBits[bit]) {
						uint8 c = 0xFF;

						if (mPFDecodeOffset < 48) {
							c = mPFDataBuffer[mPFDecodeOffset];
							mPFDecodeAbCharInv = (uint8)((sint8)c >> 7) & mCharInvert;
						}

						uint8 d = mPFCharBuffer[mPFDecodeCharOffset++];

						uint8 himask = (c & 128) ? 0xff : 0;
						uint8 inv = himask & mCharInvert;

						d &= (~himask | mCharBlink) & globalMask;

						// scan lines 8 and 9 are always blanked

						mAbnormalDecodeShifter |= d;
					}

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = (mAbnormalDecodeShifter ^ mPFDecodeAbCharInv) >> 4;
				}

				break;
			}

			case 3:		// 40 column text, 1.5 colors, 10 scanlines
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 4;

					if (mAbnormalDecodePattern & kBits[bit]) {
						uint8 c = 0xFF;

						if (mPFDecodeOffset < 48) {
							c = mPFDataBuffer[mPFDecodeOffset];
							mPFDecodeAbCharInv = (uint8)((sint8)c >> 7) & mCharInvert;
						}

						uint8 d = mPFCharBuffer[mPFDecodeCharOffset++];

						uint8 himask = (c & 128) ? 0xff : 0;
						uint8 mask = mRowCounter >= 2 ? 0xff : 0x00;

						if ((mRowCounter & 6) == 0) {
							if ((c & 0x60) != 0x60)
								mask ^= 0xff;
						}

						d &= (~himask | mCharBlink);
						d &= mask;

						mAbnormalDecodeShifter |= d;
					}

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = (mAbnormalDecodeShifter ^ mPFDecodeAbCharInv) >> 4;
				}
				break;

			case 4:		// 40 column text, 5 colors, 8 scanlines
			case 5:		// 40 column text, 5 colors, 16 scanlines
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 4;

					if (mAbnormalDecodePattern & kBits[bit]) {
						uint8 c = 0xFF;

						if (mPFDecodeOffset < 48) {
							c = mPFDataBuffer[mPFDecodeOffset];
							mPFDecodeAbCharInv = (uint8)((sint8)c >> 7) & mCharInvert;
						}

						uint8 d = mPFCharBuffer[mPFDecodeCharOffset++];

						mAbnormalDecodeShifter |= d;
					}

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = (mPFDecodeAbCharInv ? kExpand160Alt : kExpand160)[mAbnormalDecodeShifter >> 4];
				}
				break;

			case 6:		// 20 column text, 5 colors, 8 scanlines
			case 7:		// 20 column text, 5 colors, 16 scanlines
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 2;

					if (mAbnormalDecodePattern & kBits[bit]) {
						uint8 c = 0xFF;

						if (mPFDecodeOffset < 48) {
							c = mPFDataBuffer[mPFDecodeOffset];
							mPFDecodeAbCharInv = (uint8)(c >> 6);
						}

						uint8 d = mPFCharBuffer[mPFDecodeCharOffset++];

						mAbnormalDecodeShifter |= d;
					}

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = (uint8)kExpandMode6[mPFDecodeAbCharInv][mAbnormalDecodeShifter >> 4];
				}
				break;

			case 8:
				// Mode 8 shifts two bits every four color clocks, or every two machine cycles.
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					if (mAbnormalDecodePattern & (0x55 << (x2 & 1)))
						mAbnormalDecodeShifter = (mAbnormalDecodeShifter << 2);

					if (mAbnormalDecodePattern & kBits[bit]) {
						mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];
					}

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = kExpandModeAb8[mAbnormalDecodeShifter >> 6];
				}
				break;

			case 9:	// 80x2
				// Mode 9 shifts one bit every two color clocks, or one bit every machine cycle.
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter += mAbnormalDecodeShifter;

					if (mAbnormalDecodePattern & kBits[bit])
						mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = kExpandModeAb8[mAbnormalDecodeShifter >> 7];
				}
				break;

			case 10:	// 80x4
				// Mode A shifts two bits every two color clocks, or two bits every machine cycle.
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 2;

					if (mAbnormalDecodePattern & kBits[bit])
						mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = kExpandModeAb8[mAbnormalDecodeShifter >> 6];
				}
				break;

			case 11:	// 160x2
			case 12:
				// Modes B and C shift one bit every color clock, or two bits every machine cycle.
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 2;

					if (mAbnormalDecodePattern & kBits[bit])
						mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = kExpandModeAbB[mAbnormalDecodeShifter >> 6];
				}
				break;

			case 13:	// 160x4
			case 14:
				// Modes D and E shift two bits every color clock, or four bits every machine cycle.
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 4;

					if (mAbnormalDecodePattern & kBits[bit])
						mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = kExpand160[mAbnormalDecodeShifter >> 4];
				}
				break;

			case 15:	// 320x1.5
				for(; x < limit; ++x) {
					const int x2 = x - mPFDMAStart;
					const int bit = x2 & 7;

					mAbnormalDecodeShifter <<= 4;

					if (mAbnormalDecodePattern & kBits[bit])
						mAbnormalDecodeShifter |= (mPFDecodeOffset & 48) == 48 ? 0xFF : mPFDataBuffer[mPFDecodeOffset];

					if ((x + 2 >= (int)mPFDMAVEnd ? decodePattern2 : mAbnormalDecodePattern) & kBits[bit+2]) {
						if (++mPFDecodeOffset >= 63)
							mPFDecodeOffset = 0;
					}

					*dst++ = mAbnormalDecodeShifter >> 4;
				}
				break;
		}
	}

	mPFDecodeCounter = x;
}

uint8 ATAnticEmulator::ReadByte(uint8 reg) const {
	reg &= 0x0F;

	switch(reg) {
		case 0x0B: {
			// There is a one cycle delay between the time that VCOUNT increments and when
			// it is reset to zero for the beginning of the next frame. The incremented
			// cycle is seen on cycle 111; it is cleared on cycle 112 if it is wrong and
			// should be zero.
			uint32 ypos = mY;

			if (mX >= 111) {
				++ypos;

				if (mX >= 112 && ypos >= mScanlineLimit)
					ypos = 0;
			}

			return (uint8)(ypos >> 1);
		}

		case 0x0C:
			return mPENH;

		case 0x0D:
			return mPENV;

		case 0x0E:
			return 0xFF;		// needed or else Karateka breaks

		case 0x0F:
			return mNMIST;

		default:
//			__debugbreak();
			break;
	}

	return 0xFF;
}

void ATAnticEmulator::WriteByte(uint8 reg, uint8 value) {
	reg &= 0x0F;

	switch(reg) {
		case 0x00:	// DMACTL [D400]
			value &= 0x3F;

			if (value != mDMACTL) {
				// Ugh. We need to check whether we have crossed the current start or stop boundaries
				// and latch PF start/end as necessary. This reflects the fact that you can't change
				// the DMA start and stop boundaries once you already cross them.
				LatchPlayfieldEdges();

				if (mbInBuggedVBlank && (uint32)(mY-mVSyncStart) < 6) {
					if ((mDMACTL & 3) && !(value & 3))
						mVSyncShiftTime = mX;
				}

				SyncWithGTIA(0);

				// Check if we are shutting off missile DMA on cycle 113. This is too late. Note
				// that player DMA implicitly enables missile DMA, so we must check that too.
				if (mX == 113 && !(value & 0x0C) && (mDMACTL & 0x0C) && (uint32)(mY - 7) < 240)
					mbMissileDMADisabledLate = true;

				mDMACTL = value;

				switch(mDMACTL & 3) {
				case 0:
					mPFWidth = kPFDisabled;
					break;
				case 1:
					mPFWidth = kPFNarrow;
					break;
				case 2:
					mPFWidth = kPFNormal;
					break;
				case 3:
					mPFWidth = kPFWide;
					break;
				}

				UpdatePlayfieldTiming();
			}
			break;

		case 0x01:
			value &= 0x07;

			if (mCHACTL != value) {
				SyncWithGTIA(0);
				mCHACTL = value;
				mCharInvert = (mCHACTL & 0x02) ? 0xFF : 0x00;
				mCharBlink = (mCHACTL & 0x01) ? 0x00 : 0xFF;
				UpdateCurrentCharRow();
			}
			break;

		case 0x02:
			mDLIST = (mDLIST & 0xff00) + value;
			break;

		case 0x03:
			mDLIST = (mDLIST & 0xff) + (value << 8);
			break;

		case 0x04:
			value &= 15;

			// If we are changing the LSB, we are toggling the color clock delay on and off. This
			// takes place immediately. A change to upper bits, however, only shifts the playfield
			// stop and start locations, and doesn't take place immediately.
			if (mHSCROL != value) {
				if (mbHScrollEnabled) {
					uint8 delta = mHSCROL ^ value;

					if (delta & 1) {
						SyncWithGTIA(0);

						mbHScrollDelay = (value & 1) != 0;
					}

					// If there is a change to bits 1-3, then we are shifting the playfield timing.
					if (delta & 14) {
						LatchPlayfieldEdges();

						mPFHScrollDMAOffset = (value & 14) >> 1;

						UpdatePlayfieldTiming();
					}
				}

				mHSCROL = value;
			}
			break;

		case 0x05:
			mVSCROL = value & 15;

			// mLatchedVScroll captures the state of VSCROL at cycle 109 on
			// each scanline. It is used at cycles 112 and 1, and always
			// updated on 1.
			if (mX >= 1 && mX < 109)
				mLatchedVScroll = mVSCROL;
			break;

		case 0x07:
			mPMBASE = value & 0xFC;
			break;

		case 0x09:	// $D409 CHBASE
			QueueRegisterUpdate(2, reg, value);
			break;

		case 0x0A:	// $D40A WSYNC
			if (!mWSYNCPending || (mWSYNCPending == 1 && mX == 104)) {
				mWSYNCPending = 2;

				if (!mpEventWSYNC)
					mpScheduler->SetEvent(1, this, kATAnticEvent_WSYNC, mpEventWSYNC);

				mpConn->AnticForceNextCPUCycleSlow();
			}
			break;

		case 0x0E:
			mNMIEN = value & 0xC0;
			break;

		case 0x0F:	// NMIRES
			// Check if we have a pending NMI and are exactly at cycle 7. If so, we should NOT
			// reset the pending NMI, as it is set for both cycles.
			mNMIST = 0x1F;
			if (mX == 7 && mPendingNMIs)
				mNMIST |= mPendingNMIs;
			break;

		default:
//			__debugbreak();
			break;
	}
}

void ATAnticEmulator::DumpStatus() {
	ATConsolePrintf("DMACTL = %02x  : %s%s%s%s%s\n"
		, mDMACTL
		, (mDMACTL&3) == 0 ? "none"
		: (mDMACTL&3) == 1 ? "narrow"
		: (mDMACTL&3) == 2 ? "normal"
		: "wide"
		, mDMACTL & 0x04 ? " missiles" : ""
		, mDMACTL & 0x08 ? " players" : ""
		, mDMACTL & 0x10 ? " 1-line" : " 2-line"
		, mDMACTL & 0x20 ? " dlist" : ""
		);
	ATConsolePrintf("CHACTL = %02x  :%s%s%s\n"
		, mCHACTL
		, mCHACTL & 0x04 ? " reflect" : ""
		, mCHACTL & 0x02 ? " invert" : ""
		, mCHACTL & 0x01 ? " blank" : ""
		);
	ATConsolePrintf("DLIST  = %04x\n", mDLIST);
	ATConsolePrintf("HSCROL = %02x\n", mHSCROL);
	ATConsolePrintf("VSCROL = %02x\n", mVSCROL);
	ATConsolePrintf("PMBASE = %02x\n", mPMBASE);
 	ATConsolePrintf("CHBASE = %02x\n", mCHBASE);
	ATConsolePrintf("NMIEN  = %02x  :%s%s\n"
		, mNMIEN
		, mNMIEN & 0x80 ? " dli" : ""
		, mNMIEN & 0x40 ? " vbi" : ""
		);
	ATConsolePrintf("NMIST  = %02x  :%s%s%s\n"
		, mNMIST
		, mNMIST & 0x80 ? " dli" : ""
		, mNMIST & 0x40 ? " vbi" : ""
		, mNMIST & 0x20 ? " reset" : ""
		);
	ATConsolePrintf("PENH/V = %02x %02x\n", mPENH, mPENV);
}

void ATAnticEmulator::DumpDMALineBuffer() {
	VDStringA s;
	for(int i=0; i<48; i+=16) {
		s.sprintf("%02X:", i);

		for(int j=0; j<16; ++j) {
			s.append_sprintf(" %02X", mPFDataBuffer[i+j]);
		}

		s += '\n';

		ATConsoleWrite(s.c_str());
	}
}

void ATAnticEmulator::DumpDMAPattern() {
	char buf[116];
	buf[114] = '\n';
	buf[115] = 0;

	for(int i=0; i<114; ++i)
		buf[i] = (i >= 100) && !(i % 10) ? '1' : ' ';

	ATConsoleWrite(buf);

	for(int i=0; i<114; ++i)
		buf[i] = (i % 10) || (i < 10) ? ' ' : '0' + ((i / 10) % 10);

	ATConsoleWrite(buf);

	for(int i=0; i<114; ++i)
		buf[i] = '0' + (i % 10);

	ATConsoleWrite(buf);

	for(int i=0; i<114; ++i) {
		buf[i] = '.';

		uint8 dma = mDMAPattern[i];

		if (dma & 8) {
			switch(dma & 7) {
				case 3:
					buf[i] = 'f';
					break;

				case 5:
					buf[i] = 'c';
					break;

				default:
					buf[i] = '#';
					break;
			}
		} else if (dma & 16)
			buf[i] = 'V';
		else if (dma & 1) {
			switch(dma & 15) {
				case 1:
					buf[i] = 'R';
					break;
				case 3:
					buf[i] = 'F';
					break;
				case 5:
					buf[i] = 'C';
					break;
			}
		}
	}

	if (mDMACTL & 0x0C) {
		if (mDMAPattern[0] & 0x01)
			buf[0] = '#';
		else
			buf[0] = 'M';

		if (mDMACTL & 0x08) {
			for(int i=2; i<6; ++i) {
				if (mDMAPattern[i] & 1)
					buf[i] = '#';
				else
					buf[i] = 'P';
			}
		}
	}

	if (mDMACTL & 0x20) {
		if (mDMAPattern[1] & 0x01)
			buf[1] = '#';
		else
			buf[1] = 'D';
	}

	ATConsoleWrite(buf);
	ATConsolePrintf("%*c\n", mX+1, '^');
	ATConsoleWrite("Legend: (M)issile (P)layer (D)isplayList (R)efresh Play(F)ield (C)haracter (V)irtual (cf#)Abnormal\n");
	ATConsoleWrite("\n");
}

void ATAnticEmulator::DumpDMAActivityMap() {
	if (mAnalysisMode != kAnalyzeDMATiming) {
		ATConsoleWrite("ANTIC DMA timing analysis mode must be enabled to use the .dmamap command.\n");
		return;
	}

	VDStringA line;

	for(uint32 y=8; y<248; ++y) {
		const uint8 *actrow = mActivityMap[y];

		line.sprintf("%3u: ", y);

		int cycles = 0;

		for(uint32 x=0; x<114; ++x) {
			line += actrow[x] ? '*' : '.';

			if (actrow[x])
				++cycles;
		}

		line.append_sprintf(" | %3u:%-3u\n", cycles, 114-cycles);
		ATConsoleWrite(line.c_str());
	}
}

template<class T>
void ATAnticEmulator::ExchangeState(T& io) {
	// We don't save the frame here as it must monotonically increase.

	io != mX;
	io != mY;
	io != mScanlineLimit;
	io != mScanlineMax;

	io != mbDLExtraLoadsPending;
	io != mbDLActive;
	io != mbDLDMAEnabledInTime;
	io != mPFDisplayCounter;
	io != mPFDecodeCounter;
	io != mPFDecodeOffset;
	io != mPFDecodeCharOffset;
	io != mPFDMALastCheckX;
	io != mPFDecodeAbCharInv;
	io != mbPFDMAEnabled;
	io != mbPFDMAActive;
	io != mbWSYNCActive;
	io != mbWSYNCRelease;
	io != mbHScrollEnabled;
	io != mbHScrollDelay;
	io != mbRowStopUseVScroll;
	io != mbRowAdvance;
	io != mPendingNMIs;
	io != mEarlyNMIEN;
	io != mEarlyNMIEN2;
	io != mRowCounter;
	io != mRowCount;
	io != mLatchedVScroll;
	io != mLatchedVScroll2;

	io != mPFRowDMAPtrBase;
	io != mPFRowDMAPtrOffset;
	io != mPFPushCycleMask;
	io != mAbnormalDMAPattern;
	io != mEndingDMAPattern;
	io != mAbnormalDecodePattern;
	io != mAbnormalDecodeShifter;

	io != mPFCharFetchPtr;

	io != mPFWidthShift;
	io != mPFHScrollDMAOffset;

	io != mPFPushMode;

	io != mPFHiresMode;

	io != mPFWidth;
	io != mPFFetchWidth;

	io != mPFDisplayStart;
	io != mPFDisplayEnd;
	io != mPFDMAStart;
	io != mPFDMAVEnd;
	io != mPFDMAVEndWide;
	io != mPFDMAEnd;
	io != mPFDMALatchedStart;
	io != mPFDMALatchedVEnd;
	io != mPFDMALatchedEnd;

	io != mDLControlPrev;
	io != mDLControl;
	io != mDLNext;

	// DMA pattern is recomputed.

	io != mPFDataBuffer;
	io != mPFCharBuffer;

	io != mPFDecodeBuffer;

	io != mDLISTLatch;

	io != mWSYNCPending;
	io != mbPhantomPlayerDMA;
	io != mbMissileDMADisabledLate;
}

void ATAnticEmulator::BeginLoadState(ATSaveStateReader& reader) {
	mpPFDataRead = mPFDataBuffer;
	mpPFDataWrite = mPFDataBuffer;
	mpPFCharFetchPtr = mPFCharBuffer;
	mPFDecodeOffset = 0;
	mPFDecodeCharOffset = 0;

	reader.RegisterHandlerMethod(kATSaveStateSection_Arch, VDMAKEFOURCC('A', 'N', 'T', 'C'), this, &ATAnticEmulator::LoadStateArch);
	reader.RegisterHandlerMethod(kATSaveStateSection_Private, VDMAKEFOURCC('A', 'N', 'T', 'C'), this, &ATAnticEmulator::LoadStatePrivate);
	reader.RegisterHandlerMethod(kATSaveStateSection_End, 0, this, &ATAnticEmulator::EndLoadState);
}

void ATAnticEmulator::LoadStateArch(ATSaveStateReader& reader) {
	mDMACTL	= reader.ReadUint8() & 0x3F;
	mCHACTL	= reader.ReadUint8() & 0x07;
	mDLIST	= reader.ReadUint16();
	mHSCROL	= reader.ReadUint8() & 0x0F;
	mVSCROL	= reader.ReadUint8() & 0x0F;
	mPMBASE	= reader.ReadUint8() & 0xFC;
	mCHBASE	= reader.ReadUint8() & 0xFE;
	mNMIEN	= reader.ReadUint8() & 0xC0;
	mNMIST	= reader.ReadUint8() | 0x1F;
}

void ATAnticEmulator::LoadStatePrivate(ATSaveStateReader& reader) {
	ExchangeState(reader);

	if (mpRegisterUpdateEvent) {
		mpScheduler->RemoveEvent(mpRegisterUpdateEvent);
		mpRegisterUpdateEvent = NULL;
	}

	mRegisterUpdates.clear();
	mRegisterUpdateHeadIdx = 0;

	if (mWSYNCPending) {
		if (!mpEventWSYNC)
			mpEventWSYNC = mpScheduler->AddEvent(1, this, kATAnticEvent_WSYNC);
	} else {
		if (mpEventWSYNC) {
			mpScheduler->RemoveEvent(mpEventWSYNC);
			mpEventWSYNC = NULL;
		}
	}

	uint32 updateCount = reader.ReadUint32();
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	while(updateCount--) {
		QueuedRegisterUpdate ru;
		ru.mTime = t + reader.ReadUint32();
		ru.mReg = reader.ReadUint8();
		ru.mValue = reader.ReadUint8();
	}

	const uint8 dataReadOffset = reader.ReadUint8();
	const uint8 dataWriteOffset = reader.ReadUint8();
	const uint8 charWriteOffset = reader.ReadUint8();

	if (dataReadOffset > mX || dataWriteOffset > mX || charWriteOffset > mX)
		throw ATInvalidSaveStateException();

	mpPFDataRead = mPFDataBuffer + dataReadOffset;
	mpPFDataWrite = mPFDataBuffer + dataWriteOffset;
	mpPFCharFetchPtr = mPFCharBuffer + charWriteOffset;
}

void ATAnticEmulator::EndLoadState(ATSaveStateReader& reader) {
	// Bump the frame counter in case we went backwards in beam direction.
	++mFrame;
	mFrameStart = ATSCHEDULER_GETTIME(mpScheduler) - mX - mY*114;

	// Synchronize other state.
	switch(mDMACTL & 3) {
	case 0:
		mPFWidth = kPFDisabled;
		break;
	case 1:
		mPFWidth = kPFNarrow;
		break;
	case 2:
		mPFWidth = kPFNormal;
		break;
	case 3:
		mPFWidth = kPFWide;
		break;
	}

	mCharInvert = (mCHACTL & 0x02) ? 0xFF : 0x00;
	mCharBlink = (mCHACTL & 0x01) ? 0x00 : 0xFF;
	mCharBaseAddr128 = (uint32)(mCHBASE & 0xfc) << 8;
	mCharBaseAddr64 = (uint32)(mCHBASE & 0xfe) << 8;

	mPFDMAPatternCacheKey = 0xFFFFFFFF;

	UpdateCurrentCharRow();

	mpPFDataWrite = mPFDataBuffer;
	mpPFDataRead = mPFDataBuffer;
	mpPFCharFetchPtr = mPFCharBuffer;

	UpdateDMAPattern();

	ExecuteQueuedUpdates();
}

void ATAnticEmulator::BeginSaveState(ATSaveStateWriter& writer) {
	writer.RegisterHandlerMethod(kATSaveStateSection_Arch, this, &ATAnticEmulator::SaveStateArch);
	writer.RegisterHandlerMethod(kATSaveStateSection_Private, this, &ATAnticEmulator::SaveStatePrivate);
}

void ATAnticEmulator::SaveStateArch(ATSaveStateWriter& writer) {
	writer.BeginChunk(VDMAKEFOURCC('A', 'N', 'T', 'C'));
	writer.WriteUint8(mDMACTL);
	writer.WriteUint8(mCHACTL);
	writer.WriteUint16(mDLIST);
	writer.WriteUint8(mHSCROL);
	writer.WriteUint8(mVSCROL);
	writer.WriteUint8(mPMBASE);
	writer.WriteUint8(mCHBASE);
	writer.WriteUint8(mNMIEN);
	writer.WriteUint8(mNMIST);
	writer.EndChunk();
}

void ATAnticEmulator::SaveStatePrivate(ATSaveStateWriter& writer) {
	writer.BeginChunk(VDMAKEFOURCC('A', 'N', 'T', 'C'));
	ExchangeState(writer);

	uint32 i = mRegisterUpdateHeadIdx;
	uint32 n = (uint32)mRegisterUpdates.size();
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	writer.WriteUint32(n - i);
	for(; i<n; ++i) {
		const QueuedRegisterUpdate& ru = mRegisterUpdates[i];

		writer.WriteUint32(ru.mTime - t);
		writer.WriteUint8(ru.mReg);
		writer.WriteUint8(ru.mValue);
	}

	writer.WriteUint8((uint8)(mpPFDataRead - mPFDataBuffer));
	writer.WriteUint8((uint8)(mpPFDataWrite - mPFDataBuffer));
	writer.WriteUint8((uint8)(mpPFCharFetchPtr - mPFCharBuffer));

	writer.EndChunk();
}

void ATAnticEmulator::GetRegisterState(ATAnticRegisterState& state) const {
	state.mDMACTL	= mDMACTL;
	state.mCHACTL	= mCHACTL;
	state.mDLISTL	= (uint8)mDLIST;
	state.mDLISTH	= (uint8)(mDLIST >> 8);
	state.mHSCROL	= mHSCROL;
	state.mVSCROL	= mVSCROL;
	state.mPMBASE	= mPMBASE;
	state.mCHBASE	= mCHBASE;
	state.mNMIEN	= mNMIEN;
}

void ATAnticEmulator::SetTraceContext(ATTraceContext *context) {
	mpTraceContext = context;

	if (context) {
		ATTraceCollection *coll = mpTraceContext->mpCollection;

		const uint64 baseTime = mpTraceContext->mBaseTime;
		double invCycleRate = mpScheduler->GetRate().AsInverseDouble();

		mpTraceChannelFrames = coll->AddGroup(L"Frames", kATTraceGroupType_Frames)->AddSimpleChannel(baseTime, invCycleRate, L"Frames");
		mpTraceChannelDisplayList = coll->AddGroup(L"ANTIC")->AddSimpleChannel(baseTime, invCycleRate, L"DL");
	} else {
		mpTraceChannelDisplayList = nullptr;
		mpTraceChannelFrames = nullptr;
	}
}

inline void ATAnticSetDMACycles(void *dst0, uint32 start, uint32 end, uint8 cyclePattern, uint8 dmaMask) {
	if (end > 115)
		end = 115;

	if (end <= start)
		return;

#if VD_CPU_X86 || VD_CPU_X64
	ATAnticSetDMACycles_SSE2(dst0, start, end, cyclePattern, dmaMask);
	return;
#endif

	uint8 *VDRESTRICT dst = (uint8 *)dst0;

	for (uint32 i = start; i < end; ++i) {
		if (cyclePattern & (1 << (i & 7)))
			dst[i] |= dmaMask;
	}
}

inline void ATAnticSetRefreshCycles(uint8 *dmaPattern) {
#if VD_CPU_X86 || VD_CPU_X64
	ATAnticSetRefreshCycles_SSE2(dmaPattern);
#else
	// This is very simple. Refresh does 9 cycles every 4 starting at cycle 25.
	// If DMA is already occurring, the refresh occurs on the next available cycle.
	// If refresh hasn't gotten a chance to run by the time the next refresh cycle
	// occurs, it is simply dropped. The latest a refresh cycle will ever run is
	// 106, which happens on a wide 40 char badline.
	uint8 *VDRESTRICT dp = dmaPattern;
	int r = 24;
	for(int x=25; x<61; x += 4) {
		if (r >= x)
			continue;

		r = x;

		while(r < 107) {
			if (!(dp[r++] & 1)) {
				++dp[r-1];
				break;
			}
		}
	}
#endif
}

void ATAnticEmulator::UpdateDMAPattern() {
	int dmaStart = mPFDMAStart;
	int dmaVEnd = mPFDMAVEnd;
	uint8 mode = mDLControl & 15;
	uint32 key = (dmaStart << 16)
		+ (dmaVEnd << 8)
		+ mode
		+ (mbPFDMAActive ? 0x80 : 0x00)
		+ (mbPFDMAEnabled ? 0x40 : 0x00)
		+ (mPFWidth != kPFDisabled ? 0x20 : 0x00)
		+ (mAbnormalDMAPattern << 24);

	if (key != mPFDMAPatternCacheKey) {
		mPFDMAPatternCacheKey = key;

		uint8 textFetchMode = 0;
		uint8 graphicFetchMode = 0;
		switch(mode) {
			case 2:
			case 3:
			case 4:
			case 5:
				textFetchMode = 5;
				graphicFetchMode = 3;
				break;
			case 6:
			case 7:
				textFetchMode = 5;
				graphicFetchMode = 3;
				break;
			case 8:
			case 9:
				graphicFetchMode = 3;
				break;
			case 10:
			case 11:
			case 12:
				graphicFetchMode = 3;
				break;
			case 13:
			case 14:
			case 15:
				graphicFetchMode = 3;
				break;
		}

#if VD_CPU_X86 || VD_CPU_X64
		ATAnticClearDMACycles_SSE2(mDMAPattern);
#else
		memset(mDMAPattern, 0, sizeof(mDMAPattern));
#endif

		// Playfield DMA
		// =============
		//
		// The DMA clock is started and stopped on exactly one cycle, based on the current
		// playfield width and horizontal scroll settings. This clock can drive up to three
		// different kinds of cycles:
		//
		// - Character name fetch is the earliest DMA cycle that can be triggered off the
		//   clock.
		//
		// - Bitmap data fetches occur two cycles later than where the character name
		//   fetch would be.
		//
		// - Character data fetch occur three cycles later than the character name fetch.
		//
		// DMA pattern timing
		// ==================
		// Modes 2-5: Every 2 from 10/18/26.
		// Modes 6-7: Every 4 from 10/18/26.
		// Modes 8-9: Every 8 from 12/20/28.
		// Modes A-C: Every 4 from 12/20/28.
		// Modes D-F: Every 2 from 12/20/28.
		//
		// DMA is delayed by one clock for every 2 in HSCROL.
		//
		// If playfield DMA is disabled in the middle of the scanline, all cycles become
		// virtual.
		//
		// Playfield DMA is blocked from occurring in cycles 105-113. In this region, the
		// DMA requests will occur, loads will happen into line buffer RAM and both the memory
		// scan counter and line counter will increment, but no actual DMA cycles will occur.
		// This means that data on the bus will be loaded into the line buffer. It is normal
		// for these cycles to occur within HBLANK at wide fetch width and in fact they are
		// required to maintain the normal 12/24/48 byte pitch.
		//
		//
		// Abnormal DMA
		// ============
		// Usually, the start cycle injects a single bit into the clock, and the stop cycle
		// removes that bit. However, misaligning the stop and start with HSCROL will prevent
		// that from happening and cause what we call abnormal DMA, with extra bits flying
		// around the clock. We can both add bits by missing the stop, or subtract bits where
		// the errant bits line up with the current DMA pattern for the line.
		//
		// Note that the different types of cycles are not embedded within the DMA clock. The
		// DMA clock normally only has one bit flying around, and the various types of DMA
		// cycles are driven by the current mode and row.
		//
		// In Altirra, the current state of the DMA clock is only computed on a per-scanline
		// basis and it's the job of this function to compute the delta according to the
		// current playfield start and stop positions. This is then modified at the beginning
		// of the next scanline according to the next mode line, which can lead to a different
		// mode of entering abnormal DMA where latent bits in the clock are recaptured by a
		// slowly clock mode. That is handled elsewhere.

		if (!mbPFDMAEnabled)
			graphicFetchMode = 0;

		if (dmaStart >= dmaVEnd)
			textFetchMode = graphicFetchMode = 0;

		mAbnormalDecodePattern = 0;

		if (mAbnormalDMAPattern) {
			uint8 clock = mAbnormalDMAPattern;

			if (mode >= 8)
				clock = (uint8)((clock << 2) + (clock >> 6));

			int dmaStartOffset = dmaStart & 7;
			clock |= kClockPattern[kModeToFetchRate[mode]][dmaStartOffset];

			if (dmaStartOffset)
				clock = (clock >> dmaStartOffset) + (clock << (8 - dmaStartOffset));

			mAbnormalDecodePattern = clock;

			if (graphicFetchMode)
				graphicFetchMode |= 8;

			if (textFetchMode)
				textFetchMode |= 8;
		}

		if (mPFWidth == kPFDisabled) {
			if (graphicFetchMode) {
				graphicFetchMode |= 0x10;
				graphicFetchMode &= ~1;
			}

			if (textFetchMode) {
				textFetchMode |= 0x10;
				textFetchMode &= ~1;
			}
		}

		if (mbPFDMAActive) {
			// There are five phases for both sections below:
			//
			// 1) Write DMA cycles for the bits already flying around the DMA clock prior to
			//    playfield start.
			//
			// 2) Set bits in the DMA clock at playfield start.
			//
			// 3) Write DMA cycles from playfield start to playfield stop.
			//
			// 4) Clear bits in the DMA clock at playfield stop.
			//
			// 5) Write DMA cycles from playfield stop to the end of the scanline.
			//
			// Sections 1) and 5) only occur with abnormal DMA. Note that we must maintain
			// the clocks separately for the two paths due to the different timings. In
			// hardware the clock always stops and starts at the same times and the different
			// DMA requests are delayed; here we maintain separate delayed clocks instead.
			//
			if (graphicFetchMode) {
				uint8 clock = mAbnormalDMAPattern;				
				if (clock) {
					if (mode >= 8)
						clock = (uint8)((clock << 2) + (clock >> 6));

					graphicFetchMode |= 8;

					ATAnticSetDMACycles(mDMAPattern, 0, dmaStart, clock, graphicFetchMode);
				}

				clock |= kClockPattern[kModeToFetchRate[mode]][dmaStart & 7];
				
				ATAnticSetDMACycles(mDMAPattern, dmaStart, dmaVEnd, clock, graphicFetchMode);

				clock &= ~kClockPattern[kModeToFetchRate[mode]][dmaVEnd & 7];

				if (clock) {
					graphicFetchMode |= 8;

					ATAnticSetDMACycles(mDMAPattern, dmaVEnd, 115, clock, graphicFetchMode);
				}
			}

			// Character DMA
			//
			// Modes 2-5: Every 2 from 13/21/29.
			// Modes 6-7: Every 4 from 13/21/29.
			//
			// The character fetch always occurs 3 clocks after the playfield fetch.

			if (textFetchMode) {
				const int textFetchDMAVStart = mPFDMAStart + 3;
				const int textFetchDMAVEnd = mPFDMAVEnd + 3;

				uint8 clock = mAbnormalDMAPattern;
				clock = (uint8)((clock << 3) + (clock >> 5));

				if (clock) {
					textFetchMode |= 8;

					ATAnticSetDMACycles(mDMAPattern, 0, textFetchDMAVStart, clock, textFetchMode);
				}

				clock |= kClockPattern[kModeToFetchRate[mode]][textFetchDMAVStart & 7];

				ATAnticSetDMACycles(mDMAPattern, textFetchDMAVStart, textFetchDMAVEnd, clock, textFetchMode);

				clock &= ~kClockPattern[kModeToFetchRate[mode]][textFetchDMAVEnd & 7];

				if (clock) {
					textFetchMode |= 8;

					ATAnticSetDMACycles(mDMAPattern, textFetchDMAVEnd, 115, clock, textFetchMode);
				}
			}

			// Check for DMA cycles intruding into the horizontal blank region at the end of the
			// scanline (106-113); these must be turned into virtual cycles BEFORE we start
			// plopping down refresh cycles. Note that we do NOT do this for cycles 0-9 as
			// playfield DMA can actually happen there in abnormal cases and it conflicts with
			// special DMA cycles (!).
			for(int x=106; x<=114; ++x) {
				if (mDMAPattern[x])
					mDMAPattern[x] = (mDMAPattern[x] & 0xfe) | 0x10;
			}

			// Cycle 0 cannot be a DMA cycle, either.
			if (mDMAPattern[0])
				mDMAPattern[0] = (mDMAPattern[0] & 0xfe) | 0x10;
		}

		// Memory refresh
		ATAnticSetRefreshCycles(mDMAPattern);

		// Mark off special cycles.
		mDMAPattern[0] |= mDMAPattern[114];

		for(int i=1; i<7; ++i) {
			if (mDMAPattern[i] & 7) {
				mDMAPattern[i] |= 0x28;
				mDMAPattern[i] &= ~0x10;
			}
		}

		VDASSERT(!(mDMAPattern[0] & 1));

		mDMAPattern[  0] |= 0x80;	// Missile DMA
		mDMAPattern[  1] |= 0x80;	// Display list DMA
		mDMAPattern[  2] |= 0x80;	// Player DMA
		mDMAPattern[  3] |= 0x80;	// Player DMA
		mDMAPattern[  4] |= 0x80;	// Player DMA
		mDMAPattern[  5] |= 0x80;	// Player DMA
		mDMAPattern[  6] |= 0x80;	// Display list DMA
		mDMAPattern[  7] |= 0x80;	// Display list DMA
		mDMAPattern[  8] |= 0x80;	// NMI
		mDMAPattern[  9] |= 0x80;	// NMI
		mDMAPattern[ 10] |= 0x80;	// NMI
		mDMAPattern[ 16] |= 0x80;	// end HBLANK
		mDMAPattern[105] |= 0x80;	// WSYNC end
		mDMAPattern[112] |= 0x80;
		mDMAPattern[114] = 0x80;
	}

	if (mAnalysisMode == kAnalyzeDMATiming) {
		for(int i=mX; i<114; ++i)
			mActivityMap[mY][i] = (mDMAPattern[i] & 1);
	}
}


void ATAnticEmulator::LatchPlayfieldEdges() {
	if (mPFWidth) {
		uint32 cycleRange = mX - mPFDMALastCheckX;
		const int offset = (mDLControl & 15) < 8 ? 24-23 : 26-23;

		if ((uint32)((mPFDMAStart - offset) - mPFDMALastCheckX) <= cycleRange) {
			mPFDMALatchedStart = mPFDMAStart;
		}

		if ((uint32)((mPFDMAEnd - offset) - mPFDMALastCheckX) <= cycleRange) {
			mPFDMALatchedEnd = mPFDMAEnd;
		}

		if ((uint32)((mPFDMAVEnd - offset) - mPFDMALastCheckX) <= cycleRange) {
			mPFDMALatchedVEnd = mPFDMAVEnd;
		}
	}

	mPFDMALastCheckX = mX;
}

void ATAnticEmulator::UpdateCurrentCharRow() {
	mPFCharMask = 0x7f;

	switch(mDLControl & 15) {
	case 2:
	case 3:
	case 4:
		mPFCharFetchPtr = mCharBaseAddr128 + ((mCHACTL & 4 ? 7 : 0) ^ (mRowCounter & 7));
		break;
	case 5:
		mPFCharFetchPtr = mCharBaseAddr128 + ((mCHACTL & 4 ? 7 : 0) ^ (mRowCounter >> 1));
		break;
	case 6:
		mPFCharFetchPtr = mCharBaseAddr64 + ((mCHACTL & 4 ? 7 : 0) ^ (mRowCounter & 7));
		mPFCharMask = 0x3f;
		break;
	case 7:
		mPFCharFetchPtr = mCharBaseAddr64 + ((mCHACTL & 4 ? 7 : 0) ^ (mRowCounter >> 1));
		mPFCharMask = 0x3f;
		break;
	}
}

void ATAnticEmulator::UpdatePlayfieldTiming() {
	mPFFetchWidth = mPFWidth;
	if (mbHScrollEnabled && mPFFetchWidth != kPFDisabled && mPFFetchWidth != kPFWide)
		mPFFetchWidth = (PFWidthMode)((int)mPFFetchWidth + 1);

	bool pfActive = (uint32)(mX - mPFDisplayStart) < (uint32)(mPFDisplayEnd - mPFDisplayStart);

	switch(mPFWidth) {
		case kPFDisabled:
			mPFDisplayStart = 110;
			mPFDisplayEnd = 110;
			break;
		case kPFNarrow:
			mPFDisplayStart = 32;
			mPFDisplayEnd = 96;
			break;
		case kPFNormal:
			mPFDisplayStart = 24;
			mPFDisplayEnd = 104;
			break;
		case kPFWide:
			mPFDisplayStart = 22;
			mPFDisplayEnd = 112;
			break;
	}

	mPFDMAStart = 114;
	mPFDMAEnd = 114;
	mPFDMAVEnd = 114;

	uint8 mode = mDLControl & 15;

	if (mode >= 2) {
		switch(mPFFetchWidth) {
			case kPFDisabled:
				break;
			case kPFNarrow:
				mPFDMAStart = mode < 8 ? 26 : 28;
				mPFDMAEnd = mPFDMAStart + 64;
				break;
			case kPFNormal:
				mPFDMAStart = mode < 8 ? 18 : 20;
				mPFDMAEnd = mPFDMAStart + 80;
				break;
			case kPFWide:
				mPFDMAStart = mode < 8 ? 10 : 12;
				mPFDMAEnd = mPFDMAStart + 96;
				break;
		}

		mPFDMAVEndWide = mode < 8 ? 106 : 108;

		mPFDMAStart += mPFHScrollDMAOffset;
		mPFDMAEnd += mPFHScrollDMAOffset;
		mPFDMAVEnd = mPFDMAEnd;
		mPFDMAVEndWide += mPFHScrollDMAOffset;

		if (mPFDMALatchedStart)
			mPFDMAStart = mPFDMALatchedStart;

		if (mPFDMALatchedEnd)
			mPFDMAEnd = mPFDMALatchedEnd;

		if (mPFDMALatchedVEnd)
			mPFDMAVEnd = mPFDMALatchedVEnd;		// FIXME: THIS IS A NOP DUE TO BELOW

		// Timing in the plasma section of RayOfHope is very critical... it expects to
		// be able to change the DLI pointer between WSYNC and the next DLI.
		//
		// Update: According to Bennet's graph, DMA is not allowed to extend beyond clock
		// cycle 105. Playfield DMA is terminated past that point.
		//

		if (mPFDMAEnd > 106)
			mPFDMAEnd = 106;
	}

	// Check whether playfield DMA should be active. Playfield DMA should be active if it
	// is enabled and if we have already seen or will see the DMA start. Otherwise, either
	// playfield DMA is turned off or we have already missed the start point on the
	// scanline.

	// Note that this check must be <= because we can be hit on cycle 10.
	mbPFDMAActive = mPFDMALatchedStart || mX <= std::max<uint32>(10, mPFDMAStart - (mode < 8 ? 2 : 4));

	UpdateDMAPattern();
}

void ATAnticEmulator::OnScheduledEvent(uint32 id) {
	if (id == kATAnticEvent_UpdateRegisters) {
		mpRegisterUpdateEvent = NULL;

		ExecuteQueuedUpdates();
	} else if (id == kATAnticEvent_WSYNC) {
		mpEventWSYNC = NULL;

		if (!--mWSYNCPending) {
			// The 6502 doesn't respond to RDY for write cycles, so if the next CPU cycle is a write,
			// we cannot pull RDY yet.
			if (mpConn->AnticIsNextCPUCycleWrite())
				++mWSYNCPending;
			else {
				mbWSYNCActive = true;

				// We're relying on the fact that with the standard setup, the only cycles
				// that can follow the last write cycle is an instruction fetch -- the next
				// CPU cycle is always either the opcode fetch, the second insn byte fetch,
				// or a dummy insn fetch. This is, however, too early if the next cycle(s)
				// are DMA cycles. Hopefully this is not too bad of a transgression (doing
				// this properly requires implementing half memory cycles on the CPU core).
				mWSYNCHoldValue = mpConn->AnticGetCPUHeldCycleValue();

				if (mX < 113)
					*mpConn->mpAnticBusData = mWSYNCHoldValue;
			}
		}

		if (mWSYNCPending)
			mpEventWSYNC = mpScheduler->AddEvent(1, this, kATAnticEvent_WSYNC);
	}
}

void ATAnticEmulator::QueueRegisterUpdate(uint32 delay, uint8 reg, uint8 value) {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	uint32 i = mRegisterUpdateHeadIdx;
	uint32 n = (uint32)mRegisterUpdates.size();
	uint32 j = n;

	while(j > i) {
		const QueuedRegisterUpdate& ru = mRegisterUpdates[j - 1];

		if (ru.mTime - t <= delay)
			break;

		--j;
	}

	QueuedRegisterUpdate ru;
	ru.mTime = t + delay;
	ru.mReg = reg;
	ru.mValue = value;
	mRegisterUpdates.insert(mRegisterUpdates.begin() + j, ru);

	if (j == i) {
		if (mpRegisterUpdateEvent)
			mpScheduler->RemoveEvent(mpRegisterUpdateEvent);

		mpRegisterUpdateEvent = mpScheduler->AddEvent(delay, this, kATAnticEvent_UpdateRegisters);
	}
}

void ATAnticEmulator::ExecuteQueuedUpdates() {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	uint32 n = (uint32)mRegisterUpdates.size();

	while(mRegisterUpdateHeadIdx < n) {
		const QueuedRegisterUpdate& ru = mRegisterUpdates[mRegisterUpdateHeadIdx];

		if ((sint32)(ru.mTime - t) > 0)
			break;

		++mRegisterUpdateHeadIdx;

		const uint8 reg = ru.mReg;
		const uint8 value = ru.mValue;

		switch(reg) {
			case 0x09:		// [D409] CHBASE
				SyncWithGTIA(0);
				mCHBASE = value;
				mCharBaseAddr128 = (uint32)(mCHBASE & 0xfc) << 8;
				mCharBaseAddr64 = (uint32)(mCHBASE & 0xfe) << 8;
				UpdateCurrentCharRow();
				break;
		}
	}

	if (n > 32 && mRegisterUpdateHeadIdx > (n >> 1)) {
		mRegisterUpdates.erase(mRegisterUpdates.begin(), mRegisterUpdates.begin() + mRegisterUpdateHeadIdx);
		n -= mRegisterUpdateHeadIdx;
		mRegisterUpdateHeadIdx = 0;
	}

	if (mRegisterUpdateHeadIdx != n) {
		VDASSERT(!mpRegisterUpdateEvent);
		mpRegisterUpdateEvent = mpScheduler->AddEvent(mRegisterUpdates[mRegisterUpdateHeadIdx].mTime - t, this, kATAnticEvent_UpdateRegisters);
	}
}
