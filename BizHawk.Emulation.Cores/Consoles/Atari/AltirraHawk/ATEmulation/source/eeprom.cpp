//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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
#include <at/atemulation/eeprom.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/logging.h>

ATLogChannel g_ATLCEEPROMRead(false, false, "EEPROMR", "EEPROM reads");
ATLogChannel g_ATLCEEPROMWrite(false, false, "EEPROMW", "EEPROM writes");
ATLogChannel g_ATLCEEPROM(false, false, "EEPROM", "EEPROM commands");

ATEEPROMEmulator::ATEEPROMEmulator() {
}

void ATEEPROMEmulator::Init() {
	memset(mMemory, 0, sizeof mMemory);

	ColdReset();
}

void ATEEPROMEmulator::ColdReset() {
	mPhase = 0;
	mAddress = 0;
	mStatus = 0;
	mValueIn = 0xFF;
	mValueOut = 0xFF;
	mCommandState = kCommandState_Initial;

	mbState = true;
	mbChipEnable = false;
	mbSPIClock = false;
}

void ATEEPROMEmulator::Load(const uint8 data[0x100]) {
	memcpy(mMemory, data, 0x100);
}

void ATEEPROMEmulator::Save(uint8 data[0x100]) const {
	memcpy(data, mMemory, 0x100);
}

bool ATEEPROMEmulator::ReadState() const {
	return mbState;
}

void ATEEPROMEmulator::WriteState(bool chipEnable, bool clock, bool data) {
	if (mbChipEnable && mbSPIClock != clock) {
		mbSPIClock = clock;

		// Data in is sampled on a rising (0 -> 1) clock transition.
		// Data out is changed on a falling (1 -> 0) clock transition.

		if (clock) {	// read (0 -> 1 clock transition)
			// shift in next bit
			mValueIn += mValueIn;
			if (data)
				++mValueIn;
		} else {		// write (1 -> 0 clock transition)
			// increment phase
			if (++mPhase >= 8) {
				mPhase = 0;

				OnNextByte();
			}

			// shift out next bit
			mbState = (mValueOut >= 0x80);
			mValueOut += mValueOut;
			++mValueOut;
		}
	}

	// The!Cart menu drops CS as the same time as SCK, so we can't reset
	// everything until we've processed the clock transition above.
	if (mbChipEnable != chipEnable) {
		mbChipEnable = chipEnable;

		if (!chipEnable) {
			mCommandState = kCommandState_Initial;
			mPhase = 0;
			mbState = true;
			mbSPIClock = true;
		}
	}
}

void ATEEPROMEmulator::DumpStatus(ATConsoleOutput& output) {
	output <<= "25AA02A status:";
	output("  Output state:     %d", mbState);
	output("  Current register: $%02x (%s)", mAddress & 0x7f, mAddress & 0x80 ? "write" : "read");
	output("  Phase:            %s bit %u", mPhase & 8 ? "data" : "address", ~mPhase & 7);
	output <<= "";
	output <<= "NVRAM:";

	for(int offset = 0; offset < 0x100; offset += 0x10) {
		output("%02X: %02X %02X %02X %02X %02X %02X %02X %02X-%02X %02X %02X %02X %02X %02X %02X %02X"
			, offset + 0x20
			, mMemory[offset + 0]
			, mMemory[offset + 1]
			, mMemory[offset + 2]
			, mMemory[offset + 3]
			, mMemory[offset + 4]
			, mMemory[offset + 5]
			, mMemory[offset + 6]
			, mMemory[offset + 7]
			, mMemory[offset + 8]
			, mMemory[offset + 9]
			, mMemory[offset + 10]
			, mMemory[offset + 11]
			, mMemory[offset + 12]
			, mMemory[offset + 13]
			, mMemory[offset + 14]
			, mMemory[offset + 15]
			);
	}
}

void ATEEPROMEmulator::OnNextByte() {
	switch(mCommandState) {
		case kCommandState_Initial:
			g_ATLCEEPROM("Received command $%02X\n", mValueIn);
			switch(mValueIn & 0xF7) {
				case 0x01:	// write status register
					mCommandState = kCommandState_WriteStatus;
					break;

				case 0x02:	// write memory
					mCommandState = kCommandState_WriteMemoryAddress;
					break;

				case 0x03:	// read memory
					mCommandState = kCommandState_ReadMemoryAddress;
					break;

				case 0x04:	// reset write enable latch (disable writes)
					mStatus &= ~0x02;
					mCommandState = kCommandState_CommandCompleted;
					break;

				case 0x05:	// read status register
					mValueOut = mStatus;
					mCommandState = kCommandState_CommandCompleted;
					break;

				case 0x06:	// set write enable latch (enable writes)
					mStatus |= 0x02;
					mCommandState = kCommandState_CommandCompleted;
					break;

				default:
					// ignore command
					break;
			}
			break;

		case kCommandState_CommandCompleted:
			// nothing to do
			break;

		case kCommandState_WriteStatus:
			// change write protection bits
			mStatus = (mStatus & 0x03) + (mValueIn & 0x0c);
			mCommandState = kCommandState_CommandCompleted;
			break;

		case kCommandState_WriteMemoryAddress:
			mAddress = mValueIn;
			mCommandState = kCommandState_WriteMemoryNext;
			break;

		case kCommandState_WriteMemoryNext:
			WriteRegister();
			break;

		case kCommandState_ReadMemoryAddress:
			mAddress = mValueIn;
			mCommandState = kCommandState_ReadMemoryNext;
			// fall through to read first memory location
		case kCommandState_ReadMemoryNext:
			// read commands wrap through entire 256-byte address space
			ReadRegister();
			break;
	}
}

void ATEEPROMEmulator::ReadRegister() {
	mValueOut = mMemory[mAddress];
	g_ATLCEEPROMRead("Read[$%02X] = $%02X\n", mAddress, mValueOut);

	++mAddress;
}

void ATEEPROMEmulator::WriteRegister() {
	// check if this write is allowed by current write protection state
	bool writeAllowed = false;

	switch(mStatus & 0x0C) {
		case 0x00:	// all writes allowed
			writeAllowed = true;
			break;

		case 0x04:	// $C0-FF protected
			if (mAddress < 0xC0)
				writeAllowed = true;
			break;

		case 0x08:	// $80-FF protected
			if (mAddress < 0x80)
				writeAllowed = true;
			break;

		case 0x0C:	// all writes blocked
			writeAllowed = true;
			break;
	}

	if (writeAllowed) {
		g_ATLCEEPROMWrite("Write[$%02X] = $%02X\n", mAddress, mValueIn);
		mMemory[mAddress] = mValueIn;
	} else {
		g_ATLCEEPROMWrite("Write[$%02X] ! $%02X (write blocked by protection)\n", mAddress, mValueIn);
	}

	// write commands only wrap within the current 16-byte page
	mAddress = ((mAddress + 1) & 0x0F) + (mAddress & 0xF0);
}
