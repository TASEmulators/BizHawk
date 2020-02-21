//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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

class ATUIDialogDevicePercom : public VDDialogFrameW32 {
public:
	ATUIDialogDevicePercom(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboDriveSelect;
	VDUIProxyComboBoxControl mComboDriveTypes[4];

	static const uint32 kDriveTypeIds[];
};

const uint32 ATUIDialogDevicePercom::kDriveTypeIds[]={
	IDC_DRIVETYPE1,
	IDC_DRIVETYPE2,
	IDC_DRIVETYPE3,
	IDC_DRIVETYPE4,
};

ATUIDialogDevicePercom::ATUIDialogDevicePercom(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_PERCOM)
	, mPropSet(props)
{
}

bool ATUIDialogDevicePercom::OnLoaded() {
	AddProxy(&mComboDriveSelect, IDC_DRIVESELECT);

	VDStringW s;
	for(int i=1; i<=8; ++i) {
		s.sprintf(L"Drive %d (D%d:)", i, i);
		mComboDriveSelect.AddItem(s.c_str());
	}

	mComboDriveSelect.SetSelection(0);

	for(size_t i=0; i<4; ++i) {
		auto& combo = mComboDriveTypes[i];
		AddProxy(&combo, kDriveTypeIds[i]);

		combo.AddItem(L"None");
		combo.AddItem(L"5.25\" (40 track)");
		combo.AddItem(L"5.25\" (80 track)");

		combo.SetSelection(0);
	}

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDevicePercom::OnDataExchange(bool write) {
	if (write) {
		mPropSet.SetUint32("id", mComboDriveSelect.GetSelection());

		VDStringA s;
		for(size_t i=0; i<4; ++i) {
			s.sprintf("drivetype%u", i);
			mPropSet.SetUint32(s.c_str(), mComboDriveTypes[i].GetSelection());
		}
	} else {
		VDStringA s;
		for(size_t i=0; i<4; ++i) {
			s.sprintf("drivetype%u", i);
			mComboDriveTypes[i].SetSelection(mPropSet.GetUint32(s.c_str(), i ? 0 : 1));
		}

		mComboDriveSelect.SetSelection(mPropSet.GetUint32("id", 0));
	}
}

bool ATUIConfDevPercom(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDevicePercom dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
