//	Altirra - Atari 800/800XL/5200 emulator
//	Networking emulation library - internal TCP implementation
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

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/int128.h>
#include <vd2/system/time.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/logging.h>
#include <at/atnetwork/ethernetframe.h>
#include <at/atnetwork/tcp.h>
#include "ipstack.h"
#include "tcpstack.h"

ATLogChannel g_ATLCTCP(false, false, "TCP", "TCP/IP stack emulation");

ATNetTcpRingBuffer::ATNetTcpRingBuffer()
	: mReadPtr(0)
	, mWritePtr(0)
	, mLevel(0)
	, mBaseSeq(0)
{
}

void ATNetTcpRingBuffer::Init(char *buf, uint32 size) {
	mpBuffer = buf;
	mSize = size;
}

void ATNetTcpRingBuffer::Reset(uint32 seq) {
	mBaseSeq = seq;
	mReadPtr = 0;
	mWritePtr = 0;
	mLevel = 0;
}

uint32 ATNetTcpRingBuffer::Write(const void *p, uint32 n) {
	if (mSize - mLevel < n)
		n = mSize - mLevel;

	mLevel += n;

	uint32 toWrap = mSize - mWritePtr;
	if (toWrap < n) {
		memcpy(mpBuffer + mWritePtr, p, toWrap);
		p = (const char *)p + toWrap;
		n -= toWrap;
		mWritePtr = 0;
	}

	memcpy(mpBuffer + mWritePtr, p, n);
	mWritePtr += n;
	return n;
}

void ATNetTcpRingBuffer::Read(uint32 offset, void *p, uint32 n) const {
	VDASSERT(offset <= mLevel && mLevel - offset >= n);

	uint32 readPtr = mReadPtr + offset;

	while(n) {
		if (readPtr >= mSize)
			readPtr -= mSize;

		uint32 tc = mSize - readPtr;
		if (tc > n)
			tc = n;

		n -= tc;

		memcpy(p, mpBuffer + readPtr, tc);
		p = (char *)p + tc;
		readPtr += tc;
	}
}

void ATNetTcpRingBuffer::Ack(uint32 n) {
	VDASSERT(n <= mLevel);

	mReadPtr += n;
	if (mReadPtr >= mSize)
		mReadPtr -= mSize;

	mLevel -= n;
	mBaseSeq += n;
}

///////////////////////////////////////////////////////////////////////////

ATNetTcpStack::ATNetTcpStack()
	: mpIpStack(NULL)
	, mpBridgeListener(NULL)
	, mPortCounter(49152)
{
}

void ATNetTcpStack::Init(ATNetIpStack *ipStack) {
	mpIpStack = ipStack;
	mpClock = ipStack->GetClock();

	mXmitInitialSequenceSalt = (uint32)(VDGetCurrentProcessId() ^ (VDGetPreciseTick() / 147));
}

void ATNetTcpStack::Shutdown() {
	mListeningSockets.clear();

	CloseAllConnections();

	mpClock = NULL;
	mpIpStack = NULL;
}

void ATNetTcpStack::SetBridgeListener(IATSocketListener *p) {
	mpBridgeListener = p;
}

bool ATNetTcpStack::Bind(uint16 port, IATSocketListener *listener) {
	ListeningSockets::insert_return_type r = mListeningSockets.insert(port);

	if (!r.second)
		return false;

	r.first->second.mpHandler = listener;
	return true;
}

void ATNetTcpStack::Unbind(uint16 port, IATSocketListener *listener) {
	ListeningSockets::iterator it = mListeningSockets.find(port);

	if (it != mListeningSockets.end() && it->second.mpHandler == listener)
		mListeningSockets.erase(it);
}

 bool ATNetTcpStack::Connect(uint32 dstIpAddr, uint16 dstPort, IATSocketHandler *handler, IATSocket **newSocket) {
	// find an unused port in dynamic range
	for(uint32 i=49152; i<65535; ++i) {
		if (++mPortCounter == 0)
			mPortCounter = 49152;

		bool valid = true;

		if (mListeningSockets.find(mPortCounter) != mListeningSockets.end()) {
			valid = false;
		} else {
			for(const auto& conn : mConnections) {
				if (conn.first.mLocalPort == mPortCounter) {
					valid = false;
					break;
				}
			}
		}

		if (valid)
			goto found_free_port;
	}

	// doh... no free ports!
	return false;

found_free_port:

	// create connection key
	ATNetTcpConnectionKey connKey;
	connKey.mLocalAddress = mpIpStack->GetIpAddress();
	connKey.mLocalPort = mPortCounter;
	connKey.mRemoteAddress = dstIpAddr;
	connKey.mRemotePort = dstPort;

	// initialize new connection
	vdrefptr<ATNetTcpConnection> conn(new ATNetTcpConnection(this, connKey));

	conn->InitOutgoing(handler, mXmitInitialSequenceSalt);

	mConnections[connKey] = conn;
	conn->AddRef();

	// Send SYN+ACK
	conn->Transmit(false);

	*newSocket = conn.release();
	return true;
}

void ATNetTcpStack::CloseAllConnections() {
	for(Connections::const_iterator it = mConnections.begin(), itEnd = mConnections.end();
		it != itEnd;
		++it)
	{
		delete it->second;
	}

	mConnections.clear();
}

void ATNetTcpStack::GetConnectionInfo(vdfastvector<ATNetTcpConnectionInfo>& conns) const {
	conns.resize(mConnections.size());

	ATNetTcpConnectionInfo *dst = conns.data();

	for(Connections::const_iterator it = mConnections.begin(), itEnd = mConnections.end();
		it != itEnd;
		++it)
	{
		it->second->GetInfo(*dst++);
	}
}

void ATNetTcpStack::OnPacket(const ATEthernetPacket& packet, const ATIPv4HeaderInfo& iphdr, const uint8 *data, const uint32 len) {
	ATTcpHeaderInfo tcpHdr;

	if (!ATTcpDecodeHeader(tcpHdr, iphdr, data, len))
		return;

	// attempt to lookup connection
	ATNetTcpConnectionKey connKey;
	connKey.mLocalAddress = iphdr.mDstAddr;
	connKey.mRemoteAddress = iphdr.mSrcAddr;
	connKey.mRemotePort = tcpHdr.mSrcPort;
	connKey.mLocalPort = tcpHdr.mDstPort;

	Connections::iterator itConn = mConnections.find(connKey);

	if (itConn != mConnections.end()) {
		ATNetTcpConnection& conn = *itConn->second;

		conn.AddRef();
		conn.OnPacket(packet, iphdr, tcpHdr, data, len);
		conn.Release();

		return;
	}

	// if this is an RST, drop it on the floor -- we have no connection, so nothing to reset
	if (tcpHdr.mbRST)
		return;

	// if this isn't a SYN packet, send RST
	if (!tcpHdr.mbSYN) {
		SendReset(iphdr, tcpHdr.mSrcPort, tcpHdr.mDstPort, tcpHdr);
		return;
	}

	// check if this is a connection to the gateway or if we are bridging/NATing
	IATSocketListener *listener = mpBridgeListener;

	if (iphdr.mDstAddr == mpIpStack->GetIpAddress()) {
		// see if we have a listening socket for this port
		ListeningSockets::const_iterator itListen = mListeningSockets.find(tcpHdr.mDstPort);

		if (itListen != mListeningSockets.end())
			listener = itListen->second.mpHandler;
	}

	if (!listener) {
		// No socket is listening on this port -- send RST
		SendReset(iphdr, tcpHdr.mSrcPort, tcpHdr.mDstPort, tcpHdr);
		return;
	}

	// Socket is listening -- establish a new connection in SYN_RCVD state
	vdrefptr<ATNetTcpConnection> conn(new ATNetTcpConnection(this, connKey));

	conn->Init(packet, iphdr, tcpHdr, data);

	vdrefptr<IATSocketHandler> socketHandler;
	if (!listener->OnSocketIncomingConnection(iphdr.mSrcAddr, tcpHdr.mSrcPort, iphdr.mDstAddr, tcpHdr.mDstPort, conn, ~socketHandler)) {
		// Uh oh... we can't accept this connection.
		SendReset(iphdr, tcpHdr.mSrcPort, tcpHdr.mDstPort, tcpHdr);
		return;
	}

	conn->SetSocketHandler(socketHandler);

	mConnections[connKey] = conn;
	ATNetTcpConnection *conn2 = conn.release();

	// Send SYN+ACK
	conn2->Transmit(true);
}

uint32 ATNetTcpStack::EncodePacket(uint8 *dst, uint32 len, uint32 srcIpAddr, uint32 dstIpAddr, const ATTcpHeaderInfo& hdrInfo, const void *data, uint32 dataLen, const void *opts, uint32 optLen) {
	if (len < 22 + 20 + dataLen)
		return 0;

	// encode EtherType and IPv4 header
	const uint32 optLenM4 = (optLen + 3) & ~3;
	ATIPv4HeaderInfo iphdr;
	mpIpStack->InitHeader(iphdr);
	iphdr.mSrcAddr = srcIpAddr;
	iphdr.mDstAddr = dstIpAddr;
	iphdr.mProtocol = 6;
	iphdr.mDataOffset = 0;
	iphdr.mDataLength = 20 + dataLen + optLenM4;
	VDVERIFY(ATIPv4EncodeHeader(dst, 22, iphdr));
	dst += 22;

	// encode TCP header
	VDWriteUnalignedBEU16(dst + 0, hdrInfo.mSrcPort);
	VDWriteUnalignedBEU16(dst + 2, hdrInfo.mDstPort);
	VDWriteUnalignedBEU32(dst + 4, hdrInfo.mSequenceNo);
	VDWriteUnalignedBEU32(dst + 8, hdrInfo.mAckNo);
	dst[12] = 0x50 + (optLenM4 << 2);
	dst[13] = 0;
	if (hdrInfo.mbURG) dst[13] |= 0x20;
	if (hdrInfo.mbACK) dst[13] |= 0x10;
	if (hdrInfo.mbPSH) dst[13] |= 0x08;
	if (hdrInfo.mbRST) dst[13] |= 0x04;
	if (hdrInfo.mbSYN) dst[13] |= 0x02;
	if (hdrInfo.mbFIN) dst[13] |= 0x01;

	VDWriteUnalignedBEU16(dst + 14, hdrInfo.mWindow);
	dst[16] = 0;	// checksum lo (temp)
	dst[17] = 0;	// checksum hi (temp)
	VDWriteUnalignedBEU16(dst + 18, hdrInfo.mUrgentPtr);

	// add TCP options
	if (opts) {
		memcpy(dst + 20, opts, optLen);
		memset(dst + 20 + optLen, 0, (4 - optLen) & 3);
	}

	//---- compute TCP checksum
	//
	// Note that the Internet checksum is associative, so we can do the sums in
	// any order.

	// checksum pseudo-header
	uint64 newSum64 = iphdr.mSrcAddr;
	newSum64 += iphdr.mDstAddr;
	newSum64 += VDToBE32(0x60000 + 20 + dataLen + optLenM4);

	// checksum data payload
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

	// checksum header and write
	VDWriteUnalignedU16(dst + 16, ATIPComputeChecksum(newSum64, dst, 5 + (optLenM4 >> 2)));

	dst += 20 + optLenM4;

	if (dataLen)
		memcpy(dst, data, dataLen);

	return 22 + 20 + dataLen + optLenM4;
}

void ATNetTcpStack::SendReset(const ATIPv4HeaderInfo& iphdr, uint16 srcPort, uint16 dstPort, const ATTcpHeaderInfo& origTcpHdr) {
	SendReset(iphdr.mSrcAddr, iphdr.mDstAddr, srcPort, dstPort, origTcpHdr);
}

void ATNetTcpStack::SendReset(uint32 srcIpAddr, uint32 dstIpAddr, uint16 srcPort, uint16 dstPort, const ATTcpHeaderInfo& origTcpHdr) {
	VDALIGN(4) uint8 rstPacket[42 + 2];

	ATTcpHeaderInfo tcpHeader = {};
	tcpHeader.mSrcPort = dstPort;
	tcpHeader.mDstPort = srcPort;
	tcpHeader.mSequenceNo = origTcpHdr.mAckNo;
	tcpHeader.mbRST = true;

	// For a SYN packet, we haven't established a sequence yet, so we need to ACK
	// the SYN instead.
	if (origTcpHdr.mbSYN) {
		tcpHeader.mbACK = true;
		tcpHeader.mAckNo = origTcpHdr.mSequenceNo + 1;
	}

	VDVERIFY(EncodePacket(rstPacket + 2, 42, dstIpAddr, srcIpAddr, tcpHeader, NULL, 0));

	SendFrame(srcIpAddr, rstPacket + 2, 42);
}

void ATNetTcpStack::SendFrame(uint32 dstIpAddr, const void *data, uint32 len) {
	mpIpStack->SendFrame(dstIpAddr, data, len);
}

void ATNetTcpStack::SendFrame(const ATEthernetAddr& dstAddr, const void *data, uint32 len) {
	mpIpStack->SendFrame(dstAddr, data, len);
}

void ATNetTcpStack::DeleteConnection(const ATNetTcpConnectionKey& connKey) {
	Connections::iterator it = mConnections.find(connKey);

	if (it != mConnections.end()) {
		ATNetTcpConnection *conn = it->second;

		mConnections.erase(it);
		conn->Release();
	} else {
		VDASSERT(!"Attempt to delete a nonexistent TCP connection.");
	}
}

///////////////////////////////////////////////////////////////////////////

ATNetTcpConnection::ATNetTcpConnection(ATNetTcpStack *stack, const ATNetTcpConnectionKey& connKey)
	: mpTcpStack(stack)
	, mConnKey(connKey)
{
	mRecvRing.Init(mRecvBuf, sizeof mRecvBuf);
	mXmitRing.Init(mXmitBuf, sizeof mXmitBuf);

	PacketTimer& root = mPacketTimers.push_back();
	root.mNext = 0;
	root.mPrev = 0;
	root.mSequenceStart = 0;
	root.mSequenceEnd = 0;
}

ATNetTcpConnection::~ATNetTcpConnection() {
	ClearEvents();
}

void ATNetTcpConnection::GetInfo(ATNetTcpConnectionInfo& info) const {
	info.mConnKey = mConnKey;
	info.mConnState = mConnState;
}

void ATNetTcpConnection::Init(const ATEthernetPacket& packet, const ATIPv4HeaderInfo& ipHdr, const ATTcpHeaderInfo& tcpHdr, const uint8 *data) {
	mConnState = kATNetTcpConnectionState_SYN_RCVD;

	// initialize receive state to received sequence number
	mRecvRing.Reset(tcpHdr.mSequenceNo + 1);

	// initialize our sequence number
	mXmitNext = 1;

	// initialize transmit window
	mXmitLastAck = tcpHdr.mSequenceNo + 1;
	mXmitWindowLimit = mXmitLastAck + tcpHdr.mWindow;

	// queue bogus data for SYN packet
	mXmitRing.Reset(mXmitNext);
	mXmitRing.Write("", 1);

	mbSynQueued = true;

	ProcessSynOptions(packet, tcpHdr, data);
}

void ATNetTcpConnection::InitOutgoing(IATSocketHandler *h, uint32 isnSalt) {
	mpSocketHandler = h;

	mConnState = kATNetTcpConnectionState_SYN_SENT;

	// Compute initial sequence number (ISN) [RFC6528].
	// We're not currently doing hashing, so this isn't a very secure
	// implementation yet.
	mXmitNext = (uint32)(vduint128(VDGetPreciseTick()) * vduint128(250000) / vduint128(VDGetPreciseTicksPerSecondI()));
	mXmitNext ^= isnSalt;
	mXmitNext += mConnKey.mLocalAddress;
	mXmitNext += VDRotateLeftU32(mConnKey.mRemoteAddress, 14);
	mXmitNext += mConnKey.mLocalPort;
	mXmitNext += (uint32)mConnKey.mRemotePort << 16;

	// Init placeholder values for the remote sequence. The zero window
	// will prevent us from sending until we get the real values from
	// the SYN+ACK reply.
	mXmitLastAck = 0;
	mXmitWindowLimit = 0;

	// Queue the SYN packet and the bogus data for the sequence number
	// it occupies.
	mXmitRing.Reset(mXmitNext);
	mXmitRing.Write("", 1);

	mbSynQueued = true;
}

void ATNetTcpConnection::SetSocketHandler(IATSocketHandler *h) {
	mpSocketHandler = h;
}

void ATNetTcpConnection::OnPacket(const ATEthernetPacket& packet, const ATIPv4HeaderInfo& iphdr, const ATTcpHeaderInfo& tcpHdr, const uint8 *data, const uint32 len) {
	VDASSERT(mpTcpStack);

	// check if RST is set
	if (tcpHdr.mbRST) {
		// if we are in SYN-SENT, the RST is valid if it ACKs the SYN; otherwise, it is
		// valid if it is within the window
		if (mConnState == kATNetTcpConnectionState_SYN_SENT) {
			if (!tcpHdr.mbACK || tcpHdr.mAckNo != mXmitNext) {
				g_ATLCTCP <<= "Rejecting invalid RST during SYN_SENT phase\n";
				return;
			}
		} else {
			// Note that we may have advertised a zero window. Pretend that the RST takes
			// no space, so it can fit at the end.
			if ((uint32)(tcpHdr.mSequenceNo - mRecvRing.GetBaseSeq()) > mRecvRing.GetSpace()) {
				g_ATLCTCP("Rejecting invalid RST due to bad sequence number: %u not in [%u, %u)\n"
					, tcpHdr.mSequenceNo
					, mRecvRing.GetBaseSeq()
					, mRecvRing.GetBaseSeq() + mRecvRing.GetSpace()
					);
				return;
			}
		}

		// mark both ends closed so we don't send a RST in response to a RST and so that we
		// don't respond to a local close
		mbLocalOpen = false;
		mbFinReceived = true;
		// delete the connection :-/
		if (mpSocketHandler)
			mpSocketHandler->OnSocketError();

		g_ATLCTCP("Closing connection due to RST\n");

		Shutdown();
		return;
	}

	// check if we're getting a SYN (only valid in this path if we are connecting out)
	if (mConnState == kATNetTcpConnectionState_SYN_SENT) {
		// We had better get a SYN or SYN+ACK. RST was already handled above.
		//
		// HOWEVER:
		// - It is valid to receive a SYN in response to a SYN, instead of a SYN+ACK.
		//   This happens if both sides simultaneously attempt to connect to each other.
		//   This results in a single connection and is explicitly allowed by RFC793 3.4
		//   and reaffirmed by RFC1122 4.2.2.10.
		//
		// - We can also get FIN. One-packet SYN+ACK+FIN is allowed....
		//
		if (!tcpHdr.mbSYN) {
			g_ATLCTCP("Aborting connection due to receiving packet without SYN or RST during SYN_SENT phase.\n");

			if (mpSocketHandler)
				mpSocketHandler->OnSocketError();

			Shutdown();
			return;
		}

		mConnState = kATNetTcpConnectionState_SYN_RCVD;

		// initialize receive state to received sequence number
		mRecvRing.Reset(tcpHdr.mSequenceNo + 1);

		// process options
		ProcessSynOptions(packet, tcpHdr, data);
	}

	if (g_ATLCTCP.IsEnabled()) {
		g_ATLCTCP("Received packet: seq=%u, ack=%u, xmitbuf=%u:%u(%u)\n"
			, tcpHdr.mSequenceNo
			, tcpHdr.mAckNo
			, mXmitRing.GetBaseSeq()
			, mXmitRing.GetTailSeq()
			, mXmitRing.GetLevel()
			);
	}

	// update window
	if (tcpHdr.mbACK)
		mXmitLastAck = tcpHdr.mAckNo;

	mXmitWindowLimit = mXmitLastAck + tcpHdr.mWindow;

	if (mXmitMaxWindow < tcpHdr.mWindow) {
		mXmitMaxWindow = tcpHdr.mWindow;
		mXmitWindowThreshold = std::min<uint32>(mXmitMaxWindow >> 1, 256);
	}

	// check if data is being ACKed
	if (tcpHdr.mbACK) {
		uint32 xmitBaseSeq = mXmitRing.GetBaseSeq();
		uint32 ackOffset = (uint32)(tcpHdr.mAckNo - xmitBaseSeq);

		if (ackOffset <= mXmitRing.GetLevel()) {
			// update the retransmit queue
			PacketTimer& root = mPacketTimers[0];
			while(root.mNext) {
				const uint32 timerIdx = root.mNext;
				PacketTimer& timer = mPacketTimers[timerIdx];

				// Stop removing packet timers once we reach an entry that isn't fully ACK'd.
				//
				// We need to clamp the sequence number in the timer to the current window.
				// We may either have been probing a zero window or the window may have been
				// retracted, in which case we should accept the farthest ACK within the
				// window possible.
				uint32 packetExpectedAck = timer.mSequenceEnd;

				if ((uint32)(packetExpectedAck - mXmitWindowLimit) < 0x80000000U)		// expectedAck >= windowLimit
					packetExpectedAck = mXmitWindowLimit;

				if ((uint32)(packetExpectedAck - tcpHdr.mAckNo - 1) < 0x7FFFFFFFU) {
					g_ATLCTCP("Next packet in retransmit queue: [%u,%u) not cleared by [%u,%u)\n", timer.mSequenceStart, timer.mSequenceEnd, mXmitLastAck, mXmitWindowLimit);
					break;
				}

				g_ATLCTCP("Removing packet from retransmit queue: [%u,%u)\n", timer.mSequenceStart, timer.mSequenceEnd);

				if (mEventRetransmit) {
					mpTcpStack->GetClock()->RemoveClockEvent(mEventRetransmit);
					mEventRetransmit = 0;
				}

				root.mNext = timer.mNext;
				mPacketTimers[root.mNext].mPrev = 0;

				// link timer into free list
				timer.mNext = root.mSequenceStart;
				root.mSequenceStart = timerIdx;
			}

			if (root.mNext && !mEventRetransmit) {
				auto *pClock = mpTcpStack->GetClock();
				mEventRetransmit = pClock->AddClockEvent(pClock->GetTimestamp(3000), this, kEventId_Retransmit);
				g_ATLCTCP("Resetting retransmit timer\n");
			}

			if (ackOffset) {
				mXmitRing.Ack(ackOffset);
				mbSynQueued = false;

				// if our side is closed but we haven't yet allocated the sequence number
				// for the FIN packet, do so now as now we have space
				if (!mbLocalOpen && !mbFinQueued) {
					mbFinQueued = true;
					mXmitRing.Write("", 1);
				}

				// check if we were in SYN_RCVD status -- if so, we just got the ACK we were waiting for
				if (mConnState == kATNetTcpConnectionState_SYN_RCVD)
					mConnState = kATNetTcpConnectionState_ESTABLISHED;

				// notify the socket handler that more space is available; however, note
				// that this is internal buffer, not window space
				if (mpSocketHandler && mbLocalOpen)
					mpSocketHandler->OnSocketWriteReady(mXmitRing.GetSpace());

				if (!mpTcpStack)
					return;
			}

			// if the ACK just emptied the buffer and we already queued the FIN, then the FIN
			// has been ACKed
			if (mbFinQueued && !mXmitRing.GetLevel()) {
				switch(mConnState) {
					case kATNetTcpConnectionState_CLOSING:
						mConnState = kATNetTcpConnectionState_TIME_WAIT;

						VDASSERT(!mEventClose);
						{
							IATEthernetClock *clk = mpTcpStack->GetClock();

							mEventClose = clk->AddClockEvent(clk->GetTimestamp(500), this, kEventId_Close);
						}
						break;

					case kATNetTcpConnectionState_LAST_ACK:
						mConnState = kATNetTcpConnectionState_CLOSED;
						break;

					case kATNetTcpConnectionState_FIN_WAIT_1:
						mConnState = kATNetTcpConnectionState_FIN_WAIT_2;
						break;
				}
			}
		}
	}

	// check if new data is coming in; note that FIN takes a sequence number slot
	uint32 ackLen = tcpHdr.mDataLength + (tcpHdr.mbFIN ? 1 : 0);
	bool ackNeeded = tcpHdr.mbSYN;

	if (ackLen) {
		// we always need to reply if data is coming in
		ackNeeded = true;

		// check if the new data is where we expect it to be
		if (tcpHdr.mSequenceNo == mRecvRing.GetTailSeq()) {
			// check how much space we have
			const uint32 recvSpace = mRecvRing.GetSpace();
			uint32 tc = ackLen;

			// truncate the new data if we don't have enough space; note that we
			// explicitly must ACK with win=0 on one byte on closed window, as this
			// is required for the sender to probe the window
			if (tc > recvSpace)
				tc = recvSpace;

			if (tc) {
				// check for special case where we are ending with FIN
				if (tc > tcpHdr.mDataLength) {
					mRecvRing.Write(data + tcpHdr.mDataOffset, tcpHdr.mDataLength);

					if (!mbFinReceived) {
						mbFinReceived = true;

						// advance state since we have now received and processed the FIN
						switch(mConnState) {
							case kATNetTcpConnectionState_ESTABLISHED:
								mConnState = kATNetTcpConnectionState_CLOSE_WAIT;
								break;

							case kATNetTcpConnectionState_FIN_WAIT_2:
								mConnState = kATNetTcpConnectionState_TIME_WAIT;

								VDASSERT(!mEventClose);
								mEventClose = mpTcpStack->GetClock()->AddClockEvent(500, this, kEventId_Close);
								break;
						}

						if (mpSocketHandler)
							mpSocketHandler->OnSocketClose();

						if (!mpTcpStack)
							return;
					}
				} else
					mRecvRing.Write(data + tcpHdr.mDataOffset, tc);

				if (mpSocketHandler)
					mpSocketHandler->OnSocketReadReady(mRecvRing.GetLevel());

				if (!mpTcpStack)
					return;
			}
		}
	}

	// if the connection is dead, delete it now -- this should be done before we send a reply
	// packet, in case we got both an ACK and the last FIN
	if (mConnState == kATNetTcpConnectionState_CLOSED) {
		Shutdown();
		return;
	}

	// check if we need a reply packet or if we received an ACK and can transmit now
	if (ackNeeded)
		Transmit(true);
	else if (!mEventTransmit)
		TryTransmitMore(true);
}

void ATNetTcpConnection::TryTransmitMore(bool immediate) {
	auto transmitStatus = GetTransmitStatus();
	IATEthernetClock *clk = mpTcpStack->GetClock();

	if (transmitStatus == kTransmitStatus_Yes) {
		if (immediate)
			Transmit(false);
		else if (!mEventTransmit) {
			mEventTransmit = clk->AddClockEvent(clk->GetTimestamp(1), this, kEventId_Transmit);
		}
	} else {
		if (mEventTransmit) {
			clk->RemoveClockEvent(mEventTransmit);
			mEventTransmit = 0;
		}

		if (transmitStatus == kTransmitStatus_Deferred) {
			if (!mEventZeroWindowProbe) {
				clk->RemoveClockEvent(mEventZeroWindowProbe);
				mEventZeroWindowProbe = 0;
			}

			// one-second timeout for SWS avoidance
			mEventWindowProbe = clk->AddClockEvent(clk->GetTimestamp(1000), this, kEventId_WindowProbe);
		} else if (transmitStatus == kTransmitStatus_DeferredZeroWindow) {
			if (!mEventWindowProbe) {
				clk->RemoveClockEvent(mEventWindowProbe);
				mEventWindowProbe = 0;
			}

			if (!mEventZeroWindowProbe) {
				// 30-second timeout for zero window probe
				mEventZeroWindowProbe = clk->AddClockEvent(clk->GetTimestamp(30000), this, kEventId_ZeroWindowProbe);
			}
		}
	}
}

void ATNetTcpConnection::Transmit(bool ack, int retransmitCount, bool enableWindowProbe) {
	uint8 replyPacket[576];
	uint8 data[512];

	VDASSERT(sizeof(data) >= mXmitMaxSegment);

	// Kill the window probe and transmit timers, as we are going to actually send a packet.
	if (mEventWindowProbe) {
		mpTcpStack->GetClock()->RemoveClockEvent(mEventWindowProbe);
		mEventWindowProbe = 0;
	}

	if (mEventZeroWindowProbe) {
		mpTcpStack->GetClock()->RemoveClockEvent(mEventZeroWindowProbe);
		mEventZeroWindowProbe = 0;
	}

	if (mEventTransmit) {
		mpTcpStack->GetClock()->RemoveClockEvent(mEventTransmit);
		mEventTransmit = 0;
	}

	// Check if we are sending a SYN packet. The SYN occupies a sequence number, which
	// we fake in the transmit buffer with a dummy byte. However, there is no byte sent
	// in the payload to correspond to the SYN and we must not send it.
	//
	// It is possible to send data with SYN (see RFC 7413 - TCP Fast Open), but it's
	// unusual and considered suspicious. From Cisco Security
	// (https://tools.cisco.com/security/center/viewIpsSignature.x?signatureId=1314&signatureSubId=0&softwareVersion=6.0&releaseVersion=S272):
	//
	// "This signature will fire when TCP payload is sent in the SYN packet. Sending
	//  data in the SYN packet has been used as an evasion technique for security
	//  inspection systems."
	//
	// Therefore, we always force the SYN to be sent alone first. Any additional data
	// is held until we have confirmed that the 3WHS is completed.
	//
	const uint32 xmitOffset = mXmitNext - mXmitRing.GetBaseSeq();
	bool syn = mbSynQueued && xmitOffset == 0;
	bool fin = false;

	// compute how much data payload to send; we avoid doing so for a SYN packet
	// because it's considered unusual behavior, although normal
	uint32 dataLen = syn || mbSynQueued ? 0 : mXmitRing.GetLevel() - xmitOffset;
	bool sendingLast = true;

	// limit data send according to window
	uint32 windowSpace = mXmitWindowLimit - mXmitNext;
	if (windowSpace >= 0x80000000U) {
		// The receive window has shrunk. This is discouraged but valid according
		// to RFC 793.
		windowSpace = 0;
	}

	// probe with one byte if we have data waiting, window is closed, and we are waiting
	if (enableWindowProbe && !windowSpace && dataLen > 0 && !syn)
		dataLen = 1;

	if (dataLen > windowSpace) {
		dataLen = windowSpace;
		sendingLast = false;
	}

	// limit data send according to outgoing MSS
	if (dataLen > mXmitMaxSegment) {
		dataLen = mXmitMaxSegment;
		sendingLast = false;
	} else if (syn) {
		dataLen = 0;
	} else if (mbFinQueued && sendingLast && dataLen) {
		// If we have already sent a FIN, we must not send one again short of a
		// resend, as it takes a sequence number. This means that once we send
		// a FIN and it's been ACKed, any subsequent ACKs we send will not have
		// FIN set.
		fin = true;

		// FIN takes a sequence number and we queue a ring byte for it, but we
		// don't actually send a byte for it.
		--dataLen;
	}

	// compute ending sequence number
	const uint32 xmitNextNext = mXmitNext + dataLen + (fin ? 1 : 0) + (syn ? 1 : 0);

	// set PSH if we are sending data and have reached the end
	const bool psh = dataLen && (xmitNextNext == mXmitRing.GetTailSeq() - (fin ? 1 : 0));

	ATTcpHeaderInfo replyTcpHeader = {};

	// ACK must always be sent once the connection is established [RFC793, 3.1].
	// Linux/BSD will work without it, but the Windows 8/10 TCP stack barfs with
	// an ACK Invalid error according to the ETW log.
	if (!syn)
		ack = true;

	replyTcpHeader.mSrcPort = mConnKey.mLocalPort;
	replyTcpHeader.mDstPort = mConnKey.mRemotePort;
	replyTcpHeader.mbACK = ack;
	replyTcpHeader.mbSYN = syn;
	replyTcpHeader.mbFIN = fin;
	replyTcpHeader.mbPSH = psh;
	replyTcpHeader.mAckNo = ack ? mRecvRing.GetBaseSeq() + mRecvRing.GetLevel() + (mbFinReceived ? 1 : 0) : 0;
	replyTcpHeader.mSequenceNo = mXmitNext;
	replyTcpHeader.mWindow = mRecvRing.GetSpace();
	
	if (dataLen)
		mXmitRing.Read(xmitOffset, data, dataLen);

	// If we are sending a SYN, include MSS. We do this both for origination and a reply.
	uint8 optdat[4];
	const uint8 *opts = nullptr;
	uint32 optLen = 0;

	if (syn) {
		optdat[0] = 2;		// Kind=2 (Maximum Segment Size)
		optdat[1] = 4;		// Length=4
		VDWriteUnalignedBEU16(optdat + 2, mRecvMaxSegment);

		opts = optdat;
		optLen = 4;
	}

	uint32 replyLen = mpTcpStack->EncodePacket(replyPacket + 2, sizeof replyPacket - 2, mConnKey.mLocalAddress, mConnKey.mRemoteAddress, replyTcpHeader, data, dataLen, opts, optLen);

	mpTcpStack->SendFrame(mConnKey.mRemoteAddress, replyPacket + 2, replyLen);

	mXmitNext = xmitNextNext;
	VDASSERT(mXmitNext - mXmitRing.GetBaseSeq() <= mXmitRing.GetLevel());

	// if we sent data or a FIN, queue a packet timer
	if (dataLen || replyTcpHeader.mbFIN) {
		uint32 free = mPacketTimers[0].mSequenceStart;

		if (!free) {
			free = (uint32)mPacketTimers.size();
			mPacketTimers.push_back();
		} else {
			mPacketTimers[0].mSequenceStart = mPacketTimers[free].mNext;
		}

		PacketTimer& root = mPacketTimers[0];
		PacketTimer& timer = mPacketTimers[free];

		timer.mPrev = root.mPrev;
		timer.mNext = 0;
		mPacketTimers[root.mPrev].mNext = free;
		root.mPrev = free;

		IATEthernetClock *clk = mpTcpStack->GetClock();
		timer.mSequenceStart = replyTcpHeader.mSequenceNo;
		timer.mSequenceEnd = mXmitNext;
		timer.mRetransmitCount = retransmitCount;

		if (!mEventRetransmit)
			mEventRetransmit = clk->AddClockEvent(clk->GetTimestamp(3000), this, kEventId_Retransmit);
	}

	// queue future transmits as needed
	TryTransmitMore(false);
}

void ATNetTcpConnection::OnClockEvent(uint32 eventid, uint32 userid) {
	switch(userid) {
		case kEventId_Close:
			VDASSERT(mConnState == kATNetTcpConnectionState_TIME_WAIT);
			VDASSERT(mEventClose == eventid);
			mEventClose = 0;

			Shutdown();
			return;

		case kEventId_Transmit:
			mEventTransmit = 0;

			TryTransmitMore(true);
			return;

		case kEventId_Retransmit:
			mEventRetransmit = 0;

			{
				PacketTimer& root = mPacketTimers.front();
				VDASSERT(root.mNext);

				// go back to the first packet
				PacketTimer& head = mPacketTimers[root.mNext];

				// bump the retransmit count
				int rtcount = head.mRetransmitCount + 1;

				// check if we've exceeded max
				if (rtcount >= kMaxRetransmits) {
					// uh oh -- nuke the connection
					g_ATLCTCP("Dropping connection due to max retransmit limit being reached.\n");

					mbLocalOpen = false;
					mbFinReceived = true;

					AddRef();
					if (mpSocketHandler)
						mpSocketHandler->OnSocketError();

					Shutdown();
					Release();
					return;
				}

				// check if we've gotten an ACK partway
				const uint32 xmitBase = mXmitRing.GetBaseSeq();

				if ((uint32)(head.mSequenceStart - xmitBase) < mXmitRing.GetLevel())
					mXmitNext = head.mSequenceStart;
				else
					mXmitNext = xmitBase;

				g_ATLCTCP("Retransmitting at %u due to lost or unacknowledged packet: [%u,%u)\n", mXmitNext, head.mSequenceStart, head.mSequenceEnd);

				// clear the ring
				root.mPrev = root.mNext = root.mSequenceStart = 0;
				mPacketTimers.resize(1);

				// retransmit starting at highest found ACK
				Transmit(true, rtcount);
			}
			return;

		case kEventId_WindowProbe:
			mEventWindowProbe = 0;

			// If we can transmit for any reason, do so.
			if (GetTransmitStatus() != kTransmitStatus_No) {
				g_ATLCTCP("Sending window probe\n");
				Transmit(false, 0, true);
			}

			return;

		case kEventId_ZeroWindowProbe:
			mEventZeroWindowProbe = 0;

			// If we can transmit for any reason, do so.
			if (GetTransmitStatus() != kTransmitStatus_No) {
				g_ATLCTCP("Sending zero window probe\n");
				Transmit(false, 0, true);
			}

			return;
	}
}

void ATNetTcpConnection::Shutdown() {
	if (!mpTcpStack)
		return;

	AddRef();

	// If one side hasn't been closed, send an RST.
	if (mbLocalOpen || !mbFinReceived) {
		ATTcpHeaderInfo dummyHdr = {};
		dummyHdr.mAckNo = mXmitNext;
		mpTcpStack->SendReset(mConnKey.mRemoteAddress, mConnKey.mLocalAddress, mConnKey.mRemotePort, mConnKey.mLocalPort, dummyHdr);
	}

	// Delete us.
	ClearEvents();
	mpTcpStack->DeleteConnection(mConnKey);
	mpTcpStack = nullptr;

	Release();
}

uint32 ATNetTcpConnection::Read(void *buf, uint32 len) {
	uint32 tc = mRecvRing.GetLevel();

	if (tc > len)
		tc = len;

	if (tc) {
		mRecvRing.Read(0, buf, tc);
		mRecvRing.Ack(tc);
	}

	return tc;
}

uint32 ATNetTcpConnection::Write(const void *buf, uint32 len) {
	VDASSERT(mbLocalOpen);

	if (len) {
		uint32 space = mXmitRing.GetSpace();
		if (len > space) {
			len = space;
			if (!len)
				return 0;
		}

		VDVERIFY(mXmitRing.Write(buf, len));

		// If this means we can transmit now, enable the transmit timer.
		TryTransmitMore(false);
	}

	return len;
}

void ATNetTcpConnection::Close() {
	if (mbLocalOpen) {
		mbLocalOpen = false;

		mpSocketHandler = nullptr;

		if (mXmitRing.GetSpace()) {
			mXmitRing.Write("", 1);
			mbFinQueued = true;
		}

		if (mConnState == kATNetTcpConnectionState_ESTABLISHED)
			mConnState = kATNetTcpConnectionState_FIN_WAIT_1;
		else if (mConnState == kATNetTcpConnectionState_CLOSE_WAIT)
			mConnState = kATNetTcpConnectionState_CLOSED;

		TryTransmitMore(false);
	}
}

ATNetTcpConnection::TransmitStatus ATNetTcpConnection::GetTransmitStatus() const {
	// We can transmit if:
	// - there is space in the window
	// - there is data waiting to send
	//
	// If a SYN is queued, we can only send the SYN. We hold the rest of
	// the data until the SYN is ack'd, after which it won't be queued.
	//
	// Note that it is discouraged but legal to shrink the receive window,
	// so we must handle the case where the window limit is rewound behind
	// where we have already sent.

	uint32 sendLimit = mXmitRing.GetBaseSeq() + (mbSynQueued ? 1 : mXmitRing.GetLevel());

	if (sendLimit == mXmitNext) {
		// We don't have any data to send.
		return kTransmitStatus_No;
	}

	// Compute the available window, and block send if the window is closed.
	// Note that the window may be moved backwards, making this unsigned negative.
	const uint32 availableWindow = mXmitWindowLimit - mXmitNext;

	if (availableWindow - 1 >= 0x20000U) {
		// Zero or negative window
		return kTransmitStatus_DeferredZeroWindow;
	}

	// Apply Silly Window Syndrome avoidance. We should only send the remaining
	// data if we can fill either a full segment or at least half the window.
	if (!mbSynQueued && availableWindow < mXmitWindowThreshold)
		return kTransmitStatus_Deferred;

	// We have data to send, and we should send it.
	return kTransmitStatus_Yes;
}

void ATNetTcpConnection::ClearEvents() {
	if (!mpTcpStack)
		return;

	IATEthernetClock *clk = mpTcpStack->GetClock();

	if (mEventClose) {
		clk->RemoveClockEvent(mEventClose);
		mEventClose = 0;
	}

	if (mEventTransmit) {
		clk->RemoveClockEvent(mEventTransmit);
		mEventTransmit = 0;
	}

	if (mEventRetransmit) {
		clk->RemoveClockEvent(mEventRetransmit);
		mEventRetransmit = 0;
	}

	if (mEventWindowProbe) {
		clk->RemoveClockEvent(mEventWindowProbe);
		mEventWindowProbe = 0;
	}

	if (mEventZeroWindowProbe) {
		clk->RemoveClockEvent(mEventZeroWindowProbe);
		mEventZeroWindowProbe = 0;
	}
}

void ATNetTcpConnection::ProcessSynOptions(const ATEthernetPacket& packet, const ATTcpHeaderInfo& tcpHdr, const uint8 *data) {
	const uint8 *src = data + 20;
	const uint8 *end = data + tcpHdr.mDataOffset;

	while(src != end) {
		const uint8 kind = src[0];
		if (!kind)
			return;

		// check for NOP
		if (kind == 1) {
			++src;
			continue;
		}

		// parse length (includes kind and length)
		const uint8 len = src[1];
		if (len < 2 || (uint32)(end - src) > len)
			break;

		switch(kind) {
			case 2:		// Maximum Segment Size
				{
					uint32 mss = VDReadUnalignedBEU16(src + 2);

					// Check if the MSS is below 256 bytes; if so, just ignore it and hope
					// for the best. Windows requires at least 536; Linux requires at least
					// 88-512 depending on the version. We should support at least 512 bytes
					// since a lot of BSD-derived OSes use it as a default, and at least 536
					// since that falls out of the IPv4 MTU requirement of 576 octets.
					//
					// Note that the MSS value does NOT include IPv4 or TCP header options,
					// but we don't send any with packets that have data payloads.

					if (mss >= 256) {
						if (mXmitMaxSegment > mss)
							mXmitMaxSegment = mss;
					}
				}
				break;
		}

		src += len;
	}
}
