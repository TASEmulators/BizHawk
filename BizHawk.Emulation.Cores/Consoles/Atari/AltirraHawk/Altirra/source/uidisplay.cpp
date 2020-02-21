//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include <vd2/system/file.h>
#include <vd2/system/math.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/w32assist.h>
#include <vd2/system/win32/touch.h>
#include <vd2/VDDisplay/display.h>
#include <at/atcore/logging.h>
#include <at/atcore/profile.h>
#include <at/atnativeui/messageloop.h>
#include <at/atnativeui/uiframe.h>
#include "console.h"
#include "inputmanager.h"
#include "oshelper.h"
#include "simulator.h"
#include "uidisplay.h"
#include "uienhancedtext.h"
#include "uirender.h"
#include "uitypes.h"
#include "uicaptionupdater.h"
#include <at/atui/uimanager.h>
#include "uimenu.h"
#include "uiportmenus.h"
#include <at/atui/uimenulist.h>
#include <at/atui/uicontainer.h>
#include "uiprofiler.h"
#include <at/atuicontrols/uitextedit.h>
#include <at/atuicontrols/uibutton.h>
#include <at/atuicontrols/uilistview.h>
#include "uifilebrowser.h"
#include "uivideodisplaywindow.h"
#include "uimessagebox.h"
#include "uiqueue.h"
#include "uiaccessors.h"
#include "uicommondialogs.h"
#include "uiprofiler.h"
#include "resource.h"
#include "options.h"
#include "decode_png.h"

///////////////////////////////////////////////////////////////////////////

#ifndef WM_GESTURE
#define WM_GESTURE 0x0119

#ifndef GID_BEGIN
#define GID_BEGIN			1
#endif

#ifndef GID_END
#define GID_END				2
#endif

#ifndef GID_ZOOM
#define GID_ZOOM			3
#endif

#ifndef GID_PAN
#define GID_PAN				4
#endif

DECLARE_HANDLE(HGESTUREINFO);

typedef struct _GESTUREINFO {
	UINT cbSize;
	DWORD dwFlags;
	DWORD dwID;
	HWND hwndTarget;
	POINTS ptsLocation;
	DWORD dwInstanceID;
	DWORD dwSequenceID;
	ULONGLONG ullArguments;
	UINT cbExtraArgs;
} GESTUREINFO, *PGESTUREINFO;
#endif

BOOL WINAPI ATGetGestureInfoW32(HGESTUREINFO hGestureInfo, PGESTUREINFO pGestureInfo) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "GetGestureInfo");

	return pfn && ((BOOL (WINAPI *)(HGESTUREINFO, PGESTUREINFO))pfn)(hGestureInfo, pGestureInfo);
}

BOOL WINAPI ATCloseGestureInfoHandleW32(HGESTUREINFO hGestureInfo) {
	static auto pfn = GetProcAddress(GetModuleHandle(_T("user32")), "CloseGestureInfoHandle");

	return pfn && ((BOOL (WINAPI *)(HGESTUREINFO))pfn)(hGestureInfo);
}

///////////////////////////////////////////////////////////////////////////

extern ATSimulator g_sim;

extern IVDVideoDisplay *g_pDisplay;

extern bool g_ATAutoFrameFlipping;
extern LOGFONTW g_enhancedTextFont;

extern ATDisplayFilterMode g_dispFilterMode;
extern int g_dispFilterSharpness;

extern ATDisplayStretchMode g_displayStretchMode;

ATLogChannel g_ATLCHostKeys(false, false, "HOSTKEYS", "Host keyboard activity");

ATUIManager g_ATUIManager;

void OnCommandEditPasteText();

void ATUIFlushDisplay();
void ATUIResizeDisplay();

void ATUISetDragDropSubTarget(VDGUIHandle h, ATUIManager *mgr);

///////////////////////////////////////////////////////////////////////////

ATUIVideoDisplayWindow *g_pATVideoDisplayWindow;
bool g_dispPadIndicators = false;

void ATUIOpenOnScreenKeyboard() {
	if (g_pATVideoDisplayWindow)
		g_pATVideoDisplayWindow->OpenOSK();
}

void ATUIToggleHoldKeys() {
	if (g_pATVideoDisplayWindow)
		g_pATVideoDisplayWindow->ToggleHoldKeys();
}

bool ATUIGetDisplayPadIndicators() {
	return g_dispPadIndicators;
}

void ATUISetDisplayPadIndicators(bool enabled) {
	if (g_dispPadIndicators != enabled) {
		g_dispPadIndicators = enabled;

		ATUIResizeDisplay();
	}
}

bool ATUIGetDisplayIndicators() {
	IATUIRenderer *r = g_sim.GetUIRenderer();
	return r->IsVisible();
}

void ATUISetDisplayIndicators(bool enabled) {
	IATUIRenderer *r = g_sim.GetUIRenderer();
	r->SetVisible(enabled);

	if (g_dispPadIndicators)
		ATUIResizeDisplay();
}

#ifndef MOUSEEVENTF_MASK
	#define MOUSEEVENTF_MASK 0xFFFFFF00
#endif

#ifndef MOUSEEVENTF_FROMTOUCH
	#define MOUSEEVENTF_FROMTOUCH 0xFF515700
#endif

namespace {
	bool IsInjectedTouchMouseEvent() {
		// No WM_TOUCH prior to Windows 7.
		if (!VDIsAtLeast7W32())
			return false;

		// Recommended by MSDN. Seriously. Bit 7 is to distinguish pen events from
		// touch events.
		return (GetMessageExtraInfo() & (MOUSEEVENTF_MASK | 0x80)) == (MOUSEEVENTF_FROMTOUCH | 0x80);

	}
}

///////////////////////////////////////////////////////////////////////////

class ATDisplayImageDecoder final : public IVDDisplayImageDecoder {
public:
	bool DecodeImage(VDPixmapBuffer& buf, VDBufferedStream& stream) override;
} gATDisplayImageDecoder;

bool ATDisplayImageDecoder::DecodeImage(VDPixmapBuffer& buf, VDBufferedStream& stream) {
	vdautoptr<IVDImageDecoderPNG> dec { VDCreateImageDecoderPNG() };

	sint64 len = stream.Length();
	if (len > 64*1024*1024)
		return false;

	vdblock<uint8> rawbuf(len);
	stream.Read(rawbuf.data(), len);
	auto err = dec->Decode(rawbuf.data(), len);

	if (err != kPNGDecodeOK)
		return false;

	buf.assign(dec->GetFrameBuffer());
	return true;
}

///////////////////////////////////////////////////////////////////////////

struct ATUIStepDoModalWindow final : public vdrefcount {
	ATUIStepDoModalWindow()
		: mbModalWindowActive(true)
	{
	}

	void Run() {
		if (mbModalWindowActive) {
			auto thisptr = vdmakerefptr(this);
			ATUIPushStep([thisptr]() { thisptr->Run(); });

			int rc;
			ATUIProcessMessages(true, rc);
		}
	}

	bool mbModalWindowActive;
};

class ATUINativeDisplay final : public IATUINativeDisplay, public IATUIClipboard {
public:
	ATUINativeDisplay()
		: mhwnd(NULL)
		, mpFrame(NULL)
		, mModalCount(0)
		, mbMouseConstrained(false)
		, mbMouseCaptured(false)
		, mbMouseMotionMode(false)
		, mbCursorImageChangesEnabled(false)
		, mhcurTarget((HCURSOR)LoadImage(VDGetLocalModuleHandleW32(), MAKEINTRESOURCE(IDC_TARGET), IMAGE_CURSOR, 0, 0, LR_SHARED))
		, mhcurTargetOff((HCURSOR)LoadImage(VDGetLocalModuleHandleW32(), MAKEINTRESOURCE(IDC_TARGET_OFF), IMAGE_CURSOR, 0, 0, LR_SHARED))
	{
	}

	virtual void Invalidate() {
		if (!g_pDisplay)
			return;

		if (mbIgnoreAutoFlipping || (!g_ATAutoFrameFlipping || mModalCount))
			g_pDisplay->Invalidate();
	}

	virtual void ConstrainCursor(bool constrain) {
		if (constrain) {
			if (mhwnd) {
				RECT r;

				if (::GetClientRect(mhwnd, &r)) {
					::MapWindowPoints(mhwnd, NULL, (LPPOINT)&r, 2);
					::ClipCursor(&r);
				}
			}
			mbMouseConstrained = true;
		} else {
			::ClipCursor(NULL);
			mbMouseConstrained = false;
		}
	}

	virtual void CaptureCursor(bool motionMode) {
		if (!mbMouseCaptured) {
			if (mhwnd)
				::SetCapture(mhwnd);

			mbMouseCaptured = true;
		}

		if (mbMouseMotionMode != motionMode) {
			mbMouseMotionMode = motionMode;

			if (motionMode)
				WarpCapturedMouse();
		}
	}

	virtual void ReleaseCursor() {
		mbMouseCaptured = false;
		mbMouseMotionMode = false;
		mbMouseConstrained = false;

		if (mhwnd)
			::ReleaseCapture();
	}

	virtual vdpoint32 GetCursorPosition() {
		if (!mhwnd)
			return vdpoint32(0, 0);

		DWORD pos = GetMessagePos();
		POINT pt = { (short)LOWORD(pos), (short)HIWORD(pos) };

		::ScreenToClient(mhwnd, &pt);

		return vdpoint32(pt.x, pt.y);
	}

	virtual void SetCursorImage(uint32 id) {
		if (mbCursorImageChangesEnabled)
			SetCursorImageDirect(id);
	}

	virtual void *BeginModal() {
		ATUIStepDoModalWindow *step = new ATUIStepDoModalWindow;
		step->AddRef();

		auto stepptr = vdmakerefptr(step);
		ATUIPushStep([stepptr]() { stepptr->Run(); });

		if (!mModalCount++) {
			ATUISetMenuEnabled(false);

			if (mpFrame)
				mpFrame->GetContainer()->SetModalFrame(mpFrame);
		}

		ATUIFlushDisplay();

		return step;
	}

	virtual void EndModal(void *cookie) {
		VDASSERT(mModalCount);

		if (!--mModalCount) {
			if (mpFrame)
				mpFrame->GetContainer()->SetModalFrame(NULL);

			ATUISetMenuEnabled(true);
		}

		ATUIStepDoModalWindow *step = (ATUIStepDoModalWindow *)cookie;
		step->mbModalWindowActive = false;
		step->Release();
	}

	virtual bool IsKeyDown(uint32 vk) {
		return GetKeyState(vk) < 0;
	}

	virtual IATUIClipboard *GetClipboard() {
		return this;
	}

public:
	virtual void CopyText(const char *s) {
		if (mhwnd)
			ATCopyTextToClipboard(mhwnd, s);
	}

public:
	bool IsMouseCaptured() const { return mbMouseCaptured; }
	bool IsMouseConstrained() const { return mbMouseConstrained; }
	bool IsMouseMotionModeEnabled() const { return mbMouseMotionMode; }

	void SetDisplay(HWND hwnd, ATFrameWindow *frame) {
		if (mpFrame && mModalCount)
			mpFrame->GetContainer()->SetModalFrame(NULL);

		mhwnd = hwnd;
		mpFrame = frame;

		if (mpFrame && mModalCount)
			mpFrame->GetContainer()->SetModalFrame(mpFrame);
	}

	vdpoint32 WarpCapturedMouse() {
		RECT r;
		if (!mhwnd || !::GetClientRect(mhwnd, &r))
			return vdpoint32(0, 0);

		const vdpoint32 pt(r.right >> 1, r.bottom >> 1);

		POINT pt2 = {pt.x, pt.y};
		::ClientToScreen(mhwnd, &pt2);
		::SetCursorPos(pt2.x, pt2.y);

		return pt;
	}

	void OnCaptureLost() {
		mbMouseCaptured = false;
		mbMouseMotionMode = false;
		mbMouseConstrained = false;
	}

	void SetCursorImageChangesEnabled(bool enabled) {
		mbCursorImageChangesEnabled = enabled;
	}

	void SetCursorImageDirect(uint32 id) {
		switch(id) {
			case kATUICursorImage_Hidden:
				::SetCursor(NULL);
				break;

			case kATUICursorImage_Arrow:
				::SetCursor(::LoadCursor(NULL, IDC_ARROW));
				break;

			case kATUICursorImage_IBeam:
				::SetCursor(::LoadCursor(NULL, IDC_IBEAM));
				break;

			case kATUICursorImage_Cross:
				::SetCursor(::LoadCursor(NULL, IDC_CROSS));
				break;

			case kATUICursorImage_Query:
				::SetCursor(::LoadCursor(NULL, IDC_HELP));
				break;

			case kATUICursorImage_Target:
				::SetCursor(mhcurTarget);
				break;

			case kATUICursorImage_TargetOff:
				::SetCursor(mhcurTargetOff);
				break;
		}
	}

	void SetIgnoreAutoFlipping(bool ignore) { mbIgnoreAutoFlipping = ignore; }

private:
	HWND mhwnd;
	ATFrameWindow *mpFrame;
	uint32 mModalCount;
	bool mbMouseCaptured;
	bool mbMouseConstrained;
	bool mbMouseMotionMode;
	bool mbCursorImageChangesEnabled;
	bool mbIgnoreAutoFlipping = false;
	HCURSOR	mhcurTarget;
	HCURSOR	mhcurTargetOff;
} g_ATUINativeDisplay;

///////////////////////////////////////////////////////////////////////////

void ATUIUpdateThemeScaleCallback(ATOptions& opts, const ATOptions *prevOpts, void *) {
	sint32 scale = opts.mThemeScale;

	if (scale < 10)
		scale = 10;

	if (scale > 1000)
		scale = 1000;

	g_ATUIManager.SetThemeScaleFactor((float)opts.mThemeScale / 100.0f);
}

void ATUIInitManager() {
	ATOptionsAddUpdateCallback(true, ATUIUpdateThemeScaleCallback);
	g_ATUIManager.Init(&g_ATUINativeDisplay);

	VDDisplaySetImageDecoder(&gATDisplayImageDecoder);
	g_pATVideoDisplayWindow = new ATUIVideoDisplayWindow;
	g_pATVideoDisplayWindow->AddRef();
	g_pATVideoDisplayWindow->Init(*g_sim.GetEventManager(), *g_sim.GetDeviceManager());
	g_ATUIManager.GetMainWindow()->AddChild(g_pATVideoDisplayWindow);
	g_pATVideoDisplayWindow->SetDockMode(kATUIDockMode_Fill);

	g_sim.GetUIRenderer()->SetUIManager(&g_ATUIManager);

	g_pATVideoDisplayWindow->Focus();

//	ATUIProfileCreateWindow(&g_ATUIManager);
}

void ATUIShutdownManager() {
	ATUIProfileDestroyWindow();

	vdsaferelease <<= g_pATVideoDisplayWindow;

	g_sim.GetUIRenderer()->SetUIManager(NULL);

	g_ATUIManager.Shutdown();
}

void ATUITriggerButtonDown(uint32 vk) {
	ATUIKeyEvent event;
	event.mVirtKey = vk;
	event.mExtendedVirtKey = vk;
	event.mbIsRepeat = false;
	event.mbIsExtendedKey = false;
	g_ATUIManager.OnKeyDown(event);
}

void ATUITriggerButtonUp(uint32 vk) {
	ATUIKeyEvent event;
	event.mVirtKey = vk;
	event.mExtendedVirtKey = vk;
	event.mbIsRepeat = false;
	event.mbIsExtendedKey = false;
	g_ATUIManager.OnKeyUp(event);
}

void ATUIFlushDisplay() {
	if (g_ATUIManager.IsInvalidated()) {
		if (g_pDisplay)
			g_pDisplay->Invalidate();
	}
}

bool ATUIIsActiveModal() {
	return g_ATUIManager.GetModalWindow() != NULL;
}

class ATDisplayPane final : public ATUIPane, public IATDisplayPane {
public:
	ATDisplayPane();
	~ATDisplayPane();

	void *AsInterface(uint32 iid);

	void ReleaseMouse();
	void ToggleCaptureMouse();
	void ResetDisplay();

	bool IsTextSelected() const { return g_pATVideoDisplayWindow->IsTextSelected(); }
	void Copy(bool enableEscaping);
	void CopyFrame(bool trueAspect);
	void SaveFrame(bool trueAspect, const wchar_t *path = nullptr);
	void Paste();
	void Paste(const wchar_t *s, size_t len);

	void OnSize();
	void UpdateFilterMode();

protected:
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);
	virtual ATUITouchMode GetTouchModeAtPoint(const vdpoint32& pt) const;

	bool OnCreate();
	void OnDestroy();
	void OnMouseMove(WPARAM wParam, LPARAM lParam);
	void OnMouseHover(WPARAM wParam);

	ATUIKeyEvent ConvertVirtKeyEvent(WPARAM wParam, LPARAM lParam);

	void SetHaveMouse();
	void WarpCapturedMouse();
	void UpdateTextDisplay(bool enabled);
	void UpdateTextModeFont();

	void OnMenuActivated(ATUIMenuList *);
	void OnMenuItemSelected(ATUIMenuList *sender, uint32 id);
	void OnAllowContextMenu();
	void OnDisplayContextMenu(const vdpoint32& pt);
	void OnOSKChange();
	void ResizeDisplay();

	HWND	mhwndDisplay = nullptr;
	HMENU	mhmenuContext = nullptr;
	IVDVideoDisplay *mpDisplay = nullptr;
	int		mLastTrackMouseX = 0;
	int		mLastTrackMouseY = 0;
	int		mMouseCursorLock = 0;
	int		mWidth = 0;
	int		mHeight = 0;

	vdpoint32	mGestureOrigin = { 0, 0 };

	vdrect32	mDisplayRect = { 0, 0, 0, 0 };

	bool	mbTextModeEnabled = false;
	bool	mbTextModeVirtScreen = false;
	bool	mbHaveMouse = false;
	bool	mbEatNextInjectedCaps = false;
	bool	mbTurnOffCaps = false;
	bool	mbAllowContextMenu = false;

	enum {
		kDblTapState_Initial,
		kDblTapState_WaitDown1,
		kDblTapState_WaitDown2,
		kDblTapState_WaitUp2,
		kDblTapState_Invalid
	} mDblTapState = kDblTapState_Initial;

	DWORD	mDblTapTime = 0;
	LONG	mDblTapX = 0;
	LONG	mDblTapY = 0;
	LONG	mDblTapThreshold = 0;

	vdautoptr<IATUIEnhancedTextEngine> mpEnhTextEngine;
	vdrefptr<ATUIMenuList> mpMenuBar;

	vdfunction<void()> mIndicatorSafeAreaChangedFn;

	TOUCHINPUT mRawTouchInputs[32];
	ATUITouchInput mTouchInputs[32];
};

ATDisplayPane::ATDisplayPane()
	: ATUIPane(kATUIPaneId_Display, L"Display")
	, mhmenuContext(LoadMenu(NULL, MAKEINTRESOURCE(IDR_DISPLAY_CONTEXT_MENU)))
{
	SetTouchMode(kATUITouchMode_Dynamic);

	mPreferredDockCode = kATContainerDockCenter;
}

ATDisplayPane::~ATDisplayPane() {
	if (mhmenuContext)
		DestroyMenu(mhmenuContext);
}

void *ATDisplayPane::AsInterface(uint32 iid) {
	if (iid == IATDisplayPane::kTypeID)
		return static_cast<IATDisplayPane *>(this);

	return ATUIPane::AsInterface(iid);
}

LRESULT ATDisplayPane::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case ATWM_PRESYSKEYDOWN:
		case ATWM_PREKEYDOWN:
			if (mbEatNextInjectedCaps && LOWORD(wParam) == VK_CAPITAL) {
				// drop injected CAPS LOCK keys
				if (!(lParam & 0x00ff0000)) {
					mbEatNextInjectedCaps = false;
					return true;
				}
			}

			{
				bool result = g_ATUIManager.OnKeyDown(ConvertVirtKeyEvent(wParam, lParam));

				g_ATLCHostKeys("Received host vkey down: VK=$%02X LP=%08X (current key mask: %016llX)%s\n", LOWORD(wParam), (unsigned)lParam, g_sim.GetPokey().GetRawKeyMask(), (HIWORD(lParam) & KF_REPEAT) != 0 ? " (repeat)" : "");

				if (result) {
					if (LOWORD(wParam) == VK_CAPITAL)
						mbTurnOffCaps = true;

					return true;
				}
			}
			return false;


		case ATWM_PRESYSKEYUP:
		case ATWM_PREKEYUP:
			if (LOWORD(wParam) == VK_CAPITAL && mbTurnOffCaps && (GetKeyState(VK_CAPITAL) & 1)) {
				mbTurnOffCaps = false;

				// force caps lock back off
				mbEatNextInjectedCaps = true;

				keybd_event(VK_CAPITAL, 0, 0, 0);
				keybd_event(VK_CAPITAL, 0, KEYEVENTF_KEYUP, 0);
			}


			// Repost the key up message back to ourselves in case we need it to match against
			// a WM_CHAR message. This is necessary since WM_CHAR is posted and in theory could
			// arrive after the corresponding WM_KEYUP, although this has not been seen in practice.
			PostMessage(mhwnd, ATWM_CHARUP, wParam, lParam);

			{
				bool result = g_ATUIManager.OnKeyUp(ConvertVirtKeyEvent(wParam, lParam));

				g_ATLCHostKeys("Received host vkey up:   VK=$%02X LP=%08X (current key mask: %016llX)\n", LOWORD(wParam), (unsigned)lParam, g_sim.GetPokey().GetRawKeyMask());
				return result;
			}

		case ATWM_SETFULLSCREEN:
			if (mpMenuBar)
				mpMenuBar->SetVisible(wParam != 0);

			return 0;

		case ATWM_ENDTRACKING:
			if (g_pATVideoDisplayWindow)
				g_pATVideoDisplayWindow->EndEnhTextSizeIndicator();
			return 0;

		case ATWM_FORCEKEYSUP:
			g_ATUIManager.OnForceKeysUp();
			return 0;

		case WM_SIZE:
			{
				int w = LOWORD(lParam);
				int h = HIWORD(lParam);

				if (w != mWidth || h != mHeight) {
					mWidth = w;
					mHeight = h;

					g_ATUIManager.Resize(w, h);

					IATUIRenderer *r = g_sim.GetUIRenderer();

					if (r)
						r->Relayout(w, h);

					if (g_pATVideoDisplayWindow) {
						ATFrameWindow *frame = ATFrameWindow::GetFrameWindow(GetParent(mhwnd));
						
						if (frame && frame->IsActivelyMovingSizing()) {
							frame->EnableEndTrackNotification();
							g_pATVideoDisplayWindow->BeginEnhTextSizeIndicator();
						}
					}
				}
			}
			break;

		case WM_CHAR:
			{
				ATUICharEvent event;
				event.mCh = (uint32)wParam;
				event.mScanCode = (uint32)((lParam >> 16) & 0xFF);
				event.mbIsRepeat = (lParam & 0x40000000) != 0;

				g_ATUIManager.OnChar(event);

				g_ATLCHostKeys("Received host char:      CH=$%02X LP=%08X (current key mask: %016llX)%s\n", LOWORD(wParam), (unsigned)lParam, g_sim.GetPokey().GetRawKeyMask(), event.mbIsRepeat ? " (repeat)" : "");
			}

			return 0;

		case ATWM_CHARUP:
			{
				ATUICharEvent event;
				event.mCh = 0;
				event.mScanCode = (uint32)((lParam >> 16) & 0xFF);
				event.mbIsRepeat = false;

				g_ATUIManager.OnCharUp(event);
			}

			g_ATLCHostKeys("Received host char up:   LP=%08X (current key mask: %016llX)\n", (unsigned)lParam, g_sim.GetPokey().GetRawKeyMask());
			return 0;

		case WM_PARENTNOTIFY:
			switch(LOWORD(wParam)) {
			case WM_LBUTTONDOWN:
			case WM_MBUTTONDOWN:
			case WM_RBUTTONDOWN:
				SetFocus(mhwnd);
				break;
			}
			break;

		case WM_LBUTTONUP:
			if (IsInjectedTouchMouseEvent())
				break;

			g_ATUIManager.OnMouseUp((short)LOWORD(lParam), (short)HIWORD(lParam), kATUIVK_LButton);
			break;

		case WM_MBUTTONUP:
			g_ATUIManager.OnMouseUp((short)LOWORD(lParam), (short)HIWORD(lParam), kATUIVK_MButton);
			break;

		case WM_RBUTTONUP:
			mbAllowContextMenu = false;
			g_ATUIManager.OnMouseUp((short)LOWORD(lParam), (short)HIWORD(lParam), kATUIVK_RButton);

			if (mbAllowContextMenu)
				break;

			return 0;

		case WM_XBUTTONUP:
			switch(HIWORD(wParam)) {
				case XBUTTON1:
					g_ATUIManager.OnMouseUp((short)LOWORD(lParam), (short)HIWORD(lParam), kATUIVK_XButton1);
					break;
				case XBUTTON2:
					g_ATUIManager.OnMouseUp((short)LOWORD(lParam), (short)HIWORD(lParam), kATUIVK_XButton2);
					break;
			}
			break;

		case WM_LBUTTONDOWN:
		case WM_LBUTTONDBLCLK:
			if (IsInjectedTouchMouseEvent())
				break;

			// fall through

		case WM_MBUTTONDOWN:
		case WM_MBUTTONDBLCLK:
		case WM_RBUTTONDOWN:
		case WM_RBUTTONDBLCLK:
		case WM_XBUTTONDOWN:
		case WM_XBUTTONDBLCLK:
			SetHaveMouse();

			::SetFocus(mhwnd);

			{
				int x = (short)LOWORD(lParam);
				int y = (short)HIWORD(lParam);

				if (!ATUIGetFullscreen())
					ATUISetNativeDialogMode(true);

				switch(msg) {
					case WM_LBUTTONDOWN:
					case WM_LBUTTONDBLCLK:
						if (g_ATUIManager.OnMouseDown(x, y, kATUIVK_LButton, msg == WM_LBUTTONDBLCLK))
							return 0;
						break;

					case WM_RBUTTONDOWN:
					case WM_RBUTTONDBLCLK:
						if (g_ATUIManager.OnMouseDown(x, y, kATUIVK_RButton, false))
							return 0;
						break;

					case WM_MBUTTONDOWN:
					case WM_MBUTTONDBLCLK:
						if (g_ATUIManager.OnMouseDown(x, y, kATUIVK_MButton, false))
							return 0;
						break;

					case WM_XBUTTONDOWN:
					case WM_XBUTTONDBLCLK:
						switch(HIWORD(wParam)) {
							case XBUTTON1:
								if (g_ATUIManager.OnMouseDown(x, y, kATUIVK_XButton1, false))
									return 0;
								break;
							case XBUTTON2:
								if (g_ATUIManager.OnMouseDown(x, y, kATUIVK_XButton2, false))
									return 0;
								break;
						}
						break;
				}
			}
			break;

		case WM_MOUSEWHEEL:
			{
				int x = (short)LOWORD(lParam);
				int y = (short)HIWORD(lParam);
				int dz = (short)HIWORD(wParam);

				POINT pt = { x, y };
				ScreenToClient(mhwnd, &pt);

				UINT lines = 0;
				::SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, &lines, FALSE);
				g_ATUIManager.OnMouseWheel(pt.x, pt.y, (float)dz / (float)WHEEL_DELTA * (float)lines);
			}
			break;

		case WM_CONTEXTMENU:
			{
				POINT pt = { (short)LOWORD(lParam), (short)HIWORD(lParam) };

				if (pt.x == -1 && pt.y == -1) {
					g_ATUIManager.OnContextMenu(NULL);
					return 0;
				}

				ScreenToClient(mhwnd, &pt);

				if ((uint32)pt.x < (uint32)mWidth && (uint32)pt.y < (uint32)mHeight) {
					vdpoint32 pt2(pt.x, pt.y);
					g_ATUIManager.OnContextMenu(&pt2);
					return 0;
				}
			}
			break;

		case WM_SETCURSOR:
			if (mMouseCursorLock)
				break;

			if (LOWORD(lParam) == HTCLIENT) {
				const uint32 id = g_ATUIManager.GetCurrentCursorImageId();

				if (id) {
					g_ATUINativeDisplay.SetCursorImageDirect(id);
					return 0;
				}
			}
			break;

		case WM_MOUSEMOVE:
			OnMouseMove(wParam, lParam);
			break;

		case WM_MOUSEHOVER:
			OnMouseHover(wParam);
			return 0;

		case WM_MOUSELEAVE:
			// We can get mouse leave messages while the mouse is captured. Suppress these
			// until capture ends.
			if (::GetCapture() != mhwnd) {
				g_ATUINativeDisplay.SetCursorImageChangesEnabled(false);
				mbHaveMouse = false;

				g_ATUIManager.OnMouseLeave();
			}
			return 0;

		case WM_SETFOCUS:
			// Remember that we are effectively nesting window managers here -- when the display
			// window gains focus, to our window manager it is like a Win32 window becoming
			// active.
			g_ATUIManager.SetForeground(true);
			break;

		case WM_KILLFOCUS:
			g_ATUIManager.SetForeground(false);

			if (g_ATUINativeDisplay.IsMouseConstrained())
				::ClipCursor(NULL);

			if (::GetCapture() == mhwnd)
				::ReleaseCapture();
			break;

		case WM_CAPTURECHANGED:
			if (g_ATUINativeDisplay.IsMouseCaptured()) {
				g_ATUINativeDisplay.OnCaptureLost();
				g_ATUIManager.OnCaptureLost();
			}

			if (mbHaveMouse) {
				TRACKMOUSEEVENT tme = {sizeof(TRACKMOUSEEVENT)};
				tme.dwFlags = TME_LEAVE;
				tme.hwndTrack = mhwnd;
				::TrackMouseEvent(&tme);
			}
			break;

		case WM_ERASEBKGND:
			return 0;

		case WM_COPY:
			Copy(false);
			return 0;

		case WM_PASTE:
			Paste();
			return 0;

		case WM_ENTERMENULOOP:
			// We get this directly for a pop-up menu.
			g_ATUIManager.OnForceKeysUp();
			break;

		case ATWM_GETAUTOSIZE:
			{
				vdsize32& sz = *(vdsize32 *)lParam;

				sz.w = mDisplayRect.width();
				sz.h = mDisplayRect.height();

				if (g_dispPadIndicators) {
					if (auto *p = g_sim.GetUIRenderer())
						sz.h += p->GetIndicatorSafeHeight();
				}

				return TRUE;
			}
			break;

		case WM_TOUCH:
			{
				uint32 numInputs = LOWORD(wParam);
				if (numInputs > vdcountof(mTouchInputs))
					numInputs = vdcountof(mTouchInputs);

				HTOUCHINPUT hti = (HTOUCHINPUT)lParam;
				if (numInputs && VDGetTouchInputInfoW32(hti, numInputs, mRawTouchInputs, sizeof(TOUCHINPUT))) {
					const TOUCHINPUT *src = mRawTouchInputs;
					ATUITouchInput *dst = mTouchInputs;
					uint32 numCookedInputs = 0;

					SetHaveMouse();

					::SetFocus(mhwnd);

					if (!ATUIGetFullscreen())
						ATUISetNativeDialogMode(true);

					for(uint32 i=0; i<numInputs; ++i, ++src) {
						if (src->dwFlags & TOUCHEVENTF_PALM)
							continue;

						POINT pt = { (src->x + 50) / 100, (src->y + 50) / 100 };

						if (!::ScreenToClient(mhwnd, &pt))
							continue;

						dst->mId = src->dwID;
						dst->mX = pt.x;
						dst->mY = pt.y;
						dst->mbPrimary = (src->dwFlags & TOUCHEVENTF_PRIMARY) != 0;
						dst->mbDown = (src->dwFlags & TOUCHEVENTF_DOWN) != 0;
						dst->mbUp = (src->dwFlags & TOUCHEVENTF_UP) != 0;
						dst->mbDoubleTap = false;

						if (dst->mbPrimary) {
							bool dblTapValid = false;

							if (mDblTapState != kDblTapState_Initial) {
								// The regular double-click metrics are too small for touch.
								if (abs(pt.x - mDblTapX) <= 32
									&& abs(pt.y - mDblTapY) <= 32)
								{
									dblTapValid = true;
								}
							}

							if (dst->mbDown) {
								if (mDblTapState == kDblTapState_Initial) {
									mDblTapState = kDblTapState_WaitDown1;
									mDblTapX = pt.x;
									mDblTapY = pt.y;
									mDblTapTime = VDGetCurrentTick();
								} else if (mDblTapState == kDblTapState_WaitDown2) {
									if (dblTapValid && (VDGetCurrentTick() - mDblTapTime) <= GetDoubleClickTime()) {
										dst->mbDoubleTap = true;
										mDblTapState = kDblTapState_WaitUp2;
									} else {
										mDblTapX = pt.x;
										mDblTapY = pt.y;
										mDblTapTime = VDGetCurrentTick();
									}
								}

								mGestureOrigin = vdpoint32(pt.x, pt.y);
							} else if (dst->mbUp) {
								if (mDblTapState == kDblTapState_Invalid)
									mDblTapState = kDblTapState_Initial;
								else if (mDblTapState == kDblTapState_WaitDown1) {
									if (dblTapValid)
										mDblTapState = kDblTapState_WaitDown2;
									else
										mDblTapState = kDblTapState_Initial;
								} else if (mDblTapState == kDblTapState_WaitUp2) {
									mDblTapState = kDblTapState_Initial;
								}

								// A swipe up of at least 1/6th of the screen from the bottom quarter opens
								// the OSK.
								sint32 dy = pt.y - mGestureOrigin.y;

								if (dy < -mHeight / 6 && mGestureOrigin.y >= mHeight * 3 / 4) {
									if (g_pATVideoDisplayWindow)
										g_pATVideoDisplayWindow->OpenOSK();
								}
							} else {
								if (mDblTapState != kDblTapState_Initial && !dblTapValid)
									mDblTapState = kDblTapState_Invalid;
							}
						}

						++dst;
						++numCookedInputs;
					}

					VDCloseTouchInputHandleW32(hti);

					g_ATUIManager.OnTouchInput(mTouchInputs, numCookedInputs);
					return 0;
				}
			}

			break;
	}

	return ATUIPane::WndProc(msg, wParam, lParam);
}

ATUITouchMode ATDisplayPane::GetTouchModeAtPoint(const vdpoint32& pt) const {
	return g_ATUIManager.GetTouchModeAtPoint(pt);
}

bool ATDisplayPane::OnCreate() {
	if (!ATUIPane::OnCreate())
		return false;

	mhwndDisplay = (HWND)VDCreateDisplayWindowW32(WS_EX_NOPARENTNOTIFY, WS_CHILD|WS_VISIBLE, 0, 0, 0, 0, (VDGUIHandle)mhwnd);
	if (!mhwndDisplay)
		return false;

	mpDisplay = VDGetIVideoDisplay((VDGUIHandle)mhwndDisplay);
	g_pDisplay = mpDisplay;

	mpDisplay->SetReturnFocus(true);
	mpDisplay->SetTouchEnabled(true);
	mpDisplay->SetUse16Bit(g_ATOptions.mbDisplay16Bit);
	UpdateFilterMode();
	mpDisplay->SetAccelerationMode(IVDVideoDisplay::kAccelResetInForeground);
	mpDisplay->SetCompositor(&g_ATUIManager);

	// We need to push in an initial frame for two reasons: (1) black out immediately, (2) force full
	// screen mode to size the window correctly.
	mpDisplay->SetSourceSolidColor(0);

	mpDisplay->SetProfileHook(
		[](IVDVideoDisplay::ProfileEvent event) {
			switch(event) {
				case IVDVideoDisplay::kProfileEvent_BeginTick:
					ATProfileBeginRegion(kATProfileRegion_DisplayTick);
					break;

				case IVDVideoDisplay::kProfileEvent_EndTick:
					ATProfileEndRegion(kATProfileRegion_DisplayTick);
					break;
			}
		}
	);

	g_ATUINativeDisplay.SetDisplay(mhwnd, ATFrameWindow::GetFrameWindow(GetParent(mhwnd)));

	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	if (!mpEnhTextEngine)
		gtia.SetVideoOutput(mpDisplay);

	gtia.UpdateScreen(true, true);

	g_ATUINativeDisplay.SetIgnoreAutoFlipping(mpEnhTextEngine != nullptr);

	mpMenuBar = new ATUIMenuList;
	mpMenuBar->SetVisible(false);
	g_ATUIManager.GetMainWindow()->AddChild(mpMenuBar);
	mpMenuBar->SetFont(g_ATUIManager.GetThemeFont(kATUIThemeFont_Menu));
	mpMenuBar->SetMenu(ATUIGetMenu());
	mpMenuBar->SetAutoHide(true);
	mpMenuBar->SetDockMode(kATUIDockMode_TopFloat);
	mpMenuBar->OnActivatedEvent() = ATBINDCALLBACK(this, &ATDisplayPane::OnMenuActivated);
	mpMenuBar->OnItemSelected() = ATBINDCALLBACK(this, &ATDisplayPane::OnMenuItemSelected);
	mpMenuBar->SetArea(vdrect32(0, 0, 0, mpMenuBar->GetIdealHeight()));

	g_pATVideoDisplayWindow->SetOnAllowContextMenu([this]() { OnAllowContextMenu(); });
	g_pATVideoDisplayWindow->SetOnDisplayContextMenu([this](const vdpoint32& pt) { OnDisplayContextMenu(pt); });
	g_pATVideoDisplayWindow->SetOnOSKChange([this]() { OnOSKChange(); });

	g_pATVideoDisplayWindow->SetDisplaySourceMapping(
		[this](vdfloat2& pt) -> bool {
			return !mpDisplay || mpDisplay->MapNormDestPtToSource(pt);
		},

		[this](vdfloat2& pt) -> bool {
			return !mpDisplay || mpDisplay->MapNormSourcePtToDest(pt);
		}
	);

	mIndicatorSafeAreaChangedFn = [this] { ResizeDisplay(); };
	g_sim.GetUIRenderer()->AddIndicatorSafeHeightChangedHandler(&mIndicatorSafeAreaChangedFn);

	ATUISetDragDropSubTarget((VDGUIHandle)mhwnd, &g_ATUIManager);
	return true;
}

void ATDisplayPane::OnDestroy() {
	ATUISetDragDropSubTarget(nullptr, nullptr);

	g_sim.GetUIRenderer()->RemoveIndicatorSafeHeightChangedHandler(&mIndicatorSafeAreaChangedFn);

	if (g_pATVideoDisplayWindow) {
		g_pATVideoDisplayWindow->SetOnAllowContextMenu(nullptr);
		g_pATVideoDisplayWindow->SetOnDisplayContextMenu(nullptr);
		g_pATVideoDisplayWindow->SetOnOSKChange(nullptr);
		g_pATVideoDisplayWindow->SetDisplaySourceMapping({}, {});
		g_pATVideoDisplayWindow->UnbindAllActions();
	}

	if (mpMenuBar) {
		mpMenuBar->Destroy();
		mpMenuBar.clear();
	}

	if (mpEnhTextEngine) {
		if (g_pATVideoDisplayWindow)
			g_pATVideoDisplayWindow->SetEnhancedTextEngine(NULL);

		mpEnhTextEngine->Shutdown();
		mpEnhTextEngine = NULL;
	}

	if (mpDisplay) {
		g_ATUINativeDisplay.SetDisplay(NULL, NULL);
		g_sim.GetGTIA().SetVideoOutput(NULL);

		mpDisplay->Destroy();
		mpDisplay = NULL;
		g_pDisplay = NULL;
		mhwndDisplay = NULL;
	}

	ATUIPane::OnDestroy();
}

void ATDisplayPane::OnMouseMove(WPARAM wParam, LPARAM lParam) {
	const int x = (short)LOWORD(lParam);
	const int y = (short)HIWORD(lParam);
	const bool hadMouse = mbHaveMouse;

	SetHaveMouse();

	// WM_SETCURSOR isn't set when mouse capture is enabled, in which case we must poll for a cursor
	// update.
	if (g_ATUINativeDisplay.IsMouseCaptured()) {
		const uint32 id = g_ATUIManager.GetCurrentCursorImageId();

		if (id)
			g_ATUINativeDisplay.SetCursorImageDirect(id);
	}

	if (g_ATUINativeDisplay.IsMouseMotionModeEnabled()) {
		int dx = x - mLastTrackMouseX;
		int dy = y - mLastTrackMouseY;

		if (dx | dy) {
			// If this is the first move message we've gotten since getting the mouse,
			// ignore the delta.
			if (hadMouse)
				g_ATUIManager.OnMouseRelativeMove(dx, dy);

			WarpCapturedMouse();
		}
	} else {
		TRACKMOUSEEVENT tme = {sizeof(TRACKMOUSEEVENT)};
		tme.dwFlags = TME_HOVER;
		tme.hwndTrack = mhwnd;
		tme.dwHoverTime = HOVER_DEFAULT;

		TrackMouseEvent(&tme);

		g_ATUIManager.OnMouseMove(x, y);
	}
}

void ATDisplayPane::OnMouseHover(WPARAM wParam) {
	if (!g_ATUINativeDisplay.IsMouseMotionModeEnabled()) {
		DWORD pos = ::GetMessagePos();

		int x = (short)LOWORD(pos);
		int y = (short)HIWORD(pos);

		POINT pt = { x, y };

		if (::ScreenToClient(mhwnd, &pt))
			g_ATUIManager.OnMouseHover(pt.x, pt.y);
	}
}

ATUIKeyEvent ATDisplayPane::ConvertVirtKeyEvent(WPARAM wParam, LPARAM lParam) {
	ATUIKeyEvent event;
	event.mVirtKey = LOWORD(wParam);
	event.mExtendedVirtKey = event.mVirtKey;
	event.mbIsRepeat = (HIWORD(lParam) & KF_REPEAT) != 0;
	event.mbIsExtendedKey = (HIWORD(lParam) & KF_EXTENDED) != 0;

	// Decode extended virt key.
	switch(event.mExtendedVirtKey) {
		case VK_RETURN:
			if (event.mbIsExtendedKey)
				event.mExtendedVirtKey = kATInputCode_KeyNumpadEnter;
			break;

		case VK_SHIFT:
			// Windows doesn't set the ext bit for RShift, so we have to use the scan
			// code instead.
			if (MapVirtualKey(LOBYTE(HIWORD(lParam)), 3) == VK_RSHIFT)
				event.mExtendedVirtKey = kATUIVK_RShift;
			else
				event.mExtendedVirtKey = kATUIVK_LShift;
			break;

		case VK_CONTROL:
			event.mExtendedVirtKey = event.mbIsExtendedKey ? kATUIVK_RControl : kATUIVK_LControl;
			break;

		case VK_MENU:
			event.mExtendedVirtKey = event.mbIsExtendedKey ? kATUIVK_RAlt : kATUIVK_LAlt;
			break;
	}

	return event;
}

void ATDisplayPane::ReleaseMouse() {
	if (g_pATVideoDisplayWindow)
		g_pATVideoDisplayWindow->ReleaseMouse();
}

void ATDisplayPane::ToggleCaptureMouse() {
	if (g_pATVideoDisplayWindow)
		g_pATVideoDisplayWindow->ToggleCaptureMouse();
}

void ATDisplayPane::ResetDisplay() {
	if (mpDisplay) {
		mpDisplay->Reset();
		mpDisplay->SetUse16Bit(g_ATOptions.mbDisplay16Bit);
	}
}

void ATDisplayPane::Copy(bool enableEscaping) {
	g_pATVideoDisplayWindow->Copy(enableEscaping);
}

void ATDisplayPane::CopyFrame(bool trueAspect) {
	g_pATVideoDisplayWindow->CopySaveFrame(false, trueAspect);
}

void ATDisplayPane::SaveFrame(bool trueAspect, const wchar_t *path) {
	g_pATVideoDisplayWindow->CopySaveFrame(true, trueAspect, path);
}

void ATDisplayPane::Paste() {
	OnCommandEditPasteText();
}

void ATDisplayPane::Paste(const wchar_t *s, size_t len) {
	if (mpEnhTextEngine)
		mpEnhTextEngine->Paste(s, len);
}

void ATDisplayPane::OnSize() {
	RECT r;
	GetClientRect(mhwnd, &r);

	if (g_ATUINativeDisplay.IsMouseConstrained()) {
		RECT rs = r;

		MapWindowPoints(mhwnd, NULL, (LPPOINT)&rs, 2);
		ClipCursor(&rs);
	}

	if (mhwndDisplay) {
		if (mpDisplay)
			ResizeDisplay();

		SetWindowPos(mhwndDisplay, NULL, 0, 0, r.right, r.bottom, SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE);
	}
}

void ATDisplayPane::UpdateFilterMode() {
	if (!mpDisplay)
		return;

	switch(g_dispFilterMode) {
		case kATDisplayFilterMode_Point:
			mpDisplay->SetFilterMode(IVDVideoDisplay::kFilterPoint);
			mpDisplay->SetPixelSharpness(1.0f, 1.0f);
			break;

		case kATDisplayFilterMode_Bilinear:
			mpDisplay->SetFilterMode(IVDVideoDisplay::kFilterBilinear);
			mpDisplay->SetPixelSharpness(1.0f, 1.0f);
			break;

		case kATDisplayFilterMode_SharpBilinear:
			mpDisplay->SetFilterMode(IVDVideoDisplay::kFilterBilinear);
			{
				ATGTIAEmulator& gtia = g_sim.GetGTIA();
				int pw = 2;
				int ph = 2;

				gtia.GetPixelAspectMultiple(pw, ph);

				static const float kFactors[5] = { 1.259f, 1.587f, 2.0f, 2.520f, 3.175f };

				const float factor = kFactors[std::max(0, std::min(4, g_dispFilterSharpness + 2))];

				const auto afmode = gtia.GetArtifactingMode();
				const bool isHighArtifacting = afmode == ATGTIAEmulator::kArtifactNTSCHi || afmode == ATGTIAEmulator::kArtifactPALHi || afmode == ATGTIAEmulator::kArtifactAutoHi;

				mpDisplay->SetPixelSharpness(isHighArtifacting ? 1.0f : std::max(1.0f, factor / (float)pw), std::max(1.0f, factor / (float)ph));
			}
			break;

		case kATDisplayFilterMode_Bicubic:
			mpDisplay->SetFilterMode(IVDVideoDisplay::kFilterBicubic);
			mpDisplay->SetPixelSharpness(1.0f, 1.0f);
			break;

		case kATDisplayFilterMode_AnySuitable:
			mpDisplay->SetFilterMode(IVDVideoDisplay::kFilterAnySuitable);
			mpDisplay->SetPixelSharpness(1.0f, 1.0f);
			break;
	}
}

void ATDisplayPane::SetHaveMouse() {
	if (!mbHaveMouse) {
		mbHaveMouse = true;

		g_ATUINativeDisplay.SetCursorImageChangesEnabled(true);

		TRACKMOUSEEVENT tme = {sizeof(TRACKMOUSEEVENT)};
		tme.dwFlags = TME_LEAVE;
		tme.hwndTrack = mhwnd;
		::TrackMouseEvent(&tme);
	}
}

void ATDisplayPane::WarpCapturedMouse() {
	const vdpoint32& pt = g_ATUINativeDisplay.WarpCapturedMouse();

	mLastTrackMouseX = pt.x;
	mLastTrackMouseY = pt.y;
}

void ATDisplayPane::UpdateTextDisplay(bool enabled) {
	g_ATUINativeDisplay.SetIgnoreAutoFlipping(enabled);

	if (!enabled) {
		if (mbTextModeEnabled) {
			mbTextModeEnabled = false;

			if (mpEnhTextEngine) {
				g_pATVideoDisplayWindow->SetEnhancedTextEngine(NULL);
				mpEnhTextEngine->Shutdown();
				mpEnhTextEngine = NULL;

				g_sim.GetGTIA().SetVideoOutput(mpDisplay);
				if (mpDisplay)
					mpDisplay->SetSourceSolidColor(0);
			}
		}

		return;
	}

	bool forceInvalidate = false;
	bool virtScreen = (g_sim.GetVirtualScreenHandler() != nullptr);

	if (!mbTextModeEnabled) {
		mbTextModeEnabled = true;
		mbTextModeVirtScreen = virtScreen;

		if (!mpEnhTextEngine) {
			mpEnhTextEngine = ATUICreateEnhancedTextEngine();
			mpEnhTextEngine->Init(g_pATVideoDisplayWindow, &g_sim);
		}

		g_sim.GetGTIA().SetVideoOutput(NULL);

		// This will also register the enhanced text engine with the video window.
		UpdateTextModeFont();

		forceInvalidate = true;
	} else if (mbTextModeVirtScreen != virtScreen) {
		mbTextModeVirtScreen = virtScreen;

		g_pATVideoDisplayWindow->UpdateEnhTextSize();

		forceInvalidate = true;
	}

	if (mpEnhTextEngine)
		mpEnhTextEngine->Update(forceInvalidate);

	if (forceInvalidate)
		g_pATVideoDisplayWindow->InvalidateTextOutput();
}

void ATDisplayPane::UpdateTextModeFont() {
	if (mpEnhTextEngine) {
		mpEnhTextEngine->SetFont(&g_enhancedTextFont);

		// Must be done after we have changed the font to reinitialize char dimensions based on char size.
		g_pATVideoDisplayWindow->SetEnhancedTextEngine(nullptr);
		g_pATVideoDisplayWindow->SetEnhancedTextEngine(mpEnhTextEngine);
	}
}

void ATDisplayPane::OnMenuActivated(ATUIMenuList *) {
	ATUIUpdateMenu();
	ATUpdatePortMenus();
}

void ATDisplayPane::OnMenuItemSelected(ATUIMenuList *sender, uint32 id) {
	// trampoline to top-level window
	::SendMessage(::GetAncestor(mhwnd, GA_ROOT), WM_COMMAND, id, 0);
}

void ATDisplayPane::OnAllowContextMenu() {
	mbAllowContextMenu = true;
}

void ATDisplayPane::OnDisplayContextMenu(const vdpoint32& pt) {
	HMENU hmenuPopup = GetSubMenu(mhmenuContext, 0);

	if (hmenuPopup) {
		POINT pt2 = { pt.x, pt.y };

		::ClientToScreen(mhwnd, &pt2);

		++mMouseCursorLock;
		EnableMenuItem(hmenuPopup, ID_DISPLAYCONTEXTMENU_COPY, IsTextSelected() ? MF_ENABLED : MF_DISABLED|MF_GRAYED);
		EnableMenuItem(hmenuPopup, ID_DISPLAYCONTEXTMENU_PASTE, IsClipboardFormatAvailable(CF_TEXT) ? MF_ENABLED : MF_DISABLED|MF_GRAYED);

		UINT cmd = (UINT)TrackPopupMenu(hmenuPopup, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_RETURNCMD, pt2.x, pt2.y, 0, GetAncestor(mhwnd, GA_ROOTOWNER), NULL);
		switch(cmd) {
			case ID_DISPLAYCONTEXTMENU_COPY:
				Copy(false);
				break;

			case ID_DISPLAYCONTEXTMENU_PASTE:
				Paste();
				break;
		}

		--mMouseCursorLock;
	}
}

void ATDisplayPane::OnOSKChange() {
	ResizeDisplay();
}

void ATDisplayPane::ResizeDisplay() {
	UpdateFilterMode();

	RECT r;
	GetClientRect(mhwnd, &r);

	vdrect32 rd(r.left, r.top, r.right, r.bottom);
	sint32 w = rd.width();
	sint32 h = rd.height();

	if (g_dispPadIndicators) {
		h -= g_sim.GetUIRenderer()->GetIndicatorSafeHeight();
	}

	if (g_pATVideoDisplayWindow) {
		vdrect32 rsafe = g_pATVideoDisplayWindow->GetOSKSafeArea();

		h = std::min<sint32>(h, rsafe.height());
	}

	int sw = 1;
	int sh = 1;
	g_sim.GetGTIA().GetFrameSize(sw, sh);

	if (w < 1)
		w = 1;

	if (h < 1)
		h = 1;

	if (g_displayStretchMode == kATDisplayStretchMode_PreserveAspectRatio || g_displayStretchMode == kATDisplayStretchMode_IntegralPreserveAspectRatio) {
		// NTSC-50 and PAL-60 are de-facto standards for when NTSC video needs to be played in
		// a PAL environment or vice versa. PAL-60, for instance, involves munging NTSC video
		// into a form good enough for a PAL decoder to accept. Therefore, we assume that the
		// video uses NTSC pixel aspect ratio, but is displayed with PAL colors.
		const bool pal = g_sim.GetVideoStandard() != kATVideoStandard_NTSC && g_sim.GetVideoStandard() != kATVideoStandard_PAL60;
		const float desiredAspect = (pal ? 1.03964f : 0.857141f);
		const float fsw = (float)sw * desiredAspect;
		const float fsh = (float)sh;
		const float fw = (float)w;
		const float fh = (float)h;
		float zoom = std::min<float>(fw / fsw, fh / fsh);

		if (g_displayStretchMode == kATDisplayStretchMode_IntegralPreserveAspectRatio && zoom > 1) {
			// We may have some small rounding errors, so give a teeny bit of leeway.
			zoom = floorf(zoom * 1.0001f);
		}

		sint32 w2 = (sint32)(0.5f + fsw * zoom);
		sint32 h2 = (sint32)(0.5f + fsh * zoom);

		rd.left		= (w - w2) >> 1;
		rd.right	= rd.left + w2;
		rd.top		= (h - h2) >> 1;
		rd.bottom	= rd.top + h2;
	} else if (g_displayStretchMode == kATDisplayStretchMode_SquarePixels || g_displayStretchMode == kATDisplayStretchMode_Integral) {
		int ratio = std::min<int>(w / sw, h / sh);

		if (ratio < 1 || g_displayStretchMode == kATDisplayStretchMode_SquarePixels) {
			if (w*sh < h*sw) {		// (w / sw) < (h / sh) -> w*sh < h*sw
				// width is smaller ratio -- compute restricted height
				int restrictedHeight = (sh * w + (sw >> 1)) / sw;

				rd.top = (h - restrictedHeight) >> 1;
				rd.bottom = rd.top + restrictedHeight;
			} else {
				// height is smaller ratio -- compute restricted width
				int restrictedWidth = (sw * h + (sh >> 1)) / sh;

				rd.left = (w - restrictedWidth) >> 1;
				rd.right = rd.left+ restrictedWidth;
			}
		} else {
			int finalWidth = sw * ratio;
			int finalHeight = sh * ratio;

			rd.left = (w - finalWidth) >> 1;
			rd.right = rd.left + finalWidth;

			rd.top = (h - finalHeight) >> 1;
			rd.bottom = rd.top + finalHeight;
		}
	}

	mDisplayRect = rd;
	mpDisplay->SetDestRect(&rd, 0);
	g_pATVideoDisplayWindow->SetDisplayRect(mDisplayRect);
}

///////////////////////////////////////////////////////////////////////////

void ATUIRegisterDisplayPane() {
	ATRegisterUIPaneType(kATUIPaneId_Display, VDRefCountObjectFactory<ATDisplayPane, ATUIPane>);
}
