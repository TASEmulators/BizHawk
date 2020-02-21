//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <array>
#include <vd2/system/filesys.h>
#include <at/atui/uicommandmanager.h>
#include <at/atcore/devicemanager.h>
#include "uiaccessors.h"
#include "uicaptionupdater.h"
#include "uidevices.h"
#include "uifirmwaremenu.h"
#include "uipageddialog.h"
#include "uikeyboard.h"
#include "resource.h"
#include "constants.h"
#include "cartridge.h"
#include "diskinterface.h"
#include "simulator.h"

extern ATSimulator g_sim;
extern ATUIKeyboardOptions g_kbdOpts;

extern void ATUISwitchKernel(uint64 id);
extern void ATUISwitchBasic(uint64 id);

class ATUIDialogSysConfigPage : public ATUIDialogPage {
public:
	ATUIDialogSysConfigPage(uint32 id);
	~ATUIDialogSysConfigPage();

	void OnDestroy() override;
	void OnDataExchange(bool write) override;

protected:
	struct CmdMapEntry {
		const char *mpCommand;
		const wchar_t *mpLabel;
	};

	class CmdBinding {
	public:
		virtual ~CmdBinding() = default;

		virtual void Read() = 0;
		virtual void Write() = 0;
	};

	class CmdComboBinding final : public CmdBinding {
	public:
		CmdComboBinding(vdvector_view<const CmdMapEntry> table);

		void SetTable(vdvector_view<const CmdMapEntry> table) { mLookupTable = table; }
		void Bind(VDUIProxyComboBoxControl *combo);
		void Read();
		void Write();

	private:
		VDUIProxyComboBoxControl *mpControl {};
		vdvector_view<const CmdMapEntry> mLookupTable;
		vdfastvector<uint32> mActiveEntries;
	};

	class CmdRadioBinding final : public CmdBinding {
	public:
		void Bind(const char *cmd, VDUIProxyButtonControl *control);
		void Read();
		void Write();

	private:
		struct Entry {
			const char *mpCommand;
			VDUIProxyButtonControl *mpControl;
		};

		vdfastvector<Entry> mEntries;
	};

	class CmdBoolBinding final : public CmdBinding {
	public:
		CmdBoolBinding(const char *enableOrToggleCmd, const char *disableCmd = nullptr) : mpEnableOrToggleCmd(enableOrToggleCmd), mpDisableCmd(disableCmd) {}

		VDUIProxyButtonControl& GetView() { return *mpControl; }

		void Bind(VDUIProxyButtonControl *ctl);
		void Read();
		void Write();

	private:
		VDUIProxyButtonControl *mpControl {};
		const char *mpEnableOrToggleCmd {};
		const char *mpDisableCmd {};
	};

	class CmdTriggerBinding final : public CmdBinding {
	public:
		CmdTriggerBinding(const char *cmd) : mpCommand(cmd) {}

		void Bind(VDUIProxyButtonControl *ctl);
		void Read();
		void Write();

	private:
		VDUIProxyButtonControl *mpControl {};
		const char *mpCommand {};
	};

	CmdBoolBinding *BindCheckbox(uint32 id, const char *cmd);

	template<typename T, typename... Args>
	T *AllocateObject(Args&&... args);

	template<typename T>
	void AddAutoFreeObject(T *p);

	void ClearBindings();
	void AddAutoReadBinding(CmdBinding *binding);
	void AutoReadBindings();

	struct AllocatedObject {
		void *mpObject;
		void (*mpFreeFn)(void *);
	};

	vdfastvector<AllocatedObject> mAllocatedObjects;
	vdfastvector<CmdBinding *> mAutoReadBindings;
};

ATUIDialogSysConfigPage::ATUIDialogSysConfigPage(uint32 id)
	: ATUIDialogPage(id)
{
}

ATUIDialogSysConfigPage::~ATUIDialogSysConfigPage() {
	ClearBindings();
}

void ATUIDialogSysConfigPage::OnDestroy() {
	ATUIDialogPage::OnDestroy();

	ClearBindings();
}

void ATUIDialogSysConfigPage::OnDataExchange(bool write) {
	if (!write)
		AutoReadBindings();
}

ATUIDialogSysConfigPage::CmdBoolBinding *ATUIDialogSysConfigPage::BindCheckbox(uint32 id, const char *cmd) {
	auto *view = AllocateObject<VDUIProxyButtonControl>();
	auto *binding = AllocateObject<CmdBoolBinding>(cmd);

	AddProxy(view, id);
	binding->Bind(view);
	view->SetOnClicked([binding] { binding->Write(); });

	AddAutoReadBinding(binding);

	return binding;
}

template<typename T, typename... Args>
T *ATUIDialogSysConfigPage::AllocateObject(Args&&... args) {
	T *p = new T(std::forward<Args>(args)...);

	try {
		AddAutoFreeObject(p);
	} catch(...) {
		delete p;
		throw;
	}

	return p;
}

template<typename T>
void ATUIDialogSysConfigPage::AddAutoFreeObject(T *p) {
	mAllocatedObjects.push_back({p, [](void *p) { delete (T *)p; }});
}

void ATUIDialogSysConfigPage::ClearBindings() {
	mAutoReadBindings.clear();

	while(!mAllocatedObjects.empty()) {
		AllocatedObject obj = mAllocatedObjects.back();
		mAllocatedObjects.pop_back();

		obj.mpFreeFn(obj.mpObject);
	}
}

void ATUIDialogSysConfigPage::AddAutoReadBinding(CmdBinding *binding) {
	mAutoReadBindings.push_back(binding);
}

void ATUIDialogSysConfigPage::AutoReadBindings() {
	for(CmdBinding *p : mAutoReadBindings)
		p->Read();
}

ATUIDialogSysConfigPage::CmdComboBinding::CmdComboBinding(vdvector_view<const CmdMapEntry> table) {
	mLookupTable = table;
}

void ATUIDialogSysConfigPage::CmdComboBinding::Bind(VDUIProxyComboBoxControl *combo) {
	mpControl = combo;
	mActiveEntries.clear();
}

void ATUIDialogSysConfigPage::CmdComboBinding::Read() {
	auto& cm = ATUIGetCommandManager();
	vdfastvector<uint32> newEntries;

	uint32 srcIdx = 0;
	uint32 filteredIdx = 0;
	sint32 selIdx = -1;

	for(const auto& entry : mLookupTable) {
		const ATUICommand *cmd = cm.GetCommand(entry.mpCommand);

		if (cmd && (!cmd->mpTestFn || cmd->mpTestFn())) {
			newEntries.push_back(srcIdx);

			if (cmd->mpStateFn && cmd->mpStateFn() != kATUICmdState_None)
				selIdx = (sint32)filteredIdx;

			++filteredIdx;
		}

		++srcIdx;
	}

	if (mActiveEntries != newEntries) {
		mActiveEntries = newEntries;

		mpControl->Clear();

		for(uint32 idx : mActiveEntries)
			mpControl->AddItem(mLookupTable[idx].mpLabel);
	}

	mpControl->SetEnabled(!mActiveEntries.empty());
	mpControl->SetSelection(selIdx);
}

void ATUIDialogSysConfigPage::CmdComboBinding::Write() {
	int idx = mpControl->GetSelection();

	if ((uint32)idx >= mActiveEntries.size())
		return;

	auto& cm = ATUIGetCommandManager();
	const ATUICommand *cmd = cm.GetCommand(mLookupTable[mActiveEntries[idx]].mpCommand);

	if (cmd) {
		if (!cmd->mpTestFn || cmd->mpTestFn()) {
			cmd->mpExecuteFn();

			// Some commands may fail, so we need to re-read in case the change got rolled back.
			Read();
		}
	}
}

void ATUIDialogSysConfigPage::CmdRadioBinding::Bind(const char *cmd, VDUIProxyButtonControl *control) {
	mEntries.push_back({ cmd, control });
}

void ATUIDialogSysConfigPage::CmdRadioBinding::Read() {
	auto& cm = ATUIGetCommandManager();

	for(const auto& entry : mEntries) {
		const ATUICommand *cmd = cm.GetCommand(entry.mpCommand);
		if (!cmd)
			continue;

		if (!cmd->mpTestFn || cmd->mpTestFn()) {
			entry.mpControl->SetEnabled(true);

			if (cmd->mpStateFn && cmd->mpStateFn() != kATUICmdState_None)
				entry.mpControl->SetChecked(true);
			else
				entry.mpControl->SetChecked(false);
		} else {
			entry.mpControl->SetEnabled(false);
		}
	}
}

void ATUIDialogSysConfigPage::CmdRadioBinding::Write() {
	auto& cm = ATUIGetCommandManager();

	for(const auto& entry : mEntries) {
		if (entry.mpControl->GetChecked()) {
			const ATUICommand *cmd = cm.GetCommand(entry.mpCommand);
			if (cmd && (!cmd->mpTestFn || cmd->mpTestFn())) {
				cmd->mpExecuteFn();
				Read();
			}
			break;
		}
	}
}

void ATUIDialogSysConfigPage::CmdBoolBinding::Bind(VDUIProxyButtonControl *ctl) {
	mpControl = ctl;
}

void ATUIDialogSysConfigPage::CmdBoolBinding::Read() {
	auto& cm = ATUIGetCommandManager();
	const ATUICommand *cmd = cm.GetCommand(mpEnableOrToggleCmd);

	if (!cmd)
		return;

	bool checked = false;
	bool enabled = false;

	if (!cmd->mpTestFn || cmd->mpTestFn()) {
		enabled = true;

		if (cmd->mpStateFn && cmd->mpStateFn() != kATUICmdState_None)
			checked = true;	
	}

	mpControl->SetEnabled(enabled);
	mpControl->SetChecked(checked);
}

void ATUIDialogSysConfigPage::CmdBoolBinding::Write() {
	auto& cm = ATUIGetCommandManager();
	const bool newState = mpControl->GetChecked();
	const ATUICommand *cmd = cm.GetCommand(mpEnableOrToggleCmd);

	if (!cmd)
		return;

	bool checked = false;

	if (!cmd->mpTestFn || cmd->mpTestFn()) {
		if (cmd->mpStateFn && cmd->mpStateFn() != kATUICmdState_None)
			checked = true;	

		if (newState != checked) {
			const ATUICommand *cmd2 = newState || !mpDisableCmd ? cmd : cm.GetCommand(mpDisableCmd);

			if (cmd2) {
				cmd2->mpExecuteFn();
				Read();
			}
		}
	}
}

void ATUIDialogSysConfigPage::CmdTriggerBinding::Bind(VDUIProxyButtonControl *ctl) {
	mpControl = ctl;
}

void ATUIDialogSysConfigPage::CmdTriggerBinding::Read() {
	auto& cm = ATUIGetCommandManager();
	const ATUICommand *cmd = cm.GetCommand(mpCommand);

	if (!cmd)
		return;

	bool enabled = false;

	if (!cmd->mpTestFn || cmd->mpTestFn())
		enabled = true;

	mpControl->SetEnabled(enabled);
}

void ATUIDialogSysConfigPage::CmdTriggerBinding::Write() {
	auto& cm = ATUIGetCommandManager();
	const ATUICommand *cmd = cm.GetCommand(mpCommand);

	if (!cmd)
		return;

	if (!cmd->mpTestFn || cmd->mpTestFn()) {
		if (cmd) {
			cmd->mpExecuteFn();
			Read();
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigOverview final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigOverview();

private:
	bool OnLoaded() override;
	bool OnLinkActivated(const wchar_t *link);
	void CopyConfiguration();
	void RemakeOverview();

	VDUIProxyRichEditControl mResultView;
	VDUIProxyButtonControl mCopyView;

	VDStringA mRtfBuffer;
};

ATUIDialogSysConfigOverview::ATUIDialogSysConfigOverview()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_OVERVIEW)
{
	mCopyView.SetOnClicked([this] { CopyConfiguration(); });

	mResultView.SetOnLinkSelected([this](const wchar_t *link) { return OnLinkActivated(link); });
}

bool ATUIDialogSysConfigOverview::OnLoaded() {
	AddProxy(&mResultView, IDC_RICHEDIT);
	AddProxy(&mCopyView, IDC_COPY);

	mResultView.SetReadOnlyBackground();
	mResultView.DisableCaret();
	mResultView.DisableSelectOnFocus();

	RemakeOverview();

	return true;
}

bool ATUIDialogSysConfigOverview::OnLinkActivated(const wchar_t *link) {
	mpParentPagedDialog->SwitchToPage("devices");
	return true;
}

void ATUIDialogSysConfigOverview::CopyConfiguration() {
	mResultView.SelectAll();
	mResultView.Copy();
	mResultView.SetCaretPos(0, 0);
}

void ATUIDialogSysConfigOverview::RemakeOverview() {
	// This is a hack to prevent the caret from showing up on the rich edit control.
	Focus();

	mRtfBuffer = R"---({\rtf1\deftab1920{\colortbl;\red0\green0\blue255;})---";

	mRtfBuffer.append("\\pard\\li1920\\fi-1920 ");
	mResultView.AppendEscapedRTF(mRtfBuffer, L"Base system");
	mRtfBuffer += "\\tab ";

	const wchar_t *vsname = L"";
	switch(g_sim.GetVideoStandard()) {
		case kATVideoStandard_NTSC:			vsname = L"NTSC"; break;
		case kATVideoStandard_NTSC50:		vsname = L"NTSC50"; break;
		case kATVideoStandard_PAL:			vsname = L"PAL"; break;
		case kATVideoStandard_PAL60:		vsname = L"PAL60"; break;
		case kATVideoStandard_SECAM:		vsname = L"SECAM"; break;
	}

	mResultView.AppendEscapedRTF(mRtfBuffer, vsname);
	mResultView.AppendEscapedRTF(mRtfBuffer, L" ");

	const wchar_t *model = L"";
	ATMemoryMode defaultMemory = kATMemoryMode_64K;
	switch(g_sim.GetHardwareMode()) {
		case kATHardwareMode_800:
			model = L"800";
			defaultMemory = kATMemoryMode_48K;
			break;

		case kATHardwareMode_800XL:
			model = L"800XL";
			defaultMemory = kATMemoryMode_64K;
			break;

		case kATHardwareMode_5200:
			model = L"5200 SuperSystem";
			defaultMemory = kATMemoryMode_16K;
			break;

		case kATHardwareMode_XEGS:
			model = L"XE Game System (XEGS)";
			defaultMemory = kATMemoryMode_64K;
			break;

		case kATHardwareMode_1200XL:
			model = L"1200XL";
			defaultMemory = kATMemoryMode_64K;
			break;

		case kATHardwareMode_130XE:	
			model = L"130XE";
			defaultMemory = kATMemoryMode_128K;
			break;
	}

	mResultView.AppendEscapedRTF(mRtfBuffer, model);

	if (g_sim.GetMemoryMode() != defaultMemory) {
		const wchar_t *memstr = nullptr;

		switch(g_sim.GetMemoryMode()) {
			case kATMemoryMode_48K:			memstr = L"48K"; break;
			case kATMemoryMode_52K:			memstr = L"52K"; break;
			case kATMemoryMode_64K:			memstr = L"64K"; break;
			case kATMemoryMode_128K:		memstr = L"128K"; break;
			case kATMemoryMode_320K:		memstr = L"320K"; break;
			case kATMemoryMode_576K:		memstr = L"576K"; break;
			case kATMemoryMode_1088K:		memstr = L"1088K"; break;
			case kATMemoryMode_16K:			memstr = L"16K"; break;
			case kATMemoryMode_8K:			memstr = L"8K"; break;
			case kATMemoryMode_24K:			memstr = L"24K"; break;
			case kATMemoryMode_32K:			memstr = L"32K"; break;
			case kATMemoryMode_40K:			memstr = L"40K"; break;
			case kATMemoryMode_320K_Compy:	memstr = L"320K Compy"; break;
			case kATMemoryMode_576K_Compy:	memstr = L"576K Compy"; break;
			case kATMemoryMode_256K:		memstr = L"256K"; break;
		}

		if (memstr) {
			mResultView.AppendEscapedRTF(mRtfBuffer, L" (");
			mResultView.AppendEscapedRTF(mRtfBuffer, memstr);
			mResultView.AppendEscapedRTF(mRtfBuffer, L")");
		}
	}

	mRtfBuffer.append("\\par\\li1920\\fi-1920 ");
	mResultView.AppendEscapedRTF(mRtfBuffer, L"Additional Devices");
	mRtfBuffer += "\\tab ";

	vdvector<VDStringW> devices;
	for(IATDevice *dev : g_sim.GetDeviceManager()->GetDevices(true, true)) {
		ATDeviceInfo info;
		dev->GetDeviceInfo(info);
		devices.emplace_back(info.mpDef->mpName);
	}

	if (devices.empty()) {
		mResultView.AppendEscapedRTF(mRtfBuffer, L"None");
	} else {
		bool first = true;

		for(const VDStringW& s : devices) {
			if (first)
				first = false;
			else
				mResultView.AppendEscapedRTF(mRtfBuffer, L", ");

			mResultView.AppendEscapedRTF(mRtfBuffer, s.c_str());
		}
	}

	mRtfBuffer.append("\\par\\li1920\\fi-1920 ");
	mResultView.AppendEscapedRTF(mRtfBuffer, L"OS Firmware");

	mRtfBuffer += "\\tab ";

	ATFirmwareInfo fwInfo;
	g_sim.GetFirmwareManager()->GetFirmwareInfo(g_sim.GetActualKernelId(), fwInfo);

	VDStringW buf;
	buf.sprintf(L"%ls [%08X]", fwInfo.mName.c_str(), g_sim.ComputeKernelCRC32());
	mResultView.AppendEscapedRTF(mRtfBuffer, buf.c_str());

	mRtfBuffer.append("\\par\\li1920\\fi-1920 ");
	mResultView.AppendEscapedRTF(mRtfBuffer, L"Mounted Images");

	bool firstImage = true;
	bool foundImage = false;
	for(int i=0; i<15; ++i) {
		ATDiskInterface& di = g_sim.GetDiskInterface(i);
		IATDiskImage *image = di.GetDiskImage();

		if (image) {
			const wchar_t *s = VDFileSplitPath(di.GetPath());

			buf = L"Disk: ";
			buf += s;

			const auto crc = image->GetImageFileCRC();

			if (crc.has_value())
				buf.append_sprintf(L" [%08X]", crc.value());

			if (firstImage) {
				firstImage = false;
				mRtfBuffer += "\\tab ";
			} else
				mRtfBuffer += "\\line ";

			mResultView.AppendEscapedRTF(mRtfBuffer, buf.c_str());
			foundImage = true;
		}
	}

	for(uint32 i=0; i<2; ++i) {
		ATCartridgeEmulator *ce = g_sim.GetCartridge(i);

		if (ce) {
			const wchar_t *path = ce->GetPath();

			if (path) {
				buf = L"Cartridge: ";
				buf += VDFileSplitPath(path);

				const auto crc = ce->GetImageFileCRC();
				if (crc.has_value())
					buf.append_sprintf(L" [%08X]", crc.value());

				if (firstImage) {
					firstImage = false;
					mRtfBuffer += "\\tab ";
				} else
					mRtfBuffer += "\\line ";

				mResultView.AppendEscapedRTF(mRtfBuffer, buf.c_str());
				foundImage = true;
			}
		}
	}

	if (!foundImage) {
		mRtfBuffer.append("\\tab ");
		mResultView.AppendEscapedRTF(mRtfBuffer, L"None");
	}

	bool firmwareIssue = false;

	for(IATDeviceFirmware *fw : g_sim.GetDeviceManager()->GetInterfaces<IATDeviceFirmware>(false, true)) {
		if (fw->GetFirmwareStatus() != ATDeviceFirmwareStatus::OK) {
			firmwareIssue = true;
			break;
		}
	}

	if (firmwareIssue) {
		mRtfBuffer.append("\\par\\par\\li1920\\fi-1920 ");
		mResultView.AppendEscapedRTF(mRtfBuffer, L"Issues");
		mRtfBuffer.append("\\tab ");
		mResultView.AppendEscapedRTF(mRtfBuffer, L"A device has missing or invalid firmware.  ");

		mRtfBuffer += "{\\field {\\*\\fldinst HYPERLINK \"devices\"}";

		// Explicit underline and color override is needed on XP.
		mRtfBuffer += "{\\fldrslt\\ul\\cf1 ";

		mResultView.AppendEscapedRTF(mRtfBuffer, L"Check devices");

		mRtfBuffer += "}}";
	}

	mRtfBuffer += "}";

	mResultView.SetTextRTF(mRtfBuffer.c_str());
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigAssessment final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigAssessment();

private:
	bool OnLoaded() override;

	bool OnLinkActivated(const wchar_t *);
	void Reassess();

	void AssessForAccuracy();
	void AssessForCompatibility();
	void AssessForPerformance();

	void AssessCPU();
	void AssessFastFP();
	void AssessStandardPAL();
	void AssessFirmware();
	void AssessKeyboard();

	void AddFnEntry(const wchar_t *s, const wchar_t *linkText, vdfunction<void()> fn, const wchar_t *linkText2 = nullptr, vdfunction<void()> fn2 = nullptr);
	void AddEntry(const wchar_t *s, const wchar_t *linkText, const char *command, const wchar_t *linkText2 = nullptr, const char *command2 = nullptr);
	bool AddAutoStateEntry(const wchar_t *s, const wchar_t *linkText, const char *command, bool targetState, const wchar_t *linkText2 = nullptr, const char *command2 = nullptr);
	void ExecuteCommand(const char *s);

	VDUIProxyComboBoxControl mTargetView;
	VDUIProxyRichEditControl mResultView;

	enum class AssessmentMode : sint8 {
		None = -1,
		Compatibility,
		Accuracy,
		Performance
	};

	AssessmentMode mAssessmentMode = AssessmentMode::Compatibility;

	vdvector<vdfunction<void()>> mLinkHandlers;
	VDStringA mRtfBuffer;
};

ATUIDialogSysConfigAssessment::ATUIDialogSysConfigAssessment()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_ASSESSMENT)
{
	mTargetView.SetOnSelectionChanged([this](int sel) {
		if (sel >= 0 && sel < 3) {
			AssessmentMode mode = (AssessmentMode)sel;

			if (mAssessmentMode != mode) {
				mAssessmentMode = mode;

				Reassess();
			}
		}
	});

	mResultView.SetOnLinkSelected([this](const wchar_t *link) { return OnLinkActivated(link); });
}

bool ATUIDialogSysConfigAssessment::OnLoaded() {
	AddProxy(&mTargetView, IDC_TARGET);
	AddProxy(&mResultView, IDC_RICHEDIT);

	mResultView.SetReadOnlyBackground();
	mResultView.DisableCaret();
	mResultView.DisableSelectOnFocus();

	mTargetView.AddItem(L"Compatibility");
	mTargetView.AddItem(L"Accuracy");
	mTargetView.AddItem(L"Emulator performance");
	mTargetView.SetSelection(0);

	mTargetView.Focus();
	Reassess();

	return true;
}

bool ATUIDialogSysConfigAssessment::OnLinkActivated(const wchar_t *s) {
	if (*s) {
		uint32 index = (uint32)wcstoul(s, nullptr, 10);

		if (index < mLinkHandlers.size()) {
			const auto fn = mLinkHandlers[index];

			fn();
		}
	}

	return true;
}

void ATUIDialogSysConfigAssessment::Reassess() {
	mLinkHandlers.clear();

	// This is a hack to prevent the caret from showing up on the rich edit control.
	mTargetView.Focus();

	mRtfBuffer = R"---({\rtf1{\colortbl;\red0\green0\blue255;})---";

	mRtfBuffer.append("{\\*\\pn\\pnlvlblt\\pnindent0{\\pntxtb\\'B7}}\\fi-240\\li340 ");

	auto baseSize = mRtfBuffer.size();

	switch(mAssessmentMode) {
		case AssessmentMode::Accuracy:
			AssessForAccuracy();
			break;

		case AssessmentMode::Compatibility:
			AssessForCompatibility();
			break;

		case AssessmentMode::Performance:
			AssessForPerformance();
			break;
	}

	if (mRtfBuffer.size() == baseSize)
		mResultView.AppendEscapedRTF(mRtfBuffer, L"No recommendations.");

	mRtfBuffer += "}";

	mResultView.SetTextRTF(mRtfBuffer.c_str());
}

void ATUIDialogSysConfigAssessment::AssessForAccuracy() {
	AssessFirmware();

	AddAutoStateEntry(L"Disk accesses are being accelerated by SIO patch.", L"Disable disk SIO patch", "Disk.ToggleSIOPatch", false);
	AddAutoStateEntry(L"Disk burst I/O is enabled.", L"Turn off", "Disk.ToggleBurstTransfers", false);

	AssessFastFP();

	AssessStandardPAL();

	if (g_sim.GetVideoStandard() == kATVideoStandard_NTSC)
		AddAutoStateEntry(L"Video standard is set to NTSC (60Hz). Games written for PAL regions will execute faster than intended and may malfunction.", L"Switch to PAL", "Video.StandardPAL", true);

	AssessKeyboard();
}

void ATUIDialogSysConfigAssessment::AssessForCompatibility() {
	AssessFirmware();
	AssessCPU();

	switch(g_sim.GetMemoryMode()) {
		case kATMemoryMode_8K:
		case kATMemoryMode_16K:
		case kATMemoryMode_24K:
		case kATMemoryMode_32K:
		case kATMemoryMode_40K:
			if (!AddAutoStateEntry(L"System memory is below 48K. Most programs need at least 48K to run correctly.", L"Switch to 48K", "System.MemoryMode48K", true))
				AddAutoStateEntry(L"System memory is below 48K. Most programs need at least 48K to run correctly.", L"Switch to 64K", "System.MemoryMode64K", true);
			break;
	}

	AssessFastFP();
	AddAutoStateEntry(L"Internal BASIC is enabled. Non-BASIC programs often require BASIC to be disabled by holding Option on boot.", L"Disable internal BASIC", "System.ToggleBASIC", false);

	AssessStandardPAL();
	AssessKeyboard();
}

void ATUIDialogSysConfigAssessment::AssessForPerformance() {
	AddAutoStateEntry(L"CPU execution history is enabled. If not needed, turning it off will slightly improve performance.", L"Turn off CPU history", "System.ToggleCPUHistory", false);
}

void ATUIDialogSysConfigAssessment::AssessFirmware() {
	if (g_sim.GetActualKernelId() < kATFirmwareId_Custom) {
		AddFnEntry(L"AltirraOS is being used as the current operating system. This will work with most well-behaved software, but some programs only work with the Atari OS.",
			L"Check firmware settings",
			[this] {
				mpParentPagedDialog->SwitchToPage("firmware");
			}
		);
	}
}

void ATUIDialogSysConfigAssessment::AssessKeyboard() {
	if (!g_kbdOpts.mbRawKeys) {
		AddFnEntry(L"The keyboard mode is set to Cooked. This makes it easier to type text but can cause issues with programs that check for held keys.",
			L"Switch to Raw Key mode",
			[this] {
				ExecuteCommand("Input.KeyboardModeRaw");
			},
			L"View keyboard settings",
			[this] {
				mpParentPagedDialog->SwitchToPage("keyboard");
			}
		);
	}
}

void ATUIDialogSysConfigAssessment::AssessCPU() {
	switch(g_sim.GetCPU().GetCPUMode()) {
		case kATCPUMode_65C02:
			AddEntry(L"CPU mode is set to 65C02.", L"Change to 6502", "System.CPUMode6502");
			break;

		case kATCPUMode_65C816:
			AddEntry(L"CPU mode is set to 65C816.", L"Change to 6502", "System.CPUMode6502");
			break;
	}

	AddAutoStateEntry(L"The Stop on BRK Instruction debugging option is enabled. Occasionally some programs require BRK instructions to run properly.", L"Re-enable normal BRK handling", "System.ToggleCPUStopOnBRK", false);
}

void ATUIDialogSysConfigAssessment::AssessFastFP() {
	AddAutoStateEntry(L"Fast floating-point math acceleration is enabled. BASIC programs will execute much faster than normal.", L"Disable fast FP", "System.ToggleFPPatch", false);
}

void ATUIDialogSysConfigAssessment::AssessStandardPAL() {
	if (g_sim.GetVideoStandard() == kATVideoStandard_PAL)
		AddAutoStateEntry(L"Video standard is set to PAL (50Hz). Games written for NTSC regions will execute slower than intended.", L"Switch to NTSC", "Video.StandardNTSC", true);
}

void ATUIDialogSysConfigAssessment::AddFnEntry(const wchar_t *s, const wchar_t *linkText, vdfunction<void()> fn, const wchar_t *linkText2, vdfunction<void()> fn2) {
	mResultView.AppendEscapedRTF(mRtfBuffer, s);

	while (linkText) {
		mRtfBuffer += "  {\\field {\\*\\fldinst HYPERLINK \"";
		mRtfBuffer.append_sprintf("%u", mLinkHandlers.size());

		// Explicit underline and color override is needed on XP.
		mRtfBuffer += "\"}{\\fldrslt\\ul\\cf1 ";

		mResultView.AppendEscapedRTF(mRtfBuffer, linkText);

		mRtfBuffer += "}}";

		mLinkHandlers.emplace_back(std::move(fn));

		linkText = linkText2;
		linkText2 = nullptr;

		fn = std::move(fn2);
		fn2 = nullptr;
	}

	mRtfBuffer += "\\par ";
}

void ATUIDialogSysConfigAssessment::AddEntry(const wchar_t *s, const wchar_t *linkText, const char *command, const wchar_t *linkText2, const char *command2) {
	AddFnEntry(
		s,
		linkText,
		[cmd = VDStringA(command), this] { ExecuteCommand(cmd.c_str()); },
		linkText2,
		linkText2 ? vdfunction<void()>([cmd = VDStringA(command2), this] { ExecuteCommand(cmd.c_str()); }) : nullptr);
}

bool ATUIDialogSysConfigAssessment::AddAutoStateEntry(const wchar_t *s, const wchar_t *linkText, const char *command, bool targetState, const wchar_t *linkText2, const char *command2) {
	const ATUICommand *cmd = ATUIGetCommandManager().GetCommand(command);

	if (!cmd) {
		VDFAIL("Unknown command referenced.");
		return false;
	}

	if (!cmd->mpStateFn) {
		VDFAIL("Cannot use auto-set entry with command that has no check/radio-check.");
		return false;
	}

	if (cmd->mpTestFn && !cmd->mpTestFn())
		return false;
		
	ATUICmdState state = cmd->mpStateFn();

	if ((state != kATUICmdState_None) == targetState)
		return false;

	AddEntry(s, linkText, command, linkText2, command2);
	return true;
}

void ATUIDialogSysConfigAssessment::ExecuteCommand(const char *s) {
	ATUIGetCommandManager().ExecuteCommand(s);
	Reassess();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigSystem final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigSystem();

protected:
	bool OnLoaded() override;

	VDUIProxyComboBoxControl mHardwareTypeView;
	VDUIProxyComboBoxControl mVideoStandardView;
	VDUIProxyButtonControl mVideoStdToggleView;

	static constexpr CmdMapEntry sHardwareTypeOptions[] = {
		{ "System.HardwareMode800",		L"400/800" },
		{ "System.HardwareMode800XL",	L"600XL/800XL" },
		{ "System.HardwareMode130XE",	L"65XE/130XE" },
		{ "System.HardwareMode1200XL",	L"1200XL" },
		{ "System.HardwareModeXEGS",	L"XE Game System (XEGS)" },
		{ "System.HardwareMode5200",	L"5200 SuperSystem" },
	};

	static constexpr CmdMapEntry sVideoStandardOptions[] = {
		{ "Video.StandardNTSC",		L"NTSC" },
		{ "Video.StandardPAL",		L"PAL" },
		{ "Video.StandardSECAM",	L"SECAM" },
		{ "Video.StandardNTSC50",	L"NTSC-50" },
		{ "Video.StandardPAL60",	L"PAL-60" },
	};

	CmdComboBinding mHardwareTypeMapTable { sHardwareTypeOptions };
	CmdComboBinding mVideoStandardMapTable { sVideoStandardOptions };
	
	CmdTriggerBinding mVideoStdToggle { "Video.ToggleStandardNTSCPAL" };
};

constexpr ATUIDialogSysConfigSystem::CmdMapEntry ATUIDialogSysConfigSystem::sHardwareTypeOptions[];
constexpr ATUIDialogSysConfigSystem::CmdMapEntry ATUIDialogSysConfigSystem::sVideoStandardOptions[];

ATUIDialogSysConfigSystem::ATUIDialogSysConfigSystem()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_SYSTEM)
{
	mHardwareTypeView.SetOnSelectionChanged(
		[this](int idx) {
			mHardwareTypeMapTable.Write();
			OnDataExchange(false);
		}
	);

	mVideoStandardView.SetOnSelectionChanged(
		[this](int idx) {
			mVideoStandardMapTable.Write();
		}
	);

	mVideoStdToggleView.SetOnClicked(
		[this] {
			mVideoStdToggle.Write();
			mVideoStandardMapTable.Read();
		}
	);
}

bool ATUIDialogSysConfigSystem::OnLoaded() {
	AddProxy(&mHardwareTypeView, IDC_HARDWARE_TYPE);
	AddProxy(&mVideoStandardView, IDC_VIDEO_STANDARD);
	AddProxy(&mVideoStdToggleView, IDC_TOGGLE_NTSC_PAL);

	BindCheckbox(IDC_CTIA, "Video.ToggleCTIA");

	mHardwareTypeMapTable.Bind(&mHardwareTypeView);
	mVideoStandardMapTable.Bind(&mVideoStandardView);
	mVideoStdToggle.Bind(&mVideoStdToggleView);

	AddAutoReadBinding(&mHardwareTypeMapTable);
	AddAutoReadBinding(&mVideoStandardMapTable);
	AddAutoReadBinding(&mVideoStdToggle);
	
	AddHelpEntry(IDC_HARDWARE_TYPE, L"Hardware type",
		L"Select base computer model. XL modes enable many XL/XE hardware features, while \
the XE mode enables floating bus behavior.");

	AddHelpEntry(IDC_VIDEO_STANDARD, L"Video standard",
		L"Video signal type produced by the computer. This affects the frame rate, colors, \
and aspect ratio of the video output. NTSC runs at 60Hz and is appropriate for software targeted \
at North America; PAL at 50Hz is more appropriate and often required for later software \
written in Europe. NTSC-50 and PAL-60 represent systems modified with mixed ANTIC and GTIA \
chips, e.g. PAL ANTIC with NTSC GTIA.");

	AddHelpEntry(IDC_CTIA, L"CTIA mode",
		L"Select a CTIA as the video chip instead of the GTIA. The CTIA is nearly the same as \
the GTIA except for the absence of the GTIA video modes. A few very early programs require a CTIA to display \
properly as they show corrupted displays with a GTIA.");

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigMemory final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigMemory();

protected:
	bool OnLoaded() override;

	VDUIProxyComboBoxControl mMemoryTypeView;
	VDUIProxyComboBoxControl mMemoryClearModeView;
	VDUIProxyComboBoxControl mAxlonSizeView;
	VDUIProxyComboBoxControl mHighMemoryView;

	static constexpr CmdMapEntry sMemoryTypeOptions[] = {
		{ "System.MemoryMode16K",		L"16K" },
		{ "System.MemoryMode48K",		L"48K (800)" },
		{ "System.MemoryMode64K",		L"64K (800XL/1200XL)" },
		{ "System.MemoryMode128K",		L"128K (130XE)" },
		{ "System.MemoryMode320K",		L"320K (Rambo)" },
		{ "System.MemoryMode320KCompy",	L"320K (Compy)" },
		{ "System.MemoryMode576K",		L"576K (Rambo)" },
		{ "System.MemoryMode576KCompy",	L"576K (Compy)" },
		{ "System.MemoryMode1088K",		L"1088K" },
		{ "System.MemoryMode8K",		L"8K" },
		{ "System.MemoryMode24K",		L"24K" },
		{ "System.MemoryMode32K",		L"32K" },
		{ "System.MemoryMode40K",		L"40K" },
		{ "System.MemoryMode52K",		L"52K" },
		{ "System.MemoryMode256K",		L"256K (Rambo)" },
	};

	static constexpr CmdMapEntry sMemoryClearOptions[] = {
		{ "System.MemoryClearZero",		L"Cleared" },
		{ "System.MemoryClearRandom",	L"SRAM (random)" },
		{ "System.MemoryClearDRAM1",	L"DRAM 1" },
		{ "System.MemoryClearDRAM2",	L"DRAM 2" },
		{ "System.MemoryClearDRAM3",	L"DRAM 3" },
	};

	static constexpr CmdMapEntry sAxlonSizeOptions[] = {
		{ "System.AxlonMemoryNone",		L"None" },
		{ "System.AxlonMemory64K",		L"64K (4 banks)" },
		{ "System.AxlonMemory128K",		L"128K (8 banks)" },
		{ "System.AxlonMemory256K",		L"256K (16 banks)" },
		{ "System.AxlonMemory512K",		L"512K (32 banks)" },
		{ "System.AxlonMemory1024K",	L"1024K (64 banks)" },
		{ "System.AxlonMemory2048K",	L"2048K (128 banks)" },
		{ "System.AxlonMemory4096K",	L"4096K (256 banks)" },
	};

	static constexpr CmdMapEntry sHighMemoryOptions[] = {
		{ "System.HighMemoryNA",		L"None (16-bit addressing)" },
		{ "System.HighMemoryNone",		L"None (24-bit addressing)" },
		{ "System.HighMemory64K",		L"64K (bank $01)" },
		{ "System.HighMemory192K",		L"192K (banks $01-03)" },
		{ "System.HighMemory960K",		L"1MB (banks $01-0F)" },
		{ "System.HighMemory4032K",		L"4MB (banks $01-3F)" },
		{ "System.HighMemory16320K",	L"16MB (banks $01-FF)" },
	};

	CmdComboBinding mMemoryTypeBinding { sMemoryTypeOptions };
	CmdComboBinding mMemoryClearModeBinding { sMemoryClearOptions };
	CmdComboBinding mAxlonSizeBinding { sAxlonSizeOptions };
	CmdComboBinding mHighMemoryBinding { sHighMemoryOptions };
};

constexpr ATUIDialogSysConfigMemory::CmdMapEntry ATUIDialogSysConfigMemory::sMemoryTypeOptions[];
constexpr ATUIDialogSysConfigMemory::CmdMapEntry ATUIDialogSysConfigMemory::sMemoryClearOptions[];
constexpr ATUIDialogSysConfigMemory::CmdMapEntry ATUIDialogSysConfigMemory::sAxlonSizeOptions[];
constexpr ATUIDialogSysConfigMemory::CmdMapEntry ATUIDialogSysConfigMemory::sHighMemoryOptions[];

ATUIDialogSysConfigMemory::ATUIDialogSysConfigMemory()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_MEMORY)
{
}

bool ATUIDialogSysConfigMemory::OnLoaded() {
	AddProxy(&mMemoryTypeView, IDC_MEMORY_TYPE);
	AddProxy(&mMemoryClearModeView, IDC_POWERUP_PATTERN);
	AddProxy(&mAxlonSizeView, IDC_AXLON_RAMDISK);
	AddProxy(&mHighMemoryView, IDC_HIGH_MEMORY);

	BindCheckbox(IDC_ENABLE_MAPRAM, "System.ToggleMapRAM");
	BindCheckbox(IDC_PRESERVEEXTRAM, "System.TogglePreserveExtRAM");
	CmdBoolBinding *axlonAliasingBinding = BindCheckbox(IDC_AXLON_ALIASING, "System.ToggleAxlonAliasing");
	BindCheckbox(IDC_ENABLE_FLOATINGIOBUS, "System.ToggleFloatingIOBus");
	CmdBoolBinding *ultimate1MBBinding = BindCheckbox(IDC_ENABLE_ULTIMATE1MB, "System.ToggleUltimate1MB");

	mMemoryTypeBinding.Bind(&mMemoryTypeView);
	mMemoryClearModeBinding.Bind(&mMemoryClearModeView);
	mAxlonSizeBinding.Bind(&mAxlonSizeView);
	mHighMemoryBinding.Bind(&mHighMemoryView);

	mMemoryTypeView.SetOnSelectionChanged([this](int) { mMemoryTypeBinding.Write(); });
	mMemoryClearModeView.SetOnSelectionChanged([this](int) { mMemoryClearModeBinding.Write(); });

	ultimate1MBBinding->GetView().SetOnClicked(
		[=] {
			ultimate1MBBinding->Write();
			mMemoryTypeBinding.Read();
		}
	);

	mAxlonSizeView.SetOnSelectionChanged(
		[=](int) {
			mAxlonSizeBinding.Write();
			axlonAliasingBinding->Read();
		}
	);

	mHighMemoryView.SetOnSelectionChanged([this](int) { mHighMemoryBinding.Write(); });

	AddAutoReadBinding(&mMemoryTypeBinding);
	AddAutoReadBinding(&mMemoryClearModeBinding);
	AddAutoReadBinding(&mAxlonSizeBinding);
	AddAutoReadBinding(&mHighMemoryBinding);

	AddHelpEntry(IDC_MEMORY_TYPE, L"Memory type",
		L"Set the amount of main and PORTB extended memory installed. XE and Compy Shop type \
extensions allow separate CPU/ANTIC access control. Sub-64K sizes other than 16K and 48K are \
800-only."
);

	AddHelpEntry(IDC_ENABLE_MAPRAM, L"Enable MapRAM support (XL/XE only)",
		L"Allow normally inaccessible 2K of memory hidden beneath $D000-D7FF to be mapped \
to $5000-57FF by PORTB bit 7=0, bit 0=1. This is a relatively modern hardware modification."
		);

	AddHelpEntry(IDC_ENABLE_FLOATINGIOBUS, L"Enable floating I/O bus (800 only)",
		L"Emulate 800-specific behavior of having a separate I/O bus on the personality board that \
can float independently from the main data bus. This slows down emulation but is needed for accurate \
emulation of some software."
);

	AddHelpEntry(IDC_ENABLE_ULTIMATE1MB, L"Enable Ultimate1MB",
		L"Emulate the Ultimate1MB memory expansion."
);

	AddHelpEntry(IDC_PRESERVEEXTRAM, L"Preserve extended memory on cold reset",
		L"Keep contents of extended memory intact across a cold reset (power off/on). This emulates \
battery-backed extended memory. Note that contents are not preserved across an emulator restart."
);

	AddHelpEntry(IDC_AXLON_RAMDISK, L"Axlon RAM disk",
		L"Set the size of extended memory accessed through the Axlon RAM disk protocol, an older \
method of extended memory provided by a special slot 2 memory card in an 800. This is different from \
and can be used independently from the more common PORTB-based extended memory.");

	AddHelpEntry(IDC_AXLON_ALIASING, L"Enable Axlon bank register aliasing",
		L"Enable emulation of the bank register alias at $0FF0-0FFF. This accurately emulates \
the behavior of the original Axlon RAMPower hardware but can cause compatibility issues. Compatible \
clones and especially modern recreations may not have this alias.");

	AddHelpEntry(IDC_POWERUP_PATTERN, L"Power-up memory pattern",
		L"Set the memory pattern stored in RAM on a cold power-up. This depends on the RAM chips \
installed and a few programs depend on the specific pattern used. Cleared is not realistic but \
traditional for emulation; random data is accurate for static RAM but also useful for debugging.");

	AddHelpEntry(IDC_HIGH_MEMORY, L"High memory",
		L"Set the amount of memory available above bank 0 for 65C816-based programs; also known as \
linear memory. This has no effect without a 65C816 CPU and software that can use 65C816 long addressing.");

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigAcceleration final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigAcceleration();

protected:
	bool OnLoaded() override;

	VDUIProxyButtonControl mSIOAccelSIOVView;
	VDUIProxyButtonControl mSIOAccelPBIView;
	VDUIProxyButtonControl mSIOAccelBothView;
	
	CmdRadioBinding mSIOAccelBinding;
};

ATUIDialogSysConfigAcceleration::ATUIDialogSysConfigAcceleration()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_ACCELERATION)
{
	mSIOAccelSIOVView.SetOnClicked([this] { mSIOAccelBinding.Write(); });
	mSIOAccelPBIView.SetOnClicked([this] { mSIOAccelBinding.Write(); });
	mSIOAccelBothView.SetOnClicked([this] { mSIOAccelBinding.Write(); });
}

bool ATUIDialogSysConfigAcceleration::OnLoaded() {
	AddProxy(&mSIOAccelSIOVView, IDC_SIOACCEL_SIOV);
	AddProxy(&mSIOAccelPBIView, IDC_SIOACCEL_PBI);
	AddProxy(&mSIOAccelBothView, IDC_SIOACCEL_BOTH);

	BindCheckbox(IDC_FASTBOOT, "System.ToggleFastBoot");
	BindCheckbox(IDC_FASTFPMATH, "System.ToggleFPPatch");
	BindCheckbox(IDC_SIOPATCH_C, "Cassette.ToggleSIOPatch");
	BindCheckbox(IDC_SIOPATCH_D, "Disk.ToggleSIOPatch");
	BindCheckbox(IDC_SIOPATCH_OTHER, "Devices.ToggleSIOPatch");
	BindCheckbox(IDC_SIOBURST_D, "Disk.ToggleBurstTransfers");
	BindCheckbox(IDC_SIOBURST_OTHER, "Devices.ToggleSIOBurstTransfers");
	BindCheckbox(IDC_SIOOVERRIDEDETECT, "Disk.ToggleSIOOverrideDetection");
	BindCheckbox(IDC_CIOPATCH_H, "Devices.ToggleCIOPatchH");
	BindCheckbox(IDC_CIOPATCH_P, "Devices.ToggleCIOPatchP");
	BindCheckbox(IDC_CIOPATCH_R, "Devices.ToggleCIOPatchR");
	BindCheckbox(IDC_CIOPATCH_T, "Devices.ToggleCIOPatchT");
	BindCheckbox(IDC_CIOBURST, "Devices.ToggleCIOBurstTransfers");

	mSIOAccelBinding.Bind("Devices.SIOAccelModePatch", &mSIOAccelSIOVView);
	mSIOAccelBinding.Bind("Devices.SIOAccelModePBI", &mSIOAccelPBIView);
	mSIOAccelBinding.Bind("Devices.SIOAccelModeBoth", &mSIOAccelBothView);

	AddAutoReadBinding(&mSIOAccelBinding);

	AddHelpEntry(IDC_FASTBOOT, L"Fast boot",
		L"Accelerate standard OS checksum and memory test routines to speed up OS boot. \
May not work for custom OSes with different code."
);

	AddHelpEntry(IDC_FASTFPMATH, L"Fast floating-point math",
		L"Intercept calls to the floating-point math pack and execute FP math operations \
in native code. This greatly speeds up math-heavy code and also improves accuracy. It should \
be disabled when benchmarking BASIC code."
		);

	AddHelpEntry(IDC_SIOPATCH_C, L"SIO C: patch",
		L"Intercept and accelerate serial I/O transfers to the cassette (C:) device."
		);

	AddHelpEntry(IDC_SIOPATCH_D, L"SIO D: patch",
		L"Intercept and accelerate serial I/O transfers to disk drive devices (D:). This only works \
for disk access through the OS and will not speed up custom disk routines. It also will not \
work for full disk drive emulation."
		);

	AddHelpEntry(IDC_SIOPATCH_OTHER, L"SIO PRT: patch",
		L"Intercept and accelerate serial I/O transfers to other SIO bus devices."
		);

	AddHelpEntry(IDC_SIOBURST_D, L"SIO D: burst I/O",
		L"Speed up transfers to disk drives by bursting data as fast as the software driver can handle. \
This can accelerate custom disk loaders that bypass the D: patch."
		);

	AddHelpEntry(IDC_SIOBURST_OTHER, L"SIO PRT: burst I/O",
		L"Speed up transfers to other devices by bursting data as fast as the software driver can handle, \
for transfers not already handled by the SIO patch."
		);

	AddHelpEntry(IDC_SIOACCEL_SIOV, L"SIO patch mode",
		L"Selects OS hooking methods for SIO patching. SIOV is the traditional method of hooking calls \
to the OS SIO routines. PBI uses a virtual Parallel Bus Interface device, which avoids some override issues \
with custom OSes or other PBI devices and more easily works with SpartaDOS X, but requires a PBI-capable \
OS or DOS.");

	LinkHelpEntry(IDC_SIOACCEL_PBI, IDC_SIOACCEL_SIOV);
	LinkHelpEntry(IDC_SIOACCEL_BOTH, IDC_SIOACCEL_SIOV);

	AddHelpEntry(IDC_SIOOVERRIDEDETECT, L"SIO override detection",
		L"Attempt to detect when the OS or firmware is intercepting requests to an SIO device and prevent \
SIO patches from overriding the firmware. This allows ramdisks and IDE-based disk image emulation to work \
with patches enabled. This is generally only necessary for SIOV patch mode as PBI mode usually bypasses this \
issue."
		);

	AddHelpEntry(IDC_CIOPATCH_H, L"CIO H: patch",
		L"Intercept and accelerate serial I/O transfers to the host device (H:)."
		);

	AddHelpEntry(IDC_CIOPATCH_P, L"CIO P: patch",
		L"Intercept and accelerate serial I/O transfers to the printer device (P:)."
		);

	AddHelpEntry(IDC_CIOPATCH_R, L"CIO R: patch",
		L"Intercept and accelerate serial I/O transfers to the RS-232 serial device (R:)."
		);

	AddHelpEntry(IDC_CIOPATCH_T, L"CIO T: patch",
		L"Intercept and accelerate serial I/O transfers to the 1030 serial device (T:)."
		);

	AddHelpEntry(IDC_CIOBURST, L"CIO burst transfers",
		L"Detect and accelerate large block transfers by handling them directly in the driver instead \
of a byte at a time, when a CIO patch is active for a device."
		);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigVideo final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigVideo();

protected:
	bool OnLoaded() override;

	VDUIProxyComboBoxControl mArtifactModeView;
	VDUIProxyButtonControl mEnhTextNoneView;
	VDUIProxyButtonControl mEnhTextHWView;
	VDUIProxyButtonControl mEnhTextSWView;
	VDUIProxyButtonControl mEnhTextFontView;
	
	static constexpr CmdMapEntry sArtifactModeOptions[] = {
		{ "Video.ArtifactingNone",		L"None" },
		{ "Video.ArtifactingNTSC",		L"NTSC artifacting" },
		{ "Video.ArtifactingNTSCHi",	L"NTSC high artifacting" },
		{ "Video.ArtifactingPAL",		L"PAL artifacting" },
		{ "Video.ArtifactingPALHi",		L"PAL high artifacting" },
		{ "Video.ArtifactingAuto",		L"NTSC/PAL artifacting (auto-switch)" },
		{ "Video.ArtifactingAutoHi",	L"NTSC/PAL high artifacting (auto-switch)" },
	};

	CmdComboBinding mArtifactModeBinding { sArtifactModeOptions };
	CmdRadioBinding mEnhTextModeBinding;
	CmdTriggerBinding mEnhTextFontBinding { "Video.EnhancedTextFontDialog" };
};

constexpr ATUIDialogSysConfigVideo::CmdMapEntry ATUIDialogSysConfigVideo::sArtifactModeOptions[];

ATUIDialogSysConfigVideo::ATUIDialogSysConfigVideo()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_VIDEO)
{
	mArtifactModeView.SetOnSelectionChanged([this](int) { mArtifactModeBinding.Write(); });
	mEnhTextNoneView.SetOnClicked([this] { mEnhTextModeBinding.Write(); });
	mEnhTextHWView.SetOnClicked([this] { mEnhTextModeBinding.Write(); });
	mEnhTextSWView.SetOnClicked([this] { mEnhTextModeBinding.Write(); });
	mEnhTextFontView.SetOnClicked([this] { mEnhTextFontBinding.Write(); });
}

bool ATUIDialogSysConfigVideo::OnLoaded() {
	AddProxy(&mArtifactModeView, IDC_ARTIFACTMODE);
	AddProxy(&mEnhTextNoneView, IDC_ENHTEXT_NONE);
	AddProxy(&mEnhTextHWView, IDC_ENHTEXT_HW);
	AddProxy(&mEnhTextSWView, IDC_ENHTEXT_SW);
	AddProxy(&mEnhTextFontView, IDC_ENHTEXTFONT);

	BindCheckbox(IDC_FRAMEBLENDING, "Video.ToggleFrameBlending");
	BindCheckbox(IDC_INTERLACE, "Video.ToggleInterlace");
	BindCheckbox(IDC_SCANLINES, "Video.ToggleScanlines");

	mArtifactModeBinding.Bind(&mArtifactModeView);
	mEnhTextModeBinding.Bind("Video.EnhancedModeNone", &mEnhTextNoneView);
	mEnhTextModeBinding.Bind("Video.EnhancedModeHardware", &mEnhTextHWView);
	mEnhTextModeBinding.Bind("Video.EnhancedModeCIO", &mEnhTextSWView);
	mEnhTextFontBinding.Bind(&mEnhTextFontView);

	AddAutoReadBinding(&mArtifactModeBinding);
	AddAutoReadBinding(&mEnhTextModeBinding);
	AddAutoReadBinding(&mEnhTextFontBinding);

	AddHelpEntry(IDC_FRAMEBLENDING, L"Frame blending",
		L"Blend adjacent frames together to eliminate flickering from frame alternation effects."
);

	AddHelpEntry(IDC_INTERLACE, L"Interlace",
		L"Enable support for display video as interlaced fields instead of frames. This requires using software \
that can manipulate ANTIC video timing to force even/odd fields."
		);

	AddHelpEntry(IDC_SCANLINES, L"Scanlines",
		L"Darken video between scanlines to simulate beam scanning of a CRT."
		);

	AddHelpEntry(IDC_ARTIFACTMODE, L"Artifacting mode",
		L"Emulate false color effects derived from composite video encoding."
		);

	AddHelpEntry(IDC_ENHTEXT_NONE, L"Enhanced text mode",
		L"Enable enhanced text screen editor. Hardware mode is more compatible and displays the regular \
hardware screen with native quality fonts. CIO mode uses a software hook to provide a bigger screen and \
better editing capabilities, but only works with software that uses OS facilites to print text.");

	LinkHelpEntry(IDC_ENHTEXT_SW, IDC_ENHTEXT_NONE);
	LinkHelpEntry(IDC_ENHTEXT_HW, IDC_ENHTEXT_NONE);
	LinkHelpEntry(IDC_ENHTEXTFONT, IDC_ENHTEXT_NONE);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigDisk final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigDisk();

protected:
	bool OnLoaded() override;
};

ATUIDialogSysConfigDisk::ATUIDialogSysConfigDisk()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_DISK)
{
}

bool ATUIDialogSysConfigDisk::OnLoaded() {
	BindCheckbox(IDC_ACCURATESECTORTIMING, "Disk.ToggleAccurateSectorTiming");
	BindCheckbox(IDC_SHOWSECTORCOUNTER, "Disk.ToggleSectorCounter");
	BindCheckbox(IDC_DRIVESOUNDS, "Disk.ToggleDriveSounds");

	AddHelpEntry(IDC_ACCURATESECTORTIMING, L"Accurate sector timing",
		L"Emulate the seek times and rotational delays of a real disk drive so that disk operations \
take realistic amounts of time. This is required for copy protection checks to pass in some disk loaders."
);

	AddHelpEntry(IDC_SHOWSECTORCOUNTER, L"Show sector counter",
		L"During disk access, display the sector number being accessed instead of the drive number."
		);

	AddHelpEntry(IDC_DRIVESOUNDS, L"Drive sounds",
		L"Simulate the sounds of a real disk drive grinding and whirring. The sounds used depend on \
the disk emulation type. Best used with accurate sector timing."
		);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigAudio final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigAudio();

protected:
	bool OnLoaded() override;

	VDUIProxyButtonControl mHostAudioView;
	CmdTriggerBinding mHostAudioBinding { "Audio.OptionsDialog" };
};

ATUIDialogSysConfigAudio::ATUIDialogSysConfigAudio()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_AUDIO)
{
}

bool ATUIDialogSysConfigAudio::OnLoaded() {
	AddProxy(&mHostAudioView, IDC_HOSTAUDIOOPTIONS);
	mHostAudioBinding.Bind(&mHostAudioView);

	BindCheckbox(IDC_STEREO, "Audio.ToggleStereo");
	BindCheckbox(IDC_NONLINEARMIXING, "Audio.ToggleNonlinearMixing");
	BindCheckbox(IDC_SERIALNOISE, "Audio.ToggleSerialNoise");
	BindCheckbox(IDC_AUDIOMONITOR, "Audio.ToggleMonitor");
	BindCheckbox(IDC_MUTEALL, "Audio.ToggleMute");
	BindCheckbox(IDC_AUDIO_CH1, "Audio.ToggleChannel1");
	BindCheckbox(IDC_AUDIO_CH2, "Audio.ToggleChannel2");
	BindCheckbox(IDC_AUDIO_CH3, "Audio.ToggleChannel3");
	BindCheckbox(IDC_AUDIO_CH4, "Audio.ToggleChannel4");

	mHostAudioView.SetOnClicked([this] { mHostAudioBinding.Write(); });

	AddAutoReadBinding(&mHostAudioBinding);

	AddHelpEntry(IDC_STEREO, L"Stereo",
		L"Enable emulation of two POKEYs, controlling the left and right channels independently."
);

	AddHelpEntry(IDC_NONLINEARMIXING, L"Non-linear mixing",
		L"Emulate analog behavior where audio signal output is compressed at high volume levels. This improves \
reproduction accuracy of some sound effects."
		);

	AddHelpEntry(IDC_SERIALNOISE, L"Serial noise",
		L"Enable audio noise when serial transfers occur. This \
emulates quiet high-pitched noise heard during disk loads when normal beep-beep load audio is turned off."
		);

	AddHelpEntry(IDC_AUDIOMONITOR, L"Audio monitor",
		L"Display real-time audio output monitor on screen."
		);

	AddHelpEntry(IDC_MUTEALL, L"Mute channels",
		L"Mute all audio output or only specific POKEY channels."
		);

	LinkHelpEntry(IDC_AUDIO_CH1, IDC_MUTEALL);
	LinkHelpEntry(IDC_AUDIO_CH2, IDC_MUTEALL);
	LinkHelpEntry(IDC_AUDIO_CH3, IDC_MUTEALL);
	LinkHelpEntry(IDC_AUDIO_CH4, IDC_MUTEALL);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigCassette final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigCassette();

protected:
	bool OnLoaded() override;

	VDUIProxyComboBoxControl mTurboTypeView;

	static constexpr CmdMapEntry sTurboTypeOptions[] = {
		{ "Cassette.TurboModeNone", L"Disabled" },
		{ "Cassette.TurboModeAlways", L"Enabled (always on)" },
		{ "Cassette.TurboModeCommandControl", L"Enabled (command control)" },
		{ "Cassette.TurboModeProceedSense", L"Enabled (proceed sense)" },
		{ "Cassette.TurboModeInterruptSense", L"Enabled (interrupt sense)" },
	};

	CmdComboBinding mTurboTypeBinding { sTurboTypeOptions };

	VDUIProxyComboBoxControl mDirectSenseView;

	static constexpr CmdMapEntry sDirectSenseOptions[] = {
		{ "Cassette.DirectSenseNormal", L"Normal (~2000 baud)" },
		{ "Cassette.DirectSenseLowSpeed", L"Low speed (~1000 baud)" },
		{ "Cassette.DirectSenseHighSpeed", L"High speed (~4000 baud)" },
		{ "Cassette.DirectSenseMaxSpeed", L"Max speed" },
	};

	CmdComboBinding mDirectSenseBinding { sDirectSenseOptions };
};

constexpr ATUIDialogSysConfigCassette::CmdMapEntry ATUIDialogSysConfigCassette::sTurboTypeOptions[];
constexpr ATUIDialogSysConfigCassette::CmdMapEntry ATUIDialogSysConfigCassette::sDirectSenseOptions[];

ATUIDialogSysConfigCassette::ATUIDialogSysConfigCassette()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_CASSETTE)
{
}

bool ATUIDialogSysConfigCassette::OnLoaded() {
	BindCheckbox(IDC_AUTOBOOT, "Cassette.ToggleAutoBoot");
	BindCheckbox(IDC_AUTOBASICBOOT, "Cassette.ToggleAutoBasicBoot");
	BindCheckbox(IDC_AUTOREWIND, "Cassette.ToggleAutoRewind");
	BindCheckbox(IDC_LOADDATAASAUDIO, "Cassette.ToggleLoadDataAsAudio");
	BindCheckbox(IDC_RANDOMIZESTARTPOS, "Cassette.ToggleRandomizeStartPosition");
	CmdBoolBinding *turboInvertBinding = BindCheckbox(IDC_TURBOINVERT, "Cassette.TogglePolarity");

	AddProxy(&mTurboTypeView, IDC_TURBOTYPE);
	mTurboTypeBinding.Bind(&mTurboTypeView);
	AddAutoReadBinding(&mTurboTypeBinding);

	mTurboTypeView.SetOnSelectionChanged(
		[=](int) {
			mTurboTypeBinding.Write();
			turboInvertBinding->Read();
		}
	);

	AddProxy(&mDirectSenseView, IDC_DIRECTSENSE);
	mDirectSenseBinding.Bind(&mDirectSenseView);
	AddAutoReadBinding(&mDirectSenseBinding);
	mDirectSenseView.SetOnSelectionChanged(
		[this](int) {
			mDirectSenseBinding.Write();
		}
	);


	AddHelpEntry(IDC_AUTOBOOT, L"Auto-boot on startup",
		L"Automatically hold down the Start button on power-up and press a key to start a binary load off of \
the cassette tape. This should not be used for a tape that starts with a BASIC program, in which case the \
BASIC CLOAD command should be used instead."
);

	AddHelpEntry(IDC_AUTOBASICBOOT, L"Auto-boot BASIC on startup",
		L"Try to determine if the tape has a BASIC or binary program and toggle BASIC accordingly. This only \
has an effect when auto-boot is enabled.");

	AddHelpEntry(IDC_AUTOREWIND, L"Auto-rewind on startup",
		L"Automatically rewind the tape to the beginning when the computer is restarted."
		);

	AddHelpEntry(IDC_LOADDATAASAUDIO, L"Load data as audio",
		L"Play the data track as the audio track when using a tape image that doesn't have a separate audio track (CAS or mono audio)."
		);

	AddHelpEntry(IDC_RANDOMIZESTARTPOS, L"Randomize start position",
		L"Apply a slight jitter to the start position of the tape so it doesn't play at exactly the same position \
on each boot. Useful to work around bugs in the OS that cause random tape loading failures at a low rate (~1%)."
		);

	AddHelpEntry(IDC_TURBOTYPE, L"Turbo type",
		L"Select turbo tape hardware modification to support. The types differ in the way the computer enables \
turbo mode and how the turbo encoding is read."
		);

	AddHelpEntry(IDC_TURBOINVERT, L"Invert turbo data",
		L"Invert the polarity of turbo data read by the computer. May be needed if the tape image has been recorded \
with an inverted signal, since some turbo tape loaders are sensitive to the data polarity."
		);

	AddHelpEntry(IDC_DIRECTSENSE, L"Direct read filter (FSK only)",
		L"Selects the bandwidth of filter used when reading data bits directly. Lower bandwidth filters limit max baud rate but \
improve noise immunity. This filter is not used with turbo decoding."
		);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigCPU final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigCPU();

protected:
	bool OnLoaded() override;

	VDUIProxyButtonControl mCPUMode6502View;
	VDUIProxyButtonControl mCPUMode65C02View;
	VDUIProxyButtonControl mCPUMode65C816_1XView;
	VDUIProxyButtonControl mCPUMode65C816_2XView;
	VDUIProxyButtonControl mCPUMode65C816_4XView;
	VDUIProxyButtonControl mCPUMode65C816_6XView;
	VDUIProxyButtonControl mCPUMode65C816_8XView;
	VDUIProxyButtonControl mCPUMode65C816_10XView;
	VDUIProxyButtonControl mCPUMode65C816_12XView;
	
	CmdRadioBinding mCPUModeBinding;
};

ATUIDialogSysConfigCPU::ATUIDialogSysConfigCPU()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_CPU)
{
}

bool ATUIDialogSysConfigCPU::OnLoaded() {
	BindCheckbox(IDC_ENABLE_HISTORY, "System.ToggleCPUHistory");
	BindCheckbox(IDC_ENABLE_PATHS, "System.ToggleCPUPathTracing");
	BindCheckbox(IDC_ENABLE_ILLEGALS, "System.ToggleCPUIllegalInstructions");
	BindCheckbox(IDC_STOP_ON_BRK, "System.ToggleCPUStopOnBRK");
	BindCheckbox(IDC_ALLOWNMIBLOCKING, "System.ToggleCPUNMIBlocking");
	CmdBoolBinding *shadowROMBinding = BindCheckbox(IDC_SHADOW_ROM, "System.ToggleShadowROM");
	CmdBoolBinding *shadowCartsBinding = BindCheckbox(IDC_SHADOW_CARTS, "System.ToggleShadowCarts");

	auto cpuModeCallback = [=] {
		mCPUModeBinding.Write();
		shadowROMBinding->Read();
		shadowCartsBinding->Read();
	};

	mCPUMode6502View.SetOnClicked(cpuModeCallback);
	mCPUMode65C02View.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_1XView.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_2XView.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_4XView.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_6XView.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_8XView.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_10XView.SetOnClicked(cpuModeCallback);
	mCPUMode65C816_12XView.SetOnClicked(cpuModeCallback);

	AddProxy(&mCPUMode6502View, IDC_CPUMODEL_6502C);
	AddProxy(&mCPUMode65C02View, IDC_CPUMODEL_65C02);
	AddProxy(&mCPUMode65C816_1XView, IDC_CPUMODEL_65C816);
	AddProxy(&mCPUMode65C816_2XView, IDC_CPUMODEL_65C816_3MHZ);
	AddProxy(&mCPUMode65C816_4XView, IDC_CPUMODEL_65C816_7MHZ);
	AddProxy(&mCPUMode65C816_6XView, IDC_CPUMODEL_65C816_10MHZ);
	AddProxy(&mCPUMode65C816_8XView, IDC_CPUMODEL_65C816_14MHZ);
	AddProxy(&mCPUMode65C816_10XView, IDC_CPUMODEL_65C816_17MHZ);
	AddProxy(&mCPUMode65C816_12XView, IDC_CPUMODEL_65C816_21MHZ);

	mCPUModeBinding.Bind("System.CPUMode6502", &mCPUMode6502View);
	mCPUModeBinding.Bind("System.CPUMode65C02", &mCPUMode65C02View);
	mCPUModeBinding.Bind("System.CPUMode65C816", &mCPUMode65C816_1XView);
	mCPUModeBinding.Bind("System.CPUMode65C816x2", &mCPUMode65C816_2XView);
	mCPUModeBinding.Bind("System.CPUMode65C816x4", &mCPUMode65C816_4XView);
	mCPUModeBinding.Bind("System.CPUMode65C816x6", &mCPUMode65C816_6XView);
	mCPUModeBinding.Bind("System.CPUMode65C816x8", &mCPUMode65C816_8XView);
	mCPUModeBinding.Bind("System.CPUMode65C816x10", &mCPUMode65C816_10XView);
	mCPUModeBinding.Bind("System.CPUMode65C816x12", &mCPUMode65C816_12XView);

	AddAutoReadBinding(&mCPUModeBinding);

	AddHelpEntry(IDC_ENABLE_HISTORY, L"Enable history tracing",
		L"Record execution history during CPU execution. Slows down CPU emulation, but enables the History view in the debugger."
);

	AddHelpEntry(IDC_ENABLE_PATHS, L"Enable path tracing",
		L"Record execution paths during CPU execution. Slows down CPU emulation, but enables automatic labeling of blocks in the debugger."
		);

	AddHelpEntry(IDC_ENABLE_ILLEGALS, L"Enable illegal instructions",
		L"Enable illegal 6502 instruction encodings. Required for compatibility with some software."
		);

	AddHelpEntry(IDC_STOP_ON_BRK, L"Stop on BRK instruction",
		L"Stop in the debugger when the BRK (break) instruction is hit. Useful for debugging, but should be off otherwise."
		);

	AddHelpEntry(IDC_ALLOWNMIBLOCKING, L"Allow NMI blocking",
		L"Emulate the non-maskable interrupt (NMI) being ignored under specific circumstances. Needed for full accuracy, but not generally \
relied on by software."
		);

	AddHelpEntry(IDC_SHADOW_ROM, L"Shadow ROMs in fast memory",
		L"Enable full-speed CPU operation in ROM address space to simulate a CPU accelerator that shadows the on-board ROMs in fast memory."
		);

	AddHelpEntry(IDC_SHADOW_CARTS, L"Shadow cartridges in fast memory",
		L"Enable full-speed CPU operation in cartridge address space to simulate a CPU accelerator that shadows cartridges in fast memory."
		);

	AddHelpEntry(IDC_CPUMODEL_6502C, L"CPU mode",
		L"Select CPU type and speed. All original computers use a 6502/6502C; accelerators use the enhanced 65C816. The 65C02 is a mild \
enhancement of the 6502 and rarely used (not to be confused with the 6502C)."
		);

	LinkHelpEntry(IDC_CPUMODEL_65C02, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816_3MHZ, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816_7MHZ, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816_10MHZ, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816_14MHZ, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816_17MHZ, IDC_CPUMODEL_6502C);
	LinkHelpEntry(IDC_CPUMODEL_65C816_21MHZ, IDC_CPUMODEL_6502C);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigFirmware final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigFirmware();

	const char *GetPageTag() const override { return "firmware"; }

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	void OnOSSelected(int selIdx);
	void OnBASICSelected(int selIdx);

	void PopulateFirmwareView(const vdfastvector<const ATFirmwareInfo *>& fwlist, VDUIProxyComboBoxControl& control, uint64 id);

	VDUIProxyComboBoxControl mOSFirmwareView;
	VDUIProxyComboBoxControl mBASICFirmwareView;
	VDUIProxyButtonControl mEnableBASICView;
	VDUIProxyButtonControl mFirmwareManagerView;
	
	CmdBoolBinding mEnableBASICBinding { "System.ToggleBASIC" };
	CmdTriggerBinding mFirmwareManagerBinding { "System.ROMImagesDialog" };

	vdvector<ATFirmwareInfo> mOSFirmwares;
	vdfastvector<const ATFirmwareInfo *> mOSSortedFirmwares;
	vdvector<ATFirmwareInfo> mBASICFirmwares;
	vdfastvector<const ATFirmwareInfo *> mBASICSortedFirmwares;
};

ATUIDialogSysConfigFirmware::ATUIDialogSysConfigFirmware()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_FIRMWARE)
{
	mOSFirmwareView.SetOnSelectionChanged([this](int i) { OnOSSelected(i); });
	mBASICFirmwareView.SetOnSelectionChanged([this](int i) { OnBASICSelected(i); });
	mEnableBASICView.SetOnClicked([this] { mEnableBASICBinding.Write(); });
	mFirmwareManagerView.SetOnClicked(
		[this] {
			mFirmwareManagerBinding.Write();

			// The set of available firmware may have changed, so refresh the combos.
			OnDataExchange(false);
		}
	);
}

bool ATUIDialogSysConfigFirmware::OnLoaded() {
	AddProxy(&mOSFirmwareView, IDC_OS);
	AddProxy(&mBASICFirmwareView, IDC_BASIC);
	AddProxy(&mEnableBASICView, IDC_ENABLEBASIC);
	AddProxy(&mFirmwareManagerView, IDC_FIRMWAREMANAGER);

	mEnableBASICBinding.Bind(&mEnableBASICView);
	mFirmwareManagerBinding.Bind(&mFirmwareManagerView);

	AddHelpEntry(IDC_OS, L"Operating system firmware",
		L"Select the firmware ROM image used for the operating system.");

	AddHelpEntry(IDC_BASIC, L"BASIC firmware",
		L"Select the firmware ROM image used for BASIC. For computer models that have built-in BASIC, this determines the BASIC \
used when the computer is booted without holding down the Option button. For other models, this also determines the BASIC \
cartridge used when the insert BASIC cartridge option is used."
		);

	AddHelpEntry(IDC_ENABLEBASIC, L"Enable BASIC (boot without Option pressed)",
		L"Controls whether internal BASIC is enabled on boot. If enabled, BASIC starts after disk boot completes. If disabled, \
the Option button is automatically held down during boot to suppress internal BASIC."
		);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

void ATUIDialogSysConfigFirmware::OnDataExchange(bool write) {
	if (write) {
	} else {
		mEnableBASICBinding.Read();

		mOSFirmwareView.Clear();
		mOSSortedFirmwares.clear();
		mOSFirmwares.clear();
		ATUIGetKernelFirmwareList(g_sim.GetHardwareMode(), mOSFirmwares, mOSSortedFirmwares);
		PopulateFirmwareView(mOSSortedFirmwares, mOSFirmwareView, g_sim.GetKernelId());

		mBASICFirmwareView.Clear();
		mBASICSortedFirmwares.clear();
		mBASICFirmwares.clear();
		ATUIGetBASICFirmwareList(mBASICFirmwares, mBASICSortedFirmwares);
		PopulateFirmwareView(mBASICSortedFirmwares, mBASICFirmwareView, g_sim.GetBasicId());
	}
}

void ATUIDialogSysConfigFirmware::OnOSSelected(int selIdx) {
	if ((uint32)selIdx < mOSSortedFirmwares.size()) {
		const ATFirmwareInfo& fw = *mOSSortedFirmwares[selIdx];
	
		ATUISwitchKernel(fw.mId);
	}
}

void ATUIDialogSysConfigFirmware::OnBASICSelected(int selIdx) {
	if ((uint32)selIdx < mBASICSortedFirmwares.size()) {
		const ATFirmwareInfo& fw = *mBASICSortedFirmwares[selIdx];

		ATUISwitchBasic(fw.mId);
	}
}

void ATUIDialogSysConfigFirmware::PopulateFirmwareView(const vdfastvector<const ATFirmwareInfo *>& fwlist, VDUIProxyComboBoxControl& control, uint64 currentId) {
	int selIdx = -1;
	int idx = 0;

	for(const ATFirmwareInfo *fw : fwlist) {
		control.AddItem(fw->mName.c_str());

		if (currentId == fw->mId)
			selIdx = idx;

		++idx;
	}

	control.SetSelection(selIdx);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigDevices final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigDevices();

	const char *GetPageTag() const override { return "devices"; }

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	void OnDpiChanged() override;

	VDUIProxyTreeViewControl mTreeView;
	VDUIProxyButtonControl mAddView;
	VDUIProxyButtonControl mRemoveView;
	VDUIProxyButtonControl mRemoveAllView;
	VDUIProxyButtonControl mSettingsView;
	VDUIProxyButtonControl mFirmwareManagerView;

	CmdTriggerBinding mFirmwareManagerBinding { "System.ROMImagesDialog" };

	ATUIControllerDevices mCtrlDevs;
};

ATUIDialogSysConfigDevices::ATUIDialogSysConfigDevices()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_DEVICES)
	, mCtrlDevs(*this, *g_sim.GetDeviceManager(), mTreeView, mSettingsView, mRemoveView)
{
	mAddView.SetOnClicked([this] { mCtrlDevs.Add(); });
	mRemoveView.SetOnClicked([this] { mCtrlDevs.Remove(); });
	mRemoveAllView.SetOnClicked([this] { mCtrlDevs.RemoveAll(); });
	mSettingsView.SetOnClicked([this] { mCtrlDevs.Settings(); });

	mFirmwareManagerView.SetOnClicked(
		[this] {
			mFirmwareManagerBinding.Write();

			// The set of available firmware may have changed, so refresh the combos.
			OnDataExchange(false);
		}
	);
}

bool ATUIDialogSysConfigDevices::OnLoaded() {
	AddProxy(&mTreeView, IDC_TREE);
	AddProxy(&mAddView, IDC_ADD);
	AddProxy(&mRemoveView, IDC_REMOVE);
	AddProxy(&mRemoveAllView, IDC_REMOVEALL);
	AddProxy(&mSettingsView, IDC_SETTINGS);
	AddProxy(&mFirmwareManagerView, IDC_FIRMWAREMANAGER);

	mFirmwareManagerBinding.Bind(&mFirmwareManagerView);

	mResizer.Add(IDC_TREE, mResizer.kMC);
	mResizer.Add(IDC_ADD, mResizer.kBL);
	mResizer.Add(IDC_REMOVE, mResizer.kBL);
	mResizer.Add(IDC_REMOVEALL, mResizer.kBL);
	mResizer.Add(IDC_SETTINGS, mResizer.kBL);
	mResizer.Add(IDC_FIRMWAREMANAGER, mResizer.kBL);
	mResizer.Add(IDOK, mResizer.kBR);

	mCtrlDevs.OnDpiChanged();

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

void ATUIDialogSysConfigDevices::OnDataExchange(bool write) {
	mCtrlDevs.OnDataExchange(write);
}

void ATUIDialogSysConfigDevices::OnDpiChanged() {
	mCtrlDevs.OnDpiChanged();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigSpeed final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigSpeed();

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	void OnHScroll(uint32 id, int code) override;

	void UpdateSpeedLabel();
	int GetAdjustedSpeedTicks();

	VDUIProxyButtonControl mMatchHardwareView;
	VDUIProxyButtonControl mMatchBroadcastView;
	VDUIProxyButtonControl mIntegralView;
	VDUIProxyButtonControl mWarpSpeedView;
	VDUIProxyButtonControl mAutoPauseView;

	CmdRadioBinding mFrameRateModeBinding;
	CmdBoolBinding mWarpSpeedBinding { "System.ToggleWarpSpeed" };
	CmdBoolBinding mAutoPauseBinding { "System.TogglePauseWhenInactive" };
};

ATUIDialogSysConfigSpeed::ATUIDialogSysConfigSpeed()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_SPEED)
{
	mMatchHardwareView.SetOnClicked([this] { mFrameRateModeBinding.Write(); });
	mMatchBroadcastView.SetOnClicked([this] { mFrameRateModeBinding.Write(); });
	mIntegralView.SetOnClicked([this] { mFrameRateModeBinding.Write(); });
	mWarpSpeedView.SetOnClicked([this] { mWarpSpeedBinding.Write(); });
	mAutoPauseView.SetOnClicked([this] { mAutoPauseBinding.Write(); });
}

bool ATUIDialogSysConfigSpeed::OnLoaded() {
	TBSetRange(IDC_SPEED_ADJUST, 1, 550);

	AddProxy(&mMatchHardwareView, IDC_RATE_HARDWARE);
	AddProxy(&mMatchBroadcastView, IDC_RATE_BROADCAST);
	AddProxy(&mIntegralView, IDC_RATE_INTEGRAL);
	AddProxy(&mWarpSpeedView, IDC_WARP);
	AddProxy(&mAutoPauseView, IDC_PAUSEWHENINACTIVE);

	mFrameRateModeBinding.Bind("System.FrameRateModeHardware", &mMatchHardwareView);
	mFrameRateModeBinding.Bind("System.FrameRateModeBroadcast", &mMatchBroadcastView);
	mFrameRateModeBinding.Bind("System.FrameRateModeIntegral", &mIntegralView);
	mWarpSpeedBinding.Bind(&mWarpSpeedView);
	mAutoPauseBinding.Bind(&mAutoPauseView);

	AddHelpEntry(IDC_RATE_HARDWARE, L"Base frame rate",
		L"Select the baseline rate at which the emulator runs. \"Match hardware\" uses the accurate speed of the \
original hardware, but can result in occasional stuttering due to being slightly non-standard. \"Broadcast\" and \
\"integral\" use rates that are more common in PC graphics hardware. This adjustment scales the entire emulation speed \
slightly so that emulation accuracy is not affected.");

	LinkHelpEntry(IDC_RATE_BROADCAST, IDC_RATE_HARDWARE);
	LinkHelpEntry(IDC_RATE_INTEGRAL, IDC_RATE_HARDWARE);

	AddHelpEntry(IDC_SPEED_ADJUST, L"Speed adjustment",
		L"Scale the baseline rate to run the emulation faster or slower than normal compared to real time. Events \
within the emulation still occur at correct relative timing so that emulation accuracy is not affected.");

	AddHelpEntry(IDC_WARP, L"Run as fast as possible (warp)",
		L"Disable the speed limiter and run the emulation as fast as the host PC can handle. Events within the emulation \
still occur at correct relative timing, so there is no change within the emulation other than it occurring faster in \
real time."
	);

	AddHelpEntry(IDC_PAUSEWHENINACTIVE, L"Pause when emulator window is inactive",
		L"Automatically pause the emulation when another application becomes active.");

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

void ATUIDialogSysConfigSpeed::OnDataExchange(bool write) {
	if (!write) {
		int rawTicks = VDRoundToInt((ATUIGetSpeedModifier() + 1.0f) * 100.0f);

		if (rawTicks >= 100) {
			if (rawTicks > 100)
				rawTicks += 50;
			else
				rawTicks += 25;
		}

		TBSetValue(IDC_SPEED_ADJUST, rawTicks);

		UpdateSpeedLabel();

		mFrameRateModeBinding.Read();
		mWarpSpeedBinding.Read();
		mAutoPauseBinding.Read();
	}
}

void ATUIDialogSysConfigSpeed::OnHScroll(uint32 id, int code) {
	ATUISetSpeedModifier((float)GetAdjustedSpeedTicks() / 100.0f - 1.0f);
	UpdateSpeedLabel();
}

void ATUIDialogSysConfigSpeed::UpdateSpeedLabel() {
	SetControlTextF(IDC_STATIC_SPEED_ADJUST, L"%d%%", GetAdjustedSpeedTicks());
}

int ATUIDialogSysConfigSpeed::GetAdjustedSpeedTicks() {
	int rawTicks = TBGetValue(IDC_SPEED_ADJUST);

	if (rawTicks >= 100) {
		if (rawTicks >= 150)
			rawTicks -= 50;
		else
			rawTicks = 100;
	}

	return rawTicks;
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigEaseOfUse final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigEaseOfUse();

protected:
	bool OnLoaded() override;
};

ATUIDialogSysConfigEaseOfUse::ATUIDialogSysConfigEaseOfUse()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_EASEOFUSE)
{
}

bool ATUIDialogSysConfigEaseOfUse::OnLoaded() {
	BindCheckbox(IDC_AUTORESET_CARTRIDGE, "Options.ToggleAutoResetCartridge");
	BindCheckbox(IDC_AUTORESET_BASIC, "Options.ToggleAutoResetBasic");
	BindCheckbox(IDC_AUTORESET_VIDEOSTD, "Options.ToggleAutoResetVideoStandard");

	AddHelpEntry(IDC_AUTORESET_CARTRIDGE, L"Reset when changing cartridges",
		L"Reset when adding or removing a cartridge. This is the default since most of the time pulling or inserting a cartridge \
live would cause the computer to crash anyway. However, there are a few specific cartridges that are designed to be hot-pulled or \
hot-inserted.");

	AddHelpEntry(IDC_AUTORESET_BASIC, L"Reset when toggling internal BASIC",
		L"Reset when enabling or disabling internal BASIC. This is needed for the change to take effect since the Option button \
is only checked on startup.");

	AddHelpEntry(IDC_AUTORESET_VIDEOSTD, L"Reset when changing video standard",
		L"Reset when changing between NTSC/PAL or any other video standard. This is not usually necessary but there are a few \
rare cases where NTSC/PAL is only checked at startup, such as the C: cassette tape handler. On a real computer, this change \
involves swapping chips or the motherboard."
	);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigDebugger final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigDebugger();

protected:
	bool OnLoaded() override;

	VDUIProxyComboBoxControl mSymbolLoadModePreStartView;
	VDUIProxyComboBoxControl mSymbolLoadModePostStartView;
	VDUIProxyComboBoxControl mScriptAutoLoadModeView;

	static constexpr CmdMapEntry sPreStartModes[] = {
		{ "Debug.PreStartSymbolLoadDisabled",	L"Disabled - no automatic symbol load" },
		{ "Debug.PreStartSymbolLoadDeferred",	L"Deferred - load when manually triggered (default)" },
		{ "Debug.PreStartSymbolLoadEnabled",	L"Enabled - load symbols along with images" },
	};

	static constexpr CmdMapEntry sPostStartModes[] = {
		{ "Debug.PostStartSymbolLoadDisabled",	L"Disabled - no automatic symbol load" },
		{ "Debug.PostStartSymbolLoadDeferred",	L"Deferred - load when manually triggered" },
		{ "Debug.PostStartSymbolLoadEnabled",	L"Enabled - load symbols along with images (default)" },
	};

	static constexpr CmdMapEntry sScriptModes[] = {
		{ "Debug.ScriptAutoLoadDisabled",	L"Disabled - do not autoload .atdbg files" },
		{ "Debug.ScriptAutoLoadAskToLoad",	L"Ask to load (default)" },
		{ "Debug.ScriptAutoLoadEnabled",	L"Enabled - automatically load .atdbg files" },
	};

	CmdComboBinding mPreStartMapBinding { sPreStartModes };
	CmdComboBinding mPostStartMapBinding { sPostStartModes };
	CmdComboBinding mScriptMapBinding { sScriptModes };
};

ATUIDialogSysConfigDebugger::ATUIDialogSysConfigDebugger()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_DEBUGGER)
{
	mSymbolLoadModePreStartView.SetOnSelectionChanged(
		[this](int idx) {
			mPreStartMapBinding.Write();
		}
	);

	mSymbolLoadModePostStartView.SetOnSelectionChanged(
		[this](int idx) {
			mPostStartMapBinding.Write();
		}
	);

	mScriptAutoLoadModeView.SetOnSelectionChanged(
		[this](int idx) {
			mScriptMapBinding.Write();
		}
	);
}

bool ATUIDialogSysConfigDebugger::OnLoaded() {
	AddProxy(&mSymbolLoadModePreStartView, IDC_SYMBOLLOAD_PRESTART);
	AddProxy(&mSymbolLoadModePostStartView, IDC_SYMBOLLOAD_POSTSTART);
	AddProxy(&mScriptAutoLoadModeView, IDC_SCRIPTAUTOLOAD);

	mPreStartMapBinding.Bind(&mSymbolLoadModePreStartView);
	mPostStartMapBinding.Bind(&mSymbolLoadModePostStartView);
	mScriptMapBinding.Bind(&mScriptAutoLoadModeView);

	AddAutoReadBinding(&mPreStartMapBinding);
	AddAutoReadBinding(&mPostStartMapBinding);
	AddAutoReadBinding(&mScriptMapBinding);

	AddHelpEntry(IDC_SYMBOLLOAD_PRESTART, L"Pre-start symbol load mode",
		L"Whether symbols and scripts are loaded for images that are loaded while the debugger is disabled. The default "
		L"is Deferred, which tracks but does not load symbols until explicitly triggered."
	);

	AddHelpEntry(IDC_SYMBOLLOAD_POSTSTART, L"Post-start symbol load mode",
		L"Whether symbols and scripts are loaded for images that are loaded after the debugger is enabled. The default "
		L"is Enabled, which loads the symbols immediately."
	);

	AddHelpEntry(IDC_SCRIPTAUTOLOAD, L"Script auto-loading",
		L"Controls when debugger script (.atdbg) files are auto-loaded when an image is loaded. Note that this only "
		L"happens if symbol loading is set to Enabled or Deferred."
	);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigDisplay final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigDisplay();

protected:
	bool OnLoaded() override;
};

ATUIDialogSysConfigDisplay::ATUIDialogSysConfigDisplay()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_DISPLAY)
{
}

bool ATUIDialogSysConfigDisplay::OnLoaded() {
	auto *indicatorBinding = BindCheckbox(IDC_SHOW_INDICATORS, "View.ToggleIndicators");
	auto *marginBinding = BindCheckbox(IDC_PAD_INDICATORS, "View.ToggleIndicatorMargin");

	indicatorBinding->GetView().SetOnClicked([=] {
		indicatorBinding->Write();
		marginBinding->Read();
	});

	BindCheckbox(IDC_HWACCEL_SCREENFX, "View.ToggleAccelScreenFX");

	AddHelpEntry(IDC_SHOW_INDICATORS, L"Show indicators",
		L"Draw on-screen overlays for device status.");

	AddHelpEntry(IDC_PAD_INDICATORS, L"Pad bottom margin to reserve space for indicators",
		L"Move the display up and reserve space at the bottom of the display for indicators.");

	AddHelpEntry(IDC_HWACCEL_SCREENFX,
		L"Use hardware acceleration for screen effects",
		L"Accelerate screen effects like scanlines and color correction with shaders. This requires "
			L"a shader-capable graphics card and Direct3D 9 or 11 to be enabled in Options."
	);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigBoot final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigBoot();

protected:
	bool OnLoaded() override;

	VDUIProxyComboBoxControl mProgramLoadModeView;

	static constexpr CmdMapEntry kProgramLoadModeOptions[] = {
		{ "System.ProgramLoadModeDefault", L"Default" },
		{ "System.ProgramLoadModeDiskBoot", L"Disk boot" },
		{ "System.ProgramLoadModeType3Poll", L"Type 3 poll" },
		{ "System.ProgramLoadModeDeferred", L"Deferred" },
	};

	CmdComboBinding mProgramLoadModeBinding { kProgramLoadModeOptions };
};

constexpr ATUIDialogSysConfigPage::CmdMapEntry ATUIDialogSysConfigBoot::kProgramLoadModeOptions[];

ATUIDialogSysConfigBoot::ATUIDialogSysConfigBoot()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_BOOT)
{
	mProgramLoadModeView.SetOnSelectionChanged([this](int) { mProgramLoadModeBinding.Write(); });
}

bool ATUIDialogSysConfigBoot::OnLoaded() {
	BindCheckbox(IDC_BOOTUNLOAD_CARTRIDGES, "Options.ToggleBootUnloadCartridges");
	BindCheckbox(IDC_BOOTUNLOAD_DISKS, "Options.ToggleBootUnloadDisks");
	BindCheckbox(IDC_BOOTUNLOAD_TAPES, "Options.ToggleBootUnloadTapes");

	AddProxy(&mProgramLoadModeView, IDC_PROGRAMLOADMODE);
	mProgramLoadModeBinding.Bind(&mProgramLoadModeView);

	AddAutoReadBinding(&mProgramLoadModeBinding);

	AddHelpEntry(IDC_PROGRAMLOADMODE, L"Program load mode",
		L"Method to use when booting binary programs directly in the emulator. Disk boot is the default method, triggering \
when the disk boot would normally start. Type 3 poll triggers when the XL/XE OS polls for SIO handlers after disk and cartridge boots, \
allowing the load to start after DOS 2.x boots. Deferred waits for the load to be triggered manually by a program, such as LOADEXE.COM \
on the Additions disk. Disk boot uses a simulated disk image with a special boot sector.");

	AddHelpEntry(IDC_BOOTUNLOAD_CARTRIDGES, L"Unload image types when booting new image",
		L"Selects image types to automatically unload when booting a new image. The default is to unload everything so that only \
the new image is mounted. When booting with more than one image type, like a cartridge that also allows booting DOS, it can be \
handy to disable auto-unload for some types."
	);

	LinkHelpEntry(IDC_BOOTUNLOAD_DISKS, IDC_BOOTUNLOAD_CARTRIDGES);
	LinkHelpEntry(IDC_BOOTUNLOAD_TAPES, IDC_BOOTUNLOAD_CARTRIDGES);

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigKeyboard final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigKeyboard();

	const char *GetPageTag() const override { return "keyboard"; }

protected:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	VDUIProxyComboBoxControl mProgramLoadModeView;

	static constexpr CmdMapEntry kLayoutOptions[] = {
		{ "Input.KeyboardLayoutNatural", L"Natural: Map host keys by typed character" },
		{ "Input.KeyboardLayoutDirect", L"Direct: Map host keys by location" },
		{ "Input.KeyboardLayoutCustom", L"Custom layout" },
	};

	static constexpr CmdMapEntry kModeOptions[] = {
		{ "Input.KeyboardModeCooked", L"Cooked keys" },
		{ "Input.KeyboardModeRaw", L"Raw keys" },
		{ "Input.KeyboardModeFullScan", L"Full raw keyboard scan" },
	};

	static constexpr CmdMapEntry kArrowModeOptions[] = {
		{ "Input.KeyboardArrowModeDefault", L"Arrows by default; Ctrl inverted" },
		{ "Input.KeyboardArrowModeAutoCtrl", L"Arrows by default; Ctrl/Shift states mapped directly" },
		{ "Input.KeyboardArrowModeRaw", L"Map host keys directly to -/=/+/*" },
	};

	VDUIProxyComboBoxControl mLayoutView;
	VDUIProxyComboBoxControl mModeView;
	VDUIProxyComboBoxControl mArrowModeView;

	CmdComboBinding mLayoutBinding { kLayoutOptions };
	CmdComboBinding mModeBinding { kModeOptions };
	CmdComboBinding mArrowModeBinding { kArrowModeOptions };

	VDUIProxyButtonControl mCopyToCustomView;
	VDUIProxyButtonControl mCustomizeLayoutView;
	VDUIProxyButtonControl mFunctionKeysView;
	VDUIProxyButtonControl mAllowShiftOnResetView;
	VDUIProxyButtonControl mAllowInputMapOverlapView;

	CmdTriggerBinding mCopyToCustomBinding { "Input.KeyboardCopyToCustomLayout" };
	CmdTriggerBinding mCustomizeLayoutBinding { "Input.KeyboardCustomizeLayoutDialog" };
	CmdBoolBinding mFunctionKeysBinding { "Input.Toggle1200XLFunctionKeys" };
	CmdBoolBinding mAllowShiftOnResetBinding { "Input.ToggleAllowShiftOnReset" };
	CmdBoolBinding mAllowInputMapOverlapBinding { "Input.ToggleAllowInputMapKeyboardOverlap" };
};

constexpr ATUIDialogSysConfigPage::CmdMapEntry ATUIDialogSysConfigKeyboard::kModeOptions[];

ATUIDialogSysConfigKeyboard::ATUIDialogSysConfigKeyboard()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_KEYBOARD)
{
	mLayoutView.SetOnSelectionChanged([this](int) { mLayoutBinding.Write(); mCopyToCustomBinding.Read(); mCustomizeLayoutBinding.Read(); mFunctionKeysBinding.Read(); });
	mModeView.SetOnSelectionChanged([this](int) { mModeBinding.Write(); });
	mArrowModeView.SetOnSelectionChanged([this](int) { mArrowModeBinding.Write(); });

	mCopyToCustomView.SetOnClicked([this] { mCopyToCustomBinding.Write(); mLayoutBinding.Read(); mCustomizeLayoutBinding.Read(); mFunctionKeysBinding.Read(); });
	mCustomizeLayoutView.SetOnClicked([this] { mCustomizeLayoutBinding.Write(); });
	mFunctionKeysView.SetOnClicked([this] { mFunctionKeysBinding.Write(); });
	mAllowShiftOnResetView.SetOnClicked([this] { mAllowShiftOnResetBinding.Write(); });
	mAllowInputMapOverlapView.SetOnClicked([this] { mAllowInputMapOverlapBinding.Write(); });
}

bool ATUIDialogSysConfigKeyboard::OnLoaded() {
	AddProxy(&mLayoutView, IDC_LAYOUT);
	AddProxy(&mModeView, IDC_KEYMODE);
	AddProxy(&mArrowModeView, IDC_ARROWKEYMODE);
	AddProxy(&mCopyToCustomView, IDC_COPY_TO_CUSTOM);
	AddProxy(&mCustomizeLayoutView, IDC_CUSTOMIZE);
	AddProxy(&mFunctionKeysView, IDC_ENABLE_FKEYS);
	AddProxy(&mAllowShiftOnResetView, IDC_RESETSHIFT);
	AddProxy(&mAllowInputMapOverlapView, IDC_ALLOW_INPUT_OVERLAP);

	mLayoutBinding.Bind(&mLayoutView);
	mModeBinding.Bind(&mModeView);
	mArrowModeBinding.Bind(&mArrowModeView);
	mCopyToCustomBinding.Bind(&mCopyToCustomView);
	mCustomizeLayoutBinding.Bind(&mCustomizeLayoutView);
	mFunctionKeysBinding.Bind(&mFunctionKeysView);
	mAllowShiftOnResetBinding.Bind(&mAllowShiftOnResetView);
	mAllowInputMapOverlapBinding.Bind(&mAllowInputMapOverlapView);

	AddHelpEntry(IDC_KEYMODE, L"Key press mode",
L"Control how keys are sent to the emulation. Cooked mode sends key presses, reducing dropped or duplicate characters for easier typing in productivity \
apps. Raw mode lets programs sense held keys for better compatibility with non-typing uses of the keyboard and is best for games. Full raw scan is the most accurate mode and emulates keyboard scanning delays (seldom needed).");

	AddHelpEntry(IDC_LAYOUT, L"Layout",
L"Select mapping from host to emulated keyboard. Natural maps letters and symbols and is best for typing (Shift+2 = @). Direct maps keys by position instead \
for programs that have non-typing keyboard usage (Shift+2 = \"), but can be more confusing for symbols. Custom allows all key combinations to be remapped as needed.");

	AddHelpEntry(IDC_COPY_TO_CUSTOM, L"Copy default layout to custom layout",
L"Copy one of the default layouts to the custom layout. This avoids having to set up the custom layout completely from scratch.");

	AddHelpEntry(IDC_CUSTOMIZE, L"Customize layout",
L"Open the custom keyboard layout editor.");

	AddHelpEntry(IDC_ENABLE_FKEYS, L"Enable F1-F4 as 1200XL function keys",
L"Map F1-F4 in the default keyboard layouts to the F1-F4 keys on the 1200XL keyboard. These keys were only present on the 1200XL and rarely used. \
Note that this overrides the default F2-F4 keys for Start/Select/Option unless you remap those in a custom layout.");

	AddHelpEntry(IDC_RESETSHIFT, L"Allow SHIFT key to be detected on cold reset",
L"Control whether the emulation detects the SHIFT key if it is held when a cold reset occurs. By default this is suppressed so that \
the default Shift+F5 shortcut for a cold reset doesn't also cause cartridges to see SHIFT held on boot. Enabling this option allows \
SHIFT to be sensed.");

	AddHelpEntry(IDC_ALLOW_INPUT_OVERLAP, L"Share host keys between keyboard and input maps",
L"Allow the same key to be mapped by the keyboard and an input map. If disabled, input maps will have priority and any conflicting \
keyboard mappings are ignored. If enabled, both the input map and keyboard mapping will activate. For instance, the joystick and keyboard \
could both be activated by the host arrow keys.");

	AddHelpEntry(IDC_ARROWKEYMODE, L"Arrow key mode",
L"Controls how arrow keys are mapped in default layouts. The default mode flips Ctrl on the arrow keys so that they work as arrow keys \
naturally and Ctrl must be held to access the non-arrow-key functions of those keys. The second mode maps Shift+arrow and Ctrl+arrow directly \
but doesn't allow access to the non-arrow states of the keys. The third mode directly maps the host arrow keys to -/=/+/* as on the original keyboard.");

	OnDataExchange(false);

	return ATUIDialogSysConfigPage::OnLoaded();
}

void ATUIDialogSysConfigKeyboard::OnDataExchange(bool write) {
	if (!write) {
		mLayoutBinding.Read();
		mModeBinding.Read();
		mCopyToCustomBinding.Read();
		mCustomizeLayoutBinding.Read();
		mArrowModeBinding.Read();
		mFunctionKeysBinding.Read();
		mAllowShiftOnResetBinding.Read();
		mAllowInputMapOverlapBinding.Read();
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogSysConfigCaption final : public ATUIDialogSysConfigPage {
public:
	ATUIDialogSysConfigCaption();

private:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	void UpdateEnables();
	void OnCustomChanged();
	void OnTemplateChanged();
	void OnElementsClicked();

	VDUIProxyButtonControl mCustomView;
	VDUIProxyRichEditControl mTemplateView;
	VDUIProxyRichEditControl mCaptionView;
	VDUIProxyButtonControl mElementsView;

	uint32 mUpdateInterlock = 0;
};

ATUIDialogSysConfigCaption::ATUIDialogSysConfigCaption()
	: ATUIDialogSysConfigPage(IDD_CONFIGURE_CAPTION)
{
}

bool ATUIDialogSysConfigCaption::OnLoaded() {
	AddProxy(&mCustomView, IDC_CUSTOM);
	AddProxy(&mTemplateView, IDC_TEMPLATE);
	AddProxy(&mCaptionView, IDC_CAPTION);
	AddProxy(&mElementsView, IDC_ELEMENTS);

	mResizer.Add(IDC_TEMPLATE, mResizer.kMC | mResizer.kAvoidFlicker | mResizer.kSuppressFontChange);
	mResizer.Add(IDC_CAPTION, mResizer.kMC | mResizer.kAvoidFlicker | mResizer.kSuppressFontChange);

	OnDataExchange(false);

	mTemplateView.SetFontFamily(L"Lucida Console");
	mTemplateView.SetPlainTextMode();
	mCaptionView.SetFontFamily(L"Lucida Console");
	mCaptionView.SetPlainTextMode();
	mCaptionView.SetReadOnlyBackground();

	mCustomView.SetOnClicked([this] { OnCustomChanged(); });
	mTemplateView.SetOnTextChanged([this] { OnTemplateChanged(); });
	mElementsView.SetOnClicked([this] { OnElementsClicked(); });

	return ATUIDialogSysConfigPage::OnLoaded();
}

void ATUIDialogSysConfigCaption::OnDataExchange(bool write) {
	if (write) {
		if (mCustomView.GetChecked()) {
			ATUISetWindowCaptionTemplate(VDTextWToU8(mTemplateView.GetCaption()).c_str());
		} else {
			ATUISetWindowCaptionTemplate("");
		}
	} else {
		const char *s = ATUIGetWindowCaptionTemplate();

		++mUpdateInterlock;
		mCustomView.SetChecked(*s != 0);
		mTemplateView.SetCaption(VDTextU8ToW(VDStringSpanA(s)).c_str());
		--mUpdateInterlock;

		UpdateEnables();
	}
}

void ATUIDialogSysConfigCaption::UpdateEnables() {
	mTemplateView.SetEnabled(mCustomView.GetChecked());
}

void ATUIDialogSysConfigCaption::OnCustomChanged() {
	if (mCustomView.GetChecked()) {
		if (mTemplateView.GetCaption().empty())
			mTemplateView.SetCaption(VDTextAToW(ATUIGetDefaultWindowCaptionTemplate()).c_str());
	} else {
		mTemplateView.SetCaption(L"");
	}

	UpdateEnables();
}

void ATUIDialogSysConfigCaption::OnTemplateChanged() {
	VDStringW captionText;

	vdrefptr<IATUIWindowCaptionUpdater> updater;
	ATUICreateWindowCaptionUpdater(~updater);

	updater->Init([&](const wchar_t *s) { captionText = s; });
	updater->InitMonitoring(&g_sim);

	uint32 errorPos;
	const VDStringA& tmp8 = VDTextWToU8(mTemplateView.GetCaption());
	if (updater->SetTemplate(tmp8.c_str(), &errorPos)) {
		updater->Update(true, 0, 0, 0);

		mCaptionView.SetText(captionText.c_str());
	} else {
		VDStringW errorStr { L"[Compile error]\r\n\r\n" };

		uint32 lineStart = errorPos;

		while(lineStart > 0 && tmp8[lineStart - 1] != '\n')
			--lineStart;

		uint32 prevLineStart = lineStart;

		if (prevLineStart) {
			--prevLineStart;

			while(prevLineStart > 0 && tmp8[prevLineStart - 1] != '\n')
				--prevLineStart;
		}

		uint32 len = (uint32)tmp8.size();
		uint32 end = errorPos;

		while(end < len && tmp8[end] != '\n')
			++end;

		errorStr.append(VDTextU8ToW(tmp8.subspan(prevLineStart, end - prevLineStart)));

		for(uint32 i = errorPos - lineStart; i; --i)
			errorStr += L' ';

		errorStr += L'^';

		mCaptionView.SetText(errorStr.c_str());
	}
}

void ATUIDialogSysConfigCaption::OnElementsClicked() {
	static constexpr const wchar_t *kElements[][2]={
		{	L"~"				, L"~"				"\tInclude following element only if variable(s) non-empty" },
		{	L"!"				, L"!"				"\tInvert - evaluate '1' if following element empty" },
		{	L"( )"				, L"( )"			"\tGrouping" },
		{	L"?"				, L"?"				"\tConditional (if)" },
		{	L"? :"				, L"? :"			"\tConditional (if-else)" },
		{	L"basic"			, L"basic"			"\t'BASIC' if internal BASIC enabled" },
		{	L"extcpu"			, L"extcpu"			"\t'C02' or '816' for extended CPU types" },
		{	L"fps"				, L"fps"			"\tCurrent frames per second" },
		{	L"frame"			, L"frame"			"\tCurrent frame number" },
		{	L"hardwareType"		, L"hardwareType"	"\tHardware type setting" },
		{	L"hostcpu"			, L"hostcpu"		"\tCurrent host CPU usage" },
		{	L"isTempProfile"	, L"isTempProfile"	"\t'1' if current profile is temporary" },
		{	L"isDefaultProfile"	, L"isDefaultProfile"	"\t'1' if current profile is a machine type default" },
		{	L"isDebugging"		, L"isDebugging"	"\t'1' if debugger active" },
		{	L"isRunning"		, L"isRunning"		"\t'1' if emulation unpaused" },
		{	L"is5200"			, L"is5200"			"\t'5200' if hardware type is 5200" },
		{	L"kernelType"		, L"kernelType"		"\tKernel (OS) ROM type" },
		{	L"mainTitle"		, L"mainTitle"		"\tMain program title" },
		{	L"memoryType"		, L"memoryType"		"\tMemory type setting" },
		{	L"mouseCapture"		, L"mouseCapture"	"\tDeactivation text if mouse captured" },
		{	L"profilename"		, L"profilename"	"\tCurrent profile name" },
		{	L"rapidus"			, L"rapidus"		"\t'Rapidus' if Rapidus Accelerator enabled" },
		{	L"showfps"			, L"showfps"		"\t'1' if Show FPS is enabled" },
		{	L"u1mb"				, L"u1mb"			"\t'U1MB' if Ultimate1MB enabled" },
		{	L"vbxe"				, L"vbxe"			"\t'VBXE' if VideoBoard XE enabled" },
		{	L"videoType"		, L"videoType"		"\tVideo standard setting" },
		{	nullptr, nullptr }
	};

	constexpr auto sliceArray = [](const auto& elements, size_t column) constexpr {
		std::array<const wchar_t *, vdcountof(elements)> r {};

		for(size_t i = 0; i < vdcountof(elements); ++i)
			r[i] = elements[i][column];

		return r;
	};

	static constexpr auto kInsertTokens = sliceArray(kElements, 0);
	static constexpr auto kMenuItems = sliceArray(kElements, 1);

	int idx = ActivateMenuButton(IDC_ELEMENTS, kMenuItems.data());

	if ((unsigned)idx < kInsertTokens.size()-1) {
		const wchar_t *insertToken = kInsertTokens[idx];

		mTemplateView.ReplaceSelectedText(insertToken);
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogConfigureSystem final : public ATUIPagedDialog {
public:
	ATUIDialogConfigureSystem();

protected:
	void OnPopulatePages();
};

ATUIDialogConfigureSystem::ATUIDialogConfigureSystem()
	: ATUIPagedDialog(IDD_CONFIGURE)
{
}

void ATUIDialogConfigureSystem::OnPopulatePages() {
	AddPage(L"Overview", vdmakeunique<ATUIDialogSysConfigOverview>());
	AddPage(L"Recommendations", vdmakeunique<ATUIDialogSysConfigAssessment>());
	PushCategory(L"Computer");
	AddPage(L"System", vdmakeunique<ATUIDialogSysConfigSystem>());
	AddPage(L"CPU", vdmakeunique<ATUIDialogSysConfigCPU>());
	AddPage(L"Firmware", vdmakeunique<ATUIDialogSysConfigFirmware>());
	AddPage(L"Memory", vdmakeunique<ATUIDialogSysConfigMemory>());
	AddPage(L"Acceleration", vdmakeunique<ATUIDialogSysConfigAcceleration>());
	AddPage(L"Speed", vdmakeunique<ATUIDialogSysConfigSpeed>());
	AddPage(L"Boot", vdmakeunique<ATUIDialogSysConfigBoot>());
	PopCategory();
	PushCategory(L"Outputs");
	AddPage(L"Video", vdmakeunique<ATUIDialogSysConfigVideo>());
	AddPage(L"Audio", vdmakeunique<ATUIDialogSysConfigAudio>());
	PopCategory();
	PushCategory(L"Peripherals");
	AddPage(L"Devices", vdmakeunique<ATUIDialogSysConfigDevices>());
	AddPage(L"Keyboard", vdmakeunique<ATUIDialogSysConfigKeyboard>());
	AddPage(L"Disk", vdmakeunique<ATUIDialogSysConfigDisk>());
	AddPage(L"Cassette", vdmakeunique<ATUIDialogSysConfigCassette>());
	PopCategory();
	PushCategory(L"Emulator");
	AddPage(L"Debugger", vdmakeunique<ATUIDialogSysConfigDebugger>());
	AddPage(L"Display", vdmakeunique<ATUIDialogSysConfigDisplay>());
	AddPage(L"Ease of use", vdmakeunique<ATUIDialogSysConfigEaseOfUse>());
	AddPage(L"Window caption", vdmakeunique<ATUIDialogSysConfigCaption>());
	PopCategory();
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogConfigureSystem(VDGUIHandle hParent) {
	static int sLastPage = 0;

	ATUIDialogConfigureSystem dlg;

	dlg.SetInitialPage(sLastPage);
	dlg.ShowDialog(hParent);

	sLastPage = dlg.GetSelectedPage();
}

void ATUIShowCPUOptionsDialog(VDGUIHandle h) {
	ATUIDialogConfigureSystem dlg;
	
	dlg.SetInitialPage(1);
	dlg.ShowDialog(h);
}

void ATUIShowDialogDevices(VDGUIHandle hParent) {
	ATUIDialogConfigureSystem dlg;

	dlg.SetInitialPage(2);
	dlg.ShowDialog(hParent);
}

void ATUIShowDialogSpeedOptions(VDGUIHandle hParent) {
	ATUIDialogConfigureSystem dlg;

	dlg.SetInitialPage(3);
	dlg.ShowDialog(hParent);
}

struct ATUIKeyboardOptions;
bool ATUIShowDialogKeyboardOptions(VDGUIHandle hParent, ATUIKeyboardOptions& opts) {
	ATUIDialogConfigureSystem dlg;

	dlg.SetInitialPage(13);
	dlg.ShowDialog(hParent);
	return false;
}
