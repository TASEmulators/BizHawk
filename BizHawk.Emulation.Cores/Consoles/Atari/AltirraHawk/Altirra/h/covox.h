//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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

#ifndef f_AT_COVOX_H
#define f_AT_COVOX_H

#include <vd2/system/memory.h>
#include <at/atcore/audiosource.h>

class ATScheduler;
class ATMemoryManager;
class ATMemoryLayer;
class IATAudioMixer;
class ATConsoleOutput;

class ATCovoxEmulator final : public VDAlignedObject<16>, public IATSyncAudioSource {
	ATCovoxEmulator(const ATCovoxEmulator&) = delete;
	ATCovoxEmulator& operator=(const ATCovoxEmulator&) = delete;
public:
	ATCovoxEmulator();
	~ATCovoxEmulator();
	
	bool IsFourChannels() const { return mbFourCh; }
	void SetFourChannels(bool ch4) { mbFourCh = ch4; }
	void SetAddressRange(uint32 lo, uint32 hi, bool passWrites);

	void SetEnabled(bool enable);

	void Init(ATMemoryManager *memMan, ATScheduler *sch, IATAudioMixer *mixer);
	void Shutdown();

	void ColdReset();
	void WarmReset();

	void DumpStatus(ATConsoleOutput&);

	void WriteControl(uint8 addr, uint8 value);
	void WriteMono(uint8 value);

	void Run(int cycles);

public:
	bool RequiresStereoMixingNow() const override { return mbUnbalancedSticky; }
	void WriteAudio(const ATSyncAudioMixInfo& mixInfo) override;

protected:
	void Flush();
	void InitMapping();

	static sint32 StaticReadControl(void *thisptr, uint32 addr);
	static bool StaticWriteControl(void *thisptr, uint32 addr, uint8 value);

	ATMemoryLayer *mpMemLayerControl = nullptr;
	ATScheduler *mpScheduler = nullptr;
	ATMemoryManager *mpMemMan = nullptr;
	IATAudioMixer *mpAudioMixer = nullptr;

	uint8	mVolume[4] = {};

	float	mOutputAccumLeft = 0;
	float	mOutputAccumRight = 0;
	uint32	mOutputCount = 0;
	uint32	mOutputLevel = 0;
	bool	mbUnbalanced = false;
	bool	mbUnbalancedSticky = false;
	bool	mbEnabled = true;

	uint32	mAddrLo = 0xD600;
	uint32	mAddrHi = 0xD6FF;
	bool	mbFourCh = false;
	bool	mbPassWrites = true;

	uint32	mLastUpdate = 0;

	enum {
		kAccumBufferSize = 1536
	};

	VDALIGN(16) float mAccumBufferLeft[kAccumBufferSize];
	VDALIGN(16) float mAccumBufferRight[kAccumBufferSize];
};

#endif
