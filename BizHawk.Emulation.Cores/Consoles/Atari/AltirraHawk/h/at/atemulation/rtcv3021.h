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

#ifndef f_AT_RTCV3021_H
#define f_AT_RTCV3021_H

/// V3021 real-time clock emulator.
class ATRTCV3021Emulator {
public:
	struct NVState {
		uint8 mData[10];
	};

	ATRTCV3021Emulator();

	void Init();

	// The V3021 has an annoyance where you must stomp all of NVRAM in order
	// to read the clock. If you are using the NVRAM to store user data, this
	// introduces a race where if you are interrupted while reading the clock,
	// the user contents are lost. Since the emulator may be reset or shut down
	// at inopportune times, we shadow the user RAM contents on a clock read
	// so they can be preserved even if the 6502 code hasn't gotten a chance
	// to put the user contents back. To be precise, Restore() copies back any
	// user RAM bytes that were overwritten by a clock read and not re-written
	// back since. If Restore() is not used, the RTC emulation operates per
	// spec.
	void Restore();

	void Load(const NVState& state);
	void Save(NVState& state, bool useShadow) const;

	bool DebugReadBit() const;
	bool ReadBit();
	void WriteBit(bool bit);

protected:
	void CopyRAMToClock();
	void CopyClockToRAM();

	uint8	mPhase;
	uint8	mAddress;
	uint8	mValue;
	uint8	mRAM[16];
	uint8	mShadow[10];
};

#endif
