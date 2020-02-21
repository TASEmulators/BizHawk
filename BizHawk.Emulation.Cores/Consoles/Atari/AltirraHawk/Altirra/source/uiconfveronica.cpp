//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include <at/atcore/propertyset.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"

class ATUIDialogDeviceVeronica : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceVeronica(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboVersion;
};

ATUIDialogDeviceVeronica::ATUIDialogDeviceVeronica(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_VERONICA)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceVeronica::OnLoaded() {
	AddProxy(&mComboVersion, IDC_VERSION);

	mComboVersion.AddItem(L"V1 - three RAM chips");
	mComboVersion.AddItem(L"V2 - single RAM chip");

	mComboVersion.SetSelection(1);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceVeronica::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		if (mComboVersion.GetSelection() == 0)
			mPropSet.SetBool("version1", true);
	} else {
		mComboVersion.SetSelection(mPropSet.GetBool("version1", false) ? 0 : 1);
	}
}

bool ATUIConfDevVeronica(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceVeronica dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
