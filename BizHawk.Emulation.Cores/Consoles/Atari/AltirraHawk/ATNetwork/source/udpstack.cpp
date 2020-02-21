#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/vdalloc.h>
#include <at/atnetwork/ethernetframe.h>
#include <at/atnetwork/udp.h>
#include "ipstack.h"
#include "udpstack.h"

ATNetUdpStack::ATNetUdpStack()
	: mpIpStack(NULL)
	, mpBridgeListener(NULL)
	, mNextPort(0)
{
}

uint32 ATNetUdpStack::GetIpAddress() const {
	return mpIpStack->GetIpAddress();
}

uint32 ATNetUdpStack::GetIpNetMask() const {
	return mpIpStack->GetIpNetMask();
}

void ATNetUdpStack::Init(ATNetIpStack *ipStack) {
	mpIpStack = ipStack;
	mpClock = ipStack->GetClock();
}

void ATNetUdpStack::Shutdown() {
	mListeningSockets.clear();
	mpBridgeListener = NULL;

	mpClock = NULL;
	mpIpStack = NULL;
}

void ATNetUdpStack::SetBridgeListener(IATUdpSocketListener *p) {
	mpBridgeListener = p;
}

bool ATNetUdpStack::Bind(uint16 port, IATUdpSocketListener *listener) {
	ListeningSockets::insert_return_type r = mListeningSockets.insert(port);

	if (!r.second)
		return false;

	r.first->second.mpHandler = listener;
	return true;
}

uint16 ATNetUdpStack::Bind(IATUdpSocketListener *listener) {
	if (mListeningSockets.size() >= 65535)
		return 0;

	for(;;) {
		if (++mNextPort > 65535)
			mNextPort = 1;

		if (Bind(mNextPort, listener))
			break;
	}

	return mNextPort;
}

void ATNetUdpStack::Unbind(uint16 port, IATUdpSocketListener *listener) {
	ListeningSockets::iterator it = mListeningSockets.find(port);

	if (it != mListeningSockets.end() && it->second.mpHandler == listener)
		mListeningSockets.erase(it);
}

void ATNetUdpStack::OnPacket(const ATEthernetPacket& packet, const ATIPv4HeaderInfo& iphdr, const uint8 *data, const uint32 len) {
	ATUdpHeaderInfo udpHdr;

	if (!ATUdpDecodeHeader(udpHdr, iphdr, data, len))
		return;

	// check if this is a connection to the gateway or if we are bridging/NATing
	IATUdpSocketListener *listener = mpBridgeListener;
	uint32 dstAddr = iphdr.mDstAddr;

	const uint32 ipaddr = mpIpStack->GetIpAddress();
	if (mpIpStack->IsLocalOrBroadcastAddress(ipaddr)) {
		// see if we have a listening socket for this port
		ListeningSockets::const_iterator itListen = mListeningSockets.find(udpHdr.mDstPort);

		if (itListen != mListeningSockets.end())
			listener = itListen->second.mpHandler;
	}

	if (listener)
		listener->OnUdpDatagram(packet.mSrcAddr, iphdr.mSrcAddr, udpHdr.mSrcPort, dstAddr, udpHdr.mDstPort, data + udpHdr.mDataOffset, udpHdr.mDataLength);
}

void ATNetUdpStack::SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen) {
	SendDatagram(srcIpAddr, srcPort, dstIpAddr, dstPort, NULL, data, dataLen);
}

void ATNetUdpStack::SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const ATEthernetAddr& dstHwAddr, const void *data, uint32 dataLen) {
	SendDatagram(srcIpAddr, srcPort, dstIpAddr, dstPort, &dstHwAddr, data, dataLen);
}

void ATNetUdpStack::SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const ATEthernetAddr *dstHwAddr, const void *data, uint32 dataLen) {
	// don't send UDP packets as coming from broadcast
	if (srcIpAddr == 0xFFFFFFFFU && srcIpAddr == (GetIpAddress() | ~GetIpNetMask()))
		srcIpAddr = GetIpAddress();

	uint32 frameLen = dataLen + 22 + 8;
	void *frame = _alloca(frameLen + 2);
	uint8 *dst = (uint8 *)frame + 2;

	// encode EtherType and IPv4 header
	ATIPv4HeaderInfo iphdr;
	mpIpStack->InitHeader(iphdr);
	iphdr.mSrcAddr = srcIpAddr;
	iphdr.mDstAddr = dstIpAddr;
	iphdr.mProtocol = 17;
	iphdr.mDataOffset = 0;
	iphdr.mDataLength = 8 + dataLen;
	VDVERIFY(ATIPv4EncodeHeader(dst, 22, iphdr));
	dst += 22;

	// encode UDP header
	VDWriteUnalignedBEU16(dst + 0, srcPort);
	VDWriteUnalignedBEU16(dst + 2, dstPort);
	dst[6] = 0;	// checksum lo (temp)
	dst[7] = 0;	// checksum hi (temp)
	VDWriteUnalignedBEU16(dst + 4, dataLen + 8);

	// compute UDP checksum
	uint64 newSum64 = iphdr.mSrcAddr;
	newSum64 += iphdr.mDstAddr;
	newSum64 += VDToBE32(0x110000 + 8 + dataLen);

	const uint8 *chksrc = (const uint8 *)data;
	for(uint32 dataLen4 = dataLen >> 2; dataLen4; --dataLen4) {
		newSum64 += VDReadUnalignedU32(chksrc);
		chksrc += 4;
	}

	if (dataLen & 2) {
		newSum64 += VDReadUnalignedU16(chksrc);
		chksrc += 2;
	}

	if (dataLen & 1)
		newSum64 += VDFromLE16(*chksrc);

	VDWriteUnalignedU16(dst + 6, ATIPComputeChecksum(newSum64, dst, 2));

	dst += 8;

	if (dataLen)
		memcpy(dst, data, dataLen);

	if (dstHwAddr)
		mpIpStack->SendFrame(*dstHwAddr, (char *)frame + 2, frameLen);
	else
		mpIpStack->SendFrame(dstIpAddr, (char *)frame + 2, frameLen);
}
