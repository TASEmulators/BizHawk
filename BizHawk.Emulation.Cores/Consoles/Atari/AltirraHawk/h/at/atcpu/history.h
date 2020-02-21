//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
//	Debugger module - target execution history interface
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

#ifndef f_AT_ATCPU_HISTORY_H
#define f_AT_ATCPU_HISTORY_H

#include <vd2/system/vdtypes.h>
#include <utility>

struct ATCPUHistoryEntry {
	uint32	mCycle;
	uint32	mUnhaltedCycle;
	uint32	mEA;
	union {
		uint8	mA;
		uint8	mZ80_A;
	};
	union {
		uint8	mX;
		uint8	mZ80_F;
		uint8	m8048_P1;
	};
	union {
		uint8	mY;
		uint8	mZ80_B;
		uint8	m8048_P2;
	};
	union {
		uint8	mS;
		uint8	mZ80_C;
	};
	uint16	mPC;
	union {
		uint8	mP;
		uint8	mZ80_D;
	};
	bool	mbIRQ : 1;			// Both are set -> HLE.
	bool	mbNMI : 1;
	bool	mbEmulation : 1;
	uint8	mSubCycle : 5;
	uint8	mOpcode[4];

	union {
		struct {
			union {
				uint8	mAH;
				uint8	mZ80_E;
				uint8	m8048_R0;
			};
			union {
				uint8	mXH;
				uint8	mZ80_H;
				uint8	m8048_R1;
			};
			union {
				uint8	mYH;
				uint8	mZ80_L;
			};

			uint8	mSH;
		} mExt;

		uint32 mGlobalPCBase;
	};

	uint8	mB;
	uint8	mK;

	union {
		uint16	mD;
		uint16	mZ80_SP;
	};
};

static_assert(sizeof(ATCPUHistoryEntry) == 32, "struct layout problem");

struct ATCPUBeamPosition {
	uint32	mFrame;
	uint32	mX;
	uint32	mY;
};

struct ATCPUTimestampDecoder {
	uint32 mFrameTimestampBase;
	uint32 mFrameCountBase;
	sint32 mCyclesPerFrame;

	uint32 GetFrameStartTime(uint32 timestamp) const {
		sint32 cycleDelta = (sint32)(timestamp - mFrameTimestampBase);

		if (cycleDelta < 0) {
			uint32 offset = (uint32)(-(cycleDelta % mCyclesPerFrame));

			return offset ? timestamp + offset - mCyclesPerFrame : timestamp;
		} else
			return timestamp - (uint32)(cycleDelta % mCyclesPerFrame);
	}

	ATCPUBeamPosition GetBeamPosition(uint32 timestamp) const {
		sint32 cycleDelta = (sint32)(timestamp - mFrameTimestampBase);
		sint32 frameCountDelta = cycleDelta / mCyclesPerFrame;
		sint32 frameCycleDelta = cycleDelta % mCyclesPerFrame;

		if (frameCycleDelta < 0) {
			--frameCountDelta;
			frameCycleDelta += mCyclesPerFrame;
		}

		return ATCPUBeamPosition { mFrameCountBase + (uint32)frameCountDelta, (uint32)(frameCycleDelta % 114), (uint32)(frameCycleDelta / 114) };
	}

	bool IsInterruptPositionVBI(uint32 timestamp) const {
		sint32 cycleDelta = (sint32)(timestamp - mFrameTimestampBase);
		sint32 frameCycleDelta = cycleDelta % mCyclesPerFrame;

		if (frameCycleDelta < 0)
			frameCycleDelta += mCyclesPerFrame;

		return frameCycleDelta >= 248*114;
	}
};

class IATCPUTimestampDecoderProvider {
public:
	virtual ATCPUTimestampDecoder GetTimestampDecoder() const = 0;
};

#endif
