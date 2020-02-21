//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include <vd2/system/registry.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include "resource.h"
#include "cheatengine.h"
#include "uifilefilters.h"

///////////////////////////////////////////////////////////////////////////

class ATUIDialogEditCheat : public VDDialogFrameW32 {
	ATUIDialogEditCheat(const ATUIDialogEditCheat&);
	ATUIDialogEditCheat& operator=(const ATUIDialogEditCheat&);
public:
	ATUIDialogEditCheat(ATCheatEngine::Cheat& cheat);
	~ATUIDialogEditCheat();

protected:
	void OnDataExchange(bool write);
	uint32 GetValue(uint32 id);

	ATCheatEngine::Cheat& mCheat;
};

ATUIDialogEditCheat::ATUIDialogEditCheat(ATCheatEngine::Cheat& cheat)
	: VDDialogFrameW32(IDD_CHEAT_EDIT)
	, mCheat(cheat)
{
}

ATUIDialogEditCheat::~ATUIDialogEditCheat() {
}

void ATUIDialogEditCheat::OnDataExchange(bool write) {
	if (write) {
		uint32 addr = GetValue(IDC_ADDRESS);
		uint32 value = GetValue(IDC_VALUE);
		bool is16 = IsButtonChecked(IDC_MODE_16BIT);

		if (!mbValidationFailed) {
			mCheat.mAddress = addr;
			mCheat.mValue = value;
			mCheat.mb16Bit = is16;
		}
	} else {
		SetControlTextF(IDC_ADDRESS, L"$%04X", mCheat.mAddress);
		SetControlTextF(IDC_VALUE, mCheat.mb16Bit ? L"$%04X" : L"$%02X", mCheat.mValue);

		CheckButton(mCheat.mb16Bit ? IDC_MODE_16BIT : IDC_MODE_8BIT, true);
	}
}

uint32 ATUIDialogEditCheat::GetValue(uint32 id) {
	VDStringW s;
	GetControlText(id, s);

	const wchar_t *t = s.c_str();

	while(*t == L' ')
		++t;

	unsigned v;
	wchar_t c;
	if (*t == L'$') {
		++t;

		if (1 != swscanf(t, L"%x%c", &v, &c))
			FailValidation(id);
	} else {
		if (1 != swscanf(t, L"%u%c", &v, &c))
			FailValidation(id);
	}

	return (uint32)v;
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogCheater final : public VDDialogFrameW32 {
	ATUIDialogCheater(const ATUIDialogCheater&) = delete;
	ATUIDialogCheater& operator=(const ATUIDialogCheater&) = delete;

public:
	ATUIDialogCheater(ATCheatEngine *engine);
	~ATUIDialogCheater();

protected:
	bool OnLoaded();
	void OnDestroy();
	void OnDataExchange(bool write);
	bool OnCommand(uint32 id, uint32 extcode);
	void OnResultDblClk(VDUIProxyListView *sender, int index);
	void OnResultSelChanged(VDUIProxyListView *sender, int index);
	void OnCheatDblClk(VDUIProxyListView *sender, int index);
	void OnCheatCheckedChanged(VDUIProxyListView *sender, int index);
	void OnModeChanged(VDUIProxyComboBoxControl *sender, int index);
	void UpdateResultView();
	void UpdateActiveView();
	void UpdateValueEnable();

	ATCheatEngine *const mpEngine;

	VDUIProxyListView mResultView;
	VDUIProxyListView mActiveView;
	VDUIProxyComboBoxControl mModeView;

	VDDelegate mDelResultDblClk;
	VDDelegate mDelResultSelChanged;
	VDDelegate mDelActiveDblClk;
	VDDelegate mDelModeChanged;
	VDDelegate mDelActiveCheckChanged;

	class VItem : public vdrefcounted<IVDUIListViewVirtualItem> {
	public:
		VItem(uint32 addr, uint32 val, bool bit16)
			: mAddress(addr)
			, mValue(val)
			, mb16Bit(bit16)
		{
		}

		void GetText(int subItem, VDStringW& s) const {
			switch(subItem) {
				case 0:
					s.sprintf(L"$%04X", mAddress);
					break;

				case 1:
					if (mb16Bit)
						s.sprintf(L"$%04X (%u)", mValue, mValue);
					else
						s.sprintf(L"$%02X (%u)", mValue, mValue);
					break;
			}
		}

		const uint32 mAddress;
		const uint32 mValue;
		const bool mb16Bit;
	};

	class AItem : public vdrefcounted<IVDUIListViewVirtualItem> {
	public:
		AItem(ATCheatEngine *engine, uint32 i)
			: mpEngine(engine)
			, mIndex(i)
		{
		}

		void GetText(int subItem, VDStringW& s) const {
			const ATCheatEngine::Cheat& cheat = mpEngine->GetCheatByIndex(mIndex);

			switch(subItem) {
				case 0:
					s.sprintf(L"$%04X", cheat.mAddress);
					break;

				case 1:
					if (cheat.mb16Bit)
						s.sprintf(L"$%04X (%u)", cheat.mValue, cheat.mValue);
					else
						s.sprintf(L"$%02X (%u)", cheat.mValue, cheat.mValue);
					break;
			}
		}

		ATCheatEngine *mpEngine;
		uint32 mIndex;
	};

	struct AItemSorter : public IVDUIListViewVirtualComparer {
	public:
		virtual int Compare(IVDUIListViewVirtualItem *x, IVDUIListViewVirtualItem *y) {
			AItem *a = static_cast<AItem *>(x);
			AItem *b = static_cast<AItem *>(y);
			const ATCheatEngine::Cheat& c = a->mpEngine->GetCheatByIndex(a->mIndex);
			const ATCheatEngine::Cheat& d = b->mpEngine->GetCheatByIndex(b->mIndex);

			if (c.mAddress != d.mAddress)
				return c.mAddress < d.mAddress ? -1 : 1;

			return a->mIndex < b->mIndex ? -1 : 1;
		}
	};
};

ATUIDialogCheater::ATUIDialogCheater(ATCheatEngine *engine)
	: VDDialogFrameW32(IDD_CHEATER)
	, mpEngine(engine)
{
	mResultView.OnItemDoubleClicked() += mDelResultDblClk.Bind(this, &ATUIDialogCheater::OnResultDblClk);
	mResultView.OnItemSelectionChanged() += mDelResultSelChanged.Bind(this, &ATUIDialogCheater::OnResultSelChanged);
	mActiveView.OnItemDoubleClicked() += mDelActiveDblClk.Bind(this, &ATUIDialogCheater::OnCheatDblClk);
	mActiveView.OnItemCheckedChanged() += mDelActiveCheckChanged.Bind(this, &ATUIDialogCheater::OnCheatCheckedChanged);
	mModeView.OnSelectionChanged() += mDelModeChanged.Bind(this, &ATUIDialogCheater::OnModeChanged);
}

ATUIDialogCheater::~ATUIDialogCheater() {
}

bool ATUIDialogCheater::OnLoaded() {
	AddProxy(&mResultView, IDC_RESULTS);
	AddProxy(&mActiveView, IDC_ACTIVE);
	AddProxy(&mModeView, IDC_MODE);

	mResultView.SetFullRowSelectEnabled(true);
	mResultView.InsertColumn(0, L"Address", 0);
	mResultView.InsertColumn(1, L"Value", 0);

	mActiveView.SetItemCheckboxesEnabled(true);
	mActiveView.SetFullRowSelectEnabled(true);
	mActiveView.InsertColumn(0, L"Address", 0);
	mActiveView.InsertColumn(1, L"Value", 0);

	mModeView.AddItem(L"Start with new snapshot");
	mModeView.AddItem(L"= : Unchanged");
	mModeView.AddItem(L"!= : Changed");
	mModeView.AddItem(L"< : Values going down");
	mModeView.AddItem(L"<= : Values sometimes going down");
	mModeView.AddItem(L"> : Values going up");
	mModeView.AddItem(L">= : Values sometimes going up");
	mModeView.AddItem(L"=X : Find an exact value");

	OnDataExchange(false);
	SetFocusToControl(IDC_MODE);
	return true;
}

void ATUIDialogCheater::OnDestroy() {
	mResultView.Clear();
	mActiveView.Clear();
}

void ATUIDialogCheater::OnDataExchange(bool write) {
	VDRegistryAppKey key("Persistence\\Cheater");

	if (write) {
		key.setInt("Mode", mModeView.GetSelection());
		key.setInt("Value", GetControlValueSint32(IDC_VALUE));
		key.setInt("Type", IsButtonChecked(IDC_VT_16BIT));
	} else {
		mModeView.SetSelection(key.getEnumInt("Mode", kATCheatSnapModeCount - 1, 0));
		SetControlTextF(IDC_VALUE, L"%d", key.getInt("Value"));

		CheckButton(key.getInt("Type") ? IDC_VT_16BIT : IDC_VT_8BIT, true);

		UpdateValueEnable();
		UpdateResultView();
		UpdateActiveView();
	}
}

bool ATUIDialogCheater::OnCommand(uint32 id, uint32 extcode) {
	switch(id) {
		case IDC_UPDATE:
			{
				bool bit16 = IsButtonChecked(IDC_VT_16BIT);

				switch(mModeView.GetSelection()) {
					case 0:
						mpEngine->Snapshot(kATCheatSnapMode_Replace, 0, false);
						break;
					case 1:
						mpEngine->Snapshot(kATCheatSnapMode_Equal, 0, bit16);
						break;
					case 2:
						mpEngine->Snapshot(kATCheatSnapMode_NotEqual, 0, bit16);
						break;
					case 3:
						mpEngine->Snapshot(kATCheatSnapMode_Less, 0, bit16);
						break;
					case 4:
						mpEngine->Snapshot(kATCheatSnapMode_LessEqual, 0, bit16);
						break;
					case 5:
						mpEngine->Snapshot(kATCheatSnapMode_Greater, 0, bit16);
						break;
					case 6:
						mpEngine->Snapshot(kATCheatSnapMode_GreaterEqual, 0, bit16);
						break;
					case 7:
						{
							mbValidationFailed = false;
							sint32 v = GetControlValueSint32(IDC_VALUE);

							if (bit16) {
								if (v < -32768 || v > 65535)
									mbValidationFailed = true;
							} else {
								if (v < -128 || v > 255)
									mbValidationFailed = true;
							}

							if (mbValidationFailed) {
								if (bit16)
									ShowError(L"Invalid search value. The search value must be within -32768 to 65535.", L"Altirra Error");
								else
									ShowError(L"Invalid search value. The search value must be within -128 to 255.", L"Altirra Error");

								return true;
							}

							mpEngine->Snapshot(kATCheatSnapMode_EqualRef, v, bit16);
						}
						break;
				}

				UpdateResultView();
			}
			return true;

		case IDC_LOAD:
			{
				const VDStringW& fn = VDGetLoadFileName('CHET'
					, (VDGUIHandle)mhdlg
					, L"Load cheat file"
					, g_ATUIFileFilter_Cheats
					, NULL);

				if (!fn.empty()) {
					try {
						mpEngine->Load(fn.c_str());
					} catch(const MyError& e) {
						e.post((VDZHWND)mhdlg, "Altirra Error");
					}
					UpdateResultView();
					UpdateActiveView();
				}
			}
			return true;

		case IDC_SAVE:
			{
				const VDStringW& fn = VDGetSaveFileName('CHET', (VDGUIHandle)mhdlg, L"Save cheat file", L"Altirra cheat set\0*.atcheats\0", L"atcheats");

				if (!fn.empty()) {
					try {
						mpEngine->Save(fn.c_str());
					} catch(const MyError& e) {
						e.post((VDZHWND)mhdlg, "Altirra Error");
					}
				}
			}
			return true;

		case IDC_ADD:
			{
				ATCheatEngine::Cheat cheat = {};
				ATUIDialogEditCheat dlg(cheat);

				if (dlg.ShowDialog((VDGUIHandle)mhdlg)) {
					cheat.mbEnabled = true;
					mpEngine->AddCheat(cheat);
					UpdateActiveView();
				}
			}
			return true;

		case IDC_EDIT:
			{
				int selIdx = mActiveView.GetSelectedIndex();

				if (selIdx >= 0)
					OnCheatDblClk(&mActiveView, selIdx);
			}
			return true;

		case IDC_DELETE:
			{
				int selIdx = mActiveView.GetSelectedIndex();
				AItem *selItem = static_cast<AItem *>(mActiveView.GetSelectedItem());

				if (selIdx >= 0 && selItem) {
					uint32 cheatIndex = selItem->mIndex;

					mActiveView.Clear();
					mpEngine->RemoveCheatByIndex(cheatIndex);
					UpdateActiveView();
					mActiveView.SetSelectedIndex(selIdx);
				}
			}
			return true;

		case IDC_TRANSFER:
			{
				int selIdx = mResultView.GetSelectedIndex();

				if (selIdx >= 0)
					OnResultDblClk(&mResultView, selIdx);
			}
			return true;

		case IDC_TRANSFERALL:
			{
				int n = mResultView.GetItemCount();

				for(int i=0; i<n; ++i) {
					VItem *vi = static_cast<VItem *>(mResultView.GetVirtualItem(i));

					if (vi) {
						ATCheatEngine::Cheat cheat;
						cheat.mAddress = vi->mAddress;
						cheat.mValue = vi->mValue;
						cheat.mb16Bit = vi->mb16Bit;
						cheat.mbEnabled = true;
						mpEngine->AddCheat(cheat);
					}
				}

				UpdateActiveView();
			}
			return true;
	}

	return false;
}

void ATUIDialogCheater::OnResultDblClk(VDUIProxyListView *sender, int index) {
	VItem *vi = static_cast<VItem *>(sender->GetVirtualItem(index));

	if (vi) {
		ATCheatEngine::Cheat cheat;
		cheat.mAddress = vi->mAddress;
		cheat.mValue = vi->mValue;
		cheat.mb16Bit = vi->mb16Bit;
		cheat.mbEnabled = true;
		mpEngine->AddCheat(cheat);
		UpdateActiveView();
	}
}

void ATUIDialogCheater::OnResultSelChanged(VDUIProxyListView *sender, int index) {
	EnableControl(IDC_TRANSFER, index >= 0);
}

void ATUIDialogCheater::OnCheatDblClk(VDUIProxyListView *sender, int index) {
	AItem *ai = static_cast<AItem *>(sender->GetVirtualItem(index));

	if (ai) {
		ATCheatEngine::Cheat cheat(mpEngine->GetCheatByIndex(ai->mIndex));

		ATUIDialogEditCheat dlg(cheat);

		if (dlg.ShowDialog((VDGUIHandle)mhdlg)) {
			mpEngine->UpdateCheat(ai->mIndex, cheat);
			mActiveView.RefreshItem(ai->mIndex);

			AItemSorter sorter;
			mActiveView.Sort(sorter);
		}
	}
}

void ATUIDialogCheater::OnCheatCheckedChanged(VDUIProxyListView *sender, int index) {
	AItem *ai = static_cast<AItem *>(sender->GetVirtualItem(index));

	if (ai) {
		ATCheatEngine::Cheat cheat(mpEngine->GetCheatByIndex(ai->mIndex));
		cheat.mbEnabled = sender->IsItemChecked(index);
		mpEngine->UpdateCheat(ai->mIndex, cheat);
	}
}

void ATUIDialogCheater::OnModeChanged(VDUIProxyComboBoxControl *sender, int index) {
	UpdateValueEnable();
}

void ATUIDialogCheater::UpdateResultView() {
	mResultView.Clear();

	enum { kMaxIds = 250 };

	uint32 ids[kMaxIds];
	uint32 n = mpEngine->GetValidOffsets(ids, kMaxIds);

	if (!n) {
		EnableControl(IDC_RESULTS, false);
		mResultView.InsertItem(0, L"No results left. Try again.");
	} else if (n >= 250) {
		EnableControl(IDC_RESULTS, false);
		mResultView.InsertItem(0, VDStringW().sprintf(L"Too many results (%u).", n).c_str());
	} else {
		EnableControl(IDC_RESULTS, true);

		const bool bit16 = IsButtonChecked(IDC_VT_16BIT);
		VDStringW s;
		for(uint32 i=0; i<n; ++i) {
			vdrefptr<VItem> item(new VItem(ids[i], mpEngine->GetOffsetCurrentValue(ids[i], bit16), bit16));
			mResultView.InsertVirtualItem(i, item);
		}
	}

	mResultView.AutoSizeColumns();

	EnableControl(IDC_TRANSFERALL, n > 0);
}

void ATUIDialogCheater::UpdateActiveView() {
	mActiveView.Clear();

	uint32 n = mpEngine->GetCheatCount();

	VDStringW s;
	for(uint32 i=0; i<n; ++i) {
		vdrefptr<AItem> item(new AItem(mpEngine, i));
		mActiveView.InsertVirtualItem(i, item);
		mActiveView.SetItemChecked(i, mpEngine->GetCheatByIndex(i).mbEnabled);
	}

	AItemSorter sorter;
	mActiveView.Sort(sorter);

	mActiveView.AutoSizeColumns();
}

void ATUIDialogCheater::UpdateValueEnable() {
	EnableControl(IDC_VALUE, mModeView.GetSelection() == 7);
}

/////////////////////////////////////////////////////////////////////////////

void ATUIShowDialogCheater(VDGUIHandle hParent, ATCheatEngine *engine) {
	ATUIDialogCheater dlg(engine);

	dlg.ShowDialog(hParent);
}
