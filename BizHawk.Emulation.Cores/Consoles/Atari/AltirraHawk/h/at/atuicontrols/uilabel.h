#ifndef f_AT2_UILABEL_H
#define f_AT2_UILABEL_H

#include <vd2/system/VDString.h>
#include <at/atui/uiwidget.h>

class IVDDisplayFont;

class ATUILabel : public ATUIWidget {
public:
	enum Align {
		kAlignLeft,
		kAlignCenter,
		kAlignRight
	};

	enum VAlign {
		kVAlignTop,
		kVAlignMiddle,
		kVAlignBottom
	};

	ATUILabel();

	void SetFont(IVDDisplayFont *font);
	void SetBoldFont(IVDDisplayFont *font);

	void ClearBorderColor();
	void SetBorderColor(uint32 c);

	void SetTextAlign(Align align);
	void SetTextVAlign(VAlign align);
	void SetTextColor(uint32 c);
	void SetTextOffset(sint32 x, sint32 y);

	void SetText(const wchar_t *s);
	void SetTextF(const wchar_t *format, ...);

	void SetHTMLText(const wchar_t *s);

	void Clear();
	void AppendFormattedText(uint32 color, const wchar_t *s);
	void AppendFormattedTextF(uint32 color, const wchar_t *format, ...);

	void AutoSize() { AutoSize(mArea.left, mArea.top); }
	void AutoSize(int x, int y);

protected:
	virtual void OnCreate();
	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	void Reflow();

	vdrefptr<IVDDisplayFont> mpFont;
	vdrefptr<IVDDisplayFont> mpBoldFont;
	VDStringW mText;
	VDStringW mTextF;
	Align mTextAlign;
	VAlign mTextVAlign;
	uint32 mTextColor;
	sint32 mTextX;
	sint32 mTextY;
	sint32 mBorderColor;
	vdsize32 mTextSize;
	bool mbReflowPending;

	struct Span {
		sint32 mX;
		sint32 mWidth;
		sint32 mY;
		sint32 mFgColor;
		sint32 mBgColor;
		uint32 mStart;
		uint32 mChars;
		bool mbBold;
	};

	vdfastvector<Span> mSpans;

	struct Line {
		uint32 mSpanCount;
		uint32 mAscent;
		uint32 mDescent;
		sint32 mWidth;
	};

	vdfastvector<Line> mLines;
};

#endif
