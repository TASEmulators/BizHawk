#ifndef f_VD2_VDDISPLAY_DISPLAYDRV3D_H
#define f_VD2_VDDISPLAY_DISPLAYDRV3D_H

#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <vd2/VDDisplay/display.h>
#include <vd2/VDDisplay/displaydrv.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/Tessa/Context.h>
#include "displaynode3d.h"
#include "renderer3d.h"

struct VDPixmap;
class IVDTContext;
class IVDTTexture2D;

///////////////////////////////////////////////////////////////////////////

class VDDisplayDriver3D final : public VDVideoDisplayMinidriver, public IVDDisplayCompositionEngine, public IVDTAsyncPresent {
	VDDisplayDriver3D(const VDDisplayDriver3D&) = delete;
	VDDisplayDriver3D& operator=(const VDDisplayDriver3D&) = delete;
public:
	VDDisplayDriver3D();
	~VDDisplayDriver3D();

	virtual bool Init(HWND hwnd, HMONITOR hmonitor, const VDVideoDisplaySourceInfo& info) override;
	virtual void Shutdown() override;

	virtual bool ModifySource(const VDVideoDisplaySourceInfo& info) override;

	virtual void SetFilterMode(FilterMode mode) override;
	virtual void SetFullScreen(bool fullscreen, uint32 w, uint32 h, uint32 refresh, bool use16bit) override;
	virtual void SetDestRect(const vdrect32 *r, uint32 color) override;
	virtual void SetPixelSharpness(float xfactor, float yfactor) override;
	virtual bool SetScreenFX(const VDVideoDisplayScreenFXInfo *screenFX) override;

	virtual bool IsValid() override;
	virtual bool IsFramePending() override;
	virtual bool IsScreenFXSupported() const override;

	virtual bool Resize(int w, int h) override;
	virtual bool Update(UpdateMode) override;
	virtual void Refresh(UpdateMode) override;
	virtual bool Paint(HDC hdc, const RECT& rClient, UpdateMode lastUpdateMode) override;
	virtual void PresentQueued() override;

	virtual bool AreVSyncTicksNeeded() const override { return false; }

	IVDDisplayCompositionEngine *GetDisplayCompositionEngine() override { return this; }

public:
	void LoadCustomEffect(const wchar_t *path) override {}

public:
	virtual void QueuePresent();

private:
	bool CreateSwapChain();
	bool CreateImageNode();
	void DestroyImageNode();
	bool BufferNode(VDDisplayNode3D *srcNode, uint32 w, uint32 h, VDDisplaySourceNode3D **ppNode);
	bool RebuildTree();

	HWND mhwnd;
	HMONITOR mhMonitor;
	IVDTContext *mpContext;
	IVDTSwapChain *mpSwapChain;
	VDDisplayImageNode3D *mpImageNode;
	VDDisplayImageSourceNode3D *mpImageSourceNode;
	VDDisplayNode3D *mpRootNode;

	FilterMode mFilterMode;
	bool mbCompositionTreeDirty;
	bool mbFramePending;

	bool mbUseScreenFX = false;
	VDVideoDisplayScreenFXInfo mScreenFXInfo {};

	bool mbFullScreen;
	uint32 mFullScreenWidth;
	uint32 mFullScreenHeight;
	uint32 mFullScreenRefreshRate;

	VDVideoDisplaySourceInfo mSource;

	VDDisplayNodeContext3D mDisplayNodeContext;
	VDDisplayRenderer3D mRenderer;
};

#endif
