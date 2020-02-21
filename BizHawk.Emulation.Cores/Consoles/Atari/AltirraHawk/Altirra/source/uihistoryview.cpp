//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2017 Avery Lee
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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#include <stdafx.h>
#include <windows.h>
#include <windowsx.h>
#include <commctrl.h>
#include <vd2/system/strutil.h>
#include <vd2/system/thunk.h>
#include <vd2/system/w32assist.h>
#include <vd2/system/vdalloc.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atdebugger/historytree.h>
#include <at/atdebugger/historytreebuilder.h>
#include <at/atdebugger/target.h>
#include <at/atnativeui/uinativewindow.h>
#include "cpu.h"
#include "disasm.h"
#include "oshelper.h"
#include "resource.h"
#include "uihistoryview.h"

class ATUIHistoryView final : public ATUINativeWindow, public IATUIHistoryView {
public:
	ATUIHistoryView();
	~ATUIHistoryView();

	int AddRef() override;
	int Release() override;

	ATUINativeWindow *AsNativeWindow() override { return this; }
	void SetHistoryModel(IATUIHistoryModel *model) override;
	void SetDisasmMode(ATDebugDisasmMode disasmMode, uint32 subCycles, bool decodeAnticNMI) override;
	void SetFonts(HFONT hfontProp, sint32 fontPropHeight, HFONT hfontMono, sint32 fontMonoHeight) override;

	void SetTimestampOrigin(uint32 cycles, uint32 unhaltedCycles) override;

	void SelectInsn(uint32 insnIndex) override;
	void ClearInsns() override;
	void UpdateInsns(uint32 historyStart, uint32 historyEnd) override;
	void RefreshAll() override;

protected:
	LRESULT EditWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);
	bool OnCreate();
	void OnDestroy();
	void OnSize();
	void OnFontsUpdated();
	void OnLButtonDown(int x, int y, int mods);
	void OnLButtonDblClk(int x, int y, int mods);
	bool OnKeyDown(int code);
	void OnMouseWheel(int lineDelta);
	void OnHScroll(int code);
	void OnVScroll(int code);
	void OnPaint();
	void PaintItems(HDC hdc, const RECT *rPaint, uint32 itemStart, uint32 itemEnd, ATHTNode *startNode);
	const char *GetLineText(const ATHTLineIterator& it);
	void HScrollToPixel(int y);
	void ScrollToPixel(int y);
	void InvalidateNode(ATHTNode *node);
	void InvalidateStartingAtNode(ATHTNode *node);
	void InvalidateLine(const ATHTLineIterator& it);
	void SelectLine(const ATHTLineIterator& it);
	void ExpandNode(ATHTNode *node);
	void CollapseNode(ATHTNode *node);
	void EnsureLineVisible(const ATHTLineIterator& it);
	void RefreshNode(ATHTNode *node, uint32 subIndex);
	ATHTLineIterator GetLineFromClientPoint(int x, int y) const;

	void UpdateScrollMax();
	void UpdateScrollBar();
	void UpdateHScrollBar();
	bool GetLineHistoryIndex(const ATHTLineIterator& it, uint32& index) const;
	const ATCPUHistoryEntry *GetLineHistoryEntry(const ATHTLineIterator& it) const;
	const ATCPUHistoryEntry *GetSelectedLineHistoryEntry() const;

	void Reset();
	void ReloadOpcodes();
	void UpdateOpcodes(uint32 historyStart, uint32 historyEnd);
	void ClearAllNodes();

	ATHTNode *InsertNode(ATHTNode *parent, ATHTNode *after, uint32 insnOffset, ATHTNodeType nodeType);
	void RemoveNode(ATHTNode *node);

	void CopyVisibleLines();
	void CopyLines(const ATHTLineIterator& startLine, const ATHTLineIterator& endLine);

	void Search(const char *substr);

	enum {
		kMaxNestingDepth = 64,
		kControlIdPanel = 100,
		kControlIdClearButton,
		kControlIdSearchEdit
	};

	HWND mhwndPanel = nullptr;
	HWND mhwndClear = nullptr;
	HWND mhwndEdit = nullptr;
	VDFunctionThunkInfo	*mpEditThunk = nullptr;
	WNDPROC	mEditProc = nullptr;
	HMENU mMenu = nullptr;
	RECT mContentRect = {};
	ATHTLineIterator mSelectedLine = {};
	uint32 mWidth = 0;
	uint32 mHeight = 0;
	uint32 mClearButtonWidth = 0;
	uint32 mHeaderHeight = 0;
	uint32 mCharWidth = 0;
	uint32 mItemHeight = 0;
	uint32 mItemTextVOffset = 0;
	uint32 mPageItems = 0;
	uint32 mScrollX = 0;
	uint32 mScrollY = 0;
	uint32 mScrollMax = 0;
	sint32 mScrollWheelAccum = 0;

	uint32 mInsnPosStart = 0;
	uint32 mInsnPosEnd = 0;

	enum TimestampMode {
		kTsMode_Beam,
		kTsMode_Microseconds,
		kTsMode_Cycles,
		kTsMode_UnhaltedCycles,
		kTsMode_TapePositionSamples,
		kTsMode_TapePositionSeconds,
	} mTimestampMode = {};

	bool	mbFocus = false;
	bool	mbHistoryError = false;
	bool	mbUpdatesBlocked = false;
	bool	mbInvalidatesBlocked = false;
	bool	mbDirtyScrollBar = false;
	bool	mbDirtyHScrollBar = false;
	bool	mbSearchActive = false;
	bool	mbShowPCAddress = false;
	bool	mbShowGlobalPCAddress = false;
	bool	mbShowRegisters = false;
	bool	mbShowSpecialRegisters = false;
	bool	mbShowFlags = false;
	bool	mbShowCodeBytes = false;
	bool	mbShowLabels = false;
	bool	mbShowLabelNamespaces = false;
	bool	mbCollapseLoops = false;
	bool	mbCollapseCalls = false;
	bool	mbCollapseInterrupts = false;

	uint32	mTimeBaseCycles = 0;
	uint32	mTimeBaseUnhaltedCycles = 0;

	uint32	mTimeBaseCyclesDefault = 0;
	uint32	mTimeBaseUnhaltedCyclesDefault = 0;

	VDStringA mTempLine;

	ATHistoryTree mHistoryTree;

	ATHTNode *mpPreviewNode = nullptr;
	ATCPUHistoryEntry mPreviewNodeHEnt {};

	vdfastdeque<ATCPUHistoryEntry, std::allocator<ATCPUHistoryEntry>, 10> mInsnBuffer;
	vdfastvector<uint32> mFilteredInsnLookup;

	class Panel : public ATUINativeWindow {
	public:
		Panel(ATUIHistoryView *p) : mpParent(p) {}

		virtual LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
			if (msg == WM_COMMAND) {
				if (HIWORD(wParam) == BN_CLICKED && LOWORD(wParam) == kControlIdClearButton) {
					if (mpParent) {
						if (mpParent->mbSearchActive) {
							mpParent->Search(NULL);
							SetWindowTextW(mpParent->mhwndEdit, L"");
						} else {
							mpParent->Search(VDGetWindowTextAW32(mpParent->mhwndEdit).c_str());
						}
					}
					return 0;
				}
			}

			return ATUINativeWindow::WndProc(msg, wParam, lParam);
		}

		ATUIHistoryView *mpParent;
	};

	vdrefptr<Panel> mpPanel;
	VDStringW mTextSearch;
	VDStringW mTextClear;

	HFONT mhfontProp = nullptr;
	HFONT mhfontMono = nullptr;
	sint32 mFontPropHeight = 1;
	sint32 mFontMonoHeight = 1;

	uint32 mSubCycles = 0;
	ATDebugDisasmMode mDisasmMode = {};
	bool mbDecodeAnticNMI = false;
	IATUIHistoryModel *mpHistoryModel = nullptr;

	ATHistoryTreeBuilder mHistoryTreeBuilder;
};

ATUIHistoryView::ATUIHistoryView()
	: mMenu(LoadMenu(VDGetLocalModuleHandleW32(), MAKEINTRESOURCE(IDR_HISTORY_CONTEXT_MENU)))
	, mPageItems(1)
	, mTimestampMode(kTsMode_Beam)
	, mbCollapseLoops(true)
	, mbCollapseCalls(true)
	, mbCollapseInterrupts(true)
	, mbShowPCAddress(true)
	, mbShowRegisters(true)
	, mbShowFlags(true)
	, mbShowCodeBytes(true)
	, mbShowLabels(true)
	, mbShowLabelNamespaces(true)
	, mpPanel(new Panel(this))
{
	SetTouchMode(kATUITouchMode_2DPanSmooth);

	memset(&mContentRect, 0, sizeof mContentRect);

	mHistoryTreeBuilder.Init(&mHistoryTree);
	ClearAllNodes();

	mTextSearch = L"Search";
	mTextClear = L"Clear";
}

ATUIHistoryView::~ATUIHistoryView() {
	if (mMenu)
		DestroyMenu(mMenu);
}

int ATUIHistoryView::AddRef() {
	return ATUINativeWindow::AddRef();
}

int ATUIHistoryView::Release() {
	return ATUINativeWindow::Release();
}

void ATUIHistoryView::SetHistoryModel(IATUIHistoryModel *model) {
	mpHistoryModel = model;
}

void ATUIHistoryView::SetDisasmMode(ATDebugDisasmMode disasmMode, uint32 subCycles, bool decodeAnticNMI) {
	mDisasmMode = disasmMode;
	mSubCycles = subCycles;
	mbDecodeAnticNMI = decodeAnticNMI;

	RefreshAll();
}

void ATUIHistoryView::SetFonts(HFONT hfontProp, sint32 fontPropHeight, HFONT hfontMono, sint32 fontMonoHeight) {
	mhfontProp = hfontProp;
	mFontPropHeight = fontPropHeight;

	mhfontMono = hfontMono;
	mFontMonoHeight = fontMonoHeight;

	OnFontsUpdated();
}

void ATUIHistoryView::SetTimestampOrigin(uint32 cycles, uint32 unhaltedCycles) {
	mTimeBaseCycles = cycles;
	mTimeBaseCyclesDefault = cycles;
	mTimeBaseUnhaltedCycles = unhaltedCycles;
	mTimeBaseUnhaltedCyclesDefault = unhaltedCycles;
}

void ATUIHistoryView::SelectInsn(uint32 insnIndex) {
	struct ScanRange {
		ATHTNode *begin;
		ATHTNode *end;
		bool recurse;
	};

	vdfastvector<ScanRange> scanStack;

	ATHTNode *node = mHistoryTree.GetRootNode()->mpFirstChild;
	ATHTNode *endNode = nullptr;
	bool recurse = false;

	for(;;) {
		// check if we are doing a recursion range or a scan range
		if (recurse) {
			while(node != endNode) {
				ATHTNode *nextNode = node->mpNextSibling;

				// if this node has children, recurse into it to do a scan
				if (node->mpFirstChild) {
					scanStack.push_back(ScanRange { nextNode, endNode, true });

					endNode = nullptr;
					node = node->mpFirstChild;
					recurse = false;
					break;
				}

				node = nextNode;
			}
		} else {
			// scan the given range
			ATHTNode *recurseStart = node;

			for(;;) {
				// find next immediate insn node
				ATHTNode *nextInsnNode = node;

				for(; nextInsnNode != endNode; nextInsnNode = nextInsnNode->mpNextSibling) {
					if (nextInsnNode->mNodeType == kATHTNodeType_Insn)
						break;
				}

				if (nextInsnNode != endNode) {
					// check if instruction is within this node
					const uint32 lineOffset = insnIndex - nextInsnNode->mInsn.mOffset;
					if (lineOffset < nextInsnNode->mInsn.mCount) {
						// we're done -- select line within this node and exit
						const ATHTLineIterator lineIt { nextInsnNode, lineOffset };
						SelectLine(lineIt);
						EnsureLineVisible(lineIt);
						return;
					}
				}

				// check if instruction is before this node
				if (nextInsnNode == endNode || insnIndex < nextInsnNode->mInsn.mOffset) {
					// yes -- if insn exists, it will be in a child of a preceding node, so
					// begin recursing into each one
					recurse = true;
					endNode = nextInsnNode;
					node = recurseStart;
					break;
				} else {
					// no -- continue scan with this node
					recurseStart = node;
					node = nextInsnNode->mpNextSibling;
				}
			}
		}

		// pop next range off stack
		if (node == endNode) {
			if (scanStack.empty())
				break;

			ScanRange scanRange = scanStack.back();
			node = scanRange.begin;
			endNode = scanRange.end;
			recurse = scanRange.recurse;

			scanStack.pop_back();
		}
	}
}

void ATUIHistoryView::ClearInsns() {
	Reset();
}

void ATUIHistoryView::UpdateInsns(uint32 historyStart, uint32 historyEnd) {
	UpdateOpcodes(historyStart, historyEnd);
}

void ATUIHistoryView::RefreshAll() {
	if (mhwnd)
		InvalidateRect(mhwnd, NULL, TRUE);
}

LRESULT ATUIHistoryView::EditWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN) {
		if (wParam == VK_ESCAPE) {
			SetFocus(mhwnd);
			return 0;
		}
	} else if (msg == WM_CHAR || msg == WM_SYSCHAR) {
		if (wParam == '\r') {
			Search(VDGetWindowTextAW32(hwnd).c_str());
			return 0;
		}
	}

	return CallWindowProc(mEditProc, hwnd, msg, wParam, lParam);
}

LRESULT ATUIHistoryView::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			OnCreate();
			break;

		case WM_DESTROY:
			OnDestroy();
			break;

		case WM_SIZE:
			OnSize();
			break;

		case WM_PAINT:
			OnPaint();
			return 0;

		case WM_ERASEBKGND:
			return 0;

		case WM_HSCROLL:
			OnHScroll(LOWORD(wParam));
			return 0;

		case WM_VSCROLL:
			OnVScroll(LOWORD(wParam));
			return 0;

		case WM_KEYDOWN:
			if (OnKeyDown((int)wParam))
				return 0;
			break;

		case WM_LBUTTONDOWN:
			::SetFocus(mhwnd);
			OnLButtonDown(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam), (int)wParam);
			return 0;

		case WM_LBUTTONDBLCLK:
			::SetFocus(mhwnd);
			OnLButtonDblClk(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam), (int)wParam);
			return 0;

		case WM_MOUSEWHEEL:
			OnMouseWheel((short)HIWORD(wParam));
			break;

		case WM_CONTEXTMENU:
			{
				int x = GET_X_LPARAM(lParam);
				int y = GET_Y_LPARAM(lParam);

				HMENU menu = GetSubMenu(mMenu, 0);

				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWPCADDRESS, mbShowPCAddress);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWGLOBALPCADDRESS, mbShowGlobalPCAddress);
				VDEnableMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWGLOBALPCADDRESS, mbShowPCAddress);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWREGISTERS, mbShowRegisters);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWSPECIALREGISTERS, mbShowSpecialRegisters);
				VDEnableMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWSPECIALREGISTERS, mbShowRegisters);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWFLAGS, mbShowFlags);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWCODEBYTES, mbShowCodeBytes);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWLABELS, mbShowLabels);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWLABELNAMESPACES, mbShowLabelNamespaces);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_COLLAPSELOOPS, mbCollapseLoops);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_COLLAPSECALLS, mbCollapseCalls);
				VDCheckMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_COLLAPSEINTERRUPTS, mbCollapseInterrupts);
				VDCheckRadioMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWBEAMPOSITION, mTimestampMode == kTsMode_Beam);
				VDCheckRadioMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWMICROSECONDS, mTimestampMode == kTsMode_Microseconds);
				VDCheckRadioMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWCYCLES, mTimestampMode == kTsMode_Cycles);
				VDCheckRadioMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWUNHALTEDCYCLES, mTimestampMode == kTsMode_UnhaltedCycles);
				VDCheckRadioMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWTAPEPOSITIONSAMPLES, mTimestampMode == kTsMode_TapePositionSamples);
				VDCheckRadioMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SHOWTAPEPOSITIONSECONDS, mTimestampMode == kTsMode_TapePositionSeconds);

				POINT pt = {x, y};
				ScreenToClient(mhwnd, &pt);
				ATHTLineIterator it = GetLineFromClientPoint(pt.x, pt.y);
				SelectLine(it);

				VDEnableMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_GOTOSOURCE, !!it);
				VDEnableMenuItemByCommandW32(menu, ID_HISTORYCONTEXTMENU_SETTIMESTAMPORIGIN, it && it.mpNode->mNodeType == kATHTNodeType_Insn);

				TrackPopupMenu(menu, TPM_LEFTALIGN|TPM_TOPALIGN, x, y, 0, mhwnd, NULL);
			}
			return 0;

		case WM_COMMAND:
			switch(LOWORD(wParam)) {
				case ID_HISTORYCONTEXTMENU_GOTOSOURCE:
					{
						const ATCPUHistoryEntry *p = GetSelectedLineHistoryEntry();
						if (p)
							mpHistoryModel->JumpToSource(p->mPC);
					}
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWPCADDRESS:
					mbShowPCAddress = !mbShowPCAddress;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWGLOBALPCADDRESS:
					mbShowGlobalPCAddress = !mbShowGlobalPCAddress;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWREGISTERS:
					mbShowRegisters = !mbShowRegisters;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWSPECIALREGISTERS:
					mbShowSpecialRegisters = !mbShowSpecialRegisters;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWFLAGS:
					mbShowFlags = !mbShowFlags;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWCODEBYTES:
					mbShowCodeBytes = !mbShowCodeBytes;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWLABELS:
					mbShowLabels = !mbShowLabels;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;
				case ID_HISTORYCONTEXTMENU_SHOWLABELNAMESPACES:
					mbShowLabelNamespaces = !mbShowLabelNamespaces;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_COLLAPSELOOPS:
					mbCollapseLoops = !mbCollapseLoops;
					ReloadOpcodes();
					return true;

				case ID_HISTORYCONTEXTMENU_COLLAPSECALLS:
					mbCollapseCalls = !mbCollapseCalls;
					ReloadOpcodes();
					return true;

				case ID_HISTORYCONTEXTMENU_COLLAPSEINTERRUPTS:
					mbCollapseInterrupts = !mbCollapseInterrupts;
					ReloadOpcodes();
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWBEAMPOSITION:
					mTimestampMode = kTsMode_Beam;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWMICROSECONDS:
					mTimestampMode = kTsMode_Microseconds;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWCYCLES:
					mTimestampMode = kTsMode_Cycles;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWUNHALTEDCYCLES:
					mTimestampMode = kTsMode_UnhaltedCycles;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWTAPEPOSITIONSAMPLES:
					mTimestampMode = kTsMode_TapePositionSamples;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_SHOWTAPEPOSITIONSECONDS:
					mTimestampMode = kTsMode_TapePositionSeconds;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_RESETTIMESTAMPORIGIN:
					mTimeBaseCycles = mTimeBaseCyclesDefault;
					mTimeBaseUnhaltedCycles = mTimeBaseUnhaltedCyclesDefault;
					InvalidateRect(mhwnd, NULL, TRUE);
					return true;

				case ID_HISTORYCONTEXTMENU_SETTIMESTAMPORIGIN:
					if (const ATCPUHistoryEntry *he = GetSelectedLineHistoryEntry()) {
						mTimeBaseCycles = he->mCycle;
						mTimeBaseUnhaltedCycles = he->mUnhaltedCycle;
						InvalidateRect(mhwnd, NULL, TRUE);
					}

					return true;

				case ID_HISTORYCONTEXTMENU_COPYVISIBLETOCLIPBOARD:
					CopyVisibleLines();
					return true;
			}

			break;

		case WM_SETFOCUS:
			if (!mbFocus) {
				mbFocus = true;
				InvalidateLine(mSelectedLine);
			}
			break;

		case WM_KILLFOCUS:
			if (mbFocus) {
				mbFocus = false;
				InvalidateLine(mSelectedLine);
			}
			break;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

bool ATUIHistoryView::OnCreate() {
	mhwndPanel = CreateWindowEx(0, MAKEINTATOM(ATUINativeWindow::Register()), _T(""), WS_VISIBLE|WS_CHILD|WS_CLIPCHILDREN, 0, 0, 0, 0, mhwnd, (HMENU)kControlIdPanel, VDGetLocalModuleHandleW32(), mpPanel);
	if (!mhwndPanel)
		return false;

	mhwndClear = CreateWindowEx(0, WC_BUTTON, _T(""), WS_VISIBLE|WS_CHILD|BS_CENTER, 0, 0, 0, 0, mhwndPanel, (HMENU)kControlIdClearButton, VDGetLocalModuleHandleW32(), NULL);
	if (!mhwndClear)
		return false;

	mhwndEdit = CreateWindowEx(WS_EX_CLIENTEDGE, WC_EDIT, _T(""), WS_VISIBLE|WS_CHILD, 0, 0, 0, 0, mhwndPanel, (HMENU)kControlIdSearchEdit, VDGetLocalModuleHandleW32(), NULL);
	if (!mhwndEdit)
		return false;

	SendMessageW(mhwndEdit, EM_SETCUEBANNER, FALSE, (LPARAM)L"substring");

	OnFontsUpdated();

	VDSetWindowTextW32(mhwndClear, mTextSearch.c_str());

	mpEditThunk = VDCreateFunctionThunkFromMethod(this, &ATUIHistoryView::EditWndProc, true);
	mEditProc = (WNDPROC)GetWindowLongPtr(mhwndEdit, GWLP_WNDPROC);
	SetWindowLongPtr(mhwndEdit, GWLP_WNDPROC, (LONG_PTR)VDGetThunkFunction<WNDPROC>(mpEditThunk));

	OnSize();
	Reset();
	UpdateOpcodes(mInsnPosStart, mInsnPosEnd);
	return true;
}

void ATUIHistoryView::OnDestroy() {
	if (mpPanel) {
		mpPanel->mpParent = NULL;
		mpPanel = NULL;
	}

	mHistoryTree.Clear();

	mhwndPanel = NULL;

	if (mhwndEdit) {
		DestroyWindow(mhwndEdit);
		mhwndEdit = NULL;
	}

	if (mpEditThunk) {
		VDDestroyFunctionThunk(mpEditThunk);
		mpEditThunk = NULL;
	}
}

void ATUIHistoryView::OnSize() {
	RECT r;

	if (!GetClientRect(mhwnd, &r))
		return;

	mWidth = r.right;
	mHeight = r.bottom;
	mHeaderHeight = 0;

	if (mhwndPanel) {
		mHeaderHeight = std::max(mFontMonoHeight, mFontPropHeight) + 2*GetSystemMetrics(SM_CYEDGE);

		VDVERIFY(SetWindowPos(mhwndPanel, NULL, 0, 0, mWidth, mHeaderHeight, SWP_NOZORDER|SWP_NOACTIVATE));

		if (mhwndClear)
			VDVERIFY(SetWindowPos(mhwndClear, NULL, 0, 0, mClearButtonWidth, mHeaderHeight, SWP_NOZORDER|SWP_NOACTIVATE));

		if (mhwndEdit)
			VDVERIFY(SetWindowPos(mhwndEdit, NULL, mClearButtonWidth, 0, mWidth < mClearButtonWidth ? 0 : mWidth - mClearButtonWidth, mHeaderHeight, SWP_NOZORDER|SWP_NOACTIVATE));
	}

	mContentRect = r;
	mContentRect.top = mHeaderHeight;
	if (mContentRect.top > mContentRect.bottom)
		mContentRect.top = mContentRect.bottom;

	if (mbHistoryError) {
		InvalidateRect(mhwnd, NULL, TRUE);
		return;
	}

	mPageItems = 0;

	if (mHeight > mHeaderHeight && mItemHeight)
		mPageItems = (mHeight - mHeaderHeight) / mItemHeight;

	if (!mPageItems)
		mPageItems = 1;

	UpdateScrollMax();
	UpdateScrollBar();
	UpdateHScrollBar();

	ScrollToPixel(mScrollY);
	HScrollToPixel(mScrollX);
}

void ATUIHistoryView::OnFontsUpdated() {
	SendMessage(mhwndClear, WM_SETFONT, (WPARAM)mhfontProp, TRUE);
	SendMessage(mhwndEdit, WM_SETFONT, (WPARAM)mhfontMono, TRUE);

	mCharWidth = 12;
	mItemHeight = 16;
	mItemTextVOffset = 0;

	if (HDC hdc = GetDC(mhwnd)) {
		SelectObject(hdc, mhfontMono);

		TEXTMETRIC tm = {0};
		if (GetTextMetrics(hdc, &tm)) {
			mCharWidth = tm.tmAveCharWidth;
			mItemHeight = tm.tmHeight;
			mItemTextVOffset = 0;
		}

		SelectObject(hdc, mhfontProp);

		mClearButtonWidth = 0;

		SIZE sz;
		if (GetTextExtentPoint32W(hdc, mTextSearch.data(), mTextSearch.size(), &sz))
			mClearButtonWidth = sz.cx;

		if (GetTextExtentPoint32W(hdc, mTextClear.data(), mTextClear.size(), &sz)) {
			if (sz.cx > 0 && mClearButtonWidth < (uint32)sz.cx)
				mClearButtonWidth = (uint32)sz.cx;
		}

		mClearButtonWidth += 8 * GetSystemMetrics(SM_CXEDGE);

		ReleaseDC(mhwnd, hdc);
	}

	OnSize();
	InvalidateRect(mhwnd, NULL, TRUE);
}

void ATUIHistoryView::OnLButtonDown(int x, int y, int mods) {
	ATHTLineIterator it = GetLineFromClientPoint(x, y);

	if (!it) {
		SelectLine(it);
		return;
	}

	int level = 0;
	for(ATHTNode *p = it.mpNode->mpParent; p; p = p->mpParent)
		++level;

	x += mScrollX;

	if (x >= level * (int)mItemHeight) {
		SelectLine(it);
	} else if (x >= (level - 1)*(int)mItemHeight && it.mpNode->mpFirstChild) {
		if (it.mpNode->mbExpanded)
			CollapseNode(it.mpNode);
		else
			ExpandNode(it.mpNode);
	}
}

void ATUIHistoryView::OnLButtonDblClk(int x, int y, int mods) {
	ATHTLineIterator it = GetLineFromClientPoint(x, y);

	if (!it) {
		SelectLine(it);
		return;
	}

	int level = 0;
	for(ATHTNode *p = it.mpNode->mpParent; p; p = p->mpParent)
		++level;

	if (x >= level * (int)mItemHeight) {
		SelectLine(it);

		const ATCPUHistoryEntry *p = GetLineHistoryEntry(it);
		if (p)
			mpHistoryModel->JumpToInsn(p->mPC);
	} else if (x >= (level - 1)*(int)mItemHeight && it.mpNode->mpFirstChild) {
		if (it.mpNode->mbExpanded)
			CollapseNode(it.mpNode);
		else
			ExpandNode(it.mpNode);
	}
}

bool ATUIHistoryView::OnKeyDown(int code) {
	switch(code) {
		case VK_ESCAPE:
			mpHistoryModel->OnEsc();
			break;

		case VK_PRIOR:
			if (mSelectedLine) {
				ATHTLineIterator it = mSelectedLine;

				for(uint32 i=0; i<mPageItems; ++i) {
					ATHTLineIterator it2 = mHistoryTree.GetPrevVisibleLine(it);

					if (!it2)
						break;

					it = it2;
				}

				SelectLine(it);
			}
			break;

		case VK_NEXT:
			if (mSelectedLine) {
				ATHTLineIterator it = mSelectedLine;

				for(uint32 i=0; i<mPageItems; ++i) {
					ATHTLineIterator it2 = mHistoryTree.GetNextVisibleLine(it);

					if (!it2)
						break;

					it = it2;
				}

				SelectLine(it);
			}
			break;

		case VK_UP:
			if (ATHTLineIterator it = mHistoryTree.GetPrevVisibleLine(mSelectedLine))
				SelectLine(it);
			break;

		case VK_DOWN:
			if (ATHTLineIterator it = mHistoryTree.GetNextVisibleLine(mSelectedLine))
				SelectLine(it);
			break;

		case VK_LEFT:
			if (mSelectedLine) {
				if (mSelectedLine.mpNode->mbExpanded) {
					CollapseNode(mSelectedLine.mpNode);
					EnsureLineVisible(mSelectedLine);
				} else {
					ATHTNode *p = mSelectedLine.mpNode->mpParent;
					if (p->mpParent) {
						SelectLine(ATHTLineIterator { p, p->mVisibleLines - 1 });
					}
				}
			}
			break;
		case VK_RIGHT:
			if (mSelectedLine) {
				ATHTNode *child = mSelectedLine.mpNode->mpFirstChild;
				
				if (child) {
					if (!mSelectedLine.mpNode->mbExpanded) {
						ExpandNode(mSelectedLine.mpNode);
						EnsureLineVisible(mSelectedLine);
					} else {
						SelectLine(ATHTLineIterator { child, 0 });
					}
				}
			}
			break;
		case VK_HOME:
			if (ATHTNode *node = mHistoryTree.GetFrontNode())
				SelectLine(mHistoryTree.GetNearestVisibleLine(ATHTLineIterator { node, 0 }));
			break;
		case VK_END:
			if (ATHTNode *node = mHistoryTree.GetBackNode()) {
				ATHTLineIterator it { node, node->mVisibleLines - 1 };

				if (!mHistoryTree.IsLineVisible(it))
					it = mHistoryTree.GetPrevVisibleLine(it);

				if (it)
					SelectLine(it);
			}
			break;
		default:
			return false;
	}

	return true;
}

void ATUIHistoryView::OnMouseWheel(int dz) {
	mScrollWheelAccum += dz;

	int actions = mScrollWheelAccum / WHEEL_DELTA;
	if (!actions)
		return;

	mScrollWheelAccum -= actions * WHEEL_DELTA;

	UINT linesPerAction;
	if (SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, &linesPerAction, FALSE)) {
		ScrollToPixel(mScrollY - (int)linesPerAction * actions * (int)mItemHeight);
	}
}

void ATUIHistoryView::OnHScroll(int code) {
	SCROLLINFO si = {0};
	si.cbSize = sizeof(SCROLLINFO);
	si.fMask = SIF_TRACKPOS | SIF_POS | SIF_PAGE | SIF_RANGE;

	GetScrollInfo(mhwnd, SB_HORZ, &si);

	int pos = si.nPos;

	switch(code) {
		case SB_TOP:
			pos = si.nMin;
			break;

		case SB_BOTTOM:
			pos = si.nMax;
			break;

		case SB_ENDSCROLL:
			break;

		case SB_LINEDOWN:
			if (si.nMax - pos >= 16 * (int)mItemHeight)
				pos += 16 * mItemHeight;
			else
				pos = si.nMax;
			break;

		case SB_LINEUP:
			if (pos - si.nMin >= 16 * (int)mItemHeight)
				pos -= 16 * mItemHeight;
			else
				pos = si.nMin;
			break;

		case SB_PAGEDOWN:
			if (si.nMax - pos >= (int)si.nPage)
				pos += si.nPage;
			else
				pos = si.nMax;
			break;

		case SB_PAGEUP:
			if (pos - si.nMin >= (int)si.nPage)
				pos -= si.nPage;
			else
				pos = si.nMin;
			break;

		case SB_THUMBPOSITION:
		case SB_THUMBTRACK:
			pos = si.nTrackPos;
			break;
	}

	if (pos != si.nPos) {
		si.nPos = pos;
		si.fMask = SIF_POS;
		SetScrollInfo(mhwnd, SB_HORZ, &si, TRUE);
	}

	HScrollToPixel(pos);
}

void ATUIHistoryView::OnVScroll(int code) {
	SCROLLINFO si = {0};
	si.cbSize = sizeof(SCROLLINFO);
	si.fMask = SIF_TRACKPOS | SIF_POS | SIF_PAGE | SIF_RANGE;

	GetScrollInfo(mhwnd, SB_VERT, &si);

	int pos = si.nPos;

	switch(code) {
		case SB_TOP:
			pos = si.nMin;
			break;

		case SB_BOTTOM:
			pos = si.nMax;
			break;

		case SB_ENDSCROLL:
			break;

		case SB_LINEDOWN:
			if (si.nMax - pos >= (int)mItemHeight)
				pos += mItemHeight;
			else
				pos = si.nMax;
			break;

		case SB_LINEUP:
			if (pos - si.nMin >= (int)mItemHeight)
				pos -= mItemHeight;
			else
				pos = si.nMin;
			break;

		case SB_PAGEDOWN:
			if (si.nMax - pos >= (int)si.nPage)
				pos += si.nPage;
			else
				pos = si.nMax;
			break;

		case SB_PAGEUP:
			if (pos - si.nMin >= (int)si.nPage)
				pos -= si.nPage;
			else
				pos = si.nMin;
			break;

		case SB_THUMBPOSITION:
		case SB_THUMBTRACK:
			pos = si.nTrackPos;
			break;
	}

	if (pos != si.nPos) {
		si.nPos = pos;
		si.fMask = SIF_POS;
		SetScrollInfo(mhwnd, SB_VERT, &si, TRUE);
	}

	ScrollToPixel(pos);
}

void ATUIHistoryView::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhwnd, &ps);
	if (!hdc)
		return;

	int sdc = SaveDC(hdc);
	if (sdc) {
		SelectObject(hdc, mhfontMono);

		int y1 = ps.rcPaint.top + mScrollY - mHeaderHeight;
		int y2 = ps.rcPaint.bottom + mScrollY - mHeaderHeight;

		if (y1 < 0)
			y1 = 0;

		if (y2 < y1)
			y2 = y1;

		uint32 itemStart = (uint32)y1 / mItemHeight;
		uint32 itemEnd = (uint32)(y2 + mItemHeight - 1) / mItemHeight;

		SetBkMode(hdc, OPAQUE);
		SetTextAlign(hdc, TA_LEFT | TA_TOP);

		ATHTNode *root = mHistoryTree.GetRootNode();
		if (root->mpFirstChild) {
			PaintItems(hdc, &ps.rcPaint, itemStart, itemEnd, root->mpFirstChild);
		}

		if (itemEnd > root->mHeight - 1) {
			SetBkColor(hdc, GetSysColor(COLOR_WINDOW));

			RECT r = ps.rcPaint;
			r.top = (root->mHeight - 1) * mItemHeight - mScrollY + mHeaderHeight;
			ExtTextOut(hdc, r.left, r.top, ETO_OPAQUE, &r, _T(""), 0, NULL);
		}

		const bool kShowRedraws = false;
		if constexpr(kShowRedraws) {
			static unsigned cycle = 0;

			SelectObject(hdc, GetStockObject(DC_PEN));
			SelectObject(hdc, GetStockObject(NULL_BRUSH));

			static constexpr COLORREF kColors[3] = { RGB(255,0,0), RGB(0,255,0), RGB(0,0,255) };
			SetDCPenColor(hdc, kColors[cycle]);
			cycle = (cycle >= 2) ? 0 : cycle+1;

			Rectangle(hdc, ps.rcPaint.left, ps.rcPaint.top, ps.rcPaint.right, ps.rcPaint.bottom);
		}

		RestoreDC(hdc, sdc);
	}

	EndPaint(mhwnd, &ps);
}

void ATUIHistoryView::PaintItems(HDC hdc, const RECT *rPaint, uint32 itemStart, uint32 itemEnd, ATHTNode *startNode) {
	if (!startNode)
		return;

	uint32 pos = 0;
	uint32 level = 0;

	ATHTNode *baseParent = startNode->mpParent;
	ATHTNode *node = startNode;

	while(pos < itemEnd) {
		// Check if the node is visible at all. If not, we can skip rendering and traversal.
		if (!node->mbVisible) {
			// skip invisible node
		} else if (pos + node->mHeight <= itemStart) {
			pos += node->mHeight;
		} else {
			uint32 lineCount = std::min<uint32>(node->mVisibleLines, itemEnd - pos);
			uint32 lineIndex = pos < itemStart ? itemStart - pos : 0;
			const bool nodeSelected = (mSelectedLine.mpNode == node);
			const bool isFiltered = (lineCount > 1 && node->mbFiltered);
			const int x = mItemHeight * level - mScrollX;
			int y = (pos + lineIndex) * mItemHeight + mHeaderHeight - mScrollY;
			uint32 insnOffset = node->mInsn.mOffset;

			for(; lineIndex < lineCount; ++lineIndex, ++insnOffset) {
				// draw the node
				bool selected = false;

				uint32 bgc;
				uint32 fgc;
				if (nodeSelected && mSelectedLine.mLineIndex == lineIndex) {
					selected = true;
					
					bgc = GetSysColor(mbFocus ? COLOR_HIGHLIGHT : COLOR_3DFACE);
					fgc = GetSysColor(mbFocus ? COLOR_HIGHLIGHTTEXT : COLOR_WINDOWTEXT);
				} else {
					bgc = GetSysColor(COLOR_WINDOW);
					fgc = GetSysColor(COLOR_WINDOWTEXT);
				}

				SetBkColor(hdc, bgc);
				SetTextColor(hdc, fgc);

				RECT rOpaque;
				rOpaque.left = x;
				rOpaque.top = y;
				rOpaque.right = mWidth;
				rOpaque.bottom = y + mItemHeight;

				const char *s = GetLineText(ATHTLineIterator { node, lineIndex });

				ExtTextOutA(hdc, x + mItemHeight, rOpaque.top + mItemTextVOffset, ETO_OPAQUE | ETO_CLIPPED, &rOpaque, s, (UINT)strlen(s), NULL);

				RECT rPad;
				rPad.left = rPaint->left;
				rPad.top = y;
				rPad.right = x;
				rPad.bottom = y + mItemHeight;

				FillRect(hdc, &rPad, (HBRUSH)(COLOR_WINDOW + 1));

				if (node->mpFirstChild) {
					SelectObject(hdc, selected && mbFocus ? GetStockObject(WHITE_PEN) : GetStockObject(BLACK_PEN));

					int boxsize = (mItemHeight - 3) & ~1;
					int x1 = x + 1;
					int y1 = y + 1;
					int x2 = x1 + boxsize;
					int y2 = y1 + boxsize;

					MoveToEx(hdc, x1, y1, NULL);
					LineTo(hdc, x2, y1);
					LineTo(hdc, x2, y2);
					LineTo(hdc, x1, y2);
					LineTo(hdc, x1, y1);

					int xh = (x1 + x2) >> 1;
					int yh = (y1 + y2) >> 1;
					MoveToEx(hdc, x1 + 2, yh, NULL);
					LineTo(hdc, x2 - 1, yh);

					if (!node->mbExpanded) {
						MoveToEx(hdc, xh, y1 + 2, NULL);
						LineTo(hdc, xh, y2 - 1);
					}
				}

				y += mItemHeight;
			}

			// Check if we should recurse.
			if (node->mbExpanded && node->mpFirstChild) {
				++pos;
				++level;

				node = node->mpFirstChild;
				continue;
			} else
				pos += node->mHeight;
		}

		for(;;) {
			// exit if we've hit the end of the tree
			if (node == baseParent)
				return;

			// step to next tree node
			if (node->mpNextSibling) {
				node = node->mpNextSibling;
				break;
			} else {
				node = node->mpParent;
				--level;
			}
		}
	}
}

namespace {
	static const char kHexDig[]="0123456789ABCDEF";

	void FastFormat02X(char *s, uint8 v) {
		s[0] = kHexDig[v >> 4];
		s[1] = kHexDig[v & 15];
	}

	void FastFormat02U(char *s, uint32 v) {
		s[0] = (char)('0' + v / 10);
		s[1] = (char)('0' + v % 10);
	}

	void FastFormat3U(char *s, uint32 v) {
		if (v < 10) {
			s[0] = ' ';
			s[1] = ' ';
			s[2] = (char)('0' + v);
		} else {
			uint32 d0 = v % 10; v /= 10;
			uint32 d1 = v % 10; v /= 10;

			if (v)
				s[0] = (char)('0' + v);
			else
				s[0] = ' ';

			s[1] = (char)('0' + d1);
			s[2] = (char)('0' + d0);
		}
	}

	int FastFormat10D(char *s, uint32 v) {
		for(int i=9; i>=0; --i) {
			s[i] = (char)('0' + (v % 10));
			v /= 10;

			if (!v)
				return i;
		}

		return 0;
	}
}

const char *ATUIHistoryView::GetLineText(const ATHTLineIterator& it) {
	ATHTNode *node = it.mpNode;
	const char *s = nullptr;

	switch(node->mNodeType) {
		case kATHTNodeType_Repeat:
			if (node->mRepeat.mSize == 1)
				mTempLine.sprintf("Last insn repeated %u times", node->mRepeat.mCount);
			else
				mTempLine.sprintf("Last %u insns repeated %u times", node->mRepeat.mSize, node->mRepeat.mCount);

			s = mTempLine.c_str();
			break;

		case kATHTNodeType_Interrupt:
			{
				const ATCPUHistoryEntry& hent = *GetLineHistoryEntry(it);

				uint32 ts = mpHistoryModel->ConvertRawTimestamp(hent.mCycle);

				if (!mbDecodeAnticNMI) {
					if (hent.mbNMI)
						mTempLine = hent.mbIRQ ? "FIRQ interrupt" : "NMI interrupt";
					else
						mTempLine = "IRQ interrupt";
				} else {
					mTempLine = hent.mbNMI ? mpHistoryModel->IsInterruptPositionVBI(ts) ? "NMI interrupt (VBI)" : "NMI interrupt (DLI)" : "IRQ interrupt";
				}
				s = mTempLine.c_str();
			}
			break;

		case kATHTNodeType_Insn:
		case kATHTNodeType_InsnPreview:
			{
				const bool is65C816 = mDisasmMode == kATDebugDisasmMode_65C816;
				const ATCPUHistoryEntry& hent = *GetLineHistoryEntry(it);

				if (node->mNodeType == kATHTNodeType_Insn) {
					switch(mTimestampMode) {
						case kTsMode_Beam:{
							uint32 ts = mpHistoryModel->ConvertRawTimestamp(hent.mCycle);
							const auto& beamPos = mpHistoryModel->DecodeBeamPosition(ts);

							if (mSubCycles >= 10) {
								mTempLine.sprintf("%3d:%3d:%3d.%2u | "
										, beamPos.mFrame
										, beamPos.mY
										, beamPos.mX
										, hent.mSubCycle
									);
							} else if (mSubCycles > 1) {
								mTempLine.sprintf("%3d:%3d:%3d.%u | "
										, beamPos.mFrame
										, beamPos.mY
										, beamPos.mX
										, hent.mSubCycle
									);
							} else {
								char tsbuf[] = "0000000000:  0:  0 | ";

								const int offset = FastFormat10D(tsbuf, beamPos.mFrame);
								FastFormat3U(tsbuf + 11, beamPos.mY);
								FastFormat3U(tsbuf + 15, beamPos.mX);

								mTempLine.assign(tsbuf + offset, tsbuf + sizeof(tsbuf) - 1);
							}
							break;
						}

						case kTsMode_Microseconds: {
							mTempLine.sprintf("T%+.6f | ", mpHistoryModel->ConvertRawTimestampDeltaF((sint32)(hent.mCycle - mTimeBaseCycles)));
							break;
						}

						case kTsMode_Cycles: {
							mTempLine.sprintf("T%+-4d | ", (sint32)(hent.mCycle - mTimeBaseCycles));
							break;
						}

						case kTsMode_UnhaltedCycles: {
							mTempLine.sprintf("T%+-4d | ", (sint32)(hent.mUnhaltedCycle - mTimeBaseUnhaltedCycles));
							break;
						}

						case kTsMode_TapePositionSeconds:
						case kTsMode_TapePositionSamples: {
							{
								const uint32 ts = mpHistoryModel->ConvertRawTimestamp(hent.mCycle);

								if (mTimestampMode == kTsMode_TapePositionSeconds) {
									const double tapeSecs = mpHistoryModel->DecodeTapeSeconds(ts);

									if (tapeSecs >= 0)
										mTempLine.sprintf("%8.5f | ", tapeSecs);
									else
										mTempLine = "N/A      | ";
								} else {
									const double tapeSample = mpHistoryModel->DecodeTapeSample(ts);

									if (tapeSample >= 0)
										mTempLine.sprintf("%8.1f | ", tapeSample);
									else
										mTempLine = "N/A      | ";
								}
							}
							break;
						}
					}
				} else {
					switch(mTimestampMode) {
						case kTsMode_Beam: {
							if (mSubCycles >= 10)
								mTempLine = "NEXT           | ";
							else if (mSubCycles > 1)
								mTempLine = "NEXT          | ";
							else
								mTempLine = "NEXT         | ";
							break;
						}

						case kTsMode_Microseconds:
							mTempLine = "NEXT    | ";
							break;

						case kTsMode_Cycles:
						case kTsMode_UnhaltedCycles:
							mTempLine = "NEXT  | ";
							break;
					}
				}

				if (mbShowRegisters) {
					if (mDisasmMode == kATDebugDisasmMode_8048) {
						mTempLine.append_sprintf("A=%02X PSW=%02X R0=%02X R1=%02X"
							, hent.mA
							, hent.mP
							, hent.mExt.m8048_R0
							, hent.mExt.m8048_R1
							);

						if (mbShowSpecialRegisters) {
							mTempLine.append_sprintf(" P1=%02X P2=%02X"
								, hent.m8048_P1
								, hent.m8048_P2
								);
						}
					} else if (mDisasmMode == kATDebugDisasmMode_Z80) {
						mTempLine.append_sprintf("A=%02X BC=%02X%02X DE=%02X%02X HL=%02X%02X"
							, hent.mZ80_A
							, hent.mZ80_B
							, hent.mZ80_C
							, hent.mZ80_D
							, hent.mExt.mZ80_E
							, hent.mExt.mZ80_H
							, hent.mExt.mZ80_L
							);

						if (mbShowSpecialRegisters) {
							mTempLine.append_sprintf(" SP=%04X F=%02X"
								, hent.mZ80_SP
								, hent.mZ80_F
								);
						}
					} else if (mDisasmMode == kATDebugDisasmMode_6809) {
						mTempLine.append_sprintf("A=%02X B=%02X X=%02X%02X Y=%02X%02X"
							, hent.mA
							, hent.mExt.mAH
							, hent.mExt.mXH
							, hent.mX
							, hent.mExt.mYH
							, hent.mY
							);

						if (mbShowSpecialRegisters) {
							mTempLine.append_sprintf(" U=%04X S=%02X%02X DP=%02X CC=%02X"
								, hent.mD
								, hent.mExt.mSH
								, hent.mS
								, hent.mK
								, hent.mP
								);
						}
					} else if (is65C816) {
						if (!hent.mbEmulation) {
							if (hent.mP & AT6502::kFlagM) {
								mTempLine.append_sprintf("C=%02X%02X"
									, hent.mExt.mAH
									, hent.mA
									);
							} else {
								mTempLine.append_sprintf("A=%02X%02X"
									, hent.mExt.mAH
									, hent.mA
									);
							}

							if (hent.mP & AT6502::kFlagX) {
								mTempLine.append_sprintf(" X=--%02X Y=--%02X"
									, hent.mX
									, hent.mY
									);
							} else {
								mTempLine.append_sprintf(" X=%02X%02X Y=%02X%02X"
									, hent.mExt.mXH
									, hent.mX
									, hent.mExt.mYH
									, hent.mY
									);
							}

							if (mbShowSpecialRegisters) {
								mTempLine.append_sprintf(" S=%02X%02X B=%02X D=%04X P=%02X"
									, hent.mExt.mSH
									, hent.mS
									, hent.mB
									, hent.mD
									, hent.mP
									);
							}
						} else {
							mTempLine.append_sprintf("A=%02X:%02X X=%02X Y=%02X"
								, hent.mExt.mAH
								, hent.mA
								, hent.mX
								, hent.mY
								);

							if (mbShowSpecialRegisters) {
								mTempLine.append_sprintf(" S=%02X B=%02X D=%04X P=%02X"
									, hent.mS
									, hent.mB
									, hent.mD
									, hent.mP
									);
							}
						}
					} else {
						char axybuf[] = "A=-- X=-- Y=-- S=-- P=--";

						FastFormat02X(axybuf+2, hent.mA);
						FastFormat02X(axybuf+7, hent.mX);
						FastFormat02X(axybuf+12, hent.mY);

						if (!mbShowSpecialRegisters) {
							mTempLine.append(axybuf, axybuf + 14);
						} else {
							FastFormat02X(axybuf+17, hent.mS);
							FastFormat02X(axybuf+22, hent.mP);
							mTempLine.append(axybuf, axybuf + 24);
						}
					}
				}

				if (mbShowFlags) {
					if (mDisasmMode == kATDebugDisasmMode_8048) {
						mTempLine.append_sprintf(" (%c%c%c/RB%c)"
							, (hent.mP & 0x80) ? 'C' : ' '
							, (hent.mP & 0x40) ? 'A' : ' '
							, (hent.mP & 0x20) ? 'F' : ' '
							, (hent.mP & 0x10) ? '1' : '0'
							);
					} else if (mDisasmMode == kATDebugDisasmMode_Z80) {
						mTempLine.append_sprintf(" (%c%c-%c-%c%c%c)"
							, (hent.mZ80_F & 0x80) ? 'S' : ' '
							, (hent.mZ80_F & 0x40) ? 'Z' : ' '
							, (hent.mZ80_F & 0x10) ? 'H' : ' '
							, (hent.mZ80_F & 0x04) ? 'P' : ' '
							, (hent.mZ80_F & 0x02) ? 'N' : ' '
							, (hent.mZ80_F & 0x01) ? 'C' : ' '
							);
					} else if (mDisasmMode == kATDebugDisasmMode_6809) {
						mTempLine.append_sprintf(" (%c%c%c%c%c%c%c%c)"
							, (hent.mP & 0x80) ? 'E' : ' '
							, (hent.mP & 0x40) ? 'F' : ' '
							, (hent.mP & 0x20) ? 'H' : ' '
							, (hent.mP & 0x10) ? 'I' : ' '
							, (hent.mP & 0x08) ? 'N' : ' '
							, (hent.mP & 0x04) ? 'Z' : ' '
							, (hent.mP & 0x02) ? 'V' : ' '
							, (hent.mP & 0x01) ? 'C' : ' '
							);
					} else if (is65C816 && !hent.mbEmulation) {
						mTempLine.append_sprintf(" (%c%c%c%c%c%c%c%c)"
							, (hent.mP & AT6502::kFlagN) ? 'N' : ' '
							, (hent.mP & AT6502::kFlagV) ? 'V' : ' '
							, (hent.mP & AT6502::kFlagM) ? 'M' : ' '
							, (hent.mP & AT6502::kFlagX) ? 'X' : ' '
							, (hent.mP & AT6502::kFlagD) ? 'D' : ' '
							, (hent.mP & AT6502::kFlagI) ? 'I' : ' '
							, (hent.mP & AT6502::kFlagZ) ? 'Z' : ' '
							, (hent.mP & AT6502::kFlagC) ? 'C' : ' '
							);
					} else {
						mTempLine.append_sprintf(" (%c%c%c%c%c%c)"
							, (hent.mP & AT6502::kFlagN) ? 'N' : ' '
							, (hent.mP & AT6502::kFlagV) ? 'V' : ' '
							, (hent.mP & AT6502::kFlagD) ? 'D' : ' '
							, (hent.mP & AT6502::kFlagI) ? 'I' : ' '
							, (hent.mP & AT6502::kFlagZ) ? 'Z' : ' '
							, (hent.mP & AT6502::kFlagC) ? 'C' : ' '
							);
					}
				}

				if (mbShowRegisters || mbShowFlags)
					mTempLine += " | ";

				if (hent.mbIRQ && hent.mbNMI && mDisasmMode != kATDebugDisasmMode_6809)
					mTempLine.append_sprintf("%04X: -- High level emulation --", hent.mPC);
				else
					ATDisassembleInsn(mTempLine, nullptr, mDisasmMode, hent, false, true, mbShowPCAddress, mbShowCodeBytes, mbShowLabels, false, false, mbShowLabelNamespaces, true, mbShowGlobalPCAddress);

				s = mTempLine.c_str();
			}
			break;

		case kATHTNodeType_Label:
			s = node->mpLabel;
			break;
	}

	return s;
}

void ATUIHistoryView::HScrollToPixel(int pos) {
	if (pos < 0)
		pos = 0;

	if (mScrollX != pos) {
		sint32 delta = (sint32)mScrollX - (sint32)pos;
		mScrollX = pos;

		ScrollWindowEx(mhwnd, delta, 0, &mContentRect, &mContentRect, NULL, NULL, SW_INVALIDATE);
		UpdateHScrollBar();
	}
}

void ATUIHistoryView::ScrollToPixel(int pos) {
	if (pos < 0)
		pos = 0;

	if ((uint32)pos > mScrollMax)
		pos = mScrollMax;

	if (mScrollY != pos) {
		sint32 delta = (sint32)mScrollY - (sint32)pos;
		mScrollY = pos;

		ScrollWindowEx(mhwnd, 0, delta, &mContentRect, &mContentRect, NULL, NULL, SW_INVALIDATE);
		UpdateScrollBar();
	}
}

void ATUIHistoryView::InvalidateNode(ATHTNode *node) {
	uint32 y = mHistoryTree.GetNodeYPos(node) * mItemHeight;

	if (y < mScrollY + (mContentRect.bottom - mContentRect.top) && y + mItemHeight > mScrollY) {
		RECT r;
		r.left = 0;
		r.top = (int)y - (int)mScrollY + mHeaderHeight;
		r.right = mWidth;
		r.bottom = r.top + mItemHeight;

		InvalidateRect(mhwnd, &r, TRUE);
	}
}

void ATUIHistoryView::InvalidateStartingAtNode(ATHTNode *node) {
	VDASSERT(node->mpParent);
	int y = mHistoryTree.GetNodeYPos(node) * mItemHeight;

	if ((uint32)y < mScrollY + (mContentRect.bottom - mContentRect.top)) {
		RECT r;
		r.left = 0;
		r.top = (int)y - (int)mScrollY + mHeaderHeight;
		r.right = mWidth;
		r.bottom = mContentRect.bottom;
		InvalidateRect(mhwnd, &r, TRUE);
	}
}

void ATUIHistoryView::InvalidateLine(const ATHTLineIterator& it) {
	if (!it)
		return;

	uint32 y = (mHistoryTree.GetNodeYPos(it.mpNode) + it.mLineIndex) * mItemHeight;

	if (y < mScrollY + (mContentRect.bottom - mContentRect.top) && y + mItemHeight > mScrollY) {
		RECT r;
		r.left = 0;
		r.top = (int)y - (int)mScrollY + mHeaderHeight;
		r.right = mWidth;
		r.bottom = r.top + mItemHeight;

		InvalidateRect(mhwnd, &r, TRUE);
	}
}

void ATUIHistoryView::SelectLine(const ATHTLineIterator& it) {
	if (mSelectedLine == it)
		return;

	InvalidateLine(mSelectedLine);
	mSelectedLine = it;
	InvalidateLine(it);

	EnsureLineVisible(it);

	if (it.mpNode && it.mpNode->mNodeType == kATHTNodeType_Insn)
		mpHistoryModel->OnInsnSelected(mInsnPosStart + it.mpNode->mInsn.mOffset + it.mLineIndex);
}

void ATUIHistoryView::ExpandNode(ATHTNode *node) {
	if (mHistoryTree.ExpandNode(node)) {
		UpdateScrollMax();
 
		InvalidateStartingAtNode(node);
		UpdateScrollBar();
		ScrollToPixel(mScrollY);
	}
}

void ATUIHistoryView::CollapseNode(ATHTNode *node) {
	if (mHistoryTree.CollapseNode(node)) {
		UpdateScrollMax();

		InvalidateStartingAtNode(node);
		UpdateScrollBar();
		ScrollToPixel(mScrollY);
	}
}

void ATUIHistoryView::EnsureLineVisible(const ATHTLineIterator& it) {
	if (!it)
		return;

	for(ATHTNode *p = it.mpNode->mpParent; p; p = p->mpParent) {
		if (!p->mbExpanded)
			ExpandNode(p);
	}

	uint32 ypos = mHistoryTree.GetNodeYPos(it.mpNode);
	uint32 y = (ypos + it.mLineIndex) * mItemHeight;

	const sint32 contentHeight = mContentRect.bottom - mContentRect.top;
	if (y < mScrollY) {
		uint32 scrollMargin = (uint32)std::max<sint32>(0, mItemHeight * (contentHeight / mItemHeight / 4));

		if (y + scrollMargin < mScrollY)
			ScrollToPixel(y > scrollMargin ? y - scrollMargin : 0);
		else
			ScrollToPixel(y);
	} else if (y + mItemHeight > mScrollY + contentHeight) {
		uint32 scrollMargin = (uint32)std::max<sint32>(0, mItemHeight * (contentHeight / mItemHeight / 4));

		if (y > mScrollY + contentHeight + scrollMargin)
			ScrollToPixel(y + scrollMargin + mItemHeight - contentHeight);
		else
			ScrollToPixel(std::max<uint32>(contentHeight, y + mItemHeight) - contentHeight);
	}
}

void ATUIHistoryView::RefreshNode(ATHTNode *node, uint32 subIndex) {
	uint32 ypos1 = mHistoryTree.GetNodeYPos(node) + mItemHeight * subIndex;
	uint32 ypos2 = ypos1 + mItemHeight;

	if (ypos2 >= mScrollY && ypos1 < mScrollY + mHeight) {
		RECT r = { 0, (LONG)(ypos1 - mScrollY), (LONG)mWidth, (LONG)(ypos2 - mScrollY) };

		InvalidateRect(mhwnd, &r, TRUE);
	}
}

ATHTLineIterator ATUIHistoryView::GetLineFromClientPoint(int x, int y) const {
	if (y < (int)mHeaderHeight || !mItemHeight)
		return {};

	const uint32 pos = ((uint32)y - mHeaderHeight + mScrollY) / mItemHeight;

	return mHistoryTree.GetLineFromPos(pos);
}

void ATUIHistoryView::UpdateHScrollBar() {
	if (mbUpdatesBlocked) {
		mbDirtyHScrollBar = true;
		return;
	}

	if (mbHistoryError) {
		ShowScrollBar(mhwnd, SB_HORZ, FALSE);
	} else {
		SCROLLINFO si;
		si.cbSize = sizeof si;
		si.fMask = SIF_RANGE | SIF_PAGE | SIF_POS;
		si.nPage = mWidth;
		si.nMin = 0;
		si.nMax = mItemHeight * 64 + mCharWidth * 64;
		si.nPos = mScrollX;
		si.nTrackPos = 0;
		SetScrollInfo(mhwnd, SB_HORZ, &si, TRUE);

		ShowScrollBar(mhwnd, SB_HORZ, TRUE);
	}

	mbDirtyHScrollBar = false;
}

bool ATUIHistoryView::GetLineHistoryIndex(const ATHTLineIterator& it, uint32& index) const {
	if (it.mpNode->mNodeType != kATHTNodeType_Insn)
		return false;

	if (it.mLineIndex >= it.mpNode->mVisibleLines)
		return false;

	if (it.mpNode->mbFiltered)
		index = mFilteredInsnLookup[it.mpNode->mInsn.mOffset + it.mLineIndex];
	else
		index = it.mpNode->mInsn.mOffset + it.mLineIndex;

	return true;
}

const ATCPUHistoryEntry *ATUIHistoryView::GetLineHistoryEntry(const ATHTLineIterator& it) const {
	ATHTNode *node = it.mpNode;

	if (!node)
		return nullptr;

	switch(node->mNodeType) {
		case kATHTNodeType_Insn:
		case kATHTNodeType_Interrupt:
			if (it.mLineIndex >= node->mVisibleLines)
				return nullptr;

			if (node->mbFiltered)
				return &mInsnBuffer[mFilteredInsnLookup[node->mInsn.mOffset + it.mLineIndex]];

			return &mInsnBuffer[node->mInsn.mOffset + it.mLineIndex];

		case kATHTNodeType_InsnPreview:
			return &mPreviewNodeHEnt;

		default:
			return nullptr;
	}
}

const ATCPUHistoryEntry *ATUIHistoryView::GetSelectedLineHistoryEntry() const {
	return GetLineHistoryEntry(mSelectedLine);
}

void ATUIHistoryView::UpdateScrollMax() {
	const ATHTNode *root = mHistoryTree.GetRootNode();
	const uint32 treeHeight = root->mHeight - 1;

	mScrollMax = treeHeight <= mPageItems ? 0 : (treeHeight - mPageItems) * mItemHeight;
}

void ATUIHistoryView::UpdateScrollBar() {
	if (mbUpdatesBlocked) {
		mbDirtyScrollBar = true;
		return;
	}

	if (mbHistoryError) {
		ShowScrollBar(mhwnd, SB_VERT, FALSE);
	} else {
		SCROLLINFO si;
		si.cbSize = sizeof si;
		si.fMask = SIF_RANGE | SIF_PAGE | SIF_POS;
		si.nPage = mPageItems * mItemHeight;
		si.nMin = 0;
		si.nMax = mScrollMax + si.nPage - 1;
		si.nPos = mScrollY;
		si.nTrackPos = 0;
		SetScrollInfo(mhwnd, SB_VERT, &si, TRUE);

		ShowScrollBar(mhwnd, SB_VERT, si.nMax > 0);
	}

	mbDirtyScrollBar = false;
}

void ATUIHistoryView::Reset() {
	ClearAllNodes();

	mInsnPosStart = 0;
	mInsnPosEnd = 0;
}

void ATUIHistoryView::ReloadOpcodes() {
	const uint32 hstart = mInsnPosStart;
	const uint32 hend = mInsnPosEnd;

	Reset();
	UpdateOpcodes(hstart, hend);
}

void ATUIHistoryView::UpdateOpcodes(uint32 historyStart, uint32 historyEnd) {	
	if (!mpHistoryModel)
		return;

#if VERIFY_HISTORY_TREE
	mHistoryTree.Verify();
#endif

	if (mInsnBuffer.empty())
		mInsnPosStart = historyStart;

	const ATHistoryTranslateInsnFn translateFn = ATHistoryGetTranslateInsnFn(mDisasmMode);

	uint32 c = historyEnd;
	uint32 dist = c - mInsnPosEnd;
	uint32 l = historyEnd - historyStart;
	
	ATHTNode *last = NULL;
	bool quickMode = false;
	bool heightChanged = false;

	if (dist > 0) {
		// remove the temp node
		if (mpPreviewNode) {
			VDASSERT(mHistoryTree.GetBackNode() != nullptr);
			RemoveNode(mpPreviewNode);
			mpPreviewNode = nullptr;
		}

		if (dist > l || mInsnBuffer.size() > 500000) {
			ClearAllNodes();
			Reset();
			dist = l;
			mInsnPosEnd = c - l;
		}

		if (mbSearchActive) {
			Search(NULL);
			if (mhwndEdit)
				SetWindowTextW(mhwndEdit, L"");
		}

		if (dist > 1000) {
			quickMode = true;
			mbInvalidatesBlocked = true;
		}

		mbUpdatesBlocked = true;

		const uint32 origHt = mHistoryTree.GetRootNode()->mHeight;

		mHistoryTreeBuilder.SetCollapseCalls(mbCollapseCalls);
		mHistoryTreeBuilder.SetCollapseInterrupts(mbCollapseInterrupts);
		mHistoryTreeBuilder.SetCollapseLoops(mbCollapseLoops);
		mHistoryTreeBuilder.BeginUpdate(!quickMode);

		const ATCPUHistoryEntry *htab[64];
		ATHistoryTraceInsn httab[64];
		uint32 hposnext = mInsnPosEnd;

		mInsnPosEnd += dist;
		while(dist) {
			uint32 batchSize = std::min<uint32>(dist, vdcountof(htab));
			batchSize = mpHistoryModel->ReadInsns(htab, hposnext, batchSize);
		
			hposnext += batchSize;
			dist -= batchSize;
				
			for(uint32 i=0; i<batchSize; ++i)
				mInsnBuffer.push_back(*htab[i]);

			translateFn(httab, htab, batchSize);

			mHistoryTreeBuilder.Update(httab, batchSize);
		}

		const uint32 updatePos = mHistoryTreeBuilder.EndUpdate(last);

		const uint32 newHt = mHistoryTree.GetRootNode()->mHeight;

		if (!quickMode) {
			if (!updatePos || updatePos < (mScrollY + mHeight) / mItemHeight + 1) {
				// The root node contains a hidden line, which we ignore since it is never displayed.
				int y1 = (int)updatePos * mItemHeight;

				if ((uint32)y1 < mScrollY + (mContentRect.bottom - mContentRect.top)) {
					RECT r;
					r.left = 0;
					r.top = y1 - (int)mScrollY + mHeaderHeight;
					r.right = mWidth;
					r.bottom = mContentRect.bottom;
					InvalidateRect(mhwnd, &r, TRUE);
				}
			} else if (newHt < origHt) {
				InvalidateRect(mhwnd, nullptr, TRUE);
			} else if (newHt > origHt) {
				// The root node contains a hidden line, which we ignore since it is never displayed.
				int y1 = ((int)origHt - 1) * mItemHeight;
				int y2 = ((int)newHt - 1) * mItemHeight;

				if ((uint32)y1 < mScrollY + (mContentRect.bottom - mContentRect.top)) {
					RECT r;
					r.left = 0;
					r.top = y1 - (int)mScrollY + mHeaderHeight;
					r.right = mWidth;
					r.bottom = y2 - (int)mScrollY + mHeaderHeight;
					InvalidateRect(mhwnd, &r, TRUE);
				}
			}
		}

		heightChanged = true;
	}

	// readd the temp node
	if (mpHistoryModel->UpdatePreviewNode(mPreviewNodeHEnt)) {
		if (mpPreviewNode) {
			RefreshNode(mpPreviewNode, 0);
		} else {
			if (!last)
				last = mHistoryTree.GetBackNode();

			mpPreviewNode = InsertNode(last ? last->mpParent : mHistoryTree.GetRootNode(), last, 0, kATHTNodeType_InsnPreview);
			heightChanged = true;
		}

		last = mpPreviewNode;
	}

	if (heightChanged) {
		UpdateScrollMax();
		mbDirtyScrollBar = true;
	}

	if (last)
		SelectLine(ATHTLineIterator { last, last->mVisibleLines - 1 });
	
#if VERIFY_HISTORY_TREE
	mHistoryTree.Verify();
#endif

	if (quickMode) {
		InvalidateRect(mhwnd, NULL, TRUE);
		mbInvalidatesBlocked = false;
	}

	mbUpdatesBlocked = false;

	if (mbDirtyScrollBar)
		UpdateScrollBar();

	if (mbDirtyHScrollBar)
		UpdateHScrollBar();
}

void ATUIHistoryView::ClearAllNodes() {
	mSelectedLine = {};

	mHistoryTree.Clear();
	mHistoryTreeBuilder.Reset();

	mpPreviewNode = nullptr;

	mInsnBuffer.clear();

	UpdateScrollMax();

	if (mhwnd)
		InvalidateRect(mhwnd, NULL, TRUE);
}

ATHTNode *ATUIHistoryView::InsertNode(ATHTNode *parent, ATHTNode *insertAfter, uint32 insnOffset, ATHTNodeType nodeType) {
	ATHTNode *node = mHistoryTree.InsertNode(parent, insertAfter, insnOffset, nodeType);

	UpdateScrollMax();

	if (!mbInvalidatesBlocked) {
		// Check if this node is at the bottom of the tree. If so, we only need to invalidate this node.
		bool lineAtBottom = node->mHeight == 1;

		for(const ATHTNode *p = node; p && lineAtBottom; p = p->mpParent) {
			if (p->mpNextSibling)
				lineAtBottom = false;
		}

		if (lineAtBottom)
			InvalidateNode(node);
		else
			InvalidateStartingAtNode(node);
	}

	return node;
}

void ATUIHistoryView::RemoveNode(ATHTNode *node) {
	VDASSERT(node);

	ATHTNode *successorNode = nullptr;
	
	if (!mbInvalidatesBlocked) {
		// Check if this node is at the bottom of the tree. If so, we only need to invalidate this node.
		bool lineAtBottom = node->mHeight == 1;

		for(const ATHTNode *p = node; p && lineAtBottom; p = p->mpParent) {
			if (p->mpNextSibling)
				lineAtBottom = false;
		}

		if (lineAtBottom)
			InvalidateNode(node);
		else
			InvalidateStartingAtNode(node);
	}

	mHistoryTree.RemoveNode(node);
}

void ATUIHistoryView::CopyVisibleLines() {
	ATHTLineIterator startLine = GetLineFromClientPoint(0, mHeaderHeight);
	ATHTLineIterator endLine = GetLineFromClientPoint(0, mHeight);

	CopyLines(startLine, endLine);
}

void ATUIHistoryView::CopyLines(const ATHTLineIterator& startLine, const ATHTLineIterator& endLine) {
	ATHTLineIterator begin = mHistoryTree.GetNearestVisibleLine(startLine);
	ATHTLineIterator end = mHistoryTree.GetNearestVisibleLine(endLine);

	if (!begin || (end && mHistoryTree.GetLineYPos(end) <= mHistoryTree.GetLineYPos(begin)))
		return;

	int minLevel = INT_MAX;
	for(ATHTNode *p = begin.mpNode; p; p = mHistoryTree.GetNextVisibleNode(p)) {
		if (p == end.mpNode && end.mLineIndex == 0)
			break;

		int level = 0;

		for(ATHTNode *q = p; q; q = q->mpParent)
			++level;

		if (minLevel > level)
			minLevel = level;

		if (p == end.mpNode)
			break;
	}

	if (minLevel == INT_MAX)
		return;

	VDStringA s;

	for(ATHTLineIterator it = begin; it != end; it = mHistoryTree.GetNextVisibleLine(it)) {
		int level = -minLevel;

		for(ATHTNode *p = it.mpNode; p; p = p->mpParent)
			++level;

		if (level > 0) {
			for(int i=0; i<level; ++i) {
				s += ' ';
				s += ' ';
			}
		}

		s += it.mpNode->mpFirstChild ? it.mpNode->mbExpanded ? '-' : '+' : ' ';
		s += ' ';
		s += GetLineText(it);
		s += '\r';
		s += '\n';
	}
	
	ATCopyTextToClipboard(mhwnd, s.c_str());
}

void ATUIHistoryView::Search(const char *substr) {
	if (substr && !*substr)
		substr = NULL;

	if (!substr && !mbSearchActive)
		return;

	if (substr) {
		mFilteredInsnLookup.clear();
		mFilteredInsnLookup.resize(mInsnBuffer.size(), 0);

		const char ch0 = substr[0];
		const char mask0 = (unsigned)(((unsigned char)ch0 & 0xdf) - 'A') < 26 ? (char)0xdf : (char)0xff;
		const size_t substrlen = strlen(substr);

		const auto filter = [=](const ATHTNode& node) {
			const bool isInsnNode = (node.mNodeType == kATHTNodeType_Insn);
			const uint32 lineCount = isInsnNode ? node.mInsn.mCount : 1;
			const uint32 startOffset = isInsnNode ? node.mInsn.mOffset : 0;
			uint32 visibleLines = 0;

			for(uint32 lineIndex = 0; lineIndex < lineCount; ++lineIndex) {
				const char *s = GetLineText(ATHTLineIterator { const_cast<ATHTNode *>(&node), lineIndex });

				const size_t len = strlen(s);

				if (len < substrlen)
					continue;

				size_t maxoffset = len - substrlen;
				for(size_t i = 0; i <= maxoffset; ++i) {
					if (!((s[i] ^ ch0) & mask0) && !vdstricmp(substr, s + i, substrlen)) {
						if (isInsnNode)
							mFilteredInsnLookup[startOffset + visibleLines] = startOffset + lineIndex;

						++visibleLines;
						break;
					}
				}
			}

			return visibleLines;
		};

		mHistoryTree.Search(filter);

		mSelectedLine = mHistoryTree.GetNearestVisibleLine(ATHTLineIterator { mHistoryTree.GetFrontNode(), 0 });
	} else {
		// if the currently selected line is from a filtered node, translate to unfiltered
		if (mSelectedLine.mpNode) {
			if (mSelectedLine.mpNode->mbFiltered) {
				const uint32 insnBase = mSelectedLine.mpNode->mInsn.mOffset;
				uint32 insnIndex = mFilteredInsnLookup[insnBase + mSelectedLine.mLineIndex] - insnBase;

				mSelectedLine.mLineIndex = insnIndex;
			}
		}

		mHistoryTree.Unsearch();

		if (!mSelectedLine.mpNode) {
			// no selected line -- use last line (must use unfiltered count!)
			mSelectedLine = { mpPreviewNode, 0 };

			if (!mpPreviewNode) {
				mSelectedLine.mpNode = mHistoryTree.GetBackNode();

				if (mSelectedLine.mpNode)
					mSelectedLine.mLineIndex = mSelectedLine.mpNode->mVisibleLines - 1;
			}
		}
	}

#if VERIFY_HISTORY_TREE
	VDASSERT(mHistoryTree.Verify());
#endif

	const bool searchActive = (substr != NULL);

	if (mbSearchActive != searchActive) {
		mbSearchActive = searchActive;

		VDSetWindowTextW32(mhwndClear, searchActive ? mTextClear.c_str() : mTextSearch.c_str());
	}

	UpdateScrollMax();
	InvalidateRect(mhwnd, NULL, TRUE);
	UpdateScrollBar();
	EnsureLineVisible(mSelectedLine);
}

///////////////////////////////////////////////////////////////////////////

bool ATUICreateHistoryView(VDGUIHandle parent, IATUIHistoryView **ppview) {
	vdrefptr<ATUIHistoryView> view { new ATUIHistoryView };

	if (!view->CreateChild((HWND)parent, 0, 0, 0, 0, 0, WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN))
		return false;

	*ppview = view.release();

	return *ppview;
}
