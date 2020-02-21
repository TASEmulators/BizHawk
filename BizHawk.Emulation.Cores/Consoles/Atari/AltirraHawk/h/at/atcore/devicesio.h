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

//=========================================================================
// Device SIO interface
//
// The device SIO interface reduces the work needed to implement emulation
// of an SIO device, and also reduces the load on the POKEY emulation by
// multiplexing all command frame based traffic through a chokepoint.
//
// First, the basic 5-byte SIO command frame is received and validated by
// the SIO manager before any devices see it. This includes monitoring the
// command line, receiving the bytes, waiting for the command line to
// deassert, and verifying the checksum. Devices only see commands that
// have passed validation. The command frame is passed down the chain until
// a device claims it.
//
// Once a device claims the command, it owns the SIO bus. The SIO device
// manager now handles both communication and timing for the SIO transaction
// by means of a timed step queue, which can contain several types of
// commands:
//
//	- Send data: Transmit data bytes across the SIO bus. A checksum can
//	  automatically be computed and added by the SIO device manager.
//
//	- Receive data: Wait for data bytes to arrive. The checksum is
//	  automatically checked by the manager; the device can either use this
//	  result or just ignore it. (It's cheap to compute.)
// 
//	- Delay: Timing delays can also be added as steps, relieving the need
//	  to do so via state machine.
//
//	- Fence: Device activities can be synchronized to the step queue by
//	  inserting a fence, which then results in a callback to the device.
//
// For read commands, this permits fire-and-forget implementation in
// the device where all transmits for a command can be queued up and the
// command ended before the transaction starts. For write commands, this
// permits a simple transmit-receive-fence-transmit sequence with only
// one callback needed.
//
// There are a couple of limitations. Only one command can be in flight, and
// a command overlap will cause the existing command to be terminated. The
// behavior of overlapping SIO commands is not well defined with most real
// devices anyway, so it's hard to centralize this. Second, only 64K of
// total data can be queued either for transmission or reception.
// Transactions involving more than that require intermediate fences to
// allow the transmit buffer to catch up.
//
// The SIO manager provides special support for handling type 3 polls by
// returning a poll count the command, where 0 is the first poll received
// since power up or last null poll.
//
//
// High speed support
// ------------------
// The command structure contains a field which indicates if the command
// frame was sent at 19,200 baud. Most devices should validate this field
// and reject commands sent at other rates. Devices which do support high
// speed commands can check the cycles per bit field to validate whether
// the command frame is sent at a supported rate, typically with 5%
// tolerance. 
//
// By default, all transmissions during the command are done at 19200 baud
// with 810-like timings. Both bit and byte timings can be altered via the
// SetTransferRate() command.
//
//
// Acceleration support
// --------------------
// The SIO manager also handles SIOV intercept based acceleration of device
// requests. The request is packaged into an SIO request structure and then
// run down the device chain as usual. In addition to checking the device ID
// and command, the following also need to be checked:
//
//	- Whether the mode and transfer length are valid for the request
//	- Whether the timeout is long enough for the request
//	- Anything else that may make the request non-trivial to accelerate
//
// Buffer address and CPU state validation are handled by the SIO manager
// itself, which automatically bypasses requests that are unsafe to
// accelerate, including reads within page 2-3 and calls to SIOV with the
// I flag set.
//
// Not all requests need to be accelerated. If a request is weird for any
// reason, or a rare case not worth handling, the device can return
// BypassAccel to force the request to be non-accelerated. (This differs from
// NotHandled if there is another device further down the chain that would
// handle the request.)
//
// If a device handles the request, then the usual procedure applies,
// subject to the following changes:
//
//	- SendACK(), SendNAK(), SendError(), and SendComplete() are used to
//	  set the status code.
//	- Exactly one send or receive request can be serviced, per the SIO
//	  mode byte. The request is serviced immediately without going through
//	  POKEY or the CPU.
//	- Delay steps are ignored.
//	- Fence steps occur immediately.
//
// All memory and CPU state changes are handled by the SIO manager. In
// general, this mechanism is intended to allow devices to reuse the same
// code paths for accelerated and non-accelerated requests.
//
//
// Raw devices
// -----------
// For devices that require direct access to the SIO bus, the SIO manager
// supports raw devices. Raw devices can receive and transmit arbitrary
// data at arbitrary rates, and also assert the PROCEED and INTERRUPT
// lines.
//
// Because all SIO traffic has to be passed through all raw devices, raw
// devices should be used sparingly. In contrast, regular SIO devices are
// very cheap because they are not polled until a valid SIO command frame
// is transmitted.
//

#ifndef f_AT_ATCORE_DEVICESIO_H
#define f_AT_ATCORE_DEVICESIO_H

#include <vd2/system/unknown.h>

class IATDeviceSIO;
class IATDeviceRawSIO;

struct ATDeviceSIOCommand {
	uint8 mDevice;
	uint8 mCommand;
	uint8 mAUX[2];

	// Cycles per bit that the command was sent at.
	uint32 mCyclesPerBit;

	// True if the command was sent at standard 19200 baud rate.
	bool mbStandardRate;

	// Number of type 3 polls that have taken place prior to this
	// command.
	uint8 mPollCount;
};

struct ATDeviceSIORequest : public ATDeviceSIOCommand {
	uint8	mMode;			// SIO write (bit 7) / read (bit 6) mode bits
	uint8	mTimeout;		// SIO timeout (x64 VBLANKs)
	uint16	mLength;		// transfer length in bytes
	uint16	mSector;		// AUX1/2 read as a word
};

class IATDeviceSIOManager {
public:
	virtual void AddDevice(IATDeviceSIO *dev) = 0;
	virtual void RemoveDevice(IATDeviceSIO *dev) = 0;

	virtual void BeginCommand() = 0;

	// Send data across the SIO bus. If addChecksum is set, the standard SIO checksum
	// is computed and added at the end.
	virtual void SendData(const void *data, uint32 len, bool addChecksum) = 0;

	// Send ACK, NAK, Complete, and Error status bytes. These must be used instead of
	// explicit sends if acceleration is supported, as any regular sends will be
	// captured into the transfer buffer. If autoDelay is set, a delay of 450 cycles
	// will automatically be added prior to C/E bytes to satisfy SIO protocol timing.
	// Otherwise, this delay must be provided by the device.
	virtual void SendACK() = 0;
	virtual void SendNAK() = 0;
	virtual void SendComplete(bool autoDelay = true) = 0;
	virtual void SendError(bool autoDelay = true) = 0;

	virtual void ReceiveData(uint32 id, uint32 len, bool autoProtocol) = 0;

	// Set timing parameters for the transfer in machine cycles per bit and byte.
	// A byte is 10 bits including start bit and stop bit, so the byte time should
	// be at least 10 times the bit time.
	virtual void SetTransferRate(uint32 cyclesPerBit, uint32 cyclesPerByte) = 0;

	// Enables or disables synchronous transmissions. Normally, POKEY receive operations
	// in synchronous mode are considered unreliable due to lack of phase synchronization
	// between the sender and receiver. Enabling the synchronous transmit flag indicates
	// that the transmission should be synchronized to the receive clock. Currently,
	// it's assumed that the receive clock is not required and enabling this mode allows
	// both synchronous and asynchronous reception.
	virtual void SetSynchronousTransmit(bool enable) = 0;

	virtual void Delay(uint32 ticks) = 0;
	virtual void InsertFence(uint32 id) = 0;

	// Removes any remaining previously queued commands. Used to abort the remainder of
	// a command.
	virtual void FlushQueue() = 0;

	virtual void EndCommand() = 0;

	// Returns true if an acceleration request is currently being processed.
	virtual bool IsAccelRequest() const = 0;

	// Returns the time skew in cycles due to delays omitted during request acceleration.
	// This is cumulative and should always be differenced.
	virtual uint32 GetAccelTimeSkew() const = 0;

	// Returns the high speed index (POKEY divisor) that should be used for high-speed
	// transfers. This is used for devices that might not otherwise have an inherent high
	// speed transfer rate. -1 means that standard speed should be used.
	virtual sint32 GetHighSpeedIndex() const = 0;

	// Gets the number of cycles per bit that POKEY is currently configured to receive at.
	// Zero means that receive is disabled, such as if the serial clock is frozen. Note
	// that this may be as large as several million cycles if POKEY is not actually
	// being used to receive serial data.
	virtual uint32 GetCyclesPerBitRecv() const = 0;

	// Changes every time the serial input register in POKEY is reset.
	virtual uint32 GetRecvResetCounter() const = 0;

	virtual void AddRawDevice(IATDeviceRawSIO *dev) = 0;
	virtual void RemoveRawDevice(IATDeviceRawSIO *dev) = 0;
	virtual void SendRawByte(uint8 byte, uint32 cyclesPerBit, bool synchronous = false, bool forceFramingError = false, bool simulateInput = true) = 0;
	virtual void SetRawInput(bool input) = 0;

	// Returns if the SIO command and motor lines are asserted. Both are active low,
	// so true (asserted) means low and active, and false (negated) means high and not active.
	virtual bool IsSIOCommandAsserted() const = 0;
	virtual bool IsSIOMotorAsserted() const = 0;

	// Control SIO interrupt and proceed lines. These lines are normally high and active
	// low if any device is pulling them low.
	virtual void SetSIOInterrupt(IATDeviceRawSIO *dev, bool state) = 0;
	virtual void SetSIOProceed(IATDeviceRawSIO *dev, bool state) = 0;

	// Set an external clock signal to be fed into POKEY's external clock input.
	// Initial offset is in clock cycles from current time; period is in cycles.
	// A period of 0 disables the external clock.
	virtual void SetExternalClock(IATDeviceRawSIO *dev, uint32 initialOffset, uint32 period) = 0;
};

class IATDeviceSIO {
public:
	enum { kTypeID = 'adsi' };

	enum CmdResponse {
		kCmdResponse_NotHandled,
		kCmdResponse_Start,
		kCmdResponse_Send_ACK_Complete,
		kCmdResponse_Fail_NAK,
		kCmdResponse_BypassAccel
	};

	virtual void InitSIO(IATDeviceSIOManager *mgr) = 0;
	virtual CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) = 0;
	virtual void OnSerialAbortCommand() = 0;
	virtual void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) = 0;
	virtual void OnSerialFence(uint32 id) = 0; 

	// Attempt to accelerate a command via SIOV intercept. This receives a superset
	// of the command structure received by OnSerialBeginCommand() and is intended
	// to allow a direct forward.
	//
	// This routine can also return the additional BypassAccel value, which means
	// to abort acceleration and force usage of native SIO. It is used for requests
	// that the device recognizes but which cannot be safely accelerated by any
	// device.
	virtual CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) = 0;
};

class IATDeviceRawSIO {
public:
	enum { kTypeID = 'adsr' };

	virtual void OnCommandStateChanged(bool asserted) = 0;
	virtual void OnMotorStateChanged(bool asserted) = 0;
	virtual void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) = 0;
	virtual void OnSendReady() = 0;
};

#endif
