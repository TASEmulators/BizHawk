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

#ifndef f_AT_SLIGHTSID_H
#define f_AT_SLIGHTSID_H

#include <vd2/system/memory.h>
#include <at/atcore/audiosource.h>

class ATScheduler;
class ATMemoryManager;
class ATMemoryLayer;
class IATAudioMixer;
class ATConsoleOutput;

// SlightSID expansion emulator.
//
// The SlightSID bolts a 6581/8580 SID onto the Atari cartridge bus at $D500-D51F.
// The SID is run at approximately the same speed as on the C64 to preserve pitch,
// at 5/9ths the Atari machine clock (0.985248MHz for PAL). Because of this
// mismatch, only writes to the SID are possible and all reads return the value
// $33.
//
// This particular implementation is not very well optimized, although it does
// run acceptably: it takes about 60% as much time as the rest of the machine
// emulation. This is because the waveform oscillators and envelope generators
// are run at 1MHz speed. The channel outputs are mixed and crudely box filtered
// down to Altirra's internal mixing rate (64KHz) before the filter is applied.
//
class ATSlightSIDEmulator final : public VDAlignedObject<16>, public IATSyncAudioSource {
	ATSlightSIDEmulator(const ATSlightSIDEmulator&) = delete;
	ATSlightSIDEmulator& operator=(const ATSlightSIDEmulator&) = delete;
public:
	enum { kTypeID = 'ssid' };

	ATSlightSIDEmulator();
	~ATSlightSIDEmulator();

	void Init(ATMemoryManager *memMan, ATScheduler *sch, IATAudioMixer *mixer);
	void Shutdown();

	void ColdReset();
	void WarmReset();

	void DumpStatus(ATConsoleOutput& output);

	void WriteControl(uint8 addr, uint8 value);

	const uint8 *GetRegisters() const { return mRegisters; }
	uint8 GetEnvelopeValue(int ch) const { return mChannels[ch].mEnvelope; }
	int GetEnvelopeMode(int ch) const { return mChannels[ch].mEnvelopeMode; }

	void Run(uint32 cycles);

public:
	bool RequiresStereoMixingNow() const override { return false; }
	void WriteAudio(const ATSyncAudioMixInfo& mixInfo) override;

protected:
	void Flush();

	static sint32 StaticReadControl(void *thisptr, uint32 addr);
	static bool StaticWriteControl(void *thisptr, uint32 addr, uint8 value);

	struct Channel {
		uint32		mPrevPhase;
		uint32		mPhase;			// 24-bit phase accumulator
		uint32		mFreq;			// 16-bit frequency (lower 16 bits of phase)
		uint32		mPulseWidth;
		sint32		mPrescalerAccum;
		bool		mbSync;
		bool		mbRingMod;
		bool		mbTestOn;
		uint8		mEnvelopeMode;
		uint8		mEnvelope;
		uint8		mWaveform;
		uint8		mAttack;
		uint8		mDecay;
		uint8		mSustain;
		uint8		mRelease;
		uint8		mControl;
		sint32		mFilteredEnable;
		sint32		mNonFilteredEnable;
		uint32		mNoiseLFSR;
	};

	ATMemoryLayer *mpMemLayerControl;
	ATScheduler *mpScheduler;
	ATMemoryManager *mpMemMan;
	IATAudioMixer *mpAudioMixer;

	float	mVolumeScale;
	float	mFilterDelayX1;
	float	mFilterDelayX2;
	float	mFilterDelayY1;
	float	mFilterDelayY2;
	float	mFilterCoeffB0;
	float	mFilterCoeffB1;
	float	mFilterCoeffB2;
	float	mFilterCoeffA0;
	float	mFilterCoeffA1;
	float	mFilterCoeffA2;

	float	mOutputAccumNF;
	float	mOutputAccumF;
	uint32	mOutputCount;
	uint32	mOutputLevel;

	uint32	mLastUpdate;
	uint32	mCycleAccum;

	uint8	mRegisters[32];

	Channel mChannels[3];

	enum {
		kAccumBufferSize = 1536
	};

	VDALIGN(16) float mAccumBuffer[kAccumBufferSize];
};

#endif
