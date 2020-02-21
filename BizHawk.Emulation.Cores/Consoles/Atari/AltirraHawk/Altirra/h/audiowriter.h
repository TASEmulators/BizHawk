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

#ifndef f_AT_AUDIOWRITER_H
#define f_AT_AUDIOWRITER_H

#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/memory.h>
#include "audiooutput.h"

class IATUIRenderer;

class ATAudioWriterFilter {
public:
	ATAudioWriterFilter(bool pal);

	size_t Process(const float *src, size_t n);
	const sint16 *Extract() {
		mOutputLevel = 0;
		return mOutputBuffer;
	}

	uint32 GetOutputLevel() const { return mOutputLevel; }

protected:
	uint32 mSourceLevel;
	uint32 mOutputLevel;
	uint64 mAccum64;
	uint64 mInc64;

	VDALIGN(16) sint16 mSourceBuffer[4096];
	VDALIGN(16) sint16 mOutputBuffer[4096];
};

class ATAudioWriter final : public VDAlignedObject<16>, public IATAudioTap {
	ATAudioWriter(const ATAudioWriter&) = delete;
	ATAudioWriter& operator=(const ATAudioWriter&) = delete;
public:
	ATAudioWriter(const wchar_t *filename, bool rawMode, bool stereo, bool pal, IATUIRenderer *r);
	~ATAudioWriter();

	bool IsRecordingRaw() const { return mbRawMode; }

	void CheckExceptions();

	void Finalize();
	void WriteRawAudio(const float *left, const float *right, uint32 count, uint32 timestamp);
	
protected:
	void WriteRawAudioMix(const float *left, const float *right, uint32 count);
	void WriteInterleaved(const float *left, const float *right, uint32 count);
	void WriteInterleaved(const sint16 *left, const sint16 *right, uint32 count);

	bool mbErrorState;
	bool mbRawMode;
	bool mbStereo;
	VDFile mFile;
	MyError mError;

	IATUIRenderer	*mpUIRenderer;
	uint64 mTotalInputSamples;
	float mInvSampleRate;

	ATAudioWriterFilter mLeftFilter;
	ATAudioWriterFilter mRightFilter;
};

#endif	// f_AT_AUDIOWRITER_H
