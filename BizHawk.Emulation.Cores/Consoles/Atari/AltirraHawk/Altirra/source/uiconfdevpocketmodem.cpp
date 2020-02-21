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

class ATUIDialogDevicePocketModem : public VDDialogFrameW32 {
public:
	ATUIDialogDevicePocketModem(ATPropertySet& props);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
	bool OnCommand(uint32 id, uint32 extcode);
	void UpdateEnables();

	ATPropertySet& mPropSet;
	bool mbAccept;
	bool mbOutbound;
	bool mbTelnet;
	VDUIProxyComboBoxControl mComboTermType;

	static const wchar_t *const kTerminalTypes[];
};

const wchar_t *const ATUIDialogDevicePocketModem::kTerminalTypes[]={
	// RFC 1010, and now IANA, maintains a list of terminal types to be used
	// with Telnet terminal type negotation (RFC 1091). This is intended to
	// provide a list of common names... which of course, modern systems
	// gladly ignore. CentOS 5.2 doesn't even allow DEC-VT100, requiring
	// VT100 instead.
	//
	// Note that the terminal names must be sent in uppercase. This conversion
	// is done in the Telnet module.

	L"ansi",
	L"dec-vt52",
	L"dec-vt100",
	L"vt52",
	L"vt100",
	L"vt102",
	L"vt320",
};

ATUIDialogDevicePocketModem::ATUIDialogDevicePocketModem(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_POCKETMODEM)
	, mPropSet(props)
{
}

bool ATUIDialogDevicePocketModem::OnLoaded() {
	AddProxy(&mComboTermType, IDC_TERMINAL_TYPE);

	mComboTermType.AddItem(L"(None)");

	for(size_t i=0; i<vdcountof(kTerminalTypes); ++i)
		mComboTermType.AddItem(kTerminalTypes[i]);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDevicePocketModem::OnDataExchange(bool write) {
	if (write) {
		if (IsButtonChecked(IDC_ACCEPT_CONNECTIONS)) {
			uint32 port = GetControlValueUint32(IDC_LISTEN_PORT);

			if (port < 1 || port > 65535) {
				FailValidation(IDC_LISTEN_PORT);
				return;
			}

			mPropSet.SetUint32("port", port);
		}

		mPropSet.SetBool("outbound", mbOutbound);

		int termIdx = mComboTermType.GetSelection();

		if (termIdx != 0) {
			VDStringW s;

			GetControlText(IDC_TERMINAL_TYPE, s);
			mPropSet.SetString("termtype", s.c_str());
		}

		mPropSet.SetBool("telnet", IsButtonChecked(IDC_TELNET));
		mPropSet.SetBool("telnetlf", IsButtonChecked(IDC_TELNET_LFCONVERSION));
		mPropSet.SetBool("ipv6", IsButtonChecked(IDC_ACCEPT_IPV6));

		VDStringW address;
		GetControlText(IDC_DIAL_ADDRESS, address);
		if (!address.empty())
			mPropSet.SetString("dialaddr", address.c_str());

		VDStringW service;
		GetControlText(IDC_DIAL_SERVICE, service);
		if (!service.empty())
			mPropSet.SetString("dialsvc", service.c_str());
	} else {
		const uint32 port = mPropSet.GetUint32("port");
		mbAccept = port > 0;
		mbTelnet = mPropSet.GetBool("telnet", true);
		mbOutbound = mPropSet.GetBool("outbound", true);

		const wchar_t *termType = mPropSet.GetString("termtype");
		if (termType && *termType) {
			int termIdx = 0;

			for(size_t i=0; i<vdcountof(kTerminalTypes); ++i) {
				if (!wcscmp(termType, kTerminalTypes[i]))
					termIdx = (int)(i + 1);
			}

			if (termIdx)
				mComboTermType.SetSelection(termIdx);
			else {
				mComboTermType.SetSelection(-1);
				SetControlText(IDC_TERMINAL_TYPE, termType);
			}
		} else
			mComboTermType.SetSelection(0);

		CheckButton(IDC_TELNET, mbTelnet);
		CheckButton(IDC_TELNET_LFCONVERSION, mPropSet.GetBool("telnetlf", true));
		CheckButton(IDC_ALLOW_OUTBOUND, mbOutbound);
		CheckButton(IDC_ACCEPT_IPV6, mPropSet.GetBool("ipv6", true));

		CheckButton(IDC_ACCEPT_CONNECTIONS, mbAccept);
		SetControlTextF(IDC_LISTEN_PORT, L"%u", port ? port : 9000);

		SetControlText(IDC_DIAL_ADDRESS, mPropSet.GetString("dialaddr", L""));
		SetControlText(IDC_DIAL_SERVICE, mPropSet.GetString("dialsvc", L""));

		UpdateEnables();
	}
}

bool ATUIDialogDevicePocketModem::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_TELNET) {
		bool telnet = IsButtonChecked(IDC_TELNET);

		if (mbTelnet != telnet) {
			mbTelnet = telnet;

			UpdateEnables();
		}
	} else if (id == IDC_ACCEPT_CONNECTIONS) {
		bool accept = IsButtonChecked(IDC_ACCEPT_CONNECTIONS);

		if (mbAccept != accept) {
			mbAccept = accept;

			UpdateEnables();
		}
	} else if (id == IDC_ALLOW_OUTBOUND) {
		bool outbound = IsButtonChecked(IDC_ALLOW_OUTBOUND);

		if (mbOutbound != outbound) {
			mbOutbound = outbound;

			UpdateEnables();
		}
	}

	return false;
}

void ATUIDialogDevicePocketModem::UpdateEnables() {
	bool accept = mbAccept;
	bool telnet = mbTelnet;

	EnableControl(IDC_TELNET_LFCONVERSION, telnet);
	EnableControl(IDC_STATIC_TERMINAL_TYPE, mbOutbound);
	EnableControl(IDC_TERMINAL_TYPE, mbOutbound);
	EnableControl(IDC_LISTEN_PORT, accept);
	EnableControl(IDC_ACCEPT_IPV6, accept);
}

bool ATUIConfDevPocketModem(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDevicePocketModem dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
