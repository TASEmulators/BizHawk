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

#include <stdafx.h>
#include <vd2/system/strutil.h>
#include <at/atcore/logging.h>

ATLogChannel *g_ATLogChannels;
ATLogWriteFn g_pATLogWrite = [](ATLogChannel *, const char *s) {};
ATLogWriteVFn g_pATLogWriteV = [](ATLogChannel *, const char *format, va_list args) {};

void ATLogRegisterChannel(ATLogChannel *channel) {
	channel->mpNext = g_ATLogChannels;
	g_ATLogChannels = channel;
}

ATLogChannel *ATLogGetFirstChannel() {
	return g_ATLogChannels;
}

ATLogChannel *ATLogGetNextChannel(ATLogChannel *p) {
	return p->mpNext;
}

void ATLogWrite(ATLogChannel *channel, const char *s) {
	g_pATLogWrite(channel, s);
}

void ATLogWriteV(ATLogChannel *channel, const char *format, va_list args) {
	g_pATLogWriteV(channel, format, args);
}

void ATLogSetWriteCallbacks(ATLogWriteFn write, ATLogWriteVFn writev) {
	g_pATLogWrite = write;
	g_pATLogWriteV = writev;
}

///////////////////////////////////////////////////////////////////////////

void ATLogChannel::operator<<=(const char *message) {
	if (!mbEnabled)
		return;

	ATLogWrite(this, message);
}

void ATLogChannel::operator()(const char *format, ...) {
	if (!mbEnabled)
		return;

	va_list val;

	va_start(val, format);
	ATLogWriteV(this, format, val);
	va_end(val);
}
