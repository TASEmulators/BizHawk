#include <stdafx.h>
#include <vd2/VDDisplay/renderer.h>

VDDisplaySubRenderCache::VDDisplaySubRenderCache()
	: mUniquenessCounter(0)
{
}

VDDisplaySubRenderCache::~VDDisplaySubRenderCache() {
}

///////////////////////////////////////////////////////////////////////////

VDDisplayImageView::VDDisplayImageView()
	: mbDynamic(false)
	, mUniquenessCounter(0)
{
	memset(&mPixmap, 0, sizeof mPixmap);

	for(size_t i=0; i<vdcountof(mCaches); ++i)
		mCaches[i].mId = 0;
}

VDDisplayImageView::~VDDisplayImageView() {
}

void VDDisplayImageView::SetImage() {
	VDPixmap px = {};

	SetImage(px, false);
}

void VDDisplayImageView::SetImage(const VDPixmap& px, bool dynamic) {
	for(size_t i=0; i<vdcountof(mCaches); ++i) {
		mCaches[i].mId = 0;
		mCaches[i].mpCache.clear();
	}

	mPixmap = px;
	mbDynamic = dynamic;
}

void VDDisplayImageView::SetCachedImage(uint32 id, IVDRefUnknown *p) {
	mCaches[0].mpCache.swap(mCaches[1].mpCache);
	mCaches[1].mId = id;
	mCaches[1].mpCache = p;
}

const vdrect32 *VDDisplayImageView::GetDirtyList() const {
	return mDirtyRects.data();
}

uint32 VDDisplayImageView::GetDirtyListSize() const {
	return mDirtyRects.size();
}

void VDDisplayImageView::Invalidate() {
	++mUniquenessCounter;
	mDirtyRects.clear();
}

void VDDisplayImageView::Invalidate(const vdrect32 *rects, uint32 n) {
	if (!n)
		return;

	++mUniquenessCounter;
	mDirtyRects.assign(rects, rects + n);
}
