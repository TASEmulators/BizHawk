//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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
#include <vd2/Dita/services.h>
#include <at/atcore/propertyset.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"

class ATUIDialogPCLink : public VDDialogFrameW32 {
public:
	ATUIDialogPCLink(ATPropertySet& pset);
	~ATUIDialogPCLink();

	bool OnLoaded();
	void OnDataExchange(bool write);
	bool OnCommand(uint32 id, uint32 extcode);
	void Update(int id);

protected:
	ATPropertySet& mProps;
};

ATUIDialogPCLink::ATUIDialogPCLink(ATPropertySet& pset)
	: VDDialogFrameW32(IDD_PCLINK)
	, mProps(pset)
{
}

ATUIDialogPCLink::~ATUIDialogPCLink() {
}

bool ATUIDialogPCLink::OnLoaded() {
	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogPCLink::OnDataExchange(bool write) {
	if (!write) {
		CheckButton(IDC_ALLOW_WRITES, mProps.GetBool("write"));
		CheckButton(IDC_SET_TIMESTAMPS, mProps.GetBool("set_timestamps"));
		SetControlText(IDC_PATH, mProps.GetString("path", L""));
	} else {
		mProps.Clear();

		if (IsButtonChecked(IDC_ALLOW_WRITES))
			mProps.SetBool("write", true);

		if (IsButtonChecked(IDC_SET_TIMESTAMPS))
			mProps.SetBool("set_timestamps", true);

		VDStringW path;
		GetControlText(IDC_PATH, path);
		mProps.SetString("path", path.c_str());
	}
}

bool ATUIDialogPCLink::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_BROWSE:
			{
				VDStringW s(VDGetDirectory('pclk', (VDGUIHandle)mhdlg, L"Select base directory"));
				if (!s.empty())
					SetControlText(IDC_PATH, s.c_str());
			}
			return true;
	}

	return false;
}

bool ATUIConfDevPCLink(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogPCLink dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
