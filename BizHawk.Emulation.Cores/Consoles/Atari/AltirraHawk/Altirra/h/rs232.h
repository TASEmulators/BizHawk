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

#ifndef f_AT_RS232_H
#define f_AT_RS232_H

#include <vd2/system/refcount.h>
#include <vd2/system/unknown.h>
#include <vd2/system/VDString.h>
#include <at/atcore/enumparse.h>

class ATFirmwareManager;
class ATCPUEmulator;
class ATCPUEmulatorMemory;
class ATScheduler;
class IATUIRenderer;
class ATPokeyEmulator;
class ATPIAEmulator;

enum AT850SIOEmulationLevel {
	kAT850SIOEmulationLevel_None,
	kAT850SIOEmulationLevel_StubLoader,
	kAT850SIOEmulationLevel_Full,
	kAT850SIOEmulationLevelCount
};

enum ATRS232DeviceMode {
	kATRS232DeviceMode_850,
	kATRS232DeviceMode_1030,
	kATRS232DeviceMode_SX212,
	kATRS232DeviceModeCount
};

enum class ATModemNetworkMode : uint8 {
	None,		// no sound or delays
	Minimal,	// simulate dialing but not handshake
	Full		// simulate dialing and handshake
};

AT_DECLARE_ENUM_TABLE(ATModemNetworkMode);

struct ATRS232Config {
	ATRS232DeviceMode mDeviceMode;
	bool	mbTelnetEmulation;
	bool	mbTelnetLFConversion;
	bool	mbAllowOutbound;
	bool	mbRequireMatchedDTERate;
	bool	mbExtendedBaudRates;
	bool	mbListenForIPv6;
	bool	mbDisableThrottling;
	uint32	mListenPort;
	uint32	mConnectionSpeed;
	AT850SIOEmulationLevel	m850SIOLevel;
	VDStringA	mDialAddress;
	VDStringA	mDialService;
	ATModemNetworkMode	mNetworkMode;
	VDStringA	mTelnetTermType;

	ATRS232Config()
		: mDeviceMode(kATRS232DeviceMode_850)
		, mbTelnetEmulation(true)
		, mbTelnetLFConversion(true)
		, mbAllowOutbound(true)
		, mbRequireMatchedDTERate(false)
		, mbExtendedBaudRates(false)
		, mbListenForIPv6(true)
		, mbDisableThrottling(false)
		, mListenPort(0)
		, mConnectionSpeed(9600)
		, m850SIOLevel(kAT850SIOEmulationLevel_None)
		, mNetworkMode(ATModemNetworkMode::Full)
	{
	}
};

class IATRS232Device : public IVDRefUnknown {
public:
	enum { kTypeID = 'r2dv' };

	virtual void SetConfig(const ATRS232Config&) = 0;

	virtual void SetToneDialingMode(bool enable) = 0;
	virtual bool IsToneDialingMode() const = 0;
	virtual void HangUp() = 0;
	virtual void Dial(const char *address, const char *service, const char *desc = nullptr) = 0;
	virtual void Answer() = 0;
};

#endif
