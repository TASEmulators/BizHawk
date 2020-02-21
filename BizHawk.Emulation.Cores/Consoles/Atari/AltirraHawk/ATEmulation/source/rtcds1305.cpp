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
#include <at/atcore/consoleoutput.h>
#include <at/atcore/logging.h>
#include <at/atemulation/rtcds1305.h>

ATLogChannel g_ATLCDS1305CRead(false, false, "DS1305CR", "DS1305 real time clock reads");
ATLogChannel g_ATLCDS1305URead(false, false, "DS1305UR", "DS1305 real time clock user reads");
ATLogChannel g_ATLCDS1305Write(false, false, "DS1305W", "DS1305 real time clock writes");

namespace {
	uint8 ToBCD(uint8 v) {
		return ((v / 10) << 4) + (v % 10);
	}
}

ATRTCDS1305Emulator::ATRTCDS1305Emulator() {
}

void ATRTCDS1305Emulator::Init() {
	memset(mClockRAM, 0, sizeof mClockRAM);
	memset(mUserRAM, 0, sizeof mUserRAM);

	ColdReset();
}

void ATRTCDS1305Emulator::ColdReset() {
	mPhase = 0;
	mAddress = 0;
	mbState = true;
	mbChipEnable = false;
	mbSPIClock = false;
}

void ATRTCDS1305Emulator::Load(const uint8 data[0x72]) {
	memcpy(mClockRAM, data, 0x12);
	memcpy(mUserRAM, data + 0x12, 0x60);
}

void ATRTCDS1305Emulator::Save(uint8 data[0x72]) const {
	memcpy(data, mClockRAM, 0x12);
	memcpy(data + 0x12, mUserRAM, 0x60);
}

bool ATRTCDS1305Emulator::ReadState() const {
	return mbState;
}

void ATRTCDS1305Emulator::WriteState(bool chipEnable, bool clock, bool data) {
	if (mbChipEnable != chipEnable) {
		mbChipEnable = chipEnable;

		if (!chipEnable) {
			mPhase = 0;
			mbState = true;
		} else {
			mbSPIClock = clock;
			mbSPIInitialClock = clock;
		}
	}

	if (chipEnable && mbSPIClock != clock) {
		mbSPIClock = clock;

		if (clock != mbSPIInitialClock) {	// read (0 -> 1 clock transition)
			if (mPhase >= 8 && mAddress < 0x80) {
				mbState = (mValue >= 0x80);
				mValue += mValue;

				if (++mPhase == 16) {
					mPhase = 8;

					ReadRegister();
					IncrementAddressRegister();
				}
			}
		} else {		// write (1 -> 0 clock transition)
			if (mPhase < 8) {
				// shifting in address, MSB first
				mAddress += mAddress;
				if (data)
					++mAddress;

				if (++mPhase == 8) {
					if (mAddress < 0x80) {
						if (mAddress < 0x20)
							UpdateClock();

						ReadRegister();
						IncrementAddressRegister();
					}
				}
			} else if (mAddress >= 0x80) {
				// shifting in data, MSB first
				mValue += mValue;
				if (data)
					++mValue;

				// check if we're done with a byte
				if (++mPhase == 16) {
					mPhase = 8;

					WriteRegister();
					IncrementAddressRegister();
				}
			}
		}
	}
}

void ATRTCDS1305Emulator::DumpStatus(ATConsoleOutput& output) {
	output <<= "DS1305 status:";
	output("  Output state:     %d", mbState);
	output("  Current register: $%02x (%s)", mAddress & 0x7f, mAddress & 0x80 ? "write" : "read");
	output("  Phase:            %s bit %u", mPhase & 8 ? "data" : "address", ~mPhase & 7);
	output <<= "";
	output("Clock RAM:");
	output("00: %02X %02X %02X %02X %02X %02X %02X %02X-%02X %02X %02X %02X %02X %02X %02X %02X"
		, mClockRAM[0]
		, mClockRAM[1]
		, mClockRAM[2]
		, mClockRAM[3]
		, mClockRAM[4]
		, mClockRAM[5]
		, mClockRAM[6]
		, mClockRAM[7]
		, mClockRAM[8]
		, mClockRAM[9]
		, mClockRAM[10]
		, mClockRAM[11]
		, mClockRAM[12]
		, mClockRAM[13]
		, mClockRAM[14]
		, mClockRAM[15]
		);
	output("10: %02X %02X"
		, mClockRAM[16]
		, mClockRAM[17]
		);

	output <<= "";
	output <<= "User NVRAM:";

	for(int offset = 0; offset < 0x60; offset += 0x10) {
		output("%02X: %02X %02X %02X %02X %02X %02X %02X %02X-%02X %02X %02X %02X %02X %02X %02X %02X"
			, offset + 0x20
			, mUserRAM[offset + 0]
			, mUserRAM[offset + 1]
			, mUserRAM[offset + 2]
			, mUserRAM[offset + 3]
			, mUserRAM[offset + 4]
			, mUserRAM[offset + 5]
			, mUserRAM[offset + 6]
			, mUserRAM[offset + 7]
			, mUserRAM[offset + 8]
			, mUserRAM[offset + 9]
			, mUserRAM[offset + 10]
			, mUserRAM[offset + 11]
			, mUserRAM[offset + 12]
			, mUserRAM[offset + 13]
			, mUserRAM[offset + 14]
			, mUserRAM[offset + 15]
			);
	}
}

void ATRTCDS1305Emulator::ReadRegister() {
	if (mAddress < 0x12)
		mValue = mClockRAM[mAddress];
	else if (mAddress < 0x20)
		mValue = 0;
	else if (mAddress < 0x80)
		mValue = mUserRAM[mAddress - 0x20];
	else
		mValue = 0xFF;

	if (mAddress < 0x20)
		g_ATLCDS1305CRead("Read[$%02X] = $%02X\n", mAddress, mValue);
	else
		g_ATLCDS1305URead("Read[$%02X] = $%02X\n", mAddress, mValue);
}

void ATRTCDS1305Emulator::WriteRegister() {
	g_ATLCDS1305Write("Write[$%02X] = $%02X\n", mAddress, mValue);

	if (mAddress < 0x92) {
		static const uint8 kRegisterWriteMasks[0x12]={
			0x7F, 0x7F, 0x7F, 0x0F, 0x3F, 0x3F, 0xFF, 0xFF,
			0xFF, 0xFF, 0x8F, 0xFF, 0xFF, 0xFF, 0x8F, 0x87,
			0x00, 0xFF
		};

		mClockRAM[mAddress - 0x80] = mValue & kRegisterWriteMasks[mAddress - 0x80];
	} else if (mAddress >= 0xA0)
		mUserRAM[mAddress - 0xA0] = mValue;
}

void ATRTCDS1305Emulator::IncrementAddressRegister() {
	++mAddress;

	uint8 addr7 = mAddress & 0x7F;
	if (addr7 == 0x20)
		mAddress -= 0x20;
	else if (addr7 == 0)
		mAddress -= 0x60;
}

void ATRTCDS1305Emulator::UpdateClock() {
	// copy clock to RAM
	time_t t;

	time(&t);
	const tm *p = localtime(&t);

	mClockRAM[0] = ToBCD(p->tm_sec);
	mClockRAM[1] = ToBCD(p->tm_min);

	if (mClockRAM[2] & 0x40) {
		// 12 hour mode
		uint8 hr = p->tm_hour;

		if (hr >= 12) {
			// PM
			mClockRAM[2] = ToBCD(hr - 11) + 0x20;
		} else {
			// AM
			mClockRAM[2] = ToBCD(hr + 1);
		}
	} else {
		// 24 hour mode
		mClockRAM[2] = ToBCD(p->tm_hour);
	}

	mClockRAM[3] = p->tm_wday + 1;
	mClockRAM[4] = ToBCD(p->tm_mday);
	mClockRAM[5] = ToBCD(p->tm_mon + 1);
	mClockRAM[6] = ToBCD(p->tm_year % 100);
	mClockRAM[7] = ToBCD(p->tm_mday);
}
