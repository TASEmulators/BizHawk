#include <stdafx.h>
#include <vd2/system/math.h>
#include <at/atuicontrols/uibutton.h>
#include <at/atuicontrols/uislider.h>
#include <at/atui/uimanager.h>
#include <at/atui/uidrawingutils.h>

ATUISlider::ATUISlider()
	: mMin(0)
	, mMax(0)
	, mPageSize(1)
	, mLineSize(1)
	, mPos(0)
	, mFloatPos(0)
	, mPixelPos(0)
	, mThumbSize(0)
	, mTrackMin(0)
	, mTrackSize(0)
	, mbFrameEnabled(true)
	, mbVertical(true)
	, mbDragging(false)
	, mDragOffset(0)
	, mpButtonLower(NULL)
	, mpButtonRaise(NULL)
{
	SetFillColor(0xD4D0C8);

	mMax = 100;
	mPageSize = 10;
}

ATUISlider::~ATUISlider() {
}

void ATUISlider::SetFrameEnabled(bool enabled) {
	mbFrameEnabled = enabled;
}

void ATUISlider::SetVertical(bool vert) {
	if (mbVertical == vert)
		return;

	mbVertical = vert;
	OnSize();
}

void ATUISlider::SetPos(sint32 pos) {
	SetPosInternal(pos, false);
}

void ATUISlider::SetPageSize(sint32 pageSize) {
	if (mPageSize != pageSize) {
		mPageSize = pageSize;

		OnSize();
	}
}

void ATUISlider::SetRange(sint32 minVal, sint32 maxVal) {
	if (mMin != minVal || mMax != maxVal) {
		mMin = minVal;
		mMax = maxVal;
		
		OnSize();
	}
}

void ATUISlider::OnCreate() {
	ATUIContainer::OnCreate();

	mpButtonLower = new ATUIButton;
	mpButtonLower->AddRef();
	mpButtonLower->SetFrameEnabled(mbFrameEnabled);
	AddChild(mpButtonLower);
	mpButtonLower->OnPressedEvent() = [this] { OnButtonLowerPressed(); };
	mpButtonLower->OnActivatedEvent() = [this] { OnButtonReleased(); };

	mpButtonRaise = new ATUIButton;
	mpButtonRaise->AddRef();
	mpButtonRaise->SetFrameEnabled(mbFrameEnabled);
	AddChild(mpButtonRaise);
	mpButtonRaise->OnPressedEvent() = [this] { OnButtonRaisePressed(); };
	mpButtonRaise->OnActivatedEvent() = [this] { OnButtonReleased(); };

	if (!mbFrameEnabled) {
		mpButtonLower->SetTextColor(0xFFFFFF);
		mpButtonRaise->SetTextColor(0xFFFFFF);
	}

	if (mbVertical) {
		mpButtonLower->SetStockImage(kATUIStockImageIdx_ButtonUp);
		mpButtonRaise->SetStockImage(kATUIStockImageIdx_ButtonDown);
	} else {
		mpButtonLower->SetStockImage(kATUIStockImageIdx_ButtonLeft);
		mpButtonRaise->SetStockImage(kATUIStockImageIdx_ButtonRight);
	}
}

void ATUISlider::OnDestroy() {
	vdsaferelease <<= mpButtonLower;
	vdsaferelease <<= mpButtonRaise;

	ATUIContainer::OnDestroy();
}

void ATUISlider::OnSize() {
	ATUIContainer::OnSize();

	const vdsize32 sz = mClientArea.size();
	sint32 sliderLen;
	sint32 buttonSize;

	if (mbVertical) {
		sliderLen = sz.h;
		buttonSize = sz.w;
	} else {
		sliderLen = sz.w;
		buttonSize = sz.h;
	}

	if (buttonSize * 2 > sliderLen)
		buttonSize = sliderLen >> 1;

	if (mbVertical) {
		mpButtonLower->SetArea(vdrect32(0, 0, sz.w, buttonSize));
		mpButtonRaise->SetArea(vdrect32(0, sz.h - buttonSize, sz.w, sz.h));
	} else {
		mpButtonLower->SetArea(vdrect32(0, 0, buttonSize, sz.h));
		mpButtonRaise->SetArea(vdrect32(sz.w - buttonSize, 0, sz.w, sz.h));
	}

	// compute track length
	const sint32 trackLen = sliderLen - 2 * buttonSize;

	// compute thumb size
	const uint32 range = mMax - mMin + mPageSize;
	sint32 thumbSize = range ? (sint32)(((sint64)trackLen * mPageSize + (range >> 1)) / range) : trackLen;

	if (thumbSize < buttonSize)
		thumbSize = buttonSize;

	if (thumbSize > trackLen)
		thumbSize = trackLen;

	mThumbSize = thumbSize;
	mTrackMin = buttonSize;
	mTrackSize = trackLen - thumbSize;

	// recompute pixel position
	mPixelPos = mTrackMin + VDRoundToInt32(mFloatPos * (float)mTrackSize);

	Invalidate();
}

void ATUISlider::OnMouseDownL(sint32 x, sint32 y) {
	sint32 offset = (mbVertical ? y : x) - mPixelPos;

	if (offset < 0) {
		ATUITriggerBinding binding = {};
		binding.mVk = kATUIVK_LButton;
		binding.mAction = kActionPagePrior;
		mpManager->BeginAction(this, binding);
	} else if (offset < mThumbSize) {
		CaptureCursor();
		mbDragging = true;

		if (mbVertical)
			mDragOffset = y - mPixelPos;
		else
			mDragOffset = x - mPixelPos;

		Invalidate();
	} else {
		ATUITriggerBinding binding = {};
		binding.mVk = kATUIVK_LButton;
		binding.mAction = kActionPageNext;
		mpManager->BeginAction(this, binding);
	}
}

void ATUISlider::OnMouseMove(sint32 x, sint32 y) {
	if (!mbDragging)
		return;

	sint32 ppos = (y - mDragOffset) - mTrackMin;

	if (ppos > mTrackSize)
		ppos = mTrackSize;

	if (ppos < 0)
		ppos = 0;

	if (mPixelPos != ppos) {
		sint32 pos = mMin;
		
		if (mMax != mMin && mTrackSize)
			pos += (sint32)(((sint64)ppos * (mMax - mMin) + (mTrackSize >> 1)) / mTrackSize);

		mPixelPos = mTrackMin + ppos;
		mFloatPos = mTrackSize ? ppos / (float)mTrackSize : 0.0f;

		Invalidate();

		if (mPos != pos) {
			mPos = pos;

			if (mpValueChangedFn)
				mpValueChangedFn(pos);
		}
	}
}

void ATUISlider::OnMouseUpL(sint32 x, sint32 y) {
	if (mbDragging) {
		OnMouseMove(x, y);
		OnCaptureLost();
		ReleaseCursor();
	} else {
		mpManager->EndAction(kATUIVK_LButton);
	}
}

void ATUISlider::OnActionStart(uint32 trid) {
	switch(trid) {
		case kActionLinePrior:
		case kActionLineNext:
		case kActionPagePrior:
		case kActionPageNext:
			OnActionRepeat(trid);
			break;
	}

	return ATUIContainer::OnActionStart(trid);
}

void ATUISlider::OnActionRepeat(uint32 trid) {
	switch(trid) {
		case kActionLinePrior:
			SetPosInternal(mPos - mLineSize, true);
			break;

		case kActionLineNext:
			SetPosInternal(mPos + mLineSize, true);
			break;

		case kActionPagePrior:
			SetPosInternal(mPos - mPageSize, true);
			break;

		case kActionPageNext:
			SetPosInternal(mPos + mPageSize, true);
			break;
	}
}

void ATUISlider::OnCaptureLost() {
	mbDragging = false;
	Invalidate();
}

void ATUISlider::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	vdrect32 rThumb;

	if (mbVertical)
		rThumb.set(0, mPixelPos, w, mPixelPos + mThumbSize);
	else
		rThumb.set(mPixelPos, 0, mPixelPos + mThumbSize, h);

	if (mbFrameEnabled)
		ATUIDraw3DRect(rdr, rThumb, mbDragging);
	else {
		rdr.SetColorRGB(0xFFFFFF);

		sint32 w3 = rThumb.width() / 3;
		rdr.FillRect(rThumb.left + w3, rThumb.top, rThumb.width() - w3*2, rThumb.height());
	}

	ATUIContainer::Paint(rdr, w, h);
}

void ATUISlider::OnButtonLowerPressed() {
	ATUITriggerBinding binding = {};
	binding.mVk = kATUIVK_LButton;
	binding.mAction = kActionLinePrior;
	mpManager->BeginAction(this, binding);
}

void ATUISlider::OnButtonRaisePressed() {
	ATUITriggerBinding binding = {};
	binding.mVk = kATUIVK_LButton;
	binding.mAction = kActionLineNext;
	mpManager->BeginAction(this, binding);
}

void ATUISlider::OnButtonReleased() {
	mpManager->EndAction(kATUIVK_LButton);
}

void ATUISlider::SetPosInternal(sint32 pos, bool notify) {
	if (pos > mMax)
		pos = mMax;

	if (pos < mMin)
		pos = mMin;

	if (mPos == pos)
		return;

	mPos = pos;

	if (mFloatPos != (float)pos) {
		mFloatPos = (float)pos;

		sint32 range = mMax - mMin;
		sint32 ppos = mTrackMin + (range ? (sint32)(((sint64)mTrackSize * (pos - mMin) + (range >> 1)) / range) : 0);

		if (mPixelPos != ppos) {
			mPixelPos = ppos;

			Invalidate();
		}
	}

	if (notify) {
		if (mpValueChangedFn)
			mpValueChangedFn(pos);
	}
}
