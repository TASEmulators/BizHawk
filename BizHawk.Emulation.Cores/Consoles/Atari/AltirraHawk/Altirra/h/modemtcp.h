//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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

#ifndef f_AT_MODEMTCP_H
#define f_AT_MODEMTCP_H

#include <vd2/system/VDString.h>

class IATModemDriverCallback;
struct ATRS232Config;

enum ATModemEvent {
	kATModemEvent_None,
	kATModemEvent_GenericError,
	kATModemEvent_AllocFail,
	kATModemEvent_NameLookupFailed,
	kATModemEvent_ConnectFailed,
	kATModemEvent_ConnectionClosing,
	kATModemEvent_ConnectionDropped,
	kATModemEvent_LineInUse,
	kATModemEvent_NoDialTone,
	kATModemEvent_Connected,
};

enum ATModemPhase {
	kATModemPhase_Init,
	kATModemPhase_NameLookup,
	kATModemPhase_Connecting,
	kATModemPhase_Connect,
	kATModemPhase_Listen,
	kATModemPhase_Accept,
	kATModemPhase_Connected
};

class IATModemDriver {
public:
	virtual ~IATModemDriver() {}

	virtual bool Init(const char *address, const char *service, uint32 port, bool loggingEnabled, IATModemDriverCallback *callback) = 0;
	virtual void Shutdown() = 0;

	virtual bool GetLastIncomingAddress(VDStringA& address, uint32& port) = 0;

	virtual uint32 Write(const void *data, uint32 len) = 0;
	virtual uint32 Read(void *buf, uint32 len) = 0;
	virtual bool ReadLogMessages(VDStringA& messages) = 0;

	virtual void SetLoggingEnabled(bool enabled) = 0;
	virtual void SetConfig(const ATRS232Config& config) = 0;
};

class IATModemDriverCallback {
public:
	virtual void OnReadAvail(IATModemDriver *sender, uint32 len) = 0;
	virtual void OnWriteAvail(IATModemDriver *sender) = 0;
	virtual void OnEvent(IATModemDriver *sender, ATModemPhase phase, ATModemEvent event) = 0;
};

IATModemDriver *ATCreateModemDriverTCP();

#endif
