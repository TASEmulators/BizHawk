//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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

class ATUIDialogDeviceMyIDE2 : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceMyIDE2(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboVersion;
};

ATUIDialogDeviceMyIDE2::ATUIDialogDeviceMyIDE2(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_MYIDE2)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceMyIDE2::OnLoaded() {
	AddProxy(&mComboVersion, IDC_VERSION);

	mComboVersion.AddItem(L"Original");
	mComboVersion.AddItem(L"Updated CPLD with video playback window");

	mComboVersion.SetSelection(0);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceMyIDE2::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		if (mComboVersion.GetSelection() == 1)
			mPropSet.SetUint32("cpldver", 2);
	} else {
		mComboVersion.SetSelection(mPropSet.GetUint32("cpldver") >= 2 ? 1 : 0);
	}
}

bool ATUIConfDevMyIDE2(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceMyIDE2 dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
