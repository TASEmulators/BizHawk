//	Altirra - Atari 800/800XL/5200 emulator
//	Core library -- Controller port interfaces
//	Copyright (C) 2009-2016 Avery Lee
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

//=========================================================================
// Device controller port interface
//
// The device port manager provides access to the PIA inputs and outputs
// for the controller ports. Two types of connections can be registered,
// inputs to the PIA and outputs from the PIA. For all I/O operations,
// bits 0-7 contain port A data (controller ports 1-2) and bits 8-15
// contain port B data (controller ports 3-4). For the XL/XE series, which
// only has two controller ports, port B contains the banking data instead.
//

#ifndef f_AT_ATCORE_DEVICEPORT_H
#define f_AT_ATCORE_DEVICEPORT_H

#include <vd2/system/unknown.h>

typedef void (*ATPortOutputFn)(void *data, uint32 outputState);

class IATDevicePortManager {
public:
	// Allocate a new input to supply signals to the PIA. Returns an input index
	// or -1 if no inputs are available.
	virtual int AllocInput() = 0;

	// Free an input from AllocInput(). Silently ignored for invalid index (-1).
	virtual void FreeInput(int index) = 0;

	// Change the signals supplied to the PIA by an input. Redundant sets are
	// tossed and the call is silently ignored for an invalid index (-1).
	virtual void SetInput(int index, uint32 rval) = 0;

	// Get the current outputs from the PIA.
	virtual uint32 GetOutputState() const = 0;

	// Allocate a new output from the PIA. The output function is called
	// whenever a relevant change occurs, according to the supplied change
	// mask. The function is not called initially, so self-init must occur.
	// -1 is returned if no more output slots are available.
	virtual int AllocOutput(ATPortOutputFn fn, void *ptr, uint32 changeMask) = 0;

	// Free an output allocated by AllocOutput(). It is OK to call this with
	// the invalid output ID (-1).
	virtual void FreeOutput(int index) = 0;
};

#endif
