//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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
#include <windows.h>
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/math.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/services.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/messageloop.h>
#include <at/atnativeui/uinativewindow.h>
#include "resource.h"
#include "gtia.h"
#include "oshelper.h"
#include "simulator.h"
#include "uiaccessors.h"

extern ATSimulator g_sim;

///////////////////////////////////////////////////////////////////////////

class ATUIColorReferenceControl final : public ATUINativeWindow {
public:
	ATUIColorReferenceControl();

	void UpdateFromPalette(const uint32 *palette);

private:
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam) override;

	void OnSetFont(HFONT hfont, bool redraw);
	void UpdateMetrics();
	void UpdateScrollBar();
	void OnVScroll(int code);
	void OnMouseWheel(float delta);
	void OnPaint();

	HFONT mhfont = nullptr;
	float mScrollAccum = 0;
	sint32 mScrollY = 0;
	sint32 mScrollMax = 0;
	sint32 mRowHeight = 1;
	sint32 mTextOffsetX = 0;
	sint32 mTextOffsetY = 0;
	sint32 mWidth = 0;
	sint32 mHeight = 0;

	struct ColorEntry {
		VDStringW mLabel;
		uint8 mPaletteIndex;
		uint32 mBgColor;
		uint32 mFgColor;
	};

	vdvector<ColorEntry> mColors;
};

ATUIColorReferenceControl::ATUIColorReferenceControl() {
	mColors.emplace_back(ColorEntry { VDStringW(L"$94: GR.0 background"), 0x94 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$9A: GR.0 foreground"), 0x9A });
	mColors.emplace_back(ColorEntry { VDStringW(L"$72: Ballblazer sky"), 0x72 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$96: Pitfall sky"), 0x96 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$86: Pitfall II sky"), 0x86 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$A0: Star Raiders shields"), 0xA0 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$90: Star Raiders galactic map"), 0x90 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$96: Star Raiders map BG"), 0x96 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$B8: Star Raiders map FG"), 0xB8 });
	mColors.emplace_back(ColorEntry { VDStringW(L"$AA: Pole Position sky"), 0xAA });
	mColors.emplace_back(ColorEntry { VDStringW(L"$D8: Pole Position grass"), 0xD8 });
}

void ATUIColorReferenceControl::UpdateFromPalette(const uint32 *palette) {
	bool redraw = false;

	for(ColorEntry& ce : mColors) {
		const uint32 c = palette[ce.mPaletteIndex] & 0xFFFFFF;

		if (ce.mBgColor != c) {
			ce.mBgColor = c;
			redraw = true;

			const uint32 luma = (c & 0xFF00FF) * ((19 << 16) + 54) + (c & 0xFF00) * (183 << 8);
			ce.mFgColor = (luma >= UINT32_C(0x80000000)) ? 0 : 0xFFFFFF;
		}
	}

	if (redraw)
		InvalidateRect(mhwnd, nullptr, true);
}

LRESULT ATUIColorReferenceControl::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			{
				RECT r {};
				GetClientRect(mhwnd, &r);

				mWidth = r.right;
				mHeight = r.bottom;
			}
			OnSetFont(nullptr, false);
			break;

		case WM_SIZE:
			mWidth = LOWORD(lParam);
			mHeight = HIWORD(lParam);
			UpdateScrollBar();
			break;

		case WM_MOUSEWHEEL:
			OnMouseWheel((float)(sint16)HIWORD(wParam) / (float)WHEEL_DELTA);
			return 0;

		case WM_PAINT:
			OnPaint();
			return 0;

		case WM_ERASEBKGND:
			return 0;

		case WM_SETFONT:
			OnSetFont((HFONT)wParam, LOWORD(lParam) != 0);
			return 0;

		case WM_VSCROLL:
			OnVScroll(LOWORD(wParam));
			return 0;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

void ATUIColorReferenceControl::OnSetFont(HFONT hfont, bool redraw) {
	if (!hfont)
		hfont = (HFONT)GetStockObject(SYSTEM_FONT);

	mhfont = hfont;

	UpdateMetrics();

	if (redraw)
		InvalidateRect(mhwnd, nullptr, true);
}

void ATUIColorReferenceControl::UpdateScrollBar() {
	SCROLLINFO si {};

	si.cbSize = sizeof(SCROLLINFO);
	si.nMin = 0;
	si.nMax = (sint32)(mRowHeight * mColors.size());
	si.nPage = mHeight;
	si.nPos = mScrollY;
	si.fMask = SIF_RANGE | SIF_POS | SIF_PAGE;

	mScrollMax = std::max<sint32>(0, si.nMax - mHeight);

	if (si.nMax <= (sint32)si.nPage) {
		ShowScrollBar(mhwnd, SB_VERT, false);

		if (mScrollY) {
			sint32 oldScrollY = mScrollY;
			mScrollY = 0;

			ScrollWindow(mhwnd, 0, oldScrollY, nullptr, nullptr);
		}
	} else
		ShowScrollBar(mhwnd, SB_VERT, true);

	SetScrollInfo(mhwnd, SB_VERT, &si, true);
}

void ATUIColorReferenceControl::UpdateMetrics() {
	mRowHeight = 1;
	mTextOffsetX = 0;
	mTextOffsetY = 0;

	if (HDC hdc = GetDC(mhwnd)) {
		if (HGDIOBJ hOldFont = SelectObject(hdc, mhfont)) {
			TEXTMETRICW tm {};

			if (GetTextMetrics(hdc, &tm)) {
				int margin = std::max<int>(2, tm.tmHeight / 5);

				mRowHeight = tm.tmAscent - tm.tmInternalLeading + 2*std::max<int>(tm.tmInternalLeading, tm.tmDescent) + 2*margin;
				mTextOffsetX = margin;
				mTextOffsetY = (mRowHeight - tm.tmAscent) / 2;
			}

			SelectObject(hdc, hOldFont);
		}

		ReleaseDC(mhwnd, hdc);
	}

	UpdateScrollBar();
}

void ATUIColorReferenceControl::OnVScroll(int code) {
	SCROLLINFO si {};
	si.cbSize = sizeof(SCROLLINFO);
	si.fMask = SIF_TRACKPOS | SIF_PAGE | SIF_RANGE | SIF_POS;

	if (!GetScrollInfo(mhwnd, SB_VERT, &si))
		return;

	si.cbSize = sizeof(SCROLLINFO);
	si.fMask = SIF_POS;

	switch(code) {
		case SB_TOP:
			si.nPos = 0;
			break;

		case SB_BOTTOM:
			si.nPos = mScrollMax;
			break;

		case SB_LINEUP:
			si.nPos -= mRowHeight;
			break;

		case SB_LINEDOWN:
			si.nPos += mRowHeight;
			break;

		case SB_PAGEUP:
			si.nPos -= mHeight;
			break;

		case SB_PAGEDOWN:
			si.nPos += mHeight;
			break;

		case SB_THUMBPOSITION:
		case SB_THUMBTRACK:
			si.nPos = si.nTrackPos;
			break;
	}

	si.nPos = std::clamp<sint32>(si.nPos, 0, mScrollMax);

	SetScrollInfo(mhwnd, SB_VERT, &si, TRUE);

	sint32 delta = mScrollY - si.nPos;
	if (delta) {
		mScrollY = si.nPos;

		ScrollWindow(mhwnd, 0, delta, nullptr, nullptr);
	}
}

void ATUIColorReferenceControl::OnMouseWheel(float delta) {
	if (!delta)
		return;

	UINT linesPerNotch = 3;
	SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, &linesPerNotch, 0);

	mScrollAccum += delta * (float)((sint32)linesPerNotch * mRowHeight);

	sint32 pixels = VDRoundToInt32(mScrollAccum);

	if (pixels) {
		mScrollAccum -= (float)pixels;

		sint32 newScrollY = std::clamp<sint32>(mScrollY - pixels, 0, mScrollMax);
		sint32 delta = mScrollY - newScrollY;

		if (delta) {
			mScrollY = newScrollY;

			ScrollWindow(mhwnd, 0, delta, nullptr, nullptr);
			UpdateScrollBar();
		}
	}
}

void ATUIColorReferenceControl::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhwnd, &ps);
	if (!hdc)
		return;

	int savedDC = SaveDC(hdc);
	if (savedDC) {
		SelectObject(hdc, mhfont);

		int rawRow1 = (ps.rcPaint.top + mScrollY) / mRowHeight;
		int rawRow2 = (ps.rcPaint.bottom + mScrollY + mRowHeight - 1) / mRowHeight;
		int row1 = std::max(rawRow1, 0);
		int row2 = std::min(rawRow2, (int)mColors.size());

		RECT rClip;
		rClip.left = 0;
		rClip.top = row1 * mRowHeight - mScrollY;
		rClip.right = mWidth;
		rClip.bottom = rClip.top + mRowHeight;

		SetTextAlign(hdc, TA_LEFT | TA_TOP);

		for(int row = row1; row < row2; ++row) {
			const ColorEntry& ce = mColors[row];

			SetBkColor(hdc, VDSwizzleU32(ce.mBgColor) >> 8);
			SetTextColor(hdc, VDSwizzleU32(ce.mFgColor) >> 8);

			ExtTextOutW(hdc, rClip.left + mTextOffsetX, rClip.top + mTextOffsetY, ETO_OPAQUE | ETO_CLIPPED, &rClip, ce.mLabel.c_str(), ce.mLabel.size(), nullptr);
			rClip.top += mRowHeight;
			rClip.bottom += mRowHeight;
		}

		if (rClip.top < ps.rcPaint.bottom) {
			rClip.bottom = ps.rcPaint.bottom;

			SetBkColor(hdc, 0);
			ExtTextOutW(hdc, rClip.left, rClip.top, ETO_OPAQUE | ETO_CLIPPED, &rClip, L"", 0, nullptr);
		}

		RestoreDC(hdc, savedDC);
	}

	EndPaint(mhwnd, &ps);
}


///////////////////////////////////////////////////////////////////////////

class ATAdjustColorsDialog final : public VDResizableDialogFrameW32 {
public:
	ATAdjustColorsDialog();

protected:
	bool OnLoaded() override;
	void OnDestroy() override;
	void OnDataExchange(bool write) override;
	void OnEnable(bool enable) override;
	bool OnCommand(uint32 id, uint32 extcode) override;
	void OnInitMenu(VDZHMENU hmenu) override;
	void OnHScroll(uint32 id, int code) override;
	VDZINT_PTR DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) override;
	void OnParamUpdated(uint32 id);
	void UpdateLabel(uint32 id);
	void UpdateColorImage();
	void UpdateGammaWarning();
	void ExportPalette(const wchar_t *s);
	void OnPresetChanged(int sel);
	void OnLumaRampChanged(VDUIProxyComboBoxControl *sender, int sel);
	void OnColorModeChanged(VDUIProxyComboBoxControl *sender, int sel);
	void OnGammaRampHelp();

	bool mbShowRelativeOffsets = false;

	ATColorSettings mSettings;
	ATNamedColorParams *mpParams;
	ATNamedColorParams *mpOtherParams;

	VDUIProxyComboBoxControl mPresetCombo;
	VDUIProxyComboBoxControl mLumaRampCombo;
	VDUIProxyComboBoxControl mColorModeCombo;
	VDDelegate mDelLumaRampChanged;
	VDDelegate mDelColorModeChanged;

	VDUIProxySysLinkControl mGammaWarning;

	vdrefptr<ATUIColorReferenceControl> mpSamplesControl;
};

ATAdjustColorsDialog g_adjustColorsDialog;

ATAdjustColorsDialog::ATAdjustColorsDialog()
	: VDResizableDialogFrameW32(IDD_ADJUST_COLORS)
	, mpSamplesControl(new ATUIColorReferenceControl)
{
	mGammaWarning.SetOnClicked([this] { OnGammaRampHelp(); });
	mPresetCombo.SetOnSelectionChanged([this](int sel) { OnPresetChanged(sel); });
	mLumaRampCombo.OnSelectionChanged() += mDelLumaRampChanged.Bind(this, &ATAdjustColorsDialog::OnLumaRampChanged);
	mColorModeCombo.OnSelectionChanged() += mDelColorModeChanged.Bind(this, &ATAdjustColorsDialog::OnColorModeChanged);
}

bool ATAdjustColorsDialog::OnLoaded() {
	ATUIRegisterModelessDialog(mhwnd);

	HWND hwndPlaceholderRef = GetControl(IDC_REFERENCE_VIEW);
	if (hwndPlaceholderRef) {
		ATUINativeWindowProxy proxy(hwndPlaceholderRef);
		const vdrect32 r = proxy.GetArea();

		mpSamplesControl->CreateChild(mhdlg, IDC_REFERENCE_VIEW, r.left, r.top, r.width(), r.height(), WS_VISIBLE | WS_CHILD, WS_EX_CLIENTEDGE);

		if (mpSamplesControl->IsValid()) {
			mResizer.AddAlias(mpSamplesControl->GetHandleW32(), hwndPlaceholderRef, mResizer.kMC | mResizer.kAvoidFlicker);
			mResizer.Remove(hwndPlaceholderRef);

			DestroyWindow(hwndPlaceholderRef);

			ApplyFontToControl(IDC_REFERENCE_VIEW);
		}
	}

	mResizer.Add(IDC_GAMMA_WARNING, mResizer.kBC);
	mResizer.Add(IDC_COLORS, mResizer.kTL);

	AddProxy(&mGammaWarning, IDC_GAMMA_WARNING);
	AddProxy(&mPresetCombo, IDC_PRESET);

	AddProxy(&mLumaRampCombo, IDC_LUMA_RAMP);
	mLumaRampCombo.AddItem(L"Linear");
	mLumaRampCombo.AddItem(L"XL/XE");

	AddProxy(&mColorModeCombo, IDC_COLORMATCHING_MODE);
	mColorModeCombo.AddItem(L"None");
	mColorModeCombo.AddItem(L"NTSC/PAL to sRGB");
	mColorModeCombo.AddItem(L"NTSC/PAL to Adobe RGB");

	TBSetRange(IDC_HUESTART, -120, 360);
	TBSetRange(IDC_HUERANGE, 0, 540);
	TBSetRange(IDC_BRIGHTNESS, -50, 50);
	TBSetRange(IDC_CONTRAST, 0, 200);
	TBSetRange(IDC_SATURATION, 0, 100);
	TBSetRange(IDC_GAMMACORRECT, 50, 260);
	TBSetRange(IDC_INTENSITYSCALE, 50, 200 + 20);
	TBSetRange(IDC_ARTPHASE, -60, 360);
	TBSetRange(IDC_ARTSAT, 0, 400);
	TBSetRange(IDC_ARTSHARP, 0, 100);
	TBSetRange(IDC_RED_SHIFT, -225, 225);
	TBSetRange(IDC_RED_SCALE, 0, 400);
	TBSetRange(IDC_GRN_SHIFT, -225, 225);
	TBSetRange(IDC_GRN_SCALE, 0, 400);
	TBSetRange(IDC_BLU_SHIFT, -225, 225);
	TBSetRange(IDC_BLU_SCALE, 0, 400);

	UpdateGammaWarning();

	OnDataExchange(false);
	SetFocusToControl(IDC_HUESTART);
	return true;
}

void ATAdjustColorsDialog::OnDestroy() {
	ATUIUnregisterModelessDialog(mhwnd);

	VDDialogFrameW32::OnDestroy();
}

void ATAdjustColorsDialog::OnDataExchange(bool write) {
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	if (write) {
		if (!mSettings.mbUsePALParams)
			*mpOtherParams = *mpParams;

		g_sim.GetGTIA().SetColorSettings(mSettings);
	} else {
		mSettings = gtia.GetColorSettings();

		if (gtia.IsPALMode()) {
			mpParams = &mSettings.mPALParams;
			mpOtherParams = &mSettings.mNTSCParams;
		} else {
			mpParams = &mSettings.mNTSCParams;
			mpOtherParams = &mSettings.mPALParams;
		}

		mPresetCombo.Clear();
		mPresetCombo.AddItem(L"Custom");

		const uint32 n = ATGetColorPresetCount();
		for(uint32 i = 0; i < n; ++i) {
			mPresetCombo.AddItem(ATGetColorPresetNameByIndex(i));
		}

		mPresetCombo.SetSelection(ATGetColorPresetIndexByTag(mpParams->mPresetTag.c_str()) + 1);

		CheckButton(IDC_SHARED, !mSettings.mbUsePALParams);
		CheckButton(IDC_PALQUIRKS, mpParams->mbUsePALQuirks);

		TBSetValue(IDC_HUESTART, VDRoundToInt(mpParams->mHueStart));
		TBSetValue(IDC_HUERANGE, VDRoundToInt(mpParams->mHueRange));
		TBSetValue(IDC_BRIGHTNESS, VDRoundToInt(mpParams->mBrightness * 100.0f));
		TBSetValue(IDC_CONTRAST, VDRoundToInt(mpParams->mContrast * 100.0f));
		TBSetValue(IDC_SATURATION, VDRoundToInt(mpParams->mSaturation * 100.0f));
		TBSetValue(IDC_GAMMACORRECT, VDRoundToInt(mpParams->mGammaCorrect * 100.0f));

		// apply dead zone
		int adjustedIntensityScale = VDRoundToInt(mpParams->mIntensityScale * 100.0f);
		if (adjustedIntensityScale > 100)
			adjustedIntensityScale += 20;
		else if (adjustedIntensityScale == 100)
			adjustedIntensityScale += 10;
		TBSetValue(IDC_INTENSITYSCALE, adjustedIntensityScale);

		sint32 adjustedHue = VDRoundToInt(mpParams->mArtifactHue);
		if (adjustedHue < -60)
			adjustedHue += 360;
		else if (adjustedHue > 360)
			adjustedHue -= 360;
		TBSetValue(IDC_ARTPHASE, adjustedHue);

		TBSetValue(IDC_ARTSAT, VDRoundToInt(mpParams->mArtifactSat * 100.0f));
		TBSetValue(IDC_ARTSHARP, VDRoundToInt(mpParams->mArtifactSharpness * 100.0f));
		TBSetValue(IDC_RED_SHIFT, VDRoundToInt(mpParams->mRedShift * 10.0f));
		TBSetValue(IDC_RED_SCALE, VDRoundToInt(mpParams->mRedScale * 100.0f));
		TBSetValue(IDC_GRN_SHIFT, VDRoundToInt(mpParams->mGrnShift * 10.0f));
		TBSetValue(IDC_GRN_SCALE, VDRoundToInt(mpParams->mGrnScale * 100.0f));
		TBSetValue(IDC_BLU_SHIFT, VDRoundToInt(mpParams->mBluShift * 10.0f));
		TBSetValue(IDC_BLU_SCALE, VDRoundToInt(mpParams->mBluScale * 100.0f));

		mLumaRampCombo.SetSelection(mpParams->mLumaRampMode);
		mColorModeCombo.SetSelection((int)mpParams->mColorMatchingMode);

		UpdateLabel(IDC_HUESTART);
		UpdateLabel(IDC_HUERANGE);
		UpdateLabel(IDC_BRIGHTNESS);
		UpdateLabel(IDC_CONTRAST);
		UpdateLabel(IDC_SATURATION);
		UpdateLabel(IDC_GAMMACORRECT);
		UpdateLabel(IDC_INTENSITYSCALE);
		UpdateLabel(IDC_ARTPHASE);
		UpdateLabel(IDC_ARTSAT);
		UpdateLabel(IDC_ARTSHARP);
		UpdateLabel(IDC_RED_SHIFT);
		UpdateLabel(IDC_RED_SCALE);
		UpdateLabel(IDC_GRN_SHIFT);
		UpdateLabel(IDC_GRN_SCALE);
		UpdateLabel(IDC_BLU_SHIFT);
		UpdateLabel(IDC_BLU_SCALE);
		UpdateColorImage();
	}
}

void ATAdjustColorsDialog::OnEnable(bool enable) {
	ATUISetGlobalEnableState(enable);
}

bool ATAdjustColorsDialog::OnCommand(uint32 id, uint32 extcode) {
	if (id == ID_OPTIONS_SHAREDPALETTES) {
		if (mSettings.mbUsePALParams) {
			if (!Confirm(L"Enabling palette sharing will overwrite the other profile with the current colors. Proceed?", L"Altirra Warning")) {
				return true;
			}

			mSettings.mbUsePALParams = false;
			OnDataExchange(true);
		}

		return true;
	} else if (id == ID_OPTIONS_SEPARATEPALETTES) {
		if (!mSettings.mbUsePALParams) {
			mSettings.mbUsePALParams = true;
			OnDataExchange(true);
		}

		return true;
	} else if (id == ID_OPTIONS_USEPALQUIRKS) {
		mpParams->mbUsePALQuirks = !mpParams->mbUsePALQuirks;

		OnDataExchange(true);
		UpdateColorImage();
	} else if (id == ID_VIEW_SHOWRGBSHIFTS) {
		if (mbShowRelativeOffsets) {
			mbShowRelativeOffsets = false;

			UpdateLabel(IDC_RED_SHIFT);
			UpdateLabel(IDC_RED_SCALE);
			UpdateLabel(IDC_GRN_SHIFT);
			UpdateLabel(IDC_GRN_SCALE);
		}
	} else if (id == ID_VIEW_SHOWRGBRELATIVEOFFSETS) {
		if (!mbShowRelativeOffsets) {
			mbShowRelativeOffsets = true;

			UpdateLabel(IDC_RED_SHIFT);
			UpdateLabel(IDC_GRN_SHIFT);
		}
	} else if (id == IDC_EXPORT) {
		const VDStringW& fn = VDGetSaveFileName('pal ', (VDGUIHandle)mhdlg, L"Export palette", L"Atari800 palette (*.pal)\0*.pal", L"pal");

		if (!fn.empty()) {
			ExportPalette(fn.c_str());
		}
	}

	return false;
}

void ATAdjustColorsDialog::OnInitMenu(VDZHMENU hmenu) {
	VDCheckRadioMenuItemByCommandW32(hmenu, ID_OPTIONS_SEPARATEPALETTES, mSettings.mbUsePALParams);
	VDCheckRadioMenuItemByCommandW32(hmenu, ID_OPTIONS_SHAREDPALETTES, !mSettings.mbUsePALParams);
	VDCheckMenuItemByCommandW32(hmenu, ID_OPTIONS_USEPALQUIRKS, mpParams->mbUsePALQuirks);
	VDEnableMenuItemByCommandW32(hmenu, ID_OPTIONS_USEPALQUIRKS, g_sim.GetGTIA().IsPALMode());
	VDCheckRadioMenuItemByCommandW32(hmenu, ID_VIEW_SHOWRGBSHIFTS, !mbShowRelativeOffsets);
	VDCheckRadioMenuItemByCommandW32(hmenu, ID_VIEW_SHOWRGBRELATIVEOFFSETS, mbShowRelativeOffsets);
}

void ATAdjustColorsDialog::OnHScroll(uint32 id, int code) {
	if (id == IDC_HUESTART) {
		float v = (float)TBGetValue(IDC_HUESTART);

		if (mpParams->mHueStart != v) {
			mpParams->mHueStart = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_HUERANGE) {
		float v = (float)TBGetValue(IDC_HUERANGE);

		if (mpParams->mHueRange != v) {
			mpParams->mHueRange = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_BRIGHTNESS) {
		float v = (float)TBGetValue(IDC_BRIGHTNESS) / 100.0f;

		if (mpParams->mBrightness != v) {
			mpParams->mBrightness = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_CONTRAST) {
		float v = (float)TBGetValue(IDC_CONTRAST) / 100.0f;

		if (mpParams->mContrast != v) {
			mpParams->mContrast = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_SATURATION) {
		float v = (float)TBGetValue(IDC_SATURATION) / 100.0f;

		if (mpParams->mSaturation != v) {
			mpParams->mSaturation = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_GAMMACORRECT) {
		float v = (float)TBGetValue(IDC_GAMMACORRECT) / 100.0f;

		if (mpParams->mGammaCorrect != v) {
			mpParams->mGammaCorrect = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_INTENSITYSCALE) {
		int rawValue = TBGetValue(IDC_INTENSITYSCALE);

		if (rawValue >= 120)
			rawValue -= 20;
		else if (rawValue >= 100)
			rawValue = 100;

		float v = (float)rawValue / 100.0f;

		if (mpParams->mIntensityScale != v) {
			mpParams->mIntensityScale = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_ARTPHASE) {
		float v = (float)TBGetValue(IDC_ARTPHASE);

		if (mpParams->mArtifactHue != v) {
			mpParams->mArtifactHue = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_ARTSAT) {
		float v = (float)TBGetValue(IDC_ARTSAT) / 100.0f;

		if (mpParams->mArtifactSat != v) {
			mpParams->mArtifactSat = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_ARTSHARP) {
		float v = (float)TBGetValue(IDC_ARTSHARP) / 100.0f;

		if (mpParams->mArtifactSharpness != v) {
			mpParams->mArtifactSharpness = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_RED_SHIFT) {
		float v = (float)TBGetValue(IDC_RED_SHIFT) / 10.0f;

		if (mpParams->mRedShift != v) {
			mpParams->mRedShift = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_RED_SCALE) {
		float v = (float)TBGetValue(IDC_RED_SCALE) / 100.0f;

		if (mpParams->mRedScale != v) {
			mpParams->mRedScale = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_GRN_SHIFT) {
		float v = (float)TBGetValue(IDC_GRN_SHIFT) / 10.0f;

		if (mpParams->mGrnShift != v) {
			mpParams->mGrnShift = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_GRN_SCALE) {
		float v = (float)TBGetValue(IDC_GRN_SCALE) / 100.0f;

		if (mpParams->mGrnScale != v) {
			mpParams->mGrnScale = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_BLU_SHIFT) {
		float v = (float)TBGetValue(IDC_BLU_SHIFT) / 10.0f;

		if (mpParams->mBluShift != v) {
			mpParams->mBluShift = v;

			OnParamUpdated(id);
		}
	} else if (id == IDC_BLU_SCALE) {
		float v = (float)TBGetValue(IDC_BLU_SCALE) / 100.0f;

		if (mpParams->mBluScale != v) {
			mpParams->mBluScale = v;

			OnParamUpdated(id);
		}
	}
}

VDZINT_PTR ATAdjustColorsDialog::DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	if (msg == WM_DRAWITEM) {
		if (wParam == IDC_COLORS) {
			const DRAWITEMSTRUCT& drawInfo = *(const DRAWITEMSTRUCT *)lParam;

			BITMAPINFO bi = {
				{
					sizeof(BITMAPINFOHEADER),
					16,
					19,
					1,
					32,
					BI_RGB,
					16*19*4,
					0,
					0,
					0,
					0
				}
			};

			uint32 image[256 + 48] = {0};
			uint32 *pal = image + 48;
			g_sim.GetGTIA().GetPalette(pal);

			// last three rows are used for 'text' screen
			image[0x10] = pal[0];
			image[0x00] = pal[0];
			for(int i=0; i<10; ++i) {
				image[0x11 + i] = pal[0x94];
				image[0x01 + i] = pal[0x94];
			}
			image[0x10+2] = pal[0x9A];
			image[0x10+11] = pal[0];
			image[0x00+11] = pal[0];

			// add NTSC artifacting colors
			uint32 ntscac[2];
			g_sim.GetGTIA().GetNTSCArtifactColors(ntscac);

			image[0x1C] = image[0x1D] = ntscac[0];
			image[0x1E] = image[0x1F] = ntscac[1];

			// flip palette section
			for(int i=0; i<128; i += 16)
				VDSwapMemory(&pal[i], &pal[240-i], 16*sizeof(uint32));

			StretchDIBits(drawInfo.hDC,
				drawInfo.rcItem.left,
				drawInfo.rcItem.top,
				drawInfo.rcItem.right - drawInfo.rcItem.left,
				drawInfo.rcItem.bottom - drawInfo.rcItem.top,
				0, 0, 16, 19, image, &bi, DIB_RGB_COLORS, SRCCOPY);

			SetWindowLongPtr(mhdlg, DWLP_MSGRESULT, TRUE);
			return TRUE;
		}
	}

	return VDDialogFrameW32::DlgProc(msg, wParam, lParam);
}

void ATAdjustColorsDialog::OnParamUpdated(uint32 id) {
	// force preset to custom
	if (!mpParams->mPresetTag.empty()) {
		mpParams->mPresetTag.clear();

		mPresetCombo.SetSelection(0);
	}

	OnDataExchange(true);
	UpdateColorImage();
	UpdateLabel(id);
}

void ATAdjustColorsDialog::UpdateLabel(uint32 id) {
	switch(id) {
		case IDC_HUESTART:
			SetControlTextF(IDC_STATIC_HUESTART, L"%.0f\u00B0", mpParams->mHueStart);
			break;
		case IDC_HUERANGE:
			SetControlTextF(IDC_STATIC_HUERANGE, L"%.1f\u00B0", mpParams->mHueRange / 15.0f);
			break;
		case IDC_BRIGHTNESS:
			SetControlTextF(IDC_STATIC_BRIGHTNESS, L"%+.0f%%", mpParams->mBrightness * 100.0f);
			break;
		case IDC_CONTRAST:
			SetControlTextF(IDC_STATIC_CONTRAST, L"%.0f%%", mpParams->mContrast * 100.0f);
			break;
		case IDC_SATURATION:
			SetControlTextF(IDC_STATIC_SATURATION, L"%.0f%%", mpParams->mSaturation * 100.0f);
			break;
		case IDC_INTENSITYSCALE:
			SetControlTextF(IDC_STATIC_INTENSITYSCALE, L"%.2f", mpParams->mIntensityScale);
			break;
		case IDC_GAMMACORRECT:
			SetControlTextF(IDC_STATIC_GAMMACORRECT, L"%.2f", mpParams->mGammaCorrect);
			break;
		case IDC_ARTPHASE:
			SetControlTextF(IDC_STATIC_ARTPHASE, L"%.0f\u00B0", mpParams->mArtifactHue);
			break;
		case IDC_ARTSAT:
			SetControlTextF(IDC_STATIC_ARTSAT, L"%.0f%%", mpParams->mArtifactSat * 100.0f);
			break;
		case IDC_ARTSHARP:
			SetControlTextF(IDC_STATIC_ARTSHARP, L"%.2f", mpParams->mArtifactSharpness);
			break;

		case IDC_RED_SHIFT:
			// The shifts are defined as deviations from the standard R-Y and B-Y axes, so the
			// biases here come from the angles in the standard matrix.
			if (mbShowRelativeOffsets)
				SetControlTextF(IDC_STATIC_RED_SHIFT, L"B%+.1f\u00B0", 90.0f - mpParams->mRedShift);
			else
				SetControlTextF(IDC_STATIC_RED_SHIFT, L"%.1f\u00B0", mpParams->mRedShift);
			break;
		case IDC_RED_SCALE:
			if (mbShowRelativeOffsets)
				SetControlTextF(IDC_STATIC_RED_SCALE, L"B\u00D7%.2f", mpParams->mRedScale * 0.560949f);
			else
				SetControlTextF(IDC_STATIC_RED_SCALE, L"%.2f", mpParams->mRedScale);
			break;

		case IDC_GRN_SHIFT:
			if (mbShowRelativeOffsets)
				SetControlTextF(IDC_STATIC_GRN_SHIFT, L"B%+.1f\u00B0", 235.80197f - mpParams->mGrnShift);
			else
				SetControlTextF(IDC_STATIC_GRN_SHIFT, L"%.1f\u00B0", mpParams->mGrnShift);
			break;
		case IDC_GRN_SCALE:
			if (mbShowRelativeOffsets)
				SetControlTextF(IDC_STATIC_GRN_SCALE, L"B\u00D7%.2f", mpParams->mGrnScale * 0.3454831f);
			else
				SetControlTextF(IDC_STATIC_GRN_SCALE, L"%.2f", mpParams->mGrnScale);
			break;

		case IDC_BLU_SHIFT:
			SetControlTextF(IDC_STATIC_BLU_SHIFT, L"%.1f\u00B0", mpParams->mBluShift);
			break;
		case IDC_BLU_SCALE:
			SetControlTextF(IDC_STATIC_BLU_SCALE, L"%.2f", mpParams->mBluScale);
			break;
	}
}

void ATAdjustColorsDialog::UpdateColorImage() {
	// update image
	HWND hwndColors = GetDlgItem(mhdlg, IDC_COLORS);
	InvalidateRect(hwndColors, NULL, FALSE);

	uint32 pal[256] = {};
	g_sim.GetGTIA().GetPalette(pal);
	mpSamplesControl->UpdateFromPalette(pal);
}

void ATAdjustColorsDialog::UpdateGammaWarning() {
	bool gammaNonIdentity = false;

	HMONITOR hmon = MonitorFromWindow((HWND)ATUIGetMainWindow(), MONITOR_DEFAULTTOPRIMARY);
	if (hmon) {
		MONITORINFOEXW mi { sizeof(MONITORINFOEXW) };

		if (GetMonitorInfoW(hmon, &mi)) {
			HDC hdc = CreateICW(mi.szDevice, mi.szDevice, nullptr, nullptr);

			if (hdc) {
				WORD gammaRamp[3][256] {};

				if (GetDeviceGammaRamp(hdc, gammaRamp)) {
					for(uint32 i=0; i<256; ++i) {
						sint32 expected = i;

						for(uint32 j=0; j<3; ++j) {
							sint32 actual = gammaRamp[j][i] >> 8;

							if (abs(actual - expected) > 1) {
								gammaNonIdentity = true;
								goto stop_search;
							}
						}
					}
stop_search:
					;
				}

				DeleteDC(hdc);
			}
		}
	}

	if (gammaNonIdentity)
		mGammaWarning.Show();
	else
		mGammaWarning.Hide();
}

void ATAdjustColorsDialog::ExportPalette(const wchar_t *s) {
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	uint32 pal[256];
	gtia.GetPalette(pal);

	uint8 pal8[768];
	for(int i=0; i<256; ++i) {
		const uint32 c = pal[i];

		pal8[i*3+0] = (uint8)(c >> 16);
		pal8[i*3+1] = (uint8)(c >>  8);
		pal8[i*3+2] = (uint8)(c >>  0);
	}

	VDFile f(s, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
	f.write(pal8, sizeof pal8);
}

void ATAdjustColorsDialog::OnPresetChanged(int sel) {
	if (sel == 0) {
		mpParams->mPresetTag.clear();
		OnDataExchange(true);
	} else if (sel > 0 && (uint32)sel <= ATGetColorPresetCount()) {
		mpParams->mPresetTag = ATGetColorPresetTagByIndex(sel - 1);
		static_cast<ATColorParams&>(*mpParams) = ATGetColorPresetByIndex(sel - 1);

		OnDataExchange(true);
		OnDataExchange(false);
	}
}

void ATAdjustColorsDialog::OnLumaRampChanged(VDUIProxyComboBoxControl *sender, int sel) {
	ATLumaRampMode newMode = (ATLumaRampMode)sel;

	if (mpParams->mLumaRampMode != newMode) {
		mpParams->mLumaRampMode = newMode;

		OnParamUpdated(0);
	}
}

void ATAdjustColorsDialog::OnColorModeChanged(VDUIProxyComboBoxControl *sender, int sel) {
	ATColorMatchingMode newMode = (ATColorMatchingMode)sel;

	if (mpParams->mColorMatchingMode != newMode) {
		mpParams->mColorMatchingMode = newMode;

		OnParamUpdated(0);
	}
}

void ATAdjustColorsDialog::OnGammaRampHelp() {
	ATShowHelp(mhdlg, L"colors.html#contexthelp-gamma-ramp");
}

void ATUIOpenAdjustColorsDialog(VDGUIHandle hParent) {
	g_adjustColorsDialog.Create(hParent);
}

void ATUICloseAdjustColorsDialog() {
	g_adjustColorsDialog.Destroy();
}
