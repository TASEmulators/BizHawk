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

#ifndef f_AT_AUDIOMONITOR_H
#define f_AT_AUDIOMONITOR_H

#include "pokey.h"

class IATUIRenderer;

class ATAudioMonitor {
	ATAudioMonitor(const ATAudioMonitor&);
	ATAudioMonitor& operator=(const ATAudioMonitor&);
public:
	ATAudioMonitor();
	~ATAudioMonitor();

	void Init(ATPokeyEmulator *pokey, IATUIRenderer *uir, bool secondary);
	void Shutdown();

	void Update(ATPokeyAudioLog **log, ATPokeyRegisterState **rstate);
	void Reset();

protected:
	ATPokeyEmulator		*mpPokey;
	IATUIRenderer		*mpUIRenderer;
	bool				mbSecondary;

	ATPokeyAudioLog		mLog;
	ATPokeyRegisterState	mRegisterState;

	ATPokeyAudioState	mAudioStates[312];
};

#endif	// f_AT_AUDIOMONITOR_H
