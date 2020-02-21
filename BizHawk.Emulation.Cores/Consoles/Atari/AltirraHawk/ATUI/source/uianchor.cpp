#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/refcount.h>
#include <at/atui/uianchor.h>

class ATUITranslationAnchor : public vdrefcounted<IATUIAnchor> {
public:
	ATUITranslationAnchor(float fx, float fy) : mFractionX(fx), mFractionY(fy) {}

	virtual void *AsInterface(uint32 typeId) { return NULL; }

	virtual vdrect32 Position(const vdrect32& containerArea, const vdsize32& size);

protected:
	const float mFractionX;
	const float mFractionY;
};

vdrect32 ATUITranslationAnchor::Position(const vdrect32& containerArea, const vdsize32& size) {
	const sint32 dx = containerArea.width() - size.w;
	const sint32 dy = containerArea.height() - size.h;

	vdrect32 r(containerArea);

	if (dx > 0) {
		r.left = VDRoundToInt32(dx * mFractionX);
		r.right = r.left + size.w;
	}

	if (dy > 0) {
		r.top = VDRoundToInt32(dy * mFractionY);
		r.bottom = r.top + size.h;
	}

	return r;
}

void ATUICreateTranslationAnchor(float fractionX, float fractionY, IATUIAnchor **anchor) {
	IATUIAnchor *p = new ATUITranslationAnchor(fractionX, fractionY);

	p->AddRef();
	*anchor = p;
}

class ATUIProportionAnchor : public vdrefcounted<IATUIAnchor> {
public:
	ATUIProportionAnchor(const vdrect32f& area) : mArea(area) {}

	void *AsInterface(uint32 typeId) override { return NULL; }

	vdrect32 Position(const vdrect32& containerArea, const vdsize32& size) override;

protected:
	vdrect32f mArea;
};

vdrect32 ATUIProportionAnchor::Position(const vdrect32& containerArea, const vdsize32& size) {
	const float w = (float)containerArea.width();
	const float h = (float)containerArea.height();

	vdrect32 r;
	r.left = VDRoundToInt32(w * mArea.left);
	r.top = VDRoundToInt32(h * mArea.top);
	r.right = VDRoundToInt32(w * mArea.right);
	r.bottom = VDRoundToInt32(h * mArea.bottom);

	return r;
}

void ATUICreateProportionAnchor(const vdrect32f& area, IATUIAnchor **anchor) {
	IATUIAnchor *p = new ATUIProportionAnchor(area);

	p->AddRef();
	*anchor = p;
}
