//	VirtualDub - Video processing and capture application
//	Copyright (C) 1998-2001 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

struct IUnknown;

#pragma warning(push, 0)
#include <windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>
#pragma warning(pop)

#include <vd2/system/math.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/w32assist.h>
#include <vd2/Riza/audioout.h>

class VDAudioOutputWASAPIW32 final : public IVDAudioOutput {
public:
	VDAudioOutputWASAPIW32();
	~VDAudioOutputWASAPIW32();

	uint32	GetPreferredSamplingRate(const wchar_t *preferredDevice) const override;

	bool	Init(uint32 bufsize, uint32 bufcount, const WAVEFORMATEX *wf, const wchar_t *preferredDevice) override;
	void	Shutdown() override;
	void	GoSilent() override;

	bool	IsSilent() override;
	bool	IsFrozen() override;
	uint32	GetAvailSpace() override;
	uint32	GetBufferLevel() override;
	uint32	EstimateHWBufferLevel(bool *underflowDetected) override;
	sint32	GetPosition() override;
	sint32	GetPositionBytes() override;
	double	GetPositionTime() override;

	uint32	GetMixingRate() const override;

	bool	Start() override;
	bool	Stop() override;
	bool	Flush() override;

	bool	Write(const void *data, uint32 len) override;
	bool	Finalize(uint32 timeout) override;

private:
	bool	InitAudioClient();
	bool	InitAudioClient2();
	void	ShutdownAudioClient();
	bool	ReinitAudioClient(HRESULT hr);

	REFERENCE_TIME	mBufferDuration = 0;
	uint32 mBufferSampleCount = 0;
	uint32 mBufferSize = 0;
	uint32 mSampleSize = 0;
	uint32 mMixingRate = 0;
	vdstructex<WAVEFORMATEX> mAudioFormat;
	vdrefptr<IMMDeviceEnumerator> mpDeviceEnum;
	vdrefptr<IMMDevice> mpDevice;
	vdrefptr<IAudioClient> mpAudioClient;
	vdrefptr<IAudioRenderClient> mpAudioRenderClient;
};

IVDAudioOutput *VDCreateAudioOutputWASAPIW32() {
	return new VDAudioOutputWASAPIW32;
}

VDAudioOutputWASAPIW32::VDAudioOutputWASAPIW32() {
}

VDAudioOutputWASAPIW32::~VDAudioOutputWASAPIW32() {
	Shutdown();
}

uint32 VDAudioOutputWASAPIW32::GetPreferredSamplingRate(const wchar_t *preferredDevice) const {
	// borrow the DirectSound implementation for now... nothing XAudio2 specific here
	vdautoptr<IVDAudioOutput> p { VDCreateAudioOutputDirectSoundW32() };
	return p->GetPreferredSamplingRate(preferredDevice);
}

bool VDAudioOutputWASAPIW32::Init(uint32 bufsize, uint32 bufcount, const WAVEFORMATEX *wf, const wchar_t *preferredDevice) {
	// alignment is screwy on WAVEFORMATEX, make sure we only copy what's valid
	static_assert(sizeof(WAVEFORMATEX) == 18);
	mAudioFormat.assign(wf, sizeof(WAVEFORMATEX) + wf->cbSize);

	mSampleSize = wf->nBlockAlign;
	mBufferDuration = (bufsize * bufcount * 10000000ull) / wf->nAvgBytesPerSec;

	// This will get quickly overwritten by the mixing rate, but put something reasonable here
	// in case we fail to init.
	mMixingRate = wf->nSamplesPerSec;

	HRESULT hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, __uuidof(IMMDeviceEnumerator), (void **)~mpDeviceEnum);
	if (FAILED(hr))
		return false;

	return InitAudioClient();
}

void VDAudioOutputWASAPIW32::Shutdown() {
	ShutdownAudioClient();

	mpDeviceEnum = nullptr;
}

void VDAudioOutputWASAPIW32::GoSilent() {
}

bool VDAudioOutputWASAPIW32::IsSilent() {
	return mpAudioClient == nullptr;
}

uint32 VDAudioOutputWASAPIW32::GetMixingRate() const {
	return mMixingRate;
}

bool VDAudioOutputWASAPIW32::Start() {
	return true;
}

bool VDAudioOutputWASAPIW32::Stop() {
	return false;
}

uint32 VDAudioOutputWASAPIW32::GetAvailSpace() {
	return mBufferSize - GetBufferLevel();
}

uint32 VDAudioOutputWASAPIW32::GetBufferLevel() {
	return EstimateHWBufferLevel(nullptr);
}

uint32 VDAudioOutputWASAPIW32::EstimateHWBufferLevel(bool *underflowDetected) {
	if (!mpAudioClient)
		return 0;

	UINT32 paddingFrames = 0;
	HRESULT hr = mpAudioClient->GetCurrentPadding(&paddingFrames);

	if (FAILED(hr)) {
		// try to reinit the audio client... but even if we succeed, the buffer
		// will be empty
		ReinitAudioClient(hr);
		return 0;
	}

	return paddingFrames * mSampleSize;
}

bool VDAudioOutputWASAPIW32::Write(const void *data, uint32 len) {
	if (!mpAudioClient)
		return false;

	while(len) {
		UINT32 paddingFrames = 0;
		HRESULT hr = mpAudioClient->GetCurrentPadding(&paddingFrames);
		if (FAILED(hr)) {
			if (ReinitAudioClient(hr))
				continue;

			return false;
		}

		if (paddingFrames == mBufferSampleCount) {
			::Sleep(1);
			continue;
		}

		const uint32 framesToCopy = std::min<uint32>(len / mSampleSize, mBufferSampleCount - paddingFrames);
		BYTE *buf = nullptr;
		hr = mpAudioRenderClient->GetBuffer(framesToCopy, &buf);
		if (FAILED(hr)) {
			if (ReinitAudioClient(hr))
				continue;

			return false;
		}

		const uint32 bytesToCopy = framesToCopy * mSampleSize;

		if (data) {
			memcpy(buf, data, bytesToCopy);
			mpAudioRenderClient->ReleaseBuffer(framesToCopy, 0);
			data = (const char *)data + bytesToCopy;
		} else {
			mpAudioRenderClient->ReleaseBuffer(framesToCopy, AUDCLNT_BUFFERFLAGS_SILENT);
		}

		len -= bytesToCopy;
	}

	return true;
}

bool VDAudioOutputWASAPIW32::Flush() {
	return true;
}

bool VDAudioOutputWASAPIW32::Finalize(uint32 timeout) {
	return true;
}

sint32 VDAudioOutputWASAPIW32::GetPosition() {
	return -1;
}

sint32 VDAudioOutputWASAPIW32::GetPositionBytes() {
	return -1;
}

double VDAudioOutputWASAPIW32::GetPositionTime() {
	return -1;
}

bool VDAudioOutputWASAPIW32::IsFrozen() {
	return EstimateHWBufferLevel(nullptr) == 0;
}

bool VDAudioOutputWASAPIW32::InitAudioClient() {
	if (InitAudioClient2())
		return true;

	ShutdownAudioClient();
	return false;
}

bool VDAudioOutputWASAPIW32::InitAudioClient2() {
	vdrefptr<IMMDevice> dev;
	HRESULT hr = mpDeviceEnum->GetDefaultAudioEndpoint(eRender, eMultimedia, ~dev);
	if (FAILED(hr))
		return false;

	vdrefptr<IAudioClient> audioClient;
	hr = dev->Activate(__uuidof(IAudioClient), CLSCTX_ALL, nullptr, (void **)~mpAudioClient);
	if (FAILED(hr))
		return false;

	WAVEFORMATEX *mixFormat = nullptr;
	hr = mpAudioClient->GetMixFormat(&mixFormat);
	if (FAILED(hr))
		return false;

	mMixingRate = mixFormat->nSamplesPerSec;
	CoTaskMemFree(mixFormat);
	mixFormat = nullptr;

	mAudioFormat->nSamplesPerSec = mMixingRate;
	mAudioFormat->nAvgBytesPerSec = mMixingRate * mSampleSize;

	hr = mpAudioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, 0, mBufferDuration, 0, mAudioFormat.data(), nullptr);
	if (FAILED(hr))
		return false;

	UINT32 bufferFrames = 0;
	mpAudioClient->GetBufferSize(&bufferFrames);

	mBufferSampleCount = bufferFrames;
	mBufferSize = mBufferSampleCount * mSampleSize;

	vdrefptr<IAudioRenderClient> renderClient;
	hr = mpAudioClient->GetService(__uuidof(IAudioRenderClient), (void **)~mpAudioRenderClient);
	if (FAILED(hr))
		return false;

	mpAudioClient->Start();
	return true;
}

void VDAudioOutputWASAPIW32::ShutdownAudioClient() {
	mpAudioRenderClient = nullptr;

	if (mpAudioClient) {
		mpAudioClient->Stop();
		mpAudioClient = nullptr;
	}

	mpDevice = nullptr;
}

bool VDAudioOutputWASAPIW32::ReinitAudioClient(HRESULT hr) {
	// We really need to handle device invalidations as this will occur any
	// time the sound device itself is removed or changed (new preferred
	// format in control panel). Once this occurs, we may get a new device
	// and new mixing format.
	if (hr != AUDCLNT_E_DEVICE_INVALIDATED)
		return false;

	ShutdownAudioClient();
	return InitAudioClient();
}
