//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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
#include <vd2/system/binary.h>
#include <vd2/system/bitmath.h>
#include <vd2/system/cpuaccel.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/math.h>
#include <vd2/system/refcount.h>
#include <at/atcore/audiosource.h>
#include <at/atcore/progress.h>
#include <at/atio/cassetteblock.h>
#include <at/atio/cassettedecoder.h>
#include <at/atio/cassetteimage.h>
#include <at/atio/wav.h>
#include <at/atcore/logging.h>

#if VD_CPU_X86 || VD_CPU_X64
#include <emmintrin.h>
#endif

#ifdef _DEBUG
	#define AT_CASSETTE_VALIDATE() Validate()
#else
	#define AT_CASSETTE_VALIDATE() ((void)0)
#endif

using namespace nsVDWinFormats;

ATLogChannel g_ATLCCasImage(false, false, "CASIMAGE", "Cassette image processing");

///////////////////////////////////////////////////////////////////////////

namespace {
	void ReadMono8(sint16 *dst, IVDRandomAccessStream& src, uint32 count) {
		uint8 buf[1024];

		while(count) {
			uint32 tc = count > 1024 ? 1024 : count;
			count -= tc;

			src.Read(buf, tc);

			for(uint32 i=0; i<tc; ++i) {
				dst[0] = dst[1] = (buf[i] << 8) - 0x8000;
				dst += 2;
			}
		}
	}

	void ReadMono16(sint16 *dst, IVDRandomAccessStream& src, uint32 count) {
		sint16 buf[1024];

		while(count) {
			uint32 tc = count > 1024 ? 1024 : count;
			count -= tc;

			src.Read(buf, tc*2);

			for(uint32 i=0; i<tc; ++i) {
				dst[0] = dst[1] = buf[i];
				dst += 2;
			}
		}
	}

	void ReadStereo8(sint16 *dst, IVDRandomAccessStream& src, uint32 count) {
		uint8 buf[1024][2];

		while(count) {
			uint32 tc = count > 1024 ? 1024 : count;
			count -= tc;

			src.Read(buf, tc*2);

			for(uint32 i=0; i<tc; ++i) {
				dst[0] = (buf[i][0] << 8) - 0x8000;
				dst[1] = (buf[i][1] << 8) - 0x8000;
				dst += 2;
			}
		}
	}

	void ReadStereo16(sint16 *dst, IVDRandomAccessStream& src, uint32 count) {
		src.Read(dst, count*4);
	}

	extern "C" VDALIGN(16) const sint16 kernel[32][8] = {
		{+0x0000,+0x0000,+0x0000,+0x4000,+0x0000,+0x0000,+0x0000,+0x0000 },
		{-0x000a,+0x0052,-0x0179,+0x3fe2,+0x019f,-0x005b,+0x000c,+0x0000 },
		{-0x0013,+0x009c,-0x02cc,+0x3f86,+0x0362,-0x00c0,+0x001a,+0x0000 },
		{-0x001a,+0x00dc,-0x03f9,+0x3eef,+0x054a,-0x012c,+0x002b,+0x0000 },
		{-0x001f,+0x0113,-0x0500,+0x3e1d,+0x0753,-0x01a0,+0x003d,+0x0000 },
		{-0x0023,+0x0141,-0x05e1,+0x3d12,+0x097c,-0x021a,+0x0050,-0x0001 },
		{-0x0026,+0x0166,-0x069e,+0x3bd0,+0x0bc4,-0x029a,+0x0066,-0x0001 },
		{-0x0027,+0x0182,-0x0738,+0x3a5a,+0x0e27,-0x031f,+0x007d,-0x0002 },
		{-0x0028,+0x0197,-0x07b0,+0x38b2,+0x10a2,-0x03a7,+0x0096,-0x0003 },
		{-0x0027,+0x01a5,-0x0807,+0x36dc,+0x1333,-0x0430,+0x00af,-0x0005 },
		{-0x0026,+0x01ab,-0x083f,+0x34db,+0x15d5,-0x04ba,+0x00ca,-0x0007 },
		{-0x0024,+0x01ac,-0x085b,+0x32b3,+0x1886,-0x0541,+0x00e5,-0x0008 },
		{-0x0022,+0x01a6,-0x085d,+0x3068,+0x1b40,-0x05c6,+0x0101,-0x000b },
		{-0x001f,+0x019c,-0x0846,+0x2dfe,+0x1e00,-0x0644,+0x011c,-0x000d },
		{-0x001c,+0x018e,-0x0819,+0x2b7a,+0x20c1,-0x06bb,+0x0136,-0x0010 },
		{-0x0019,+0x017c,-0x07d9,+0x28e1,+0x2380,-0x0727,+0x014f,-0x0013 },
		{-0x0016,+0x0167,-0x0788,+0x2637,+0x2637,-0x0788,+0x0167,-0x0016 },
		{-0x0013,+0x014f,-0x0727,+0x2380,+0x28e1,-0x07d9,+0x017c,-0x0019 },
		{-0x0010,+0x0136,-0x06bb,+0x20c1,+0x2b7a,-0x0819,+0x018e,-0x001c },
		{-0x000d,+0x011c,-0x0644,+0x1e00,+0x2dfe,-0x0846,+0x019c,-0x001f },
		{-0x000b,+0x0101,-0x05c6,+0x1b40,+0x3068,-0x085d,+0x01a6,-0x0022 },
		{-0x0008,+0x00e5,-0x0541,+0x1886,+0x32b3,-0x085b,+0x01ac,-0x0024 },
		{-0x0007,+0x00ca,-0x04ba,+0x15d5,+0x34db,-0x083f,+0x01ab,-0x0026 },
		{-0x0005,+0x00af,-0x0430,+0x1333,+0x36dc,-0x0807,+0x01a5,-0x0027 },
		{-0x0003,+0x0096,-0x03a7,+0x10a2,+0x38b2,-0x07b0,+0x0197,-0x0028 },
		{-0x0002,+0x007d,-0x031f,+0x0e27,+0x3a5a,-0x0738,+0x0182,-0x0027 },
		{-0x0001,+0x0066,-0x029a,+0x0bc4,+0x3bd0,-0x069e,+0x0166,-0x0026 },
		{-0x0001,+0x0050,-0x021a,+0x097c,+0x3d12,-0x05e1,+0x0141,-0x0023 },
		{+0x0000,+0x003d,-0x01a0,+0x0753,+0x3e1d,-0x0500,+0x0113,-0x001f },
		{+0x0000,+0x002b,-0x012c,+0x054a,+0x3eef,-0x03f9,+0x00dc,-0x001a },
		{+0x0000,+0x001a,-0x00c0,+0x0362,+0x3f86,-0x02cc,+0x009c,-0x0013 },
		{+0x0000,+0x000c,-0x005b,+0x019f,+0x3fe2,-0x0179,+0x0052,-0x000a },
	};

	uint64 resample16x2_scalar(sint16 *d, const sint16 *s, uint32 count, uint64 accum, sint64 inc) {
		do {
			const sint16 *s2 = s + (uint32)(accum >> 32)*2;
			const sint16 *f = kernel[(uint32)accum >> 27];

			accum += inc;

			sint32 l= (sint32)s2[ 0]*(sint32)f[0]
					+ (sint32)s2[ 2]*(sint32)f[1]
					+ (sint32)s2[ 4]*(sint32)f[2]
					+ (sint32)s2[ 6]*(sint32)f[3]
					+ (sint32)s2[ 8]*(sint32)f[4]
					+ (sint32)s2[10]*(sint32)f[5]
					+ (sint32)s2[12]*(sint32)f[6]
					+ (sint32)s2[14]*(sint32)f[7]
					+ 0x20002000;

			sint32 r= (sint32)s2[ 1]*(sint32)f[0]
					+ (sint32)s2[ 3]*(sint32)f[1]
					+ (sint32)s2[ 5]*(sint32)f[2]
					+ (sint32)s2[ 7]*(sint32)f[3]
					+ (sint32)s2[ 9]*(sint32)f[4]
					+ (sint32)s2[11]*(sint32)f[5]
					+ (sint32)s2[13]*(sint32)f[6]
					+ (sint32)s2[15]*(sint32)f[7]
					+ 0x20002000;

			l >>= 14;
			r >>= 14;

			if ((uint32)l >= 0x10000)
				l = ~l >> 31;
			if ((uint32)r >= 0x10000)
				r = ~r >> 31;

			d[0] = (sint16)(l - 0x8000);
			d[1] = (sint16)(r - 0x8000);
			d += 2;
		} while(--count);

		return accum;
	}

#if VD_CPU_X86 || VD_CPU_X64
	uint64 resample16x2_SSE2(sint16 *d, const sint16 *s, uint32 count, uint64 accum, sint64 inc) {
		__m128i round = _mm_set1_epi32(0x2000);

		do {
			const sint16 *s2 = s + (uint32)(accum >> 32)*2;
			const sint16 *f = kernel[(uint32)accum >> 27];
			const __m128i coeff16 = *(const __m128i *)f;

			accum += inc;

			__m128i x0 = _mm_loadu_si128((__m128i *)s2);
			__m128i x1 = _mm_loadu_si128((__m128i *)s2 + 1);

			__m128i y0 = _mm_shufflehi_epi16(_mm_shufflelo_epi16(x0, 0xd8), 0xd8);
			__m128i y1 = _mm_shufflehi_epi16(_mm_shufflelo_epi16(x1, 0xd8), 0xd8);

			__m128i z0 = _mm_madd_epi16(y0, _mm_shuffle_epi32(coeff16, 0x50));
			__m128i z1 = _mm_madd_epi16(y1, _mm_shuffle_epi32(coeff16, 0xfa));

			__m128i a = _mm_add_epi32(z0, z1);
			__m128i b = _mm_add_epi32(a, _mm_shuffle_epi32(a, 0xee));
			__m128i r = _mm_srai_epi32(_mm_add_epi32(b, round), 14);

			__m128i result = _mm_packs_epi32(r, r);

			*(int *)d = _mm_cvtsi128_si32(result);
			d += 2;
		} while(--count);

		return accum;
	}
#endif

	uint64 resample16x2(sint16 *d, const sint16 *s, uint32 count, uint64 accum, sint64 inc) {
#if VD_CPU_X86 || VD_CPU_X64
		if (SSE2_enabled)
			return resample16x2_SSE2(d, s, count, accum, inc);
		else
#endif
			return resample16x2_scalar(d, s, count, accum, inc);
	}

#if VD_CPU_X86 || VD_CPU_X64
	void minMax16x2_SSE2(const sint16 * VDRESTRICT src, uint32 n, sint32& minvL, sint32& maxvL, sint32& minvR, sint32& maxvR) {
		// We do unaligned loads from this array, so it's important that we
		// avoid data cache unit (DCU) split penalties on older CPUs.
		static const __declspec(align(64)) uint64 window_table[6] = {
			0, 0, (uint64)0 - 1, (uint64)0 - 1, 0, 0
		};

		const __m128i * VDRESTRICT src128 = (const __m128i *)((uintptr)src & ~(uintptr)15);
		const __m128i * VDRESTRICT srcend128 = (const __m128i *)((uintptr)(src + n*2) & ~(uintptr)15);
		const ptrdiff_t leftOffset = (ptrdiff_t)((uintptr)src & 15);
		const __m128i leftMask = _mm_loadu_si128((const __m128i *)((const char *)window_table + 16 - leftOffset));
		const ptrdiff_t rightOffset = (ptrdiff_t)((uintptr)(src + n * 2) & 15);
		const __m128i rightMask = _mm_loadu_si128((const __m128i *)((const char *)window_table + 32 - rightOffset));

		__m128i minAcc = _mm_insert_epi16(_mm_cvtsi32_si128(minvL), minvR, 1);
		__m128i maxAcc = _mm_insert_epi16(_mm_cvtsi32_si128(maxvL), maxvR, 1);

		if (src128 != srcend128) {
			__m128i vleft = _mm_and_si128(*src128++, leftMask);
			minAcc = _mm_min_epi16(minAcc, vleft);
			maxAcc = _mm_max_epi16(maxAcc, vleft);

			while(src128 != srcend128) {
				__m128i vmid = *src128++;

				minAcc = _mm_min_epi16(minAcc, vmid);
				maxAcc = _mm_max_epi16(maxAcc, vmid);
			}

			if (rightOffset) {
				__m128i vright = _mm_and_si128(*src128, rightMask);
				minAcc = _mm_min_epi16(minAcc, vright);
				maxAcc = _mm_max_epi16(maxAcc, vright);
			}
		} else {
			__m128i v = _mm_and_si128(src128[0], _mm_and_si128(leftMask, rightMask));

			minAcc = _mm_min_epi16(minAcc, v);
			maxAcc = _mm_max_epi16(maxAcc, v);
		}

		// four four accumulators
		minAcc = _mm_min_epi16(minAcc, _mm_shuffle_epi32(minAcc, 0xEE));
		maxAcc = _mm_max_epi16(maxAcc, _mm_shuffle_epi32(maxAcc, 0xEE));
		minAcc = _mm_min_epi16(minAcc, _mm_shuffle_epi32(minAcc, 0x55));
		maxAcc = _mm_max_epi16(maxAcc, _mm_shuffle_epi32(maxAcc, 0x55));

		minvL = (sint16)_mm_extract_epi16(minAcc, 0);
		minvR = (sint16)_mm_extract_epi16(minAcc, 1);
		maxvL = (sint16)_mm_extract_epi16(maxAcc, 0);
		maxvR = (sint16)_mm_extract_epi16(maxAcc, 1);
	}
#endif

	void minMax16x2_scalar(const sint16 * VDRESTRICT src, uint32 n, sint32& minvL, sint32& maxvL, sint32& minvR, sint32& maxvR) {
		for(uint32 i=0; i<n; ++i) {
			sint32 vL = src[0];
			sint32 vR = src[1];
			src += 2;

			minvL = std::min(minvL, vL);
			maxvL = std::max(maxvL, vL);
			minvR = std::min(minvR, vR);
			maxvR = std::max(maxvR, vR);
		}
	}

	void minMax16x2(const sint16 * VDRESTRICT src, uint32 n, sint32& minvL, sint32& maxvL, sint32& minvR, sint32& maxvR) {
#if VD_CPU_X86 || VD_CPU_X64
		if (SSE2_enabled)
			minMax16x2_SSE2(src, n, minvL, maxvL, minvR, maxvR);
		else
#endif
			minMax16x2_scalar(src, n, minvL, maxvL, minvR, maxvR);
	}
}

///////////////////////////////////////////////////////////////////////////////

class ATCassetteTooLongException : public MyError {
public:
	ATCassetteTooLongException() : MyError("Tape too long (exceeds 7 hours).") {}
};

///////////////////////////////////////////////////////////////////////////////

class ATCassetteImage final : public vdrefcounted<IATCassetteImage> {
public:
	ATCassetteImage();
	~ATCassetteImage();
	
	void *AsInterface(uint32 id) override;

	ATImageType GetImageType() const override { return kATImageType_Tape; }

	uint32 GetDataLength() const override { return mDataLength; }
	uint32 GetAudioLength() const override { return mAudioLength; }
	bool IsAudioCreated() const override { return mbAudioCreated; }

	bool GetBit(uint32 pos, uint32 averagingPeriod, uint32 threshold, bool prevBit, bool bypassFSK) const override;
	bool GetTurboBit(uint32 pos) const override;
	uint32 GetNearestLowBitPos(uint32 pos) const override;

	void ReadPeakMap(float t0, float dt, uint32 n, float *data, float *audio) override;
	void AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const override;

	uint32 GetWriteCursor() const override;
	void SetWriteCursor(uint32 writePos) override;
	void WriteBlankData(uint32 len) override;
	void WriteStdData(uint8 byte, uint32 cyclesPerHalfBit) override;
	void WriteFSKPulse(bool polarity, uint32 samples) override;

	void InitNew();
	void Load(IVDRandomAccessStream& file, IVDRandomAccessStream *afile);
	void SaveCAS(IVDRandomAccessStream& file);
	void SaveWAV(IVDRandomAccessStream& file);

protected:
	struct SortedBlock {
		uint32	mStart;
		uint32	mOffset;
		ATCassetteImageBlock *mpImageBlock;
	};

	uint32 GetBitSum(uint32 pos, uint32 averagingPeriod, bool bypassFSK) const;
	uint32 GetSortedDataBlock(uint32 pos) const;

	uint32 SplitBlock(uint32 startBlockIdx, uint32 splitPt);

	void Validate();

	void ParseWAVE(IVDRandomAccessStream& file, IVDRandomAccessStream *afile);
	void ParseCAS(IVDRandomAccessStream& file);
	void ConvertDataToPeaks();
	void RefreshPeaksFromData(uint32 startSample, uint32 endSample);

	uint32 mDataLength = 0;
	uint32 mAudioLength = 0;
	bool mbAudioCreated = false;

	mutable int mCachedDataBlockIndex = 0;
	uint32 mDataBlockCount = 0;
	uint32 mAudioBlockCount = 0;

	uint32 mWriteCursor = 0;
	sint32 mCurrentWriteBlockIndex = -1;

	typedef vdfastvector<SortedBlock> SortedBlocks;
	SortedBlocks mDataBlocks;
	SortedBlocks mAudioBlocks;

	typedef vdfastvector<ATCassetteImageBlock *> ImageBlocks;
	ImageBlocks mImageBlocks;

	vdfastvector<uint8> mPeakMaps[2];

	uint32 mPeakDirtyStart = (uint32)0 - 1;
	uint32 mPeakDirtyEnd = 0;

	static constexpr int kDataSamplesPerPeakSample = 1024;
	static constexpr float kPeakSamplesPerSecond = kATCassetteDataSampleRate / (float)kDataSamplesPerPeakSample;
	static constexpr float kSecondsPerPeakSample = (float)kDataSamplesPerPeakSample / kATCassetteDataSampleRate;
};

ATCassetteImage::ATCassetteImage() {
}

ATCassetteImage::~ATCassetteImage() {
	while(!mImageBlocks.empty()) {
		ATCassetteImageBlock *p = mImageBlocks.back();

		if (p)
			delete p;

		mImageBlocks.pop_back();
	}
}

void *ATCassetteImage::AsInterface(uint32 id) {
	switch(id) {
		case IATCassetteImage::kTypeID: return static_cast<IATCassetteImage *>(this);
	}

	return nullptr;
}

uint32 ATCassetteImage::GetBitSum(uint32 pos, uint32 len, bool bypassFSK) const {
	if (pos >= mDataLength)
		return len;

	uint32 idx = GetSortedDataBlock(pos);
	const auto *p = &mDataBlocks[idx];
	if (!p->mpImageBlock)
		return len;

	uint32 sum = 0;
	uint32 offset = pos - p->mStart;
	for(auto *p = &mDataBlocks[idx]; p->mpImageBlock; ++p) {
		uint32 sectionLen = p[1].mStart - pos;

		if (sectionLen > len)
			sectionLen = len;

		sum += p->mpImageBlock->GetBitSum(offset + p->mOffset, sectionLen, bypassFSK);

		len-= sectionLen;
		if (!len)
			break;

		offset = 0;
		pos += sectionLen;
	}

	return sum;
}

bool ATCassetteImage::GetBit(uint32 pos, uint32 averagingPeriod, uint32 threshold, bool prevBit, bool bypassFSK) const {
	uint32 sum = GetBitSum(pos, averagingPeriod, bypassFSK);

	if (sum < threshold)
		return false;
	else if (sum > averagingPeriod - threshold)
		return true;
	else
		return prevBit;
}

bool ATCassetteImage::GetTurboBit(uint32 pos) const {
	if (pos >= mDataLength)
		return true;

	uint32 idx = GetSortedDataBlock(pos);
	const auto *p = &mDataBlocks[idx];
	if (!p->mpImageBlock)
		return true;

	uint32 offset = pos - p->mStart;
	for(auto *p = &mDataBlocks[idx]; p->mpImageBlock; ++p) {
		uint32 sectionLen = p[1].mStart - pos;

		if (sectionLen)
			return p->mpImageBlock->GetBitSum(offset + p->mOffset, 1, true) > 0;

		offset = 0;
	}

	return true;
}

uint32 ATCassetteImage::GetNearestLowBitPos(uint32 pos) const {
	if (pos >= mDataLength)
		return mDataLength;

	uint32 idx = GetSortedDataBlock(pos);
	const auto *block = &mDataBlocks[idx];

	for(; block->mpImageBlock; ++block) {
		// if this is a blank block, we can just skip it outright
		const auto& imageBlock = *block->mpImageBlock;
		if (imageBlock.GetBlockType() == kATCassetteImageBlockType_Blank) {
			pos = block[1].mStart;
			continue;
		}

		// scan the block looking for a low bit
		uint32 relOffset = block->mOffset - block->mStart;
		uint32 nextPos = block[1].mStart;

		while(pos < nextPos) {
			if (!imageBlock.GetBitSum(pos + relOffset, 1, false))
				return pos;

			++pos;
		}
	}

	return mDataLength;
}

void ATCassetteImage::ReadPeakMap(float t0, float dt, uint32 n, float *data, float *audio) {
	if (mPeakDirtyEnd > mPeakDirtyStart) {
		RefreshPeaksFromData(mPeakDirtyStart, mPeakDirtyEnd);
		mPeakDirtyStart = 0;
		--mPeakDirtyStart;	// intentional underflow to -1
		mPeakDirtyEnd = 0;
	}

	if (mPeakMaps[0].empty()) {
		memset(data, 0, sizeof(float)*n*2);
		memset(audio, 0, sizeof(float)*n*2);
		return;
	}

	const size_t m = mPeakMaps[0].size() >> 1;

	float x0 = t0 * kPeakSamplesPerSecond;
	float dx = dt * kPeakSamplesPerSecond;

	const auto *peakMap0 = mPeakMaps[0].data();
	const auto *peakMap1 = mPeakMaps[1].empty() ? peakMap0 : mPeakMaps[1].data();

	while(n--) {
		float x1 = x0 + dx;
		sint32 ix0 = VDCeilToInt(x0 - 0.5f);
		sint32 ix1 = VDCeilToInt(x1 - 0.5f);
		x0 = x1;

		if ((uint32)ix0 >= m)
			ix0 = (ix0 < 0) ? 0 : (sint32)(m-1);

		if ((uint32)ix1 >= m)
			ix1 = (sint32)m;

		ix0 *= 2;
		ix1 *= 2;

		uint8 minR = peakMap0[ix0];
		uint8 maxR = peakMap0[ix0+1];
		uint8 minL = peakMap1[ix0];
		uint8 maxL = peakMap1[ix0+1];

		for(sint32 ix = ix0 + 2; ix < ix1; ix += 2) {
			const uint8 vMinR = peakMap0[ix];
			const uint8 vMaxR = peakMap0[ix+1];
			const uint8 vMinL = peakMap1[ix];
			const uint8 vMaxL = peakMap1[ix+1];

			if (minR > vMinR) minR = vMinR;
			if (maxR < vMaxR) maxR = vMaxR;
			if (minL > vMinL) minL = vMinL;
			if (maxL < vMaxL) maxL = vMaxL;
		}

		*data++  = ((float)minR - 128.0f) / 127.0f;
		*data++  = ((float)maxR - 128.0f) / 127.0f;
		*audio++ = ((float)minL - 128.0f) / 127.0f;
		*audio++ = ((float)maxL - 128.0f) / 127.0f;
	}
}

void ATCassetteImage::AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const {
	if (!n)
		return;

	uint32 i = 0;
	uint32 j = mAudioBlockCount;

	const SortedBlock *p;
	for(;;) {
		uint32 mid = (i + j) >> 1;
		p = &mAudioBlocks[mid];

		if (i + 1 >= j) {
			if (!p->mpImageBlock)
				return;

			break;
		}

		if (posSample < p->mStart)
			j = mid;
		else
			i = mid;
	}
	
	const uint32 posSample0 = posSample;
	const uint32 posCycle0 = posCycle;

	VDASSERT(posSample >= p->mStart);
	VDASSERT(!p->mpImageBlock || posSample <= p[1].mStart);

	while(n && p->mpImageBlock) {
		const uint32 audioSampleLimit = ((p[1].mStart - posSample) * kATCassetteCyclesPerAudioSample - posCycle + kATCyclesPerSyncSample - 1) / kATCyclesPerSyncSample;

		posSample -= p->mStart;
		posSample += p->mOffset;
		VDASSERT(posSample < UINT32_C(0x80000000));

		// check if we need to clip
		uint32 tc = n;
		if (tc > audioSampleLimit)
			tc = audioSampleLimit;

		n -= p->mpImageBlock->AccumulateAudio(dst, posSample, posCycle, tc);

		posSample -= p->mOffset;
		posSample += p->mStart;
		++p;

		VDASSERT(!n || posSample >= p->mStart);
	}
}

uint32 ATCassetteImage::GetWriteCursor() const {
	return mWriteCursor;
}

void ATCassetteImage::SetWriteCursor(uint32 pos) {
	if (pos > kATCassetteDataLimit)
		pos = kATCassetteDataLimit;

	mWriteCursor = pos;
	mCurrentWriteBlockIndex = -1;
}

void ATCassetteImage::WriteBlankData(uint32 len) {
	if (!len)
		return;

	// check if write would go beyond end and clamp
	if (mWriteCursor >= kATCassetteDataLimit)
		return;

	if (len > kATCassetteDataLimit - mWriteCursor)
		len = kATCassetteDataLimit - mWriteCursor;

	// if write cursor is beyond end, extend start to current end
	if (mWriteCursor > mDataLength) {
		const uint32 extendLen = mWriteCursor - mDataLength;

		mWriteCursor = mDataLength;
		len += extendLen;
	}

	AT_CASSETTE_VALIDATE();

	// check if we have a prev block
	if (mCurrentWriteBlockIndex < 0) {
		if (mWriteCursor)
			mCurrentWriteBlockIndex = (sint32)GetSortedDataBlock(mWriteCursor - 1);
		else
			mCurrentWriteBlockIndex = 0;
	}

	// check if the current write block is compatible
	if (!mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock || mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock->GetBlockType() != kATCassetteImageBlockType_Blank) {
		// split it and insert a new blank block 
		mCurrentWriteBlockIndex = SplitBlock(mCurrentWriteBlockIndex, mWriteCursor);

		if (!mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock || mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock->GetBlockType() != kATCassetteImageBlockType_Blank) {
			auto *emptyBlock = new ATCassetteImageBlockBlank;
			auto emptyBlockHolder = vdmakeautoptr(emptyBlock);

			mImageBlocks.push_back(emptyBlock);
			emptyBlockHolder.release();

			// insert null block at start
			mDataBlocks.insert(mDataBlocks.begin() + mCurrentWriteBlockIndex, SortedBlock { mWriteCursor, 0, emptyBlock });
			++mDataBlockCount;
		}
	}

	AT_CASSETTE_VALIDATE();

	VDASSERT(mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock->GetBlockType() == kATCassetteImageBlockType_Blank);

	// advance write cursor
	if (mPeakDirtyStart > mWriteCursor)
		mPeakDirtyStart = mWriteCursor;

	mWriteCursor += len;

	if (mPeakDirtyEnd < mWriteCursor)
		mPeakDirtyEnd = mWriteCursor;

	// truncate valid overlapping blocks
	const uint32 nextIndex = mCurrentWriteBlockIndex + 1;
	for(;;) {
		auto& nextBlock = mDataBlocks[nextIndex];

		// stop if the next block no longer overlaps the write range
		if (nextBlock.mStart >= mWriteCursor)
			break;

		// stop if the next block is the sentinel
		if (!nextBlock.mpImageBlock) {
			// extend the end of tape and stop
			nextBlock.mStart = mWriteCursor;
			mDataLength = mWriteCursor;
			break;
		}

		// check if the next block is also blank -- if so, we can trivially merge with it
		if (nextBlock.mpImageBlock->GetBlockType() != kATCassetteImageBlockType_Blank) {
			// check if the next block is entirely contained in the write
			// range
			auto& nextNextBlock = mDataBlocks[nextIndex + 1];

			if (nextNextBlock.mStart > mWriteCursor) {
				// no -- check if it is compatible
				if (nextBlock.mpImageBlock->GetBlockType() != kATCassetteImageBlockType_Blank) {
					// no -- truncate and stop
					const uint32 truncOffset = mWriteCursor - nextBlock.mStart;

					nextBlock.mOffset += truncOffset;
					nextBlock.mStart = mWriteCursor;
					break;
				}
			}

			// yes -- fall through to delete the existing block (merge)
		}

		// yes -- delete the next block and continue
		mDataBlocks.erase(mDataBlocks.begin() + nextIndex);
		--mDataBlockCount;
	}

	AT_CASSETTE_VALIDATE();
}

void ATCassetteImage::WriteStdData(uint8 byte, uint32 baudRate) {
	if (!baudRate)
		return;

	// if write cursor is beyond end, insert an intermediate blank area
	if (mWriteCursor > mDataLength) {
		const uint32 extendLen = mWriteCursor - mDataLength;

		WriteBlankData(extendLen);
	}

	// check if we would go beyond end (with suitable buffer)
	if (mWriteCursor >= kATCassetteDataLimit || kATCassetteDataLimit - mWriteCursor < kATCassetteDataWriteByteBuffer)
		return;

	AT_CASSETTE_VALIDATE();

	// check if we have a prev block
	if (mCurrentWriteBlockIndex < 0) {
		if (mWriteCursor) {
			mCurrentWriteBlockIndex = (sint32)GetSortedDataBlock(mWriteCursor - 1);

			auto& prevBlock = mDataBlocks[mCurrentWriteBlockIndex];

			// split the previous block, even if it's a standard data block
			++mCurrentWriteBlockIndex;

			auto *emptyBlock = new ATCassetteImageDataBlockStd;
			auto emptyBlockHolder = vdmakeautoptr(emptyBlock);
			
			emptyBlock->Init(baudRate);

			mImageBlocks.push_back(emptyBlock);
			emptyBlockHolder.release();

			const SortedBlock newBlocks[] = {
				{ mWriteCursor, 0, emptyBlock },
				{ mWriteCursor, mWriteCursor - prevBlock.mStart, prevBlock.mpImageBlock}
			};

			mDataBlocks.insert(mDataBlocks.begin() + mCurrentWriteBlockIndex,
				std::begin(newBlocks), std::end(newBlocks));

			mDataBlockCount += 2;
		} else {
			// insert new block at start
			mCurrentWriteBlockIndex = 0;

			auto *emptyBlock = new ATCassetteImageDataBlockStd;
			auto emptyBlockHolder = vdmakeautoptr(emptyBlock);

			emptyBlock->Init(baudRate);

			mImageBlocks.push_back(emptyBlock);
			emptyBlockHolder.release();

			mDataBlocks.insert(mDataBlocks.begin(),
				SortedBlock { 0, 0, emptyBlock } );

			++mDataBlockCount;
		}
	} else {
		// we have an existing block -- check if the baud rate is compatible
		auto *curBlock = mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock;

		if (curBlock->GetBlockType() != kATCassetteImageBlockType_Std
			|| static_cast<ATCassetteImageDataBlockStd *>(curBlock)->GetBaudRate() != baudRate)
		{
			// not compatible -- split the existing block if necessary, then insert a new block
			mCurrentWriteBlockIndex = SplitBlock(mCurrentWriteBlockIndex, mWriteCursor);

			VDASSERT(mDataBlocks[mCurrentWriteBlockIndex].mStart == mWriteCursor);

			auto *emptyBlock = new ATCassetteImageDataBlockStd;
			auto emptyBlockHolder = vdmakeautoptr(emptyBlock);

			emptyBlock->Init(baudRate);

			mImageBlocks.push_back(emptyBlock);
			emptyBlockHolder.release();

			mDataBlocks.insert(mDataBlocks.begin() + mCurrentWriteBlockIndex,
				SortedBlock { mWriteCursor, 0, emptyBlock } );

			++mDataBlockCount;
		}
	}

	// add the new data
	auto *dataBlockPtr = mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock;
	VDASSERT(dataBlockPtr->GetBlockType() == kATCassetteImageBlockType_Std);

	auto *dataBlock = static_cast<ATCassetteImageDataBlockStd *>(dataBlockPtr);

	VDASSERT(mDataBlocks[mCurrentWriteBlockIndex].mOffset == 0);
	VDASSERT(dataBlock->GetBlockType() == kATCassetteImageBlockType_Std);
	dataBlock->AddData(&byte, 1);

	// advance write cursor
	const uint32 newWriteCursor = mDataBlocks[mCurrentWriteBlockIndex].mStart + dataBlock->GetDataSampleCount();
	VDASSERT(mWriteCursor <= newWriteCursor);

	if (mPeakDirtyStart > mWriteCursor)
		mPeakDirtyStart = mWriteCursor;

	if (mPeakDirtyEnd < newWriteCursor)
		mPeakDirtyEnd = newWriteCursor;

	mWriteCursor = newWriteCursor;

	// truncate valid overlapping blocks
	const uint32 nextIndex = mCurrentWriteBlockIndex + 1;
	for(;;) {
		auto& nextBlock = mDataBlocks[nextIndex];

		// stop if the next block no longer overlaps the write range
		if (nextBlock.mStart >= mWriteCursor)
			break;

		// stop if the next block is the sentinel
		if (!nextBlock.mpImageBlock) {
			// extend end of tape and stop
			nextBlock.mStart = mWriteCursor;
			mDataLength = mWriteCursor;
			break;
		}

		// check if the next block is entirely contained in the write
		// range
		auto& nextNextBlock = mDataBlocks[nextIndex + 1];

		if (nextNextBlock.mStart > mWriteCursor) {
			// no -- truncate and stop, even if it is also a data block
			const uint32 truncOffset = nextNextBlock.mStart - mWriteCursor;

			nextBlock.mOffset += truncOffset;
			nextBlock.mStart = mWriteCursor;
			break;
		}

		// yes -- delete the next block and continue
		mDataBlocks.erase(mDataBlocks.begin() + nextIndex);
		--mDataBlockCount;
	}

	AT_CASSETTE_VALIDATE();
}

void ATCassetteImage::WriteFSKPulse(bool polarity, uint32 samples) {
	// if write cursor is beyond end, insert an intermediate blank area
	if (mWriteCursor > mDataLength) {
		const uint32 extendLen = mWriteCursor - mDataLength;

		WriteBlankData(extendLen);
	}

	// check if we have a prev block
	if (mCurrentWriteBlockIndex < 0) {
		if (mWriteCursor) {
			mCurrentWriteBlockIndex = (sint32)GetSortedDataBlock(mWriteCursor - 1);

			auto& prevBlock = mDataBlocks[mCurrentWriteBlockIndex];

			// split the previous block, even if it's an FSK data block
			++mCurrentWriteBlockIndex;

			auto *emptyBlock = new ATCassetteImageBlockRawData;
			auto emptyBlockHolder = vdmakeautoptr(emptyBlock);

			mImageBlocks.push_back(emptyBlock);
			emptyBlockHolder.release();

			const SortedBlock newBlocks[] = {
				{ mWriteCursor, 0, emptyBlock },
				{ mWriteCursor, mWriteCursor - prevBlock.mStart, prevBlock.mpImageBlock}
			};

			mDataBlocks.insert(mDataBlocks.begin() + mCurrentWriteBlockIndex,
				std::begin(newBlocks), std::end(newBlocks));

			mDataBlockCount += 2;
		} else {
			// insert new block at start
			mCurrentWriteBlockIndex = 0;

			auto *emptyBlock = new ATCassetteImageBlockRawData;
			auto emptyBlockHolder = vdmakeautoptr(emptyBlock);

			mImageBlocks.push_back(emptyBlock);
			emptyBlockHolder.release();

			mDataBlocks.insert(mDataBlocks.begin(),
				SortedBlock { 0, 0, emptyBlock } );

			++mDataBlockCount;
		}
	}

	// add the new data
	auto *fskBlock = static_cast<ATCassetteImageBlockRawData *>(mDataBlocks[mCurrentWriteBlockIndex].mpImageBlock);

	VDASSERT(mDataBlocks[mCurrentWriteBlockIndex].mOffset == 0);
	VDASSERT(fskBlock->GetBlockType() == kATCassetteImageBlockType_FSK);
	fskBlock->AddFSKPulseSamples(polarity, samples);

	// advance write cursor
	mWriteCursor = mDataBlocks[mCurrentWriteBlockIndex].mStart + fskBlock->GetDataSampleCount();

	// truncate valid overlapping blocks
	const uint32 nextIndex = mCurrentWriteBlockIndex + 1;
	for(;;) {
		auto& nextBlock = mDataBlocks[nextIndex];

		// stop if the next block no longer overlaps the write range
		if (nextBlock.mStart >= mWriteCursor)
			break;

		// stop if the next block is the sentinel
		if (!nextBlock.mpImageBlock) {
			// extend end of tape and stop
			nextBlock.mStart = mWriteCursor;
			mDataLength = mWriteCursor;
			break;
		}

		// check if the next block is entirely contained in the write
		// range
		auto& nextNextBlock = mDataBlocks[nextIndex + 1];

		if (nextNextBlock.mStart < mWriteCursor) {
			// no -- check if it is compatible
			if (nextBlock.mpImageBlock->GetBlockType() != kATCassetteImageBlockType_Blank) {
				// no -- truncate and stop
				const uint32 truncOffset = nextNextBlock.mStart - mWriteCursor;

				nextBlock.mOffset += truncOffset;
				nextBlock.mStart = mWriteCursor;
				break;
			}

			// yes -- fall through to delete the existing block (merge)
		}

		// yes -- delete the next block and continue
		mDataBlocks.erase(mDataBlocks.begin() + nextIndex);
		--mDataBlockCount;
	}
}

void ATCassetteImage::InitNew() {
	mCachedDataBlockIndex = 0;
	mWriteCursor = 0;
	mCurrentWriteBlockIndex = -1;
	mDataLength = 0;
	mAudioLength = 0;

	mDataBlocks.assign( { SortedBlock { 0, 0, nullptr } });
	mDataBlockCount = 0;

	mAudioBlocks.assign({ SortedBlock { 0, 0, nullptr } });
	mAudioBlockCount = 1;
}

void ATCassetteImage::Load(IVDRandomAccessStream& file, IVDRandomAccessStream *afile) {
	uint32 basehdr;
	if (file.ReadData(&basehdr, 4) != 4)
		basehdr = 0;

	file.Seek(0);

	mCachedDataBlockIndex = 0;
	mWriteCursor = 0;
	mCurrentWriteBlockIndex = -1;

	uint32 baseid = VDFromLE32(basehdr);
	if (baseid == VDMAKEFOURCC('R', 'I', 'F', 'F'))
		return ParseWAVE(file, afile);

	if (afile)
		throw MyError("Cannot write analysis file for this cassette format.");

	else if (baseid == VDMAKEFOURCC('F', 'U', 'J', 'I'))
		ParseCAS(file);
	else
		throw MyError("%ls is not in a recognizable Atari cassette format.", file.GetNameForError());
}

void ATCassetteImage::SaveCAS(IVDRandomAccessStream& file) {
	VDBufferedWriteStream ws(&file, 65536);

	// write header
	const uint32 kFUJI = VDMAKEFOURCC('F', 'U', 'J', 'I');
	const uint32 fujiSize = 0;

	ws.Write(&kFUJI, 4);
	ws.Write(&fujiSize, 4);

	// iterate down the blocks
	uint32 pos = 0;
	uint32 lastBaudRate = 0;

	vdfastvector<uint32> pulses32;
	vdfastvector<uint16> pulses16;

	for(const SortedBlock& blk : mDataBlocks) {
		if (!blk.mpImageBlock)
			continue;

		switch(blk.mpImageBlock->GetBlockType()) {
			case kATCassetteImageBlockType_Blank:
			case kATCassetteImageBlockType_RawAudio:
				// skip these block types
				break;

			case kATCassetteImageBlockType_FSK:
				{
					const auto& fsk = *static_cast<ATCassetteImageBlockRawData *>(blk.mpImageBlock);
					uint32 gapMS = 0;

					if (blk.mStart > pos)
						gapMS = VDRoundToInt32((double)(blk.mStart - pos) * 1000.0 / kATCassetteDataSampleRate);

					uint8 header[8] = {
						(uint8)'f',
						(uint8)'s',
						(uint8)'k',
						(uint8)' ',
						0,
						0,
						0xFF,
						0xFF,
					};

					while(gapMS > 65535) {
						ws.Write(header, 8);
						gapMS -= 65535;
					}

					// convert raw data to pulse widths
					pulses32.clear();
					fsk.ExtractPulses(pulses32, false);

					// Resample pulse widths from data sample rate to 100us and convert pulses to 16-bit.
					// The first pulse width is for 0, which ExtractPulses() has guaranteed; however, this
					// also means that the first pulse width may *be* 0. Since our data sample rate is now
					// also higher than 10KHz, we have to deal with the possibility of a runt pulse.
					pulses16.clear();
					pulses16.reserve(pulses32.size());

					uint32 pulsePosSrcRate = 0;
					uint32 pulsePosDstRate = 0;
					bool canAppend = true;

					pulses16.push_back(0);
					for(uint32 pulseLen : pulses32) {
						// compute absolute time of next transition at source rate
						pulsePosSrcRate += pulseLen;

						// convert transition time to destination rate
						const uint32 pulsePosDstRate2 = (uint32)(0.5 + (double)pulsePosSrcRate * (10000.0 / (double)kATCassetteDataSampleRate));

						// compute length of pulse in destination rate from last edge
						VDASSERT(pulsePosDstRate2 >= pulsePosDstRate);
						uint32 pulseLenDstRate = pulsePosDstRate2 - pulsePosDstRate;
						pulsePosDstRate = pulsePosDstRate2;

						// If we can append to the previous sample because it's the same polarity, do so
						// up to the limit of 0xFFFF counts.
						if (canAppend && pulseLenDstRate > 0) {
							uint32 maxAppend = 0xFFFF - pulses16.back();
							if (maxAppend > pulseLenDstRate)
								maxAppend = pulseLenDstRate;

							pulseLenDstRate -= maxAppend;
							pulses16.back() += maxAppend;
						}

						// If we still have counts left, write out samples. There is a complication if
						// we have a period of 0xFFFF samples or more, because we must write out zero
						// periods of the opposite polarity. If our period is *exactly* a multiple of
						// 0xFFFF, we must refrain from writing out two zeroes in a row; instead, we
						// only write one and set the can-append flag.
						if (pulseLenDstRate > 0) {
							if (canAppend) {
								// Last sample was from the same polarity, so we must write a zero in
								// between. (This can only happen if we'd already pushed some counts
								// into that sample and hit the 0xFFFF limit.)
								pulses16.push_back(0);
								canAppend = false;
							}

							while(pulseLenDstRate > 0xFFFF) {
								pulseLenDstRate -= 0xFFFF;

								pulses16.push_back(0xFFFF);
								pulses16.push_back(0);
							}

							if (pulseLenDstRate > 0) {
								pulses16.push_back((uint16)pulseLenDstRate);

								// Next sample will be of different polarity, so it cannot append (already
								// cleared above).
							} else {
								canAppend = true;
							}
						} else {
							canAppend = !canAppend;
						}
					}

					while(!pulses16.empty() && pulses16.back() == 0)
						pulses16.pop_back();

					// stream out the pulses
					const uint16 *pulseSrc = pulses16.data();
					uint32 pulsesLeft = (uint32)pulses16.size();

					while(pulsesLeft) {
						// We can write a maximum of 65535 bytes, or 32767 pulses. However,
						// that's annoying because it repeats the 0 bit pulse, so we only
						// write 32766 pulses instead.
						uint32 pulsesToWrite = std::min<uint32>(pulsesLeft, 0x7FFE);

						VDWriteUnalignedLEU16(header + 4, pulsesToWrite * 2);
						VDWriteUnalignedLEU16(header + 6, gapMS);

						ws.Write(header, 8);
						ws.Write(pulseSrc, pulsesToWrite * 2);

						pulseSrc += pulsesToWrite;
						pulsesLeft -= pulsesToWrite;
						gapMS = 0;
					}

					pos = blk.mStart + fsk.GetDataSampleCount();
				}
				break;

			case kATCassetteImageBlockType_Std:
				{
					const auto& std = *static_cast<ATCassetteImageDataBlockStd *>(blk.mpImageBlock);
					const uint32 baudRate = std.GetBaudRate();

					if (lastBaudRate != baudRate) {
						lastBaudRate = baudRate;

						uint8 baudRateHeader[8] = {
							(uint8)'b',
							(uint8)'a',
							(uint8)'u',
							(uint8)'d',
						};

						VDWriteUnalignedLEU16(baudRateHeader + 6, (uint16)baudRate);

						ws.Write(baudRateHeader, 8);
					}

					uint32 gapMS = 0;

					if (blk.mStart > pos)
						gapMS = VDRoundToInt32((double)(blk.mStart - pos) * 1000.0 / kATCassetteDataSampleRate);

					uint8 header[8] = {
						(uint8)'d',
						(uint8)'a',
						(uint8)'t',
						(uint8)'a',
						0,
						0,
						0xFF,
						0xFF,
					};

					while(gapMS > 65535) {
						gapMS -= 65535;
						ws.Write(header, 8);
					}

					uint32 dataLen = std.GetDataLen();
					const uint8 *data = std.GetData();

					while(dataLen > 0) {
						uint32 tc = dataLen > 65535 ? 65535 : dataLen;

						VDWriteUnalignedLEU16(header + 4, tc);
						VDWriteUnalignedLEU16(header + 6, gapMS);

						ws.Write(header, 8);
						ws.Write(data, tc);

						dataLen -= tc;
						data += tc;

						gapMS = 0;
					}

					pos = blk.mStart + std.GetDataSampleCount();
				}
				break;
		}
	}

	ws.Flush();
}

void ATCassetteImage::SaveWAV(IVDRandomAccessStream& file) {
	// initialize WAV header
	static constexpr uint8 kHeader[]={
		(uint8)'R', (uint8)'I', (uint8)'F', (uint8)'F',
		0, 0, 0, 0,
		(uint8)'W', (uint8)'A', (uint8)'V', (uint8)'E',

		(uint8)'f', (uint8)'m', (uint8)'t', (uint8)' ',
		0x10, 0x00, 0x00, 0x00,
		0x01, 0x00,					// PCMWAVEFORMAT
		0x01, 0x00,					// 1 channel
		0x44, 0xAC, 0x00, 0x00,		// 44100Hz
		0x44, 0xAC, 0x00, 0x00,		// 44100 bytes/sec
		0x01, 0x00,					// 1 byte/sample
		0x08, 0x00,					// 8 bits/sample

		(uint8)'d', (uint8)'a', (uint8)'t', (uint8)'a',
		0, 0, 0, 0,
	};

	uint8 header[vdcountof(kHeader)];
	memcpy(header, kHeader, sizeof header);
	file.Write(header, sizeof header);

	// precompute sine table
	uint8 sineTable[1024];

	for(int i=0; i<1024; ++i) {
		// export at ~80% amplitude
		float v = sinf((float)i * (nsVDMath::kfTwoPi / 1024.0f));
		sineTable[i] = (uint8)(128.5f + 100.0f * v);
	}

	// preallocate buffer
	static constexpr uint32 kBlockSize = 65536;
	vdblock<uint8> buf(kBlockSize);

	// compute source step in 32.32x
	static constexpr uint64 kSourceStepX32 = (uint64)(0.5f + 4294967296.0 * (kATCassetteDataSampleRate / 44100.0));
	uint64 sourcePosX32 = 0;

	// process data in blocks
	static constexpr uint32 kPhaseInc0 = (uint32)(uint64)(0.5 + 3995.0 / kATCassetteDataSampleRate * 4294967296.0);
	static constexpr uint32 kPhaseInc1 = (uint32)(uint64)(0.5 + 5326.7 / kATCassetteDataSampleRate * 4294967296.0);
	uint32 phaseAccum = 0;
	uint32 pos = 0;
	uint32 len = ((uint64)mDataLength << 32) / kSourceStepX32;
	bool currentBit = true;
	uint32 currentPhaseInc = kPhaseInc1;

	while(pos < len) {
		uint32 tc = len - pos;
		if (tc > kBlockSize)
			tc = kBlockSize;

		for(uint32 i = 0; i < tc; ++i) {
			const uint64 sampleStartX32 = sourcePosX32;
			const uint64 sampleEndX32 = sourcePosX32 + kSourceStepX32;
			const uint32 sampleStart = (uint32)(sampleStartX32 >> 32);
			const uint32 sampleEnd = (uint32)((sampleEndX32 - 1) >> 32);
			sourcePosX32 = sampleEndX32;

			static_assert(kSourceStepX32 < (UINT64_C(1) << 32), "Invalid assumption: source step in [0, 1)");

			if (sampleStart == sampleEnd) {
				phaseAccum += (uint32)(((uint64)currentPhaseInc * (sampleEndX32 - sampleStartX32)) >> 32);
			} else {
				phaseAccum += (uint32)(((uint64)currentPhaseInc * (uint32)(0U - sampleStartX32)) >> 32);
				currentBit = GetBit(sampleStart, 8, 3, currentBit, false);
				currentPhaseInc = currentBit ? kPhaseInc1 : kPhaseInc0;
				phaseAccum += (uint32)(((uint64)currentPhaseInc * (uint32)(sampleEndX32)) >> 32);
			}

			const uint32 samplePhase = (uint32)(phaseAccum + (currentPhaseInc >> 1));
			buf[i] = sineTable[samplePhase >> 22];
		}

		file.Write(buf.data(), tc);
		pos += tc;
	}

	const uint64 endPos = file.Pos();
	VDWriteUnalignedLEU32(header + 4, VDClampToUint32(endPos - 8));
	VDWriteUnalignedLEU32(header + sizeof(header) - 4, VDClampToUint32(endPos - (uint32)sizeof(header)));
	file.Seek(0);
	file.Write(header, sizeof header);
}

uint32 ATCassetteImage::GetSortedDataBlock(uint32 pos) const {
	uint32 i = 0;
	uint32 j = mDataBlockCount;

	if (pos < mDataBlocks[mCachedDataBlockIndex].mStart)
		j = mCachedDataBlockIndex;
	else if (pos >= mDataBlocks[mCachedDataBlockIndex + 1].mStart)
		i = mCachedDataBlockIndex + 1;
	else
		return mCachedDataBlockIndex;

	for(;;) {
		uint32 mid = (i + j) >> 1;
		const SortedBlock *p = &mDataBlocks[mid];

		if (i + 1 >= j) {
			if (p->mpImageBlock)
				mCachedDataBlockIndex = mid;

			return mid;
		}

		if (pos < p->mStart)
			j = mid;
		else
			i = mid;
	}
}

uint32 ATCassetteImage::SplitBlock(uint32 startBlockIdx, uint32 splitPt) {
	VDASSERT(mDataBlocks.size() == mDataBlockCount + 1);
	VDASSERT(startBlockIdx <= mDataBlockCount);

	const uint32 pos1 = mDataBlocks[startBlockIdx].mStart;

	VDASSERT(splitPt >= pos1);

	if (splitPt == pos1)
		return startBlockIdx;

	const uint32 pos2 = mDataBlocks[startBlockIdx + 1].mStart;
	VDASSERT(splitPt <= pos2);

	if (splitPt != pos2) {
		VDASSERT(startBlockIdx < mDataBlockCount);

		SortedBlock newBlock = mDataBlocks[startBlockIdx];

		newBlock.mStart = splitPt;
		newBlock.mOffset = splitPt - pos1;

		mDataBlocks.insert(mDataBlocks.begin() + startBlockIdx + 1, newBlock);
		++mDataBlockCount;

		AT_CASSETTE_VALIDATE();
	}
	
	return startBlockIdx + 1;
}

void ATCassetteImage::Validate() {
	VDASSERT(mDataBlocks.size() == mDataBlockCount + 1);
	VDASSERT(!mDataBlocks.back().mpImageBlock);
	VDASSERT(mDataLength == mDataBlocks.back().mStart);

	VDASSERT(std::is_sorted(mDataBlocks.begin(), mDataBlocks.end(),
		[](const SortedBlock& a, const SortedBlock& b) { return a.mStart < b.mStart; }));
}

namespace {
	class PeakMapProcessor {
	public:
		PeakMapProcessor(bool stereo, double inputSamplesPerPeakSample, vdfastvector<uint8>& peakMapL, vdfastvector<uint8>& peakMapR);

		void Process(const sint16 *samples, uint32 n);

	private:
		bool mbStereo = false;
		sint32 mValAccumL0 = 0;
		sint32 mValAccumL1 = 0;
		sint32 mValAccumR0 = 0;
		sint32 mValAccumR1 = 0;
		uint64 mRateAccum = UINT32_C(0x80000000);
		uint64 mRateAccumInc = 0;

		uint32 mInputSamplesLeft = 0;

		vdfastvector<uint8>& mPeakMapL;
		vdfastvector<uint8>& mPeakMapR;
	};

	PeakMapProcessor::PeakMapProcessor(bool stereo, double inputSamplesPerPeakSample, vdfastvector<uint8>& peakMapL, vdfastvector<uint8>& peakMapR)
		: mbStereo(stereo)
		, mRateAccumInc((uint64)VDRoundToInt64(inputSamplesPerPeakSample * 4294967296.0))
		, mPeakMapL(peakMapL)
		, mPeakMapR(peakMapR)
	{
		mRateAccum += mRateAccumInc;
		mInputSamplesLeft = (uint32)(mRateAccum >> 32);
		mRateAccum = (uint32)mRateAccum;
	}

	void PeakMapProcessor::Process(const sint16 *samples, uint32 n) {
		uint32 peakSamples = 0;
		while(n) {
			while(mInputSamplesLeft == 0) {
				mRateAccum += mRateAccumInc;
				mInputSamplesLeft = (uint32)(mRateAccum >> 32);
				mRateAccum = (uint32)mRateAccum;

				static constexpr float scale = 1.0f / 32767.0f * 127.0f / 255.0f;

				const uint8 vR0 = VDClampedRoundFixedToUint8Fast(mValAccumR0 * scale + 128.0f / 255.0f);
				const uint8 vR1 = VDClampedRoundFixedToUint8Fast(mValAccumR1 * scale + 128.0f / 255.0f);
				mPeakMapR.push_back(vR0);
				mPeakMapR.push_back(vR1);
				mValAccumR0 = 0;
				mValAccumR1 = 0;

				if (mbStereo) {
					const uint8 vL0 = VDClampedRoundFixedToUint8Fast(mValAccumL0 * scale + 128.0f / 255.0f);
					const uint8 vL1 = VDClampedRoundFixedToUint8Fast(mValAccumL1 * scale + 128.0f / 255.0f);
					mPeakMapL.push_back(vL0);
					mPeakMapL.push_back(vL1);
					mValAccumL0 = 0;
					mValAccumL1 = 0;
				}
			}

			// accumulate peak map samples
			uint32 toScan = n;
			if (toScan > mInputSamplesLeft)
				toScan = mInputSamplesLeft;

			minMax16x2(samples, toScan, mValAccumL0, mValAccumL1, mValAccumR0, mValAccumR1);

			mInputSamplesLeft -= toScan;

			n -= toScan;
			samples += toScan*2;
		}
	}

	uint32 *ExtendBitfield(vdfastvector<uint32>& dstv, uint32 offset, uint32 n) {
		if (!n)
			return nullptr;

		const uint32 wordsToAdd = (((offset - 1) & 31) + n) >> 5;

		if (wordsToAdd) {
			// vector::resize() does not have an amortization guarantee and our impl definitely
			// doesn't have it, so force it.
			const size_t curSize = dstv.size();
			const size_t curCapacity = dstv.capacity();

			if (curCapacity - curSize < wordsToAdd)
				dstv.reserve(std::max<size_t>(curCapacity + (curCapacity >> 1), curSize + wordsToAdd));

			dstv.resize(curSize + wordsToAdd, 0);
		}

		return &*(dstv.end() - wordsToAdd - (offset ? 1 : 0));
	}
}

void ATCassetteImage::ParseWAVE(IVDRandomAccessStream& file, IVDRandomAccessStream *afile) {
	WaveFormatEx wf = {0};
	sint64 limit = file.Length();
	sint64 datapos = -1;
	uint32 datalen = 0;

	ATProgress progress;

	for(;;) {
		uint32 hdr[2];

		if (file.Pos() >= limit)
			break;

		if (8 != file.ReadData(hdr, 8))
			break;

		uint32 fcc = hdr[0];
		uint32 len = VDFromLE32(hdr[1]);

		switch(fcc) {
		case VDMAKEFOURCC('R', 'I', 'F', 'F'):
			limit = file.Pos() + len;
			if (len < 4)
				throw MyError("'%ls' is an invalid WAV file.", file.GetNameForError());

			file.Read(hdr, 4);
			if (hdr[0] != VDMAKEFOURCC('W', 'A', 'V', 'E'))
				throw MyError("'%ls' is not a WAV file.", file.GetNameForError());

			len = 0;
			break;

		case VDMAKEFOURCC('f', 'm', 't', ' '):
			{
				uint32 toread = std::min<uint32>(sizeof(wf), len);

				file.Read(&wf, toread);
				len -= toread;

				// validate format
				if (wf.mFormatTag != kWAVE_FORMAT_PCM
					|| (wf.mBitsPerSample != 8 && wf.mBitsPerSample != 16)
					|| (wf.mChannels != 1 && wf.mChannels != 2)
					|| (wf.mBlockAlign != wf.mBitsPerSample * wf.mChannels / 8)
					|| wf.GetSamplesPerSec() < 8000)
				{
					throw MyError("'%ls' uses an unsupported WAV format.", file.GetNameForError());
				}
			}
			break;

		case VDMAKEFOURCC('d', 'a', 't', 'a'):
			datapos = file.Pos();
			datalen = len;
			break;
		}

		if (len)
			file.Seek(file.Pos() + len + (len & 1));
	}

	if (!wf.mBlockAlign || datapos < 0)
		throw MyError("'%ls' is not a valid WAV file.", file.GetNameForError());

	// These are hard-coded into the 410 hardware.
	ATCassetteDecoderFSK	fskDecoder;
	ATCassetteDecoderDirect	directDecoder;

	uint64	resampAccum = 0;
	uint64	resampStep = VDRoundToInt64((double)wf.GetSamplesPerSec() / (double)kATCassetteImageAudioRate * 4294967296.0);

	sint16	inputBuffer[512][2] = {0};
	uint32	inputBufferLevel = 0;

	sint16	outputBuffer[4096][2] = {0};
	uint32	outputBufferIdx = 0;
	uint32	outputBufferLevel = 0;

	uint32	inputSamplesLeft = datalen / wf.mBlockAlign;

	static const uint8 kHeader[]={
		(uint8)'R', (uint8)'I', (uint8)'F', (uint8)'F',
		0, 0, 0, 0,
		(uint8)'W', (uint8)'A', (uint8)'V', (uint8)'E',

		(uint8)'f', (uint8)'m', (uint8)'t', (uint8)' ',
		0x28, 0x00, 0x00, 0x00,
		0xFE, 0xFF,					// WAVEFORMATEXTENSIBLE
		0x06, 0x00,					// 6 channels
		0xD9, 0x7C, 0x00, 0x00,		// 31961Hz
		0x58, 0xB4, 0x0B, 0x00,		// 767064 bytes/sec
		0x18, 0x00,					// 24 bytes/sample
		0x20, 0x00,					// 32 bits/sample
		0x16, 0x00,					// 22 bytes extra
		0x20, 0x00,					// 32 valid bits/sample
		0x3F, 0x00, 0x00, 0x00,		// channel mask
		0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71,	// KSDATAFORMAT_SUBTYPE_IEEE_FLOAT

		(uint8)'d', (uint8)'a', (uint8)'t', (uint8)'a',
		0, 0, 0, 0,
	};

	uint8 header[vdcountof(kHeader)];

	// The actual data rate we have is 31960.223Hz, but the closest we can put in the header
	// is 31960Hz. This difference is enough to cause noticeable drift when comparing the analysis
	// output against the original signal. To fix this, we specify 31961Hz and repeat samples
	// periodically. The ideal interval is 31960.223/(31961-31960.223); the fraction means we need to dither
	// the interval.
	constexpr uint64 kAnalysisRepeatIntervalF32 = (uint64)(0.5 + (double)(UINT64_C(1) << 32) * kATCassetteDataSampleRate / (31961.0 - kATCassetteDataSampleRate));
	uint32 analysisRepeatCounter = (uint32)((kAnalysisRepeatIntervalF32 + 0x80000000U) >> 32);
	uint64 analysisRepeatCounterFrac = (uint32)(kAnalysisRepeatIntervalF32 + 0x80000000U);	// yes, this truncates

	vdblock<float> analysisBuffer;
	vdautoptr<VDBufferedWriteStream> analysisWriteStream;
	if (afile) {
		analysisBuffer.resize(4096 * 6);
		analysisWriteStream = new VDBufferedWriteStream(afile, 65536);

		memcpy(header, kHeader, sizeof header);
		analysisWriteStream->Write(header, sizeof header);
	}

	file.Seek(datapos);

	mImageBlocks.resize(2, (ATCassetteImageBlock *)NULL);

	ATCassetteImageBlockRawData *pDataBlock = nullptr;
	mDataBlocks.clear();
	mDataBlockCount = 0;

	ATCassetteImageBlockRawAudio *pAudioBlock = new ATCassetteImageBlockRawAudio;
	mImageBlocks[1] = pAudioBlock;

	progress.InitF((uint32)((uint64)datalen >> 10), L"Processed %uK / %uK", L"Processing raw waveform");

	uint32 bitfieldOffset = 0;

	// We have a 12 sample delay on the FSK filter to deal with, by appending 12 input samples at
	// the end and discarding the first 12 filtered samples.
	const uint32 kFilterDelay = 12;
	bool outputTailAdded = false;
	uint32 filterDiscardLeft = kFilterDelay;

	PeakMapProcessor peakMapProcessor(wf.mChannels > 1, (double)wf.GetSamplesPerSec() / (double)kPeakSamplesPerSecond, mPeakMaps[1], mPeakMaps[0]);

	for(;;) {
		// check if we need to run the resampler
		if (outputBufferIdx >= outputBufferLevel) {
			uint32 toRead = 512 - inputBufferLevel;

			if (inputSamplesLeft) {
				// refill input buffer
				if (toRead > inputSamplesLeft)
					toRead = inputSamplesLeft;

				inputSamplesLeft -= toRead;

				if (wf.mBlockAlign == 1) {
					ReadMono8(inputBuffer[inputBufferLevel], file, toRead);
				} else if (wf.mBlockAlign == 2) {
					if (wf.mChannels == 1)
						ReadMono16(inputBuffer[inputBufferLevel], file, toRead);
					else
						ReadStereo8(inputBuffer[inputBufferLevel], file, toRead);
				} else if (wf.mBlockAlign == 4) {
					ReadStereo16(inputBuffer[inputBufferLevel], file, toRead);
				}
	
				// update progress UI
				progress.Update((uint32)((uint64)(file.Pos() - datapos) >> 10));

				// update peak samples
				peakMapProcessor.Process(&inputBuffer[inputBufferLevel][0], toRead);

				inputBufferLevel += toRead;
			}

			// move overlap tail to prefix portion of buffer
			const uint32 outputSamplesToPreserve = std::min<uint32>(outputBufferIdx, kFilterDelay);
			if (outputSamplesToPreserve)
				memmove(outputBuffer[kFilterDelay - outputSamplesToPreserve], outputBuffer[outputBufferLevel + kFilterDelay - outputSamplesToPreserve], outputSamplesToPreserve * sizeof(outputBuffer[0]));

			// compute how far we can run the resampler
			//
			// resampAccum + resampStep*(count - 1) < ((inputBufferLevel - 7) << 32)
			// count <= (((inputBufferLevel - 7) << 32) - resampAccum) / resampStep
			sint32 resampCount = (sint32)((sint64)(((uint64)(inputBufferLevel - 7) << 32) - resampAccum) / resampStep);

			if (!resampCount) {
				if (outputTailAdded)
					break;

				outputTailAdded = true;

				if (outputBufferIdx > 0) {
					// replicate last sample
					for(uint32 i=0; i<kFilterDelay; ++i) {
						outputBuffer[outputBufferIdx + i][0] = outputBuffer[outputBufferIdx - 1][0];
						outputBuffer[outputBufferIdx + i][1] = outputBuffer[outputBufferIdx - 1][1];
					}
				}
			} else {
				// clamp resampler output count if it exceeds output buffer
				resampCount = std::min<uint32>(resampCount, vdcountof(outputBuffer) - kFilterDelay);

				// run the resampler
				resampAccum = resample16x2(outputBuffer[kFilterDelay], inputBuffer[0], resampCount, resampAccum, resampStep);

				// shift down input buffer to remove samples we don't need anymore
				const uint32 shift = (uint32)(resampAccum >> 32);
				if (shift) {
					memmove(inputBuffer[0], inputBuffer[shift], (inputBufferLevel - shift)*sizeof(inputBuffer[0]));
					inputBufferLevel -= shift;
					resampAccum -= (uint64)shift << 32;
				}
			}

			outputBufferIdx = 0;
			outputBufferLevel = resampCount;
		}

		static_assert(kATCassetteAudioSamplesPerDataSample == 1, "Current decoder code requires 1:1 data samples.");

		if (!pDataBlock) {
			pDataBlock = new ATCassetteImageBlockRawData;
			mImageBlocks.push_back(pDataBlock);

			mDataBlocks.push_back(SortedBlock { mDataLength, 0, pDataBlock });
		}

		uint32 samplesToProcess = outputBufferLevel - outputBufferIdx;

		if (samplesToProcess) {
			bool discard = false;
			if (filterDiscardLeft) {
				if (samplesToProcess > filterDiscardLeft)
					samplesToProcess = filterDiscardLeft;

				filterDiscardLeft -= samplesToProcess;

				// run samples through the FSK filter only to start it out 12 samples ahead
				uint32 dummy = 0;
				fskDecoder.Process<false>(&outputBuffer[outputBufferIdx][1], samplesToProcess, &dummy, 0, nullptr);
			} else {
				// run the FSK decoder 12 samples ahead of the direct decoder
				uint32 *dstFSK = ExtendBitfield(pDataBlock->mDataFSK, bitfieldOffset, samplesToProcess);

				if (afile) {
					fskDecoder.Process<true>(&outputBuffer[outputBufferIdx + kFilterDelay][1], samplesToProcess, dstFSK, bitfieldOffset, analysisBuffer.data());
				} else
					fskDecoder.Process<false>(&outputBuffer[outputBufferIdx + kFilterDelay][1], samplesToProcess, dstFSK, bitfieldOffset, nullptr);

				// run the direct decoder 12 samples behind the FSK decoder
				uint32 *dstRaw = ExtendBitfield(pDataBlock->mDataRaw, bitfieldOffset, samplesToProcess);
				if (afile)
					directDecoder.Process<true>(&outputBuffer[outputBufferIdx][1], samplesToProcess, dstRaw, bitfieldOffset, analysisBuffer.data());
				else
					directDecoder.Process<false>(&outputBuffer[outputBufferIdx][1], samplesToProcess, dstRaw, bitfieldOffset, nullptr);

				if (afile) {
					const float *src = analysisBuffer.data();
					uint32 sampToWrite = samplesToProcess;

					while(sampToWrite) {
						uint32 chunkSize = std::min<uint32>(sampToWrite, analysisRepeatCounter);

						analysisWriteStream->Write(src, 24 * chunkSize);
						src += 6 * chunkSize;
						sampToWrite -= chunkSize;
						analysisRepeatCounter -= chunkSize;

						if (!analysisRepeatCounter) {
							analysisRepeatCounterFrac += kAnalysisRepeatIntervalF32;
							analysisRepeatCounter = (uint32)(analysisRepeatCounterFrac >> 32);
							analysisRepeatCounterFrac = (uint32)analysisRepeatCounterFrac;

							analysisWriteStream->Write(src - 6, 24);
						}
					}
				}

				bitfieldOffset = (bitfieldOffset + samplesToProcess) & 31;

				for(uint32 i = 0; i < samplesToProcess; ++i) {
					pAudioBlock->mAudio.push_back((outputBuffer[outputBufferIdx + i][0] >> 8) + 0x80);
				}
			}
		}

		outputBufferIdx += samplesToProcess;
	}

	// finalize data stream
	if (pDataBlock) {
		pDataBlock->mDataLength = ((uint32)pDataBlock->mDataFSK.size() << 5) + bitfieldOffset - (bitfieldOffset ? 32 : 0);

		mDataLength += pDataBlock->mDataLength;
	}

	mDataBlockCount = (uint32)mDataBlocks.size();
	mDataBlocks.push_back(SortedBlock { mDataLength });

	// finalize audio stream
	mAudioLength = pAudioBlock->mAudioLength = (uint32)pAudioBlock->mAudio.size();

	mAudioBlocks.resize(2);
	mAudioBlocks[0].mStart = 0;
	mAudioBlocks[0].mOffset = 0;
	mAudioBlocks[0].mpImageBlock = pAudioBlock;
	mAudioBlocks[1].mStart = mAudioLength;
	mAudioBlocks[1].mOffset = 0;
	mAudioBlocks[1].mpImageBlock = NULL;
	mAudioBlockCount = 1;

	if (afile) {
		analysisWriteStream->Flush();
		const uint64 endPos = analysisWriteStream->Pos();
		VDWriteUnalignedLEU32(header + 4, VDClampToUint32(endPos - 8));
		VDWriteUnalignedLEU32(header + sizeof(header) - 4, VDClampToUint32(endPos - (uint32)sizeof(header)));
		analysisWriteStream->Seek(0);
		analysisWriteStream->Write(header, sizeof header);
		analysisWriteStream->Flush();
	}
}

void ATCassetteImage::ParseCAS(IVDRandomAccessStream& file) {
	uint32 baudRate = 600;
	uint8 buf[128];

	ATProgress progress;
	progress.InitF((uint32)((uint64)file.Length() >> 10), L"Processing %uK of %uK", L"Processing CAS file");

	mDataLength = 0;

	vdfastvector<ATCassetteImageDataBlockStd *> dataBlocks;

	bool lastIsBlank = false;
	bool lastIsFSK = false;
	bool lastIsData = false;
	uint32 lastBaudRate = 0;

	auto addGap = [&,this](float seconds) {
		const uint32 samples = (uint32)(kATCassetteDataSampleRate * seconds);

		if (!samples)
			return;

		if (!lastIsBlank) {
			lastIsBlank = true;
			lastIsFSK = false;
			lastIsData = false;

			mImageBlocks.push_back(new ATCassetteImageBlockBlank);
			mDataBlocks.push_back({ mDataLength, 0, mImageBlocks.back() });
		}

		mDataLength += samples;
	};

	// enforce at least a 10 second mark tone at beginning of tape
	float minGap = 10.0f;

	for(;;) {
		progress.Update((uint32)((uint64)file.Pos() >> 10));

		struct {
			uint32 id;
			uint16 len;
			uint8 aux1;
			uint8 aux2;
		} hdr;

		if (file.ReadData(&hdr, 8) != 8)
			break;

		uint32 len = VDFromLE16(hdr.len);

		switch(hdr.id) {
			case VDMAKEFOURCC('F', 'U', 'J', 'I'):
				break;

			case VDMAKEFOURCC('b', 'a', 'u', 'd'):
				baudRate = hdr.aux1 + ((uint32)hdr.aux2 << 8);

				if (!baudRate)
					throw MyError("The cassette image contains an invalid baud rate in the data block at offset %lld.", file.Pos() - 8);

				break;

			case VDMAKEFOURCC('d', 'a', 't', 'a'):{
				// encode inter-record gap
				const sint32 gapms = hdr.aux1 + ((uint32)hdr.aux2 << 8);

				if (g_ATLCCasImage.IsEnabled()) {
					float pos = (float)mDataLength / (float)kATCassetteDataSampleRate;
					int mins = (int)(pos / 60.0f);
					float secs = pos - (float)mins * 60.0f;

					g_ATLCCasImage("Data block @ %3d:%06.3f: %ums gap, %u data bytes @ %u baud\n", mins, secs, gapms, len, baudRate);
				}

				addGap(std::max<float>(minGap, (float)gapms / 1000.0f));
				minGap = 0;

				// encode data bytes
				if (len > 0) {
					ATCassetteImageDataBlockStd *dataBlock = nullptr;
					
					if (lastIsData && lastBaudRate == baudRate)
						dataBlock = static_cast<ATCassetteImageDataBlockStd *>(mDataBlocks.back().mpImageBlock);

					if (!dataBlock) {
						dataBlock = new ATCassetteImageDataBlockStd;
						mImageBlocks.push_back(dataBlock);
						mDataBlocks.push_back({ mDataLength, 0, dataBlock });
						dataBlocks.push_back(dataBlock);
						dataBlock->Init(baudRate);

						lastBaudRate = baudRate;
						lastIsBlank = false;
						lastIsData = true;
						lastIsFSK = false;
					}

					while(len > 0) {
						uint32 tc = sizeof(buf);
						if (tc > len)
							tc = len;

						file.Read(buf, tc);

						dataBlock->AddData(buf, tc);

						len -= tc;
					}

					uint64 newDataLength = mDataBlocks.back().mStart + dataBlock->GetDataSampleCount64();

					if (newDataLength > kATCassetteDataLimit)
						throw ATCassetteTooLongException();

					mDataLength = (uint32)newDataLength;
				}
				break;
			}

			case VDMAKEFOURCC('f', 's', 'k', ' '):{
				// length must be even or chunk is malformed
				if (len & 1)
					throw MyError("Broken FSK chunk found at offset %lld.", file.Pos() - 8);

				const sint32 gapms = hdr.aux1 + ((uint32)hdr.aux2 << 8);

				// Don't use mingap for FSK -- we can't be sure that the early blocks are actual data instead of
				// noise, and we put more trust whoever decoded it when FSK is used.
				addGap((float)gapms / 1000.0f);
				minGap = 0;

				// encode FSK bits
				if (len > 0) {
					ATCassetteImageBlockRawData *fskBlock = nullptr;
					
					if (lastIsFSK)
						fskBlock = static_cast<ATCassetteImageBlockRawData *>(mDataBlocks.back().mpImageBlock);

					if (!fskBlock) {
						fskBlock = new ATCassetteImageBlockRawData;
						mImageBlocks.push_back(fskBlock);
						mDataBlocks.push_back({ mDataLength, 0, fskBlock });

						lastIsBlank = false;
						lastIsData = false;
						lastIsFSK = true;
					}

					// FSK blocks can easily exceed the data limit even within a single block, so we
					// monitor the length on the fly. Note that we might already have gone over a little
					// bit due to addGap(); we just ignore that, knowing that it'll be caught later.
					const uint32 maxBlockLen = mDataLength < kATCassetteDataLimit ? kATCassetteDataLimit - mDataLength : 0;
					bool polarity = false;
					const uint32 transitions = len >> 1;

					while(len > 0) {
						uint16 rawPulseWidth;
						file.Read(&rawPulseWidth, 2);

						fskBlock->AddFSKPulse(polarity, VDFromLE16(rawPulseWidth));

						if (fskBlock->GetDataSampleCount() > maxBlockLen)
							throw ATCassetteTooLongException();

						polarity = !polarity;
						len -= 2;
					}

					if (g_ATLCCasImage.IsEnabled()) {
						float pos = (float)mDataLength / (float)kATCassetteDataSampleRate;
						int mins = (int)(pos / 60.0f);
						float secs = pos - (float)mins * 60.0f;

						g_ATLCCasImage("FSK block @ %3d:%06.3f: %ums gap, %u transition%s (%.1f ms)\n"
							, mins
							, secs
							, gapms
							, transitions
							, transitions != 1 ? "s" : ""
							, fskBlock->GetDataSampleCount() / kATCassetteDataSampleRate * 1000.0f);
					}

					mDataLength = mDataBlocks.back().mStart + fskBlock->GetDataSampleCount();
				}

				break;
			}

			case VDMAKEFOURCC('p', 'w', 'm', 'c'):{
				break;
			}

			case VDMAKEFOURCC('p', 'w', 'm', 'd'):
			case VDMAKEFOURCC('p', 'w', 'm', 'l'):
				throw MyError("Cannot load tape: turbo encoded (PWM) data exists in image.");
		}

		file.Seek(file.Pos() + len);
	}

	// add two second footer
	addGap(2.0f);

	// final size check
	if (mDataLength > kATCassetteDataLimit)
		throw ATCassetteTooLongException();

	// set up data blocks
	mDataBlockCount = (uint32)mDataBlocks.size();
	mDataBlocks.push_back( { mDataLength, 0, nullptr } );

	ConvertDataToPeaks();

	// set up audio blocks
	mbAudioCreated = true;
	mAudioBlocks = mDataBlocks;

	for(auto& block : mAudioBlocks)
		block.mStart *= kATCassetteAudioSamplesPerDataSample;

	mAudioBlockCount = mDataBlockCount;
	mAudioLength = mDataLength * kATCassetteAudioSamplesPerDataSample;

	mPeakMaps[1] = mPeakMaps[0];
}

void ATCassetteImage::ConvertDataToPeaks() {
	RefreshPeaksFromData(0, mDataLength);
}

void ATCassetteImage::RefreshPeaksFromData(uint32 startSample, uint32 endSample) {
	if (endSample > mDataLength)
		endSample = mDataLength;

	if (endSample <= startSample)
		return;

	uint32 firstPeak = startSample / kDataSamplesPerPeakSample;
	uint32 lastPeak = (endSample - 1) / kDataSamplesPerPeakSample;

	uint32 reqAlloc = (lastPeak + 1) * 2;
	if (mPeakMaps[0].size() < reqAlloc)
		mPeakMaps[0].resize(reqAlloc, 0x80);
	
	for(uint32 peak = firstPeak; peak <= lastPeak; ++peak) {
		uint32 blockStart = peak * kDataSamplesPerPeakSample;
		uint32 blockLen = std::min<uint32>(kDataSamplesPerPeakSample, mDataLength - blockStart);

		int delta = 127 - (GetBitSum(blockStart, blockLen, false) * 127) / blockLen;

		mPeakMaps[0][peak*2+0] = 128 - delta;
		mPeakMaps[0][peak*2+1] = 128 + delta;
	}
}

///////////////////////////////////////////////////////////////////////////

void ATCreateNewCassetteImage(IATCassetteImage **ppImage) {
	vdrefptr<ATCassetteImage> pImage(new ATCassetteImage);
	pImage->InitNew();
	*ppImage = pImage.release();
}

void ATLoadCassetteImage(IVDRandomAccessStream& stream, IVDRandomAccessStream *analysisOutput, IATCassetteImage **ppImage) {
	vdrefptr<ATCassetteImage> pImage(new ATCassetteImage);

	VDBufferedStream bs(&stream, 65536);

	pImage->Load(bs, analysisOutput);

	*ppImage = pImage.release();
}

void ATSaveCassetteImageCAS(IVDRandomAccessStream& file, IATCassetteImage *image) {
	static_cast<ATCassetteImage *>(image)->SaveCAS(file);
}

void ATSaveCassetteImageWAV(IVDRandomAccessStream& file, IATCassetteImage *image) {
	static_cast<ATCassetteImage *>(image)->SaveWAV(file);
}
