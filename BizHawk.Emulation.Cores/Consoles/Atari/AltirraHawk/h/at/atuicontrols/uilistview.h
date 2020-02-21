#ifndef f_AT_UILISTVIEW_H
#define f_AT_UILISTVIEW_H

#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include <vd2/VDDisplay/font.h>
#include <at/atui/uicontainer.h>

class ATUISlider;

class IATUIListViewVirtualItem : public IVDRefUnknown {
public:
	virtual void GetText(VDStringW& s) = 0;
};

class IATUIListViewSorter {
public:
	virtual bool Compare(IATUIListViewVirtualItem *a, IATUIListViewVirtualItem *b) const = 0;
};

struct ATUIListViewItem {
	VDStringW mText;
	vdrefptr<IATUIListViewVirtualItem> mpVirtualItem;
};

VDMOVE_CAPABLE(ATUIListViewItem);

class ATUIListView : public ATUIContainer {
public:
	enum {
		kActionMoveFirst = kActionCustom,
		kActionMoveLast,
		kActionMoveUp,
		kActionMoveDown,
		kActionMovePagePrev,
		kActionMovePageNext,
		kActionActivateItem
	};

	ATUIListView();
	~ATUIListView();

	void AddItem(const wchar_t *text);
	void AddItem(IATUIListViewVirtualItem *item);
	void InsertItem(sint32 pos, const wchar_t *text);
	void InsertItem(sint32 pos, IATUIListViewVirtualItem *text);
	void RemoveItem(sint32 pos);
	void RemoveAllItems();

	void Sort(const IATUIListViewSorter& item);

	IATUIListViewVirtualItem *GetSelectedVirtualItem();
	void SetSelectedItem(sint32 idx);
	void EnsureSelectedItemVisible(bool fullyVisible);

	void ScrollToPixel(sint32 py, bool updateSlider);

	vdfunction<void(sint32)>& OnItemSelectedEvent() { return mItemSelectedEvent; }
	vdfunction<void(sint32)>& OnItemActivatedEvent() { return mItemActivatedEvent; }

public:
	virtual void OnMouseDownL(sint32 x, sint32 y) override;
	virtual void OnMouseDblClkL(sint32 x, sint32 y) override;
	virtual bool OnMouseWheel(sint32 x, sint32 y, float delta) override;

	virtual void OnActionStart(uint32 id) override;
	virtual void OnActionRepeat(uint32 id) override;

	virtual void OnCreate() override;
	virtual void OnDestroy() override;
	virtual void OnSize() override;
	virtual void OnSetFocus() override;
	virtual void OnKillFocus() override;

	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

protected:
	void OnScroll(sint32 pos);
	void RecomputeSlider();

	sint32	mScrollY;
	sint32	mSelectedIndex;
	sint32	mItemHeight;
	float	mScrollAccum;

	uint32	mTextColor;
	uint32	mHighlightBackgroundColor;
	uint32	mHighlightTextColor;
	uint32	mInactiveHighlightBackgroundColor;

	VDStringW	mTempStr;

	vdrefptr<IVDDisplayFont> mpFont;
	vdvector<ATUIListViewItem> mItems;

	ATUISlider *mpSlider;

	vdfunction<void(sint32)> mItemSelectedEvent;
	vdfunction<void(sint32)> mItemActivatedEvent;
};

#endif
