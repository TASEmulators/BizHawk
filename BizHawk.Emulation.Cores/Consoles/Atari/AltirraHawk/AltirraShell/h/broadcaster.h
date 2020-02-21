//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - broadcast notification module
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

#ifndef f_ATS_BROADCASTER_H
#define f_ATS_BROADCASTER_H

#include <vd2/system/function.h>

struct ATSEventRegHandle;

class ATSEventImpl;

ATSEventImpl *ATSInitEvent(void (VDCDECL *deleter)());
void ATSDestroyEvent(ATSEventImpl *);

template<class T>
class ATSEvent {
public:
	static void VDCDECL Shutdown() {
		ATSDestroyEvent(spEvent);
		spEvent = nullptr;
	}

	static ATSEventImpl *GetEventImpl() {
		if (!spEvent)
			spEvent = ATSInitEvent(Shutdown);

		return spEvent;
	}

private:
	static ATSEventImpl *spEvent;
};

template<class T>
ATSEventImpl *ATSEvent<T>::spEvent;

ATSEventRegHandle *ATSRegisterForEvent(ATSEventImpl *eventImpl, const vdfunction<void(const void *)>& fn);
void ATSUnregisterForEvent(ATSEventRegHandle *);
void ATSRaiseEvent(ATSEventImpl *eventImpl, const void *args);

template<class T>
void ATSRaiseEvent(const T& args) {
	ATSRaiseEvent(ATSEvent<T>::GetEventImpl(), &args);
}

class ATSEventRegistration {
public:
	~ATSEventRegistration();

	void Unregister();

	template<class T, class Fn>
	void Register(const Fn& fn) {
		mpRegistration = ATSRegisterForEvent(ATSEvent<T>::GetEventImpl(),
			[=](const void *p) {
				fn(*(const T*)p);
			}
		);
	}

	ATSEventRegHandle *mpRegistration = nullptr;
};

#endif
