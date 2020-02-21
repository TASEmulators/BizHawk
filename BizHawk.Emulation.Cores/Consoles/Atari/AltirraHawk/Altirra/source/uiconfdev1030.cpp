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
#include "uiconfmodem.h"

class ATUIDialogDevice1030 : public VDDialogFrameW32 {
public:
	ATUIDialogDevice1030(ATPropertySet& props);

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
	VDUIProxyComboBoxControl mComboSioModes;
	VDUIProxyComboBoxControl mComboNetworkMode;

	static const uint32 kConnectionSpeeds[];

	static const wchar_t *const kSioEmuModes[];
};

const wchar_t *const ATUIDialogDevice1030::kSioEmuModes[]={
	L"None - Emulated T: handler only",
	L"Minimal - Emulated T: handler + stub loader only",
	L"Full - SIO protocol and 6502 T: handler",
};

ATUIDialogDevice1030::ATUIDialogDevice1030(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_1030MODEM)
	, mPropSet(props)
{
}

bool ATUIDialogDevice1030::OnLoaded() {
	AddProxy(&mComboTermType, IDC_TERMINAL_TYPE);
	AddProxy(&mComboNetworkMode, IDC_NETWORKMODE);
	AddProxy(&mComboSioModes, IDC_SIOLEVEL);

	ATUIPopulateModemTermTypeList(mComboTermType);
	ATUIPopulateModemNetworkModeList(mComboNetworkMode);

	for(size_t i=0; i<vdcountof(kSioEmuModes); ++i)
		mComboSioModes.AddItem(kSioEmuModes[i]);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDevice1030::OnDataExchange(bool write) {
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
		mPropSet.SetBool("telnet", IsButtonChecked(IDC_TELNET));
		mPropSet.SetBool("telnetlf", IsButtonChecked(IDC_TELNET_LFCONVERSION));
		mPropSet.SetBool("ipv6", IsButtonChecked(IDC_ACCEPT_IPV6));
		mPropSet.SetBool("unthrottled", IsButtonChecked(IDC_DISABLE_THROTTLING));

		int sioLevel = mComboSioModes.GetSelection();
		mPropSet.SetUint32("emulevel", sioLevel);

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

		CheckButton(IDC_TELNET, mbTelnet);
		CheckButton(IDC_TELNET_LFCONVERSION, mPropSet.GetBool("telnetlf", true));
		CheckButton(IDC_ALLOW_OUTBOUND, mbOutbound);
		CheckButton(IDC_ACCEPT_IPV6, mPropSet.GetBool("ipv6", true));
		CheckButton(IDC_DISABLE_THROTTLING, mPropSet.GetBool("unthrottled", false));

		mComboSioModes.SetSelection(mPropSet.GetUint32("emulevel", 0));

		CheckButton(IDC_ACCEPT_CONNECTIONS, mbAccept);
		SetControlTextF(IDC_LISTEN_PORT, L"%u", port ? port : 9000);

		SetControlText(IDC_DIAL_ADDRESS, mPropSet.GetString("dialaddr", L""));
		SetControlText(IDC_DIAL_SERVICE, mPropSet.GetString("dialsvc", L""));

		UpdateEnables();
	}

	ATUIExchangeModemTermTypeList(write, mPropSet, mComboTermType);
	ATUIExchangeModemNetworkModeList(write, mPropSet, mComboNetworkMode);
}

bool ATUIDialogDevice1030::OnCommand(uint32 id, uint32 extcode) {
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

void ATUIDialogDevice1030::UpdateEnables() {
	bool accept = mbAccept;
	bool telnet = mbTelnet;

	EnableControl(IDC_TELNET_LFCONVERSION, telnet);
	EnableControl(IDC_STATIC_TERMINAL_TYPE, mbOutbound);
	EnableControl(IDC_TERMINAL_TYPE, mbOutbound);
	EnableControl(IDC_LISTEN_PORT, accept);
	EnableControl(IDC_ACCEPT_IPV6, accept);
}

bool ATUIConfDev1030(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDevice1030 dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
