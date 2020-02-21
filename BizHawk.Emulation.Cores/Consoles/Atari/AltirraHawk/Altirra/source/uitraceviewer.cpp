//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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
#include <vd2/system/binary.h>
#include <vd2/system/registry.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/resample.h>
#include <vd2/Riza/bitmap.h>
#include <at/atdebugger/target.h>
#include <at/atio/cassetteimage.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiframe.h>
#include <windows.h>
#include <commctrl.h>
#include "cassette.h"
#include "console.h"
#include "oshelper.h"
#include "resource.h"
#include "simulator.h"
#include "trace.h"
#include "tracecpu.h"
#include "tracetape.h"
#include "tracevideo.h"
#include "uihistoryview.h"
#include "profiler.h"
#include "profilerui.h"

extern ATSimulator g_sim;

///////////////////////////////////////////////////////////////////////////

namespace {
	static constexpr uint32 kColor_PanelBackground = 0xD8D8D8;
	static constexpr uint32 kColor_TimescaleBackground = 0xE8E8E8;
	static constexpr uint32 kColor_ChannelBackground = 0xE6E4E4;
}

///////////////////////////////////////////////////////////////////////////

template<typename T>
class ATGDIObjectW32 {
public:
	vdnothrow ATGDIObjectW32() vdnoexcept : mHandle(nullptr), mpBlock(nullptr){}
	vdnothrow ATGDIObjectW32(T handle) vdnoexcept : ATGDIObjectW32() { reset(handle); }
	vdnothrow ATGDIObjectW32(const ATGDIObjectW32&) vdnoexcept;

	vdnothrow ATGDIObjectW32(ATGDIObjectW32&& src) vdnoexcept {
		mHandle = src.mHandle;
		mpBlock = src.mpBlock;
		src.mHandle = nullptr;
		src.mpBlock = nullptr;
	}

	~ATGDIObjectW32();

	vdnothrow ATGDIObjectW32& operator=(const ATGDIObjectW32&) vdnoexcept;
	vdnothrow ATGDIObjectW32& operator=(ATGDIObjectW32&& src) vdnoexcept;

	T get() const { return mHandle; }
	vdnothrow void reset() vdnoexcept;
	void reset(T h);

private:
	struct RefCountBlock {
		T mHandle;
		VDAtomicInt mRefCount;

		vdnothrow ~RefCountBlock() vdnoexcept;
	};

	T mHandle;
	RefCountBlock *mpBlock;
};

template<typename T>
vdnothrow ATGDIObjectW32<T>::RefCountBlock::~RefCountBlock() vdnoexcept {
	VDVERIFY(DeleteObject(mHandle));
}

template<typename T>
ATGDIObjectW32<T>::~ATGDIObjectW32() {
	reset();
}

template<typename T>
vdnothrow ATGDIObjectW32<T>::ATGDIObjectW32(const ATGDIObjectW32& src) vdnoexcept
	: mHandle(src.mHandle)
	, mpBlock(src.mpBlock)
{
	if (mpBlock)
		++mpBlock->mRefCount;
}

template<typename T>
vdnothrow ATGDIObjectW32<T>& ATGDIObjectW32<T>::operator=(const ATGDIObjectW32& src) vdnoexcept {
	if (src.mpBlock != mpBlock) {
		if (mpBlock && !--mpBlock->mRefCount)
			delete mpBlock;

		mpBlock = src.mpBlock;
		mHandle = src.mHandle;

		if (mpBlock)
			++mpBlock->mRefCount;
	}

	return *this;
}

template<typename T>
vdnothrow ATGDIObjectW32<T>& ATGDIObjectW32<T>::operator=(ATGDIObjectW32&& src) vdnoexcept {
	if (&src != this) {
		reset();

		mHandle = src.mHandle;
		mpBlock = src.mpBlock;
		src.mHandle = nullptr;
		src.mpBlock = nullptr;
	}

	return *this;
}

template<typename T>
vdnothrow void ATGDIObjectW32<T>::reset() vdnoexcept {
	if (mpBlock && !--mpBlock->mRefCount)
		delete mpBlock;

	mHandle = nullptr;
	mpBlock = nullptr;
}

template<typename T>
void ATGDIObjectW32<T>::reset(T h) {
	RefCountBlock *b = h ? new RefCountBlock { h, 1 } : nullptr;
	reset();

	mHandle = h;
	mpBlock = b;
}

typedef ATGDIObjectW32<HFONT> ATGDIFontHandleW32;

///////////////////////////////////////////////////////////////////////////

void ATTraceLoadDefaults(ATTraceSettings& settings) {
	VDRegistryAppKey key("Debugger", false);

	settings = {};
	settings.mbTraceVideo = key.getBool("Trace: Enable video", true);
	settings.mTraceVideoDivisor = key.getInt("Trace: Video divisor", 1);
	settings.mbTraceCpuInsns = key.getBool("Trace: Enable CPU insns", true);
	settings.mbTraceBasic = key.getBool("Trace: Enable BASIC", false);
	settings.mbAutoLimitTraceMemory = key.getBool("Trace: Auto-limit trace memory", true);
}

void ATTraceSaveDefaults(const ATTraceSettings& settings) {
	VDRegistryAppKey key("Debugger");

	key.setBool("Trace: Enable video", settings.mbTraceVideo);
	key.setInt("Trace: Video divisor", settings.mTraceVideoDivisor);
	key.setBool("Trace: Enable CPU insns", settings.mbTraceCpuInsns);
	key.setBool("Trace: Enable BASIC", settings.mbTraceBasic);
	key.setBool("Trace: Auto-limit trace memory", settings.mbAutoLimitTraceMemory);
}

///////////////////////////////////////////////////////////////////////////

class ATUIDialogTraceSettings final : public VDDialogFrameW32 {
public:
	ATUIDialogTraceSettings(ATTraceSettings& settings);

	bool OnLoaded() override;
	void OnDataExchange(bool write) override;

private:
	void UpdateEnables();
	
	ATTraceSettings& mSettings;
	VDUIProxyButtonControl mVideoButton;
	VDUIProxyComboBoxControl mVideoRateCombo;
};

ATUIDialogTraceSettings::ATUIDialogTraceSettings(ATTraceSettings& settings)
	: VDDialogFrameW32(IDD_TRACE_SETTINGS)
	, mSettings(settings)
{
	mVideoButton.SetOnClicked([this] { UpdateEnables(); });
}

bool ATUIDialogTraceSettings::OnLoaded() {
	AddProxy(&mVideoButton, IDC_TRACE_VIDEO);
	AddProxy(&mVideoRateCombo, IDC_VIDEO_RATE);
	mVideoRateCombo.AddItem(L"All frames");
	mVideoRateCombo.AddItem(L"Every two frames");
	mVideoRateCombo.AddItem(L"Every three frames");

	return VDDialogFrameW32::OnLoaded();
}

void ATUIDialogTraceSettings::OnDataExchange(bool write) {
	ExchangeControlValueBoolCheckbox(write, IDC_TRACE_VIDEO, mSettings.mbTraceVideo);
	ExchangeControlValueBoolCheckbox(write, IDC_TRACE_HISTORY, mSettings.mbTraceCpuInsns);
	ExchangeControlValueBoolCheckbox(write, IDC_TRACE_BASIC, mSettings.mbTraceBasic);
	ExchangeControlValueBoolCheckbox(write, IDC_LIMIT_MEMORY, mSettings.mbAutoLimitTraceMemory);

	if (write) {
		mSettings.mTraceVideoDivisor = mVideoRateCombo.GetSelection() + 1;
	} else {
		switch(mSettings.mTraceVideoDivisor) {
			case 0:
			case 1:
				mVideoRateCombo.SetSelection(0);
				break;

			case 2:
				mVideoRateCombo.SetSelection(1);
				break;

			case 3:
			default:
				mVideoRateCombo.SetSelection(2);
				break;
		}

		UpdateEnables();
	}
}

void ATUIDialogTraceSettings::UpdateEnables() {
	mVideoRateCombo.SetEnabled(mVideoButton.GetChecked());
}

///////////////////////////////////////////////////////////////////////////

struct ATUITraceViewerChannel {
	enum Type {
		kType_Default,
		kType_Video,
		kType_Tape,
	};

	sint32 mPosY;
	sint32 mHeight;
	Type mType;
	vdrefptr<IATTraceChannel> mpChannel;
};

struct ATUITraceViewerGroup {
	VDStringW mName;
	sint32 mPosY;
	sint32 mHeight;

	vdrefptr<ATTraceGroup> mpGroup;
	vdfastvector<ATUITraceViewerChannel *> mChannels;
};

class IATUITraceViewer {
public:
	virtual void ScrollDeltaPixels(sint32 dx, sint32 dy) = 0;
	virtual void ZoomDeltaSteps(double centerTime, sint32 steps) = 0;
	virtual void SetFocusTime(double t) = 0;
	virtual void SetSelection(double startTime, double endTime) = 0;
	virtual void SetSelectionMode(bool selMode) = 0;
};

struct ATUITraceViewerContext {
	int mDpi = 0;
	bool mbSelectionMode = false;

	ATGDIFontHandleW32 mChannelFont;
	TEXTMETRICW mChannelFontMetrics;
	ATGDIFontHandleW32 mTimestampFont;
	TEXTMETRICW mTimestampFontMetrics;
	ATGDIFontHandleW32 mEventFont;
	TEXTMETRICW mEventFontMetrics;

	IATUITraceViewer *mpParent;
	vdfastvector<ATUITraceViewerGroup *> mGroups;

	vdrefptr<ATTraceChannelCPUHistory> mpCPUHistoryChannel;
};

///////////////////////////////////////////////////////////////////////////

class ATUITraceViewerCPUHistoryView final : public VDDialogFrameW32, private IATUIHistoryModel {
public:
	ATUITraceViewerCPUHistoryView(ATUITraceViewerContext& context);
	~ATUITraceViewerCPUHistoryView();

	void SetCPUTraceChannel(ATTraceChannelCPUHistory *channel);
	void SetCenterTime(double t);

private:
	double DecodeTapeSample(uint32 cycle) override;
	double DecodeTapeSeconds(uint32 cycle) override;
	uint32 ConvertRawTimestamp(uint32 rawCycle) override;
	float ConvertRawTimestampDeltaF(sint32 rawCycleDelta) override;
	ATCPUBeamPosition DecodeBeamPosition(uint32 cycle) override;
	bool IsInterruptPositionVBI(uint32 cycle) override;
	bool UpdatePreviewNode(ATCPUHistoryEntry&) override;
	uint32 ReadInsns(const ATCPUHistoryEntry **ppInsns, uint32 startIndex, uint32 n) override;
	void OnEsc() override;
	void OnInsnSelected(uint32 index) override;
	void JumpToInsn(uint32 pc) override;
	void JumpToSource(uint32 pc) override;

private:
	bool OnLoaded() override;

	void OnFontsChanged();
	void RebuildInsnView();
	void PopulateFromNewChannel();

	ATUITraceViewerContext& mContext;
	vdrefptr<ATTraceChannelCPUHistory> mpChannel;
	double mCenterTime = -1;
	double mViewStartTime = 0;
	double mViewEndTime = -1;
	uint32 mNotifyRecursionCounter = 0;

	vdrefptr<IATUIHistoryView> mpHistoryView;
	vdfunction<void()> mpFontCallback;
};

ATUITraceViewerCPUHistoryView::ATUITraceViewerCPUHistoryView(ATUITraceViewerContext& context)
	: VDDialogFrameW32(IDD_TRACEVIEWER_CPUHISTORY)
	, mContext(context)
{
	mpFontCallback = [this]() { OnFontsChanged(); };
	ATConsoleAddFontNotification(&mpFontCallback);
}

ATUITraceViewerCPUHistoryView::~ATUITraceViewerCPUHistoryView() {
	ATConsoleRemoveFontNotification(&mpFontCallback);
}

void ATUITraceViewerCPUHistoryView::SetCPUTraceChannel(ATTraceChannelCPUHistory *channel) {
	if (mpChannel != channel) {
		mpChannel = channel;

		PopulateFromNewChannel();
	}
}

void ATUITraceViewerCPUHistoryView::SetCenterTime(double t) {
	if (mNotifyRecursionCounter)
		return;
	
	++mNotifyRecursionCounter;

	// check if time is within current view
	if (t < mViewStartTime || t > mViewEndTime) {
		// recenter view
		if (mCenterTime != t) {
			mCenterTime = t;

			RebuildInsnView();
		}
	}

	if (mpChannel) {
		// look up and select insn
		mpHistoryView->SelectInsn(mpChannel->FindEvent(t));
	}

	--mNotifyRecursionCounter;
}

double ATUITraceViewerCPUHistoryView::DecodeTapeSample(uint32 cycle) {
	return 0;
}

double ATUITraceViewerCPUHistoryView::DecodeTapeSeconds(uint32 cycle) {
	return 0;
}

uint32 ATUITraceViewerCPUHistoryView::ConvertRawTimestamp(uint32 rawCycle) {
	return rawCycle;
}

float ATUITraceViewerCPUHistoryView::ConvertRawTimestampDeltaF(sint32 rawCycleDelta) {
	return mpChannel ? (float)((double)rawCycleDelta * mpChannel->GetSecondsPerTick()) : 0;
}

ATCPUBeamPosition ATUITraceViewerCPUHistoryView::DecodeBeamPosition(uint32 cycle) {
	return g_sim.GetTimestampDecoder().GetBeamPosition(cycle);
}

bool ATUITraceViewerCPUHistoryView::IsInterruptPositionVBI(uint32 cycle) {
	return g_sim.GetTimestampDecoder().IsInterruptPositionVBI(cycle);
}

bool ATUITraceViewerCPUHistoryView::UpdatePreviewNode(ATCPUHistoryEntry& he) {
	return false;
}

uint32 ATUITraceViewerCPUHistoryView::ReadInsns(const ATCPUHistoryEntry **ppInsns, uint32 startIndex, uint32 n) {
	return mpChannel->ReadHistoryEvents(ppInsns, startIndex, n);
}

void ATUITraceViewerCPUHistoryView::OnEsc() {
}

void ATUITraceViewerCPUHistoryView::OnInsnSelected(uint32 index) {
	if (mNotifyRecursionCounter)
		return;

	if (mpChannel) {
		++mNotifyRecursionCounter;
		mContext.mpParent->SetFocusTime(mpChannel->GetEventTime(index));
		--mNotifyRecursionCounter;
	}
}

void ATUITraceViewerCPUHistoryView::JumpToInsn(uint32 pc) {
}

void ATUITraceViewerCPUHistoryView::JumpToSource(uint32 pc) {
}

bool ATUITraceViewerCPUHistoryView::OnLoaded() {
	ATUICreateHistoryView((VDGUIHandle)mhdlg, ~mpHistoryView);

	mpHistoryView->SetHistoryModel(this);

	OnFontsChanged();

	if (mpHistoryView)
		SetFocus(mpHistoryView->AsNativeWindow()->GetHandleW32());

	const vdsize32 sz = GetClientArea().size();
	mResizer.Add(mpHistoryView->AsNativeWindow()->GetHandleW32(), 0, 0, sz.w, sz.h, mResizer.kMC | mResizer.kAvoidFlicker);

	PopulateFromNewChannel();

	return true;
}

void ATUITraceViewerCPUHistoryView::OnFontsChanged() {
	if (mpHistoryView)
		mpHistoryView->SetFonts(ATConsoleGetPropFontW32(), ATConsoleGetPropFontLineHeightW32(), ATGetConsoleFontW32(), ATGetConsoleFontLineHeightW32());
}

void ATUITraceViewerCPUHistoryView::RebuildInsnView() {
	if (!mpHistoryView || !mpChannel)
		return;

	mpChannel->StartHistoryIteration(mCenterTime, -200000);

	uint32 n = mpChannel->ReadHistoryEvents(nullptr, 0, 400000);

	if (n) {
		mViewStartTime = mpChannel->GetEventTime(0);
		mViewEndTime = mpChannel->GetEventTime(n - 1);
	} else {
		mViewStartTime = 0;
		mViewEndTime = -1;
	}

	mpHistoryView->ClearInsns();
	mpHistoryView->UpdateInsns(0, n);
}

void ATUITraceViewerCPUHistoryView::PopulateFromNewChannel() {
	if (mpHistoryView) {
		mpHistoryView->ClearInsns();

		if (mpChannel && !mpChannel->IsEmpty()) {
			mpHistoryView->SetDisasmMode(mpChannel->GetDisasmMode(), mpChannel->GetSubCycles(), true);
			mpChannel->StartHistoryIteration(0, 0);

			const ATCPUHistoryEntry *he = nullptr;
			if (mpChannel->ReadHistoryEvents(&he, 0, 1)) {
				mpHistoryView->SetTimestampOrigin(he->mCycle, he->mUnhaltedCycle);
			}
		}
	}

	mViewStartTime = 0;
	mViewEndTime = -1;
	mCenterTime = -1;
}

///////////////////////////////////////////////////////////////////////////

class ATUITraceViewerCPUProfileView final : public VDDialogFrameW32 {
public:
	ATUITraceViewerCPUProfileView(ATUITraceViewerContext& context);
	~ATUITraceViewerCPUProfileView();

	void SetCPUTraceChannel(ATTraceChannelCPUHistory *channel);

	void ClearSelectedRange();
	void SetSelectedRange(double startTime, double endTime);

private:
	bool OnLoaded() override;
	void OnSize() override;

	void OnFontsChanged();
	void RemakeView();

	void OnToolbarClicked(uint32 id);
	void UpdateProfilingModeImage();

	enum : uint32 {
		kToolbarId_Refresh = 1000,
		kToolbarId_Link,
		kToolbarId_Mode,
		kToolbarId_Options,
		kToolbarId_Range,
	};

	ATUITraceViewerContext& mContext;
	vdrefptr<ATTraceChannelCPUHistory> mpChannel;
	double mViewStartTime = 0;
	double mViewEndTime = -1;
	double mSelectionStartTime = 0;
	double mSelectionEndTime = -1;
	uint32 mNotifyRecursionCounter = 0;
	bool mbGlobalAddressesEnabled = false;

	ATProfileMode mProfileMode = kATProfileMode_Insns;

	vdrefptr<IATUIProfileView> mpProfileView;
	ATProfileCounterMode mProfileCounterModes[2] {};
	ATProfileSession mSession;
	vdrefptr<ATProfileMergedFrame> mpMergedFrame;

	VDUIProxyToolbarControl mToolbar;
};

ATUITraceViewerCPUProfileView::ATUITraceViewerCPUProfileView(ATUITraceViewerContext& context)
	: VDDialogFrameW32(IDD_TRACEVIEWER_CPUPROFILE)
	, mContext(context)
{
	mToolbar.SetOnClicked([this](uint32 id) { OnToolbarClicked(id); });
}

ATUITraceViewerCPUProfileView::~ATUITraceViewerCPUProfileView() {
}

void ATUITraceViewerCPUProfileView::SetCPUTraceChannel(ATTraceChannelCPUHistory *channel) {
	if (mpChannel != channel) {
		mpChannel = channel;

		if (mpProfileView) {
		}

		mViewStartTime = 0;
		mViewEndTime = -1;

		RemakeView();
	}
}

void ATUITraceViewerCPUProfileView::ClearSelectedRange() {
	mSelectionStartTime = 0;
	mSelectionEndTime = -1;
}

void ATUITraceViewerCPUProfileView::SetSelectedRange(double startTime, double endTime) {
	mSelectionStartTime = startTime;
	mSelectionEndTime = endTime;
}

bool ATUITraceViewerCPUProfileView::OnLoaded() {
	AddProxy(&mToolbar, IDC_TOOLBAR);

	mToolbar.Clear();
	mToolbar.AddButton(kToolbarId_Refresh, 8, nullptr);
	mToolbar.AddButton(kToolbarId_Link, 9, nullptr);
	mToolbar.AddSeparator();
	mToolbar.AddDropdownButton(kToolbarId_Mode, 2, nullptr);
	mToolbar.AddDropdownButton(kToolbarId_Options, -1, L"Options");
	mToolbar.AddButton(kToolbarId_Range, -1, L"");

	mToolbar.SetItemVisible(kToolbarId_Range, false);

	const sint32 iconSize = GetDpiScaledMetric(SM_CXSMICON);

	VDPixmapBuffer pximg;
	if (ATLoadImageResource(IDB_TOOLBAR_PROFILER2, pximg)) {
		const uint32 n = pximg.w / pximg.h;
		mToolbar.InitImageList(n, iconSize, iconSize);
		mToolbar.AddImages(n, pximg);
	}

	UpdateProfilingModeImage();

	ATUICreateProfileView(~mpProfileView);

	mpProfileView->Create(this, 100);
	mpProfileView->AsUINativeWindow()->Focus();

	return true;
}

void ATUITraceViewerCPUProfileView::OnSize() {
	const vdsize32 sz = GetClientArea().size();

	mToolbar.SetPosition(vdpoint32(0, 0));
	mToolbar.AutoSize();
	const vdrect32 toolbarArea = mToolbar.GetArea();

	mpProfileView->AsUINativeWindow()->SetArea(vdrect32(0, toolbarArea.bottom, sz.w, sz.h));
}

void ATUITraceViewerCPUProfileView::OnFontsChanged() {
}

void ATUITraceViewerCPUProfileView::RemakeView() {
	if (!mpProfileView || !mpChannel)
		return;

	{
		ATCPUProfileBuilder builder;
		builder.Init(mProfileMode, mProfileCounterModes[0], mProfileCounterModes[1]);
		builder.SetGlobalAddressesEnabled(mbGlobalAddressesEnabled);

		const auto tsDecoder = g_sim.GetTimestampDecoder();

		uint32 startEventIdx = 0;
		uint32 endEventIdx = mpChannel->GetEventCount();

		if (mSelectionEndTime > mSelectionStartTime) {
			startEventIdx = mpChannel->FindEvent(mSelectionStartTime);
			endEventIdx = mpChannel->FindEvent(mSelectionEndTime);

			VDStringW s;
			s.sprintf(L"Range: %.2fs - %.2fs \u00D7", mSelectionStartTime, mSelectionEndTime);
			mToolbar.SetItemText(kToolbarId_Range, s.c_str());
			mToolbar.SetItemVisible(kToolbarId_Range, true);
		} else {
			mToolbar.SetItemVisible(kToolbarId_Range, false);
		}

		const ATCPUHistoryEntry *hents[256];
		uint32 pos = startEventIdx;
		uint32 cycle = 0;
		uint32 unhaltedCycle = 0;

		if (mpChannel->ReadHistoryEvents(hents, pos, 1)) {
			cycle = hents[0]->mCycle;
			unhaltedCycle = hents[0]->mUnhaltedCycle;
		}

		builder.OpenFrame(cycle, unhaltedCycle, tsDecoder);

		const bool useGlobalAddrs = mpChannel->GetDisasmMode() == kATDebugDisasmMode_6502;

		while(pos < endEventIdx) {
			uint32 n = mpChannel->ReadHistoryEvents(hents, pos, std::min<uint32>(endEventIdx - pos, (uint32)vdcountof(hents)));
			if (n < 2)
				break;
			
			pos += n - 1;

			builder.Update(tsDecoder, hents, n - 1, useGlobalAddrs);
		}

		if (pos && mpChannel->ReadHistoryEvents(hents, pos - 1, 1)) {
			cycle = hents[0]->mCycle;
			unhaltedCycle = hents[0]->mUnhaltedCycle;
		}

		builder.CloseFrame(cycle, unhaltedCycle, true);
		builder.Finalize();
		builder.TakeSession(mSession);
	}

	ATProfileMergeFrames(mSession, 0, 1, ~mpMergedFrame);
	mpProfileView->SetData(&mSession, mpMergedFrame, mpMergedFrame);
}

void ATUITraceViewerCPUProfileView::OnToolbarClicked(uint32 id) {
	switch(id) {
		case kToolbarId_Refresh:
			RemakeView();
			break;

		case kToolbarId_Link:
			break;

		case kToolbarId_Mode:
			{
				static constexpr ATProfileMode kProfilerModes[]={
					kATProfileMode_Insns,
					kATProfileMode_Functions,
					kATProfileMode_BasicBlock,
					kATProfileMode_CallGraph,
				};

				static constexpr const wchar_t *kProfilerModeLabels[]={
					L"Instructions",
					L"Functions",
					L"Basic Blocks",
					L"Call Graph",
					nullptr
				};

				const sint32 idx = mToolbar.ShowDropDownMenu(kToolbarId_Mode, kProfilerModeLabels);

				if ((uint32)idx < vdcountof(kProfilerModes)) {
					mProfileMode = kProfilerModes[idx];

					UpdateProfilingModeImage();
					RemakeView();
				}
			}
			break;

		case kToolbarId_Options:
			{
				HMENU hmenu = LoadMenu(NULL, MAKEINTRESOURCE(IDR_PROFILE_OPTIONS_MENU));

				if (!hmenu)
					break;

				const auto counterModeMenuIds = ATUIGetProfilerCounterModeMenuIds();
				uint32 activeMask = 0;

				for(const auto cm : mProfileCounterModes) {
					if (cm) {
						activeMask |= (1 << (cm - 1));
						VDCheckMenuItemByCommandW32(hmenu, counterModeMenuIds[cm - 1], true);
					}
				}

				if (mProfileCounterModes[vdcountof(mProfileCounterModes) - 1]) {
					for(uint32 i=0; i<counterModeMenuIds.size(); ++i) {
						if (!(activeMask & (1 << i)))
							VDEnableMenuItemByCommandW32(hmenu, counterModeMenuIds[i], false);
					}
				}

				VDEnableMenuItemByCommandW32(hmenu, ID_FRAMETRIGGER_NONE, false);
				VDEnableMenuItemByCommandW32(hmenu, ID_FRAMETRIGGER_VBLANK, false);
				VDEnableMenuItemByCommandW32(hmenu, ID_FRAMETRIGGER_PCADDRESS, false);
				VDCheckMenuItemByCommandW32(hmenu, ID_MENU_ENABLEGLOBALADDRESSES, mbGlobalAddressesEnabled);

				const uint32 selectedId = mToolbar.ShowDropDownMenu(kToolbarId_Options, GetSubMenu(hmenu, 0));

				DestroyMenu(hmenu);

				for(uint32 i = 0, n = (uint32)counterModeMenuIds.size(); i < n; ++i) {
					if (selectedId == counterModeMenuIds[i]) {
						const ATProfileCounterMode selectedMode = (ATProfileCounterMode)(i + 1);

						// check if we already have this mode
						auto it = std::find(std::begin(mProfileCounterModes), std::end(mProfileCounterModes), selectedMode);
						if (it == std::end(mProfileCounterModes)) {
							// we don't -- add it
							it = std::find(std::begin(mProfileCounterModes), std::end(mProfileCounterModes), kATProfileCounterMode_None);
							if (it != std::end(mProfileCounterModes))
								*it = selectedMode;
						} else {
							// we do -- remove it
							*it = kATProfileCounterMode_None;
							std::rotate(it, it+1, std::end(mProfileCounterModes));
						}
						break;
					}
				}

				if (selectedId == ID_MENU_ENABLEGLOBALADDRESSES) {
					mbGlobalAddressesEnabled = !mbGlobalAddressesEnabled;
					RemakeView();
				}
			}
			break;

		case kToolbarId_Range:
			ClearSelectedRange();
			RemakeView();
			break;
	}
}

void ATUITraceViewerCPUProfileView::UpdateProfilingModeImage() {
	switch(mProfileMode) {
		case kATProfileMode_Insns:
		default:
			mToolbar.SetItemImage(kToolbarId_Mode, 2);
			break;

		case kATProfileMode_Functions:
			mToolbar.SetItemImage(kToolbarId_Mode, 3);
			break;

		case kATProfileMode_CallGraph:
			mToolbar.SetItemImage(kToolbarId_Mode, 5);
			break;

		case kATProfileMode_BasicBlock:
			mToolbar.SetItemImage(kToolbarId_Mode, 6);
			break;

		case kATProfileMode_BasicLines:
			mToolbar.SetItemImage(kToolbarId_Mode, 4);
			break;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUITraceViewerChannelView final : public VDDialogFrameW32 {
public:
	ATUITraceViewerChannelView(ATUITraceViewerContext& context);

	void OnChannelsChanged();
	void OnSelectionModeChanged();

private:
	bool OnPaint() override;
	void OnMouseDownL(int x, int y);
	void OnDpiChanged() override;
	sint32 GetBackgroundColor() const override { return kColor_ChannelBackground; }

	ATUITraceViewerContext& mContext;
	sint32 mScrollY = 0;
};

ATUITraceViewerChannelView::ATUITraceViewerChannelView(ATUITraceViewerContext& context)
	: VDDialogFrameW32(IDD_TRACEVIEWER_CHANNELS)
	, mContext(context)
{
}

void ATUITraceViewerChannelView::OnChannelsChanged() {
	InvalidateRect(mhdlg, NULL, TRUE);
}

bool ATUITraceViewerChannelView::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhdlg, &ps);
	int savedDC = SaveDC(hdc);

	SelectObject(hdc, GetStockObject(DC_PEN));
	SelectObject(hdc, GetStockObject(DC_BRUSH));
	SelectObject(hdc, mContext.mChannelFont.get());

	const vdsize32& sz = GetClientArea().size();

	for(const auto *group : mContext.mGroups) {
		RECT rch = { 0, group->mPosY - mScrollY, sz.w, group->mPosY + group->mHeight - mScrollY };

		SetBkMode(hdc, OPAQUE);
		SetDCBrushColor(hdc, GetSysColor(COLOR_3DFACE));
		DrawEdge(hdc, &rch, BDR_RAISED, BF_RECT | BF_FLAT);

		SetBkMode(hdc, TRANSPARENT);
		SetTextColor(hdc, RGB(0, 0, 0));

		SetTextAlign(hdc, TA_LEFT | TA_TOP);
		ExtTextOutW(hdc, rch.left + 10, rch.top + 10, 0, nullptr, group->mName.c_str(), group->mName.size(), nullptr);

		for(const auto *ch : group->mChannels) {
			const wchar_t *name = ch->mpChannel->GetName();

			RECT r;
			r.left = rch.left + 10;
			r.right = rch.right - 10;
			r.top = rch.top + ch->mPosY;
			r.bottom = r.top + ch->mHeight;

			DrawText(hdc, name, wcslen(name), &r, DT_RIGHT | DT_SINGLELINE | DT_VCENTER | DT_NOPREFIX); 
		}
	}

	RestoreDC(hdc, savedDC);
	EndPaint(mhdlg, &ps);
	return true;
}

void ATUITraceViewerChannelView::OnMouseDownL(int x, int y) {
	y += mScrollY;

	for(const auto *group : mContext.mGroups) {
		const uint32 yoffset = (uint32)(y - group->mPosY);

		if (yoffset < (uint32)group->mHeight) {
			for(const auto *ch : group->mChannels) {
				if ((uint32)(yoffset - ch->mPosY) < (uint32)ch->mHeight) {
					IATTraceChannel *tch = ch->mpChannel;

					if (auto *tchVideo = vdpoly_cast<IATTraceChannelVideo *>(tch)) {
						VDStringW msg;
						const uint64 traceSize = tchVideo->GetTraceSize();
						const double duration = tchVideo->AsTraceChannel()->GetDuration();
						
						msg.sprintf(L"Video trace: %.1f MB (%.2f MB/sec)"
							, (double)traceSize / 1048576.0
							, duration > 0 ? (double)traceSize / 1048576.0 / duration : 0
						);
						ShowInfo(msg.c_str());
						return;
					}
					break;
				}
			}

			if (group->mName == L"CPU") {
				if (auto *tchCPU = vdpoly_cast<ATTraceChannelCPUHistory *>(mContext.mpCPUHistoryChannel)) {
					VDStringW msg;
					const uint64 traceSize = tchCPU->GetTraceSize();
					const double duration = tchCPU->GetDuration();
					const uint32 eventCount = tchCPU->GetEventCount();
						
					msg.sprintf(L"CPU trace: %.1f MB (%.2f MB/sec), %u events (%.1f bytes/event)"
						, (double)traceSize / 1048576.0
						, duration > 0 ? (double)traceSize / 1048576.0 / duration : 0
						, eventCount
						, eventCount > 0 ? (double)traceSize / (double)eventCount : 0
					);
					ShowInfo(msg.c_str());
				}
			}
			break;
		}
	}
}

void ATUITraceViewerChannelView::OnDpiChanged() {
	InvalidateRect(mhdlg, nullptr, TRUE);
}

///////////////////////////////////////////////////////////////////////////

class ATUITraceViewerEventView final : public VDDialogFrameW32 {
public:
	ATUITraceViewerEventView(ATUITraceViewerContext& context);

	void SetFrameChannel(IATTraceChannel *channel);

	void SetSelectionMode(bool selMode);
	void ClearSelection();
	void SetSelection(double startTime, double endTime);

	void RescaleView(double newStartTime, double secondsPerPixel);
	void ScrollToTime(double newStartTime);
	void ScrollDeltaPixels(double newStartTime, sint32 dx, sint32 dy);

private:
	bool OnLoaded() override;
	void OnDestroy() override;
	void OnSize() override;
	void OnMouseMove(int x, int y) override;
	void OnMouseDownL(int x, int y) override;
	void OnMouseUpL(int x, int y) override;
	void OnMouseWheel(int x, int y, sint32 delta) override;
	bool OnSetCursor(ATUICursorImage& image);
	void OnCaptureLost() override;
	bool OnErase(VDZHDC hdc) override;
	bool OnPaint() override;
	void OnDpiChanged() override;

private:
	struct SelectedPixelRange {
		sint32 x1;
		sint32 x2;
	};

	void InvalidateSelectedRange(double t1, double t2);
	void InvalidateSelectedPixelRange(const SelectedPixelRange& range);
	SelectedPixelRange SelectedTimeRangeToPixelRange(double t1, double t2) const;
	void RecomputeEndTime();
	double PixelToTime(sint32 x) const;
	sint32 TimeToPixel(double t) const;

	ATUITraceViewerContext& mContext;
	sint32 mWidth = 0;
	sint32 mHeight = 0;
	double mStartTime = 0;
	double mEndTime = 0;
	double mSecondsPerPixel = 1.0f / 10.0f;

	sint32 mWheelAccum = 0;
	sint32 mDragAnchorX = 0;
	sint32 mDragAnchorY = 0;
	bool mbDragging = false;
	bool mbSelectionMode = false;
	bool mbSelectionValid = false;

	double mSelectStart = 0;
	double mSelectEnd = 0;
	uint32 mNotifyRecursionCounter = 0;

	vdrefptr<IATTraceChannel> mpFrameChannel;

	VDPixmapBuffer mImageBufferSrc;
	VDPixmapBuffer mImageBufferDst;
	vdsize32 mImageResamplerSrcSize;
	vdsize32 mImageResamplerDstSize;
	vdautoptr<IVDPixmapResampler> mpImageResampler;
	vdfastvector<uint32> mTapeBlitBuffer;

	HDC mhdcSel = nullptr;
	HBITMAP mhbmSel = nullptr;
	HBITMAP mhbmSelOld = nullptr;
};

ATUITraceViewerEventView::ATUITraceViewerEventView(ATUITraceViewerContext& context)
	: VDDialogFrameW32(IDD_TRACEVIEWER_EVENTS)
	, mContext(context)
{
}

void ATUITraceViewerEventView::SetFrameChannel(IATTraceChannel *channel) {
	mpFrameChannel = channel;
}

void ATUITraceViewerEventView::SetSelectionMode(bool selMode) {
	mbSelectionMode = selMode;
}

void ATUITraceViewerEventView::ClearSelection() {
	if (mbSelectionValid) {
		mbSelectionValid = false;

		InvalidateSelectedRange(mSelectStart, mSelectEnd);

		++mNotifyRecursionCounter;
		mContext.mpParent->SetSelection(0, -1);
		--mNotifyRecursionCounter;
	}
}

void ATUITraceViewerEventView::SetSelection(double startTime, double endTime) {
	if (mNotifyRecursionCounter)
		return;

	if (mbSelectionValid && mSelectStart == startTime && mSelectEnd == endTime)
		return;

	if (!mbSelectionValid) {
		InvalidateSelectedRange(startTime, endTime);
	} else {
		const SelectedPixelRange oldPixelRange = SelectedTimeRangeToPixelRange(mSelectStart, mSelectEnd);
		const SelectedPixelRange newPixelRange = SelectedTimeRangeToPixelRange(startTime, endTime);

		// check if the old and new range overlap
		if (oldPixelRange.x1 < newPixelRange.x2
			&& newPixelRange.x1 < oldPixelRange.x2
			&& oldPixelRange.x1 < oldPixelRange.x2
			&& newPixelRange.x1 < newPixelRange.x2)
		{
			// they do -- invalidate min to min and max to max
			InvalidateSelectedPixelRange({ std::min(oldPixelRange.x1, newPixelRange.x1), std::max(oldPixelRange.x1, newPixelRange.x1) });
			InvalidateSelectedPixelRange({ std::min(oldPixelRange.x2, newPixelRange.x2), std::max(oldPixelRange.x2, newPixelRange.x2) });
		} else {
			// they don't -- invalidate the old and new times
			InvalidateSelectedPixelRange(oldPixelRange);
			InvalidateSelectedPixelRange(newPixelRange);
		}
	}

	mSelectStart = startTime;
	mSelectEnd = endTime;
	mbSelectionValid = true;

	++mNotifyRecursionCounter;
	mContext.mpParent->SetSelection(startTime, endTime);
	--mNotifyRecursionCounter;
}

void ATUITraceViewerEventView::RescaleView(double newStartTime, double secondsPerPixel) {
	mStartTime = newStartTime;
	mSecondsPerPixel = secondsPerPixel;
	RecomputeEndTime();

	InvalidateRect(mhdlg, nullptr, TRUE);
}

void ATUITraceViewerEventView::ScrollToTime(double newStartTime) {
	mStartTime = newStartTime;
	RecomputeEndTime();

	InvalidateRect(mhdlg, nullptr, TRUE);
}

void ATUITraceViewerEventView::ScrollDeltaPixels(double newStartTime, sint32 dx, sint32 dy) {
	mStartTime = newStartTime;
	RecomputeEndTime();
	ScrollWindow(mhdlg, dx, dy, nullptr, nullptr);
}

bool ATUITraceViewerEventView::OnLoaded() {
	if (HDC hdc = GetDC(mhdlg)) {
		mhdcSel = CreateCompatibleDC(hdc);

		if (mhdcSel) {
			BITMAPINFO bi = {};
			bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
			bi.bmiHeader.biWidth = 1;
			bi.bmiHeader.biHeight = 1;
			bi.bmiHeader.biBitCount = 32;
			bi.bmiHeader.biPlanes = 1;
			bi.bmiHeader.biSizeImage = 4;
			bi.bmiHeader.biCompression = BI_RGB;

			void *bits;
			mhbmSel = CreateDIBSection(mhdcSel, &bi, DIB_RGB_COLORS, &bits, nullptr, 0);
			if (mhbmSel) {
				mhbmSelOld = (HBITMAP)SelectObject(mhdcSel, mhbmSel);

				*(uint32 *)bits = 0x84BDFF;
			}
		}

		ReleaseDC(mhdlg, hdc);
	}

	VDDialogFrameW32::OnLoaded();

	OnSize();

	return false;
}

void ATUITraceViewerEventView::OnDestroy() {
	if (mhdcSel) {
		if (mhbmSelOld) {
			SelectObject(mhdcSel, mhbmSelOld);
			mhbmSelOld = nullptr;
		}

		DeleteDC(mhdcSel);
	}

	if (mhbmSel) {
		DeleteObject(mhbmSel);
		mhbmSel = nullptr;
	}

	VDDialogFrameW32::OnDestroy();
}

void ATUITraceViewerEventView::OnSize() {
	VDDialogFrameW32::OnSize();

	const auto& sz = GetClientArea();
	mWidth = sz.width();
	mHeight = sz.height();

	RecomputeEndTime();
}

void ATUITraceViewerEventView::OnMouseMove(int x, int y) {
	const sint32 dx = x - mDragAnchorX;
	const sint32 dy = y - mDragAnchorY;

	if (!mbDragging && (GetKeyState(VK_LBUTTON) < 0)) {
		int dragRadius = GetDpiScaledMetric(SM_CXDRAG);

		if (abs(dx) > dragRadius || abs(dy) > dragRadius) {
			mbDragging = true;
			SetCapture();
		}
	}

	if (mbDragging) {
		mDragAnchorX = x;
		mDragAnchorY = y;

		if (mbSelectionMode)
			SetSelection(mSelectStart, PixelToTime(x));
		else if (dx|dy)
			mContext.mpParent->ScrollDeltaPixels(dx, 0);
	}
}

void ATUITraceViewerEventView::OnMouseDownL(int x, int y) {
	mDragAnchorX = x;
	mDragAnchorY = y;

	if (mbSelectionMode) {
		double t = PixelToTime(x);
		SetSelection(t, t);
	}
}

void ATUITraceViewerEventView::OnMouseUpL(int x, int y) {
	if (mbDragging) {
		mbDragging = false;

		ReleaseCapture();

		if (mbSelectionMode)
			SetSelection(mSelectStart, PixelToTime(x));
	} else {
		mContext.mpParent->SetFocusTime(PixelToTime(x));
	}
}

void ATUITraceViewerEventView::OnMouseWheel(int x, int y, sint32 delta) {
	mWheelAccum += delta;

	const int steps = mWheelAccum / 120;
	if (steps) {
		mWheelAccum -= steps * 120;

		mContext.mpParent->ZoomDeltaSteps(mStartTime + mSecondsPerPixel * (double)x, steps);
	}
}

bool ATUITraceViewerEventView::OnSetCursor(ATUICursorImage& image) {
	image = mContext.mbSelectionMode ? kATUICursorImage_IBeam : kATUICursorImage_Arrow;
	return true;
}

void ATUITraceViewerEventView::OnCaptureLost() {
	mbDragging = false;
}

bool ATUITraceViewerEventView::OnErase(VDZHDC hdc) {
	SetDCBrushColor(hdc, RGB(240, 240, 240));

	RECT rClient;
	if (GetClientRect(mhdlg, &rClient))
		FillRect(hdc, &rClient, (HBRUSH)GetStockObject(DC_BRUSH));

	return true;
}

bool ATUITraceViewerEventView::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhdlg, &ps);
	int savedDC = SaveDC(hdc);
	const vdsize32& sz = GetClientArea().size();

	SelectObject(hdc, GetStockObject(DC_PEN));
	SelectObject(hdc, GetStockObject(DC_BRUSH));
	SelectObject(hdc, mContext.mEventFont.get());
	
	const sint32 clipX1 = ps.rcPaint.left;
	const sint32 clipX2 = ps.rcPaint.right;

	const double startTime = mStartTime;
	const double endTime = mEndTime;

	double eventStartClip = startTime - mSecondsPerPixel * 10;
	double eventEndClip = endTime + mSecondsPerPixel * 10;
	double labelStartClip = startTime - mSecondsPerPixel * (double)sz.w;
	double labelEndClip = endTime + mSecondsPerPixel * (double)sz.w;

	ATTraceEvent ev;

	// Draw frame boundaries and VBLANK regions.
	//
	// We cheat a little here to draw the blanking regions since we know that we're dealing
	// with only either NTSC or PAL/SECAM timings.
	if (mpFrameChannel) {
		mpFrameChannel->StartIteration(eventStartClip, eventEndClip, mSecondsPerPixel * 4);

		SetDCPenColor(hdc, RGB(192, 192, 192));
		SetDCBrushColor(hdc, RGB(232, 232, 232));

		while(mpFrameChannel->GetNextEvent(ev)) {
			if (ev.mpName) {
				const double duration = ev.mEventStop - ev.mEventStart;
				const bool isPal = ev.mEventStop - ev.mEventStart > 1.0f / 55.0f;
				const double vblEndFrac = isPal ? 8.0f / 312.0f : 8.0f / 262.0f;
				const double vblStartFrac = isPal ? 248.0f / 312.0f : 248.0f / 262.0f;

				const double x0f = (ev.mEventStart - mStartTime) / mSecondsPerPixel;
				const sint32 x0 = (sint32)x0f;
				const sint32 xve = (sint32)(x0f + (vblEndFrac * duration) / mSecondsPerPixel);
				const sint32 xvs = (sint32)(x0f + (vblStartFrac * duration) / mSecondsPerPixel);
				const sint32 x1 = (sint32)((ev.mEventStop - mStartTime) / mSecondsPerPixel);

				MoveToEx(hdc, x0, 0, nullptr);
				LineTo(hdc, x0, sz.h);

				if (xve > x0 + 1) {
					RECT r = { x0 + 1, 0, xve, sz.h };
					FillRect(hdc, &r, (HBRUSH)GetStockObject(DC_BRUSH));
				}

				if (xvs < x1) {
					RECT r = { xvs, 0, x1, sz.h };
					FillRect(hdc, &r, (HBRUSH)GetStockObject(DC_BRUSH));
				}
			}
		}
	}

	if (!mContext.mGroups.empty()) {
		sint32 lastY = 0;

		for(const auto *group : mContext.mGroups) {
			const sint32 groupY = group->mPosY;

			for(const auto *ch : group->mChannels) {
				const sint32 y = ch->mPosY + groupY;

				SetDCPenColor(hdc, RGB(160, 160, 160));
				MoveToEx(hdc, 0, groupY + ch->mPosY, nullptr);
				LineTo(hdc, sz.w, groupY + ch->mPosY);

				if (ch->mType == ATUITraceViewerChannel::kType_Video) {
					IATTraceChannelVideo *vch = vdpoly_cast<IATTraceChannelVideo *>(ch->mpChannel.get());
					const sint32 imgh = ch->mHeight - (3 * mContext.mDpi + 48) / 96;
					const sint32 imgw = (imgh * 4 + 1) / 3;
					const sint32 imgy = groupY + ch->mPosY + 2;
					double secondsPerFrame = mSecondsPerPixel * (double)imgw;

					// We are binning the timeline into equally spaced bins <secondsPerFrame> apart and binning
					// frame times by that, choosing the nearest frame to represent each bin. However, the frames
					// themselves take half a frame time on either side of that, so we expand the bracket by half
					// a frame on either side.
					double frameStartIndex = floor(eventStartClip / secondsPerFrame - 0.5);
					double frameStartTime = frameStartIndex * secondsPerFrame;
					double frameEndIndex = ceil(eventEndClip / secondsPerFrame + 0.5);
					sint32 frameCount = (sint32)(frameEndIndex - frameStartIndex + 0.5);
					double lastFrameTime = -DBL_MAX;

					for(sint32 i = 0; i < frameCount; ++i) {
						double frameStartBracketTime = (frameStartIndex + (double)i) * secondsPerFrame;
						double frameTime;
						const VDPixmap *px = vch->GetNearestFrame(frameStartBracketTime, frameStartBracketTime + secondsPerFrame, frameTime);

						// skip if no frame lies within this bracket
						if (!px)
							continue;

						// check if we just drew this frame
						if (frameTime == lastFrameTime)
							continue;

						lastFrameTime = frameTime;

						// compute the blit position for this frame
						const sint32 imgx = (sint32)((frameTime - mStartTime - (double)imgw * mSecondsPerPixel * 0.5) / mSecondsPerPixel);

						// skip if outside of clip range
						if (imgx <= clipX1 - imgw || imgx >= clipX2)
							continue;

						mImageBufferSrc.init(px->w, px->h, nsVDPixmap::kPixFormat_XRGB8888);
						VDPixmapBlt(mImageBufferSrc, *px);

						VDPixmapLayout layout;
						VDMakeBitmapCompatiblePixmapLayout(layout, imgw, imgh, nsVDPixmap::kPixFormat_XRGB8888, 0);
						mImageBufferDst.init(layout);

						vdsize32 srcSize { px->w, px->h };
						vdsize32 dstSize { imgw, imgh };

						if (!mpImageResampler) {
							mpImageResampler = VDCreatePixmapResampler();
							mpImageResampler->SetFilters(IVDPixmapResampler::kFilterLinear, IVDPixmapResampler::kFilterLinear, false);
						}

						if (mImageResamplerSrcSize != srcSize || mImageResamplerDstSize != dstSize) {
							mpImageResampler->Init(dstSize.w, dstSize.h, nsVDPixmap::kPixFormat_XRGB8888, srcSize.w, srcSize.h, nsVDPixmap::kPixFormat_XRGB8888);
						}

						mpImageResampler->Process(mImageBufferDst, mImageBufferSrc);
						//VDPixmapResample(mImageBufferDst, mImageBufferSrc, IVDPixmapResampler::kFilterLinear);

						BITMAPINFO bi = {
							{
								sizeof(BITMAPINFOHEADER),
								(LONG)imgw,
								(LONG)imgh,
								1,
								32,
								0,
								(DWORD)(imgw*imgh*4)
							}
						};

						SetDIBitsToDevice(hdc, imgx, imgy, imgw, imgh, 0, 0, 0, imgh, mImageBufferDst.base(), &bi, DIB_RGB_COLORS);
					}
				} else {
					const sint32 eventY1 = y + (2 * mContext.mDpi + 48) / 96;
					const sint32 eventY2 = y + ch->mHeight - (2 * mContext.mDpi + 48) / 96;
					const sint32 eventTextY1 = y + (4 * mContext.mDpi + 48) / 96;
					const sint32 eventTextY2 = y + ch->mHeight - (4 * mContext.mDpi + 48) / 96;

					ch->mpChannel->StartIteration(mStartTime, endTime, mSecondsPerPixel * 4);

					if (ch->mType == ATUITraceViewerChannel::kType_Tape) {
						IATCassetteImage *image = g_sim.GetCassette().GetImage();

						if (image) {
							SetDCBrushColor(hdc, RGB(0x20, 0x20, 0x20));

							ATTraceChannelTape *tapeChannel = static_cast<ATTraceChannelTape *>(&*ch->mpChannel);
							const bool turbo = tapeChannel->IsTurbo();
							const double samplesPerSec32 = tapeChannel->GetSamplesPerSec() * 4294967296.0;

							while(ch->mpChannel->GetNextEvent(ev)) {
								double evStart = std::max<double>(ev.mEventStart, eventStartClip);
								double evEnd = std::min<double>(ev.mEventStop, eventEndClip);
								sint32 x1 = (sint32)((evStart - mStartTime) / mSecondsPerPixel);
								sint32 x2 = (sint32)((evEnd - mStartTime) / mSecondsPerPixel) + 1;

								if (x1 < clipX1)
									x1 = clipX1;

								if (x2 > clipX2)
									x2 = clipX2;

								const auto& tapeEvent = tapeChannel->GetLastEvent();
								uint64 pos32 = (uint64)(0.5 + (mStartTime + mSecondsPerPixel * (double)(x1 + 0.5f) - ev.mEventStart) * samplesPerSec32) + ((uint64)tapeEvent.mPosition << 32);
								uint64 posinc32 = (uint64)(0.5 + mSecondsPerPixel * samplesPerSec32);

								const sint32 channelh = eventY2 - eventY1;
								const sint32 blity = channelh / 6;
								const sint32 blith = channelh - blity*2;
								const uint32 prevPos = (uint32)((pos32 - posinc32) >> 32);
								bool lastBit = pos32 < posinc32 || (turbo ? image->GetTurboBit(prevPos) : image->GetBit(prevPos, 1, 1, false, false));

								while(x1 < x2) {
									const sint32 blitw = std::min<sint32>(x2 - x1, 4096);

									RECT channelRect = { x1, eventY1, x2, eventY2 };
									FillRect(hdc, &channelRect, (HBRUSH)GetStockObject(DC_BRUSH));

									mTapeBlitBuffer.clear();
									mTapeBlitBuffer.resize(blitw * blith, 0xFF202020);

									uint32 *pLo = mTapeBlitBuffer.data();
									uint32 *pHi = pLo + blitw * (blith - 1);
									const uint32 color = 0xFF70A0FF;

									for(sint32 x=0; x<blitw; ++x) {
										const uint32 pos = (uint32)(pos32 >> 32);
										bool bit = turbo ? image->GetTurboBit(pos) : image->GetBit(pos, 1, 1, false, false);
										pos32 += posinc32;

										if (bit != lastBit) {
											uint32 *p = pLo + x;

											for(sint32 y=0; y<blith; ++y) {
												*p = color;
												p += blitw;
											}
										} if (bit)
											pHi[x] = color;
										else
											pLo[x] = color;

										lastBit = bit;
									}

									BITMAPINFO bi = {
										{
											sizeof(BITMAPINFOHEADER),
											(LONG)blitw,
											(LONG)blith,
											1,
											32,
											0,
											(DWORD)(blith*blith*4)
										}
									};

									SetDIBitsToDevice(hdc, x1, eventY1 + blity, blitw, blith, 0, 0, 0, blith, mTapeBlitBuffer.data(), &bi, DIB_RGB_COLORS);

									x1 += blitw;
								}
							}
						}
					} else {
						SetDCPenColor(hdc, RGB(0, 0, 0));

						SetBkMode(hdc, OPAQUE);
						SetDCBrushColor(hdc, RGB(160, 255, 192));
						SetTextAlign(hdc, TA_TOP | TA_CENTER);

						while(ch->mpChannel->GetNextEvent(ev)) {
							double evStart = std::max<double>(ev.mEventStart, eventStartClip);
							double evEnd = std::min<double>(ev.mEventStop, eventEndClip);
							sint32 x1 = (sint32)((evStart - mStartTime) / mSecondsPerPixel);
							sint32 x2 = (sint32)((evEnd - mStartTime) / mSecondsPerPixel) + 1;

							if (ev.mpName) {
								const uint32 bgColor = VDSwizzleU32(ev.mBgColor) >> 8;
								SetBkColor(hdc, bgColor);
								SetDCBrushColor(hdc, bgColor);
								Rectangle(hdc, x1, eventY1, x2, eventY2);

								double labelTime = (ev.mEventStart + ev.mEventStop) * 0.5;

								if (labelTime > labelStartClip && labelTime < labelEndClip) {
									RECT rtx = { x1 + 1, eventTextY1, x2 - 1, eventTextY2};

									SetTextColor(hdc, VDSwizzleU32(ev.mFgColor) >> 8);
									ExtTextOutW(hdc, (sint32)floor((labelTime - mStartTime) / mSecondsPerPixel), eventTextY1, ETO_CLIPPED, &rtx, ev.mpName, wcslen(ev.mpName), nullptr);
								}
							} else {
								const uint32 mutedBgColor = 0x606060 + ((ev.mBgColor & 0xFEFEFE) >> 1);
								SetBkColor(hdc, VDSwizzleU32(mutedBgColor) >> 8);
								RECT rev = { x1, eventY1, x2, eventY2 };
								ExtTextOutW(hdc, 0, y, ETO_OPAQUE, &rev, L"", 0, nullptr);
							}
						}
					}
				}

				lastY = ch->mPosY + ch->mHeight;
			}
		}

		MoveToEx(hdc, 0, lastY, nullptr);
		LineTo(hdc, sz.w, lastY);
	}

	if (mbSelectionValid) {
		const SelectedPixelRange selRange = SelectedTimeRangeToPixelRange(mSelectStart, mSelectEnd);

		if (selRange.x1 < selRange.x2)
			AlphaBlend(hdc, selRange.x1, 0, selRange.x2 - selRange.x1, sz.h, mhdcSel, 0, 0, 1, 1, BLENDFUNCTION { AC_SRC_OVER, 0, 128, 0 });
	}

	RestoreDC(hdc, savedDC);
	EndPaint(mhdlg, &ps);
	return true;
}

void ATUITraceViewerEventView::OnDpiChanged() {
	InvalidateRect(mhdlg, nullptr, TRUE);
}

void ATUITraceViewerEventView::InvalidateSelectedRange(double t1, double t2) {
	InvalidateSelectedPixelRange(SelectedTimeRangeToPixelRange(t1, t2));
}

void ATUITraceViewerEventView::InvalidateSelectedPixelRange(const SelectedPixelRange& range) {
	if (range.x1 != range.x2) {
		RECT r = { range.x1, 0, range.x2, mHeight };
		InvalidateRect(mhdlg, &r, TRUE);
	}
}

ATUITraceViewerEventView::SelectedPixelRange ATUITraceViewerEventView::SelectedTimeRangeToPixelRange(double t1, double t2) const {
	sint32 x1 = TimeToPixel(std::min(t1, t2));
	sint32 x2 = TimeToPixel(std::max(t1, t2));

	if (x1 == x2)
		++x2;

	return SelectedPixelRange { x1, x2 };
}

void ATUITraceViewerEventView::RecomputeEndTime() {
	mEndTime = mStartTime + mSecondsPerPixel * mWidth;
}

double ATUITraceViewerEventView::PixelToTime(sint32 x) const {
	return ((double)x + 0.5) * mSecondsPerPixel + mStartTime;
}

sint32 ATUITraceViewerEventView::TimeToPixel(double t) const {
	t = std::max<double>(t - mStartTime, -5 * mSecondsPerPixel);
	t = std::min<double>(t, mEndTime - mStartTime + mSecondsPerPixel * 5);
	return (sint32)floor(t / mSecondsPerPixel);
}

///////////////////////////////////////////////////////////////////////////

class ATUITraceViewerTimescaleView final : public VDDialogFrameW32 {
public:
	ATUITraceViewerTimescaleView(ATUITraceViewerContext& context);

	sint32 GetIdealHeight() const;

	void ScrollToTime(double newStartTime);
	void ScrollDeltaPixels(double newStartTime, sint32 dx);
	void RescaleView(double newStartTime, double secondsPerPixel, double secondsPerDivision);

private:
	bool OnPaint() override;
	void OnDpiChanged() override;
	sint32 GetBackgroundColor() const override { return kColor_TimescaleBackground; }

private:
	ATUITraceViewerContext& mContext;

	double mStartTime = 0;
	double mSecondsPerPixel = 1.0f / 10.0f;
	double mSecondsPerDivision = 1.0f;
	int mDivisionDecimals = 0;
};

ATUITraceViewerTimescaleView::ATUITraceViewerTimescaleView(ATUITraceViewerContext& context)
	: VDDialogFrameW32(IDD_TRACEVIEWER_TIMESCALE)
	, mContext(context)
{
}

sint32 ATUITraceViewerTimescaleView::GetIdealHeight() const {
	return mContext.mTimestampFontMetrics.tmHeight + (14 * mContext.mDpi + 48) / 96;
}

void ATUITraceViewerTimescaleView::ScrollToTime(double newStartTime) {
	mStartTime = newStartTime;
	InvalidateRect(mhdlg, nullptr, TRUE);
}

void ATUITraceViewerTimescaleView::ScrollDeltaPixels(double newStartTime, sint32 dx) {
	mStartTime = newStartTime;
	ScrollWindow(mhdlg, dx, 0, nullptr, nullptr);
}

void ATUITraceViewerTimescaleView::RescaleView(double newStartTime, double secondsPerPixel, double secondsPerDivision) {
	mStartTime = newStartTime;
	mSecondsPerPixel = secondsPerPixel;
	mSecondsPerDivision = secondsPerDivision;

	mDivisionDecimals = 0;
	if (mSecondsPerDivision < 0.000002f)
		mDivisionDecimals = 6;
	else if (mSecondsPerDivision < 0.00002f)
		mDivisionDecimals = 5;
	else if (mSecondsPerDivision < 0.0002f)
		mDivisionDecimals = 4;
	else if (mSecondsPerDivision < 0.002f)
		mDivisionDecimals = 3;
	else if (mSecondsPerDivision < 0.02f)
		mDivisionDecimals = 2;
	else if (mSecondsPerDivision < 0.2f)
		mDivisionDecimals = 1;

	InvalidateRect(mhdlg, nullptr, TRUE);
}

bool ATUITraceViewerTimescaleView::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhdlg, &ps);
	int savedDC = SaveDC(hdc);

	const vdsize32& sz = GetClientArea().size();

	SelectObject(hdc, mContext.mTimestampFont.get());
	SelectObject(hdc, GetStockObject(DC_PEN));
	SetDCPenColor(hdc, RGB(0, 0, 0));
	MoveToEx(hdc, 0, sz.h - 1, nullptr);
	LineTo(hdc, sz.w, sz.h - 1);

	SetTextAlign(hdc, TA_TOP | TA_CENTER);
	SetBkColor(hdc, VDSwizzleU32(GetBackgroundColor()) >> 8);

	double div1f = mStartTime / mSecondsPerDivision;
	double div2f = (mStartTime + (double)sz.w * mSecondsPerPixel) / mSecondsPerDivision;
	sint64 div1 = (sint64)ceil(div1f);
	sint64 div2 = (sint64)ceil(div2f);

	VDStringW s;

	int majorDivHeight = (12 * mContext.mDpi + 48) / 96;
	int minorDivHeight = (6 * mContext.mDpi + 48) / 96;

	for(sint64 div = div1 - 1; div < div2 + 1; ++div) {
		sint32 x = (sint32)floor(((double)div * mSecondsPerDivision - mStartTime) / mSecondsPerPixel);
		const bool major = (div % 5) == 0;
		
		MoveToEx(hdc, x, major ? sz.h - majorDivHeight : sz.h - minorDivHeight, nullptr);
		LineTo(hdc, x, sz.h - 1);

		if (major) {
			s.sprintf(L"%.*f", mDivisionDecimals, (double)div * mSecondsPerDivision);
			ExtTextOutW(hdc, x, 0, ETO_OPAQUE, nullptr, s.c_str(), (UINT)s.size(), nullptr);
		}
	}

	RestoreDC(hdc, savedDC);
	EndPaint(mhdlg, &ps);
	return true;
}

void ATUITraceViewerTimescaleView::OnDpiChanged() {
	InvalidateRect(mhdlg, nullptr, TRUE);
}

///////////////////////////////////////////////////////////////////////////

class ATUITraceViewer final : public VDDialogFrameW32, public IATUITraceViewer {
public:
	ATUITraceViewer(ATTraceCollection *collection);

	double GetViewEndTime() const;
	double GetViewCenterTime() const;

	void StartTracing();
	void StopTracing();
	void ZoomIn();
	void ZoomOut();

	void SetCollection(ATTraceCollection *collection);

private:
	void ScrollDeltaPixels(sint32 dx, sint32 dy) override;
	void ScrollToCenterTime(double centerTime);
	void ZoomDeltaSteps(double centerTime, sint32 steps) override;
	void SetFocusTime(double t) override;
	void SetSelection(double startTime, double endTime) override;
	void SetSelectionMode(bool selMode) override;

private:
	bool OnLoaded() override;
	void OnDestroy() override;
	void OnSize() override;
	void OnHScroll(uint32 id, int code) override;
	void OnDpiChanged() override;
	sint32 GetBackgroundColor() const override { return kColor_PanelBackground; }
	bool PreNCDestroy() override { return true; }

	void RebuildViews();
	void RebuildToolbar();
	void UpdateToolbarSelectionState();
	void UpdateFonts();
	void ClearGroupViews();
	void UpdateChannels();
	void UpdateHScroll(bool updateRange);

	void OnToolbarClicked(uint32 id);

	void OpenHistory();
	void OpenProfile();

	enum : uint32 {
		kToolbarId_Stop = 1000,
		kToolbarId_Start,
		kToolbarId_ZoomIn,
		kToolbarId_ZoomOut,
		kToolbarId_Settings,
		kToolbarId_Select,
		kToolbarId_Move,
		kToolbarId_Profile,
		kToolbarId_History,
	};

	uint64 mCyclesPerPixel = 1000;
	sint32 mChannelSplitX = 200;
	sint32 mSplitterWidth = 5;
	sint32 mEventViewWidth = 0;

	double mStartTime = 0;
	double mSecondsPerPixel = 1.0f / 10.0f;
	double mSecondsPerHScrollTick = 1.0f;
	sint32 mPixelsPerHScrollTick = 1;
	double mTraceDuration = 0;
	sint32 mZoomLevel = -5;

	uint32 mSimEventIdTraceLimited = 0;

	HWND mhwndHScrollbar = nullptr;

	vdrefptr<ATTraceCollection> mpCollection;
	ATTraceSettings mSettings {};

	ATUITraceViewerContext mContext;
	ATUITraceViewerTimescaleView mTimescaleView;
	ATUITraceViewerChannelView mChannelView;
	ATUITraceViewerEventView mEventView;
	ATUITraceViewerCPUHistoryView mCPUHistoryView;
	ATUITraceViewerCPUProfileView mCPUProfileView;
	VDUIProxyToolbarControl mToolbar;
};

ATUITraceViewer::ATUITraceViewer(ATTraceCollection *collection)
	: VDDialogFrameW32(IDD_TRACEVIEWER)
	, mpCollection(collection)
	, mTimescaleView(mContext)
	, mChannelView(mContext)
	, mEventView(mContext)
	, mCPUHistoryView(mContext)
	, mCPUProfileView(mContext)
{
	mContext.mpParent = this;

	ATTraceLoadDefaults(mSettings);

	mToolbar.SetOnClicked([this](uint32 id) { OnToolbarClicked(id); });
}

double ATUITraceViewer::GetViewEndTime() const {
	return mStartTime + mSecondsPerPixel * (double)mEventView.GetClientArea().width();
}

double ATUITraceViewer::GetViewCenterTime() const {
	return mStartTime + mSecondsPerPixel * (double)mEventView.GetClientArea().width() * 0.5;
}

void ATUITraceViewer::StartTracing() {
	SetCollection(nullptr);

	g_sim.SetTracingEnabled(&mSettings);
	g_sim.Resume();
}

void ATUITraceViewer::StopTracing() {
	vdrefptr<ATTraceCollection> newCollection { g_sim.GetTraceCollection() };
	g_sim.SetTracingEnabled(nullptr);
	g_sim.Pause();

	SetCollection(newCollection);
}

void ATUITraceViewer::ZoomIn() {
	ZoomDeltaSteps(GetViewCenterTime(), 1);
}

void ATUITraceViewer::ZoomOut() {
	ZoomDeltaSteps(GetViewCenterTime(), -1);
}

void ATUITraceViewer::SetCollection(ATTraceCollection *collection) {
	if (mpCollection != collection) {
		mpCollection = collection;

		mStartTime = 0;
		RebuildViews();
	}
}

void ATUITraceViewer::ScrollDeltaPixels(sint32 dx, sint32 dy) {
	mStartTime -= mSecondsPerPixel*(double)dx;
	mEventView.ScrollDeltaPixels(mStartTime, dx, dy);
	mTimescaleView.ScrollDeltaPixels(mStartTime, dx);

	UpdateHScroll(false);
}

void ATUITraceViewer::ScrollToCenterTime(double t) {
	double viewWidthTime = (double)mEventViewWidth * mSecondsPerPixel;
	double pixelf = t / mSecondsPerPixel - (double)(mEventViewWidth >> 1);
	double oldStartTime = mStartTime;
	double newStartTime = floor(0.5 + pixelf) * mSecondsPerPixel;

	if (mStartTime == newStartTime)
		return;

	mStartTime = newStartTime;

	// Check if we're scrolling by more than a full view. This is more efficient and
	// also avoids overflow problems in computing the pixel delta.
	if (fabs(newStartTime - oldStartTime) >= viewWidthTime) {
		mEventView.ScrollToTime(mStartTime);
		mTimescaleView.ScrollToTime(mStartTime);
	} else {
		sint32 dx = (sint32)floor(0.5 + (oldStartTime - newStartTime) / mSecondsPerPixel);

		mEventView.ScrollDeltaPixels(mStartTime, dx, 0);
		mTimescaleView.ScrollDeltaPixels(mStartTime, dx);
	}

	UpdateHScroll(false);
}

void ATUITraceViewer::ZoomDeltaSteps(double centerTime, sint32 steps) {
	double pixelOffset = (centerTime - mStartTime) / mSecondsPerPixel;

	mZoomLevel -= steps;

	// Enforce some sane limits: 10ns - 1 second per 96dpi pixel
	mZoomLevel = std::max(std::min(mZoomLevel, 0), -40);

	mSecondsPerPixel = pow(10.0, (double)mZoomLevel / 5.0) * 96.0 / (double)mContext.mDpi;
	mStartTime = centerTime - mSecondsPerPixel * pixelOffset;
	
	int zl2 = (mZoomLevel < 0 ? mZoomLevel - 4 : mZoomLevel) / 5;

	double secondsPerDivision = pow(10.0, zl2 + 2);

	mEventView.RescaleView(mStartTime, mSecondsPerPixel);
	mTimescaleView.RescaleView(mStartTime, mSecondsPerPixel, secondsPerDivision);

	UpdateHScroll(true);
}

void ATUITraceViewer::SetFocusTime(double t) {
	mCPUHistoryView.SetCenterTime(t);

	const double viewWidthTime = mSecondsPerPixel * (double)mEventViewWidth;
	if (t < mStartTime || t > mStartTime + viewWidthTime) {
		double idealStartTime = t - mSecondsPerPixel * (double)(mEventViewWidth >> 1);

		if (fabs(idealStartTime - mStartTime) > viewWidthTime) {
			mStartTime = idealStartTime;
			mEventView.ScrollToTime(mStartTime);
			mTimescaleView.ScrollToTime(mStartTime);
		} else {
			double scrollDelta = idealStartTime - mStartTime;
			sint32 dx = (sint32)floor(0.5 + scrollDelta / mSecondsPerPixel);

			ScrollDeltaPixels(-dx, 0);
		}
	}

	mEventView.SetSelection(t, t);
	UpdateHScroll(false);
}

void ATUITraceViewer::SetSelection(double startTime, double endTime) {
	mCPUProfileView.SetSelectedRange(startTime, endTime);
}

void ATUITraceViewer::SetSelectionMode(bool selMode) {
	if (mContext.mbSelectionMode != selMode) {
		mContext.mbSelectionMode = selMode;
		mEventView.SetSelectionMode(selMode);
		UpdateToolbarSelectionState();
	}
}

bool ATUITraceViewer::OnLoaded() {
	mContext.mDpi = mCurrentDpi;

	HWND hwndToolbar = CreateWindow(TOOLBARCLASSNAME, _T(""), WS_CHILD | WS_VISIBLE | TBSTYLE_FLAT | WS_BORDER, 0, 0, 0, 0, mhdlg, (HMENU)100, VDGetLocalModuleHandleW32(), NULL);
	if (!hwndToolbar)
		return false;

	AddProxy(&mToolbar, hwndToolbar);

	mhwndHScrollbar = GetControl(IDC_HSCROLLBAR);

	SendMessage(mToolbar.GetHandle(), WM_SETFONT, (WPARAM)mhfont, TRUE);

	RebuildToolbar();
	UpdateFonts();

	mTimescaleView.Create(this);
	mChannelView.Create(this);
	mEventView.Create(this);

	OpenHistory();

	OnSize();

	RebuildViews();

	if (!mSimEventIdTraceLimited) {
		mSimEventIdTraceLimited = g_sim.GetEventManager()->AddEventCallback(kATSimEvent_TracingLimitReached, [this] { StopTracing(); });
	}

	return VDDialogFrameW32::OnLoaded();
}

void ATUITraceViewer::OnDestroy() {
	mhwndHScrollbar = nullptr;

	mTimescaleView.Destroy();
	mChannelView.Destroy();
	mEventView.Destroy();
	mCPUHistoryView.Destroy();

	mContext.mChannelFont.reset();
	mContext.mTimestampFont.reset();

	ClearGroupViews();

	if (mSimEventIdTraceLimited) {
		g_sim.GetEventManager()->RemoveEventCallback(mSimEventIdTraceLimited);
		mSimEventIdTraceLimited = 0;
	}
}

void ATUITraceViewer::OnSize() {
	const vdrect32& r = GetClientArea();
	vdrect32 rToolbar = mToolbar.GetArea();

	mToolbar.SetArea(vdrect32(0, 0, r.width(), rToolbar.height()));

	const sint32 toolbarH = rToolbar.bottom - rToolbar.top;
	const sint32 timelineSplitY = toolbarH + mTimescaleView.GetIdealHeight();

	const sint32 hscrollH = GetDpiScaledMetric(SM_CYHSCROLL);
	const sint32 eventViewH = std::max<sint32>(0, r.bottom - hscrollH - toolbarH - mSplitterWidth);

	const sint32 eventViewX1 = mChannelSplitX + mSplitterWidth;
	mEventViewWidth = std::max<sint32>(0, r.right - eventViewX1);

	const sint32 eventViewX2 = eventViewX1 + mEventViewWidth;
	const sint32 eventViewY2 = toolbarH + mSplitterWidth + eventViewH;

	mTimescaleView.SetArea(vdrect32(mChannelSplitX + mSplitterWidth, toolbarH, eventViewX2, timelineSplitY), false);
	mChannelView.SetArea(vdrect32(0, timelineSplitY + mSplitterWidth, mChannelSplitX, eventViewY2), false);
	mEventView.SetArea(vdrect32(eventViewX1, timelineSplitY + mSplitterWidth, eventViewX2, eventViewY2), false);

	if (mhwndHScrollbar)
		SetWindowPos(mhwndHScrollbar, nullptr, eventViewX1, eventViewY2, mEventViewWidth, hscrollH, SWP_NOZORDER | SWP_NOACTIVATE);
}

void ATUITraceViewer::OnHScroll(uint32 id, int code) {
	if (!mhwndHScrollbar)
		return;

	SCROLLINFO si = { sizeof(SCROLLINFO), SIF_POS | SIF_TRACKPOS | SIF_RANGE | SIF_PAGE };
	GetScrollInfo(mhwndHScrollbar, SB_CTL, &si);

	sint32 newPos = si.nPos;

	switch(code) {
		case SB_LEFT:
			newPos = 0;
			break;

		case SB_RIGHT:
			newPos = si.nMax - si.nPage + 1;
			break;

		case SB_LINELEFT:
			newPos = si.nPos - std::max<sint32>(1, (32 * 96) / (mCurrentDpi * mPixelsPerHScrollTick));
			break;

		case SB_LINERIGHT:
			newPos = si.nPos + std::max<sint32>(1, (32 * 96) / (mCurrentDpi * mPixelsPerHScrollTick));
			break;

		case SB_PAGELEFT:
			newPos = si.nPos - si.nPage;
			break;

		case SB_PAGERIGHT:
			newPos = si.nPos + si.nPage;
			break;

		case SB_THUMBTRACK:
		case SB_THUMBPOSITION:
			newPos = si.nTrackPos;
			break;
	}

	newPos = std::max<sint32>(0, std::min<sint32>(newPos, si.nMax - si.nPage + 1));

	if (newPos != si.nPos) {
		si.nPos = newPos;
		si.fMask = SIF_POS;
		SetScrollInfo(mhwndHScrollbar, SB_CTL, &si, TRUE);
	}

	ScrollToCenterTime((double)si.nPos * mSecondsPerHScrollTick);
}

void ATUITraceViewer::OnDpiChanged() {
	mContext.mDpi = mCurrentDpi;

	UpdateFonts();
	OnSize();
	UpdateChannels();
	RebuildToolbar();

	mTimescaleView.UpdateChildDpi();
	mChannelView.UpdateChildDpi();
	mEventView.UpdateChildDpi();

	ZoomDeltaSteps(GetViewCenterTime(), 0);
}

void ATUITraceViewer::RebuildViews() {
	vdrefptr<IATTraceChannel> frameChannel;
	vdrefptr<ATTraceChannelCPUHistory> cpuHistoryChannel;

	ClearGroupViews();

	mTraceDuration = 0;

	if (mpCollection) {
		for(size_t i = 0, n = mpCollection->GetGroupCount(); i < n; ++i) {
			ATTraceGroup *group = mpCollection->GetGroup(i);

			if (group->GetChannelCount() == 0)
				continue;

			mTraceDuration = std::max(mTraceDuration, group->GetDuration());

			ATTraceGroupType type = group->GetType();
			if (type == kATTraceGroupType_Frames) {
				frameChannel = group->GetChannel(0);
				continue;
			} else if (type == kATTraceGroupType_CPUHistory) {
				cpuHistoryChannel = vdpoly_cast<ATTraceChannelCPUHistory *>(group->GetChannel(0));
				continue;
			}

			vdautoptr<ATUITraceViewerGroup> viewerGroup;

			const size_t numChannels = group->GetChannelCount();
			for(size_t chIdx = 0; chIdx < numChannels; ++chIdx) {
				IATTraceChannel *channel = group->GetChannel(chIdx);
				if (channel->IsEmpty())
					continue;

				if (!viewerGroup) {
					viewerGroup = new ATUITraceViewerGroup;
					viewerGroup->mName = group->GetName();
					viewerGroup->mHeight = 0;
					viewerGroup->mpGroup = group;
				}

				vdautoptr<ATUITraceViewerChannel> viewerChannel { new ATUITraceViewerChannel };

				viewerChannel->mPosY = 0;
				viewerChannel->mHeight = 0;
				viewerChannel->mpChannel = channel;
				viewerChannel->mType
					= (type == kATTraceGroupType_Video) ? ATUITraceViewerChannel::kType_Video
					: (type == kATTraceGroupType_Tape) ? ATUITraceViewerChannel::kType_Tape
					: ATUITraceViewerChannel::kType_Default;
				viewerGroup->mChannels.push_back(viewerChannel);
				viewerChannel.release();
			}

			if (viewerGroup) {
				mContext.mGroups.push_back(viewerGroup);
				viewerGroup.release();
			}
		}
	}

	mContext.mpCPUHistoryChannel = cpuHistoryChannel;

	mEventView.SetFrameChannel(frameChannel);
	mCPUHistoryView.SetCPUTraceChannel(cpuHistoryChannel);
	mCPUProfileView.SetCPUTraceChannel(cpuHistoryChannel);

	UpdateChannels();

	ZoomDeltaSteps(0, mZoomLevel + 15);
}

void ATUITraceViewer::RebuildToolbar() {
	mToolbar.Clear();

	{
		VDPixmapBuffer pximg;
		if (ATLoadImageResource(IDB_TOOLBAR_TRACEVIEWER, pximg)) {
			const sint32 iconSize = (GetDpiScaledMetric(SM_CXSMICON) + GetDpiScaledMetric(SM_CXICON)) / 2;

			const uint32 n = pximg.w / pximg.h;
			mToolbar.InitImageList(n, iconSize, iconSize);
			mToolbar.AddImages(n, pximg);
		}
	}

	mToolbar.AddButton(kToolbarId_Stop, 0, L"Stop");
	mToolbar.AddButton(kToolbarId_Start, 1, L"Start");
	mToolbar.AddButton(kToolbarId_ZoomIn, 3, L"Zoom In");
	mToolbar.AddButton(kToolbarId_ZoomOut, 2, L"Zoom Out");
	mToolbar.AddButton(kToolbarId_Settings, 4, L"Settings");
	mToolbar.AddSeparator();
	mToolbar.AddButton(kToolbarId_Select, 5, L"Select");
	mToolbar.AddButton(kToolbarId_Move, 6, L"Move");
	mToolbar.AddButton(kToolbarId_Profile, 7, L"Profile");
	mToolbar.AddButton(kToolbarId_History, 8, L"CPU History");

	mToolbar.AutoSize();

	UpdateToolbarSelectionState();
}

void ATUITraceViewer::UpdateToolbarSelectionState() {
	mToolbar.SetItemPressed(kToolbarId_Select, mContext.mbSelectionMode);
	mToolbar.SetItemPressed(kToolbarId_Move, !mContext.mbSelectionMode);
}

void ATUITraceViewer::UpdateFonts() {
	mContext.mChannelFont.reset(CreateFontW((12 * mContext.mDpi + 48) / 96, 0, 0, 0, 0, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, FF_DONTCARE | DEFAULT_PITCH, L"MS Shell Dlg 2"));
	mContext.mTimestampFont = mContext.mChannelFont;
	mContext.mEventFont = mContext.mChannelFont;

	mContext.mChannelFontMetrics = {};

	if (HDC hdc = GetDC(mhdlg)) {
		HGDIOBJ hOldFont = SelectObject(hdc, mContext.mChannelFont.get());
		if (hOldFont) {
			GetTextMetricsW(hdc, &mContext.mChannelFontMetrics);
			SelectObject(hdc, hOldFont);
		}
		ReleaseDC(mhdlg, hdc);
	}

	mContext.mTimestampFontMetrics = mContext.mChannelFontMetrics;
	mContext.mEventFontMetrics = mContext.mChannelFontMetrics;
}

void ATUITraceViewer::ClearGroupViews() {
	while(!mContext.mGroups.empty()) {
		auto *p = mContext.mGroups.back();
		mContext.mGroups.pop_back();

		for(auto *ch : p->mChannels)
			delete ch;

		delete p;
	}

	mContext.mpCPUHistoryChannel = nullptr;
}

void ATUITraceViewer::UpdateChannels() {
	sint32 posY = 0;
	const sint32 channelHeight = mContext.mEventFontMetrics.tmHeight + (8 * mContext.mDpi + 48) / 96;
	const sint32 videoChannelHeight = (100 * mContext.mDpi + 48) / 96;
	const sint32 tapeChannelHeight = videoChannelHeight >> 1;
	const sint32 groupHeight = mContext.mChannelFontMetrics.tmHeight * 2 + (10 * mContext.mDpi + 48) / 96;

	for (auto *group : mContext.mGroups) {
		sint32 groupY = 0;

		for (auto *ch : group->mChannels) {
			ch->mPosY = groupY;

			switch(ch->mType) {
				case ATUITraceViewerChannel::kType_Default:
				default:
					ch->mHeight = channelHeight;
					break;

				case ATUITraceViewerChannel::kType_Video:
					ch->mHeight = videoChannelHeight;
					break;

				case ATUITraceViewerChannel::kType_Tape:
					ch->mHeight = tapeChannelHeight;
					break;
			}

			groupY += ch->mHeight;
		}

		group->mPosY = posY;
		group->mHeight = std::max<sint32>(groupY, 50);
		posY += group->mHeight;
	}

	mChannelView.OnChannelsChanged();
}

void ATUITraceViewer::UpdateHScroll(bool updateRange) {
	if (!mhwndHScrollbar)
		return;

	if (updateRange) {
		double ticks = mTraceDuration / mSecondsPerPixel;

		mSecondsPerHScrollTick = mSecondsPerPixel;
		mPixelsPerHScrollTick = 1;

		while(ticks > (double)0x1FFFFFFF) {
			ticks *= 0.5;
			mSecondsPerHScrollTick += mSecondsPerHScrollTick;
			mPixelsPerHScrollTick += mPixelsPerHScrollTick;
		}
	}

	SCROLLINFO si {};
	si.cbSize = sizeof(SCROLLINFO);
	si.fMask = SIF_POS;
	si.nPos = (sint32)(0.5 + (mStartTime + (double)mEventViewWidth * 0.5 * mSecondsPerPixel) / mSecondsPerHScrollTick);

	if (updateRange) {
		si.fMask |= SIF_RANGE | SIF_PAGE;
		si.nPage = std::max(1, (mEventViewWidth + mPixelsPerHScrollTick - 1) / mPixelsPerHScrollTick);
		si.nMin = 0;
		si.nMax = (sint32)ceil(mTraceDuration / mSecondsPerHScrollTick) + si.nPage - 1;
	}

	SetScrollInfo(mhwndHScrollbar, SB_CTL, &si, TRUE);
}

void ATUITraceViewer::OnToolbarClicked(uint32 id) {
	switch(id) {
		case kToolbarId_Stop:
			StopTracing();
			break;

		case kToolbarId_Start:
			StartTracing();
			break;

		case kToolbarId_ZoomIn:
			ZoomIn();
			break;

		case kToolbarId_ZoomOut:
			ZoomOut();
			break;

		case kToolbarId_Settings:
			{
				ATUIDialogTraceSettings dlg(mSettings);

				if (dlg.ShowDialog(this))
					ATTraceSaveDefaults(mSettings);
			}
			break;

		case kToolbarId_Select:
			SetSelectionMode(true);
			break;

		case kToolbarId_Move:
			SetSelectionMode(false);
			break;

		case kToolbarId_Profile:
			OpenProfile();
			break;

		case kToolbarId_History:
			OpenHistory();
			break;
	}
}

void ATUITraceViewer::OpenHistory() {
	if (mCPUHistoryView.IsCreated()) {
		ATFrameWindow *frame = ATFrameWindow::GetFrameWindowFromContent(mCPUHistoryView.GetWindowHandle());

		if (frame)
			frame->ActivateFrame();
	} else {
		ATFrameWindow *frame = ATFrameWindow::GetFrameWindow(GetParent(mhdlg));
		if (frame) {
			ATContainerWindow *container = frame->GetContainer();
			vdrefptr<ATFrameWindow> frame2(new ATFrameWindow(container));

			frame2->Create(L"CPU History", CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, (VDGUIHandle)container->GetHandleW32());
			frame2->SetVisible(true);
			container->DockFrame(frame2, kATContainerDockRight);

			mCPUHistoryView.Create((VDGUIHandle)frame2->GetHandleW32());
		}
	}
}

void ATUITraceViewer::OpenProfile() {
	if (mCPUProfileView.IsCreated()) {
		ATFrameWindow *frame = ATFrameWindow::GetFrameWindowFromContent(mCPUProfileView.GetWindowHandle());

		if (frame)
			frame->ActivateFrame();
	} else {
		ATFrameWindow *frame = ATFrameWindow::GetFrameWindow(GetParent(mhdlg));
		if (frame) {
			ATContainerWindow *container = frame->GetContainer();
			vdrefptr<ATFrameWindow> frame2(new ATFrameWindow(container));

			frame2->Create(L"CPU Profile", CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, (VDGUIHandle)container->GetHandleW32());
			frame2->SetVisible(true);
			container->DockFrame(frame2, kATContainerDockBottom);

			mCPUProfileView.Create((VDGUIHandle)frame2->GetHandleW32());
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATUIPerformanceAnalyzerContainerWindow : public ATContainerWindow {
};

///////////////////////////////////////////////////////////////////////////

void ATUIOpenTraceViewer(VDGUIHandle h, ATTraceCollection *collection) {
	vdrefptr<ATUIPerformanceAnalyzerContainerWindow> container { new ATUIPerformanceAnalyzerContainerWindow };

	if (container->Create(CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, nullptr, true)) {
		SetWindowTextW(container->GetHandleW32(), L"Altirra Performance Analyzer");

		vdrefptr<ATFrameWindow> frame(new ATFrameWindow(container));

		frame->Create(L"Trace", CW_USEDEFAULT, CW_USEDEFAULT, 300, 200, (VDGUIHandle)container->GetHandleW32());

		container->DockFrame(frame, kATContainerDockCenter);

		ATUITraceViewer *p = new ATUITraceViewer(collection);
		if (p->Create((VDGUIHandle)frame->GetHandleW32())) {
			RECT r;
			if (GetClientRect(frame->GetHandleW32(), &r)) {
				p->SetArea(vdrect32(0, 0, r.right, r.bottom), false);
			}
		} else {
			delete p;
		}
	}
}
