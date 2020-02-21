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

#define INITGUID
struct IUnknown;
#include <windows.h>
#include <mmsystem.h>
#include <cguid.h>
#include <dsound.h>
#include <mmdeviceapi.h>

#pragma warning(push)
#pragma warning(disable: 4091)
#include <audioclient.h>
#pragma warning(pop)

#include <vd2/system/math.h>
#include <vd2/system/refcount.h>
#include <vd2/system/text.h>
#include <vd2/system/thread.h>
#include <vd2/system/VDString.h>
#include <vd2/system/VDRingBuffer.h>
#include <vd2/system/w32assist.h>
#include <vd2/Riza/audioout.h>

// Declare these here since we don't want to require mmddk.h.
#ifndef DRVM_MAPPER_PREFERRED_GET
#define DRVM_MAPPER_PREFERRED_GET 0x2015
#endif

#ifndef DRV_QUERYFUNCTIONINSTANCEID
#define DRV_QUERYFUNCTIONINSTANCEID		(DRV_RESERVED + 17)
#define DRV_QUERYFUNCTIONINSTANCEIDSIZE	(DRV_RESERVED + 18)
#endif

class VDAudioOutputWaveOutW32 final : public IVDAudioOutput {
public:
	VDAudioOutputWaveOutW32();
	~VDAudioOutputWaveOutW32();

	uint32	GetPreferredSamplingRate(const wchar_t *preferredDevice) const override;

	bool	Init(uint32 bufsize, uint32 bufcount, const tWAVEFORMATEX *wf, const wchar_t *preferredDevice) override;
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
	bool	CheckBuffers();
	bool	WaitBuffers(uint32 timeout);

	static UINT FindDevice(const wchar_t *preferredDevice);

	uint32	mBlockHead;
	uint32	mBlockTail;
	uint32	mBlockWriteOffset;
	uint32	mBlocksPending;
	uint32	mBlockSize;
	uint32	mBlockCount;
	uint32	mBytesQueued;
	vdblock<char> mBuffer;
	vdblock<WAVEHDR> mHeaders;

	HWAVEOUT__ *mhWaveOut;
	void *	mhWaveEvent;
	uint32	mSamplesPerSec;
	uint32	mAvgBytesPerSec;
	VDCriticalSection	mcsWaveDevice;

	enum InitState {
		kStateNone		= 0,
		kStateOpened	= 1,
		kStatePlaying	= 2,
		kStateSilent	= 10,
	} mCurState;
};

IVDAudioOutput *VDCreateAudioOutputWaveOutW32() {
	return new VDAudioOutputWaveOutW32;
}

VDAudioOutputWaveOutW32::VDAudioOutputWaveOutW32()
	: mBlockHead(0)
	, mBlockTail(0)
	, mBlockWriteOffset(0)
	, mBlocksPending(0)
	, mBlockSize(0)
	, mBlockCount(0)
	, mBytesQueued(0)
	, mhWaveOut(NULL)
	, mhWaveEvent(NULL)
	, mSamplesPerSec(0)
	, mAvgBytesPerSec(0)
	, mCurState(kStateNone)
{
}

VDAudioOutputWaveOutW32::~VDAudioOutputWaveOutW32() {
	Shutdown();
}

uint32 VDAudioOutputWaveOutW32::GetPreferredSamplingRate(const wchar_t *preferredDevice) const {
	// if we don't have WASAPI, just return don't know
	if (!VDIsAtLeastVistaW32())
		return 0;

	DWORD deviceID = FindDevice(preferredDevice);

	if (deviceID == WAVE_MAPPER) {
		// The wave mapper will just give us a blank ID, so we must look up the actual device.
		DWORD statusFlags;
		waveOutMessage((HWAVEOUT)(UINT_PTR)deviceID, DRVM_MAPPER_PREFERRED_GET, (DWORD_PTR)&deviceID, (DWORD_PTR)&statusFlags);
	}

	// retrieve the endpoint ID for the device
	size_t len = 0;
	if (MMSYSERR_NOERROR != waveOutMessage((HWAVEOUT)(UINT_PTR)deviceID, DRV_QUERYFUNCTIONINSTANCEIDSIZE, (DWORD_PTR)&len, NULL))
		return 0;

	vdblock<WCHAR> buf(len / sizeof(WCHAR) + 2);
	memset(buf.data(), 0, sizeof(buf[0]) * buf.size());

	if (MMSYSERR_NOERROR != waveOutMessage((HWAVEOUT)(UINT_PTR)deviceID, DRV_QUERYFUNCTIONINSTANCEID, (DWORD_PTR)buf.data(), len))
		return 0;

	HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
	if (FAILED(hr))
		return 0;
	
	uint32 samplingRate = 0;

	// enumerate audio endpoints in WASAPI and see if we get a match
	{
		vdrefptr<IMMDeviceEnumerator> devEnum;
		hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_INPROC_SERVER, __uuidof(IMMDeviceEnumerator), (LPVOID *)~devEnum);
		if (SUCCEEDED(hr)) {
			vdrefptr<IMMDevice> dev;
			hr = devEnum->GetDevice(buf.data(), ~dev);
			if (SUCCEEDED(hr)) {
				vdrefptr<IAudioClient> client;
				hr = dev->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, NULL, (void **)~client);
				if (SUCCEEDED(hr)) {
					WAVEFORMATEX *pwfex = nullptr;
					hr = client->GetMixFormat(&pwfex);
					if (pwfex) {
						if (SUCCEEDED(hr))
							samplingRate = pwfex->nSamplesPerSec;

						CoTaskMemFree(pwfex);
					}
				}
			}
		}
	}

	CoUninitialize();

	return samplingRate;
}

bool VDAudioOutputWaveOutW32::Init(uint32 bufsize, uint32 bufcount, const WAVEFORMATEX *wf, const wchar_t *preferredDevice) {
	const UINT deviceID = FindDevice(preferredDevice);

	mBuffer.resize(bufsize * bufcount);
	mBlockHead = 0;
	mBlockTail = 0;
	mBlockWriteOffset = 0;
	mBlocksPending = 0;
	mBlockSize = bufsize;
	mBlockCount = bufcount;
	mBytesQueued = 0;

	mSamplesPerSec = wf->nSamplesPerSec;
	mAvgBytesPerSec = wf->nAvgBytesPerSec;

	if (!mhWaveEvent) {
		mhWaveEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

		if (!mhWaveEvent)
			return false;
	}

	MMRESULT res = waveOutOpen(&mhWaveOut, deviceID, wf, (DWORD_PTR)mhWaveEvent, 0, CALLBACK_EVENT);
	if (MMSYSERR_NOERROR != res) {
		Shutdown();
		return false;
	}

	mCurState = kStateOpened;

	// Hmmm... we can't allocate buffers while the wave device
	// is active...
	mHeaders.resize(bufcount);
	memset(mHeaders.data(), 0, bufcount * sizeof mHeaders[0]);

	for(uint32 i=0; i<bufcount; ++i) {
		WAVEHDR& hdr = mHeaders[i];

		hdr.dwBufferLength	= bufsize;
		hdr.dwBytesRecorded	= 0;
		hdr.dwFlags			= 0;
		hdr.dwLoops			= 0;
		hdr.dwUser			= 0;
		hdr.lpData			= mBuffer.data() + bufsize * i;

		res = waveOutPrepareHeader(mhWaveOut, &hdr, sizeof hdr);
		if (MMSYSERR_NOERROR != res) {
			Shutdown();
			return false;
		}
	}

	waveOutPause(mhWaveOut);
	return true;
}

void VDAudioOutputWaveOutW32::Shutdown() {
	if (mCurState == kStateSilent)
		return;

	Stop();

	if (!mHeaders.empty()) {
		for(int i=mHeaders.size()-1; i>=0; --i) {
			WAVEHDR& hdr = mHeaders[i];

			if (hdr.dwFlags & WHDR_PREPARED)
				waveOutUnprepareHeader(mhWaveOut, &hdr, sizeof hdr);
		}
	}

	mHeaders.clear();
	mBuffer.clear();
	mBlocksPending = 0;
	mBlockCount = 0;
	mBlockSize = 0;
	mBytesQueued = 0;

	if (mhWaveOut) {
		waveOutClose(mhWaveOut);
		mhWaveOut = NULL;
	}

	if (mhWaveEvent) {
		CloseHandle(mhWaveEvent);
		mhWaveEvent = NULL;
	}

	mCurState = kStateNone;
}

void VDAudioOutputWaveOutW32::GoSilent() {
	mCurState = kStateSilent;
}

bool VDAudioOutputWaveOutW32::IsSilent() {
	return mCurState == kStateSilent;
}

uint32 VDAudioOutputWaveOutW32::GetMixingRate() const {
	return mSamplesPerSec;
}

bool VDAudioOutputWaveOutW32::Start() {
	if (mCurState == kStateSilent)
		return true;

	if (mCurState < kStateOpened)
		return false;

	if (MMSYSERR_NOERROR != waveOutRestart(mhWaveOut))
		return false;

	mCurState = kStatePlaying;

	return true;
}

bool VDAudioOutputWaveOutW32::Stop() {
	if (mCurState == kStateSilent) return true;

	if (mCurState >= kStateOpened) {
		if (MMSYSERR_NOERROR != waveOutReset(mhWaveOut))
			return false;

		mCurState = kStateOpened;

		CheckBuffers();
	}

	return true;
}

bool VDAudioOutputWaveOutW32::CheckBuffers() {
	if (mCurState == kStateSilent) return true;

	bool found = false;
	for(;;) {
		if (!mBlocksPending)
			return found;

		WAVEHDR& hdr = mHeaders[mBlockHead];

		if (!(hdr.dwFlags & WHDR_DONE))
			return found;

		++mBlockHead;
		if (mBlockHead >= mBlockCount)
			mBlockHead = 0;
		--mBlocksPending;
		mBytesQueued -= hdr.dwBufferLength;
		VDASSERT(mBlocksPending >= 0);
		found = true;
	}
}

bool VDAudioOutputWaveOutW32::WaitBuffers(uint32 timeout) {
	if (mCurState == kStateSilent) return true;

	if (mhWaveOut && timeout) {
		for(;;) {
			if (WAIT_OBJECT_0 != WaitForSingleObject(mhWaveEvent, timeout))
				return false;

			if (CheckBuffers())
				return true;
		}
	}

	return CheckBuffers();
}

uint32 VDAudioOutputWaveOutW32::GetAvailSpace() {
	CheckBuffers();
	return (mBlockCount - mBlocksPending) * mBlockSize - mBlockWriteOffset;
}

uint32 VDAudioOutputWaveOutW32::GetBufferLevel() {
	CheckBuffers();

	uint32 level = mBlocksPending * mBlockSize;
	if (mBlockWriteOffset) {
		level -= mBlockSize;
		level += mBlockWriteOffset;
	}

	return level;
}

uint32 VDAudioOutputWaveOutW32::EstimateHWBufferLevel(bool *underflowDetected) {
	return GetBufferLevel();
}

bool VDAudioOutputWaveOutW32::Write(const void *data, uint32 len) {
	if (mCurState == kStateSilent)
		return true;

	CheckBuffers();

	while(len) {
		if (mBlocksPending >= mBlockCount) {
			if (mCurState == kStateOpened) {
				if (!Start())
					return false;
			}

			if (!WaitBuffers(0)) {
				if (!WaitBuffers(INFINITE)) {
					return false;
				}
				continue;
			}
			break;
		}

		WAVEHDR& hdr = mHeaders[mBlockTail];

		uint32 tc = mBlockSize - mBlockWriteOffset;
		if (tc > len)
			tc = len;

		if (tc) {
			if (data) {
				memcpy((char *)hdr.lpData + mBlockWriteOffset, data, tc);
				data = (const char *)data + tc;
			} else
				memset((char *)hdr.lpData + mBlockWriteOffset, 0, tc);

			mBlockWriteOffset += tc;
			len -= tc;
		}

		if (mBlockWriteOffset >= mBlockSize) {
			if (!Flush())
				return false;
		}
	}

	return true;
}

bool VDAudioOutputWaveOutW32::Flush() {
	if (mCurState == kStateOpened) {
		if (!Start())
			return false;
	}

	if (mBlockWriteOffset <= 0)
		return true;

	WAVEHDR& hdr = mHeaders[mBlockTail];

	hdr.dwBufferLength = mBlockWriteOffset;
	hdr.dwFlags &= ~WHDR_DONE;
	MMRESULT res = waveOutWrite(mhWaveOut, &hdr, sizeof hdr);
	mBytesQueued += mBlockWriteOffset;
	mBlockWriteOffset = 0;

	if (res != MMSYSERR_NOERROR)
		return false;

	++mBlockTail;
	if (mBlockTail >= mBlockCount)
		mBlockTail = 0;
	++mBlocksPending;
	VDASSERT(mBlocksPending <= mBlockCount);
	return true;
}

bool VDAudioOutputWaveOutW32::Finalize(uint32 timeout) {
	if (mCurState == kStateSilent) return true;

	Flush();

	while(CheckBuffers(), mBlocksPending) {
		if (WAIT_OBJECT_0 != WaitForSingleObject(mhWaveEvent, timeout))
			return false;
	}

	return true;
}

sint32 VDAudioOutputWaveOutW32::GetPosition() {
	MMTIME mmtime;

	if (mCurState != kStatePlaying) return -1;

	mmtime.wType = TIME_SAMPLES;

	MMRESULT res;

	vdsynchronized(mcsWaveDevice) {
		res = waveOutGetPosition(mhWaveOut, &mmtime, sizeof mmtime);
	}

	if (MMSYSERR_NOERROR != res)
		return -1;

	switch(mmtime.wType) {
	case TIME_BYTES:
		return MulDiv(mmtime.u.cb, 1000, mAvgBytesPerSec);
	case TIME_MS:
		return mmtime.u.ms;
	case TIME_SAMPLES:
		return MulDiv(mmtime.u.sample, 1000, mSamplesPerSec);
	}

	return -1;
}

sint32 VDAudioOutputWaveOutW32::GetPositionBytes() {
	MMTIME mmtime;

	if (mCurState != kStatePlaying) return -1;

	mmtime.wType = TIME_BYTES;

	MMRESULT res;

	vdsynchronized(mcsWaveDevice) {
		res = waveOutGetPosition(mhWaveOut, &mmtime, sizeof mmtime);
	}

	if (MMSYSERR_NOERROR != res)
		return -1;

	switch(mmtime.wType) {
	case TIME_BYTES:
		return mmtime.u.cb;
	case TIME_MS:
		return MulDiv(mmtime.u.ms, mAvgBytesPerSec, 1000);
	case TIME_SAMPLES:
		return MulDiv(mmtime.u.sample, mAvgBytesPerSec, mSamplesPerSec);
	}

	return -1;
}

double VDAudioOutputWaveOutW32::GetPositionTime() {
	MMTIME mmtime;

	if (mCurState != kStatePlaying) return -1;

	mmtime.wType = TIME_MS;

	MMRESULT res;

	vdsynchronized(mcsWaveDevice) {
		res = waveOutGetPosition(mhWaveOut, &mmtime, sizeof mmtime);
	}

	if (MMSYSERR_NOERROR != res)
		return -1;

	switch(mmtime.wType) {
	case TIME_BYTES:
		return (double)mmtime.u.cb / (double)mAvgBytesPerSec;
	case TIME_MS:
		return (double)mmtime.u.ms / 1000.0;
	case TIME_SAMPLES:
		return (double)mmtime.u.sample / (double)mSamplesPerSec;
	}

	return -1;
}

bool VDAudioOutputWaveOutW32::IsFrozen() {
	if (mCurState != kStatePlaying)
		return true;

	CheckBuffers();

	return !mBlocksPending;
}

UINT VDAudioOutputWaveOutW32::FindDevice(const wchar_t *preferredDevice) {
	UINT deviceID = WAVE_MAPPER;

	if (preferredDevice && *preferredDevice) {
		UINT numDevices = waveOutGetNumDevs();

		for(UINT i=0; i<numDevices; ++i) {
			WAVEOUTCAPSW caps = {0};

			if (MMSYSERR_NOERROR == waveOutGetDevCapsW(i, &caps, sizeof(caps))) {
				const VDStringSpanW key(caps.szPname);

				if (key == preferredDevice) {
					deviceID = i;
					break;
				}
			}
		}
	}

	return deviceID;
}

///////////////////////////////////////////////////////////////////////////

class VDAudioOutputDirectSoundW32 final : public IVDAudioOutput, private VDThread {
public:
	VDAudioOutputDirectSoundW32();
	~VDAudioOutputDirectSoundW32();

	uint32	GetPreferredSamplingRate(const wchar_t *preferredDevice) const override;

	bool	Init(uint32 bufsize, uint32 bufcount, const tWAVEFORMATEX *wf, const wchar_t *preferredDevice) override;
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
	struct Cursors {
		uint32	mPlayCursor;
		uint32	mWriteCursor;
	};

	bool	ReadCursors(Cursors& cursors) const;
	bool	WriteAudio(uint32 offset, const void *buf, uint32 bytes);
	void	ThreadRun() override;
	bool	InitDirectSound();
	void	ShutdownDirectSound();
	bool	InitPlayback();
	void	StartPlayback();
	void	StopPlayback();
	void	ShutdownPlayback();

	uint32				mStreamWritePosition;

	HMODULE				mhmodDS;
	IDirectSound8		*mpDS8;
	IDirectSoundBuffer8	*mpDSBuffer;

	uint32				mBufferSize;
	uint32				mDSBufferSize;
	uint32				mDSBufferSizeHalf;
	uint32				mDSWriteCursor;

	double				mMillisecsPerByte;

	enum ThreadState {
		kThreadStateStop,
		kThreadStatePlay,
		kThreadStateFinalize,
		kThreadStateExit
	};

	vdstructex<tWAVEFORMATEX>	mInitFormat;

	VDCriticalSection	mMutex;
	uint32				mDSBufferedBytes;
	uint32				mDSStreamPlayPosition;
	vdfastvector<uint8>	mBuffer;
	uint32				mBufferReadOffset;
	uint32				mBufferWriteOffset;
	uint32				mBufferLevel;
	bool				mbThreadInited;
	bool				mbThreadInitSucceeded;
	bool				mbFrozen;
	ThreadState			mThreadState;
	VDSignal			mUpdateEvent;
	VDSignal			mResponseEvent;

	bool				mbDSBufferPlaying;
};

IVDAudioOutput *VDCreateAudioOutputDirectSoundW32() {
	return new VDAudioOutputDirectSoundW32;
}

VDAudioOutputDirectSoundW32::VDAudioOutputDirectSoundW32()
	: mStreamWritePosition(0)
	, mhmodDS(NULL)
	, mpDS8(NULL)
	, mpDSBuffer(NULL)
	, mDSBufferedBytes(0)
	, mDSStreamPlayPosition(0)
	, mBufferReadOffset(0)
	, mBufferWriteOffset(0)
	, mBufferLevel(0)
	, mBufferSize(0)
	, mbFrozen(false)
	, mThreadState(kThreadStateStop)
	, mbDSBufferPlaying(false)
{
}

VDAudioOutputDirectSoundW32::~VDAudioOutputDirectSoundW32() {
	Shutdown();
}

uint32 VDAudioOutputDirectSoundW32::GetPreferredSamplingRate(const wchar_t *preferredDevice) const {
	HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
	if (FAILED(hr))
		return 0;
	
	uint32 samplingRate = 0;

	// enumerate audio endpoints in WASAPI and see if we get a match
	{
		vdrefptr<IMMDeviceEnumerator> devEnum;
		hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_INPROC_SERVER, __uuidof(IMMDeviceEnumerator), (LPVOID *)~devEnum);
		if (SUCCEEDED(hr)) {
			vdrefptr<IMMDevice> dev;
			hr = devEnum->GetDefaultAudioEndpoint(eRender, eConsole, ~dev);
			if (SUCCEEDED(hr)) {
				vdrefptr<IAudioClient> client;
				hr = dev->Activate(__uuidof(IAudioClient), CLSCTX_INPROC_SERVER, NULL, (void **)~client);
				if (SUCCEEDED(hr)) {
					WAVEFORMATEX *pwfex = nullptr;
					hr = client->GetMixFormat(&pwfex);
					if (pwfex) {
						if (SUCCEEDED(hr))
							samplingRate = pwfex->nSamplesPerSec;

						CoTaskMemFree(pwfex);
					}
				}
			}
		}
	}

	CoUninitialize();

	return samplingRate;
}

bool VDAudioOutputDirectSoundW32::Init(uint32 bufsize, uint32 bufcount, const tWAVEFORMATEX *wf, const wchar_t *preferredDevice) {
	mBufferSize = bufsize * bufcount;
	mBuffer.resize(mBufferSize);
	mBufferReadOffset = 0;
	mBufferWriteOffset = 0;
	mBufferLevel = 0;

	if (wf->wFormatTag == WAVE_FORMAT_PCM) {
		mInitFormat.resize(sizeof(tWAVEFORMATEX));
		memcpy(&*mInitFormat, wf, sizeof(PCMWAVEFORMAT));
		mInitFormat->cbSize = 0;
	} else
		mInitFormat.assign(wf, sizeof(tWAVEFORMATEX) + wf->cbSize);

	mMutex.Lock();
	mbThreadInited = false;
	mbThreadInitSucceeded = false;
	mMutex.Unlock();

	if (!ThreadStart())
		return false;

	mMutex.Lock();
	while(!mbThreadInited) {
		mMutex.Unlock();
		HANDLE h[2] = { getThreadHandle(), mUpdateEvent.getHandle() };

		if (WaitForMultipleObjects(2, h, FALSE, INFINITE) != WAIT_OBJECT_0 + 1)
			break;

		mMutex.Lock();
	}
	bool succeeded = mbThreadInitSucceeded;
	mMutex.Unlock();

	return succeeded;
}

void VDAudioOutputDirectSoundW32::Shutdown() {
	mMutex.Lock();
	mThreadState = kThreadStateExit;
	mMutex.Unlock();
	mUpdateEvent.signal();

	ThreadWait();
}

void VDAudioOutputDirectSoundW32::GoSilent() {
}

bool VDAudioOutputDirectSoundW32::IsSilent() {
	return false;
}

bool VDAudioOutputDirectSoundW32::IsFrozen() {
	return mbFrozen;
}

uint32 VDAudioOutputDirectSoundW32::GetAvailSpace() {
	mMutex.Lock();
	uint32 space = mBufferSize - mBufferLevel;
	mMutex.Unlock();

	return space;
}

uint32 VDAudioOutputDirectSoundW32::GetBufferLevel() {
	mMutex.Lock();
	uint32 level = mBufferLevel;
	mMutex.Unlock();

	return level;
}

uint32 VDAudioOutputDirectSoundW32::EstimateHWBufferLevel(bool *underflowDetected) {
	mMutex.Lock();
	uint32 level = mBufferLevel + mDSBufferedBytes;
	mMutex.Unlock();

	return level;
}

sint32 VDAudioOutputDirectSoundW32::GetPosition() {
	mMutex.Lock();
	uint32 pos = VDRoundToInt(mDSStreamPlayPosition * mMillisecsPerByte);
	mMutex.Unlock();

	return pos;
}

sint32 VDAudioOutputDirectSoundW32::GetPositionBytes() {
	mMutex.Lock();
	uint32 pos = mDSStreamPlayPosition;
	mMutex.Unlock();

	return pos;
}

double VDAudioOutputDirectSoundW32::GetPositionTime() {
	return GetPosition() / 1000.0;
}

uint32 VDAudioOutputDirectSoundW32::GetMixingRate() const {
	return mInitFormat->nSamplesPerSec;
}

bool VDAudioOutputDirectSoundW32::Start() {
	mMutex.Lock();
	mThreadState = kThreadStatePlay;
	mMutex.Unlock();
	mUpdateEvent.signal();
	return true;
}

bool VDAudioOutputDirectSoundW32::Stop() {
	mMutex.Lock();
	mThreadState = kThreadStateStop;
	mMutex.Unlock();
	mUpdateEvent.signal();
	return true;
}

bool VDAudioOutputDirectSoundW32::Flush() {
	return true;
}

bool VDAudioOutputDirectSoundW32::Write(const void *data, uint32 len) {
	if (!len)
		return true;

	mStreamWritePosition += len;

	bool wroteData = false;

	mMutex.Lock();
	while(len > 0) {
		uint32 tc = mBufferSize - mBufferLevel;

		if (!tc) {
			mMutex.Unlock();

			if (wroteData) {
				mUpdateEvent.signal();
				wroteData = false;
			}

			mResponseEvent.wait();
			mMutex.Lock();
			continue;
		}

		if (tc > len)
			tc = len;

		uint32 contigLeft = mBufferSize - mBufferWriteOffset;
		if (tc > contigLeft)
			tc = contigLeft;

		memcpy(mBuffer.data() + mBufferWriteOffset, data, tc);

		mBufferWriteOffset += tc;
		if (mBufferWriteOffset >= mBufferSize)
			mBufferWriteOffset = 0;

		data = (const char *)data + tc;
		len -= tc;
		wroteData = true;

		mBufferLevel += tc;
	}
	mMutex.Unlock();

	if (wroteData)
		mUpdateEvent.signal();

	return true;
}

bool VDAudioOutputDirectSoundW32::Finalize(uint32 timeout) {
	DWORD deadline = GetTickCount() + timeout;

	mMutex.Lock();
	for(;;) {
		if (mThreadState != kThreadStatePlay || !isThreadAttached() || mDSStreamPlayPosition == mStreamWritePosition)
			break;

		mMutex.Unlock();

		if (timeout == (uint32)-1)
			mResponseEvent.wait();
		else {
			uint32 timeNow = GetTickCount();
			sint32 delta = deadline - timeNow;

			if (delta < 0)
				return false;

			mResponseEvent.tryWait(delta);
		}

		mMutex.Lock();
	}
	mMutex.Unlock();
	return true;
}

bool VDAudioOutputDirectSoundW32::ReadCursors(Cursors& cursors) const {
	if (!mpDSBuffer)
		return false;

	DWORD playCursor, writeCursor;
	HRESULT hr = mpDSBuffer->GetCurrentPosition(&playCursor, &writeCursor);

	if (FAILED(hr))
		return false;

	cursors.mPlayCursor = playCursor;
	cursors.mWriteCursor = writeCursor;
	return true;
}

bool VDAudioOutputDirectSoundW32::WriteAudio(uint32 offset, const void *data, uint32 bytes) {
	VDASSERT(offset < mDSBufferSize);
	VDASSERT(mDSBufferSize - offset >= bytes);

	LPVOID p1, p2;
	DWORD tc1, tc2;
	HRESULT hr = mpDSBuffer->Lock(offset, bytes, &p1, &tc1, &p2, &tc2, 0);

	if (FAILED(hr))
		return false;

	memcpy(p1, data, tc1);
	data = (char *)data + tc1;
	memcpy(p2, data, tc2);
	data = (char *)data + tc2;

	mpDSBuffer->Unlock(p1, tc1, p2, tc2);
	return true;
}

bool VDAudioOutputDirectSoundW32::InitDirectSound() {
	CoInitializeEx(nullptr, COINIT_MULTITHREADED);

	mDSBufferSize = mBufferSize * 2;
	mDSBufferSizeHalf = mBufferSize;

	// attempt to load DirectSound library
	mhmodDS = VDLoadSystemLibraryW32("dsound");
	if (!mhmodDS)
		return false;

	typedef HRESULT (WINAPI *tpDirectSoundCreate8)(LPCGUID, LPDIRECTSOUND8 *, LPUNKNOWN);
	tpDirectSoundCreate8 pDirectSoundCreate8 = (tpDirectSoundCreate8)GetProcAddress(mhmodDS, "DirectSoundCreate8");
	if (!pDirectSoundCreate8) {
		VDDEBUG("VDAudioOutputDirectSound: Cannot find DirectSoundCreate8 entry point!\n");
		return false;
	}

	// attempt to create DirectSound object
	HRESULT hr = pDirectSoundCreate8(NULL, &mpDS8, NULL);
	if (FAILED(hr)) {
		VDDEBUG("VDAudioOutputDirectSound: Failed to create DirectSound object! hr=%08x\n", hr);
		return false;
	}

	// Set cooperative level.
	//
	// From microsoft.public.win32.programmer.directx.audio, by an SDE on the Windows AV team:
	//
	// "I can't speak for all DirectX components but DirectSound does not
	//  subclass the window procedure.  It simply uses the window handle to
	//  determine (every 1/2 second, in a seperate thread) if the window that
	//  corresponds to the handle has the focus (Actually, it is slightly more
	//  complicated than that, but that is close enough for this discussion). 
	//  You can feel free to use the desktop window or console window for the
	//  window handle if you are going to create GLOBAL_FOCUS buffers."
	//
	// Alright, you guys said we could do it!
	//
	hr = mpDS8->SetCooperativeLevel(GetDesktopWindow(), DSSCL_PRIORITY);
	if (FAILED(hr)) {
		VDDEBUG("VDAudioOutputDirectSound: Failed to set cooperative level! hr=%08x\n", hr);
		return false;
	}

	return true;
}

void VDAudioOutputDirectSoundW32::ShutdownDirectSound() {
	ShutdownPlayback();

	if (mpDS8) {
		mpDS8->Release();
		mpDS8 = NULL;
	}

	if (mhmodDS) {
		FreeLibrary(mhmodDS);
		mhmodDS = NULL;
	}

	CoUninitialize();
}

bool VDAudioOutputDirectSoundW32::InitPlayback() {
	tWAVEFORMATEX *wf = &*mInitFormat;
	mMillisecsPerByte = 1000.0 * (double)wf->nBlockAlign / (double)wf->nAvgBytesPerSec;

	// create looping secondary buffer
	DSBUFFERDESC dsd={sizeof(DSBUFFERDESC)};
	dsd.dwFlags			= DSBCAPS_GETCURRENTPOSITION2 | DSBCAPS_GLOBALFOCUS;
	dsd.dwBufferBytes	= mDSBufferSize;
	dsd.lpwfxFormat		= (WAVEFORMATEX *)wf;
	dsd.guid3DAlgorithm	= DS3DALG_DEFAULT;

	IDirectSoundBuffer *pDSB;
	HRESULT hr = mpDS8->CreateSoundBuffer(&dsd, &pDSB, NULL);
	if (FAILED(hr)) {
		VDDEBUG("VDAudioOutputDirectSound: Failed to create secondary buffer! hr=%08x\n", hr);
		return false;
	}

	// query to IDirectSoundBuffer8
	hr = pDSB->QueryInterface(IID_IDirectSoundBuffer8, (void **)&mpDSBuffer);
	pDSB->Release();
	if (FAILED(hr)) {
		VDDEBUG("VDAudioOutputDirectSound: Failed to obtain IDirectSoundBuffer8 interface! hr=%08x\n", hr);
		return false;
	}

	// all done!
	mDSWriteCursor = 0;
	return true;
}

void VDAudioOutputDirectSoundW32::StartPlayback() {
	if (!mbDSBufferPlaying) {
		if (mpDSBuffer)
			mpDSBuffer->Play(0, 0, DSBPLAY_LOOPING);

		mbDSBufferPlaying = true;
	}
}

void VDAudioOutputDirectSoundW32::StopPlayback() {
	if (mbDSBufferPlaying) {
		if (mpDSBuffer)
			mpDSBuffer->Stop();

		mbDSBufferPlaying = false;
	}
}

void VDAudioOutputDirectSoundW32::ShutdownPlayback() {
	if (mpDSBuffer) {
		mpDSBuffer->Release();
		mpDSBuffer = NULL;
	}
}

void VDAudioOutputDirectSoundW32::ThreadRun() {
	if (!InitDirectSound()) {
		ShutdownDirectSound();
		mMutex.Lock();
		mbThreadInited = true;
		mbThreadInitSucceeded = false;
		mMutex.Unlock();
		mUpdateEvent.signal();
		return;
	}

	ThreadState threadState = kThreadStateStop;
	uint32 lastInitCount = 0;
	bool underflow = false;
	bool playing = false;
	uint32 dsStreamWritePosition = 0;

	if (!InitPlayback()) {
		ShutdownDirectSound();

		mMutex.Lock();
		mbThreadInited = true;
		mbThreadInitSucceeded = false;
		mMutex.Unlock();
		mUpdateEvent.signal();
		return;
	}

	mMutex.Lock();
	mbThreadInited = true;
	mbThreadInitSucceeded = true;
	mUpdateEvent.signal();
	mMutex.Unlock();

	for(;;) {
		if (playing)
			mUpdateEvent.tryWait(10);
		else
			mUpdateEvent.wait();

		mMutex.Lock();
		threadState = (ThreadState)mThreadState;
		mMutex.Unlock();

		if (threadState == kThreadStatePlay) {
			if (!underflow) {
				StartPlayback();
				playing = true;
			}
		} else {
			StopPlayback();
			playing = false;
		}

		if (threadState == kThreadStateExit)
			break;

		if (!playing)
			continue;

		Cursors cursors;
		if (!ReadCursors(cursors))
			continue;

		uint32 level;
		mMutex.Lock();
		level = mBufferLevel;

		// Compute current buffering level.
		sint32 bufferedLevel = mDSWriteCursor - cursors.mPlayCursor;
		if (bufferedLevel > (sint32)mDSBufferSizeHalf)
			bufferedLevel -= mDSBufferSize;
		else if (bufferedLevel < -(sint32)mDSBufferSizeHalf)
			bufferedLevel += mDSBufferSize;

		if (bufferedLevel < 0) {
			bufferedLevel = 0;
			mDSWriteCursor = cursors.mWriteCursor;
		}

		// Compute the stream play position. This should never go backward. If it
		// has, we have underflowed.
		uint32 newDSStreamPlayPos = dsStreamWritePosition - bufferedLevel;

		if (newDSStreamPlayPos < mDSStreamPlayPosition) {
			mDSStreamPlayPosition = dsStreamWritePosition;
			bufferedLevel = 0;
		}

		mDSBufferedBytes = bufferedLevel;

		mMutex.Unlock();

		if (!level) {
			if (!underflow && playing) {
				// Check for underflow.
				if (!bufferedLevel) {
					StopPlayback();
					playing = false;
				}
			}

			continue;
		}

		// compute how many bytes to copy
		uint32 toCopy = level;
		if (toCopy + bufferedLevel > mDSBufferSizeHalf)
			toCopy = mDSBufferSizeHalf - bufferedLevel;

		if (!toCopy)
			continue;

		// update local write position
		dsStreamWritePosition += toCopy;

		// lock and copy into DirectSound buffer
		const uint8 *src = mBuffer.data();
		uint32 consumed = 0;
		while(toCopy > 0) {
			const uint32 tc2 = std::min<uint32>(toCopy, mBufferSize - mBufferReadOffset);
			const uint32 tc3 = std::min<uint32>(tc2, mDSBufferSize - mDSWriteCursor);

			WriteAudio(mDSWriteCursor, src + mBufferReadOffset, tc3);
			mBufferReadOffset += tc3;
			if (mBufferReadOffset >= mBufferSize)
				mBufferReadOffset = 0;

			mDSWriteCursor += tc3;
			if (mDSWriteCursor >= mDSBufferSize)
				mDSWriteCursor = 0;

			toCopy -= tc3;
			consumed += tc3;
		}

		mMutex.Lock();
		mBufferLevel -= consumed;
		mMutex.Unlock();

		// restart playback if we were in underflow state
		if (underflow && !playing) {
			underflow = false;
			playing = true;

			StartPlayback();
		}

		mResponseEvent.signal();
	}

	ShutdownPlayback();
	ShutdownDirectSound();
}
