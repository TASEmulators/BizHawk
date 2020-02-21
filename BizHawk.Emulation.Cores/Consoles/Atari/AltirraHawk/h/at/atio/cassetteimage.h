//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - cassette tape image definitions
//	Copyright (C) 2009-2016 Avery Lee
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

#ifndef f_AT_ATIO_CASSETTEIMAGE_H
#define f_AT_ATIO_CASSETTEIMAGE_H

#include <vd2/system/refcount.h>
#include <at/atio/image.h>

class IVDRandomAccessStream;

// Cassette internal storage is defined in terms of NTSC cycle timings.
//
// Master sync mixer rate: 1.79MHz / 28 = 64KHz
// Audio samples: sync mixer rate / 2 = 32KHz
// Data samples: audio sample rate / 8 = 4KHz
//
// Note that we currently have a problem in that these are always defined
// in terms of NTSC timings, but the machine cycle rate and sync mixer
// run about 1% slower in PAL. We currently cheat and just run the tape
// 1% slower too....

const int kATCassetteAudioSamplesPerDataSample = 1;
const int kATCassetteCyclesPerAudioSample = 56;
const int kATCassetteCyclesPerDataSample = kATCassetteCyclesPerAudioSample * kATCassetteAudioSamplesPerDataSample;

/// Sampling rate for data samples stored in memory.
constexpr float kATCassetteDataSampleRate = (7159090.0f / 4.0f) / (float)kATCassetteCyclesPerDataSample;
constexpr double kATCassetteDataSampleRateD = (7159090.0 / 4.0) / (double)kATCassetteCyclesPerDataSample;

/// Sampling rate for audio samples stored in memory. Note that this is internal to
/// block storage; the blocks themselves resample up to sync mixer rate.
constexpr float kATCassetteImageAudioRate = (7159090.0f / 4.0f) / (float)kATCassetteCyclesPerAudioSample;

constexpr float kATCassetteSecondsPerDataSample = 1.0f / kATCassetteDataSampleRate;
constexpr float kATCassetteMSPerDataSample = 1000.0f / kATCassetteDataSampleRate;

/// Maximum number of data samples that we allow in a cassette image. The
/// code uses uint32, but we limit to 2^31 to give us plenty of buffer
/// room (and also to limit memory usage). At 4KHz, this is about 37
/// hours of tape.
const uint32 kATCassetteDataLimit = UINT32_C(0x1FFFFFFF);

/// How much room we require before the limit before we will write out a byte.
/// At 1 baud, it takes 10 seconds to write out a byte. 
const uint32 kATCassetteDataWriteByteBuffer = (uint32)(kATCassetteDataSampleRate * 12);


class IATCassetteImage : public IATImage {
public:
	enum : uint32 { kTypeID = 'csim' };

	/// Returns length of data track, in data samples.
	virtual uint32 GetDataLength() const = 0;

	/// Returns length of audio track, in audio samples.
	virtual uint32 GetAudioLength() const = 0;

	/// Returns true if the audio track was created from the data track.
	virtual bool IsAudioCreated() const = 0;

	/// Decodes a bit from the tape.
	///
	/// pos: Center data sample position for decoding.
	/// averagingPeriod: Number of data samples over which to extract a bit.
	/// threshold: Threshold for 0/1 detection, relative to count (averaging period).
	/// prevBit: Previous bit to reuse if sum doesn't exceed hysteresis threshold.
	///
	/// Returns the decoded bit.
	///
	virtual bool GetBit(uint32 pos, uint32 averagingPeriod, uint32 threshold, bool prevBit, bool bypassFSK) const = 0;

	/// Decodes a bit from the tape without averaging and bypassing FSK decoding.
	virtual bool GetTurboBit(uint32 pos) const = 0;

	/// Return the first position at or after the given position that is a low data bit.
	/// Does no low-pass filtering. Particularly optimized for skipping blocks that
	/// are already marked as silence internally. Always uses FSK if both FSK and turbo
	/// encodings are available.
	virtual uint32 GetNearestLowBitPos(uint32 pos) const = 0;

	/// Read signal peaks.
	///
	/// t0: First sample requested, in seconds.
	/// dt: Time between samples, in seconds.
	/// n: Number of samples requested.
	/// data: Receives [n] min/max pairs for data track.
	/// audio: Receives [n] min/max pairs for audio track.
	///
	/// Peaks are returned as min/max pairs with values in [-1, 1] range.
	///
	virtual void ReadPeakMap(float t0, float dt, uint32 n, float *data, float *audio) = 0;

	/// Read audio.
	///
	/// dst: Auto-incremented dest pointer to output channel.
	/// posSample/posCycle: Auto-incremented integer/fractional audio sample position.
	/// n: Number of samples requested.
	///
	/// Returns number of samples provided. If the end of the audio track is hit,
	/// fewer than requested samples may be returned.
	virtual void AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const = 0;

	virtual uint32 GetWriteCursor() const = 0;
	virtual void SetWriteCursor(uint32 pos) = 0;
	virtual void WriteBlankData(uint32 len) = 0;
	virtual void WriteStdData(uint8 byte, uint32 baudRate) = 0;
	virtual void WriteFSKPulse(bool polarity, uint32 samples) = 0;
};

void ATCreateNewCassetteImage(IATCassetteImage **ppImage);
void ATLoadCassetteImage(IVDRandomAccessStream& file, IVDRandomAccessStream *analysisOutput, IATCassetteImage **ppImage);
void ATSaveCassetteImageCAS(IVDRandomAccessStream& file, IATCassetteImage *image);
void ATSaveCassetteImageWAV(IVDRandomAccessStream& file, IATCassetteImage *image);

#endif
