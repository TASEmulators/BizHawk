//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include "simulator.h"
#include "rs232.h"

extern ATSimulator g_sim;

class ATUIDialogDevice850 : public VDDialogFrameW32 {
public:
	ATUIDialogDevice850(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);

	ATPropertySet& mPropSet;
	VDUIProxyComboBoxControl mComboSioModes;

	static const wchar_t *const kSioEmuModes[];
};

const wchar_t *const ATUIDialogDevice850::kSioEmuModes[]={
	L"None - Emulated R: handler only",
	L"Minimal - Emulated R: handler + stub loader only",
	L"Full - SIO protocol and 6502 R: handler",
};

ATUIDialogDevice850::ATUIDialogDevice850(ATPropertySet& props)
	: VDDialogFrameW32(IDD_SERIAL_PORTS)
	, mPropSet(props)
{
}

bool ATUIDialogDevice850::OnLoaded() {
	AddProxy(&mComboSioModes, IDC_SIOLEVEL);

	for(size_t i=0; i<vdcountof(kSioEmuModes); ++i)
		mComboSioModes.AddItem(kSioEmuModes[i]);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDevice850::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();
		
		if (IsButtonChecked(IDC_DISABLE_THROTTLING))
			mPropSet.SetBool("unthrottled", true);

		if (IsButtonChecked(IDC_EXTENDED_BAUD_RATES))
			mPropSet.SetBool("baudex", true);

		int sioLevel = mComboSioModes.GetSelection();
		if (sioLevel >= 0 && sioLevel < kAT850SIOEmulationLevelCount)
			mPropSet.SetUint32("emulevel", (uint32)sioLevel);
	} else {
		uint32 level = mPropSet.GetUint32("emulevel", 0);
		if (level >= kAT850SIOEmulationLevelCount)
			level = 0;

		mComboSioModes.SetSelection(level);

		CheckButton(IDC_DISABLE_THROTTLING, mPropSet.GetBool("unthrottled", false));
		CheckButton(IDC_EXTENDED_BAUD_RATES, mPropSet.GetBool("baudex", false));
	}
}

bool ATUIConfDev850(VDGUIHandle h, ATPropertySet& props) {
	ATUIDialogDevice850 dlg(props);

	return dlg.ShowDialog(h) != 0;
}
