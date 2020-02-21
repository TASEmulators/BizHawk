//	Altirra - Atari 800/800XL/5200 emulator
//	UI library
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

#ifndef f_AT_ATUI_DIALOG_H
#define f_AT_ATUI_DIALOG_H

#ifdef _MSC_VER
#pragma once
#endif

#include <vd2/system/function.h>
#include <vd2/system/thread.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vectors.h>
#include <vd2/system/win32/miniwindows.h>
#include <at/atnativeui/nativewindowproxy.h>
#include <at/atnativeui/uiproxies.h>
#include <list>

class MyError;
enum ATUICursorImage : uint32;

class IVDUIDropFileList {
public:
	virtual bool GetFileName(int index, VDStringW& fileName) = 0;
};

#define VDWM_APP_POSTEDCALL (WM_APP + 0x400)

#ifndef IDOK
#define IDOK                1
#endif

#ifndef IDCANCEL
#define IDCANCEL            2
#endif

class VDDialogResizerW32 {
public:
	VDDialogResizerW32();
	~VDDialogResizerW32();

	enum : uint32 {
		kAnchorX1_C	= 0x01,
		kAnchorX1_R	= 0x02,
		kAnchorX2_C	= 0x04,
		kAnchorX2_R	= 0x08,
		kAnchorY1_M	= 0x10,
		kAnchorY1_B	= 0x20,
		kAnchorY2_M	= 0x40,
		kAnchorY2_B	= 0x80,

		kL		= 0,
		kC		= kAnchorX2_R,
		kR		= kAnchorX2_R | kAnchorX1_R,
		kHMask	= 0x0F,

		kT		= 0,
		kM		= kAnchorY2_B,
		kB		= kAnchorY2_B | kAnchorY1_B,
		kVMask	= 0xF0,

		kX1Y1Mask = 0x33,
		kX2Y2Mask = 0xCC,

		kTL		= kT | kL,
		kTR		= kT | kR,
		kTC		= kT | kC,
		kML		= kM | kL,
		kMR		= kM | kR,
		kMC		= kM | kC,
		kBL		= kB | kL,
		kBR		= kB | kR,
		kBC		= kB | kC,

		kAvoidFlicker = 0x100,
		kSuppressFontChange = 0x200,
		kUpDownAutoBuddy = 0x400
	};

	void Init(VDZHWND hwnd);
	void Shutdown();
	void SetRefUnits(int refX, int refY);
	void Relayout(const int *newRefX = nullptr, const int *newRefY = nullptr);
	void Relayout(int width, int height, const int *newRefX = nullptr, const int *newRefY = nullptr);
	void Add(uint32 id, uint32 alignment);
	void Add(VDZHWND hwndControl, uint32 alignment);
	void Add(VDZHWND hwnd, sint32 x, sint32 y, sint32 w, sint32 h, uint32 alignment);
	void AddAlias(VDZHWND hwndTarget, VDZHWND hwndSource, uint32 mergeFlags);
	void Remove(VDZHWND hwnd);

	void Broadcast(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);

	void Erase(const VDZHDC *phdc, sint32 bgColorOverride = -1);

private:
	struct Anchors {
		int mXAnchors[4];
		int mYAnchors[4];
	};

	struct ControlEntry {
		VDZHWND	mhwnd;
		uint32	mAlignment;
		sint32	mX1;
		sint32	mY1;
		sint32	mX2;
		sint32	mY2;
		sint32	mRefX;
		sint32	mRefY;
	};

	Anchors ComputeAnchors() const;
	void ComputeLayout(const ControlEntry& ce, const Anchors& anchors, int& x1, int& y1, int& w, int& h, uint32& flags, bool forceMove) const;

	VDZHWND	mhwndBase;
	int		mWidth;
	int		mHeight;
	int		mRefX;
	int		mRefY;

	typedef vdfastvector<ControlEntry> Controls;
	Controls mControls;
};

class VDDialogFrameW32 : public ATUINativeWindowProxy {
public:
	virtual ~VDDialogFrameW32() = default;

	bool IsCreated() const { return mhdlg != NULL; }

	bool	Create(VDGUIHandle hwndParent);
	bool	Create(VDDialogFrameW32 *parent);

	void Sync(bool writeToDataStore);

	using ATUINativeWindowProxy::SetArea;
	using ATUINativeWindowProxy::SetCaption;
	using ATUINativeWindowProxy::SetSize;

	void SetSize(const vdsize32& sz, bool repositionSafe);
	void SetArea(const vdrect32& r, bool repositionSafe);

	VDZHFONT GetFont() const;
	void SetFont(VDZHFONT hfont);

	void AdjustPosition();
	void CenterOnParent();
	void UpdateChildDpi();

	sintptr ShowDialog(VDGUIHandle hwndParent);
	sintptr ShowDialog(VDDialogFrameW32 *parent);

	static void ShowInfo(VDGUIHandle hParent, const wchar_t *message, const wchar_t *caption);
	static void SetDefaultCaption(const wchar_t *caption);

	void ShowInfo(const wchar_t *message, const wchar_t *caption = nullptr);
	void ShowInfo2(const wchar_t *message, const wchar_t *title = nullptr);
	void ShowWarning(const wchar_t *message, const wchar_t *caption = nullptr);
	void ShowError(const wchar_t *message, const wchar_t *caption = nullptr);
	void ShowError(const MyError&);
	void ShowError2(const wchar_t *message, const wchar_t *title = nullptr);
	bool Confirm(const wchar_t *message, const wchar_t *caption = nullptr);
	bool Confirm2(const char *ignoreTag, const wchar_t *message, const wchar_t *title = nullptr);

protected:
	VDDialogFrameW32(uint32 dlgid);

	void End(sintptr result);

	void AddProxy(VDUIProxyControl *proxy, uint32 id);
	void AddProxy(VDUIProxyControl *proxy, VDZHWND hwnd);

	void SetCurrentSizeAsMinSize();
	void SetCurrentSizeAsMaxSize(bool width, bool height);

	VDZHWND GetControl(uint32 id);

	VDZHWND GetFocusedWindow() const;
	void SetFocusToControl(uint32 id);
	void EnableControl(uint32 id, bool enabled);
	void ShowControl(uint32 id, bool visible);
	void ApplyFontToControl(uint32 id);

	vdrect32 GetControlPos(uint32 id);
	vdrect32 GetControlScreenPos(uint32 id);
	void SetControlPos(uint32 id, const vdrect32& r);

	void SetCaption(uint32 id, const wchar_t *format);

	bool GetControlText(uint32 id, VDStringW& s);
	void SetControlText(uint32 id, const wchar_t *s);
	void SetControlTextF(uint32 id, const wchar_t *format, ...);

	sint32 GetControlValueSint32(uint32 id);
	uint32 GetControlValueUint32(uint32 id);
	double GetControlValueDouble(uint32 id);
	VDStringW GetControlValueString(uint32 id);

	void ExchangeControlValueBoolCheckbox(bool write, uint32 id, bool& val);
	void ExchangeControlValueSint32(bool write, uint32 id, sint32& val, sint32 minVal, sint32 maxVal);
	void ExchangeControlValueUint32(bool write, uint32 id, uint32& val, uint32 minVal, uint32 maxVal);
	void ExchangeControlValueDouble(bool write, uint32 id, const wchar_t *format, double& val, double minVal, double maxVal);
	void ExchangeControlValueString(bool write, uint32 id, VDStringW& s);

	void CheckButton(uint32 id, bool checked);
	bool IsButtonChecked(uint32 id) const;

	int GetButtonTriState(uint32 id);
	void SetButtonTriState(uint32 id, int state);

	void BeginValidation();
	bool EndValidation();

	void FailValidation(uint32 id);
	void FailValidation(uint32 id, const wchar_t *msg);
	void SignalFailedValidation(uint32 id);

	void SetPeriodicTimer(uint32 id, uint32 msperiod);

	int ActivateMenuButton(uint32 id, const wchar_t *const *items);
	int ActivatePopupMenu(int x, int y, const wchar_t *const *items);

	// listbox
	void LBClear(uint32 id);
	sint32 LBGetSelectedIndex(uint32 id);
	void LBSetSelectedIndex(uint32 id, sint32 idx);
	void LBAddString(uint32 id, const wchar_t *s);
	void LBAddStringF(uint32 id, const wchar_t *format, ...);

	// combobox
	void CBClear(uint32 id);
	sint32 CBGetSelectedIndex(uint32 id);
	void CBSetSelectedIndex(uint32 id, sint32 idx);
	void CBAddString(uint32 id, const wchar_t *s);

	// trackbar
	sint32 TBGetValue(uint32 id);
	void TBSetValue(uint32 id, sint32 value);
	void TBSetRange(uint32 id, sint32 minval, sint32 maxval);
	void TBSetPageStep(uint32 id, sint32 pageStep);

	// up/down controls
	void UDSetRange(uint32 id, sint32 minval, sint32 maxval);

	bool PostCall(const vdfunction<void()>& call);
	bool PostCall(vdfunction<void()>&& call);

protected:
	virtual VDZINT_PTR DlgProc(VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);
	virtual void OnDataExchange(bool write);
	virtual void OnPreLoaded();
	virtual bool OnLoaded();
	virtual bool OnOK();
	virtual bool OnCancel();
	virtual void OnSize();
	virtual bool OnClose();
	virtual void OnDestroy();
	virtual void OnEnable(bool enable);
	virtual bool OnTimer(uint32 id);
	virtual bool OnErase(VDZHDC hdc);
	virtual bool OnPaint();
	virtual bool OnCommand(uint32 id, uint32 extcode);
	virtual void OnDropFiles(VDZHDROP hDrop);
	virtual void OnDropFiles(IVDUIDropFileList *dropFileList);
	virtual void OnHScroll(uint32 id, int code);
	virtual void OnVScroll(uint32 id, int code);
	virtual void OnMouseMove(int x, int y);
	virtual void OnMouseDownL(int x, int y);
	virtual void OnMouseUpL(int x, int y);
	virtual void OnMouseWheel(int x, int y, sint32 delta);
	virtual void OnMouseLeave();
	virtual bool OnSetCursor(ATUICursorImage& image);
	virtual void OnCaptureLost();
	virtual void OnHelp();
	virtual void OnInitMenu(VDZHMENU hmenu);
	virtual void OnContextMenu(uint32 id, int x, int y);
	virtual void OnSetFont(VDZHFONT hfont);
	virtual void OnDpiChanging(uint16 newDpiX, uint16 newDpiY, const vdrect32 *suggestedRect);
	virtual void OnDpiChanged();
	virtual bool PreNCDestroy();
	virtual bool ShouldSetDialogIcon() const;
	virtual sint32 GetBackgroundColor() const;

	void SetCapture();
	void ReleaseCapture();
	void RegisterForMouseLeave();
	void LoadAcceleratorTable(uint32 id);
	sint32 GetDpiScaledMetric(int index);

	bool	mbValidationFailed;
	bool	mbIsModal;
	VDZHFONT	mhfont;
	int		mMinWidth;
	int		mMinHeight;
	uint32	mCurrentDpi;

	struct DialogUnits {
		uint32 mWidth4;
		uint32 mHeight8;
	} mDialogUnits;

	int		mMaxWidth;
	int		mMaxHeight;
	VDZHACCEL	mAccel = nullptr;

private:
	void ExecutePostedCalls();
	void SetDialogIcon();
	VDZHFONT CreateNewFont(int dpiOverride = 0) const;
	DialogUnits ComputeDialogUnits(VDZHFONT hFont) const;
	void RecomputeDialogUnits();
	vdsize32 ComputeTemplatePixelSize(const DialogUnits& dialogUnits, uint32 dpi) const;
	void AdjustSize(int& width, int& height, const vdsize32& templatePixelSize, const DialogUnits& dialogUnits) const;
	sintptr DoCreate(VDZHWND parent, bool modal);

	static VDZINT_PTR VDZCALLBACK StaticDlgProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);

	const wchar_t *mpDialogResourceName;
	uint32	mFailedId;
	VDStringW mFailedMsg;

	VDGUIHandle mhPrevProgressParent = nullptr;
	bool mbProgressParentHooked = false;

	uint32	mTemplateWidthDLUs;
	uint32	mTemplateHeightDLUs;
	uint32	mTemplateControlCount;
	const char *mpTemplateControls;
	const wchar_t *mpTemplateFont;
	uint32	mTemplateFontPointSize;

	bool mbResizableWidth;
	bool mbResizableHeight;

	VDCriticalSection mMutex;
	std::list<vdfunction<void()>> mPostedCalls;

	static const wchar_t *spDefaultCaption;

protected:
	VDUIProxyMessageDispatcherW32 mMsgDispatcher;
	VDDialogResizerW32 mResizer;
};

class VDResizableDialogFrameW32 : public VDDialogFrameW32 {
protected:
	VDResizableDialogFrameW32(uint32 dlgid);

	void OnPreLoaded() override;
};

#endif
