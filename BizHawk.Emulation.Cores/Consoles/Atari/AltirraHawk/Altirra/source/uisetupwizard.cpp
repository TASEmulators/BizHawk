//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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
#include <vd2/system/vdalloc.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"
#include "settings.h"
#include "simulator.h"
#include "gtia.h"
#include "firmwaremanager.h"
#include "uiaccessors.h"
#include "uitypes.h"

extern ATSimulator g_sim;

void ATUIScanForFirmware(VDGUIHandle hParent, ATFirmwareManager& fwmgr);

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardMessage : public VDDialogFrameW32 {
public:
	ATUIDialogSetupWizardMessage() : VDDialogFrameW32(IDD_WIZARD_MESSAGE) {}

protected:
	bool OnLoaded() override;
	
	virtual void SetupMessage() = 0;

	VDUIProxyRichEditControl mRichEdit;
};

bool ATUIDialogSetupWizardMessage::OnLoaded() {
	AddProxy(&mRichEdit, IDC_MESSAGE);

	mRichEdit.SetReadOnlyBackground();

	SetupMessage();
	return false;
};

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardWelcome final : public ATUIDialogSetupWizardMessage {
public:
	void SetupMessage() override;
};

void ATUIDialogSetupWizardWelcome::SetupMessage() {
	mRichEdit.SetTextRTF("{\\rtf{\\fonttbl{\\f0\\fcharset0 MS Shell Dlg;}}\\pard\\f0\\fs16\\sa240\\slmult1\\sl0 \
Welcome to Altirra!\
\\par \
This wizard will help you configure the emulator for the first time. To begin, click Next.\
\\par \
If you would like to skip the setup process, click Close to exit this wizard and start \
the emulator. All of the settings here can also be set up manually. You can also repeat \
the first time setup process via the Tools menu at any time. \\par}");
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardFirmware final : public VDDialogFrameW32 {
public:
	ATUIDialogSetupWizardFirmware();

private:
	bool OnLoaded() override;

	void OnScan();
	void UpdateFirmwareStatus();

	VDUIProxyListView mList;
	VDUIProxyButtonControl mScanButton;

	struct FirmwareItem final : public vdrefcounted<IVDUIListViewVirtualItem> {
		FirmwareItem(const wchar_t *name) : mpName(name), mbPresent(false) {}

		void GetText(int subItem, VDStringW& s) const;

		const wchar_t *mpName;
		bool mbPresent;
	};

	vdrefptr<FirmwareItem> mpFirmwareItems[4];
};

ATUIDialogSetupWizardFirmware::ATUIDialogSetupWizardFirmware()
	: VDDialogFrameW32(IDD_WIZARD_FIRMWARE)
{
	mScanButton.SetOnClicked([this] { OnScan(); });
}

void ATUIDialogSetupWizardFirmware::FirmwareItem::GetText(int subItem, VDStringW& s) const {
	if (subItem == 0)
		s = mpName;
	else if (subItem == 1)
		s = (mbPresent ? L"OK" : L"Not found");
}

bool ATUIDialogSetupWizardFirmware::OnLoaded() {
	AddProxy(&mList, IDC_LIST);
	AddProxy(&mScanButton, IDC_SCAN);

	mpFirmwareItems[0] = new FirmwareItem(L"800 OS (OS-B)");
	mpFirmwareItems[1] = new FirmwareItem(L"XL/XE OS");
	mpFirmwareItems[2] = new FirmwareItem(L"BASIC");
	mpFirmwareItems[3] = new FirmwareItem(L"5200 OS");

	mList.InsertColumn(0, L"ROM Image", 0);
	mList.InsertColumn(1, L"Status", 0);

	for(FirmwareItem *p : mpFirmwareItems)
		mList.InsertVirtualItem(-1, p);

	mList.AutoSizeColumns();

	SetControlText(IDC_MESSAGE, L"\
Altirra has internal replacements for all standard ROMs. However, \
if you have original ROM images, you can set these up now for better \
compatibility.\n\
\n\
If you do not have ROM images or do not want to set them up now, just click Next.");

	UpdateFirmwareStatus();

	SetFocusToControl(IDC_SCAN);
	return true;
}

void ATUIDialogSetupWizardFirmware::OnScan() {
	ATUIScanForFirmware((VDGUIHandle)GetWindowHandle(), *g_sim.GetFirmwareManager());
	UpdateFirmwareStatus();
}

void ATUIDialogSetupWizardFirmware::UpdateFirmwareStatus() {
	ATFirmwareManager& fwm = *g_sim.GetFirmwareManager();

	static const ATFirmwareType kFirmwareTypes[] = {
		kATFirmwareType_Kernel800_OSB,
		kATFirmwareType_KernelXL,
		kATFirmwareType_Basic,
		kATFirmwareType_Kernel5200,
	};

	static_assert(vdcountof(kFirmwareTypes) == vdcountof(mpFirmwareItems), "Firmware lists are inconsistent");

	for(size_t i=0; i<vdcountof(kFirmwareTypes); ++i) {
		uint64 fwid = fwm.GetCompatibleFirmware(kFirmwareTypes[i]);
		const bool isPresent = (fwid && fwid >= kATFirmwareId_Custom);

		if (mpFirmwareItems[i]->mbPresent != isPresent) {
			mpFirmwareItems[i]->mbPresent = isPresent;

			mList.RefreshItem(i);
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardPostFirmware final : public ATUIDialogSetupWizardMessage {
public:
	void SetupMessage() override;
};

void ATUIDialogSetupWizardPostFirmware::SetupMessage() {
	mRichEdit.SetTextRTF("{\\rtf{\\fonttbl{\\f0\\fnil\\fcharset0 MS Shell Dlg;}}\\pard\\sa120\\slmult1\\sl0\\fs16\\f0 \
ROM image setup is complete. \
\\par \
If you want to set up more firmware ROM images in the future, this can \
be done through the menu option System | Firmware Images. \
\\par}"
	);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardSelectSystem final : public VDDialogFrameW32 {
public:
	ATUIDialogSetupWizardSelectSystem();

private:
	void OnDataExchange(bool write) override;
};

ATUIDialogSetupWizardSelectSystem::ATUIDialogSetupWizardSelectSystem()
	: VDDialogFrameW32(IDD_WIZARD_SELECTTYPE)
{
}

void ATUIDialogSetupWizardSelectSystem::OnDataExchange(bool write) {
	const bool is5200 = (g_sim.GetHardwareMode() == kATHardwareMode_5200);

	if (write) {
		bool select5200 = IsButtonChecked(IDC_TYPE_5200);

		if (is5200 != select5200) {
			uint32 profileId = ATGetDefaultProfileId(select5200 ? kATDefaultProfile_5200 : kATDefaultProfile_XL);

			ATSettingsSwitchProfile(profileId);
		}
	} else {
		CheckButton(IDC_TYPE_COMPUTER, !is5200);
		CheckButton(IDC_TYPE_5200, is5200);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardSelectVideoType final : public VDDialogFrameW32 {
public:
	ATUIDialogSetupWizardSelectVideoType();

private:
	void OnDataExchange(bool write) override;
};

ATUIDialogSetupWizardSelectVideoType::ATUIDialogSetupWizardSelectVideoType()
	: VDDialogFrameW32(IDD_WIZARD_SELECTVIDEOTYPE)
{
}

void ATUIDialogSetupWizardSelectVideoType::OnDataExchange(bool write) {
	bool isNTSC = false;

	// We call this dialog 'video type', but what we're really after
	// here is frame rate.
	switch(g_sim.GetVideoStandard()) {
		case kATVideoStandard_NTSC:
		case kATVideoStandard_PAL60:
			isNTSC = true;
			break;
	}

	if (write) {
		bool selectNTSC = IsButtonChecked(IDC_TYPE_NTSC);

		if (isNTSC != selectNTSC) {
			ATSetVideoStandard(selectNTSC ? kATVideoStandard_NTSC : kATVideoStandard_PAL);
		}
	} else {
		CheckButton(IDC_TYPE_NTSC, isNTSC);
		CheckButton(IDC_TYPE_PAL, !isNTSC);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardExperience final : public VDDialogFrameW32 {
public:
	ATUIDialogSetupWizardExperience();

private:
	void OnDataExchange(bool write) override;
};

ATUIDialogSetupWizardExperience::ATUIDialogSetupWizardExperience()
	: VDDialogFrameW32(IDD_WIZARD_EXPERIENCE)
{
}

void ATUIDialogSetupWizardExperience::OnDataExchange(bool write) {
	const bool isAuthentic = g_sim.GetGTIA().GetArtifactingMode() != ATGTIAEmulator::kArtifactNone;

	if (write) {
		bool selectAuthentic = IsButtonChecked(IDC_TYPE_AUTHENTIC);

		if (isAuthentic != selectAuthentic) {
			if (selectAuthentic) {
				bool isNTSC = false;

				// We call this dialog 'video type', but what we're really after
				// here is frame rate.
				switch(g_sim.GetVideoStandard()) {
					case kATVideoStandard_NTSC:
					case kATVideoStandard_NTSC50:
						isNTSC = true;
						break;
				}

				g_sim.GetGTIA().SetArtifactingMode(ATGTIAEmulator::kArtifactAutoHi);

				g_sim.SetCassetteSIOPatchEnabled(false);
				g_sim.SetDiskSIOPatchEnabled(false);
				g_sim.SetDiskAccurateTimingEnabled(true);
				ATUISetDriveSoundsEnabled(true);
				ATUISetDisplayFilterMode(kATDisplayFilterMode_Bilinear);
			} else {
				ATUISetDriveSoundsEnabled(false);
				g_sim.SetCassetteSIOPatchEnabled(true);
				g_sim.SetDiskSIOPatchEnabled(true);
				g_sim.SetDiskAccurateTimingEnabled(false);
				g_sim.GetGTIA().SetArtifactingMode(ATGTIAEmulator::kArtifactNone);
				ATUISetDisplayFilterMode(kATDisplayFilterMode_SharpBilinear);
				ATUISetViewFilterSharpness(+1);
			}
		}
	} else {
		CheckButton(IDC_TYPE_AUTHENTIC, isAuthentic);
		CheckButton(IDC_TYPE_STREAMLINED, !isAuthentic);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardFinish800 final : public ATUIDialogSetupWizardMessage {
public:
	void SetupMessage() override;
};

void ATUIDialogSetupWizardFinish800::SetupMessage() {
	mRichEdit.SetTextRTF("{\\rtf{\\fonttbl{\\f0\\fnil\\fcharset0 MS Shell Dlg;}}\\pard\\sa120\\slmult1\\sl0\\fs16\\f0\
Setup is now complete.\
\\par \
Click Finish to exit and power up the emulated computer. You can then use the \
File | Boot Image... menu option to boot a disk, cartridge, or cassette tape image, or start \
a program.\n\
\\par \
If you want to repeat this process in the future, the setup wizard can be restarted via the \
Tools menu.\\par}");
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizardFinish5200 final : public ATUIDialogSetupWizardMessage {
public:
	void SetupMessage() override;
};

void ATUIDialogSetupWizardFinish5200::SetupMessage() {
	mRichEdit.SetTextRTF("{\\rtf{\\fonttbl{\\f0\\fnil\\fcharset0 MS Shell Dlg;}}\\pard\\sa120\\slmult1\\sl0\\fs16\\f0\
Setup is now complete.\
\\par \
Click Finish to exit and power up the emulated console. The 5200 needs a cartridge \
to work, so select File | Boot Image... to attach and start a cartridge image.\n\
\\par \
You will probably want to check your controller settings. The default setup \
binds F2-F4, the digit key row, arrow keys, and Ctrl/Shift to joystick 1. Alternate \
bindings can be selected from the Input menu or new ones can be defined in Input | Input Mappings.\n\
\\par \
If you want to repeat this process in the future, choose Tools | First Time Setup... from the menu.\
\\par}");
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSetupWizard final : public VDDialogFrameW32 {
public:
	ATUIDialogSetupWizard();

protected:
	bool OnLoaded() override;
	void OnDestroy() override;
	bool OnCommand(uint32 id, uint32 extcode) override;
	void UpdateEnables();
	void SelectPage(int index);

	int GetPrevPage() const;
	int GetNextPage() const;

	int mSelectedPage;
	bool mbWentPastFirstPage = false;
	vdautoptr<VDDialogFrameW32> mpChildPage;

	VDUIProxyRichEditControl mStepsList;
};

ATUIDialogSetupWizard::ATUIDialogSetupWizard()
	: VDDialogFrameW32(IDD_WIZARD)
{
}

bool ATUIDialogSetupWizard::OnLoaded() {
	AddProxy(&mStepsList, IDC_STEPS_LIST);

	mStepsList.SetReadOnlyBackground();

	ShowControl(IDC_STEP_AREA, false);
	mSelectedPage = -1;

	SelectPage(0);

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogSetupWizard::OnDestroy() {
	if (mpChildPage) {
		mResizer.Remove(mpChildPage->GetWindowHandle());
		mpChildPage->Destroy();
		mpChildPage = nullptr;
	}

	// Force a hard reset if the user went past the welcome page since something
	// may have changed in machine configuration.
	if (mbWentPastFirstPage) {
		g_sim.LoadROMs();
		g_sim.ColdReset();
	}
}

bool ATUIDialogSetupWizard::OnCommand(uint32 id, uint32 extcode) {
	if (id == IDC_PREV) {
		int page = GetPrevPage();

		if (page >= 0)
			SelectPage(page);
	} else if (id == IDC_NEXT) {
		// We have to sync the page to recompute the Next page as it may depend on
		// settings on the current page.
		if (mpChildPage)
			mpChildPage->Sync(true);

		int page = GetNextPage();

		if (page >= 0)
			SelectPage(page);
	}
	return false;
}

void ATUIDialogSetupWizard::UpdateEnables() {
	EnableControl(IDC_PREV, GetPrevPage() >= 0);

	const bool canDoNext = (GetNextPage() >= 0);
	EnableControl(IDC_NEXT, canDoNext);
	EnableControl(IDOK, !canDoNext);
	ShowControl(IDC_NEXT, canDoNext);
	ShowControl(IDOK, !canDoNext);

	SetFocusToControl(canDoNext ? IDC_NEXT : IDOK);
}

void ATUIDialogSetupWizard::SelectPage(int index) {
	if (mSelectedPage == index)
		return;

	if (index > 0)
		mbWentPastFirstPage = true;

	if (mSelectedPage >= 0) {
		if (mpChildPage) {
			mpChildPage->Sync(true);
			mResizer.Remove(mpChildPage->GetWindowHandle());
			mpChildPage->Destroy();
			mpChildPage = nullptr;
		}
	}

	mSelectedPage = index;

	if (mSelectedPage >= 0) {
		switch(mSelectedPage) {
			case 0: mpChildPage = new ATUIDialogSetupWizardWelcome(); break;
			case 10: mpChildPage = new ATUIDialogSetupWizardFirmware(); break;
			case 11: mpChildPage = new ATUIDialogSetupWizardPostFirmware(); break;
			case 20: mpChildPage = new ATUIDialogSetupWizardSelectSystem(); break;
			case 21: mpChildPage = new ATUIDialogSetupWizardSelectVideoType(); break;
			case 30: mpChildPage = new ATUIDialogSetupWizardExperience(); break;
			case 40: mpChildPage = new ATUIDialogSetupWizardFinish800(); break;
			case 41: mpChildPage = new ATUIDialogSetupWizardFinish5200(); break;
		}

		if (mpChildPage->Create((VDGUIHandle)mhdlg)) {
			mResizer.AddAlias(mpChildPage->GetWindowHandle(), GetControl(IDC_STEP_AREA), 0);
			mpChildPage->Show();
		}
	}

	static const struct { int pageMin; int pageMax; const wchar_t *label; } kPageLabels[] = {
		{ 0, 9, L"Welcome" },
		{ 10, 19, L"Setup firmware" },
		{ 20, 29, L"Select system" },
		{ 30, 39, L"Experience" },
		{ 40, 49, L"Finish" },
	};

	int selLine = -1;
	VDStringA buf;
	VDStringA s("{\\rtf \\fs16\\sa90 ");

	int curLine = 0;
	for(const auto& pageLabel : kPageLabels) {
		if (index >= pageLabel.pageMin && index <= pageLabel.pageMax) {
			s += "{\\b ";
			mStepsList.AppendEscapedRTF(s, pageLabel.label);
			s += "}";
			selLine = curLine;
		} else
			mStepsList.AppendEscapedRTF(s, pageLabel.label);

		s += "\\par ";

		++curLine;
	}

	s += "}";

	mStepsList.SetTextRTF(s.c_str());

	if (selLine >= 0) {
		mStepsList.SetCaretPos(selLine, 0);
		mStepsList.EnsureCaretVisible();
	}

	UpdateEnables();
}

int ATUIDialogSetupWizard::GetPrevPage() const {
	switch(mSelectedPage) {
		case 0:		return -1;
		case 10:	return 0;
		case 11:	return 10;
		case 20:	return 11;
		case 21:	return 20;
		case 30:	return g_sim.GetHardwareMode() == kATHardwareMode_5200 ? 20 : 21;
		case 40:	return 30;
		case 41:	return 30;
		default:	return 0;
	}
}

int ATUIDialogSetupWizard::GetNextPage() const {
	switch(mSelectedPage) {
		case 0:		return 10;
		case 10:	return 11;
		case 11:	return 20;
		case 20:	return g_sim.GetHardwareMode() == kATHardwareMode_5200 ? 30 : 21;
		case 21:	return 30;
		case 30:	return g_sim.GetHardwareMode() == kATHardwareMode_5200 ? 41 : 40;
		default:	return -1;
	}
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogSetupWizard(VDGUIHandle hParent) {
	ATUIDialogSetupWizard dlg;

	dlg.ShowDialog(hParent);
}
