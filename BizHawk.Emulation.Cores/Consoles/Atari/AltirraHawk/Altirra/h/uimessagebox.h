#ifndef f_AT_UIMESSAGEBOX_H
#define f_AT_UIMESSAGEBOX_H

#include <vd2/system/VDString.h>
#include <at/atui/uicontainer.h>
#include "callback.h"

class IVDDisplayFont;
class ATUILabel;
class ATUIButton;

class ATUIMessageBox : public ATUIContainer {
public:
	enum Result {
		kResultOK,
		kResultCancel
	};

	ATUIMessageBox();
	~ATUIMessageBox();

	void SetCaption(const wchar_t *s);
	void SetText(const wchar_t *s);
	void SetQueryMode(bool enabled);

	void ShowModal();

	void AutoSize();

	ATCallbackHandler1<void, uint32>& OnCompletedEvent() { return mCompletedEvent; }

public:
	virtual void OnCreate();
	virtual void OnDestroy();
	virtual void OnSize();
	virtual void OnSetFocus();

protected:
	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	void EndWithResult(Result result);

	void OnOKPressed();
	void OnCancelPressed();

	VDStringW mCaption;
	VDStringW mText;
	IVDDisplayFont *mpCaptionFont;
	sint32 mCaptionHeight;
	bool mbModal;
	bool mbQueryMode;

	ATUILabel *mpMessageLabel;
	ATUIButton *mpButtonOK;
	ATUIButton *mpButtonCancel;

	ATCallbackHandler1<void, uint32> mCompletedEvent;
};

#endif
