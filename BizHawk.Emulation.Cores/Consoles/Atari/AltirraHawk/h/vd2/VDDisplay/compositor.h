#ifndef f_VD2_VDDISPLAY_COMPOSITOR_H
#define f_VD2_VDDISPLAY_COMPOSITOR_H

#include <vd2/system/refcount.h>

class IVDDisplayRenderer;

struct VDDisplayCompositeInfo {
	uint32 mWidth;
	uint32 mHeight;
};

class VDINTERFACE IVDDisplayCompositionEngine {
public:
	virtual void LoadCustomEffect(const wchar_t *path) = 0;
};

class VDINTERFACE IVDDisplayCompositor : public IVDRefCount {
public:
	virtual void AttachCompositor(IVDDisplayCompositionEngine& r) = 0;
	virtual void DetachCompositor() = 0;

	virtual void PreComposite(const VDDisplayCompositeInfo& compInfo) = 0;
	virtual void Composite(IVDDisplayRenderer& r, const VDDisplayCompositeInfo& compInfo) = 0;
};

#endif
