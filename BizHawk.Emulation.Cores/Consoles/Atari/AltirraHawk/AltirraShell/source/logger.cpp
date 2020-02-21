//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - multithreaded logging module
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
#include <vd2/system/thread.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/VDString.h>
#include <at/atcore/logging.h>
#include "logger.h"

class ATSLogger final : public IATSLogger {
public:
	ATSLogger();

	void SetLogHandler(vdfunction<void()> fn) override;

	void BeginAsync() override;
	uint32 GetTextLength() const override { return (uint32)mAppendBuffer.size(); }
	const wchar_t *LockText() override;
	void UnlockTextAndClear() override;
	void EndAsync() override;

private:
	void Write(const char *s);

	static void LogWrite(ATLogChannel *channel, const char *s);
	static void LogWriteV(ATLogChannel *channel, const char *format, va_list args);

	VDCriticalSection	mMutex;
	VDStringW	mAppendBuffer;

	vdfunction<void()> mpLogHandler;

	static const uint32 kLogCaptureLimit = 4096;
};

ATSLogger *g_pATSLogger;

ATSLogger::ATSLogger() {
	ATLogSetWriteCallbacks(LogWrite, LogWriteV);
}

void ATSLogger::SetLogHandler(vdfunction<void()> fn) {
	mMutex.Lock();
	mpLogHandler = fn;
	mMutex.Unlock();
}

void ATSLogger::BeginAsync() {
	mMutex.Lock();
}

const wchar_t *ATSLogger::LockText() {
	mAppendBuffer.push_back(0);

	return mAppendBuffer.data();
}

void ATSLogger::UnlockTextAndClear() {
	mAppendBuffer.clear();
}

void ATSLogger::EndAsync() {
	mMutex.Unlock();
}

void ATSLogger::Write(const char *s) {
	if (!*s)
		return;

	mMutex.Lock();

	do {
		const char *eol = strchr(s, '\n');
		const char *end = eol;

		if (!end)
			end = s + strlen(s);

		if (mAppendBuffer.size() >= kLogCaptureLimit)
			mAppendBuffer.clear();

		for(const char *t = s; t != end; ++t)
			mAppendBuffer += (wchar_t)(uint8)*t;

		if (!eol)
			break;

		mAppendBuffer += '\r';
		mAppendBuffer += '\n';

		s = eol+1;
	} while(*s);

	if (mpLogHandler)
		mpLogHandler();

	mMutex.Unlock();
}

void ATSLogger::LogWrite(ATLogChannel *channel, const char *s) {
	g_pATSLogger->Write(s);
}

void ATSLogger::LogWriteV(ATLogChannel *channel, const char *format, va_list args) {
	char buf[3072];

	if ((unsigned)_vsnprintf(buf, 3072, format, args) < 3072)
		g_pATSLogger->Write(buf);
}

///////////////////////////////////////////////////////////////////////////

IATSLogger *ATSGetLogger() {
	return g_pATSLogger;
}

void ATSInitLogger() {
	g_pATSLogger = new ATSLogger;

	for(auto *p = ATLogGetFirstChannel(); p; p = ATLogGetNextChannel(p)) {
		if (!vdstricmp(p->GetName(), "siocmd"))
			p->SetEnabled(true);

		if (!vdstricmp(p->GetName(), "siosteps"))
			p->SetEnabled(true);

		if (!vdstricmp(p->GetName(), "disk"))
			p->SetEnabled(true);
	}
}

void ATSShutdownLogger() {
	vdsafedelete <<= g_pATSLogger;
}
