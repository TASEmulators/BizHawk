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
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>
#include <at/atnativeui/uiframe.h>
#include "console.h"
#include "resource.h"

#ifndef WM_DPICHANGED
#define WM_DPICHANGED 0x02E0
#endif

class ATUIDialogDebugFont : public VDDialogFrameW32 {
public:
	ATUIDialogDebugFont(const wchar_t *name, int dp);
	~ATUIDialogDebugFont();

	int GetDeciPoints() const { return VDRoundToInt(10 * mPointSize); }
	const wchar_t *GetFontFamily() const { return mFontFamily.c_str(); }

private:
	VDZINT_PTR DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);

	bool OnLoaded();
	void OnDataExchange(bool write);
	void SelectFontFromList();
	void SelectSizeFromList();

	void UpdateFont();

	void OnFontSelected(VDUIProxyListBoxControl *, int idx);
	void OnSizeSelected(VDUIProxyListBoxControl *, int idx);

	static int CALLBACK FontCallback(const LOGFONTW *logfont, const TEXTMETRICW *phyfont, DWORD fontType, LPARAM thisPtr);

	double mPointSize;
	VDStringW mFontFamily;
	HFONT mhFont;

	typedef vdvector<VDStringW> FontFamilies;
	FontFamilies mFontFamilies;

	typedef vdhashmap<const wchar_t *, uint32, vdhash<VDStringW>, vdstringpredi> FontLookup;
	FontLookup mFontLookup;

	VDUIProxyListBoxControl mFontList;
	VDUIProxyListBoxControl mSizeList;
	VDDelegate mDelFontSelected;
	VDDelegate mDelSizeSelected;

	struct FontFamilySort {
		bool operator()(const VDStringW& x, const VDStringW& y) const {
			return x.comparei(y) < 0;
		}
	};

	static const int kStandardPointSizes[];
};

const int ATUIDialogDebugFont::kStandardPointSizes[]={
	 50, 55,
	 60, 65,
	 70, 75,
	 80, 85,
	 90, 95,
	100,
	110,
	120,
	130,
	140,
	160,
	180,
	200,
	220,
	240,
	280,
	320,
	400,
	520,
	640,
	800,
};

ATUIDialogDebugFont::ATUIDialogDebugFont(const wchar_t *name, int dp)
	: VDDialogFrameW32(IDD_DEBUG_CHOOSE_FONT)
	, mPointSize(dp / 10.0)
	, mFontFamily(name)
	, mhFont(NULL)
{
	mFontList.OnSelectionChanged() += mDelFontSelected.Bind(this, &ATUIDialogDebugFont::OnFontSelected);
	mSizeList.OnSelectionChanged() += mDelSizeSelected.Bind(this, &ATUIDialogDebugFont::OnSizeSelected);
}

ATUIDialogDebugFont::~ATUIDialogDebugFont() {
	if (mhFont)
		DeleteObject(mhFont);
}

VDZINT_PTR ATUIDialogDebugFont::DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	switch(msg) {
		case WM_DPICHANGED:
			UpdateFont();
			break;
	}

	return VDDialogFrameW32::DlgProc(msg, wParam, lParam);
}

bool ATUIDialogDebugFont::OnLoaded() {
	AddProxy(&mFontList, IDC_FONT_LIST);
	AddProxy(&mSizeList, IDC_SIZE_LIST);

	if (HDC hdc = GetDC(NULL)) {
		LOGFONT filter = {0};
		filter.lfCharSet = DEFAULT_CHARSET;

		EnumFontFamiliesExW(hdc, &filter, FontCallback, (LPARAM)this, 0);
		ReleaseDC(NULL, hdc);
	}

	std::sort(mFontFamilies.begin(), mFontFamilies.end(), FontFamilySort());
	mFontFamilies.erase(std::unique(mFontFamilies.begin(), mFontFamilies.end()), mFontFamilies.end());

	for(size_t i=0, n=mFontFamilies.size(); i<n; ++i) {
		mFontList.AddItem(mFontFamilies[i].c_str());
		mFontLookup.insert(mFontFamilies[i].c_str()).first->second = (uint32)i;
	}

	VDStringW s;
	for(size_t i=0; i<vdcountof(kStandardPointSizes); ++i) {
		s.sprintf(L"%g", (float)kStandardPointSizes[i] / 10.0f);
		mSizeList.AddItem(s.c_str());
	}

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogDebugFont::OnDataExchange(bool write) {
	ExchangeControlValueString(write, IDC_FONT_SEL, mFontFamily);
	ExchangeControlValueDouble(write, IDC_SIZE_SEL, L"%.1f", mPointSize, 1.0, 127.0);

	if (!write) {
		SelectFontFromList();
		SelectSizeFromList();
	}
}

void ATUIDialogDebugFont::SelectFontFromList() {
	const VDStringW& name = GetControlValueString(IDC_FONT_SEL);

	FontLookup::const_iterator it = mFontLookup.find(name.c_str());

	if (it == mFontLookup.end())
		mFontList.SetSelection(-1);
	else {
		mFontList.SetSelection(it->second);
		UpdateFont();
	}
}

void ATUIDialogDebugFont::SelectSizeFromList() {
	const int dp = VDRoundToInt(10 * GetControlValueDouble(IDC_SIZE_SEL));
	const int *begin = kStandardPointSizes;
	const int *end = kStandardPointSizes + vdcountof(kStandardPointSizes);
	const int *sel = std::find(begin, end, dp);

	if (sel == end)
		mSizeList.SetSelection(-1);
	else {
		mSizeList.SetSelection((int)(sel - begin));
		UpdateFont();
	}
}

void ATUIDialogDebugFont::UpdateFont() {
	const VDStringW& name = GetControlValueString(IDC_FONT_SEL);
	const int dp = VDRoundToInt(10 * GetControlValueDouble(IDC_SIZE_SEL));

	LOGFONTW lf = {0};

	vdwcslcpy(lf.lfFaceName, name.c_str(), vdcountof(lf.lfFaceName));

	uint32 dpi = ATUIGetWindowDpiW32(mhdlg);
	if (dpi)
		lf.lfHeight = -MulDiv(dp, dpi, 720);

	HFONT hNewFont = CreateFontIndirectW(&lf);
	if (hNewFont) {
		HWND hwnd = GetControl(IDC_STATIC_SAMPLETEXT);

		if (hwnd)
			SendMessageW(hwnd, WM_SETFONT, (WPARAM)hNewFont, TRUE);

		if (mhFont)
			DeleteObject(mhFont);

		mhFont = hNewFont;
	}
}

void ATUIDialogDebugFont::OnFontSelected(VDUIProxyListBoxControl *, int idx) {
	if ((size_t)idx < mFontFamilies.size()) {
		SetControlText(IDC_FONT_SEL, mFontFamilies[idx].c_str());
		UpdateFont();
	}
}

void ATUIDialogDebugFont::OnSizeSelected(VDUIProxyListBoxControl *, int idx) {
	if ((size_t)idx < vdcountof(kStandardPointSizes)) {
		SetControlTextF(IDC_SIZE_SEL, L"%g", (double)kStandardPointSizes[idx] / 10.0);
		UpdateFont();
	}
}

int CALLBACK ATUIDialogDebugFont::FontCallback(const LOGFONTW *logfont, const TEXTMETRICW *phyfont, DWORD fontType, LPARAM lParam) {
	ATUIDialogDebugFont *thisPtr = (ATUIDialogDebugFont *)lParam;

	if ((logfont->lfPitchAndFamily & 3) == FIXED_PITCH)
		thisPtr->mFontFamilies.push_back() = logfont->lfFaceName;

	return TRUE;
}

///////////////////////////////////////////////////////////////////////////

void ATUIShowDialogDebugFont(VDGUIHandle hParent) {
	LOGFONT lf;
	int pointSizeTenths;
	ATConsoleGetFont(lf, pointSizeTenths);

	ATUIDialogDebugFont dlg(lf.lfFaceName, pointSizeTenths);

	if (dlg.ShowDialog(hParent)) {
		vdwcslcpy(lf.lfFaceName, dlg.GetFontFamily(), vdcountof(lf.lfFaceName));

		lf.lfWidth			= 0;
		lf.lfEscapement		= 0;
		lf.lfOrientation	= 0;
		lf.lfWeight			= 0;
		lf.lfItalic			= FALSE;
		lf.lfUnderline		= FALSE;
		lf.lfStrikeOut		= FALSE;
		lf.lfCharSet		= DEFAULT_CHARSET;
		lf.lfOutPrecision	= OUT_DEFAULT_PRECIS;
		lf.lfClipPrecision	= CLIP_DEFAULT_PRECIS;
		lf.lfQuality		= DEFAULT_QUALITY;
		lf.lfPitchAndFamily	= FF_DONTCARE | DEFAULT_PITCH;

		ATConsoleSetFont(lf, dlg.GetDeciPoints());
	}
}
