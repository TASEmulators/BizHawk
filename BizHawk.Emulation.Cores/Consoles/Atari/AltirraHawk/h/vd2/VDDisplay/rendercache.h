#ifndef f_VD2_VDDISPLAY_RENDERCACHE_H
#define f_VD2_VDDISPLAY_RENDERCACHE_H

#include <vd2/system/unknown.h>
#include <vd2/system/vdstl.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/renderer.h>

class VDDisplaySubRenderCache;

class VDDisplayRenderCacheGeneric : public vdrefcounted<IVDRefUnknown>, public vdlist_node {
	VDDisplayRenderCacheGeneric(const VDDisplayRenderCacheGeneric&);
	VDDisplayRenderCacheGeneric& operator=(const VDDisplayRenderCacheGeneric&);
public:
	enum { kTypeID = 'crd ' };

	VDDisplayRenderCacheGeneric();
	~VDDisplayRenderCacheGeneric();

	void *AsInterface(uint32 iid);

	bool Init(const VDDisplaySubRenderCache& subRenderCache, uint32 w, uint32 h, int format);

	void Update(const VDDisplaySubRenderCache& subRenderCache);

public:
	VDPixmapBuffer mBuffer;
	VDDisplayImageView mImageView;

	uint32 mUniquenessCounter;
};

#endif
