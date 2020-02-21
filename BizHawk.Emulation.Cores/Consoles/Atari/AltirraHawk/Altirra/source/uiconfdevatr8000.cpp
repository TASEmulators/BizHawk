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

class ATUIDialogDeviceATR8000 : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceATR8000(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboDriveTypes[4];
	VDUIProxyComboBoxControl mComboSerialSignal1;
	VDUIProxyComboBoxControl mComboSerialSignal2;

	static const uint32 kDriveTypeIds[];

	static constexpr const wchar_t *const kSignal1Names[] = {
		L"rts", L"dtr"
	};

	static constexpr const wchar_t *const kSignal2Names[] = {
		L"cts", L"dsr", L"cd", L"srts"
	};
};

const uint32 ATUIDialogDeviceATR8000::kDriveTypeIds[]={
	IDC_DRIVETYPE1,
	IDC_DRIVETYPE2,
	IDC_DRIVETYPE3,
	IDC_DRIVETYPE4,
};

ATUIDialogDeviceATR8000::ATUIDialogDeviceATR8000(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_ATR8000)
	, mPropSet(props)
{
}

bool ATUIDialogDeviceATR8000::OnLoaded() {
	for(size_t i=0; i<4; ++i) {
		auto& combo = mComboDriveTypes[i];
		AddProxy(&combo, kDriveTypeIds[i]);

		combo.AddItem(L"None");
		combo.AddItem(L"5.25\"");
		combo.AddItem(L"8\"");

		combo.SetSelection(0);
	}

	AddProxy(&mComboSerialSignal1, IDC_SERIALPORT_SIGNAL1);
	mComboSerialSignal1.AddItem(L"Request To Send (RTS)");
	mComboSerialSignal1.AddItem(L"Data Terminal Ready (DTR)");

	AddProxy(&mComboSerialSignal2, IDC_SERIALPORT_SIGNAL2);
	mComboSerialSignal2.AddItem(L"Clear To Send (CTS)");
	mComboSerialSignal2.AddItem(L"Data Set Ready (DSR)");
	mComboSerialSignal2.AddItem(L"Carrier Detect (CD)");
	mComboSerialSignal2.AddItem(L"Secondary Request To Send (SRTS)");

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceATR8000::OnDataExchange(bool write) {
	if (write) {
		VDStringA s;
		for(size_t i=0; i<4; ++i) {
			s.sprintf("drivetype%u", i);
			mPropSet.SetUint32(s.c_str(), mComboDriveTypes[i].GetSelection());
		}

		const int idx1 = mComboSerialSignal1.GetSelection();
		mPropSet.SetString("signal1", (unsigned)idx1 < vdcountof(kSignal1Names) ? kSignal1Names[idx1] : kSignal1Names[0]);

		const int idx2 = mComboSerialSignal2.GetSelection();
		mPropSet.SetString("signal2", (unsigned)idx2 < vdcountof(kSignal2Names) ? kSignal2Names[idx2] : kSignal2Names[0]);
	} else {
		VDStringA s;
		for(size_t i=0; i<4; ++i) {
			s.sprintf("drivetype%u", i);
			mComboDriveTypes[i].SetSelection(mPropSet.GetUint32(s.c_str(), i ? 0 : 1));
		}

		const VDStringSpanW signal1 { mPropSet.GetString("signal1", L"") };
		auto it1 = std::find_if(std::begin(kSignal1Names), std::end(kSignal1Names), [=](const wchar_t *s) { return signal1 == s; });

		if (it1 != std::end(kSignal1Names))
			mComboSerialSignal1.SetSelection((int)(it1 - std::begin(kSignal1Names)));
		else
			mComboSerialSignal1.SetSelection(0);

		const VDStringSpanW signal2 { mPropSet.GetString("signal2", L"") };
		auto it2 = std::find_if(std::begin(kSignal2Names), std::end(kSignal2Names), [=](const wchar_t *s) { return signal2 == s; });

		if (it2 != std::end(kSignal2Names))
			mComboSerialSignal2.SetSelection((int)(it2 - std::begin(kSignal2Names)));
		else
			mComboSerialSignal2.SetSelection(0);
	}
}

bool ATUIConfDevATR8000(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceATR8000 dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
