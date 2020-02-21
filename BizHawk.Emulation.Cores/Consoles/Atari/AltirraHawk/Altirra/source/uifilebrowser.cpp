#include <stdafx.h>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <vd2/system/math.h>
#include <vd2/Dita/services.h>
#include "uifilebrowser.h"
#include <at/atui/uimanager.h>

struct ATUIFileBrowserItem : public vdrefcounted<IATUIListViewVirtualItem> {
	void *AsInterface(uint32 id) { return NULL; }

	void GetText(VDStringW& s);

	VDStringW mName;
	VDStringW mDescription;
	bool mbIsDirectory;
};

void ATUIFileBrowserItem::GetText(VDStringW& s) {
	s = mDescription;
}

///////////////////////////////////////////////////////////////////////////

struct ATUIFileBrowserItemSorter : public IATUIListViewSorter {
	bool Compare(IATUIListViewVirtualItem *a, IATUIListViewVirtualItem *b) const {
		const ATUIFileBrowserItem& x = *static_cast<ATUIFileBrowserItem *>(a);
		const ATUIFileBrowserItem& y = *static_cast<ATUIFileBrowserItem *>(b);

		if (x.mbIsDirectory != y.mbIsDirectory)
			return x.mbIsDirectory;

		return x.mName.comparei(y.mName) < 0;
	}
};

///////////////////////////////////////////////////////////////////////////

ATUIFileBrowser::ATUIFileBrowser()
	: mbModal(false)
{
}

ATUIFileBrowser::~ATUIFileBrowser() {
}

void ATUIFileBrowser::LoadPersistentData(uint32 id) {
	SetPath(VDGetLastLoadSavePath(id).c_str());
}

void ATUIFileBrowser::SavePersistentData(uint32 id) {
	VDSetLastLoadSavePath(id, GetPath());
}

const wchar_t *ATUIFileBrowser::GetPath() const {
	return mPath.c_str();
}

void ATUIFileBrowser::SetPath(const wchar_t *path) {
	mPath = path;
	const wchar_t *file = VDFileSplitPath(path);

	if (mpTextEditPath)
		mpTextEditPath->SetText(VDStringW(path, file).c_str());

	if (mpTextEdit)
		mpTextEdit->SetText(file);

	Repopulate();
}

void ATUIFileBrowser::SetDirectory(const wchar_t *path) {
	if (mpTextEditPath)
		mpTextEditPath->SetText(path);

	Repopulate();
}

void ATUIFileBrowser::SetTitle(const wchar_t *title) {
	mTitle = title;

	if (mpLabel)
		mpLabel->SetText(title);
}

void ATUIFileBrowser::ShowModal() {
	mbModal = true;
	mpManager->BeginModal(this);
}

void ATUIFileBrowser::Ascend() {
	const VDStringW path(mpTextEditPath->GetText());
	VDParsedPath ppath(path.c_str());

	ppath.RemoveLastComponent();

	const VDStringW parentPath = ppath.ToString();

	if (parentPath != path) {
		mpTextEditPath->SetText(parentPath.c_str());

		Repopulate();
	}
}

void ATUIFileBrowser::OnCreate() {
	mpListView = new ATUIListView;
	AddChild(mpListView);
	mpListView->SetDockMode(kATUIDockMode_Fill);
	mpListView->OnItemSelectedEvent() = [this](auto index) { OnItemSelected(index); };
	mpListView->OnItemActivatedEvent() = [this](auto index) { OnItemActivated(index); };
	mpListView->SetFrameMode(kATUIFrameMode_Sunken);

	mpRootListView = new ATUIListView;
	AddChild(mpRootListView);
	mpRootListView->SetDockMode(kATUIDockMode_Left);
	mpRootListView->OnItemActivatedEvent() = [this](auto index) { OnRootItemActivated(index); };
//	mpRootListView->SetFrameMode(kATUIFrameMode_Sunken);
	mpRootListView->SetArea(vdrect32(0, 0, 100, 0));

	vdvector<VDStringW> rootPaths;
	VDGetRootPaths(rootPaths);

	while(!rootPaths.empty()) {
		VDStringW& s = rootPaths.back();

		vdrefptr<ATUIFileBrowserItem> item(new ATUIFileBrowserItem);

		vdmove(item->mName, s);

		const VDStringW& volLabel = VDGetRootVolumeLabel(item->mName.c_str());

		size_t nameLen = item->mName.size();

		if (nameLen && item->mName.back() == L'\\')
			--nameLen;

		if (volLabel.empty())
			item->mDescription.assign(item->mName, 0, (uint32)nameLen);
		else {
			item->mDescription.sprintf(L"%ls (%.*ls)", volLabel.c_str(), nameLen, item->mName.c_str());
		}

		item->mbIsDirectory = true;

		mpRootListView->AddItem(item);

		rootPaths.pop_back();
	}

	mpRootListView->Sort(ATUIFileBrowserItemSorter());

	mpTopContainer = new ATUIContainer;
	AddChild(mpTopContainer);
	mpTopContainer->SetDockMode(kATUIDockMode_Top);

	mpTextEditPath = new ATUITextEdit;
	mpTopContainer->AddChild(mpTextEditPath);
	mpTextEditPath->SetDockMode(kATUIDockMode_Fill);
	mpTextEditPath->SetFrameMode(kATUIFrameMode_Sunken);
	mpTextEditPath->OnReturnPressed() = [this] { OnNewPathEntered(); };

	const sint32 rowHt = mpTextEditPath->GetIdealHeight();
	const sint32 buttonWidth = VDRoundToInt32((float)rowHt * (75.0f / 20.0f));
	mpTopContainer->SetArea(vdrect32(0, 0, 0, rowHt));

	mpButtonUp = new ATUIButton;
	mpTopContainer->AddChild(mpButtonUp);
	mpButtonUp->SetArea(vdrect32(0, 0, buttonWidth, 20));
	mpButtonUp->SetText(L"Up");
	mpButtonUp->SetDockMode(kATUIDockMode_Right);
	mpButtonUp->OnActivatedEvent() = [this] { OnGoUpPressed(); };

	mpBottomContainer = new ATUIContainer;
	AddChild(mpBottomContainer);
	mpBottomContainer->SetDockMode(kATUIDockMode_Bottom);

	mpTextEdit = new ATUITextEdit;
	mpBottomContainer->AddChild(mpTextEdit);
	mpTextEdit->SetDockMode(kATUIDockMode_Fill);
	mpTextEdit->SetFrameMode(kATUIFrameMode_Sunken);

	mpBottomContainer->SetArea(vdrect32(0, 0, 0, mpTextEdit->GetIdealHeight()));

	mpButtonOK = new ATUIButton;
	mpBottomContainer->AddChild(mpButtonOK);
	mpButtonOK->SetArea(vdrect32(0, 0, buttonWidth, 20));
	mpButtonOK->SetText(L"OK");
	mpButtonOK->SetDockMode(kATUIDockMode_Right);
	mpButtonOK->OnActivatedEvent() = [this] { OnOKPressed(); };

	mpButtonCancel = new ATUIButton;
	mpBottomContainer->AddChild(mpButtonCancel);
	mpButtonCancel->SetArea(vdrect32(0, 0, buttonWidth, 20));
	mpButtonCancel->SetText(L"Cancel");
	mpButtonCancel->SetDockMode(kATUIDockMode_Right);
	mpButtonCancel->OnActivatedEvent() = [this] { OnCancelPressed(); };

	mpLabel = new ATUILabel;
	AddChild(mpLabel);
	mpLabel->SetTextAlign(ATUILabel::kAlignCenter);
	mpLabel->SetFrameMode(kATUIFrameMode_Raised);
	mpLabel->SetText(mTitle.c_str());
	mpLabel->SetTextOffset(0, 6);
	mpLabel->AutoSize();
	mpLabel->SetDockMode(kATUIDockMode_Top);

	OnSize();

	UnbindAllActions();
	BindAction(kATUIVK_Escape, ATUIButton::kActionActivate, 0, mpButtonCancel->GetInstanceId());
	BindAction(kATUIVK_UIReject, ATUIButton::kActionActivate, 0, mpButtonCancel->GetInstanceId());
	BindAction(kATUIVK_Back, ATUIButton::kActionActivate, 0, mpButtonUp->GetInstanceId());
	BindAction(kATUIVK_UIMenu, ATUIButton::kActionActivate, 0, mpButtonUp->GetInstanceId());

	mpTextEdit->BindAction(kATUIVK_Return, ATUIButton::kActionActivate, 0, mpButtonOK->GetInstanceId());

	mpListView->BindAction(kATUIVK_Return, ATUIListView::kActionActivateItem);
	mpListView->BindAction(kATUIVK_UIAccept, ATUIListView::kActionActivateItem);
	mpListView->BindAction(kATUIVK_UIUp, ATUIListView::kActionMoveUp);
	mpListView->BindAction(kATUIVK_UIDown, ATUIListView::kActionMoveDown);
	mpListView->BindAction(kATUIVK_UILeft, ATUIListView::kActionMovePagePrev);
	mpListView->BindAction(kATUIVK_UIRight, ATUIListView::kActionMovePageNext);
	mpListView->BindAction(kATUIVK_UISwitchLeft, ATUIListView::kActionFocus, 0, mpRootListView->GetInstanceId());

	mpRootListView->BindAction(kATUIVK_Return, ATUIListView::kActionActivateItem);
	mpRootListView->BindAction(kATUIVK_UIAccept, ATUIListView::kActionActivateItem);
	mpRootListView->BindAction(kATUIVK_UIUp, ATUIListView::kActionMoveUp);
	mpRootListView->BindAction(kATUIVK_UIDown, ATUIListView::kActionMoveDown);
	mpRootListView->BindAction(kATUIVK_UILeft, ATUIListView::kActionMovePagePrev);
	mpRootListView->BindAction(kATUIVK_UIRight, ATUIListView::kActionMovePageNext);
	mpRootListView->BindAction(kATUIVK_UISwitchRight, ATUIListView::kActionFocus, 0, mpListView->GetInstanceId());

	// set up tab order
	mpTextEditPath->BindAction(kATUIVK_Tab, ATUIWidget::kActionFocus, 0, mpRootListView->GetInstanceId());
	mpRootListView->BindAction(kATUIVK_Tab, ATUIWidget::kActionFocus, 0, mpListView->GetInstanceId());
	mpListView->BindAction(kATUIVK_Tab, ATUIWidget::kActionFocus, 0, mpTextEdit->GetInstanceId());
	mpTextEdit->BindAction(kATUIVK_Tab, ATUIWidget::kActionFocus, 0, mpButtonOK->GetInstanceId());
	mpButtonOK->BindAction(kATUIVK_Tab, ATUIWidget::kActionFocus, 0, mpButtonCancel->GetInstanceId());
	mpButtonCancel->BindAction(kATUIVK_Tab, ATUIWidget::kActionFocus, 0, mpTextEditPath->GetInstanceId());

	Repopulate();
}

void ATUIFileBrowser::OnDestroy() {
	mpLabel.clear();
	mpListView.clear();
	mpRootListView.clear();
	mpTextEdit.clear();
	mpTextEditPath.clear();
	mpBottomContainer.clear();
	mpTopContainer.clear();
	mpButtonUp.clear();
	mpButtonOK.clear();
	mpButtonCancel.clear();

	RemoveAllChildren();
}

void ATUIFileBrowser::OnSize() {
	if (mpRootListView) {
		vdrect32 r = mpRootListView->GetArea();

		r.right = r.left + GetClientArea().width() / 5;

		mpRootListView->SetArea(r);

		InvalidateLayout();
	}
}

void ATUIFileBrowser::OnGoUpPressed() {
	Ascend();
}

void ATUIFileBrowser::OnOKPressed() {
	const VDStringW name(mpTextEdit->GetText());

	if (name.empty())
		return;

	VDStringW path;
	
	if (VDFileIsRelativePath(name.c_str()))
		path = VDMakePath(mpTextEditPath->GetText(), name.c_str());
	else
		path = name;

	path = VDFileGetCanonicalPath(path.c_str());
	
	const uint32 attr = VDFileGetAttributes(path.c_str());

	if (attr == kVDFileAttr_Invalid)
		return;

	if (attr & kVDFileAttr_Directory) {
		SetDirectory(path.c_str());
		mpTextEdit->SetText(L"");
		return;
	}

	mPath = path;

	if (mbModal) {
		mbModal = false;
		mpManager->EndModal();
	}

	if (mpCompletionFn)
		mpCompletionFn(true);

	if (mpParent)
		mpParent->RemoveChild(this);
}

void ATUIFileBrowser::OnCancelPressed() {
	if (mbModal) {
		mbModal = false;
		mpManager->EndModal();
	}

	if (mpCompletionFn)
		mpCompletionFn(false);

	if (mpParent)
		mpParent->RemoveChild(this);
}

void ATUIFileBrowser::OnItemSelected(sint32 idx) {
	ATUIFileBrowserItem *item = static_cast<ATUIFileBrowserItem *>(mpListView->GetSelectedVirtualItem());

	if (item)
		mpTextEdit->SetText(item->mName.c_str());
}

void ATUIFileBrowser::OnItemActivated(sint32 idx) {
	OnItemSelected(idx);
	OnOKPressed();
}

void ATUIFileBrowser::OnRootItemActivated(sint32) {
	ATUIFileBrowserItem *item = static_cast<ATUIFileBrowserItem *>(mpRootListView->GetSelectedVirtualItem());

	if (item && item->mbIsDirectory) {
		mpRootListView->SetSelectedItem(-1);
		SetDirectory(item->mName.c_str());
		mpListView->Focus();
		mpTextEdit->SetText(L"");
	}
}

void ATUIFileBrowser::OnNewPathEntered() {
	const wchar_t *path = mpTextEditPath->GetText();
	const uint32 attr = VDFileGetAttributes(path);

	if ((attr != kVDFileAttr_Invalid) && (attr & kVDFileAttr_Directory))
		SetDirectory(VDFileGetCanonicalPath(path).c_str());

	mpListView->Focus();
}

void ATUIFileBrowser::Repopulate() {
	mpListView->RemoveAllItems();

	try {
		VDDirectoryIterator it(VDMakePath(mpTextEditPath->GetText(), L"*.*").c_str());

		while(it.Next()) {
			if (it.IsDotDirectory())
				continue;

			vdrefptr<ATUIFileBrowserItem> item(new ATUIFileBrowserItem);

			item->mName = it.GetName();
			item->mDescription = item->mName;
			item->mbIsDirectory = it.IsDirectory();

			if (item->mbIsDirectory)
				item->mDescription += L'\\';

			mpListView->AddItem(item);
		}

		mpListView->Sort(ATUIFileBrowserItemSorter());
	} catch(const MyError&) {
	}

	mpListView->SetSelectedItem(0);
}
