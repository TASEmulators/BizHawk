//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - serial emulation options UI
//	Copyright (C) 2009-2015 Avery Lee
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
#include <setupapi.h>
#include <winioctl.h>
#include <vd2/system/error.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"
#include "serialconfig.h"

class ATSUIDialogSerialOptions final : public VDDialogFrameW32 {
public:
	ATSUIDialogSerialOptions(ATSSerialConfig& cfg);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 code) override;

	void FindSerialPorts();
	void UpdateEnables();

	ATSSerialConfig& mConfig;

	VDUIProxyComboBoxControl mComboSerialPort;
	VDUIProxyComboBoxControl mComboBaudRate;

	struct PortInfo {
		VDStringW mPath;
		VDStringW mFriendlyName;
		VDStringW mDesc;
	};

	vdvector<PortInfo> mPorts;
};

ATSUIDialogSerialOptions::ATSUIDialogSerialOptions(ATSSerialConfig& cfg)
	: VDDialogFrameW32(IDD_SERIAL_OPTIONS)
	, mConfig(cfg)
{
}

bool ATSUIDialogSerialOptions::OnLoaded() {
	AddProxy(&mComboSerialPort, IDC_SERIAL_PORT);
	AddProxy(&mComboBaudRate, IDC_BAUDRATE);

	FindSerialPorts();

	for(const auto& port : mPorts)
		mComboSerialPort.AddItem(port.mFriendlyName.c_str());

	mComboSerialPort.SetSelection(0);

	mComboBaudRate.AddItem(L"38400 baud (2x)");
	mComboBaudRate.AddItem(L"57600 baud (3x)");

	return VDDialogFrameW32::OnLoaded();
}

void ATSUIDialogSerialOptions::OnDataExchange(bool write) {
	if (write) {
		int index = mComboSerialPort.GetSelection();

		if (index >= 0 && (unsigned)index < mPorts.size())
			mConfig.mSerialPath = mPorts[index].mPath;

		if (IsButtonChecked(IDC_HS_DISABLED)) {
			mConfig.mHighSpeedMode = mConfig.kHighSpeed_Disabled;
		} else if (IsButtonChecked(IDC_HS_STANDARD)) {
			mConfig.mHighSpeedMode = mConfig.kHighSpeed_Standard;

			mConfig.mHSBaudRate = mComboBaudRate.GetSelection() ? 57600 : 38400;
		} else {
			mConfig.mHighSpeedMode = mConfig.kHighSpeed_PokeyDivisor;

			mConfig.mHSPokeyDivisor = GetControlValueUint32(IDC_DIVISOR);
			if (mConfig.mHSPokeyDivisor >= 47) {
				FailValidation(IDC_DIVISOR, L"High-speed POKEY divisor must be between 0 and 46.");
				return;
			}
		}
	} else {
		if (!mConfig.mSerialPath.empty()) {
			int index = 0;

			for(const PortInfo& port : mPorts) {
				if (!port.mPath.comparei(mConfig.mSerialPath)) {
					mComboSerialPort.SetSelection(index);
					break;
				}

				++index;
			}
		}

		mComboBaudRate.SetSelection(mConfig.mHSBaudRate == 57600 ? 1 : 0);
		
		SetControlTextF(IDC_DIVISOR, L"%u", mConfig.mHSPokeyDivisor);

		switch(mConfig.mHighSpeedMode) {
			case ATSSerialConfig::kHighSpeed_Disabled:
			default:
				CheckButton(IDC_HS_DISABLED, true);
				break;

			case ATSSerialConfig::kHighSpeed_Standard:
				CheckButton(IDC_HS_STANDARD, true);
				break;

			case ATSSerialConfig::kHighSpeed_PokeyDivisor:
				CheckButton(IDC_HS_CUSTOM, true);
				break;
		}

		UpdateEnables();
	}
}

bool ATSUIDialogSerialOptions::OnCommand(uint32 id, uint32 code) {
	switch(id) {
		case IDC_HS_DISABLED:
		case IDC_HS_STANDARD:
		case IDC_HS_CUSTOM:
			UpdateEnables();
			break;
	}

	return false;
}

void ATSUIDialogSerialOptions::FindSerialPorts() {
	HDEVINFO hdi = SetupDiGetClassDevs(&GUID_DEVINTERFACE_COMPORT, NULL, NULL, DIGCF_INTERFACEDEVICE | DIGCF_PRESENT);

	if (!hdi)
		return;

	vdblock<char> buf(4096);

	DWORD idx = 0;
	for(;;) {
		SP_DEVICE_INTERFACE_DATA data = {sizeof(SP_DEVICE_INTERFACE_DATA)};
		if (!SetupDiEnumDeviceInterfaces(hdi, NULL, &GUID_DEVINTERFACE_COMPORT, idx++, &data))
			break;

		PortInfo portInfo;

		((SP_DEVICE_INTERFACE_DETAIL_DATA *)buf.data())->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);
		SP_DEVINFO_DATA spdd = {sizeof(SP_DEVINFO_DATA)};
		DWORD reqSize = 0;
		if (!SetupDiGetDeviceInterfaceDetail(hdi, &data, (SP_DEVICE_INTERFACE_DETAIL_DATA *)buf.data(), buf.size(), &reqSize, &spdd)) {
			if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
				continue;

			buf.resize(reqSize);
			((SP_DEVICE_INTERFACE_DETAIL_DATA *)buf.data())->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);
			if (!SetupDiGetDeviceInterfaceDetail(hdi, &data, (SP_DEVICE_INTERFACE_DETAIL_DATA *)buf.data(), buf.size(), NULL, &spdd))
				continue;
		}

		portInfo.mPath = ((SP_DEVICE_INTERFACE_DETAIL_DATA *)buf.data())->DevicePath;

		DWORD type;
		DWORD reqsize;
		if (!SetupDiGetDeviceRegistryPropertyW(hdi, &spdd, SPDRP_DEVICEDESC, &type, (PBYTE)buf.data(), buf.size(), &reqsize) || type != REG_SZ)
			continue;

		portInfo.mDesc = (LPCWSTR)buf.data();

		if (!SetupDiGetDeviceRegistryPropertyW(hdi, &spdd, SPDRP_FRIENDLYNAME, &type, (PBYTE)buf.data(), buf.size(), &reqsize) || type != REG_SZ)
			continue;

		portInfo.mFriendlyName = (LPCWSTR)buf.data();

		mPorts.push_back(portInfo);
	}

	SetupDiDestroyDeviceInfoList(hdi);
}

void ATSUIDialogSerialOptions::UpdateEnables() {
	EnableControl(IDC_BAUDRATE, IsButtonChecked(IDC_HS_STANDARD));
	EnableControl(IDC_DIVISOR, IsButtonChecked(IDC_HS_CUSTOM));
}

///////////////////////////////////////////////////////////////////////////

bool ATSUIShowDialogSerialOptions(VDGUIHandle hParent, ATSSerialConfig& cfg) {
	ATSUIDialogSerialOptions dlg(cfg);

	return dlg.ShowDialog(hParent) != 0;
}
