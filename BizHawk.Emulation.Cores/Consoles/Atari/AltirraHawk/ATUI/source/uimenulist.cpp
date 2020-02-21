#include <stdafx.h>
#include <windows.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <at/atui/uimanager.h>
#include <at/atui/uimenulist.h>
#include <at/atui/uicontainer.h>

template<>
void vdmove<ATUIMenuItem>(ATUIMenuItem& dst, ATUIMenuItem& src) {
	vdmove(dst.mText, src.mText);
	dst.mpSubMenu.from(src.mpSubMenu);
	dst.mbSeparator = src.mbSeparator;
	dst.mbDisabled = src.mbDisabled;
}

namespace {
	void DrawBevel(IVDDisplayRenderer& rdr, const vdrect32& r, uint32 tlColor, uint32 brColor) {
		vdpoint32 pts[5] = {
			vdpoint32(r.right-1, r.top),
			vdpoint32(r.left, r.top),
			vdpoint32(r.left, r.bottom-1),
			vdpoint32(r.right-1, r.bottom-1),
			vdpoint32(r.right-1, r.top),
		};

		rdr.SetColorRGB(tlColor);
		rdr.PolyLine(pts, 2);
		rdr.SetColorRGB(brColor);
		rdr.PolyLine(pts+2, 2);
	}

	void DrawThin3DRect(IVDDisplayRenderer& rdr, const vdrect32& r, bool depressed) {
		DrawBevel(rdr, vdrect32(r.left+1, r.top+1, r.right-1, r.bottom-1), depressed ? 0x404040 : 0xFFFFFF, depressed ? 0xFFFFFF : 0x404040);
	}

	void Draw3DRect(IVDDisplayRenderer& rdr, const vdrect32& r, bool depressed) {
		DrawBevel(rdr, r, depressed ? 0x404040 : 0xD4D0C8, depressed ? 0xD4D0C8 : 0x404040);
		DrawBevel(rdr, vdrect32(r.left+1, r.top+1, r.right-1, r.bottom-1), depressed ? 0x404040 : 0xFFFFFF, depressed ? 0xFFFFFF : 0x404040);
	}

	uint32 GetMenuDelay() {
		DWORD delay = 250;
		::SystemParametersInfo(SPI_GETMENUSHOWDELAY, 0, &delay, FALSE);

		return delay;
	}
}

void ATUIMenu::AddSeparator() {
	ATUIMenuItem item = ATUIMenuItem();

	item.mbSeparator = true;

	AddItem(item);
}

void ATUIMenu::InsertItem(int pos, const ATUIMenuItem& item) {
	const size_t n = mItems.size();
		
	if (pos < 0 || pos > (int)n)
		pos = (int)n;

	mItems.insert(mItems.begin() + pos, item);
}

void ATUIMenu::RemoveItems(uint32 start, uint32 n) {
	size_t existingCount = mItems.size();

	if (start >= existingCount)
		return;

	if (n > existingCount - start)
		n = (uint32)(existingCount - start);

	mItems.erase(mItems.begin() + start, mItems.begin() + (start + n));
}

void ATUIMenu::RemoveAllItems() {
	mItems.clear();
}

ATUIMenuItem *ATUIMenu::GetItemById(uint32 id, bool recurse) {
	return const_cast<ATUIMenuItem *>(const_cast<const ATUIMenu *>(this)->GetItemById(id, recurse));
}

const ATUIMenuItem *ATUIMenu::GetItemById(uint32 id, bool recurse) const {
	for(Items::const_iterator it(mItems.begin()), itEnd(mItems.end());
		it != itEnd;
		++it)
	{
		const ATUIMenuItem& item = *it;

		if (item.mId == id)
			return &item;

		if (recurse && item.mpSubMenu) {
			const ATUIMenuItem *result = item.mpSubMenu->GetItemById(id, true);

			if (result)
				return result;
		}
	}

	return NULL;
}

ATUIMenuList::ATUIMenuList()
	: mSelectedIndex(-1)
	, mbPopup(false)
	, mbActive(false)
	, mbAutoHide(false)
	, mpRootList(NULL)
	, mItemSelectedEvent()
{
	SetFillColor(0xD4D0C8);
	SetCursorImage(kATUICursorImage_Arrow);

	BindAction(kATUIVK_Left, kActionBarLeft);
	BindAction(kATUIVK_Right, kActionBarRight);
	BindAction(kATUIVK_Up, kActionPopupUp);
	BindAction(kATUIVK_Down, kActionPopupDown);
	BindAction(kATUIVK_Return, kActionSelect);

	BindAction(kATUIVK_UILeft, kActionBarLeft);
	BindAction(kATUIVK_UIRight, kActionBarRight);
	BindAction(kATUIVK_UIUp, kActionPopupUp);
	BindAction(kATUIVK_UIDown, kActionPopupDown);
	BindAction(kATUIVK_UIAccept, kActionSelect);
	BindAction(kATUIVK_UIMenu, kActionClose);
	BindAction(kATUIVK_UIReject, kActionBack);
}

ATUIMenuList::~ATUIMenuList() {
}

void ATUIMenuList::SetAutoHide(bool en) {
	if (mbAutoHide == en)
		return;

	mbAutoHide = en;

	if (en) {
		CloseSubMenu();
		ReleaseCursor();
		mSelectedIndex = -1;

		if (mpParent)
			mpParent->Focus();

		mbFastClip = true;
		SetAlphaFillColor(0);
		Invalidate();

		VDASSERT(!mpSubMenu);
	}
}

void ATUIMenuList::SetFont(IVDDisplayFont *font) {
	if (mpFont != font) {
		mpFont = font;

		Reflow();
		Invalidate();
	}
}

void ATUIMenuList::SetMenu(ATUIMenu *menu) {
	if (mpMenu == menu)
		return;

	mpMenu = menu;
	Reflow();
	Invalidate();
}

int ATUIMenuList::GetItemFromPoint(sint32 x, sint32 y) const {
	sint32 index = 0;

	for(MenuItems::const_iterator it(mMenuItems.begin()), itEnd(mMenuItems.end());
		it != itEnd;
		++it, ++index)
	{
		const ItemInfo& item = *it;

		if (!item.mbSelectable)
			continue;

		if (mbPopup) {
			if (y >= item.mPos && y < item.mPos + item.mSize)
				return index;
		} else {
			if (x >= item.mPos && x < item.mPos + item.mSize)
				return index;
		}
	}

	return -1;
}

void ATUIMenuList::AutoSize() {
	SetArea(vdrect32(mArea.left, mArea.top, mArea.left + mIdealSize.w, mArea.top + mIdealSize.h));
}

void ATUIMenuList::Activate() {
	if (!mbActive) {
		mbActive = true;

		mbFastClip = false;
		SetFillColor(0xD4D0C8);
		Invalidate();

		SetSelectedIndex(0, true);
		if (mpSubMenu)
			MoveNext();
	}

	Focus();
}

void ATUIMenuList::MovePrev() {
	ATUIMenuList *p = GetTail();
	int n = (int)p->mpMenu->GetItemCount();

	if (!n)
		return;

	int idx0 = p->mSelectedIndex;
	if (idx0 < 0)
		idx0 = 0;

	int idx = idx0;
	for(;;) {
		if (--idx < 0)
			idx = n - 1;

		ATUIMenuItem *item = p->mpMenu->GetItemByIndex(idx);
		if (!item->mbSeparator && !item->mbDisabled)
			break;
	}

	p->SetSelectedIndex(idx, p == this, false);

	if (p == this && mpSubMenu)
		MoveNext();
}

void ATUIMenuList::MoveNext() {
	ATUIMenuList *p = GetTail();
	int n = (int)p->mpMenu->GetItemCount();

	if (!n)
		return;

	int idx0 = p->mSelectedIndex;
	if (idx0 < 0)
		idx0 = n - 1;

	int idx = idx0;
	for(;;) {
		if (++idx >= n)
			idx = 0;

		ATUIMenuItem *item = p->mpMenu->GetItemByIndex(idx);
		if (!item->mbSeparator && !item->mbDisabled)
			break;
	}

	p->SetSelectedIndex(idx, p == this, false);

	if (p == this && mpSubMenu)
		MoveNext();
}

void ATUIMenuList::CloseMenu() {
	if (!mpRootList) {
		SetSelectedIndex(-1, true);
		CloseSubMenu();

		if (mbActive) {
			mbActive = false;

			ReleaseCursor();
		}
	}
}

void ATUIMenuList::OnMouseMove(sint32 x, sint32 y) {
	if (mbAutoHide && mbFastClip) {
		mbFastClip = false;
		SetFillColor(0xD4D0C8);
		Invalidate();
	}

	HandleMouseMove(x, y);
}

void ATUIMenuList::OnMouseDownL(sint32 x, sint32 y) {
	uint32 itemSelected = 0;

	Focus();

	if (!HandleMouseDownL(x, y, false, itemSelected)) {
		if (mbActive) {
			mbActive = false;
			CloseSubMenu();
			ReleaseCursor();
			mSelectedIndex = -1;
		}
	}

	if (itemSelected) {
		CloseMenu();

		if (mItemSelectedEvent)
			mItemSelectedEvent(this, itemSelected);
	}
}

void ATUIMenuList::OnMouseLeave() {
	mbActive = false;
	CloseSubMenu();

	if (mpParent)
		mpParent->Focus();

	if (mbAutoHide && !mbFastClip) {
		mbFastClip = true;
		SetAlphaFillColor(0);
		Invalidate();
	}
}

void ATUIMenuList::OnCaptureLost() {
	OnMouseLeave();
}

bool ATUIMenuList::OnChar(const ATUICharEvent& event) {
	return false;
}

void ATUIMenuList::OnActionStart(uint32 id) {
	switch(id) {
		case kActionSelect:
			{
				ATUIMenuList *p = GetTail();

				if (p->mSelectedIndex >= 0) {
					ATUIMenuItem *item = p->mpMenu->GetItemByIndex(p->mSelectedIndex);

					if (item->mpSubMenu) {
						p->OpenSubMenu();

						if (p->mpSubMenu)
							MoveNext();
					} else {
						OnMouseLeave();

						if (mItemSelectedEvent)
							mItemSelectedEvent(this, item->mId);
					}
				}
			}
			break;

		case kActionBack:
			if (mpSubMenu && mpSubMenu->mpSubMenu) {
				ATUIMenuList *p = this;

				while(p->mpSubMenu->mpSubMenu)
					p = p->mpSubMenu;

				p->CloseSubMenu();
			} else
				OnMouseLeave();
			break;

		case kActionClose:
			OnMouseLeave();
			break;

		case kActionActivate:
			if (mbVisible)
				Activate();
			break;

		default:
			if (id >= kActionCustom)
				OnActionRepeat(id);

			return ATUIWidget::OnActionStart(id);
	}
}

void ATUIMenuList::OnActionRepeat(uint32 id) {
	switch(id) {
		case kActionBarLeft:
			if (mpSubMenu)
				CloseSubMenu();

			if (!GetTail()->mbPopup)
				MovePrev();
			break;

		case kActionBarRight:
			if (mpSubMenu)
				CloseSubMenu();

			if (!GetTail()->mbPopup)
				MoveNext();
			break;

		case kActionPopupUp:
			if (GetTail()->mbPopup)
				MovePrev();
			break;

		case kActionPopupDown:
			if (GetTail()->mbPopup)
				MoveNext();
			break;

		default:
			return ATUIWidget::OnActionRepeat(id);
	}
}

void ATUIMenuList::OnCreate() {
	Reflow();
}

void ATUIMenuList::TimerCallback() {
	CloseSubMenu();

	if (mSelectedIndex >= 0 && mbActive && mMenuItems[mSelectedIndex].mbPopup)
		OpenSubMenu();
}

void ATUIMenuList::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	if (mbFastClip)
		return;

	VDDisplayTextRenderer& tr = *rdr.GetTextRenderer();
	tr.SetFont(mpFont);
	tr.SetAlignment(VDDisplayTextRenderer::kAlignLeft, VDDisplayTextRenderer::kVertAlignTop);

	if (mbPopup) {
		Draw3DRect(rdr, vdrect32(0, 0, w, h), false);

		if (mSelectedIndex >= 0) {
			const ItemInfo& selItem = mMenuItems[mSelectedIndex];

			if (selItem.mbSelectable) {
				rdr.SetColorRGB(0x0A246A);
				rdr.FillRect(3, selItem.mPos, mArea.width()-6, selItem.mSize);
			}
		}

		int index = 0;
		for(MenuItems::const_iterator it(mMenuItems.begin()), itEnd(mMenuItems.end());
			it != itEnd;
			++it, ++index)
		{
			const ItemInfo& info = *it;

			uint32 color = (index == mSelectedIndex ? 0xFFFFFF : info.mbDisabled ? 0x606060 : 0);
			tr.SetColorRGB(color);
			tr.SetPosition(3 + mLeftMargin, info.mPos + 2);

			if (info.mbSeparator)
				DrawThin3DRect(rdr, vdrect32(3, info.mPos, w-3, info.mPos + info.mSize), true);
			else {
				if (!info.mLeftText.empty())
					tr.DrawTextSpan(info.mLeftText.data(), info.mLeftText.size());

				if (!info.mRightText.empty()) {
					tr.SetPosition(mTextSplitX, info.mPos + 2);
					tr.DrawTextSpan(info.mRightText.data(), info.mRightText.size());
				}

				if (info.mUnderX1 < info.mUnderX2) {
					rdr.SetColorRGB(color);
					rdr.FillRect(info.mUnderX1, info.mUnderY, info.mUnderX2 - info.mUnderX1, 1);
				}

				if (info.mbRadioChecked) {
					ATUIStockImage& radioImage = mpManager->GetStockImage(kATUIStockImageIdx_MenuRadio);
					VDDisplayBlt blt;
					blt.mDestX = 3;
					blt.mDestY = info.mPos + 2;
					blt.mSrcX = 0;
					blt.mSrcY = 0;
					blt.mWidth = radioImage.mWidth;
					blt.mHeight = radioImage.mHeight;
					rdr.MultiBlt(&blt, 1, radioImage.mImageView, IVDDisplayRenderer::kBltMode_Color);
				}

				if (info.mbChecked) {
					ATUIStockImage& checkImage = mpManager->GetStockImage(kATUIStockImageIdx_MenuCheck);
					VDDisplayBlt blt;
					blt.mDestX = 3;
					blt.mDestY = info.mPos + 2;
					blt.mSrcX = 0;
					blt.mSrcY = 0;
					blt.mWidth = checkImage.mWidth;
					blt.mHeight = checkImage.mHeight;
					rdr.MultiBlt(&blt, 1, checkImage.mImageView, IVDDisplayRenderer::kBltMode_Color);
				}

				if (info.mbPopup) {
					ATUIStockImage& popupImage = mpManager->GetStockImage(kATUIStockImageIdx_MenuArrow);
					VDDisplayBlt blt;
					blt.mDestX = mArea.width() - mRightMargin - 3;
					blt.mDestY = info.mPos + 2;
					blt.mSrcX = 0;
					blt.mSrcY = 0;
					blt.mWidth = popupImage.mWidth;
					blt.mHeight = popupImage.mHeight;
					rdr.MultiBlt(&blt, 1, popupImage.mImageView, IVDDisplayRenderer::kBltMode_Color);
				}
			}
		}
	} else {
		if (mSelectedIndex >= 0) {
			const ItemInfo& selItem = mMenuItems[mSelectedIndex];
			vdrect32 r;

			r.left = selItem.mPos;
			r.top = mArea.top;
			r.right = selItem.mPos + selItem.mSize;
			r.bottom = mArea.bottom;

			DrawThin3DRect(rdr, r, mbActive);
		}

		int index = 0;
		for(MenuItems::const_iterator it(mMenuItems.begin()), itEnd(mMenuItems.end());
			it != itEnd;
			++it, ++index)
		{
			const ItemInfo& info = *it;
			const uint32 color = info.mbDisabled ? 0x606060 : 0;

			tr.SetColorRGB(color);
			tr.SetPosition(info.mPos + 8, 3);

			tr.DrawTextSpan(info.mLeftText.data(), info.mLeftText.size());

			if (info.mUnderX1 < info.mUnderX2) {
				rdr.SetColorRGB(color);
				rdr.FillRect(info.mUnderX1, info.mUnderY, info.mUnderX2 - info.mUnderX1, 1);
			}
		}
	}
}

bool ATUIMenuList::HandleMouseMove(sint32 x, sint32 y) {
	if (mpSubMenu) {
		const vdrect32& subMenuArea = mpSubMenu->GetArea();

		if (mpSubMenu->HandleMouseMove(x - subMenuArea.left + mArea.left, y - subMenuArea.top + mArea.top))
			return true;
	}

	if ((uint32)x >= (uint32)mArea.width() || (uint32)y >= (uint32)mArea.height())
		return false;

	sint32 selIndex = GetItemFromPoint(x, y);

	if (mbPopup || selIndex >= 0 || !mbActive)
		SetSelectedIndex(selIndex, !mbPopup);

	return true;
}

bool ATUIMenuList::HandleMouseDownL(sint32 x, sint32 y, bool nested, uint32& itemSelected) {
	if (mpSubMenu) {
		const vdrect32& subMenuArea = mpSubMenu->GetArea();

		if (mpSubMenu->HandleMouseDownL(x - subMenuArea.left + mArea.left, y - subMenuArea.top + mArea.top, true, itemSelected))
			return true;
	}

	if ((uint32)x >= (uint32)mArea.width() || (uint32)y >= (uint32)mArea.height())
		return false;

	sint32 selIndex = GetItemFromPoint(x, y);

	if (!mbPopup)
		Focus();

	if (mbPopup) {
		SetSelectedIndex(selIndex, true);

		if (selIndex >= 0) {
			const ATUIMenuItem *item = mpMenu->GetItemByIndex(selIndex);

			if (item && !item->mpSubMenu)
				itemSelected = item->mId;
		}
	} else {
		// For a menu bar, we switch the current item or toggle it if it's the same item. Clicking
		// outside of an item closes any currently open submenu.

		bool active;
		
		if (selIndex == mSelectedIndex) {
			if (mpSubMenu)
				CloseSubMenu();
			else if (selIndex >= 0)
				OpenSubMenu();

			active = (mpSubMenu != NULL);
		} else {
			active = (selIndex >= 0);

			SetSelectedIndex(selIndex, true);

			if (selIndex >= 0) {
				CloseSubMenu();
				OpenSubMenu();
			}
		}

		if (mbActive != active) {
			mbActive = active;

			if (active) {
				if (mActivatedEvent)
					mActivatedEvent(this);

				CaptureCursor();
			} else
				ReleaseCursor();
		}
	}

	return true;
}

ATUIMenuList *ATUIMenuList::GetTail() {
	ATUIMenuList *p = this;

	while(p->mpSubMenu)
		p = p->mpSubMenu;

	return p;
}

void ATUIMenuList::SetSelectedIndex(sint32 selIndex, bool immediate, bool deferredOpen) {
	if (mSelectedIndex != selIndex) {
		mSelectedIndex = selIndex;

		mSubMenuTimer.Stop();

		if (immediate)
			TimerCallback();
		else if (deferredOpen)
			mSubMenuTimer.SetOneShot(this, GetMenuDelay());

		Invalidate();
	}
}

void ATUIMenuList::OpenSubMenu() {
	VDASSERT(!mbFastClip);
	VDASSERT(!mpSubMenu && mSelectedIndex >= 0);

	const sint32 itemPos = mMenuItems[mSelectedIndex].mPos;

	mpSubMenu = new ATUIMenuList;
	mpSubMenu->mbActive = true;
	mpSubMenu->mpRootList = mpRootList ? mpRootList : this;
	mpSubMenu->SetPopup(true);
	mpSubMenu->SetFont(mpFont);

	const ATUIMenuItem *item = mpMenu->GetItemByIndex(mSelectedIndex);

	if (item)
		mpSubMenu->SetMenu(item->mpSubMenu);

	vdrect32 r(mArea);

	if (mbPopup) {
		r.left = r.right - 8;
		r.right = r.left + 150;
		r.top += itemPos - 3;
		r.bottom = r.top + 300;
	} else {
		r.left += itemPos;
		r.right = r.left + 150;
		r.top = r.bottom - 1;
		r.bottom = r.top + 300;
	}

	mpParent->AddChild(mpSubMenu);

	mpSubMenu->SetArea(r);
	mpSubMenu->AutoSize();
}

void ATUIMenuList::CloseSubMenu() {
	if (mpSubMenu) {
		mpSubMenu->CloseSubMenu();
		mpSubMenu->Destroy();
		mpSubMenu.clear();
	}
}

void ATUIMenuList::Reflow() {
	if (!mpFont || !mpManager)
		return;

	sint32 pos = mbPopup ? 3 : 0;

	uint32 n = mpMenu ? mpMenu->GetItemCount() : 0;

	mMenuItems.clear();
	mMenuItems.resize(n);

	sint32 maxItemWidthLeft = 0;
	sint32 maxItemWidthRight = 0;

	VDDisplayFontMetrics metrics;
	mpFont->GetMetrics(metrics);

	mLeftMargin = mpManager->GetStockImage(kATUIStockImageIdx_MenuCheck).mWidth;
	mRightMargin = mpManager->GetStockImage(kATUIStockImageIdx_MenuArrow).mWidth;

	for(uint32 i=0; i<n; ++i) {
		ItemInfo& info = mMenuItems[i];
		const ATUIMenuItem *item = mpMenu->GetItemByIndex(i);

		vdsize32 sizeLeft(0, 0);
		vdsize32 sizeRight(0, 0);
		
		info.mUnderX1 = 0;
		info.mUnderX2 = 0;
		info.mUnderY = 0;

		if (!item->mbSeparator) {
			const wchar_t *s = item->mText.c_str();
			const wchar_t *split = wcschr(s, L'\t');
			size_t leftLen;

			if (split) {
				leftLen = split - s;
				info.mRightText = split+1;
				sizeRight = mpFont->MeasureString(info.mRightText.c_str(), info.mRightText.size(), false);
				sizeRight.w += 12;
			} else
				leftLen = wcslen(s);

			info.mLeftText.assign(s, s+leftLen);

			VDStringW::size_type prefixPos = info.mLeftText.find(L'&');

			if (prefixPos != VDStringW::npos) {
				info.mUnderX1 = mpFont->MeasureString(s, prefixPos, false).w;
				info.mUnderX2 = info.mUnderX1 + mpFont->MeasureString(s + prefixPos + 1, 1, false).w;
				info.mUnderY = metrics.mAscent + metrics.mDescent;

				if (mbPopup) {
					info.mUnderX1 += 3 + mLeftMargin;
					info.mUnderX2 += 3 + mLeftMargin;
					info.mUnderY += pos + 1;
				} else {
					info.mUnderX1 += pos + 8;
					info.mUnderX2 += pos + 8;
					info.mUnderY += 2;
				}

				info.mLeftText.erase(prefixPos, 1);
			}

			sizeLeft = mpFont->MeasureString(info.mLeftText.data(), info.mLeftText.size(), false);
		}

		info.mPos = pos;

		if (mbPopup) {
			info.mSize = std::max<sint32>(sizeLeft.h, sizeRight.h) + 4;

			sint32 wl = sizeLeft.w + mLeftMargin;
			sint32 wr = sizeRight.w + mRightMargin;

			if (maxItemWidthLeft < wl)
				maxItemWidthLeft = wl;

			if (maxItemWidthRight < wr)
				maxItemWidthRight = wr;
		} else
			info.mSize = sizeLeft.w + 16;

		info.mbPopup = item->mpSubMenu != NULL;
		info.mbDisabled = item->mbDisabled;
		info.mbSelectable = !item->mbDisabled && !item->mbSeparator;
		info.mbSeparator = item->mbSeparator;
		info.mbChecked = item->mbChecked;
		info.mbRadioChecked = item->mbRadioChecked;

		pos += info.mSize;
	}

	mTextSplitX = 0;

	if (mbPopup) {
		mIdealSize = vdsize32(maxItemWidthLeft + maxItemWidthRight + 6, pos + 6);

		mTextSplitX = maxItemWidthLeft + 6 + 12;
	} else {
		mIdealSize = vdsize32(0, metrics.mAscent + metrics.mDescent + 4);
	}
}
