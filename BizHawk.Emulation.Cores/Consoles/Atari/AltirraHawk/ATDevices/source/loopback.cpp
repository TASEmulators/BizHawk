//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - device registration
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceserial.h>

void ATCreateDeviceLoopback(const ATPropertySet& pset, IATDevice **dev);

extern const ATDeviceDefinition g_ATDeviceDefLoopback = { "loopback", nullptr, L"Loopback", ATCreateDeviceLoopback };

class ATLoopbackDevice final : public ATDevice
					, public IATDeviceSerial
{
	ATLoopbackDevice(const ATLoopbackDevice&) = delete;
	ATLoopbackDevice& operator=(const ATLoopbackDevice&) = delete;

public:
	ATLoopbackDevice();
	~ATLoopbackDevice();

public:
	int AddRef() override { return ATDevice::AddRef(); }
	int Release() override { return ATDevice::Release(); }
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void Shutdown() override;
	void ColdReset() override;

public:
	void SetOnStatusChange(const vdfunction<void(const ATDeviceSerialStatus&)>& fn) override;
	void SetTerminalState(const ATDeviceSerialTerminalState&) override;
	ATDeviceSerialStatus GetStatus() override;
	bool Read(uint32& baudRate, uint8& c) override;
	bool Read(uint32 baudRate, uint8& c, bool& framingError) override;
	void Write(uint32 baudRate, uint8 c) override;
	void FlushBuffers() override;

private:
	uint8 mBufferedByte;
	uint32 mBufferedBaudRate;
};

ATLoopbackDevice::ATLoopbackDevice() {
}

ATLoopbackDevice::~ATLoopbackDevice() {
	Shutdown();
}

void *ATLoopbackDevice::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDevice::kTypeID:
			return static_cast<IATDevice *>(this);

		case IATDeviceSerial::kTypeID:
			return static_cast<IATDeviceSerial *>(this);

		default:
			return ATDevice::AsInterface(iid);
	}
}

void ATLoopbackDevice::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefLoopback;
}

void ATLoopbackDevice::Shutdown() {
}

void ATLoopbackDevice::ColdReset() {
	FlushBuffers();
}

void ATLoopbackDevice::SetOnStatusChange(const vdfunction<void(const ATDeviceSerialStatus&)>& fn) {
}

void ATLoopbackDevice::SetTerminalState(const ATDeviceSerialTerminalState& state) {
}

ATDeviceSerialStatus ATLoopbackDevice::GetStatus() {
	return {};
}

bool ATLoopbackDevice::Read(uint32& baudRate, uint8& c) {
	if (!mBufferedBaudRate)
		return false;

	baudRate = mBufferedBaudRate;
	mBufferedBaudRate = 0;
	c = mBufferedByte;

	return true;
}

bool ATLoopbackDevice::Read(uint32 baudRate, uint8& c, bool& framingError) {
	framingError = false;

	uint32 transmitRate;
	if (!Read(transmitRate, c))
		return false;

	// check for more than a 5% discrepancy in baud rates between modem and serial port
	if (abs((int)baudRate - (int)transmitRate) * 20 > (int)transmitRate) {
		// baud rate mismatch -- return some bogus character and flag a framing error
		c = 'U';
		framingError = true;
	}

	return true;
}

void ATLoopbackDevice::Write(uint32 baudRate, uint8 c) {
	mBufferedByte = c;
	mBufferedBaudRate = baudRate;
}

void ATLoopbackDevice::FlushBuffers() {
	mBufferedBaudRate = 0;
}

///////////////////////////////////////////////////////////////////////////

void ATCreateDeviceLoopback(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATLoopbackDevice> p(new ATLoopbackDevice);

	*dev = p;
	(*dev)->AddRef();
}
