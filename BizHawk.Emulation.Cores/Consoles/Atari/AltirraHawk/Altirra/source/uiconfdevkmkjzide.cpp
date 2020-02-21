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

class ATUIDialogDeviceKMKJZIDE : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceKMKJZIDE(ATPropertySet& props);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboId;
};

ATUIDialogDeviceKMKJZIDE::ATUIDialogDeviceKMKJZIDE(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_KMKJZIDE)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceKMKJZIDE::OnLoaded() {
	AddProxy(&mComboId, IDC_DEVICE_ID);

	for(uint32 i=0; i<8; ++i) {
		const wchar_t idstr[] = { (wchar_t)(L'0' + i), 0 };

		mComboId.AddItem(idstr);
	}

	OnDataExchange(false);
	SetFocusToControl(IDC_DEVICE_ID);
	return true;
}

void ATUIDialogDeviceKMKJZIDE::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		int id = mComboId.GetSelection();

		if (id >= 0 && id <= 7)
			mPropSet.SetUint32("id", (uint32)id);
	} else {
		CheckButton(IDC_ENABLE_SDX, mPropSet.GetBool("enablesdx", true));

		uint32 id = mPropSet.GetUint32("id", 0);
		mComboId.SetSelection(id < 8 ? id : 0);
	}
}

bool ATUIConfDevKMKJZIDE(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceKMKJZIDE dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
