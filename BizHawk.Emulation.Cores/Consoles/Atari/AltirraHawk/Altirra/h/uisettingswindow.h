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

#ifndef f_AT_UISETTINGSWINDOW_H
#define f_AT_UISETTINGSWINDOW_H

#include <initializer_list>
#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <at/atui/uicontainer.h>
#include <at/atuicontrols/uislider.h>
#include "uiqueue.h"

class ATUISetting;
class ATUISettingWindow;
class ATUISettingsWindow;
class IVDDisplayFont;

struct ATUIEnumValue {
	sint32 mValue;
	const wchar_t *mpName;
};

class IATUISettingsScreen : public IVDRefCount {
public:
	virtual void BuildSettings(ATUISettingsWindow *dst) = 0;
};

class ATUISetting : public IVDUnknown {
public:
	enum { kTypeID = 'ause' };

	ATUISetting(const wchar_t *name) : mbValueDynamic(false), mName(name) {}
	virtual ~ATUISetting() {}

	void *AsInterface(uint32 id);

	virtual bool IsDeferred() const;

	virtual void Read();
	virtual void Write();

	void SetValueDynamic();
	bool IsValueDynamic() const { return mbValueDynamic; }

	bool IsNameDynamic() const;
	void GetDynamicName(VDStringW& name) const;

	void SetDynamicNameFn(const vdfunction<void(VDStringW&)>& fn);

	const wchar_t *GetName() const { return mName.c_str(); }

protected:
	bool mbValueDynamic;
	VDStringW mName;
	vdfunction<void(VDStringW&)> mpDynamicNameFn;
};

class ATUIBoolSetting final : public ATUISetting {
public:
	enum { kTypeID = 'aubs' };

	ATUIBoolSetting(const wchar_t *name);
	
	void *AsInterface(uint32 id);

	void Read();
	void Write();

	void SetGetter(const vdfunction<bool()>& fn);
	void SetSetter(const vdfunction<void(bool)>& fn);
	void SetImmediateSetter(const vdfunction<void(bool)>& fn);

	bool GetValue() const { return mValue; }
	void SetValue(bool value);

private:
	bool mValue;
	vdfunction<bool()> mpGetter;
	vdfunction<void(bool)> mpSetter;
	vdfunction<void(bool)> mpImmediateSetter;
};

class ATUIIntSetting final : public ATUISetting {
public:
	enum { kTypeID = 'auis' };

	ATUIIntSetting(const wchar_t *name, sint32 minVal, sint32 maxVal);
	
	void *AsInterface(uint32 id);

	void Read();
	void Write();

	void SetGetter(const vdfunction<sint32()>& fn);
	void SetSetter(const vdfunction<void(sint32)>& fn);
	void SetImmediateSetter(const vdfunction<void(sint32)>& fn);

	sint32 GetMinValue() const { return mMinVal; }
	sint32 GetMaxValue() const { return mMaxVal; }

	sint32 GetValue() const { return mValue; }
	void SetValue(sint32 value);

private:
	sint32 mValue;
	sint32 mMinVal;
	sint32 mMaxVal;
	vdfunction<sint32()> mpGetter;
	vdfunction<void(sint32)> mpSetter;
	vdfunction<void(sint32)> mpImmediateSetter;
};

class ATUIEnumSetting final : public ATUISetting {
public:
	enum { kTypeID = 'aues' };

	ATUIEnumSetting(const wchar_t *name, const ATUIEnumValue *values, uint32 n);
	ATUIEnumSetting(const wchar_t *name, std::initializer_list<ATUIEnumValue> il);

	void *AsInterface(uint32 id);

	bool IsDeferred() const override;

	void Read();
	void Write();

	void SetGetter(const vdfunction<sint32()>& fn);
	void SetSetter(const vdfunction<void(sint32)>& fn);
	void SetImmediateSetter(const vdfunction<void(sint32)>& fn);

	sint32 GetValue() const { return mValueIndex; }
	void SetValue(sint32 value);

	uint32 GetValueCount() const { return (uint32)mValues.size(); }
	const wchar_t *GetValueName(uint32 index) const { return mValues[index].mpName; }

protected:
	vdfastvector<ATUIEnumValue> mValues;
	sint32 mValueIndex;
	vdfunction<sint32()> mpGetter;
	vdfunction<void(sint32)> mpSetter;
	vdfunction<void(sint32)> mpImmediateSetter;
};

class ATUISubScreenSetting final : public ATUISetting {
public:
	enum { kTypeID = 'auss' };

	ATUISubScreenSetting(const wchar_t *name, const vdfunction<void(IATUISettingsScreen **)>& builder) : ATUISetting(name), mpBuilder(builder) {}

	void *AsInterface(uint32 id);

	void BuildScreen(IATUISettingsScreen **screen);

protected:
	vdfunction<void(IATUISettingsScreen **)> mpBuilder;
};

class ATUIActionSetting final : public ATUISetting {
public:
	enum { kTypeID = 'auas' };

	ATUIActionSetting(const wchar_t *name) : ATUISetting(name) {}
	ATUIActionSetting(const wchar_t *name, const vdfunction<bool()>& action) : ATUISetting(name), mpAction(action) {}

	void SetAction(const vdfunction<bool()>& action) { mpAction = action; }
	void SetAsyncAction(const vdfunction<vdrefptr<ATUIFutureWithResult<bool>>()>& action) { mpAsyncAction = action; }

	void *AsInterface(uint32 id);

	bool IsAsync() const { return mpAsyncAction; }
	bool Activate();
	vdrefptr<ATUIFutureWithResult<bool>> ActivateAsync();

protected:
	vdfunction<bool()> mpAction;
	vdfunction<vdrefptr<ATUIFutureWithResult<bool>>()> mpAsyncAction;
};

class ATUISettingsWindow final : public ATUIContainer {
public:
	enum {
		kActionUp = kActionCustom,
		kActionDown,
		kActionCancel
	};

	ATUISettingsWindow();

	void SetSettingsScreen(IATUISettingsScreen *screen);
	void SetCaption(const wchar_t *caption);
	void AddSetting(ATUISetting *setting);
	void AddSeparator();

	void SetOnDestroy(const vdfunction<void()>& fn);

	void SetSelectedIndex(sint32 index, bool scroll);

public:
	void OnCreate() override;
	void OnDestroy() override;
	void OnSize() override;
	void OnSetFocus() override;
	void OnMouseDownL(sint32 x, sint32 y) override;
	void OnMouseUpL(sint32 x, sint32 y) override;
	void OnMouseMove(sint32 x, sint32 y) override;
	bool OnMouseWheel(sint32 x, sint32 y, float delta) override;
	void OnMouseLeave() override;
	void OnActionStart(uint32 trid) override;
	void OnActionRepeat(uint32 trid) override;
	void OnActionStop(uint32 trid) override;
	void OnTrackCursorChanges(ATUIWidget *w) override;
	void UpdateLayout() override;

protected:
	void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) override;

	void DestroyScreen();
	void BuildScreen();
	void RecomputeSlider();
	void OnSubScreenActivated(ATUISubScreenSetting *s);
	void OnAction(ATUIActionSetting *s);
	void OnAsyncActionCompleted();
	void OnDynamicUpdate();

	struct SettingsEntry {
		ATUISetting *mpSetting;
		sint32 mVPos;
	};

	vdfastvector<SettingsEntry> mSettings;
	vdfastvector<ATUISettingWindow *> mSettingWindows;
	sint32 mSelectedIndex;
	sint32 mRowHeight;
	sint32 mHeaderHeight;
	sint32 mHeaderBaseline;
	VDStringW mCaption;
	bool mbHeaderActivated;
	bool mbHeaderHighlighted;
	float mScrollAccum;
	sint32 mCurrentVPos;
	sint32 mTotalHeight;

	vdrefptr<IVDDisplayFont> mpFont;
	vdrefptr<ATUIContainer> mpScrollPane;
	vdrefptr<ATUIWidget> mpSelectionFill;
	vdrefptr<ATUISlider> mpSlider;
	vdrefptr<IATUISettingsScreen> mpCurrentScreen;

	struct StackedScreen {
		IATUISettingsScreen *mpScreen;
		int mSelIndex;
	};
	vdfastvector<StackedScreen> mScreenStack;

	vdfunction<void()> mpOnDestroy;
	vdrefptr<ATUIFutureWithResult<bool>> mpPendingResult;
};

void ATCreateUISettingsWindow(ATUISettingsWindow **pp);

#endif	// f_AT_UISETTINGSWINDOW_H
