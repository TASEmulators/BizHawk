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

#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/time.h>
#include <vd2/system/vdalloc.h>
#include <vd2/Kasumi/triblt.h>
#include <vd2/VDDisplay/font.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <vd2/VDDisplay/renderersoft.h>
#include <at/atui/uicontainer.h>
#include <at/atui/uidragdrop.h>
#include <at/atui/uimanager.h>

///////////////////////////////////////////////////////////////////////////

class ATUIManager::ActiveAction final : public IVDTimerCallback {
public:
	virtual void TimerCallback();

public:
	ATUIManager *mpParent;
	uint32 mTargetInstance;
	uint32 mActionId;
	uint32 mRepeatDelay;

	VDLazyTimer mRepeatTimer;
};

void ATUIManager::ActiveAction::TimerCallback() {
	mpParent->RepeatAction(*this);

	mRepeatTimer.SetOneShot(this, mRepeatDelay);

	if (mRepeatDelay > 20)
		mRepeatDelay = mRepeatDelay - 10;
}

///////////////////////////////////////////////////////////////////////////

ATUIManager::ATUIManager()
	: mpNativeDisplay(NULL)
	, mpMainWindow(NULL)
	, mpCursorWindow(NULL)
	, mpActiveWindow(NULL)
	, mpModalWindow(NULL)
	, mpModalCookie(NULL)
	, mbCursorCaptured(false)
	, mbCursorMotionMode(false)
	, mCursorImageId(kATUICursorImage_None)
	, mbForeground(true)
	, mbInvalidated(false)
	, mThemeScale(2.0f)
	, mDestroyLocks(0)
	, mNextInstanceId(0)
	, mSystemMetrics()
{
	for(size_t i=0; i<vdcountof(mpStockImages); ++i)
		mpStockImages[i] = NULL;
}

ATUIManager::~ATUIManager() {
	Shutdown();
}

void ATUIManager::Init(IATUINativeDisplay *natDisp) {
	mpNativeDisplay = natDisp;

	mpMainWindow = new ATUIContainer;
	mpMainWindow->AddRef();
	mpMainWindow->SetHitTransparent(true);
	mpMainWindow->SetParent(this, NULL);

	ReinitTheme();
}

void ATUIManager::Shutdown() {
	VDASSERT(!mDestroyLocks);

	vdsaferelease <<= mpDropObject;
	vdsafedelete <<= mpStockImages;

	if (mpMainWindow) {
		mpMainWindow->SetParent(NULL, NULL);
		vdsaferelease <<= mpMainWindow;
	}

	vdsaferelease <<= mpThemeFonts;

	mpNativeDisplay = NULL;

	VDASSERT(mInstanceMap.empty());
}

IATUIClipboard *ATUIManager::GetClipboard() {
	return mpNativeDisplay ? mpNativeDisplay->GetClipboard() : NULL;
}

ATUIContainer *ATUIManager::GetMainWindow() const {
	return mpMainWindow;
}

ATUIWidget *ATUIManager::GetFocusWindow() const {
	return mbForeground ? mpActiveWindow : NULL;
}

ATUIWidget *ATUIManager::GetWindowByInstance(uint32 id) const {
	InstanceMap::const_iterator it = mInstanceMap.find(id);

	if (it == mInstanceMap.end())
		return NULL;

	return it->second;
}

void ATUIManager::BeginAction(ATUIWidget *w, const ATUITriggerBinding& binding) {
	ATUIWidget *target = w;

	if (binding.mTargetInstanceId) {
		target = GetWindowByInstance(binding.mTargetInstanceId);
		if (!target)
			return;
	}

	ActiveActionMap::insert_return_type r = mActiveActionMap.insert(binding.mVk);
	if (!r.second)
		return;

	ActiveAction *action = new ActiveAction;
	action->mpParent = this;
	action->mTargetInstance = target->GetInstanceId();
	action->mActionId = binding.mAction;

	r.first->second = action;

	LockDestroy();
	// must do this first as the action may go away!
	action->mRepeatDelay = 100;
	action->mRepeatTimer.SetOneShot(action, 400);

	target->OnActionStart(binding.mAction);
	UnlockDestroy();
}

void ATUIManager::EndAction(uint32 vk) {
	ActiveActionMap::iterator it = mActiveActionMap.find(vk);

	if (it != mActiveActionMap.end()) {
		ActiveAction *action = it->second;
		mActiveActionMap.erase(it);

		ATUIWidget *target = GetWindowByInstance(action->mTargetInstance);

		if (target) {
			LockDestroy();
			target->OnActionStop(action->mActionId);
			UnlockDestroy();
		}

		delete action;
	}
}

void ATUIManager::Resize(sint32 w, sint32 h) {
	if (w > 0 && h > 0 && mpMainWindow)
		mpMainWindow->SetArea(vdrect32(0, 0, w, h));
}

void ATUIManager::SetForeground(bool fg) {
	if (mbForeground != fg) {
		mbForeground = fg;

		if (!fg) {
			while(!mActiveActionMap.empty())
				EndAction(mActiveActionMap.begin()->first);
		}

		LockDestroy();

		ATUIWidget *w = mpActiveWindow;

		for(ATUIWidget *p = mpActiveWindow; p && w == mpActiveWindow; p = p->GetParent())
			p->SetActivated(fg);

		UnlockDestroy();
	}
}

void ATUIManager::SetThemeScaleFactor(float scale) {
	if (mThemeScale != scale) {
		mThemeScale = scale;
		ReinitTheme();
	}
}

void ATUIManager::SetActiveWindow(ATUIWidget *w) {
	if (mpActiveWindow == w)
		return;

	VDASSERT(w->GetManager() == this);

	ATUIWidget *prevFocus = mpActiveWindow;
	mpActiveWindow = w;

	LockDestroy();

	if (prevFocus)
		prevFocus->OnKillFocus();

	for(ATUIWidget *p = prevFocus; p && p->IsActivated() && !p->IsSameOrAncestorOf(mpActiveWindow); p = p->GetParent())
		p->SetActivated(false);

	for(ATUIWidget *p = mpActiveWindow; p && !p->IsActivated() && mpActiveWindow == w; p = p->GetParent())
		p->SetActivated(true);

	if (mpActiveWindow == w)
		w->OnSetFocus();
	
	UnlockDestroy();
}

void ATUIManager::CaptureCursor(ATUIWidget *w, bool motionMode, bool constrainPosition) {
	if (mpCursorWindow != w) {
		LockDestroy();

		if (mpCursorWindow) {
			if (mbCursorCaptured)
				mpCursorWindow->OnCaptureLost();

			if (w)
				mpCursorWindow->OnPointerLeave(1);
		}

		if (w) {
			SetCursorWindow(w);
			mpCursorWindow->OnPointerEnter(1);
		}

		UnlockDestroy();

		if (w)
			UpdateCursorImage();
	}

	if (!w)
		motionMode = false;

	if (mpNativeDisplay)
		mpNativeDisplay->ConstrainCursor(w && constrainPosition);

	bool newCaptureState = (w != NULL);

	if (mbCursorCaptured != newCaptureState || mbCursorMotionMode != motionMode) {
		mbCursorCaptured = newCaptureState;
		mbCursorMotionMode = motionMode;

		if (mpNativeDisplay) {
			if (newCaptureState)
				mpNativeDisplay->CaptureCursor(motionMode);
			else
				mpNativeDisplay->ReleaseCursor();
		}
	}
}

void ATUIManager::AddTrackingWindow(ATUIWidget *w) {
	auto it = std::lower_bound(mTrackingWindows.begin(), mTrackingWindows.end(), w);

	if (it == mTrackingWindows.end() || *it != w)
		mTrackingWindows.insert(it, w);
}

void ATUIManager::RemoveTrackingWindow(ATUIWidget *w) {
	auto it = std::lower_bound(mTrackingWindows.begin(), mTrackingWindows.end(), w);

	if (it != mTrackingWindows.end() && *it == w)
		mTrackingWindows.erase(it);
}

void ATUIManager::BeginModal(ATUIWidget *w) {
	if (!w || w->GetManager() != this) {
		VDASSERT(false);
		return;
	}

	void *cookie = NULL;
	if (mpNativeDisplay)
		cookie = mpNativeDisplay->BeginModal();

	ModalEntry& me = mModalStack.push_back();
	me.mpPreviousModal = mpModalWindow;
	me.mpPreviousModalCookie = mpModalCookie;

	mpModalWindow = w;
	mpModalCookie = cookie;

	if (!w->IsSameOrAncestorOf(mpActiveWindow))
		w->Focus();
}

void ATUIManager::EndModal() {
	VDASSERT(mpModalWindow);
	VDASSERT(!mModalStack.empty());

	if (mpNativeDisplay)
		mpNativeDisplay->EndModal(mpModalCookie);

	const ModalEntry& me = mModalStack.back();

	mpModalWindow = me.mpPreviousModal;
	mpModalCookie = me.mpPreviousModalCookie;
	mModalStack.pop_back();
}

bool ATUIManager::IsKeyDown(uint32 vk) {
	return mpNativeDisplay && mpNativeDisplay->IsKeyDown(vk);
}

vdpoint32 ATUIManager::GetCursorPosition() {
	if (!mpNativeDisplay)
		return vdpoint32(0, 0);

	return mpNativeDisplay->GetCursorPosition();
}

ATUITouchMode ATUIManager::GetTouchModeAtPoint(const vdpoint32& pt) const {
	ATUIWidget *w = mpMainWindow->HitTest(pt);

	if (!w)
		return kATUITouchMode_Default;

	if (mpModalWindow && !mpModalWindow->IsSameOrAncestorOf(w))
		return kATUITouchMode_Default;
	
	ATUITouchMode mode = w->GetTouchMode();

	if (mode == kATUITouchMode_Dynamic || mode == kATUITouchMode_MultiTouchDynamic) {
		vdpoint32 cpt;
		w->TranslateScreenPtToClientPt(pt, cpt);
		mode = w->GetTouchModeAtPoint(cpt);
	}

	return mode;
}

void ATUIManager::OnTouchInput(const ATUITouchInput *inputs, uint32 n) {
	for(uint32 i=0; i<n; ++i) {
		const ATUITouchInput& input = inputs[i];

		if (input.mbPrimary) {
			if (input.mbDown)
				OnMouseDown(input.mX, input.mY, kATUIVK_LButton, input.mbDoubleTap);
			else if (input.mbUp)
				OnMouseUp(input.mX, input.mY, kATUIVK_LButton);
			else
				OnMouseMove(input.mX, input.mY);
		} else {
			int freeIdx = -1;
			int matchIdx = -1;

			for(int i=0; i<(int)vdcountof(mPointers); ++i) {
				if (freeIdx < 0 && !mPointers[i].mpTargetWindow)
					freeIdx = i;

				if (mPointers[i].mId == input.mId && mPointers[i].mpTargetWindow)
					matchIdx = i;
			}

			if (matchIdx < 0) {
				if (!input.mbDown)
					continue;

				if (freeIdx < 0)
					continue;
				
				matchIdx = freeIdx;
				mPointers[matchIdx].mId = input.mId;
			}

			PointerInfo& ptr = mPointers[matchIdx];

			LockDestroy();
			ATUIWidget *w = mpMainWindow->HitTest(vdpoint32(input.mX, input.mY));

			const uint8 ptrBit = 2 << matchIdx;
			if (input.mbDown && ptr.mpTargetWindow != w) {
				if (ptr.mpTargetWindow)
					ptr.mpTargetWindow->OnPointerLeave(ptrBit);

				ptr.mpTargetWindow = w;

				if (w)
					w->OnPointerEnter(ptrBit);
			} else
				w = ptr.mpTargetWindow;

			if (w) {
				vdpoint32 cpt;

				w->TranslateScreenPtToClientPt(vdpoint32(input.mX, input.mY), cpt);

				if (input.mbDown)
					w->OnMouseDown(cpt.x, cpt.y, kATUIVK_LButton, false);
				else if (input.mbUp) {
					w->OnMouseUp(cpt.x, cpt.y, kATUIVK_LButton);

					ptr.mpTargetWindow = NULL;

					w->OnPointerLeave(ptrBit);
				} else
					w->OnMouseMove(cpt.x, cpt.y);
			}
			UnlockDestroy();
		}
	}
}

bool ATUIManager::OnMouseRelativeMove(sint32 dx, sint32 dy) {
	if (!mbCursorCaptured || !mpCursorWindow)
		return false;

	if (!(dx | dy))
		return false;

	LockDestroy();
	mpCursorWindow->OnMouseRelativeMove(dx, dy);
	UnlockDestroy();
	return true;
}

bool ATUIManager::OnMouseMove(sint32 x, sint32 y) {
	VDASSERT(!mDestroyLocks);

	if (!mbCursorCaptured) {
		if (!UpdateCursorWindow(x, y))
			return true;
	}

	if (!mpCursorWindow)
		return false;

	vdpoint32 cpt;
	if (mpCursorWindow->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt) || mbCursorCaptured) {
		LockDestroy();
		mpCursorWindow->OnMouseMove(cpt.x, cpt.y);
		UnlockDestroy();
	}
	return true;
}

bool ATUIManager::OnMouseDown(sint32 x, sint32 y, uint32 vk, bool dblclk) {
	if (!mpMainWindow)
		return false;

	if (!mbCursorCaptured) {
		if (!UpdateCursorWindow(x, y))
			return true;
	}

	if (!mpCursorWindow)
		return false;

	vdpoint32 cpt;
	if (mpCursorWindow->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt) || mbCursorCaptured) {
		LockDestroy();
		mpCursorWindow->OnMouseDown(cpt.x, cpt.y, vk, dblclk);
		UnlockDestroy();
	}

	return true;
}

bool ATUIManager::OnMouseUp(sint32 x, sint32 y, uint32 vk) {
	if (!mpMainWindow)
		return false;

	if (!mbCursorCaptured) {
		if (!UpdateCursorWindow(x, y))
			return true;
	}

	if (!mpCursorWindow)
		return false;

	vdpoint32 cpt;
	if (mpCursorWindow->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt) || mbCursorCaptured) {
		LockDestroy();
		mpCursorWindow->OnMouseUp(cpt.x, cpt.y, vk);
		UnlockDestroy();
	}

	return true;
}

bool ATUIManager::OnMouseWheel(sint32 x, sint32 y, float delta) {
	if (!mpMainWindow)
		return false;

	if (!mbCursorCaptured) {
		if (!UpdateCursorWindow(x, y))
			return true;
	}

	if (!mpCursorWindow)
		return false;

	LockDestroy();
	for(ATUIWidget *w = mpCursorWindow; w; w = w->GetParent()) {
		vdpoint32 cpt;
		if (w->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt) || mbCursorCaptured) {
			if (w->OnMouseWheel(cpt.x, cpt.y, delta))
				break;
		}

		if (mbCursorCaptured)
			break;
	}
	UnlockDestroy();

	return true;
}

void ATUIManager::OnMouseLeave() {
	if (!mpCursorWindow)
		return;

	LockDestroy();

	if (mpCursorWindow) {
		CaptureCursor(NULL);
		mpCursorWindow->OnPointerLeave(1);

		SetCursorWindow(nullptr);
	}

	UnlockDestroy();
}

void ATUIManager::OnMouseHover(sint32 x, sint32 y) {
	if (!mpCursorWindow)
		return;

	vdpoint32 cpt;
	if (mpCursorWindow->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt) || mbCursorCaptured) {
		LockDestroy();
		mpCursorWindow->OnMouseHover(cpt.x, cpt.y);
		UnlockDestroy();
	}
}

bool ATUIManager::OnContextMenu(const vdpoint32 *pt) {
	// If we have a point then we should do the lookup that way; otherwise, use
	// the focus window (keyboard activated).

	bool success = false;

	if (pt) {
		if (!mpCursorWindow)
			return false;

		vdpoint32 cpt;

		if (mpCursorWindow->TranslateScreenPtToClientPt(*pt, cpt) || mbCursorCaptured) {
			LockDestroy();
			success = mpCursorWindow->OnContextMenu(&cpt);
			UnlockDestroy();
		}
	} else if (mpActiveWindow) {
		LockDestroy();
		success = mpActiveWindow->OnContextMenu(NULL);
		UnlockDestroy();
	}

	return success;
}

bool ATUIManager::OnKeyDown(const ATUIKeyEvent& event) {
	LockDestroy();

	for(ATUIWidget *w = mpActiveWindow; w; w = w->GetParent()) {
		if (w->OnKeyDown(event)) {
			UnlockDestroy();
			return true;
		}

		if (w == mpModalWindow)
			break;
	}

	UnlockDestroy();
	return false;
}

bool ATUIManager::OnKeyUp(const ATUIKeyEvent& event) {
	LockDestroy();

	EndAction(event.mVirtKey);

	if (event.mVirtKey != event.mExtendedVirtKey)
		EndAction(event.mExtendedVirtKey);

	for(ATUIWidget *w = mpActiveWindow; w; w = w->GetParent()) {
		if (w->OnKeyUp(event)) {
			UnlockDestroy();
			return true;
		}

		if (w == mpModalWindow)
			break;
	}

	UnlockDestroy();
	return false;
}

bool ATUIManager::OnChar(const ATUICharEvent& event) {
	LockDestroy();

	for(ATUIWidget *w = mpActiveWindow; w; w = w->GetParent()) {
		if (w->OnChar(event)) {
			UnlockDestroy();
			return true;
		}

		if (w == mpModalWindow)
			break;
	}

	UnlockDestroy();
	return false;
}

bool ATUIManager::OnCharUp(const ATUICharEvent& event) {
	LockDestroy();

	for(ATUIWidget *w = mpActiveWindow; w; w = w->GetParent()) {
		if (w->OnCharUp(event)) {
			UnlockDestroy();
			return true;
		}

		if (w == mpModalWindow)
			break;
	}

	UnlockDestroy();
	return false;
}

void ATUIManager::OnForceKeysUp() {
	LockDestroy();

	for(ATUIWidget *w = mpActiveWindow; w; w = w->GetParent()) {
		w->OnForceKeysUp();

		if (w == mpModalWindow)
			break;
	}

	UnlockDestroy();

	// kill all actions from regular keys
	vdfastvector<uint32> vksToRelease;

	for(const auto& entry : mActiveActionMap) {
		if (entry.first < 0x200)
			vksToRelease.push_back(entry.first);
	}

	for(uint32 vk : vksToRelease)
		EndAction(vk);
}

void ATUIManager::OnCaptureLost() {
	if (mbCursorCaptured) {
		mbCursorCaptured = false;

		if (mpCursorWindow) {
			LockDestroy();
			mpCursorWindow->OnCaptureLost();
			UnlockDestroy();
		}
	}
}

ATUIDragEffect ATUIManager::OnDragEnter(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) {
	if (mpDropObject != obj) {
		OnDragLeave();

		IATUIDragDropObject *oldObject = mpDropObject;
		
		mpDropObject = obj;

		if (oldObject)
			oldObject->Release();

		if (obj)
			obj->AddRef();
	}

	if (!obj)
		return ATUIDragEffect::None;

	return OnDragOver(x, y, modifiers);
}

ATUIDragEffect ATUIManager::OnDragOver(sint32 x, sint32 y, ATUIDragModifiers modifiers) {
	ATUIWidget *w = mpMainWindow->DragHitTest(vdpoint32(x, y));
	bool isEnter = false;

	if (mpDropTargetWindow != w) {
		ATUIWidget *prev = mpDropTargetWindow;

		mpDropTargetWindow = w;

		if (prev)
			prev->OnDragLeave();

		isEnter = true;
	}
	
	if (!mpDropTargetWindow)
		return ATUIDragEffect::None;

	vdpoint32 cpt;
	if (!mpDropTargetWindow->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt))
		return ATUIDragEffect::None;

	return isEnter ? w->OnDragEnter(cpt.x, cpt.y, modifiers, mpDropObject) : w->OnDragOver(cpt.x, cpt.y, modifiers, mpDropObject);
}

void ATUIManager::OnDragLeave() {
	if (mpDropTargetWindow) {
		ATUIWidget *w = mpDropTargetWindow;
		mpDropTargetWindow = nullptr;

		w->OnDragLeave();
	}

	if (mpDropObject) {
		IATUIDragDropObject *obj = mpDropObject;
		mpDropObject = nullptr;

		obj->Release();
	}
}

ATUIDragEffect ATUIManager::OnDragDrop(sint32 x, sint32 y, ATUIDragModifiers modifiers, IATUIDragDropObject *obj) {
	OnDragEnter(x, y, modifiers, obj);

	ATUIDragEffect effect = ATUIDragEffect::None;
	if (mpDropTargetWindow) {
		vdpoint32 cpt;
		if (mpDropTargetWindow->TranslateScreenPtToClientPt(vdpoint32(x, y), cpt))
			effect = mpDropTargetWindow->OnDragDrop(cpt.x, cpt.y, modifiers, obj);

		if (mpDropTargetWindow) {
			ATUIWidget *w = mpDropTargetWindow;
			mpDropTargetWindow = nullptr;

			w->OnDragLeave();
		}
	}

	return effect;
}

const wchar_t *ATUIManager::GetCustomEffectPath() const {
	return mCustomEffectPath.c_str();
}

void ATUIManager::SetCustomEffectPath(const wchar_t *s, bool forceReload) {
	if (forceReload || mCustomEffectPath != s) {
		mCustomEffectPath = s;
		mbPendingCustomEffectPath = true;
	}
}

void ATUIManager::Attach(ATUIWidget *w) {
	do {
		++mNextInstanceId;
	} while(!mNextInstanceId || !mInstanceMap.insert(InstanceMap::value_type(mNextInstanceId, w)).second);

	w->SetInstanceId(mNextInstanceId);
}

void ATUIManager::Detach(ATUIWidget *w) {
	VDASSERT(mpModalWindow != w);

	uint32 instanceId = w->GetInstanceId();
	VDASSERT(instanceId);

	InstanceMap::iterator itInst = mInstanceMap.find(instanceId);
	if (itInst != mInstanceMap.end()) {
		mInstanceMap.erase(itInst);
	} else {
		VDASSERT(!"Detaching window not in instance map.");
	}

	w->SetInstanceId(0);

	if (mDestroyLocks) {
		w->AddRef();
		mDestroyList.push_back(w);
	}

	if (mpDropTargetWindow == w)
		mpDropTargetWindow = nullptr;

	if (mpActiveWindow == w) {
		ATUIWidget *newActive = w->GetParentOrOwner();
		VDASSERT(!newActive || newActive->GetManager());

		mpActiveWindow = nullptr;
		SetActiveWindow(newActive);
	}

	if (w->HasCursor()) {
		if (mpCursorWindow == w) {
			if (mbCursorCaptured) {
				mbCursorCaptured = false;

				if (mpNativeDisplay)
					mpNativeDisplay->ReleaseCursor();
			}

			SetCursorWindow(w->GetParent());

			if (mpCursorWindow)
				mpCursorWindow->OnPointerEnter(1);

			mbCursorCaptured = false;
			UpdateCursorImage();
		}

		for(uint32 i=0; i<vdcountof(mPointers); ++i) {
			if (mPointers[i].mpTargetWindow == w)
				mPointers[i].mpTargetWindow = NULL;
		}

		w->OnPointerClear();
	} else {
		VDASSERT(mpCursorWindow != w);
	}

	RemoveTrackingWindow(w);

	for(ModalStack::iterator it = mModalStack.begin(), itEnd = mModalStack.end();
		it != itEnd;
		++it)
	{
		ModalEntry& ent = *it;

		if (ent.mpPreviousModal == w)
			ent.mpPreviousModal = w->GetParent();
	}
}

void ATUIManager::Invalidate(ATUIWidget *w) {
	if (!mbInvalidated) {
		mbInvalidated = true;

		if (mpNativeDisplay)
			mpNativeDisplay->Invalidate();
	}
}

void ATUIManager::UpdateCursorImage(ATUIWidget *w) {
	if (w->IsSameOrAncestorOf(mpCursorWindow))
		UpdateCursorImage();
}

void ATUIManager::AttachCompositor(IVDDisplayCompositionEngine& dce) {
	mpDisplayCompositionEngine = &dce;
	mbPendingCustomEffectPath = true;
}

void ATUIManager::DetachCompositor() {
	mpDisplayCompositionEngine = nullptr;
}

void ATUIManager::PreComposite(const VDDisplayCompositeInfo& compInfo) {
	if (mbPendingCustomEffectPath) {
		mbPendingCustomEffectPath = false;

		mpDisplayCompositionEngine->LoadCustomEffect(mCustomEffectPath.c_str());
	}
}

void ATUIManager::Composite(IVDDisplayRenderer& r, const VDDisplayCompositeInfo& compInfo) {
	// !!NOTE!! We _cannot_ check for mDestroyLocks=0 here. A modal dialog from the Win32 side
	// may cause reentrancy that requires us to paint while being in the middle of an event
	// handler. This should be OK since the drawing path shouldn't be messing with the window
	// hierarchy.

	if (mpMainWindow) {
		mpMainWindow->UpdateLayout();
		mpMainWindow->Draw(r);
	}

	mbInvalidated = false;
}

void ATUIManager::UpdateCursorImage() {
	uint32 id = 0;

	for(ATUIWidget *w = mpCursorWindow; w && w != mpModalWindow; w = w->GetParent()) {
		id = w->GetCursorImage();

		if (id)
			break;
	}

	if (mCursorImageId != id) {
		mCursorImageId = id;

		if (mpNativeDisplay)
			mpNativeDisplay->SetCursorImage(mCursorImageId);
	}
}

bool ATUIManager::UpdateCursorWindow(sint32 x, sint32 y) {
	VDASSERT(!mbCursorCaptured);

	ATUIWidget *w = mpMainWindow->HitTest(vdpoint32(x, y));

	if (w != mpCursorWindow) {
		if (w && w->HasCursor())
			w = NULL;

		if (mpModalWindow && !mpModalWindow->IsSameOrAncestorOf(w))
			return false;

		LockDestroy();

		if (mpCursorWindow)
			mpCursorWindow->OnPointerLeave(1);

		SetCursorWindow(w);

		if (w)
			w->OnPointerEnter(1);

		UnlockDestroy();

		UpdateCursorImage();
	}

	return true;
}

void ATUIManager::SetCursorWindow(ATUIWidget *w) {
	if (mpCursorWindow == w)
		return;

	for(ATUIWidget *tw : mTrackingWindows) {
		if (tw->IsSameOrAncestorOf(w))
			tw->OnTrackCursorChanges(w);
		else if (tw->IsSameOrAncestorOf(mpCursorWindow))
			tw->OnTrackCursorChanges(nullptr);
	}

	mpCursorWindow = w;
}

void ATUIManager::LockDestroy() {
	++mDestroyLocks;
}

void ATUIManager::UnlockDestroy() {
	if (!--mDestroyLocks && !mDestroyList.empty()) {
		DestroyList dlist;

		dlist.swap(mDestroyList);

		while(!dlist.empty()) {
			dlist.back()->Release();
			dlist.pop_back();
		}
	}
}

void ATUIManager::RepeatAction(ActiveAction& action) {
	ATUIWidget *target = GetWindowByInstance(action.mTargetInstance);
	if (!target)
		return;

	LockDestroy();
	target->OnActionRepeat(action.mActionId);
	UnlockDestroy();
}

void ATUIManager::ReinitTheme() {
	vdsaferelease <<= mpThemeFonts;
	vdsafedelete <<= mpStockImages;

	VDCreateDisplaySystemFont(VDRoundToInt(15*mThemeScale), false, "MS Shell Dlg", &mpThemeFonts[kATUIThemeFont_Default]);
	VDCreateDisplaySystemFont(VDRoundToInt(20*mThemeScale), false, "MS Shell Dlg", &mpThemeFonts[kATUIThemeFont_Header]);
	VDCreateDisplaySystemFont(VDRoundToInt(14*mThemeScale), false, "Lucida Console", &mpThemeFonts[kATUIThemeFont_MonoSmall]);
	VDCreateDisplaySystemFont(VDRoundToInt(20*mThemeScale), false, "Lucida Console", &mpThemeFonts[kATUIThemeFont_Mono]);
	VDCreateDisplaySystemFont(-VDRoundToInt(11*mThemeScale), false, "Tahoma", &mpThemeFonts[kATUIThemeFont_Tooltip]);
	VDCreateDisplaySystemFont(-VDRoundToInt(11*mThemeScale), true, "Tahoma", &mpThemeFonts[kATUIThemeFont_TooltipBold]);
	mpThemeFonts[kATUIThemeFont_Menu] = mpThemeFonts[kATUIThemeFont_Tooltip];
	mpThemeFonts[kATUIThemeFont_Menu]->AddRef();

	static const wchar_t kMagicChars[]={
		L'a',	// menu check
		L'h',	// menu radio
		L'8',	// menu arrow
		L'3',	// button left
		L'4',	// button right
		L'5',	// button up
		L'6',	// button down
	};

	VDASSERTCT(vdcountof(kMagicChars) == vdcountof(mpStockImages));

	VDDisplayFontMetrics menuFontMetrics;
	mpThemeFonts[kATUIThemeFont_Menu]->GetMetrics(menuFontMetrics);

	vdrefptr<IVDDisplayFont> marlett;
	VDCreateDisplaySystemFont(menuFontMetrics.mAscent + menuFontMetrics.mDescent + 2, false, "Marlett", ~marlett);

	mSystemMetrics.mVertSliderWidth = menuFontMetrics.mAscent + menuFontMetrics.mDescent + 4;

	for(int i=0; i<vdcountof(mpStockImages); ++i) {
		vdfastvector<VDDisplayFontGlyphPlacement> placements;
		vdrect32 bounds;
		marlett->ShapeText(&kMagicChars[i], 1, placements, &bounds, NULL, NULL);

		ATUIStockImage *img1 = new ATUIStockImage;
		mpStockImages[i] = img1;
		img1->mBuffer.init(bounds.width(), bounds.height(), nsVDPixmap::kPixFormat_XRGB8888);
		img1->mWidth = bounds.width();
		img1->mHeight = bounds.height();
		img1->mOffsetX = bounds.left;
		img1->mOffsetY = bounds.top;

		VDDisplayRendererSoft rs;
		rs.Init();
		rs.Begin(img1->mBuffer);
		rs.SetColorRGB(0);
		rs.FillRect(0, 0, bounds.width(), bounds.height());
		VDDisplayTextRenderer& tr = *rs.GetTextRenderer();
		tr.SetColorRGB(0xFFFFFF);
		tr.SetFont(marlett);
		tr.SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
		tr.SetPosition(0, 0);
		tr.DrawTextSpan(&kMagicChars[i], 1);

		img1->mImageView.SetImage(img1->mBuffer, false);
	}

	if (mpMainWindow)
		mpMainWindow->InvalidateLayout();
}
