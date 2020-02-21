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

class ATUIDialogDeviceVBXE final : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceVBXE(ATPropertySet& props);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	VDUIProxyComboBoxControl mCoreVersionControl;
	ATPropertySet& mPropSet;
};

ATUIDialogDeviceVBXE::ATUIDialogDeviceVBXE(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_VBXE)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceVBXE::OnLoaded() {
	AddProxy(&mCoreVersionControl, IDC_COREVERSION);
	mCoreVersionControl.AddItem(L"FX 1.20");
	mCoreVersionControl.AddItem(L"FX 1.24");
	mCoreVersionControl.AddItem(L"FX 1.26");

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceVBXE::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		const bool altPage = IsButtonChecked(IDC_VBXEBASE_D700);
		mPropSet.SetBool("alt_page", altPage);
		mPropSet.SetBool("shared_mem", IsButtonChecked(IDC_VBXE_SHAREDMEM));

		const sint32 versionSel = mCoreVersionControl.GetSelection();
		mPropSet.SetUint32("version", versionSel == 0 ? 120 : versionSel == 1 ? 124 : 126);
	} else {
		bool altPage = mPropSet.GetBool("alt_page");

		CheckButton(IDC_VBXEBASE_D600, !altPage);
		CheckButton(IDC_VBXEBASE_D700, altPage);

		CheckButton(IDC_VBXE_SHAREDMEM, mPropSet.GetBool("shared_mem"));

		const uint32 version = mPropSet.GetUint32("version", 126);
		mCoreVersionControl.SetSelection(version >= 126 ? 2 : version >= 124 ? 1 : 0);
	}
}

bool ATUIConfDevVBXE(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceVBXE dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
