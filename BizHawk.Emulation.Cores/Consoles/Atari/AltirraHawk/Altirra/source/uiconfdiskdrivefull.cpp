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

class ATUIDialogDeviceDiskDriveFull : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceDiskDriveFull(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboDriveSelect;
};

ATUIDialogDeviceDiskDriveFull::ATUIDialogDeviceDiskDriveFull(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_DISKDRIVEFULL)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceDiskDriveFull::OnLoaded() {
	AddProxy(&mComboDriveSelect, IDC_DRIVESELECT);

	VDStringW s;
	for(int i=1; i<=4; ++i) {
		s.sprintf(L"Drive %d (D%d:)", i, i);
		mComboDriveSelect.AddItem(s.c_str());
	}

	mComboDriveSelect.SetSelection(0);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceDiskDriveFull::OnDataExchange(bool write) {
	if (write) {
		mPropSet.SetUint32("id", mComboDriveSelect.GetSelection());
	} else {
		mComboDriveSelect.SetSelection(mPropSet.GetUint32("id", 0));
	}
}

bool ATUIConfDevDiskDriveFull(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceDiskDriveFull dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
