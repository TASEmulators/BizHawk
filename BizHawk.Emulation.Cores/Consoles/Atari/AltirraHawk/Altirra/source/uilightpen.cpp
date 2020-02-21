//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "inputcontroller.h"

class ATUIDialogLightPen : public VDDialogFrameW32 {
public:
	ATUIDialogLightPen(ATLightPenPort *lpp);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATLightPenPort *mpLPP;
};

ATUIDialogLightPen::ATUIDialogLightPen(ATLightPenPort *lpp)
	: VDDialogFrameW32(IDD_LIGHTPEN)
	, mpLPP(lpp)
{
}

bool ATUIDialogLightPen::OnLoaded() {
	UDSetRange(IDC_HSPIN, -64, 64);
	UDSetRange(IDC_VSPIN, 64, -64);
	OnDataExchange(false);
	SetFocusToControl(IDC_HVALUE);
	return true;
}

void ATUIDialogLightPen::OnDataExchange(bool write) {
	sint32 x, y;

	if (write) {
		x = GetControlValueSint32(IDC_HVALUE);
		y = GetControlValueSint32(IDC_VVALUE);

		if (!mbValidationFailed) {
			mpLPP->SetAdjust(x, y);
		}
	} else {
		x = mpLPP->GetAdjustX();
		y = mpLPP->GetAdjustY();

		SetControlTextF(IDC_HVALUE, L"%d", x);
		SetControlTextF(IDC_VVALUE, L"%d", y);
	}
}

void ATUIShowDialogLightPen(VDGUIHandle h, ATLightPenPort *lpp) {
	ATUIDialogLightPen dlg(lpp);

	dlg.ShowDialog(h);
}
