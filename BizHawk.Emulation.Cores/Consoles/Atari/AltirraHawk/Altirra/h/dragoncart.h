//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2013 Avery Lee
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

#ifndef f_AT_DRAGONCART_H
#define f_AT_DRAGONCART_H

#include <vd2/system/file.h>
#include <at/atnetwork/ethernet.h>
#include "cs8900a.h"

class ATMemoryManager;
class ATMemoryLayer;
class ATEthernetBus;
class ATEthernetSimClock;
class IATEthernetGatewayServer;
class IATNetSockWorker;
class IATNetSockVxlanTunnel;
class ATPropertySet;
class ATConsoleOutput;
class ATScheduler;

struct ATDragonCartSettings {
	enum AccessMode {
		kAccessMode_None,
		kAccessMode_HostOnly,
		kAccessMode_NAT,
		kAccessModeCount
	};

	uint32 mNetAddr;		// 0xaabbccdd -> a.b.c.d
	uint32 mNetMask;
	AccessMode mAccessMode;

	uint32 mForwardingAddr;
	uint16 mForwardingPort;

	uint32 mTunnelAddr;
	uint16 mTunnelSrcPort;
	uint16 mTunnelTgtPort;

	void SetDefault();

	void LoadFromProps(const ATPropertySet& pset);
	void SaveToProps(ATPropertySet& pset);

	bool operator==(const ATDragonCartSettings&) const;
	bool operator!=(const ATDragonCartSettings&) const;
};

class ATDragonCartEmulator final : public IATEthernetEndpoint {
	ATDragonCartEmulator(const ATDragonCartEmulator&) = delete;
	ATDragonCartEmulator& operator=(const ATDragonCartEmulator&) = delete;
public:
	enum { kTypeID = 'atdr' };

	ATDragonCartEmulator();
	~ATDragonCartEmulator();

	const ATDragonCartSettings& GetSettings() const { return mSettings; }

	void Init(ATMemoryManager *memmgr, ATScheduler *slowSched, const ATDragonCartSettings& settings);
	void Shutdown();

	void ColdReset();
	void WarmReset();

	void DumpConnectionInfo(ATConsoleOutput&);
	void OpenPacketTrace(const wchar_t *path);
	void ClosePacketTrace();

public:
	void ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo) override;

protected:
	static sint32 OnDebugRead(void *thisptr, uint32 addr);
	static sint32 OnRead(void *thisptr, uint32 addr);
	static bool OnWrite(void *thisptr, uint32 addr, uint8 value);

	ATMemoryManager *mpMemMgr;
	ATMemoryLayer *mpMemLayer;

	ATEthernetBus *mpEthernetBus;
	ATEthernetSimClock *mpEthernetClock;
	IATEthernetGatewayServer *mpGateway;
	IATNetSockWorker *mpNetSockWorker;
	IATNetSockVxlanTunnel *mpNetSockVxlanTunnel = nullptr;
	uint32 mEthernetClockId;

	ATDragonCartSettings mSettings;

	ATCS8900AEmulator mCS8900A;

	vdautoptr<VDFileStream> mPacketTraceFile;
	vdautoptr<VDBufferedWriteStream> mPacketTraceStream;
	uint32 mPacketTraceEndpointId = 0;
	uint32 mPacketTraceStartTimestamp = 0;
	sint64 mPacketTraceStartTime;
};

#endif
