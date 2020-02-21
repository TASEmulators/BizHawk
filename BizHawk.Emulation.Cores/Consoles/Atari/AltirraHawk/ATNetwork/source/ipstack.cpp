#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/vdstl.h>
#include <at/atnetwork/ethernetframe.h>
#include "ipstack.h"

class ATNetIpStack::PendingArpEntry final : public IATEthernetClockEventSink {
public:
	PendingArpEntry(ATNetIpStack *parent, uint32 ipAddr);
	~PendingArpEntry();

	void Start();

	void AddFrame(const void *data, uint32 len);
	void Flush(const ATEthernetAddr& hwaddr);

	void OnClockEvent(uint32 eventid, uint32 userid) override;

private:
	void SendArpRequest();
	void StartEvent();
	void StopEvent();

	ATNetIpStack *const mpParent;
	const uint32 mIpAddr;
	uint32 mTimeoutEventId = 0;
	uint32 mRetriesLeft = kRetryCount;

	struct PendingFrame {
		char *mpData;
		uint32 mLen;
	};

	vdfastvector<PendingFrame> mPendingFrames;

	static const uint32 kPendingFrameLimit = 3;
	static const uint32 kRetryCount = 3;
	static const uint32 kArpRequestTimeout = 1000;
};

ATNetIpStack::PendingArpEntry::PendingArpEntry(ATNetIpStack *parent, uint32 ipAddr)
	: mpParent(parent)
	, mIpAddr(ipAddr)
{
}

ATNetIpStack::PendingArpEntry::~PendingArpEntry() {
	StopEvent();

	while(!mPendingFrames.empty()) {
		char *p = mPendingFrames.back().mpData;
		mPendingFrames.pop_back();

		delete[] p;
	}
}

void ATNetIpStack::PendingArpEntry::Start() {
	SendArpRequest();
}

void ATNetIpStack::PendingArpEntry::AddFrame(const void *data, uint32 len) {
	if (mPendingFrames.size() >= kPendingFrameLimit)
		return;

	vdautoarrayptr<char> p(new char[len]);
	memcpy(p.get(), data, len);

	mPendingFrames.push_back(PendingFrame { p.get(), len });
	p.release();
}

void ATNetIpStack::PendingArpEntry::Flush(const ATEthernetAddr& hwaddr) {
	StopEvent();

	// send pending frames
	vdfastvector<PendingFrame> pendingFrames(std::move(mPendingFrames));

	try {
		for(PendingFrame& frame : pendingFrames) {
			mpParent->SendFrame(hwaddr, frame.mpData, frame.mLen);

			delete[] frame.mpData;
			frame.mpData = nullptr;
		}
	} catch(...) {
		for(PendingFrame& frame : pendingFrames) {
			delete[] frame.mpData;
			frame.mpData = nullptr;
		}
	}
}

void ATNetIpStack::PendingArpEntry::OnClockEvent(uint32 eventid, uint32 userid) {
	mTimeoutEventId = 0;

	--mRetriesLeft;

	if (mRetriesLeft)
		SendArpRequest();
	else
		mpParent->DeletePendingArpEntry(mIpAddr);
}

void ATNetIpStack::PendingArpEntry::SendArpRequest() {
	StartEvent();

	ATEthernetArpFrameInfo arpInfo = {};
	arpInfo.mOp = arpInfo.kOpRequest;
	arpInfo.mSenderHardwareAddr = mpParent->mHwAddress;
	arpInfo.mSenderProtocolAddr = mpParent->mIpAddress;

	// Target hardware address should be ignored in requests and set to zero
	// per RFC 5227, 1.1.
	memset(&arpInfo.mTargetHardwareAddr, 0, sizeof arpInfo.mTargetHardwareAddr);

	arpInfo.mTargetProtocolAddr = mIpAddr;

	ATEthernetArpFrameBuffer frameBuffer;

	VDVERIFY(ATEthernetEncodeArpPacket(frameBuffer, sizeof frameBuffer, arpInfo));

	mpParent->SendFrame(ATEthernetGetBroadcastAddr(), frameBuffer, sizeof frameBuffer);
}

void ATNetIpStack::PendingArpEntry::StartEvent() {
	if (!mTimeoutEventId) {
		IATEthernetClock *clock = mpParent->mpEthSegment->GetClock(mpParent->mEthClockId);
		mTimeoutEventId = clock->AddClockEvent(clock->GetTimestamp(kArpRequestTimeout), this, 0);
	}
}

void ATNetIpStack::PendingArpEntry::StopEvent() {
	if (mTimeoutEventId) {
		IATEthernetClock *clock = mpParent->mpEthSegment->GetClock(mpParent->mEthClockId);
		clock->RemoveClockEvent(mTimeoutEventId);
		mTimeoutEventId = 0;
	}
}

///////////////////////////////////////////////////////////////////////////

ATNetIpStack::ATNetIpStack()
	: mpEthSegment(NULL)
	, mEthClockId(0)
	, mEthEndpointId(0)
	, mIpCounter(1)
{
}

ATNetIpStack::~ATNetIpStack() {
	Shutdown();
}

IATEthernetClock *ATNetIpStack::GetClock() const {
	return mpEthSegment->GetClock(mEthClockId);
}

bool ATNetIpStack::IsLocalOrBroadcastAddress(uint32 addr) const {
	return addr == 0xFFFFFFFFU || addr == (mIpAddress | ~mIpNetMask) || addr == mIpAddress;
}

void ATNetIpStack::Init(const ATEthernetAddr& hwaddr, uint32 ipaddr, uint32 netmask, IATEthernetSegment *segment, uint32 clockId, uint32 endpointId) {
	mHwAddress = hwaddr;
	mpEthSegment = segment;
	mEthClockId = clockId;
	mEthEndpointId = endpointId;
	mIpAddress = ipaddr;
	mIpNetMask = netmask;
}

void ATNetIpStack::Shutdown() {
	for(const auto& entry : mPendingArpRequests)
		delete entry.second;

	mPendingArpRequests.clear();

	mpEthSegment = NULL;
	mEthClockId = 0;
	mEthEndpointId = 0;
}

void ATNetIpStack::InitHeader(ATIPv4HeaderInfo& iphdr) {
	iphdr.mFlags = 0;
	iphdr.mTTL = 127;
	iphdr.mTOS = 0;
	iphdr.mId = ++mIpCounter;
	iphdr.mFragmentOffset = 0;
}

void ATNetIpStack::ClearArpCache() {
	mArpCache.clear();
}

void ATNetIpStack::AddArpEntry(uint32 ipaddr, const ATEthernetAddr& hwaddr, bool pendingOnly) {
	if (pendingOnly && mPendingArpRequests.find(ipaddr) == mPendingArpRequests.end())
		return;

	mArpCache[ipaddr] = hwaddr;

	// if there is a pending ARP entry for this IP address, flush all of its pending
	// frames
	FlushPendingArpEntry(ipaddr, hwaddr);
}

void ATNetIpStack::SendFrame(const ATEthernetAddr& dstAddr, const void *data, uint32 len) {
	ATEthernetPacket newPacket;
	newPacket.mClockIndex = mEthClockId;
	newPacket.mSrcAddr = mHwAddress;
	newPacket.mDstAddr = dstAddr;
	newPacket.mTimestamp = 100;
	newPacket.mpData = (const uint8 *)data;
	newPacket.mLength = len;
	mpEthSegment->TransmitFrame(mEthEndpointId, newPacket);
}

void ATNetIpStack::SendFrame(uint32 dstIpAddr, const void *data, uint32 len) {
	// check if we're sending to broadcast IP, in which case we should just broadcast on
	// the subnet
	if (dstIpAddr == (mIpAddress | ~mIpNetMask)) {
		SendFrame(ATEthernetGetBroadcastAddr(), data, len);
		return;
	}

	// try to find an entry in the ARP cache
	ArpCache::const_iterator it = mArpCache.find(dstIpAddr);

	if (it != mArpCache.end()) {
		SendFrame(it->second, data, len);
		return;
	}

	// Uh oh, we don't have a hardware address yet. Establish a pending ARP entry and
	// send out an ARP request if we haven't done so yet, then queue the packet. Note
	// that we may end up dropping the packet if we have too many queued already.
	auto it2 = mPendingArpRequests.insert(dstIpAddr);
	PendingArpEntry *pe = it2.first->second;

	if (!pe) {
		pe = new PendingArpEntry(this, dstIpAddr);
		it2.first->second = pe;

		pe->Start();
	}

	pe->AddFrame(data, len);
}

void ATNetIpStack::DeletePendingArpEntry(uint32 ipAddr) {
	auto it = mPendingArpRequests.find(ipAddr);

	if (it != mPendingArpRequests.end()) {
		PendingArpEntry *pe = it->second;
		mPendingArpRequests.erase(it);

		delete pe;
	} else {
		VDASSERT(!"Pending ARP entry not found.");
	}
}

void ATNetIpStack::FlushPendingArpEntry(uint32 ipAddr, const ATEthernetAddr& hwaddr) {
	auto it = mPendingArpRequests.find(ipAddr);

	if (it != mPendingArpRequests.end()) {
		PendingArpEntry *pe = it->second;
		mPendingArpRequests.erase(it);

		pe->Flush(hwaddr);
		delete pe;
	}
}
