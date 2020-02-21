//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2010 Avery Lee
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

/////////////////////////////////////////////////////////////////////////////
// The R-Time 8 uses a M3002-16PI hooked to the cartridge control range of
// the Atari memory space. A 74HCT138 decoder checks address bits and only
// enables the M3002 if A7-A3 are 11101, putting the RT8 at D5B8-D5BF. The
// low three address bits and upper four data bits don't matter; the M3002
// is driven via a three step sequence via the data bus only. The IRQ,
// PULSE, and BUSY lines are not connected, so the alarm and timer are of
// limited use.
//
// For emulation purposes, we currently just reflect the time of day into
// registers 0-6. Week number is initialized in register 7 but not updated.
// This is necessary since SpartaDOS X overwrites that register as part of
// its detection routine. Registers 8-15 are merely reflected as R/W
// storage; the alarm and timer do not count, and all control bits are
// ignored.

#include <stdafx.h>
#include <time.h>
#include "rtime8.h"
#include "memorymanager.h"

namespace {
	uint8 ToBCD(uint8 v) {
		return ((v / 10) << 4) + (v % 10);
	}
}

ATRTime8Emulator::ATRTime8Emulator()
	: mAddress(0)
	, mPhase(0)
{
	time_t t;

	time(&t);
	const tm *p = localtime(&t);

	memset(mRAM, 0, sizeof mRAM);
	mRAM[7] = ToBCD((p->tm_yday / 7) + 1);
	mRAM[15] = 0x01;
}

ATRTime8Emulator::~ATRTime8Emulator() {
}

uint8 ATRTime8Emulator::ReadControl(uint8 addr) {
	uint8 v = DebugReadControl(addr);

	static const uint8 kReadAdvanceLookup[3] = {0, 2, 0};
	mPhase = kReadAdvanceLookup[mPhase];

	return v;
}

uint8 ATRTime8Emulator::DebugReadControl(uint8 addr) {
	if (mPhase == 0)
		return 0x00;		// 0000 = idle, 1111 = update pending

	const tm *p = NULL;
	if (mAddress < 8) {
		time_t t;

		time(&t);
		p = localtime(&t);
	}

	uint8 v = 0;

	switch(mAddress) {
		case 0:		// seconds (0-59)
			v = p->tm_sec;
			break;
		case 1:		// minutes (0-59)
			v = p->tm_min;
			break;
		case 2:		// hours (0-23)
			v = p->tm_hour;
			break;
		case 3:		// day (1-31)
			v = p->tm_mday;
			break;
		case 4:		// month (1-12)
			v = p->tm_mon + 1;
			break;
		case 5:		// year (0-99)
			v = p->tm_year % 100;
			break;
		case 6:		// weekday (1-7)
			v = p->tm_wday + 1;
			break;
		case 7:		// week number (1-53) -- IMPORTANT: SDX writes this as a test!
		case 8:		// alarm seconds
		case 9:		// alarm minutes
		case 10:	// alarm hours
		case 11:	// alarm day
		case 12:	// timer seconds
		case 13:	// timer minutes
		case 14:	// timer hours
		case 15:	// control status
			return mPhase == 1 ? mRAM[mAddress] >> 4 : mRAM[mAddress] & 15;
	}

	return mPhase == 1 ? v / 10 : v % 10;
}

void ATRTime8Emulator::WriteControl(uint8 addr, uint8 value) {
	switch(mPhase) {
		case 0:
			mAddress = value & 0x0f;
			break;

		case 1:
			if (mAddress >= 7)
				mRAM[mAddress] = (mRAM[mAddress] & 0x0F) + (value << 4);
			break;
		case 2:
			if (mAddress >= 7)
				mRAM[mAddress] = (mRAM[mAddress] & 0xF0) + (value & 15);
			break;
	}

	static const uint8 kWriteAdvanceLookup[3] = {1, 2, 0};
	mPhase = kWriteAdvanceLookup[mPhase];
}

///////////////////////////////////////////////////////////////////////////

void ATCreateDeviceRTime8(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceRTime8> p(new ATDeviceRTime8);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefRTime8 = { "rtime8", nullptr, L"R-Time 8", ATCreateDeviceRTime8 };

ATDeviceRTime8::ATDeviceRTime8()
	: mpMemMan(nullptr)
	, mpMemLayerRT8(nullptr)
{
}

void *ATDeviceRTime8::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceMemMap::kTypeID:
			return static_cast<IATDeviceMemMap *>(this);

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceRTime8::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefRTime8;
}

void ATDeviceRTime8::Shutdown() {
	if (mpMemLayerRT8) {
		mpMemMan->DeleteLayer(mpMemLayerRT8);
		mpMemLayerRT8 = nullptr;
	}

	mpMemMan = nullptr;
}

void ATDeviceRTime8::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;

	ATMemoryHandlerTable handlerTable = {};
	handlerTable.mpThis = this;
	handlerTable.mbPassAnticReads = true;
	handlerTable.mbPassReads = true;
	handlerTable.mbPassWrites = true;
	handlerTable.mpDebugReadHandler = ReadByte;
	handlerTable.mpReadHandler = ReadByte;
	handlerTable.mpWriteHandler = WriteByte;
	mpMemLayerRT8 = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, handlerTable, 0xD5, 0x01);
	mpMemMan->SetLayerName(mpMemLayerRT8, "R-Time 8");
	mpMemMan->EnableLayer(mpMemLayerRT8, true);
}

bool ATDeviceRTime8::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD5B8;
		hi = 0xD5C0;
		return true;
	}

	return false;
}

sint32 ATDeviceRTime8::ReadByte(void *thisptr0, uint32 addr) {
	if (addr - 0xD5B8 < 8) {
		ATDeviceRTime8 *thisptr = (ATDeviceRTime8 *)thisptr0;

		// The R-Time 8 only drives the lower four data lines.
		return (thisptr->mRTime8.ReadControl((uint8)addr) & 0x0F) + (thisptr->mpMemMan->ReadFloatingDataBus() & 0xF0);
	}

	return -1;
}

bool ATDeviceRTime8::WriteByte(void *thisptr0, uint32 addr, uint8 value) {
	if (addr - 0xD5B8 < 8) {
		((ATDeviceRTime8 *)thisptr0)->mRTime8.WriteControl((uint8)addr, value);
		return true;
	}

	return false;
}
