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

#ifndef TEXTDOM_H
#define TEXTDOM_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdstl.h>

namespace nsVDTextDOM {
	class Document;

	class Line {
	public:
		int		mStart;
		int		mLength;
		int		mHeight;

		int		GetEnd() const { return mStart + mLength; }
	};

	class Span {
	public:
		int		mStart;
		sint32	mForeColor;
		sint32	mBackColor;

		static Span MakeSpan(int start, sint32 fore, sint32 back) {
			Span sp = { start, fore, back };
			return sp;
		}
	};

	class Paragraph {
	public:
		int		mYPos;
		int		mHeight;

		typedef vdfastvector<Span> Spans;
		Spans	mSpans;

		typedef vdfastvector<Line> Lines;
		Lines	mLines;

		vdfastvector<char> mText;

		int GetYBottom() const { return mYPos + mHeight; }

		int GetSpanIndexFromOffset(int offset) const;
		int GetLineIndexFromOffset(int offset) const;

		void Insert(int line, int offset, const char *s, size_t len);
		void Append(const Paragraph& src);
		void DeleteFromStart(int line, int offset);
		void DeleteToEnd(int line, int offset);
		void DeleteRange(int startLine, int startOffset, int endLine, int endOffset);
		void Split(int line, int offset, Paragraph& dst);

		void Validate();

	protected:
		void AppendSpans(const Paragraph& src);
		void AppendLines(const Paragraph& src);
		void DeleteOffsetRangeSpans(int start, int end);
		void SplitSpans(int offset, Paragraph& dst);
		void SplitLines(int line, int offset, Paragraph& dst);
	};

	class Iterator : public vdlist_node {
	public:
		Iterator();
		Iterator(Document& doc, int para = 0, int line = 0, int offset = 0);
		Iterator(const Iterator&);
		~Iterator();

		operator bool() const { return mpParent != NULL; }

		bool operator==(const Iterator& src) const;
		bool operator!=(const Iterator& src) const;
		bool operator<(const Iterator& src) const;
		bool operator<=(const Iterator& src) const;
		bool operator>(const Iterator& src) const;
		bool operator>=(const Iterator& src) const;

		int GetYPos() const;

		int GetParaOffset() const;

		Iterator& operator=(const Iterator& src);
		void Attach(Document& doc);
		void Detach();
		void Swap(Iterator&);

		void Validate();
		void MoveToParaOffset(int para, int offset);
		void MoveToStart();
		void MoveToEnd();
		void MoveToPrevChar();
		void MoveToNextChar();
		void MoveToLineStart();
		void MoveToLineEnd();
		void MoveToPrevLine();
		void MoveToNextLine();

		int mPara;
		int mLine;
		int mOffset;
		Document *mpParent;
	};

	class IDocumentCallback {
	public:
		virtual void InvalidateRows(int ystart, int yend) = 0;
		virtual void VerticalShiftRows(int ysrc, int ydst) = 0;
		virtual void ReflowPara(int paraIdx, const Paragraph& para) = 0;
		virtual void RecolorParagraph(int paraIdx, Paragraph& para) = 0;
		virtual void ChangeTotalHeight(int y) = 0;
	};

	class Document {
		friend class Iterator;
	public:
		Document();
		~Document();

		void SetCallback(IDocumentCallback *pCB);

		int GetParagraphCount() const { return (int)mParagraphs.size(); }

		const Paragraph *GetParagraph(int para) { return (unsigned)para < mParagraphs.size() ? mParagraphs[para] : NULL; }
		int GetParagraphFromPos(int pos);
		int GetParagraphFromY(int y);

		void GetParagraphText(int paraIdx, vdfastvector<char>& buf);
		void GetText(const Iterator& it1, const Iterator& it2, bool forceCRLF, vdfastvector<char>& buf);

		void Insert(const Iterator& it, const char *text, size_t len, Iterator *after);
		void Delete(const Iterator& it1, const Iterator& it2);

		void ReflowPara(int paraIdx);
		void ReflowPara(int paraIdx, const Line *newLines, size_t cont);
		void RecolorPara(int paraIdx);
		void RecomputeParaPositions();

	protected:
		int mTotalHeight;
		IDocumentCallback *mpCB;

		typedef vdfastvector<Paragraph *> Paragraphs;
		Paragraphs	mParagraphs;

		typedef vdlist<Iterator> Iterators;
		Iterators	mIterators;
	};
}

#endif
