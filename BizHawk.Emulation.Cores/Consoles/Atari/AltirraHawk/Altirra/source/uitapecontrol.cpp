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
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Riza/bitmap.h>
#include <at/atio/cassetteimage.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/messageloop.h>
#include <at/atnativeui/uiframe.h>
#include <at/atnativeui/uinativewindow.h>
#include "resource.h"
#include "cassette.h"

///////////////////////////////////////////////////////////////////////////

class ATUITapePeakControl : public ATUINativeWindow {
public:
	ATUITapePeakControl();
	~ATUITapePeakControl();

	virtual LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);

	void SetImage(IATCassetteImage *image);
	void SetPosition(float secs);

	void Refresh();

	VDEvent<ATUITapePeakControl, float>& OnPositionChanged() { return mPositionChangedEvent; }

protected:
	void OnSize();
	void OnClickOrMove(sint32 x);
	void Clear();
	void ClearBitmap();
	void UpdatePositionPx();
	void UpdateImage();
	void DrawPeaks(sint32 x1, sint32 x2, float y1, float y2, const float *data, uint32 c);

	IATCassetteImage *mpImage = nullptr;
	float mImageLenSecs = 0;
	HDC mhdc = nullptr;
	HBITMAP mhBitmap = nullptr;
	HGDIOBJ mhOldBitmap = nullptr;
	bool mbImageDirty = false;
	sint32 mWidth = 0;
	sint32 mHeight = 0;
	float mPositionSecs = 0;
	sint32 mPositionPx = 0;

	VDPixmapBuffer mPixmapBuffer;

	VDEvent<ATUITapePeakControl, float> mPositionChangedEvent;
};

ATUITapePeakControl::ATUITapePeakControl() {
}

ATUITapePeakControl::~ATUITapePeakControl() {
	Clear();
}

void ATUITapePeakControl::SetImage(IATCassetteImage *image) {
	if (mpImage == image)
		return;

	mpImage = image;

	Refresh();
}

void ATUITapePeakControl::SetPosition(float secs) {
	if (mPositionSecs != secs) {
		mPositionSecs = secs;

		UpdatePositionPx();
	}
}

void ATUITapePeakControl::Refresh() {
	mbImageDirty = true;

	if (mpImage) {
		const uint32 n = mpImage->GetDataLength();
		mImageLenSecs = (float)n / kATCassetteDataSampleRate;
	} else {
		mImageLenSecs = 0;
	}

	if (mhwnd)
		InvalidateRect(mhwnd, NULL, TRUE);
}

LRESULT ATUITapePeakControl::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			mWidth = 0;
			mHeight = 0;
			OnSize();
			break;

		case WM_DESTROY:
			Clear();
			break;

		case WM_SIZE:
			OnSize();
			break;

		case WM_LBUTTONDOWN:
		case WM_LBUTTONDBLCLK:
			OnClickOrMove((short)LOWORD(lParam));
			SetCapture(mhwnd);
			return 0;

		case WM_LBUTTONUP:
			if (::GetCapture() == mhwnd)
				::ReleaseCapture();
			return 0;

		case WM_MOUSEMOVE:
			if (wParam & MK_LBUTTON)
				OnClickOrMove((short)LOWORD(lParam));

			return 0;

		case WM_ERASEBKGND:
			return 0;

		case WM_PAINT:
			{
				PAINTSTRUCT ps;
				HDC hdc = BeginPaint(mhwnd, &ps);
				if (hdc) {
					UpdateImage();

					if (mhdc && mhBitmap) {
						if ((uint32)mPositionPx < (uint32)mWidth) {
							VDVERIFY(BitBlt(hdc, 0, 0, mPositionPx, mHeight, mhdc, 0, 0, SRCCOPY));

							RECT rPos = { mPositionPx, 0, mPositionPx + 1, mHeight };
							VDVERIFY(FillRect(hdc, &rPos, (HBRUSH)::GetStockObject(WHITE_BRUSH)));
							
							VDVERIFY(BitBlt(hdc, mPositionPx + 1, 0, mWidth - (mPositionPx + 1), mHeight, mhdc, mPositionPx + 1, 0, SRCCOPY));
						} else {
							VDVERIFY(BitBlt(hdc, 0, 0, mWidth, mHeight, mhdc, 0, 0, SRCCOPY));
						}
					} else {
						RECT r = { 0, 0, mWidth, mHeight };
						VDVERIFY(FillRect(hdc, &r, (HBRUSH)(COLOR_WINDOW + 1)));
					}

					EndPaint(mhwnd, &ps);
				}

				return 0;
			}
			break;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

void ATUITapePeakControl::OnSize() {
	RECT r;

	if (GetClientRect(mhwnd, &r)) {
		if (mWidth != r.right || mHeight != r.bottom) {
			mWidth = r.right;
			mHeight = r.bottom;

			ClearBitmap();
			InvalidateRect(mhwnd, NULL, FALSE);

			UpdatePositionPx();
		}
	}
}

void ATUITapePeakControl::OnClickOrMove(sint32 x) {
	if (x >= mWidth)
		x = mWidth - 1;

	if (x < 0)
		x = 0;

	const float pos = x * mImageLenSecs / (float)mWidth;

	if (mPositionSecs != pos) {
		SetPosition(pos);
		mPositionChangedEvent.Raise(this, pos);
	}
}

void ATUITapePeakControl::Clear() {
	ClearBitmap();

	if (mhdc) {
		VDVERIFY(::DeleteDC(mhdc));
		mhdc = NULL;
	}
}

void ATUITapePeakControl::ClearBitmap() {
	if (mhOldBitmap) {
		VDVERIFY(::SelectObject(mhdc, mhOldBitmap));
		mhOldBitmap = NULL;
	}

	if (mhBitmap) {
		VDVERIFY(::DeleteObject(mhBitmap));
		mhBitmap = NULL;
	}
}

void ATUITapePeakControl::UpdatePositionPx() {
	if (!mWidth || !mHeight || mImageLenSecs <= 0)
		return;

	sint32 px = VDFloorToInt(mPositionSecs * mWidth / mImageLenSecs);

	if (mPositionPx != px) {
		RECT r1 = { mPositionPx, 0, mPositionPx + 1, mHeight };
		RECT r2 = { px, 0, px + 1, mHeight };

		mPositionPx = px;

		if (mhwnd) {
			InvalidateRect(mhwnd, &r1, FALSE);
			InvalidateRect(mhwnd, &r2, FALSE);
		}
	}
}

void ATUITapePeakControl::UpdateImage() {
	if (!mWidth)
		return;

	if (!mhdc || !mhBitmap) {
		HDC hdc = GetDC(mhwnd);

		if (hdc) {
			if (!mhdc)
				mhdc = CreateCompatibleDC(hdc);

			if (!mhBitmap) {
				mhBitmap = CreateCompatibleBitmap(hdc, mWidth, mHeight);
				mbImageDirty = true;
			}

			ReleaseDC(mhwnd, hdc);
		}

		if (!mhdc || !mhBitmap)
			return;
	}

	if (!mhOldBitmap) {
		mhOldBitmap = SelectObject(mhdc, mhBitmap);
		if (!mhOldBitmap)
			return;
	}

	if (mbImageDirty) {
		mbImageDirty = false;

		VDPixmapLayout layout;
		VDMakeBitmapCompatiblePixmapLayout(layout, mWidth, mHeight, nsVDPixmap::kPixFormat_XRGB8888, 0);
		VDPixmapLayoutFlipV(layout);
		mPixmapBuffer.init(layout);

		memset(mPixmapBuffer.base(), 0, mPixmapBuffer.size());

		if (mImageLenSecs > 0) {
			const float secsPerPixel = mImageLenSecs / (float)mWidth;

			vdfastvector<float> buf(mWidth * 4);
			mpImage->ReadPeakMap(secsPerPixel * 0.5f, secsPerPixel, mWidth, buf.data(), buf.data() + mWidth*2);

			float hf = (float)mHeight;

			const uint32 red = 0xFFFF0000;
			const uint32 blue = 0xFF7E7EFF;

			if (mpImage->GetAudioLength()) {
				DrawPeaks(0, mWidth, 0, hf * 0.5f, buf.data(), blue);
				DrawPeaks(0, mWidth, hf * 0.5f, hf, buf.data() + mWidth * 2, red);
			} else {
				DrawPeaks(0, mWidth, 0, hf, buf.data(), blue);
			}
		}

		BITMAPINFO bi = {0};
		bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bi.bmiHeader.biWidth = mWidth;
		bi.bmiHeader.biHeight = mHeight;
		bi.bmiHeader.biPlanes = 1;
		bi.bmiHeader.biBitCount = 32;
		bi.bmiHeader.biCompression = BI_RGB;
		bi.bmiHeader.biSizeImage = 0;
		bi.bmiHeader.biXPelsPerMeter = 0;
		bi.bmiHeader.biYPelsPerMeter = 0;
		bi.bmiHeader.biClrUsed = 0;
		bi.bmiHeader.biClrImportant = 0;

		::SetDIBitsToDevice(mhdc, 0, 0, mWidth, mHeight, 0, 0, 0, mHeight, mPixmapBuffer.data, &bi, DIB_RGB_COLORS);
	}
}

void ATUITapePeakControl::DrawPeaks(sint32 x1, sint32 x2, float y1, float y2, const float *data, uint32 c) {
	float ymid = 0.5f*(y2 + y1);
	float ydel = 0.5f*(y2 - y1);

	for(sint32 x = x1; x < x2; ++x) {
		float v2 = ymid - ydel*(*data++);
		float v1 = ymid - ydel*(*data++);
		sint32 iy2 = VDCeilToInt(v2 - 0.5f);
		sint32 iy1 = VDCeilToInt(v1 - 0.5f);

		if (iy1 < 0)
			iy1 = 0;

		if (iy2 > mPixmapBuffer.h)
			iy2 = mPixmapBuffer.h;

		if (iy1 != iy2) {
			uint32 *p = vdptroffset((uint32 *)mPixmapBuffer.data, mPixmapBuffer.pitch * iy1) + x;

			for(sint32 y = iy1; y < iy2; ++y) {
				*p = c;

				vdptrstep(p, mPixmapBuffer.pitch);
			}
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATTapeControlDialog : public VDDialogFrameW32 {
public:
	ATTapeControlDialog(ATCassetteEmulator& tape);

	static void Open(VDGUIHandle parent, ATCassetteEmulator& tape);

public:
	void OnPositionChanged(ATUITapePeakControl *sender, float pos);

protected:
	void Stop();
	void TogglePause();
	void Play();
	void Record();
	void SeekStart();
	void SeekEnd();

	bool OnLoaded() override;
	void OnDestroy() override;
	void OnHScroll(uint32 id, int code) override;
	bool PreNCDestroy() override;
	void OnDpiChanged() override;

	void UpdateLabelText();
	void UpdateFonts();
	void RepositionPeakControl();
	void AppendTime(VDStringW& s, float t);

	void OnTapePositionChanged();
	void OnTapeChanging();
	void OnTapeChanged();
	void OnTapePeaksUpdated();
	void UpdateTapePosLen();
	void UpdatePlayState();

	ATCassetteEmulator& mTape;
	VDStringW mLabel;
	float mPos;
	uint32 mPosTenthsSec;
	float mLength;
	float mSecondsPerTick;
	float mTicksPerSecond;

	HFONT mhfontWebdings = nullptr;

	vdrefptr<ATUITapePeakControl> mpPeakControl;
	VDDelegate mDelPositionChanged;

	vdfunction<void()> mFnTapePositionChanged;
	vdfunction<void()> mFnTapePlayStateChanged;
	vdfunction<void()> mFnTapeChanging;
	vdfunction<void()> mFnTapeChanged;
	vdfunction<void()> mFnTapePeaksUpdated;

	VDUIProxyButtonControl mButtonStop;
	VDUIProxyButtonControl mButtonPause;
	VDUIProxyButtonControl mButtonPlay;
	VDUIProxyButtonControl mButtonRecord;
	VDUIProxyButtonControl mButtonSeekStart;
	VDUIProxyButtonControl mButtonSeekEnd;

	static ATTapeControlDialog *spDialog;

	static const UINT kIconButtonIds[];
};

const UINT ATTapeControlDialog::kIconButtonIds[] = {
	IDC_RECORD,
	IDC_PLAY,
	IDC_SEEK_START,
	IDC_SEEK_END,
	IDC_STOP,
	IDC_PAUSE
};

ATTapeControlDialog *ATTapeControlDialog::spDialog;

ATTapeControlDialog::ATTapeControlDialog(ATCassetteEmulator& tape)
	: VDDialogFrameW32(IDD_TAPE_CONTROL)
	, mTape(tape)
	, mFnTapePositionChanged([this]() { OnTapePositionChanged(); })
	, mFnTapePlayStateChanged([this]() { UpdatePlayState(); })
	, mFnTapeChanging([this]() { OnTapeChanging(); })
	, mFnTapeChanged([this]() { OnTapeChanged(); })
	, mFnTapePeaksUpdated([this]() { OnTapePeaksUpdated(); })
{
	mButtonStop.SetOnClicked([this] { Stop(); });
	mButtonPause.SetOnClicked([this] { TogglePause(); });
	mButtonPlay.SetOnClicked([this] { Play(); });
	mButtonRecord.SetOnClicked([this] { Record(); });
	mButtonSeekStart.SetOnClicked([this] { SeekStart(); });
	mButtonSeekEnd.SetOnClicked([this] { SeekEnd(); });
}

void ATTapeControlDialog::Open(VDGUIHandle parent, ATCassetteEmulator& tape) {
	if (spDialog)
		SetActiveWindow(spDialog->GetWindowHandle());
	else {
		ATTapeControlDialog *dlg = new ATTapeControlDialog(tape);

		if (dlg->Create(parent)) {
			ATUIRegisterModelessDialog(dlg->GetWindowHandle());
		} else
			delete dlg;

	}
}

void ATTapeControlDialog::OnPositionChanged(ATUITapePeakControl *sender, float pos) {
	if (mPos != pos) {
		mPos = pos;

		mTape.SeekToTime(mPos);

		if (mSecondsPerTick > 0)
			TBSetValue(IDC_POSITION, VDRoundToInt(pos / mSecondsPerTick));

		UpdateLabelText();
	}
}

void ATTapeControlDialog::Stop() {
	mTape.Stop();
}

void ATTapeControlDialog::TogglePause() {
	mTape.SetPaused(!mTape.IsPaused());
}

void ATTapeControlDialog::Play() {
	mTape.Play();
}

void ATTapeControlDialog::Record() {
	mTape.Record();
}

void ATTapeControlDialog::SeekStart() {
	mTape.SeekToTime(0);
}

void ATTapeControlDialog::SeekEnd() {
	mTape.SeekToTime(mTape.GetLength());
}

bool ATTapeControlDialog::OnLoaded() {
	if (!spDialog)
		spDialog = this;

	mResizer.Add(IDC_STOP, mResizer.kTL | mResizer.kSuppressFontChange);
	mResizer.Add(IDC_PAUSE, mResizer.kTL | mResizer.kSuppressFontChange);
	mResizer.Add(IDC_PLAY, mResizer.kTL | mResizer.kSuppressFontChange);
	mResizer.Add(IDC_RECORD, mResizer.kTL | mResizer.kSuppressFontChange);
	mResizer.Add(IDC_SEEK_START, mResizer.kTL | mResizer.kSuppressFontChange);
	mResizer.Add(IDC_SEEK_END, mResizer.kTL | mResizer.kSuppressFontChange);

	AddProxy(&mButtonStop, IDC_STOP);
	AddProxy(&mButtonPause, IDC_PAUSE);
	AddProxy(&mButtonPlay, IDC_PLAY);
	AddProxy(&mButtonRecord, IDC_RECORD);
	AddProxy(&mButtonSeekStart, IDC_SEEK_START);
	AddProxy(&mButtonSeekEnd, IDC_SEEK_END);

	UpdateFonts();

	mpPeakControl = new ATUITapePeakControl;

	::CreateWindowEx(WS_EX_CLIENTEDGE, MAKEINTATOM(ATUINativeWindow::Register()), _T(""), WS_CHILD | WS_VISIBLE | WS_TABSTOP, 0, 0, 0, 0, mhdlg, (HMENU)IDC_PEAK_CONTROL, VDGetLocalModuleHandleW32(),
		static_cast<ATUINativeWindow *>(&*mpPeakControl));

	mpPeakControl->OnPositionChanged() += mDelPositionChanged.Bind(this, &ATTapeControlDialog::OnPositionChanged);

	RepositionPeakControl();

	OnTapeChanged();

	SetFocusToControl(IDC_POSITION);

	mTape.PositionChanged += &mFnTapePositionChanged;
	mTape.PlayStateChanged += &mFnTapePlayStateChanged;
	mTape.TapeChanging += &mFnTapeChanging;
	mTape.TapeChanged += &mFnTapeChanged;
	mTape.TapePeaksUpdated += &mFnTapePeaksUpdated;

	UpdatePlayState();
	return true;
}

void ATTapeControlDialog::OnDestroy() {
	mTape.TapePeaksUpdated -= &mFnTapePeaksUpdated;
	mTape.TapeChanged -= &mFnTapeChanged;
	mTape.TapeChanging -= &mFnTapeChanging;
	mTape.PlayStateChanged -= &mFnTapePlayStateChanged;
	mTape.PositionChanged -= &mFnTapePositionChanged;

	VDDialogFrameW32::OnDestroy();
}

void ATTapeControlDialog::OnHScroll(uint32 id, int code) {
	float pos = (float)TBGetValue(IDC_POSITION) * mSecondsPerTick;

	if (pos != mPos) {
		mPos = pos;
		mPosTenthsSec = VDRoundToInt(pos * 10.0f);

		mTape.SeekToTime(mPos);

		if (mpPeakControl)
			mpPeakControl->SetPosition(mPos);

		UpdateLabelText();
	}
}

bool ATTapeControlDialog::PreNCDestroy() {
	if (mhfontWebdings) {
		DeleteObject(mhfontWebdings);
		mhfontWebdings = nullptr;
	}

	if (spDialog == this) {
		ATUIUnregisterModelessDialog(mhdlg);
		spDialog = nullptr;
	}
	return true;
}

void ATTapeControlDialog::OnDpiChanged() {
	VDDialogFrameW32::OnDpiChanged();

	UpdateFonts();
	RepositionPeakControl();
}

void ATTapeControlDialog::UpdateLabelText() {
	mLabel.clear();
	AppendTime(mLabel, mTape.GetPosition());
	mLabel += L'/';
	AppendTime(mLabel, mTape.GetLength());

	SetControlText(IDC_STATIC_POSITION, mLabel.c_str());
}

void ATTapeControlDialog::UpdateFonts() {
	int ptSize = 12;
	int ht = (ptSize * mCurrentDpi + 36) / 72;

	HFONT hNewFont = CreateFont(-ht, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, FF_DONTCARE | DEFAULT_PITCH,
		_T("Webdings"));

	if (hNewFont) {
		for(UINT id : kIconButtonIds)
			SendDlgItemMessage(mhdlg, id, WM_SETFONT, (WPARAM)hNewFont, TRUE);

		if (mhfontWebdings)
			DeleteObject(mhfontWebdings);

		mhfontWebdings = hNewFont;
	}
}

void ATTapeControlDialog::RepositionPeakControl() {
	const vdrect32 r = GetControlPos(IDC_PEAK_IMAGE);

	if (!r.empty()) {
		SetControlPos(IDC_PEAK_CONTROL, r);
	}
}

void ATTapeControlDialog::AppendTime(VDStringW& s, float t) {
	sint32 ticks = VDRoundToInt32(t*10.0f);
	int minutesWidth = 1;

	if (ticks >= 36000) {
		sint32 hours = ticks / 36000;
		ticks %= 36000;

		s.append_sprintf(L"%d:", hours);
		minutesWidth = 2;
	}

	sint32 minutes = ticks / 600;
	ticks %= 600;

	sint32 seconds = ticks / 10;
	ticks %= 10;

	s.append_sprintf(L"%0*d:%02d.%d", minutesWidth, minutes, seconds, ticks);
}

void ATTapeControlDialog::OnTapePositionChanged() {
	float pos = mTape.GetPosition();

	if (mPos != pos) {
		mPos = pos;

		mpPeakControl->SetPosition(pos);

		TBSetValue(IDC_POSITION, VDRoundToInt(mTicksPerSecond * pos));

		uint32 tenths = VDRoundToInt(pos * 10.0f);

		if (mPosTenthsSec != tenths) {
			mPosTenthsSec = tenths;
			UpdateLabelText();
		}
	}
}

void ATTapeControlDialog::OnTapeChanging() {
	mpPeakControl->SetImage(nullptr);
}

void ATTapeControlDialog::OnTapeChanged() {
	auto *image = mTape.GetImage();

	mpPeakControl->SetImage(image);

	ShowControl(IDC_PEAK_IMAGE, image == nullptr);
	ShowControl(IDC_PEAK_CONTROL, image != nullptr);

	UpdateTapePosLen();
}

void ATTapeControlDialog::OnTapePeaksUpdated() {
	mpPeakControl->Refresh();
	UpdateTapePosLen();
}

void ATTapeControlDialog::UpdateTapePosLen() {
	mPos = mTape.GetPosition();
	mPosTenthsSec = VDRoundToInt(mPos * 10.0f);
	mLength = ceilf(mTape.GetLength());

	if (mpPeakControl)
		mpPeakControl->SetPosition(mPos);

	float r = mLength < 1e-5f ? 0.0f : mPos / mLength;
	int ticks = 100000;

	if (mLength < 10000.0f)
		ticks = VDCeilToInt(mLength * 10.0f);

	mSecondsPerTick = mLength / (float)ticks;
	mTicksPerSecond = mLength > 0 ? (float)ticks / mLength : 0;

	TBSetRange(IDC_POSITION, 0, ticks);
	TBSetValue(IDC_POSITION, VDRoundToInt(r * (float)ticks));
	
	UpdateLabelText();
}

void ATTapeControlDialog::UpdatePlayState() {
	mButtonPlay.SetChecked(mTape.IsPlayEnabled());
	mButtonStop.SetChecked(mTape.IsStopped());
	mButtonPause.SetChecked(mTape.IsPaused());
	mButtonRecord.SetChecked(mTape.IsRecordEnabled());
}

////////////////////////////////////////////////////////////////////////////

void ATUIShowTapeControlDialog(VDGUIHandle hParent, ATCassetteEmulator& cassette) {
	ATTapeControlDialog::Open(hParent, cassette);
}
