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

#ifndef f_AT_AUDIORAWSOURCE_H
#define f_AT_AUDIORAWSOURCE_H

#include <vd2/system/vdstl.h>
#include <at/atcore/audiomixer.h>
#include <at/atcore/audiosource.h>

class IATAudioOutput;

class ATAudioRawSource final : public IATSyncAudioSource {
	ATAudioRawSource(const ATAudioRawSource&) = delete;
	ATAudioRawSource& operator=(const ATAudioRawSource&) = delete;
public:
	ATAudioRawSource();
	~ATAudioRawSource();
	
	void Init(IATAudioMixer *mixer);
	void Shutdown();

	void SetOutput(uint32 t, float level);

public:
	bool RequiresStereoMixingNow() const override;
	void WriteAudio(const ATSyncAudioMixInfo& mixInfo) override;

protected:
	IATAudioMixer *mpAudioMixer = nullptr;

	float	mLevel = {};
	float	mStartLevel = {};

	struct Edge {
		uint32 mTime;
		float mLevel;

		bool operator==(const Edge& other) const {
			return mTime == other.mTime && mLevel == other.mLevel;
		}
	};

	vdfastdeque<Edge> mLevelEdges;
};

#endif
