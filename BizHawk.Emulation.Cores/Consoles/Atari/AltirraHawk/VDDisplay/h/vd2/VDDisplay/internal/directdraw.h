#ifndef f_VD2_VDDISPLAY_INTERNAL_DIRECTDRAW_H
#define f_VD2_VDDISPLAY_INTERNAL_DIRECTDRAW_H

class IVDDirectDrawClient {
public:
	virtual void DirectDrawShutdown() = 0;
	virtual void DirectDrawPrimaryRestored() = 0;
};

class IVDDirectDrawManager {
public:
	virtual bool Init(IVDDirectDrawClient *pClient) = 0;
	virtual bool Shutdown(IVDDirectDrawClient *pClient) = 0;
	virtual IDirectDraw2 *GetDDraw() = 0;
	virtual const DDCAPS& GetCaps() = 0;
	virtual IDirectDrawSurface2 *GetPrimary() = 0;
	virtual const DDSURFACEDESC& GetPrimaryDesc() = 0;
	virtual HMONITOR GetMonitor() = 0;
	virtual const vdrect32& GetMonitorRect() = 0;
	virtual bool Restore() = 0;
};

#endif
