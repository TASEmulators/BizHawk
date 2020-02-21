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
#include <at/atcore/propertyset.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include <at/atnativeui/uiproxies.h>
#include <at/atcore/propertyset.h>
#include "rs232.h"
#include "uiconfmodem.h"

static constexpr const wchar_t *kATModemTerminalTypes[]={
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

void ATUIPopulateModemTermTypeList(VDUIProxyComboBoxControl& ctl) {
	ctl.AddItem(L"(None)");

	for(const wchar_t *termtype : kATModemTerminalTypes)
		ctl.AddItem(termtype);
}

void ATUIExchangeModemTermTypeList(bool write, ATPropertySet& pset, VDUIProxyComboBoxControl& ctl) {
	if (write) {
		int termIdx = ctl.GetSelection();

		if (termIdx != 0) {
			const VDStringW& s = ctl.GetCaption();
			pset.SetString("termtype", s.c_str());
		}
	} else {
		const wchar_t *termType = pset.GetString("termtype");
		if (termType && *termType) {
			int termIdx = 0;

			for(size_t i=0; i<vdcountof(kATModemTerminalTypes); ++i) {
				if (!wcscmp(termType, kATModemTerminalTypes[i]))
					termIdx = (int)i + 1;
			}

			if (termIdx)
				ctl.SetSelection(termIdx);
			else {
				ctl.SetSelection(-1);
				ctl.SetCaption(termType);
			}
		} else
			ctl.SetSelection(0);
	}
}

const wchar_t *const kATUINetworkModeLabels[]={
	L"Disabled - no audio or delays",
	L"Minimal - simulate dialing but skip handshaking phase",
	L"Full - simulate dialing and handshaking",
};

const wchar_t *const kATUINetworkModeValues[]={
	L"none",
	L"minimal",
	L"full"
};

void ATUIPopulateModemNetworkModeList(VDUIProxyComboBoxControl& ctl) {
	for(const wchar_t *s : kATUINetworkModeLabels)
		ctl.AddItem(s);
}

void ATUIExchangeModemNetworkModeList(bool write, ATPropertySet& pset, VDUIProxyComboBoxControl& ctl) {
	if (write) {
		int netModeIdx = ctl.GetSelection();
		if ((unsigned)netModeIdx < vdcountof(kATUINetworkModeValues))
			pset.SetString("netmode", kATUINetworkModeValues[netModeIdx]);
	} else {
		const wchar_t *netmode = pset.GetString("netmode", L"full");
		int netmodeidx = 2;		// default to full
		for(size_t i=0; i<vdcountof(kATUINetworkModeValues); ++i) {
			if (!wcscmp(netmode, kATUINetworkModeValues[i])) {
				netmodeidx = i;
				break;
			}
		}

		ctl.SetSelection(netmodeidx);
	}
}

class ATUIDialogDeviceModem : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceModem(ATPropertySet& pset);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
	bool OnCommand(uint32 id, uint32 extcode);
	void UpdateEnables();

	ATPropertySet& mPropSet;
	bool mbAccept;
	bool mbOutbound;
	bool mbTelnet;
	VDUIProxyComboBoxControl mComboConnectSpeed;
	VDUIProxyComboBoxControl mComboTermType;
	VDUIProxyComboBoxControl mComboNetworkMode;

	static const uint32 kConnectionSpeeds[];
};

const uint32 ATUIDialogDeviceModem::kConnectionSpeeds[]={
	300,
	600,
	1200,
	2400,
	4800,
	7200,
	9600,
	12000,
	14400,
	19200,
	38400,
	57600,
	115200,
	230400
};

ATUIDialogDeviceModem::ATUIDialogDeviceModem(ATPropertySet& pset)
	: VDDialogFrameW32(IDD_DEVICE_MODEM)
	, mPropSet(pset)
{
}

bool ATUIDialogDeviceModem::OnLoaded() {
	AddProxy(&mComboConnectSpeed, IDC_CONNECTION_SPEED);
	AddProxy(&mComboTermType, IDC_TERMINAL_TYPE);
	AddProxy(&mComboNetworkMode, IDC_NETWORKMODE);

	VDStringW s;

	for(uint32 speed : kConnectionSpeeds) {
		s.sprintf(L"%u baud", speed);
		mComboConnectSpeed.AddItem(s.c_str());
	}

	ATUIPopulateModemTermTypeList(mComboTermType);
	ATUIPopulateModemNetworkModeList(mComboNetworkMode);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceModem::OnDataExchange(bool write) {
	if (write) {
		mPropSet.Clear();

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
		mPropSet.SetBool("check_rate", IsButtonChecked(IDC_REQUIRE_MATCHED_DTE_RATE));

		int selIdx = mComboConnectSpeed.GetSelection();
		mPropSet.SetUint32("connect_rate", selIdx >= 0 ? kConnectionSpeeds[selIdx] : 9600);

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

		CheckButton(IDC_ACCEPT_CONNECTIONS, mbAccept);
		SetControlTextF(IDC_LISTEN_PORT, L"%u", port ? port : 9000);

		const uint32 connectRate = mPropSet.GetUint32("connect_rate", 9600);
		const uint32 *begin = kConnectionSpeeds;
		const uint32 *end = kConnectionSpeeds + sizeof(kConnectionSpeeds)/sizeof(kConnectionSpeeds[0]);
		const uint32 *it = std::lower_bound(begin, end, connectRate);

		if (it == end)
			--it;

		if (it != begin && connectRate - it[-1] < it[0] - connectRate)
			--it;

		mComboConnectSpeed.SetSelection((int)(it - begin));

		CheckButton(IDC_REQUIRE_MATCHED_DTE_RATE, mPropSet.GetBool("check_rate", false));

		SetControlText(IDC_DIAL_ADDRESS, mPropSet.GetString("dialaddr", L""));
		SetControlText(IDC_DIAL_SERVICE, mPropSet.GetString("dialsvc", L""));

		UpdateEnables();
	}

	ATUIExchangeModemTermTypeList(write, mPropSet, mComboTermType);
	ATUIExchangeModemNetworkModeList(write, mPropSet, mComboNetworkMode);
}

bool ATUIDialogDeviceModem::OnCommand(uint32 id, uint32 extcode) {
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

void ATUIDialogDeviceModem::UpdateEnables() {
	bool accept = mbAccept;
	bool telnet = mbTelnet;

	EnableControl(IDC_TELNET_LFCONVERSION, telnet);
	EnableControl(IDC_STATIC_TERMINAL_TYPE, mbOutbound);
	EnableControl(IDC_TERMINAL_TYPE, mbOutbound);
	EnableControl(IDC_LISTEN_PORT, accept);
	EnableControl(IDC_ACCEPT_IPV6, accept);
}

bool ATUIConfDevModem(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceModem dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
