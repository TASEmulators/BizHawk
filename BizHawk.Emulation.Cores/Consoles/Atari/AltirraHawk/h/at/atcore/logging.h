//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - logging support
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

#ifndef f_AT_ATCORE_LOGGING_H
#define f_AT_ATCORE_LOGGING_H

#include <stdarg.h>

enum ATLogFlags : uint32 {
	kATLogFlags_None = 0,
	kATLogFlags_Timestamp = 0x1,
	kATLogFlags_CassettePos = 0x2
};

class ATLogChannel;

void ATLogRegisterChannel(ATLogChannel *channel);
void ATLogWrite(ATLogChannel *channel, const char *s);
void ATLogWriteV(ATLogChannel *channel, const char *format, va_list args);

typedef void (*ATLogWriteFn)(ATLogChannel *channel, const char *s);
typedef void (*ATLogWriteVFn)(ATLogChannel *channel, const char *format, va_list args);
void ATLogSetWriteCallbacks(ATLogWriteFn write, ATLogWriteVFn writev);

ATLogChannel *ATLogGetFirstChannel();
ATLogChannel *ATLogGetNextChannel(ATLogChannel *);

class ATLogChannel {
	ATLogChannel(const ATLogChannel&) = delete;
	ATLogChannel& operator=(const ATLogChannel&) = delete;

	struct Dummy {
		void A() {}
	};
	typedef void (Dummy::*DummyMemberFn)();

public:
	ATLogChannel(bool enabled, bool tagged, const char *shortName, const char *longDesc)
		: mbEnabled(enabled)
		, mTagFlags(tagged ? kATLogFlags_Timestamp : 0)
		, mpShortName(shortName)
		, mpLongDesc(longDesc)
	{
		ATLogRegisterChannel(this);
	}

	bool IsEnabled() const { return mbEnabled; }
	void SetEnabled(bool enabled) { mbEnabled = enabled; }

	uint32 GetTagFlags() const { return mTagFlags; }
	void SetTagFlags(uint32 flags) { mTagFlags = flags; }

	const char *GetName() const { return mpShortName; }
	const char *GetDesc() const { return mpLongDesc; }

	operator DummyMemberFn() const { return mbEnabled ? &Dummy::A : NULL; }
	void operator<<=(const char *message);
	void operator()(const char *message, ...);

protected:
	friend void ATLogRegisterChannel(ATLogChannel *channel);
	friend ATLogChannel *ATLogGetNextChannel(ATLogChannel *);

	ATLogChannel *mpNext;
	bool mbEnabled;
	uint32 mTagFlags;
	const char *mpShortName;
	const char *mpLongDesc;
};

#endif
