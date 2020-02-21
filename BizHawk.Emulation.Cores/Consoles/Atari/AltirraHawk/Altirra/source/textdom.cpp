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
#include "textdom.h"

namespace nsVDTextDOM {

///////////////////////////////////////////////////////////////////////////////

namespace {
	struct LinesByOffsetPred {
		bool operator()(const Line& x, const Line& y) const {
			return x.mStart < y.mStart;
		}

		bool operator()(const Line& x, int y) const {
			return x.mStart < y;
		}

		bool operator()(int x, const Line& y) const {
			return x < y.mStart;
		}
	};

	struct SpansByOffsetPred {
		bool operator()(const Span& x, const Span& y) const {
			return x.mStart < y.mStart;
		}

		bool operator()(int x, const Span& y) const {
			return x < y.mStart;
		}

		bool operator()(const Span& x,int y) const {
			return x.mStart < y;
		}
	};

	struct LinesByStartPred {
		bool operator()(const Line& x, const Line& y) const {
			return x.mStart < y.mStart;
		}

		bool operator()(int x, const Line& y) const {
			return x < y.mStart;
		}

		bool operator()(const Line& x,int y) const {
			return x.mStart < y;
		}
	};

	struct LinesByEndPred {
		bool operator()(const Line& x, const Line& y) const {
			return x.mStart+x.mLength < y.mStart+y.mLength;
		}

		bool operator()(int x, const Line& y) const {
			return x < y.mStart+y.mLength;
		}

		bool operator()(const Line& x,int y) const {
			return x.mStart+x.mLength < y;
		}
	};
}

int Paragraph::GetLineIndexFromOffset(int offset) const {
	Lines::const_iterator it = std::upper_bound(mLines.begin(), mLines.end(), offset, LinesByOffsetPred());
	int lnidx = 0;

	if (it != mLines.begin())
		lnidx = (int)((it - mLines.begin()) - 1);

	return lnidx;
}

int Paragraph::GetSpanIndexFromOffset(int offset) const {
	Spans::const_iterator it = std::upper_bound(mSpans.begin(), mSpans.end(), offset, SpansByOffsetPred());
	int spidx = 0;

	if (it != mSpans.begin())
		spidx = (int)((it - mSpans.begin()) - 1);

	return spidx;
}

void Paragraph::Insert(int line, int offset, const char *s, size_t len) {
	if (!len)
		return;

	VDASSERT((unsigned)line < mLines.size());
	Line& ln = mLines[line];
	VDASSERT((unsigned)offset <= (unsigned)ln.mLength);

	mText.insert(mText.begin() + ln.mStart + offset, s, s+len);
	ln.mLength += (int)len;

	Spans::iterator itSEnd(mSpans.end());
	Spans::iterator itS(std::lower_bound(mSpans.begin(), itSEnd, offset + 1, SpansByOffsetPred()));
	for(; itS!=itSEnd; ++itS)
		itS->mStart += (int)len;

	Lines::iterator itL(mLines.begin() + line + 1), itLEnd(mLines.end());
	for(; itL!=itLEnd; ++itL)
		itL->mStart += (int)len;

	// check if we added a newline
	if (s[len - 1] == '\n')
		--ln.mLength;

	Validate();
}

void Paragraph::Append(const Paragraph& src) {
	AppendSpans(src);
	AppendLines(src);
	mText.insert(mText.end(), src.mText.begin(), src.mText.end());
	Validate();
}

void Paragraph::AppendSpans(const Paragraph& src) {
	Spans::const_iterator it1(src.mSpans.begin());
	Spans::const_iterator it2(src.mSpans.end());

	// check for splice case
	int charDelta = (int)mText.size();

	if (!mSpans.empty() && !src.mSpans.empty()) {
		const Span& sp1 = mSpans.back();
		const Span& sp2 = mSpans.front();

		if (sp1.mForeColor == sp2.mForeColor && sp1.mBackColor == sp2.mBackColor)
			++it1;
	}

	// fixup positions on added spans
	Spans::iterator it3(mSpans.insert(mSpans.end(), it1, it2));
	Spans::iterator it4(mSpans.end());
	for(; it3 != it4; ++it3)
		it3->mStart += charDelta;
}

void Paragraph::AppendLines(const Paragraph& src) {
	Lines::const_iterator it1(src.mLines.begin());
	Lines::const_iterator it2(src.mLines.end());

	// fixup positions on added lines
	int charDelta = (int)mText.size();
	Lines::iterator it3(mLines.insert(mLines.end(), it1, it2));
	Lines::iterator it4(mLines.end());
	for(; it3 != it4; ++it3) {
		it3->mStart += charDelta;
		mHeight += it3->mHeight;
	}
}

void Paragraph::DeleteFromStart(int line, int offset) {
	Validate();
	int charPos = mLines[line].mStart + offset;
	if (line)
		mLines.erase(mLines.begin(), mLines.begin() + line - 1);

	mLines.front().mStart += offset;
	mLines.front().mLength -= offset;

	mHeight = 0;
	for(Lines::iterator it(mLines.begin()), itEnd(mLines.end()); it!=itEnd; ++it) {
		it->mStart -= charPos;
		mHeight += it->mHeight;
	}

	DeleteOffsetRangeSpans(0, charPos);
	mText.erase(mText.begin(), mText.begin() + charPos);
	Validate();
}

void Paragraph::DeleteToEnd(int line, int offset) {
	int charPos = mLines[line].mStart + offset;

	mLines.erase(mLines.begin() + line + 1, mLines.end());
	mLines[line].mLength = offset;

	mHeight = 0;
	for(int i=0; i<=line; ++i)
		mHeight += mLines[i].mHeight;

	DeleteOffsetRangeSpans(charPos, (int)mText.size());
	mText.erase(mText.begin() + charPos, mText.end());
	Validate();
}

void Paragraph::DeleteRange(int startLine, int startOffset, int endLine, int endOffset) {
	VDASSERT((unsigned)startLine < mLines.size());
	VDASSERT((unsigned)endLine < mLines.size());
	VDASSERT((unsigned)startOffset <= (unsigned)mLines[startLine].mLength);
	VDASSERT((unsigned)endOffset <= (unsigned)mLines[endLine].mLength);

	int charStart = mLines[startLine].mStart + startOffset;
	int charEnd = mLines[endLine].mStart + endOffset;

	for(int i=startLine + 1; i <= endLine; ++i)
		mHeight -= mLines[i].mHeight;

	for(Lines::iterator it(mLines.begin() + endLine + 1), itEnd(mLines.end()); it != itEnd; ++it)
		it->mStart -= (charEnd - charStart);

	mLines[startLine].mLength = startOffset + (mLines[endLine].mLength - endOffset);
	mLines.erase(mLines.begin() + startLine + 1, mLines.begin() + endLine + 1);

	mText.erase(mText.begin() + charStart, mText.begin() + charEnd);

	// fixup spans
	DeleteOffsetRangeSpans(charStart, charEnd);

	Validate();
}

void Paragraph::DeleteOffsetRangeSpans(int start, int end) {
	Spans::iterator itB(mSpans.begin());
	Spans::iterator itE(mSpans.end());
	Spans::iterator it1(std::lower_bound(itB, itE, start, SpansByOffsetPred()));
	Spans::iterator it2(std::upper_bound(itB, itE, end, SpansByOffsetPred()));

	if (it1 == itB) {
		if (it2 == itB)
			return;
		++it1;
	}

	VDASSERT(it1 <= it2);

	if (it1 != it2 && it2 != itE && itE->mStart != end) {
		--it2;
		it2->mStart = end;
	}

	it1 = mSpans.erase(it1, it2);
	itE = mSpans.end();

	int len = end - start;
	for(; it1!=itE; ++it1) {
		it1->mStart -= len;
	}
}

void Paragraph::Split(int line, int offset, Paragraph& dst) {
	VDASSERT((unsigned)line < mLines.size());
	Line& ln = mLines[line];
	VDASSERT((unsigned)offset <= (unsigned)ln.mLength);

	int pos = ln.mStart + offset;

	SplitSpans(pos, dst);
	SplitLines(line, offset, dst);

	dst.mText.assign(mText.begin() + pos, mText.end());
	mText.resize(pos);

	Validate();
	dst.Validate();
}

void Paragraph::SplitSpans(int offset, Paragraph& dst) {
	Spans::iterator itEnd(mSpans.end());
	Spans::iterator it(std::upper_bound(mSpans.begin(), itEnd, offset - 1, SpansByOffsetPred()));

	if (it == itEnd || it->mStart > offset) {
		dst.mSpans.assign(it - 1, itEnd);
		dst.mSpans.front().mStart = offset;
	} else
		dst.mSpans.assign(it, itEnd);

	for(Spans::iterator it2(dst.mSpans.begin()), it2End(dst.mSpans.end()); it2 != it2End; ++it2) {
		it2->mStart -= offset;
	}

	if (it == mSpans.begin())
		++it;

	mSpans.erase(it, mSpans.end());
}

void Paragraph::SplitLines(int line, int offset, Paragraph& dst) {
	Lines::iterator itBegin(mLines.begin());
	Lines::iterator itEnd(mLines.end());
	Lines::iterator it(itBegin + line);

	dst.mLines.assign(it, itEnd);
	mLines.erase(it + 1, itEnd);
	it->mLength = offset;

	Lines::iterator it2(dst.mLines.begin());
	Lines::iterator it2End(dst.mLines.end());
	VDASSERT(it2 != it2End);

	it2->mStart = 0;
	it2->mLength -= offset;
	dst.mHeight = it2->mHeight;
	++it2;

	for(; it2 != it2End; ++it2) {
		Line& ln = *it2;
		ln.mStart -= offset;
		dst.mHeight += ln.mHeight;
		mHeight -= ln.mHeight;
	}
}

void Paragraph::Validate() {
	VDASSERT(!mSpans.empty());
	VDASSERT(!mLines.empty());
	VDASSERT(mSpans.front().mStart == 0);
	VDASSERT(mLines.front().mStart == 0);

	Lines::const_iterator it(mLines.begin());
	Lines::const_iterator itEnd(mLines.end());
	for(; it != itEnd; ++it) {
		const Line& ln = *it;

		VDASSERT(ln.mStart >= 0 && (unsigned)ln.mStart <= mText.size());
		VDASSERT((unsigned)(ln.mStart + ln.mLength) <= mText.size());
	}
}

///////////////////////////////////////////////////////////////////////////////

Iterator::Iterator()
	: mPara(0)
	, mLine(0)
	, mOffset(0)
	, mpParent(NULL)
{
}

Iterator::Iterator(Document& doc, int para, int line, int offset)
	: mPara(para)
	, mLine(line)
	, mOffset(offset)
	, mpParent(&doc)
{
	mpParent->mIterators.push_back(this);
}

Iterator::Iterator(const Iterator& src)
	: mPara(src.mPara)
	, mLine(src.mLine)
	, mOffset(src.mOffset)
	, mpParent(src.mpParent)
{
	if (mpParent)
		mpParent->mIterators.push_back(this);
}

Iterator::~Iterator() {
	if (mpParent)
		mpParent->mIterators.erase(this);
}

bool Iterator::operator==(const Iterator& src) const {
	return 0 == ((mPara ^ src.mPara) | (mLine ^ src.mLine) | (mOffset ^ src.mOffset));
}

bool Iterator::operator!=(const Iterator& src) const {
	return 0 != ((mPara ^ src.mPara) | (mLine ^ src.mLine) | (mOffset ^ src.mOffset));
}

bool Iterator::operator<(const Iterator& src) const {
	return mPara < src.mPara ||
		(mPara == src.mPara &&
		(mLine < src.mLine ||
		(mLine == src.mLine &&
		mOffset < src.mOffset)));
}

bool Iterator::operator<=(const Iterator& src) const {
	return mPara < src.mPara ||
		(mPara == src.mPara &&
		(mLine < src.mLine ||
		(mLine == src.mLine &&
		mOffset <= src.mOffset)));
}

bool Iterator::operator>(const Iterator& src) const {
	return mPara > src.mPara ||
		(mPara == src.mPara &&
		(mLine > src.mLine ||
		(mLine == src.mLine &&
		mOffset > src.mOffset)));
}

bool Iterator::operator>=(const Iterator& src) const {
	return mPara > src.mPara ||
		(mPara == src.mPara &&
		(mLine > src.mLine ||
		(mLine == src.mLine &&
		mOffset >= src.mOffset)));
}

int Iterator::GetYPos() const {
	if (mpParent) {
		const Paragraph *para = mpParent->mParagraphs[mPara];
		int y = para->mYPos;

		for(int i=0; i<mLine; ++i)
			y += para->mLines[i].mHeight;

		return y;
	}

	return 0;
}

int Iterator::GetParaOffset() const {
	if (mpParent) {
		const Paragraph *para = mpParent->mParagraphs[mPara];

		return para->mLines[mLine].mStart + mOffset;
	}
	return 0;
}

Iterator& Iterator::operator=(const Iterator& src) {
	if (mpParent != src.mpParent) {
		if (mpParent)
			mpParent->mIterators.erase(this);
		mpParent = src.mpParent;
		if (mpParent)
			mpParent->mIterators.push_back(this);
	}
	mPara = src.mPara;
	mLine = src.mLine;
	mOffset = src.mOffset;
	return *this;
}

void Iterator::Attach(Document& doc) {
	if (mpParent != &doc) {
		if (mpParent)
			mpParent->mIterators.erase(this);
		mpParent = &doc;
		mpParent->mIterators.push_back(this);
	}
	mPara = 0;
	mLine = 0;
	mOffset = 0;
}

void Iterator::Detach() {
	if (mpParent) {
		mpParent->mIterators.erase(this);
		mpParent = NULL;
	}
}

void Iterator::Swap(Iterator& src) {
	if (mpParent != src.mpParent) {
		if (mpParent)
			mpParent->mIterators.erase(this);

		if (src.mpParent)
			src.mpParent->mIterators.erase(&src);

		std::swap(mpParent, src.mpParent);

		if (mpParent)
			mpParent->mIterators.push_back(this);

		if (src.mpParent)
			src.mpParent->mIterators.push_back(&src);
	}

	std::swap(mPara, src.mPara);
	std::swap(mLine, src.mLine);
	std::swap(mOffset, src.mOffset);
}

void Iterator::Validate() {
	if (mpParent) {
		VDASSERT((unsigned)mPara < (unsigned)mpParent->GetParagraphCount());

		const Paragraph& para = *mpParent->GetParagraph(mPara);
		VDASSERT(mLine < (int)para.mLines.size());

		const Line& line = para.mLines[mLine];
		VDASSERT(mOffset <= line.mLength);
	}
}

void Iterator::MoveToParaOffset(int paraIdx, int offset) {
	if (!mpParent || paraIdx < 0) {
		MoveToStart();
		return;
	}

	const Paragraph *para = mpParent->GetParagraph(paraIdx);
	if (!para) {
		MoveToEnd();
		return;
	}

	mPara = paraIdx;

	if (offset < 0)
		offset = 0;

	mLine = para->GetLineIndexFromOffset(offset);

	const Line& ln = para->mLines[mLine];
	mOffset = offset - ln.mStart;
	if (mOffset < 0)
		mOffset = 0;
	else if (mOffset > ln.mLength)
		mOffset = ln.mLength;
}

void Iterator::MoveToStart() {
	mPara = 0;
	mLine = 0;
	mOffset = 0;
}

void Iterator::MoveToEnd() {
	if (mpParent) {
		mPara = (int)mpParent->mParagraphs.size() - 1;

		const Paragraph *para = mpParent->mParagraphs[mPara];
		mLine = (int)para->mLines.size() - 1;
		mOffset = para->mLines.back().mLength;
	}
}

void Iterator::MoveToPrevChar() {
	if (mpParent) {
		const Paragraph *para = mpParent->mParagraphs[mPara];

		if (mOffset > 0)
			--mOffset;
		else {
			if (mLine > 0) {
				--mLine;
				mOffset = para->mLines[mLine].mLength;
			} else {
				if (mPara > 0) {
					--mPara;
					para = mpParent->mParagraphs[mPara];
					mLine = (int)para->mLines.size() - 1;
					mOffset = para->mLines.back().mLength;
				}
			}
		}
	}
}

void Iterator::MoveToNextChar() {
	if (mpParent) {
		const Paragraph *para = mpParent->mParagraphs[mPara];
		const Line& ln = para->mLines[mLine];

		if (mOffset < ln.mLength)
			++mOffset;
		else {
			if (mLine + 1 < (int)para->mLines.size()) {
				++mLine;
				mOffset = 0;
			} else {
				if (mPara + 1 < (int)mpParent->mParagraphs.size()) {
					++mPara;
					mLine = 0;
					mOffset = 0;
				}
			}
		}
	}
}

void Iterator::MoveToLineStart() {
	mOffset = 0;
}

void Iterator::MoveToLineEnd() {
	if (mpParent) {
		const Paragraph *para = mpParent->mParagraphs[mPara];
		const Line& ln = para->mLines[mLine];
		mOffset = ln.mLength;
	}
}

void Iterator::MoveToPrevLine() {
	if (mpParent) {
		Paragraph *para = mpParent->mParagraphs[mPara];

		if (mLine > 0) {
			--mLine;
		} else {
			if (mPara > 0) {
				--mPara;
				para = mpParent->mParagraphs[mPara];
				mLine = (int)para->mLines.size() - 1;
			}
		}

		int maxChar = para->mLines[mLine].mLength;
		if (mOffset > maxChar)
			mOffset = maxChar;
	}
}

void Iterator::MoveToNextLine() {
	if (mpParent) {
		Paragraph *para = mpParent->mParagraphs[mPara];

		if (mLine + 1 < (int)para->mLines.size()) {
			++mLine;
		} else {
			if (mPara + 1 < (int)mpParent->mParagraphs.size()) {
				++mPara;
				para = mpParent->mParagraphs[mPara];
				mLine = 0;
			}
		}

		int maxChar = para->mLines[mLine].mLength;
		if (mOffset > maxChar)
			mOffset = maxChar;
	}
}

///////////////////////////////////////////////////////////////////////////////

namespace {
	struct ParagraphsByYPred {
		bool operator()(const Paragraph *p1, const Paragraph *p2) const {
			return p1->mYPos < p2->mYPos;
		}

		bool operator()(const Paragraph *p1, int pos2) const {
			return p1->mYPos < pos2;
		}

		bool operator()(int pos1, const Paragraph *p2) const {
			return pos1 < p2->mYPos;
		}
	};
}

Document::Document()
	: mpCB(NULL)
{
	Paragraph *para = new Paragraph;
	mParagraphs.push_back(para);

	para->mHeight = 0;
	para->mYPos = 0;

	para->mLines.resize(1);
	Line& ln = para->mLines.back();
	ln.mStart = 0;
	ln.mLength = 0;
	ln.mHeight = 0;

	para->mSpans.resize(1);
	Span& sp = para->mSpans.back();
	sp.mStart = 0;
	sp.mForeColor = -1;
	sp.mBackColor = -1;

	mTotalHeight = 0;
}

Document::~Document() {
	while(!mParagraphs.empty()) {
		Paragraph *p = mParagraphs.back();

		if (p)
			delete p;

		mParagraphs.pop_back();
	}
}

void Document::SetCallback(IDocumentCallback *pCB) {
	mpCB = pCB;
}

int Document::GetParagraphFromY(int y) {
	Paragraphs::iterator it(std::upper_bound(mParagraphs.begin(), mParagraphs.end(), y, ParagraphsByYPred()));
	int para = 0;

	if (it != mParagraphs.begin())
		para = (int)(it - mParagraphs.begin()) - 1;

	return para;
}

void Document::GetParagraphText(int paraIdx, vdfastvector<char>& buf) {
	const Paragraph *para = mParagraphs[paraIdx];

	buf = para->mText;
}

void Document::GetText(const Iterator& it1, const Iterator& it2, bool forceCRLF, vdfastvector<char>& buf) {
	if (it1 > it2)
		return GetText(it2, it1, forceCRLF, buf);

	buf.clear();

	for(int paraIdx = it1.mPara; paraIdx <= it2.mPara; ++paraIdx) {
		const Paragraph& para = *mParagraphs[paraIdx];

		int offset1 = 0;
		if (paraIdx == it1.mPara)
			offset1 = it1.GetParaOffset();

		int offset2;
		bool includeEnd = false;
		if (paraIdx == it2.mPara) {
			offset2 = it2.GetParaOffset();
		} else {
			offset2 = para.mLines.back().GetEnd();
			includeEnd = true;
		}

		buf.insert(buf.end(), para.mText.begin() + offset1, para.mText.begin() + offset2);
		if (includeEnd) {
			if (forceCRLF)
				buf.push_back('\r');
			buf.push_back('\n');
		}
	}
}

void Document::Insert(const Iterator& it, const char *text, size_t len, Iterator *after) {
	if (!len)
		return;

	Paragraph *para = mParagraphs[it.mPara];
	Line& ln = para->mLines[it.mLine];
	bool atEndOfLine = it.mOffset >= ln.mLength;

	vdfastvector<Paragraph *> newParas;
	Paragraph *lastPara = NULL;

	int paraIdx = it.mPara;
	int topY = para->mYPos;
	int bottomYOld = para->GetYBottom();

	// check for newline
	const char *s = (const char *)memchr(text, '\n', len);
	bool splitRequired = false;
	if (!s)
		s = text+len;
	else {
		++s;
		splitRequired = true;
	}

	// insert text into paragraph
	int lastAdded = (int)len;
	if (s > text) {
		if (splitRequired) {
			lastPara = new Paragraph;
			lastPara->mHeight = 0;
			lastPara->mYPos = para->mYPos;
			
			para->Split(it.mLine, it.mOffset, *lastPara);
		}
		para->Insert(it.mLine, it.mOffset, text, s-text);

		for(Iterators::iterator itI(mIterators.begin()), itIEnd(mIterators.end()); itI!=itIEnd; ++itI) {
			Iterator& it2 = **itI;

			if (it2.mPara != paraIdx)
				continue;

			if (it2 > it)
				it2.mOffset += (int)(s - text);
		}
		len -= (s-text);
		text = s;

		ReflowPara(paraIdx);
	}
	
	// generate new lines
	while(s = (const char *)memchr(text, '\n', len)) {
		++s;

		Paragraph *p = new Paragraph;
		p->mHeight = 0;
		p->mYPos = para->mYPos;

		Span sp(para->mSpans.back());
		sp.mStart = 0;
		p->mSpans.push_back(sp);
		
		Line ln(para->mLines.back());
		ln.mStart = 0;
		ln.mLength = (int)((s - 1) - text);
		p->mLines.push_back(ln);

		p->mText.assign(text, s);

		len -= (s-text);
		text = s;

		newParas.push_back(p);
	}

	if (lastPara) {
		lastAdded = (int)len;
		if (len)
			lastPara->Insert(0, 0, text, len);

		newParas.push_back(lastPara);
	}

	// bump paragraphs
	int parasAdded = (int)newParas.size();
	if (parasAdded) {
		mParagraphs.insert(mParagraphs.begin() + it.mPara + 1, newParas.begin(), newParas.end());

		for(int i=0; i<parasAdded; ++i)
			ReflowPara(paraIdx + i + 1);

		// fixup iterators
		for(Iterators::iterator itI(mIterators.begin()), itIEnd(mIterators.end()); itI!=itIEnd; ++itI) {
			Iterator& it2 = **itI;

			if (it2.mPara < it.mPara)
				continue;
			
			if (it2.mPara > it.mPara) {
				it2.mPara += parasAdded;
				continue;
			}

			if (it2.mLine < it.mLine)
				continue;

			if (it2.mLine == it.mLine) {
				if (it2.mOffset > it.mOffset) {
					it2.mPara += parasAdded;
					it2.mLine = 0;
					it2.mOffset += lastAdded - it.mOffset;
				}
				continue;
			}

			it2.mLine -= it.mLine;
			if (atEndOfLine)
				--it2.mLine;
		}

		RecomputeParaPositions();

		if (after) {
			// Note: *after may alias it!
			after->Attach(*this);
			after->mPara = paraIdx + parasAdded;
			after->mLine = 0;
			after->mOffset = lastAdded;
			after->Validate();
		}
	} else {
		// fixup iterators
		for(Iterators::iterator itI(mIterators.begin()), itIEnd(mIterators.end()); itI!=itIEnd; ++itI) {
			Iterator& it2 = **itI;

			if (it2.mPara == it.mPara && it2.mLine == it.mLine && it2.mOffset > it.mOffset)
				it2.mOffset += lastAdded;
		}

		if (after) {
			// Note: *after may alias it!
			int lineIdx = it.mLine;
			int offset = it.mOffset;

			after->Attach(*this);
			after->mPara = paraIdx;
			after->mLine = lineIdx;
			after->mOffset = offset + lastAdded;
			after->Validate();
		}
	}

	int bottomYNew = mParagraphs[paraIdx + parasAdded]->GetYBottom();

	if (mpCB) {
		mpCB->VerticalShiftRows(bottomYOld, bottomYNew);
		mpCB->InvalidateRows(topY, bottomYNew);
	}
}

void Document::Delete(const Iterator& it1, const Iterator& it2) {
	VDASSERT(!(it1 < it2 && it2 < it1));
	if (it2 < it1)
		return Delete(it2, it1);
	else if (it2 == it1)
		return;

	int paraIdx1 = it1.mPara;
	int paraIdx2 = it2.mPara;
	int lineIdx1 = it1.mLine;
	int lineIdx2 = it2.mLine;
	int offset1 = it1.mOffset;
	int offset2 = it2.mOffset;

	int topY = mParagraphs[paraIdx1]->mYPos;
	int bottomYOld = mParagraphs[paraIdx2]->GetYBottom();

	if (it1.mPara == it2.mPara) {
		Paragraph& para = *mParagraphs[it1.mPara];

		para.DeleteRange(it1.mLine, it1.mOffset, it2.mLine, it2.mOffset);

		// fixup iterators
		for(Iterators::iterator itI(mIterators.begin()), itIEnd(mIterators.end()); itI!=itIEnd; ++itI) {
			Iterator& it = **itI;

			if (it >= it1) {
				if (it.mPara == paraIdx2) {
					if (it.mLine > lineIdx2) {
						it.mLine += lineIdx1 - lineIdx2;
					} else if (it.mLine < lineIdx2) {
						it = it1;
					} else if (it.mOffset > offset2) {
						it.mOffset += offset1 - offset2;
					} else {
						it = it1;
					}
				}
			}

			it.Validate();
		}
	} else {
		Paragraph& para1 = *mParagraphs[it1.mPara];
		Paragraph& para2 = *mParagraphs[it2.mPara];

		para1.DeleteToEnd(it1.mLine, it1.mOffset);
		para2.DeleteFromStart(it2.mLine, it2.mOffset);

		para1.Append(para2);

		// erase paragraphs that were fully deleted
		for(int paraIdx = it1.mPara + 1; paraIdx <= it2.mPara; ++paraIdx) {
			delete mParagraphs[paraIdx];
		}

		mParagraphs.erase(mParagraphs.begin() + it1.mPara + 1, mParagraphs.begin() + it2.mPara + 1);

		// fixup iterators
		for(Iterators::iterator itI(mIterators.begin()), itIEnd(mIterators.end()); itI!=itIEnd; ++itI) {
			Iterator& it = **itI;

			if (it >= it1) {		// note that this also excludes it1 itself
				if (it.mPara > paraIdx2) {
					it.mPara += (paraIdx1 - paraIdx2);
				} else if (it.mPara < paraIdx2 || it.mLine < lineIdx2) {
					it = it1;
				} else if (it.mLine > lineIdx2 || it.mOffset < offset2) {
					it.mLine += lineIdx1 - lineIdx2;
					it.mOffset += offset1 - offset2;
					it.mPara = it1.mPara;
				} else {
					it = it1;
				}
			}

			it.Validate();
		}
	}

	ReflowPara(paraIdx1);

	if (paraIdx1 != paraIdx2 || lineIdx1 != lineIdx2)
		RecomputeParaPositions();

	int bottomYNew = mParagraphs[paraIdx1]->GetYBottom();

	if (mpCB) {
		mpCB->VerticalShiftRows(bottomYOld, bottomYNew);
		mpCB->InvalidateRows(topY, bottomYOld);
	}
}

void Document::ReflowPara(int paraIdx) {
	Paragraph& para = *mParagraphs[paraIdx];

	if (mpCB) {
		mpCB->ReflowPara(paraIdx, para);
		mpCB->RecolorParagraph(paraIdx, para);
	}
}

void Document::ReflowPara(int paraIdx, const Line *newLines, size_t count) {
	// fixup iterators
	for(Iterators::iterator it(mIterators.begin()), itEnd(mIterators.end()); it!=itEnd; ++it) {
		Iterator *textit = *it;

		if (textit->mPara == paraIdx) {
			int offset = textit->GetParaOffset();

			const Line *it(std::upper_bound(newLines, newLines + count, offset, LinesByOffsetPred()));

			if (it != newLines)
				--it;

			textit->mLine = (int)(it - newLines);
			textit->mOffset = offset - it->mStart;
		}
	}

	// update lines
	Paragraph::Lines newLines2(newLines, newLines + count);
	Paragraph& para = *mParagraphs[paraIdx];
	para.mLines.swap(newLines2);
	para.Validate();
}

void Document::RecolorPara(int paraIdx) {
	if ((unsigned)paraIdx >= mParagraphs.size())
		return;

	Paragraph& para = *mParagraphs[paraIdx];

	if (mpCB) {
		mpCB->RecolorParagraph(paraIdx, para);
		mpCB->InvalidateRows(para.mYPos, paraIdx+1 >= (int)mParagraphs.size() ? mTotalHeight : mParagraphs[paraIdx+1]->mYPos);
	}
}

void Document::RecomputeParaPositions() {
	int y = 0;

	for(Paragraphs::iterator itP(mParagraphs.begin()), itPEnd(mParagraphs.end()); itP!=itPEnd; ++itP) {
		Paragraph *para = *itP;

		int ht = 0;
		for(Paragraph::Lines::const_iterator itL(para->mLines.begin()), itLEnd(para->mLines.end()); itL!=itLEnd; ++itL) {
			const Line& ln = *itL;
			ht += ln.mHeight;
		}

		para->mYPos = y;
		para->mHeight = ht;

		y += ht;
	}

	if (mTotalHeight != y) {
		mTotalHeight = y;

		if (mpCB)
			mpCB->ChangeTotalHeight(y);
	}
}

}	// namespace nsVDTextDOM
