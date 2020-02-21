#include <stdafx.h>
#include <vd2/system/VDString.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atuicontrols/uitextedit.h>
#include <at/atui/uimanager.h>

ATUITextEdit::ATUITextEdit()
	: mScrollX(0)
	, mCaretPosX(0)
	, mCaretPixelX(0)
	, mAnchorPosX(-1)
	, mAnchorPixelX(0)
	, mTextMarginX(2)
	, mTextMarginY(2)
	, mTextColor(0)
	, mHighlightBackgroundColor(0x0A246A)
	, mHighlightTextColor(0xFFFFFF)
	, mbFocused(false)
	, mbCaretOn(false)
	, mReturnPressedEvent()
{
	SetFillColor(0xFFFFFF);
	SetCursorImage(kATUICursorImage_IBeam);
}

ATUITextEdit::~ATUITextEdit() {
}

void ATUITextEdit::AutoSize() {
	if (!mpFont)
		return;

	SetSize(vdsize32(mClientArea.width(), GetIdealHeight()));
}

sint32 ATUITextEdit::GetIdealHeight() const {
	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);

	return ComputeWindowSize(vdrect32(0, 0, 0, metrics.mAscent + metrics.mDescent + mTextMarginY * 2)).height();
}

void ATUITextEdit::ClearSelection() {
	if (mAnchorPosX >= 0) {
		mAnchorPosX = -1;

		Invalidate();
	}
}

void ATUITextEdit::Delete() {
	if (mAnchorPosX >= 0) {
		if (mAnchorPosX > mCaretPosX) {
			mText.erase(mCaretPosX, mAnchorPosX - mCaretPosX);
			ClearSelection();
			UpdateCaretPixelX();
			Invalidate();
		} else {
			mText.erase(mAnchorPosX, mCaretPosX - mAnchorPosX);
			SetCaretPosX(mCaretPosX, false);
			Invalidate();
		}
	} else if (mCaretPosX < (sint32)mText.size()) {
		mText.erase(mCaretPosX, 1);
		UpdateCaretPixelX();
		Invalidate();
	}
}

void ATUITextEdit::SetText(const wchar_t *s) {
	mText = s;
	mCaretPosX = (sint32)mText.size();
	mAnchorPosX = -1;

	UpdateCaretPixelX();
	Invalidate();
}

void ATUITextEdit::OnMouseDownL(sint32 x, sint32 y) {
	Focus();

	if (mpFont) {
		sint32 pos = GetNearestPosFromX(x);

		SetCaretPosX(pos, mpManager->IsKeyDown(kATUIVK_Shift));

		CaptureCursor();
	}
}

void ATUITextEdit::OnMouseUpL(sint32 x, sint32 y) {
	ReleaseCursor();
}

void ATUITextEdit::OnMouseMove(sint32 x, sint32 y) {
	if (mpManager->IsCursorCaptured()) {
		sint32 pos = GetNearestPosFromX(x);

		SetCaretPosX(pos, true);
	}
}

bool ATUITextEdit::OnKeyDown(const ATUIKeyEvent& event) {
	if (ATUIWidget::OnKeyDown(event))
		return true;

	switch(event.mVirtKey) {
		case kATUIVK_Home:
			SetCaretPosX(0, mpManager->IsKeyDown(kATUIVK_Shift));
			return true;

		case kATUIVK_End:
			SetCaretPosX((sint32)mText.size(), mpManager->IsKeyDown(kATUIVK_Shift));
			return true;

		case kATUIVK_Left:
			SetCaretPosX(mCaretPosX - 1, mpManager->IsKeyDown(kATUIVK_Shift));
			return true;

		case kATUIVK_Right:
			SetCaretPosX(mCaretPosX + 1, mpManager->IsKeyDown(kATUIVK_Shift));
			return true;

		case kATUIVK_Back:
			if (mAnchorPosX >= 0) {
				Delete();
			} else if (mCaretPosX) {
				mText.erase(mCaretPosX - 1, 1);
				SetCaretPosX(mCaretPosX - 1, false);
				Invalidate();
			}
			return true;

		case kATUIVK_Delete:
			Delete();
			return true;

		case kATUIVK_Return:
			if (mReturnPressedEvent)
				mReturnPressedEvent();
			return true;
	}

	return false;
}

bool ATUITextEdit::OnKeyUp(const ATUIKeyEvent& event) {
	return ATUIWidget::OnKeyUp(event);
}

bool ATUITextEdit::OnChar(const ATUICharEvent& event) {
	uint32 ch = event.mCh;

	if (ch >= 0x20 && ch != 0x7F) {
		mText.insert(mText.begin() + mCaretPosX, (wchar_t)ch);
		++mCaretPosX;
		UpdateCaretPixelX();
		Invalidate();
	}

	return false;
}

void ATUITextEdit::OnCreate() {
	mpFont = mpManager->GetThemeFont(kATUIThemeFont_Default);

	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);
	mFontHeight = metrics.mAscent + metrics.mDescent;
	mFontAscent = metrics.mAscent;
}

void ATUITextEdit::OnDestroy() {
	mbFocused = false;
	mCaretTimer.Stop();
}

void ATUITextEdit::OnKillFocus() {
	mbFocused = false;

	if (mbCaretOn) {
		mbCaretOn = false;

		Invalidate();
	}
}

void ATUITextEdit::OnSetFocus() {
	mbFocused = true;

	mCaretTimer.SetPeriodic(this, 500);
	TurnCaretOn();
}

void ATUITextEdit::TimerCallback() {
	mbCaretOn = !mbCaretOn;
	Invalidate();
}

void ATUITextEdit::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	VDDisplayTextRenderer *tr = rdr.GetTextRenderer();

	if (rdr.PushViewport(vdrect32(mTextMarginX, mTextMarginY, w - mTextMarginX, h - mTextMarginY), mTextMarginX, mTextMarginY)) {
		if (mAnchorPosX >= 0) {
			sint32 x1 = mAnchorPixelX;
			sint32 x2 = mCaretPixelX;

			rdr.SetColorRGB(mHighlightBackgroundColor);

			if (x1 < x2)
				rdr.FillRect(x1, 0, x2 - x1, mFontHeight);
			else if (x1 > x2)
				rdr.FillRect(x2, 0, x1 - x2, mFontHeight);
		}

		tr->SetFont(mpFont);
		tr->SetColorRGB(mTextColor);
		tr->SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
		tr->SetPosition(0, 0);

		mGlyphPlacements.clear();
		mpFont->ShapeText(mText.data(), mText.size(), mGlyphPlacements, NULL, NULL, NULL);

		if (mAnchorPosX >= 0) {
			uint32 p1 = std::min(mAnchorPosX, mCaretPosX);
			uint32 plen = std::max(mAnchorPosX, mCaretPosX) - p1;

			GlyphPlacements::iterator it1 = mGlyphPlacements.begin();
			GlyphPlacements::iterator itDst = it1;
			GlyphPlacements::iterator it2 = mGlyphPlacements.end();

			mGlyphPlacements2.clear();
			for(; it1 != it2; ++it1) {
				if ((uint32)it1->mOriginalOffset - p1 >= plen)
					*itDst++ = *it1;
				else
					mGlyphPlacements2.push_back(*it1);
			}

			tr->DrawPrearrangedText(0, mFontAscent, mGlyphPlacements.data(), (uint32)(itDst - mGlyphPlacements.begin()));

			tr->SetColorRGB(mHighlightTextColor);
			tr->DrawPrearrangedText(0, mFontAscent, mGlyphPlacements2.data(), (uint32)mGlyphPlacements2.size());
		} else {
			tr->DrawPrearrangedText(0, mFontAscent, mGlyphPlacements.data(), (uint32)mGlyphPlacements.size());
		}

		// draw caret
		if (mbFocused && mbCaretOn)
			rdr.FillRect(mCaretPixelX, 0, 1, mFontHeight);

		rdr.PopViewport();
	}
}

sint32 ATUITextEdit::GetNearestPosFromX(sint32 x) const {
	vdfastvector<VDDisplayFontGlyphPlacement> glyphPlacements;

	vdpoint32 nextPos;
	mpFont->ShapeText(mText.data(), mText.size(), glyphPlacements, NULL, NULL, &nextPos);

	sint32 bestDist = 0x7FFFFFFF;
	uint32 bestPos = 0;

	uint32 n = (uint32)glyphPlacements.size();
	for(uint32 i=0; i<n; ++i) {
		sint32 gx = glyphPlacements[i].mCellX;
		sint32 dist = abs(gx - x);

		if (bestDist > dist) {
			bestDist = dist;
			bestPos = i;
		}
	}

	if (abs(nextPos.x - x) < bestDist)
		return n;

	return bestPos;
}

void ATUITextEdit::SetCaretPosX(sint32 x, bool enableSelection) {
	if (x < 0)
		x = 0;
	else {
		sint32 n = (sint32)mText.size();
		if (x > n)
			x = n;
	}

	if (mCaretPosX == x)
		return;

	if (enableSelection) {
		if (mAnchorPosX < 0) {
			mAnchorPosX = mCaretPosX;
			mAnchorPixelX = mCaretPixelX;
		} else if (mAnchorPosX == x)
			ClearSelection();
	} else {
		ClearSelection();
	}

	mCaretPosX = x;

	mbCaretOn = true;
	mCaretTimer.SetPeriodic(this, 500);

	UpdateCaretPixelX();
}

void ATUITextEdit::UpdateCaretPixelX() {
	if (!mpFont)
		return;

	sint32 px = mpFont->MeasureString(mText.data(), mCaretPosX, false).w;

	if (mCaretPixelX != px) {
		mCaretPixelX = px;
		TurnCaretOn();
		Invalidate();
	}
}

void ATUITextEdit::TurnCaretOn() {
	if (!mbCaretOn) {
		mbCaretOn = true;

		Invalidate();
	}
}
