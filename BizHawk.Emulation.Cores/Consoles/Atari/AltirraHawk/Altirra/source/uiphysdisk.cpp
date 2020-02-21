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
#include <windows.h>
#include <commctrl.h>
#include <setupapi.h>
#include <winioctl.h>
#include <vd2/system/error.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"
#include "ide.h"
#include "simulator.h"

extern ATSimulator g_sim;

class ATUIDialogBrowsePhysicalDisks : public VDDialogFrameW32 {
public:
	ATUIDialogBrowsePhysicalDisks();
	~ATUIDialogBrowsePhysicalDisks();

	const wchar_t *GetDevicePath() const { return mDevicePath.c_str(); }

	bool OnLoaded();
	void OnDestroy();
	void OnDataExchange(bool write);

protected:
	void OnSelectionChanged(VDUIProxyTreeViewControl *sender, int);
	void UpdateEnables();
	void LoadTree();

	VDUIProxyTreeViewControl mTreeView;
	VDStringW mDevicePath;
	VDDelegate mDelSelChanged;

	class TreeItem : public vdrefcounted<IVDUITreeViewVirtualItem> {
	public:
		void *AsInterface(uint32 iid) {
			return NULL;
		}

		virtual void GetText(VDStringW& s) const {
			s = mText;
		}

		VDStringW mText;
		VDStringW mPath;
	};
};

ATUIDialogBrowsePhysicalDisks::ATUIDialogBrowsePhysicalDisks()
	: VDDialogFrameW32(IDD_SELECT_PHYSICAL_DISK)
{
	mTreeView.OnItemSelectionChanged() += mDelSelChanged.Bind(this, &ATUIDialogBrowsePhysicalDisks::OnSelectionChanged);
}

ATUIDialogBrowsePhysicalDisks::~ATUIDialogBrowsePhysicalDisks() {
}

bool ATUIDialogBrowsePhysicalDisks::OnLoaded() {
	AddProxy(&mTreeView, IDC_TREE);

	LoadTree();	

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogBrowsePhysicalDisks::OnDestroy() {
	mTreeView.Clear();
}

void ATUIDialogBrowsePhysicalDisks::OnDataExchange(bool write) {
	if (write) {
		TreeItem *item = static_cast<TreeItem *>(mTreeView.GetSelectedVirtualItem());

		if (item)
			mDevicePath = item->mPath;
	}
}

void ATUIDialogBrowsePhysicalDisks::OnSelectionChanged(VDUIProxyTreeViewControl *sender, int) {
	UpdateEnables();
}

void ATUIDialogBrowsePhysicalDisks::UpdateEnables() {
	EnableControl(IDOK, mTreeView.GetSelectedVirtualItem() != NULL);
}

void ATUIDialogBrowsePhysicalDisks::LoadTree() {
	HMODULE hmodSetupApi = VDLoadSystemLibraryW32("setupapi");

	if (!hmodSetupApi)
		return;

	typedef HDEVINFO (WINAPI *tpSetupDiGetClassDevsW)(const GUID* ClassGuid, PCWSTR Enumerator, HWND hwndParent, DWORD Flags);
	typedef BOOL (WINAPI *tpSetupDiEnumDeviceInterfaces)(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, const GUID* InterfaceClassGuid, DWORD MemberIndex, PSP_DEVICE_INTERFACE_DATA DeviceInterfaceData);
	typedef BOOL (WINAPI *tpSetupDiGetDeviceRegistryPropertyW)(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, DWORD Property, PDWORD PropertyRegDataType, PBYTE PropertyBuffer, DWORD PropertyBufferSize, PDWORD RequiredSize);
	typedef BOOL (WINAPI *tpSetupDiGetDeviceInterfaceDetailW)(HDEVINFO DeviceInfoSet, PSP_DEVICE_INTERFACE_DATA DeviceInterfaceData, PSP_DEVICE_INTERFACE_DETAIL_DATA_W DeviceInterfaceDetailData, DWORD DeviceInterfaceDetailDataSize, PDWORD RequiredSize, PSP_DEVINFO_DATA DeviceInfoData);
	typedef BOOL (WINAPI *tpSetupDiDestroyDeviceInfoList)(HDEVINFO DeviceInfoSet);

	tpSetupDiGetClassDevsW pSetupDiGetClassDevsW = (tpSetupDiGetClassDevsW)GetProcAddress(hmodSetupApi, "SetupDiGetClassDevsW");
	tpSetupDiEnumDeviceInterfaces pSetupDiEnumDeviceInterfaces = (tpSetupDiEnumDeviceInterfaces)GetProcAddress(hmodSetupApi, "SetupDiEnumDeviceInterfaces");
	tpSetupDiGetDeviceRegistryPropertyW pSetupDiGetDeviceRegistryPropertyW = (tpSetupDiGetDeviceRegistryPropertyW)GetProcAddress(hmodSetupApi, "SetupDiGetDeviceRegistryPropertyW");
	tpSetupDiGetDeviceInterfaceDetailW pSetupDiGetDeviceInterfaceDetailW = (tpSetupDiGetDeviceInterfaceDetailW)GetProcAddress(hmodSetupApi, "SetupDiGetDeviceInterfaceDetailW");
	tpSetupDiDestroyDeviceInfoList pSetupDiDestroyDeviceInfoList = (tpSetupDiDestroyDeviceInfoList)GetProcAddress(hmodSetupApi, "SetupDiDestroyDeviceInfoList");

	if (!pSetupDiGetClassDevsW || !pSetupDiEnumDeviceInterfaces || !pSetupDiGetDeviceRegistryPropertyW || !pSetupDiGetDeviceInterfaceDetailW || !pSetupDiDestroyDeviceInfoList) {
		FreeLibrary(hmodSetupApi);
		return;
	}

	vdstructex<SP_DEVICE_INTERFACE_DETAIL_DATA_W> detail;
	typedef vdhashmap<int, VDUIProxyTreeViewControl::NodeRef> DeviceNodes;
	DeviceNodes deviceNodes;

	// enumerate devices
	HDEVINFO hdi = pSetupDiGetClassDevsW(&GUID_DEVINTERFACE_DISK, NULL, NULL, DIGCF_PRESENT | DIGCF_INTERFACEDEVICE);

	if (hdi) {
		for(int index = 0; ; ++index) {
			SP_DEVICE_INTERFACE_DATA spdi = {sizeof(SP_DEVICE_INTERFACE_DATA)};
			if (!pSetupDiEnumDeviceInterfaces(hdi, NULL, &GUID_DEVINTERFACE_DISK, index, &spdi))
				break;

			DWORD reqSize;
			if (!pSetupDiGetDeviceInterfaceDetailW(hdi, &spdi, NULL, 0, &reqSize, NULL) && GetLastError() != ERROR_INSUFFICIENT_BUFFER)
				break;

			detail.resize(reqSize);
			detail->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W);

			SP_DEVINFO_DATA spdd = {sizeof(SP_DEVINFO_DATA)};
			if (!pSetupDiGetDeviceInterfaceDetailW(hdi, &spdi, &*detail, reqSize, &reqSize, &spdd))
				break;

			vdrefptr<TreeItem> item(new TreeItem);

			item->mPath = detail->DevicePath;

			DWORD type;
			DWORD reqsize;
			WCHAR tmp[MAX_PATH];
			if (pSetupDiGetDeviceRegistryPropertyW(hdi, &spdd, SPDRP_DEVICEDESC, &type, (PBYTE)tmp, sizeof(tmp), &reqsize) && type == REG_SZ)
				item->mText = tmp;
			else
				item->mText = L"Unknown device";

			if (pSetupDiGetDeviceRegistryPropertyW(hdi, &spdd, SPDRP_FRIENDLYNAME, &type, (PBYTE)tmp, sizeof(tmp), &reqsize) && type == REG_SZ) {
				item->mText += L" (";
				item->mText += tmp;
				item->mText += L")";
			}

			VDUIProxyTreeViewControl::NodeRef nodeRef = mTreeView.AddVirtualItem(VDUIProxyTreeViewControl::kNodeRoot, VDUIProxyTreeViewControl::kNodeLast, item);

			HANDLE h = CreateFileW(detail->DevicePath, 0*GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
			if (h != INVALID_HANDLE_VALUE) {
				STORAGE_DEVICE_NUMBER sdn = {0};
				DWORD r;
				if (DeviceIoControl(h, IOCTL_STORAGE_GET_DEVICE_NUMBER, NULL, 0, &sdn, sizeof(sdn), &r, NULL)) {
					deviceNodes[sdn.DeviceNumber] = nodeRef;
				}

				CloseHandle(h);
			}
		}

		pSetupDiDestroyDeviceInfoList(hdi);
	}

	// enumerate volumes
	hdi = pSetupDiGetClassDevsW(&GUID_DEVINTERFACE_VOLUME, NULL, NULL, DIGCF_PRESENT | DIGCF_INTERFACEDEVICE);

	if (hdi) {
		for(int index = 0; ; ++index) {
			SP_DEVICE_INTERFACE_DATA spdi = {sizeof(SP_DEVICE_INTERFACE_DATA)};
			if (!pSetupDiEnumDeviceInterfaces(hdi, NULL, &GUID_DEVINTERFACE_VOLUME, index, &spdi))
				break;

			DWORD reqSize;
			SP_DEVINFO_DATA spdd = {sizeof(SP_DEVINFO_DATA)};
			if (!pSetupDiGetDeviceInterfaceDetailW(hdi, &spdi, NULL, 0, &reqSize, NULL) && GetLastError() != ERROR_INSUFFICIENT_BUFFER)
				break;

			detail.resize(reqSize);
			detail->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA_W);
			if (!pSetupDiGetDeviceInterfaceDetailW(hdi, &spdi, &*detail, reqSize, &reqSize, &spdd))
				break;

			vdrefptr<TreeItem> item(new TreeItem);

			item->mPath = detail->DevicePath;
			item->mText = L"Unknown volume";

			int deviceNumber = -1;

			HANDLE h = CreateFileW(detail->DevicePath, 0*GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
			if (h != INVALID_HANDLE_VALUE) {
				STORAGE_DEVICE_NUMBER sdn = {0};
				DWORD r;
				if (DeviceIoControl(h, IOCTL_STORAGE_GET_DEVICE_NUMBER, NULL, 0, &sdn, sizeof(sdn), &r, NULL)) {
					item->mText.sprintf(L"Partition %u", sdn.PartitionNumber);
					if (sdn.DeviceType == FILE_DEVICE_DISK)
						deviceNumber = sdn.DeviceNumber;
				}

				CloseHandle(h);
			}

			VDUIProxyTreeViewControl::NodeRef parent = VDUIProxyTreeViewControl::kNodeRoot;

			if (deviceNumber >= 0) {
				DeviceNodes::const_iterator it(deviceNodes.find(deviceNumber));

				if (it != deviceNodes.end())
					parent = it->second;
			}

			DWORD type;
			DWORD reqsize;
			WCHAR tmp[MAX_PATH];

			if (pSetupDiGetDeviceRegistryPropertyW(hdi, &spdd, SPDRP_FRIENDLYNAME, &type, (PBYTE)tmp, sizeof(tmp), &reqsize) && type == REG_SZ) {
				item->mText = tmp;
			}

			VDStringW volumePath(detail->DevicePath);

			volumePath += L'\\';
			
			WCHAR tmp2[MAX_PATH];

			if (GetVolumeNameForVolumeMountPointW(volumePath.c_str(), tmp2, MAX_PATH)) {
				DWORD len;
				if (!GetVolumePathNamesForVolumeNameW(tmp2, NULL, 0, &len) && GetLastError() == ERROR_MORE_DATA) {
					vdfastvector<WCHAR> paths(len);

					if (GetVolumePathNamesForVolumeNameW(tmp2, paths.data(), len, &len)) {
						if (paths[0]) {
							item->mText += L" (";
							item->mText += paths.data();
							item->mText += L")";
						}
					}
				}
			}

			mTreeView.AddVirtualItem(parent, VDUIProxyTreeViewControl::kNodeLast, item);
		}

		pSetupDiDestroyDeviceInfoList(hdi);
	}

	FreeLibrary(hmodSetupApi);
}

VDStringW ATUIShowDialogBrowsePhysicalDisks(VDGUIHandle hParent) {
	ATUIDialogBrowsePhysicalDisks dlg;

	if (dlg.ShowDialog(hParent))
		return VDStringW(dlg.GetDevicePath());

	return VDStringW();
}
