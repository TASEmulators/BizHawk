//	Altirra - Atari 800/800XL/5200 emulator
//	POKEY audio monitor
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
#include "audiomonitor.h"
#include "uirender.h"

ATAudioMonitor::ATAudioMonitor()
	: mpPokey(NULL)
	, mpUIRenderer(NULL)
	, mbSecondary(false)
{
}

ATAudioMonitor::~ATAudioMonitor() {
	Shutdown();
}

void ATAudioMonitor::Init(ATPokeyEmulator *pokey, IATUIRenderer *uir, bool secondary) {
	mpPokey = pokey;
	mpUIRenderer = uir;
	mbSecondary = secondary;

	mLog.mpStates = mAudioStates;
	mLog.mRecordedCount = 0;
	mLog.mMaxCount = sizeof(mAudioStates)/sizeof(mAudioStates[0]);

	pokey->SetAudioLog(&mLog);
	uir->SetAudioMonitor(mbSecondary, this);
}

void ATAudioMonitor::Shutdown() {
	if (mpPokey) {
		mpPokey->SetAudioLog(NULL);
		mpPokey = NULL;
	}

	if (mpUIRenderer) {
		mpUIRenderer->SetAudioMonitor(mbSecondary, NULL);
		mpUIRenderer = NULL;

	}
}

void ATAudioMonitor::Update(ATPokeyAudioLog **log, ATPokeyRegisterState **rstate) {
	mpPokey->GetRegisterState(mRegisterState);

	*log = &mLog;
	*rstate = &mRegisterState;
}

void ATAudioMonitor::Reset() {
	mLog.mRecordedCount = 0;
}
