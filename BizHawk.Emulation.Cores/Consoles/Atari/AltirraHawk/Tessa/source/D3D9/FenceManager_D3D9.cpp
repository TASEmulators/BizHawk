#include <stdafx.h>
#include <d3d9.h>
#include "D3D9/FenceManager_D3D9.h"

VDTFenceManagerD3D9::VDTFenceManagerD3D9()
: mpD3DDevice(NULL)
{
}

VDTFenceManagerD3D9::~VDTFenceManagerD3D9() {
}

void VDTFenceManagerD3D9::Init(IDirect3DDevice9 *dev) {
	HRESULT hr = dev->CreateQuery(D3DQUERYTYPE_EVENT, NULL);
	mbEventQueriesSupported = SUCCEEDED(hr);

	mpD3DDevice = dev;
	mpD3DDevice->AddRef();

	mFirstFenceId = 1;
	mNextFenceId = 1;
}

void VDTFenceManagerD3D9::Shutdown() {
	FlushDefaultResources();

	if (mpD3DDevice) {
		mpD3DDevice->Release();
		mpD3DDevice = NULL;
	}
}

void VDTFenceManagerD3D9::FlushDefaultResources() {
	while(!mIdleQueries.empty()) {
		IDirect3DQuery9 *q = mIdleQueries.back();
		mIdleQueries.pop_back();

		q->Release();
	}

	while(!mActiveQueries.empty()) {
		IDirect3DQuery9 *q = mActiveQueries.back();
		mActiveQueries.pop_back();

		if (q)
			q->Release();
	}

	mFirstFenceId = mNextFenceId;
}

uint32 VDTFenceManagerD3D9::InsertFence() {
	if (!mbEventQueriesSupported)
		return mNextFenceId++;

	IDirect3DQuery9 *q = NULL;
	HRESULT hr;

	if (mIdleQueries.empty()) {
		hr = mpD3DDevice->CreateQuery(D3DQUERYTYPE_EVENT, &q);
		if (FAILED(hr))
			q = NULL;
	} else {
		q = mIdleQueries.back();
		mIdleQueries.pop_back();
	}

	if (q) {
		hr = q->Issue(D3DISSUE_END);

		if (FAILED(hr)) {
			q->Release();
			q = NULL;
		}
	}

	mActiveQueries.push_back(q);
	return mNextFenceId++;
}

bool VDTFenceManagerD3D9::CheckFence(uint32 fenceId) {
	uint32 distance = mNextFenceId - fenceId;
	if (distance > mActiveQueries.size())
		return true;

	while(!mActiveQueries.empty()) {
		IDirect3DQuery9 *q = mActiveQueries.front();

		if (q) {
			HRESULT hr = q->GetData(NULL, 0, D3DGETDATA_FLUSH);
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
