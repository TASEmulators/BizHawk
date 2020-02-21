//	Altirra - Atari 800/800XL/5200 emulator
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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/strutil.h>
#include <vd2/Dita/accel.h>
#include <vd2/Dita/services.h>
#include <vd2/vdjson/jsonreader.h>
#include <vd2/vdjson/jsonwriter.h>
#include <vd2/vdjson/jsonoutput.h>
#include <vd2/vdjson/jsonvalue.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/hotkeyexcontrol.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"
#include "uikeyboard.h"

class ATUIDialogKeyboardCustomize : public VDDialogFrameW32 {
public:
	ATUIDialogKeyboardCustomize();

protected:
	class MappingEntry;

	bool OnLoaded();
	void OnDataExchange(bool write);

	void OnScanCodeSelChanged(VDUIProxyListBoxControl *, int idx);
	void OnBindingSelChanged(VDUIProxyListView *, int idx);
	void OnHotKeyChanged(IVDUIHotKeyExControl *, VDUIAccelerator);

	void OnAddClicked();
	void OnRemoveClicked();
	void OnImportClicked();
	void OnExportClicked();
	void OnClearClicked();

	void SetCookedMode(bool enabled);
	void ReloadEmuKeyList();
	void SortMappings();
	void RebuildMappingList();

	bool CompareMappingOrder(uint32 a, uint32 b) const;
	uint32 GetCurrentAccelMapping() const;

	static uint32 GetMappingForAccel(const VDUIAccelerator& accel);
	static VDUIAccelerator GetAccelForMapping(uint32 mapping);

	bool IsScanCodeMapped(uint32 scanCode) const;
	void UpdateScanCodeIsMapped(uint32 scanCode);

	uint32 mSelChangeRecursionLock = 0;

	vdrefptr<IVDUIHotKeyExControl> mpHotKeyControl;
	vdfastvector<uint32> mMappings;
	vdfastvector<uint32> mScanCodeFilteredList;

	VDUIProxyEditControl mSearchControl;
	VDUIProxyListBoxControl mScanCodeList;
	VDUIProxyListView mBindingListControl;
	VDUIProxyButtonControl mCharModeControl;
	VDUIProxyButtonControl mVKModeControl;
	VDUIProxyButtonControl mAddControl;
	VDUIProxyButtonControl mRemoveControl;
	VDUIProxyButtonControl mImportControl;
	VDUIProxyButtonControl mExportControl;
	VDUIProxyButtonControl mClearButton;

	VDDelegate mDelScanCodeSelChanged;
	VDDelegate mDelBindingSelChanged;
	VDDelegate mDelHotKeyChanged;

	uint32 mScanCodeToEntry[0x200];

	static const uint32 kScanCodeTable[];
};

const uint32 ATUIDialogKeyboardCustomize::kScanCodeTable[]={
	kATUIKeyScanCode_Start,
	kATUIKeyScanCode_Select,
	kATUIKeyScanCode_Option,
	kATUIKeyScanCode_Break,

	0x3F,	// A
	0x15,	// B
	0x12,	// C
	0x3A,	// D
	0x2A,	// E
	0x38,	// F
	0x3D,	// G
	0x39,	// H
	0x0D,	// I
	0x01,	// J
	0x05,	// K
	0x00,	// L
	0x25,	// M
	0x23,	// N
	0x08,	// O
	0x0A,	// P
	0x2F,	// Q
	0x28,	// R
	0x3E,	// S
	0x2D,	// T
	0x0B,	// U
	0x10,	// V
	0x2E,	// W
	0x16,	// X
	0x2B,	// Y
	0x17,	// Z
	0x1F,	// 1
	0x1E,	// 2
	0x1A,	// 3
	0x18,	// 4
	0x1D,	// 5
	0x1B,	// 6
	0x33,	// 7
	0x35,	// 8
	0x30,	// 9
	0x32,	// 0
	0x03,	// F1
	0x04,	// F2
	0x13,	// F3
	0x14,	// F4
	0x22,	// .
	0x20,	// ,
	0x02,	// ;
	0x06,	// +
	0x07,	// *
	0x0E,	// -
	0x0F,	// =
	0x26,	// /
	0x36,	// <
	0x37,	// >
	0x21,	// Space
	0x0C,	// Enter
	0x34,	// Backspace
	0x1C,	// Esc
	0x2C,	// Tab
	0x27,	// Invert (Fuji)
	0x11,	// Help
	0x3C,	// Caps

	0x7F,	// Shift+A
	0x55,	// Shift+B
	0x52,	// Shift+C
	0x7A,	// Shift+D
	0x6A,	// Shift+E
	0x78,	// Shift+F
	0x7D,	// Shift+G
	0x79,	// Shift+H
	0x4D,	// Shift+I
	0x41,	// Shift+J
	0x45,	// Shift+K
	0x40,	// Shift+L
	0x65,	// Shift+M
	0x63,	// Shift+N
	0x48,	// Shift+O
	0x4A,	// Shift+P
	0x6F,	// Shift+Q
	0x68,	// Shift+R
	0x7E,	// Shift+S
	0x6D,	// Shift+T
	0x4B,	// Shift+U
	0x50,	// Shift+V
	0x6E,	// Shift+W
	0x56,	// Shift+X
	0x6B,	// Shift+Y
	0x57,	// Shift+Z

	0x5F,	// Shift+1 (!)
	0x5E,	// Shift+2 (\")
	0x5A,	// Shift+3 (#)
	0x58,	// Shift+4 ($)
	0x5D,	// Shift+5 (%)
	0x5B,	// Shift+6 (&)
	0x73,	// Shift+7 (')
	0x75,	// Shift+8 (@)
	0x70,	// Shift+9 (()
	0x72,	// Shift+0 ())

	0x43,	// Shift+F1
	0x44,	// Shift+F2
	0x53,	// Shift+F3
	0x54,	// Shift+F4

	0x60,	// Shift+, ([)
	0x62,	// Shift+. (])
	0x42,	// Shift+; (:)
	0x46,	// Shift++ (\\)
	0x47,	// Shift+* (^)
	0x4E,	// Shift+- (_)
	0x4F,	// Shift+= (|)
	0x66,	// Shift+/ (?)
	0x76,	// Shift+< (Clear)
	0x77,	// Shift+> (Insert)
	0x61,	// Shift+Space
	0x4C,	// Shift+Enter
	0x74,	// Shift+Backspace
	0x5C,	// Shift+Esc
	0x6C,	// Shift+Tab
	0x67,	// Shift+Invert (Fuji)
	0x51,	// Shift+Help
	0x7C,	// Shift+Caps

	0xBF,	// Ctrl+A
	0x95,	// Ctrl+B
	0x92,	// Ctrl+C
	0xBA,	// Ctrl+D
	0xAA,	// Ctrl+E
	0xB8,	// Ctrl+F
	0xBD,	// Ctrl+G
	0xB9,	// Ctrl+H
	0x8D,	// Ctrl+I
	0x81,	// Ctrl+J
	0x85,	// Ctrl+K
	0x80,	// Ctrl+L
	0xA5,	// Ctrl+M
	0xA3,	// Ctrl+N
	0x88,	// Ctrl+O
	0x8A,	// Ctrl+P
	0xAF,	// Ctrl+Q
	0xA8,	// Ctrl+R
	0xBE,	// Ctrl+S
	0xAD,	// Ctrl+T
	0x8B,	// Ctrl+U
	0x90,	// Ctrl+V
	0xAE,	// Ctrl+W
	0x96,	// Ctrl+X
	0xAB,	// Ctrl+Y
	0x97,	// Ctrl+Z

	0x9F,	// Ctrl+1
	0x9E,	// Ctrl+2
	0x9A,	// Ctrl+3
	0x98,	// Ctrl+4
	0x9D,	// Ctrl+5
	0x9B,	// Ctrl+6
	0xB3,	// Ctrl+7
	0xB5,	// Ctrl+8
	0xB0,	// Ctrl+9
	0xB2,	// Ctrl+0

	0x83,	// Ctrl+F1
	0x84,	// Ctrl+F2
	0x93,	// Ctrl+F3
	0x94,	// Ctrl+F4

	0xA0,	// Ctrl+,
	0xA2,	// Ctrl+.
	0x82,	// Ctrl+;
	0x86,	// Ctrl++ (Left)
	0x87,	// Ctrl+* (Right)
	0x8E,	// Ctrl+- (Up)
	0x8F,	// Ctrl+= (Down)
	0xA6,	// Ctrl+/
	0xB6,	// Ctrl+<
	0xB7,	// Ctrl+>
	0xA1,	// Ctrl+Space
	0x8C,	// Ctrl+Enter
	0xB4,	// Ctrl+Backspace
	0x9C,	// Ctrl+Esc
	0xAC,	// Ctrl+Tab
	0xA7,	// Ctrl+Invert (Fuji)
	0x91,	// Ctrl+Help
	0xBC,	// Ctrl+Caps

	0xFF,	// Ctrl+Shift+A
	0xD5,	// Ctrl+Shift+B
	0xD2,	// Ctrl+Shift+C
	0xFA,	// Ctrl+Shift+D
	0xEA,	// Ctrl+Shift+E
	0xF8,	// Ctrl+Shift+F
	0xFD,	// Ctrl+Shift+G
	0xF9,	// Ctrl+Shift+H
	0xCD,	// Ctrl+Shift+I
	0xC1,	// Ctrl+Shift+J
	0xC5,	// Ctrl+Shift+K
	0xC0,	// Ctrl+Shift+L
	0xE5,	// Ctrl+Shift+M
	0xE3,	// Ctrl+Shift+N
	0xC8,	// Ctrl+Shift+O
	0xCA,	// Ctrl+Shift+P
	0xEF,	// Ctrl+Shift+Q
	0xE8,	// Ctrl+Shift+R
	0xFE,	// Ctrl+Shift+S
	0xED,	// Ctrl+Shift+T
	0xCB,	// Ctrl+Shift+U
	0xD0,	// Ctrl+Shift+V
	0xEE,	// Ctrl+Shift+W
	0xD6,	// Ctrl+Shift+X
	0xEB,	// Ctrl+Shift+Y
	0xD7,	// Ctrl+Shift+Z
	0xDF,	// Ctrl+Shift+1
	0xDE,	// Ctrl+Shift+2
	0xDA,	// Ctrl+Shift+3
	0xD8,	// Ctrl+Shift+4
	0xDD,	// Ctrl+Shift+5
	0xDB,	// Ctrl+Shift+6
	0xF3,	// Ctrl+Shift+7
	0xF5,	// Ctrl+Shift+8
	0xF0,	// Ctrl+Shift+9
	0xF2,	// Ctrl+Shift+0
	0xC3,	// Ctrl+Shift+F1
	0xC4,	// Ctrl+Shift+F2
	0xD3,	// Ctrl+Shift+F3
	0xD4,	// Ctrl+Shift+F4
	0xE0,	// Ctrl+Shift+,
	0xE2,	// Ctrl+Shift+.
	0xC2,	// Ctrl+Shift+;
	0xC6,	// Ctrl+Shift++
	0xC7,	// Ctrl+Shift+*
	0xCE,	// Ctrl+Shift+-
	0xCF,	// Ctrl+Shift+=
	0xE6,	// Ctrl+Shift+/
	0xF6,	// Ctrl+Shift+<
	0xF7,	// Ctrl+Shift+>
	0xE1,	// Ctrl+Shift+Space
	0xCC,	// Ctrl+Shift+Enter
	0xF4,	// Ctrl+Shift+Backspace
	0xDC,	// Ctrl+Shift+Esc
	0xEC,	// Ctrl+Shift+Tab
	0xE7,	// Ctrl+Shift+Invert (Fuji)
	0xD1,	// Ctrl+Help
	0xFC,	// Ctrl+Shift+Caps
};

///////////////////////////////////////////////////////////////////////////

class ATUIDialogKeyboardCustomize::MappingEntry final : public vdrefcounted<IVDUIListViewVirtualItem> {
public:
	MappingEntry(uint32 scanCodeEntryIndex, uint32 mapping);

	uint32 GetMapping() const { return mMapping; }
	void GetText(int subItem, VDStringW& s) const override;

private:
	uint32 mScanCodeEntryIndex;
	uint32 mMapping;
};

ATUIDialogKeyboardCustomize::MappingEntry::MappingEntry(uint32 scanCodeEntryIndex, uint32 mapping)
	: mScanCodeEntryIndex(scanCodeEntryIndex)
	, mMapping(mapping)
{
}

void ATUIDialogKeyboardCustomize::MappingEntry::GetText(int subItem, VDStringW& s) const {
	if (subItem == 0) {
		const wchar_t *label = nullptr;

		if (mScanCodeEntryIndex < vdcountof(kScanCodeTable))
			label = ATUIGetNameForKeyCode(kScanCodeTable[mScanCodeEntryIndex]);

		s = label ? label : L"?";
	} else {
		VDUIAccelerator accel = GetAccelForMapping(mMapping);

		VDUIGetAcceleratorString(accel, s);

		VDASSERT(!wcsstr(s.c_str(), L"Num"));
	}
}

///////////////////////////////////////////////////////////////////////////

ATUIDialogKeyboardCustomize::ATUIDialogKeyboardCustomize()
	: VDDialogFrameW32(IDD_KEYBOARD_CUSTOMIZE)
{
	static_assert(vdcountof(kScanCodeTable) == (uint8)vdcountof(kScanCodeTable), "Scan code table overflow");

	for(auto& e : mScanCodeToEntry)
		e = (uint8)vdcountof(kScanCodeTable);

	for(size_t i=0; i<vdcountof(kScanCodeTable); ++i) {
		mScanCodeToEntry[kScanCodeTable[i]] = (uint8)i;
	}

	mScanCodeList.OnSelectionChanged() += mDelScanCodeSelChanged.Bind(this, &ATUIDialogKeyboardCustomize::OnScanCodeSelChanged);
	mBindingListControl.OnItemSelectionChanged() += mDelBindingSelChanged.Bind(this, &ATUIDialogKeyboardCustomize::OnBindingSelChanged);
	mSearchControl.SetOnTextChanged([this](VDUIProxyEditControl*) { ReloadEmuKeyList(); });
	mCharModeControl.SetOnClicked([this] { SetCookedMode(true); });
	mVKModeControl.SetOnClicked([this] { SetCookedMode(false); });
	mAddControl.SetOnClicked([this] { OnAddClicked(); });
	mRemoveControl.SetOnClicked([this] { OnRemoveClicked(); });
	mImportControl.SetOnClicked([this] { OnImportClicked(); });
	mExportControl.SetOnClicked([this] { OnExportClicked(); });
	mClearButton.SetOnClicked([this] { OnClearClicked(); });
}

bool ATUIDialogKeyboardCustomize::OnLoaded() {
	AddProxy(&mScanCodeList, IDC_EMUKEY_LIST);
	AddProxy(&mSearchControl, IDC_SEARCH);
	AddProxy(&mBindingListControl, IDC_HOSTKEY_BINDINGS);
	AddProxy(&mCharModeControl, IDC_KEYMODE_CHAR);
	AddProxy(&mVKModeControl, IDC_KEYMODE_VK);
	AddProxy(&mAddControl, IDC_ADD);
	AddProxy(&mRemoveControl, IDC_REMOVE);
	AddProxy(&mImportControl, IDC_IMPORT);
	AddProxy(&mExportControl, IDC_EXPORT);
	AddProxy(&mClearButton, IDC_CLEAR);

	mBindingListControl.InsertColumn(0, L"Emulation Key", 0);
	mBindingListControl.InsertColumn(1, L"Host Key", 0);

	mBindingListControl.SetFullRowSelectEnabled(true);

	mVKModeControl.SetChecked(true);

	auto h = GetControl(IDC_HOTKEY);
	if (h) {
		auto *p = VDGetUIHotKeyExControl((VDGUIHandle)h);

		if (p) {
			mpHotKeyControl = p;
			mpHotKeyControl->OnChange() += mDelHotKeyChanged.Bind(this, &ATUIDialogKeyboardCustomize::OnHotKeyChanged);
		}
	}

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogKeyboardCustomize::OnDataExchange(bool write) {
	if (write) {
		ATUISetCustomKeyMap(mMappings.data(), mMappings.size());
	} else {
		mMappings.clear();
		ATUIGetCustomKeyMap(mMappings);

		SortMappings();
		RebuildMappingList();
		ReloadEmuKeyList();
	}
}

void ATUIDialogKeyboardCustomize::OnScanCodeSelChanged(VDUIProxyListBoxControl *, int idx) {
	if (mSelChangeRecursionLock)
		return;

	if ((unsigned)idx >= mScanCodeFilteredList.size())
		return;

	const uint8 scanCode = kScanCodeTable[mScanCodeFilteredList[idx]];
	auto it = std::find_if(mMappings.begin(), mMappings.end(), 
		[=](uint32 mapping) { return (mapping & 0x1FF) == scanCode; });

	if (it != mMappings.end()) {
		const int bindingIdx = (int)(it - mMappings.begin());

		++mSelChangeRecursionLock;
		mBindingListControl.SetSelectedIndex(bindingIdx);
		mBindingListControl.EnsureItemVisible(bindingIdx);
		--mSelChangeRecursionLock;
	}
}

void ATUIDialogKeyboardCustomize::OnBindingSelChanged(VDUIProxyListView *, int idx) {
	if (mSelChangeRecursionLock)
		return;

	auto *p = static_cast<MappingEntry *>(mBindingListControl.GetSelectedVirtualItem());

	if (!p)
		return;

	++mSelChangeRecursionLock;

	const uint32 mapping = p->GetMapping();
	const uint8 scanCodeIndex = mScanCodeToEntry[mapping & 0x1FF];

	if (scanCodeIndex < vdcountof(kScanCodeTable)) {
		auto it = std::lower_bound(mScanCodeFilteredList.begin(), mScanCodeFilteredList.end(), scanCodeIndex);

		if (it != mScanCodeFilteredList.end() && *it == scanCodeIndex)
			mScanCodeList.SetSelection((int)(it - mScanCodeFilteredList.begin()));
	}

	if (mapping & kATUIKeyboardMappingModifier_Cooked) {
		mCharModeControl.SetChecked(true);
		mVKModeControl.SetChecked(false);
	} else {
		mCharModeControl.SetChecked(false);
		mVKModeControl.SetChecked(true);
	}

	if (mpHotKeyControl) {
		VDUIAccelerator accel = GetAccelForMapping(mapping);

		mpHotKeyControl->SetAccelerator(accel);
	}

	--mSelChangeRecursionLock;
}

void ATUIDialogKeyboardCustomize::OnHotKeyChanged(IVDUIHotKeyExControl *, VDUIAccelerator) {
	if (mSelChangeRecursionLock)
		return;

	uint32 mappingBase = GetCurrentAccelMapping();
	if (!mappingBase)
		return;

	// try to find a mapping
	for(int i=0; i<2; ++i) {
		auto it = std::find_if(mMappings.begin(), mMappings.end(), [=](uint32 mapping) { return (mapping & 0xFFFFFE00) == mappingBase; });
		if (it != mMappings.end()) {
			++mSelChangeRecursionLock;
			int selIdx = (int)(it - mMappings.begin());
			mBindingListControl.SetSelectedIndex(selIdx);
			mBindingListControl.EnsureItemVisible(selIdx);
			--mSelChangeRecursionLock;
		}

		// couldn't find mapping of current type -- try flipping the mapping
		VDUIAccelerator accel;
		if (mappingBase & kATUIKeyboardMappingModifier_Cooked) {
			if (!VDUIGetVkAcceleratorForChar(accel, (wchar_t)((mappingBase >> 9) & 0xFFFF)))
				break;
		} else {
			accel = GetAccelForMapping(mappingBase);
			if (!VDUIGetCharAcceleratorForVk(accel))
				break;
		}

		mappingBase = GetMappingForAccel(accel);
		if (!mappingBase)
			break;
	}
}

void ATUIDialogKeyboardCustomize::OnAddClicked() {
	int scanCodeSelIndex = mScanCodeList.GetSelection();
	if (scanCodeSelIndex < 0)
		return;

	int scanCodeIndex = (int)mScanCodeList.GetItemData(scanCodeSelIndex);
	if ((unsigned)scanCodeIndex >= vdcountof(kScanCodeTable))
		return;

	uint32 mapping = GetCurrentAccelMapping();
	if (!mapping)
		return;

	mapping += kScanCodeTable[scanCodeIndex];

	// delete all items that have either the same source key
	size_t n = mMappings.size();
	for(size_t i = n; i; --i) {
		uint32 existingMapping = mMappings[i - 1];
		uint32 diff = existingMapping ^ mapping;

		if (!(diff & 0xFFFFFE00)) {
			mMappings.erase(mMappings.begin() + i - 1);
			--n;

			mBindingListControl.DeleteItem((int)(i - 1));
		}
	}

	auto it2 = std::lower_bound(mMappings.begin(), mMappings.end(), mapping, [this](uint32 a, uint32 b) { return CompareMappingOrder(a, b); });

	int insertIdx = (int)(it2 - mMappings.begin());
	mMappings.insert(it2, mapping);

	++mSelChangeRecursionLock;
	mBindingListControl.InsertVirtualItem(insertIdx, vdmakerefptr(new MappingEntry((uint32)scanCodeIndex, mapping)));
	mBindingListControl.SetSelectedIndex(insertIdx);
	mBindingListControl.EnsureItemVisible(insertIdx);
	mBindingListControl.AutoSizeColumns(true);
	--mSelChangeRecursionLock;
}

void ATUIDialogKeyboardCustomize::OnRemoveClicked() {
	auto *p = static_cast<MappingEntry *>(mBindingListControl.GetSelectedVirtualItem());

	if (!p)
		return;

	uint32 mapping = p->GetMapping();

	auto it = std::find(mMappings.begin(), mMappings.end(), mapping);
	if (it != mMappings.end())
		mMappings.erase(it);

	int idx = mBindingListControl.GetSelectedIndex();

	++mSelChangeRecursionLock;
	mBindingListControl.DeleteItem(idx);
	mBindingListControl.SetSelectedIndex(idx);
	mBindingListControl.EnsureItemVisible(idx);
	--mSelChangeRecursionLock;

	UpdateScanCodeIsMapped((uint8)(mapping & 0xFF));
}

void ATUIDialogKeyboardCustomize::OnImportClicked() {
	const auto& s = VDGetLoadFileName('kmap', (VDGUIHandle)mhdlg, L"Load custom keyboard map", L"Altirra keyboard map (*.atkmap)\0*.atkmap\0All files\0*.*\0", L"atkmap");
	if (s.empty())
		return;

	struct InvalidKeyboardMapFileException {};
	try {
		VDFile f(s.c_str());
		sint64 size = f.size();

		if (size > 0x1000000)
			throw InvalidKeyboardMapFileException();

		vdblock<char> buf((uint32)size);
		f.read(buf.data(), (long)size);
		f.close();

		VDJSONDocument doc;
		{
			VDJSONReader reader;
			if (!reader.Parse(buf.data(), buf.size(), doc))
				throw InvalidKeyboardMapFileException();
		}

		const auto& rootNode = doc.Root();
		if (!rootNode.IsObject())
			throw InvalidKeyboardMapFileException();

		if (wcscmp(rootNode[".type"].AsString(), L"keymap"))
			throw InvalidKeyboardMapFileException();

		const auto& mappingsNode = rootNode["mappings"];
		if (!mappingsNode.IsArray())
			throw InvalidKeyboardMapFileException();

		vdfastvector<uint32> mappings;
		size_t n = mappingsNode.GetArrayLength();
		for(size_t i=0; i<n; ++i) {
			const auto& mappingNode = mappingsNode[i];

			if (!mappingNode.IsObject())
				throw InvalidKeyboardMapFileException();

			const auto& scanCodeNode = mappingNode["scancode"];
			if (!scanCodeNode.IsValid())
				throw InvalidKeyboardMapFileException();

			sint64 scanCode = scanCodeNode.AsInt64();
			if (scanCode < 0 || scanCode > 511 || !ATIsValidScanCode((uint32)scanCode))
				continue;

			const auto& charNode = mappingNode["char"];
			if (charNode.IsValid()) {
				uint32 charCode = 0;

				if (charNode.IsString()) {
					const wchar_t *s = charNode.AsString();
					if (!*s || s[1])
						throw InvalidKeyboardMapFileException();

					charCode = (uint32)(uint16)*s;
				} else if (charNode.IsInt()) {
					sint64 cc = charNode.AsInt64();
					if (cc < 0 || cc >= 65535)
						continue;

					charCode = (uint16)cc;
				} else
					throw InvalidKeyboardMapFileException();

				mappings.push_back(ATUIPackKeyboardMapping((uint32)scanCode, charCode, kATUIKeyboardMappingModifier_Cooked));
			} else {
				const auto& vkNode = mappingNode["vk"];
				sint64 vk;

				if (vkNode.IsInt()) {
					vk = vkNode.AsInt64();
					if (vk <= 0 || vk > 65535)
						continue;
				} else if (vkNode.IsString()) {
					const wchar_t *s = vkNode.AsString();

					if (!s[0] || s[1])
						throw InvalidKeyboardMapFileException();

					uint32 ch = (uint32)s[0];

					if ((ch - (uint32)'0') >= 10 && (ch - (uint32)'A') >= 26)
						throw InvalidKeyboardMapFileException();

					vk = ch;
				} else
					throw InvalidKeyboardMapFileException();

				const auto& modifiersNode = mappingNode["modifiers"];
				sint64 mods = 0;
				if (modifiersNode.IsValid()) {
					if (!modifiersNode.IsInt())
						throw InvalidKeyboardMapFileException();

					mods = modifiersNode.AsInt64();
					if (mods <= 0 || mods > 15)
						continue;
				}

				mappings.push_back(ATUIPackKeyboardMapping((uint32)scanCode, (uint32)vk, (uint32)mods << 25));
			}
		}

		// strip duplicates
		std::sort(mappings.begin(), mappings.end());
		mappings.erase(std::unique(mappings.begin(), mappings.end()), mappings.end());

		// all good!
		mMappings.swap(mappings);
		SortMappings();
		ReloadEmuKeyList();
		RebuildMappingList();
	} catch(const MyError& e) {
		ShowError(e);
	} catch(const InvalidKeyboardMapFileException&) {
		VDStringW err;
		err.sprintf(L"\"%ls\" is not a valid keyboard map file.", s.c_str());
		ShowError(s.c_str());
	}
}

void ATUIDialogKeyboardCustomize::OnExportClicked() {
	const auto& path = VDGetSaveFileName('kmap', (VDGUIHandle)mhdlg, L"Save custom keyboard map", L"Altirra keyboard map (*.atkmap)\0*.atkmap\0", L"atkmap");
	if (path.empty())
		return;

	try {
		VDStringW s;
		VDJSONStringWriterOutputSysLE output(s);

		VDJSONWriter writer;

		writer.Begin(&output);
		writer.OpenObject();
		writer.WriteMemberName(L".comment");
		writer.WriteString(L"Altirra keyboard map");
		writer.WriteMemberName(L".type");
		writer.WriteString(L"keymap");
		writer.WriteMemberName(L"mappings");

		writer.OpenArray();

		auto sortedMappings = mMappings;

		std::sort(sortedMappings.begin(), sortedMappings.end());

		for(uint32 mapping : sortedMappings) {
			writer.OpenObject();

			writer.WriteMemberName(L"scancode");
			writer.WriteInt(mapping & 0x1FF);

			if (mapping & kATUIKeyboardMappingModifier_Cooked) {
				writer.WriteMemberName(L"char");
				
				uint32 ch = (mapping >> 9) & 0xFFFF;
				if (ch >= 0x20 && ch < 0x7F) {
					wchar_t buf[2] = { (wchar_t)ch, 0 };

					writer.WriteString(buf);
				} else
					writer.WriteInt(ch);
			} else {
				writer.WriteMemberName(L"vk");

				uint32 vk = (mapping >> 9) & 0xFFFF;
				if ((vk - 0x30) < 10 || (vk - 0x41) < 26) {
					wchar_t buf[2] = { (wchar_t)vk, 0 };

					writer.WriteString(buf);
				} else
					writer.WriteInt(vk);

				if (mapping >> 25) {
					writer.WriteMemberName(L"modifiers");
					writer.WriteInt(mapping >> 25);
				}
			}

			writer.Close();
		}
		writer.Close();

		writer.Close();
		writer.End();

		VDFileStream fs(path.c_str(), nsVDFile::kWrite | nsVDFile::kCreateAlways | nsVDFile::kDenyAll | nsVDFile::kSequential);
		VDStringA u8s(VDTextWToU8(s));

		fs.write(u8s.data(), u8s.size());
	} catch(const MyError& e) {
		ShowError(e);
	}
}

void ATUIDialogKeyboardCustomize::OnClearClicked() {
	if (Confirm(L"Are you sure you want to clear all key mappings?")) {
		mMappings.clear();
		ReloadEmuKeyList();
		RebuildMappingList();
	}
}

void ATUIDialogKeyboardCustomize::SetCookedMode(bool enabled) {
	if (mpHotKeyControl)
		mpHotKeyControl->SetCookedMode(enabled);
}

void ATUIDialogKeyboardCustomize::ReloadEmuKeyList() {
	mScanCodeList.SetRedraw(false);
	mScanCodeList.Clear();

	mScanCodeFilteredList.clear();

	VDStringW searchText;
	GetControlText(IDC_SEARCH, searchText);
	std::transform(searchText.begin(), searchText.end(), searchText.begin(), towlower);

	unsigned int index = 0;
	VDStringW temp;
	for(uint32 keyCode : kScanCodeTable) {
		const wchar_t *label = ATUIGetNameForKeyCode(keyCode);

		if (!label) {
			++index;
			continue;
		}

		if (!searchText.empty()) {
			temp = label;

			std::transform(temp.begin(), temp.end(), temp.begin(), towlower);
			if (!wcsstr(temp.c_str(), searchText.c_str())) {
				++index;
				continue;
			}
		}

		mScanCodeFilteredList.push_back(index);

		if (IsScanCodeMapped(keyCode)) {
			mScanCodeList.AddItem(label, index);
		} else {
			temp = label;
			temp += L" [not mapped]";

			mScanCodeList.AddItem(temp.c_str(), index);
		}

		++index;
	}

	mScanCodeList.SetRedraw(true);
}

void ATUIDialogKeyboardCustomize::SortMappings() {
	std::sort(mMappings.begin(), mMappings.end(), [this](uint32 a, uint32 b) { return CompareMappingOrder(a, b); });
}

void ATUIDialogKeyboardCustomize::RebuildMappingList() {
	mBindingListControl.SetRedraw(false);
	mBindingListControl.Clear();

	for(const auto& mapping : mMappings) {
		auto p = vdmakerefptr(new MappingEntry(mScanCodeToEntry[mapping & 0x1FF], mapping));

		mBindingListControl.InsertVirtualItem(-1, p);
	}

	mBindingListControl.SetRedraw(true);
	mBindingListControl.AutoSizeColumns(true);
}

bool ATUIDialogKeyboardCustomize::CompareMappingOrder(uint32 a, uint32 b) const {
	if ((a^b) & 0x1FF) {
		uint8 scanCodeOrderA = mScanCodeToEntry[a & 0x1FF];
		uint8 scanCodeOrderB = mScanCodeToEntry[b & 0x1FF];

		return scanCodeOrderA < scanCodeOrderB;
	}

	return a < b;
}

uint32 ATUIDialogKeyboardCustomize::GetCurrentAccelMapping() const {
	if (!mpHotKeyControl)
		return 0;

	VDUIAccelerator acc;
	mpHotKeyControl->GetAccelerator(acc);

	return GetMappingForAccel(acc);
}

uint32 ATUIDialogKeyboardCustomize::GetMappingForAccel(const VDUIAccelerator& acc) {
	if (!acc.mVirtKey)
		return 0;

	uint32 modifiers = 0;

	if (acc.mModifiers & acc.kModShift)
		modifiers += kATUIKeyboardMappingModifier_Shift;

	if (acc.mModifiers & acc.kModCtrl)
		modifiers += kATUIKeyboardMappingModifier_Ctrl;

	if (acc.mModifiers & acc.kModAlt)
		modifiers += kATUIKeyboardMappingModifier_Alt;

	if (acc.mModifiers & acc.kModExtended)
		modifiers += kATUIKeyboardMappingModifier_Extended;

	if (acc.mModifiers & acc.kModCooked)
		modifiers += kATUIKeyboardMappingModifier_Cooked;

	return ATUIPackKeyboardMapping(0, acc.mVirtKey, modifiers);
}

VDUIAccelerator ATUIDialogKeyboardCustomize::GetAccelForMapping(uint32 mapping) {
	VDUIAccelerator accel = {};
	accel.mVirtKey = (mapping >> 9) & 0xFFFF;

	if (mapping & kATUIKeyboardMappingModifier_Shift)
		accel.mModifiers += VDUIAccelerator::kModShift;

	if (mapping & kATUIKeyboardMappingModifier_Ctrl)
		accel.mModifiers += VDUIAccelerator::kModCtrl;

	if (mapping & kATUIKeyboardMappingModifier_Alt)
		accel.mModifiers += VDUIAccelerator::kModAlt;

	if (mapping & kATUIKeyboardMappingModifier_Extended)
		accel.mModifiers += VDUIAccelerator::kModExtended;

	if (mapping & kATUIKeyboardMappingModifier_Cooked)
		accel.mModifiers += VDUIAccelerator::kModCooked;

	return accel;
}

bool ATUIDialogKeyboardCustomize::IsScanCodeMapped(uint32 scanCode) const {
	return std::find_if(mMappings.begin(), mMappings.end(), [=](uint32 mapping) { return (mapping & 0x1FF) == scanCode; }) != mMappings.end();
}

void ATUIDialogKeyboardCustomize::UpdateScanCodeIsMapped(uint32 scanCode) {
	if (scanCode > kATUIKeyScanCodeLast)
		return;

	uint8 scanCodeIndex = mScanCodeToEntry[scanCode];

	if (scanCodeIndex >= vdcountof(kScanCodeTable))
		return;

	auto it = std::lower_bound(mScanCodeFilteredList.begin(), mScanCodeFilteredList.end(), scanCodeIndex);
	if (it == mScanCodeFilteredList.end() || *it != scanCodeIndex)
		return;

	int scanCodeFilteredIndex = (int)(it - mScanCodeFilteredList.begin());

	VDStringW s;
	
	const wchar_t *label = ATUIGetNameForKeyCode(kScanCodeTable[scanCodeIndex]);

	if (label)
		s = label;

	if (!IsScanCodeMapped(scanCode))
		s += L" [not mapped]";

	mScanCodeList.SetItemText(scanCodeFilteredIndex, s.c_str());
}

///////////////////////////////////////////////////////////////////////////

bool ATUIShowDialogKeyboardCustomize(VDGUIHandle hParent) {
	ATUIDialogKeyboardCustomize dlg;

	if (!dlg.ShowDialog(hParent))
		return false;

	return true;
}
