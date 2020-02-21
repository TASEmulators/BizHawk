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

#ifndef f_AT_UIVIDEODISPLAY_H
#define f_AT_UIVIDEODISPLAY_H

#include <at/atcore/devicemanager.h>
#include <at/atui/uiwidget.h>
#include <at/atui/uicontainer.h>
#include "callback.h"
#include "simeventmanager.h"
#include "uienhancedtext.h"
#include "uikeyboard.h"

class IATUIEnhancedTextEngine;
class ATUILabel;
class ATUIOnScreenKeyboard;
class ATUISettingsWindow;
class ATXEP80Emulator;
class ATSimulatorEventManager;
class IATDeviceVideoOutput;

class ATUIVideoDisplayWindow final : public ATUIContainer, public IATDeviceChangeCallback, public IATUIEnhancedTextOutput {
public:
	enum {
		kActionOpenOSK = kActionCustom,
		kActionCloseOSK,
		kActionOpenSidePanel
	};

	ATUIVideoDisplayWindow();
	~ATUIVideoDisplayWindow();

	bool Init(ATSimulatorEventManager& sem, ATDeviceManager& devmgr);
	void Shutdown();

	void Copy(bool enableEscaping);
	void CopySaveFrame(bool saveFrame, bool trueAspect, const wchar_t *path = nullptr);

	void ToggleHoldKeys();

	void ToggleCaptureMouse();
	void ReleaseMouse();
	void CaptureMouse();

	void OpenOSK();
	void CloseOSK();

	void OpenSidePanel();
	void CloseSidePanel();

	void BeginEnhTextSizeIndicator();
	void EndEnhTextSizeIndicator();

	bool IsTextSelected() const { return !mDragPreviewSpans.empty(); }

	vdrect32 GetOSKSafeArea() const;

	void SetDisplaySourceMapping(vdfunction<bool(vdfloat2&)> dispToSrcFn, vdfunction<bool(vdfloat2&)> srcToDispFn);
	void SetDisplayRect(const vdrect32& r);

	void ClearHighlights();

	void SetXEP(IATDeviceVideoOutput *xep);
	void SetEnhancedTextEngine(IATUIEnhancedTextEngine *p);

	void SetOnAllowContextMenu(const vdfunction<void()>& fn) { mpOnAllowContextMenu = fn; }
	void SetOnDisplayContextMenu(const vdfunction<void(const vdpoint32&)>& fn) { mpOnDisplayContextMenu = fn; }
	void SetOnOSKChange(const vdfunction<void()>& fn) { mpOnOSKChange = fn; }

public:
	void InvalidateTextOutput() override;

private:
	void OnReset();
	void OnFrameTick();

public:
	virtual void OnDeviceAdded(uint32 iid, IATDevice *dev, void *iface) override;
	virtual void OnDeviceRemoving(uint32 iid, IATDevice *dev, void *iface) override;
	virtual void OnDeviceRemoved(uint32 iid, IATDevice *dev, void *iface) override;

protected:
	virtual ATUITouchMode GetTouchModeAtPoint(const vdpoint32& pt) const;
	virtual void OnMouseDown(sint32 x, sint32 y, uint32 vk, bool dblclk);
	virtual void OnMouseUp(sint32 x, sint32 y, uint32 vk);
	virtual void OnMouseRelativeMove(sint32 dx, sint32 dy);
	virtual void OnMouseMove(sint32 x, sint32 y);
	virtual void OnMouseLeave();
	virtual void OnMouseHover(sint32 x, sint32 y);

	virtual bool OnContextMenu(const vdpoint32 *pt);

	virtual bool OnKeyDown(const ATUIKeyEvent& event);
	virtual bool OnKeyUp(const ATUIKeyEvent& event);
	virtual bool OnChar(const ATUICharEvent& event);
	virtual bool OnCharUp(const ATUICharEvent& event);

	virtual void OnForceKeysUp();

	virtual void OnActionStart(uint32 id);
	virtual void OnActionStop(uint32 id);

	virtual void OnCreate();
	virtual void OnDestroy();
	virtual void OnSize();

	virtual void OnSetFocus();
	virtual void OnKillFocus();
	virtual void OnCaptureLost();
	
	virtual void OnDeactivate() override;

	ATUIDragEffect OnDragEnter(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) override;
	ATUIDragEffect OnDragOver(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) override;
	void OnDragLeave() override;
	ATUIDragEffect OnDragDrop(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) override;

	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

public:
	void UpdateEnhTextSize();
	void UpdateAltDisplay();

protected:
	bool ProcessKeyDown(const ATUIKeyEvent& event, bool enableKeyInput);
	bool ProcessKeyUp(const ATUIKeyEvent& event, bool enableKeyInput);
	void ProcessVirtKey(uint32 vkey, uint32 scancode, uint32 keycode, bool repeat);
	void ProcessSpecialKey(uint32 scanCode, bool state);
	void ToggleHeldKey(uint8 keycode);
	void ToggleHeldConsoleButton(uint8 encoding);
	void UpdateCtrlShiftState();

	uint32 ComputeCursorImage(const vdpoint32& pt) const;
	void UpdateMousePosition(int x, int y);
	const vdrect32 GetAltDisplayArea() const;
	bool MapPixelToBeamPosition(int x, int y, float& hcyc, float& vcyc, bool clamp) const;
	bool MapPixelToBeamPosition(int x, int y, int& xc, int& yc, bool clamp) const;
	void MapBeamPositionToPoint(int xc, int yc, int& x, int& y) const;
	vdfloat2 MapBeamPositionToPointF(vdfloat2 pt) const;
	void UpdateDragPreview(int x, int y);
	void UpdateDragPreviewAlt(int x, int y);
	void UpdateDragPreviewAntic(int x, int y);
	void UpdateDragPreviewRects();
	void ClearDragPreview();
	int GetModeLineYPos(int ys, bool checkValidCopyText) const;
	std::pair<int, int> GetModeLineXYPos(int xcc, int ys, bool checkValidCopyText) const;
	int ReadText(uint8 *dst, int yc, int startChar, int numChars) const;
	void ClearCoordinateIndicator();
	void SetCoordinateIndicator(int x, int y);

	void ClearHoverTip();

	sint32 FindDropTargetOverlay(sint32 x, sint32 y) const;
	void HighlightDropTargetOverlay(int index);
	void CreateDropTargetOverlays();
	void DestroyDropTargetOverlays();

	bool mbShiftDepressed = false;
	bool mbShiftToggledPostKeyDown = false;
	bool mbHoldKeys = false;

	vdrect32 mDisplayRect = { 0, 0, 0, 0 };

	vdfunction<bool(vdfloat2&)> mpMapDisplayToSourcePt;
	vdfunction<bool(vdfloat2&)> mpMapSourceToDisplayPt;

	bool	mbDragActive = false;
	bool	mbDragInitial = false;
	uint32	mDragStartTime = 0;
	int		mDragAnchorX = 0;
	int		mDragAnchorY = 0;

	bool	mbMouseHidden = false;
	int		mMouseHideX = 0;
	int		mMouseHideY = 0;

	bool	mbOpenSidePanelDeferred = false;

	struct HighlightPoint {
		int mX;
		int mY;
		bool mbClose;
	};

	typedef vdfastvector<HighlightPoint> HighlightPoints;
	HighlightPoints mHighlightPoints;

	bool	mbShowEnhSizeIndicator = false;
	bool	mbCoordIndicatorActive = false;
	bool	mbCoordIndicatorEnabled = false;
	vdrect32	mHoverTipArea = { 0, 0, 0, 0 };
	bool		mbHoverTipActive = false;

	struct TextSpan {
		int mX;
		int mY;
		int mWidth;
		int mHeight;
		int mCharX;
		int mCharWidth;
	};

	typedef vdfastvector<TextSpan> TextSpans;
	TextSpans mDragPreviewSpans;

	IATUIEnhancedTextEngine *mpEnhTextEngine = nullptr;
	ATUIOnScreenKeyboard *mpOSK = nullptr;
	ATUIContainer *mpOSKPanel = nullptr;
	ATUISettingsWindow *mpSidePanel = nullptr;

	ATSimulatorEventManager *mpSEM = nullptr;
	uint32 mEventCallbackIdWarmReset = 0;
	uint32 mEventCallbackIdColdReset = 0;
	uint32 mEventCallbackIdFrameTick = 0;

	ATDeviceManager *mpDevMgr = nullptr;
	IATDeviceVideoOutput *mpXEP = nullptr;
	uint32 mXEPDataReceivedCount = 0;

	IATDeviceVideoOutput *mpAltVideoOutput = nullptr;
	VDDisplayImageView mAltVOImageView;
	uint32 mAltVOChangeCount = 0;
	uint32 mAltVOLayoutChangeCount = 0;

	ATUILabel *mpUILabelBadSignal = nullptr;
	ATUILabel *mpUILabelEnhTextSize = nullptr;

	vdfunction<void()> mpOnAllowContextMenu;
	vdfunction<void(const vdpoint32&)> mpOnDisplayContextMenu;
	vdfunction<void()> mpOnOSKChange;

	vdrefptr<ATUIWidget> mpDropTargetOverlays[7];

	struct ActiveKey {
		uint32 mVkey;
		uint32 mNativeScanCode;
		uint8 mScanCode;
	};

	vdfastvector<ActiveKey> mActiveKeys;
	uint32 mActiveSpecialVKeys[kATUIKeyScanCodeLast + 1 - kATUIKeyScanCodeFirst] = {};
	uint32 mActiveSpecialScanCodes[kATUIKeyScanCodeLast + 1 - kATUIKeyScanCodeFirst] = {};
};

#endif
