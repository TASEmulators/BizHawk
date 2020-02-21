//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include <vd2/system/math.h>
#include <vd2/system/memory.h>
#include <vd2/system/time.h>
#include <vd2/system/VDString.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/text.h>
#include <vd2/Kasumi/resample.h>
#include <vd2/VDDisplay/compositor.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/renderersoft.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atcore/notifylist.h>
#include "uirender.h"
#include "audiomonitor.h"
#include "slightsid.h"
#include <at/atui/uiwidget.h>
#include <at/atuicontrols/uilabel.h>
#include "uikeyboard.h"
#include <at/atui/uicontainer.h>
#include <at/atui/uimanager.h>

namespace {
	void Shade(IVDDisplayRenderer& rdr, int x1, int y1, int dx, int dy) {
		if (rdr.GetCaps().mbSupportsAlphaBlending) {
			rdr.AlphaFillRect(x1, y1, dx, dy, 0x80000000);
		} else {
			rdr.SetColorRGB(0);
			rdr.FillRect(x1, y1, dx, dy);
		}
	}
}

///////////////////////////////////////////////////////////////////////////
class ATUIAudioStatusDisplay : public ATUIWidget {
public:
	void SetFont(IVDDisplayFont *font);
	void Update(const ATUIAudioStatus& status);
	void AutoSize();

protected:
	void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	void FormatLine(int idx, const ATUIAudioStatus& status);

	vdrefptr<IVDDisplayFont> mpFont;
	VDStringW mText;
	ATUIAudioStatus mAudioStatus;
};

void ATUIAudioStatusDisplay::SetFont(IVDDisplayFont *font) {
	mpFont = font;
}

void ATUIAudioStatusDisplay::Update(const ATUIAudioStatus& status) {
	mAudioStatus = status;

	Invalidate();
}

void ATUIAudioStatusDisplay::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);

	const int fonth = metrics.mAscent + metrics.mDescent;
	int y2 = 2;

	VDDisplayTextRenderer& tr = *rdr.GetTextRenderer();
	tr.SetFont(mpFont);
	tr.SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
	tr.SetColorRGB(0xffffff);

	for(int i=0; i<8; ++i) {
		FormatLine(i, mAudioStatus);
		tr.DrawTextLine(2, y2, mText.c_str());
		y2 += fonth;
	}
}

void ATUIAudioStatusDisplay::AutoSize() {
	if (!mpFont)
		return;

	// create a test structure with some big values in it
	ATUIAudioStatus testStatus = {};

	testStatus.mUnderflowCount = 9999;
	testStatus.mOverflowCount = 9999;
	testStatus.mDropCount = 9999;
	testStatus.mMeasuredMin = 999999;
	testStatus.mMeasuredMax = 999999;
	testStatus.mTargetMin = 999999;
	testStatus.mTargetMax = 999999;
	testStatus.mIncomingRate = 99999;
	testStatus.mExpectedRate = 99999;
	testStatus.mbStereoMixing = true;

	// loop over all the strings and compute max size
	sint32 w = 0;
	for(int i=0; i<8; ++i) {
		FormatLine(i, testStatus);

		w = std::max<sint32>(w, mpFont->MeasureString(mText.c_str(), mText.size(), false).w);
	}

	// resize me
	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);

	const int fonth = metrics.mAscent + metrics.mDescent;

	SetSize(vdsize32(4 + w, 4 + fonth * 8));
}

void ATUIAudioStatusDisplay::FormatLine(int idx, const ATUIAudioStatus& status) {
	switch(idx) {
	case 0:
		mText.sprintf(L"Underflow count: %d", status.mUnderflowCount);
		break;

	case 1:
		mText.sprintf(L"Overflow count: %d", status.mOverflowCount);
		break;

	case 2:
		mText.sprintf(L"Drop count: %d", status.mDropCount);
		break;

	case 3:
		mText.sprintf(L"Measured range: %5d-%5d (%.1f ms)"
			, status.mMeasuredMin
			, status.mMeasuredMax
			, (float)status.mMeasuredMin * 1000.0f / (status.mSamplingRate * 4.0f)
		);
		break;

	case 4:
		mText.sprintf(L"Target range: %5d-%5d", status.mTargetMin, status.mTargetMax);
		break;

	case 5:
		mText.sprintf(L"Incoming data rate: %.2f samples/sec", status.mIncomingRate);
		break;

	case 6:
		mText.sprintf(L"Expected data rate: %.2f samples/sec", status.mExpectedRate);
		break;

	case 7:
		mText.sprintf(L"Mixing mode: %ls", status.mbStereoMixing ? L"stereo" : L"mono");
		break;
	}
}

///////////////////////////////////////////////////////////////////////////
class ATUIAudioDisplay : public ATUIWidget {
public:
	ATUIAudioDisplay();

	void SetCyclesPerSecond(double rate) { mCyclesPerSecond = rate; }

	void SetAudioMonitor(ATAudioMonitor *mon);
	void SetSlightSID(ATSlightSIDEmulator *ss);

	void SetBigFont(IVDDisplayFont *font);
	void SetSmallFont(IVDDisplayFont *font);

	void AutoSize();
	void Update() { Invalidate(); }

protected:
	void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);
	void PaintSID(IVDDisplayRenderer& rdr, VDDisplayTextRenderer& tr, sint32 w, sint32 h);
	void PaintPOKEY(IVDDisplayRenderer& rdr, VDDisplayTextRenderer& tr, sint32 w, sint32 h);

	double mCyclesPerSecond = 1;

	vdrefptr<IVDDisplayFont> mpBigFont;
	vdrefptr<IVDDisplayFont> mpSmallFont;

	int mBigFontW;
	int mBigFontH;
	int mSmallFontW;
	int mSmallFontH;

	ATAudioMonitor *mpAudioMonitor;
	ATSlightSIDEmulator *mpSlightSID;
};

ATUIAudioDisplay::ATUIAudioDisplay()
	: mBigFontW(0)
	, mBigFontH(0)
	, mSmallFontW(0)
	, mSmallFontH(0)
	, mpAudioMonitor(NULL)
	, mpSlightSID(NULL)
{
}

void ATUIAudioDisplay::SetAudioMonitor(ATAudioMonitor *mon) {
	mpAudioMonitor = mon;
}

void ATUIAudioDisplay::SetSlightSID(ATSlightSIDEmulator *ss) {
	mpSlightSID = ss;
}

void ATUIAudioDisplay::SetBigFont(IVDDisplayFont *font) {
	if (mpBigFont == font)
		return;

	mpBigFont = font;

	const vdsize32& size = font->MeasureString(L"0123456789", 10, false);

	mBigFontW = size.w / 10;
	mBigFontH = size.h;
}

void ATUIAudioDisplay::SetSmallFont(IVDDisplayFont *font) {
	if (mpSmallFont == font)
		return;

	mpSmallFont = font;

	const vdsize32& size = font->MeasureString(L"0123456789", 10, false);

	mSmallFontW = size.w / 10;
	mSmallFontH = size.h;
}

void ATUIAudioDisplay::AutoSize() {
	const int chanht = 5 + mBigFontH + mSmallFontH;
	const int chanw = (std::max<int>(11*mSmallFontW, 8*mBigFontW) + 4) * 4;

	SetArea(vdrect32(mArea.left, mArea.top, mArea.left + chanw, mArea.top + chanht * 4));
}

void ATUIAudioDisplay::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	VDDisplayTextRenderer& tr = *rdr.GetTextRenderer();

	if (mpSlightSID)
		PaintSID(rdr, tr, w, h);
	else
		PaintPOKEY(rdr, tr, w, h);
}

void ATUIAudioDisplay::PaintSID(IVDDisplayRenderer& rdr, VDDisplayTextRenderer& tr, sint32 w, sint32 h) {
	const int fontw = mBigFontW;
	const int fonth = mBigFontH;
	const int fontsmw = mSmallFontW;
	const int fontsmh = mSmallFontH;

	const int chanht = 5 + fonth + fontsmh;

	const int x = 0;
	const int y = 0;

	const int x_freq = x + fontw * 8;
	const int x_note1 = x;
	const int x_note2 = x + fontsmw * 5;
	const int x_modes = x_freq + 4;
	const int x_duty = x_freq + 4;
	const int x_volbar = x_modes + 9*fontsmw;
	const int x_adsr = x_volbar + 5;

	wchar_t buf[128];

	const uint8 *regbase = mpSlightSID->GetRegisters();
	for(int ch=0; ch<3; ++ch) {
		const uint8 *chreg = regbase + 7*ch;
		const int chy = y + chanht*ch;
		const int chy_freq = chy;
		const int chy_modes = chy;
		const int chy_duty = chy + fontsmh;
		const int chy_note = chy + fonth;
		const int chy_adsr = chy + 1;
		const int chy_envelope = chy_adsr + fontsmh;
		const uint32 color = (ch != 2 || !(regbase[0x18] & 0x80)) ? 0xFFFFFF : 0x006e6e6e;

		const uint32 freq = chreg[0] + chreg[1]*256;
		//const float hz = (float)freq * (17897725.0f / 18.0f / 16777216.0f);
		const float hz = (float)freq * (985248.0f / 16777216.0f);
		swprintf(buf, 128, L"%.1f", hz);

		tr.SetFont(mpBigFont);
		tr.SetColorRGB(color);
		tr.SetAlignment(VDDisplayTextRenderer::kAlignRight, VDDisplayTextRenderer::kVertAlignTop);
		tr.DrawTextLine(x_freq, chy_freq, buf);
		tr.SetFont(mpSmallFont);

		buf[0] = chreg[4] & 0x80 ? 'N' : ' ';
		buf[1] = chreg[4] & 0x40 ? 'P' : ' ';
		buf[2] = chreg[4] & 0x20 ? 'S' : ' ';
		buf[3] = chreg[4] & 0x10 ? 'T' : ' ';
		buf[4] = chreg[4] & 0x08 ? 'E' : ' ';
		buf[5] = chreg[4] & 0x04 ? 'R' : ' ';
		buf[6] = chreg[4] & 0x02 ? 'S' : ' ';
		buf[7] = chreg[4] & 0x01 ? 'G' : ' ';
		buf[8] = 0;
		tr.SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
		tr.DrawTextLine(x_modes, chy_modes, buf);

		swprintf(buf, 128, L"%3.0f%%", (chreg[2] + (chreg[3] & 15)*256) * 100.0f / 4096.0f);
		tr.DrawTextLine(x_duty, chy_duty, buf);

		float midiNote = 69.0f + logf(hz + 0.0001f) * 17.312340490667560888319096172023f - 105.37631656229591524883618971458f;

		if (midiNote < 0)
			midiNote = 0;
		else if (midiNote > 140)
			midiNote = 140;

		int midiNoteInt = (int)(0.5f + midiNote);
		swprintf(buf, 128, L"%04X", freq);
		tr.DrawTextLine(x_note1, chy_note, buf);

		swprintf(buf, 128, L"%3u%+1.0f", midiNoteInt, (midiNote - midiNoteInt) * 10.0f);
		tr.DrawTextLine(x_note2, chy_note, buf);

		const int maxbarht = chanht - 2;

		int env = mpSlightSID->GetEnvelopeValue(ch);
		int ht = (env >> 4) * maxbarht / 15;

		int sustain = (chreg[6] >> 4);
		int sustainht = (sustain * maxbarht)/15;
		rdr.SetColorRGB(0x003b3b3b);
		rdr.FillRect(x_volbar, chy+chanht-1-sustainht, 2, sustainht);

		rdr.SetColorRGB(color);
		rdr.FillRect(x_volbar, chy+chanht-1-ht, 2, ht);

		int envmode = mpSlightSID->GetEnvelopeMode(ch);
		bool sustainMode = env <= sustain*17;

		int x2 = x_adsr;
		tr.SetColorRGB(envmode == 0 ? 0xFFFFFF : 0x007a4500);
		tr.DrawTextLine(x2, chy_adsr, L"A");
		x2 += mpSmallFont->MeasureString(L"A", 1, false).w;

		tr.SetColorRGB(envmode == 1 && !sustainMode ? 0xFFFFFF : 0x007a4500);
		tr.DrawTextLine(x2, chy_adsr, L"D");
		x2 += mpSmallFont->MeasureString(L"D", 1, false).w;

		tr.SetColorRGB(envmode == 1 && sustainMode ? 0xFFFFFF : 0x007a4500);
		tr.DrawTextLine(x2, chy_adsr, L"S");
		x2 += mpSmallFont->MeasureString(L"S", 1, false).w;

		tr.SetColorRGB(envmode == 2 ? 0xFFFFFF : 0x007a4500);
		tr.DrawTextLine(x2, chy_adsr, L"R");

		swprintf(buf, 128, L"%02X%02X", chreg[5], chreg[6]);
		tr.SetColorRGB(color);
		tr.DrawTextLine(x_adsr, chy_envelope, buf);
	}

	swprintf(buf, 128, L"%ls %ls %ls @ $%04X [%X] -> CH%lc%lc%lc"
		, regbase[24] & 0x10 ? L"LP" : L"  "
		, regbase[24] & 0x20 ? L"BP" : L"  "
		, regbase[24] & 0x40 ? L"HP" : L"  "
		, (regbase[21] & 7) + 8*regbase[22]
		, regbase[23] >> 4
		, regbase[23] & 0x01 ? L'1' : L' '
		, regbase[23] & 0x02 ? L'2' : L' '
		, regbase[23] & 0x04 ? L'3' : L' '
	);

	tr.SetColorRGB(0xFFFFFF);
	tr.DrawTextLine(x, y + chanht*3 + 6, buf);
}

void ATUIAudioDisplay::PaintPOKEY(IVDDisplayRenderer& rdr, VDDisplayTextRenderer& tr, sint32 w, sint32 h) {
	const int fontw = mBigFontW;
	const int fonth = mBigFontH;
	const int fontsmw = mSmallFontW;
	const int fontsmh = mSmallFontH;

	ATPokeyAudioLog *log;
	ATPokeyRegisterState *rstate;

	mpAudioMonitor->Update(&log, &rstate);

	uint8 audctl = rstate->mReg[8];

	int slowRate = audctl & 0x01 ? 114 : 28;
	int divisors[4];

	divisors[0] = (audctl & 0x40) ? (int)rstate->mReg[0] + 4 : ((int)rstate->mReg[0] + 1) * slowRate;

	divisors[1] = (audctl & 0x10)
		? (audctl & 0x40) ? rstate->mReg[0] + ((int)rstate->mReg[2] << 8) + 7 : (rstate->mReg[0] + ((int)rstate->mReg[2] << 8) + 1) * slowRate
		: ((int)rstate->mReg[2] + 1) * slowRate;

	divisors[2] = (audctl & 0x20) ? (int)rstate->mReg[4] + 4 : ((int)rstate->mReg[4] + 1) * slowRate;

	divisors[3] = (audctl & 0x08)
		? (audctl & 0x20) ? rstate->mReg[4] + ((int)rstate->mReg[6] << 8) + 7 : (rstate->mReg[4] + ((int)rstate->mReg[6] << 8) + 1) * slowRate
		: ((int)rstate->mReg[6] + 1) * slowRate;

	// layout
	const int chanht = 5 + fonth + fontsmh;

	const int x = 0;
	const int y = 0;

	const int x_link = x;
	const int x_clock = x + 1*fontsmw;
	const int x_highpass = x + 5*fontsmw + (fontsmw >> 1);
	const int x_mode = x + 7*fontsmw;
	const int x_noise = x + 9*fontsmw;
	const int x_waveform = x + std::max<int>(11*fontsmw, 8*fontw) + 4;

	const int chanw = (x_waveform - x) * 4;

	sint32 hstep = log->mRecordedCount ? ((chanw - x_waveform - 4) << 16) / log->mRecordedCount : 0;

	Shade(rdr, x, y, chanw, chanht * 4);

	wchar_t buf[128];
	for(int ch=0; ch<4; ++ch) {
		const int chy = y + chanht*ch;
		const int chanfreqy = chy + 4;
		const int chandetaily = chy + 4 + fonth + 1;

		// draw frequency
		swprintf(buf, 128, L"%.1f", (mCyclesPerSecond * 0.5) / divisors[ch]);

		tr.SetColorRGB(0xFFFFFF);
		tr.SetFont(mpBigFont);
		tr.SetAlignment(VDDisplayTextRenderer::kAlignRight, VDDisplayTextRenderer::kVertAlignTop);
		tr.DrawTextLine(x_waveform - 4, chanfreqy, buf);

		tr.SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
		tr.SetFont(mpSmallFont);

		// draw link/clock indicator
		if ((ch == 1 && (audctl & 0x10)) || (ch == 3 && (audctl & 0x08)))
			tr.DrawTextLine(x_link, chandetaily, L"16");
		else if ((ch == 0 && (audctl & 0x40)) || (ch == 2 && (audctl & 0x20)))
			tr.DrawTextLine(x_clock, chandetaily, L"1.79");
		else
			tr.DrawTextLine(x_clock, chandetaily, audctl & 1 ? L" 15K" : L" 64K");

		// draw high-pass indicator
		if ((ch == 0 && (audctl & 4)) || (ch == 1 && (audctl & 2)))
			tr.DrawTextLine(x_highpass, chandetaily, L"H");

		// draw mode indicator
		const uint8 ctl = rstate->mReg[ch*2 + 1];
		if (ctl & 0x10)
			tr.DrawTextLine(x_mode, chandetaily, L"V");
		else {
			tr.DrawTextLine(x_mode, chandetaily, (ctl & 0x80) ? L"L" : L"5");

			if (ctl & 0x20)
				tr.DrawTextLine(x_noise, chandetaily, L"T");
			else if (ctl & 0x40)
				tr.DrawTextLine(x_noise, chandetaily, L"4");
			else
				tr.DrawTextLine(x_noise, chandetaily, (audctl & 0x80) ? L"9" : L"17");
		}

		// draw volume indicator
		int vol = (ctl & 15) * (chanht - 3) / 15;

		rdr.SetColorRGB(0xFFFFFF);
		rdr.FillRect(x_waveform, chy + chanht - 1 - vol, 1, vol);

		const uint32 n = log->mRecordedCount;

		if (n >= 2) {
			uint32 hpos = 0x8000 + ((x_waveform + 2) << 16);
			int pybase = chy + chanht - 1;

			vdfastvector<vdpoint32> pts(n);

			for(uint32 pos = 0; pos < n; ++pos) {
				int px = hpos >> 16;
				int py = pybase - log->mpStates[pos].mChannelOutputs[ch] * (chanht - 3) / 15;

				pts[pos] = vdpoint32(px, py);

				hpos += hstep;
			}

			rdr.PolyLine(pts.data(), n - 1);
		}
	}

	mpAudioMonitor->Reset();
}

///////////////////////////////////////////////////////////////////////////
class ATUIRenderer final : public vdrefcount, public IATUIRenderer, public IVDTimerCallback {
public:
	ATUIRenderer();
	~ATUIRenderer();

	int AddRef() { return vdrefcount::AddRef(); }
	int Release() { return vdrefcount::Release(); }

	bool IsVisible() const;
	bool SetVisible() const;
	void SetVisible(bool visible);

	void SetStatusFlags(uint32 flags) { mStatusFlags |= flags; mStickyStatusFlags |= flags; }
	void ResetStatusFlags(uint32 flags) { mStatusFlags &= ~flags; }
	void PulseStatusFlags(uint32 flags) { mStickyStatusFlags |= flags; }

	void SetCyclesPerSecond(double rate);

	void SetStatusCounter(uint32 index, uint32 value);
	void SetDiskLEDState(uint32 index, sint32 ledDisplay);
	void SetDiskMotorActivity(uint32 index, bool on);
	void SetDiskErrorState(uint32 index, bool on);

	void SetHActivity(bool write);
	void SetPCLinkActivity(bool write);
	void SetIDEActivity(bool write, uint32 lba);
	void SetFlashWriteActivity();

	void SetCassetteIndicatorVisible(bool vis) { mbShowCassetteIndicator = vis; }
	void SetCassettePosition(float pos, float len, bool recordMode, bool fskMode);

	void SetRecordingPosition();
	void SetRecordingPosition(float time, sint64 size);

	void SetTracingSize(sint64 size);

	void SetModemConnection(const char *str);

	void SetStatusMessage(const wchar_t *s);

	void SetLedStatus(uint8 ledMask);
	void SetHeldButtonStatus(uint8 consolMask);
	void SetPendingHoldMode(bool enabled);
	void SetPendingHeldKey(int key);
	void SetPendingHeldButtons(uint8 consolMask);

	void ClearWatchedValue(int index);
	void SetWatchedValue(int index, uint32 value, int len);
	void SetAudioStatus(ATUIAudioStatus *status);
	void SetAudioMonitor(bool secondary, ATAudioMonitor *monitor);
	void SetSlightSID(ATSlightSIDEmulator *emu);

	void SetFpsIndicator(float fps);

	void SetHoverTip(int px, int py, const wchar_t *text);

	void SetPaused(bool paused);

	void SetUIManager(ATUIManager *m);

	void Relayout(int w, int h);
	void Update();

	sint32 GetIndicatorSafeHeight() const;

	void AddIndicatorSafeHeightChangedHandler(const vdfunction<void()> *pfn);
	void RemoveIndicatorSafeHeightChangedHandler(const vdfunction<void()> *pfn);

public:
	virtual void TimerCallback();

protected:
	void InvalidateLayout();

	void UpdatePendingHoldLabel();
	void RelayoutPendingKeyLabels();
	void UpdateHostDeviceLabel();
	void UpdatePCLinkLabel();
	void UpdateHoverTipPos();
	void RemakeLEDFont();

	double	mCyclesPerSecond = 1;

	uint32	mStatusFlags = 0;
	uint32	mStickyStatusFlags = 0;
	uint32	mStatusCounter[15] = {};
	sint32	mStatusLEDs[15] = {};
	uint32	mDiskMotorFlags = 0;
	uint32	mDiskErrorFlags = 0;
	float	mCassettePos = 0;
	int		mRecordingPos = -1;
	sint64	mRecordingSize = -1;
	sint64	mTracingSize = -1;
	bool	mbShowCassetteIndicator = false;
	int		mShowCassetteIndicatorCounter = 0;

	uint32	mHardDiskLBA = 0;
	uint8	mHardDiskCounter = 0;
	bool	mbHardDiskRead = false;
	bool	mbHardDiskWrite = false;

	uint8	mHReadCounter = 0;
	uint8	mHWriteCounter = 0;
	uint8	mPCLinkReadCounter = 0;
	uint8	mPCLinkWriteCounter = 0;
	uint8	mFlashWriteCounter = 0;

	VDStringW	mModemConnection;
	VDStringW	mStatusMessage;

	uint8	mLedStatus = 0;

	sint32 mIndicatorSafeHeight = 0;
	ATNotifyList<const vdfunction<void()> *> mIndicatorSafeAreaListeners;

	uint32	mWatchedValues[8];
	sint8	mWatchedValueLens[8];

	ATAudioMonitor	*mpAudioMonitors[2] = {};
	ATSlightSIDEmulator *mpSlightSID = nullptr;

	VDDisplaySubRenderCache mFpsRenderCache;
	float mFps = -1.0f;

	vdrefptr<IVDDisplayFont> mpSysFont;
	vdrefptr<IVDDisplayFont> mpSmallMonoSysFont;
	vdrefptr<IVDDisplayFont> mpSysMonoFont;
	vdrefptr<IVDDisplayFont> mpSysHoverTipFont;
	vdrefptr<IVDDisplayFont> mpSysBoldHoverTipFont;
	int mSysFontDigitWidth = 0;
	int mSysFontDigitHeight = 0;
	int mSysFontDigitAscent = 0;
	int mSysFontDigitInternalLeading = 0;
	int mSysMonoFontHeight = 0;

	sint32	mPrevLayoutWidth = 0;
	sint32	mPrevLayoutHeight = 0;

	int		mPendingHeldKey = -1;
	uint8	mPendingHeldButtons = 0;
	bool	mbPendingHoldMode = false;

	int		mLEDFontCellWidth = 0;
	int		mLEDFontCellAscent = 0;
	vdrefptr<IVDDisplayFont> mpLEDFont;

	vdrefptr<ATUIContainer> mpContainer;
	vdrefptr<ATUILabel> mpDiskDriveIndicatorLabels[15];
	vdrefptr<ATUILabel> mpFpsLabel;
	vdrefptr<ATUILabel> mpStatusMessageLabel;
	vdrefptr<ATUILabel> mpWatchLabels[8];
	vdrefptr<ATUILabel> mpHardDiskDeviceLabel;
	vdrefptr<ATUILabel> mpRecordingLabel;
	vdrefptr<ATUILabel> mpTracingLabel;
	vdrefptr<ATUILabel> mpFlashWriteLabel;
	vdrefptr<ATUILabel> mpHostDeviceLabel;
	vdrefptr<ATUILabel> mpPCLinkLabel;
	vdrefptr<ATUILabel> mpLedLabels[2];
	vdrefptr<ATUILabel> mpCassetteLabel;
	vdrefptr<ATUILabel> mpCassetteTimeLabel;
	vdrefptr<ATUILabel> mpPausedLabel;
	vdrefptr<ATUILabel> mpHeldButtonLabels[3];
	vdrefptr<ATUILabel> mpPendingHeldKeyLabel;
	vdrefptr<ATUIAudioStatusDisplay> mpAudioStatusDisplay;
	vdrefptr<ATUIAudioDisplay> mpAudioDisplays[2];

	vdrefptr<ATUILabel> mpHoverTip;
	int mHoverTipX = 0;
	int mHoverTipY = 0;

	VDLazyTimer mStatusTimer;

	static const uint32 kDiskColors[8][2];
};

const uint32 ATUIRenderer::kDiskColors[8][2]={
	{ 0x91a100, 0xffff67 },
	{ 0xd37040, 0xffe7b7 },
	{ 0xd454cf, 0xffcbff },
	{ 0x9266ff, 0xffddff },
	{ 0x4796ec, 0xbeffff },
	{ 0x35ba61, 0xacffd8 },
	{ 0x6cb200, 0xe3ff6f },
	{ 0xbb860e, 0xfffd85 },
};

void ATCreateUIRenderer(IATUIRenderer **r) {
	*r = new ATUIRenderer;
	(*r)->AddRef();
}

ATUIRenderer::ATUIRenderer() {
	for(int i=0; i<15; ++i) {
		mStatusCounter[i] = i+1;
		mStatusLEDs[i] = -1;
	}

	for(int i=0; i<8; ++i)
		mWatchedValueLens[i] = -1;

	mpContainer = new ATUIContainer;
	mpContainer->SetDockMode(kATUIDockMode_Fill);
	mpContainer->SetHitTransparent(true);

	for(int i=0; i<15; ++i) {
		ATUILabel *label = new ATUILabel;
		
		mpDiskDriveIndicatorLabels[i] = label;

		label->SetTextColor(0);
		label->SetVisible(false);
		label->SetTextOffset(2, 1);
	}

	for(int i=0; i<8; ++i) {
		ATUILabel *label = new ATUILabel;
		
		mpWatchLabels[i] = label;

		label->SetFillColor(0);
		label->SetTextColor(0xFFFFFF);
		label->SetVisible(false);
		label->SetTextOffset(2, 1);
	}

	mpStatusMessageLabel = new ATUILabel;
	mpStatusMessageLabel->SetVisible(false);
	mpStatusMessageLabel->SetTextOffset(6, 2);

	mpFpsLabel = new ATUILabel;
	mpFpsLabel->SetVisible(false);
	mpFpsLabel->SetTextColor(0xFFFFFF);
	mpFpsLabel->SetFillColor(0);
	mpFpsLabel->SetTextOffset(2, 0);

	mpAudioStatusDisplay = new ATUIAudioStatusDisplay;
	mpAudioStatusDisplay->SetVisible(false);
	mpAudioStatusDisplay->SetAlphaFillColor(0x80000000);
	mpAudioStatusDisplay->AutoSize();

	for(auto& disp : mpAudioDisplays) {
		disp = new ATUIAudioDisplay;
		disp->SetVisible(false);
		disp->SetAlphaFillColor(0x80000000);
		disp->SetSmallFont(mpSmallMonoSysFont);
	}

	mpHardDiskDeviceLabel = new ATUILabel;
	mpHardDiskDeviceLabel->SetVisible(false);
	mpHardDiskDeviceLabel->SetFillColor(0x91d81d);
	mpHardDiskDeviceLabel->SetTextColor(0);

	mpRecordingLabel = new ATUILabel;
	mpRecordingLabel->SetVisible(false);
	mpRecordingLabel->SetFillColor(0x932f00);
	mpRecordingLabel->SetTextColor(0xffffff);

	mpTracingLabel = new ATUILabel;
	mpTracingLabel->SetVisible(false);
	mpTracingLabel->SetFillColor(0x932f00);
	mpTracingLabel->SetTextColor(0xffffff);

	mpFlashWriteLabel = new ATUILabel;
	mpFlashWriteLabel->SetVisible(false);
	mpFlashWriteLabel->SetFillColor(0xd37040);
	mpFlashWriteLabel->SetTextColor(0x000000);
	mpFlashWriteLabel->SetText(L"F");

	for(int i=0; i<2; ++i) {
		ATUILabel *label = new ATUILabel;
		
		mpLedLabels[i] = label;

		label->SetVisible(false);
		label->SetFillColor(0xffffff);
		label->SetTextColor(0xdd5d87);
		label->SetText(i ? L"2" : L"1");
	}

	mpPCLinkLabel = new ATUILabel;
	mpPCLinkLabel->SetVisible(false);
	mpPCLinkLabel->SetFillColor(0x007920);
	mpPCLinkLabel->SetTextColor(0x000000);

	mpHostDeviceLabel = new ATUILabel;
	mpHostDeviceLabel->SetVisible(false);
	mpHostDeviceLabel->SetFillColor(0x007920);
	mpHostDeviceLabel->SetTextColor(0x000000);

	mpCassetteLabel = new ATUILabel;
	mpCassetteLabel->SetVisible(false);
	mpCassetteLabel->SetFillColor(0x93e1ff);
	mpCassetteLabel->SetTextColor(0);
	mpCassetteLabel->SetText(L"C");

	mpCassetteTimeLabel = new ATUILabel;
	mpCassetteTimeLabel->SetVisible(false);
	mpCassetteTimeLabel->SetFillColor(0);

	mpPausedLabel = new ATUILabel;
	mpPausedLabel->SetVisible(false);
	mpPausedLabel->SetFont(mpSysFont);
	mpPausedLabel->SetTextOffset(4, 2);
	mpPausedLabel->SetTextColor(0xffffff);
	mpPausedLabel->SetFillColor(0x404040);
	mpPausedLabel->SetBorderColor(0xffffff);
	mpPausedLabel->SetText(L"Paused");

	mpHoverTip = new ATUILabel;
	mpHoverTip->SetVisible(false);
	mpHoverTip->SetTextColor(0);
	mpHoverTip->SetFillColor(0xffffe1);
	mpHoverTip->SetBorderColor(0);
	mpHoverTip->SetTextOffset(4, 4);

	static const wchar_t *const kHeldButtonLabels[]={
		L"Start",
		L"Select",
		L"Option",
	};

	VDASSERTCT(vdcountof(kHeldButtonLabels) == vdcountof(mpHeldButtonLabels));

	for(int i=0; i<(int)vdcountof(mpHeldButtonLabels); ++i) {
		mpHeldButtonLabels[i] = new ATUILabel;
		mpHeldButtonLabels[i]->SetVisible(false);
		mpHeldButtonLabels[i]->SetTextColor(0);
		mpHeldButtonLabels[i]->SetFillColor(0xd4d080);
		mpHeldButtonLabels[i]->SetText(kHeldButtonLabels[i]);
	}

	mpPendingHeldKeyLabel = new ATUILabel;
	mpPendingHeldKeyLabel->SetVisible(false);
	mpPendingHeldKeyLabel->SetBorderColor(0xffffff);
	mpPendingHeldKeyLabel->SetTextOffset(2, 2);
	mpPendingHeldKeyLabel->SetTextColor(0xffffff);
	mpPendingHeldKeyLabel->SetFillColor(0xa44050);
}

ATUIRenderer::~ATUIRenderer() {
}

bool ATUIRenderer::IsVisible() const {
	return mpContainer->IsVisible();
}

void ATUIRenderer::SetVisible(bool visible) {
	mpContainer->SetVisible(visible);
}

void ATUIRenderer::SetCyclesPerSecond(double rate) {
	mCyclesPerSecond = rate;

	mpAudioDisplays[0]->SetCyclesPerSecond(rate);
	mpAudioDisplays[1]->SetCyclesPerSecond(rate);
}

void ATUIRenderer::SetStatusCounter(uint32 index, uint32 value) {
	mStatusCounter[index] = value;
}

void ATUIRenderer::SetDiskLEDState(uint32 index, sint32 ledDisplay) {
	mStatusLEDs[index] = ledDisplay;
}

void ATUIRenderer::SetDiskMotorActivity(uint32 index, bool on) {
	if (on)
		mDiskMotorFlags |= (1 << index);
	else
		mDiskMotorFlags &= ~(1 << index);
}

void ATUIRenderer::SetDiskErrorState(uint32 index, bool on) {
	if (on)
		mDiskErrorFlags |= (1 << index);
	else
		mDiskErrorFlags &= ~(1 << index);
}

void ATUIRenderer::SetHActivity(bool write) {
	bool update = false;

	if (write) {
		if (mHWriteCounter < 25)
			update = true;

		mHWriteCounter = 30;
	} else {
		if (mHReadCounter < 25)
			update = true;

		mHReadCounter = 30;
	}

	if (update)
		UpdateHostDeviceLabel();
}

void ATUIRenderer::SetPCLinkActivity(bool write) {
	bool update = false;

	if (write) {
		if (mPCLinkWriteCounter < 25)
			update = true;

		mPCLinkWriteCounter = 30;
	} else {
		if (mPCLinkReadCounter < 25)
			update = true;

		mPCLinkReadCounter = 30;
	}

	if (update)
		UpdatePCLinkLabel();
}

void ATUIRenderer::SetIDEActivity(bool write, uint32 lba) {
	if (mHardDiskLBA != lba) {
		mbHardDiskWrite = false;
		mbHardDiskRead = false;
	}

	mHardDiskCounter = 3;

	if (write)
		mbHardDiskWrite = true;
	else
		mbHardDiskRead = true;

	mHardDiskLBA = lba;

	mpHardDiskDeviceLabel->SetVisible(true);
	mpHardDiskDeviceLabel->SetTextF(L"%lc%u", mbHardDiskWrite ? L'W' : L'R', mHardDiskLBA);
	mpHardDiskDeviceLabel->AutoSize();
}

void ATUIRenderer::SetFlashWriteActivity() {
	mFlashWriteCounter = 20;

	mpFlashWriteLabel->SetVisible(true);
}

namespace {
	const uint32 kModemMessageBkColor = 0x1e00ac;
	const uint32 kModemMessageFgColor = 0x8458ff;

	const uint32 kStatusMessageBkColor = 0x303850;
	const uint32 kStatusMessageFgColor = 0xffffff;
}

void ATUIRenderer::SetModemConnection(const char *str) {
	if (str && *str) {
		mModemConnection = VDTextAToW(str);

		if (mStatusMessage.empty()) {
			mpStatusMessageLabel->SetVisible(true);
			mpStatusMessageLabel->SetFillColor(kModemMessageBkColor);
			mpStatusMessageLabel->SetTextColor(kModemMessageFgColor);
			mpStatusMessageLabel->SetBorderColor(kModemMessageFgColor);
			mpStatusMessageLabel->SetText(mModemConnection.c_str());
			mpStatusMessageLabel->AutoSize();
		}
	} else {
		mModemConnection.clear();

		if (mStatusMessage.empty())
			mpStatusMessageLabel->SetVisible(false);
	}
}

void ATUIRenderer::SetStatusMessage(const wchar_t *s) {
	mStatusMessage = s;

	mStatusTimer.SetOneShot(this, 1500);

	mpStatusMessageLabel->SetVisible(true);
	mpStatusMessageLabel->SetFillColor(kStatusMessageBkColor);
	mpStatusMessageLabel->SetTextColor(kStatusMessageFgColor);
	mpStatusMessageLabel->SetBorderColor(kStatusMessageFgColor);
	mpStatusMessageLabel->SetText(mStatusMessage.c_str());
	mpStatusMessageLabel->AutoSize();
}

void ATUIRenderer::SetRecordingPosition() {
	mRecordingPos = -1;
	mRecordingSize = -1;
	mpRecordingLabel->SetVisible(false);
}

void ATUIRenderer::SetRecordingPosition(float time, sint64 size) {
	int cpos = VDRoundToInt(time);
	uint32 csize = (uint32)((size * 10) >> 10);
	bool usemb = false;

	if (csize >= 10240) {
		csize &= 0xFFFFFC00;
		usemb = true;
	} else {
		csize -= csize % 10;
	}

	if (mRecordingPos == cpos && mRecordingSize == csize)
		return;

	mRecordingPos = cpos;
	mRecordingSize = csize;

	int secs = cpos % 60;
	int mins = cpos / 60;
	int hours = mins / 60;
	mins %= 60;

	if (usemb)
		mpRecordingLabel->SetTextF(L"R%02u:%02u:%02u (%.1fM)", hours, mins, secs, (float)csize / 10240.0f);
	else
		mpRecordingLabel->SetTextF(L"R%02u:%02u:%02u (%uK)", hours, mins, secs, csize / 10);

	mpRecordingLabel->AutoSize();
	mpRecordingLabel->SetVisible(true);
}

void ATUIRenderer::SetTracingSize(sint64 size) {
	if (mTracingSize != size) {
		mpTracingLabel->SetVisible(size >= 0);

		if (size >= 0) {
			if ((mTracingSize ^ size) >> 18) {
				mpTracingLabel->SetTextF(L"Tracing %.1fM", (double)size / 1048576.0);
				mpTracingLabel->AutoSize();
			}
		}

		mTracingSize = size;
	}
}

void ATUIRenderer::SetLedStatus(uint8 ledMask) {
	if (mLedStatus == ledMask)
		return;

	mLedStatus = ledMask;

	mpLedLabels[0]->SetVisible((ledMask & 1) != 0);
	mpLedLabels[1]->SetVisible((ledMask & 2) != 0);
}

void ATUIRenderer::SetHeldButtonStatus(uint8 consolMask) {
	for(int i=0; i<(int)vdcountof(mpHeldButtonLabels); ++i)
		mpHeldButtonLabels[i]->SetVisible((consolMask & (1 << i)) != 0);
}

void ATUIRenderer::SetPendingHoldMode(bool enable) {
	if (mbPendingHoldMode != enable) {
		mbPendingHoldMode = enable;

		UpdatePendingHoldLabel();
	}
}

void ATUIRenderer::SetPendingHeldKey(int key) {
	if (mPendingHeldKey != key) {
		mPendingHeldKey = key;

		UpdatePendingHoldLabel();
	}
}

void ATUIRenderer::SetPendingHeldButtons(uint8 consolMask) {
	if (mPendingHeldButtons != consolMask) {
		mPendingHeldButtons = consolMask;

		UpdatePendingHoldLabel();
	}
}

void ATUIRenderer::SetCassettePosition(float pos, float len, bool recordMode, bool fskMode) {
	mpCassetteTimeLabel->SetTextColor(recordMode ? 0xff8040 : 0x93e1ff);

	if (mCassettePos == pos)
		return;

	mCassettePos = pos;

	int cpos = VDRoundToInt(mCassettePos);

	int secs = cpos % 60;
	int mins = cpos / 60;
	int hours = mins / 60;
	mins %= 60;

	const float frac = len > 0.01f ? pos / len : 0.0f;

	mpCassetteTimeLabel->SetTextF(L"%02u:%02u:%02u [%d%%] %ls%ls", hours, mins, secs, (int)(frac * 100.0f), fskMode ? L"" : L"T-", recordMode ? L"REC" : L"Play");
	mpCassetteTimeLabel->AutoSize();
}

void ATUIRenderer::ClearWatchedValue(int index) {
	if (index >= 0 && index < 8) {
		mWatchedValueLens[index] = -1;
		mpWatchLabels[index]->SetVisible(false);
	}
}

void ATUIRenderer::SetWatchedValue(int index, uint32 value, int len) {
	if (index >= 0 && index < 8) {
		mWatchedValues[index] = value;
		mWatchedValueLens[index] = len;
		mpWatchLabels[index]->SetVisible(true);
	}
}

void ATUIRenderer::SetAudioStatus(ATUIAudioStatus *status) {
	if (status) {
		mpAudioStatusDisplay->Update(*status);
		mpAudioStatusDisplay->SetVisible(true);
	} else {
		mpAudioStatusDisplay->SetVisible(false);
	}
}

void ATUIRenderer::SetAudioMonitor(bool secondary, ATAudioMonitor *monitor) {
	mpAudioMonitors[secondary] = monitor;

	ATUIAudioDisplay *disp = mpAudioDisplays[secondary];
	disp->SetAudioMonitor(monitor);
	disp->AutoSize();
	disp->SetVisible(monitor != NULL);
	InvalidateLayout();
}

void ATUIRenderer::SetSlightSID(ATSlightSIDEmulator *emu) {
	mpSlightSID = emu;

	mpAudioDisplays[0]->SetSlightSID(emu);
	mpAudioDisplays[0]->AutoSize();
	InvalidateLayout();
}

void ATUIRenderer::SetFpsIndicator(float fps) {
	if (mFps != fps) {
		mFps = fps;

		if (fps < 0) {
			mpFpsLabel->SetVisible(false);
		} else {
			mpFpsLabel->SetVisible(true);
			mpFpsLabel->SetTextF(L"%.3f fps", fps);
			mpFpsLabel->AutoSize();
		}
	}
}

void ATUIRenderer::SetHoverTip(int px, int py, const wchar_t *text) {
	if (!text || !*text) {
		mpHoverTip->SetVisible(false);
	} else {
		mHoverTipX = px;
		mHoverTipY = py;

		mpHoverTip->SetHTMLText(text);
		mpHoverTip->AutoSize();

		mpHoverTip->SetVisible(true);
		UpdateHoverTipPos();
	}
}

void ATUIRenderer::SetPaused(bool paused) {
	mpPausedLabel->SetVisible(paused);
}

void ATUIRenderer::SetUIManager(ATUIManager *m) {
	if (m) {
		m->GetMainWindow()->AddChild(mpContainer);

		ATUIContainer *c = mpContainer;

		for(int i = 14; i >= 0; --i)
			c->AddChild(mpDiskDriveIndicatorLabels[i]);

		c->AddChild(mpFpsLabel);
		c->AddChild(mpCassetteLabel);
		c->AddChild(mpCassetteTimeLabel);

		for(int i=0; i<2; ++i)
			c->AddChild(mpLedLabels[i]);

		for(auto&& p : mpHeldButtonLabels)
			c->AddChild(p);

		c->AddChild(mpPendingHeldKeyLabel);

		c->AddChild(mpHostDeviceLabel);
		c->AddChild(mpPCLinkLabel);
		c->AddChild(mpRecordingLabel);
		c->AddChild(mpTracingLabel);
		c->AddChild(mpHardDiskDeviceLabel);
		c->AddChild(mpFlashWriteLabel);
		c->AddChild(mpStatusMessageLabel);

		for(int i=0; i<8; ++i)
			c->AddChild(mpWatchLabels[i]);

		c->AddChild(mpAudioDisplays[0]);
		c->AddChild(mpAudioDisplays[1]);
		c->AddChild(mpAudioStatusDisplay);
		c->AddChild(mpPausedLabel);
		c->AddChild(mpHoverTip);

		// update fonts
		mpSysFont = m->GetThemeFont(kATUIThemeFont_Header);
		mpSmallMonoSysFont = m->GetThemeFont(kATUIThemeFont_MonoSmall);
		mpSysMonoFont = m->GetThemeFont(kATUIThemeFont_Mono);
		mpSysHoverTipFont = m->GetThemeFont(kATUIThemeFont_Tooltip);
		mpSysBoldHoverTipFont = m->GetThemeFont(kATUIThemeFont_TooltipBold);

		if (mpSysFont) {
			vdsize32 digitSize = mpSysFont->MeasureString(L"0123456789", 10, false);
			mSysFontDigitWidth = digitSize.w / 10;
			mSysFontDigitHeight = digitSize.h;

			VDDisplayFontMetrics sysFontMetrics;
			mpSysFont->GetMetrics(sysFontMetrics);
			mSysFontDigitAscent = sysFontMetrics.mAscent;
		}

		mSysMonoFontHeight = 0;

		if (mpSysMonoFont) {
			VDDisplayFontMetrics metrics;
			mpSysMonoFont->GetMetrics(metrics);

			mSysMonoFontHeight = metrics.mAscent + metrics.mDescent;
		}

		RemakeLEDFont();

		for(int i=0; i<15; ++i) {
			mpDiskDriveIndicatorLabels[i]->SetFont(mpSysFont);
			mpDiskDriveIndicatorLabels[i]->SetBoldFont(mpLEDFont);
		}

		for(int i=0; i<8; ++i)
			mpWatchLabels[i]->SetFont(mpSysFont);

		mpStatusMessageLabel->SetFont(mpSysFont);
		mpFpsLabel->SetFont(mpSysFont);
		mpAudioStatusDisplay->SetFont(mpSysFont);
		mpAudioStatusDisplay->AutoSize();

		for(ATUIAudioDisplay *disp : mpAudioDisplays) {
			disp->SetBigFont(mpSysMonoFont);
			disp->SetSmallFont(mpSmallMonoSysFont);
		}

		mpHardDiskDeviceLabel->SetFont(mpSysFont);
		mpRecordingLabel->SetFont(mpSysFont);
		mpTracingLabel->SetFont(mpSysFont);
		mpFlashWriteLabel->SetFont(mpSysFont);
		mpFlashWriteLabel->AutoSize();

		for(int i=0; i<2; ++i) {
			mpLedLabels[i]->SetFont(mpSysFont);
			mpLedLabels[i]->AutoSize();
		}

		for(auto&& p : mpHeldButtonLabels) {
			p->SetFont(mpSysFont);
			p->AutoSize();
		}

		mpPendingHeldKeyLabel->SetFont(mpSysFont);
		mpPendingHeldKeyLabel->AutoSize();

		mpPCLinkLabel->SetFont(mpSysFont);
		mpHostDeviceLabel->SetFont(mpSysFont);
		mpCassetteLabel->SetFont(mpSysFont);
		mpCassetteLabel->AutoSize();
		mpCassetteTimeLabel->SetFont(mpSysFont);
		mpPausedLabel->SetFont(mpSysFont);
		mpPausedLabel->AutoSize();
		mpHoverTip->SetFont(mpSysHoverTipFont);
		mpHoverTip->SetBoldFont(mpSysBoldHoverTipFont);

		// update layout
		InvalidateLayout();
	}
}

void ATUIRenderer::Update() {
	uint32 statusFlags = mStatusFlags | mStickyStatusFlags;
	mStickyStatusFlags = mStatusFlags;

	int x = mPrevLayoutWidth;
	int y = mPrevLayoutHeight - mSysFontDigitHeight;

	const uint32 diskErrorFlags = (VDGetCurrentTick() % 1000) >= 500 ? mDiskErrorFlags : 0;
	VDStringW s;

	for(int i = 14; i >= 0; --i) {
		ATUILabel& label = *mpDiskDriveIndicatorLabels[i];
		const uint32 flag = (1 << i);
		sint32 leds = mStatusLEDs[i];

		const bool isActive = ((diskErrorFlags | statusFlags) & flag) != 0;
		const bool shouldShow = ((statusFlags | mDiskMotorFlags | diskErrorFlags) & flag) != 0;
		if (leds >= 0 || shouldShow) {
			if (leds >= 0) {
				s.clear();
				if (shouldShow)
					s.append_sprintf(L"%u  ", mStatusCounter[i]);
				s += L"<bg=#404040><fg=#ff4018> <b>";
				s += (wchar_t)(((uint32)leds & 0xFF) + 0x80);
				s += (wchar_t)((((uint32)leds >> 8) & 0xFF) + 0x80);
				s += L"</b> </fg></bg>";
				label.SetHTMLText(s.c_str());
			} else {
				label.SetTextF(L"%u", mStatusCounter[i]);
			}

			label.AutoSize(x, mPrevLayoutHeight - mSysFontDigitHeight);

			x -= label.GetArea().width();
			label.SetPosition(vdpoint32(x, y));

			label.SetTextColor(0xFF000000);
			label.SetFillColor(kDiskColors[i & 7][isActive]);
			label.SetVisible(true);
		} else {
			label.SetVisible(false);
			x -= mSysFontDigitWidth;
		}
	}

	if (statusFlags & 0x10000) {
		mpCassetteLabel->SetVisible(true);

		mShowCassetteIndicatorCounter = 60;
	} else {
		mpCassetteLabel->SetVisible(false);

		if (mbShowCassetteIndicator)
			mShowCassetteIndicatorCounter = 60;
	}

	if (mShowCassetteIndicatorCounter) {
		--mShowCassetteIndicatorCounter;

		mpCassetteTimeLabel->SetVisible(true);
	} else {
		mpCassetteTimeLabel->SetVisible(false);
	}

	// draw H: indicators
	bool updateH = false;

	if (mHReadCounter) {
		--mHReadCounter;

		if (mHReadCounter == 24)
			updateH = true;
		else if (!mHReadCounter && !mHWriteCounter)
			updateH = true;
	}

	if (mHWriteCounter) {
		--mHWriteCounter;

		if (mHWriteCounter == 24)
			updateH = true;
		else if (!mHWriteCounter && !mHReadCounter)
			updateH = true;
	}

	if (updateH)
		UpdateHostDeviceLabel();

	// draw PCLink indicators (same place as H:)
	if (mPCLinkReadCounter || mPCLinkWriteCounter) {
		if (mPCLinkReadCounter)
			--mPCLinkReadCounter;

		if (mPCLinkWriteCounter)
			--mPCLinkWriteCounter;

		UpdatePCLinkLabel();
	}

	// draw H: indicators
	if (mbHardDiskRead || mbHardDiskWrite) {
		if (!--mHardDiskCounter) {
			mbHardDiskRead = false;
			mbHardDiskWrite = false;
		}
	} else {
		mpHardDiskDeviceLabel->SetVisible(false);
	}

	// draw flash write counter
	if (mFlashWriteCounter) {
		if (!--mFlashWriteCounter)
			mpFlashWriteLabel->SetVisible(false);
	}

	// draw watched values
	for(int i=0; i<8; ++i) {
		int len = mWatchedValueLens[i];
		if (len < 0)
			continue;

		ATUILabel& label = *mpWatchLabels[i];

		switch(len) {
			case 0:
				label.SetTextF(L"%d", (int)mWatchedValues[i]);
				break;
			case 1:
				label.SetTextF(L"%02X", mWatchedValues[i]);
				break;
			case 2:
				label.SetTextF(L"%04X", mWatchedValues[i]);
				break;
		}

		label.AutoSize();
	}

	// update audio monitor
	for(ATUIAudioDisplay *disp : mpAudioDisplays)
		disp->Update();

	// update indicator safe area
	sint32 ish = mSysFontDigitHeight * 2 + 6;

	if (mIndicatorSafeHeight != ish) {
		mIndicatorSafeHeight = ish;

		mIndicatorSafeAreaListeners.Notify([](const vdfunction<void()> *pfn) { (*pfn)(); return false; });
	}
}

sint32 ATUIRenderer::GetIndicatorSafeHeight() const {
	return mpContainer->IsVisible() ? mIndicatorSafeHeight : 0;
}

void ATUIRenderer::AddIndicatorSafeHeightChangedHandler(const vdfunction<void()> *pfn) {
	mIndicatorSafeAreaListeners.Add(pfn);
}

void ATUIRenderer::RemoveIndicatorSafeHeightChangedHandler(const vdfunction<void()> *pfn) {
	mIndicatorSafeAreaListeners.Remove(pfn);
}

void ATUIRenderer::TimerCallback() {
	mStatusMessage.clear();

	if (mModemConnection.empty())
		mpStatusMessageLabel->SetVisible(false);
	else {
		mpStatusMessageLabel->SetVisible(true);
		mpStatusMessageLabel->SetFillColor(kModemMessageBkColor);
		mpStatusMessageLabel->SetTextColor(kModemMessageFgColor);
		mpStatusMessageLabel->SetBorderColor(kModemMessageFgColor);
		mpStatusMessageLabel->SetText(mModemConnection.c_str());
		mpStatusMessageLabel->AutoSize();
	}
}

void ATUIRenderer::InvalidateLayout() {
	Relayout(mPrevLayoutWidth, mPrevLayoutHeight);
}

void ATUIRenderer::Relayout(int w, int h) {
	mPrevLayoutWidth = w;
	mPrevLayoutHeight = h;

	mpFpsLabel->SetPosition(vdpoint32(w - 10 * mSysFontDigitWidth, 10));
	mpStatusMessageLabel->SetPosition(vdpoint32(1, h - mSysFontDigitHeight * 2 - 4));

	const vdrect32 rdisp0 = mpAudioDisplays[0]->GetArea();
	mpAudioDisplays[0]->SetPosition(vdpoint32(8, h - rdisp0.height() - mSysFontDigitHeight * 4));

	const vdrect32 rdisp1 = mpAudioDisplays[1]->GetArea();
	mpAudioDisplays[1]->SetPosition(vdpoint32(std::max(rdisp0.right, w - rdisp1.width()), h - rdisp1.height() - mSysFontDigitHeight * 4));

	for(int i=0; i<8; ++i) {
		ATUILabel& label = *mpWatchLabels[i];

		int y = h - 4*mSysFontDigitHeight - (7 - i)*mSysMonoFontHeight;

		label.SetPosition(vdpoint32(64, y));
	}

	int ystats = h - mSysFontDigitHeight;

	mpHardDiskDeviceLabel->SetPosition(vdpoint32(mSysFontDigitWidth * 36, ystats));
	mpRecordingLabel->SetPosition(vdpoint32(mSysFontDigitWidth * 27, ystats));
	mpTracingLabel->SetPosition(vdpoint32(mSysFontDigitWidth * 37, ystats));
	mpFlashWriteLabel->SetPosition(vdpoint32(mSysFontDigitWidth * 47, ystats));
	mpPCLinkLabel->SetPosition(vdpoint32(mSysFontDigitWidth * 19, ystats));
	mpHostDeviceLabel->SetPosition(vdpoint32(mSysFontDigitWidth * 19, ystats));

	for(int i=0; i<2; ++i)
		mpLedLabels[i]->SetPosition(vdpoint32(mSysFontDigitWidth * (11+i), ystats));

	mpCassetteLabel->SetPosition(vdpoint32(0, ystats));
	mpCassetteTimeLabel->SetPosition(vdpoint32(mpCassetteLabel->GetArea().width(), ystats));

	const int ystats2 = ystats - (mSysFontDigitHeight * 5) / 4;
	const int ystats3 = ystats2 - (mSysFontDigitHeight * 5) / 4;
	int x = w;

	for(int i=(int)vdcountof(mpHeldButtonLabels)-1; i>=0; --i) {
		ATUILabel& label = *mpHeldButtonLabels[i];

		x -= label.GetArea().width();
		label.SetPosition(vdpoint32(x, ystats2));
	}

	RelayoutPendingKeyLabels();

	mpAudioStatusDisplay->SetPosition(vdpoint32(16, 16));

	mpPausedLabel->SetPosition(vdpoint32((w - mpPausedLabel->GetArea().width()) >> 1, 64));

	UpdateHoverTipPos();
}

void ATUIRenderer::UpdatePendingHoldLabel() {
	if (mbPendingHoldMode || mPendingHeldButtons || mPendingHeldKey >= 0) {
		VDStringW s;

		if (mbPendingHoldMode)
			s = L"Press keys to hold on next reset: ";

		if (mPendingHeldButtons & 1)
			s += L"Start+";

		if (mPendingHeldButtons & 2)
			s += L"Select+";

		if (mPendingHeldButtons & 4)
			s += L"Option+";

		if (mPendingHeldKey >= 0) {
			const wchar_t *label = ATUIGetNameForKeyCode((uint8)mPendingHeldKey);
			
			if (label)
				s += label;
			else
				s.append_sprintf(L"[$%02X]", mPendingHeldKey);
		}

		if (!s.empty() && s.back() == L'+')
			s.pop_back();

		mpPendingHeldKeyLabel->SetText(s.c_str());

		mpPendingHeldKeyLabel->AutoSize();
		mpPendingHeldKeyLabel->SetVisible(true);

		RelayoutPendingKeyLabels();
	} else {
		mpPendingHeldKeyLabel->SetVisible(false);
	}
}

void ATUIRenderer::RelayoutPendingKeyLabels() {
	const int h = mPrevLayoutHeight;
	const int ystats3 = h - mSysFontDigitHeight - ((mSysFontDigitHeight * 5) / 4) * 2;
	int x = mPrevLayoutWidth;

	if (mpPendingHeldKeyLabel->IsVisible()) {
		x -= mpPendingHeldKeyLabel->GetArea().width();
		mpPendingHeldKeyLabel->SetPosition(vdpoint32(x, ystats3));
	}
}

void ATUIRenderer::UpdateHostDeviceLabel() {
	if (!mHReadCounter && !mHWriteCounter) {
		mpHostDeviceLabel->SetVisible(false);
		return;
	}

	mpHostDeviceLabel->Clear();
	mpHostDeviceLabel->AppendFormattedText(0, L"H:");

	mpHostDeviceLabel->AppendFormattedText(
		mHReadCounter >= 25 ? 0xFFFFFF : mHReadCounter ? 0x000000 : 0x007920,
		L"R");
	mpHostDeviceLabel->AppendFormattedText(
		mHWriteCounter >= 25 ? 0xFFFFFF : mHWriteCounter ? 0x000000 : 0x007920,
		L"W");

	mpHostDeviceLabel->AutoSize();
	mpHostDeviceLabel->SetVisible(true);
}

void ATUIRenderer::UpdatePCLinkLabel() {
	if (!mPCLinkReadCounter && !mPCLinkWriteCounter) {
		mpPCLinkLabel->SetVisible(false);
		return;
	}

	mpPCLinkLabel->Clear();
	mpPCLinkLabel->AppendFormattedText(0, L"PCL:");

	mpPCLinkLabel->AppendFormattedText(
		mPCLinkReadCounter >= 25 ? 0xFFFFFF : mPCLinkReadCounter ? 0x000000 : 0x007920,
		L"R");
	mpPCLinkLabel->AppendFormattedText(
		mPCLinkWriteCounter >= 25 ? 0xFFFFFF : mPCLinkWriteCounter ? 0x000000 : 0x007920,
		L"W");

	mpPCLinkLabel->AutoSize();
	mpPCLinkLabel->SetVisible(true);
}

void ATUIRenderer::UpdateHoverTipPos() {
	if (mpHoverTip->IsVisible()) {
		const vdsize32 htsize = mpHoverTip->GetArea().size();

		int x = mHoverTipX;
		int y = mHoverTipY + 32;

		if (x + htsize.w > mPrevLayoutWidth)
			x = std::max<int>(0, mPrevLayoutWidth - htsize.w);

		if (y + htsize.h > mPrevLayoutHeight) {
			int y2 = y - 32 - htsize.h;

			if (y2 >= 0)
				y = y2;
		}

		mpHoverTip->SetPosition(vdpoint32(x, y));
	}
}

void ATUIRenderer::RemakeLEDFont() {
	if (mpLEDFont) {
		if (mLEDFontCellWidth == mSysFontDigitWidth && mLEDFontCellAscent == mSysFontDigitAscent)
			return;
	}

	class RefCountedBitmap : public vdrefcounted<VDPixmapBuffer, IVDRefCount> {};
	vdrefptr<RefCountedBitmap> p(new RefCountedBitmap);

	mLEDFontCellWidth = mSysFontDigitWidth;
	mLEDFontCellAscent = mSysFontDigitAscent;

	wchar_t chars[128] = {};
	for(uint32 i=0; i<128; ++i)
		chars[i] = (wchar_t)(i + 0x80);

	int w = mLEDFontCellWidth;
	int h = mLEDFontCellAscent;
	int pad = 8;
	int tw = w * 8 + 16;
	int th = h * 8 + 16;

	while(tw < 128 && th < 128) {
		tw += tw;
		th += th;
		pad += pad;
	}

	VDPixmapBuffer tempBuf(tw, th, nsVDPixmap::kPixFormat_XRGB8888);

	p->init(w + 2, (h + 2) * 128, nsVDPixmap::kPixFormat_XRGB8888);

	VDPixmap pxDstCell = *p;
	pxDstCell.w = w + 2;
	pxDstCell.h = h + 2;

	VDDisplayRendererSoft rs;
	rs.Init();
	rs.Begin(tempBuf);
	
	const int stemWidth = std::min<int>(tw, th) / 10;
	const int endOffset = tw / 16;
	const int gridX1 = pad + tw / 6;
	const int gridX2 = pad + tw - tw / 6;
	const int gridY1 = pad + th / 6;
	const int gridY2 = pad + th / 2;
	const int gridY3 = pad + th - th / 6;
	const int descent = (th - (gridY3 + stemWidth)) / pad;

	const vdrect32 segmentRects[7]={
		vdrect32(gridX1 + endOffset, gridY1 - stemWidth, gridX2 - endOffset, gridY1 + stemWidth),	// A (top)
		vdrect32(gridX2 - stemWidth, gridY1 + endOffset, gridX2 + stemWidth, gridY2 - endOffset),	// B (top right)
		vdrect32(gridX2 - stemWidth, gridY2 + endOffset, gridX2 + stemWidth, gridY3 - endOffset),	// C (bottom right)
		vdrect32(gridX1 + endOffset, gridY3 - stemWidth, gridX2 - endOffset, gridY3 + stemWidth),	// D (bottom)
		vdrect32(gridX1 - stemWidth, gridY2 + endOffset, gridX1 + stemWidth, gridY3 - endOffset),	// E (bottom left)
		vdrect32(gridX1 - stemWidth, gridY1 + endOffset, gridX1 + stemWidth, gridY2 - endOffset),	// F (top left)
		vdrect32(gridX1 + endOffset, gridY2 - stemWidth, gridX2 - endOffset, gridY2 + stemWidth),	// G (center)
	};

	for(uint32 i=0; i<128; ++i) {
		rs.SetColorRGB(0);
		rs.FillRect(0, 0, tw, th);

		rs.SetColorRGB(0xFFFFFF);
		for(uint32 bit=0; bit<7; ++bit) {
			if (i & (1 << bit)) {
				const auto& r = segmentRects[bit];
				rs.FillRect(r.left, r.top, r.width(), r.height());
			}
		}

		pxDstCell.data = (char *)p->data + (p->pitch * (h + 2)) * i;
		VDPixmapResample(pxDstCell, tempBuf, IVDPixmapResampler::kFilterLinear);
	}

	vdblock<VDDisplayBitmapFontGlyphInfo> glyphInfos(128);
	int cellPad = std::max<int>(1, mLEDFontCellWidth / 10 + 1);
	for(uint32 i=0; i<128; ++i) {
		auto& gi = glyphInfos[i];
		gi.mAdvance = mLEDFontCellWidth + (cellPad + 1) / 2;
		gi.mCellX = -1 + cellPad / 2;
		gi.mCellY = -(mLEDFontCellAscent + 1) + descent;
		gi.mWidth = mLEDFontCellWidth + 2;
		gi.mHeight = mLEDFontCellAscent + 2;
		gi.mBitmapX = 0;
		gi.mBitmapY = (mLEDFontCellAscent + 2) * i;
	}

	VDDisplayFontMetrics metrics {};
	metrics.mAscent = mLEDFontCellAscent - descent;
	metrics.mDescent = descent;
	VDCreateDisplayBitmapFont(metrics, 128, chars, glyphInfos.data(), *p, 0, p, ~mpLEDFont);
}
