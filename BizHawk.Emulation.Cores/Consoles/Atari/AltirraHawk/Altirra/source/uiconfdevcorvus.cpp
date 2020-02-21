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

class ATUIDialogDeviceCorvus : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceCorvus(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboJoyPort;
};

ATUIDialogDeviceCorvus::ATUIDialogDeviceCorvus(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_CORVUS)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceCorvus::OnLoaded() {
	AddProxy(&mComboJoyPort, IDC_PORT);

	mComboJoyPort.AddItem(L"Ports 3+4 (standard, but 400/800 only)");
	mComboJoyPort.AddItem(L"Ports 1+2 (XL/XE compatible)");

	mComboJoyPort.SetSelection(0);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceCorvus::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		if (mComboJoyPort.GetSelection() > 0)
			mPropSet.SetBool("altports", true);
	} else {
		mComboJoyPort.SetSelection(mPropSet.GetBool("altports", false) ? 1 : 0);
	}
}

bool ATUIConfDevCorvus(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceCorvus dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
