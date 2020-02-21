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

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/date.h>
#include <vd2/system/math.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include <at/atnetwork/ethernetbus.h>
#include <at/atnetwork/gatewayserver.h>
#include <at/atnetworksockets/vxlantunnel.h>
#include <at/atnetworksockets/worker.h>
#include "dragoncart.h"
#include "memorymanager.h"

// libpcap format. Reference: https://wiki.wireshark.org/Development/LibpcapFileFormat
struct ATPCapHeader {
	uint32	magic_number;
	uint16	version_major;
	uint16	version_minor;
	sint32	thiszone;
	uint32	sigfigs;
	uint32	snaplen;
	uint32	network;

	enum : uint32 {
		kMagic = 0xa1b2c3d4,
		kVersionMajor = 2,
		kVersionMinor = 4,
		kNetworkEthernet = 1
	};
};

struct ATPCapRecordHeader {
	uint32 ts_sec;
	uint32 ts_usec;
	uint32 incl_len;
	uint32 orig_len;
};

class ATEthernetSimClock final : public IATSchedulerCallback, public IATEthernetClock {
public:
	static const float kTicksPerSecond;
	static const float kSecondsPerTick;

	ATEthernetSimClock();
	~ATEthernetSimClock();

	void Init(ATScheduler *slowSched);
	void Shutdown();

	uint32 GetTimestamp(sint32 offsetMS) override;
	sint32 SubtractTimestamps(uint32 t1, uint32 t2) override;
	uint32 AddClockEvent(uint32 timestamp, IATEthernetClockEventSink *sink, uint32 userid) override;
	void RemoveClockEvent(uint32 eventid) override;

public:
	void OnScheduledEvent(uint32 id) override;

protected:
	void DispatchEvents();

	ATScheduler *mpSlowScheduler;
	ATEvent *mpNextEvent;

	struct HeapEvent {
		uint32 mTimestamp;
		uint32 mEventIdx;
	};

	vdfastvector<HeapEvent> mHeap;

	struct Event {
		IATEthernetClockEventSink *mpSink;
		uint32 mHeapIdx;
		uint32 mUserId;
	};

	vdfastvector<Event> mEvents;
	uint32 mEventFreeChain;
};

const float ATEthernetSimClock::kTicksPerSecond = 15699.75877192982456140350877193f;
const float ATEthernetSimClock::kSecondsPerTick = 6.3695246183523324891850779917559e-5f;

ATEthernetSimClock::ATEthernetSimClock()
	: mpSlowScheduler(NULL)
	, mpNextEvent(NULL)
	, mEventFreeChain(0)
{
}

ATEthernetSimClock::~ATEthernetSimClock() {
	Shutdown();
}

void ATEthernetSimClock::Init(ATScheduler *slowSched) {
	mpSlowScheduler = slowSched;
}

void ATEthernetSimClock::Shutdown() {
	if (mpNextEvent) {
		mpSlowScheduler->RemoveEvent(mpNextEvent);
		mpNextEvent = NULL;
	}

	mpSlowScheduler = NULL;
}

uint32 ATEthernetSimClock::GetTimestamp(sint32 offsetMS) {
	return (uint32)(ATSCHEDULER_GETTIME(mpSlowScheduler) + VDRoundToInt32(offsetMS * (kTicksPerSecond / 1000.0f)));
}

sint32 ATEthernetSimClock::SubtractTimestamps(uint32 t1, uint32 t2) {
	sint32 tdelta = (sint32)(t1 - t2);

	return VDRoundToInt32((float)tdelta * (kSecondsPerTick * 1000.0f));
}

uint32 ATEthernetSimClock::AddClockEvent(uint32 timestamp, IATEthernetClockEventSink *sink, uint32 userid) {
	const uint32 t = ATSCHEDULER_GETTIME(mpSlowScheduler);
	uint32 relativeDelay = timestamp - t;

	if (relativeDelay >= 0x1000000) {
		relativeDelay = 1;
		timestamp = t+1;
	}

	uint32 eventIdx;

	if (mEventFreeChain) {
		eventIdx = mEventFreeChain - 1;

		VDASSERT(!mEvents[eventIdx].mpSink);

		mEventFreeChain = mEvents[eventIdx].mHeapIdx;
	} else {
		eventIdx = (uint32)mEvents.size();
		mEvents.push_back();
	}

	Event& ev = mEvents[eventIdx];
	ev.mpSink = sink;
	ev.mUserId = userid;

	uint32 holeIdx = (uint32)mHeap.size();
	mHeap.push_back();

	while(holeIdx != 0) {
		uint32 parentIdx = (holeIdx - 1) >> 1;

		if (mHeap[parentIdx].mTimestamp - t <= relativeDelay)
			break;

		mHeap[holeIdx] = mHeap[parentIdx];
		mEvents[mHeap[holeIdx].mEventIdx].mHeapIdx = holeIdx;

		holeIdx = parentIdx;
	}

	ev.mHeapIdx = holeIdx;

	HeapEvent he = { timestamp, eventIdx };
	mHeap[holeIdx] = he;

	if (holeIdx == 0)
		mpSlowScheduler->SetEvent(relativeDelay, this, 1, mpNextEvent);

	return eventIdx + 1;
}

void ATEthernetSimClock::RemoveClockEvent(uint32 eventId) {
	if (!eventId)
		return;

	const uint32 eventIdx = eventId - 1;

	Event& ev = mEvents[eventIdx];
	VDASSERT(ev.mpSink);

	uint32 holeIdx = ev.mHeapIdx;

	ev.mpSink = NULL;
	ev.mHeapIdx = mEventFreeChain;
	mEventFreeChain = eventId;

	const uint32 n = (uint32)mHeap.size() - 1;

	if (holeIdx < n) {
		const uint32 t = ATSCHEDULER_GETTIME(mpSlowScheduler);
		const HeapEvent heCopy = mHeap[n];
		const uint32 relDelay = heCopy.mTimestamp - t;

		for(;;) {
			uint32 childIdx = holeIdx * 2 + 1;

			if (childIdx >= n)
				break;

			const uint32 leftDelay = mHeap[childIdx].mTimestamp - t;
			if (leftDelay < relDelay) {
				if (childIdx + 1 < n && mHeap[childIdx + 1].mTimestamp - t < leftDelay)
					++childIdx;
			} else {
				++childIdx;

				if (childIdx >= n || mHeap[childIdx].mTimestamp - t >= relDelay)
					break;
			}

			mHeap[holeIdx] = mHeap[childIdx];
			mEvents[mHeap[holeIdx].mEventIdx].mHeapIdx = holeIdx;

			holeIdx = childIdx;
		}

		mHeap[holeIdx] = heCopy;
		mEvents[heCopy.mEventIdx].mHeapIdx = holeIdx;
	}

	mHeap.pop_back();
}

void ATEthernetSimClock::OnScheduledEvent(uint32 id) {
	mpNextEvent = NULL;

	DispatchEvents();

	if (!mHeap.empty()) {
		const uint32 t = ATSCHEDULER_GETTIME(mpSlowScheduler);
		const uint32 relDelay = mHeap.front().mTimestamp - t;

		VDASSERT(relDelay > 0);

		mpSlowScheduler->SetEvent(relDelay, this, 1, mpNextEvent);
	}
}

void ATEthernetSimClock::DispatchEvents() {
	const uint32 t = ATSCHEDULER_GETTIME(mpSlowScheduler);

	while(!mHeap.empty() && (mHeap.front().mTimestamp - t - 1) >= 0x100000) {
		const uint32 nextEventIdx = mHeap.front().mEventIdx;
		const uint32 nextEventId = nextEventIdx + 1;
		const Event evCopy = mEvents[nextEventIdx];

		RemoveClockEvent(nextEventId);

		evCopy.mpSink->OnClockEvent(nextEventId, evCopy.mUserId);
	}
}

///////////////////////////////////////////////////////////////////////////

void ATDragonCartSettings::SetDefault() {
	mNetAddr = 0xC0A80000;		// 192.168.0.0
	mNetMask = 0xFFFFFF00;		// 255.255.255.0
	mAccessMode = ATDragonCartSettings::kAccessMode_NAT;
	mForwardingAddr = 0;
	mForwardingPort = 0;
	mTunnelAddr = 0;
	mTunnelSrcPort = 0;
	mTunnelTgtPort = 0;
}

void ATDragonCartSettings::LoadFromProps(const ATPropertySet& pset) {
	SetDefault();

	pset.TryGetUint32("netaddr", mNetAddr);
	pset.TryGetUint32("netmask", mNetMask);

	const wchar_t *s = pset.GetString("access");
	if (s) {
		if (!wcscmp(s, L"none"))
			mAccessMode = kAccessMode_None;
		else if (!wcscmp(s, L"hostonly"))
			mAccessMode = kAccessMode_HostOnly;
		else if (!wcscmp(s, L"nat"))
			mAccessMode = kAccessMode_NAT;
	}

	uint32 fwaddr;
	uint32 fwport;

	if (pset.TryGetUint32("fwaddr", fwaddr) &&
		pset.TryGetUint32("fwport", fwport) &&
		fwaddr &&
		fwport &&
		fwport < 65536)
	{
		mForwardingAddr = fwaddr;
		mForwardingPort = fwport;
	}

	pset.TryGetUint32("tunaddr", mTunnelAddr);

	uint32 port;
	if (pset.TryGetUint32("tunsrcport", port))
		mTunnelSrcPort = (uint16)port;

	if (pset.TryGetUint32("tuntgtport", port))
		mTunnelTgtPort = (uint16)port;
}

void ATDragonCartSettings::SaveToProps(ATPropertySet& pset) {
	pset.Clear();
	pset.SetUint32("netaddr", mNetAddr);
	pset.SetUint32("netmask", mNetMask);

	switch(mAccessMode) {
		case kAccessMode_None:
			pset.SetString("access", L"none");
			break;
		case kAccessMode_HostOnly:
			pset.SetString("access", L"hostonly");
			break;
		case kAccessMode_NAT:
			pset.SetString("access", L"nat");
			break;
	}

	if (mForwardingAddr && mForwardingPort) {
		pset.SetUint32("fwaddr", mForwardingAddr);
		pset.SetUint32("fwport", mForwardingPort);
	}

	if (mTunnelAddr) {
		pset.SetUint32("tunaddr", mTunnelAddr);
		pset.SetUint32("tunsrcport", mTunnelSrcPort);
		pset.SetUint32("tuntgtport", mTunnelTgtPort);
	}
}

bool ATDragonCartSettings::operator==(const ATDragonCartSettings& x) const {
	return mNetAddr == x.mNetAddr
		&& mNetMask == x.mNetMask
		&& mAccessMode == x.mAccessMode
		&& mForwardingAddr == x.mForwardingAddr
		&& mForwardingPort == x.mForwardingPort
		&& mTunnelAddr == x.mTunnelAddr
		&& mTunnelSrcPort == x.mTunnelSrcPort
		&& mTunnelTgtPort == x.mTunnelTgtPort;
}

bool ATDragonCartSettings::operator!=(const ATDragonCartSettings& x) const {
	return !operator==(x);
}

ATDragonCartEmulator::ATDragonCartEmulator()
	: mpMemMgr(NULL)
	, mpMemLayer(NULL)
	, mpEthernetClock(NULL)
	, mpEthernetBus(NULL)
	, mpGateway(NULL)
	, mpNetSockWorker(NULL)
{
}

ATDragonCartEmulator::~ATDragonCartEmulator() {
	Shutdown();
}

void ATDragonCartEmulator::Init(ATMemoryManager *memmgr, ATScheduler *slowSched, const ATDragonCartSettings& settings) {
	mpMemMgr = memmgr;
	mSettings = settings;

	ATMemoryHandlerTable handlers = {};
	handlers.mbPassAnticReads = true;
	handlers.mbPassReads = true;
	handlers.mbPassWrites = true;
	handlers.mpThis = this;
	handlers.mpDebugReadHandler = OnDebugRead;
	handlers.mpReadHandler = OnRead;
	handlers.mpWriteHandler = OnWrite;
	mpMemLayer = mpMemMgr->CreateLayer(kATMemoryPri_Cartridge2, handlers, 0xD5, 0x01);
	mpMemMgr->SetLayerName(mpMemLayer, "DragonCart control");
	mpMemMgr->EnableLayer(mpMemLayer, true);

	mpEthernetClock = new ATEthernetSimClock;
	mpEthernetClock->Init(slowSched);

	mpEthernetBus = new ATEthernetBus;
	mEthernetClockId = mpEthernetBus->AddClock(mpEthernetClock);

	if (settings.mTunnelAddr)
		ATCreateNetSockVxlanTunnel(VDToBE32(settings.mTunnelAddr), settings.mTunnelSrcPort, settings.mTunnelTgtPort, mpEthernetBus, mEthernetClockId, &mpNetSockVxlanTunnel);

	if (settings.mAccessMode != ATDragonCartSettings::kAccessMode_None) {
		ATCreateEthernetGatewayServer(&mpGateway);
		mpGateway->Init(mpEthernetBus, mEthernetClockId, VDToBE32(mSettings.mNetAddr), VDToBE32(mSettings.mNetMask));

		ATCreateNetSockWorker(
			mpGateway->GetUdpStack(),
			mpGateway->GetTcpStack(),
			settings.mAccessMode == ATDragonCartSettings::kAccessMode_NAT,
			VDToBE32(settings.mForwardingAddr),
			settings.mForwardingPort,
			&mpNetSockWorker);
		mpGateway->SetBridgeListener(mpNetSockWorker->AsSocketListener(), mpNetSockWorker->AsUdpListener());
	}

	mCS8900A.Init(mpEthernetBus, mEthernetClockId);
}

void ATDragonCartEmulator::Shutdown() {
	ClosePacketTrace();

	mCS8900A.Shutdown();

	vdsaferelease <<= mpNetSockVxlanTunnel;

	if (mpNetSockWorker) {
		mpGateway->SetBridgeListener(NULL, NULL);
		vdsaferelease <<= mpNetSockWorker;
	}

	vdsaferelease <<= mpGateway;
	vdsafedelete <<= mpEthernetBus, mpEthernetClock;

	if (mpMemLayer) {
		mpMemMgr->DeleteLayer(mpMemLayer);
		mpMemLayer = NULL;
	}

	mpMemMgr = NULL;
}

void ATDragonCartEmulator::ColdReset() {
	if (mpNetSockWorker)
		mpNetSockWorker->ResetAllConnections();

	if (mpGateway)
		mpGateway->ColdReset();

	mpEthernetBus->ClearPendingFrames();
	mCS8900A.ColdReset();
}

void ATDragonCartEmulator::WarmReset() {
	mCS8900A.WarmReset();
}

namespace {
	struct ConnectionSort {
		bool operator()(const ATNetConnectionInfo& x, const ATNetConnectionInfo& y) const {
			const uint32 xloc = VDReadUnalignedBEU32(x.mLocalAddr);
			const uint32 yloc = VDReadUnalignedBEU32(y.mLocalAddr);

			if (xloc != yloc)
				return xloc < yloc;

			if (x.mLocalPort != y.mLocalPort)
				return x.mLocalPort < y.mLocalPort;

			const uint32 xrem = VDReadUnalignedBEU32(x.mRemoteAddr);
			const uint32 yrem = VDReadUnalignedBEU32(y.mRemoteAddr);

			if (xrem != yrem)
				return xrem < yrem;

			return x.mRemotePort < y.mRemotePort;

		}
	};
}

void ATDragonCartEmulator::OpenPacketTrace(const wchar_t *path) {
	ClosePacketTrace();

	vdautoptr<VDFileStream> fs(new VDFileStream);
	fs->open(path, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways | nsVDFile::kSequential);

	vdautoptr<VDBufferedWriteStream> bs(new VDBufferedWriteStream(fs, 4096));

	mPacketTraceFile = std::move(fs);
	mPacketTraceStream = std::move(bs);

	ATPCapHeader hdr = {};
	hdr.magic_number = hdr.kMagic;
	hdr.version_major = hdr.kVersionMajor;
	hdr.version_minor = hdr.kVersionMinor;
	hdr.thiszone = 0;
	hdr.sigfigs = 0;
	hdr.snaplen = 65535;
	hdr.network = hdr.kNetworkEthernet;

	mPacketTraceStream->Write(&hdr, sizeof hdr);

	mPacketTraceEndpointId = mpEthernetBus->AddEndpoint(this);
	mPacketTraceStartTime = VDGetDateAsTimeT(VDGetCurrentDate());
	mPacketTraceStartTimestamp = mpEthernetClock->GetTimestamp(0);
}

void ATDragonCartEmulator::ClosePacketTrace() {
	if (mPacketTraceEndpointId) {
		mpEthernetBus->RemoveEndpoint(mPacketTraceEndpointId);
		mPacketTraceEndpointId = 0;
	}

	if (mPacketTraceStream) {
		try {
			mPacketTraceStream->Flush();
		} catch(...) {
		}

		mPacketTraceStream.reset();
	}

	mPacketTraceFile.reset();
}

void ATDragonCartEmulator::ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo) {
	const double deltaSeconds = (double)(mpEthernetClock->GetTimestamp(0) - mPacketTraceStartTimestamp) * mpEthernetClock->kSecondsPerTick;
	const double wholeDeltaSeconds = floor(deltaSeconds);
	sint32 fracDeltaSeconds = VDRoundToInt((deltaSeconds - wholeDeltaSeconds) * 1000000.0);
	sint64 intSeconds = (sint64)wholeDeltaSeconds + mPacketTraceStartTime;

	if (fracDeltaSeconds >= 1000000) {
		fracDeltaSeconds -= 1000000;
		++intSeconds;
	}

	ATPCapRecordHeader hdr = {};
	hdr.ts_sec = 0;
	hdr.ts_usec = 0;

	if (intSeconds >= 0) {
		if (intSeconds >= UINT64_C(0x100000000)) {
			hdr.ts_sec = UINT64_C(0xFFFFFFFF);
			hdr.ts_usec = 999999;
		} else {
			hdr.ts_sec = (uint32)intSeconds;
			hdr.ts_usec = fracDeltaSeconds;
		}
	}

	hdr.incl_len = packet.mLength + 12;
	hdr.orig_len = packet.mLength + 12;

	mPacketTraceStream->Write(&hdr, sizeof hdr);
	mPacketTraceStream->Write(packet.mDstAddr.mAddr, 6);
	mPacketTraceStream->Write(packet.mSrcAddr.mAddr, 6);
	mPacketTraceStream->Write(packet.mpData, packet.mLength);
}

void ATDragonCartEmulator::DumpConnectionInfo(ATConsoleOutput& output) {
	typedef vdfastvector<ATNetConnectionInfo> ConnInfos;
	ConnInfos connInfos;

	mpGateway->GetConnectionInfo(connInfos);

	std::sort(connInfos.begin(), connInfos.end(), ConnectionSort());

	output <<= "  Proto  Local address          Foreign address        State        NAT address";

	VDStringA line;

	for(ConnInfos::const_iterator it(connInfos.begin()), itEnd(connInfos.end());
		it != itEnd;
		++it)
	{
		const ATNetConnectionInfo& conn = *it;

		line.sprintf("  %-5s  ", conn.mpProtocol);

		line.append_sprintf("%u.%u.%u.%u:%u", conn.mRemoteAddr[0], conn.mRemoteAddr[1], conn.mRemoteAddr[2], conn.mRemoteAddr[3], conn.mRemotePort);
		if (line.size() < 30)
			line.resize(30, ' ');

		line.append_sprintf("%u.%u.%u.%u:%u", conn.mLocalAddr[0], conn.mLocalAddr[1], conn.mLocalAddr[2], conn.mLocalAddr[3], conn.mLocalPort);
		if (line.size() < 55)
			line.resize(55, ' ');

		line.append_sprintf("  %s", conn.mpState);

		uint32 natIpAddr;
		uint16 natPort;
		if (mpNetSockWorker->GetHostAddressForLocalAddress(!strcmp(conn.mpProtocol, "TCP"), VDReadUnalignedU32(conn.mRemoteAddr), conn.mRemotePort, VDReadUnalignedU32(conn.mLocalAddr), conn.mLocalPort, natIpAddr, natPort)) {
			if (line.size() < 68)
				line.resize(68, ' ');

			uint8 natIpAddr4[4];
			VDWriteUnalignedU32(natIpAddr4, natIpAddr);

			line.append_sprintf("%u.%u.%u.%u:%u", natIpAddr4[0], natIpAddr4[1], natIpAddr4[2], natIpAddr4[3], natPort);
		}

		output <<= line.c_str();
	}
}

sint32 ATDragonCartEmulator::OnDebugRead(void *thisptr0, uint32 addr) {
	ATDragonCartEmulator *thisptr = (ATDragonCartEmulator *)thisptr0;

	if ((addr & 0xFFF0) == 0xD500)
		return thisptr->mCS8900A.DebugReadByte((uint8)addr);

	return -1;
}

sint32 ATDragonCartEmulator::OnRead(void *thisptr0, uint32 addr) {
	ATDragonCartEmulator *thisptr = (ATDragonCartEmulator *)thisptr0;

	if ((addr & 0xFFF0) == 0xD500)
		return thisptr->mCS8900A.ReadByte((uint8)addr);

	return -1;
}

bool ATDragonCartEmulator::OnWrite(void *thisptr0, uint32 addr, uint8 value) {
	ATDragonCartEmulator *thisptr = (ATDragonCartEmulator *)thisptr0;

	if ((addr & 0xFFF0) == 0xD500) {
		thisptr->mCS8900A.WriteByte((uint8)addr, value);
		return true;
	}

	return false;
}

///////////////////////////////////////////////////////////////////////////

class ATDeviceDragonCart : public ATDevice
					, public IATDeviceMemMap
					, public IATDeviceScheduling
					, public IATDeviceDiagnostics
{
public:
	ATDeviceDragonCart();

	virtual void *AsInterface(uint32 id) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void GetSettings(ATPropertySet& settings) override;
	virtual bool SetSettings(const ATPropertySet& settings) override;
	virtual void WarmReset() override;
	virtual void ColdReset() override;
	virtual void Init() override;
	virtual void Shutdown() override;

public: // IATDeviceMemMap
	virtual void InitMemMap(ATMemoryManager *memmap) override;
	virtual bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:	// IATDeviceScheduling
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceDiagnostics
	virtual void DumpStatus(ATConsoleOutput& output) override;

private:
	ATMemoryManager *mpMemMan;
	ATScheduler *mpSlowScheduler;
	ATDragonCartSettings mSettings;
	bool mbDragonCartInited;

	ATDragonCartEmulator mDragonCart;
};

void ATCreateDeviceDragonCart(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceDragonCart> p(new ATDeviceDragonCart);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefDragonCart = { "dragoncart", "dragoncart", L"DragonCart", ATCreateDeviceDragonCart };

ATDeviceDragonCart::ATDeviceDragonCart()
	: mpMemMan(nullptr)
	, mpSlowScheduler(nullptr)
	, mbDragonCartInited(false)
{
	mSettings.SetDefault();
}

void *ATDeviceDragonCart::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceMemMap::kTypeID:
			return static_cast<IATDeviceMemMap *>(this);

		case IATDeviceScheduling::kTypeID:
			return static_cast<IATDeviceScheduling *>(this);

		case IATDeviceDiagnostics::kTypeID:
			return static_cast<IATDeviceDiagnostics *>(this);

		case ATDragonCartEmulator::kTypeID:
			return &mDragonCart;

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceDragonCart::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefDragonCart;
}

void ATDeviceDragonCart::GetSettings(ATPropertySet& props) {
	mSettings.SaveToProps(props);
}

bool ATDeviceDragonCart::SetSettings(const ATPropertySet& props) {
	ATDragonCartSettings settings;
	settings.LoadFromProps(props);

	if (mSettings != settings) {
		mSettings = settings;

		if (mbDragonCartInited) {
			mDragonCart.Shutdown();
			mDragonCart.Init(mpMemMan, mpSlowScheduler, mSettings);
		}
	}

	return true;
}

void ATDeviceDragonCart::WarmReset() {
	mDragonCart.WarmReset();
}

void ATDeviceDragonCart::ColdReset() {
	mDragonCart.ColdReset();
}

void ATDeviceDragonCart::Init() {
	mDragonCart.Init(mpMemMan, mpSlowScheduler, mSettings);
	mbDragonCartInited = true;
}

void ATDeviceDragonCart::Shutdown() {
	mbDragonCartInited = false;
	mDragonCart.Shutdown();

	mpSlowScheduler = nullptr;
	mpMemMan = nullptr;
}

void ATDeviceDragonCart::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;
}

bool ATDeviceDragonCart::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD500;
		hi = 0xD600;
		return true;
	}

	return false;
}

void ATDeviceDragonCart::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpSlowScheduler = slowsch;
}

void ATDeviceDragonCart::DumpStatus(ATConsoleOutput& output) {
	mDragonCart.DumpConnectionInfo(output);
}
