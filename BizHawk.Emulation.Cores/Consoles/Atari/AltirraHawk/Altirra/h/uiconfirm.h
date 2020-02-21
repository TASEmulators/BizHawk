//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_UICONFIRM_H
#define f_AT_UICONFIRM_H

#include <vd2/system/vdtypes.h>

enum ATUIResetFlag : uint32 {
	kATUIResetFlag_None					= 0x00000000,
	kATUIResetFlag_CartridgeChange		= 0x00000001,
	kATUIResetFlag_BasicChange			= 0x00000002,
	kATUIResetFlag_VideoStandardChange	= 0x00000004,
	kATUIResetFlag_All					= 0x00000007,
	kATUIResetFlag_Default				= kATUIResetFlag_CartridgeChange,
};

uint32 ATUIGetResetFlags();
void ATUISetResetFlags(uint32 flags);
bool ATUIIsResetNeeded(uint32 flag);
void ATUIModifyResetFlag(uint32 flag, bool newState);

bool ATUIConfirmDiscardMemory(VDGUIHandle h, const wchar_t *title);
bool ATUIConfirmReset(VDGUIHandle h, const char *key, const wchar_t *message, const wchar_t *title);
void ATUIConfirmResetComplete();

bool ATUIConfirmBasicChangeReset();
void ATUIConfirmBasicChangeResetComplete();

bool ATUIConfirmVideoStandardChangeReset();
void ATUIConfirmVideoStandardChangeResetComplete();

bool ATUIConfirmCartridgeChangeReset();
void ATUIConfirmCartridgeChangeResetComplete();

bool ATUIConfirmSystemChangeReset();
void ATUIConfirmSystemChangeResetComplete();

bool ATUIConfirmAddFullDrive();
#endif
