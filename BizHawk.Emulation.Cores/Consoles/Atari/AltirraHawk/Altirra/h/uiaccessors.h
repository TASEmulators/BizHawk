//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2014 Avery Lee
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

#ifndef f_AT_UIACCESSORS_H
#define f_AT_UIACCESSORS_H

#include <vd2/system/vdtypes.h>

// UI accessors are scattered all over the code base, so we at least collect
// the prototypes here.

enum ATHardwareMode : uint32;
enum ATMemoryMode : uint32;
enum ATVideoStandard : uint32;
enum ATDisplayFilterMode : uint32;
enum ATDisplayStretchMode : uint32;
enum ATFrameRateMode : uint32;
class ATUICommandManager;

bool ATUIGetXEPViewEnabled();
void ATUISetXEPViewEnabled(bool enabled);

bool ATUIGetXEPViewAutoswitchingEnabled();
void ATUISetXEPViewAutoswitchingEnabled(bool enabled);

ATDisplayStretchMode ATUIGetDisplayStretchMode();
void ATUISetDisplayStretchMode(ATDisplayStretchMode mode);

void ATSetVideoStandard(ATVideoStandard vs);
bool ATUISwitchHardwareMode(VDGUIHandle h, ATHardwareMode mode, bool switchProfile);
void ATUISwitchMemoryMode(VDGUIHandle h, ATMemoryMode mode);

bool ATUIGetDriveSoundsEnabled();
void ATUISetDriveSoundsEnabled(bool enabled);

void ATUIOpenOnScreenKeyboard();
void ATUIToggleHoldKeys();

uint32 ATUIGetBootUnloadStorageMask();
void ATUISetBootUnloadStorageMask(uint32 mask);

int ATUIGetViewFilterSharpness();
void ATUISetViewFilterSharpness(int sharpness);
int ATUIGetViewFilterSharpness();
void ATUISetViewFilterSharpness(int sharpness);
ATDisplayFilterMode ATUIGetDisplayFilterMode();
void ATUISetDisplayFilterMode(ATDisplayFilterMode mode);
bool ATUIGetShowFPS();
void ATUISetShowFPS(bool enabled);
bool ATUIGetFullscreen();
bool ATUIGetDisplayFullscreen();
void ATSetFullscreen(bool);

bool ATUIGetDisplayPadIndicators();
void ATUISetDisplayPadIndicators(bool enabled);

bool ATUIGetDisplayIndicators();
void ATUISetDisplayIndicators(bool enabled);

bool ATUICanManipulateWindows();

bool ATUIIsMouseCaptured();

bool ATUIGetMouseAutoCapture();
void ATUISetMouseAutoCapture(bool enabled);

bool ATUIGetTurbo();
void ATUISetTurbo(bool turbo);

bool ATUIGetTurboPulse();
void ATUISetTurboPulse(bool turbo);

bool ATUIGetSlowMotion();
void ATUISetSlowMotion(bool slowmo);

bool ATUIGetPauseWhenInactive();
void ATUISetPauseWhenInactive(bool enabled);

ATFrameRateMode ATUIGetFrameRateMode();
void ATUISetFrameRateMode(ATFrameRateMode mode);

float ATUIGetSpeedModifier();
void ATUISetSpeedModifier(float modifier);

enum ATUIRecordingStatus {
	kATUIRecordingStatus_None,
	kATUIRecordingStatus_Video,
	kATUIRecordingStatus_Audio,
	kATUIRecordingStatus_RawAudio,
	kATUIRecordingStatus_Sap
};

ATUIRecordingStatus ATUIGetRecordingStatus();

enum ATUIEnhancedTextMode {
	kATUIEnhancedTextMode_None,
	kATUIEnhancedTextMode_Hardware,
	kATUIEnhancedTextMode_Software,
	kATUIEnhancedTextModeCount
};

ATUIEnhancedTextMode ATUIGetEnhancedTextMode();
void ATUISetEnhancedTextMode(ATUIEnhancedTextMode mode);

VDGUIHandle ATUIGetMainWindow();
VDGUIHandle ATUIGetNewPopupOwner();
bool ATUIGetAppActive();
void ATUISetAppActive(bool active);

void ATUIExit(bool forceNoConfirm);

void ATUISetWindowCaptionTemplate(const char *s);
const char *ATUIGetWindowCaptionTemplate();

bool ATUIGetDeviceButtonSupported(uint32 idx);
bool ATUIGetDeviceButtonDepressed(uint32 idx);
void ATUIActivateDeviceButton(uint32 idx, bool state);

ATUICommandManager& ATUIGetCommandManager();

void ATUIBootImage(const wchar_t *path);

#endif	// f_AT_UIACCESSORS_H
