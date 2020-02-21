#ifndef f_VD2_TESSA_D3D9_FENCEMANAGER_D3D9_H
#define f_VD2_TESSA_D3D9_FENCEMANAGER_D3D9_H

#include <vd2/system/vdstl.h>

struct IDirect3DDevice9;
struct IDirect3DQuery9;

class VDTFenceManagerD3D9 {
	VDTFenceManagerD3D9(const VDTFenceManagerD3D9&);
	VDTFenceManagerD3D9& operator=(const VDTFenceManagerD3D9&);
public:
	VDTFenceManagerD3D9();
	~VDTFenceManagerD3D9();

	void Init(IDirect3DDevice9 *dev);
	void Shutdown();

	void FlushDefaultResources();

	uint32 InsertFence();
	bool CheckFence(uint32 fence);

protected:
	typedef vdfastvector<IDirect3DQuery9 *> IdleQueries;
	typedef vdfastdeque<IDirect3DQuery9 *> ActiveQueries;

	IDirect3DDevice9 *mpD3DDevice;
	uint32 mFirstFenceId;
	uint32 mNextFenceId;
	bool mbEventQueriesSupported;
	ActiveQueries mActiveQueries;
	IdleQueries mIdleQueries;
};

#endif
