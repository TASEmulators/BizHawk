//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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
#include <initializer_list>
#include <vd2/system/math.h>
#include <vd2/system/vdalloc.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atui/uicontainer.h>
#include <at/atui/uimanager.h>
#include <at/atuicontrols/uilabel.h>
#include <at/atuicontrols/uibutton.h>
#include "uisettingswindow.h"

///////////////////////////////////////////////////////////////////////////

void *ATUISetting::AsInterface(uint32 id) {
	if (id == ATUISetting::kTypeID)
		return static_cast<ATUISetting *>(this);

	return nullptr;
}

bool ATUISetting::IsDeferred() const {
	return false;
}

void ATUISetting::Read() {}
void ATUISetting::Write() {}

void ATUISetting::SetValueDynamic() {
	mbValueDynamic = true;
}

bool ATUISetting::IsNameDynamic() const {
	return mpDynamicNameFn;
}

void ATUISetting::GetDynamicName(VDStringW& name) const {
	mpDynamicNameFn(name);
}

void ATUISetting::SetDynamicNameFn(const vdfunction<void(VDStringW&)>& fn) {
	mpDynamicNameFn = fn;
}

///////////////////////////////////////////////////////////////////////////

ATUIBoolSetting::ATUIBoolSetting(const wchar_t *name)
	: ATUISetting(name)
	, mValue(false)
{
}

void *ATUIBoolSetting::AsInterface(uint32 id) {
	if (id == ATUIBoolSetting::kTypeID)
		return static_cast<ATUIBoolSetting *>(this);

	return ATUISetting::AsInterface(id);
}

void ATUIBoolSetting::Read() {
	if (mpGetter)
		mValue = mpGetter();
}

void ATUIBoolSetting::Write() {
	if (mpSetter)
		mpSetter(mValue);
}

void ATUIBoolSetting::SetGetter(const vdfunction<bool()>& fn) { mpGetter = fn; }
void ATUIBoolSetting::SetSetter(const vdfunction<void(bool)>& fn) { mpSetter = fn; }
void ATUIBoolSetting::SetImmediateSetter(const vdfunction<void(bool)>& fn) { mpImmediateSetter = fn; }

void ATUIBoolSetting::SetValue(bool value) {
	if (mValue == value)
		return;

	mValue = value;

	if (mpImmediateSetter)
		mpImmediateSetter(mValue);
}

///////////////////////////////////////////////////////////////////////////

ATUIIntSetting::ATUIIntSetting(const wchar_t *name, sint32 minVal, sint32 maxVal)
	: ATUISetting(name)
	, mValue(0)
	, mMinVal(minVal)
	, mMaxVal(maxVal)
{
}

void *ATUIIntSetting::AsInterface(uint32 id) {
	if (id == ATUIIntSetting::kTypeID)
		return static_cast<ATUIIntSetting *>(this);

	return ATUISetting::AsInterface(id);
}

void ATUIIntSetting::Read() {
	if (mpGetter)
		mValue = mpGetter();
}

void ATUIIntSetting::Write() {
	if (mpSetter)
		mpSetter(mValue);
}

void ATUIIntSetting::SetGetter(const vdfunction<sint32()>& fn) { mpGetter = fn; }
void ATUIIntSetting::SetSetter(const vdfunction<void(sint32)>& fn) { mpSetter = fn; }
void ATUIIntSetting::SetImmediateSetter(const vdfunction<void(sint32)>& fn) { mpImmediateSetter = fn; }

void ATUIIntSetting::SetValue(sint32 value) {
	if (mValue == value)
		return;

	mValue = value;

	if (mpImmediateSetter)
		mpImmediateSetter(mValue);
}

///////////////////////////////////////////////////////////////////////////

ATUIEnumSetting::ATUIEnumSetting(const wchar_t *name, const ATUIEnumValue *values, uint32 n)
	: ATUISetting(name)
	, mValues(values, values + n)
	, mValueIndex(0)
{
}

ATUIEnumSetting::ATUIEnumSetting(const wchar_t *name, std::initializer_list<ATUIEnumValue> il)
	: ATUISetting(name)
	, mValues(il.begin(), il.end())
	, mValueIndex(0)
{
}

void *ATUIEnumSetting::AsInterface(uint32 id) {
	if (id == ATUIEnumSetting::kTypeID)
		return static_cast<ATUIEnumSetting *>(this);

	return ATUISetting::AsInterface(id);
}

bool ATUIEnumSetting::IsDeferred() const {
	return mpSetter;
}

void ATUIEnumSetting::Read() {
	if (mpGetter) {
		sint32 value = mpGetter();

		sint32 index = 0;
		for(const auto& entry : mValues) {
			if (entry.mValue == value) {
				mValueIndex = index;
				break;
			}

			++index;
		}
	}
}

void ATUIEnumSetting::Write() {
	if ((uint32)mValueIndex < mValues.size()) {
		if (mpSetter)
			mpSetter(mValues[mValueIndex].mValue);
	}
}

void ATUIEnumSetting::SetGetter(const vdfunction<sint32()>& fn) { mpGetter = fn; }
void ATUIEnumSetting::SetSetter(const vdfunction<void(sint32)>& fn) { mpSetter = fn; }
void ATUIEnumSetting::SetImmediateSetter(const vdfunction<void(sint32)>& fn) { mpImmediateSetter = fn; }

void ATUIEnumSetting::SetValue(sint32 value) {
	if (mValueIndex == value)
		return;

	mValueIndex = value;

	if ((uint32)mValueIndex < mValues.size()) {
		if (mpImmediateSetter)
			mpImmediateSetter(mValues[value].mValue);
	}
}

/////////////////////////////////////////////////////////////////////////

void *ATUISubScreenSetting::AsInterface(uint32 id) {
	if (id == ATUISubScreenSetting::kTypeID)
		return static_cast<ATUISubScreenSetting *>(this);

	return ATUISetting::AsInterface(id);
}

void ATUISubScreenSetting::BuildScreen(IATUISettingsScreen **screen) {
	mpBuilder(screen);
}

/////////////////////////////////////////////////////////////////////////

void *ATUIActionSetting::AsInterface(uint32 id) {
	if (id == ATUIActionSetting::kTypeID)
		return static_cast<ATUIActionSetting *>(this);

	return ATUISetting::AsInterface(id);
}

bool ATUIActionSetting::Activate() {
	return mpAction();
}

vdrefptr<ATUIFutureWithResult<bool>> ATUIActionSetting::ActivateAsync() {
	return mpAsyncAction();
}

/////////////////////////////////////////////////////////////////////////

class ATUIBarSettingWindow final : public ATUIWidget {
public:
	ATUIBarSettingWindow();

	void SetValue(sint32 val);
	void SetRange(sint32 mn, sint32 mx);

public:
	void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) override;

	uint32 mMin;
	uint32 mMax;
	uint32 mValue;
	uint32 mRange;
};

ATUIBarSettingWindow::ATUIBarSettingWindow()
	: mMin(0x80000000U)
	, mMax(0x80000000U)
	, mValue(0x80000000U)
	, mRange(0)
{
}

void ATUIBarSettingWindow::SetValue(sint32 val) {
	const uint32 uval = val < 0 ? (uint32)((val + 0x7fffffff) + 1) : (uint32)val + 0x80000000U;

	if (mValue != uval) {
		mValue = uval;
		Invalidate();
	}
}

void ATUIBarSettingWindow::SetRange(sint32 mn, sint32 mx) {
	const uint32 umn = mn < 0 ? (uint32)((mn + 0x7fffffff) + 1) : (uint32)mn + 0x80000000U;
	const uint32 umx = mx < 0 ? (uint32)((mx + 0x7fffffff) + 1) : (uint32)mx + 0x80000000U;

	if (mMin != umn || mMax != umx) {
		mMin = umn;
		mMax = umx;
		mRange = umn > umx ? umn - umx : umx - umn;

		Invalidate();
	}
}

void ATUIBarSettingWindow::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	if (!mRange)
		return;

	if (mValue <= mMin)
		return;

	uint32 offset;

	if (mValue >= mMax)
		offset = mRange;
	else if (mMax > mMin)
		offset = mValue - mMin;
	else
		offset = mMin - mValue;

	const sint32 barw = (sint32)(((uint64)w * offset + (mRange >> 1)) / mRange);
	const uint32 grn = (sint32)(((uint64)255 * offset + (mRange >> 1)) / mRange);

	rdr.SetColorRGB(0xFFFF00 - (grn << 8));

	sint32 h3 = h / 3;
	rdr.FillRect(0, h3, barw, h - 2*h3);
}

/////////////////////////////////////////////////////////////////////////

class ATUISettingWindow final : public ATUIContainer {
public:
	enum {
		kActionActivate = kActionCustom,
		kActionTurnOn,
		kActionTurnOff,
		kActionDecrease,
		kActionIncrease
	};

	ATUISettingWindow(ATUISetting *setting);

	void SetSelectedIndex(int index);

	void SetOnSubScreen(const vdfunction<void(ATUISubScreenSetting *)>& fn) {
		mpOnSubScreen = fn;
	}

	void SetOnAction(const vdfunction<void(ATUIActionSetting *)>& fn) {
		mpOnAction = fn;
	}

	void SetOnDynamicUpdate(const vdfunction<void()>& fn) {
		mpOnDynamicUpdate = fn;
	}

	void UpdateDynamic();

public:
	void OnCreate() override;
	void OnSize() override;
	void Paint(IVDDisplayRenderer&r, sint32 w, sint32 h) override;
	void OnMouseDownL(sint32 x, sint32 y) override;
	void OnMouseUpL(sint32 x, sint32 y) override;
	void OnMouseLeave() override;
	void OnActionStart(uint32 id) override;
	void OnActionRepeat(uint32 id) override;

private:
	void OnPrevDown();
	void OnPrevUp();
	void OnNextDown();
	void OnNextUp();
	void UpdateLabels();

	vdrefptr<IVDDisplayFont> mpFont;
	vdrefptr<ATUILabel> mpLabelName;
	vdrefptr<ATUILabel> mpLabelSetting;
	vdrefptr<ATUIButton> mpButtonPrev;
	vdrefptr<ATUIButton> mpButtonNext;
	vdrefptr<ATUIBarSettingWindow> mpBar;

	ATUISetting *mpSetting;
	ATUIBoolSetting *mpBoolSetting;
	ATUIIntSetting *mpIntSetting;
	ATUIEnumSetting *mpEnumSetting;
	ATUIActionSetting *mpActionSetting;
	ATUISubScreenSetting *mpSubScreenSetting;

	sint32 mSelectedIndex;
	sint32 mMinVal;
	sint32 mMaxVal;
	bool mbDirty;
	bool mbMouseActivatePending;

	vdfunction<void(ATUISubScreenSetting *)> mpOnSubScreen;
	vdfunction<void(ATUIActionSetting *)> mpOnAction;
	vdfunction<void()> mpOnDynamicUpdate;
};

ATUISettingWindow::ATUISettingWindow(ATUISetting *setting)
	: mpSetting(setting)
	, mpBoolSetting(vdpoly_cast<ATUIBoolSetting *>(setting))
	, mpIntSetting(vdpoly_cast<ATUIIntSetting *>(setting))
	, mpEnumSetting(vdpoly_cast<ATUIEnumSetting *>(setting))
	, mpActionSetting(vdpoly_cast<ATUIActionSetting *>(setting))
	, mpSubScreenSetting(vdpoly_cast<ATUISubScreenSetting *>(setting))
	, mbDirty(false)
	, mbMouseActivatePending(false)
{
	SetAlphaFillColor(0);

	mSelectedIndex = 0;
	mMinVal = 0;
	mMaxVal = 0;

	if (mpEnumSetting)
		mMaxVal = (sint32)mpEnumSetting->GetValueCount() - 1;
	else if (mpBoolSetting)
		mMaxVal = 1;
	else if (mpIntSetting) {
		mMinVal = mpIntSetting->GetMinValue();
		mMaxVal = mpIntSetting->GetMaxValue();
	}
}

void ATUISettingWindow::SetSelectedIndex(int index) {
	if (index < mMinVal)
		index = mMinVal;

	if (index > mMaxVal)
		index = mMaxVal;

	if (mSelectedIndex == index)
		return;

	mSelectedIndex = index;

	if (mpEnumSetting)
		mpEnumSetting->SetValue(index);
	else if (mpIntSetting)
		mpIntSetting->SetValue(index);
	else if (mpBoolSetting)
		mpBoolSetting->SetValue(index != 0);

	if (mpSetting->IsDeferred() && !mbDirty) {
		mbDirty = true;

		if (mpLabelName)
			mpLabelName->SetTextColor(0xFF0000);

		if (mpLabelSetting)
			mpLabelSetting->SetTextColor(0xFF0000);
	}

	UpdateLabels();

	if (!mpSetting->IsDeferred() && mpOnDynamicUpdate)
		mpOnDynamicUpdate();
}

void ATUISettingWindow::UpdateDynamic() {
	if (mpSetting->IsNameDynamic()) {
		VDStringW name;
		mpSetting->GetDynamicName(name);
		mpLabelName->SetText(name.c_str());
	}

	if (mpSetting->IsValueDynamic() && !mbDirty) {
		if (mpEnumSetting) {
			mpSetting->Read();

			const sint32 newValue = mpEnumSetting->GetValue();

			if (mSelectedIndex != newValue) {
				mSelectedIndex = newValue;
				UpdateLabels();
			}
		} else if (mpBoolSetting) {
			mpSetting->Read();

			const sint32 newSetting = mpBoolSetting->GetValue() ? 1 : 0;

			if (mSelectedIndex != newSetting) {
				mSelectedIndex = newSetting;
				UpdateLabels();
			}
		}
	}
}

void ATUISettingWindow::OnCreate() {
	mpFont = mpManager->GetThemeFont(kATUIThemeFont_Default);

	mpLabelName = new ATUILabel;
	mpLabelName->SetHitTransparent(true);
	mpLabelName->SetAlphaFillColor(0);
	mpLabelName->SetTextColor(0xFFFFFF);

	if (mpSetting->IsNameDynamic()) {
		VDStringW name;
		mpSetting->GetDynamicName(name);
		mpLabelName->SetText(name.c_str());
	} else
		mpLabelName->SetText(mpSetting->GetName());

	AddChild(mpLabelName);

	if (mpEnumSetting || mpBoolSetting || mpIntSetting) {
		mpButtonPrev = new ATUIButton;
		mpButtonPrev->SetFrameEnabled(false);
		mpButtonPrev->SetTextColor(0xFFFFFF);
		mpButtonPrev->SetStockImage(kATUIStockImageIdx_ButtonLeft);
		mpButtonPrev->OnPressedEvent() = [this] { OnPrevDown(); };
		mpButtonPrev->OnActivatedEvent() = [this] { OnPrevUp(); };
		AddChild(mpButtonPrev);
	}

	if (mpEnumSetting || mpBoolSetting) {
		mpLabelSetting = new ATUILabel;
		mpLabelSetting->SetHitTransparent(true);
		mpLabelSetting->SetAlphaFillColor(0);
		mpLabelSetting->SetTextColor(0xFFFFFF);
		mpLabelSetting->SetTextAlign(ATUILabel::kAlignCenter);
		AddChild(mpLabelSetting);
	} else if (mpIntSetting) {
		mpBar = new ATUIBarSettingWindow;
		mpBar->SetHitTransparent(true);
		mpBar->SetAlphaFillColor(0);
		mpBar->SetRange(mpIntSetting->GetMinValue(), mpIntSetting->GetMaxValue());
		AddChild(mpBar);
	}

	if (mpEnumSetting || mpSubScreenSetting || mpBoolSetting || mpIntSetting) {
		mpButtonNext = new ATUIButton;
		mpButtonNext->SetTextColor(0xFFFFFF);
		mpButtonNext->SetFrameEnabled(false);
		mpButtonNext->SetStockImage(kATUIStockImageIdx_ButtonRight);
		mpButtonNext->OnPressedEvent() = [this] { OnNextDown(); };
		mpButtonNext->OnActivatedEvent() = [this] { OnNextUp(); };
		AddChild(mpButtonNext);
	}

	OnSize();

	if (mpIntSetting) {
		BindAction(kATUIVK_Left, kActionDecrease);
		BindAction(kATUIVK_UILeft, kActionDecrease);
		BindAction(kATUIVK_Right, kActionIncrease);
		BindAction(kATUIVK_UIRight, kActionIncrease);
	} else {
		if (mpButtonPrev) {
			BindAction(kATUIVK_Left, ATUIButton::kActionActivate, 0, mpButtonPrev->GetInstanceId());
			BindAction(kATUIVK_UILeft, ATUIButton::kActionActivate, 0, mpButtonPrev->GetInstanceId());
		}

		if (mpButtonNext) {
			BindAction(kATUIVK_Right, ATUIButton::kActionActivate, 0, mpButtonNext->GetInstanceId());
			BindAction(kATUIVK_UIRight, ATUIButton::kActionActivate, 0, mpButtonNext->GetInstanceId());
		}
	}

	BindAction(kATUIVK_UIAccept, kActionActivate);

	if (mpBoolSetting) {
		BindAction(kATUIVK_Left, kActionTurnOff);
		BindAction(kATUIVK_UILeft, kActionTurnOff);
		BindAction(kATUIVK_Right, kActionTurnOn);
		BindAction(kATUIVK_UIRight, kActionTurnOn);
	}

	mSelectedIndex = 0;

	if (mpEnumSetting)
		mSelectedIndex = mpEnumSetting->GetValue();
	else if (mpIntSetting)
		mSelectedIndex = mpIntSetting->GetValue();
	else if (mpBoolSetting)
		mSelectedIndex = mpBoolSetting->GetValue();

	UpdateLabels();
}

void ATUISettingWindow::OnSize() {
	const vdsize32& sz = GetArea().size();

	if (mpLabelName) {
		vdrect32 rLabel(0, 0, sz.w/2, sz.h);

		if (!mpButtonPrev) {
			if (!mpLabelSetting)
				rLabel.right = sz.w - sz.h;
			else if (mpButtonNext)
				rLabel.right += sz.h;
			else
				rLabel.right = sz.w;
		}

		mpLabelName->SetArea(rLabel);
	}

	if (mpButtonPrev)
		mpButtonPrev->SetArea(vdrect32(sz.w/2, 0, sz.w/2 + sz.h, sz.h));

	if (mpLabelSetting || mpBar) {
		const vdrect32 rSetting(sz.w/2 + sz.h, 0, sz.w - sz.h, sz.h);

		if (mpLabelSetting)
			mpLabelSetting->SetArea(rSetting);
		else
			mpBar->SetArea(rSetting);
	}

	if (mpButtonNext)
		mpButtonNext->SetArea(vdrect32(sz.w - sz.h, 0, sz.w, sz.h));
}

void ATUISettingWindow::Paint(IVDDisplayRenderer& r, sint32 w, sint32 h) {
	ATUIContainer::Paint(r, w, h);
}

void ATUISettingWindow::OnMouseDownL(sint32 x, sint32 y) {
	mbMouseActivatePending = true;
}

void ATUISettingWindow::OnMouseUpL(sint32 x, sint32 y) {
	if (mbMouseActivatePending) {
		if (mpBoolSetting || mpSubScreenSetting || mpActionSetting || mbDirty)
			OnActionStart(kActionActivate);

		mbMouseActivatePending = false;
	}
}

void ATUISettingWindow::OnMouseLeave() {
	mbMouseActivatePending = false;
}

void ATUISettingWindow::OnActionStart(uint32 id) {
	switch(id) {
		case kActionActivate:
			if (mpSetting->IsDeferred() && mbDirty) {
				mbDirty = false;

				mpSetting->Write();

				if (mpLabelName)
					mpLabelName->SetTextColor(0xFFFFFF);

				if (mpLabelSetting)
					mpLabelSetting->SetTextColor(0xFFFFFF);

				if (mpOnDynamicUpdate)
					mpOnDynamicUpdate();
			} else if (mpBoolSetting)
				SetSelectedIndex(mSelectedIndex ^ 1);
			else if (mpSubScreenSetting || mpActionSetting)
				OnNextUp();
			break;

		case kActionTurnOn:
			SetSelectedIndex(1);
			break;

		case kActionTurnOff:
			SetSelectedIndex(0);
			break;

		case kActionIncrease:
		case kActionDecrease:
			return OnActionRepeat(id);
	}

	ATUIContainer::OnActionStart(id);
}

void ATUISettingWindow::OnActionRepeat(uint32 id) {
	switch(id) {
		case kActionDecrease:
			SetSelectedIndex(mSelectedIndex - 1);
			break;

		case kActionIncrease:
			SetSelectedIndex(mSelectedIndex + 1);
			break;
	}

	return ATUIContainer::OnActionRepeat(id);
}

void ATUISettingWindow::OnPrevDown() {
	if (mpIntSetting) {
		ATUITriggerBinding binding = {};
		binding.mVk = kATUIVK_UILeft;
		binding.mAction = kActionDecrease;
		mpManager->BeginAction(this, binding);
	}
}

void ATUISettingWindow::OnPrevUp() {
	if (mpBoolSetting)
		SetSelectedIndex(mSelectedIndex ^ 1);
	else if (mpIntSetting)
		mpManager->EndAction(kATUIVK_UILeft);
	else
		SetSelectedIndex(mSelectedIndex - 1);
}

void ATUISettingWindow::OnNextDown() {
	if (mpIntSetting) {
		ATUITriggerBinding binding = {};
		binding.mVk = kATUIVK_UIRight;
		binding.mAction = kActionIncrease;
		mpManager->BeginAction(this, binding);
	}
}

void ATUISettingWindow::OnNextUp() {
	if (mpActionSetting) {
		if (mpOnAction)
			mpOnAction(mpActionSetting);

		if (mpOnDynamicUpdate)
			mpOnDynamicUpdate();
	} else if (mpSubScreenSetting) {
		if (mpOnSubScreen)
			mpOnSubScreen(mpSubScreenSetting);
	} else if (mpBoolSetting) {
		SetSelectedIndex(mSelectedIndex ^ 1);
	} else if (mpIntSetting) {
		mpManager->EndAction(kATUIVK_UIRight);
	} else {
		SetSelectedIndex(mSelectedIndex + 1);
	}
}

void ATUISettingWindow::UpdateLabels() {
	if (mpLabelSetting) {
		if (mpEnumSetting)
			mpLabelSetting->SetText(mpEnumSetting->GetValueName(mSelectedIndex));
		else if (mpBoolSetting)
			mpLabelSetting->SetText(mSelectedIndex ? L"On" : L"Off");
		else
			mpLabelSetting->SetTextF(L"%d", mSelectedIndex);
	} else if (mpBar)
		mpBar->SetValue(mSelectedIndex);

	if (mpButtonPrev && !mpBoolSetting)
		mpButtonPrev->SetVisible(mSelectedIndex > mMinVal);

	if (mpButtonNext && mpEnumSetting)
		mpButtonNext->SetVisible(mSelectedIndex < mMaxVal);
}

///////////////////////////////////////////////////////////////////////////

class ATUIScrollPane : public ATUIContainer {
public:
	ATUIScrollPane() {
		mbFastClip = true;
		SetHitTransparent(true);
	}
};

///////////////////////////////////////////////////////////////////////////

ATUISettingsWindow::ATUISettingsWindow()
	: mbHeaderActivated(false)
	, mbHeaderHighlighted(false)
	, mScrollAccum(0)
	, mTotalHeight(0)
{
	mbFastClip = false;

	SetAlphaFillColor(0xE0202020);
	SetCursorImage(kATUICursorImage_Arrow);

	BindAction(kATUIVK_Up, kActionUp);
	BindAction(kATUIVK_UIUp, kActionUp);
	BindAction(kATUIVK_Down, kActionDown);
	BindAction(kATUIVK_UIDown, kActionDown);
	BindAction(kATUIVK_UIReject, kActionCancel);
	BindAction(kATUIVK_Escape, kActionCancel);
}

void ATUISettingsWindow::SetSettingsScreen(IATUISettingsScreen *screen) {
	if (mpCurrentScreen == screen)
		return;

	// tear down current screen and settings
	DestroyScreen();

	mpCurrentScreen = screen;
	mCurrentVPos = 0;

	BuildScreen();
}

void ATUISettingsWindow::SetCaption(const wchar_t *caption) {
	if (mCaption != caption) {
		mCaption = caption;

		Invalidate();
	}
}

void ATUISettingsWindow::AddSetting(ATUISetting *setting) {
	mSettings.push_back( { setting, mCurrentVPos } );

	mCurrentVPos += 2;
}

void ATUISettingsWindow::AddSeparator() {
	++mCurrentVPos;
}

void ATUISettingsWindow::SetOnDestroy(const vdfunction<void()>& fn) {
	mpOnDestroy = fn;
}

void ATUISettingsWindow::SetSelectedIndex(int index, bool scroll) {
	if (index < 0)
		index = -1;
	else if ((unsigned)index >= mSettings.size())
		index = (int)mSettings.size() - 1;

	if (mSelectedIndex == index)
		return;

	mSelectedIndex = index;

	if (mpSelectionFill) {
		if (index < 0) {
			mpSelectionFill->SetVisible(false);
		} else {
			mpSelectionFill->SetVisible(true);

			const sint32 y = (mSettings[index].mVPos * mRowHeight) >> 1;
			mpSelectionFill->SetArea(vdrect32(0, y, GetArea().width(), y + mRowHeight));

			if (scroll) {
				sint32 cury = mpScrollPane->GetClientOrigin().y;
				sint32 viewh =  mpScrollPane->GetClientArea().height();

				if (y < cury) {
					mpScrollPane->SetClientOrigin(vdpoint32(0, y));
					mpSlider->SetPos(y);
				} else if (y + mRowHeight > cury + viewh) {
					const sint32 newPos = std::max<sint32>(0, y + mRowHeight - viewh);

					mpScrollPane->SetClientOrigin(vdpoint32(0, newPos));
					mpSlider->SetPos(newPos);
				}
			}
		}
	}

	if (index < 0)
		Focus();
	else
		mSettingWindows[index]->Focus();
}

void ATUISettingsWindow::OnCreate() {
	mpFont = mpManager->GetThemeFont(kATUIThemeFont_Default);

	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);
	mRowHeight = metrics.mAscent + metrics.mDescent + 4;
	mHeaderBaseline = metrics.mAscent;
	mHeaderHeight = mRowHeight + 2;

	mpScrollPane = new ATUIScrollPane;
	AddChild(mpScrollPane);

	mpSlider = new ATUISlider;
	mpSlider->SetLineSize(mRowHeight);
	mpSlider->SetFrameEnabled(false);
	mpSlider->SetAlphaFillColor(0);
	mpSlider->SetOnValueChanged(
		[this](sint32 pos) {
			mpScrollPane->SetClientOrigin(vdpoint32(0, pos));
		}
	);
	AddChild(mpSlider);

	OnSize();

	// if we currently have a screen, build it now
	BuildScreen();

	mpSelectionFill = new ATUILabel;
	mpSelectionFill->SetFillColor(0xFF0034D0);
	mpScrollPane->AddChild(mpSelectionFill);
	mpScrollPane->SendToBack(mpSelectionFill);

	mSelectedIndex = -1;
	SetSelectedIndex(0, false);

	mpManager->AddTrackingWindow(this);
}

void ATUISettingsWindow::OnDestroy() {
	mpPendingResult.clear();

	SetSettingsScreen(nullptr);

	while(!mScreenStack.empty()) {
		mScreenStack.back().mpScreen->Release();
		mScreenStack.pop_back();
	}

	vdsaferelease <<= mpSelectionFill, mpScrollPane, mpSlider;

	if (mpOnDestroy)
		mpOnDestroy();

	ATUIContainer::OnDestroy();
}

void ATUISettingsWindow::OnSize() {
	ATUIContainer::OnSize();

	vdrect32 r = GetClientArea();
	r.top += mHeaderHeight;

	vdrect32 rScroll = r;
	if (mpSlider)
		rScroll.right -= mpManager->GetSystemMetrics().mVertSliderWidth;

	vdrect32 rSlider = rScroll;
	rSlider.left = rScroll.right;
	rSlider.right = r.right;

	if (mpScrollPane) {
		mpScrollPane->SetArea(rScroll);

		for(ATUIWidget *w : mSettingWindows) {
			vdrect32 r2 = w->GetArea();
			r2.right = rScroll.right;
			w->SetArea(r2);
		}
	}

	if (mpSlider) {
		mpSlider->SetArea(rSlider);

		RecomputeSlider();
	}
}

void ATUISettingsWindow::OnSetFocus() {
	if ((uint32)mSelectedIndex < mSettingWindows.size())
		mSettingWindows[mSelectedIndex]->Focus();
}

void ATUISettingsWindow::OnMouseDownL(sint32 x, sint32 y) {
	Focus();
	
	if (!mScreenStack.empty()) {
		bool active = y < mHeaderHeight;
	
		if (mbHeaderActivated != active) {
			mbHeaderActivated = active;

			Invalidate();
		}
	}
}

void ATUISettingsWindow::OnMouseUpL(sint32 x, sint32 y) {
	if (mbHeaderActivated) {
		mbHeaderActivated = false;

		Invalidate();

		OnActionRepeat(kActionCancel);
	}
}

void ATUISettingsWindow::OnMouseMove(sint32 x, sint32 y) {
	const bool hilite = !mScreenStack.empty() && y < mHeaderHeight;

	if (mbHeaderHighlighted != hilite) {
		mbHeaderHighlighted = hilite;

		Invalidate();
	}
}

bool ATUISettingsWindow::OnMouseWheel(sint32 x, sint32 y, float delta) {
	mScrollAccum += delta * (float)mRowHeight;

	sint32 idelta = (sint32)mScrollAccum;
	mScrollAccum -= (float)idelta;

	if (idelta) {
		vdpoint32 pt = mpScrollPane->GetClientOrigin();
		pt.y -= idelta;

		if (pt.y > mTotalHeight - mpScrollPane->GetClientArea().height())
			pt.y = mTotalHeight - mpScrollPane->GetClientArea().height();

		if (pt.y < 0)
			pt.y = 0;

		mpSlider->SetPos(pt.y);

		mpScrollPane->SetClientOrigin(pt);
	}

	return true;
}

void ATUISettingsWindow::OnMouseLeave() {
	if (mbHeaderHighlighted) {
		mbHeaderHighlighted = false;

		Invalidate();
	}

	if (mbHeaderActivated) {
		mbHeaderActivated = false;

		Invalidate();
	}
}

void ATUISettingsWindow::OnActionStart(uint32 trid) {
	OnActionRepeat(trid);
}

void ATUISettingsWindow::OnActionRepeat(uint32 trid) {
	switch(trid) {
		case kActionUp:
			if (mSelectedIndex > 0)
				SetSelectedIndex(mSelectedIndex - 1, true);
			break;

		case kActionDown:
			SetSelectedIndex(mSelectedIndex + 1, true);
			break;

		case kActionCancel:
			if (mScreenStack.empty()) {
				Destroy();
			} else {
				auto stackedScreen = mScreenStack.back();
				mScreenStack.pop_back();

				SetSettingsScreen(stackedScreen.mpScreen);
				SetSelectedIndex(stackedScreen.mSelIndex, true);
				stackedScreen.mpScreen->Release();
			}
			break;
	}
}

void ATUISettingsWindow::OnActionStop(uint32 trid) {
}

void ATUISettingsWindow::OnTrackCursorChanges(ATUIWidget *w) {
	sint32 index = -1;

	if (w && w != this && w != mpScrollPane) {
		vdpoint32 pt;

		if (mpScrollPane) {
			// We may get rounding errors during positioning, so test the center of the
			// control rather than the top.
			mpScrollPane->TranslateScreenPtToClientPt(w->TranslateClientPtToScreenPt(vdpoint32(0, w->GetArea().height() / 2)), pt);
			sint32 vpos = pt.y * 2 / mRowHeight;

			if (vpos >= 0) {
				auto it = std::upper_bound(mSettings.begin(), mSettings.end(), vpos,
					[](sint32 v, const SettingsEntry& se) { return v < se.mVPos; } );

				if (it != mSettings.begin() && (uint32)(vpos - it[-1].mVPos) < 2)
					index = (sint32)((it - 1) - mSettings.begin());
			}
		}
	}

	SetSelectedIndex(index, false);
}

void ATUISettingsWindow::UpdateLayout() {
	ATUIWidget *p = GetParent();
	if (!p)
		return;

	const sint32 disph = p->GetClientArea().height();
	sint32 w = VDRoundToInt32(250 * mpManager->GetThemeScaleFactor());
	sint32 h = std::min<sint32>(disph, VDRoundToInt32(300 * mpManager->GetThemeScaleFactor()));
	sint32 y = std::max<sint32>(0, std::min<sint32>(VDRoundToInt32(100 * mpManager->GetThemeScaleFactor()), (disph - h) >> 1));

	SetArea(vdrect32(0, y, w, y + h));
}

void ATUISettingsWindow::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	auto *tr = rdr.GetTextRenderer();

	if (mbHeaderHighlighted) {
		rdr.SetColorRGB(0x0024A0);
		rdr.FillRect(0, 0, w, mHeaderHeight);
	}

	uint32 x = 0;
	rdr.SetColorRGB(mbHeaderActivated ? 0x00FFFFFF : 0x00A0A0A0);

	if (!mScreenStack.empty()) {
		ATUIStockImage& limg = mpManager->GetStockImage(kATUIStockImageIdx_ButtonLeft);

		VDDisplayBlt blt = {};
		blt.mDestX = limg.mOffsetX;
		blt.mDestY = (mHeaderHeight - limg.mHeight - 2) / 2;
		blt.mSrcX = 0;
		blt.mSrcY = 0;
		blt.mWidth = limg.mWidth;
		blt.mHeight = limg.mHeight;
		rdr.MultiBlt(&blt, 1, limg.mImageView, rdr.kBltMode_Stencil);

		x = blt.mWidth;
	}

	tr->SetFont(mpFont);
	tr->SetAlignment(tr->kAlignLeft, tr->kVertAlignBaseline);
	tr->SetColorRGB(mbHeaderActivated ? 0x00FFFFFF : 0x00A0A0A0);
	tr->DrawTextLine(x, mHeaderBaseline, mCaption.c_str());
	tr->SetFont(nullptr);

	rdr.FillRect(0, mHeaderHeight - 2, w, 1);

	ATUIContainer::Paint(rdr, w, h);
}

void ATUISettingsWindow::DestroyScreen() {
	while(!mSettings.empty()) {
		auto *p = mSettings.back().mpSetting;
		mSettings.pop_back();

		delete p;
	}

	while(!mSettingWindows.empty()) {
		auto *p = mSettingWindows.back();
		mSettingWindows.pop_back();

		mpScrollPane->RemoveChild(p);

		p->Release();
	}
}

void ATUISettingsWindow::BuildScreen() {
	if (!mpManager || !mpCurrentScreen)
		return;

	DestroyScreen();

	mpScrollPane->SetClientOrigin(vdpoint32(0,0));

	mpCurrentScreen->BuildSettings(this);

	sint32 width = mpScrollPane->GetClientArea().width();
	for(const auto& entry : mSettings) {
		ATUISetting *s = entry.mpSetting;

		s->Read();

		vdrefptr<ATUISettingWindow> w(new ATUISettingWindow(s));

		w->SetOnSubScreen([this](ATUISubScreenSetting *s) { this->OnSubScreenActivated(s); });
		w->SetOnAction([this](ATUIActionSetting *s) { this->OnAction(s); });
		w->SetOnDynamicUpdate([this]() { this->OnDynamicUpdate(); });
	
		vdrect32 r(0, 0, width, 0);

		r.top = (entry.mVPos * mRowHeight) >> 1;
		r.bottom = r.top + mRowHeight;

		w->SetArea(r);

		mTotalHeight = r.bottom;

		mpScrollPane->AddChild(w);

		mSettingWindows.push_back(w);
		w.release();
	}

	RecomputeSlider();

	mSelectedIndex = -1;
	SetSelectedIndex(0, false);
}

void ATUISettingsWindow::RecomputeSlider() {
	if (mpScrollPane && mpSlider) {
		const sint32 pageHeight = mpScrollPane->GetClientArea().height();
		const sint32 range = std::max<sint32>(mTotalHeight - pageHeight, 0);

		if (range > 0) {
			mpSlider->SetVisible(true);
			mpSlider->SetRange(0, range);
			mpSlider->SetPageSize(pageHeight);
		} else {
			mpSlider->SetVisible(false);
		}
	}
}

void ATUISettingsWindow::OnSubScreenActivated(ATUISubScreenSetting *s) {
	vdrefptr<IATUISettingsScreen> newScreen;

	s->BuildScreen(~newScreen);

	if (mpCurrentScreen) {
		auto& stackedEntry = mScreenStack.push_back();
		stackedEntry.mpScreen = mpCurrentScreen.release();
		stackedEntry.mSelIndex = mSelectedIndex;
	}

	SetSettingsScreen(newScreen);
}

void ATUISettingsWindow::OnAction(ATUIActionSetting *s) {
	if (s->IsAsync()) {
		auto p = vdmakerefptr(this);

		ATUIPushStep([p]() { p->OnAsyncActionCompleted(); });
		mpPendingResult = s->ActivateAsync();
		ATUIPushStep(mpPendingResult->GetStep());
	} else {
		if (s->Activate())
			Destroy();
	}
}

void ATUISettingsWindow::OnAsyncActionCompleted() {
	if (mpPendingResult) {
		bool result = mpPendingResult->GetResult();
		mpPendingResult.clear();

		if (result)
			Destroy();
		else
			OnDynamicUpdate();
	}
}

void ATUISettingsWindow::OnDynamicUpdate() {
	for(ATUISettingWindow *w : mSettingWindows)
		w->UpdateDynamic();
}

///////////////////////////////////////////////////////////////////////////

void ATCreateUISettingsWindow(ATUISettingsWindow **pp) {
	ATUISettingsWindow *p = new ATUISettingsWindow;
	p->AddRef();
	*pp = p;
}
