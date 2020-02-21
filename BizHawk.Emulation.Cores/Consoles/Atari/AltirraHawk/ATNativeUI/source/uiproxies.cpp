#include <stdafx.h>
#include <windows.h>
#include <richedit.h>
#include <commctrl.h>
#include <tom.h>

#include <vd2/system/w32assist.h>
#include <vd2/system/strutil.h>
#include <vd2/system/thunk.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/vdstl.h>
#include <vd2/Dita/accel.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/resample.h>
#include <vd2/Riza/bitmap.h>
#include <at/atnativeui/uiproxies.h>

#pragma comment(lib, "oleaut32")

namespace {
	union AlphaBitmapHeader {
		BITMAPV5HEADER v5;
		VDAVIBitmapInfoHeader bi;

		AlphaBitmapHeader(uint32 w, uint32 h);
	};

	AlphaBitmapHeader::AlphaBitmapHeader(uint32 w, uint32 h) {
		v5 = {};
		v5.bV5Size = sizeof(BITMAPV5HEADER);
		v5.bV5Width = w;
		v5.bV5Height = h;
		v5.bV5Planes = 1;
		v5.bV5BitCount = 32;
		v5.bV5Compression = BI_BITFIELDS;
		v5.bV5SizeImage = w*h*4;
		v5.bV5XPelsPerMeter = 0;
		v5.bV5YPelsPerMeter = 0;
		v5.bV5ClrUsed = 0;
		v5.bV5ClrImportant = 0;
		v5.bV5RedMask = 0x00FF0000;
		v5.bV5GreenMask = 0x0000FF00;
		v5.bV5BlueMask = 0x000000FF;
		v5.bV5AlphaMask = 0xFF000000;
		v5.bV5CSType = LCS_WINDOWS_COLOR_SPACE;
		v5.bV5Intent = LCS_GM_BUSINESS;
		v5.bV5ProfileSize = 0;
	}

	void AddImagesToImageList(HIMAGELIST hImageList, const VDPixmap& px, uint32 imgw, uint32 imgh) {
		if (px.format != nsVDPixmap::kPixFormat_XRGB8888) {
			VDFAIL("Image list not in correct format.");
			return;
		}

		if (!hImageList)
			return;

		HDC hdc2 = nullptr;

		if (HDC hdc = GetDC(nullptr)) {
			hdc2 = CreateCompatibleDC(hdc);
			ReleaseDC(nullptr, hdc);
		}

		if (hdc2) {
			AlphaBitmapHeader hdr(imgw, imgh);

			void *bits;

			HBITMAP hbm = CreateDIBSection(hdc2, (BITMAPINFO *)&hdr, DIB_RGB_COLORS, &bits, nullptr, 0);
			if (hbm) {
				VDPixmap pxbuf = VDGetPixmapForBitmap(hdr.bi, bits);
				const uint32 srcw = px.w;
				const uint32 srch = px.h;
				VDPixmapBuffer alphasrc(srcw, srch, nsVDPixmap::kPixFormat_XRGB8888);
				VDPixmapBuffer alphadst(imgw, imgh, nsVDPixmap::kPixFormat_XRGB8888);

				GdiFlush();

				VDPixmapResample(pxbuf, px, IVDPixmapResampler::kFilterCubic);

				// We can't guarantee that the resampler will handle alpha, so resample alpha as color
				// and remerge.
				VDMemcpyRect(alphasrc.data, alphasrc.pitch, px.data, px.pitch, srcw*4, srch);

				uint32 numDwords = alphasrc.size() / 4;
				for(uint32 i = 0; i < numDwords; ++i) {
					((uint32 *)alphasrc.data)[i] >>= 24;
				}

				VDPixmapResample(alphadst, alphasrc, IVDPixmapResampler::kFilterCubic);

				for(uint32 y=0; y<imgh; ++y) {
					const uint32 *VDRESTRICT src = (const uint32 *)((const char *)alphadst.data + alphadst.pitch * y);
					uint32 *VDRESTRICT dst = (uint32 *)((char *)pxbuf.data + pxbuf.pitch * y);

					for(uint32 x=0; x<imgw; ++x) {
						dst[x] = (dst[x] & 0x00FFFFFF) + (src[x] << 24);
					}
				}
					
				ImageList_Add(hImageList, hbm, nullptr);

				DeleteObject(hbm);
			}

			DeleteDC(hdc2);
		}
	}
}

///////////////////////////////////////////////////////////////////////////////

VDUIProxyControl::VDUIProxyControl()
	: mRedrawInhibitCount(0)
{
}

void VDUIProxyControl::Attach(VDZHWND hwnd) {
	VDASSERT(IsWindow(hwnd));
	mhwnd = hwnd;
}

void VDUIProxyControl::Detach() {
	mhwnd = NULL;
}

void VDUIProxyControl::SetRedraw(bool redraw) {
	if (redraw) {
		if (!--mRedrawInhibitCount) {
			if (mhwnd)
				SendMessage(mhwnd, WM_SETREDRAW, TRUE, 0);
		}
	} else {
		if (!mRedrawInhibitCount++) {
			if (mhwnd)
				SendMessage(mhwnd, WM_SETREDRAW, FALSE, 0);
		}
	}
}

VDZLRESULT VDUIProxyControl::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	return 0;
}

VDZLRESULT VDUIProxyControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	return 0;
}

void VDUIProxyControl::OnFontChanged() {
}


///////////////////////////////////////////////////////////////////////////////

void VDUIProxyMessageDispatcherW32::AddControl(VDUIProxyControl *control) {
	VDZHWND hwnd = control->GetHandle();
	size_t hc = Hash(hwnd);

	mHashTable[hc].push_back(control);
}

void VDUIProxyMessageDispatcherW32::RemoveControl(VDZHWND hwnd) {
	size_t hc = Hash(hwnd);
	HashChain& hchain = mHashTable[hc];

	HashChain::iterator it(hchain.begin()), itEnd(hchain.end());
	for(; it != itEnd; ++it) {
		VDUIProxyControl *control = *it;

		if (control->GetHandle() == hwnd) {
			hchain.erase(control);
			break;
		}
	}

}

void VDUIProxyMessageDispatcherW32::RemoveAllControls(bool detach) {
	for(int i=0; i<kHashTableSize; ++i) {
		HashChain& hchain = mHashTable[i];

		if (detach) {
			HashChain::iterator it(hchain.begin()), itEnd(hchain.end());
			for(; it != itEnd; ++it) {
				VDUIProxyControl *control = *it;

				control->Detach();
			}
		}

		hchain.clear();
	}
}

bool VDUIProxyMessageDispatcherW32::TryDispatch_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam, VDZLRESULT& result) {
	VDUIProxyControl *control = GetControl((HWND)lParam);

	if (control) {
		result = control->On_WM_COMMAND(wParam, lParam);
		return true;
	}

	return false;
}

bool VDUIProxyMessageDispatcherW32::TryDispatch_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam, VDZLRESULT& result) {
	const NMHDR *hdr = (const NMHDR *)lParam;
	VDUIProxyControl *control = GetControl(hdr->hwndFrom);

	if (control) {
		result = control->On_WM_NOTIFY(wParam, lParam);
		return true;
	}

	return false;
}

VDZLRESULT VDUIProxyMessageDispatcherW32::Dispatch_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	VDUIProxyControl *control = GetControl((HWND)lParam);

	if (control)
		return control->On_WM_COMMAND(wParam, lParam);

	return 0;
}

VDZLRESULT VDUIProxyMessageDispatcherW32::Dispatch_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	const NMHDR *hdr = (const NMHDR *)lParam;
	VDUIProxyControl *control = GetControl(hdr->hwndFrom);

	if (control)
		return control->On_WM_NOTIFY(wParam, lParam);

	return 0;
}

void VDUIProxyMessageDispatcherW32::DispatchFontChanged() {
	for(HashChain& hc : mHashTable) {
		for(VDUIProxyControl *control : hc) {
			control->OnFontChanged();
		}
	}
}

size_t VDUIProxyMessageDispatcherW32::Hash(VDZHWND hwnd) const {
	return (size_t)hwnd % (size_t)kHashTableSize;
}

VDUIProxyControl *VDUIProxyMessageDispatcherW32::GetControl(VDZHWND hwnd) {
	size_t hc = Hash(hwnd);
	HashChain& hchain = mHashTable[hc];

	HashChain::iterator it(hchain.begin()), itEnd(hchain.end());
	for(; it != itEnd; ++it) {
		VDUIProxyControl *control = *it;

		if (control->GetHandle() == hwnd)
			return control;
	}

	return NULL;
}

///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////

VDUIProxyListView::VDUIProxyListView() {
}

void VDUIProxyListView::Detach() {
	Clear();
	VDUIProxyControl::Detach();
}

void VDUIProxyListView::SetIndexedProvider(IVDUIListViewIndexedProvider *p) {
	mbIndexedMode = true;
	mpIndexedProvider = p;
}

void VDUIProxyListView::AutoSizeColumns(bool expandlast) {
	const int colCount = GetColumnCount();

	int colCacheCount = (int)mColumnWidthCache.size();
	while(colCacheCount < colCount) {
		SendMessage(mhwnd, LVM_SETCOLUMNWIDTH, colCacheCount, LVSCW_AUTOSIZE_USEHEADER);
		mColumnWidthCache.push_back((int)SendMessage(mhwnd, LVM_GETCOLUMNWIDTH, colCacheCount, 0));
		++colCacheCount;
	}

	int totalWidth = 0;
	for(int col=0; col<colCount; ++col) {
		const int hdrWidth = mColumnWidthCache[col];

		SendMessage(mhwnd, LVM_SETCOLUMNWIDTH, col, LVSCW_AUTOSIZE);
		int dataWidth = (int)SendMessage(mhwnd, LVM_GETCOLUMNWIDTH, col, 0);

		if (dataWidth < hdrWidth)
			dataWidth = hdrWidth;

		if (expandlast && col == colCount-1) {
			RECT r;
			if (GetClientRect(mhwnd, &r)) {
				int extraWidth = r.right - totalWidth;

				if (dataWidth < extraWidth)
					dataWidth = extraWidth;
			}
		}

		SendMessage(mhwnd, LVM_SETCOLUMNWIDTH, col, dataWidth);

		totalWidth += dataWidth;
	}
}

void VDUIProxyListView::Clear() {
	if (mhwnd)
		SendMessage(mhwnd, LVM_DELETEALLITEMS, 0, 0);
}

void VDUIProxyListView::ClearExtraColumns() {
	if (!mhwnd)
		return;

	uint32 n = GetColumnCount();
	for(uint32 i=n; i > 1; --i)
		ListView_DeleteColumn(mhwnd, i - 1);

	if (!mColumnWidthCache.empty())
		mColumnWidthCache.resize(1);
}

void VDUIProxyListView::DeleteItem(int index) {
	if (index >= 0)
		SendMessage(mhwnd, LVM_DELETEITEM, index, 0);
}

int VDUIProxyListView::GetColumnCount() const {
	HWND hwndHeader = (HWND)SendMessage(mhwnd, LVM_GETHEADER, 0, 0);
	if (!hwndHeader)
		return 0;

	return (int)SendMessage(hwndHeader, HDM_GETITEMCOUNT, 0, 0);
}

int VDUIProxyListView::GetItemCount() const {
	return (int)SendMessage(mhwnd, LVM_GETITEMCOUNT, 0, 0);
}

int VDUIProxyListView::GetSelectedIndex() const {
	return ListView_GetNextItem(mhwnd, -1, LVNI_SELECTED);
}

void VDUIProxyListView::SetSelectedIndex(int index) {
	ListView_SetItemState(mhwnd, index, LVIS_SELECTED|LVIS_FOCUSED, LVIS_SELECTED|LVIS_FOCUSED);
}

uint32 VDUIProxyListView::GetSelectedItemId() const {
	int idx = GetSelectedIndex();

	if (idx < 0)
		return 0;

	return GetItemId(idx);
}

IVDUIListViewVirtualItem *VDUIProxyListView::GetSelectedItem() const {
	int idx = GetSelectedIndex();

	if (idx < 0)
		return NULL;

	return GetVirtualItem(idx);
}

void VDUIProxyListView::GetSelectedIndices(vdfastvector<int>& indices) const {
	int idx = -1;

	indices.clear();
	for(;;) {
		idx = ListView_GetNextItem(mhwnd, idx, LVNI_SELECTED);
		if (idx < 0)
			return;

		indices.push_back(idx);
	}
}

void VDUIProxyListView::SetFullRowSelectEnabled(bool enabled) {
	ListView_SetExtendedListViewStyleEx(mhwnd, LVS_EX_FULLROWSELECT, enabled ? LVS_EX_FULLROWSELECT : 0);
}

void VDUIProxyListView::SetGridLinesEnabled(bool enabled) {
	ListView_SetExtendedListViewStyleEx(mhwnd, LVS_EX_GRIDLINES, enabled ? LVS_EX_GRIDLINES : 0);
}

bool VDUIProxyListView::AreItemCheckboxesEnabled() const {
	return (ListView_GetExtendedListViewStyle(mhwnd) & LVS_EX_CHECKBOXES) != 0;
}

void VDUIProxyListView::SetItemCheckboxesEnabled(bool enabled) {
	ListView_SetExtendedListViewStyleEx(mhwnd, LVS_EX_CHECKBOXES, enabled ? LVS_EX_CHECKBOXES : 0);
}

void VDUIProxyListView::EnsureItemVisible(int index) {
	ListView_EnsureVisible(mhwnd, index, FALSE);
}

int VDUIProxyListView::GetVisibleTopIndex() {
	return ListView_GetTopIndex(mhwnd);
}

void VDUIProxyListView::SetVisibleTopIndex(int index) {
	int n = ListView_GetItemCount(mhwnd);
	if (n > 0) {
		ListView_EnsureVisible(mhwnd, n - 1, FALSE);
		ListView_EnsureVisible(mhwnd, index, FALSE);
	}
}

IVDUIListViewVirtualItem *VDUIProxyListView::GetSelectedVirtualItem() const {
	int index = GetSelectedIndex();
	if (index < 0)
		return NULL;

	return GetVirtualItem(index);
}

IVDUIListViewVirtualItem *VDUIProxyListView::GetVirtualItem(int index) const {
	if (index < 0)
		return NULL;

	LVITEMW itemw={};
	itemw.mask = LVIF_PARAM;
	itemw.iItem = index;
	itemw.iSubItem = 0;
	if (SendMessage(mhwnd, LVM_GETITEMW, 0, (LPARAM)&itemw))
		return (IVDUIListViewVirtualItem *)itemw.lParam;

	return NULL;
}

uint32 VDUIProxyListView::GetItemId(int index) const {
	if (index < 0)
		return 0;

	LVITEMW itemw={};
	itemw.mask = LVIF_PARAM;
	itemw.iItem = index;
	itemw.iSubItem = 0;
	if (SendMessage(mhwnd, LVM_GETITEMW, 0, (LPARAM)&itemw))
		return (uint32)itemw.lParam;

	return 0;
}

void VDUIProxyListView::InsertColumn(int index, const wchar_t *label, int width, bool rightAligned) {
	VDASSERT(index || !rightAligned);

	LVCOLUMNW colw = {};

	colw.mask		= LVCF_FMT | LVCF_TEXT | LVCF_WIDTH;
	colw.fmt		= rightAligned ? LVCFMT_RIGHT : LVCFMT_LEFT;
	colw.cx			= width;
	colw.pszText	= (LPWSTR)label;

	SendMessageW(mhwnd, LVM_INSERTCOLUMNW, (WPARAM)index, (LPARAM)&colw);
}

int VDUIProxyListView::InsertItem(int item, const wchar_t *text) {
	if (item < 0)
		item = 0x7FFFFFFF;

	LVITEMW itemw = {};

	itemw.mask		= LVIF_TEXT;
	itemw.iItem		= item;
	itemw.pszText	= (LPWSTR)text;

	return (int)SendMessageW(mhwnd, LVM_INSERTITEMW, 0, (LPARAM)&itemw);
}

int VDUIProxyListView::InsertVirtualItem(int item, IVDUIListViewVirtualItem *lvvi) {
	VDASSERT(!mbIndexedMode);

	if (item < 0)
		item = 0x7FFFFFFF;

	++mChangeNotificationLocks;

	LVITEMW itemw = {};

	itemw.mask		= LVIF_TEXT | LVIF_PARAM;
	itemw.iItem		= item;
	itemw.pszText	= LPSTR_TEXTCALLBACKW;
	itemw.lParam	= (LPARAM)lvvi;

	const int index = (int)SendMessageW(mhwnd, LVM_INSERTITEMW, 0, (LPARAM)&itemw);

	--mChangeNotificationLocks;

	if (index >= 0)
		lvvi->AddRef();

	return index;
}

int VDUIProxyListView::InsertIndexedItem(int item, uint32 id) {
	VDASSERT(mbIndexedMode);
	VDASSERT(id);

	if (item < 0)
		item = 0x7FFFFFFF;

	++mChangeNotificationLocks;

	LVITEMW itemw = {};

	itemw.mask		= LVIF_TEXT | LVIF_PARAM;
	itemw.iItem		= item;
	itemw.pszText	= LPSTR_TEXTCALLBACKW;
	itemw.lParam	= (LPARAM)id;

	const int index = (int)SendMessageW(mhwnd, LVM_INSERTITEMW, 0, (LPARAM)&itemw);

	--mChangeNotificationLocks;

	return index;
}

void VDUIProxyListView::RefreshItem(int item) {
	SendMessage(mhwnd, LVM_REDRAWITEMS, item, item);
}

void VDUIProxyListView::RefreshAllItems() {
	int n = GetItemCount();

	if (n)
		SendMessage(mhwnd, LVM_REDRAWITEMS, 0, n - 1);
}

void VDUIProxyListView::EditItemLabel(int item) {
	ListView_EditLabel(mhwnd, item);
}

void VDUIProxyListView::GetItemText(int item, VDStringW& s) const {
	LVITEMW itemw;
	wchar_t buf[512];

	itemw.iSubItem = 0;
	itemw.cchTextMax = 511;
	itemw.pszText = buf;
	buf[0] = 0;
	SendMessageW(mhwnd, LVM_GETITEMTEXTW, item, (LPARAM)&itemw);

	s = buf;
}

void VDUIProxyListView::SetItemText(int item, int subitem, const wchar_t *text) {
	LVITEMW itemw = {};

	itemw.mask		= LVIF_TEXT;
	itemw.iItem		= item;
	itemw.iSubItem	= subitem;
	itemw.pszText	= (LPWSTR)text;

	SendMessageW(mhwnd, LVM_SETITEMW, 0, (LPARAM)&itemw);
}

bool VDUIProxyListView::IsItemChecked(int item) {
	return ListView_GetCheckState(mhwnd, item) != 0;
}

void VDUIProxyListView::SetItemChecked(int item, bool checked) {
	ListView_SetCheckState(mhwnd, item, checked);
}

void VDUIProxyListView::SetItemCheckedVisible(int item, bool checked) {
	if (!mhwnd)
		return;

	ListView_SetItemState(mhwnd, item, INDEXTOSTATEIMAGEMASK(0), LVIS_STATEIMAGEMASK);
}

void VDUIProxyListView::SetItemImage(int item, uint32 imageIndex) {
	LVITEMW itemw = {};

	itemw.mask		= LVIF_IMAGE;
	itemw.iItem		= item;
	itemw.iSubItem	= 0;
	itemw.iImage	= imageIndex;

	SendMessageW(mhwnd, LVM_SETITEMW, 0, (LPARAM)&itemw);
}

bool VDUIProxyListView::GetItemScreenRect(int item, vdrect32& r) const {
	r.set(0, 0, 0, 0);

	if (!mhwnd)
		return false;

	RECT nr = {LVIR_BOUNDS};
	if (!SendMessage(mhwnd, LVM_GETITEMRECT, (WPARAM)item, (LPARAM)&nr))
		return false;

	MapWindowPoints(mhwnd, NULL, (LPPOINT)&nr, 2);

	r.set(nr.left, nr.top, nr.right, nr.bottom);
	return true;
}

void VDUIProxyListView::Sort(IVDUIListViewIndexedComparer& comparer) {
	VDASSERT(mbIndexedMode);

	const auto sortAdapter = 
		[](LPARAM x, LPARAM y, LPARAM cookie) {
			return ((IVDUIListViewIndexedComparer *)cookie)->Compare((uint32)x, (uint32)y);
		};

	ListView_SortItems(mhwnd, sortAdapter, (LPARAM)&comparer);
}

void VDUIProxyListView::Sort(IVDUIListViewVirtualComparer& comparer) {
	VDASSERT(!mbIndexedMode);

	const auto sortAdapter = 
		[](LPARAM x, LPARAM y, LPARAM cookie) {
			return ((IVDUIListViewVirtualComparer *)cookie)->Compare((IVDUIListViewVirtualItem *)x, (IVDUIListViewVirtualItem *)y);
		};

	ListView_SortItems(mhwnd, sortAdapter, (LPARAM)&comparer);
}

void VDUIProxyListView::SetOnItemDoubleClicked(vdfunction<void(int)> fn) {
	mpOnItemDoubleClicked = std::move(fn);
}

VDZLRESULT VDUIProxyListView::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	const NMHDR *hdr = (const NMHDR *)lParam;

	switch(hdr->code) {
		case LVN_GETDISPINFOA:
			{
				NMLVDISPINFOA *dispa = (NMLVDISPINFOA *)hdr;

				if (dispa->item.mask & LVIF_TEXT) {
					mTextW[0].clear();

					if (mbIndexedMode) {
						if (mpIndexedProvider)
							mpIndexedProvider->GetText((uint32)dispa->item.lParam, dispa->item.iSubItem, mTextW[0]);
					} else {
						IVDUIListViewVirtualItem *lvvi = (IVDUIListViewVirtualItem *)dispa->item.lParam;
						if (lvvi)
							lvvi->GetText(dispa->item.iSubItem, mTextW[0]);
					}

					mTextA[mNextTextIndex] = VDTextWToA(mTextW[0]);
					dispa->item.pszText = (LPSTR)mTextA[mNextTextIndex].c_str();

					if (++mNextTextIndex >= 3)
						mNextTextIndex = 0;
				}
			}
			break;

		case LVN_GETDISPINFOW:
			{
				NMLVDISPINFOW *dispw = (NMLVDISPINFOW *)hdr;

				if (dispw->item.mask & LVIF_TEXT) {
					mTextW[mNextTextIndex].clear();

					if (mbIndexedMode) {
						if (mpIndexedProvider)
							mpIndexedProvider->GetText((uint32)dispw->item.lParam, dispw->item.iSubItem, mTextW[mNextTextIndex]);
					} else {
						IVDUIListViewVirtualItem *lvvi = (IVDUIListViewVirtualItem *)dispw->item.lParam;
						if (lvvi)
							lvvi->GetText(dispw->item.iSubItem, mTextW[mNextTextIndex]);
					}

					dispw->item.pszText = (LPWSTR)mTextW[mNextTextIndex].c_str();

					if (++mNextTextIndex >= 3)
						mNextTextIndex = 0;
				}
			}
			break;

		case LVN_DELETEITEM:
			if (!mbIndexedMode) {
				const NMLISTVIEW *nmlv = (const NMLISTVIEW *)hdr;
				IVDUIListViewVirtualItem *lvvi = (IVDUIListViewVirtualItem *)nmlv->lParam;

				if (lvvi)
					lvvi->Release();
			}
			break;

		case LVN_COLUMNCLICK:
			{
				const NMLISTVIEW *nmlv = (const NMLISTVIEW *)hdr;

				mEventColumnClicked.Raise(this, nmlv->iSubItem);
			}
			break;

		case LVN_ITEMCHANGING:
			if (!mChangeNotificationLocks) {
				const NMLISTVIEW *nmlv = (const NMLISTVIEW *)hdr;

				if (nmlv->uChanged & LVIF_STATE) {
					uint32 deltaState = nmlv->uOldState ^ nmlv->uNewState;

					if (deltaState & LVIS_STATEIMAGEMASK) {
						VDASSERT(nmlv->iItem >= 0);

						CheckedChangingEvent event;
						event.mIndex = nmlv->iItem;
						event.mbNewVisible = (nmlv->uNewState & LVIS_STATEIMAGEMASK) != 0;
						event.mbNewChecked = (nmlv->uNewState & 0x2000) != 0;
						event.mbAllowChange = true;
						mEventItemCheckedChanging.Raise(this, &event);

						if (!event.mbAllowChange)
							return TRUE;
					}
				}
			}
			break;

		case LVN_ITEMCHANGED:
			if (!mChangeNotificationLocks) {
				const NMLISTVIEW *nmlv = (const NMLISTVIEW *)hdr;

				if (nmlv->uChanged & LVIF_STATE) {
					uint32 deltaState = nmlv->uOldState ^ nmlv->uNewState;

					if (deltaState & LVIS_SELECTED) {
						int selIndex = ListView_GetNextItem(mhwnd, -1, LVNI_ALL | LVNI_SELECTED);

						mEventItemSelectionChanged.Raise(this, selIndex);
					}

					if (deltaState & LVIS_STATEIMAGEMASK) {
						VDASSERT(nmlv->iItem >= 0);
						mEventItemCheckedChanged.Raise(this, nmlv->iItem);
					}
				}
			}
			break;

		case LVN_ENDLABELEDITA:
			{
				const NMLVDISPINFOA *di = (const NMLVDISPINFOA *)hdr;
				if (di->item.pszText) {
					const VDStringW label(VDTextAToW(di->item.pszText));
					LabelChangedEvent event = {
						true,
						di->item.iItem,
						label.c_str()
					};

					mEventItemLabelEdited.Raise(this, &event);

					if (!event.mbAllowEdit)
						return FALSE;
				}
			}
			return TRUE;

		case LVN_ENDLABELEDITW:
			{
				const NMLVDISPINFOW *di = (const NMLVDISPINFOW *)hdr;

				LabelChangedEvent event = {
					true,
					di->item.iItem,
					di->item.pszText
				};

				mEventItemLabelEdited.Raise(this, &event);

				if (!event.mbAllowEdit)
					return FALSE;
			}
			return TRUE;

		case LVN_BEGINDRAG:
			{
				const NMLISTVIEW *nmlv = (const NMLISTVIEW *)hdr;

				mEventItemBeginDrag.Raise(this, nmlv->iItem);
			}
			return 0;

		case LVN_BEGINRDRAG:
			{
				const NMLISTVIEW *nmlv = (const NMLISTVIEW *)hdr;

				mEventItemBeginRDrag.Raise(this, nmlv->iItem);
			}
			return 0;

		case NM_RCLICK:
			{
				const NMITEMACTIVATE *nmia = (const NMITEMACTIVATE *)hdr;

				ContextMenuEvent event;
				event.mIndex = nmia->iItem;

				POINT pt = nmia->ptAction;
				ClientToScreen(mhwnd, &pt);
				event.mX = pt.x;
				event.mY = pt.y;
				mEventItemContextMenu.Raise(this, event);
			}
			return 0;

		case NM_DBLCLK:
			{
				const NMITEMACTIVATE *nmia = (const NMITEMACTIVATE *)hdr;

				// skip handling if the double click is on the checkbox
				LVHITTESTINFO hti {};
				hti.pt = nmia->ptAction;

				SendMessage(mhwnd, LVM_SUBITEMHITTEST, 0, (LPARAM)&hti);

				if (hti.flags & LVHT_ONITEMSTATEICON)
					return 0;

				mEventItemDoubleClicked.Raise(this, nmia->iItem);
				if (mpOnItemDoubleClicked)
					mpOnItemDoubleClicked(nmia->iItem);
			}
			return 0;
	}

	return 0;
}

void VDUIProxyListView::OnFontChanged() {
	// Windows 10 ver 1703 has a problem with leaving list view items at ridiculous height when
	// moving a window from higher to lower DPI in per monitor V2 mode. To work around this
	// issue, we flash the view to list mode and back to force it to recompute the item heights.
	if (AreItemCheckboxesEnabled()) {
		ListView_SetView(mhwnd, LV_VIEW_LIST);
		ListView_SetView(mhwnd, LV_VIEW_DETAILS);
	}
}

///////////////////////////////////////////////////////////////////////////

VDUIProxyHotKeyControl::VDUIProxyHotKeyControl() {
}

VDUIProxyHotKeyControl::~VDUIProxyHotKeyControl() {
}

bool VDUIProxyHotKeyControl::GetAccelerator(VDUIAccelerator& accel) const {
	if (!mhwnd)
		return false;

	uint32 v = (uint32)SendMessage(mhwnd, HKM_GETHOTKEY, 0, 0);

	accel.mVirtKey = (uint8)v;
	accel.mModifiers = 0;
	
	const uint8 mods = (uint8)(v >> 8);
	if (mods & HOTKEYF_SHIFT)
		accel.mModifiers |= VDUIAccelerator::kModShift;

	if (mods & HOTKEYF_CONTROL)
		accel.mModifiers |= VDUIAccelerator::kModCtrl;

	if (mods & HOTKEYF_ALT)
		accel.mModifiers |= VDUIAccelerator::kModAlt;

	if (mods & HOTKEYF_EXT)
		accel.mModifiers |= VDUIAccelerator::kModExtended;

	return true;
}

void VDUIProxyHotKeyControl::SetAccelerator(const VDUIAccelerator& accel) {
	uint32 mods = 0;

	if (accel.mModifiers & VDUIAccelerator::kModShift)
		mods |= HOTKEYF_SHIFT;

	if (accel.mModifiers & VDUIAccelerator::kModCtrl)
		mods |= HOTKEYF_CONTROL;

	if (accel.mModifiers & VDUIAccelerator::kModAlt)
		mods |= HOTKEYF_ALT;

	if (accel.mModifiers & VDUIAccelerator::kModExtended)
		mods |= HOTKEYF_EXT;

	SendMessage(mhwnd, HKM_SETHOTKEY, accel.mVirtKey + (mods << 8), 0);
}

VDZLRESULT VDUIProxyHotKeyControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (HIWORD(wParam) == EN_CHANGE) {
		VDUIAccelerator accel;
		GetAccelerator(accel);
		mEventHotKeyChanged.Raise(this, accel);
	}

	return 0;
}

///////////////////////////////////////////////////////////////////////////

VDUIProxyTabControl::VDUIProxyTabControl() {
}

VDUIProxyTabControl::~VDUIProxyTabControl() {
}

void VDUIProxyTabControl::AddItem(const wchar_t *s) {
	if (!mhwnd)
		return;

	int n = TabCtrl_GetItemCount(mhwnd);
	TCITEMW tciw = { TCIF_TEXT };

	tciw.pszText = (LPWSTR)s;

	SendMessageW(mhwnd, TCM_INSERTITEMW, n, (LPARAM)&tciw);
}

void VDUIProxyTabControl::DeleteItem(int index) {
	if (mhwnd)
		SendMessage(mhwnd, TCM_DELETEITEM, index, 0);
}

vdsize32 VDUIProxyTabControl::GetControlSizeForContent(const vdsize32& sz) const {
	if (!mhwnd)
		return vdsize32(0, 0);

	RECT r = { 0, 0, sz.w, sz.h };
	TabCtrl_AdjustRect(mhwnd, TRUE, &r);

	return vdsize32(r.right - r.left, r.bottom - r.top);
}

vdrect32 VDUIProxyTabControl::GetContentArea() const {
	if (!mhwnd)
		return vdrect32(0, 0, 0, 0);

	RECT r = {0};
	GetWindowRect(mhwnd, &r);

	HWND hwndParent = GetParent(mhwnd);
	if (hwndParent)
		MapWindowPoints(NULL, hwndParent, (LPPOINT)&r, 2);

	TabCtrl_AdjustRect(mhwnd, FALSE, &r);

	return vdrect32(r.left, r.top, r.right, r.bottom);
}

int VDUIProxyTabControl::GetSelection() const {
	if (!mhwnd)
		return -1;

	return (int)SendMessage(mhwnd, TCM_GETCURSEL, 0, 0);
}

void VDUIProxyTabControl::SetSelection(int index) {
	if (mhwnd)
		SendMessage(mhwnd, TCM_SETCURSEL, index, 0);
}

VDZLRESULT VDUIProxyTabControl::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (((const NMHDR *)lParam)->code == TCN_SELCHANGE) {
		mSelectionChanged.Raise(this, GetSelection());
	}

	return 0;
}

///////////////////////////////////////////////////////////////////////////

VDUIProxyListBoxControl::VDUIProxyListBoxControl() {
}

VDUIProxyListBoxControl::~VDUIProxyListBoxControl() {
	if (mpEditWndProcThunk) {
		VDDestroyFunctionThunk(mpEditWndProcThunk);
		mpEditWndProcThunk = NULL;
	}

	if (mpWndProcThunk) {
		VDDestroyFunctionThunk(mpWndProcThunk);
		mpWndProcThunk = NULL;
	}

	CancelEditTimer();

	if (mpEditTimerThunk) {
		VDDestroyFunctionThunk(mpEditTimerThunk);
		mpEditTimerThunk = NULL;
	}
}

void VDUIProxyListBoxControl::EnableAutoItemEditing() {
	if (!mPrevWndProc) {
		mPrevWndProc = (void(*)())GetWindowLongPtr(mhwnd, GWLP_WNDPROC);

		if (!mpWndProcThunk)
			mpWndProcThunk = VDCreateFunctionThunkFromMethod(this, &VDUIProxyListBoxControl::ListBoxWndProc, true);

		SetWindowLongPtr(mhwnd, GWLP_WNDPROC, (LONG_PTR)VDGetThunkFunction<WNDPROC>(mpWndProcThunk));
	}
}

void VDUIProxyListBoxControl::Clear() {
	if (!mhwnd)
		return;

	SendMessage(mhwnd, LB_RESETCONTENT, 0, 0);
}

int VDUIProxyListBoxControl::AddItem(const wchar_t *s, uintptr_t cookie) {
	if (!mhwnd)
		return -1;

	CancelEditTimer();

	int idx;
	idx = (int)SendMessageW(mhwnd, LB_ADDSTRING, 0, (LPARAM)s);

	if (idx >= 0)
		SendMessage(mhwnd, LB_SETITEMDATA, idx, (LPARAM)cookie);

	return idx;
}

int VDUIProxyListBoxControl::InsertItem(int pos, const wchar_t *s, uintptr_t cookie) {
	if (!mhwnd)
		return -1;

	CancelEditTimer();

	int idx;
	idx = (int)SendMessageW(mhwnd, LB_INSERTSTRING, (WPARAM)pos, (LPARAM)s);

	if (idx >= 0)
		SendMessage(mhwnd, LB_SETITEMDATA, idx, (LPARAM)cookie);

	return idx;
}

void VDUIProxyListBoxControl::DeleteItem(int pos) {
	if (!mhwnd)
		return;

	CancelEditTimer();
	SendMessageW(mhwnd, LB_DELETESTRING, (WPARAM)pos, 0);
}

void VDUIProxyListBoxControl::EnsureItemVisible(int pos) {
	if (!mhwnd || pos < 0)
		return;

	CancelEditTimer();

	RECT r;
	if (!GetClientRect(mhwnd, &r))
		return;

	RECT rItem;
	if (LB_ERR != SendMessageW(mhwnd, LB_GETITEMRECT, (WPARAM)pos, (LPARAM)&rItem)) {
		if (rItem.top < r.bottom && r.top < rItem.bottom)
			return;
	}

	// Item isn't visible. Scroll it into view.
	int itemHeight = (int)SendMessageW(mhwnd, LB_GETITEMHEIGHT, 0, 0);
	if (itemHeight <= 0)
		return;

	int topIndex = std::max<int>(0, pos - (r.bottom - itemHeight) / (2 * itemHeight));
	SendMessageW(mhwnd, LB_SETTOPINDEX, (WPARAM)topIndex, 0);
}

void VDUIProxyListBoxControl::EditItem(int index) {
	if (!mhwnd)
		return;

	CancelEditTimer();
	EnsureItemVisible(index);

	RECT rItem {};
	if (LB_ERR == SendMessageW(mhwnd, LB_GETITEMRECT, (WPARAM)index, (LPARAM)&rItem))
		return;

	int textLen = (int)SendMessageW(mhwnd, LB_GETTEXTLEN, (WPARAM)index, 0);
	vdfastvector<WCHAR> textbuf(textLen + 1, 0);

	SendMessageW(mhwnd, LB_GETTEXT, (WPARAM)index, (LPARAM)textbuf.data());

	mEditItem = index;

	int cxedge = 1;
	int cyedge = 1;

	mhwndEdit = CreateWindowExW(WS_EX_TOPMOST | WS_EX_TOOLWINDOW, WC_EDITW, textbuf.data(), WS_CHILD | WS_BORDER | ES_AUTOHSCROLL,
		rItem.left - 0*cxedge,
		rItem.top - 2*cxedge,
		(rItem.right - rItem.left) + 0*cxedge,
		(rItem.bottom - rItem.top) + 4*cyedge,
		mhwnd, NULL, VDGetLocalModuleHandleW32(), NULL);

	SendMessageW(mhwndEdit, WM_SETFONT, (WPARAM)SendMessageW(mhwnd, WM_GETFONT, 0, 0), (LPARAM)TRUE);

	if (!mpEditWndProcThunk)
		mpEditWndProcThunk = VDCreateFunctionThunkFromMethod(this, &VDUIProxyListBoxControl::LabelEditWndProc, true);

	if (mpEditWndProcThunk) {
		mPrevEditWndProc = (void (*)())(WNDPROC)GetWindowLongPtrW(mhwndEdit, GWLP_WNDPROC);

		if (mPrevEditWndProc)
			SetWindowLongPtrW(mhwndEdit, GWLP_WNDPROC, (LONG_PTR)VDGetThunkFunction<WNDPROC>(mpEditWndProcThunk));
	}

	ShowWindow(mhwndEdit, SW_SHOWNOACTIVATE);
	::SetFocus(mhwndEdit);
	SendMessageW(mhwndEdit, EM_SETSEL, 0, -1);
}

void VDUIProxyListBoxControl::SetItemText(int index, const wchar_t *s) {
	if (!mhwnd || index < 0)
		return;

	CancelEditTimer();

	int n = (int)SendMessage(mhwnd, LB_GETCOUNT, 0, 0);
	if (index >= n)
		return;

	uintptr_t itemData = (uintptr_t)SendMessage(mhwnd, LB_GETITEMDATA, index, 0);
	bool selected = (GetSelection() == index);

	++mSuppressNotificationCount;
	SendMessage(mhwnd, LB_DELETESTRING, index, 0);
	int newIdx = (int)SendMessageW(mhwnd, LB_INSERTSTRING, 0, (LPARAM)s);
	if (newIdx >= 0) {
		SendMessage(mhwnd, LB_SETITEMDATA, newIdx, (LPARAM)itemData);

		if (selected)
			SetSelection(newIdx);
	}
	--mSuppressNotificationCount;
}

uintptr VDUIProxyListBoxControl::GetItemData(int index) const {
	if (index < 0 || !mhwnd)
		return 0;

	return SendMessage(mhwnd, LB_GETITEMDATA, index, 0);
}

int VDUIProxyListBoxControl::GetSelection() const {
	if (!mhwnd)
		return -1;

	return (int)SendMessage(mhwnd, LB_GETCURSEL, 0, 0);
}

void VDUIProxyListBoxControl::SetSelection(int index) {
	if (mhwnd)
		SendMessage(mhwnd, LB_SETCURSEL, index, 0);
}

void VDUIProxyListBoxControl::MakeSelectionVisible() {
	if (!mhwnd)
		return;

	int idx = (int)SendMessage(mhwnd, LB_GETCURSEL, 0, 0);

	if (idx >= 0)
		SendMessage(mhwnd, LB_SETCURSEL, idx, 0);
}

void VDUIProxyListBoxControl::SetOnSelectionChanged(vdfunction<void(int)> fn) {
	mpFnSelectionChanged = fn;
}

void VDUIProxyListBoxControl::SetOnItemDoubleClicked(vdfunction<void(int)> fn) {
	mpFnItemDoubleClicked = fn;
}

void VDUIProxyListBoxControl::SetOnItemEdited(vdfunction<void(int ,const wchar_t *)> fn) {
	mpFnItemEdited = fn;
}

void VDUIProxyListBoxControl::SetTabStops(const int *units, uint32 n) {
	if (!mhwnd)
		return;

	vdfastvector<INT> v(n);

	for(uint32 i=0; i<n; ++i)
		v[i] = units[i];

	SendMessage(mhwnd, LB_SETTABSTOPS, n, (LPARAM)v.data());
}

void VDUIProxyListBoxControl::EndEditItem() {
	if (mhwndEdit) {
		SetWindowLongPtrW(mhwndEdit, GWLP_WNDPROC, (LONG_PTR)mPrevEditWndProc);

		auto h = mhwndEdit;
		mhwndEdit = nullptr;

		DestroyWindow(h);
	}
}

void VDUIProxyListBoxControl::CancelEditTimer() {
	if (mAutoEditTimer) {
		KillTimer(nullptr, mAutoEditTimer);
		mAutoEditTimer = 0;
	}
}

void VDUIProxyListBoxControl::Detach() {
	EndEditItem();

	if (mPrevWndProc) {
		SetWindowLongPtrW(mhwnd, GWLP_WNDPROC, (LONG_PTR)mPrevWndProc);
		mPrevWndProc = nullptr;
	}
}

VDZLRESULT VDUIProxyListBoxControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (mSuppressNotificationCount)
		return 0;

	if (HIWORD(wParam) == LBN_SELCHANGE) {
		int selIndex = GetSelection();

		if (mpFnSelectionChanged)
			mpFnSelectionChanged(selIndex);

		mSelectionChanged.Raise(this, selIndex);
	} else if (HIWORD(wParam) == LBN_DBLCLK) {
		int sel = GetSelection();

		if (sel >= 0) {
			if (mpFnItemDoubleClicked)
				mpFnItemDoubleClicked(sel);

			mEventItemDoubleClicked.Raise(this, sel);
		}
	}

	return 0;
}

VDZLRESULT VDUIProxyListBoxControl::ListBoxWndProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	switch(msg) {
		case WM_LBUTTONDOWN:
			{
				int prevSel = (int)CallWindowProcW((WNDPROC)mPrevWndProc, hwnd, LB_GETCURSEL, 0, 0);
				LRESULT r = CallWindowProcW((WNDPROC)mPrevWndProc, hwnd, msg, wParam, lParam);
				int nextSel = (int)CallWindowProcW((WNDPROC)mPrevWndProc, hwnd, LB_GETCURSEL, 0, 0);

				CancelEditTimer();

				if (nextSel == prevSel && nextSel >= 0) {
					RECT r {};
					CallWindowProcW((WNDPROC)mPrevWndProc, hwnd, LB_GETITEMRECT, (WPARAM)nextSel, (LPARAM)&r);

					POINT pt = { (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam) };
					if (PtInRect(&r, pt)) {
						if (!mpEditTimerThunk)
							mpEditTimerThunk = VDCreateFunctionThunkFromMethod(this, &VDUIProxyListBoxControl::AutoEditTimerProc, true);

						if (mpEditTimerThunk)
							mAutoEditTimer = SetTimer(NULL, 0, 1000, VDGetThunkFunction<TIMERPROC>(mpEditTimerThunk));
					}
				}
			}
			break;

		case WM_KEYDOWN:
			if (wParam == VK_F2) {
				int sel = (int)CallWindowProcW((WNDPROC)mPrevWndProc, hwnd, LB_GETCURSEL, 0, 0);

				if (sel >= 0)
					EditItem(sel);
				return 0;
			}
			break;

		case WM_KEYUP:
			if (wParam == VK_F2)
				return 0;
			break;

		case WM_RBUTTONDOWN:
		case WM_MBUTTONDOWN:
		case WM_XBUTTONDOWN:
		case WM_LBUTTONDBLCLK:
		case WM_RBUTTONDBLCLK:
		case WM_MBUTTONDBLCLK:
		case WM_XBUTTONDBLCLK:
			CancelEditTimer();
			break;
	}

	return CallWindowProcW((WNDPROC)mPrevWndProc, hwnd, msg, wParam, lParam);
}

VDZLRESULT VDUIProxyListBoxControl::LabelEditWndProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	switch(msg) {
		case WM_GETDLGCODE:
			return DLGC_WANTALLKEYS;
			break;

		case WM_KEYDOWN:
			if (wParam == VK_RETURN) {
				VDStringW s = VDGetWindowTextW32(hwnd);
				EndEditItem();

				if (mpFnItemEdited)
					mpFnItemEdited(mEditItem, s.c_str());

				return 0;
			} else if (wParam == VK_ESCAPE) {
				EndEditItem();

				if (mpFnItemEdited)
					mpFnItemEdited(mEditItem, nullptr);
				return 0;
			}
			break;

		case WM_KILLFOCUS:
			if (wParam != (WPARAM)hwnd){
				VDStringW s = VDGetWindowTextW32(hwnd);
				EndEditItem();

				if (mpFnItemEdited)
					mpFnItemEdited(mEditItem, s.c_str());
			}
			return 0;

		case WM_MOUSEACTIVATE:
			return MA_NOACTIVATE;
	}

	return CallWindowProcW((WNDPROC)mPrevEditWndProc, hwnd, msg, wParam, lParam);
}

void VDUIProxyListBoxControl::AutoEditTimerProc(VDZHWND, VDZUINT, VDZUINT_PTR, VDZDWORD) {
	CancelEditTimer();

	int idx = (int)CallWindowProcW((WNDPROC)mPrevWndProc, mhwnd, LB_GETCURSEL, 0, 0);

	if (idx >= 0)
		EditItem(idx);
}

///////////////////////////////////////////////////////////////////////////

VDUIProxyComboBoxControl::VDUIProxyComboBoxControl() {
}

VDUIProxyComboBoxControl::~VDUIProxyComboBoxControl() {
}

void VDUIProxyComboBoxControl::Clear() {
	if (!mhwnd)
		return;

	SendMessageW(mhwnd, CB_RESETCONTENT, 0, 0);
}

void VDUIProxyComboBoxControl::AddItem(const wchar_t *s) {
	if (!mhwnd)
		return;

	SendMessageW(mhwnd, CB_ADDSTRING, 0, (LPARAM)s);
}

int VDUIProxyComboBoxControl::GetSelection() const {
	if (!mhwnd)
		return -1;

	return (int)SendMessage(mhwnd, CB_GETCURSEL, 0, 0);
}

void VDUIProxyComboBoxControl::SetSelection(int index) {
	if (mhwnd)
		SendMessage(mhwnd, CB_SETCURSEL, index, 0);
}

void VDUIProxyComboBoxControl::SetOnSelectionChanged(vdfunction<void(int)> fn) {
	mpOnSelectionChangedFn = std::move(fn);
}

VDZLRESULT VDUIProxyComboBoxControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (HIWORD(wParam) == CBN_SELCHANGE) {
		const int idx = GetSelection();

		if (mpOnSelectionChangedFn)
			mpOnSelectionChangedFn(idx);

		mSelectionChanged.Raise(this, idx);
	}

	return 0;
}

///////////////////////////////////////////////////////////////////////////

const VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::kNodeRoot = (NodeRef)TVI_ROOT;
const VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::kNodeFirst = (NodeRef)TVI_FIRST;
const VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::kNodeLast = (NodeRef)TVI_LAST;

VDUIProxyTreeViewControl::VDUIProxyTreeViewControl()
	: mNextTextIndex(0)
	, mhfontBold(NULL)
	, mbCreatedBoldFont(false)
	, mbIndexedMode(false)
	, mpIndexedProvider(nullptr)
	, mpEditWndProcThunk(NULL)
{
}

VDUIProxyTreeViewControl::~VDUIProxyTreeViewControl() {
	if (mpEditWndProcThunk) {
		SendMessageW(mhwnd, TVM_ENDEDITLABELNOW, TRUE, 0);

		VDDestroyFunctionThunk(mpEditWndProcThunk);
		mpEditWndProcThunk = NULL;
	}
}

void VDUIProxyTreeViewControl::SetIndexedProvider(IVDUITreeViewIndexedProvider *p) {
	mbIndexedMode = true;
	mpIndexedProvider = p;
}

IVDUITreeViewVirtualItem *VDUIProxyTreeViewControl::GetSelectedVirtualItem() const {
	if (!mhwnd)
		return NULL;

	HTREEITEM hti = TreeView_GetSelection(mhwnd);

	if (!hti)
		return NULL;

	TVITEMW itemw = {0};

	itemw.mask = LVIF_PARAM;
	itemw.hItem = hti;

	SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw);
	return (IVDUITreeViewVirtualItem *)itemw.lParam;
}

uint32 VDUIProxyTreeViewControl::GetSelectedItemId() const {
	if (!mhwnd)
		return 0;

	HTREEITEM hti = TreeView_GetSelection(mhwnd);

	if (!hti)
		return 0;

	TVITEMW itemw = {0};

	itemw.mask = LVIF_PARAM;
	itemw.hItem = hti;

	SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw);
	return (uint32)itemw.lParam;
}

IVDUITreeViewVirtualItem *VDUIProxyTreeViewControl::GetVirtualItem(NodeRef ref) const {
	if (!mhwnd)
		return NULL;

	TVITEMW itemw = {0};

	itemw.mask = LVIF_PARAM;
	itemw.hItem = (HTREEITEM)ref;

	SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw);

	return (IVDUITreeViewVirtualItem *)itemw.lParam;
}

uint32 VDUIProxyTreeViewControl::GetItemId(NodeRef ref) const {
	if (!mhwnd)
		return NULL;

	TVITEMW itemw = {0};

	itemw.mask = LVIF_PARAM;
	itemw.hItem = (HTREEITEM)ref;

	SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw);

	return (uint32)itemw.lParam;
}

void VDUIProxyTreeViewControl::Clear() {
	if (mhwnd) {
		TreeView_DeleteAllItems(mhwnd);
	}
}

void VDUIProxyTreeViewControl::DeleteItem(NodeRef ref) {
	if (mhwnd) {
		TreeView_DeleteItem(mhwnd, (HTREEITEM)ref);
	}
}

VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::AddItem(NodeRef parent, NodeRef insertAfter, const wchar_t *label) {
	if (!mhwnd)
		return NULL;

	TVINSERTSTRUCTW isw = { 0 };

	isw.hParent = (HTREEITEM)parent;
	isw.hInsertAfter = (HTREEITEM)insertAfter;
	isw.item.mask = TVIF_TEXT | TVIF_PARAM;
	isw.item.pszText = (LPWSTR)label;
	isw.item.lParam = NULL;

	return (NodeRef)SendMessageW(mhwnd, TVM_INSERTITEMW, 0, (LPARAM)&isw);
}

VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::AddVirtualItem(NodeRef parent, NodeRef insertAfter, IVDUITreeViewVirtualItem *item) {
	VDASSERT(!mbIndexedMode);

	if (!mhwnd)
		return NULL;

	HTREEITEM hti;

	TVINSERTSTRUCTW isw = { 0 };

	isw.hParent = (HTREEITEM)parent;
	isw.hInsertAfter = (HTREEITEM)insertAfter;
	isw.item.mask = TVIF_PARAM | TVIF_TEXT;
	isw.item.lParam = (LPARAM)item;
	isw.item.pszText = LPSTR_TEXTCALLBACKW;

	hti = (HTREEITEM)SendMessageW(mhwnd, TVM_INSERTITEMW, 0, (LPARAM)&isw);

	if (hti) {
		if (parent != kNodeRoot) {
			TreeView_Expand(mhwnd, (HTREEITEM)parent, TVE_EXPAND);
		}

		item->AddRef();
	}

	return (NodeRef)hti;
}

VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::AddIndexedItem(NodeRef parent, NodeRef insertAfter, uint32 id) {
	VDASSERT(mbIndexedMode);

	if (!mhwnd)
		return NULL;

	HTREEITEM hti;

	TVINSERTSTRUCTW isw = { 0 };

	isw.hParent = (HTREEITEM)parent;
	isw.hInsertAfter = (HTREEITEM)insertAfter;
	isw.item.mask = TVIF_PARAM | TVIF_TEXT;
	isw.item.lParam = (LPARAM)id;
	isw.item.pszText = LPSTR_TEXTCALLBACKW;

	hti = (HTREEITEM)SendMessageW(mhwnd, TVM_INSERTITEMW, 0, (LPARAM)&isw);

	if (hti && parent != kNodeRoot)
		TreeView_Expand(mhwnd, (HTREEITEM)parent, TVE_EXPAND);

	return (NodeRef)hti;
}

void VDUIProxyTreeViewControl::MakeNodeVisible(NodeRef node) {
	if (mhwnd) {
		TreeView_EnsureVisible(mhwnd, (HTREEITEM)node);
	}
}

void VDUIProxyTreeViewControl::SelectNode(NodeRef node) {
	if (mhwnd) {
		TreeView_SelectItem(mhwnd, (HTREEITEM)node);
	}
}

void VDUIProxyTreeViewControl::RefreshNode(NodeRef node) {
	VDASSERT(node);

	if (mhwnd) {
		TVITEMW itemw = {0};

		itemw.mask = LVIF_PARAM;
		itemw.hItem = (HTREEITEM)node;

		SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw);

		if (itemw.lParam) {
			itemw.mask = LVIF_TEXT;
			itemw.pszText = LPSTR_TEXTCALLBACKW;
			SendMessageW(mhwnd, TVM_SETITEMW, 0, (LPARAM)&itemw);
		}
	}
}

void VDUIProxyTreeViewControl::ExpandNode(NodeRef node, bool expanded) {
	VDASSERT(node);

	if (!mhwnd)
		return;

	TreeView_Expand(mhwnd, (HTREEITEM)node, expanded ? TVE_EXPAND : TVE_COLLAPSE);
}

void VDUIProxyTreeViewControl::EditNodeLabel(NodeRef node) {
	if (!mhwnd)
		return;

	::SetFocus(mhwnd);
	::SendMessageW(mhwnd, TVM_EDITLABELW, 0, (LPARAM)(HTREEITEM)node);
}

namespace {
	int CALLBACK TreeNodeCompareFn(LPARAM node1, LPARAM node2, LPARAM comparer) {
		if (!node1)
			return node2 ? -1 : 0;

		if (!node2)
			return 1;

		return ((IVDUITreeViewVirtualItemComparer *)comparer)->Compare(
			*(IVDUITreeViewVirtualItem *)node1,
			*(IVDUITreeViewVirtualItem *)node2);
	}
}

bool VDUIProxyTreeViewControl::HasChildren(NodeRef parent) const {
	return mhwnd && TreeView_GetChild(mhwnd, parent) != NULL;
}

void VDUIProxyTreeViewControl::EnumChildren(NodeRef parent, const vdfunction<void(IVDUITreeViewVirtualItem *)>& callback) {
	if (!mhwnd)
		return;

	TVITEMW itemw = {0};
	itemw.mask = LVIF_PARAM;

	HTREEITEM hti = TreeView_GetChild(mhwnd, parent);
	while(hti) {
		itemw.hItem = hti;
		if (SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw))
			callback((IVDUITreeViewVirtualItem *)itemw.lParam);

		hti = TreeView_GetNextSibling(mhwnd, hti);
	}
}

void VDUIProxyTreeViewControl::EnumChildrenRecursive(NodeRef parent, const vdfunction<void(IVDUITreeViewVirtualItem *)>& callback) {
	if (!mhwnd)
		return;

	HTREEITEM current = TreeView_GetChild(mhwnd, (HTREEITEM)parent);
	if (!current)
		return;

	vdfastvector<HTREEITEM> traversalStack;

	TVITEMW itemw = {0};
	itemw.mask = LVIF_PARAM;
	for(;;) {
		while(current) {
			itemw.hItem = current;
			if (SendMessageW(mhwnd, TVM_GETITEMW, 0, (LPARAM)&itemw) && itemw.lParam)
				callback((IVDUITreeViewVirtualItem *)itemw.lParam);

			HTREEITEM firstChild = TreeView_GetChild(mhwnd, current);
			if (firstChild)
				traversalStack.push_back(firstChild);

			current = TreeView_GetNextSibling(mhwnd, current);
		}

		if (traversalStack.empty())
			break;

		current = traversalStack.back();
		traversalStack.pop_back();
	}
}

void VDUIProxyTreeViewControl::SortChildren(NodeRef parent, IVDUITreeViewVirtualItemComparer& comparer) {
	if (!mhwnd)
		return;

	TVSORTCB scb;
	scb.hParent = (HTREEITEM)parent;
	scb.lParam = (LPARAM)&comparer;
	scb.lpfnCompare = TreeNodeCompareFn;

	SendMessageW(mhwnd, TVM_SORTCHILDRENCB, 0, (LPARAM)&scb);
}

void VDUIProxyTreeViewControl::InitImageList(uint32 n, uint32 width, uint32 height) {
	if (!mhwnd)
		return;

	if (!width || !height) {
		width = 16;

		if (HFONT hfont = (HFONT)SendMessage(mhwnd, WM_GETFONT, 0, 0)) {
			if (HDC hdc = GetDC(mhwnd)) {
				if (HGDIOBJ hOldFont = SelectObject(hdc, hfont)) {
					TEXTMETRICW tm = {};
					
					if (GetTextMetricsW(hdc, &tm)) {
						width = tm.tmAscent + tm.tmDescent;
					}

					SelectObject(hdc, hOldFont);
				}

				ReleaseDC(mhwnd, hdc);
			}
		}

		height = width;
	}

	mImageWidth = width;
	mImageHeight = height;
	HIMAGELIST imageList = ImageList_Create(width, height, ILC_COLOR32, 0, n);

	SendMessage(mhwnd, TVM_SETIMAGELIST, TVSIL_STATE, (LPARAM)imageList);

	if (mImageList)
		ImageList_Destroy(mImageList);

	mImageList = imageList;

	uint32 c = 0;
	VDPixmap pxEmpty {};
	pxEmpty.format = nsVDPixmap::kPixFormat_XRGB8888;
	pxEmpty.data = &c;
	pxEmpty.w = 1;
	pxEmpty.h = 1;
	AddImage(pxEmpty);
}

void VDUIProxyTreeViewControl::AddImage(const VDPixmap& px) {
	AddImagesToImageList(mImageList, px, mImageWidth, mImageHeight);
}

void VDUIProxyTreeViewControl::AddImages(uint32 n, const VDPixmap& px) {
	if (!mhwnd || !n)
		return;

	VDASSERT(px.w % n == 0);
	uint32 imageWidth = px.w / n;

	for(uint32 i=0; i<n; ++i) {
		AddImage(VDPixmapClip(px, imageWidth * i, 0, imageWidth, px.h));
	}
}

void VDUIProxyTreeViewControl::SetNodeImage(NodeRef node, uint32 imageIndex) {
	if (!mhwnd || !node)
		return;

	TVITEMEX tvi {};
	tvi.hItem = (HTREEITEM)node;
	tvi.mask = TVIF_STATE;
	tvi.state = INDEXTOSTATEIMAGEMASK(imageIndex);
	tvi.stateMask = TVIS_STATEIMAGEMASK;
	SendMessage(mhwnd, TVM_SETITEM, 0, (LPARAM)&tvi);
}

void VDUIProxyTreeViewControl::SetOnItemSelectionChanged(vdfunction<void()> fn) {
	mpOnItemSelectionChanged = fn;
}

void VDUIProxyTreeViewControl::SetOnBeginDrag(const vdfunction<void(const BeginDragEvent& event)>& fn) {
	mpOnBeginDrag = fn;
}

VDUIProxyTreeViewControl::NodeRef VDUIProxyTreeViewControl::FindDropTarget() const {
	POINT pt;
	if (!mhwnd || !GetCursorPos(&pt))
		return NULL;

	if (!ScreenToClient(mhwnd, &pt))
		return NULL;

	TVHITTESTINFO hti = {};
	hti.pt = pt;

	return (NodeRef)TreeView_HitTest(mhwnd, &hti);
}

void VDUIProxyTreeViewControl::SetDropTargetHighlight(NodeRef item) {
	if (mhwnd) {
		TreeView_SelectDropTarget(mhwnd, item);
	}
}

void VDUIProxyTreeViewControl::Attach(VDZHWND hwnd) {
	VDUIProxyControl::Attach(hwnd);

	if (mhwnd)
		SendMessageW(mhwnd, CCM_SETVERSION, 6, 0);
}

void VDUIProxyTreeViewControl::Detach() {
	DeleteFonts();

	if (mImageList) {
		if (mhwnd)
			SendMessage(mhwnd, TB_SETIMAGELIST, 0, 0);

		ImageList_Destroy(mImageList);
		mImageList = nullptr;
	}

	if (!mbIndexedMode)
		EnumChildrenRecursive(kNodeRoot, [](IVDUITreeViewVirtualItem *vi) { vi->Release(); });
}

VDZLRESULT VDUIProxyTreeViewControl::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	const NMHDR *hdr = (const NMHDR *)lParam;

	switch(hdr->code) {
		case TVN_GETDISPINFOA:
			{
				NMTVDISPINFOA *dispa = (NMTVDISPINFOA *)hdr;

				mTextW[0].clear();

				if (mbIndexedMode) {
					if (mpIndexedProvider)
						mpIndexedProvider->GetText((uint32)dispa->item.lParam, mTextW[0]);
				} else {
					IVDUITreeViewVirtualItem *lvvi = (IVDUITreeViewVirtualItem *)dispa->item.lParam;
					lvvi->GetText(mTextW[0]);
				}

				mTextA[mNextTextIndex] = VDTextWToA(mTextW[0]);
				dispa->item.pszText = (LPSTR)mTextA[mNextTextIndex].c_str();

				if (++mNextTextIndex >= 3)
					mNextTextIndex = 0;
			}
			break;

		case TVN_GETDISPINFOW:
			{
				NMTVDISPINFOW *dispw = (NMTVDISPINFOW *)hdr;

				mTextW[mNextTextIndex].clear();

				if (mbIndexedMode) {
					if (mpIndexedProvider)
						mpIndexedProvider->GetText((uint32)dispw->item.lParam, mTextW[mNextTextIndex]);
				} else {
					IVDUITreeViewVirtualItem *lvvi = (IVDUITreeViewVirtualItem *)dispw->item.lParam;
					lvvi->GetText(mTextW[mNextTextIndex]);
				}

				dispw->item.pszText = (LPWSTR)mTextW[mNextTextIndex].c_str();

				if (++mNextTextIndex >= 3)
					mNextTextIndex = 0;
			}
			break;

		case TVN_DELETEITEMA:
			if (!mbIndexedMode) {
				const NMTREEVIEWA *nmtv = (const NMTREEVIEWA *)hdr;
				IVDUITreeViewVirtualItem *lvvi = (IVDUITreeViewVirtualItem *)nmtv->itemOld.lParam;

				if (lvvi)
					lvvi->Release();
			}
			break;

		case TVN_DELETEITEMW:
			if (!mbIndexedMode) {
				const NMTREEVIEWW *nmtv = (const NMTREEVIEWW *)hdr;
				IVDUITreeViewVirtualItem *lvvi = (IVDUITreeViewVirtualItem *)nmtv->itemOld.lParam;

				if (lvvi)
					lvvi->Release();
			}
			break;

		case TVN_SELCHANGEDA:
		case TVN_SELCHANGEDW:
			if (mpOnItemSelectionChanged)
				mpOnItemSelectionChanged();

			mEventItemSelectionChanged.Raise(this, 0);
			break;

		case TVN_BEGINLABELEDITA:
			{
				const NMTVDISPINFOA& dispInfo = *(const NMTVDISPINFOA *)hdr;
				BeginEditEvent event {};

				event.mNode = (NodeRef)dispInfo.item.hItem;

				if (mbIndexedMode)
					event.mItemId = (uint32)dispInfo.item.lParam;
				else
					event.mpItem = (IVDUITreeViewVirtualItem *)dispInfo.item.lParam;

				event.mbAllowEdit = true;
				event.mbOverrideText = false;

				mEventItemBeginEdit.Raise(this, &event);

				if (event.mbAllowEdit) {
					HWND hwndEdit = (HWND)SendMessageA(mhwnd, TVM_GETEDITCONTROL, 0, 0);

					if (hwndEdit) {
						if (!mpEditWndProcThunk)
							mpEditWndProcThunk = VDCreateFunctionThunkFromMethod(this, &VDUIProxyTreeViewControl::FixLabelEditWndProcA, true);

						if (mpEditWndProcThunk) {
							mPrevEditWndProc = (void (*)())(WNDPROC)GetWindowLongPtrA(hwndEdit, GWLP_WNDPROC);

							if (mPrevEditWndProc)
								SetWindowLongPtrA(hwndEdit, GWLP_WNDPROC, (LONG_PTR)VDGetThunkFunction<WNDPROC>(mpEditWndProcThunk));
						}

						if (event.mbOverrideText)
							VDSetWindowTextW32(hwndEdit, event.mOverrideText.c_str());
					}
				}

				return !event.mbAllowEdit;
			}

		case TVN_BEGINLABELEDITW:
			{
				const NMTVDISPINFOW& dispInfo = *(const NMTVDISPINFOW *)hdr;
				BeginEditEvent event {};

				event.mNode = (NodeRef)dispInfo.item.hItem;

				if (mbIndexedMode)
					event.mItemId = (uint32)dispInfo.item.lParam;
				else
					event.mpItem = (IVDUITreeViewVirtualItem *)dispInfo.item.lParam;

				event.mbAllowEdit = true;
				event.mbOverrideText = false;

				mEventItemBeginEdit.Raise(this, &event);

				if (event.mbAllowEdit) {
					HWND hwndEdit = (HWND)SendMessageA(mhwnd, TVM_GETEDITCONTROL, 0, 0);

					if (hwndEdit) {
						if (!mpEditWndProcThunk)
							mpEditWndProcThunk = VDCreateFunctionThunkFromMethod(this, &VDUIProxyTreeViewControl::FixLabelEditWndProcW, true);

						if (mpEditWndProcThunk) {
							mPrevEditWndProc = (void (*)())(WNDPROC)GetWindowLongPtrW(hwndEdit, GWLP_WNDPROC);

							if (mPrevEditWndProc)
								SetWindowLongPtrW(hwndEdit, GWLP_WNDPROC, (LONG_PTR)VDGetThunkFunction<WNDPROC>(mpEditWndProcThunk));
						}

						if (event.mbOverrideText)
							VDSetWindowTextW32(hwndEdit, event.mOverrideText.c_str());
					}
				}

				return !event.mbAllowEdit;
			}

		case TVN_ENDLABELEDITA:
			{
				const NMTVDISPINFOA& dispInfo = *(const NMTVDISPINFOA *)hdr;

				if (dispInfo.item.pszText) {
					EndEditEvent event {};

					event.mNode = (NodeRef)dispInfo.item.hItem;

					if (mbIndexedMode)
						event.mItemId = (uint32)dispInfo.item.lParam;
					else
						event.mpItem = (IVDUITreeViewVirtualItem *)dispInfo.item.lParam;

					const VDStringW& text = VDTextAToW(dispInfo.item.pszText);
					event.mpNewText = text.c_str();

					mEventItemEndEdit.Raise(this, &event);
				}
			}
			break;

		case TVN_ENDLABELEDITW:
			{
				const NMTVDISPINFOW& dispInfo = *(const NMTVDISPINFOW *)hdr;

				if (dispInfo.item.pszText) {
					EndEditEvent event;

					event.mNode = (NodeRef)dispInfo.item.hItem;

					if (mbIndexedMode)
						event.mItemId = (uint32)dispInfo.item.lParam;
					else
						event.mpItem = (IVDUITreeViewVirtualItem *)dispInfo.item.lParam;

					event.mpNewText = dispInfo.item.pszText;

					mEventItemEndEdit.Raise(this, &event);
				}
			}
			break;

		case TVN_BEGINDRAGA:
			{
				const NMTREEVIEWA& info = *(const NMTREEVIEWA *)hdr;
				BeginDragEvent event {};

				event.mNode = (NodeRef)info.itemNew.hItem;

				if (mbIndexedMode)
					event.mItemId = (uint32)info.itemNew.lParam;
				else
					event.mpItem = (IVDUITreeViewVirtualItem *)info.itemNew.lParam;

				event.mPos = vdpoint32(info.ptDrag.x, info.ptDrag.y);

				if (mpOnBeginDrag)
					mpOnBeginDrag(event);
			}
			break;

		case TVN_BEGINDRAGW:
			{
				const NMTREEVIEWW& info = *(const NMTREEVIEWW *)hdr;
				BeginDragEvent event {};

				event.mNode = (NodeRef)info.itemNew.hItem;

				if (mbIndexedMode)
					event.mItemId = (uint32)info.itemNew.lParam;
				else
					event.mpItem = (IVDUITreeViewVirtualItem *)info.itemNew.lParam;

				event.mPos = vdpoint32(info.ptDrag.x, info.ptDrag.y);

				if (mpOnBeginDrag)
					mpOnBeginDrag(event);
			}
			break;

		case NM_DBLCLK:
			{
				HTREEITEM hti = TreeView_GetSelection(mhwnd);
				if (!hti)
					return false;

				// check if the double click is to the left of the label and suppress it if
				// so -- this is to prevent acting on the expand/contract button
				RECT r {};
				TreeView_GetItemRect(mhwnd, hti, &r, TRUE);

				DWORD pos = GetMessagePos();
				POINT pt = { (short)LOWORD(pos), (short)HIWORD(pos) };

				ScreenToClient(mhwnd, &pt);

				if (pt.x < r.left)
					return false;

				bool handled = false;
				mEventItemDoubleClicked.Raise(this, &handled);
				return handled;
			}

		case NM_CUSTOMDRAW:
			{
				NMTVCUSTOMDRAW& cd = *(LPNMTVCUSTOMDRAW)lParam;

				if (!mEventItemGetDisplayAttributes.IsEmpty()) {
					if (cd.nmcd.dwDrawStage == CDDS_PREPAINT) {
						return CDRF_NOTIFYITEMDRAW;
					} else if (cd.nmcd.dwDrawStage == CDDS_ITEMPREPAINT) {
						GetDispAttrEvent event {};
						
						if (mbIndexedMode)
							event.mItemId = (uint32)cd.nmcd.lItemlParam;
						else
							event.mpItem = (IVDUITreeViewVirtualItem *)cd.nmcd.lItemlParam;

						event.mbIsBold = false;
						event.mbIsMuted = false;

						mEventItemGetDisplayAttributes.Raise(this, &event);

						if (event.mbIsBold) {
							if (!mhfontBold) {
								HFONT hfont = (HFONT)::GetCurrentObject(cd.nmcd.hdc, OBJ_FONT);

								if (hfont) {
									LOGFONTW lfw = {0};
									if (::GetObject(hfont, sizeof lfw, &lfw)) {
										lfw.lfWeight = FW_BOLD;

										mhfontBold = ::CreateFontIndirectW(&lfw);
									}
								}

								mbCreatedBoldFont = true;
							}

							if (mhfontBold) {
								::SelectObject(cd.nmcd.hdc, mhfontBold);
								return CDRF_NEWFONT;
							}
						}

						if (event.mbIsMuted) {
							// blend half of the background color into the foreground color
							cd.clrText = (cd.clrText | (cd.clrTextBk & 0xFFFFFF)) - (((cd.clrText ^ cd.clrTextBk) & 0xFEFEFE) >> 1);
						}
					}
				}
			}
			return CDRF_DODEFAULT;
	}

	return 0;
}

void VDUIProxyTreeViewControl::OnFontChanged() {
	DeleteFonts();
}

VDZLRESULT VDUIProxyTreeViewControl::FixLabelEditWndProcA(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	switch(msg) {
		case WM_GETDLGCODE:
			return DLGC_WANTALLKEYS;
	}

	return ::CallWindowProcA((WNDPROC)mPrevEditWndProc, hwnd, msg, wParam, lParam);
}

VDZLRESULT VDUIProxyTreeViewControl::FixLabelEditWndProcW(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	switch(msg) {
		case WM_GETDLGCODE:
			return DLGC_WANTALLKEYS;
	}

	return ::CallWindowProcW((WNDPROC)mPrevEditWndProc, hwnd, msg, wParam, lParam);
}

void VDUIProxyTreeViewControl::DeleteFonts() {
	if (mhfontBold) {
		::DeleteObject(mhfontBold);
		mhfontBold = NULL;
		mbCreatedBoldFont = false;
	}
}

/////////////////////////////////////////////////////////////////////////////

VDUIProxyEditControl::VDUIProxyEditControl() {
}

VDUIProxyEditControl::~VDUIProxyEditControl() {
}

VDZLRESULT VDUIProxyEditControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (HIWORD(wParam) == EN_CHANGE) {
		if (mpOnTextChanged)
			mpOnTextChanged(this);
	}

	return 0;
}

VDStringW VDUIProxyEditControl::GetText() const {
	if (!mhwnd)
		return VDStringW();

	return VDGetWindowTextW32(mhwnd);
}

void VDUIProxyEditControl::SetText(const wchar_t *s) {
	if (mhwnd)
		::SetWindowText(mhwnd, s);
}

void VDUIProxyEditControl::SetOnTextChanged(vdfunction<void(VDUIProxyEditControl *)> fn) {
	mpOnTextChanged = std::move(fn);
}

/////////////////////////////////////////////////////////////////////////////

VDUIProxyRichEditControl::VDUIProxyRichEditControl() {
}

VDUIProxyRichEditControl::~VDUIProxyRichEditControl() {
	Detach();
}

void VDUIProxyRichEditControl::AppendEscapedRTF(VDStringA& buf, const wchar_t *text) {
	const VDStringA& texta = VDTextWToA(text);
	for(VDStringA::const_iterator it = texta.begin(), itEnd = texta.end();
		it != itEnd;
		++it)
	{
		const unsigned char c = *it;

		if (c < 0x20 || c > 0x80 || c == '{' || c == '}' || c == '\\')
			buf.append_sprintf("\\'%02x", c);
		else if (c == '\n')
			buf += "\\par ";
		else
			buf += c;
	}
}

bool VDUIProxyRichEditControl::IsSelectionPresent() const {
	if (!mhwnd)
		return false;

	DWORD start = 0, end = 0;
	::SendMessage(mhwnd, EM_GETSEL, (WPARAM)&start, (WPARAM)&end);

	return end > start;
}

void VDUIProxyRichEditControl::EnsureCaretVisible() {
	if (mhwnd)
		::SendMessage(mhwnd, EM_SCROLLCARET, 0, 0);
}

void VDUIProxyRichEditControl::SelectAll() {
	if (mhwnd)
		::SendMessage(mhwnd, EM_SETSEL, 0, -1);
}

void VDUIProxyRichEditControl::Copy() {
	if (mhwnd)
		::SendMessage(mhwnd, WM_COPY, 0, 0);
}

void VDUIProxyRichEditControl::SetCaretPos(int lineIndex, int charIndex) {
	if (!mhwnd)
		return;

	int lineStart = (int)::SendMessage(mhwnd, EM_LINEINDEX, (WPARAM)lineIndex, 0);

	if (lineStart >= 0)
		lineStart += charIndex;

	CHARRANGE cr = { lineStart, lineStart };
	::SendMessage(mhwnd, EM_EXSETSEL, 0, (LPARAM)&cr);
}

void VDUIProxyRichEditControl::SetText(const wchar_t *s) {
	if (mhwnd)
		::SetWindowText(mhwnd, s);
}

void VDUIProxyRichEditControl::SetTextRTF(const char *s) {
	if (!mhwnd)
		return;

	SETTEXTEX stex;
	stex.flags = ST_DEFAULT;
	stex.codepage = CP_ACP;
	SendMessageA(mhwnd, EM_SETTEXTEX, (WPARAM)&stex, (LPARAM)s);
}

void VDUIProxyRichEditControl::ReplaceSelectedText(const wchar_t *s) {
	if (!mhwnd)
		return;

	SendMessage(mhwnd, EM_REPLACESEL, TRUE, (LPARAM)s);
}

void VDUIProxyRichEditControl::SetFontFamily(const wchar_t *family) {
	if (!mhwnd)
		return;

	CHARFORMAT cf {};
	cf.cbSize = sizeof(CHARFORMAT);
	cf.dwMask = CFM_FACE;
	vdwcslcpy(cf.szFaceName, family, vdcountof(cf.szFaceName));

	SendMessage(mhwnd, EM_SETCHARFORMAT, SCF_ALL, (LPARAM)&cf);
}

void VDUIProxyRichEditControl::SetBackgroundColor(uint32 c) {
	if (mhwnd)
		SendMessage(mhwnd, EM_SETBKGNDCOLOR, FALSE, VDSwizzleU32(c) >> 8);
}

void VDUIProxyRichEditControl::SetReadOnlyBackground() {
	if (mhwnd)
		SendMessage(mhwnd, EM_SETBKGNDCOLOR, FALSE, GetSysColor(COLOR_3DFACE));
}

void VDUIProxyRichEditControl::SetPlainTextMode() {
	if (mhwnd)
		SendMessage(mhwnd, EM_SETTEXTMODE, TM_RICHTEXT, 0);
}

void VDUIProxyRichEditControl::DisableCaret() {
	if (mCaretDisabled)
		return;

	mCaretDisabled = true;

	if (mhwnd) {
		LRESULT mask = SendMessage(mhwnd, EM_GETEVENTMASK, 0, 0);
		SendMessage(mhwnd, EM_SETEVENTMASK, 0, mask | ENM_SELCHANGE);
		InitSubclass();

		// check if the window has the focus -- if so, hide the caret
		// immediately
		if (::GetFocus() == mhwnd)
			HideCaret(mhwnd);
	}
}

void VDUIProxyRichEditControl::DisableSelectOnFocus() {
	if (mhwnd)
		SendMessage(mhwnd, EM_SETOPTIONS, ECOOP_OR, ECO_SAVESEL);
}

void VDUIProxyRichEditControl::SetOnTextChanged(vdfunction<void()> fn) {
	mpOnTextChanged = std::move(fn);

	if (mhwnd) {
		LRESULT mask = SendMessage(mhwnd, EM_GETEVENTMASK, 0, 0);
		SendMessage(mhwnd, EM_SETEVENTMASK, 0, mask | ENM_CHANGE);
	}
}

void VDUIProxyRichEditControl::SetOnLinkSelected(vdfunction<bool(const wchar_t *)> fn) {
	mpOnLinkSelected = std::move(fn);

	UpdateLinkEnableStatus();
}

void VDUIProxyRichEditControl::UpdateMargins(sint32 xpad, sint32 ypad) {
	if (mhwnd) {
		vdrect32 cr = GetClientArea();
		// inset rect
		RECT r { xpad, ypad, cr.right-xpad, cr.bottom-ypad };
		SendMessage(mhwnd, EM_SETRECT, 0, (LPARAM)&r);
	}
}

void VDUIProxyRichEditControl::Attach(VDZHWND hwnd) {
	VDUIProxyControl::Attach(hwnd);

	if (mhwnd && !mpTextDoc) {
		vdrefptr<IUnknown> pUnk;
		SendMessage(mhwnd, EM_GETOLEINTERFACE, 0, (LPARAM)~pUnk);

		if (pUnk) {
			pUnk->QueryInterface<ITextDocument>(&mpTextDoc);
		}
	}

	UpdateLinkEnableStatus();

	if (mCaretDisabled)
		InitSubclass();
}

void VDUIProxyRichEditControl::Detach() {
	if (mSubclassed) {
		mSubclassed = false;

		RemoveWindowSubclass(mhwnd, StaticOnSubclassProc, (UINT_PTR)this);
	}

	if (mpTextDoc) {
		mpTextDoc->Release();
		mpTextDoc = nullptr;
	}

	VDUIProxyControl::Detach();
}

VDZLRESULT VDUIProxyRichEditControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (HIWORD(wParam) == EN_CHANGE) {
		if (mpOnTextChanged)
			mpOnTextChanged();
	}

	return 0;
}

VDZLRESULT VDUIProxyRichEditControl::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	const auto& hdr = *(const NMHDR *)lParam;

	if (hdr.code == EN_LINK) {
		const ENLINK& link = *(const ENLINK *)lParam;
		if (mpOnLinkSelected && link.msg == WM_LBUTTONDOWN) {

			struct AutoBSTR {
				BSTR p = nullptr;

				~AutoBSTR() { SysFreeString(p); }
			};

			vdrefptr<ITextRange> range;
			AutoBSTR linkText;
			if (SUCCEEDED(mpTextDoc->Range(link.chrg.cpMin, link.chrg.cpMax, ~range))) {
				range->GetText(&linkText.p);
			}

			if (mpOnLinkSelected && mpOnLinkSelected(linkText.p ? linkText.p : L""))
				return 1;
		}
	} else if (hdr.code == EN_SELCHANGE) {
		if (mCaretDisabled)
			HideCaret(mhwnd);
	}
	return 0;
}

VDZLRESULT VDZCALLBACK VDUIProxyRichEditControl::StaticOnSubclassProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam, VDZUINT_PTR uIdSubclass, VDZDWORD_PTR dwRefData) {
	return ((VDUIProxyRichEditControl *)uIdSubclass)->OnSubclassProc(hwnd, msg, wParam, lParam);
}

VDZLRESULT VDUIProxyRichEditControl::OnSubclassProc(VDZHWND hwnd, VDZUINT msg, VDZWPARAM wParam, VDZLPARAM lParam) {
	if (mCaretDisabled) {
		if (msg == WM_SETFOCUS) {
			LRESULT lr = DefSubclassProc(hwnd, msg, wParam, lParam);
			HideCaret(hwnd);
			return lr;
		}
	}

	return DefSubclassProc(hwnd, msg, wParam, lParam);
}

void VDUIProxyRichEditControl::UpdateLinkEnableStatus() {
	if (mhwnd) {
		LRESULT mask = SendMessage(mhwnd, EM_GETEVENTMASK, 0, 0);
		SendMessage(mhwnd, EM_SETEVENTMASK, 0, mpOnLinkSelected ? mask | ENM_LINK : mask & ~ENM_LINK);
	}
}

void VDUIProxyRichEditControl::InitSubclass() {
	if (mSubclassed || true)
		return;

	mSubclassed = true;
	SetWindowSubclass(mhwnd, StaticOnSubclassProc, (UINT_PTR)this, (DWORD_PTR)nullptr);
}


/////////////////////////////////////////////////////////////////////////////

VDUIProxyButtonControl::VDUIProxyButtonControl() {
}

VDUIProxyButtonControl::~VDUIProxyButtonControl() {
}

bool VDUIProxyButtonControl::GetChecked() const {
	return mhwnd && SendMessage(mhwnd, BM_GETCHECK, 0, 0) == BST_CHECKED;
}

void VDUIProxyButtonControl::SetChecked(bool enable) {
	if (mhwnd)
		SendMessage(mhwnd, BM_SETCHECK, enable ? BST_CHECKED : BST_UNCHECKED, 0);
}

VDZLRESULT VDUIProxyButtonControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (HIWORD(wParam) == BN_CLICKED) {
		if (mpOnClicked)
			mpOnClicked();
	}

	return 0;
}

void VDUIProxyButtonControl::SetOnClicked(vdfunction<void()> fn) {
	mpOnClicked = std::move(fn);
}

/////////////////////////////////////////////////////////////////////////////

VDUIProxyToolbarControl::VDUIProxyToolbarControl() {
}

VDUIProxyToolbarControl::~VDUIProxyToolbarControl() {
}

void VDUIProxyToolbarControl::Clear() {
	if (mhwnd) {
		while(SendMessage(mhwnd, TB_DELETEBUTTON, 0, 0))
			;
	}
}

void VDUIProxyToolbarControl::AddButton(uint32 id, sint32 imageIndex, const wchar_t *label) {
	if (!mhwnd)
		return;

	TBBUTTON tb {};
	tb.iBitmap = imageIndex >= 0 ? imageIndex : I_IMAGENONE;
	tb.idCommand = id;
	tb.fsState = TBSTATE_ENABLED;
	tb.fsStyle = TBSTYLE_BUTTON | BTNS_AUTOSIZE | (label ? BTNS_SHOWTEXT : 0);
	tb.iString = (INT_PTR)label;

	SendMessage(mhwnd, TB_ADDBUTTONS, 1, (LPARAM)&tb);
}

void VDUIProxyToolbarControl::AddDropdownButton(uint32 id, sint32 imageIndex, const wchar_t *label) {
	if (!mhwnd)
		return;

	TBBUTTON tb {};
	tb.iBitmap = imageIndex >= 0 ? imageIndex : I_IMAGENONE;
	tb.idCommand = id;
	tb.fsState = TBSTATE_ENABLED;
	tb.fsStyle = BTNS_WHOLEDROPDOWN | BTNS_AUTOSIZE | (label ? BTNS_SHOWTEXT : 0);
	tb.iString = (INT_PTR)label;

	SendMessage(mhwnd, TB_ADDBUTTONS, 1, (LPARAM)&tb);
}

void VDUIProxyToolbarControl::AddSeparator() {
	if (!mhwnd)
		return;

	TBBUTTON tb {};
	tb.fsState = TBSTATE_ENABLED;
	tb.fsStyle = TBSTYLE_SEP;

	SendMessage(mhwnd, TB_ADDBUTTONS, 1, (LPARAM)&tb);
}

void VDUIProxyToolbarControl::SetItemVisible(uint32 id, bool visible) {
	if (!mhwnd)
		return;

	SendMessage(mhwnd, TB_HIDEBUTTON, id, !visible);
}

void VDUIProxyToolbarControl::SetItemEnabled(uint32 id, bool enabled) {
	if (!mhwnd)
		return;

	SendMessage(mhwnd, TB_ENABLEBUTTON, id, enabled);
}

void VDUIProxyToolbarControl::SetItemPressed(uint32 id, bool enabled) {
	if (!mhwnd)
		return;

	SendMessage(mhwnd, TB_PRESSBUTTON, id, enabled);
}

void VDUIProxyToolbarControl::SetItemText(uint32 id, const wchar_t *text) {
	if (!mhwnd)
		return;

	TBBUTTONINFO info {};

	info.cbSize = sizeof(TBBUTTONINFO);
	info.dwMask = TBIF_TEXT;
	info.pszText = (LPWSTR)text;

	SendMessage(mhwnd, TB_SETBUTTONINFO, id, (LPARAM)&info);
}

void VDUIProxyToolbarControl::SetItemImage(uint32 id, sint32 imageIndex) {
	if (!mhwnd)
		return;

	SendMessage(mhwnd, TB_CHANGEBITMAP, id, imageIndex >= 0 ? imageIndex : I_IMAGENONE);
}

void VDUIProxyToolbarControl::InitImageList(uint32 n, uint32 width, uint32 height) {
	if (!mhwnd)
		return;

	mImageWidth = width;
	mImageHeight = height;
	HIMAGELIST imageList = ImageList_Create(width, height, ILC_COLOR32, 0, n);

	SendMessage(mhwnd, TB_SETIMAGELIST, 0, (LPARAM)imageList);

	if (mImageList)
		ImageList_Destroy(mImageList);

	mImageList = imageList;
}

void VDUIProxyToolbarControl::AddImage(const VDPixmap& px) {
	AddImagesToImageList(mImageList, px, mImageWidth, mImageHeight);
}

void VDUIProxyToolbarControl::AddImages(uint32 n, const VDPixmap& px) {
	if (!mhwnd || !n)
		return;

	VDASSERT(px.w % n == 0);
	uint32 imageWidth = px.w / n;

	for(uint32 i=0; i<n; ++i) {
		AddImage(VDPixmapClip(px, imageWidth * i, 0, imageWidth, px.h));
	}
}

void VDUIProxyToolbarControl::AutoSize() {
	if (mhwnd)
		SendMessage(mhwnd, TB_AUTOSIZE, 0, 0);
}

sint32 VDUIProxyToolbarControl::ShowDropDownMenu(uint32 itemId, const wchar_t *const *items) {
	if (!mhwnd)
		return -1;

	RECT r {};
	SendMessage(mhwnd, TB_GETRECT, itemId, (LPARAM)&r);
	MapWindowPoints(mhwnd, NULL, (LPPOINT)&r, 2);

	HMENU hmenu = CreatePopupMenu();

	uint32 id = 1;
	while(const wchar_t *s = *items++) {
		AppendMenu(hmenu, MF_STRING | MF_ENABLED, id++, s);
	}

	uint32 selectedId = ShowDropDownMenu(itemId, hmenu);

	DestroyMenu(hmenu);

	return (sint32)selectedId - 1;
}

uint32 VDUIProxyToolbarControl::ShowDropDownMenu(uint32 itemId, VDZHMENU hmenu) {
	if (!mhwnd)
		return -1;

	RECT r {};
	SendMessage(mhwnd, TB_GETRECT, itemId, (LPARAM)&r);
	MapWindowPoints(mhwnd, NULL, (LPPOINT)&r, 2);

	TPMPARAMS tpm;
	tpm.cbSize = sizeof(TPMPARAMS);
	tpm.rcExclude = r;
	uint32 selectedId = (uint32)TrackPopupMenuEx(hmenu, TPM_LEFTALIGN | TPM_LEFTBUTTON | TPM_VERTICAL | TPM_RETURNCMD, r.left, r.bottom, mhwnd, &tpm);

	return selectedId;
}

void VDUIProxyToolbarControl::SetOnClicked(vdfunction<void(uint32)> fn) {
	mpOnClicked = std::move(fn);
}

void VDUIProxyToolbarControl::Attach(VDZHWND hwnd) {
	VDUIProxyControl::Attach(hwnd);

	SendMessage(mhwnd, TB_SETEXTENDEDSTYLE, 0, TBSTYLE_EX_DRAWDDARROWS | TBSTYLE_EX_MIXEDBUTTONS);
	SendMessage(mhwnd, TB_BUTTONSTRUCTSIZE, sizeof(TBBUTTON), 0);
}

void VDUIProxyToolbarControl::Detach() {
	if (mImageList) {
		if (mhwnd)
			SendMessage(mhwnd, TB_SETIMAGELIST, 0, 0);

		ImageList_Destroy(mImageList);
		mImageList = nullptr;
	}

	VDUIProxyControl::Detach();
}

VDZLRESULT VDUIProxyToolbarControl::On_WM_COMMAND(VDZWPARAM wParam, VDZLPARAM lParam) {
	if (HIWORD(wParam) == BN_CLICKED) {
		if (mpOnClicked)
			mpOnClicked(LOWORD(wParam));
	}

	return 0;
}

VDZLRESULT VDUIProxyToolbarControl::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	const NMHDR& hdr = *(const NMHDR *)lParam;

	if (hdr.code == TBN_DROPDOWN) {
		const NMTOOLBAR& tbhdr = *(const NMTOOLBAR *)lParam;

		if (mpOnClicked)
			mpOnClicked(tbhdr.iItem);

		return TBDDRET_DEFAULT;
	}

	return 0;
}

///////////////////////////////////////////////////////////////////////////

VDUIProxySysLinkControl::VDUIProxySysLinkControl() {
}

VDUIProxySysLinkControl::~VDUIProxySysLinkControl() {
}

void VDUIProxySysLinkControl::SetOnClicked(vdfunction<void()> fn) {
	mpOnClicked = std::move(fn);
}

VDZLRESULT VDUIProxySysLinkControl::On_WM_NOTIFY(VDZWPARAM wParam, VDZLPARAM lParam) {
	const NMHDR& hdr = *(const NMHDR *)lParam;

	if (hdr.code == NM_CLICK) {
		if (mpOnClicked)
			mpOnClicked();
	}

	return 0;
}
