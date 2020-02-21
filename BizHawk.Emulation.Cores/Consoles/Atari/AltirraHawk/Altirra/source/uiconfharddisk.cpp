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
#include <windows.h>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "idephysdisk.h"
#include "idevhdimage.h"
#include "oshelper.h"
#include <at/atnativeui/uiproxies.h>
#include <at/atcore/propertyset.h>

#ifndef BCM_SETSHIELD
#define BCM_SETSHIELD	0x160C
#endif

VDStringW ATUIShowDialogBrowsePhysicalDisks(VDGUIHandle hParent);
void ATCreateDeviceHardDisk(const ATPropertySet& pset, IATDevice **dev);

///////////////////////////////////////////////////////////////////////////

class ATUIDialogCreateVHDImage2 : public VDDialogFrameW32 {
public:
	ATUIDialogCreateVHDImage2();
	~ATUIDialogCreateVHDImage2();

	uint32 GetSectorCount() const { return mSectorCount; }
	const wchar_t *GetPath() const { return mPath.c_str(); }

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
	bool OnOK();
	bool OnCommand(uint32 id, uint32 extcode);
	void UpdateGeometry();
	void UpdateEnables();

	VDStringW mPath;
	uint32 mSectorCount;
	uint32 mSizeInMB;
	uint32 mHeads;
	uint32 mSPT;
	bool mbAutoGeometry;
	bool mbDynamicDisk;
	uint32 mInhibitUpdateLocks;
};

ATUIDialogCreateVHDImage2::ATUIDialogCreateVHDImage2()
	: VDDialogFrameW32(IDD_CREATE_VHD)
	, mSectorCount(8*1024*2)		// 8MB
	, mSizeInMB(8)
	, mHeads(15)
	, mSPT(63)
	, mbAutoGeometry(true)
	, mbDynamicDisk(true)
	, mInhibitUpdateLocks(0)
{
}

ATUIDialogCreateVHDImage2::~ATUIDialogCreateVHDImage2() {
}

bool ATUIDialogCreateVHDImage2::OnLoaded() {
	UpdateGeometry();
	UpdateEnables();

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogCreateVHDImage2::OnDataExchange(bool write) {
	ExchangeControlValueString(write, IDC_PATH, mPath);
	ExchangeControlValueUint32(write, IDC_SIZE_SECTORS, mSectorCount, 2048, 0xFFFFFFFEU);
	ExchangeControlValueUint32(write, IDC_SIZE_MB, mSizeInMB, 1, 4095);

	if (write) {
		mbAutoGeometry = IsButtonChecked(IDC_GEOMETRY_AUTO);
		mbDynamicDisk = IsButtonChecked(IDC_TYPE_DYNAMIC);
	} else {
		CheckButton(IDC_GEOMETRY_AUTO, mbAutoGeometry);
		CheckButton(IDC_GEOMETRY_MANUAL, !mbAutoGeometry);

		CheckButton(IDC_TYPE_FIXED, !mbDynamicDisk);
		CheckButton(IDC_TYPE_DYNAMIC, mbDynamicDisk);
	}

	if (!write || mbAutoGeometry) {
		ExchangeControlValueUint32(write, IDC_HEADS, mHeads, 1, 16);
		ExchangeControlValueUint32(write, IDC_SPT, mHeads, 1, 255);
	}
}

bool ATUIDialogCreateVHDImage2::OnOK() {
	if (VDDialogFrameW32::OnOK())
		return true;

	// Okay, let's actually try to create the VHD image!

	try {
		ATIDEVHDImage vhd;
		vhd.InitNew(mPath.c_str(), mHeads, mSPT, mSectorCount, mbDynamicDisk);
		vhd.Flush();
	} catch(const MyUserAbortError&) {
		return true;
	} catch(const MyError& e) {
		VDStringW msg;
		msg.sprintf(L"VHD creation failed: %hs", e.gets());
		ShowError(msg.c_str(), L"Altirra Error");
		return true;
	}

	ShowInfo(L"VHD creation was successful.", L"Altirra Notice");
	return false;
}

bool ATUIDialogCreateVHDImage2::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_BROWSE:
			{
				VDStringW s(VDGetSaveFileName('vhd ', (VDGUIHandle)mhdlg, L"Select location for new VHD image file", L"Virtual hard disk image\0*.vhd\0", L"vhd"));
				if (!s.empty())
					SetControlText(IDC_PATH, s.c_str());
			}
			return true;

		case IDC_GEOMETRY_AUTO:
		case IDC_GEOMETRY_MANUAL:
			if (extcode == BN_CLICKED)
				UpdateEnables();
			return true;

		case IDC_SIZE_MB:
			if (extcode == EN_UPDATE && !mInhibitUpdateLocks) {
				uint32 mb = GetControlValueUint32(IDC_SIZE_MB);

				if (mb) {
					++mInhibitUpdateLocks;
					SetControlTextF(IDC_SIZE_SECTORS, L"%u", mb * 2048);
					--mInhibitUpdateLocks;
				}
			}
			return true;

		case IDC_SIZE_SECTORS:
			if (extcode == EN_UPDATE && !mInhibitUpdateLocks) {
				uint32 sectors = GetControlValueUint32(IDC_SIZE_SECTORS);

				if (sectors) {
					++mInhibitUpdateLocks;
					SetControlTextF(IDC_SIZE_MB, L"%u", sectors >> 11);
					--mInhibitUpdateLocks;
				}
			}
			return true;
	}

	return false;
}

void ATUIDialogCreateVHDImage2::UpdateGeometry() {
	// This calculation is from the VHD spec.
	uint32 secCount = std::min<uint32>(mSectorCount, 65535*16*255);

	if (secCount >= 65535*16*63) {
		mSPT = 255;
		mHeads = 16;
	} else {
		mSPT = 17;

		uint32 tracks = secCount / 17;
		uint32 heads = (tracks + 1023) >> 10;

		if (heads < 4) {
			heads = 4;
		}
		
		if (tracks >= (heads * 1024) || heads > 16) {
			mSPT = 31;
			heads = 16;
			tracks = secCount / 31;
		}

		if (tracks >= (heads * 1024)) {
			mSPT = 63;
			heads = 16;
		}

		mHeads = heads;
	}

	SetControlTextF(IDC_HEADS, L"%u", mHeads);
	SetControlTextF(IDC_SPT, L"%u", mSPT);
}

void ATUIDialogCreateVHDImage2::UpdateEnables() {
	bool enableManualControls = IsButtonChecked(IDC_GEOMETRY_MANUAL);

	EnableControl(IDC_STATIC_HEADS, enableManualControls);
	EnableControl(IDC_STATIC_SPT, enableManualControls);
	EnableControl(IDC_HEADS, enableManualControls);
	EnableControl(IDC_SPT, enableManualControls);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogDeviceHardDisk final : public VDDialogFrameW32 {
public:
	ATUIDialogDeviceHardDisk(ATPropertySet& props);
	~ATUIDialogDeviceHardDisk();

	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 extcode) override;

protected:
	void UpdateEnables();
	void UpdateGeometry();
	void UpdateCapacity();
	void SetCapacityBySectorCount(uint64 sectors);

	uint32 mInhibitUpdateLocks;
	ATPropertySet& mProps;
};

ATUIDialogDeviceHardDisk::ATUIDialogDeviceHardDisk(ATPropertySet& props)
	: VDDialogFrameW32(IDD_DEVICE_HARDDISK)
	, mInhibitUpdateLocks(0)
	, mProps(props)
{
}

ATUIDialogDeviceHardDisk::~ATUIDialogDeviceHardDisk() {
}

bool ATUIDialogDeviceHardDisk::OnLoaded() {
	if (VDIsAtLeastVistaW32()) {
		HWND hwndItem = GetDlgItem(mhdlg, IDC_IDE_DISKBROWSE);

		if (hwndItem)
			SendMessage(hwndItem, BCM_SETSHIELD, 0, TRUE);
	}

	ATUIEnableEditControlAutoComplete(GetControl(IDC_IDE_IMAGEPATH));

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDeviceHardDisk::OnDataExchange(bool write) {
	if (!write) {
		SetControlText(IDC_IDE_IMAGEPATH, mProps.GetString("path"));
		CheckButton(IDC_IDEREADONLY, !mProps.GetBool("write_enabled"));

		uint32 cylinders = mProps.GetUint32("cylinders", 0);
		uint32 heads = mProps.GetUint32("heads", 0);
		uint32 spt = mProps.GetUint32("sectors_per_track", 0);

		if (!cylinders || !heads || !spt) {
			heads = 0;
			spt = 0;
			cylinders = 0;
		} else {
			SetControlTextF(IDC_IDE_CYLINDERS, L"%u", cylinders);
			SetControlTextF(IDC_IDE_HEADS, L"%u", heads);
			SetControlTextF(IDC_IDE_SPT, L"%u", spt);
		}

		bool fast = mProps.GetBool("solid_state");
		CheckButton(IDC_SPEED_FAST, fast);
		CheckButton(IDC_SPEED_SLOW, !fast);

		UpdateCapacity();

		if (!cylinders || !heads || !spt) {
			uint32 totalSectors = mProps.GetUint32("sectors");
			if (totalSectors)
				SetCapacityBySectorCount(totalSectors);
		}

		UpdateEnables();
	} else {
		const bool write = !IsButtonChecked(IDC_IDEREADONLY);
		const bool fast = IsButtonChecked(IDC_SPEED_FAST);

		VDStringW path;
		GetControlText(IDC_IDE_IMAGEPATH, path);

		if (path.empty()) {
			FailValidation(IDC_IDE_IMAGEPATH);
			return;
		}

		uint32 cylinders = 0;
		uint32 heads = 0;
		uint32 sectors = 0;

		if (!path.empty()) {
			if (!GetControlValueString(IDC_IDE_CYLINDERS).empty()) {
				cylinders = GetControlValueUint32(IDC_IDE_CYLINDERS);
				if (cylinders > 16777216)
					FailValidation(IDC_IDE_CYLINDERS);
			}

			if (!GetControlValueString(IDC_IDE_HEADS).empty()) {
				heads = GetControlValueUint32(IDC_IDE_HEADS);
				if (heads > 16)
					FailValidation(IDC_IDE_HEADS);
			}

			if (!GetControlValueString(IDC_IDE_SPT).empty()) {
				sectors = GetControlValueUint32(IDC_IDE_SPT);
				if (sectors > 255)
					FailValidation(IDC_IDE_SPT);
			}
		}

		if (!mbValidationFailed) {
			mProps.Clear();
			mProps.SetString("path", path.c_str());

			if (cylinders && heads && sectors) {
				mProps.SetUint32("cylinders", cylinders);
				mProps.SetUint32("heads", heads);
				mProps.SetUint32("sectors_per_track", sectors);
				mProps.SetUint32("sectors", cylinders * heads * sectors);
			}

			mProps.SetBool("write_enabled", write);
			mProps.SetBool("solid_state", fast);
		}
	}
}

bool ATUIDialogDeviceHardDisk::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_IDE_IMAGEBROWSE:
			{
				int optvals[1]={false};

				static const VDFileDialogOption kOpts[]={
					{ VDFileDialogOption::kConfirmFile, 0 },
					{0}
				};

				VDStringW s(VDGetSaveFileName('ide ', (VDGUIHandle)mhdlg, L"Select IDE image file", L"All files\0*.*\0", NULL, kOpts, optvals));
				if (!s.empty()) {
					if (s.size() >= 4 && !vdwcsicmp(s.c_str() + s.size() - 4, L".vhd")) {
						try {
							vdrefptr<ATIDEVHDImage> vhdImage(new ATIDEVHDImage);

							vhdImage->Init(s.c_str(), false, false);

							SetCapacityBySectorCount(vhdImage->GetSectorCount());
						} catch(const MyError& e) {
							e.post(mhdlg, "Altirra Error");
							return true;
						}
					} else {
						VDDirectoryIterator it(s.c_str());

						if (it.Next()) {
							SetCapacityBySectorCount(it.GetSize() >> 9);
						}
					}

					SetControlText(IDC_IDE_IMAGEPATH, s.c_str());

					uint32 attr = VDFileGetAttributes(s.c_str());
					if (attr != kVDFileAttr_Invalid)
						CheckButton(IDC_IDEREADONLY, (attr & kVDFileAttr_ReadOnly) != 0);
				}
			}
			return true;

		case IDC_IDE_DISKBROWSE:
			if (!ATIsUserAdministrator()) {
				ShowError(L"You must run Altirra with local administrator access in order to mount a physical disk for emulation.", L"Altirra Error");
				return true;
			} else {
				ShowWarning(
					L"This option uses a physical disk for IDE emulation. You can either map the entire disk or a partition within the disk. However, only read only access is supported.\n"
					L"\n"
					L"You can use a partition that is currently mounted by Windows. However, changes to the file system in Windows may not be reflected consistently in the emulator.",
					L"Altirra Warning");
			}

			{
				const VDStringW& path = ATUIShowDialogBrowsePhysicalDisks((VDGUIHandle)mhdlg);

				if (!path.empty()) {
					SetControlText(IDC_IDE_IMAGEPATH, path.c_str());
					CheckButton(IDC_IDEREADONLY, true);

					sint64 size = ATIDEGetPhysicalDiskSize(path.c_str());
					uint64 sectors = (uint64)size >> 9;

					SetCapacityBySectorCount(sectors);
				}
			}
			return true;

		case IDC_CREATE_VHD:
			{
				ATUIDialogCreateVHDImage2 createVHDDlg;

				if (createVHDDlg.ShowDialog((VDGUIHandle)mhdlg)) {
					SetCapacityBySectorCount(createVHDDlg.GetSectorCount());
					SetControlText(IDC_IDE_IMAGEPATH, createVHDDlg.GetPath());
				}
			}
			return true;

		case IDC_IDE_CYLINDERS:
		case IDC_IDE_HEADS:
		case IDC_IDE_SPT:
			if (extcode == EN_UPDATE && !mInhibitUpdateLocks)
				UpdateCapacity();
			return true;

		case IDC_IDE_SIZE:
			if (extcode == EN_UPDATE && !mInhibitUpdateLocks)
				UpdateGeometry();
			return true;
	}

	return false;
}

void ATUIDialogDeviceHardDisk::UpdateEnables() {
}

void ATUIDialogDeviceHardDisk::UpdateGeometry() {
	uint32 imageSizeMB = GetControlValueUint32(IDC_IDE_SIZE);

	if (imageSizeMB) {
		uint32 heads;
		uint32 sectors;
		uint32 cylinders;

		if (imageSizeMB <= 64) {
			heads = 4;
			sectors = 32;
			cylinders = imageSizeMB << 4;
		} else {
			heads = 16;
			sectors = 63;
			cylinders = (imageSizeMB * 128 + 31) / 63;
		}

		if (cylinders > 16777216)
			cylinders = 16777216;

		++mInhibitUpdateLocks;
		SetControlTextF(IDC_IDE_CYLINDERS, L"%u", cylinders);
		SetControlTextF(IDC_IDE_HEADS, L"%u", heads);
		SetControlTextF(IDC_IDE_SPT, L"%u", sectors);
		--mInhibitUpdateLocks;
	}
}

void ATUIDialogDeviceHardDisk::UpdateCapacity() {
	uint32 cyls = GetControlValueUint32(IDC_IDE_CYLINDERS);
	uint32 heads = GetControlValueUint32(IDC_IDE_HEADS);
	uint32 spt = GetControlValueUint32(IDC_IDE_SPT);
	uint32 size = 0;

	if (cyls || heads || spt)
		size = (cyls * heads * spt) >> 11;

	++mInhibitUpdateLocks;

	if (size)
		SetControlTextF(IDC_IDE_SIZE, L"%u", size);
	else
		SetControlText(IDC_IDE_SIZE, L"--");

	--mInhibitUpdateLocks;
}

void ATUIDialogDeviceHardDisk::SetCapacityBySectorCount(uint64 sectors) {
	uint32 spt = 63;
	uint32 heads = 15;
	uint32 cylinders = 1;

	if (sectors)
		cylinders = VDClampToUint32((sectors - 1) / (heads * spt) + 1);

	SetControlTextF(IDC_IDE_CYLINDERS, L"%u", cylinders);
	SetControlTextF(IDC_IDE_HEADS, L"%u", heads);
	SetControlTextF(IDC_IDE_SPT, L"%u", spt);

	UpdateCapacity();
}

bool ATUIConfDevHardDisk(VDGUIHandle hParent, ATPropertySet& props) {
	ATUIDialogDeviceHardDisk dlg(props);

	return dlg.ShowDialog(hParent) != 0;
}
