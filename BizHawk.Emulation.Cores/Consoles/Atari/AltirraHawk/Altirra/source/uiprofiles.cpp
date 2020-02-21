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
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include "resource.h"
#include "settings.h"

namespace {
	const wchar_t *const kCategoryNames[]={
		L"Hardware",
		L"Firmware",
		L"Acceleration",
		L"Debugging",
		L"Devices",
		L"Startup config.",
		L"Environment",
		L"Color",
		L"View",
		L"InputMaps",
		L"Input",
		L"Speed",
		L"Mounted images",
		L"Full screen",
		L"Audio",
		L"Boot",
	};

	const wchar_t *const kCategoryDescs[]={
		L"Includes computer type, memory configuration, CPU type and speed, and video standard (NTSC/PAL).",
		L"Includes selection of operating system and BASIC ROMs.",
		L"Includes acceleration settings for fast boot and SIO patches.",
		L"Includes settings for memory randomization and automatic breaking into the debugger.",
		L"Includes all devices configured in the device tree.",
		L"Includes BASIC setting.",
		L"Includes the Pause when Inactive setting.",
		L"Includes NTSC/PAL color palettes and artifacting color settings.",
		L"Includes video stretching/filtering settings, XEP-80 external view settings, and artifacting modes.",
		L"Includes all input maps used for controller mapping.",
		L"Includes the active input maps, which input maps are enabled for quick cycling, keyboard settings, and light pen settings.",
		L"Includes warp speed and speed control settings.",
		L"Includes attached disk and cartridge images.",
		L"Includes the windowed / full screen state.",
		L"Includes audio settings.",
		L"Includes settings related to the Boot Image command.",
	};
		
	static_assert(vdcountof(kCategoryNames) == vdcountof(kCategoryDescs), "Category arrays out of sync");
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogEditProfileCategories final : public VDDialogFrameW32 {
public:
	ATUIDialogEditProfileCategories();

	uint32 GetCategoryMask() const { return mCategoryMask; }
	void SetCategoryMask(uint32 mask) { mCategoryMask = mask; }

private:
	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

	void OnItemSelChanged(VDUIProxyListView *, int index);
	void OnCheckedChanged(VDUIProxyListView *, int);

	uint32 mCategoryMask;
	uint32 mUpdateHandlerInhibit = 0;

	VDUIProxyListView mCategoryList;
	VDUIProxyRichEditControl mHelpTextControl;

	VDDelegate mDelItemSelChanged;
	VDDelegate mDelCheckedChanged;
};

ATUIDialogEditProfileCategories::ATUIDialogEditProfileCategories()
	: VDDialogFrameW32(IDD_PROFILE_CATEGORIES)
{
	mCategoryList.OnItemSelectionChanged() += mDelItemSelChanged.Bind(this, &ATUIDialogEditProfileCategories::OnItemSelChanged);
	mCategoryList.OnItemCheckedChanged() += mDelCheckedChanged.Bind(this, &ATUIDialogEditProfileCategories::OnCheckedChanged);
}

bool ATUIDialogEditProfileCategories::OnLoaded() {
	AddProxy(&mCategoryList, IDC_CATEGORIES);
	AddProxy(&mHelpTextControl, IDC_HELP_TEXT);

	mCategoryList.SetRedraw(false);
	mCategoryList.SetItemCheckboxesEnabled(true);
	mCategoryList.SetFullRowSelectEnabled(true);
	mCategoryList.InsertColumn(0, L"", 50);

	mCategoryList.InsertItem(-1, L"(All)");
	for(const wchar_t *label : kCategoryNames)
		mCategoryList.InsertItem(-1, label);

	mCategoryList.AutoSizeColumns();
	mCategoryList.SetRedraw(true);

	mHelpTextControl.SetReadOnlyBackground();

	OnItemSelChanged(nullptr, -1);

	OnDataExchange(false);

	SetFocusToControl(IDC_CATEGORIES);
	return true;
}

void ATUIDialogEditProfileCategories::OnDataExchange(bool write) {
	if (write) {
		uint32 mask = 0;
		for(int i=0; i<(int)vdcountof(kCategoryNames); ++i) {
			if (mCategoryList.IsItemChecked(i+1))
				mask |= (1 << i);
		}

		if (mask == ((1 << vdcountof(kCategoryNames)) - 1))
			mask = kATSettingsCategory_All;

		mCategoryMask = mask;
	} else {
		++mUpdateHandlerInhibit;
	
		mCategoryList.SetItemChecked(0, mCategoryMask == kATSettingsCategory_All);

		for(int i=0; i<(int)vdcountof(kCategoryNames); ++i)
			mCategoryList.SetItemChecked(i+1, (mCategoryMask & (1 << i)) != 0);

		--mUpdateHandlerInhibit;
	}
}

void ATUIDialogEditProfileCategories::OnItemSelChanged(VDUIProxyListView *, int index) {
	VDStringA buf;

	if (index > 0 && index <= (int)vdcountof(kCategoryNames)) {
		buf = "{\\rtf \\sa90 {\\b ";
		mHelpTextControl.AppendEscapedRTF(buf, kCategoryNames[index - 1]);
		buf += "}\\par ";
		mHelpTextControl.AppendEscapedRTF(buf, kCategoryDescs[index - 1]);
	} else {
		buf = "{\\rtf \\sa90 {";
		mHelpTextControl.AppendEscapedRTF(buf,
			L"Select categories to include locally in this profile. Unselected categories "
			L"will be inherited from the parent profile."
			);
	}
	buf += "}";

	mHelpTextControl.SetTextRTF(buf.c_str());
}

void ATUIDialogEditProfileCategories::OnCheckedChanged(VDUIProxyListView *, int idx) {
	if (mUpdateHandlerInhibit)
		return;

	++mUpdateHandlerInhibit;
	if (idx == 0) {
		const bool newAllState = mCategoryList.IsItemChecked(0);

		for(int i=0; i<(int)vdcountof(kCategoryNames); ++i)
			mCategoryList.SetItemChecked(i+1, newAllState);
	} else {
		bool allSet = true;

		for(int i=0; i<(int)vdcountof(kCategoryNames); ++i) {
			if (!mCategoryList.IsItemChecked(i+1)) {
				allSet = false;
				break;
			}
		}

		mCategoryList.SetItemChecked(0, allSet);
	}
	--mUpdateHandlerInhibit;
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogProfiles final : public VDResizableDialogFrameW32 {
public:
	ATUIDialogProfiles();

private:
	class ProfileNode;

	bool OnLoaded() override;
	void OnDataExchange(bool write) override;
	void OnMouseMove(int x, int y) override;
	void OnMouseUpL(int x, int y) override;
	void OnCaptureLost() override;

	void OnAdd();
	void OnDelete();
	void OnSetDefault();
	void OnUpdate();
	void OnSwitch();
	void OnVisibleChanged();
	void OnBeginLabelEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::BeginEditEvent *event);
	void OnEndLabelEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::EndEditEvent *event);
	void OnTreeSelectionChanged(VDUIProxyTreeViewControl *, int);
	void OnTreeGetDispAttr(VDUIProxyTreeViewControl *, VDUIProxyTreeViewControl::GetDispAttrEvent *event);
	void OnTreeDrag(const VDUIProxyTreeViewControl::BeginDragEvent& event);
	void OnCheckedChanging(VDUIProxyListView *, VDUIProxyListView::CheckedChangingEvent *event);

	uint32 mUpdateHandlerInhibit = 0;
	bool mbDragInProgress = false;
	vdrefptr<ProfileNode> mpDragSource;
	vdrefptr<ProfileNode> mpDragTarget;

	VDUIProxyTreeViewControl mProfileTree;
	VDUIProxyListView mCategoryList;
	VDUIProxyButtonControl mAddButton;
	VDUIProxyButtonControl mDeleteButton;
	VDUIProxyButtonControl mSetDefaultButton;
	VDUIProxyButtonControl mUpdateButton;
	VDUIProxyButtonControl mSwitchButton;
	VDUIProxyButtonControl mVisibleCheckBox;

	VDDelegate mDelBeginEdit;
	VDDelegate mDelEndEdit;
	VDDelegate mDelSelChanged;
	VDDelegate mDelGetDispAttr;
	VDDelegate mDelCheckedChanging;

	static const wchar_t *const kDefaultProfileNames[];
};

const wchar_t *const ATUIDialogProfiles::kDefaultProfileNames[]={
	L"800",
	L"1200XL",
	L"XL/XE",
	L"XEGS",
	L"5200",
	nullptr
};

class ATUIDialogProfiles::ProfileNode final : public vdrefcounted<IVDUITreeViewVirtualItem> {
public:
	ProfileNode(ProfileNode *parent, uint32 profileId, const wchar_t *s, ATSettingsCategory mask, bool visible)
		: mId(profileId), mName(s), mCategoryMask(mask), mpParent(parent), mbVisible(visible) {}

	void *AsInterface(uint32 id) override { return nullptr; }

	void GetText(VDStringW& s) const override {
		s = mName;

		bool foundAny = false;

		for(uint32 i=0; i<kATDefaultProfileCount; ++i) {
			if (ATGetDefaultProfileId((ATDefaultProfile)i) == mId) {
				if (!foundAny)
					s += L" (default for ";
				else
					s += L", ";

				s += kDefaultProfileNames[i];
				foundAny = true;
			}
		}

		if (foundAny)
			s += L")";
	}

	uint32 mId;
	VDStringW mName;
	uintptr mTreeNode;
	ATSettingsCategory mCategoryMask;
	vdrefptr<ProfileNode> mpParent;
	bool mbVisible;
};

ATUIDialogProfiles::ATUIDialogProfiles()
	: VDResizableDialogFrameW32(IDD_PROFILES)
{
	static_assert(vdcountof(kDefaultProfileNames) - 1 == kATDefaultProfileCount, "Default profile table is out of sync");

	mProfileTree.OnItemBeginEdit() += mDelBeginEdit.Bind(this, &ATUIDialogProfiles::OnBeginLabelEdit);
	mProfileTree.OnItemEndEdit() += mDelEndEdit.Bind(this, &ATUIDialogProfiles::OnEndLabelEdit);
	mProfileTree.OnItemSelectionChanged() += mDelSelChanged.Bind(this, &ATUIDialogProfiles::OnTreeSelectionChanged);
	mProfileTree.OnItemGetDisplayAttributes() += mDelGetDispAttr.Bind(this, &ATUIDialogProfiles::OnTreeGetDispAttr);
	mProfileTree.SetOnBeginDrag([this](const VDUIProxyTreeViewControl::BeginDragEvent& event) { OnTreeDrag(event); });
	mCategoryList.OnItemCheckedChanging() += mDelCheckedChanging.Bind(this, &ATUIDialogProfiles::OnCheckedChanging);

	mAddButton.SetOnClicked([this] { OnAdd(); });
	mDeleteButton.SetOnClicked([this] { OnDelete(); });
	mSetDefaultButton.SetOnClicked([this] { OnSetDefault(); });
	mUpdateButton.SetOnClicked([this] { OnUpdate(); });
	mSwitchButton.SetOnClicked([this] { OnSwitch(); });
	mVisibleCheckBox.SetOnClicked([this] { OnVisibleChanged(); });
}

bool ATUIDialogProfiles::OnLoaded() {
	mResizer.Add(IDC_PROFILES, mResizer.kMC | mResizer.kAvoidFlicker);
	mResizer.Add(IDC_CATEGORIES, mResizer.kMR | mResizer.kAvoidFlicker);
	mResizer.Add(IDC_ADD, mResizer.kBL);
	mResizer.Add(IDC_DELETE, mResizer.kBL);
	mResizer.Add(IDC_SETDEFAULT, mResizer.kBL);
	mResizer.Add(IDC_SWITCH, mResizer.kBL);
	mResizer.Add(IDC_UPDATE, mResizer.kBR);
	mResizer.Add(IDC_VISIBLE, mResizer.kBL);
	mResizer.Add(IDC_STATIC_CATEGORIES, mResizer.kTR);
	mResizer.Add(IDOK, mResizer.kBR);

	AddProxy(&mProfileTree, IDC_PROFILES);
	AddProxy(&mCategoryList, IDC_CATEGORIES);
	AddProxy(&mAddButton, IDC_ADD);
	AddProxy(&mDeleteButton, IDC_DELETE);
	AddProxy(&mSetDefaultButton, IDC_SETDEFAULT);
	AddProxy(&mSwitchButton, IDC_SWITCH);
	AddProxy(&mVisibleCheckBox, IDC_VISIBLE);
	AddProxy(&mUpdateButton, IDC_UPDATE);

	mCategoryList.SetRedraw(false);
	mCategoryList.SetItemCheckboxesEnabled(true);
	mCategoryList.SetFullRowSelectEnabled(true);
	mCategoryList.InsertColumn(0, L"", 50);

	mCategoryList.InsertItem(-1, L"(All)");
	for(const wchar_t *label : kCategoryNames)
		mCategoryList.InsertItem(-1, label);

	mCategoryList.AutoSizeColumns();
	mCategoryList.SetRedraw(true);

	OnDataExchange(false);
	SetFocusToControl(IDC_PROFILES);
	return true;
}

void ATUIDialogProfiles::OnDataExchange(bool write) {
	if (write)
		return;

	auto rootNode = vdmakerefptr(new ProfileNode(nullptr, 0, L"Global", kATSettingsCategory_All, ATSettingsProfileGetVisible(0)));
	uintptr rootTreeNode = mProfileTree.AddVirtualItem(mProfileTree.kNodeRoot, mProfileTree.kNodeFirst, rootNode);
	if (rootTreeNode)
		rootNode->mTreeNode = rootTreeNode;

	// get all profile IDs
	vdfastvector<uint32> profileIds;
	ATSettingsProfileEnum(profileIds);

	// fetch parent IDs for all profiles
	const size_t n = profileIds.size();
	vdfastvector<uint32> parentIds(n);
	std::transform(profileIds.begin(), profileIds.end(), parentIds.begin(), ATSettingsProfileGetParent);

	// convert parent IDs to indices
	for(size_t i=0; i<n; ++i) {
		if (parentIds[i]) {
			const auto it = std::lower_bound(profileIds.begin(), profileIds.end(), parentIds[i]);

			if (it != profileIds.end() && *it == parentIds[i]) {
				parentIds[i] = (uint32)(it - profileIds.begin());
				continue;
			}
		}

		parentIds[i] = (uint32)0 - 1;
	}

	// build tree
	typedef VDUIProxyTreeViewControl::NodeRef NodeRef;
	vdfastvector<NodeRef> nodes(n, NodeRef(0));
	vdvector<vdrefptr<ProfileNode>> parents(n);
	size_t built = 0;

	while(built < n) {
		uint32 firstUnhandledIdx = (uint32)n;
		bool progress = false;

		for(size_t i=0; i<n; ++i) {
			if (nodes[i])
				continue;

			uint32 parentIdx = parentIds[i];
			NodeRef parentNodeRef = rootTreeNode;
			if (parentIdx < n) {
				parentNodeRef = nodes[parentIdx];

				if (!parentNodeRef) {
					if (firstUnhandledIdx >= n)
						firstUnhandledIdx = (uint32)i;

					continue;
				}
			}

			progress = true;
			const uint32 profileId = profileIds[i];
			auto node = vdmakerefptr(new ProfileNode(parentIdx < n ? parents[parentIdx] : rootNode, profileId,
				ATSettingsProfileGetName(profileId).c_str(),
				ATSettingsProfileGetCategoryMask(profileId),
				ATSettingsProfileGetVisible(profileId)
				));
			parents[i] = node;

			auto treeNode = mProfileTree.AddVirtualItem(parentNodeRef, mProfileTree.kNodeFirst, node);
			if (treeNode)
				node->mTreeNode = treeNode;

			nodes[i] = treeNode;

			++built;
		}

		if (!progress)
			std::replace(parentIds.begin(), parentIds.end(), parentIds[firstUnhandledIdx], (uint32)0 - 1);
	}
}

void ATUIDialogProfiles::OnMouseMove(int x, int y) {
	if (mbDragInProgress) {
		auto treeNode = mProfileTree.FindDropTarget();
		mpDragTarget = static_cast<ProfileNode *>(mProfileTree.GetVirtualItem(treeNode));

		if (mpDragTarget)
			mProfileTree.SetDropTargetHighlight(treeNode);
		else
			mProfileTree.SetDropTargetHighlight(NULL);
	}
}

void ATUIDialogProfiles::OnMouseUpL(int x, int y) {
	if (!mbDragInProgress)
		return;

	mbDragInProgress = false;
	mProfileTree.SetDropTargetHighlight(NULL);

	// relocate the root tree node
	auto prevTreeNode = mpDragSource->mTreeNode;

	mpDragSource->mTreeNode = mProfileTree.AddVirtualItem(mpDragTarget->mTreeNode, mProfileTree.kNodeLast, mpDragSource);

	// relocate children
	mProfileTree.EnumChildrenRecursive(prevTreeNode,
		[&,this](IVDUITreeViewVirtualItem *item) {
			ProfileNode *node = static_cast<ProfileNode *>(item);

			node->mTreeNode = mProfileTree.AddVirtualItem(node->mpParent->mTreeNode, mProfileTree.kNodeLast, item);
		}
	);

	// nuke the original and its descendants
	mProfileTree.DeleteItem(prevTreeNode);

	// select and scroll to the new node
	mProfileTree.SelectNode(mpDragSource->mTreeNode);
	mProfileTree.MakeNodeVisible(mpDragSource->mTreeNode);

	// update the parent in the database
	ATSettingsProfileSetParent(mpDragSource->mId, mpDragTarget->mId);

	mpDragSource = nullptr;
	mpDragTarget = nullptr;

	ReleaseCapture();
}

void ATUIDialogProfiles::OnCaptureLost() {
	mbDragInProgress = false;
	mpDragSource = nullptr;
	mpDragTarget = nullptr;
	mProfileTree.SetDropTargetHighlight(NULL);
}

void ATUIDialogProfiles::OnAdd() {
	auto *parent = mProfileTree.GetSelectedVirtualItem<ProfileNode>();

	if (!parent)
		return;

	for(int i=0; i<=(int)vdcountof(kCategoryNames); ++i)
		mCategoryList.SetItemChecked(i, false);

	const uint32 profileId = ATSettingsGenerateProfileId();
	ATSettingsProfileSetParent(profileId, parent->mId);
	ATSettingsProfileSetName(profileId, L"<New>");
	ATSettingsProfileSetCategoryMask(profileId, kATSettingsCategory_None);
	ATSettingsProfileSetVisible(profileId, 0);
	auto newNode = vdmakerefptr(new ProfileNode(parent, profileId, L"<New>", kATSettingsCategory_None, false));
	uintptr p = mProfileTree.AddVirtualItem(parent->mTreeNode, mProfileTree.kNodeLast, newNode);
	if (p) {
		newNode->mTreeNode = p;

		SetFocusToControl(IDC_PROFILES);
		mProfileTree.SelectNode(p);
		mProfileTree.EditNodeLabel(p);
	}
}

void ATUIDialogProfiles::OnDelete() {
	auto node = vdmakerefptr(mProfileTree.GetSelectedVirtualItem<ProfileNode>());

	if (!node)
		return;

	// cannot delete the global profile
	uint32 profileId = node->mId;
	if (!profileId)
		return;

	// node being deleted cannot be the same as or a descendent of the current profile
	uint32 currentProfileId = ATSettingsGetCurrentProfileId();

	for(uint32 i=0; i<100; ++i) {
		if (currentProfileId == profileId) {
			ShowError(L"The selected profile cannot be deleted because it is related to the current profile. Switch to a different profile first.");
			return;
		}

		currentProfileId = ATSettingsProfileGetParent(currentProfileId);
	}

	if (mProfileTree.HasChildren(node->mTreeNode)) {
		ShowError(L"The selected profile cannot be deleted because it still has children.");
		return;
	}

	mProfileTree.DeleteItem(node->mTreeNode);
	ATSettingsProfileDelete(profileId);

	// refresh the parent as defaults may have been pushed up
	mProfileTree.RefreshNode(node->mpParent->mTreeNode);
}

void ATUIDialogProfiles::OnSetDefault() {
	const int index = ActivateMenuButton(IDC_SETDEFAULT, kDefaultProfileNames);
	if (index < 0)
		return;

	auto *node = mProfileTree.GetSelectedVirtualItem<ProfileNode>();
	if (!node)
		return;

	const ATDefaultProfile defaultProfile = (ATDefaultProfile)index;
	const uint32 oldId = ATGetDefaultProfileId(defaultProfile);
	const uint32 newId = node->mId;

	if (oldId == newId) {
		ATSetDefaultProfileId(defaultProfile, kATProfileId_Invalid);

		mProfileTree.RefreshNode(node->mTreeNode);
	} else {
		ATSetDefaultProfileId(defaultProfile, newId);

		mProfileTree.EnumChildrenRecursive(mProfileTree.kNodeRoot,
			[node, oldId, newId, this](IVDUITreeViewVirtualItem *p) {
				ProfileNode *node = static_cast<ProfileNode *>(p);
				const uint32 id = node->mId;

				if (id == oldId || id == newId)
					mProfileTree.RefreshNode(node->mTreeNode);
			}
		);
	}
}

void ATUIDialogProfiles::OnUpdate() {
	auto *node = mProfileTree.GetSelectedVirtualItem<ProfileNode>();
	if (!node || !node->mId)
		return;

	ATUIDialogEditProfileCategories dlg;
	dlg.SetCategoryMask(node->mCategoryMask);
	dlg.ShowDialog(this);

	ATSettingsCategory newMask = (ATSettingsCategory)dlg.GetCategoryMask();

	if (node->mCategoryMask != newMask) {
		node->mCategoryMask = newMask;

		ATSettingsProfileSetCategoryMask(node->mId, node->mCategoryMask);

		OnTreeSelectionChanged(nullptr, 0);
	}
}

void ATUIDialogProfiles::OnSwitch() {
	auto *node = mProfileTree.GetSelectedVirtualItem<ProfileNode>();
	if (!node)
		return;

	const uint32 oldId = ATSettingsGetCurrentProfileId();
	const uint32 newId = node->mId;

	if (oldId != newId) {
		ATSettingsSwitchProfile(newId);

		mProfileTree.EnumChildrenRecursive(mProfileTree.kNodeRoot,
			[oldId, newId, this](IVDUITreeViewVirtualItem *p) {
				ProfileNode *node2 = static_cast<ProfileNode *>(p);
				const uint32 id = node2->mId;

				if (id == oldId || id == newId)
					mProfileTree.RefreshNode(node2->mTreeNode);
			}
		);
	}
}

void ATUIDialogProfiles::OnVisibleChanged() {
	auto *node = mProfileTree.GetSelectedVirtualItem<ProfileNode>();
	if (!node)
		return;

	const bool visible = mVisibleCheckBox.GetChecked();

	if (node->mbVisible != visible) {
		node->mbVisible = visible;
		ATSettingsProfileSetVisible(node->mId, visible);
	}
}

void ATUIDialogProfiles::OnBeginLabelEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::BeginEditEvent *event) {
	auto *node = static_cast<ProfileNode *>(event->mpItem);

	event->mbAllowEdit = node && node->mId;
}

void ATUIDialogProfiles::OnEndLabelEdit(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::EndEditEvent *event) {
	ProfileNode *node = static_cast<ProfileNode *>(event->mpItem);

	node->mName = event->mpNewText;

	ATSettingsProfileSetName(node->mId, event->mpNewText);
}

void ATUIDialogProfiles::OnTreeSelectionChanged(VDUIProxyTreeViewControl *, int) {
	uint32 mask = 0;

	auto *node = mProfileTree.GetSelectedVirtualItem<ProfileNode>();
	if (node)
		mask = node->mCategoryMask;

	++mUpdateHandlerInhibit;
	
	mCategoryList.SetItemChecked(0, mask == kATSettingsCategory_All);

	for(int i=0; i<(int)vdcountof(kCategoryNames); ++i)
		mCategoryList.SetItemChecked(i+1, (mask & (1 << i)) != 0);

	mVisibleCheckBox.SetChecked(node && node->mbVisible);

	--mUpdateHandlerInhibit;
}

void ATUIDialogProfiles::OnTreeGetDispAttr(VDUIProxyTreeViewControl *, VDUIProxyTreeViewControl::GetDispAttrEvent *event) {
	if (event->mpItem)
		event->mbIsBold = static_cast<ProfileNode *>(event->mpItem)->mId == ATSettingsGetCurrentProfileId();
}

void ATUIDialogProfiles::OnTreeDrag(const VDUIProxyTreeViewControl::BeginDragEvent& event) {
	mbDragInProgress = true;
	mpDragSource = static_cast<ProfileNode *>(event.mpItem);
	mpDragTarget = nullptr;
	SetCapture();
}

void ATUIDialogProfiles::OnCheckedChanging(VDUIProxyListView *, VDUIProxyListView::CheckedChangingEvent *event) {
	event->mbAllowChange = (mUpdateHandlerInhibit > 0);
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogProfiles(VDGUIHandle hParent) {
	ATUIDialogProfiles dlg;
	
	dlg.ShowDialog(hParent);
}
