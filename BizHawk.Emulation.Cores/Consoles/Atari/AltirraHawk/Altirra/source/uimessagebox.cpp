#include <stdafx.h>
#include <vd2/VDDisplay/font.h>
#include <vd2/VDDisplay/textrenderer.h>
#include "uimessagebox.h"
#include <at/atui/uimanager.h>
#include <at/atui/uidrawingutils.h>
#include <at/atuicontrols/uibutton.h>
#include <at/atuicontrols/uilabel.h>

ATUIMessageBox::ATUIMessageBox()
	: mpCaptionFont(NULL)
	, mCaptionHeight(0)
	, mbModal(false)
	, mbQueryMode(false)
	, mpMessageLabel(NULL)
	, mpButtonOK(NULL)
	, mpButtonCancel(NULL)
	, mCompletedEvent()
{
}

ATUIMessageBox::~ATUIMessageBox() {
}

void ATUIMessageBox::SetCaption(const wchar_t *s) {
	if (mCaption == s)
		return;

	mCaption = s;
	Invalidate();
}

void ATUIMessageBox::SetText(const wchar_t *s) {
	if (mText == s)
		return;

	mText = s;

	if (mpMessageLabel)
		mpMessageLabel->SetText(s);
}

void ATUIMessageBox::SetQueryMode(bool enabled) {
	mbQueryMode = enabled;
}

void ATUIMessageBox::ShowModal() {
	mbModal = true;
	mpManager->BeginModal(this);
}

void ATUIMessageBox::AutoSize() {
	mpMessageLabel->AutoSize();

	const vdrect32& r = mpMessageLabel->GetArea();

	sint32 messageWidth = r.width() + 12;
	sint32 captionWidth = mpCaptionFont->MeasureString(mCaption.data(), mCaption.size(), true).w + 16;

	SetSize(vdsize32(std::max<sint32>(messageWidth, captionWidth), mCaptionHeight + 52 + r.height()));
}

void ATUIMessageBox::OnCreate() {
	ATUIContainer::OnCreate();

	mpCaptionFont = mpManager->GetThemeFont(kATUIThemeFont_Header);
	mpCaptionFont->AddRef();

	VDDisplayFontMetrics metrics;
	mpCaptionFont->GetMetrics(metrics);

	mCaptionHeight = metrics.mAscent + metrics.mDescent + 6;

	mpMessageLabel = new ATUILabel;
	mpMessageLabel->AddRef();
	AddChild(mpMessageLabel);

	mpButtonOK = new ATUIButton;
	mpButtonOK->AddRef();
	AddChild(mpButtonOK);
	mpButtonOK->SetText(L"OK");
	mpButtonOK->SetSize(vdsize32(75, 24));
	mpButtonOK->OnActivatedEvent() = [this] { OnOKPressed(); };

	mpButtonCancel = new ATUIButton;
	mpButtonCancel->AddRef();
	AddChild(mpButtonCancel);
	mpButtonCancel->SetText(L"Cancel");
	mpButtonCancel->SetSize(vdsize32(75, 24));
	mpButtonCancel->OnActivatedEvent() = [this] { OnCancelPressed(); };

	OnSize();

	mpButtonOK->Focus();

	BindAction(kATUIVK_UIAccept, ATUIButton::kActionActivate, 0, mpButtonOK->GetInstanceId());
	BindAction(kATUIVK_UIReject, ATUIButton::kActionActivate, 0, mpButtonCancel->GetInstanceId());
}

void ATUIMessageBox::OnDestroy() {
	vdsaferelease <<= mpButtonCancel, mpButtonOK, mpMessageLabel, mpCaptionFont;

	UnbindAllActions();

	ATUIContainer::OnDestroy();
}

void ATUIMessageBox::OnSize() {
	sint32 w = mClientArea.width();
	sint32 h = mClientArea.height();
	sint32 wh = w >> 1;

	mpMessageLabel->SetArea(vdrect32(4, mCaptionHeight + 4, w - 4, h - (2+24+4)));

	if (mbQueryMode) {
		mpButtonOK->SetPosition(vdpoint32(wh - (4+75), h - (2+24)));
		mpButtonCancel->SetPosition(vdpoint32(wh + 4, h - (2+24)));
		mpButtonCancel->SetVisible(true);
	} else {
		mpButtonOK->SetPosition(vdpoint32((w - 75) >> 1, h - (2+24)));
		mpButtonCancel->SetVisible(false);
	}
}

void ATUIMessageBox::OnSetFocus() {
	if (mpButtonCancel)
		mpButtonCancel->Focus();
	else if (mpButtonOK)
		mpButtonOK->Focus();
}

void ATUIMessageBox::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	rdr.SetColorRGB(0x0000FF);
	rdr.FillRect(0, 0, w, mCaptionHeight);

	VDDisplayTextRenderer *tr = rdr.GetTextRenderer();
	tr->SetFont(mpCaptionFont);
	tr->SetColorRGB(0xFFFFFF);
	tr->SetAlignment(VDDisplayTextRenderer::kAlignCenter, VDDisplayTextRenderer::kVertAlignTop);
	tr->DrawTextLine(mClientArea.width() >> 1, 3, mCaption.c_str());

	if (h > mCaptionHeight) {
		rdr.SetColorRGB(0xD4D0C8);
		rdr.FillRect(0, mCaptionHeight, w, h - mCaptionHeight);
	}

	ATUIContainer::Paint(rdr, w, h);
}

void ATUIMessageBox::EndWithResult(Result result) {
	if (mbModal)
		mpManager->EndModal();

	Destroy();

	if (mCompletedEvent)
		mCompletedEvent(result);
}

void ATUIMessageBox::OnOKPressed() {
	EndWithResult(kResultOK);
}

void ATUIMessageBox::OnCancelPressed() {
	EndWithResult(kResultCancel);
}
