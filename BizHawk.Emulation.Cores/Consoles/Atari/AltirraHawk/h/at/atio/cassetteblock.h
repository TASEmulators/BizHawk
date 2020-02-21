//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - cassette storage block types
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

#ifndef f_AT_ATIO_CASSETTEBLOCK_H
#define f_AT_ATIO_CASSETTEBLOCK_H

enum ATCassetteImageBlockType {
	kATCassetteImageBlockType_Blank,
	kATCassetteImageBlockType_Std,
	kATCassetteImageBlockType_FSK,
	kATCassetteImageBlockType_RawAudio
};

/// Base class for all in-memory cassette image blocks.
class ATCassetteImageBlock {
public:
	virtual ~ATCassetteImageBlock() = default;

	virtual ATCassetteImageBlockType GetBlockType() const = 0;

	/// Retrieve sum of bits starting at the given block-local offset. The
	/// count must be >0 and the range must fit within the block.
	virtual uint32 GetBitSum(uint32 pos, uint32 n, bool bypassFSK) const;

	/// Retrieve audio sync samples.
	///
	/// dst: Auto-incremented pointer to output buffer.
	/// posSample: Auto-incremented integer audio sample counter.
	/// posCycle: Auto-updated fractional audio sample counter.
	/// n: Number of sync samples requested.
	///
	/// Returns number of sync samples produced.
	///
	/// The audio samples in the block are resampled to the sync mixer sample
	/// rate, with (posSample,posCycle) as the fractional audio sample position.
	/// The resulting sync mixer samples are added into the left and right
	/// channel buffers. The right channel buffer, if provided, receives the
	/// same audio as the left buffer. The buffer must be initialized on entry
	/// such that the buffers need not be touched if there is no audio.
	///
	virtual uint32 AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const;
};

/// Cassette image block type for standard framed bytes with 8-bits of data stored
/// in FSK encoding.
class ATCassetteImageDataBlockStd final : public ATCassetteImageBlock {
public:
	ATCassetteImageDataBlockStd();

	void Init(uint32 baudRate);

	void AddData(const uint8 *data, uint32 len);

	ATCassetteImageBlockType GetBlockType() const override {
		return kATCassetteImageBlockType_Std;
	}

	const uint8 *GetData() const;
	const uint32 GetDataLen() const;

	uint32 GetBaudRate() const { return mBaudRate; }
	uint32 GetDataSampleCount() const;
	uint64 GetDataSampleCount64() const;

	uint32 GetBitSum(uint32 pos, uint32 n, bool bypassFSK) const override;
	uint32 AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const override;

private:
	uint64 mDataSamplesPerByteF32 = 0;
	uint64 mBytesPerDataSampleF32 = 0;
	uint64 mBytesPerCycleF32 = 0;
	uint32 mBitsPerSyncSampleF32 = 0;

	uint32 mPhaseAddedPerOneBitLo = 0;
	uint32 mPhaseAddedPerOneBitHi = 0;

	uint32 mBaudRate = 0;

	vdfastvector<uint8> mData;

	// Partial sum of '1' bits prior to start of current byte, mod 24. Why 24?
	// Well, we use this array to determine the phase shift caused by the
	// distribution of '0' and '1' bits. A one bit advances the phase by 1/24th
	// more than a zero. This means that we don't care about multiples of 24.
	// However, we have to do an explicit mod since 256 mod 24 is nonzero.
	vdfastvector<uint8> mPhaseSums;
};

/// Cassette image block for raw data.
class ATCassetteImageBlockRawData final : public ATCassetteImageBlock {
public:
	ATCassetteImageBlockType GetBlockType() const override {
		return kATCassetteImageBlockType_FSK;
	}

	uint32 GetDataSampleCount() const { return mDataLength; }

	void AddFSKPulse(bool polarity, uint32 duration10us);
	void AddFSKPulseSamples(bool polarity, uint32 samples);

	// Extract pairs of 0/1 pulse lengths.
	void ExtractPulses(vdfastvector<uint32>& pulses, bool bypassFSK) const;

	uint32 GetBitSum(uint32 pos, uint32 n, bool bypassFSK) const override;

	void SetBits(bool fsk, uint32 startPos, uint32 n, bool polarity);

	uint32 mDataLength = 0;
	vdfastvector<uint32> mDataRaw {};		// Storage is MSB first.
	vdfastvector<uint32> mDataFSK {};		// Storage is MSB first.

	uint64 mFractionalDataLength = 0;
	uint64 mFSKPhaseAccum = 0;
};

/// Cassette image block type for raw audio data only.
class ATCassetteImageBlockRawAudio final : public ATCassetteImageBlock {
public:
	ATCassetteImageBlockType GetBlockType() const override {
		return kATCassetteImageBlockType_RawAudio;
	}

	uint32 AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const override;

	vdfastvector<uint8> mAudio;
	uint32 mAudioLength;
};

/// Cassette image block type for blank tape.
class ATCassetteImageBlockBlank final : public ATCassetteImageBlock {
public:
	ATCassetteImageBlockType GetBlockType() const override {
		return kATCassetteImageBlockType_Blank;
	}

	uint32 AccumulateAudio(float *&dst, uint32& posSample, uint32& posCycle, uint32 n) const override;
};


#endif
