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

#include "stdafx.h"
#include <at/atnativeui/genericdialog.h>
#include "uiaccessors.h"
#include "uiconfirm.h"

uint32 g_ATUIResetFlags = 0;

uint32 ATUIGetResetFlags() {
	return g_ATUIResetFlags;
}

void ATUISetResetFlags(uint32 flags) {
	g_ATUIResetFlags = flags;
}

bool ATUIIsResetNeeded(uint32 flag) {
	return (g_ATUIResetFlags & flag) != 0;
}

void ATUIModifyResetFlag(uint32 flag, bool newState) {
	if (newState)
		g_ATUIResetFlags |= flag;
	else
		g_ATUIResetFlags &= ~flag;
}

///////////////////////////////////////////////////////////////////////////

bool ATUIConfirmBasicChangeReset() {
	if (!ATUIIsResetNeeded(kATUIResetFlag_BasicChange))
		return true;

	return ATUIConfirmReset(ATUIGetMainWindow(), "ResetBasicChange", L"This will reset the emulated computer. Are you sure?", L"Changing BASIC");
}

void ATUIConfirmBasicChangeResetComplete() {
	if (ATUIIsResetNeeded(kATUIResetFlag_BasicChange))
		ATUIConfirmResetComplete();
}

bool ATUIConfirmVideoStandardChangeReset() {
	if (!ATUIIsResetNeeded(kATUIResetFlag_VideoStandardChange))
		return true;

	return ATUIConfirmReset(ATUIGetMainWindow(), "ResetVideoStandardChange", L"This will reset the emulated computer. Are you sure?", L"Changing video standard");
}

void ATUIConfirmVideoStandardChangeResetComplete() {
	if (ATUIIsResetNeeded(kATUIResetFlag_VideoStandardChange))
		ATUIConfirmResetComplete();
}

bool ATUIConfirmCartridgeChangeReset() {
	if (!ATUIIsResetNeeded(kATUIResetFlag_CartridgeChange))
		return true;

	return ATUIConfirmReset(ATUIGetMainWindow(), "ResetCartridgeChange", L"This will reset the emulated computer. Are you sure?", L"Changing cartridge");
}

void ATUIConfirmCartridgeChangeResetComplete() {
	if (ATUIIsResetNeeded(kATUIResetFlag_CartridgeChange))
		ATUIConfirmResetComplete();
}

bool ATUIConfirmSystemChangeReset() {
	return ATUIConfirmReset(ATUIGetMainWindow(), "ResetSystemChange", L"This will reset the emulated computer. Are you sure?", L"Changing system");
}

void ATUIConfirmSystemChangeResetComplete() {
	ATUIConfirmResetComplete();
}

bool ATUIConfirmAddFullDrive() {
	return ATUIConfirm(ATUIGetNewPopupOwner(),
		"AddFullDrive",
		L"Full disk drive emulation is the most accurate but needed only for a few very "
		L"drive-specific programs. It also requires a firmware image and disables some acceleration "
		L"and assist features in the emulator. The standard disk drive emulation is better for "
		L"almost all programs.\n\nUse full drive emulation anyway?",
		L"Adding Full Disk Drive Emulation");
}
