#include <stdafx.h>
#include <vd2/VDDisplay/rendercache.h>

VDDisplayRenderCacheGeneric::VDDisplayRenderCacheGeneric()
	: mUniquenessCounter(0)
{
}

VDDisplayRenderCacheGeneric::~VDDisplayRenderCacheGeneric() {
}

void *VDDisplayRenderCacheGeneric::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return NULL;
}

bool VDDisplayRenderCacheGeneric::Init(const VDDisplaySubRenderCache& subRenderCache, uint32 w, uint32 h, int format) {
	mBuffer.init(w, h, format);
	mImageView.SetImage(mBuffer, false);

	mUniquenessCounter = subRenderCache.GetUniquenessCounter() - 1;
	return true;
}

void VDDisplayRenderCacheGeneric::Update(const VDDisplaySubRenderCache& subRenderCache) {
	mUniquenessCounter = subRenderCache.GetUniquenessCounter();
}
