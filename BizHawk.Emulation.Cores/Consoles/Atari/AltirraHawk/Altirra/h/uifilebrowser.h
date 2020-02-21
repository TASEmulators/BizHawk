#ifndef f_AT_UIFILEBROWSER_H
#define f_AT_UIFILEBROWSER_H

#include <vd2/system/function.h>
#include <at/atui/uicontainer.h>
#include <at/atuicontrols/uilabel.h>
#include <at/atuicontrols/uibutton.h>
#include <at/atuicontrols/uilistview.h>
#include <at/atuicontrols/uitextedit.h>
#include "callback.h"

class ATUIFileBrowser final : public ATUIContainer {
public:
	ATUIFileBrowser();
	~ATUIFileBrowser();

	void LoadPersistentData(uint32 id);
	void SavePersistentData(uint32 id);

	const wchar_t *GetPath() const;
	void SetPath(const wchar_t *path);
	void SetDirectory(const wchar_t *path);

	void SetTitle(const wchar_t *title);

	void ShowModal();

	void Ascend();
	void Descend(const wchar_t *folder);

	void SetCompletionFn(const vdfunction<void(bool)>& fn) { mpCompletionFn = fn; }

public:
	void OnCreate() override;
	void OnDestroy() override;
	void OnSize() override;

protected:
	void OnGoUpPressed();
	void OnOKPressed();
	void OnCancelPressed();
	void OnItemSelected(sint32);
	void OnItemActivated(sint32);
	void OnRootItemActivated(sint32);
	void OnNewPathEntered();

	void Repopulate();

	bool mbModal;
	VDStringW mPath;
	VDStringW mTitle;

	vdrefptr<ATUILabel> mpLabel;
	vdrefptr<ATUIListView> mpListView;
	vdrefptr<ATUIListView> mpRootListView;
	vdrefptr<ATUIContainer> mpBottomContainer;
	vdrefptr<ATUIContainer> mpTopContainer;
	vdrefptr<ATUIButton> mpButtonUp;
	vdrefptr<ATUIButton> mpButtonOK;
	vdrefptr<ATUIButton> mpButtonCancel;
	vdrefptr<ATUITextEdit> mpTextEdit;
	vdrefptr<ATUITextEdit> mpTextEditPath;

	vdfunction<void(bool)> mpCompletionFn;
};

#endif
