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

class ATUIDialogDeviceCovox : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceCovox(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboAddress;
	VDUIProxyComboBoxControl mComboChannels;

	static const uint16 kBaseAddresses[];
};

const uint16 ATUIDialogDeviceCovox::kBaseAddresses[] = {
	0xD100,
	0xD280,
	0xD500,
	0xD600,
	0xD700,
};

ATUIDialogDeviceCovox::ATUIDialogDeviceCovox(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_COVOX)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceCovox::OnLoaded() {
	AddProxy(&mComboAddress, IDC_ADDRESS);
	AddProxy(&mComboChannels, IDC_CHANNELS);

	for(uint16 baseAddr : kBaseAddresses)
		mComboAddress.AddItem(VDStringW().sprintf(L"$%04X-%04X", baseAddr, baseAddr | 0xFF).c_str());

	mComboChannels.AddItem(L"1 channel (mono)");
	mComboChannels.AddItem(L"4 channels (stereo)");

	OnDataExchange(false);
	SetFocusToControl(IDC_ADDRESS);

	return true;
}

void ATUIDialogDeviceCovox::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

		int sel = mComboAddress.GetSelection();
		if (sel >= 0 && sel < (int)vdcountof(kBaseAddresses))
			mPropSet.SetUint32("base", kBaseAddresses[sel]);

		mPropSet.SetUint32("channels", mComboChannels.GetSelection() > 0 ? 4 : 1);
	} else {
		uint32 baseAddr = mPropSet.GetUint32("base", 0xD600);

		auto it = std::find(std::begin(kBaseAddresses), std::end(kBaseAddresses), baseAddr);
		int idx = (it != std::end(kBaseAddresses)) ? it - std::begin(kBaseAddresses) : 3;

		mComboAddress.SetSelection(idx);

		mComboChannels.SetSelection(mPropSet.GetUint32("channels", 4) > 1 ? 1 : 0);
	}
}

bool ATUIConfDevCovox(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceCovox dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
