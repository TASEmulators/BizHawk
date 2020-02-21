#ifndef f_AT2_UIMENULIST_H
#define f_AT2_UIMENULIST_H

#include <vd2/system/event.h>
#include <vd2/system/function.h>
#include <vd2/system/time.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include <vd2/VDDisplay/font.h>
#include <at/atui/uiwidget.h>

class IVDDisplayFont;
class ATUIMenu;

struct ATUIMenuItem {
	VDStringW mText;
	vdrefptr<ATUIMenu> mpSubMenu;
	uint32 mId;
	bool mbSeparator : 1;
	bool mbDisabled : 1;
	bool mbChecked : 1;
	bool mbRadioChecked : 1;

	ATUIMenuItem()
		: mId(0)
		, mbSeparator(false)
		, mbDisabled(false)
		, mbChecked(false)
		, mbRadioChecked(false)
	{
	}
};

VDMOVE_CAPABLE(ATUIMenuItem);

class ATUIMenu : public vdrefcount {
public:
	void AddItem(const ATUIMenuItem& item) { mItems.push_back(item); }
	void AddSeparator();

	void InsertItem(int pos, const ATUIMenuItem& item);
	
	void RemoveItems(uint32 start, uint32 n);
	void RemoveAllItems();

	uint32 GetItemCount() const { return (uint32)mItems.size(); }
	ATUIMenuItem *GetItemByIndex(uint32 i) { return &mItems[i]; }
	const ATUIMenuItem *GetItemByIndex(uint32 i) const { return &mItems[i]; }
	ATUIMenuItem *GetItemById(uint32 id, bool recurse);
	const ATUIMenuItem *GetItemById(uint32 id, bool recurse) const;

protected:
	typedef vdvector<ATUIMenuItem> Items;
	Items mItems;
};

class ATUIMenuList : public ATUIWidget, public IVDTimerCallback {
public:
	enum {
		kActionBarLeft = kActionCustom,
		kActionBarRight,
		kActionPopupUp,
		kActionPopupDown,
		kActionSelect,
		kActionBack,
		kActionClose,
		kActionActivate
	};

	ATUIMenuList();
	~ATUIMenuList();

	void SetAutoHide(bool en);
	void SetFont(IVDDisplayFont *font);
	void SetPopup(bool popup) { mbPopup = popup; }
	void SetMenu(ATUIMenu *menu);

	int GetItemFromPoint(sint32 x, sint32 y) const;

	void AutoSize();
	sint32 GetIdealHeight() const { return mIdealSize.h; }

	void Activate();
	void Deactivate();
	void MovePrev();
	void MoveNext();
	void CloseMenu();

	virtual void OnMouseMove(sint32 x, sint32 y);
	virtual void OnMouseDownL(sint32 x, sint32 y);
	virtual void OnMouseLeave();
	virtual void OnCaptureLost();

	virtual bool OnChar(const ATUICharEvent& event);

	virtual void OnActionStart(uint32 id);
	virtual void OnActionRepeat(uint32 id);

	virtual void OnCreate();

	vdfunction<void(ATUIMenuList *)>& OnActivatedEvent() { return mActivatedEvent; }
	vdfunction<void(ATUIMenuList *, uint32)>& OnItemSelected() { return mItemSelectedEvent; }

public:
	virtual void TimerCallback();

protected:
	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	virtual bool HandleMouseMove(sint32 x, sint32 y);
	virtual bool HandleMouseDownL(sint32 x, sint32 y, bool nested, uint32& itemSelected);

	ATUIMenuList *GetTail();
	void SetSelectedIndex(sint32 selIndex, bool immediate, bool deferredOpen = true);
	void OpenSubMenu();
	void CloseSubMenu();

	void Reflow();

	vdrefptr<IVDDisplayFont> mpFont;
	sint32 mSelectedIndex;
	bool mbPopup;
	bool mbActive;
	bool mbAutoHide;
	vdsize32	mIdealSize;
	uint32		mTextSplitX;
	uint32		mLeftMargin;
	uint32		mRightMargin;

	ATUIMenuList *mpRootList;
	vdrefptr<ATUIMenu> mpMenu;

	struct ItemInfo {
		VDStringW mLeftText;
		VDStringW mRightText;
		sint32 mUnderX1;
		sint32 mUnderX2;
		sint32 mUnderY;
		sint32 mPos;
		sint32 mSize;
		bool mbSelectable : 1;
		bool mbSeparator : 1;
		bool mbPopup : 1;
		bool mbDisabled : 1;
		bool mbChecked : 1;
		bool mbRadioChecked : 1;
	};

	typedef vdvector<ItemInfo> MenuItems;
	MenuItems mMenuItems;

	vdrefptr<ATUIMenuList> mpSubMenu;

	vdfunction<void(ATUIMenuList *)> mActivatedEvent;
	vdfunction<void(ATUIMenuList *, uint32)> mItemSelectedEvent;

	VDLazyTimer mSubMenuTimer;
};

#endif
