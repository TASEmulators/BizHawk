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

#ifndef f_AT_RTCDS1305_H
#define f_AT_RTCDS1305_H

class ATConsoleOutput;

/// DS1305 real-time clock emulator.
class ATRTCDS1305Emulator {
public:
	ATRTCDS1305Emulator();

	void Init();

	void ColdReset();

	void Load(const uint8 data[0x72]);
	void Save(uint8 data[0x72]) const;

	bool ReadState() const;
	void WriteState(bool chipEnable, bool clock, bool data);

	void DumpStatus(ATConsoleOutput& output);

protected:
	void ReadRegister();
	void WriteRegister();
	void IncrementAddressRegister();
	void UpdateClock();

	uint8	mPhase;
	uint8	mAddress;
	uint8	mValue;
	bool	mbState;
	bool	mbChipEnable;
	bool	mbSPIInitialClock;
	bool	mbSPIClock;
	uint8	mClockRAM[0x12];
	uint8	mUserRAM[0x60];
};

#endif
