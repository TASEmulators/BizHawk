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

#ifndef f_AT_ATNATIVEUI_UIPROXIES_H
#define f_AT_ATNATIVEUI_UIPROXIES_H

#include <vd2/system/event.h>
#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <vd2/system/unknown.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vectors.h>
#include <vd2/system/VDString.h>
#include <vd2/system/win32/miniwindows.h>

#include <at/atnativeui/nativewindowproxy.h>

struct VDPixmap;
struct VDUIAccelerator;
class VDFunctionThunkInfo;
struct ITextDocument;

class VDUIProxyControl : public vdlist_node, public ATUINativeWindowProxy {
public:
	VDUIProxyControl();

	VDZHWND GetHandle() const { return mhwnd; }

	virtual void Attach(VDZHWND hwnd);
	virtual void Detach();

	void SetRedraw(bool);
	
	virtual VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);
	virtual VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam);
	virtual void OnFontChanged();

protected:
	int mRedrawInhibitCount;
};

class VDUIProxyMessageDispatcherW32 {
public:
	void AddControl(VDUIProxyControl *control);
	void RemoveControl(VDZHWND hwnd);
	void RemoveAllControls(bool detach);

	bool TryDispatch_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam, VDZLRESULT& result);
	bool TryDispatch_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam, VDZLRESULT& result);
	VDZLRESULT Dispatch_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);
	VDZLRESULT Dispatch_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam);
	void DispatchFontChanged();

protected:
	size_t Hash(VDZHWND hwnd) const;
	VDUIProxyControl *GetControl(VDZHWND hwnd);

	enum { kHashTableSize = 31 };

	typedef vdlist<VDUIProxyControl> HashChain;
	HashChain mHashTable[kHashTableSize];
};

/////////////////////////////////////////////////////////////////////////////

class IVDUIListViewVirtualItem : public IVDRefCount {
public:
	virtual void GetText(int subItem, VDStringW& s) const = 0;
};

class IVDUIListViewVirtualComparer {
public:
	virtual int Compare(IVDUIListViewVirtualItem *x, IVDUIListViewVirtualItem *y) = 0;
};

class IVDUIListViewIndexedProvider {
public:
	virtual void GetText(uint32 itemId, uint32 subItem, VDStringW& s) const = 0;
};

class IVDUIListViewIndexedComparer {
public:
	virtual int Compare(uint32 x, uint32 y) = 0;
};

class VDUIProxyListView : public VDUIProxyControl {
public:
	VDUIProxyListView();

	void SetIndexedProvider(IVDUIListViewIndexedProvider *p);

	void AutoSizeColumns(bool expandlast = false);
	void Clear();
	void ClearExtraColumns();
	void DeleteItem(int index);
	int GetColumnCount() const;
	int GetItemCount() const;
	int GetSelectedIndex() const;
	void SetSelectedIndex(int index);
	uint32 GetSelectedItemId() const;
	IVDUIListViewVirtualItem *GetSelectedItem() const;
	void GetSelectedIndices(vdfastvector<int>& indices) const;
	void SetFullRowSelectEnabled(bool enabled);
	void SetGridLinesEnabled(bool enabled);
	bool AreItemCheckboxesEnabled() const;
	void SetItemCheckboxesEnabled(bool enabled);
	void EnsureItemVisible(int index);
	int GetVisibleTopIndex();
	void SetVisibleTopIndex(int index);
	IVDUIListViewVirtualItem *GetSelectedVirtualItem() const;
	IVDUIListViewVirtualItem *GetVirtualItem(int index) const;
	uint32 GetItemId(int index) const;
	void InsertColumn(int index, const wchar_t *label, int width, bool rightAligned = false);
	int InsertItem(int item, const wchar_t *text);
	int InsertVirtualItem(int item, IVDUIListViewVirtualItem *lvvi);
	int InsertIndexedItem(int item, uint32 id);
	void RefreshItem(int item);
	void RefreshAllItems();
	void EditItemLabel(int item);
	void GetItemText(int item, VDStringW& s) const;
	void SetItemText(int item, int subitem, const wchar_t *text);

	bool IsItemChecked(int item);
	void SetItemChecked(int item, bool checked);
	void SetItemCheckedVisible(int item, bool visible);

	void SetItemImage(int item, uint32 imageIndex);

	bool GetItemScreenRect(int item, vdrect32& r) const;

	void Sort(IVDUIListViewIndexedComparer& comparer);
	void Sort(IVDUIListViewVirtualComparer& comparer);

	VDEvent<VDUIProxyListView, int>& OnColumnClicked() {
		return mEventColumnClicked;
	}

	VDEvent<VDUIProxyListView, int>& OnItemSelectionChanged() {
		return mEventItemSelectionChanged;
	}

	void SetOnItemDoubleClicked(vdfunction<void(int)> fn);

	VDEvent<VDUIProxyListView, int>& OnItemDoubleClicked() {
		return mEventItemDoubleClicked;
	}

	struct ContextMenuEvent {
		int mIndex;
		int mX;
		int mY;
	};

	VDEvent<VDUIProxyListView, ContextMenuEvent>& OnItemContextMenu() {
		return mEventItemContextMenu;
	}

	struct CheckedChangingEvent {
		int mIndex;
		bool mbNewVisible;
		bool mbNewChecked;
		bool mbAllowChange;
	};

	VDEvent<VDUIProxyListView, CheckedChangingEvent *>& OnItemCheckedChanging() {
		return mEventItemCheckedChanging;
	}

	VDEvent<VDUIProxyListView, int>& OnItemCheckedChanged() {
		return mEventItemCheckedChanged;
	}

	struct LabelChangedEvent {
		bool mbAllowEdit;
		int mIndex;
		const wchar_t *mpNewLabel;
	};

	VDEvent<VDUIProxyListView, LabelChangedEvent *>& OnItemLabelChanged() {
		return mEventItemLabelEdited;
	}

	VDEvent<VDUIProxyListView, int>& OnItemBeginDrag() {
		return mEventItemBeginDrag;
	}

	VDEvent<VDUIProxyListView, int>& OnItemBeginRDrag() {
		return mEventItemBeginRDrag;
	}

protected:
	void Detach();

	VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) override;
	void OnFontChanged() override;

	int			mChangeNotificationLocks = 0;
	int			mNextTextIndex = 0;
	bool		mbIndexedMode = false;
	IVDUIListViewIndexedProvider *mpIndexedProvider = nullptr;
	VDStringW	mTextW[3];
	VDStringA	mTextA[3];

	vdfastvector<int>	mColumnWidthCache;

	VDEvent<VDUIProxyListView, int> mEventColumnClicked;
	VDEvent<VDUIProxyListView, int> mEventItemSelectionChanged;
	VDEvent<VDUIProxyListView, int> mEventItemDoubleClicked;
	vdfunction<void(int)> mpOnItemDoubleClicked;
	VDEvent<VDUIProxyListView, int> mEventItemCheckedChanged;
	VDEvent<VDUIProxyListView, CheckedChangingEvent *> mEventItemCheckedChanging;
	VDEvent<VDUIProxyListView, ContextMenuEvent> mEventItemContextMenu;
	VDEvent<VDUIProxyListView, LabelChangedEvent *> mEventItemLabelEdited;
	VDEvent<VDUIProxyListView, int> mEventItemBeginDrag;
	VDEvent<VDUIProxyListView, int> mEventItemBeginRDrag;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyHotKeyControl : public VDUIProxyControl {
public:
	VDUIProxyHotKeyControl();
	~VDUIProxyHotKeyControl();

	bool GetAccelerator(VDUIAccelerator& accel) const;
	void SetAccelerator(const VDUIAccelerator& accel);

	VDEvent<VDUIProxyHotKeyControl, VDUIAccelerator>& OnEventHotKeyChanged() {
		return mEventHotKeyChanged;
	}

protected:
	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);

	VDEvent<VDUIProxyHotKeyControl, VDUIAccelerator> mEventHotKeyChanged;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyTabControl : public VDUIProxyControl {
public:
	VDUIProxyTabControl();
	~VDUIProxyTabControl();

	void AddItem(const wchar_t *s);
	void DeleteItem(int index);

	vdsize32 GetControlSizeForContent(const vdsize32&) const;
	vdrect32 GetContentArea() const;

	int GetSelection() const;
	void SetSelection(int index);

	VDEvent<VDUIProxyTabControl, int>& OnSelectionChanged() {
		return mSelectionChanged;
	}

protected:
	VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam);

	VDEvent<VDUIProxyTabControl, int> mSelectionChanged;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyListBoxControl final : public VDUIProxyControl {
public:
	VDUIProxyListBoxControl();
	~VDUIProxyListBoxControl();

	void EnableAutoItemEditing();

	void Clear();
	int AddItem(const wchar_t *s, uintptr data = 0);
	int InsertItem(int pos, const wchar_t *s, uintptr data = 0);
	void DeleteItem(int pos);
	void EnsureItemVisible(int pos);
	void EditItem(int pos);

	void SetItemText(int index, const wchar_t *s);
	uintptr GetItemData(int index) const;

	int GetSelection() const;
	void SetSelection(int index);

	void MakeSelectionVisible();

	void SetTabStops(const int *units, uint32 n);

	void SetOnSelectionChanged(vdfunction<void(int)> fn);
	void SetOnItemDoubleClicked(vdfunction<void(int)> fn);
	void SetOnItemEdited(vdfunction<void(int ,const wchar_t *)> fn);

	VDEvent<VDUIProxyListBoxControl, int>& OnSelectionChanged() {
		return mSelectionChanged;
	}

	VDEvent<VDUIProxyListBoxControl, int>& OnItemDoubleClicked() {
		return mEventItemDoubleClicked;
	}

protected:
	void EndEditItem();
	void CancelEditTimer();
	
	void Detach() override;

	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);
	VDZLRESULT ListBoxWndProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);
	VDZLRESULT LabelEditWndProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);
	void AutoEditTimerProc(VDZHWND, VDZUINT, VDZUINT_PTR, VDZDWORD);

	uint32 mSuppressNotificationCount = 0;

	int mEditItem = -1;
	VDZHWND mhwndEdit = nullptr;
	void (*mPrevEditWndProc)() = nullptr;
	VDFunctionThunkInfo *mpEditWndProcThunk = nullptr;
	
	void (*mPrevWndProc)() = nullptr;
	VDFunctionThunkInfo *mpWndProcThunk = nullptr;
	VDFunctionThunkInfo *mpEditTimerThunk = nullptr;
	VDZUINT_PTR mAutoEditTimer = 0;

	vdfunction<void(int)> mpFnSelectionChanged;
	vdfunction<void(int)> mpFnItemDoubleClicked;
	vdfunction<void(int, const wchar_t *)> mpFnItemEdited;
	VDEvent<VDUIProxyListBoxControl, int> mSelectionChanged;
	VDEvent<VDUIProxyListBoxControl, int> mEventItemDoubleClicked;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyComboBoxControl : public VDUIProxyControl {
public:
	VDUIProxyComboBoxControl();
	~VDUIProxyComboBoxControl();

	void Clear();
	void AddItem(const wchar_t *s);

	int GetSelection() const;
	void SetSelection(int index);

	void SetOnSelectionChanged(vdfunction<void(int)> fn);

	VDEvent<VDUIProxyComboBoxControl, int>& OnSelectionChanged() {
		return mSelectionChanged;
	}

protected:
	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);

	VDEvent<VDUIProxyComboBoxControl, int> mSelectionChanged;
	vdfunction<void(int)> mpOnSelectionChangedFn;
};

/////////////////////////////////////////////////////////////////////////////

class IVDUITreeViewVirtualItem : public IVDRefUnknown {
public:
	virtual void GetText(VDStringW& s) const = 0;
};

class IVDUITreeViewVirtualItemComparer {
public:
	virtual int Compare(IVDUITreeViewVirtualItem& x, IVDUITreeViewVirtualItem& y) const = 0;
};

class IVDUITreeViewIndexedProvider {
public:
	virtual void GetText(uint32 id, VDStringW& s) const = 0;
};

class IVDUITreeViewIndexedItemComparer {
public:
	virtual int Compare(uint32 x, uint32 y) const = 0;
};

class VDUIProxyTreeViewControl final : public VDUIProxyControl {
public:
	typedef uintptr NodeRef;

	static const NodeRef kNodeRoot;
	static const NodeRef kNodeFirst;
	static const NodeRef kNodeLast;

	VDUIProxyTreeViewControl();
	~VDUIProxyTreeViewControl();

	virtual void Attach(VDZHWND hwnd);
	virtual void Detach();

	void SetIndexedProvider(IVDUITreeViewIndexedProvider *p);

	uint32 GetSelectedItemId() const;
	IVDUITreeViewVirtualItem *GetSelectedVirtualItem() const;

	template<class T>
	T *GetSelectedVirtualItem() const { return static_cast<T *>(GetSelectedVirtualItem()); }

	IVDUITreeViewVirtualItem *GetVirtualItem(NodeRef ref) const;
	uint32 GetItemId(NodeRef ref) const;

	void Clear();
	void DeleteItem(NodeRef ref);
	NodeRef AddItem(NodeRef parent, NodeRef insertAfter, const wchar_t *label);
	NodeRef AddVirtualItem(NodeRef parent, NodeRef insertAfter, IVDUITreeViewVirtualItem *item);
	NodeRef AddIndexedItem(NodeRef parent, NodeRef insertAfter, uint32 id);

	void MakeNodeVisible(NodeRef node);
	void SelectNode(NodeRef node);
	void RefreshNode(NodeRef node);
	void ExpandNode(NodeRef node, bool expanded);
	void EditNodeLabel(NodeRef node);
	bool HasChildren(NodeRef parent) const;
	void EnumChildren(NodeRef parent, const vdfunction<void(IVDUITreeViewVirtualItem *)>& callback);
	void EnumChildrenRecursive(NodeRef parent, const vdfunction<void(IVDUITreeViewVirtualItem *)>& callback);
	void SortChildren(NodeRef parent, IVDUITreeViewVirtualItemComparer& comparer);

	void InitImageList(uint32 n, uint32 width = 0, uint32 height = 0);
	void AddImage(const VDPixmap& px);
	void AddImages(uint32 n, const VDPixmap& px);
	void SetNodeImage(NodeRef node, uint32 imageIndex);

	void SetOnItemSelectionChanged(vdfunction<void()> fn);

	VDEvent<VDUIProxyTreeViewControl, int>& OnItemSelectionChanged() {
		return mEventItemSelectionChanged;
	}

	VDEvent<VDUIProxyTreeViewControl, bool *>& OnItemDoubleClicked() {
		return mEventItemDoubleClicked;
	}

	struct BeginEditEvent {
		NodeRef mNode;

		union {
			IVDUITreeViewVirtualItem *mpItem;
			uint32 mItemId;
		};

		bool mbAllowEdit;
		bool mbOverrideText;
		VDStringW mOverrideText;
	};

	VDEvent<VDUIProxyTreeViewControl, BeginEditEvent *>& OnItemBeginEdit() {
		return mEventItemBeginEdit;
	}

	struct EndEditEvent {
		NodeRef mNode;

		union {
			IVDUITreeViewVirtualItem *mpItem;
			uint32 mItemId;
		};

		const wchar_t *mpNewText;
	};

	VDEvent<VDUIProxyTreeViewControl, EndEditEvent *>& OnItemEndEdit() {
		return mEventItemEndEdit;
	}

	struct GetDispAttrEvent {
		union {
			IVDUITreeViewVirtualItem *mpItem;
			uint32 mItemId;
		};

		bool mbIsBold;
		bool mbIsMuted;
	};

	VDEvent<VDUIProxyTreeViewControl, GetDispAttrEvent *>& OnItemGetDisplayAttributes() {
		return mEventItemGetDisplayAttributes;
	}

	struct BeginDragEvent {
		NodeRef mNode;
		
		union {
			IVDUITreeViewVirtualItem *mpItem;
			uint32 mItemId;
		};

		vdpoint32 mPos;
	};

	void SetOnBeginDrag(const vdfunction<void(const BeginDragEvent& event)>& fn);
	NodeRef FindDropTarget() const;
	void SetDropTargetHighlight(NodeRef item);

protected:
	VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) override;
	void OnFontChanged() override;

	VDZLRESULT FixLabelEditWndProcA(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);
	VDZLRESULT FixLabelEditWndProcW(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);

	void DeleteFonts();

	int			mNextTextIndex;
	VDStringW mTextW[3];
	VDStringA mTextA[3];
	VDZHFONT	mhfontBold;
	bool		mbCreatedBoldFont;
	bool		mbIndexedMode;

	IVDUITreeViewIndexedProvider *mpIndexedProvider;

	void (*mPrevEditWndProc)();
	VDFunctionThunkInfo *mpEditWndProcThunk;

	VDEvent<VDUIProxyTreeViewControl, int> mEventItemSelectionChanged;
	VDEvent<VDUIProxyTreeViewControl, bool *> mEventItemDoubleClicked;
	VDEvent<VDUIProxyTreeViewControl, BeginEditEvent *> mEventItemBeginEdit;
	VDEvent<VDUIProxyTreeViewControl, EndEditEvent *> mEventItemEndEdit;
	VDEvent<VDUIProxyTreeViewControl, GetDispAttrEvent *> mEventItemGetDisplayAttributes;
	vdfunction<void()> mpOnItemSelectionChanged;
	vdfunction<void(const BeginDragEvent& event)> mpOnBeginDrag;

	VDZHIMAGELIST mImageList = nullptr;
	uint32 mImageWidth = 0;
	uint32 mImageHeight = 0;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyEditControl final : public VDUIProxyControl {
public:
	VDUIProxyEditControl();
	~VDUIProxyEditControl();

	VDStringW GetText() const;
	void SetText(const wchar_t *s);

	void SetOnTextChanged(vdfunction<void(VDUIProxyEditControl *)> fn);

private:
	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);

	vdfunction<void(VDUIProxyEditControl *)> mpOnTextChanged;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyRichEditControl final : public VDUIProxyControl {
public:
	VDUIProxyRichEditControl();
	~VDUIProxyRichEditControl();

	static void AppendEscapedRTF(VDStringA& buf, const wchar_t *str);

	bool IsSelectionPresent() const;

	void EnsureCaretVisible();

	void SelectAll();
	void Copy();

	void SetCaretPos(int lineIndex, int charIndex);

	void SetText(const wchar_t *s);
	void SetTextRTF(const char *s);
	void ReplaceSelectedText(const wchar_t *s);

	void SetFontFamily(const wchar_t *family);

	void SetBackgroundColor(uint32 c);
	void SetReadOnlyBackground();
	void SetPlainTextMode();
	void DisableCaret();
	void DisableSelectOnFocus();

	void SetOnTextChanged(vdfunction<void()> fn);
	void SetOnLinkSelected(vdfunction<bool(const wchar_t *)> fn);

	void UpdateMargins(sint32 xpad, sint32 ypad);

public:
	void Attach(VDZHWND hwnd) override;
	void Detach() override;

private:
	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) override;
	VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) override;
	static VDZLRESULT VDZCALLBACK StaticOnSubclassProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam, VDZUINT_PTR uIdSubclass, VDZDWORD_PTR dwRefData);
	VDZLRESULT OnSubclassProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam);

	void UpdateLinkEnableStatus();
	void InitSubclass();

	vdfunction<void()> mpOnTextChanged;
	vdfunction<bool(const wchar_t *)> mpOnLinkSelected;
	ITextDocument *mpTextDoc = nullptr;
	bool mSubclassed = false;
	bool mCaretDisabled = false;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyButtonControl final : public VDUIProxyControl {
public:
	VDUIProxyButtonControl();
	~VDUIProxyButtonControl();

	bool GetChecked() const;
	void SetChecked(bool enable);

	void SetOnClicked(vdfunction<void()> fn);

private:
	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam);

	vdfunction<void()> mpOnClicked;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxyToolbarControl final : public VDUIProxyControl {
public:
	VDUIProxyToolbarControl();
	~VDUIProxyToolbarControl();

	void Clear();

	void AddButton(uint32 id, sint32 imageIndex, const wchar_t *label);
	void AddDropdownButton(uint32 id, sint32 imageIndex, const wchar_t *label);
	void AddSeparator();

	void SetItemVisible(uint32 id, bool visible);
	void SetItemEnabled(uint32 id, bool visible);
	void SetItemPressed(uint32 id, bool visible);
	void SetItemText(uint32 id, const wchar_t *text);
	void SetItemImage(uint32 id, sint32 imageIndex);

	void InitImageList(uint32 n, uint32 width = 0, uint32 height = 0);
	void AddImage(const VDPixmap& px);
	void AddImages(uint32 n, const VDPixmap& px);

	void AutoSize();

	sint32 ShowDropDownMenu(uint32 id, const wchar_t *const *items);
	uint32 ShowDropDownMenu(uint32 id, VDZHMENU hmenu);

	void SetOnClicked(vdfunction<void(uint32)> fn);

public:
	void Attach(VDZHWND hwnd) override;
	void Detach() override;

private:
	VDZLRESULT On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) override;
	VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) override;

	vdfunction<void(uint32)> mpOnClicked;
	VDZHIMAGELIST mImageList = nullptr;
	uint32 mImageWidth = 0;
	uint32 mImageHeight = 0;
};

/////////////////////////////////////////////////////////////////////////////

class VDUIProxySysLinkControl final : public VDUIProxyControl {
public:
	VDUIProxySysLinkControl();
	~VDUIProxySysLinkControl();

	void SetOnClicked(vdfunction<void()> fn);

private:
	VDZLRESULT On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) override;

	vdfunction<void()> mpOnClicked;
};

#endif
