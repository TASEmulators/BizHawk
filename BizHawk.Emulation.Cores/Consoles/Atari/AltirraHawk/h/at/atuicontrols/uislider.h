#ifndef f_AT_UISLIDER_H
#define f_AT_UISLIDER_H

#include <vd2/system/function.h>
#include <at/atui/uicontainer.h>

class ATUIButton;

class ATUISlider final : public ATUIContainer {
public:
	enum {
		kActionPagePrior = kActionCustom,
		kActionPageNext,
		kActionLinePrior,
		kActionLineNext
	};

	ATUISlider();
	~ATUISlider();

	void SetFrameEnabled(bool enabled);
	void SetVertical(bool vert);
	void SetPos(sint32 pos);
	void SetPageSize(sint32 pageSize);
	void SetLineSize(sint32 lineSize) { mLineSize = lineSize; }
	void SetRange(sint32 minVal, sint32 maxVal);

	void SetOnValueChanged(const vdfunction<void(sint32)>& fn) { mpValueChangedFn = fn; }

public:
	virtual void OnCreate();
	virtual void OnDestroy();
	virtual void OnSize();
	virtual void OnMouseDownL(sint32 x, sint32 y);
	virtual void OnMouseMove(sint32 x, sint32 y);
	virtual void OnMouseUpL(sint32 x, sint32 y);

	virtual void OnActionStart(uint32 trid);
	virtual void OnActionRepeat(uint32 trid);

	virtual void OnCaptureLost();

protected:
	virtual void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h);

	void OnButtonLowerPressed();
	void OnButtonRaisePressed();
	void OnButtonReleased();

	void SetPosInternal(sint32 pos, bool notify);

	sint32 mMin;
	sint32 mMax;
	sint32 mPageSize;
	sint32 mLineSize;
	sint32 mPos;
	float mFloatPos;
	sint32 mPixelPos;

	sint32 mThumbSize;
	sint32 mTrackMin;
	sint32 mTrackSize;

	bool mbFrameEnabled;
	bool mbVertical;
	bool mbDragging;
	sint32 mDragOffset;

	ATUIButton *mpButtonLower;
	ATUIButton *mpButtonRaise;

	vdfunction<void(sint32)> mpValueChangedFn;
};

#endif
