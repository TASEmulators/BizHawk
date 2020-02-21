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
#include <at/atcore/consoleoutput.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/scheduler.h>
#include "slightsid.h"
#include "audiooutput.h"
#include "memorymanager.h"
#include "console.h"

ATSlightSIDEmulator::ATSlightSIDEmulator()
	: mpMemLayerControl(NULL)
	, mpScheduler(NULL)
	, mpMemMan(NULL)
	, mpAudioMixer(NULL)
{
}

ATSlightSIDEmulator::~ATSlightSIDEmulator() {
	Shutdown();
}

void ATSlightSIDEmulator::Init(ATMemoryManager *memMan, ATScheduler *sch, IATAudioMixer *mixer) {
	mpMemMan = memMan;
	mpScheduler = sch;
	mpAudioMixer = mixer;

	mixer->AddSyncAudioSource(this);

	ColdReset();
}

void ATSlightSIDEmulator::Shutdown() {
	if (mpMemMan) {
		if (mpMemLayerControl) {
			mpMemMan->DeleteLayer(mpMemLayerControl);
			mpMemLayerControl = NULL;
		}

		mpMemMan = NULL;
	}

	if (mpAudioMixer) {
		mpAudioMixer->RemoveSyncAudioSource(this);
		mpAudioMixer = nullptr;
	}
}

void ATSlightSIDEmulator::ColdReset() {
	mCycleAccum = 0;
	mLastUpdate = ATSCHEDULER_GETTIME(mpScheduler);

	if (mpMemLayerControl) {
		mpMemMan->DeleteLayer(mpMemLayerControl);
		mpMemLayerControl = NULL;
	}

	ATMemoryHandlerTable handlers = {};
	handlers.mpThis = this;

	handlers.mbPassAnticReads = true;
	handlers.mbPassReads = true;
	handlers.mbPassWrites = true;
	handlers.mpDebugReadHandler = StaticReadControl;
	handlers.mpReadHandler = StaticReadControl;
	handlers.mpWriteHandler = StaticWriteControl;
	mpMemLayerControl = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay, handlers, 0xD5, 0x01);

	mpMemMan->EnableLayer(mpMemLayerControl, true);

	WarmReset();
}

void ATSlightSIDEmulator::WarmReset() {
	memset(mChannels, 0, sizeof mChannels);

	for(int i=0; i<3; ++i)
		mChannels[i].mNoiseLFSR = 1;

	mOutputAccumF = 0;
	mOutputAccumNF = 0;
	mOutputCount = 0;
	mOutputLevel = 0;
	mVolumeScale = 0;
	mFilterDelayX1 = 0;
	mFilterDelayX2 = 0;
	mFilterDelayY1 = 0;
	mFilterDelayY2 = 0;
	mFilterCoeffB0 = 0;
	mFilterCoeffB1 = 0;
	mFilterCoeffB2 = 0;
	mFilterCoeffA0 = 0;
	mFilterCoeffA1 = 0;
	mFilterCoeffA2 = 0;
 	memset(mRegisters, 0xFF, sizeof mRegisters);
	for(int i=0; i<32; ++i)
		WriteControl(i, 0);

	memset(mAccumBuffer, 0, sizeof mAccumBuffer);
}

void ATSlightSIDEmulator::DumpStatus(ATConsoleOutput& output) {
	output <<= "CH  Freq  Phase   Wfrm  ADSR  Env-M";
	for(int i=0; i<3; ++i) {
		const Channel& ch = mChannels[i];

		output("%2u  %04X  %06X  %c%c%c%c  %X%X%X%X  %02X-%c"
			, i+1
			, ch.mFreq >> 8
			, ch.mPhase >> 8
			, ch.mWaveform & 8 ? 'N' : ' '
			, ch.mWaveform & 4 ? 'P' : ' '
			, ch.mWaveform & 2 ? 'S' : ' '
			, ch.mWaveform & 1 ? 'T' : ' '
			, ch.mAttack
			, ch.mDecay
			, ch.mSustain >> 4
			, ch.mRelease
			, ch.mEnvelope
			, ch.mEnvelopeMode == 1 && ch.mEnvelope <= ch.mSustain ? 'S' : "ADR"[ch.mEnvelopeMode]
			);
	}
}

void ATSlightSIDEmulator::WriteControl(uint8 addr, uint8 value) {
	if (addr >= 25)
		return;

	const uint8 prevValue = mRegisters[addr];
	if (prevValue == value)
		return;

	Flush();
	mRegisters[addr] = value;

	if (addr < 21) {
		static const ptrdiff_t kChOffsets[21]={
			0, 0, 0, 0, 0, 0, 0,
			sizeof(Channel), sizeof(Channel), sizeof(Channel), sizeof(Channel), sizeof(Channel), sizeof(Channel), sizeof(Channel),
			sizeof(Channel)*2, sizeof(Channel)*2, sizeof(Channel)*2, sizeof(Channel)*2, sizeof(Channel)*2, sizeof(Channel)*2, sizeof(Channel)*2,
		};

		static const int kRegOffsets[21]={
			0, 0, 0, 0, 0, 0, 0,
			7, 7, 7, 7, 7, 7, 7,
			14, 14, 14, 14, 14, 14, 14
		};

		Channel& ch = *(Channel *)((char *)mChannels + kChOffsets[addr]);
		const uint8 *rbase = mRegisters + kRegOffsets[addr];

		switch(addr - kRegOffsets[addr]) {
			case 0x00:
			case 0x01:
				ch.mFreq = ((uint32)rbase[0] << 8) + ((uint32)rbase[1] << 16);
				break;

			case 0x02:
			case 0x03:
				ch.mPulseWidth = ((uint32)rbase[2] << 20) + ((uint32)(rbase[3] & 15) << 28);
				break;

			case 0x04:
				ch.mWaveform = value >> 4;

				if ((prevValue ^ value) & 1) {
					if (value & 1) {
						if (ch.mEnvelope < 255)
							ch.mEnvelopeMode = 0;
						else
							ch.mEnvelopeMode = 1;
					} else {
						ch.mEnvelopeMode = 2;
					}
				}

				ch.mbSync = (value & 2) != 0;
				ch.mbRingMod = (value & 4) != 0;

				if ((prevValue ^ value) & 8) {
					ch.mbTestOn = (value & 0x08) & 1;

					if (ch.mbTestOn) {
						ch.mPhase = 0;
						ch.mNoiseLFSR = 1;
					}
				}
				break;

			case 0x05:
				ch.mAttack = value >> 4;
				ch.mDecay = value & 15;
				break;

			case 0x06:
				ch.mSustain = (value >> 4) * 17;
				ch.mRelease = value & 15;
				break;
		}
	} else {
		switch(addr) {
			case 0x15:
			case 0x16:
			case 0x17:
			case 0x18:
				{
					// The filter cutoff varies linearly from 30Hz to 12KHz at 1MHz according to the
					// SID datasheet. This is very hand-wavy, but fortunately there is a lot of
					// variance on real C64s.
					float fc = (30.0f + 11970.0f*((mRegisters[0x15] & 7) + ((uint32)mRegisters[0x16] << 3)) / 2047.0f);

					// "Based on my experience, the resonance Q value goes from 0.71 to 1.71, which as a control
					//  goes from about 0 dB resonance to about 10 dB."
					// http://www.lemon64.com/forum/viewtopic.php?p=383458&sid=9f9e83349123f282fabdb8b1c0eb2fb5#383458
					float q = 0.71f + (float)(mRegisters[0x17] >> 4) / 15.0f;

					float w0 = 2.0f * 3.1415926535f * fc / 63920.0f;
					float alpha = sinf(w0) / (2.0f * q);

					float b0 = 0;
					float b1 = 0;
					float b2 = 0;

					// low-pass filter
					if (mRegisters[0x18] & 0x10) {
						b0 += (1.0f - cosf(w0)) * 0.5f;
						b1 += 1.0f - cosf(w0);
						b2 += (1.0f - cosf(w0)) * 0.5f;
					}

					// band-pass filter
					if (mRegisters[0x18] & 0x20) {
						b0 += sinf(w0) * 0.5f;
						b2 += -sinf(w0) * 0.5f;
					}

					// high-pass filter
					if (mRegisters[0x18] & 0x40) {
						b0 += (1.0f + cosf(w0)) * 0.5f;
						b1 += -(1.0f + cosf(w0));
						b2 += (1.0f + cosf(w0)) * 0.5f;
					}

					float inv_a0 = 1.0f / (1.0f + alpha);
					float a1 = -2.0f * cosf(w0);
					float a2 = 1.0f - alpha;
					mFilterCoeffB0 = b0 * inv_a0;
					mFilterCoeffB1 = b1 * inv_a0;
					mFilterCoeffB2 = b2 * inv_a0;
					mFilterCoeffA0 = inv_a0;
					mFilterCoeffA1 = -a1 * inv_a0;
					mFilterCoeffA2 = -a2 * inv_a0;

					// Apply a tiny bit of decay to the recursive parameters. This prevents the filter
					// from blowing up at low cutoff frequencies due to numerical accuracy issues.
					mFilterCoeffA1 *= 0.99999f;
					mFilterCoeffA2 *= 0.99999f;
				}

				mChannels[0].mFilteredEnable = (mRegisters[0x17] & 0x01) ? ~0 : 0;
				mChannels[0].mNonFilteredEnable = ~mChannels[0].mFilteredEnable;
				mChannels[1].mFilteredEnable = (mRegisters[0x17] & 0x02) ? ~0 : 0;
				mChannels[1].mNonFilteredEnable = ~mChannels[1].mFilteredEnable;
				mChannels[2].mFilteredEnable = (mRegisters[0x17] & 0x04) ? ~0 : 0;

				if (mRegisters[0x18] & 0x80)
					mChannels[2].mNonFilteredEnable = 0;
				else
					mChannels[2].mNonFilteredEnable = ~mChannels[2].mFilteredEnable;

				mVolumeScale = (float)(mRegisters[0x18] & 15);
				
				mVolumeScale *= 60.0f				// to undo POKEY scaling
								/ (128.0f*255.0f)	// waveform * envelope scaling
								/ 5.0f				// because we accumulate 28*5 counts per sample instead of 28 cycles
								/ 15.0f				// for 0-15 global volume scale
								;
				break;
		}
	}
}

#if _MSC_VER >= 1400
// We need to enable precise FP code generation here since we have anti-denormal
// code that we need not to be optimized out.
#pragma float_control(push)
#pragma float_control(precise, on)
#endif

void ATSlightSIDEmulator::Run(uint32 cycles) {
	cycles *= 5;
	cycles += mCycleAccum;

	while(cycles >= 9) {
		cycles -= 9;

		int output = 0;
		int filtoutput = 0;

		Channel *__restrict chprev = &mChannels[2];
		for(int chidx = 0; chidx < 3; ++chidx) {
			Channel *__restrict ch = &mChannels[chidx];
			uint32 prevPhase = ch->mPhase;
			uint32 nextPhase = prevPhase + ch->mFreq;

			if (ch->mbSync && chprev->mPhase < chprev->mPrevPhase)
				nextPhase = 0;

			ch->mPrevPhase = prevPhase;
			ch->mPhase = nextPhase;

			uint8 v;

			if (ch->mbTestOn) {
				v = 0;
			} else {
				// It is possible to combine waveforms through logical AND by enabling one at
				// a time, and in fact, waveform 5 (pulse windowed triangle) is common. We are
				// not currently emulating the effect of this on the noise LFSR, however.
				v = 0xFF;

				if (ch->mWaveform & 8) {
					if (nextPhase < prevPhase)
						ch->mNoiseLFSR = (ch->mNoiseLFSR << 1) + (((ch->mNoiseLFSR >> 22) + (ch->mNoiseLFSR >> 17)) & 1);

					v = (uint8)ch->mNoiseLFSR;	// not the right taps
				}

				if (ch->mWaveform & 4)
					v &= (ch->mPhase >= ch->mPulseWidth) ? 0xFF : 0x00;

				if (ch->mWaveform & 2)
					v = ch->mPhase >> 24;

				if (ch->mWaveform & 1) {
					if (ch->mbRingMod)
						v &= (ch->mPhase >> 23) ^ ((sint32)chprev->mPhase >> 31);
					else
						v &= (ch->mPhase >> 23) ^ ((sint32)ch->mPhase >> 31);
				}
			}

			// These prescaler rates are in cycles and come from the on-chip LFSR comparator
			// ROM: http://blog.kevtris.org/?p=13
			//
			// Since we don't use an LFSR, these values have been determined by running the
			// LFSR and counting the iterations until the comparator trips. They may be off
			// by one depending on how the SID prescaler works.
			//
			// Note that we're cheating here with regard to the way that the exponential
			// decay/release curve works -- we're slowing down the prescaler instead of
			// inserting another counter in between (pre-prescaler?).

			static const uint32 kPrescalerRates[6][16]={
#define PRESCALER_RATE(x) ((uint32)((0xFFFFFFFFULL + (x))/(x)))
#define PRESCALER_RATES(y) {	\
				PRESCALER_RATE(    9*(y)),	\
				PRESCALER_RATE(   32*(y)),	\
				PRESCALER_RATE(   63*(y)),	\
				PRESCALER_RATE(   95*(y)),	\
				PRESCALER_RATE(  149*(y)),	\
				PRESCALER_RATE(  220*(y)),	\
				PRESCALER_RATE(  267*(y)),	\
				PRESCALER_RATE(  313*(y)),	\
				PRESCALER_RATE(  392*(y)),	\
				PRESCALER_RATE(  977*(y)),	\
				PRESCALER_RATE( 1954*(y)),	\
				PRESCALER_RATE( 3126*(y)),	\
				PRESCALER_RATE( 3907*(y)),	\
				PRESCALER_RATE(11720*(y)),	\
				PRESCALER_RATE(19532*(y)),	\
				PRESCALER_RATE(31251*(y))	\
				}

				PRESCALER_RATES(1),
				PRESCALER_RATES(2),
				PRESCALER_RATES(4),
				PRESCALER_RATES(8),
				PRESCALER_RATES(16),
				PRESCALER_RATES(30),
#undef PRESCALER_RATES
#undef PRESCALER_RATE
			};

			// This table maps sections of the envelope ramp to different piecewise curve sections.
			// See: http://ploguechipsounds.blogspot.com/2010/03/sid-6581r3-adsr-tables-up-close.html
			//      http://ploguechipsounds.blogspot.com/2010/11/new-research-on-sid-adsr.html
			static const uint8 kLogTable[256]={
#define LOG_ENTRY(x) ((x) > 0x5D ? 0 :	\
				  (x) > 0x36 ? 1 :	\
				  (x) > 0x1A ? 2 :	\
				  (x) > 0x0E ? 3 :	\
				  (x) > 0x06 ? 4 : 5)
#define LOG_ENTRY_LINE(x)	LOG_ENTRY((x)+0),LOG_ENTRY((x)+1),LOG_ENTRY((x)+2),LOG_ENTRY((x)+3),	\
						LOG_ENTRY((x)+4),LOG_ENTRY((x)+5),LOG_ENTRY((x)+6),LOG_ENTRY((x)+7),	\
						LOG_ENTRY((x)+8),LOG_ENTRY((x)+9),LOG_ENTRY((x)+10),LOG_ENTRY((x)+11),	\
						LOG_ENTRY((x)+12),LOG_ENTRY((x)+13),LOG_ENTRY((x)+14),LOG_ENTRY((x)+15)
				LOG_ENTRY_LINE(0x00),
				LOG_ENTRY_LINE(0x10),
				LOG_ENTRY_LINE(0x20),
				LOG_ENTRY_LINE(0x30),
				LOG_ENTRY_LINE(0x40),
				LOG_ENTRY_LINE(0x50),
				LOG_ENTRY_LINE(0x60),
				LOG_ENTRY_LINE(0x70),
				LOG_ENTRY_LINE(0x80),
				LOG_ENTRY_LINE(0x90),
				LOG_ENTRY_LINE(0xA0),
				LOG_ENTRY_LINE(0xB0),
				LOG_ENTRY_LINE(0xC0),
				LOG_ENTRY_LINE(0xD0),
				LOG_ENTRY_LINE(0xE0),
				LOG_ENTRY_LINE(0xF0),
#undef LOG_ENTRY_LINE
#undef LOG_ENTRY
			};

			int env = ch->mEnvelope;

			switch(ch->mEnvelopeMode) {
				case 0:
					if (env >= 255) {
						ch->mEnvelopeMode = 1;
					} else {
						uint32 preAccumPrev = ch->mPrescalerAccum;
						uint32 preAccumNext = preAccumPrev + kPrescalerRates[0][ch->mAttack];
						ch->mPrescalerAccum = preAccumNext;

						if (preAccumNext < preAccumPrev)
							++env;
					}
					break;

				case 1:
				default:
					if (env > ch->mSustain) {
						uint32 preAccumPrev = ch->mPrescalerAccum;
						uint32 preAccumNext = preAccumPrev + kPrescalerRates[kLogTable[env]][ch->mDecay];
						ch->mPrescalerAccum = preAccumNext;

						if (preAccumNext < preAccumPrev)
							--env;
					}
					break;

				case 2:
					if (env > 0) {
						uint32 preAccumPrev = ch->mPrescalerAccum;
						uint32 preAccumNext = preAccumPrev + kPrescalerRates[kLogTable[env]][ch->mRelease];
						ch->mPrescalerAccum = preAccumNext;

						if (preAccumNext < preAccumPrev)
							--env;
					}
					break;
			}

			ch->mEnvelope = env;

			// The DC bias here is almost certainly wrong, although there *is* a DC bias in the real
			// SID. It is apparently relied upon to play digisounds with the 6581, which causes problems
			// with the 8580 due to the improved chip design and reduced bias.
			sint32 choutput = ((int)v - 128) * env;
			filtoutput += choutput & ch->mFilteredEnable;
			output += choutput & ch->mNonFilteredEnable;

			chprev = ch;
		}

		float outputF = (float)filtoutput * mVolumeScale;
		float outputNF = (float)output * mVolumeScale;

		int nativeCycles = 28*5 - mOutputCount;

		if (nativeCycles > 9) {
			mOutputAccumF += outputF;
			mOutputAccumNF += outputNF;
			mOutputCount += 9;
		} else {
			float nativeCyclesDiv9 = (float)nativeCycles * (1.0f / 9.0f);

			if (mOutputLevel < kAccumBufferSize) {
				mOutputAccumF += outputF * nativeCyclesDiv9;
				mOutputAccumNF += outputNF * nativeCyclesDiv9;

				// We use Direct Form I here because the Direct Form II of the biquad has a tendency
				// to blow up with very low cutoff frequencies (extremely high resonance relative to
				// output).
				float filtresult = mOutputAccumF * mFilterCoeffB0
						+ mFilterDelayX1 * mFilterCoeffB1
						+ mFilterDelayX2 * mFilterCoeffB2
						+ mFilterDelayY1 * mFilterCoeffA1
						+ mFilterDelayY2 * mFilterCoeffA2
						;

				mFilterDelayX2 = mFilterDelayX1;
				mFilterDelayX1 = mOutputAccumF;
				mFilterDelayY2 = mFilterDelayY1;

				// Perturb the delayed result to avoid denormals.
				mFilterDelayY1 = (filtresult + 1e-10f) - 1e-10f;

				mAccumBuffer[mOutputLevel++] = mOutputAccumNF + filtresult;
			}

			mOutputCount = 9 - nativeCycles;
			mOutputAccumF = outputF - outputF * nativeCyclesDiv9;
			mOutputAccumNF = outputNF - outputNF * nativeCyclesDiv9;
		}
	}

	mCycleAccum = cycles;
}

#if _MSC_VER >= 1400
#pragma float_control(pop)
#endif

void ATSlightSIDEmulator::WriteAudio(const ATSyncAudioMixInfo& mixInfo) {
	float *const dstLeft = mixInfo.mpLeft;
	float *const dstRightOpt = mixInfo.mpRight;
	const uint32 n = mixInfo.mCount;

	Flush();

	VDASSERT(n <= kAccumBufferSize);

	// if we don't have enough samples, pad out; eventually we'll catch up enough
	if (mOutputLevel < n) {
		memset(mAccumBuffer + mOutputLevel, 0, sizeof(mAccumBuffer[0]) * (n - mOutputLevel));

		mOutputLevel = n;
	}

	if (dstRightOpt) {
		for(uint32 i=0; i<n; ++i) {
			float v = mAccumBuffer[i];
			dstLeft[i] += v;
			dstRightOpt[i] += v;
		}
	} else {
		for(uint32 i=0; i<n; ++i)
			dstLeft[i] += mAccumBuffer[i];
	}

	// shift down accumulation buffers
	uint32 samplesLeft = mOutputLevel - n;

	if (samplesLeft)
		memmove(mAccumBuffer, mAccumBuffer + n, samplesLeft * sizeof(mAccumBuffer[0]));

	mOutputLevel = samplesLeft;
}

void ATSlightSIDEmulator::Flush() {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	uint32 dt = t - mLastUpdate;
	mLastUpdate = t;

	Run(dt);
}

sint32 ATSlightSIDEmulator::StaticReadControl(void *thisptr, uint32 addr) {
	uint8 addr8 = (uint8)addr;
	if (addr8 >= 0x20)
		return -1;

	return 0x33;
}

bool ATSlightSIDEmulator::StaticWriteControl(void *thisptr, uint32 addr, uint8 value) {
	uint8 addr8 = (uint8)addr;
	if (addr8 >= 0x20)
		return false;

	((ATSlightSIDEmulator *)thisptr)->WriteControl(addr8, value);
	return true;
}

///////////////////////////////////////////////////////////////////////////

class ATDeviceSlightSID : public VDAlignedObject<16>
					, public ATDevice
					, public IATDeviceMemMap
					, public IATDeviceScheduling
					, public IATDeviceAudioOutput
					, public IATDeviceDiagnostics
{
public:
	ATDeviceSlightSID();

	virtual void *AsInterface(uint32 id) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void WarmReset() override;
	virtual void ColdReset() override;
	virtual void Init() override;
	virtual void Shutdown() override;

public: // IATDeviceMemMap
	virtual void InitMemMap(ATMemoryManager *memmap) override;
	virtual bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:	// IATDeviceScheduling
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceAudioOutput
	virtual void InitAudioOutput(IATAudioMixer *mixer) override;

public:	// IATDeviceDiagnostics
	virtual void DumpStatus(ATConsoleOutput& output) override;

private:
	static sint32 ReadByte(void *thisptr0, uint32 addr);
	static bool WriteByte(void *thisptr0, uint32 addr, uint8 value);

	ATMemoryManager *mpMemMan;
	ATScheduler *mpScheduler;
	IATAudioMixer *mpAudioMixer;

	ATSlightSIDEmulator mSlightSID;
};

void ATCreateDeviceSlightSID(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceSlightSID> p(new ATDeviceSlightSID);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefSlightSID = { "slightsid", nullptr, L"SlightSID", ATCreateDeviceSlightSID };

ATDeviceSlightSID::ATDeviceSlightSID()
	: mpMemMan(nullptr)
	, mpScheduler(nullptr)
	, mpAudioMixer(nullptr)
{
}

void *ATDeviceSlightSID::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceMemMap::kTypeID:
			return static_cast<IATDeviceMemMap *>(this);

		case IATDeviceScheduling::kTypeID:
			return static_cast<IATDeviceScheduling *>(this);

		case IATDeviceAudioOutput::kTypeID:
			return static_cast<IATDeviceAudioOutput *>(this);

		case IATDeviceDiagnostics::kTypeID:
			return static_cast<IATDeviceDiagnostics *>(this);

		case ATSlightSIDEmulator::kTypeID:
			return static_cast<ATSlightSIDEmulator *>(&mSlightSID);

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceSlightSID::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefSlightSID;
}

void ATDeviceSlightSID::WarmReset() {
	mSlightSID.WarmReset();
}

void ATDeviceSlightSID::ColdReset() {
	mSlightSID.ColdReset();
}

void ATDeviceSlightSID::Init() {
	mSlightSID.Init(mpMemMan, mpScheduler, mpAudioMixer);
}

void ATDeviceSlightSID::Shutdown() {
	mSlightSID.Shutdown();

	mpAudioMixer = nullptr;
	mpScheduler = nullptr;
	mpMemMan = nullptr;
}

void ATDeviceSlightSID::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;
}

bool ATDeviceSlightSID::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD500;
		hi = 0xD600;
		return true;
	}

	return false;
}

void ATDeviceSlightSID::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATDeviceSlightSID::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;
}

void ATDeviceSlightSID::DumpStatus(ATConsoleOutput& output) {
	mSlightSID.DumpStatus(output);
}
