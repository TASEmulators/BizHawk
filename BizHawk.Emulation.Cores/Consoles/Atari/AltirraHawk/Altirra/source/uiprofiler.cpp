#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/time.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atcore/profile.h>
#include <at/atui/uiwidget.h>
#include "uiprofiler.h"
#include <at/atui/uicontainer.h>
#include <at/atui/uimanager.h>

class ATUIProfilerWindow final : public ATUIWidget, public IATProfiler {
public:
	ATUIProfilerWindow();

	void OnEvent(ATProfileEvent event);
	void BeginRegion(ATProfileRegion region);
	void EndRegion(ATProfileRegion region);

	void AutoSize(const vdpoint32& origin);

protected:
	void OnCreate();
	void OnDestroy();

	void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	void RecordRegion(ATProfileRegion region, uint64 t);

	int mX;
	int mLastY;
	int mMaxY;
	int mRegionStackHt;
	uint64 mFrameStartTime;
	double mPreciseTicksToPixels;

	uint32 mRegionRectStarts[kATProfileRegionCount];

	vdrefptr<IVDDisplayFont> mpFont;

	vdvector<vdrect32> mRegionRects[kATProfileRegionCount];
	ATProfileRegion mRegionStack[64];
};

ATUIProfilerWindow *g_pATUIProfilerWindow;

ATUIProfilerWindow::ATUIProfilerWindow()
	: mX(0)
	, mLastY(0)
	, mMaxY(200)
	, mRegionStackHt(0)
	, mFrameStartTime(0)
	, mPreciseTicksToPixels(0)
{
	mbHitTransparent = true;

	mMaxY = 200;
	mFrameStartTime = VDGetPreciseTick();
	mPreciseTicksToPixels = VDGetPreciseSecondsPerTick() * (double)mMaxY * 30.0f;

	std::fill(mRegionRectStarts, mRegionRectStarts + kATProfileRegionCount, 0);
}

void ATUIProfilerWindow::OnEvent(ATProfileEvent event) {
	if (event != kATProfileEvent_BeginFrame)
		return;

	Invalidate();

	mRegionStackHt = 0;
	mLastY = 0;

	mX = (mX + 1) & 255;
	mFrameStartTime = VDGetPreciseTick();

	uint32 xdel = (mX + 1) & 255;
	for(int i=0; i<kATProfileRegionCount; ++i) {
		vdvector<vdrect32>& v = mRegionRects[i];
		uint32& regionStart = mRegionRectStarts[i];
		uint32 n = (uint32)v.size();

		while(regionStart < n && v[regionStart].left == xdel)
			++regionStart;

		if (!(xdel & 0x7F)) {
			v.erase(v.begin(), v.begin() + regionStart);
			regionStart = 0;
		}
	}
}

void ATUIProfilerWindow::BeginRegion(ATProfileRegion region) {
	if (mRegionStackHt < vdcountof(mRegionStack)) {
		if (mRegionStackHt) {
			uint64 t = VDGetPreciseTick();

			RecordRegion(mRegionStack[mRegionStackHt - 1], t);
		}

		mRegionStack[mRegionStackHt++] = region;
	}
}

void ATUIProfilerWindow::EndRegion(ATProfileRegion) {
	if (mRegionStackHt) {
		uint64 t = VDGetPreciseTick();

		RecordRegion(mRegionStack[--mRegionStackHt], t);
	}
}

void ATUIProfilerWindow::AutoSize(const vdpoint32& origin) {
	int w = 256;
	int h = 200 + 4;

	if (mpFont) {
		VDDisplayFontMetrics metrics;
		mpFont->GetMetrics(metrics);

		h += (metrics.mAscent + metrics.mDescent)*kATProfileRegionCount;
	}

	SetArea(vdrect32(16, 48, 16 + w, 48 + h));
}

void ATUIProfilerWindow::OnCreate() {
	mpFont = mpManager->GetThemeFont(kATUIThemeFont_Default);

	g_pATProfiler = this;
}

void ATUIProfilerWindow::OnDestroy() {
	g_pATProfiler = nullptr;

	mpFont.clear();
}

void ATUIProfilerWindow::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	static const uint32 kColors[]={
		0x808080,
		0x4060E0,
		0xE02010,
		0x20E010,
	};

	VDASSERTCT(vdcountof(kColors) == kATProfileRegionCount);

	for(int i=0; i<kATProfileRegionCount; ++i) {
		uint32 n = (uint32)mRegionRects[i].size();
		uint32 pos = mRegionRectStarts[i];

		if (pos < n) {
			rdr.SetColorRGB(kColors[i]);
			rdr.MultiFillRect(&mRegionRects[i][pos], n - pos);
		}
	}

	if (mpFont) {
		VDDisplayTextRenderer& tr = *rdr.GetTextRenderer();

		tr.SetFont(mpFont);
		tr.SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);

		int x = 48;
		int y = 204;
		static const wchar_t *const kNames[]={
			L"Idle",
			L"Simulation",
			L"Native messages",
			L"Display tick",
		};

		VDASSERTCT(vdcountof(kNames) == kATProfileRegionCount);

		VDDisplayFontMetrics metrics;
		mpFont->GetMetrics(metrics);

		for(size_t i=0; i<vdcountof(kNames); ++i) {
			rdr.SetColorRGB(kColors[i]);
			rdr.FillRect(4, y + (metrics.mAscent + metrics.mDescent)*(int)i, 40, metrics.mAscent + metrics.mDescent);
		}

		tr.SetColorRGB(0xFFFFFF);
		for(size_t i=0; i<vdcountof(kNames); ++i) {
			tr.DrawTextLine(x, y, kNames[i]);
			y += metrics.mAscent + metrics.mDescent;
		}
	}
}

void ATUIProfilerWindow::RecordRegion(ATProfileRegion region, uint64 t) {
	int y = VDRoundToInt((double)(t - mFrameStartTime) * mPreciseTicksToPixels);

	if (y < 0)
		return;

	if (y > mMaxY)
		y = mMaxY;

	if (y > mLastY) {
		vdvector<vdrect32>& v = mRegionRects[region];
		int y1 = mMaxY-y;
		int y2 = mMaxY-mLastY;

		if (!v.empty() && v.back().left == mX && v.back().top == y2)
			v.back().top = y1;
		else
			v.push_back(vdrect32(mX, y1, mX+1, y2));

		mLastY = y;
	}
}

void ATUIProfileCreateWindow(ATUIManager *m) {
	if (!g_pATUIProfilerWindow) {
		g_pATUIProfilerWindow = new ATUIProfilerWindow;
		g_pATUIProfilerWindow->AddRef();
		m->GetMainWindow()->AddChild(g_pATUIProfilerWindow);
		g_pATUIProfilerWindow->AutoSize(vdpoint32(16, 48));
	}
}

void ATUIProfileDestroyWindow() {
	if (g_pATUIProfilerWindow) {
		g_pATUIProfilerWindow->Destroy();
		g_pATUIProfilerWindow->Release();
		g_pATUIProfilerWindow = NULL;
	}
}
