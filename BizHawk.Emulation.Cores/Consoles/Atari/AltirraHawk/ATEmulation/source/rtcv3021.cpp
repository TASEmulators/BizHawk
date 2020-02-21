//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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
#include <time.h>
#include <at/atcore/logging.h>
#include <at/atemulation/rtcv3021.h>

ATLogChannel g_ATLCV3021(false, false, "V3021", "V3021 real time clock accesses");

namespace {
	uint8 ToBCD(uint8 v) {
		return ((v / 10) << 4) + (v % 10);
	}
}

ATRTCV3021Emulator::ATRTCV3021Emulator() {
}

void ATRTCV3021Emulator::Init() {
	memset(mRAM, 0xFF, sizeof mRAM);
	memset(mShadow, 0xFF, sizeof mShadow);
	mPhase = 0;
	mAddress = 0;
}

void ATRTCV3021Emulator::Restore() {
	memcpy(mRAM, mShadow, 10);
}

void ATRTCV3021Emulator::Load(const NVState& state) {
	memcpy(mRAM, state.mData, 10);
	memcpy(mShadow, state.mData, 10);
}

void ATRTCV3021Emulator::Save(NVState& state, bool useShadow) const {
	memcpy(state.mData, useShadow ? mShadow : mRAM, 10);
}

bool ATRTCV3021Emulator::DebugReadBit() const {
	if (mPhase < 4) {
		return false;
	} else {
		if (mPhase == 4)
			return (mRAM[mAddress] & 1) != 0;
		else
			return (mValue & 1) != 0;
	}
}

bool ATRTCV3021Emulator::ReadBit() {
	if (mPhase < 4) {
		mPhase = 0;
		return false;
	} else {
		if (mPhase == 4)
			mValue = mRAM[mAddress];

		if (++mPhase >= 12)
			mPhase = 0;

		bool bit = (mValue & 1) != 0;
		mValue >>= 1;
		return bit;
	}
}

void ATRTCV3021Emulator::WriteBit(bool bit) {
	if (mPhase < 4) {
		mAddress >>= 1;

		if (bit)
			mAddress += 0x08;

		++mPhase;

		if (mPhase == 4 && mAddress >= 0x0E) {
			if (mAddress == 0x0E) {
				g_ATLCV3021("RAM -> Clock\n");
				CopyRAMToClock();
			} else {
				g_ATLCV3021("Clock -> RAM\n");
				CopyClockToRAM();
			}

			mPhase = 0;
		}
	} else {
		mValue >>= 1;

		if (bit)
			mValue += 0x80;

		if (++mPhase == 12) {
			g_ATLCV3021("RAM[$%x] = $%02X | %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n", mAddress, mValue
				, mRAM[0]
				, mRAM[1]
				, mRAM[2]
				, mRAM[3]
				, mRAM[4]
				, mRAM[5]
				, mRAM[6]
				, mRAM[7]
				, mRAM[8]
				, mRAM[9]
				);

			if (mAddress < 10) {
				mRAM[mAddress] = mValue;
				mShadow[mAddress] = mValue;
			}

			mAddress = 0;
			mPhase = 0;
		}
	}
}

void ATRTCV3021Emulator::CopyRAMToClock() {
	// check time set lock bit
	if (mRAM[0] & 0x10)
		return;

	mRAM[1] = 0;
}

void ATRTCV3021Emulator::CopyClockToRAM() {
	// copy clock to RAM
	time_t t;

	time(&t);
	const tm *p = localtime(&t);

	uint8 clock[8];
	clock[0] = ToBCD(p->tm_sec);
	clock[1] = ToBCD(p->tm_min);
	clock[2] = ToBCD(p->tm_hour);
	clock[3] = ToBCD(p->tm_mday);
	clock[4] = ToBCD(p->tm_mon + 1);
	clock[5] = ToBCD(p->tm_year % 100);
	clock[6] = p->tm_wday;
	clock[7] = ToBCD(p->tm_mday);

	mRAM[0] = 0;
	mRAM[1] = 0;

	for(int i=0; i<8; ++i) {
		if (mRAM[i + 2] != clock[i]) {
			mRAM[i + 2] = clock[i];
			mRAM[1] |= (1 << i);
		}
	}
}
