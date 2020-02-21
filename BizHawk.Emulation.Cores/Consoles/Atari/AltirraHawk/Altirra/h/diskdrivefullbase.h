//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.
//
//=========================================================================
// Full disk drive emulator utilities
//
// This contains common utilities for all full disk drive emulators, i.e.
// ones that actually emulate the controller and firmware.
//

#ifndef f_AT_DISKDRIVEFULLBASE_H
#define f_AT_DISKDRIVEFULLBASE_H

#include <at/atcore/audiomixer.h>
#include <at/atcore/device.h>

class ATDiskDriveAudioPlayer final : public IATDeviceAudioOutput
{
	ATDiskDriveAudioPlayer(const ATDiskDriveAudioPlayer&) = delete;
	ATDiskDriveAudioPlayer& operator=(const ATDiskDriveAudioPlayer&) = delete;

public:
	ATDiskDriveAudioPlayer();
	~ATDiskDriveAudioPlayer();

	void Shutdown();
	void Reset();

	void SetRotationSoundEnabled(bool enabled);
	void PlayStepSound(ATAudioSampleId sampleId, float volume);

	IATAudioMixer *GetMixer() const { return mpAudioMixer; }

public:		// IATDeviceAudioOutput
	void InitAudioOutput(IATAudioMixer *mixer) override;

private:
	IATAudioMixer *mpAudioMixer = nullptr;

	ATSoundId mRotationSoundId = ATSoundId::Invalid;
};

#endif
