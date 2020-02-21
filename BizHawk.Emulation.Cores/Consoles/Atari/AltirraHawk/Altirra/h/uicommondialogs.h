#ifndef f_AT_UICOMMONDIALOGS_H
#define f_AT_UICOMMONDIALOGS_H

#include <vd2/system/refcount.h>
#include <vd2/system/VDString.h>
#include "uiqueue.h"

class MyError;

bool ATUIGetNativeDialogMode();
void ATUISetNativeDialogMode(bool enabled);

///////////////////////////////////////////////////////////////////////////

void ATUIShowInfo(VDGUIHandle h, const wchar_t *text);
void ATUIShowWarning(VDGUIHandle h, const wchar_t *text, const wchar_t *caption);
bool ATUIShowWarningConfirm(VDGUIHandle h, const wchar_t *text);
void ATUIShowError(VDGUIHandle h, const wchar_t *text);
void ATUIShowError(VDGUIHandle h, const MyError& e);

vdrefptr<ATUIFutureWithResult<bool> > ATUIShowAlert(const wchar_t *text, const wchar_t *caption);

///////////////////////////////////////////////////////////////////////////

struct ATUIFileDialogResult : public ATUIFuture {
	bool mbAccepted;
	VDStringW mPath;
};

vdrefptr<ATUIFileDialogResult>  ATUIShowOpenFileDialog(uint32 id, const wchar_t *title, const wchar_t *filters);
vdrefptr<ATUIFileDialogResult>  ATUIShowSaveFileDialog(uint32 id, const wchar_t *title, const wchar_t *filters);

#endif
