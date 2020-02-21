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

#ifndef f_AT_EEPROM_H
#define f_AT_EEPROM_H

class ATConsoleOutput;

class ATEEPROMEmulator {
	ATEEPROMEmulator(const ATEEPROMEmulator&) = delete;
	ATEEPROMEmulator& operator=(const ATEEPROMEmulator&) = delete;
public:
	ATEEPROMEmulator();

	void Init();

	void ColdReset();

	void Load(const uint8 data[0x100]);
	void Save(uint8 data[0x100]) const;

	bool ReadState() const;
	void WriteState(bool chipEnable, bool clock, bool data);

	void DumpStatus(ATConsoleOutput&);

protected:
	void ReadRegister();
	void WriteRegister();
	void OnNextByte();

	uint8	mPhase;
	uint8	mAddress;
	uint8	mStatus;
	uint8	mValueIn;
	uint8	mValueOut;

	enum CommandState {
		kCommandState_Initial,
		kCommandState_CommandCompleted,
		kCommandState_WriteStatus,
		kCommandState_WriteMemoryAddress,
		kCommandState_WriteMemoryNext,
		kCommandState_ReadMemoryAddress,
		kCommandState_ReadMemoryNext
	} mCommandState;

	bool	mbState;
	bool	mbChipEnable;
	bool	mbSPIClock;
	uint8	mMemory[0x100];
};

#endif	// f_AT_EEPROM_H
