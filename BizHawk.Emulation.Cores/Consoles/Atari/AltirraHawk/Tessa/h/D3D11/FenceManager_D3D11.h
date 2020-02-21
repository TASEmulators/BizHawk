#ifndef f_VD2_TESSA_D3D11_FENCEMANAGER_D3D11_H
#define f_VD2_TESSA_D3D11_FENCEMANAGER_D3D11_H

#include <vd2/system/vdstl.h>

struct ID3D11Device;
struct ID3D11Query;

class VDTFenceManagerD3D11 {
	VDTFenceManagerD3D11(const VDTFenceManagerD3D11&);
	VDTFenceManagerD3D11& operator=(const VDTFenceManagerD3D11&);
public:
	VDTFenceManagerD3D11();
	~VDTFenceManagerD3D11();

	void Init(ID3D11Device *dev, ID3D11DeviceContext *devctx);
	void Shutdown();

	void FlushDefaultResources();

	uint32 InsertFence();
	bool CheckFence(uint32 fence);

protected:
	typedef vdfastvector<ID3D11Query *> IdleQueries;
	typedef vdfastdeque<ID3D11Query *> ActiveQueries;

	ID3D11Device *mpD3DDevice;
	ID3D11DeviceContext *mpD3DDeviceContext;
	uint32 mFirstFenceId;
	uint32 mNextFenceId;
	ActiveQueries mActiveQueries;
	IdleQueries mIdleQueries;
};

#endif
