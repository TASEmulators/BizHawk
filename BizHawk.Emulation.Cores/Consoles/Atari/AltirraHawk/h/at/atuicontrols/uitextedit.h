#ifndef f_AT_UITEXTEDIT_H
#define f_AT_UITEXTEDIT_H

#include <vd2/system/function.h>
#include <vd2/system/time.h>
#include <vd2/system/VDString.h>
#include <vd2/VDDisplay/font.h>
#include <at/atui/uiwidget.h>

class ATUITextEdit : public ATUIWidget, public IVDTimerCallback {
public:
	ATUITextEdit();
	~ATUITextEdit();

	void AutoSize();
	sint32 GetIdealHeight() const;

	void ClearSelection();
	void Delete();

	const wchar_t *GetText() const { return mText.c_str(); }
	void SetText(const wchar_t *s);

	vdfunction<void()>& OnReturnPressed() { return mReturnPressedEvent; }

public:
	virtual void OnMouseDownL(sint32 x, sint32 y);
	virtual void OnMouseUpL(sint32 x, sint32 y);
	virtual void OnMouseMove(sint32 x, sint32 y);

	virtual bool OnKeyDown(const ATUIKeyEvent& event);
	virtual bool OnKeyUp(const ATUIKeyEvent& event);
	virtual bool OnChar(const ATUICharEvent& event);

	virtual void OnCreate();
	virtual void OnDestroy();

	virtual void OnKillFocus();
	virtual void OnSetFocus();

	virtual void TimerCallback();

protected:
	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	sint32 GetNearestPosFromX(sint32 x) const;
	void SetCaretPosX(sint32 x, bool enableSelection);
	void UpdateCaretPixelX();
	void TurnCaretOn();

	sint32 mScrollX;
	sint32 mCaretPosX;
	sint32 mCaretPixelX;
	sint32 mAnchorPosX;
	sint32 mAnchorPixelX;
	sint32 mTextMarginX;
	sint32 mTextMarginY;
	uint32 mTextColor;
	uint32 mHighlightTextColor;
	uint32 mHighlightBackgroundColor;
	bool mbFocused;
	bool mbCaretOn;
	vdrefptr<IVDDisplayFont> mpFont;
	sint32 mFontHeight;
	sint32 mFontAscent;
	VDStringW mText;

	typedef vdfastvector<VDDisplayFontGlyphPlacement> GlyphPlacements;
	GlyphPlacements mGlyphPlacements;
	GlyphPlacements mGlyphPlacements2;

	VDLazyTimer mCaretTimer;

	vdfunction<void()> mReturnPressedEvent;
};

#endif
