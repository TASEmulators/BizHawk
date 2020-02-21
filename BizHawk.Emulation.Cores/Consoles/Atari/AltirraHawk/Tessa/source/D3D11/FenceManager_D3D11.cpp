#include <stdafx.h>
#include <vd2/system/refcount.h>
#include <d3d11.h>
#include "D3D11/FenceManager_D3D11.h"

VDTFenceManagerD3D11::VDTFenceManagerD3D11()
	: mpD3DDevice(NULL)
	, mpD3DDeviceContext(NULL)
{
}

VDTFenceManagerD3D11::~VDTFenceManagerD3D11() {
}

void VDTFenceManagerD3D11::Init(ID3D11Device *dev, ID3D11DeviceContext *devctx) {
	mpD3DDevice = dev;
	mpD3DDevice->AddRef();
	mpD3DDeviceContext = devctx;
	mpD3DDeviceContext ->AddRef();

	mFirstFenceId = 1;
	mNextFenceId = 1;
}

void VDTFenceManagerD3D11::Shutdown() {
	FlushDefaultResources();

	vdsaferelease <<= mpD3DDeviceContext, mpD3DDevice;
}

void VDTFenceManagerD3D11::FlushDefaultResources() {
	while(!mIdleQueries.empty()) {
		ID3D11Query *q = mIdleQueries.back();
		mIdleQueries.pop_back();

		q->Release();
	}

	while(!mActiveQueries.empty()) {
		ID3D11Query *q = mActiveQueries.back();
		mActiveQueries.pop_back();

		if (q)
			q->Release();
	}

	mFirstFenceId = mNextFenceId;
}

uint32 VDTFenceManagerD3D11::InsertFence() {
	ID3D11Query *q = NULL;
	HRESULT hr;

	if (mIdleQueries.empty()) {
		D3D11_QUERY_DESC desc;
		desc.Query = D3D11_QUERY_EVENT;
		desc.MiscFlags = 0;
		hr = mpD3DDevice->CreateQuery(&desc, &q);
		if (FAILED(hr))
			q = NULL;
	} else {
		q = mIdleQueries.back();
		mIdleQueries.pop_back();
	}

	if (q)
		mpD3DDeviceContext->End(q);

	mActiveQueries.push_back(q);
	return mNextFenceId++;
}

bool VDTFenceManagerD3D11::CheckFence(uint32 fenceId) {
	uint32 distance = mNextFenceId - fenceId;
	if (distance > mActiveQueries.size())
		return true;

	while(!mActiveQueries.empty()) {
		ID3D11Query *q = mActiveQueries.front();

		if (q) {
			BOOL data = FALSE;
			HRESULT hr = mpD3DDeviceContext->GetData(q, &data, sizeof data, 0);
			if (hr == S_FALSE)
				break;
		}

		if (q)
			mIdleQueries.push_back(q);

		mActiveQueries.pop_front();
		++mFirstFenceId;
	}

	return distance > mActiveQueries.size();
}
