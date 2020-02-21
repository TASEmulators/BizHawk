//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - UI logging pane
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
#include <windowsx.h>
#include <richedit.h>
#include <vd2/system/error.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiframe.h>
#include <at/atcore/device.h>
#include <at/atcore/devicemanager.h>
#include <at/atcore/propertyset.h>
#include "logger.h"
#include "panes.h"
#include "resource.h"
#include "globals.h"
#include "broadcaster.h"
#include "events.h"
#include "settings.h"

///////////////////////////////////////////////////////////////////////////

class ATSUIDevicePanel : public vdrefcounted<VDDialogFrameW32> {
public:
	ATSUIDevicePanel(uint32 resId);

	void SetFontMarlett(HFONT hfontMarlett) { mhfontMarlett = hfontMarlett; }

protected:
	void EngReadSettings();
	void EngWriteSettings(const ATPropertySet *pset);
	virtual IATDevice *EngGetDevice(ATPropertySet *pset) = 0;
	virtual void UpdateSettings(const ATPropertySet *pset) = 0;
	virtual const char *GetDeviceName() const = 0;

	HFONT mhfontMarlett = nullptr;

	ATSEventRegistration mDevicesChanged;
};

ATSUIDevicePanel::ATSUIDevicePanel(uint32 resId)
	: vdrefcounted<VDDialogFrameW32>(resId)
{
	mDevicesChanged.Register<ATSEventDevicesChanged>(
		[this](const ATSEventDevicesChanged&) {
			if (IsCreated())
				OnDataExchange(false);
		}
	);
}

void ATSUIDevicePanel::EngReadSettings() {
	ATPropertySet pset;
	IATDevice *dev = EngGetDevice(&pset);

	auto p = vdmakerefptr(this);

	if (!dev)
		PostCall([=]() { p->UpdateSettings(nullptr); });
	else
		PostCall([=]() { p->UpdateSettings(&pset); });
}

void ATSUIDevicePanel::EngWriteSettings(const ATPropertySet *pset) {
	IATDevice *dev = EngGetDevice(nullptr);

	try {
		if (!dev) {
			if (pset)
				dev = ATSGetDeviceManager()->AddDevice(GetDeviceName(), *pset, false, false);
		} else {
			if (pset)
				dev->SetSettings(*pset);
			else {
				ATSGetDeviceManager()->RemoveDevice(dev);
				dev = nullptr;
			}
		}
	} catch(const MyError&) {
	}

	auto p = vdmakerefptr(this);
	if (dev) {
		ATPropertySet psetNew;
		dev->GetSettings(psetNew);

		PostCall([=]() { p->UpdateSettings(&psetNew); });
	} else {
		PostCall([=]() { p->UpdateSettings(nullptr); });
	}
}

///////////////////////////////////////////////////////////////////////////

class ATSUIExeLoaderPanel final : public ATSUIDevicePanel {
public:
	ATSUIExeLoaderPanel();

private:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 extcode) override;

	IATDevice *EngGetDevice(ATPropertySet *pset) override;
	void UpdateSettings(const ATPropertySet *pset) override;
	const char *GetDeviceName() const override { return "exeloader"; }

	uint32 mExpandedHt = 0;
	uint32 mCollapsedHt = 0;
	bool mbExpanded = false;
};

ATSUIExeLoaderPanel::ATSUIExeLoaderPanel()
	: ATSUIDevicePanel(IDD_PANEL_EXELOADER)
{
}

bool ATSUIExeLoaderPanel::OnLoaded() {
	SendDlgItemMessage(mhdlg, IDC_EJECT, WM_SETFONT, (WPARAM)mhfontMarlett, MAKELONG(TRUE, 0));

	mExpandedHt = GetClientArea().height();
	mCollapsedHt = GetControlPos(IDC_AUTODISABLEBASIC).top;

	vdrect32 r = GetArea();
	SetArea(vdrect32(r.left, r.top, r.right, r.top + mCollapsedHt), false);

	return ATSUIDevicePanel::OnLoaded();
}

void ATSUIExeLoaderPanel::OnDataExchange(bool write) {
	if (write) {
		VDStringW path;
		GetControlText(IDC_PATH, path);
		
		auto p = vdmakerefptr(this);
		if (path.empty()) {
			ATSPostEngineRequest(
				[=]() {
					p->EngWriteSettings(nullptr);
				}
			);
		} else {
			ATPropertySet pset;

			pset.SetString("path", path.c_str());
			pset.SetBool("nobasic", IsButtonChecked(IDC_AUTODISABLEBASIC));

			ATSPostEngineRequest(
				[=]() {
					p->EngWriteSettings(&pset);
				}
			);
		}
	} else {
		auto p = vdmakerefptr(this);

		ATSPostEngineRequest(
			[=]() {
				p->EngReadSettings();
			}
		);
	}
}

bool ATSUIExeLoaderPanel::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_BROWSE:
			{
				const VDStringW path(VDGetLoadFileName('xex ', (VDGUIHandle)mhdlg, L"Mount executable", L"Atari executable (*.xex;*.obx)\0*.xex;*.obx\0", nullptr));

				if (!path.empty()) {
					SetControlText(IDC_PATH, path.c_str());

					OnDataExchange(true);
				}
			}
			return true;

		case IDC_EJECT:
			{
				VDStringW s;
				GetControlText(IDC_PATH, s);

				if (!s.empty()) {
					SetControlText(IDC_PATH, L"");
					OnDataExchange(true);
				}
			}

			return true;

		case IDC_EXPAND:
			mbExpanded = !mbExpanded;
			if (mbExpanded) {
				vdrect32 r = GetArea();
				SetArea(vdrect32(r.left, r.top, r.right, r.top + mExpandedHt), false);
				SetControlText(IDC_EXPAND, L"-");
				PostMessage(GetParent(mhdlg), ATWM_INVLAYOUT, 0, 0);
			} else {
				vdrect32 r = GetArea();
				SetArea(vdrect32(r.left, r.top, r.right, r.top + mCollapsedHt), false);
				SetControlText(IDC_EXPAND, L"+");
				PostMessage(GetParent(mhdlg), ATWM_INVLAYOUT, 0, 0);
			}
			return true;

		case IDC_AUTODISABLEBASIC:
			OnDataExchange(true);
			return true;
	}

	return ATSUIDevicePanel::OnCommand(id, extcode);;
}

IATDevice *ATSUIExeLoaderPanel::EngGetDevice(ATPropertySet *pset) {
	ATDeviceManager *dm = ATSGetDeviceManager();
	auto *founddev = dm->GetDeviceByTag("exeloader");

	if (founddev && pset) {
		pset->Clear();
		founddev->GetSettings(*pset);
	}

	return founddev;
}

void ATSUIExeLoaderPanel::UpdateSettings(const ATPropertySet *pset) {
	if (pset) {
		SetControlText(IDC_PATH, pset->GetString("path", L""));
		CheckButton(IDC_AUTODISABLEBASIC, pset->GetBool("nobasic"));
	} else {
		SetControlText(IDC_PATH, L"");
	}

}

///////////////////////////////////////////////////////////////////////////

class ATSUIPaneDriveOptions final : public ATSUIDevicePanel {
public:
	ATSUIPaneDriveOptions();

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 code) override;

private:
	void OnSelectionChanged(VDUIProxyComboBoxControl *sender, int sel);
	IATDevice *EngGetDevice(ATPropertySet *pset) override { return nullptr; }
	void UpdateSettings(const ATPropertySet *pset) override {}

	const char *GetDeviceName() const override { return "disk"; }

	VDUIProxyComboBoxControl mTimingModeCombo;
	VDDelegate mDelTimingModeChanged;

	uint32 mExpandedHt = 0;
	uint32 mCollapsedHt = 0;
	bool mbExpanded = false;
};

ATSUIPaneDriveOptions::ATSUIPaneDriveOptions()
	: ATSUIDevicePanel(IDD_PANEL_DISKOPTIONS)
{
	mTimingModeCombo.OnSelectionChanged() += mDelTimingModeChanged.Bind(this, &ATSUIPaneDriveOptions::OnSelectionChanged);
}

bool ATSUIPaneDriveOptions::OnLoaded() {
	AddProxy(&mTimingModeCombo, IDC_TIMINGMODE);

	mTimingModeCombo.AddItem(L"No delay");
	mTimingModeCombo.AddItem(L"Accurate rotational timing");

	mExpandedHt = GetClientArea().height();
	mCollapsedHt = mTimingModeCombo.GetArea().top;

	vdrect32 r = GetArea();
	SetArea(vdrect32(r.left, r.top, r.right, r.top + mCollapsedHt), false);

	return VDDialogFrameW32::OnLoaded();
}

void ATSUIPaneDriveOptions::OnDataExchange(bool write) {
	if (write) {
		const bool ate = mTimingModeCombo.GetSelection() > 0;
		
		if (g_ATSDiskAccurateTimingEnabled != ate) {
			g_ATSDiskAccurateTimingEnabled = ate;

			auto p = vdmakerefptr(this);
			ATSPostEngineRequest(
				[=]() {
					ATDeviceManager *dm = ATSGetDeviceManager();
					IATDevice *dev = dm->GetDeviceByTag("disk");

					if (dev) {
						ATPropertySet pset;
						dev->GetSettings(pset);
						pset.SetBool("actiming", ate);
						try {
							dev->SetSettings(pset);
						} catch(const MyError&) {
						}
					}
				}
			);
		}
	} else {
		mTimingModeCombo.SetSelection(g_ATSDiskAccurateTimingEnabled ? 1 : 0);
	}
}

bool ATSUIPaneDriveOptions::OnCommand(uint32 id, uint32 code) {
	switch(id) {
		case IDC_EXPAND:
			mbExpanded = !mbExpanded;
			if (mbExpanded) {
				vdrect32 r = GetArea();
				SetArea(vdrect32(r.left, r.top, r.right, r.top + mExpandedHt), false);
				SetControlText(IDC_EXPAND, L"-");
				PostMessage(GetParent(mhdlg), ATWM_INVLAYOUT, 0, 0);
			} else {
				vdrect32 r = GetArea();
				SetArea(vdrect32(r.left, r.top, r.right, r.top + mCollapsedHt), false);
				SetControlText(IDC_EXPAND, L"+");
				PostMessage(GetParent(mhdlg), ATWM_INVLAYOUT, 0, 0);
			}
			return true;
	}

	return VDDialogFrameW32::OnCommand(id, code);
}

void ATSUIPaneDriveOptions::OnSelectionChanged(VDUIProxyComboBoxControl *sender, int sel) {
	OnDataExchange(true);
}

///////////////////////////////////////////////////////////////////////////

class ATSUIPaneDrive final : public ATSUIDevicePanel {
public:
	ATSUIPaneDrive(uint8 idx);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 code) override;

private:
	void OnSelectionChanged(VDUIProxyComboBoxControl *sender, int sel);
	IATDevice *EngGetDevice(ATPropertySet *pset) override;

	void UpdateSettings(const ATPropertySet *pset) override;
	const char *GetDeviceName() const override { return "disk"; }

	void UpdateEnables();

	const uint8 mIndex;

	VDUIProxyComboBoxControl mModeCombo;
	VDDelegate mDelModeChanged;
};

ATSUIPaneDrive::ATSUIPaneDrive(uint8 idx)
	: ATSUIDevicePanel(IDD_DISKDRIVE_PANEL)
	, mIndex(idx)
{
	mModeCombo.OnSelectionChanged() += mDelModeChanged.Bind(this, &ATSUIPaneDrive::OnSelectionChanged);
}

bool ATSUIPaneDrive::OnLoaded() {
	AddProxy(&mModeCombo, IDC_MODE);

	mModeCombo.AddItem(L"Off");
	mModeCombo.AddItem(L"R/O");
	mModeCombo.AddItem(L"VRW");
	mModeCombo.AddItem(L"R/W");

	SetControlTextF(IDC_STATIC_LABEL, mIndex < 9 ? L"D&%u:" : L"D%u:", mIndex + 1);

	SendDlgItemMessage(mhdlg, IDC_EJECT, WM_SETFONT, (WPARAM)mhfontMarlett, MAKELONG(TRUE, 0));

	return VDDialogFrameW32::OnLoaded();
}

void ATSUIPaneDrive::OnDataExchange(bool write) {
	if (write) {
		int mode = mModeCombo.GetSelection();
		
		auto p = vdmakerefptr(this);
		if (!mode) {
			ATSPostEngineRequest(
				[=]() {
					p->EngWriteSettings(nullptr);
				}
			);
		} else {
			ATPropertySet pset;

			pset.SetUint32("index", mIndex);

			VDStringW path;
			GetControlText(IDC_PATH, path);
			pset.SetString("path", path.c_str());

			pset.SetBool("writable", mode > 1);
			pset.SetBool("autoflush", mode > 2);
			pset.SetBool("actiming", g_ATSDiskAccurateTimingEnabled);

			ATSPostEngineRequest(
				[=]() {
					p->EngWriteSettings(&pset);
				}
			);
		}
	} else {
		auto p = vdmakerefptr(this);

		ATSPostEngineRequest(
			[=]() {
				p->EngReadSettings();
			}
		);
	}
}

bool ATSUIPaneDrive::OnCommand(uint32 id, uint32 code) {
	switch(id) {
		case IDC_BROWSE:
			{
				const VDStringW path(VDGetLoadFileName('disk', (VDGUIHandle)mhdlg,
									 L"Mount disk image",
									 L"All supported types\0*.atr;*.atx;*.dcm;*.pro"
									 L"All files\0*.*\0",
									 nullptr));

				if (!path.empty()) {
					SetControlText(IDC_PATH, path.c_str());

					if (!mModeCombo.GetSelection())
						mModeCombo.SetSelection(1);

					OnDataExchange(true);
				}
			}
			return true;

		case IDC_EJECT:
			{
				VDStringW path;

				if (GetControlText(IDC_PATH, path)) {
					if (path.empty()) {
						if (mModeCombo.GetSelection()) {
							mModeCombo.SetSelection(0);
							OnDataExchange(true);
						}
					} else {
						SetControlText(IDC_PATH, L"");
						OnDataExchange(true);
					}
				}
			}
			return true;
	}

	return VDDialogFrameW32::OnCommand(id, code);
}

void ATSUIPaneDrive::OnSelectionChanged(VDUIProxyComboBoxControl *sender, int sel) {
	UpdateEnables();
	OnDataExchange(true);
}

IATDevice *ATSUIPaneDrive::EngGetDevice(ATPropertySet *pset) {
	ATDeviceManager *dm = ATSGetDeviceManager();
	IATDevice *dev = dm->GetDeviceByTag("disk");

	if (!dev)
		return nullptr;

	ATPropertySet psetcur;
	dev->GetSettings(psetcur);

	if (psetcur.GetUint32("index") != mIndex)
		return nullptr;

	if (pset)
		*pset = std::move(psetcur);

	return dev;
}

void ATSUIPaneDrive::UpdateSettings(const ATPropertySet *pset) {
	if (pset) {
		SetControlText(IDC_PATH, pset->GetString("path", L""));

		const bool writable = pset->GetBool("writable");
		const bool autoflush = pset->GetBool("autoflush");

		mModeCombo.SetSelection(!writable ? 1 : !autoflush ? 2 : 3);
	} else {
		SetControlText(IDC_PATH, L"");
		mModeCombo.SetSelection(0);
	}

	UpdateEnables();
}

void ATSUIPaneDrive::UpdateEnables() {
	bool enabled = mModeCombo.GetSelection() != 0;

	EnableControl(IDC_EJECT, enabled);
}

///////////////////////////////////////////////////////////////////////////

class ATSUIDrivesContainer final : public VDDialogFrameW32 {
public:
	ATSUIDrivesContainer();

	bool OnLoaded() override;
	void OnDestroy() override;
	VDZINT_PTR DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) override;

	void Relayout();

	HFONT mhFontMarlett = nullptr;
	vdrefptr<ATSUIDevicePanel> mPanels[17];
};

ATSUIDrivesContainer::ATSUIDrivesContainer()
	: VDDialogFrameW32(IDD_DISKDRIVE_PARENT)
{
}

bool ATSUIDrivesContainer::OnLoaded() {
	if (!mhFontMarlett) {
		HFONT hfontDlg = (HFONT)SendMessage(mhdlg, WM_GETFONT, 0, 0);

		if (hfontDlg) {
			LOGFONT lf = {0};
			if (GetObject(hfontDlg, sizeof lf, &lf)) {
				mhFontMarlett = CreateFont(lf.lfHeight, 0, 0, 0, FW_DONTCARE, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, _T("Marlett"));
			}
		}
	}

	int y = 0;

	vdrect32 r = GetClientArea();

	for(int i=0; i<17; ++i) {
		ATSUIDevicePanel *p = i >= 2 ? static_cast<ATSUIDevicePanel *>(new ATSUIPaneDrive((uint8)(i - 2))) : i ? static_cast<ATSUIDevicePanel *>(new ATSUIPaneDriveOptions) : new ATSUIExeLoaderPanel;
		p->SetFontMarlett(mhFontMarlett);
		mPanels[i] = p;

		p->Create(this);

		const vdrect32& rc = p->GetArea();
		p->SetArea(vdrect32(0, y, r.right, y + rc.height()), false);

		y += rc.height();
	}

	SetArea(vdrect32(0, 0, GetArea().width(), y), false);

	return VDDialogFrameW32::OnLoaded();
}

void ATSUIDrivesContainer::OnDestroy() {
	// We need to manually nuke the children before we can safely delete
	// the font.
	for (auto& ptr : mPanels) {
		if (ptr) {
			ptr->Destroy();
			ptr = nullptr;
		}
	}

	if (mhFontMarlett) {
		DeleteObject(mhFontMarlett);
		mhFontMarlett = nullptr;
	}
}

VDZINT_PTR ATSUIDrivesContainer::DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	switch(msg) {
		case ATWM_INVLAYOUT:
			Relayout();
			return TRUE;
	}

	return VDDialogFrameW32::DlgProc(msg, wParam, lParam);
}

void ATSUIDrivesContainer::Relayout() {
	const sint32 w = GetClientArea().right;

	int y = 0;

	for (auto& ptr : mPanels) {
		if (!ptr)
			continue;

		const vdsize32 size = ptr->GetArea().size();
		
		ptr->SetArea(vdrect32(0, y, size.w, y + size.h), false);
		y += size.h;
	}

	SetArea(vdrect32(0, 0, GetArea().width(), y), false);
	PostMessage(GetParent(mhdlg), ATWM_INVLAYOUT, 0, 0);
}

///////////////////////////////////////////////////////////////////////////

class ATSUIPaneDevices final : public ATUIPane {
public:
	ATSUIPaneDevices();
	~ATSUIPaneDevices();

protected:
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);

	bool OnCreate();
	void OnSize();

	ATSUIDrivesContainer mContainer;
};

ATSUIPaneDevices::ATSUIPaneDevices()
	: ATUIPane(kATSUIPaneId_Devices, L"Devices")
{
	mPreferredDockCode = kATContainerDockRight;
}

ATSUIPaneDevices::~ATSUIPaneDevices() {
}

LRESULT ATSUIPaneDevices::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
	case WM_SIZE:
		OnSize();
		return 0;
	}

	return ATUIPane::WndProc(msg, wParam, lParam);
}

bool ATSUIPaneDevices::OnCreate() {
	if (!ATUIPane::OnCreate())
		return false;

	mContainer.Create((VDGUIHandle)mhwnd);

	OnSize();

	return true;
}

void ATSUIPaneDevices::OnSize() {
	RECT r = {};
	GetClientRect(mhwnd, &r);
	mContainer.SetArea(vdrect32(0, 0, r.right, mContainer.GetSize().h), false);
}

///////////////////////////////////////////////////////////////////////////

void ATSUIRegisterPaneDevices() {
	ATRegisterUIPaneType(kATSUIPaneId_Devices, VDRefCountObjectFactory<ATSUIPaneDevices>);
}
