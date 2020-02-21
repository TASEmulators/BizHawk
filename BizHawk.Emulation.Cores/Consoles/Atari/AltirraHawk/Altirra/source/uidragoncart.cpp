//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2013 Avery Lee
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
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "dragoncart.h"

class ATUIDragonCartDialog : public VDDialogFrameW32 {
public:
	ATUIDragonCartDialog(ATPropertySet& props);
	~ATUIDragonCartDialog();

	void OnDestroy() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 code) override;

protected:
	void UpdateEnables();

	ATPropertySet& mProps;
};

ATUIDragonCartDialog::ATUIDragonCartDialog(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DRAGONCART)
	, mProps(props)
{
}

ATUIDragonCartDialog::~ATUIDragonCartDialog() {
}

void ATUIDragonCartDialog::OnDestroy() {
}

void ATUIDragonCartDialog::OnDataExchange(bool write) {
	if (!write) {
		ATDragonCartSettings settings;

		settings.LoadFromProps(mProps);

		SetControlTextF(IDC_NETADDR, L"%u.%u.%u.%u"
			, (settings.mNetAddr >> 24) & 0xff
			, (settings.mNetAddr >> 16) & 0xff
			, (settings.mNetAddr >>  8) & 0xff
			, (settings.mNetAddr >>  0) & 0xff
			);

		SetControlTextF(IDC_NETMASK, L"%u.%u.%u.%u"
			, (settings.mNetMask >> 24) & 0xff
			, (settings.mNetMask >> 16) & 0xff
			, (settings.mNetMask >>  8) & 0xff
			, (settings.mNetMask >>  0) & 0xff
			);

		switch(settings.mAccessMode) {
			case ATDragonCartSettings::kAccessMode_None:
				CheckButton(IDC_ACCESS_NONE, true);
				break;

			case ATDragonCartSettings::kAccessMode_HostOnly:
				CheckButton(IDC_ACCESS_HOSTONLY, true);
				break;

			case ATDragonCartSettings::kAccessMode_NAT:
				CheckButton(IDC_ACCESS_NAT, true);
				break;
		}

		if (settings.mTunnelAddr) {
			CheckButton(IDC_ACCESS_VXLAN, true);
			SetControlTextF(IDC_TUNNELADDR, L"%u.%u.%u.%u",
				(settings.mTunnelAddr >> 24) & 0xff,
				(settings.mTunnelAddr >> 16) & 0xff,
				(settings.mTunnelAddr >>  8) & 0xff,
				(settings.mTunnelAddr >>  0) & 0xff);

			if (settings.mTunnelSrcPort)
				SetControlTextF(IDC_TUNNELSRCPORT, L"%u", settings.mTunnelSrcPort);
			else
				SetControlText(IDC_TUNNELSRCPORT, L"");

			if (settings.mTunnelTgtPort)
				SetControlTextF(IDC_TUNNELTGTPORT, L"%u", settings.mTunnelTgtPort);
			else
				SetControlText(IDC_TUNNELTGTPORT, L"");
		} else {
			CheckButton(IDC_ACCESS_VXLAN, false);
		}

		if (settings.mForwardingAddr && settings.mForwardingPort) {
			SetControlTextF(IDC_FORWARDING_ADDRESS, L"%u.%u.%u.%u",
				(settings.mForwardingAddr >> 24) & 0xff,
				(settings.mForwardingAddr >> 16) & 0xff,
				(settings.mForwardingAddr >>  8) & 0xff,
				(settings.mForwardingAddr >>  0) & 0xff);
			SetControlTextF(IDC_FORWARDING_PORT, L"%u", settings.mForwardingPort);
		} else {
			SetControlText(IDC_FORWARDING_ADDRESS, L"");
			SetControlText(IDC_FORWARDING_PORT, L"");
		}

		UpdateEnables();
	} else {
		ATDragonCartSettings settings;
		VDStringW s;
		VDStringW t;

		settings.SetDefault();

		unsigned a0, a1, a2, a3;
		wchar_t c;
		if (!GetControlText(IDC_NETADDR, s) ||
			4 != swscanf(s.c_str(), L"%u.%u.%u.%u %c", &a0, &a1, &a2, &a3, &c) ||
			(a0 | a1 | a2 | a3) >= 256)
		{
			FailValidation(IDC_NETADDR, L"The network address must be an IPv4 address of the form A.B.C.D and different than your actual network address. Example: 192.168.10.0");
			return;
		}

		settings.mNetAddr = (a0 << 24) + (a1 << 16) + (a2 << 8) + a3;

		if (!GetControlText(IDC_NETMASK, s) ||
			4 != swscanf(s.c_str(), L"%u.%u.%u.%u %c", &a0, &a1, &a2, &a3, &c) ||
			(a0 | a1 | a2 | a3) >= 256)
		{
			FailValidation(IDC_NETMASK, L"The network mask must be of the form A.B.C.D. Example: 255.255.255.0");
			return;
		}

		// Netmask must have contiguous 1 bits on high end: (-mask) must be power of two
		settings.mNetMask = (a0 << 24) + (a1 << 16) + (a2 << 8) + a3;
		uint32 test = 0 - settings.mNetMask;
		if (test & (test - 1)) {
			FailValidation(IDC_NETMASK, L"The network mask is invalid. It must have contiguous 1 bits followed by contiguous 0 bits.");
			return;
		}

		if (settings.mNetAddr & ~settings.mNetMask) {
			FailValidation(IDC_NETADDR, L"The network mask is invalid for the given network address. For a class C network, the address must end in .0 and the mask must be 255.255.255.0.");
			return;
		}

		if (IsButtonChecked(IDC_ACCESS_VXLAN)) {
			GetControlText(IDC_TUNNELADDR, s);

			if (4 != swscanf(s.c_str(), L"%u.%u.%u.%u %c", &a0, &a1, &a2, &a3, &c) ||
				(a0 | a1 | a2 | a3) >= 256)
			{
				FailValidation(IDC_TUNNELADDR, L"Invalid VXLAN tunnel address: must be a valid IPv4 address on the host network of the form: A.B.C.D");
				return;
			}

			settings.mTunnelAddr = (a0 << 24) + (a1 << 16) + (a2 << 8) + a3;

			GetControlText(IDC_TUNNELSRCPORT, s);
			GetControlText(IDC_TUNNELTGTPORT, t);

			if (!s.empty()) {
				if (1 != swscanf(s.c_str(), L"%u %c", &a0, &c) || a0 > 65535) {
					FailValidation(IDC_TUNNELSRCPORT, L"Invalid VXLAN tunnel source port: must be a valid UDP port (1-65535) or 0/blank for dynamic.");
					return;
				}

				settings.mTunnelSrcPort = a0;
			}

			if (!t.empty()) {
				if (1 != swscanf(t.c_str(), L"%u %c", &a0, &c) || a0 > 65535) {
					FailValidation(IDC_TUNNELSRCPORT, L"Invalid VXLAN tunnel target port: must be a valid UDP port (1-65535) or blank for default (4789).");
					return;
				}

				settings.mTunnelTgtPort = a0;
			} else {
				settings.mTunnelTgtPort = 4789;
			}
		}
		
		if (IsButtonChecked(IDC_ACCESS_NAT)) {
			settings.mAccessMode = ATDragonCartSettings::kAccessMode_NAT;

			GetControlText(IDC_FORWARDING_ADDRESS, s);
			GetControlText(IDC_FORWARDING_PORT, t);

			if (!s.empty()) {
				if (4 != swscanf(s.c_str(), L"%u.%u.%u.%u %c", &a0, &a1, &a2, &a3, &c) ||
					(a0 | a1 | a2 | a3) >= 256)
				{
					FailValidation(IDC_FORWARDING_ADDRESS, L"Invalid forwarding address: must be blank or an IPv4 address of the form: A.B.C.D");
					return;
				}

				settings.mForwardingAddr = (a0 << 24) + (a1 << 16) + (a2 << 8) + a3;

				if ((settings.mForwardingAddr & settings.mNetMask) != settings.mNetAddr) {
					FailValidation(IDC_FORWARDING_ADDRESS, L"Invalid forwarding address: must be within on the emulation network.");
					return;
				}

				if (1 != swscanf(t.c_str(), L"%u %c", &a0, &c) || a0 < 1 || a0 > 65535) {
					FailValidation(IDC_FORWARDING_PORT, L"Invalid forwarding port: must be in the range 1-65535.");
					return;
				}

				settings.mForwardingPort = a0;
			}
		} else if (IsButtonChecked(IDC_ACCESS_HOSTONLY))
			settings.mAccessMode = ATDragonCartSettings::kAccessMode_HostOnly;
		else
			settings.mAccessMode = ATDragonCartSettings::kAccessMode_None;

		settings.SaveToProps(mProps);
	}
}

bool ATUIDragonCartDialog::OnCommand(uint32 id, uint32 code) {
	switch(id) {
		case IDC_ACCESS_VXLAN:
		case IDC_ACCESS_NAT:
		case IDC_ACCESS_HOSTONLY:
		case IDC_ACCESS_NONE:
			UpdateEnables();
			break;
	}

	return false;
}

void ATUIDragonCartDialog::UpdateEnables() {
	const bool fwEnable = IsButtonChecked(IDC_ACCESS_NAT);

	EnableControl(IDC_STATIC_FORWARDING_ADDRESS, fwEnable);
	EnableControl(IDC_STATIC_FORWARDING_PORT, fwEnable);
	EnableControl(IDC_FORWARDING_ADDRESS, fwEnable);
	EnableControl(IDC_FORWARDING_PORT, fwEnable);

	const bool tunEnable = IsButtonChecked(IDC_ACCESS_VXLAN);
	EnableControl(IDC_STATIC_TUNNELADDR, tunEnable);
	EnableControl(IDC_STATIC_TUNNELSRCPORT, tunEnable);
	EnableControl(IDC_STATIC_TUNNELTGTPORT, tunEnable);
	EnableControl(IDC_TUNNELADDR, tunEnable);
	EnableControl(IDC_TUNNELSRCPORT, tunEnable);
	EnableControl(IDC_TUNNELTGTPORT, tunEnable);
}

///////////////////////////////////////////////////////////////////////////

bool ATUIConfDevDragonCart(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDragonCartDialog dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
