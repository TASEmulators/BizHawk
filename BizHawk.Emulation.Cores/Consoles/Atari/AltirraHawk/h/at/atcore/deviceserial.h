//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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

#ifndef f_AT_ATCORE_DEVICESERIAL_H
#define f_AT_ATCORE_DEVICESERIAL_H

#include <vd2/system/refcount.h>
#include <vd2/system/unknown.h>
#include <vd2/system/function.h>

// State of control signals from terminal to device.
struct ATDeviceSerialTerminalState {
	bool mbDataTerminalReady;
	bool mbRequestToSend;
};

// State of control signals from device to terminal.
struct ATDeviceSerialStatus {
	bool mbCarrierDetect;
	bool mbClearToSend;
	bool mbDataSetReady;
	bool mbHighSpeed;
	bool mbRinging;
};

class IATDeviceSerial : public IVDRefUnknown {
public:
	enum { kTypeID = 'adsr' };

	virtual void SetOnStatusChange(const vdfunction<void(const ATDeviceSerialStatus&)>& fn) = 0;

	virtual void SetTerminalState(const ATDeviceSerialTerminalState&) = 0;
	virtual ATDeviceSerialStatus GetStatus() = 0;
	virtual bool Read(uint32 baudRate, uint8& c, bool& framingError) = 0;
	virtual bool Read(uint32& baudRate, uint8& c) = 0;
	virtual void Write(uint32 baudRate, uint8 c) = 0;

	virtual void FlushBuffers() = 0;
};

#endif
