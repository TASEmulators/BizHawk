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
#include <at/atui/uimanager.h>
#include "uiaccessors.h"
#include "uitypes.h"
#include "gtia.h"
#include "options.h"
#include "cmdhelpers.h"

extern ATUIManager g_ATUIManager;

void ATUISetOverscanMode(ATGTIAEmulator::OverscanMode mode);

void OnCommandViewNextFilterMode();
void OnCommandViewVerticalOverscan(ATGTIAEmulator::VerticalOverscanMode mode);
void OnCommandViewTogglePALExtended();

void OnCommandViewNextFilterMode() {
	switch(ATUIGetDisplayFilterMode()) {
		case kATDisplayFilterMode_Point:
			ATUISetDisplayFilterMode(kATDisplayFilterMode_Bilinear);
			break;
		case kATDisplayFilterMode_Bilinear:
			ATUISetDisplayFilterMode(kATDisplayFilterMode_SharpBilinear);
			break;
		case kATDisplayFilterMode_SharpBilinear:
			ATUISetDisplayFilterMode(kATDisplayFilterMode_Bicubic);
			break;
		case kATDisplayFilterMode_Bicubic:
			ATUISetDisplayFilterMode(kATDisplayFilterMode_AnySuitable);
			break;
		case kATDisplayFilterMode_AnySuitable:
			ATUISetDisplayFilterMode(kATDisplayFilterMode_Point);
			break;
	}
}

void OnCommandViewFilterModePoint() {
	ATUISetDisplayFilterMode(kATDisplayFilterMode_Point);
}

void OnCommandViewFilterModeBilinear() {
	ATUISetDisplayFilterMode(kATDisplayFilterMode_Bilinear);
}

void OnCommandViewFilterModeSharpBilinear() {
	ATUISetDisplayFilterMode(kATDisplayFilterMode_SharpBilinear);
}

void OnCommandViewFilterModeBicubic() {
	ATUISetDisplayFilterMode(kATDisplayFilterMode_Bicubic);
}

void OnCommandViewFilterModeDefault() {
	ATUISetDisplayFilterMode(kATDisplayFilterMode_AnySuitable);
}

void OnCommandViewFilterSharpnessSofter() {
	ATUISetViewFilterSharpness(-2);
}

void OnCommandViewFilterSharpnessSoft() {
	ATUISetViewFilterSharpness(-1);
}

void OnCommandViewFilterSharpnessNormal() {
	ATUISetViewFilterSharpness(0);
}

void OnCommandViewFilterSharpnessSharp() {
	ATUISetViewFilterSharpness(+1);
}

void OnCommandViewFilterSharpnessSharper() {
	ATUISetViewFilterSharpness(+2);
}

void OnCommandViewStretchFitToWindow() {
	ATUISetDisplayStretchMode(kATDisplayStretchMode_Unconstrained);
}

void OnCommandViewStretchPreserveAspectRatio() {
	ATUISetDisplayStretchMode(kATDisplayStretchMode_PreserveAspectRatio);
}

void OnCommandViewStretchSquarePixels() {
	ATUISetDisplayStretchMode(kATDisplayStretchMode_SquarePixels);
}

void OnCommandViewStretchSquarePixelsInt() {
	ATUISetDisplayStretchMode(kATDisplayStretchMode_Integral);
}

void OnCommandViewStretchPreserveAspectRatioInt() {
	ATUISetDisplayStretchMode(kATDisplayStretchMode_IntegralPreserveAspectRatio);
}

void OnCommandViewOverscanOSScreen() {
	ATUISetOverscanMode(ATGTIAEmulator::kOverscanOSScreen);
}

void OnCommandViewOverscanNormal() {
	ATUISetOverscanMode(ATGTIAEmulator::kOverscanNormal);
}

void OnCommandViewOverscanWidescreen() {
	ATUISetOverscanMode(ATGTIAEmulator::kOverscanWidescreen);
}

void OnCommandViewOverscanExtended() {
	ATUISetOverscanMode(ATGTIAEmulator::kOverscanExtended);
}

void OnCommandViewOverscanFull() {
	ATUISetOverscanMode(ATGTIAEmulator::kOverscanFull);
}

void OnCommandViewToggleFullScreen() {
	ATSetFullscreen(!ATUIGetFullscreen());
}

void OnCommandViewToggleFPS() {
	ATUISetShowFPS(!ATUIGetShowFPS());
}

void OnCommandVideoToggleXEP80View() {
	ATUISetXEPViewEnabled(!ATUIGetXEPViewEnabled());
}

void OnCommandViewEffectReload() {
	g_ATUIManager.SetCustomEffectPath(VDStringW(g_ATUIManager.GetCustomEffectPath()).c_str(), true);
}

void OnCommandViewToggleIndicatorMargin() {
	ATUISetDisplayPadIndicators(!ATUIGetDisplayPadIndicators());
}

void OnCommandViewToggleIndicators() {
	ATUISetDisplayIndicators(!ATUIGetDisplayIndicators());
}

void OnCommandViewToggleAccelScreenFX() {
	ATOptions prev(g_ATOptions);

	g_ATOptions.mbDirty = true;
	g_ATOptions.mbDisplayAccelScreenFX = !g_ATOptions.mbDisplayAccelScreenFX;

	ATOptionsSave();
	ATOptionsRunUpdateCallbacks(&prev);
}

namespace ATCommands {
	static constexpr ATUICommand kATCommandsView[] = {
		{ "View.ToggleIndicatorMargin", OnCommandViewToggleIndicatorMargin, ATUIGetDisplayIndicators, [] { return ATUIGetDisplayPadIndicators() ? kATUICmdState_Checked : kATUICmdState_None; } },
		{ "View.ToggleIndicators", OnCommandViewToggleIndicators, nullptr, [] { return ATUIGetDisplayIndicators() ? kATUICmdState_Checked : kATUICmdState_None; } },
		{ "View.ToggleAccelScreenFX", OnCommandViewToggleAccelScreenFX, NULL, [] { return ToChecked(g_ATOptions.mbDisplayAccelScreenFX); } },
	};
}

void ATUIInitCommandMappingsView(ATUICommandManager& cmdMgr) {
	using namespace ATCommands;

	cmdMgr.RegisterCommands(kATCommandsView, vdcountof(kATCommandsView));
}
