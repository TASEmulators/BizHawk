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

class ATUIDialogDeviceKMKJZIDE2 : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceKMKJZIDE2(ATPropertySet& props);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboVersion;
	VDUIProxyComboBoxControl mComboId;
};

ATUIDialogDeviceKMKJZIDE2::ATUIDialogDeviceKMKJZIDE2(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_KMKJZIDEV2)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceKMKJZIDE2::OnLoaded() {
	AddProxy(&mComboVersion, IDC_REVISION);
	AddProxy(&mComboId, IDC_DEVICE_ID);
	mComboVersion.AddItem(L"Rev. C");
	mComboVersion.AddItem(L"Rev. D");
	mComboVersion.AddItem(L"Rev. Ds/S (rev.D with Covox)");
	mComboVersion.AddItem(L"Rev. E");

	for(uint32 i=0; i<8; ++i) {
		const wchar_t idstr[] = { (wchar_t)(L'0' + i), 0 };

		mComboId.AddItem(idstr);
	}

	OnDataExchange(false);
	SetFocusToControl(IDC_VERSION);
	return true;
}

void ATUIDialogDeviceKMKJZIDE2::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();
		mPropSet.SetBool("enablesdx", IsButtonChecked(IDC_ENABLE_SDX));

		const wchar_t *rev;
		switch(mComboVersion.GetSelection()) {
			case 0:
				rev = L"c";
				break;

			case 1:
			default:
				rev = L"d";
				break;

			case 2:
				rev = L"s";
				break;

			case 3:
				rev = L"e";
				break;
		}

		mPropSet.SetString("revision", rev);

		if (IsButtonChecked(IDC_WRITE_PROTECT))
			mPropSet.SetBool("writeprotect", true);

		mPropSet.SetBool("nvramguard", IsButtonChecked(IDC_NVRAM_PROTECT));

		int id = mComboId.GetSelection();

		if (id >= 0 && id <= 7)
			mPropSet.SetUint32("id", (uint32)id);
	} else {
		CheckButton(IDC_ENABLE_SDX, mPropSet.GetBool("enablesdx", true));

		const VDStringSpanW rev(mPropSet.GetString("revision", L"d"));

		if (rev == L"e")
			mComboVersion.SetSelection(3);
		else if (rev == L"s")
			mComboVersion.SetSelection(2);
		else if (rev == L"c")
			mComboVersion.SetSelection(0);
		else
			mComboVersion.SetSelection(1);

		CheckButton(IDC_WRITE_PROTECT, mPropSet.GetBool("writeprotect", false));
		CheckButton(IDC_NVRAM_PROTECT, mPropSet.GetBool("nvramguard", true));

		uint32 id = mPropSet.GetUint32("id", 0);
		mComboId.SetSelection(id < 8 ? id : 0);
	}
}

bool ATUIConfDevKMKJZIDE2(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceKMKJZIDE2 dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
