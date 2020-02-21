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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdalloc.h>
#include <vd2/Dita/services.h>
#include <at/atio/blobimage.h>
#include <at/atio/diskimage.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/messageloop.h>
#include <at/atnativeui/uiproxies.h>
#include "simulator.h"
#include "resource.h"
#include "uifilefilters.h"
#include "uicompat.h"
#include "cartridge.h"
#include "compatedb.h"
#include "compatengine.h"
#include "hleprogramloader.h"
#include "disk.h"

extern ATSimulator g_sim;

///////////////////////////////////////////////////////////////////////////

namespace {
	VDStringW GetTagDisplayName(const char *s) {
		ATCompatKnownTag knownTag = ATCompatGetKnownTagByKey(s);

		if (knownTag)
			return VDStringW(ATUICompatGetKnownTagDisplayName(knownTag));

		VDStringW name;
		name.sprintf(L"[%hs]", s);
		return name;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogCompatDBSelectTag final : public VDDialogFrameW32 {
public:
	ATUIDialogCompatDBSelectTag();
	~ATUIDialogCompatDBSelectTag();

	void SetSelectedTag(ATCompatKnownTag tag) { mSelectedTag = tag; }
	ATCompatKnownTag GetSelectedTag() const { return mSelectedTag; }

	bool OnLoaded() override;
	void OnDataExchange(bool write);

private:
	VDUIProxyListBoxControl mList;

	ATCompatKnownTag mSelectedTag = kATCompatKnownTag_None;

	ATCompatKnownTag mSortedTags[kATCompatKnownTagCount - 1];
};

ATUIDialogCompatDBSelectTag::ATUIDialogCompatDBSelectTag()
	: VDDialogFrameW32(IDD_COMPATDB_ADDTAG)
{
}

ATUIDialogCompatDBSelectTag::~ATUIDialogCompatDBSelectTag() {
}

bool ATUIDialogCompatDBSelectTag::OnLoaded() {
	AddProxy(&mList, IDC_LIST);

	const wchar_t *tagNames[kATCompatKnownTagCount - 1];

	for(int i = 0; i < kATCompatKnownTagCount-1; ++i) {
		mSortedTags[i] = (ATCompatKnownTag)(i + 1);
		tagNames[i] = ATUICompatGetKnownTagDisplayName((ATCompatKnownTag)(i + 1));
	}

	std::sort(std::begin(mSortedTags), std::end(mSortedTags),
		[&](ATCompatKnownTag a, ATCompatKnownTag b) {
			return vdwcsicmp(tagNames[(int)a-1], tagNames[(int)b-1]) < 0;
		}
	);

	for(const auto tag : mSortedTags) {
		mList.AddItem(tagNames[(int)tag - 1]);
	}

	if (mSelectedTag) {
		auto it = std::find(std::begin(mSortedTags), std::end(mSortedTags), mSelectedTag);

		if (it != std::end(mSortedTags)) {
			const int index = (int)(it - std::begin(mSortedTags));

			mList.SetSelection(index);
			mList.EnsureItemVisible(index);
		}
	}

	SetFocusToControl(IDC_LIST);
	return false;
}

void ATUIDialogCompatDBSelectTag::OnDataExchange(bool write) {
	if (write) {
		int sel = mList.GetSelection();

		if ((unsigned)sel < vdcountof(mSortedTags))
			mSelectedTag = mSortedTags[sel];
		else 
			mSelectedTag = kATCompatKnownTag_None;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogCompatDBEditAlias final : public VDDialogFrameW32 {
public:
	ATUIDialogCompatDBEditAlias(ATCompatEDBAlias& alias, const ATCompatEDBSourcedAliasRule *aliasRules, size_t numAliasRules);
	~ATUIDialogCompatDBEditAlias();

	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

private:
	void OnAdd();
	void OnRemove();
	void OnFromFile();
	
	void RefreshSrcRules();

	VDUIProxyListBoxControl mAvailableList;
	VDUIProxyListBoxControl mActiveList;
	VDUIProxyButtonControl mAddButton;
	VDUIProxyButtonControl mRemoveButton;
	VDUIProxyButtonControl mFromFileButton;

	ATCompatEDBAlias& mAlias;
	vdfastvector<ATCompatEDBAliasRule> mRules;
	const ATCompatEDBSourcedAliasRule *mpAvailRules;
	const size_t mNumAvailRules;

	vdvector<ATCompatEDBSourcedAliasRule> mCurrentSrcRules;
};

ATUIDialogCompatDBEditAlias::ATUIDialogCompatDBEditAlias(ATCompatEDBAlias& alias, const ATCompatEDBSourcedAliasRule *aliasRules, size_t numAliasRules)
	: VDDialogFrameW32(IDD_COMPATDB_EDITALIAS)
	, mAlias(alias)
	, mpAvailRules(aliasRules)
	, mNumAvailRules(numAliasRules)
	, mCurrentSrcRules(aliasRules, aliasRules + numAliasRules)
{
	mAddButton.SetOnClicked([this] { OnAdd(); });
	mRemoveButton.SetOnClicked([this] { OnRemove(); });
	mFromFileButton.SetOnClicked([this] { OnFromFile(); });
}

ATUIDialogCompatDBEditAlias::~ATUIDialogCompatDBEditAlias() {
}

bool ATUIDialogCompatDBEditAlias::OnLoaded() {
	AddProxy(&mAvailableList, IDC_LIST_AVAILABLE);
	AddProxy(&mActiveList, IDC_LIST_ACTIVE);
	AddProxy(&mAddButton, IDC_ADD);
	AddProxy(&mRemoveButton, IDC_REMOVE);
	AddProxy(&mFromFileButton, IDC_FROM_FILE);

	RefreshSrcRules();

	OnDataExchange(false);

	SetFocusToControl(IDC_LIST_AVAILABLE);
	mAvailableList.SetSelection(0);
	return true;
}

void ATUIDialogCompatDBEditAlias::OnDataExchange(bool write) {
	if (write) {
		mAlias.mRules = mRules;
	} else {
		mRules = mAlias.mRules;
		mActiveList.Clear();
		for(const auto& rule : mRules) {
			mActiveList.AddItem(rule.ToDisplayString().c_str());
		}
	}
}

void ATUIDialogCompatDBEditAlias::OnAdd() {
	int sel = mAvailableList.GetSelection();

	if ((unsigned)sel >= mCurrentSrcRules.size())
		return;

	const auto& rule = mCurrentSrcRules[sel].mRule;

	auto it = std::find(mRules.begin(), mRules.end(), rule);
	if (it == mRules.end()) {
		mRules.push_back(rule);
		mActiveList.AddItem(rule.ToDisplayString().c_str());
	}
}

void ATUIDialogCompatDBEditAlias::OnRemove() {
	int sel = mActiveList.GetSelection();
	if ((unsigned)sel >= mRules.size())
		return;

	mRules.erase(mRules.begin() + sel);
	mActiveList.DeleteItem(sel);
}

void ATUIDialogCompatDBEditAlias::OnFromFile() {
	const VDStringW& fn = VDGetLoadFileName('cpif', (VDGUIHandle)mhdlg, L"Load image file for signatures", g_ATUIFileFilter_LoadCompatImageFile, nullptr);

	if (fn.empty())
		return;

	try {
		ATCartLoadContext cartLoadCtx {};
		cartLoadCtx.mbIgnoreMapper = true;

		ATImageLoadContext loadCtx {};
		loadCtx.mpCartLoadContext = &cartLoadCtx;

		VDFileStream fs(fn.c_str());
		vdrefptr<IATImage> image;
		ATImageLoadAuto(fn.c_str(), fn.c_str(), fs, &loadCtx, nullptr, nullptr, ~image);

		vdvector<ATCompatEDBSourcedAliasRule> newSrcRules;
		if (IATDiskImage *diskImage = vdpoly_cast<IATDiskImage *>(image)) {
			if (!diskImage->IsDynamic()) {
				uint64 checksum = diskImage->GetImageChecksum();

				newSrcRules.push_back(ATCompatEDBSourcedAliasRule { { kATCompatRuleType_DiskChecksum, checksum }, VDStringW(L"Disk") });
			}

		} else if (IATCartridgeImage *cartImage = vdpoly_cast<IATCartridgeImage *>(image)) {
			newSrcRules.push_back(ATCompatEDBSourcedAliasRule { { kATCompatRuleType_CartChecksum, cartImage->GetChecksum() }, VDStringW(L"Cart") });
		} else if (IATBlobImage *blobImage = vdpoly_cast<IATBlobImage *>(image)) {
			if (blobImage->GetImageType() == kATImageType_Program)
				newSrcRules.push_back(ATCompatEDBSourcedAliasRule { { kATCompatRuleType_ExeChecksum, blobImage->GetChecksum() }, VDStringW(L"Exe") });
		}
		
		if (newSrcRules.empty())
			throw MyError("'%ls' does not contain an image type supported by the compatibility database system.", fn.c_str());

		mCurrentSrcRules = std::move(newSrcRules);
		RefreshSrcRules();
	} catch(const MyError& e) {
		ShowError(e);
	}
}

void ATUIDialogCompatDBEditAlias::RefreshSrcRules() {
	mAvailableList.Clear();

	for(const auto& srcRule : mCurrentSrcRules)
		mAvailableList.AddItem(srcRule.ToDisplayString().c_str());
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogCompatDB final : public VDDialogFrameW32 {
public:
	ATUIDialogCompatDB(const vdfunction<void(vdvector<ATCompatEDBSourcedAliasRule>&)>& availRulesFn);
	~ATUIDialogCompatDB();

	static void ShowModeless(VDGUIHandle parent, const vdfunction<void(vdvector<ATCompatEDBSourcedAliasRule>&)>& availRulesFn);

	bool OnLoaded() override;
	void OnDestroy() override;
	bool OnOK() override;
	bool OnCancel() override;
	bool OnCommand(uint32 id, uint32 extra) override;
	bool PreNCDestroy() override { return true; }

private:
	void OnTitleEdited(int idx, const wchar_t *s);
	void OnTitleSelectionChanged();
	void OnAddTitle();
	void OnDeleteTitle();
	void RefreshAliases();
	void OnAddAlias();
	void OnDeleteAlias();
	void OnEditAlias();
	void RefreshTags();
	void OnAddTag();
	void OnDeleteTag();
	void OnExit();
	void OnNew();
	void OnLoad();
	void OnSave();
	void OnSaveAs();
	void OnCompile();
	void OnCompileTo();

	void Load(const wchar_t *path);
	void CompileTo(const wchar_t *path);
	bool ConfirmDiscard();
	void SetModified(bool modified = true);
	void UpdateCaption();

	ATCompatEDB mEDB;
	bool mbAddingItem = false;
	bool mbModified = false;

	ATCompatKnownTag mLastSelectedTag = kATCompatKnownTag_None;

	const vdfunction<void(vdvector<ATCompatEDBSourcedAliasRule>&)> mpAvailRulesFn;

	vdfastvector<ATCompatEDBTitle *> mDisplayedTitles;
	ATCompatEDBTitle *mpTitleForAliases = nullptr;
	vdfastvector<uint32> mDisplayedAliasIndices;
	vdfastvector<ATCompatEDBTag *> mDisplayedTags;

	VDStringW mPath;
	VDStringW mCompilePath;
	VDStringW mBaseCaption;

	VDUIProxyListBoxControl mListBoxTitle;
	VDUIProxyListBoxControl mListBoxAlias;
	VDUIProxyListBoxControl mListBoxTag;
	VDUIProxyButtonControl mButtonAddTitle;
	VDUIProxyButtonControl mButtonDeleteTitle;
	VDUIProxyButtonControl mButtonAddAlias;
	VDUIProxyButtonControl mButtonDeleteAlias;
	VDUIProxyButtonControl mButtonEditAlias;
	VDUIProxyButtonControl mButtonAddTag;
	VDUIProxyButtonControl mButtonDeleteTag;

	static ATUIDialogCompatDB *spCurrent;
};

ATUIDialogCompatDB *ATUIDialogCompatDB::spCurrent;

ATUIDialogCompatDB::ATUIDialogCompatDB(const vdfunction<void(vdvector<ATCompatEDBSourcedAliasRule>&)>& availRulesFn)
	: VDDialogFrameW32(IDD_COMPATDB_EDITOR)
	, mpAvailRulesFn(availRulesFn)
{
	mListBoxTitle.SetOnSelectionChanged([this](int) { OnTitleSelectionChanged(); });
	mListBoxTitle.SetOnItemEdited([this](int idx, const wchar_t *s) { OnTitleEdited(idx, s); });
	mButtonAddTitle.SetOnClicked([this] { OnAddTitle(); });
	mButtonDeleteTitle.SetOnClicked([this] { OnDeleteTitle(); });
	mButtonAddAlias.SetOnClicked([this] { OnAddAlias(); });
	mButtonDeleteAlias.SetOnClicked([this] { OnDeleteAlias(); });
	mButtonEditAlias.SetOnClicked([this] { OnEditAlias(); });
	mButtonAddTag.SetOnClicked([this] { OnAddTag(); });
	mButtonDeleteTag.SetOnClicked([this] { OnDeleteTag(); });

	spCurrent = this;
}

ATUIDialogCompatDB::~ATUIDialogCompatDB() {
	spCurrent = nullptr;
}

void ATUIDialogCompatDB::ShowModeless(VDGUIHandle parent, const vdfunction<void(vdvector<ATCompatEDBSourcedAliasRule>&)>& availRulesFn) {
	if (spCurrent) {
		spCurrent->Show();
		return;
	}

	spCurrent = new ATUIDialogCompatDB(availRulesFn);

	if (spCurrent->Create(parent)) {
		spCurrent->Show();
	} else {
		delete spCurrent;
		spCurrent = nullptr;
	}
}

bool ATUIDialogCompatDB::OnLoaded() {
	ATUIUnregisterModelessDialog(mhdlg);

	mBaseCaption = GetCaption();

	AddProxy(&mListBoxTitle, IDC_TITLE_LIST);
	mListBoxTitle.EnableAutoItemEditing();

	AddProxy(&mListBoxAlias, IDC_ALIAS_LIST);
	AddProxy(&mListBoxTag, IDC_TAG_LIST);
	AddProxy(&mButtonAddTitle, IDC_ADDTITLE);
	AddProxy(&mButtonDeleteTitle, IDC_DELETETITLE);
	AddProxy(&mButtonAddAlias, IDC_ADDALIAS);
	AddProxy(&mButtonDeleteAlias, IDC_DELETEALIAS);
	AddProxy(&mButtonEditAlias, IDC_EDITALIAS);
	AddProxy(&mButtonAddTag, IDC_ADDTAG);
	AddProxy(&mButtonDeleteTag, IDC_DELETETAG);

	LoadAcceleratorTable(IDR_COMPATDBEDITOR_ACCEL);

	SetFocusToControl(IDC_TITLE_LIST);
	UpdateCaption();
	return true;
}

void ATUIDialogCompatDB::OnDestroy() {
	ATUIUnregisterModelessDialog(mhdlg);
}

bool ATUIDialogCompatDB::OnOK() {
	if (!ConfirmDiscard())
		return true;

	End(0);
	return true;
}

bool ATUIDialogCompatDB::OnCancel() {
	return OnOK();
}

bool ATUIDialogCompatDB::OnCommand(uint32 id, uint32 extra) {
	switch(id) {
		case ID_FILE_NEW:
			OnNew();
			return true;

		case ID_FILE_LOAD:
			OnLoad();
			return true;

		case ID_FILE_SAVE:
			OnSave();
			return true;

		case ID_FILE_SAVEAS:
			OnSaveAs();
			return true;

		case ID_FILE_EXIT:
			OnExit();
			return true;

		case ID_BUILD_COMPILE:
			OnCompile();
			return true;

		case ID_BUILD_COMPILETO:
			OnCompileTo();
			return true;
	}

	return false;
}

void ATUIDialogCompatDB::OnTitleEdited(int idx, const wchar_t *s) {
	if ((unsigned)idx >= mDisplayedTitles.size())
		return;

	// check if we were adding an item; if so, canceling the item should destroy it
	auto *title = mDisplayedTitles[idx];

	if (mbAddingItem) {
		mbAddingItem = false;

		if (!s) {
			mDisplayedTitles.erase(mDisplayedTitles.begin() + idx);
			mListBoxTitle.DeleteItem(idx);
			mEDB.mTitleTable.Destroy(title->mId);
			return;
		}
	}

	// check if we simply canceled an edit
	if (!s)
		return;

	// check if the title actually changed; must use case sensitive here
	if (title->mName == s)
		return;

	SetModified();

	// remove the item
	mDisplayedTitles.erase(mDisplayedTitles.begin() + idx);
	mListBoxTitle.DeleteItem(idx);

	// change name
	bool caseChangeOnly = vdwcsicmp(title->mName.c_str(), s) == 0;

	title->mName = s;

	if (!caseChangeOnly) {
		// determine new insertion position
		size_t n = mDisplayedTitles.size();
		size_t pos = 0;
		while(n > 0) {
			size_t step = n >> 1;

			if (vdwcsicmp(mDisplayedTitles[pos + step]->mName.c_str(), s) < 0) {
				pos += step + 1;
				n -= step + 1;
			} else {
				n = step;
			}
		}

		idx = (int)pos;
	}

	// reinsert item
	mDisplayedTitles.insert(mDisplayedTitles.begin() + idx, title);
	mListBoxTitle.InsertItem(idx, s);

	mListBoxTitle.SetSelection(idx);
	mListBoxTitle.EnsureItemVisible(idx);
}

void ATUIDialogCompatDB::OnTitleSelectionChanged() {
	RefreshAliases();
	RefreshTags();
}

void ATUIDialogCompatDB::OnAddTitle() {
	const wchar_t *newTitle = L"(New Title)";

	// If we have a selection, insert it right after the selection; otherwise, add at end.
	int insertPos = mListBoxTitle.GetSelection();

	if (insertPos >= 0) {
		++insertPos;
	} else {
		insertPos = (int)mDisplayedTitles.size();
	}

	auto *newTitleEntry = mEDB.mTitleTable.Create();

	int idx = mListBoxTitle.InsertItem(insertPos, L"(New Title)");
	if (idx >= 0) {
		mDisplayedTitles.insert(mDisplayedTitles.begin() + insertPos, newTitleEntry);

		mListBoxTitle.SetSelection(idx);
		RefreshAliases();
		RefreshTags();

		mbAddingItem = true;
		mListBoxTitle.EditItem(idx);
	} else {
		mEDB.mTitleTable.Destroy(newTitleEntry->mId);
	}
}

void ATUIDialogCompatDB::OnDeleteTitle() {
	int sel = mListBoxTitle.GetSelection();

	if ((unsigned)sel >= mDisplayedTitles.size())
		return;

	SetModified();

	mListBoxAlias.Clear();
	mDisplayedAliasIndices.clear();

	mListBoxTitle.DeleteItem(sel);
	mDisplayedTitles.erase(mDisplayedTitles.begin() + sel);

	mListBoxTitle.SetSelection(sel);
}

void ATUIDialogCompatDB::RefreshAliases() {
	mpTitleForAliases = nullptr;
	mDisplayedAliasIndices.clear();
	mListBoxAlias.Clear();

	int idx = mListBoxTitle.GetSelection();

	if ((unsigned)idx >= mDisplayedTitles.size())
		return;

	auto *titleEntry = mDisplayedTitles[idx];

	mpTitleForAliases = titleEntry;

	uint32 aliasIdx = 0;
	for(const auto& alias : titleEntry->mAliases) {
		mDisplayedAliasIndices.push_back(aliasIdx++);
	}

	VDStringW s;
	for(uint32 aliasIdx : mDisplayedAliasIndices) {
		auto *p = &mpTitleForAliases->mAliases[aliasIdx];
		s.clear();

		for(const auto& rule : p->mRules) {
			if (!s.empty())
				s += L",";

			s += rule.ToDisplayString();
		}

		mListBoxAlias.AddItem(s.c_str());
	}
}

void ATUIDialogCompatDB::OnAddAlias() {
	int idx = mListBoxTitle.GetSelection();

	if ((unsigned)idx >= mDisplayedTitles.size())
		return;

	vdvector<ATCompatEDBSourcedAliasRule> aliasRules;
	mpAvailRulesFn(aliasRules);

	ATCompatEDBAlias alias;
	ATUIDialogCompatDBEditAlias dlg(alias, aliasRules.data(), aliasRules.size());

	if (dlg.ShowDialog(this)) {
		SetModified();

		mDisplayedTitles[idx]->mAliases.push_back_as(std::move(alias));

		RefreshAliases();
	}
}

void ATUIDialogCompatDB::OnDeleteAlias() {
	int sel = mListBoxAlias.GetSelection();

	if ((unsigned)sel >= mDisplayedAliasIndices.size() || !mpTitleForAliases)
		return;

	SetModified();

	const uint32 aliasIndex = mDisplayedAliasIndices[sel];

	int idx = mListBoxTitle.GetSelection();

	mpTitleForAliases->mAliases.erase(mpTitleForAliases->mAliases.begin() + aliasIndex);

	mDisplayedAliasIndices.erase(mDisplayedAliasIndices.begin() + sel);

	for(uint32& aliasIndex : mDisplayedAliasIndices) {
		if (aliasIndex > (uint32)sel)
			--aliasIndex;
	}

	mListBoxAlias.DeleteItem(sel);
	mListBoxAlias.SetSelection(sel);
}

void ATUIDialogCompatDB::OnEditAlias() {
	int sel = mListBoxAlias.GetSelection();

	if ((unsigned)sel >= mDisplayedAliasIndices.size() || !mpTitleForAliases)
		return;

	auto& alias = mpTitleForAliases->mAliases[mDisplayedAliasIndices[sel]];

	vdvector<ATCompatEDBSourcedAliasRule> aliasRules;
	mpAvailRulesFn(aliasRules);

	ATUIDialogCompatDBEditAlias dlg(alias, aliasRules.data(), aliasRules.size());
	if (dlg.ShowDialog(this)) {
		SetModified();
		RefreshAliases();
	}
}

void ATUIDialogCompatDB::RefreshTags() {
	mDisplayedTags.clear();
	mListBoxTag.Clear();

	int idx = mListBoxTitle.GetSelection();

	if ((unsigned)idx >= mDisplayedTitles.size())
		return;

	auto *titleEntry = mDisplayedTitles[idx];

	for(const VDStringA& key : titleEntry->mTags) {
		auto it = mEDB.mTagTable.find(key);

		if (it != mEDB.mTagTable.end())
			mDisplayedTags.push_back(&it->second);
	}

	for(auto *p : mDisplayedTags)
		mListBoxTag.AddItem(p->mDisplayName.c_str());
}

void ATUIDialogCompatDB::OnAddTag() {
	int sel = mListBoxTitle.GetSelection();

	if ((unsigned)sel >= mDisplayedTitles.size())
		return;

	auto *title = mDisplayedTitles[sel];

	ATUIDialogCompatDBSelectTag dlg;

	dlg.SetSelectedTag(mLastSelectedTag);

	if (dlg.ShowDialog(this)) {
		ATCompatKnownTag knownTag = dlg.GetSelectedTag();

		if (knownTag) {
			mLastSelectedTag = knownTag;

			const char *s = ATCompatGetKeyForKnownTag(knownTag);
			auto r = mEDB.mTagTable.insert_as(s);

			if (r.second) {
				auto& tag = r.first->second;

				tag.mKey = s;
				tag.mDisplayName = GetTagDisplayName(s);
			}

			if (title->mTags.end() == std::find(title->mTags.begin(), title->mTags.end(), s)) {
				title->mTags.push_back_as(s);

				SetModified();

				RefreshTags();
			}
		}
	}
}

void ATUIDialogCompatDB::OnDeleteTag() {
	int tagSel = mListBoxTag.GetSelection();
	if ((unsigned)tagSel >= mDisplayedTags.size())
		return;
	
	SetModified();

	const VDStringA key = mDisplayedTags[tagSel]->mKey;
	mDisplayedTags.erase(mDisplayedTags.begin() + tagSel);
	mListBoxTag.DeleteItem(tagSel);

	int titleSel = mListBoxTitle.GetSelection();
	if ((unsigned)titleSel < mDisplayedTitles.size()) {
		auto *title = mDisplayedTitles[titleSel];
		auto it = std::find(title->mTags.begin(), title->mTags.end(), key);
		if (it != title->mTags.end())
			title->mTags.erase(it);
	}
}

void ATUIDialogCompatDB::OnExit() {
	if (!ConfirmDiscard())
		return;

	End(0);
}

void ATUIDialogCompatDB::OnNew() {
	if (!ConfirmDiscard())
		return;

	mPath.clear();
	mCompilePath.clear();
	mbModified = false;
	UpdateCaption();

	mListBoxAlias.Clear();
	mListBoxTag.Clear();
	mListBoxTitle.Clear();

	mpTitleForAliases = nullptr;
	mDisplayedAliasIndices.clear();
	mDisplayedTags.clear();
	mDisplayedTitles.clear();

	mEDB = {};
}

void ATUIDialogCompatDB::OnLoad() {
	if (!ConfirmDiscard())
		return;

	const auto& path = VDGetLoadFileName('cpdb', (VDGUIHandle)mhdlg, L"Load compatibility database", L"Altirra CompatDB (*.atcompatdb)\0*.atcompatdb\0", L"atcompatdb");
	
	if (!path.empty()) {
		try {
			Load(path.c_str());
		} catch(const MyError& e) {
			ShowError(e);
		}
	}
}

void ATUIDialogCompatDB::OnSave() {
	if (mPath.empty())
		OnSaveAs();
	else {
		try {
			ATSaveCompatEDB(mPath.c_str(), mEDB);

			mbModified = false;
			UpdateCaption();
		} catch(const MyError& e) {
			ShowError(e);
		}
	}
}

void ATUIDialogCompatDB::OnSaveAs() {
	const auto& path = VDGetSaveFileName('cpdb', (VDGUIHandle)mhdlg, L"Save compatibility database", L"Altirra CompatDB (*.atcompatdb)\0*.atcompatdb\0", L"atcompatdb");
	
	if (!path.empty()) {
		try {
			ATSaveCompatEDB(path.c_str(), mEDB);

			mPath = path;
			mbModified = false;
			UpdateCaption();
		} catch(const MyError& e) {
			ShowError(e);
		}
	}
}

void ATUIDialogCompatDB::OnCompile() {
	if (mbModified) {
		OnSave();

		if (mbModified)
			return;
	}

	if (mCompilePath.empty()) {
		OnCompileTo();
		return;
	}

	CompileTo(VDStringW(mCompilePath).c_str());
}

void ATUIDialogCompatDB::OnCompileTo() {
	if (mbModified) {
		OnSave();

		if (mbModified)
			return;
	}

	const auto& path = VDGetSaveFileName('cpdc', (VDGUIHandle)mhdlg, L"Compile compatibility database", g_ATUIFileFilter_SaveCompatEngine, L"atcpengine");
	
	if (!path.empty()) {
		try {
			CompileTo(path.c_str());
		} catch(const MyError& e) {
			ShowError(e);
		}
	}
}

void ATUIDialogCompatDB::Load(const wchar_t *path) {
	ATCompatEDB tempEDB;
	ATLoadCompatEDB(path, tempEDB);

	mPath = path;
	mCompilePath.clear();
	mbModified = false;
	UpdateCaption();

	mListBoxAlias.Clear();
	mListBoxTag.Clear();
	mListBoxTitle.Clear();

	mpTitleForAliases = nullptr;
	mDisplayedAliasIndices.clear();
	mDisplayedTags.clear();
	mDisplayedTitles.clear();

	mEDB = std::move(tempEDB);

	mDisplayedTitles.reserve(mEDB.mTitleTable.Size());
	for(auto *title : mEDB.mTitleTable) {
		mDisplayedTitles.push_back(title);
	}

	std::sort(mDisplayedTitles.begin(), mDisplayedTitles.end(),
		[](const ATCompatEDBTitle *x, const ATCompatEDBTitle *y) {
			return x->mName.comparei(y->mName) < 0;
		}
	);

	mListBoxTitle.SetRedraw(false);
	for(auto *title : mDisplayedTitles) {
		mListBoxTitle.AddItem(title->mName.c_str());
	}
	mListBoxTitle.SetRedraw(true);
}

void ATUIDialogCompatDB::CompileTo(const wchar_t *path) {
	vdblock<char> buf;
	ATCompileCompatEDB(buf, mEDB);

	VDFile f(path, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
	f.write(buf.data(), (long)buf.size());
	f.close();

	mCompilePath = path;

	ATCompatReloadExtDatabase();
}

bool ATUIDialogCompatDB::ConfirmDiscard() {
	if (!mbModified)
		return true;

	return Confirm(L"Some changes have not been saved and will be discarded. Are you sure?");
}

void ATUIDialogCompatDB::SetModified(bool modified) {
	if (mbModified == modified)
		return;

	mbModified = modified;

	UpdateCaption();
}

void ATUIDialogCompatDB::UpdateCaption() {
	VDStringW s;

	if (mbModified)
		s = L"*";

	if (mPath.empty())
		s += L"New file";
	else
		s += VDFileSplitPathRightSpan(mPath);

	s += L" - ";

	s += mBaseCaption;

	SetCaption(s.c_str());
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogCompatDB(VDGUIHandle hParent) {
	auto getAvailRules = [](vdvector<ATCompatEDBSourcedAliasRule>& availRules) {
		for(int i=0; i<15; ++i) {
			const auto& diskIf = g_sim.GetDiskInterface(i);
			const auto *pImage = diskIf.GetDiskImage();

			if (pImage && !pImage->IsDynamic()) {
				uint64 checksum = pImage->GetImageChecksum();

				availRules.push_back(ATCompatEDBSourcedAliasRule { { kATCompatRuleType_DiskChecksum, checksum }, VDStringW().sprintf(L"D%u:", i + 1) });
			}
		}

		for(int i=0; i<2; ++i) {
			auto *cart = g_sim.GetCartridge(i);

			if (cart) {
				availRules.push_back(ATCompatEDBSourcedAliasRule { { kATCompatRuleType_CartChecksum, cart->GetChecksum() }, VDStringW(L"Cart") });
			}
		}

		auto *pl = g_sim.GetProgramLoader();
		if (pl) {
			auto *image = pl->GetCurrentImage();

			if (image)
				availRules.push_back(ATCompatEDBSourcedAliasRule { { kATCompatRuleType_ExeChecksum, image->GetChecksum() }, VDStringW(L"Exe") });
		}
	};

	ATUIDialogCompatDB::ShowModeless(hParent, getAvailRules);
}
