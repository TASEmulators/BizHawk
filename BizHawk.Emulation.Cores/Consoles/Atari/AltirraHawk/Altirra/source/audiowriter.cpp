//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2009 Avery Lee
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
#include <vd2/system/math.h>
#include "audiowriter.h"
#include "audiofilters.h"
#include "uirender.h"

ATAudioWriterFilter::ATAudioWriterFilter(bool pal)
	: mAccum64(0)
	, mInc64(pal ? (uint64)(63337.392857143 / 44100.0 * 4294967296.0 + 0.5) : (uint64)(63920.4464285714 / 44100.0 * 4294967296.0 + 0.5))
	, mSourceLevel(0)
	, mOutputLevel(0)
{
}

size_t ATAudioWriterFilter::Process(const float *src, size_t n) {
	size_t nmax = vdcountof(mSourceBuffer) - mSourceLevel;

	if (n > nmax)
		n = nmax;

	if (n) {
		for(size_t i=0; i<n; ++i) {
			int y = (int)floor(0.5f + src[i] * 32768.0f);

			if (y < -32767)
				y = -32767;
			else if (y > 32767)
				y = 32767;

			mSourceBuffer[mSourceLevel++] = (sint16)y;
		}
	}

	while(mOutputLevel < vdcountof(mOutputBuffer)) {
		uint32 offset = (uint32)(mAccum64 >> 32);

		if (offset + 64 > mSourceLevel)
			break;

		const uint32 fidx = (uint32)mAccum64 >> 26;
		const sint16 *src2 = mSourceBuffer + offset;
		const sint16 *filt1 = gATAudioResamplingKernel63To44[fidx];
		const sint16 *filt2 = gATAudioResamplingKernel63To44[fidx+1];
		sint32 accum1 = 0x4000 << 4;
		sint32 accum2 = 0x4000 << 4;

		for(int i=0; i<24; ++i) {
			accum1 += (sint32)filt1[i] * (sint32)src2[i] + (sint32)filt1[i + 40] * (sint32)src2[i + 40];
			accum2 += (sint32)filt2[i] * (sint32)src2[i] + (sint32)filt2[i + 40] * (sint32)src2[i + 40];
		}

		accum1 >>= 4;
		accum2 >>= 4;

		for(int i=0; i<16; ++i) {
			accum1 += (sint32)filt1[i + 24] * (sint32)src2[i + 24];
			accum2 += (sint32)filt2[i + 24] * (sint32)src2[i + 24];
		}

		accum1 >>= 15;
		accum2 >>= 15;

		sint32 lerpfrac = ((uint32)mAccum64 >> 12) & 0x3fff;
		sint32 val = accum1 + (((accum2 - accum1) * lerpfrac) >> 12);

		if (val < -32768)
			val = -32768;
		else if (val > 32767)
			val = 32767;

		mOutputBuffer[mOutputLevel++] = val;
		mAccum64 += mInc64;
	}

	uint32 scrollOffset = (uint32)(mAccum64 >> 32);
	if (scrollOffset > mSourceLevel)
		scrollOffset = mSourceLevel;

	if (scrollOffset) {
		mSourceLevel -= scrollOffset;
		memmove(mSourceBuffer, mSourceBuffer + scrollOffset, mSourceLevel * sizeof(mSourceBuffer[0]));
		mAccum64 -= (uint64)scrollOffset << 32;
	}

	return n;
}

///////////////////////////////////////////////////////////////////////////

ATAudioWriter::ATAudioWriter(const wchar_t *filename, bool rawMode, bool stereo, bool pal, IATUIRenderer *r)
	: mbErrorState(false)
	, mbRawMode(rawMode)
	, mbStereo(stereo)
	, mFile(filename, nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways | nsVDFile::kSequential)
	, mpUIRenderer(r)
	, mTotalInputSamples(0)
	, mInvSampleRate(pal ? 1.0f / 63337.392857143f : 1.0f / 63920.4464285714f)
	, mLeftFilter(pal)
	, mRightFilter(pal)
{
	if (!mbRawMode) {
		uint8 kWaveHeader[]={
			'R', 'I', 'F', 'F',
			0, 0, 0, 0,
			'W', 'A', 'V', 'E',
			'f', 'm', 't', ' ',
			0x12, 0, 0, 0,

			// wave format
			0x01, 0x00,
			0x01, 0x00,
			0x44, 0xAC, 0x00, 0x00,
			0x88, 0x58, 0x01, 0x00,
			0x02, 0x00,
			0x10, 0x00,
			0x00, 0x00,
			'd', 'a', 't', 'a',
			0x00, 0x00, 0x00, 0x00
		};

		uint16 channels = stereo ? 2 : 1;
		uint16 blockAlign = stereo ? 4 : 2;

		if (stereo) {
			VDWriteUnalignedLEU16(kWaveHeader + 22, channels);
			VDWriteUnalignedLEU32(kWaveHeader + 28, 44100 * blockAlign);
			VDWriteUnalignedLEU16(kWaveHeader + 32, blockAlign);
		}

		mFile.write(kWaveHeader, sizeof kWaveHeader);
	}
}

ATAudioWriter::~ATAudioWriter() {
	if (mpUIRenderer) {
		mpUIRenderer->SetRecordingPosition();
		mpUIRenderer = NULL;
	}
}

void ATAudioWriter::CheckExceptions() {
	if (!mbErrorState)
		return;

	if (!mError.empty()) {
		MyError e;

		e.TransferFrom(mError);
		throw e;
	}
}

void ATAudioWriter::Finalize() {
	if (!mbRawMode && !mbErrorState) {
		uint32 limit = VDClampToUint32(mFile.tell());

		uint8 riffSize[4];
		VDWriteUnalignedLEU32(&riffSize, limit - 8);

		mFile.seek(4);
		mFile.write(riffSize, 4);

		uint8 dataSize[4];
		VDWriteUnalignedLEU32(&dataSize, limit - 46);

		mFile.seek(42);
		mFile.write(dataSize, 4);
	}
}

void ATAudioWriter::WriteRawAudio(const float *left, const float *right, uint32 count, uint32 timestamp) {
	if (mbErrorState)
		return;

	// If we got mono, and were expecting stereo, make "stereo."
	if (mbStereo && !right)
		right = left;

	// If we got stereo, and were expecting mono... uh oh. We need to mix and retry.
	if (!mbStereo && right)
		return WriteRawAudioMix(left, right, count);

	try {
		mTotalInputSamples += count;

		if (!mbRawMode) {
			while(count) {
				size_t actual = mLeftFilter.Process(left, count);
				left += actual;

				if (right) {
					mRightFilter.Process(right, count);
					right += actual;
				}

				count -= (uint32)actual;

				size_t outlevel = mLeftFilter.GetOutputLevel();

				if (outlevel) {
					const sint16 *leftout = mLeftFilter.Extract();

					if (right) {
						const sint16 *rightout = mRightFilter.Extract();

						WriteInterleaved(leftout, rightout, (uint32)outlevel);
					} else {
						mFile.writeData(leftout, (long)(sizeof(sint16)*outlevel));
					}
				}
			}
		} else {
			if (right) {
				WriteInterleaved(left, right, count);
			} else {
				mFile.writeData(left, sizeof(float)*count);
			}
		}

		if (mpUIRenderer)
			mpUIRenderer->SetRecordingPosition((float)mTotalInputSamples * mInvSampleRate, mFile.tell());
	} catch(MyError& e) {
		mError.TransferFrom(e);
		mbErrorState = true;
	}
}

void ATAudioWriter::WriteRawAudioMix(const float *left, const float *right, uint32 count) {
	float mixbuf[512];

	while(count) {
		uint32 tc = std::min<uint32>(count, vdcountof(mixbuf));

		for(uint32 i=0; i<tc; ++i)
			mixbuf[i] = left[i] + right[i];

		WriteRawAudio(mixbuf, nullptr, tc, 0);

		count -= tc;
		left += tc;
		right += tc;
	}
}

void ATAudioWriter::WriteInterleaved(const float *left, const float *right, uint32 count) {
	float buf[512][2];

	while(count) {
		uint32 tc = count > 512 ? 512 : count;

		for(uint32 i=0; i<tc; ++i) {
			buf[i][0] = left[i];
			buf[i][1] = right[i];
		}

		mFile.writeData(buf, tc*sizeof(buf[0]));
		left += tc;
		right += tc;
		count -= tc;
	}
}

void ATAudioWriter::WriteInterleaved(const sint16 *left, const sint16 *right, uint32 count) {
	sint16 buf[512][2];

	while(count) {
		uint32 tc = count > 512 ? 512 : count;

		for(uint32 i=0; i<tc; ++i) {
			buf[i][0] = left[i];
			buf[i][1] = right[i];
		}

		mFile.writeData(buf, tc*sizeof(buf[0]));
		left += tc;
		right += tc;
		count -= tc;
	}
}
