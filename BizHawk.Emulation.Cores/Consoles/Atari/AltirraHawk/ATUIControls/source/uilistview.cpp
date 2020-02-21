#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atui/uimanager.h>
#include <at/atuicontrols/uilistview.h>
#include <at/atuicontrols/uislider.h>

ATUIListView::ATUIListView()
	: mScrollY(0)
	, mSelectedIndex(-1)
	, mItemHeight(0)
	, mScrollAccum(0.0f)
	, mTextColor(0x000000)
	, mHighlightBackgroundColor(0x0A246A)
	, mHighlightTextColor(0xFFFFFF)
	, mInactiveHighlightBackgroundColor(0x808080)
	, mpSlider(NULL)
	, mItemSelectedEvent()
	, mItemActivatedEvent()
{
	mbFastClip = false;

	SetFillColor(0xFFFFFF);
	SetCursorImage(kATUICursorImage_Arrow);

	BindAction(kATUIVK_Up, kActionMoveUp);
	BindAction(kATUIVK_Down, kActionMoveDown);
	BindAction(kATUIVK_Home, kActionMoveFirst);
	BindAction(kATUIVK_End, kActionMoveLast);
	BindAction(kATUIVK_Prior, kActionMovePagePrev);
	BindAction(kATUIVK_Next, kActionMovePageNext);
}

ATUIListView::~ATUIListView() {
}

void ATUIListView::AddItem(const wchar_t *text) {
	InsertItem(0x7FFFFFFF, text);
}

void ATUIListView::AddItem(IATUIListViewVirtualItem *item) {
	InsertItem(0x7FFFFFFF, item);
}

void ATUIListView::InsertItem(sint32 pos, const wchar_t *text) {
	uint32 n = (uint32)mItems.size();

	if (pos < 0)
		pos = 0;
	else if ((uint32)pos > n)
		pos = n;

	ATUIListViewItem& item = *mItems.insert(mItems.begin() + pos, ATUIListViewItem());

	item.mText = text;

	if (mSelectedIndex >= pos)
		++mSelectedIndex;

	Invalidate();
	RecomputeSlider();
}

void ATUIListView::InsertItem(sint32 pos, IATUIListViewVirtualItem *vitem) {
	uint32 n = (uint32)mItems.size();

	if (pos < 0)
		pos = 0;
	else if ((uint32)pos > n)
		pos = n;

	ATUIListViewItem& item = *mItems.insert(mItems.begin() + pos, ATUIListViewItem());

	item.mpVirtualItem = vitem;

	if (mSelectedIndex >= pos)
		++mSelectedIndex;

	Invalidate();
	RecomputeSlider();
}

void ATUIListView::RemoveItem(sint32 pos) {
	uint32 n = (uint32)mItems.size();
	if ((uint32)pos < n) {
		mItems.erase(mItems.begin() + pos);

		const bool selDeleted = (pos == mSelectedIndex);

		if (mSelectedIndex >= pos) {
			if (mSelectedIndex > pos)
				--mSelectedIndex;

			if ((uint32)mSelectedIndex >= n)
				mSelectedIndex = -1;
		}

		RecomputeSlider();
		Invalidate();

		mpSlider->SetRange(0, (sint32)mItems.size());

		if (selDeleted) {
			if (mItemSelectedEvent)
				mItemSelectedEvent(mSelectedIndex);
		}
	}
}

void ATUIListView::RemoveAllItems() {
	if (!mItems.empty()) {
		mItems.clear();
		mSelectedIndex = -1;
		Invalidate();
		RecomputeSlider();
	}
}

namespace {
	struct LVItemSortAdapter {
		bool operator()(const ATUIListViewItem *a, const ATUIListViewItem *b) {
			return mpSorter->Compare(a->mpVirtualItem, b->mpVirtualItem);
		}

		const IATUIListViewSorter *mpSorter;
		VDStringW mTextA;
		VDStringW mTextB;
	};
}

void ATUIListView::Sort(const IATUIListViewSorter& sorter) {
	size_t n = mItems.size();
	vdfastvector<ATUIListViewItem *> ptrlist(n);

	for(size_t i=0; i<n; ++i)
		ptrlist[i] = &mItems[i];

	LVItemSortAdapter adapter;
	adapter.mpSorter = &sorter;
	std::sort(ptrlist.begin(), ptrlist.end(), adapter);

	vdvector<ATUIListViewItem> items2;
	items2.resize(n);

	ATUIListViewItem *selItem = mSelectedIndex < 0 ? NULL : &mItems[mSelectedIndex];
	sint32 newSelIndex = -1;

	bool redrawRequired = false;
	for(size_t i=0; i<n; ++i) {
		if (ptrlist[i] == selItem)
			newSelIndex = (sint32)i;

		if (ptrlist[i] != &mItems[i])
			redrawRequired = true;

		vdmove(items2[i], *ptrlist[i]);
	}

	mItems.swap(items2);

	if (redrawRequired) {
		mSelectedIndex = newSelIndex;
		Invalidate();

		EnsureSelectedItemVisible(true);
	}
}

IATUIListViewVirtualItem *ATUIListView::GetSelectedVirtualItem() {
	if (mSelectedIndex < 0)
		return NULL;

	return mItems[mSelectedIndex].mpVirtualItem;
}

void ATUIListView::SetSelectedItem(sint32 idx) {
	if (idx < 0)
		idx = -1;

	sint32 n = (sint32)mItems.size();
	if (idx >= n)
		idx = n - 1;

	if (mSelectedIndex != idx) {
		mSelectedIndex = idx;
		Invalidate();

		if (mItemSelectedEvent)
			mItemSelectedEvent(idx);
	}
}

void ATUIListView::EnsureSelectedItemVisible(bool fullyVisible) {
	if (mSelectedIndex < 0)
		return;

	sint32 itemTop = mSelectedIndex * mItemHeight;
	sint32 itemBottom = itemTop + mItemHeight;
	sint32 scrollThreshold1 = fullyVisible ? itemTop : itemBottom;

	if (mScrollY > scrollThreshold1) {
		ScrollToPixel(scrollThreshold1, true);
		return;
	}

	sint32 scrollThreshold2 = (fullyVisible ? itemBottom : itemTop) - mClientArea.height();
	if (mScrollY < scrollThreshold2) {
		ScrollToPixel(scrollThreshold2, true);
		return;
	}
}

void ATUIListView::ScrollToPixel(sint32 py, bool updateSlider) {
	sint32 h = mClientArea.height();
	h -= h % mItemHeight;

	if (!h)
		h = mItemHeight;

	sint32 maxScroll = (sint32)mItems.size() * mItemHeight - h;

	if (py > maxScroll)
		py = maxScroll;

	if (py < 0)
		py = 0;

	if (mScrollY != py) {
		mScrollY = py;
		Invalidate();

		if (updateSlider && mpSlider)
			mpSlider->SetPos(mScrollY);
	}
}

void ATUIListView::OnMouseDownL(sint32 x, sint32 y) {
	Focus();

	sint32 idx = (mScrollY + y) / (sint32)mItemHeight;

	if (idx >= (sint32)mItems.size())
		idx = -1;

	SetSelectedItem(idx);
}

void ATUIListView::OnMouseDblClkL(sint32 x, sint32 y) {
	Focus();

	sint32 idx = (mScrollY + y) / (sint32)mItemHeight;

	if (idx >= (sint32)mItems.size())
		idx = -1;

	if (idx >= 0 && idx == mSelectedIndex) {
		if (mItemActivatedEvent)
			mItemActivatedEvent(idx);
	} else {
		SetSelectedItem(idx);
	}
}

bool ATUIListView::OnMouseWheel(sint32 x, sint32 y, float delta) {
	mScrollAccum += delta * (float)(sint32)mItemHeight;

	int pixels = VDRoundToInt(mScrollAccum);

	if (pixels) {
		mScrollAccum -= (float)pixels;

		ScrollToPixel(mScrollY - pixels, true);
	}

	return true;
}

void ATUIListView::OnActionStart(uint32 id) {
	switch(id) {
		case kActionActivateItem:
			if (mSelectedIndex >= 0) {
				if (mItemActivatedEvent)
					mItemActivatedEvent(mSelectedIndex);
			}
			return;
	}

	if (id >= kActionCustom)
		return OnActionRepeat(id);

	return ATUIWidget::OnActionStart(id);
}

void ATUIListView::OnActionRepeat(uint32 id) {
	switch(id) {
		case kActionMoveUp:
			SetSelectedItem(mSelectedIndex > 0 ? mSelectedIndex - 1 : 0);
			EnsureSelectedItemVisible(true);
			break;

		case kActionMoveDown:
			SetSelectedItem(mSelectedIndex + 1);
			EnsureSelectedItemVisible(true);
			break;

		case kActionMoveFirst:
			SetSelectedItem(0);
			EnsureSelectedItemVisible(true);
			break;

		case kActionMoveLast:
			SetSelectedItem((sint32)mItems.size() - 1);
			EnsureSelectedItemVisible(true);
			break;

		case kActionMovePagePrev:
			{
				sint32 h = mClientArea.height();
				sint32 pageItems = (sint32)(h / mItemHeight);
				ScrollToPixel(mScrollY - pageItems * mItemHeight, true);
				SetSelectedItem(mSelectedIndex >= pageItems ? mSelectedIndex - pageItems : 0);
				EnsureSelectedItemVisible(true);
			}
			break;

		case kActionMovePageNext:
			{
				sint32 h = mClientArea.height();
				sint32 pageItems = (sint32)(h / mItemHeight);
				ScrollToPixel(mScrollY + pageItems * mItemHeight, true);
				SetSelectedItem(mSelectedIndex < 0 ? 0 : mSelectedIndex + pageItems);
				EnsureSelectedItemVisible(true);
			}
			break;

		default:
			return ATUIWidget::OnActionRepeat(id);
	}
}

void ATUIListView::OnCreate() {
	ATUIContainer::OnCreate();

	mpSlider = new ATUISlider;
	mpSlider->AddRef();
	AddChild(mpSlider);
	mpSlider->SetArea(vdrect32(0, 0, mpManager->GetSystemMetrics().mVertSliderWidth, 16));
	mpSlider->SetDockMode(kATUIDockMode_Right);
	mpSlider->SetOnValueChanged([this](sint32 pos) { OnScroll(pos); });

	mpFont = mpManager->GetThemeFont(kATUIThemeFont_Default);

	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);
	mItemHeight = metrics.mAscent + metrics.mDescent + 4;
}

void ATUIListView::OnDestroy() {
	vdsaferelease <<= mpSlider;

	ATUIContainer::OnDestroy();
}

void ATUIListView::OnSize() {
	ATUIContainer::OnSize();

	RecomputeSlider();
}

void ATUIListView::OnSetFocus() {
	if (mSelectedIndex >= 0)
		Invalidate();
}

void ATUIListView::OnKillFocus() {
	if (mSelectedIndex >= 0)
		Invalidate();
}

void ATUIListView::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	VDDisplayTextRenderer *tr = rdr.GetTextRenderer();

	uint32 n = (uint32)mItems.size();
	sint32 y = -mScrollY;

	tr->SetFont(mpFont);
	tr->SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);
	
	for(uint32 i=0; i<n; ++i) {
		const ATUIListViewItem& lv = mItems[i];
		uint32 textColor = mTextColor;

		if (mSelectedIndex == (sint32)i) {
			textColor = mHighlightTextColor;
			rdr.SetColorRGB(HasFocus() ? mHighlightBackgroundColor : mInactiveHighlightBackgroundColor);
			rdr.FillRect(0, y, w, mItemHeight);
		}

		if (rdr.PushViewport(vdrect32(2, y+2, w-2, y+mItemHeight-2), 2, y+2)) {
			tr->SetColorRGB(textColor);

			if (lv.mpVirtualItem) {
				mTempStr.clear();
				lv.mpVirtualItem->GetText(mTempStr);
				tr->DrawTextLine(0, 0, mTempStr.c_str());
			} else {
				tr->DrawTextLine(0, 0, lv.mText.c_str());
			}

			rdr.PopViewport();
		}

		y += mItemHeight;
	}

	ATUIContainer::Paint(rdr, w, h);
}

void ATUIListView::OnScroll(sint32 pos) {
	ScrollToPixel(pos, false);
}

void ATUIListView::RecomputeSlider() {
	if (!mpSlider)
		return;

	const sint32 pageItemCount = mClientArea.height() / mItemHeight;
	const sint32 scrollMax = ((sint32)mItems.size() - pageItemCount) * mItemHeight;

	mpSlider->SetLineSize(mItemHeight);
	mpSlider->SetPageSize(pageItemCount * mItemHeight);
	mpSlider->SetRange(0, scrollMax);
}
