//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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

#include <stdafx.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/resample.h>
#include "gtia.h"
#include "trace.h"
#include "tracevideo.h"

///////////////////////////////////////////////////////////////////////////

class ATTraceChannelVideo final : public vdrefcounted<IATTraceChannel, IATTraceChannelVideo> {
public:
	static constexpr uint32 kTypeID = 'tcvd';

	ATTraceChannelVideo(ATTraceMemoryTracker *memTracker) : mpMemTracker(memTracker) {}

	void SetName(const wchar_t *s) { mName = s; }
	void AddFrame(const VDPixmap& px, double timestamp);

	IATTraceChannel *AsTraceChannel() override { return this; } 
	const VDPixmap *GetNearestFrame(double startTime, double endTime, double& frameTime) override;
	uint64 GetTraceSize() const override { return mTraceSize; }

public:
	void *AsInterface(uint32 iid) override;
	const wchar_t *GetName() const override;
	double GetDuration() const override;
	bool IsEmpty() const override;
	void StartIteration(double startTime, double endTime, double eventThreshold) override;
	bool GetNextEvent(ATTraceEvent& ev) override;

protected:
	static bool ArePlanesEqual(const void *src1, ptrdiff_t pitch1, const void *src2, ptrdiff_t pitch2, size_t w, size_t h);

	struct FrameBuffer {
		FrameBuffer() = default;
		FrameBuffer(FrameBuffer&&) = default;
		FrameBuffer(const FrameBuffer&) = delete;
		FrameBuffer& operator=(FrameBuffer&&) = default;
		FrameBuffer& operator=(const FrameBuffer&) = delete;

		VDPixmapBuffer mBuffer;
	};

	struct Frame {
		double mTime;
		uint32 mFrameBufferIndex;
	};

	vdvector<Frame> mFrames;
	vdvector<FrameBuffer> mFrameBuffers;
	VDStringW mName;
	uint64 mTraceSize = 0;

	ATTraceMemoryTracker *mpMemTracker = nullptr;
};

void ATTraceChannelVideo::AddFrame(const VDPixmap& px, double timestamp) {
	bool changed = true;

	if (!mFrameBuffers.empty()) {
		VDPixmapBuffer& pxLast = mFrameBuffers.back().mBuffer;

		// We know that the format is YCbCr 4:2:0, so we can simplify the checks.
		VDASSERT(px.format == nsVDPixmap::kPixFormat_YUV420_Planar_Centered);

		if (px.w == pxLast.w && px.h == pxLast.h) {
			changed = false;

			// compare the chroma planes first, as they're quarter size
			if (!ArePlanesEqual(pxLast.data2, pxLast.pitch2, px.data2, px.pitch2, (px.w + 1) >> 1, (px.h + 1) >> 1) ||
				!ArePlanesEqual(pxLast.data3, pxLast.pitch3, px.data3, px.pitch3, (px.w + 1) >> 1, (px.h + 1) >> 1) ||
				!ArePlanesEqual(pxLast.data, pxLast.pitch, px.data, px.pitch, px.w, px.h))
			{
				changed = true;
			}
		}
	}

	if (changed) {
		FrameBuffer fb;
		fb.mBuffer.assign(px);
		mTraceSize += fb.mBuffer.size();

		if (mpMemTracker)
			mpMemTracker->AddSize(fb.mBuffer.size());

		mFrameBuffers.emplace_back(std::move(fb));
	}

	Frame frame;
	frame.mTime = timestamp;
	frame.mFrameBufferIndex = (uint32)mFrameBuffers.size() - 1;
	mFrames.emplace_back(std::move(frame));
}

const VDPixmap *ATTraceChannelVideo::GetNearestFrame(double startTime, double endTime, double& frameTime) {
	// find closest frame to middle time
	double midTime = (startTime + endTime) * 0.5;

	auto it = std::lower_bound(mFrames.begin(), mFrames.end(), midTime,
		[](const Frame& x, double y) { return x.mTime < y; });

	if (it != mFrames.begin() && (it == mFrames.end() || midTime - std::prev(it)->mTime < it->mTime - midTime))
		--it;

	// return frame if it is within time bracket
	if (it != mFrames.end() && it->mTime >= startTime && it->mTime < endTime) {
		frameTime = it->mTime;
		return &mFrameBuffers[it->mFrameBufferIndex].mBuffer;
	}

	return nullptr;
}

void *ATTraceChannelVideo::AsInterface(uint32 iid) {
	if (iid == ATTraceChannelVideo::kTypeID)
		return this;
	else if (iid == IATTraceChannelVideo::kTypeID)
		return static_cast<IATTraceChannelVideo *>(this);

	return nullptr;
}

const wchar_t *ATTraceChannelVideo::GetName() const {
	return mName.c_str();
}

double ATTraceChannelVideo::GetDuration() const {
	return mFrames.empty() ? 0 : mFrames.back().mTime;
}

bool ATTraceChannelVideo::IsEmpty() const {
	return mFrames.empty();
}

void ATTraceChannelVideo::StartIteration(double startTime, double endTime, double eventThreshold) {
}

bool ATTraceChannelVideo::GetNextEvent(ATTraceEvent& ev) {
	return false;
}

bool ATTraceChannelVideo::ArePlanesEqual(const void *src1, ptrdiff_t pitch1, const void *src2, ptrdiff_t pitch2, size_t w, size_t h) {
	// flip both planes if both planes are inverted
	if (pitch1 < 0 && pitch2 < 0) {
		src1 = (const char *)src1 + pitch1 * (h - 1);
		pitch1 = -pitch1;

		src2 = (const char *)src2 + pitch2 * (h - 1);
		pitch2 = -pitch2;
	}

	// check if we can linearize the check
	if (pitch1 == w && pitch2 == w) {
		w *= h;
		h = 1;
	}

	// check scanlines
	while(h--) {
		if (memcmp(src1, src2, w))
			return false;

		src1 = (const char *)src1 + pitch1;
		src2 = (const char *)src2 + pitch2;
	}

	return true;
}

///////////////////////////////////////////////////////////////////////////

vdrefptr<IATTraceChannelVideo> ATCreateTraceChannelVideo(const wchar_t *name, ATTraceMemoryTracker *memTracker) {
	vdrefptr<ATTraceChannelVideo> p { new ATTraceChannelVideo(memTracker) };

	p->SetName(name);

	return vdrefptr<IATTraceChannelVideo>(std::move(p));
}

///////////////////////////////////////////////////////////////////////////

class ATVideoTracer final : public vdrefcounted<IATVideoTracer>, public IATGTIAVideoTap {
public:
	IATGTIAVideoTap *AsVideoTap() override { return this; }

	void Init(IATTraceChannelVideo *dst, uint64 timeOffset, double timeScale, uint32 divisor) override;
	void Shutdown() override;

	void WriteFrame(const VDPixmap& px, uint64 timestampStart, uint64 timestampEnd) override;

private:
	vdrefptr<ATTraceChannelVideo> mpDst;
	uint64 mTimeOffset = 0;
	double mTimeScale = 0;
	uint32 mDivideCounter = UINT32_MAX - 1;
	uint32 mDivisor = 0;

	VDPixmapBuffer mTempBuffer1;
	VDPixmapBuffer mTempBuffer2;
	VDPixmapBuffer mTempBuffer3;

	vdautoptr<IVDPixmapResampler> mpResampler;
	vdsize32 mResampleSrcSize { 0, 0 };
	vdsize32 mResampleDstSize { 0, 0 };
};

void ATVideoTracer::Init(IATTraceChannelVideo *dst, uint64 timeOffset, double timeScale, uint32 divisor) {
	mpDst = vdpoly_cast<ATTraceChannelVideo *>(dst);
	mTimeOffset = timeOffset;
	mTimeScale = timeScale;
	mDivisor = divisor;
}

void ATVideoTracer::Shutdown() {
	mpDst = nullptr;
}

void ATVideoTracer::WriteFrame(const VDPixmap& px, uint64 timestampStart, uint64 timestampEnd) {
	if (timestampStart < mTimeOffset)
		return;

	if (++mDivideCounter < mDivisor)
		return;

	mDivideCounter = 0;

	sint32 dsth = 128;
	sint32 dstw = (px.w * 128 + (px.h >> 1)) / px.h;

	mTempBuffer1.init(px.w, px.h, nsVDPixmap::kPixFormat_XRGB8888);
	mTempBuffer2.init(dstw, dsth, nsVDPixmap::kPixFormat_XRGB8888);
	mTempBuffer3.init(dstw, dsth, nsVDPixmap::kPixFormat_YUV420_Planar_Centered);

	VDPixmapBlt(mTempBuffer1, px);

	const vdsize32 srcSize { px.w, px.h };
	const vdsize32 dstSize { dstw, dsth };

	if (!mpResampler || mResampleSrcSize != srcSize || mResampleDstSize != dstSize) {
		mpResampler = VDCreatePixmapResampler();
		mpResampler->Init(dstSize.w, dstSize.h, nsVDPixmap::kPixFormat_XRGB8888, srcSize.w, srcSize.h, nsVDPixmap::kPixFormat_XRGB8888);
		mpResampler->SetFilters(IVDPixmapResampler::kFilterLinear, IVDPixmapResampler::kFilterLinear, false);
		mResampleSrcSize = srcSize;
		mResampleDstSize = dstSize;
	}

	mpResampler->Process(mTempBuffer2, mTempBuffer1);
	VDPixmapBlt(mTempBuffer3, mTempBuffer2);

	mpDst->AddFrame(mTempBuffer3, (double)(timestampStart + ((timestampEnd - timestampStart) >> 1) - mTimeOffset) * mTimeScale);
}

///////////////////////////////////////////////////////////////////////////

vdrefptr<IATVideoTracer> ATCreateVideoTracer() {
	return vdrefptr<IATVideoTracer>(new ATVideoTracer);
}
