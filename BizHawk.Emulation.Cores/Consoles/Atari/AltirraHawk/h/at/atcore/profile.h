//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - process Profile interface
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_AT_ATCORE_PROFILE_H
#define f_AT_ATCORE_PROFILE_H

enum ATProfileEvent {
	kATProfileEvent_BeginFrame,
};

enum ATProfileRegion {
	kATProfileRegion_Idle,
	kATProfileRegion_Simulation,
	kATProfileRegion_NativeEvents,
	kATProfileRegion_DisplayTick,
	kATProfileRegionCount
};

class IATProfiler {
public:
	virtual void OnEvent(ATProfileEvent event) = 0;
	virtual void BeginRegion(ATProfileRegion region) = 0;
	virtual void EndRegion(ATProfileRegion region) = 0;
};

extern IATProfiler *g_pATProfiler;

inline void ATProfileMarkEvent(ATProfileEvent event) {
	if (g_pATProfiler)
		g_pATProfiler->OnEvent(event);
}

inline void ATProfileBeginRegion(ATProfileRegion region) {
	if (g_pATProfiler)
		g_pATProfiler->BeginRegion(region);
}

inline void ATProfileEndRegion(ATProfileRegion region) {
	if (g_pATProfiler)
		g_pATProfiler->EndRegion(region);
}

#endif
