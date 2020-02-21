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

#include "stdafx.h"
#include "diskdrivefullbase.h"

ATDiskDriveAudioPlayer::ATDiskDriveAudioPlayer() {
}

ATDiskDriveAudioPlayer::~ATDiskDriveAudioPlayer() {
	VDASSERT(!mpAudioMixer);
}

void ATDiskDriveAudioPlayer::Shutdown() {
	if (mpAudioMixer) {
		SetRotationSoundEnabled(false);
		mpAudioMixer = nullptr;
	}
}

void ATDiskDriveAudioPlayer::SetRotationSoundEnabled(bool enabled) {
	if (!mpAudioMixer)
		return;

	if (enabled) {
		if (mRotationSoundId == ATSoundId::Invalid) {
			mRotationSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Drive, 0, kATAudioSampleId_DiskRotation, 1.0f);
		}
	} else {
		if (mRotationSoundId != ATSoundId::Invalid) {
			mpAudioMixer->GetSamplePlayer().StopSound(mRotationSoundId);
			mRotationSoundId = ATSoundId::Invalid;
		}
	}
}

void ATDiskDriveAudioPlayer::PlayStepSound(ATAudioSampleId sampleId, float volume) {
	if (!mpAudioMixer)
		return;

	mpAudioMixer->GetSamplePlayer().AddSound(kATAudioMix_Drive, 0, sampleId, volume);
}

void ATDiskDriveAudioPlayer::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;
}

