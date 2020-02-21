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
#include <at/atui/uicontainer.h>
#include <at/atui/uimanager.h>
#include <at/atui/uianchor.h>

ATUIContainer::ATUIContainer()
	: mbLayoutInvalid(false)
	, mbDescendantLayoutInvalid(false)
{
	mbFastClip = true;
	SetAlphaFillColor(0);
}

ATUIContainer::~ATUIContainer() {
	RemoveAllChildren();
}

void ATUIContainer::AddChild(ATUIWidget *w) {
	w->AddRef();
	w->SetParent(mpManager, this);

	mWidgets.push_back(w);

	if (w->IsVisible())
		Invalidate();

	InvalidateLayout();
}

void ATUIContainer::RemoveChild(ATUIWidget *w) {
	for(Widgets::iterator it(mWidgets.begin()), itEnd(mWidgets.end());
		it != itEnd;
		++it)
	{
		if (*it == w) {
			mWidgets.erase(it);
			w->SetParent(NULL, NULL);
			w->Release();
			InvalidateLayout();
			break;
		}
	}
}

void ATUIContainer::RemoveAllChildren() {
	while(!mWidgets.empty()) {
		ATUIWidget *w = mWidgets.back();

		mWidgets.pop_back();

		w->SetParent(NULL, NULL);
		w->Release();
	}
}

void ATUIContainer::SendToBack(ATUIWidget *w) {
	if (!w)
		return;

	if (w->GetParent() != this) {
		VDASSERT(!"Invalid call to SendToBack().");
		return;
	}

	auto it = std::find(mWidgets.begin(), mWidgets.end(), w);
	VDASSERT(it != mWidgets.end());

	if (it != mWidgets.begin()) {
		mWidgets.erase(it);
		mWidgets.insert(mWidgets.begin(), w);
	}
}

void ATUIContainer::BringToFront(ATUIWidget *w) {
	if (!w)
		return;

	if (w->GetParent() != this) {
		VDASSERT(!"Invalid call to SendToBack().");
		return;
	}

	auto it = std::find(mWidgets.begin(), mWidgets.end(), w);
	VDASSERT(it != mWidgets.end());

	if (it != mWidgets.end() - 1) {
		mWidgets.erase(it);
		mWidgets.push_back(w);
	}
}

void ATUIContainer::InvalidateLayout() {
	if (mbLayoutInvalid)
		return;

	mbLayoutInvalid = true;

	for(ATUIContainer *p = mpParent; p; p = p->mpParent) {
		if (p->mbDescendantLayoutInvalid)
			break;

		p->mbDescendantLayoutInvalid = true;
	}
}

void ATUIContainer::UpdateLayout() {
	if (!mbLayoutInvalid) {
		if (mbDescendantLayoutInvalid) {
			for(Widgets::const_reverse_iterator it(mWidgets.rbegin()), itEnd(mWidgets.rend());
				it != itEnd;
				++it)
			{
				ATUIWidget *w = *it;

				w->UpdateLayout();
			}

			mbDescendantLayoutInvalid = false;
		}

		return;
	}

	mbLayoutInvalid = false;
	mbDescendantLayoutInvalid = false;

	vdrect32 r(mClientArea);
	vdrect32 r2;

	r.translate(-r.left, -r.top);

	for(Widgets::const_reverse_iterator it(mWidgets.rbegin()), itEnd(mWidgets.rend());
		it != itEnd;
		++it)
	{
		ATUIWidget *w = *it;

		switch(w->GetDockMode()) {
			case kATUIDockMode_None:
				{
					IATUIAnchor *anchor = w->GetAnchor();

					if (anchor)
						w->SetArea(anchor->Position(r, w->GetArea().size()));
				}
				break;

			case kATUIDockMode_Left:
				r2 = r;
				r2.right = r2.left + w->GetArea().width();
				r.left += r2.width();
				w->SetArea(r2);
				break;

			case kATUIDockMode_Right:
				r2 = r;
				r2.left = r2.right - w->GetArea().width();
				r.right -= r2.width();
				w->SetArea(r2);
				break;

			case kATUIDockMode_Top:
				r2 = r;
				r2.bottom = r2.top + w->GetArea().height();
				r.top += r2.height();
				w->SetArea(r2);
				break;

			case kATUIDockMode_Bottom:
				r2 = r;
				r2.top = r2.bottom - w->GetArea().height();
				r.bottom -= r2.height();
				w->SetArea(r2);
				break;

			case kATUIDockMode_LeftFloat:
				r2 = r;
				r2.right = r2.left + w->GetArea().width();
				w->SetArea(r2);
				break;

			case kATUIDockMode_RightFloat:
				r2 = r;
				r2.left = r2.right - w->GetArea().width();
				w->SetArea(r2);
				break;

			case kATUIDockMode_TopFloat:
				r2 = r;
				r2.bottom = r2.top + w->GetArea().height();
				w->SetArea(r2);
				break;

			case kATUIDockMode_BottomFloat:
				r2 = r;
				r2.top = r2.bottom - w->GetArea().height();
				w->SetArea(r2);
				break;

			case kATUIDockMode_Fill:
				w->SetArea(r);
				break;
		}

		w->UpdateLayout();
	}
}

ATUIWidget *ATUIContainer::HitTest(vdpoint32 pt) {
	if (!mbVisible || !mArea.contains(pt))
		return NULL;

	pt.x -= mArea.left;
	pt.y -= mArea.top;

	if (mClientArea.contains(pt)) {
		pt.x -= mClientArea.left;
		pt.y -= mClientArea.top;
		pt.x += mClientOrigin.x;
		pt.y += mClientOrigin.y;

		for(Widgets::const_reverse_iterator it(mWidgets.rbegin()), itEnd(mWidgets.rend());
			it != itEnd;
			++it)
		{
			ATUIWidget *w = *it;

			ATUIWidget *r = w->HitTest(pt);
			if (r)
				return r;
		}
	}

	return mbHitTransparent ? NULL : this;
}

void ATUIContainer::OnDestroy() {
	RemoveAllChildren();
}

void ATUIContainer::OnSize() {
	mbLayoutInvalid = true;
	UpdateLayout();
}

void ATUIContainer::Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) {
	for(Widgets::const_iterator it(mWidgets.begin()), itEnd(mWidgets.end());
		it != itEnd;
		++it)
	{
		ATUIWidget *w = *it;

		w->Draw(rdr);
	}
}

void ATUIContainer::OnSetFocus() {
	if (!mWidgets.empty())
		mWidgets.front()->Focus();
}

ATUIWidget *ATUIContainer::DragHitTest(vdpoint32 pt) {
	if (!mbVisible || !mArea.contains(pt))
		return NULL;

	pt.x -= mArea.left;
	pt.y -= mArea.top;

	if (mClientArea.contains(pt)) {
		pt.x -= mClientArea.left;
		pt.y -= mClientArea.top;
		pt.x += mClientOrigin.x;
		pt.y += mClientOrigin.y;

		for(Widgets::const_reverse_iterator it(mWidgets.rbegin()), itEnd(mWidgets.rend());
			it != itEnd;
			++it)
		{
			ATUIWidget *w = *it;

			ATUIWidget *r = w->DragHitTest(pt);
			if (r)
				return r;
		}
	}

	return mbDropTarget ? this : nullptr;
}
