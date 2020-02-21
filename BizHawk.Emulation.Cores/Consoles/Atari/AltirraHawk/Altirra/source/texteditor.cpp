//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

#include <stdafx.h>
#include "texteditor.h"

#include <vd2/system/binary.h>
#include <vd2/system/file.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/w32assist.h>
#include <windows.h>
#include <at/atnativeui/uinativewindow.h>
#include "textdom.h"

using namespace nsVDTextDOM;

///////////////////////////////////////////////////////////////////////////

class TextEditor : public ATUINativeWindow, public IVDTextEditor, public IDocumentCallback {
public:
	TextEditor();
	~TextEditor();

	int AddRef() { return ATUINativeWindow::AddRef(); }
	int Release() { return ATUINativeWindow::Release(); }

	VDGUIHandle Create(uint32 exStyle, uint32 style, int x, int y, int cx, int cy, VDGUIHandle parent, int id);

	void	SetCallback(IVDTextEditorCallback *pCB);
	void	SetColorizer(IVDTextEditorColorizer *pColorizer);
	void	SetMsgFilter(IVDUIMessageFilterW32 *pFilter);

	void	SetGutters(int x, int y);

	bool	IsSelectionPresent();
	bool	IsCutPossible();
	bool	IsCopyPossible();
	bool	IsPastePossible();
	bool	IsUndoPossible();
	bool	IsRedoPossible();

	int		GetLineCount();
	bool	GetLineText(int line, vdfastvector<char>& buf);

	void	SetReadOnly(bool enable);
	void	SetWordWrap(bool enable);

	int		GetCursorLine();
	void	SetCursorPos(int line, int offset);
	void	SetCursorPixelPos(int x, int y);

	void	RecolorLine(int line);
	void	RecolorAll();

	bool	Find(const char *text, int len, bool caseSensitive, bool wholeWord, bool searchUp);

	int		GetVisibleHeight();
	int		GetParagraphForYPos(int y);
	int		GetVisibleLineCount();
	void	MakeLineVisible(int line);
	void	CenterViewOnLine(int line);
	
	void	SetUpdateEnabled(bool updateEnabled);

	void	Undo();
	void	Redo();
	void	Clear();
	void	Cut();
	void	Copy();
	void	Paste();
	void	Delete();
	void	DeleteSelection();
	void	SelectAll();

	void	Append(const char *s);

	void	Load(IVDStream& stream);
	void	Save(IVDTextEditorStreamOut& streamout);

protected:
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);

	void OnCreate();
	void OnDestroy();
	void OnPaint();
	void OnResize();
	void OnSetFocus();
	void OnKillFocus();
	void OnKeyDown(WPARAM key);
	void OnChar(int ch);
	void OnLButtonDown(WPARAM modifiers, int x, int y);
	void OnLButtonUp(WPARAM modifiers, int x, int y);
	void OnMouseMove(WPARAM modifiers, int x, int y);
	void OnMouseWheel(int wheelClicks, WPARAM modifiers, int x, int y);
	void OnVScroll(int cmd);
	int OnGetText(uint32 length, char *s);
	int OnGetText(uint32 length, wchar_t *s);
	int OnGetTextLength();
	bool OnSetText(const char *s);

	void MoveCaret(const Iterator& pos, bool anchor, bool sendScrollUpdate);
	void ScrollTo(int y, bool sendScrollUpdate);

	void UpdateCaretPos(bool autoscroll, bool sendScrollUpdate);
	void UpdateScrollPos();
	void UpdateScrollRange();
	void AnchorSelection();
	void ClearSelection();
	void InvalidateRange(const Iterator& pos1, const Iterator& pos2);
	void RecalcFontMetrics();
	void RecalcRelativeFontMetrics();
	void Reflow(bool force);
	void Load(const wchar_t *fn);
	void CutCopy(bool doCut);
	void ReloadColors();

	void PosToPixel(int& px, int& py, const Iterator& it);
	Iterator PixelToPos(int px, int py);

protected:
	void InvalidateRows(int ystart, int yend);
	void VerticalShiftRows(int ysrc, int ydst);
	void ReflowPara(int paraIdx, const Paragraph& para);
	void RecolorParagraph(int paraIdx, Paragraph& para);
	void ChangeTotalHeight(int y);

protected:
	HFONT	mhfont;
	int		mFontHeight;
	int		mVisibleWidth;
	int		mVisibleHeight;
	int		mTotalHeight;
	int		mReflowWidth;
	int		mScrollX;
	int		mScrollY;
	int		mTabWidth;
	int		mScrollVertMargin;
	int		mEffectiveScrollVertMargin;
	int		mGutterX;
	int		mGutterY;
	bool	mbCaretPresent;
	bool	mbCaretVisible;
	bool	mbReadOnly;
	bool	mbWordWrap;
	bool	mbDragging;

	bool	mbUpdateEnabled;
	bool	mbUpdateScrollbarPending;

	IVDTextEditorCallback	*mpCB;
	IVDTextEditorColorizer	*mpColorizer;
	IVDUIMessageFilterW32	*mpMsgFilter;
	int		mMouseWheelAccum;

	uint32	mColorTextFore;
	uint32	mColorTextBack;
	uint32	mColorTextHiFore;
	uint32	mColorTextHiBack;

	vdfastvector<char>	mPaintBuffer;
	vdfastvector<Line>	mReflowNewLines;

	Document	mDocument;
	Iterator	mCaretPos;
	Iterator	mSelectionAnchor;
};

bool VDCreateTextEditor(IVDTextEditor **ppTextEditor) {
	*ppTextEditor = new TextEditor;
	if (!*ppTextEditor)
		return false;

	(*ppTextEditor)->AddRef();
	return true;
}

TextEditor::TextEditor()
	: mhfont(NULL)
	, mFontHeight(0)
	, mVisibleWidth(0)
	, mVisibleHeight(0)
	, mTotalHeight(0)
	, mReflowWidth(0)
	, mScrollX(0)
	, mScrollY(0)
	, mTabWidth(0)
	, mScrollVertMargin(0)
	, mEffectiveScrollVertMargin(0)
	, mGutterX(GetSystemMetrics(SM_CXEDGE))
	, mGutterY(GetSystemMetrics(SM_CYEDGE))
	, mbCaretPresent(false)
	, mbCaretVisible(false)
	, mbReadOnly(false)
	, mbWordWrap(false)
	, mbDragging(false)
	, mbUpdateEnabled(true)
	, mbUpdateScrollbarPending(false)
	, mpCB(NULL)
	, mpColorizer(NULL)
	, mpMsgFilter(NULL)
	, mMouseWheelAccum(0)
	, mColorTextFore(0)
	, mColorTextBack(0)
	, mColorTextHiFore(0)
	, mColorTextHiBack(0)
{
	mDocument.SetCallback(this);
}

TextEditor::~TextEditor() {
}

VDGUIHandle TextEditor::Create(uint32 exStyle, uint32 style, int x, int y, int cx, int cy, VDGUIHandle parent, int id) {
	return (VDGUIHandle)CreateWindowEx(exStyle, (LPCTSTR)sWndClass, _T(""), style, x, y, cx, cy, (HWND)parent, (HMENU)(UINT_PTR)id, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this));
}

void TextEditor::SetCallback(IVDTextEditorCallback *pCB) {
	mpCB = pCB;
}

void TextEditor::SetColorizer(IVDTextEditorColorizer *pColorizer) {
	mpColorizer = pColorizer;
}

void TextEditor::SetMsgFilter(IVDUIMessageFilterW32 *pFilter) {
	mpMsgFilter = pFilter;
}

void TextEditor::SetGutters(int x, int y) {
	mGutterX = x;
	mGutterY = y;
	OnResize();
}

bool TextEditor::IsSelectionPresent() {
	return mSelectionAnchor && mSelectionAnchor != mCaretPos;
}

bool TextEditor::IsCutPossible() {
	if (mbReadOnly)
		return false;

	return mSelectionAnchor || mCaretPos.mPara+1 != mDocument.GetParagraphCount();
}

bool TextEditor::IsCopyPossible() {
	return mSelectionAnchor || mCaretPos.mPara+1 != mDocument.GetParagraphCount();
}

bool TextEditor::IsPastePossible() {
	if (mbReadOnly)
		return false;

	UINT formats[]={
		CF_TEXT
	};

	int index = GetPriorityClipboardFormat(formats, sizeof(formats)/sizeof(formats[0]));
	return index >= 0;
}

bool TextEditor::IsUndoPossible() {
	return false;
}

bool TextEditor::IsRedoPossible() {
	return false;
}

int TextEditor::GetLineCount() {
	return mDocument.GetParagraphCount();
}

bool TextEditor::GetLineText(int line, vdfastvector<char>& buf) {
	const Paragraph *para = mDocument.GetParagraph(line);

	buf.clear();
	if (!para)
		return false;

	buf = para->mText;
	return true;
}

void TextEditor::SetReadOnly(bool enable) {
	mbReadOnly = enable;
}

void TextEditor::SetWordWrap(bool enable) {
	if (mbWordWrap == enable)
		return;

	mbWordWrap = enable;
	Reflow(true);
	UpdateCaretPos(false, false);
}

int TextEditor::GetCursorLine() {
	return mCaretPos.mPara;
}

void TextEditor::SetCursorPos(int line, int offset) {
	int paraCount = mDocument.GetParagraphCount();

	ClearSelection();

	if (line >= paraCount)
		mCaretPos.MoveToEnd();
	else if (line < 0)
		mCaretPos.MoveToStart();
	else {
		const Paragraph& para = *mDocument.GetParagraph(line);

		mCaretPos.mPara = line;
		mCaretPos.mLine = para.GetLineIndexFromOffset(offset);
		
		const Line& ln = para.mLines[mCaretPos.mLine];
		offset -= ln.mStart;
		if (offset < 0)
			mCaretPos.mOffset = 0;
		else if (offset > ln.mLength)
			mCaretPos.mOffset = ln.mLength;
		else
			mCaretPos.mOffset = offset;
	}

	UpdateCaretPos(true, false);
}

void TextEditor::SetCursorPixelPos(int x, int y) {
	MoveCaret(PixelToPos(x - mGutterX, y + mScrollY - mGutterY), false, false);
}

namespace {
	bool IsWordChar(char c) {
		return (unsigned)((c - 'A') & 0xdf) < 26 || (unsigned)(c - '0') < 10 || c == '_';
	}
}

void TextEditor::RecolorLine(int line) {
	mDocument.RecolorPara(line);
}

void TextEditor::RecolorAll() {
	int paraCount = mDocument.GetParagraphCount();

	for(int i=0; i<paraCount; ++i)
		mDocument.RecolorPara(i);
}

bool TextEditor::Find(const char *text, int len, bool caseSensitive, bool wholeWord, bool searchUp) {
	int paraCount = mDocument.GetParagraphCount();
	int paraIdx = mCaretPos.mPara;
	int paraOffset = mCaretPos.GetParaOffset();

	if (mSelectionAnchor && mSelectionAnchor < mCaretPos) {
		paraIdx = mSelectionAnchor.mPara;
		paraOffset = mSelectionAnchor.GetParaOffset();
	}

	int basePara = paraIdx;
	int baseOffset = paraOffset;
	int hitOffset = -1;

	if (searchUp) {
		for(int i=0; i<=paraCount; ++i) {
			const Paragraph& para = *mDocument.GetParagraph(paraIdx);
			int paraLen = (int)para.mText.size();

			int x1 = 0;
			int x2 = paraLen;

			if (paraIdx == basePara) {
				if (i)
					x2 = baseOffset;
				else
					x1 = baseOffset + 1;
			}

			x1 += len - 1;

			const char *s = para.mText.data();

			if (caseSensitive) {
				for(int x=x2-1; x>=x1; --x) {
					if (!memcmp(s+x, text, len)) {
						if (wholeWord) {
							if (x && IsWordChar(s[x-1]))
								continue;

							if (x <= paraLen - len && IsWordChar(s[x+len]))
								continue;
						}
						hitOffset = x;
						goto hit;
					}
				}
			} else {
				for(int x=x2-1; x>=x1; --x) {
					if (!_memicmp(s+x, text, len)) {
						if (wholeWord) {
							if (x && IsWordChar(s[x-1]))
								continue;

							if (x <= paraLen - len && IsWordChar(s[x+len]))
								continue;
						}
						hitOffset = x;
						goto hit;
					}
				}
			}

			if (--paraIdx < 0)
				paraIdx = paraCount - 1;
		}
	} else {
		for(int i=0; i<=paraCount; ++i) {
			const Paragraph& para = *mDocument.GetParagraph(paraIdx);
			int paraLen = (int)para.mText.size();

			int x1 = 0;
			int x2 = paraLen;

			if (paraIdx == basePara) {
				if (i)
					x1 = baseOffset + 1;
				else
					x2 = baseOffset;
			}

			x2 -= len - 1;

			const char *s = para.mText.data();
			if (caseSensitive) {
				for(int x=x1; x<x2; ++x) {
					if (!memcmp(s+x, text, len)) {
						if (wholeWord) {
							if (x && IsWordChar(s[x-1]))
								continue;

							if (x <= paraLen - len && IsWordChar(s[x+len]))
								continue;
						}

						hitOffset = x;
						goto hit;
					}
				}
			} else {
				for(int x=x1; x<x2; ++x) {
					if (!_memicmp(s+x, text, len)) {
						if (wholeWord) {
							if (x && IsWordChar(s[x-1]))
								continue;

							if (x <= paraLen - len && IsWordChar(s[x+len]))
								continue;
						}
						hitOffset = x;
						goto hit;
					}
				}
			}

			if (++paraIdx >= paraCount)
				paraIdx = 0;
		}
	}
	return false;

hit:
	ClearSelection();
	mCaretPos.MoveToParaOffset(paraIdx, hitOffset);
	mSelectionAnchor.Attach(mDocument);
	mSelectionAnchor.MoveToParaOffset(paraIdx, hitOffset+len);
	InvalidateRange(mCaretPos, mSelectionAnchor);
	UpdateCaretPos(true, false);
	return true;
}

int	TextEditor::GetVisibleHeight() {
	return mVisibleHeight;
}

int	TextEditor::GetParagraphForYPos(int y) {
	return mDocument.GetParagraphFromY(y + mScrollY);
}

int TextEditor::GetVisibleLineCount() {
	return mFontHeight ? (mVisibleHeight + mFontHeight - 1) / mFontHeight : 0;
}

void TextEditor::MakeLineVisible(int line) {
	int xp, yp;
	Iterator it(mDocument, line);
	PosToPixel(xp, yp, it);

	bool tooHigh = yp < mScrollY + mEffectiveScrollVertMargin;
	bool tooLow = yp + mFontHeight + mEffectiveScrollVertMargin > mScrollY + mVisibleHeight;

	if (tooHigh) {
		if (tooLow)
			ScrollTo(yp + ((int)(mVisibleHeight - mFontHeight) >> 1), false);
		else
			ScrollTo(yp - mEffectiveScrollVertMargin, false);
	} else if (tooLow)
		ScrollTo(yp + mFontHeight - mVisibleHeight + mEffectiveScrollVertMargin, false);
}

void TextEditor::CenterViewOnLine(int line) {
	if (line >= mDocument.GetParagraphCount())
		line = mDocument.GetParagraphCount() - 1;

	if (line < 0)
		line = 0;

	int xp, yp;
	Iterator it(mDocument, line);
	PosToPixel(xp, yp, it);

	ScrollTo(yp - ((int)(mVisibleHeight - mFontHeight) >> 1), false);
}

void TextEditor::SetUpdateEnabled(bool updateEnabled) {
	if (mbUpdateEnabled == updateEnabled)
		return;

	mbUpdateEnabled = updateEnabled;

	if (updateEnabled) {
		if (mbUpdateScrollbarPending) {
			mbUpdateScrollbarPending = false;

			UpdateScrollRange();
			UpdateScrollPos();
		}
	}
}

void TextEditor::Undo() {
}

void TextEditor::Redo() {
}

void TextEditor::Clear() {
	Iterator start(mDocument);
	Iterator end(mDocument);

	end.MoveToEnd();

	mDocument.Delete(start, end);
}

void TextEditor::Cut() {
	CutCopy(true);
}

void TextEditor::Copy() {
	CutCopy(false);
}

void TextEditor::Paste() {
	if (mSelectionAnchor)
		DeleteSelection();

	if (!OpenClipboard(mhwnd))
		return;

	vdfastvector<char> buf;

	if (IsClipboardFormatAvailable(CF_TEXT)) {
		HGLOBAL hmem = GetClipboardData(CF_TEXT);
		if (hmem) {
			const char *p = (const char *)GlobalLock(hmem);
			if (p) {
				size_t len = GlobalSize(hmem);
				if (len)
					--len;

				buf.assign(p, p+len);
				GlobalUnlock(hmem);
			}
		}
	}

	CloseClipboard();

	if (!buf.empty()) {
		char *base = buf.data();
		char *t = base;
		const char *s = base;
		const char *end = s + buf.size();
		char linebreak = 0;

		while(s != end) {
			char c = *s++;

			if (c == '\r' || c == '\n') {
				if (linebreak == c) {
					linebreak = 0;
					continue;
				}

				linebreak = c ^ ('\r' ^ '\n');
				*t++ = '\n';
				continue;
			}

			linebreak = 0;
			*t++ = c;
		}

		mDocument.Insert(mCaretPos, base, t-base, &mCaretPos);
		UpdateCaretPos(true, false);
	}
}

void TextEditor::Delete() {
	if (mSelectionAnchor) {
		DeleteSelection();
	} else {
		Iterator oldPos(mCaretPos);
		Iterator newPos(mCaretPos);
		newPos.MoveToPrevChar();
		MoveCaret(newPos, false, false);

		if (oldPos != newPos) {
			mDocument.Delete(newPos, oldPos);
			if (mpCB)
				mpCB->OnTextEditorUpdated();
		}
	}
}

void TextEditor::DeleteSelection() {
	if (mCaretPos != mSelectionAnchor) {
		mDocument.Delete(mCaretPos, mSelectionAnchor);
		mSelectionAnchor.Detach();
		UpdateCaretPos(true, false);

		if (mpCB)
			mpCB->OnTextEditorUpdated();
	}
}

void TextEditor::SelectAll() {
	mSelectionAnchor.Attach(mDocument);
	mSelectionAnchor.MoveToStart();
	Iterator it(mDocument);
	it.MoveToEnd();
	MoveCaret(it, true, false);
	InvalidateRange(mSelectionAnchor, mCaretPos);
}

void TextEditor::Append(const char *s) {
	Iterator it(mDocument);
	it.MoveToEnd();
	mDocument.Insert(it, s, strlen(s), NULL);
}

void TextEditor::Load(IVDStream& stream) {
	Iterator it1(mDocument);
	Iterator it2(mDocument);
	it2.MoveToEnd();
	mDocument.Delete(it1, it2);

	char buf[4096];
	char linebreak = 0;

	for(;;) {
		sint32 actual = stream.ReadData(buf, sizeof buf);
		if (actual <= 0)
			break;

		char *dst = buf;
		const char *src = buf;
		const char *srcend = src + actual;

		for(; src!=srcend; ++src) {
			char c = *src;

			if (c == '\r' || c == '\n') {
				if (linebreak) {
					if (linebreak == (c ^ ('\r' ^ '\n')))
						linebreak = 0;
					continue;
				}
				linebreak = c;
				c = '\n';
			} else {
				linebreak = 0;
			}

			*dst++ = c;
		}

		it2.MoveToEnd();
		mDocument.Insert(it2, buf, dst-buf, NULL);
	}

	if (mpCB)
		mpCB->OnTextEditorUpdated();
}

namespace {
	class BufferedWriter {
	public:
		BufferedWriter(IVDTextEditorStreamOut& sout) : mLevel(0), mOut(sout) {}
		~BufferedWriter() {
			Flush();
		}

		void Write(const char *s, size_t len);
		void Flush();

	protected:
		uint32 mLevel;
		IVDTextEditorStreamOut& mOut;
		char buf[4096];
	};

	void BufferedWriter::Write(const char *s, size_t len) {
		while(len) {
			size_t tc = len;
			if (tc + mLevel > sizeof buf) {
				if (!mLevel) {
					mOut.Write(s, tc);
					return;
				}

				tc = sizeof buf - mLevel;
				if (!tc) {
					Flush();
					continue;
				}
			}

			memcpy(buf + mLevel, s, tc);
			s += tc;
			len -= tc;
			mLevel += (uint32)tc;
		}
	}

	void BufferedWriter::Flush() {
		if (mLevel) {
			mOut.Write(buf, mLevel);
			mLevel = 0;
		}
	}
}

void TextEditor::Save(IVDTextEditorStreamOut& streamout) {
	int paraCount = mDocument.GetParagraphCount();

	BufferedWriter writer(streamout);
	for(int i=0; i<paraCount; ++i) {
		const Paragraph& para = *mDocument.GetParagraph(i);

		if (!para.mText.empty()) {
			if (para.mText.back() == '\n') {
				writer.Write(para.mText.data(), para.mText.size() - 1);
				writer.Write("\r\n", 2);
			} else {
				writer.Write(para.mText.data(), para.mText.size());
			}
		}
	}
}

LRESULT TextEditor::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	if (mpMsgFilter) {
		LRESULT r = 0;
		if (mpMsgFilter->OnMessage(msg, wParam, lParam, r))
			return r;
	}

	switch(msg) {
	case WM_CREATE:
		OnCreate();
		OnResize();
		break;
	case WM_DESTROY:
		OnDestroy();
		break;
	case WM_SIZE:
		OnResize();
		break;
	case WM_ERASEBKGND:
		return 0;
	case WM_PAINT:
		OnPaint();
		return 0;
	case WM_SETFOCUS:
		OnSetFocus();
		return 0;
	case WM_KILLFOCUS:
		OnKillFocus();
		return 0;
	case WM_KEYDOWN:
		OnKeyDown(wParam);
		break;
	case WM_CHAR:
		OnChar((int)wParam);
		break;
	case WM_LBUTTONDOWN:
		::SetFocus(mhwnd);
		OnLButtonDown(wParam, (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;
	case WM_LBUTTONUP:
		OnLButtonUp(wParam, (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;
	case WM_MBUTTONDOWN:
	case WM_RBUTTONDOWN:
	case WM_XBUTTONDOWN:
		::SetFocus(mhwnd);
		break;
	case WM_MOUSEMOVE:
		OnMouseMove(wParam, (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;
	case WM_MOUSEWHEEL:
		OnMouseWheel((SHORT)HIWORD(wParam), LOWORD(wParam), (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;
	case WM_VSCROLL:
		if (!lParam) {
			OnVScroll(LOWORD(wParam));
			return 0;
		}
		break;

	case WM_CUT:
		Cut();
		return 0;

	case WM_COPY:
		Copy();
		return 0;

	case WM_PASTE:
		Paste();
		return 0;

	case WM_CLEAR:
		DeleteSelection();
		return 0;

	case WM_SETFONT:
		mhfont = (HFONT)wParam;
		if (!mhfont)
			mhfont = (HFONT)GetStockObject(SYSTEM_FONT);
		RecalcFontMetrics();
		Reflow(true);

		if (::GetFocus() == mhwnd) {
			OnKillFocus();
			OnSetFocus();
		}

		if (LOWORD(lParam))
			InvalidateRect(mhwnd, NULL, TRUE);
		return 0;

	case WM_SETTEXT:
		return OnSetText((const char *)lParam);

	case WM_GETTEXT:
		if (IsWindowUnicode(mhwnd))
			return OnGetText((uint32)wParam, (wchar_t *)lParam);
		else
			return OnGetText((uint32)wParam, (char *)lParam);

	case WM_GETTEXTLENGTH:
		return OnGetTextLength();

	case WM_GETDLGCODE:
		return DLGC_WANTALLKEYS;

	case WM_SYSCOLORCHANGE:
		ReloadColors();
		InvalidateRect(mhwnd, NULL, FALSE);
		break;

	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

void TextEditor::OnCreate() {
	mhfont = (HFONT)GetStockObject(DEFAULT_GUI_FONT);
	//mhfont = CreateFont(-12, 0, 0, 0, 0, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, "Lucida Console");

	RecalcFontMetrics();

	mCaretPos.Attach(mDocument);
	mScrollY = 0;
	mbCaretPresent = false;
	mbCaretVisible = false;

	ReloadColors();
}

void TextEditor::OnDestroy() {
}

void TextEditor::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhwnd, &ps);
	if (hdc) {
		if (int state = SaveDC(hdc)) {
			SelectObject(hdc, mhfont);
			SetTextAlign(hdc, TA_TOP | TA_LEFT);
			SetBkMode(hdc, OPAQUE);
			SetBkColor(hdc, mColorTextBack);

			// fill gutters
			if (mScrollY < mGutterY) {
				RECT rtop = {0, 0, mVisibleWidth, mGutterY - mScrollY};
				ExtTextOutW(hdc, 0, 0, ETO_OPAQUE, &rtop, L"", 0, NULL);
			}

			if (mScrollX < mGutterX) {
				RECT rleft = {0, 0, mGutterX - mScrollX, mVisibleHeight};
				ExtTextOutW(hdc, 0, 0, ETO_OPAQUE, &rleft, L"", 0, NULL);
			}

			const Iterator *selStart = NULL;
			const Iterator *selEnd = NULL;

			if (mSelectionAnchor && mSelectionAnchor != mCaretPos) {
				selStart = &mCaretPos;
				selEnd = &mSelectionAnchor;

				if (*selEnd < *selStart) {
					selEnd = &mCaretPos;
					selStart = &mSelectionAnchor;
				}
			}

			int y1 = ps.rcPaint.top + mScrollY - mGutterY;
			int y2 = ps.rcPaint.bottom + mScrollY - mGutterY;
			int ylast = y1;

			int paraIdx = mDocument.GetParagraphFromY(y1);

			int paraCount = mDocument.GetParagraphCount();
			while(paraIdx < paraCount) {
				const Paragraph *para = mDocument.GetParagraph(paraIdx);

				mDocument.GetParagraphText(paraIdx, mPaintBuffer);
				const char *s = mPaintBuffer.data();

				Paragraph::Lines::const_iterator itL(para->mLines.begin()), itLEnd(para->mLines.end());
				int yp = para->mYPos;

				if (yp >= y2)
					break;

				yp -= mScrollY;

				int paraSelStart = 0;
				int paraSelEnd = 0;

				if (selStart) {
					if (selStart->mPara == paraIdx)
						paraSelStart = selStart->GetParaOffset();

					if (paraIdx >= selStart->mPara && paraIdx <= selEnd->mPara) {
						if (selEnd->mPara == paraIdx)
							paraSelEnd = selEnd->GetParaOffset();
						else
							paraSelEnd = (int)para->mText.size();
					}
				}

				sint32 foreColor = RGB(0, 0, 0);
				sint32 backColor = RGB(255, 255, 255);

				Paragraph::Spans::const_iterator itS(para->mSpans.begin());
				Paragraph::Spans::const_iterator itSEnd(para->mSpans.end());
				int spanNext = INT_MAX;
				if (itS != itSEnd)
					spanNext = itS->mStart;

				for(; itL != itLEnd; ++itL) {
					const Line& ln = *itL;

					int xpos = ln.mStart;
					int xend = ln.mStart + ln.mLength;
					int xp = 0;

					for(;;) {
						VDASSERT(xpos <= spanNext);
						while(xpos == spanNext) {
							foreColor = itS->mForeColor;
							if (foreColor < 0)
								foreColor = mColorTextFore;
							backColor = itS->mBackColor;
							if (backColor < 0)
								backColor = mColorTextBack;

							++itS;
							if (itS == itSEnd)
								spanNext = INT_MAX;
							else
								spanNext = itS->mStart;
						}

						if (xpos >= xend)
							break;

						int xrend = xend;
						if (xrend > spanNext)
							xrend = spanNext;

						const char *tab = (const char *)memchr(s + xpos, '\t', xrend - xpos);
						if (tab) {
							int tabpos = (int)(tab - s);

							if (xpos == tabpos)
								xrend = xpos + 1;
							else if (xrend > tabpos)
								xrend = tabpos;
						}

						bool selectState = false;
						if (paraSelEnd) {
							if (xpos < paraSelEnd && xrend > paraSelEnd)
								xrend = paraSelEnd;

							if (xpos < paraSelStart && xrend > paraSelStart)
								xrend = paraSelStart;

							if ((unsigned)(xpos - paraSelStart) < (unsigned)(paraSelEnd - paraSelStart))
								selectState = true;
						}

						VDASSERT(xrend <= spanNext);

						if (selectState) {
							SetTextColor(hdc, mColorTextHiFore);
							SetBkColor(hdc, mColorTextHiBack);
						} else {
							SetTextColor(hdc, foreColor);
							SetBkColor(hdc, backColor);
						}

						if (s[xpos] == '\t') {
							int xpnew = xp + mTabWidth;
							xpnew -= xpnew % mTabWidth;

							RECT r;
							r.left = xp;
							r.right = xpnew;
							r.top = yp;
							r.bottom = yp + ln.mHeight;

							OffsetRect(&r, mGutterX, mGutterY);
							ExtTextOutW(hdc, mGutterX + xp, mGutterY + yp, ETO_OPAQUE, &r, L"", 0, NULL);
							xp = xpnew;
						} else {
							SIZE siz;
							GetTextExtentPoint32A(hdc, s + xpos, xrend - xpos, &siz);

							RECT r;
							r.left = xp;
							r.right = xp + siz.cx;
							r.top = yp;
							r.bottom = yp + ln.mHeight;

							OffsetRect(&r, mGutterX, mGutterY);
							ExtTextOutA(hdc, mGutterX + xp, mGutterY + yp, ETO_OPAQUE, &r, s + xpos, xrend - xpos, NULL);
							xp += siz.cx;
						}

						xpos = xrend;
					}

					RECT r = { xp, yp, ps.rcPaint.right, yp + ln.mHeight };
					OffsetRect(&r, mGutterX, mGutterY);
					SetBkColor(hdc, backColor);
					ExtTextOutW(hdc, 0, 0, ETO_OPAQUE, &r, L"", 0, NULL);

					yp += ln.mHeight;
					ylast = yp;
				}

				++paraIdx;
			}

			if (ylast < y2 - mScrollY) {
				RECT r = { ps.rcPaint.left, ylast + mGutterY, ps.rcPaint.right, ps.rcPaint.bottom };
				SetBkColor(hdc, mColorTextBack);
				ExtTextOutW(hdc, 0, 0, ETO_OPAQUE, &r, L"", 0, NULL);
			}

			RestoreDC(hdc, state);
		}


		EndPaint(mhwnd, &ps);
	}
}

void TextEditor::OnResize() {
	RECT r;
	GetClientRect(mhwnd, &r);

	mVisibleWidth	= r.right;
	mVisibleHeight	= r.bottom;

	RecalcRelativeFontMetrics();

	Reflow(false);
	UpdateScrollPos();
	UpdateScrollRange();
}

void TextEditor::OnSetFocus() {
	::CreateCaret(mhwnd, NULL, 0, mFontHeight);
	mbCaretPresent = true;
	mbCaretVisible = false;

	UpdateCaretPos(false, false);
}

void TextEditor::OnKillFocus() {
	if (mbCaretVisible)
		::HideCaret(mhwnd);
	::DestroyCaret();
	mbCaretPresent = false;
	mbCaretVisible = false;
	mbDragging = false;
}

void TextEditor::OnKeyDown(WPARAM key) {
	switch(key) {
	case VK_LEFT:
		{
			Iterator it2(mCaretPos);
			it2.MoveToPrevChar();
			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(it2, doSelect, !doSelect);
		}
		break;
	case VK_RIGHT:
		{
			Iterator it2(mCaretPos);
			it2.MoveToNextChar();
			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(it2, doSelect, !doSelect);
		}
		break;
	case VK_UP:
		if (GetKeyState(VK_CONTROL) < 0) {
			ScrollTo(mScrollY - mFontHeight, true);
			UpdateCaretPos(false, false);
		} else {
			Iterator it2(mCaretPos);
			it2.MoveToPrevLine();
			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(it2, doSelect, !doSelect);
		}
		break;
	case VK_DOWN:
		if (GetKeyState(VK_CONTROL) < 0) {
			ScrollTo(mScrollY + mFontHeight, true);
			UpdateCaretPos(false, false);
		} else {
			Iterator it2(mCaretPos);
			it2.MoveToNextLine();
			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(it2, doSelect, !doSelect);
		}
		break;
	case VK_PRIOR:
		{
			int px, py;
			PosToPixel(px, py, mCaretPos);

			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(PixelToPos(px, py - mVisibleHeight), doSelect, !doSelect);
		}
		break;
	case VK_NEXT:
		{
			int px, py;
			PosToPixel(px, py, mCaretPos);
			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(PixelToPos(px, py + mVisibleHeight), doSelect, !doSelect);
		}
		break;
	case VK_HOME:
		{
			Iterator it2(mCaretPos);

			if (GetKeyState(VK_CONTROL) < 0)
				it2.MoveToStart();
			else
				it2.MoveToLineStart();

			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(it2, doSelect, !doSelect);
		}
		break;

	case VK_END:
		{
			Iterator it2(mCaretPos);

			if (GetKeyState(VK_CONTROL) < 0)
				it2.MoveToEnd();
			else
				it2.MoveToLineEnd();

			bool doSelect = GetKeyState(VK_SHIFT) < 0;
			MoveCaret(it2, doSelect, !doSelect);
		}
		break;
	case VK_DELETE:
		if (!mbReadOnly) {
			if (mSelectionAnchor) {
				DeleteSelection();
			} else {
				Iterator pos2(mCaretPos);
				pos2.MoveToNextChar();

				if (mCaretPos != pos2) {
					mDocument.Delete(mCaretPos, pos2);
					if (mpCB)
						mpCB->OnTextEditorUpdated();
				}
			}
		}
		break;
	}
}

void TextEditor::OnChar(int ch) {
	if (ch == 0x01) {
		SelectAll();
	} else if (ch == 0x03) {
		Copy();
	}

	if (mbReadOnly)
		return;

	if (ch == '\b') {
		Delete();
	} else {
		char c = (char)ch;

		if (mSelectionAnchor) {
			mDocument.Delete(mCaretPos, mSelectionAnchor);
			UpdateCaretPos(true, false);
		}

		if (c == '\r')
			c = '\n';

		mDocument.Insert(mCaretPos, &c, 1, NULL);

		Iterator oldPos(mCaretPos);
		Iterator newPos(mCaretPos);
		newPos.MoveToNextChar();
		MoveCaret(newPos, false, false);

		if (mpCB)
			mpCB->OnTextEditorUpdated();
	}
}

void TextEditor::OnLButtonDown(WPARAM modifiers, int x, int y) {
	MoveCaret(PixelToPos(x - mGutterX, y + mScrollY - mGutterY), GetKeyState(VK_SHIFT) < 0, false);
	mbDragging = true;
}

void TextEditor::OnLButtonUp(WPARAM modifiers, int x, int y) {
	mbDragging = false;
}

void TextEditor::OnMouseMove(WPARAM modifiers, int x, int y) {
	if (modifiers & MK_LBUTTON) {
		if (mbDragging) {
			Iterator newPos(PixelToPos(x - mGutterX, y + mScrollY - mGutterY));
			MoveCaret(newPos, true, false);
		}
	} else {
		mbDragging = false;
	}
}

void TextEditor::OnMouseWheel(int wheelClicks, WPARAM modifiers, int x, int y) {
	mMouseWheelAccum += wheelClicks * mFontHeight;
	int clicks = mMouseWheelAccum / 120;

	if (clicks) {
		mMouseWheelAccum -= clicks*120;

		UINT linesPerClick = 1;
		::SystemParametersInfo(SPI_GETWHEELSCROLLLINES, 0, &linesPerClick, FALSE);

		int lines = clicks * linesPerClick;

		ScrollTo(mScrollY - lines, true);
	}
}

void TextEditor::OnVScroll(int cmd) {
	SCROLLINFO si={sizeof(SCROLLINFO)};
	si.fMask = SIF_POS | SIF_TRACKPOS | SIF_RANGE;
	if (GetScrollInfo(mhwnd, SB_VERT, &si)) {
		int newPos = si.nPos;

		switch(cmd) {
			case SB_TOP:
				newPos = 0;
				break;
			case SB_BOTTOM:
				newPos = si.nMax - si.nPage;
				break;
			case SB_LINEUP:
				newPos -= mFontHeight;
				break;
			case SB_LINEDOWN:
				newPos += mFontHeight;
				break;
			case SB_PAGEUP:
				newPos -= mVisibleHeight;
				break;
			case SB_PAGEDOWN:
				newPos += mVisibleHeight;
				break;
			case SB_THUMBPOSITION:
				newPos = si.nTrackPos;
				break;
			case SB_THUMBTRACK:
				newPos = si.nTrackPos;
				break;
		}

		if (newPos > (int)(si.nMax - si.nPage))
			newPos = si.nMax - si.nPage;
		else if (newPos < 0)
			newPos = 0;

		if (newPos != si.nPos) {
			si.cbSize = sizeof(SCROLLINFO);
			si.fMask = SIF_POS;
			si.nPos = newPos;
			SetScrollInfo(mhwnd, SB_VERT, &si, TRUE);
		}
		
		ScrollTo(si.nPos, cmd != SB_THUMBTRACK);
	}
}

int TextEditor::OnGetText(uint32 buflen, char *s) {
	if (!s || !buflen)
		return 0;

	--buflen;

	uint32 paraCount = mDocument.GetParagraphCount();
	uint32 actual = 0;
	for(uint32 i=0; i<paraCount; ++i) {
		const Paragraph& para = *mDocument.GetParagraph(i);
		uint32 maxlen = buflen - actual;
		uint32 paralen = (uint32)para.mText.size();

		if (maxlen > paralen)
			maxlen = paralen;

		memcpy(s, para.mText.data(), maxlen);
		s += maxlen;
		actual += maxlen;
		if (actual >= buflen)
			break;
	}

	*s = 0;

	return actual;
}

int TextEditor::OnGetText(uint32 buflen, wchar_t *s) {
	if (!s || !buflen)
		return 0;

	--buflen;

	uint32 paraCount = mDocument.GetParagraphCount();
	uint32 actual = 0;
	for(uint32 i=0; i<paraCount; ++i) {
		const Paragraph& para = *mDocument.GetParagraph(i);
		uint32 maxlen = buflen - actual;
		uint32 paralen = (uint32)para.mText.size();

		if (maxlen > paralen)
			maxlen = paralen;

		for(uint32 i=0; i<maxlen; ++i)
			*s++ = (wchar_t)para.mText[i];

		actual += maxlen;
		if (actual >= buflen)
			break;
	}

	*s = 0;

	return actual;
}

int TextEditor::OnGetTextLength() {
	uint32 paraCount = mDocument.GetParagraphCount();
	int len = 0;
	for(uint32 i=0; i<paraCount; ++i) {
		const Paragraph& para = *mDocument.GetParagraph(i);

		len += (int)para.mText.size();
	}

	return len;
}

bool TextEditor::OnSetText(const char *s) {
	if (!s)		// (is this valid?)
		s = "";

	Iterator it1(mDocument);
	Iterator it2(mDocument);
	it2.MoveToEnd();
	mDocument.Delete(it1, it2);
	mDocument.Insert(it1, s, strlen(s), NULL);
	UpdateCaretPos(true, false);
	return true;
}

void TextEditor::MoveCaret(const Iterator& newPos, bool anchor, bool sendScrollUpdate) {
	if (anchor) {
		if (!mSelectionAnchor)
			AnchorSelection();
	} else {
		ClearSelection();
	}

	Iterator oldPos(mCaretPos);
	
	mCaretPos = newPos;
	UpdateCaretPos(true, sendScrollUpdate);

	if (mSelectionAnchor)
		InvalidateRange(oldPos, mCaretPos);
}

void TextEditor::ScrollTo(int y, bool sendUpdate) {
	if (y > mTotalHeight - mFontHeight)
		y = mTotalHeight - mFontHeight;
	if (y < 0)
		y = 0;

	if (y == mScrollY)
		return;

	int delta = mScrollY - y;
	mScrollY = y;

	if (abs(delta) >= mVisibleHeight)
		InvalidateRect(mhwnd, NULL, FALSE);
	else
		ScrollWindowEx(mhwnd, 0, delta, NULL, NULL, NULL, NULL, SW_INVALIDATE);

	UpdateScrollPos();
	UpdateCaretPos(false, false);

	if (mpCB && sendUpdate) {
		mpCB->OnTextEditorScrolled(mDocument.GetParagraphFromY(mScrollY),
		mDocument.GetParagraphFromY(mScrollY + mVisibleHeight),
		mFontHeight ? (mVisibleHeight + mFontHeight - 1) / mFontHeight : 0,
		mDocument.GetParagraphCount());
	}
}

void TextEditor::UpdateCaretPos(bool autoscroll, bool sendScrollUpdate) {
	int xp, yp;

	PosToPixel(xp, yp, mCaretPos);

	if (autoscroll) {
		bool tooHigh = yp < mScrollY + mEffectiveScrollVertMargin;
		bool tooLow = yp + mFontHeight + mEffectiveScrollVertMargin > mScrollY + mVisibleHeight;

		if (tooHigh) {
			if (tooLow)
				ScrollTo(yp + ((int)(mVisibleHeight - mFontHeight) >> 1), sendScrollUpdate);
			else
				ScrollTo(yp - mEffectiveScrollVertMargin, sendScrollUpdate);
		} else if (tooLow)
			ScrollTo(yp + mFontHeight - mVisibleHeight + mEffectiveScrollVertMargin, sendScrollUpdate);
	}

	if (mbCaretPresent) {
		int yo = yp - mScrollY + mGutterY;

		if (yo > -mFontHeight && yo < mVisibleHeight) {
			if (!mbCaretVisible) {
				mbCaretVisible = true;
				::ShowCaret(mhwnd);
			}
			::SetCaretPos(xp, yo);
		} else {
			if (mbCaretVisible) {
				mbCaretVisible = false;
				::HideCaret(mhwnd);
			}
		}
	}
}

void TextEditor::UpdateScrollPos() {
	SCROLLINFO si;
	si.cbSize	= sizeof(SCROLLINFO);
	si.fMask	= SIF_POS;
	si.nPos		= mScrollY;
	SetScrollInfo(mhwnd, SB_VERT, &si, TRUE);
}

void TextEditor::UpdateScrollRange() {
	SCROLLINFO si;
	si.cbSize	= sizeof(SCROLLINFO);
	si.fMask	= SIF_RANGE | SIF_DISABLENOSCROLL | SIF_PAGE;
	si.nMin		= 0;
	si.nMax		= mTotalHeight;
	si.nPage	= mVisibleHeight;
	si.nPos		= mScrollY;
	SetScrollInfo(mhwnd, SB_VERT, &si, TRUE);

	// Scrollbars have a habit of getting stuck with capture mode on if you change range or page
	// size while mouse-driven repeat scrolling is occurring, so cancel the scroll.
	SendMessage(mhwnd, WM_CANCELMODE, 0, 0);
}

void TextEditor::AnchorSelection() {
	mSelectionAnchor = mCaretPos;
}

void TextEditor::ClearSelection() {
	if (mSelectionAnchor) {
		InvalidateRange(mSelectionAnchor, mCaretPos);
		mSelectionAnchor.Detach();
	}
}

void TextEditor::InvalidateRange(const Iterator& pos1, const Iterator& pos2) {
	const Paragraph& para1 = *mDocument.GetParagraph(pos1.mPara);
	const Paragraph& para2 = *mDocument.GetParagraph(pos2.mPara);
	RECT r;

	r.left = 0;
	r.right = mVisibleWidth;

	if (para1.mYPos < para2.mYPos) {
		r.top = para1.mYPos;
		r.bottom = para2.mYPos + para2.mHeight;
	} else {
		r.top = para2.mYPos;
		r.bottom = para1.mYPos + para1.mHeight;
	}

	r.top -= mScrollY;
	r.bottom -= mScrollY;

	OffsetRect(&r, 0, mGutterY);

	InvalidateRect(mhwnd, &r, FALSE);
}

void TextEditor::RecalcFontMetrics() {
	mFontHeight = 12;
	mTabWidth = 80;
	if (HDC hdc = GetDC(mhwnd)) {
		if (int state = SaveDC(hdc)) {
			TEXTMETRIC tm;

			SelectObject(hdc, mhfont);
			if (GetTextMetrics(hdc, &tm)) {
				mFontHeight = tm.tmHeight;
				mTabWidth = tm.tmAveCharWidth * 4;
			}

			RestoreDC(hdc, state);
		}
		ReleaseDC(mhwnd, hdc);
	}

	mScrollVertMargin = mFontHeight * 2;
	RecalcRelativeFontMetrics();
}

void TextEditor::RecalcRelativeFontMetrics() {
	mEffectiveScrollVertMargin = mScrollVertMargin;

	int maxVertMargin = mVisibleHeight / 3;

	if (mFontHeight)
		maxVertMargin -= maxVertMargin % mFontHeight;

	if (mEffectiveScrollVertMargin > maxVertMargin)
		mEffectiveScrollVertMargin = maxVertMargin;
}

void TextEditor::Reflow(bool force) {
	const int w = mVisibleWidth;

	if ((w == mReflowWidth || !w) && !force)
		return;

	if (HDC hdc = GetDC(mhwnd)) {
		if (int state = SaveDC(hdc)) {
			SelectObject(hdc, mhfont);

			mReflowWidth = w;

			int paraCount = mDocument.GetParagraphCount();

			for(int paraIdx=0; paraIdx<paraCount; ++paraIdx) {
				mDocument.ReflowPara(paraIdx);
			}

			RestoreDC(hdc, state);
		}

		ReleaseDC(mhwnd, hdc);
	}

	mDocument.RecomputeParaPositions();

	InvalidateRect(mhwnd, NULL, FALSE);
	UpdateCaretPos(false, false);
}

void TextEditor::CutCopy(bool doCut) {
	Iterator first, second;

	if (mSelectionAnchor) {
		first = mSelectionAnchor;
		second = mCaretPos;
	} else {
		first = mCaretPos;
		first.MoveToLineStart();
		second = mCaretPos;
		second.MoveToNextLine();
		second.MoveToLineStart();
	}

	if (first != second) {
		vdfastvector<char> buf;
		mDocument.GetText(first, second, true, buf);
		size_t len = buf.size();

		if (OpenClipboard(mhwnd)) {
			if (EmptyClipboard()) {
				HGLOBAL hmem = GlobalAlloc(GMEM_MOVEABLE, len + 1);
				if (hmem) do {
					char *p = (char *)GlobalLock(hmem);
					if (p) {
						memcpy(p, buf.data(), len);
						p[len] = 0;

						if (SetClipboardData(CF_TEXT, hmem)) {
							if (doCut) {
								mDocument.Delete(first, second);
								UpdateCaretPos(true, false);
							}

							hmem = NULL;
							break;
						}

						GlobalUnlock(hmem);
					}

					GlobalFree(hmem);
				} while(false);
			}

			CloseClipboard();
		}
	}
}

void TextEditor::ReloadColors() {
	mColorTextBack = GetSysColor(COLOR_WINDOW);
	mColorTextFore = GetSysColor(COLOR_WINDOWTEXT);
	mColorTextHiBack = GetSysColor(COLOR_HIGHLIGHT);
	mColorTextHiFore = GetSysColor(COLOR_HIGHLIGHTTEXT);
}

void TextEditor::PosToPixel(int& xp, int& yp, const Iterator& pos) {
	const Paragraph& para = *mDocument.GetParagraph(pos.mPara);

	xp = 0;
	yp = pos.GetYPos();
	VDASSERT(yp >= 0);

	if (!pos.mOffset)
		return;

	HDC hdc = GetDC(mhwnd);
	if (!hdc)
		return;

	if (int state = SaveDC(hdc)) {
		SelectObject(hdc, mhfont);

		vdfastvector<char> text;
		mDocument.GetParagraphText(pos.mPara, text);

		const Line& ln = para.mLines[pos.mLine];
		const char *s = text.data();

		int index = ln.mStart;
		int end = index + pos.mOffset;

		while(index < end) {
			int spanend = end;
			const char *tab = (const char *)memchr(s + index, '\t', end - index);
			if (tab) {
				spanend = (int)(tab - s);
				if (spanend == index) {
					xp += mTabWidth;
					xp -= xp % mTabWidth;
					++index;
					continue;
				}
			}

			SIZE sz;
			if (GetTextExtentPoint32A(hdc, s + index, spanend - index, &sz))
				xp += sz.cx;

			index = spanend;
		}

		RestoreDC(hdc, state);
	}

	ReleaseDC(mhwnd, hdc);
}

Iterator TextEditor::PixelToPos(int px, int py) {
	int paraIdx = mDocument.GetParagraphFromY(py);
	const Paragraph& para = *mDocument.GetParagraph(paraIdx);

	py -= para.mYPos;

	int lineIdx = 0;
	int lineCount = (int)para.mLines.size();
	while(lineIdx < lineCount - 1) {
		int lineHeight = para.mLines[lineIdx].mHeight;
		if (py >= lineHeight)
			py -= lineHeight;
		else
			break;

		++lineIdx;
	}

	int offset = 0;
	int xp = 0;

	if (px) {
		HDC hdc = GetDC(mhwnd);
		if (hdc) {
			if (int state = SaveDC(hdc)) {
				SelectObject(hdc, mhfont);

				const Line& ln = para.mLines[lineIdx];
				vdfastvector<char> text;
				mDocument.GetParagraphText(paraIdx, text);

				const char *s = text.data();

				int index = ln.mStart;
				int end = index + ln.mLength;

				offset = end;

				while(index < end) {
					int spanend = end;
					const char *tab = (const char *)memchr(s + index, '\t', end - index);
					if (tab) {
						spanend = (int)(tab - s);
						if (spanend == index) {
							int newxp = xp + mTabWidth;
							newxp -= newxp % mTabWidth;

							if (newxp >= px) {
								offset = index;
								if (px + px > newxp + xp)
									++offset;
								break;
							}

							++index;
							xp = newxp;
							continue;
						}
					}

					SIZE sz;
					INT fit;
					int sublen = spanend - index;
					if (GetTextExtentExPointA(hdc, s + index, sublen, px - xp, &fit, NULL, &sz)) {
						if (fit < sublen) {
							offset = index + fit;
							break;
						}

						xp += sz.cx;
					}

					index = spanend;
				}

				RestoreDC(hdc, state);
			}

			ReleaseDC(mhwnd, hdc);
		}
	}

	return Iterator(mDocument, paraIdx, lineIdx, offset);
}

void TextEditor::InvalidateRows(int ystart, int yend) {
	RECT r;
	r.left = 0;
	r.top = ystart - mScrollY + mGutterY;
	r.bottom = yend - mScrollY + mGutterY;
	r.right = mVisibleWidth;

	if (r.top < 0)
		r.top = 0;

	if (r.bottom > mVisibleHeight)
		r.bottom = mVisibleHeight;

	if (r.bottom > r.top)
		InvalidateRect(mhwnd, &r, FALSE);
}

void TextEditor::VerticalShiftRows(int ysrc, int ydst) {
	// check for scroll below bottom of window; we can ignore those
	if (std::min(ysrc, ydst) - mScrollY < mVisibleHeight)
		return;

	// check for scroll bigger than screen
	int delta = ydst - ysrc;
	if (abs(delta) >= mVisibleHeight) {
		InvalidateRect(mhwnd, NULL, FALSE);
		return;
	}

	RECT r;
	r.left = 0;
	r.right = mVisibleWidth;
	r.bottom = mVisibleHeight;

	if (ysrc < ydst)
		r.top = ysrc - mScrollY;
	else
		r.top = ydst - mScrollY;

	if (r.top < 0)
		r.top = 0;

	if (r.bottom > r.top)
		ScrollWindowEx(mhwnd, 0, delta, &r, &r, NULL, NULL, SW_INVALIDATE);
}

void TextEditor::ReflowPara(int paraIdx, const Paragraph& para) {
	if (!mbWordWrap) {
		const Line& ln1 = para.mLines.front();
		const Line& ln2 = para.mLines.back();

		Line ln;
		ln.mHeight = mFontHeight;
		ln.mStart = ln1.mStart;
		ln.mLength = ln2.mStart - ln1.mStart + ln2.mLength;
		mDocument.ReflowPara(paraIdx, &ln, 1);
		return;
	}

	HDC hdc = GetDC(mhwnd);
	if (hdc) {
		if (int state = SaveDC(hdc)) {
			SelectObject(hdc, mhfont);

			mReflowNewLines.clear();

			const char *sStart = para.mText.data();
			const char *sEnd = sStart + para.mText.size();
			const char *s = sStart;

			if (s == sEnd) {
				Line ln;

				ln.mStart = 0;
				ln.mLength = 0;
				ln.mHeight = mFontHeight;
				mReflowNewLines.push_back(ln);
			}

			while(s < sEnd) {
				INT n;
				SIZE sz;
				if (!GetTextExtentExPointA(hdc, s, (int)(sEnd - s), mReflowWidth, &n, NULL, &sz))
					break;

				if (sz.cx > mReflowWidth)
					--n;

				const char *sBreak = s + n;

				if (sBreak != sEnd) {
					while(sBreak != s) {
						const char c = sBreak[-1];

						if (c==' ' || c=='\t')
							break;

						--sBreak;
					}

					if (sBreak == s)
						sBreak = s + n;
				}

				const char *sLineEnd = sBreak;

				while(sLineEnd != s) {
					const char c = sLineEnd[-1];

					if (c!=' ' && c!='\t')
						break;

					--sLineEnd;
				}

				while(sBreak != sEnd) {
					const char c = *sBreak;

					if (c!=' ' && c!='\t')
						break;

					++sBreak;
				}

				Line ln;

				ln.mStart = (int)(s - sStart);
				ln.mLength = (int)(sBreak - s);
				ln.mHeight = mFontHeight;

				mReflowNewLines.push_back(ln);

				VDASSERT(s != sBreak);

				s = sBreak;
			}
			RestoreDC(hdc, state);
		}

		ReleaseDC(mhwnd, hdc);
	}

	mDocument.ReflowPara(paraIdx, mReflowNewLines.data(), mReflowNewLines.size());
}

namespace {
	struct SpanSorter {
		bool operator()(const Span& a, const Span& b) const {
			return a.mStart < b.mStart;
		}
	};

	class ParagraphColorization : public IVDTextEditorColorization {
	public:
		ParagraphColorization(int limit);

		void AddTextColorPoint(int start, sint32 fore, sint32 back);

		void Finalize(Paragraph& dst);

	private:
		typedef vdfastvector<Span> Spans;
		Spans	mSpans;

		int		mLimit;
	};

	ParagraphColorization::ParagraphColorization(int limit)
		: mLimit(limit)
	{
		Span sp;
		sp.mStart = 0;
		sp.mForeColor = -1;
		sp.mBackColor = -1;

		mSpans.push_back(sp);
	}

	void ParagraphColorization::AddTextColorPoint(int start, sint32 fore, sint32 back) {
		if (start > mLimit)
			start = mLimit;

		if (start < 0)
			start = 0;

		Span sp;
		sp.mStart = start;
		sp.mForeColor = fore < 0 ? -1 : VDSwizzleU32(fore) >> 8;
		sp.mBackColor = back < 0 ? -1 : VDSwizzleU32(back) >> 8;
		mSpans.push_back(sp);
	}

	void ParagraphColorization::Finalize(Paragraph& dst) {
		std::stable_sort(mSpans.begin(), mSpans.end(), SpanSorter());

		Spans::const_iterator it(mSpans.begin()), itEnd(mSpans.end());
		Spans::iterator itDst(mSpans.begin());
		int lastPos = -1;
		for(; it != itEnd; ++it) {
			const Span& sp = *it;

			if (sp.mStart == lastPos)
				--itDst;
			else
				lastPos = sp.mStart;

			*itDst = sp;
			++itDst;
		}

		dst.mSpans.assign(mSpans.begin(), itDst);
	}
};

void TextEditor::RecolorParagraph(int paraIdx, Paragraph& para) {
	para.mSpans.clear();

	if (mpColorizer) {
		int n = (int)para.mText.size();
		ParagraphColorization cz(n);

		mpColorizer->RecolorLine(paraIdx, para.mText.data(), n, &cz);

		cz.Finalize(para);
	} else {
		Span sp[1];
		sp[0].mStart = 0;
		sp[0].mForeColor = -1;
		sp[0].mBackColor = -1;

		para.mSpans.assign(sp, sp+1);
	}
}

void TextEditor::ChangeTotalHeight(int y) {
	if (y == mTotalHeight)
		return;

	mTotalHeight = y;

	if (mbUpdateEnabled)
		UpdateScrollRange();
	else
		mbUpdateScrollbarPending = true;
}
