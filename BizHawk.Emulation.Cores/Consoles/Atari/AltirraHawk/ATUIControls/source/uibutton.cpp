#include <stdafx.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atuicontrols/uibutton.h>
#include <at/atui/uimanager.h>
#include <at/atui/uidrawingutils.h>

ATUIButton::ATUIButton()
	: mStockImageIdx(-1)
	, mbDepressed(false)
	, mbHeld(false)
	, mbToggleMode(false)
	, mbFrameEnabled(true)
	, mTextX(0)
	, mTextY(0)
	, mTextColor(0)
	, mActivatedEvent()
	, mPressedEvent()
{
	SetTouchMode(kATUITouchMode_Immediate);
	SetFillColor(0xD4D0C8);
	BindAction(kATUIVK_Space, kActionActivate);
	BindAction(kATUIVK_Return, kActionActivate);
}

ATUIButton::~ATUIButton() {
}

void ATUIButton::SetStockImage(sint32 idx) {
	if (mStockImageIdx == idx)
		return;

	mStockImageIdx = idx;
	Invalidate();
}

void ATUIButton::SetText(const wchar_t *s) {
	if (mText != s) {
		mText = s;

		Relayout();
		Invalidate();
	}
}

void ATUIButton::SetTextColor(uint32 color) {
	if (mTextColor != color) {
		mTextColor = color;

		Invalidate();
	}
}

void ATUIButton::SetDepressed(bool depressed) {
	if (mbDepressed != depressed) {
		mbDepressed = depressed;

		Invalidate();

		if (depressed) {
			if (mPressedEvent)
				mPressedEvent();
		} else {
			if (mActivatedEvent)
				mActivatedEvent();
		}
	}
}

void ATUIButton::SetToggleMode(bool enabled) {
	mbToggleMode = enabled;
}

void ATUIButton::SetFrameEnabled(bool enabled) {
	if (mbFrameEnabled != enabled) {
		mbFrameEnabled = enabled;

		SetAlphaFillColor(enabled ? 0xFFD4D0C8 : 0);

		Relayout();
		Invalidate();
	}
}

void ATUIButton::OnMouseDownL(sint32 x, sint32 y) {
	SetHeld(true);
	CaptureCursor();
	Focus();
}

void ATUIButton::OnMouseUpL(sint32 x, sint32 y) {
	SetHeld(false);

	if (IsCursorCaptured())
		ReleaseCursor();
}

void ATUIButton::OnActionStart(uint32 id) {
	switch(id) {
		case kActionActivate:
			SetHeld(true);
			break;

		default:
			ATUIWidget::OnActionStart(id);
	}
}

void ATUIButton::OnActionStop(uint32 id) {
	switch(id) {
		case kActionActivate:
			SetHeld(false);
			break;

		default:
			ATUIWidget::OnActionStop(id);
	}
}

void ATUIButton::OnCreate() {
	mpFont = mpManager->GetThemeFont(kATUIThemeFont_Default);

	Relayout();
}

void ATUIButton::OnSize() {
	Relayout();
}

void ATUIButton::OnSetFocus() {
	if (mbFrameEnabled) {
		SetFillColor(0xA0C0FF);
		Invalidate();
	}
}

void ATUIButton::OnKillFocus() {
	if (mbFrameEnabled) {
		SetFillColor(0xD4D0C8);
		Invalidate();
	}
}

void ATUIButton::SetHeld(bool held) {
	if (mbHeld == held)
		return;

	mbHeld = held;

	// We set this even if toggle mode isn't currently set so that toggle
	// mode can be toggled on the fly.
	if (held)
		mbToggleNextState = !mbDepressed;

	if (mHeldEvent)
		mHeldEvent(held);

	if (!mbToggleMode)
		SetDepressed(held);
	else if (held) {
		if (mbToggleNextState)
			SetDepressed(true);
	} else {
		if (!mbToggleNextState)
			SetDepressed(false);
	}
}

void ATUIButton::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	vdrect32 r(0, 0, w, h);

	if (mbFrameEnabled) {
		ATUIDraw3DRect(rdr, r, mbDepressed);
		r.left = 2;
		r.top = 2;
		r.right -= 2;
		r.bottom -= 2;
	}

	if (rdr.PushViewport(r, r.left, r.top)) {
		if (mStockImageIdx >= 0) {
			ATUIStockImage& image = mpManager->GetStockImage((ATUIStockImageIdx)mStockImageIdx);

			VDDisplayBlt blt;
			blt.mDestX = (r.width() - image.mWidth) >> 1;
			blt.mDestY = (r.height() - image.mHeight) >> 1;
			blt.mSrcX = 0;
			blt.mSrcY = 0;
			blt.mWidth = image.mWidth;
			blt.mHeight = image.mHeight;

			rdr.SetColorRGB(mTextColor);
			rdr.MultiBlt(&blt, 1, image.mImageView, IVDDisplayRenderer::kBltMode_Color);
		} else if (mpFont) {
			VDDisplayTextRenderer *tr = rdr.GetTextRenderer();

			tr->SetFont(mpFont);
			tr->SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
			tr->SetColorRGB(mTextColor);
			tr->DrawTextLine(mTextX, mTextY, mText.c_str());
		}

		rdr.PopViewport();
	}
}

void ATUIButton::Relayout() {
	if (mpFont) {
		vdsize32 size = mpFont->MeasureString(mText.data(), mText.size(), false);

		VDDisplayFontMetrics m;
		mpFont->GetMetrics(m);

		sint32 w = mArea.width();
		sint32 h = mArea.height();

		if (mbFrameEnabled) {
			w -= 4;
			h -= 4;
		}

		mTextX = (w - size.w) >> 1;
		mTextY = (h - m.mAscent) >> 1;

		Invalidate();
	}
}
