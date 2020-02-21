//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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

#ifndef f_AT_AUDIOOUTPUT_H
#define f_AT_AUDIOOUTPUT_H

#ifdef _MSC_VER
#pragma once
#endif

#include <at/atcore/audiosource.h>
#include <at/atcore/audiomixer.h>

class IATUIRenderer;
class IATSyncAudioSource;
class IATAudioMixer;

class IATAudioTap {
public:
	virtual void WriteRawAudio(const float *left, const float *right, uint32 count, uint32 timestamp) = 0;
};

enum ATAudioApi {
	kATAudioApi_WaveOut,
	kATAudioApi_DirectSound,
	kATAudioApi_XAudio2,
	kATAudioApi_WASAPI,
	kATAudioApi_Auto,
	kATAudioApiCount
};

class IATAudioOutput {
public:
	virtual ~IATAudioOutput() {}

	virtual void Init(IATSyncAudioSamplePlayer *samplePlayer) = 0;

	virtual ATAudioApi GetApi() = 0;
	virtual void SetApi(ATAudioApi api) = 0;

	virtual void SetAudioTap(IATAudioTap *tap) = 0;

	virtual IATUIRenderer *GetStatusRenderer() = 0;
	virtual void SetStatusRenderer(IATUIRenderer *uir) = 0;

	virtual IATAudioMixer& AsMixer() = 0;

	virtual void SetCyclesPerSecond(double cps, double repeatfactor) = 0;

	virtual bool GetMute() = 0;
	virtual void SetMute(bool mute) = 0;

	virtual float GetVolume() = 0;
	virtual void SetVolume(float vol) = 0;

	virtual float GetMixLevel(ATAudioMix mix) const = 0;
	virtual void SetMixLevel(ATAudioMix mix, float level) = 0;

	virtual int GetLatency() = 0;
	virtual void SetLatency(int ms) = 0;

	virtual int GetExtraBuffer() = 0;
	virtual void SetExtraBuffer(int ms) = 0;

	virtual void SetFiltersEnabled(bool enable) = 0;

	virtual void Pause() = 0;
	virtual void Resume() = 0;

	virtual void WriteAudio(
		const float *left,
		const float *right,
		uint32 count, bool pushAudio, uint64 timestamp) = 0;
};

IATAudioOutput *ATCreateAudioOutput();

#endif	// f_AT_AUDIOOUTPUT_H
