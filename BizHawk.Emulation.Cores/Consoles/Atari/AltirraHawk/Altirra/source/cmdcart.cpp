//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/Dita/services.h>
#include <at/atcore/device.h>
#include <at/atcore/devicemanager.h>
#include "cartridge.h"
#include "simulator.h"
#include "uiaccessors.h"
#include "uifilefilters.h"
#include "uiconfirm.h"

extern ATSimulator g_sim;

bool ATUIConfirmDiscardCartridge(VDGUIHandle h);
int ATUIShowDialogCartridgeMapper(VDGUIHandle h, uint32 cartSize, const void *data);

extern void DoLoad(VDGUIHandle h, const wchar_t *path, const ATMediaWriteMode *writeMode, int cartmapper, ATImageType loadType = kATImageType_None, bool *suppressColdReset = NULL, int loadIndex = -1, bool autoProfile = false);

void OnCommandAttachCartridge(bool cart2) {
	if (!ATUIConfirmDiscardCartridge(ATUIGetMainWindow()))
		return;

	if (!ATUIConfirmCartridgeChangeReset())
		return;

	VDStringW fn(VDGetLoadFileName('cart', ATUIGetMainWindow(), L"Load cartridge",
		g_ATUIFileFilter_LoadCartridge,
		L"bin"));

	if (fn.empty())
		return;

	DoLoad(ATUIGetMainWindow(), fn.c_str(), nullptr, 0, kATImageType_Cartridge, nullptr, cart2 ? 1 : 0);
	ATUIConfirmCartridgeChangeResetComplete();
}

void OnCommandAttachCartridge() {
	OnCommandAttachCartridge(false);
}

void OnCommandAttachCartridge2() {
	OnCommandAttachCartridge(true);
}

void OnCommandDetachCartridge() {
	if (!ATUIConfirmDiscardCartridge(ATUIGetMainWindow()))
		return;

	if (!ATUIConfirmCartridgeChangeReset())
		return;

	if (g_sim.GetHardwareMode() == kATHardwareMode_5200)
		g_sim.LoadCartridge5200Default();
	else
		g_sim.UnloadCartridge(0);

	ATUIConfirmCartridgeChangeResetComplete();
}

void OnCommandDetachCartridge2() {
	if (!ATUIConfirmDiscardCartridge(ATUIGetMainWindow()))
		return;

	if (!ATUIConfirmCartridgeChangeReset())
		return;

	g_sim.UnloadCartridge(1);
	ATUIConfirmCartridgeChangeResetComplete();
}

void OnCommandAttachNewCartridge(ATCartridgeMode mode) {
	if (!ATUIConfirmDiscardCartridge(ATUIGetMainWindow()))
		return;

	if (!ATUIConfirmCartridgeChangeReset())
		return;

	g_sim.LoadNewCartridge(mode);
	ATUIConfirmCartridgeChangeResetComplete();
}

void OnCommandAttachCartridgeBASIC() {
	if (!ATUIConfirmDiscardCartridge(ATUIGetMainWindow()))
		return;

	if (!ATUIConfirmCartridgeChangeReset())
		return;

	g_sim.LoadCartridgeBASIC();
	ATUIConfirmCartridgeChangeResetComplete();
}

void OnCommandCartActivateMenuButton() {
	ATUIActivateDeviceButton(kATDeviceButton_CartridgeResetBank, true);
}

void OnCommandCartToggleSwitch() {
	g_sim.SetCartridgeSwitch(!g_sim.GetCartridgeSwitch());
}

void OnCommandSaveCartridge() {
	ATCartridgeEmulator *cart = g_sim.GetCartridge(0);
	int mode = 0;

	if (cart)
		mode = cart->GetMode();

	if (!mode)
		throw MyError("There is no cartridge to save.");

	if (mode == kATCartridgeMode_SuperCharger3D)
		throw MyError("The current cartridge cannot be saved to an image file.");

	VDFileDialogOption opts[]={
		{VDFileDialogOption::kSelectedFilter, 0, NULL, 0, 0},
		{0}
	};

	int optval[1]={0};

	VDStringW fn(VDGetSaveFileName('cart', ATUIGetMainWindow(), L"Save cartridge",
		L"Cartridge image with header (*.car)\0*.car\0"
		L"Raw cartridge image (*.bin)\0*.bin\0",

		L"car", opts, optval));

	if (!fn.empty()) {
		cart->Save(fn.c_str(), optval[0] == 1);
	}
}
