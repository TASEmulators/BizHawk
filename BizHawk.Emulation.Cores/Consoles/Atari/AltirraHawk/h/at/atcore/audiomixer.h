//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - audio mixing system definitions
//	Copyright (C) 2008-2018 Avery Lee
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

//=========================================================================
// Synchronous audio mixer output
//
// The synchronous audio mixer combines audio from multiple sources in
// emulation synchronous time. The mixer divides time into discrete mixing
// chunks and then polls all sources to mix audio into the buffer for each
// chunk. This is always done after the emulation time has passed. More
// details are in audiosource.h.
//
// For simple looping or one-shot sounds, the mixer also provides a sample
// player that automatically tracks sound lifetimes.
//

#ifndef f_AT_ATCORE_AUDIOMIXER_H
#define f_AT_ATCORE_AUDIOMIXER_H

#include <vd2/system/vdtypes.h>

class IATSyncAudioSource;
class IATSyncAudioSamplePlayer;
class IVDRefCount;

enum ATAudioMix {
	kATAudioMix_Drive,
	kATAudioMix_Covox,
	kATAudioMix_Modem,
	kATAudioMixCount
};

enum ATAudioSampleId : uint32 {
	kATAudioSampleId_None,
	kATAudioSampleId_DiskRotation,
	kATAudioSampleId_DiskStep1,
	kATAudioSampleId_DiskStep2,
	kATAudioSampleId_DiskStep2H,
	kATAudioSampleId_DiskStep3
};

enum class ATSoundId : uint32 {
	Invalid = 0
};

class IATAudioMixer {
public:
	virtual void AddSyncAudioSource(IATSyncAudioSource *src) = 0;
	virtual void RemoveSyncAudioSource(IATSyncAudioSource *src) = 0;

	virtual IATSyncAudioSamplePlayer& GetSamplePlayer() = 0;
};

class IATAudioSampleSource {
public:
	// Mix samples from the source into the destination buffer, using the
	// given volume (sample scale). Offset is the number of samples (not
	// cycles!) from the beginning of the sound to the mixing position.
	virtual void MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate) = 0;
};

class IATSyncAudioSamplePlayer {
public:
	virtual ATSoundId AddSound(ATAudioMix mix, uint32 delay, ATAudioSampleId sampleId, float volume) = 0;
	virtual ATSoundId AddLoopingSound(ATAudioMix mix, uint32 delay, ATAudioSampleId sampleId, float volume) = 0;

	virtual ATSoundId AddSound(ATAudioMix mix, uint32 delay, const sint16 *sample, uint32 len, float volume) = 0;
	virtual ATSoundId AddLoopingSound(ATAudioMix mix, uint32 delay, const sint16 *sample, uint32 len, float volume) = 0;

	virtual ATSoundId AddSound(ATAudioMix mix, uint32 delay, IATAudioSampleSource *src, IVDRefCount *owner, uint32 len, float volume) = 0;
	virtual ATSoundId AddLoopingSound(ATAudioMix mix, uint32 delay, IATAudioSampleSource *src, IVDRefCount *owner, float volume) = 0;

	// Stop a sound immediately in real time. This may stop it up to one
	// mixing frame before the current simulated time.
	virtual void ForceStopSound(ATSoundId id) = 0;

	// Stop a sound immediately in simulation time.
	virtual void StopSound(ATSoundId id) = 0;

	// Stop a sound at the given time. If the time has already passed, the
	// sound is stopped immediately. Otherwise, the sound will be truncated
	// to the given time.
	virtual void StopSound(ATSoundId id, uint64 time) = 0;
};

#endif
