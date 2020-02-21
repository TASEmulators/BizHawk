//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include <at/atnativeui/uiproxies.h>
#include <at/atcore/propertyset.h>

///////////////////////////////////////////////////////////////////////////

class ATUIDialogDeviceBlackBox : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceBlackBox(ATPropertySet& props);
	~ATUIDialogDeviceBlackBox();

	bool OnLoaded();
	void OnDataExchange(bool write);

protected:
	VDUIProxyListView mList;
	VDUIProxyComboBoxControl mRAMSizeCombo;
	ATPropertySet& mProps;
};

ATUIDialogDeviceBlackBox::ATUIDialogDeviceBlackBox(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_BLACKBOX)
	, mProps(props)
{
}

ATUIDialogDeviceBlackBox::~ATUIDialogDeviceBlackBox() {
}

bool ATUIDialogDeviceBlackBox::OnLoaded() {
	AddProxy(&mList, IDC_LIST);
	AddProxy(&mRAMSizeCombo, IDC_RAM_SIZE);

	mList.SetRedraw(false);
	mList.InsertColumn(0, L"", 0);
	mList.SetFullRowSelectEnabled(true);
	mList.SetItemCheckboxesEnabled(true);

	static const wchar_t *const kSwitchLabels[]={
		L"1: Ignore printer fault line",
		L"2: Enable hard disk and high speed floppy SIO (16K ROM only)",
		L"3: Enable printer port",
		L"4: Enable RS232 port",
		L"5: Enable printer linefeeds",
		L"6: ProWriter printer mode (disable for Epson)",
		L"7: MIO compatibility mode",
		L"8: Unused"
	};

	for(int i=0; i<8; ++i)
		mList.InsertItem(i, kSwitchLabels[i]);

	mList.AutoSizeColumns();
	mList.SetRedraw(true);

	mRAMSizeCombo.SetRedraw(false);

	for(const wchar_t *s : { L"8K", L"32K", L"64K" })
		mRAMSizeCombo.AddItem(s);

	mRAMSizeCombo.SetRedraw(true);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceBlackBox::OnDataExchange(bool write) {
	if (write) {
		uint32 flags = 0;

		for(int i=0; i<8; ++i) {
			if (mList.IsItemChecked(i))
				flags |= (1 << i);
		}

		mProps.Clear();
		mProps.SetUint32("dipsw", flags);
		mProps.SetUint32("blksize", IsButtonChecked(IDC_SECTOR_SIZE_512) ? 512 : 256);

		int idx = mRAMSizeCombo.GetSelection();
		mProps.SetUint32("ramsize", idx == 1 ? 32 : idx == 2 ? 64 : 8);
	} else {
		// Switches 1-4 are enabled in factory configuration, per manual
		uint32 flags = mProps.GetUint32("dipsw", 0x0F);

		for(int i=0; i<8; ++i)
			mList.SetItemChecked(i, (flags & (1 << i)) != 0);

		if (mProps.GetUint32("blksize") == 256)
			CheckButton(IDC_SECTOR_SIZE_256, true);
		else
			CheckButton(IDC_SECTOR_SIZE_512, true);

		switch(mProps.GetUint32("ramsize")) {
			case 8:
			default:
				mRAMSizeCombo.SetSelection(0);
				break;

			case 32:
				mRAMSizeCombo.SetSelection(1);
				break;

			case 64:
				mRAMSizeCombo.SetSelection(2);
				break;
		}
	}
}

bool ATUIConfDevBlackBox(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceBlackBox dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
