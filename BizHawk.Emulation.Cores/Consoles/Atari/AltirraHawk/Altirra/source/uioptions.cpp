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
#include <list>
#include <windows.h>
#include <richedit.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/math.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/services.h>
#include <at/atcore/media.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/genericdialog.h>
#include <at/atnativeui/uiproxies.h>
#include <at/atui/uimanager.h>
#include "resource.h"
#include "options.h"
#include "oshelper.h"
#include "settings.h"
#include "compatengine.h"
#include "uifilefilters.h"
#include "uipageddialog.h"

// This is actually deprecated in earlier SDKs (VS2005) and undeprecated
// in later ones (Win7). Interesting.
#ifndef DM_INTERLACED
#define DM_INTERLACED 0x00000002
#endif

#ifndef BCM_SETSHIELD
#define BCM_SETSHIELD	0x160C
#endif

extern ATUIManager g_ATUIManager;

void ATUIShowDialogSetFileAssociations(VDGUIHandle parent, bool allowElevation, bool userOnly);
void ATUIShowDialogRemoveFileAssociations(VDGUIHandle parent, bool allowElevation, bool userOnly);

///////////////////////////////////////////////////////////////////////////

class ATUIDialogFullScreenMode : public VDDialogFrameW32 {
public:
	struct ModeInfo {
		uint32 mWidth;
		uint32 mHeight;
		uint32 mRefresh;
	};

	ATUIDialogFullScreenMode();

	const ModeInfo& GetSelectedItem() const { return mSelectedMode; }
	void SetSelectedItem(const ModeInfo& modeInfo) { mSelectedMode = modeInfo; }

protected:
	bool OnLoaded();
	void OnDestroy();
	void OnDataExchange(bool write);
	void OnSelectedItemChanged(VDUIProxyListView *sender, int index);

	ModeInfo	mSelectedMode;

	VDUIProxyListView mList;
	VDDelegate mDelSelItemChanged;

	struct ModeItem : public vdrefcounted<IVDUIListViewVirtualItem>, public ModeInfo {
		ModeItem(const ModeInfo& modeInfo) : ModeInfo(modeInfo) {}

		void GetText(int subItem, VDStringW& s) const;
	};

	struct ModeInfoLess {
		bool operator()(const ModeInfo& x, const ModeInfo& y) const {
			if (x.mWidth != y.mWidth)
				return x.mWidth < y.mWidth;

			if (x.mHeight != y.mHeight)
				return x.mHeight < y.mHeight;

			return x.mRefresh < y.mRefresh;
		}
	};

	struct ModeInfoEqual {
		bool operator()(const ModeInfo& x, const ModeInfo& y) const {
			return x.mWidth == y.mWidth &&
				x.mHeight == y.mHeight &&
				x.mRefresh == y.mRefresh;
		}
	};

	struct ModeInfoMatch {
		ModeInfoMatch(const ModeInfo& mode) : mMode(mode) {}

		bool operator()(const ModeInfo& mode) const {
			return ModeInfoEqual()(mMode, mode);
		}

		const ModeInfo mMode;
	};
};

void ATUIDialogFullScreenMode::ModeItem::GetText(int subItem, VDStringW& s) const {
	switch(subItem) {
		case 0:
			s.sprintf(L"%ux%u", mWidth, mHeight);
			break;

		case 1:
			if (!mRefresh)
				s = L"Default";
			else
				s.sprintf(L"%uHz", mRefresh);
			break;
	}
}

ATUIDialogFullScreenMode::ATUIDialogFullScreenMode()
	: VDDialogFrameW32(IDD_OPTIONS_DISPLAY_MODE)
{
	memset(&mSelectedMode, 0, sizeof mSelectedMode);

	mList.OnItemSelectionChanged() += mDelSelItemChanged.Bind(this, &ATUIDialogFullScreenMode::OnSelectedItemChanged);
}

bool ATUIDialogFullScreenMode::OnLoaded() {
	AddProxy(&mList, IDC_MODE_LIST);
	mList.InsertColumn(0, L"Resolution", 0);
	mList.InsertColumn(1, L"Refresh rate", 0);
	mList.SetFullRowSelectEnabled(true);

	VDDialogFrameW32::OnLoaded();

	SetFocusToControl(IDC_LIST);
	return true;
}

void ATUIDialogFullScreenMode::OnDestroy() {
	mList.Clear();
}

void ATUIDialogFullScreenMode::OnDataExchange(bool write) {
	if (write) {
		ModeItem *p = static_cast<ModeItem *>(mList.GetSelectedVirtualItem());

		if (!p) {
			FailValidation(IDC_LIST);
			return;
		}

		mSelectedMode = *p;
	} else {
		struct {
			DEVMODE dm;
			char buf[1024];
		} devMode;

		vdfastvector<ModeInfo> modes;

		int modeIndex = 0;
		for(;; ++modeIndex) {
			devMode.dm.dmSize = sizeof(DEVMODE);
			devMode.dm.dmDriverExtra = sizeof devMode.buf;

			if (!EnumDisplaySettingsEx(NULL, modeIndex, &devMode.dm, EDS_RAWMODE))
				break;

			// throw out paletted modes
			if (devMode.dm.dmBitsPerPel < 15)
				continue;

			// throw out interlaced modes
			if (devMode.dm.dmDisplayFlags & DM_INTERLACED)
				continue;

			ModeInfo mode = { devMode.dm.dmPelsWidth, devMode.dm.dmPelsHeight, devMode.dm.dmDisplayFrequency };

			if (mode.mRefresh == 1)
				mode.mRefresh = 0;

			modes.push_back(mode);
		}

		std::sort(modes.begin(), modes.end(), ModeInfoLess());
		modes.erase(std::unique(modes.begin(), modes.end(), ModeInfoEqual()), modes.end());

		int selectedIndex = (int)(std::find_if(modes.begin(), modes.end(), ModeInfoMatch(mSelectedMode)) - modes.begin());
		if (selectedIndex >= (int)modes.size())
			selectedIndex = -1;

		for(vdfastvector<ModeInfo>::const_iterator it(modes.begin()), itEnd(modes.end());
			it != itEnd;
			++it)
		{
			const ModeInfo& modeInfo = *it;

			ModeItem *modeItem = new ModeItem(modeInfo);

			if (modeItem) {
				modeItem->AddRef();
				mList.InsertVirtualItem(-1, modeItem);
				modeItem->Release();
			}
		}

		mList.SetSelectedIndex(selectedIndex);
		mList.EnsureItemVisible(selectedIndex);
		mList.AutoSizeColumns();
		EnableControl(IDOK, selectedIndex >= 0);
	}
}

void ATUIDialogFullScreenMode::OnSelectedItemChanged(VDUIProxyListView *sender, int index) {
	EnableControl(IDOK, index >= 0);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPage : public ATUIDialogPage {
public:
	ATUIDialogOptionsPage(uint32 id, ATOptions& opts);

protected:
	ATOptions& mOptions;
};

ATUIDialogOptionsPage::ATUIDialogOptionsPage(uint32 id, ATOptions& opts)
	: ATUIDialogPage(id)
	, mOptions(opts)
{
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageDisplay final : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageDisplay(ATOptions& opts);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 extcode) override;
};

ATUIDialogOptionsPageDisplay::ATUIDialogOptionsPageDisplay(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_DISPLAY, opts)
{
}

bool ATUIDialogOptionsPageDisplay::OnLoaded() {
	AddHelpEntry(IDC_GRAPHICS_DDRAW, L"DirectDraw", L"Enable DirectDraw support. This is used if D3D9/OpenGL are disabled or not available.");
	AddHelpEntry(IDC_GRAPHICS_D3D9, L"Direct3D 9", L"Enable Direct3D 9 support. This is the best general option for speed and quality, and also enables the filtering options.");
	AddHelpEntry(IDC_GRAPHICS_3D, L"Direct3D 11", L"Enable Direct3D 11 support. On Windows 8.1 and later, D3D11 can give better performance and power efficiency.");
	AddHelpEntry(IDC_GRAPHICS_OPENGL, L"OpenGL", L"Enable OpenGL support. Direct3D 9 is a better option, but this is a reasonable fallback.");
	AddHelpEntry(IDC_16BIT, L"Use 16-bit surfaces", L"Use 16-bit surfaces for faster speed on low-end graphics cards. May reduce visual quality.");
	AddHelpEntry(IDC_FSMODE_BORDERLESS, L"Full screen mode: Borderless mode", L"Use a full-screen borderless window without switching to exclusive full screen mode.");
	AddHelpEntry(IDC_FSMODE_DESKTOP, L"Full screen mode: Match desktop", L"Uses the desktop resolution for full screen mode. This avoids a mode switch.");
	AddHelpEntry(IDC_FSMODE_CUSTOM, L"Full screen mode: Custom", L"Use a specific video mode for full screen mode. Zero for refresh rate allows any rate.");
	LinkHelpEntry(IDC_FSMODE_WIDTH, IDC_FSMODE_CUSTOM);
	LinkHelpEntry(IDC_FSMODE_HEIGHT, IDC_FSMODE_CUSTOM);
	LinkHelpEntry(IDC_FSMODE_REFRESH, IDC_FSMODE_CUSTOM);
	LinkHelpEntry(IDC_FSMODE_BROWSE, IDC_FSMODE_CUSTOM);

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageDisplay::OnDataExchange(bool write) {
	ExchangeControlValueBoolCheckbox(write, IDC_GRAPHICS_DDRAW, mOptions.mbDisplayDDraw);
	ExchangeControlValueBoolCheckbox(write, IDC_GRAPHICS_D3D9, mOptions.mbDisplayD3D9);
	ExchangeControlValueBoolCheckbox(write, IDC_GRAPHICS_3D, mOptions.mbDisplay3D);
	ExchangeControlValueBoolCheckbox(write, IDC_GRAPHICS_OPENGL, mOptions.mbDisplayOpenGL);
	ExchangeControlValueBoolCheckbox(write, IDC_16BIT, mOptions.mbDisplay16Bit);

	if (write) {
		mOptions.mFullScreenWidth = 0;
		mOptions.mFullScreenHeight = 0;
		mOptions.mFullScreenRefreshRate = 0;

		if (IsButtonChecked(IDC_FSMODE_BORDERLESS)) {
			mOptions.mbFullScreenBorderless = true;
		} else {
			mOptions.mbFullScreenBorderless = false;
			 
			if (IsButtonChecked(IDC_FSMODE_CUSTOM)) {
				VDStringW s;
				VDStringW t;

				if (GetControlText(IDC_FSMODE_WIDTH, s) && GetControlText(IDC_FSMODE_HEIGHT, t)) {
					mOptions.mFullScreenWidth = wcstoul(s.c_str(), NULL, 10);
					mOptions.mFullScreenHeight = wcstoul(t.c_str(), NULL, 10);

					if (GetControlText(IDC_FSMODE_REFRESH, s))
						mOptions.mFullScreenRefreshRate = wcstoul(s.c_str(), NULL, 10);
				}
			}
		}
	} else {
		if (mOptions.mFullScreenWidth && mOptions.mFullScreenHeight) {
			CheckButton(IDC_FSMODE_DESKTOP, false);
			CheckButton(IDC_FSMODE_CUSTOM, true);
			CheckButton(IDC_FSMODE_BORDERLESS, false);
			SetControlTextF(IDC_FSMODE_WIDTH, L"%u", mOptions.mFullScreenWidth);
			SetControlTextF(IDC_FSMODE_HEIGHT, L"%u", mOptions.mFullScreenHeight);
			SetControlTextF(IDC_FSMODE_REFRESH, L"%u", mOptions.mFullScreenRefreshRate);
		} else {
			CheckButton(IDC_FSMODE_DESKTOP, !mOptions.mbFullScreenBorderless);
			CheckButton(IDC_FSMODE_BORDERLESS, mOptions.mbFullScreenBorderless);
			CheckButton(IDC_FSMODE_CUSTOM, false);

			SetControlText(IDC_FSMODE_WIDTH, L"");
			SetControlText(IDC_FSMODE_HEIGHT, L"");
			SetControlText(IDC_FSMODE_REFRESH, L"");
		}
	}
}

bool ATUIDialogOptionsPageDisplay::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_FSMODE_BROWSE) {
		OnDataExchange(true);

		const ATUIDialogFullScreenMode::ModeInfo modeInfo = {
			mOptions.mFullScreenWidth,
			mOptions.mFullScreenHeight,
			mOptions.mFullScreenRefreshRate
		};

		ATUIDialogFullScreenMode dlg;
		dlg.SetSelectedItem(modeInfo);
		if (dlg.ShowDialog((VDGUIHandle)mhdlg)){
			const ATUIDialogFullScreenMode::ModeInfo& newModeInfo = dlg.GetSelectedItem();
			mOptions.mFullScreenWidth = newModeInfo.mWidth;
			mOptions.mFullScreenHeight = newModeInfo.mHeight;
			mOptions.mFullScreenRefreshRate = newModeInfo.mRefresh;
			OnDataExchange(false);
		}
		return true;
	}

	return false;
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageDisplayEffects final : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageDisplayEffects(ATOptions& opts);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	bool OnCommand(uint32 id, uint32 extcode) override;

	void ExchangeOtherSettings(bool write) override;

	VDStringW mPath;
};

ATUIDialogOptionsPageDisplayEffects::ATUIDialogOptionsPageDisplayEffects(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_DISPLAY_EFFECTS, opts)
{
}

bool ATUIDialogOptionsPageDisplayEffects::OnLoaded() {
	AddHelpEntry(IDC_PATH, L"Custom effects path", L"Path to effect file to set up custom display effect. This is only supported by the Direct3D 9 display driver.");
	LinkHelpEntry(IDC_BROWSE, IDC_PATH);

	ATUIEnableEditControlAutoComplete(GetControl(IDC_PATH));

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageDisplayEffects::OnDataExchange(bool write) {
	if (write) {
		GetControlText(IDC_PATH, mPath);
	} else {
		SetControlText(IDC_PATH, mPath.c_str());
	}
}

bool ATUIDialogOptionsPageDisplayEffects::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_BROWSE) {
		const VDStringW& s = VDGetLoadFileName('ceff', (VDGUIHandle)mhdlg, L"Load custom effect", L"CG program (*.cgp)", nullptr);

		if (!s.empty())
			SetControlText(IDC_PATH, s.c_str());

		return true;
	}

	return false;
}

void ATUIDialogOptionsPageDisplayEffects::ExchangeOtherSettings(bool write) {
	if (write) {
		g_ATUIManager.SetCustomEffectPath(mPath.c_str(), false);
	} else {
		mPath = g_ATUIManager.GetCustomEffectPath();
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageErrors : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageErrors(ATOptions& opts);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
};

ATUIDialogOptionsPageErrors::ATUIDialogOptionsPageErrors(ATOptions& opts)
: ATUIDialogOptionsPage(IDD_OPTIONS_ERRORS, opts)
{
}

bool ATUIDialogOptionsPageErrors::OnLoaded() {
	AddHelpEntry(IDC_ERRORMODE_DIALOG, L"Error mode: Dialog", L"Display a dialog with recovery options when a program fails. This is the most user friendly mode and the default.");
	AddHelpEntry(IDC_ERRORMODE_DEBUG, L"Error mode: Debug", L"Open the debugger when a program fails. This is the most convenient mode when debugging. Note that this happens anyway if the debugger is already active");
	AddHelpEntry(IDC_ERRORMODE_PAUSE, L"Error mode: Pause", L"Pause the simulation when a program fails. There is no other visual feedback when this occurs.");
	AddHelpEntry(IDC_ERRORMODE_COLDRESET, L"Error mode: Cold reset", L"Immediately restart the simulator via virtual power-off/power-on when a program fails. This is best for unattended operation.");

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageErrors::OnDataExchange(bool write) {
	if (write) {
		if (IsButtonChecked(IDC_ERRORMODE_DIALOG))
			mOptions.mErrorMode = kATErrorMode_Dialog;
		else if (IsButtonChecked(IDC_ERRORMODE_DEBUG))
			mOptions.mErrorMode = kATErrorMode_Debug;
		else if (IsButtonChecked(IDC_ERRORMODE_PAUSE))
			mOptions.mErrorMode = kATErrorMode_Pause;
		else if (IsButtonChecked(IDC_ERRORMODE_COLDRESET))
			mOptions.mErrorMode = kATErrorMode_ColdReset;
	} else {
		switch(mOptions.mErrorMode) {
			case kATErrorMode_Dialog:
				CheckButton(IDC_ERRORMODE_DIALOG, true);
				break;
			case kATErrorMode_Debug:
				CheckButton(IDC_ERRORMODE_DEBUG, true);
				break;
			case kATErrorMode_Pause:
				CheckButton(IDC_ERRORMODE_PAUSE, true);
				break;
			case kATErrorMode_ColdReset:
				CheckButton(IDC_ERRORMODE_COLDRESET, true);
				break;
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageStartup : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageStartup(ATOptions& opts);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
};

ATUIDialogOptionsPageStartup::ATUIDialogOptionsPageStartup(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_STARTUP, opts)
{
}

bool ATUIDialogOptionsPageStartup::OnLoaded() {
	AddHelpEntry(IDC_SINGLE_INSTANCE, L"Reuse program instance", L"When enabled, launching the program will attempt to reuse an existing running instance instead of starting a new one (running under the same user).");

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageStartup::OnDataExchange(bool write) {
	ExchangeControlValueBoolCheckbox(write, IDC_SINGLE_INSTANCE, mOptions.mbSingleInstance);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageFileAssoc : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageFileAssoc(ATOptions& opts);

protected:
	bool OnLoaded();
	bool OnCommand(uint32 id, uint32 extcode);
	void OnDataExchange(bool write);
};

ATUIDialogOptionsPageFileAssoc::ATUIDialogOptionsPageFileAssoc(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_FILEASSOC, opts)
{
}

bool ATUIDialogOptionsPageFileAssoc::OnLoaded() {
	if (VDIsAtLeastVistaW32()) {
		HWND hwndItem = GetDlgItem(mhdlg, IDC_SETFILEASSOC);

		if (hwndItem)
			SendMessage(hwndItem, BCM_SETSHIELD, 0, TRUE);

		hwndItem = GetDlgItem(mhdlg, IDC_REMOVEFILEASSOC);

		if (hwndItem)
			SendMessage(hwndItem, BCM_SETSHIELD, 0, TRUE);
	}	

	AddHelpEntry(IDC_AUTO_PROFILE, L"Launch images with auto-profile",
		L"Automatically switch to default profile for image type when launched as the default program. (If you have set up file associations with a previous version, re-add file associations to enable this feature.)");

	return ATUIDialogOptionsPage::OnLoaded();
}

bool ATUIDialogOptionsPageFileAssoc::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_SETFILEASSOC) {
		ATUIShowDialogSetFileAssociations((VDGUIHandle)mhdlg, true, false);
		return true;
	} else if (id == IDC_SETUSERFILEASSOC) {
		ATUIShowDialogSetFileAssociations((VDGUIHandle)mhdlg, true, true);
		return true;
	} else if (id == IDC_REMOVEFILEASSOC) {
		ATUIShowDialogRemoveFileAssociations((VDGUIHandle)mhdlg, true, false);
	} else if (id == IDC_REMOVEUSERFILEASSOC) {
		ATUIShowDialogRemoveFileAssociations((VDGUIHandle)mhdlg, true, true);
	}

	return false;
}

void ATUIDialogOptionsPageFileAssoc::OnDataExchange(bool write) {
	ExchangeControlValueBoolCheckbox(write, IDC_AUTO_PROFILE, mOptions.mbLaunchAutoProfile);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageFlash : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageFlash(ATOptions& opts);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
};

ATUIDialogOptionsPageFlash::ATUIDialogOptionsPageFlash(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_FLASH, opts)
{
}

bool ATUIDialogOptionsPageFlash::OnLoaded() {
	AddHelpEntry(IDC_SIC_FLASH, L"SIC! cartridge flash", L"Sets the flash chip used for SIC! cartridges.");
	AddHelpEntry(IDC_MAXFLASH8MB_FLASH, L"Maxflash 8Mb cartridge flash",
		L"Sets the flash chip used for MaxFlash 8Mb cartridges. The HY29F040A and SST39SF040 are only recognized by the 2012+ flasher.");
	AddHelpEntry(IDC_U1MB_FLASH, L"U1MB flash", L"Sets the flash chip used for Ultimate1MB.");

	CBAddString(IDC_SIC_FLASH, L"Am29F040B (64K sectors)");
	CBAddString(IDC_SIC_FLASH, L"SSF39SF040 (4K sectors)");

	CBAddString(IDC_MAXFLASH8MB_FLASH, L"Am29F040B (64K sectors)");
	CBAddString(IDC_MAXFLASH8MB_FLASH, L"BM29F040 (64K sectors)");
	CBAddString(IDC_MAXFLASH8MB_FLASH, L"HY29F040A (64K sectors)");
	CBAddString(IDC_MAXFLASH8MB_FLASH, L"SST39SF040 (64K sectors)");

	CBAddString(IDC_U1MB_FLASH, L"A29040 (64K sectors)");
	CBAddString(IDC_U1MB_FLASH, L"SSF39SF040 (4K sectors)");
	CBAddString(IDC_U1MB_FLASH, L"Am29F040B (64K sectors)");
	CBAddString(IDC_U1MB_FLASH, L"BM29F040 (64K sectors)");

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageFlash::OnDataExchange(bool write) {
	static const char *const kSICFlashChips[]={
		"Am29F040B",
		"SST39SF040"
	};

	static const char *const kMaxflash8MbFlashChips[]={
		"Am29F040B",
		"BM29F040",
		"HY29F040A",
		"SST39SF040"
	};

	static const char *const kU1MBFlashChips[]={
		"A29040",
		"SST39SF040",
		"Am29F040B",
		"BM29F040",
	};

	if (write) {
		int idx = CBGetSelectedIndex(IDC_SIC_FLASH);

		if ((unsigned)idx < vdcountof(kSICFlashChips))
			mOptions.mSICFlashChip = kSICFlashChips[idx];

		idx = CBGetSelectedIndex(IDC_MAXFLASH8MB_FLASH);
		if ((unsigned)idx < vdcountof(kMaxflash8MbFlashChips))
			mOptions.mMaxflash8MbFlashChip = kMaxflash8MbFlashChips[idx];

		idx = CBGetSelectedIndex(IDC_U1MB_FLASH);
		if ((unsigned)idx < vdcountof(kU1MBFlashChips))
			mOptions.mU1MBFlashChip = kU1MBFlashChips[idx];
	} else {
		int idx = 0;
		for(int i=0; i<(int)vdcountof(kSICFlashChips); ++i) {
			if (mOptions.mSICFlashChip == kSICFlashChips[i]) {
				idx = i;
				break;
			}
		}

		CBSetSelectedIndex(IDC_SIC_FLASH, idx);

		idx = 0;
		for(int i=0; i<(int)vdcountof(kMaxflash8MbFlashChips); ++i) {
			if (mOptions.mMaxflash8MbFlashChip == kMaxflash8MbFlashChips[i]) {
				idx = i;
				break;
			}
		}

		CBSetSelectedIndex(IDC_MAXFLASH8MB_FLASH, idx);

		idx = 0;
		for(int i=0; i<(int)vdcountof(kU1MBFlashChips); ++i) {
			if (mOptions.mU1MBFlashChip == kU1MBFlashChips[i]) {
				idx = i;
				break;
			}
		}

		CBSetSelectedIndex(IDC_U1MB_FLASH, idx);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageUI : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageUI(ATOptions& opts);

protected:
	bool OnLoaded();
	bool OnCommand(uint32 id, uint32 mode);
	void OnDataExchange(bool write);
};

ATUIDialogOptionsPageUI::ATUIDialogOptionsPageUI(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_UI, opts)
{
}

bool ATUIDialogOptionsPageUI::OnLoaded() {
	AddHelpEntry(IDC_SCALE, L"Scale factor", L"Scale factor in percent for on-screen UI in the display window.");
	AddHelpEntry(IDC_SCALE, L"Pause when menus are open",
		L"Pause the simulation temporarily when a menu is opened.");

	return ATUIDialogOptionsPage::OnLoaded();
}

bool ATUIDialogOptionsPageUI::OnCommand(uint32 id, uint32 mode) {
	if (id == IDC_UNDISABLE) {
		if (Confirm(L"This will re-enable all dialogs previously hidden using the \"don't show this again\" option. Are you sure?")) {
			ATUIGenericDialogUndoAllIgnores();
		}
	}

	return false;
}

void ATUIDialogOptionsPageUI::OnDataExchange(bool write) {
	ExchangeControlValueSint32(write, IDC_SCALE, mOptions.mThemeScale, 10, 1000);
	ExchangeControlValueBoolCheckbox(write, IDC_PAUSE_ON_MENU, mOptions.mbPauseDuringMenu);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageSettings : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageSettings(ATOptions& opts);

protected:
	bool OnLoaded();
	bool OnCommand(uint32 id, uint32 cmd);

	void UpdateEnables();
};

ATUIDialogOptionsPageSettings::ATUIDialogOptionsPageSettings(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_SETTINGS, opts)
{
}

bool ATUIDialogOptionsPageSettings::OnLoaded() {
	bool r = ATUIDialogOptionsPage::OnLoaded();

	AddHelpEntry(IDC_RESETALL, L"Reset All Settings",
		L"Reset all settings in the program, clearing all settings for the next startup.");
	AddHelpEntry(IDC_SWITCH_TO_PORTABLE, L"Switch to Registry/Portable Mode",
		L"Switch between storing settings in the Registry, which is better for reliability with multiple instances and roaming profiles, and in an INI file for portability.");
	LinkHelpEntry(IDC_SWITCH_TO_REGISTRY, IDC_SWITCH_TO_PORTABLE);

	UpdateEnables();
	return r;
}

bool ATUIDialogOptionsPageSettings::OnCommand(uint32 id, uint32 cmd) {
	if (id == IDC_RESETALL) {
		if (!ATSettingsIsResetPending() && !ATSettingsIsMigrationScheduled()) {
			if (Confirm2(nullptr, L"This will reset all program settings to first-time defaults. Are you sure?", L"Resetting All Settings")) {
				ATSettingsScheduleReset();

				ShowInfo2(L"All settings will be reset the next time the program is restarted.", L"Reset Scheduled");
			}
		}

		return true;
	} else if (id == IDC_SWITCH_TO_REGISTRY) {
		if (ATSettingsIsInPortableMode() && !ATSettingsIsMigrationScheduled() && !ATSettingsIsResetPending()) {
			if (Confirm2(nullptr, L"This will delete Altirra.ini and copy the settings back into the Registry.", L"Switching to Registry Mode")) {
				ATSettingsScheduleMigration();

				ShowInfo2(L"Settings will be migrated from Altirra.ini to the Registry on exit.", L"Migration Scheduled");
			}
		}
	} else if (id == IDC_SWITCH_TO_PORTABLE) {
		if (!ATSettingsIsInPortableMode() && !ATSettingsIsMigrationScheduled() && !ATSettingsIsResetPending()) {
			if (Confirm2(nullptr, L"This will remove settings from the Registry and copy them into Altirra.ini.", L"Switching to Portable Mode")) {
				// check that we can open for write
				const VDStringW path = ATSettingsGetDefaultPortablePath();
				VDFile f;
				bool success = true;

				if (f.openNT(path.c_str(), nsVDFile::kCreateNew | nsVDFile::kReadWrite)) {
					// created new -- delete the file so it doesn't exist until we migrate, in case we fail first
					f.closeNT();

					VDRemoveFile(path.c_str());
				} else if (f.openNT(path.c_str(), nsVDFile::kOpenExisting | nsVDFile::kReadWrite)) {
					// already exists somehow -- leave it until migration
				} else {
					success = false;
				}

				f.closeNT();

				if (success) {
					ATSettingsScheduleMigration();
					ShowInfo2(L"Settings will be migrated from the Registry to Altirra.ini on exit.", L"Migration Scheduled");

					UpdateEnables();
				} else {
					ShowError2(L"There was a problem creating Altirra.ini. Check if the program is in a writable location.", L"Migration failed");
				}
			}
		}
	}

	return false;
}

void ATUIDialogOptionsPageSettings::UpdateEnables() {
	bool isPortable = ATSettingsIsInPortableMode();
	bool isMigrating = ATSettingsIsMigrationScheduled();
	bool isScheduled = isMigrating || ATSettingsIsResetPending();

	ShowControl(IDC_STATIC_PORTABLE, isPortable && !isMigrating);
	ShowControl(IDC_STATIC_REGISTRY, !isPortable && !isMigrating);
	ShowControl(IDC_STATIC_MIGRATING, isMigrating);
	ShowControl(IDC_SWITCH_TO_REGISTRY, isPortable);
	ShowControl(IDC_SWITCH_TO_PORTABLE, !isPortable);
	EnableControl(IDC_RESETALL, !isScheduled);
	EnableControl(IDC_SWITCH_TO_REGISTRY, !isScheduled);
	EnableControl(IDC_SWITCH_TO_REGISTRY, !isScheduled);
	EnableControl(IDC_SWITCH_TO_PORTABLE, !isScheduled);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageMedia : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageMedia(ATOptions& opts);

protected:
	bool OnLoaded();
	void OnDataExchange(bool write);
};

ATUIDialogOptionsPageMedia::ATUIDialogOptionsPageMedia(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_MEDIA, opts)
{
}

bool ATUIDialogOptionsPageMedia::OnLoaded() {
	AddHelpEntry(IDC_WRITEMODE, L"Default write mode", L"Sets the mode used for disks mounted via the Attach/Boot commands, drag-and-drop, or command line.");

	CBAddString(IDC_WRITEMODE, L"Read only");
	CBAddString(IDC_WRITEMODE, L"Virtual read/write (prohibit format)");
	CBAddString(IDC_WRITEMODE, L"Virtual read/write");
	CBAddString(IDC_WRITEMODE, L"Read/write");

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageMedia::OnDataExchange(bool write) {
	static const ATMediaWriteMode kWriteModes[]={
		kATMediaWriteMode_RO,
		kATMediaWriteMode_VRWSafe,
		kATMediaWriteMode_VRW,
		kATMediaWriteMode_RW,
	};

	if (write) {
		int idx = CBGetSelectedIndex(IDC_WRITEMODE);
		if ((unsigned)idx < vdcountof(kWriteModes))
			mOptions.mDefaultWriteMode = kWriteModes[idx];
	} else {
		int idx = 0;

		auto it = std::find(std::begin(kWriteModes), std::end(kWriteModes), mOptions.mDefaultWriteMode);
		if (it != std::end(kWriteModes))
			idx = (int)(it - std::begin(kWriteModes));

		CBSetSelectedIndex(IDC_WRITEMODE, idx);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptionsPageCompat final : public ATUIDialogOptionsPage {
public:
	ATUIDialogOptionsPageCompat(ATOptions& opts);

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	void OnUnmuteAll();
	void OnExternalDBToggled();
	void OnBrowse();
	void UpdateEnables();

	VDUIProxyButtonControl mCheckExternalDB;
	VDUIProxyButtonControl mButtonUnmuteAll;
	VDUIProxyButtonControl mButtonBrowse;
};

ATUIDialogOptionsPageCompat::ATUIDialogOptionsPageCompat(ATOptions& opts)
	: ATUIDialogOptionsPage(IDD_OPTIONS_COMPAT, opts)
{
	mCheckExternalDB.SetOnClicked([this] { OnExternalDBToggled(); });
	mButtonUnmuteAll.SetOnClicked([this] { OnUnmuteAll(); });
	mButtonBrowse.SetOnClicked([this] { OnBrowse(); });
}

bool ATUIDialogOptionsPageCompat::OnLoaded() {
	AddProxy(&mCheckExternalDB, IDC_COMPAT_EXTERNAL);
	AddProxy(&mButtonUnmuteAll, IDC_UNMUTE_ALL);
	AddProxy(&mButtonBrowse, IDC_BROWSE);

	AddHelpEntry(IDC_COMPAT_ENABLE, L"Show compatibility warnings",
		L"If enabled, detect and warn about compatibility issues with loaded titles.");

	AddHelpEntry(IDC_COMPAT_INTERNAL, L"Use internal database",
		L"Use built-in compatibility database.");

	AddHelpEntry(IDC_COMPAT_EXTERNAL, L"Use external database",
		L"Use compatibility database in external file.");

	LinkHelpEntry(IDC_COMPAT_EXTERNAL, IDC_PATH);
	LinkHelpEntry(IDC_COMPAT_EXTERNAL, IDC_BROWSE);

	OnDataExchange(false);

	return ATUIDialogOptionsPage::OnLoaded();
}

void ATUIDialogOptionsPageCompat::OnDataExchange(bool write) {
	if (write) {
		mOptions.mbCompatEnable = IsButtonChecked(IDC_COMPAT_ENABLE);
		mOptions.mbCompatEnableInternalDB = IsButtonChecked(IDC_COMPAT_INTERNAL);
		mOptions.mbCompatEnableExternalDB = IsButtonChecked(IDC_COMPAT_EXTERNAL);
		GetControlText(IDC_PATH, mOptions.mCompatExternalDBPath);
	} else {
		CheckButton(IDC_COMPAT_ENABLE, mOptions.mbCompatEnable);
		CheckButton(IDC_COMPAT_INTERNAL, mOptions.mbCompatEnableInternalDB);
		CheckButton(IDC_COMPAT_EXTERNAL, mOptions.mbCompatEnableExternalDB);
		SetControlText(IDC_PATH, mOptions.mCompatExternalDBPath.c_str());

		UpdateEnables();
	}
}

void ATUIDialogOptionsPageCompat::OnUnmuteAll() {
	if (Confirm(L"This will unmute all compatibility warnings previously muted. Are you sure?"))
		ATCompatUnmuteAllTitles();
}

void ATUIDialogOptionsPageCompat::OnExternalDBToggled() {
	UpdateEnables();
}

void ATUIDialogOptionsPageCompat::OnBrowse() {
	const auto& path = VDGetLoadFileName('cpdc', (VDGUIHandle)mhdlg, L"Load external compatibility database", g_ATUIFileFilter_LoadCompatEngine, L"atcpengine");

	if (!path.empty()) {
		try {
			ATCompatLoadExtDatabase(path.c_str(), true);

			SetControlText(IDC_PATH, path.c_str());
		} catch(const MyError& e) {
			ShowError(e);
		}
	}
}

void ATUIDialogOptionsPageCompat::UpdateEnables() {
	bool extdb = mCheckExternalDB.GetChecked();

	EnableControl(IDC_PATH, extdb);
	EnableControl(IDC_BROWSE, extdb);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogOptions final : public ATUIPagedDialog {
public:
	ATUIDialogOptions(ATOptions& opts);

protected:
	void OnPopulatePages();

	ATOptions& mOptions;
};

ATUIDialogOptions::ATUIDialogOptions(ATOptions& opts)
	: ATUIPagedDialog(IDD_OPTIONS)
	, mOptions(opts) 
{
}

void ATUIDialogOptions::OnPopulatePages() {
	AddPage(L"Startup", vdmakeunique<ATUIDialogOptionsPageStartup>(mOptions));
	AddPage(L"Display", vdmakeunique<ATUIDialogOptionsPageDisplay>(mOptions));
	AddPage(L"Display Effects", vdmakeunique<ATUIDialogOptionsPageDisplayEffects>(mOptions));
	AddPage(L"Error Handling", vdmakeunique<ATUIDialogOptionsPageErrors>(mOptions));
	AddPage(L"File Types", vdmakeunique<ATUIDialogOptionsPageFileAssoc>(mOptions));
	AddPage(L"Flash Emulation", vdmakeunique<ATUIDialogOptionsPageFlash>(mOptions));
	AddPage(L"Media", vdmakeunique<ATUIDialogOptionsPageMedia>(mOptions));
	AddPage(L"UI", vdmakeunique<ATUIDialogOptionsPageUI>(mOptions));
	AddPage(L"Settings", vdmakeunique<ATUIDialogOptionsPageSettings>(mOptions));
	AddPage(L"Compatibility", vdmakeunique<ATUIDialogOptionsPageCompat>(mOptions));
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogOptions(VDGUIHandle hParent) {
	ATOptions opts(g_ATOptions);
	ATUIDialogOptions dlg(opts);

	if (dlg.ShowDialog(hParent)) {
		ATOptions prevOpts(g_ATOptions);
		g_ATOptions = opts;
		g_ATOptions.mbDirty = true;
		ATOptionsSave();
		ATOptionsRunUpdateCallbacks(&prevOpts);
	}
}
