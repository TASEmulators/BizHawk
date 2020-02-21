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

class ATUIDialogDeviceDongle : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceDongle(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboJoyPort;
	VDUIProxyEditControl mMappingEdit;
};

ATUIDialogDeviceDongle::ATUIDialogDeviceDongle(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_DONGLE)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceDongle::OnLoaded() {
	AddProxy(&mComboJoyPort, IDC_PORT);
	AddProxy(&mMappingEdit, IDC_MAPPING);

	mComboJoyPort.AddItem(L"Port 1");
	mComboJoyPort.AddItem(L"Port 2");
	mComboJoyPort.AddItem(L"Port 3 (400/800 only)");
	mComboJoyPort.AddItem(L"Port 4 (400/800 only)");

	mComboJoyPort.SetSelection(0);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceDongle::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		VDStringW mappingStr(mMappingEdit.GetText());

		// trim the string
		size_t len1 = 0;
		size_t len2 = mappingStr.size();

		while(len2 > len1 && iswspace(mappingStr[len2 - 1]))
			--len2;

		while(len1 < len2 && iswspace(mappingStr[len1]))
			++len1;

		mappingStr.erase(mappingStr.begin() + len2, mappingStr.end());
		mappingStr.erase(mappingStr.begin(), mappingStr.begin() + len1);

		// convert to uppercase
		std::transform(mappingStr.begin(), mappingStr.end(), mappingStr.begin(), ::towupper);

		// check if it's the wrong length or has non-hex digits
		if (len2 - len1 != 16 || std::find_if_not(mappingStr.begin(), mappingStr.end(), iswxdigit) != mappingStr.end()) {
			FailValidation(IDC_MAPPING, L"The mapping string must be a set of 16 hexadecimal digits.");
			return;
		}

		mPropSet.SetString("mapping", mappingStr.c_str());

		int port = mComboJoyPort.GetSelection();
		mPropSet.SetUint32("port", port >= 0 && port <= 3 ? port : 0);
	} else {
		mComboJoyPort.SetSelection(mPropSet.GetUint32("port"));

		mMappingEdit.SetText(mPropSet.GetString("mapping", L"FFFFFFFFFFFFFFFF"));
	}
}

bool ATUIConfDevDongle(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceDongle dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
