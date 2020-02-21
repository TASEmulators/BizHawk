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
#include <windows.h>
#include <unknwn.h>
#include <mmsystem.h>
#include <vd2/system/math.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/w32assist.h>
#include <vd2/Riza/audioout.h>

struct IXAudio28;
struct IXAudio2XEngineCallback;
struct IXAudio2XVoiceCallback;
struct IXAudio27MasteringVoice;
struct IXAudio28MasteringVoice;
struct IXAudio2XSourceVoice;
struct IXAudio27SubmixVoice;
struct IXAudio28SubmixVoice;
struct XAUDIO2X_PERFORMANCE_DATA;

struct XAUDIO2X_DEBUG_CONFIGURATION {
	UINT32 TraceMask;
	UINT32 BreakMask;
	BOOL LogThreadID;
	BOOL LogFileline;
	BOOL LogFunctionName;
	BOOL LogTiming;
};

struct XAUDIO2X_VOICE_SENDS;
struct XAUDIO2X_EFFECT_CHAIN;

#pragma pack(push, 4)
struct XAUDIO2X_VOICE_STATE {
	void *pCurrentBufferContext;
	UINT32 BuffersQueued;
	UINT64 SamplesPlayed;		// !! - unaligned on x64!
};

struct XAUDIO2X_BUFFER {
	UINT32 Flags;
	UINT32 AudioBytes;
	const BYTE *pAudioData;
	UINT32 PlayBegin;
	UINT32 PlayLength;
	UINT32 LoopBegin;
	UINT32 LoopLength;
	UINT32 LoopCount;
	void *pContext;				// !! - unaligned on x64!
};
#pragma pack(pop)

struct XAUDIO2X_BUFFER_WMA;
struct XAUDIO2X_FILTER_PARAMETERS;
struct XAUDIO2X_VOICE_DETAILS;

// veneer of AUDIO_STREAM_CATEGORY (would require Audioclient.h from Win8+ SDK)
enum XAUDIO28_STREAM_CATEGORY : UINT32 {
	XAUDIO28_STREAM_CATEGORY_GAMEEFFECTS = 6
};

struct XAUDIO27_DEVICE_DETAILS;

enum XAUDIO2X_PROCESSOR : UINT32 {
	XAUDIO2X_ANY_PROCESSOR = 0xFFFFFFFF
};

enum : UINT32 {
	XAUDIO27_DEBUG_ENGINE = 1,
	XAUDIO2X_LOG_ERRORS = 1,
	XAUDIO2X_COMMIT_NOW = 0,
};

enum : UINT32 {
	XAUDIO28_VOICE_NOSAMPLESPLAYED = 0x100
};

struct __declspec(uuid("5a508685-a254-4fba-9b82-9a24b00306af")) XAudio27;
struct __declspec(uuid("db05ea35-0329-4d4b-a53a-6dead03d3852")) XAudio27_Debug;

typedef HRESULT WINAPI XAudio28CreateFn(IXAudio28 **ppXAudio2, UINT32 flags, XAUDIO2X_PROCESSOR processor);

struct __declspec(uuid("8bcf1f58-9fe7-4583-8ac6-e2adc465c8bb")) IXAudio27 : public IUnknown {
	virtual HRESULT STDMETHODCALLTYPE GetDeviceCount(UINT32 *count) = 0;
	virtual HRESULT STDMETHODCALLTYPE GetDeviceDetails(UINT32 index, XAUDIO27_DEVICE_DETAILS *details) = 0;
	virtual HRESULT STDMETHODCALLTYPE Initialize(UINT32 flags, XAUDIO2X_PROCESSOR processor) = 0;
	virtual HRESULT STDMETHODCALLTYPE RegisterForCallbacks(IXAudio2XEngineCallback *callback) = 0;
	virtual void STDMETHODCALLTYPE UnregisterForCallbacks(IXAudio2XEngineCallback *callback) = 0;
	virtual HRESULT STDMETHODCALLTYPE CreateSourceVoice(IXAudio2XSourceVoice **voice, const WAVEFORMATEX *sourceFormat, UINT32 flags, float maxFrequencyRatio, IXAudio2XVoiceCallback *callback, const XAUDIO2X_VOICE_SENDS *sendList, const XAUDIO2X_EFFECT_CHAIN *effectChain) = 0;
	virtual HRESULT STDMETHODCALLTYPE CreateSubmixVoice(IXAudio27SubmixVoice **voice, UINT32 inputChannels, UINT32 inputSampleRate, UINT32 flags, UINT32 processingStage, const XAUDIO2X_VOICE_SENDS *sendList, const XAUDIO2X_EFFECT_CHAIN *effectChain) = 0;
	virtual HRESULT STDMETHODCALLTYPE CreateMasteringVoice(IXAudio27MasteringVoice **voice, UINT32 inputChannels, UINT32 inputSampleRate, UINT32 flags, UINT32 deviceIndex, const XAUDIO2X_EFFECT_CHAIN *effectChain) = 0;
	virtual HRESULT STDMETHODCALLTYPE StartEngine() = 0;
	virtual void STDMETHODCALLTYPE StopEngine() = 0;
	virtual HRESULT STDMETHODCALLTYPE CommitChanges(UINT32 OperationSet) = 0;
	virtual void STDMETHODCALLTYPE GetPerformanceData(XAUDIO2X_PERFORMANCE_DATA *perfData) = 0;
	virtual void STDMETHODCALLTYPE SetDebugConfiguration(XAUDIO2X_DEBUG_CONFIGURATION *debugConfiguration, void *reserved) = 0;
};

struct IXAudio28 : public IUnknown {
	virtual HRESULT STDMETHODCALLTYPE RegisterForCallbacks(IXAudio2XEngineCallback *callback) = 0;
	virtual void STDMETHODCALLTYPE UnregisterForCallbacks(IXAudio2XEngineCallback *callback) = 0;
	virtual HRESULT STDMETHODCALLTYPE CreateSourceVoice(IXAudio2XSourceVoice **voice, const WAVEFORMATEX *sourceFormat, UINT32 flags, float maxFrequencyRatio, IXAudio2XVoiceCallback *callback, const XAUDIO2X_VOICE_SENDS *sendList, const XAUDIO2X_EFFECT_CHAIN *effectChain) = 0;
	virtual HRESULT STDMETHODCALLTYPE CreateSubmixVoice(IXAudio28SubmixVoice **voice, UINT32 inputChannels, UINT32 inputSampleRate, UINT32 flags, UINT32 processingStage, const XAUDIO2X_VOICE_SENDS *sendList, const XAUDIO2X_EFFECT_CHAIN *effectChain) = 0;
	virtual HRESULT STDMETHODCALLTYPE CreateMasteringVoice(IXAudio28MasteringVoice **voice, UINT32 inputChannels, UINT32 inputSampleRate, UINT32 flags, LPCWSTR deviceId, const XAUDIO2X_EFFECT_CHAIN *effectChain, XAUDIO28_STREAM_CATEGORY streamCategory) = 0;
	virtual HRESULT STDMETHODCALLTYPE StartEngine() = 0;
	virtual void STDMETHODCALLTYPE StopEngine() = 0;
	virtual HRESULT STDMETHODCALLTYPE CommitChanges(UINT32 OperationSet) = 0;
	virtual void STDMETHODCALLTYPE GetPerformanceData(XAUDIO2X_PERFORMANCE_DATA *perfData) = 0;
	virtual void STDMETHODCALLTYPE SetDebugConfiguration(XAUDIO2X_DEBUG_CONFIGURATION *debugConfiguration, void *reserved) = 0;
};

struct IXAudio2XVoice {
	virtual void STDMETHODCALLTYPE GetVoiceDetails(XAUDIO2X_VOICE_DETAILS *voiceDetails) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetOutputVoices(const XAUDIO2X_VOICE_SENDS *sendList) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetEffectChain(const XAUDIO2X_EFFECT_CHAIN *effectChain) = 0;
	virtual void STDMETHODCALLTYPE EnableEffect(UINT32 effectIndex, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE DisableEffect(UINT32 effectIndex, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetEffectState(UINT32 effectIndex, BOOL *enabled) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetEffectParameters(UINT32 effectIndex, const void *parameters, UINT32 parametersByteSize, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetEffectParameters(UINT32 effectIndex, void *parameters, UINT32 parametersByteSize) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetFilterParameters(const XAUDIO2X_FILTER_PARAMETERS *parameters, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetFilterParameters(XAUDIO2X_FILTER_PARAMETERS *parameters) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetOutputFilterParameters(IXAudio2XVoice *destVoice, XAUDIO2X_FILTER_PARAMETERS *parameters, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetOutputFilterParameters(IXAudio2XVoice *destinationVoice, XAUDIO2X_FILTER_PARAMETERS *parameters) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetVolume(float volume, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetVolume(float *volume) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetChannelVolumes(UINT32 channels, const float *volumes, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetChannelVolumes(UINT32 channels, float *volumes) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetOutputMatrix(IXAudio2XVoice *destVoice, UINT32 sourceChannels, UINT32 destChannels, const float *levelMatrix, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetOutputMatrix(IXAudio2XVoice *destVoice, UINT32 sourceChannels, UINT32 destChannels, float *levelMatrix) = 0;
	virtual void STDMETHODCALLTYPE DestroyVoice() = 0;
};

struct IXAudio2XEngineCallback {
	virtual void STDMETHODCALLTYPE OnProcessingPassStart() = 0;
	virtual void STDMETHODCALLTYPE OnProcessingPassEnd() = 0;
	virtual void STDMETHODCALLTYPE OnCriticalError(HRESULT hr) = 0;
};

struct IXAudio2XVoiceCallback {
	virtual void STDMETHODCALLTYPE OnVoiceProcessingPassStart(UINT32 bytesRequired) = 0;
	virtual void STDMETHODCALLTYPE OnVoiceProcessingPassEnd() = 0;
	virtual void STDMETHODCALLTYPE OnStreamEnd() = 0;
	virtual void STDMETHODCALLTYPE OnBufferStart(void *bufferContext) = 0;
	virtual void STDMETHODCALLTYPE OnBufferEnd(void *bufferContext) = 0;
	virtual void STDMETHODCALLTYPE OnLoopEnd(void *bufferContext) = 0;
	virtual void STDMETHODCALLTYPE OnVoiceError(void *bufferContext, HRESULT error) = 0;
};

struct IXAudio27MasteringVoice : public IXAudio2XVoice {
};

struct IXAudio28MasteringVoice : public IXAudio2XVoice {
	virtual HRESULT STDMETHODCALLTYPE GetChannelMask(DWORD *channelMask) = 0;
};

struct IXAudio2XSourceVoice : public IXAudio2XVoice {
	virtual HRESULT STDMETHODCALLTYPE Start(UINT32 flags, UINT32 operationSet = XAUDIO2X_COMMIT_NOW) = 0;
	virtual HRESULT STDMETHODCALLTYPE Stop(UINT32 flags, UINT32 operationSet = XAUDIO2X_COMMIT_NOW) = 0;
	virtual HRESULT STDMETHODCALLTYPE SubmitSourceBuffer(const XAUDIO2X_BUFFER *buffer, const XAUDIO2X_BUFFER_WMA *bufferWMA) = 0;
	virtual HRESULT STDMETHODCALLTYPE FlushSourceBuffers() = 0;
	virtual HRESULT STDMETHODCALLTYPE Discontinuity() = 0;
	virtual HRESULT STDMETHODCALLTYPE ExitLoop(UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetState(XAUDIO2X_VOICE_STATE *voiceState, UINT32 flags) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetFrequencyRatio(float ratio, UINT32 operationSet) = 0;
	virtual void STDMETHODCALLTYPE GetFrequencyRatio(float *ratio) = 0;
	virtual HRESULT STDMETHODCALLTYPE SetSourceSampleRate(UINT32 newSourceSampleRate) = 0;
};

class VDAudioOutputXAudio2W32 final : public IVDAudioOutput, public IXAudio2XVoiceCallback, public IXAudio2XEngineCallback {
public:
	VDAudioOutputXAudio2W32();
	~VDAudioOutputXAudio2W32();

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
	void STDMETHODCALLTYPE OnVoiceProcessingPassStart(UINT32 bytesRequired) override;
	void STDMETHODCALLTYPE OnVoiceProcessingPassEnd() override;
	void STDMETHODCALLTYPE OnStreamEnd() override;
	void STDMETHODCALLTYPE OnBufferStart(void *bufferContext) override;
	void STDMETHODCALLTYPE OnBufferEnd(void *bufferContext) override;
	void STDMETHODCALLTYPE OnLoopEnd(void *bufferContext) override;
	void STDMETHODCALLTYPE OnVoiceError(void *bufferContext, HRESULT error) override;

private:
	void STDMETHODCALLTYPE OnProcessingPassStart() override;
	void STDMETHODCALLTYPE OnProcessingPassEnd() override;
	void STDMETHODCALLTYPE OnCriticalError(HRESULT hr) override;

private:
	void BlockOnSpace(uint32 len);
	bool ReinitXAudio();
	bool InitXAudio();
	bool InitXAudio2();
	void ShutdownXAudio();

	HMODULE	mhmodXAudioDLL27 = nullptr;
	HMODULE	mhmodXAudioDLL28 = nullptr;
	IXAudio27 *mpXAudio27 = nullptr;
	IXAudio28 *mpXAudio28 = nullptr;
	IXAudio27MasteringVoice *mpMasteringVoice27 = nullptr;
	IXAudio28MasteringVoice *mpMasteringVoice28 = nullptr;
	IXAudio2XSourceVoice *mpSourceVoice = nullptr;
	bool mbRegisteredForCallbacks = false;
	bool mbEngineStarted = false;

	vdstructex<WAVEFORMATEX> mFormat;
	vdblock<uint8> mBuffer;

	uint32 mBufferSize = 0;
	VDAtomicInt mBufferPending = 0;
	VDAtomicInt mBufferFree = 0;
	uint32 mSampleSize = 0;
	uint32 mSamplingRate = 0;
	uint32 mWriteOffset = 0;
	VDAtomicBool mbUnderflowDetected = false;
	VDAtomicBool mbCriticalErrorDetected = false;

	// voice thread
	uint32 mReadOffset = 0;
	uint32 mSubmitOffset = 0;
};

IVDAudioOutput *VDCreateAudioOutputXAudio2W32() {
	return new VDAudioOutputXAudio2W32;
}

VDAudioOutputXAudio2W32::VDAudioOutputXAudio2W32() {
}

VDAudioOutputXAudio2W32::~VDAudioOutputXAudio2W32() {
	Shutdown();
}

uint32 VDAudioOutputXAudio2W32::GetPreferredSamplingRate(const wchar_t *preferredDevice) const {
	// borrow the DirectSound implementation for now... nothing XAudio2 specific here
	vdautoptr<IVDAudioOutput> p { VDCreateAudioOutputDirectSoundW32() };
	return p->GetPreferredSamplingRate(preferredDevice);
}

bool VDAudioOutputXAudio2W32::Init(uint32 bufsize, uint32 bufcount, const WAVEFORMATEX *wf, const wchar_t *preferredDevice) {
	mFormat.assign(wf, sizeof(WAVEFORMATEX) + wf->cbSize);

	mBufferSize = bufsize * bufcount;
	mBuffer.resize(mBufferSize);
	mSampleSize = wf->nBlockAlign;
	mSamplingRate = wf->nSamplesPerSec;

	if (!mhmodXAudioDLL27 && !mhmodXAudioDLL28) {
		// try to load XAudio 2.8 if we are on Windows 8 or later
		if (VDIsAtLeast8W32())
			mhmodXAudioDLL28 = VDLoadSystemLibraryW32("xaudio2_8.dll");

		if (!mhmodXAudioDLL28) {
			// try to load XAudio 2.7 (June 2010 DirectX SDK)
			mhmodXAudioDLL27 = VDLoadSystemLibraryW32("xaudio2_7.dll");

			if (!mhmodXAudioDLL27)
				return false;
		}
	}

	return InitXAudio();
}

void VDAudioOutputXAudio2W32::Shutdown() {
	ShutdownXAudio();

	if (mhmodXAudioDLL28) {
		FreeLibrary(mhmodXAudioDLL28);
		mhmodXAudioDLL28 = nullptr;
	}

	if (mhmodXAudioDLL27) {
		FreeLibrary(mhmodXAudioDLL27);
		mhmodXAudioDLL27 = nullptr;
	}
}

void VDAudioOutputXAudio2W32::GoSilent() {
}

bool VDAudioOutputXAudio2W32::IsSilent() {
	return false;
}

uint32 VDAudioOutputXAudio2W32::GetMixingRate() const {
	return mSamplingRate;
}

bool VDAudioOutputXAudio2W32::Start() {
	return true;
}

bool VDAudioOutputXAudio2W32::Stop() {
	return false;
}

uint32 VDAudioOutputXAudio2W32::GetAvailSpace() {
	return mBufferFree;
}

uint32 VDAudioOutputXAudio2W32::GetBufferLevel() {
	return mBufferSize - mBufferFree;
}

uint32 VDAudioOutputXAudio2W32::EstimateHWBufferLevel(bool *underflowDetected) {
	if (mbCriticalErrorDetected)
		ReinitXAudio();

	if (underflowDetected)
		*underflowDetected = mbUnderflowDetected;

	return mBufferSize - mBufferFree;
}

bool VDAudioOutputXAudio2W32::Write(const void *data, uint32 len) {
	if (mbCriticalErrorDetected)
		ReinitXAudio();

	while(len) {
		uint32 tc = mBufferSize - mWriteOffset;

		if (tc > len)
			tc = len;

		uint32 avail = mBufferFree;
		if (tc > avail) {
			if (!avail) {
				BlockOnSpace(len);
				continue;
			}

			tc = avail;
		}
		
		len -= tc;

		if (data) {
			memcpy(mBuffer.data() + mWriteOffset, data, tc);
			data = (const char *)data + tc;
		} else
			memset(mBuffer.data() + mWriteOffset, 0, tc);

		mWriteOffset += tc;
		mBufferFree -= tc;
		mBufferPending += tc;

		if (mWriteOffset >= mBufferSize)
			mWriteOffset = 0;
	}

	return true;
}

bool VDAudioOutputXAudio2W32::Flush() {
	return true;
}

bool VDAudioOutputXAudio2W32::Finalize(uint32 timeout) {
	return true;
}

sint32 VDAudioOutputXAudio2W32::GetPosition() {
	return -1;
}

sint32 VDAudioOutputXAudio2W32::GetPositionBytes() {
	return -1;
}

double VDAudioOutputXAudio2W32::GetPositionTime() {
	return -1;
}

bool VDAudioOutputXAudio2W32::IsFrozen() {
	return mbUnderflowDetected;
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnVoiceProcessingPassStart(UINT32 bytesRequired) {
	if (!bytesRequired) {
		mbUnderflowDetected = false;
		return;
	}

	uint32 bytesToSubmit = mBufferPending;

	mbUnderflowDetected = (bytesRequired > bytesToSubmit);

	if (bytesToSubmit > bytesRequired)
		bytesToSubmit = bytesRequired;

	mBufferPending -= bytesToSubmit;

	while(bytesToSubmit) {
		uint32 submitLen = std::min<uint32>(bytesToSubmit, mBufferSize - mSubmitOffset);
		bytesToSubmit -= submitLen;

		XAUDIO2X_BUFFER buf {};

		buf.AudioBytes = mBuffer.size();
		buf.pAudioData = mBuffer.data();
		buf.PlayBegin = mSubmitOffset / mSampleSize;
		buf.PlayLength = submitLen / mSampleSize;

		mSubmitOffset += submitLen;
		if (mSubmitOffset >= mBufferSize)
			mSubmitOffset = 0;

		buf.pContext = mBuffer.data() + mSubmitOffset;
		mpSourceVoice->SubmitSourceBuffer(&buf, nullptr);
	}
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnVoiceProcessingPassEnd() {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnStreamEnd() {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnBufferStart(void *bufferContext) {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnBufferEnd(void *bufferContext) {
	const uint32 playOffset = (uint8 *)bufferContext - mBuffer.data();
	uint32 playDelta = playOffset - mReadOffset;

	if (playDelta) {
		if (playOffset < mReadOffset)
			playDelta += mBufferSize;

		mBufferFree += playDelta;
		VDASSERT((uint32)mBufferFree <= mBufferSize);
		mReadOffset = playOffset;
	}
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnLoopEnd(void *bufferContext) {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnVoiceError(void *bufferContext, HRESULT error) {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnProcessingPassStart() {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnProcessingPassEnd() {
}

void STDMETHODCALLTYPE VDAudioOutputXAudio2W32::OnCriticalError(HRESULT hr) {
	mbCriticalErrorDetected = true;
}

void VDAudioOutputXAudio2W32::BlockOnSpace(uint32 len) {
	uint32 minFree = len >= mBufferSize ? mBufferSize : len;

	while((uint32)mBufferFree < minFree) {
		Sleep(1);

		if (mbCriticalErrorDetected)
			ReinitXAudio();
	}
}

bool VDAudioOutputXAudio2W32::ReinitXAudio() {
	ShutdownXAudio();
	return InitXAudio();
}

bool VDAudioOutputXAudio2W32::InitXAudio() {
	if (!InitXAudio2()) {
		ShutdownXAudio();
		return false;
	}

	return true;
}

bool VDAudioOutputXAudio2W32::InitXAudio2() {
	HRESULT hr;

	mBufferFree = mBufferSize;

	if (mhmodXAudioDLL28) {
		// XAudio 2.8 init
		auto createFn = (XAudio28CreateFn *)GetProcAddress(mhmodXAudioDLL28, "XAudio2Create");
		if (!createFn)
			return false;

		hr = createFn(&mpXAudio28, 0, XAUDIO2X_ANY_PROCESSOR);
		if (FAILED(hr))
			return false;

#ifdef _DEBUG
		XAUDIO2X_DEBUG_CONFIGURATION conf {};
		conf.TraceMask = XAUDIO2X_LOG_ERRORS;
		mpXAudio28->SetDebugConfiguration(&conf, nullptr);
#endif

		hr = mpXAudio28->RegisterForCallbacks(this);
		if (FAILED(hr))
			return false;

		mbRegisteredForCallbacks = true;

		hr = mpXAudio28->CreateMasteringVoice(&mpMasteringVoice28, mFormat->nChannels, mFormat->nSamplesPerSec, 0, nullptr, nullptr, XAUDIO28_STREAM_CATEGORY_GAMEEFFECTS);
		if (FAILED(hr))
			return false;

		hr = mpXAudio28->CreateSourceVoice(&mpSourceVoice, mFormat.data(), 0, 1.0f, this, nullptr, nullptr);
		if (FAILED(hr))
			return false;
	} else {
		// XAudio 2.7 init
		auto gcoFn = (HRESULT (__stdcall *)(REFCLSID, REFIID, LPVOID *))GetProcAddress(mhmodXAudioDLL27, "DllGetClassObject");
		if (!gcoFn)
			return false;

		IClassFactory *factory = nullptr;
		hr = gcoFn(__uuidof(XAudio27), IID_IClassFactory, (void **)&factory);
		if (FAILED(hr))
			return false;

		hr = factory->CreateInstance(nullptr, __uuidof(IXAudio27), (void **)&mpXAudio27);
		vdsaferelease <<= factory;
		
		if (FAILED(hr))
			return false;

		hr = mpXAudio27->Initialize(0, XAUDIO2X_ANY_PROCESSOR);
		if (FAILED(hr))
			return false;

		hr = mpXAudio27->RegisterForCallbacks(this);
		if (FAILED(hr))
			return false;

		mbRegisteredForCallbacks = true;

		hr = mpXAudio27->CreateMasteringVoice(&mpMasteringVoice27, mFormat->nChannels, mFormat->nSamplesPerSec, 0, 0, nullptr);
		if (FAILED(hr))
			return false;

		hr = mpXAudio27->CreateSourceVoice(&mpSourceVoice, mFormat.data(), 0, 1.0f, this, nullptr, nullptr);
		if (FAILED(hr))
			return false;
	}

	hr = mpSourceVoice->Start(0);
	if (FAILED(hr))
		return false;

	mbCriticalErrorDetected = false;
	if (mpXAudio28)
		hr = mpXAudio28->StartEngine();
	else
		hr = mpXAudio27->StartEngine();

	if (FAILED(hr))
		return false;

	mbEngineStarted = true;
	return true;
}

void VDAudioOutputXAudio2W32::ShutdownXAudio() {
	if (mpSourceVoice) {
		mpSourceVoice->Stop(0);
		mpSourceVoice->DestroyVoice();
		mpSourceVoice = nullptr;
	}

	if (mpMasteringVoice28) {
		mpMasteringVoice28->DestroyVoice();
		mpMasteringVoice28 = nullptr;
	}

	if (mpMasteringVoice27) {
		mpMasteringVoice27->DestroyVoice();
		mpMasteringVoice27 = nullptr;
	}

	if (mpXAudio28) {
		if (mbEngineStarted) {
			mbEngineStarted = false;
			mpXAudio28->StopEngine();
		}

		if (mbRegisteredForCallbacks) {
			mbRegisteredForCallbacks = false;
			mpXAudio28->UnregisterForCallbacks(this);
		}

		mpXAudio28->Release();
		mpXAudio28 = nullptr;
	}

	if (mpXAudio27) {
		if (mbEngineStarted) {
			mbEngineStarted = false;
			mpXAudio27->StopEngine();
		}

		if (mbRegisteredForCallbacks) {
			mbRegisteredForCallbacks = false;
			mpXAudio27->UnregisterForCallbacks(this);
		}

		mpXAudio27->Release();
		mpXAudio27 = nullptr;
	}
}
