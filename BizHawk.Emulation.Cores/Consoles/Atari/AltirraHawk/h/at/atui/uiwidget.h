//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT2_UIWIDGET_H
#define f_AT2_UIWIDGET_H

#include <vd2/system/vectors.h>
#include <vd2/VDDisplay/rendercache.h>
#include <at/atui/constants.h>

class IVDDisplayRenderer;
class VDDisplayTextRenderer;
class IATUIAnchor;
class IATUIDragDropObject;

class ATUIContainer;
class ATUIManager;

enum class ATUIDragModifiers : uint32;
enum class ATUIDragEffect : uint32;

// Currently these need to match the Win32 definitions.
enum ATUIVirtKey {
	kATUIVK_LButton	= 0x01,
	kATUIVK_RButton	= 0x02,
	kATUIVK_Cancel	= 0x03,
	kATUIVK_MButton	= 0x04,
	kATUIVK_XButton1= 0x05,
	kATUIVK_XButton2= 0x06,
	kATUIVK_Back	= 0x08,		// Backspace
	kATUIVK_Tab		= 0x09,		// VK_TAB
	kATUIVK_Return	= 0x0D,
	kATUIVK_Shift	= 0x10,
	kATUIVK_Control	= 0x11,
	kATUIVK_Alt		= 0x12,
	kATUIVK_Pause	= 0x13,
	kATUIVK_CapsLock= 0x14,
	kATUIVK_Escape	= 0x1B,		// VK_ESCAPE
	kATUIVK_Space	= 0x20,		// VK_SPACE
	kATUIVK_Prior	= 0x21,		// VK_PRIOR
	kATUIVK_Next	= 0x22,		// VK_NEXT
	kATUIVK_0		= 0x30,
	kATUIVK_A		= 0x41,
	kATUIVK_End		= 0x23,
	kATUIVK_Home	= 0x24,
	kATUIVK_Left	= 0x25,		// Left arrow
	kATUIVK_Up		= 0x26,		// Up arrow
	kATUIVK_Right	= 0x27,		// Right arrow
	kATUIVK_Down	= 0x28,		// Down arrow
	kATUIVK_Delete	= 0x2E,
	kATUIVK_F1		= 0x70,		// VK_F1
	kATUIVK_F2		= 0x71,		// VK_F2
	kATUIVK_F3		= 0x72,		// VK_F3
	kATUIVK_F4		= 0x73,		// VK_F4
	kATUIVK_F5		= 0x74,		// VK_F5
	kATUIVK_F6		= 0x75,		// VK_F6
	kATUIVK_F7		= 0x76,		// VK_F7
	kATUIVK_F8		= 0x77,		// VK_F8
	kATUIVK_F9		= 0x78,		// VK_F9
	kATUIVK_F10		= 0x79,		// VK_F10
	kATUIVK_F11		= 0x7A,		// VK_F11
	kATUIVK_F12		= 0x7B,		// VK_F12
	kATUIVK_LShift	= 0xA0,		// VK_LSHIFT
	kATUIVK_RShift	= 0xA1,		// VK_RSHIFT
	kATUIVK_LControl= 0xA2,		// VK_LCONTROL
	kATUIVK_RControl= 0xA3,		// VK_RCONTROL
	kATUIVK_LAlt	= 0xA4,		// VK_LMENU
	kATUIVK_RAlt	= 0xA5,		// VK_RMENU
	kATUIVK_NumpadEnter	= 0x10D,	// VK_RETURN (extended)

	kATUIVK_UILeft		= 0x0500,
	kATUIVK_UIRight		= 0x0501,
	kATUIVK_UIUp		= 0x0502,
	kATUIVK_UIDown		= 0x0503,
	kATUIVK_UIAccept	= 0x0504,	// PSx[X], Xbox[A]
	kATUIVK_UIReject	= 0x0505,	// PSx[O], Xbox[B]
	kATUIVK_UIMenu		= 0x0506,	// PSx[T], Xbox[Y]
	kATUIVK_UIOption	= 0x0507,	// PSx[S], Xbox[X]
	kATUIVK_UISwitchLeft	= 0x0508,
	kATUIVK_UISwitchRight	= 0x0509,
	kATUIVK_UILeftShift		= 0x050A,
	kATUIVK_UIRightShift	= 0x050B,
};

enum ATUIDockMode {
	kATUIDockMode_None,
	kATUIDockMode_Left,
	kATUIDockMode_Right,
	kATUIDockMode_Top,
	kATUIDockMode_Bottom,
	kATUIDockMode_LeftFloat,
	kATUIDockMode_RightFloat,
	kATUIDockMode_TopFloat,
	kATUIDockMode_BottomFloat,
	kATUIDockMode_Fill
};

enum ATUIFrameMode {
	kATUIFrameMode_None,
	kATUIFrameMode_Raised,
	kATUIFrameMode_Sunken,
	kATUIFrameMode_SunkenThin,
	kATUIFrameMode_RaisedEdge,
	kATUIFrameModeCount
};

struct ATUIKeyEvent {
	uint32 mVirtKey;
	uint32 mExtendedVirtKey;
	bool mbIsRepeat;
	bool mbIsExtendedKey;
};

struct ATUICharEvent {
	uint32 mCh;
	uint32 mScanCode;
	bool mbIsRepeat;
};

struct ATUITriggerBinding {
	enum {
		kModNone = 0,
		kModCtrl = 1,
		kModShift = 2,
		kModAlt = 4,
		kModAll = 7
	};

	unsigned	mVk : 12;
	unsigned	mAction : 12;
	unsigned	mModVal : 3;
	unsigned	mModMask : 3;
	unsigned	: 2;
	uint32	mTargetInstanceId;
};

class ATUIWidget : public vdrefcount {
public:
	enum {
		kActionFocus = 1,
		kActionCustom = 16
	};

	ATUIWidget();
	~ATUIWidget();

	void Destroy();
	void Focus();

	uint32 GetInstanceId() const { return mInstanceId; }
	void SetInstanceId(uint32 id) { mInstanceId = id; }

	ATUIWidget *GetOwner() const;
	void SetOwner(ATUIWidget *w);
	ATUIWidget *GetParentOrOwner() const;

	bool IsActivated() const { return mbActivated; }

	bool HasFocus() const;
	bool HasCursor() const;
	bool IsCursorCaptured() const;
	void CaptureCursor(bool motionMode = false, bool constrained = false);
	void ReleaseCursor();

	void SetFillColor(uint32 color);
	void SetAlphaFillColor(uint32 color);
	void SetFrameMode(ATUIFrameMode frameMode);

	uint32 GetCursorImage() const { return mCursorImage; }
	void SetCursorImage(uint32 id);

	const vdrect32& GetArea() const { return mArea; }
	vdrect32 GetClientArea() const;
	void SetPosition(const vdpoint32& pt);
	void SetSize(const vdsize32& sz);
	void SetArea(const vdrect32& r);

	virtual void UpdateLayout() {}

	vdrect32 ComputeWindowSize(const vdrect32& clientArea) const;

	ATUIDockMode GetDockMode() const { return mDockMode; }
	void SetDockMode(ATUIDockMode mode);

	IATUIAnchor *GetAnchor() const { return mpAnchor; }
	void SetAnchor(IATUIAnchor *anchor);

	// Control visibility.
	//
	// An invisible window neither draws nor responds to input. Visibility is inherited
	// and an invisible parent window will hide all of its children.

	bool IsVisible() const { return mbVisible; }
	void SetVisible(bool visible);

	// Hit transparency.
	//
	// A window that is hit transparent does not respond to the cursor and passes mouse
	// input through to the next sibling. However, it still renders. Hit transparency
	// is _not_ inherited by children, which can receive input regardless of the parent.

	bool IsHitTransparent() const { return mbHitTransparent; }
	void SetHitTransparent(bool trans) { mbHitTransparent = trans; }

	bool IsDropTarget() const { return mbDropTarget; }
	void SetDropTarget(bool enabled) { mbDropTarget = enabled; }

	ATUITouchMode GetTouchMode() const { return mTouchMode; }
	void SetTouchMode(ATUITouchMode mode) { mTouchMode = mode; }

	ATUIManager *GetManager() const { return mpManager; }
	ATUIContainer *GetParent() const { return mpParent; }

	bool IsSameOrAncestorOf(ATUIWidget *w) const;

	virtual ATUIWidget *HitTest(vdpoint32 pt);

	// Gets/sets the (x,y) point in the client coordinate system that corresponds to the
	// upper-left corner of the client rect.
	vdpoint32 GetClientOrigin() const { return mClientOrigin; }
	void SetClientOrigin(vdpoint32 pt);

	bool TranslateScreenPtToClientPt(vdpoint32 spt, vdpoint32& cpt);
	bool TranslateWindowPtToClientPt(vdpoint32 wpt, vdpoint32& cpt);
	vdpoint32 TranslateClientPtToScreenPt(vdpoint32 cpt);

	void UnbindAction(uint32 vk, uint32 mod);
	void UnbindAllActions();
	void BindAction(const ATUITriggerBinding& binding);
	void BindAction(uint32 vk, uint32 action, uint32 mod = 0, uint32 instanceid = 0);
	const ATUITriggerBinding *FindAction(uint32 vk, uint32 extvk, uint32 mods) const;

	virtual ATUITouchMode GetTouchModeAtPoint(const vdpoint32& pt) const;
	virtual void OnMouseRelativeMove(sint32 x, sint32 y);
	virtual void OnMouseMove(sint32 x, sint32 y);
	virtual void OnMouseDownL(sint32 x, sint32 y);
	virtual void OnMouseDblClkL(sint32 x, sint32 y);
	virtual void OnMouseUpL(sint32 x, sint32 y);
	virtual void OnMouseDown(sint32 x, sint32 y, uint32 vk, bool dblclk);
	virtual void OnMouseUp(sint32 x, sint32 y, uint32 vk);
	virtual bool OnMouseWheel(sint32 x, sint32 y, float delta);
	virtual void OnMouseLeave();
	virtual void OnMouseHover(sint32 x, sint32 y);

	virtual bool OnContextMenu(const vdpoint32 *pt);

	virtual bool OnKeyDown(const ATUIKeyEvent& event);
	virtual bool OnKeyUp(const ATUIKeyEvent& event);
	virtual bool OnChar(const ATUICharEvent& event);
	virtual bool OnCharUp(const ATUICharEvent& event);

	virtual void OnForceKeysUp();

	virtual void OnActionStart(uint32 trid);
	virtual void OnActionRepeat(uint32 trid);
	virtual void OnActionStop(uint32 trid);

	virtual void OnCreate();
	virtual void OnDestroy();
	virtual void OnSize();

	virtual void OnKillFocus();
	virtual void OnSetFocus();

	virtual void OnCaptureLost();

	virtual void OnActivate();
	virtual void OnDeactivate();

	virtual void OnTrackCursorChanges(ATUIWidget *w);

	// Drag and drop support
	//
	// - DragHitTest() returns the widget or sub-widget that is a valid drop
	//   target at the given point. By default, this is the deepest visible
	//   widget with the drop target flag set. Regular hit testing including
	//   hit transparency is ignored, however.
	// - One drop target has focus at a time, bracketed by OnDragEnter() and
	//   OnDragLeave(). A leave-enter sequence is guaranteed to occur if
	//   the drag object changes.
	// - OnDragOver() is called during movements over the drag target. It is
	//   the drag analog of mouse move. It always occurs between enter-leave.
	// - OnDragDrop() is called if the drop actually occurs. It too is called
	//   between enter-leave, so OnDragLeave() is guaranteed to be available
	//   for common cleanup after a drop.
	// - All coordinates are local (client).
	// - The drag effect determines the cursor and the type of intended
	//   operation. Copy is recommended, as some drag sources block drags in
	//   Move mode if their sources are read-only.
	//
	virtual ATUIWidget *DragHitTest(vdpoint32 pt);
	virtual ATUIDragEffect OnDragEnter(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj);
	virtual ATUIDragEffect OnDragOver(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj);
	virtual void OnDragLeave();
	virtual ATUIDragEffect OnDragDrop(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj);

	void Draw(IVDDisplayRenderer& rdr);

public:
	void SetParent(ATUIManager *mgr, ATUIContainer *parent);
	void SetActivated(bool activated);
	void OnPointerEnter(uint8 bit);
	void OnPointerLeave(uint8 bit);
	void OnPointerClear();

protected:
	void Invalidate();

	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) = 0;
	void RecomputeClientArea();

	ATUIManager *mpManager;
	ATUIContainer *mpParent;
	vdrect32 mArea;
	vdrect32 mClientArea;
	vdpoint32 mClientOrigin;
	uint32 mFillColor;
	uint32 mCursorImage;
	ATUIDockMode mDockMode;
	ATUIFrameMode mFrameMode;
	ATUITouchMode mTouchMode;
	IATUIAnchor *mpAnchor;
	uint32 mInstanceId;
	uint32 mOwnerId;

	bool mbActivated;
	bool mbVisible;
	bool mbDropTarget;
	bool mbFastClip;
	bool mbHitTransparent;
	uint8 mPointersOwned;

	typedef vdfastvector<ATUITriggerBinding> ActionMap;
	ActionMap mActionMap;

	VDDisplaySubRenderCache mRenderCache;
};

#endif
