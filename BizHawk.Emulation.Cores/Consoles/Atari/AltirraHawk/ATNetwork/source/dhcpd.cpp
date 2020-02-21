#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/vdstl.h>
#include <at/atnetwork/ethernet.h>
#include <at/atnetwork/internal/dhcpd.h>

struct ATNetDhcpPacket {
	uint8	mOp;
	uint8	mHtype;
	uint8	mHlen;
	uint8	mHops;
	uint32	mXid;
	uint16	mSecs;
	uint16	mFlags;
	uint32	mCiaddr;
	uint32	mYiaddr;
	uint32	mSiaddr;
	uint32	mGiaddr;
	uint8	mChaddr[16];
	uint8	mSname[64];
	uint8	mFile[128];
	uint32	mOptionsKey;
};

ATNetDhcpDaemon::ATNetDhcpDaemon()
	: mpUdpStack(NULL)
	, mNextLeaseIdx(0)
{
}

void ATNetDhcpDaemon::Init(IATNetUdpStack *udp) {
	VDASSERTCT(sizeof(ATNetDhcpPacket) == 240);

	mpUdpStack = udp;
	mpUdpStack->Bind(67, this);

	Reset();
}

void ATNetDhcpDaemon::Shutdown() {
	if (mpUdpStack) {
		mpUdpStack->Unbind(67, this);
		mpUdpStack = NULL;
	}
}

void ATNetDhcpDaemon::Reset() {
	memset(mLeases, 0, sizeof mLeases);

	mNextLeaseIdx = 0;
}

void ATNetDhcpDaemon::OnUdpDatagram(const ATEthernetAddr& srcHwAddr, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen) {
	// minimum DHCP packet length is 240 octets
	if (dataLen < sizeof(ATNetDhcpPacket))
		return;

	// copy to aligned struct
	ATNetDhcpPacket pkt;
	memcpy(&pkt, data, sizeof pkt);

	// We only care about BOOTREQUEST (1).
	if (pkt.mOp != 1)
		return;

	enum : uint8 {
		kATNetDhcpMsgType_DHCPDISCOVER = 1,
		kATNetDhcpMsgType_DHCPOFFER = 2,
		kATNetDhcpMsgType_DHCPREQUEST = 3,
		kATNetDhcpMsgType_DHCPDECLINE = 4,
		kATNetDhcpMsgType_DHCPACK = 5,
		kATNetDhcpMsgType_DHCPNAK = 6,
		kATNetDhcpMsgType_DHCPRELEASE = 7,
		kATNetDhcpMsgType_DHCPINFORM = 8
	};

	// Hardware address type must be 10mb Ethernet (1).
	// Hardware address length must be 6.
	if (pkt.mHtype != 1 || pkt.mHlen != 6)
		return;

	// Check if options are valid.
	if (pkt.mOptionsKey != VDToBE32(0x63825363))
		return;

	// Parse options.
	uint32 reqIpAddr = 0;
	uint32 serverId = 0;
	uint32 leaseTime = 0;
	int messageType = -1;

	if (dataLen > sizeof(ATNetDhcpPacket)) {
		uint32 optionsLen = dataLen - sizeof(ATNetDhcpPacket);
		const uint8 *opdata = (const uint8 *)data + sizeof(ATNetDhcpPacket);

		for(;;) {
			if (!optionsLen--)
				return;

			const uint8 token = *opdata++;

			// check for pad token
			if (token == 0)
				continue;

			// check for end token
			if (token == 255)
				break;

			// read length
			if (!optionsLen--)
				return;

			const uint8 oplen = *opdata++;

			if (optionsLen < oplen)
				return;

			optionsLen -= oplen;

			// parse option
			if (token == 50) {
				if (oplen != 4)
					return;

				reqIpAddr = VDReadUnalignedU32(opdata);
			} else if (token == 51) {
				if (oplen != 4)
					return;

				leaseTime = VDReadUnalignedU32(opdata);
			} else if (token == 53) {
				if (oplen != 1)
					return;

				messageType = *opdata;
			} else if (token == 54) {
				if (oplen != 4)
					return;

				serverId = VDReadUnalignedU32(opdata);
			}

			opdata += oplen;
		}
	}

	// Option 53 (message type) is required by RFC 2131
	if (messageType < 0)
		return;

	// Form reply
	IATNetIpStack *const ipStack = mpUdpStack->GetIpStack();

	struct Reply {
		ATNetDhcpPacket mPkt;
		uint8 mOptBuf[128];
	} reply = {0};
	uint8 *optdst = reply.mOptBuf;

	reply.mPkt.mOp		= 2;		// BOOTREPLY
	reply.mPkt.mHtype	= 1;		// 10mb ethernet
	reply.mPkt.mHlen	= 6;		// 10mb ethernet
	reply.mPkt.mXid		= pkt.mXid;
	reply.mPkt.mFlags	= pkt.mFlags;
	reply.mPkt.mGiaddr	= pkt.mGiaddr;
	reply.mPkt.mCiaddr	= pkt.mCiaddr;
	reply.mPkt.mSiaddr	= ipStack->GetIpAddress();
	memcpy(reply.mPkt.mChaddr, pkt.mChaddr, sizeof reply.mPkt.mChaddr);
	reply.mPkt.mOptionsKey	= VDToBE32(0x63825363);

	// Check if we have a DHCPDISCOVER, DHCPREQUEST or a DHCPDECLINE/RELEASE message
	if (messageType == kATNetDhcpMsgType_DHCPREQUEST) {
		// check that the address is in the right subnet
		const uint32 netip = ipStack->GetIpAddress();
		const uint32 netmask = ipStack->GetIpNetMask();

		// IP64 passes ciaddr=0 and uses the the Requested IP Address option (50).
		const uint32 ipAddr = reqIpAddr ? reqIpAddr : pkt.mCiaddr;

		if ((ipAddr ^ netip) & netmask) {
			// uh, wrong subnet
			return;
		}

		// check that the address is within our lease range
		uint8 addrid = (uint8)VDFromBE32(ipAddr);

		if (addrid < 100 || addrid >= 100 + vdcountof(mLeases))
			return;

		// Check that the address isn't already assigned, or that it is assigned to
		// this client.
		//
		// We used to validate the Xid here, but don't anymore because IP64 and lwIP
		// both generate new Xids for the DHCPREQUEST message. This is supposed to
		// be the same as the DHCPOFFER message per RFC2131 4.4.1, but apparently
		// common DHCP servers don't validate this....
		//
		uint32 leaseIdx = addrid - 100;
		Lease& lease = mLeases[addrid - 100];

		if (lease.mbValid && memcmp(mLeases[leaseIdx].mAddr.mAddr, pkt.mChaddr, 6)) {
			// occupied -- send DHCPNAK
			reply.mPkt.mSiaddr = 0;

			// set up message type option (required by RFC2131)
			*optdst++ = 53;
			*optdst++ = 1;
			*optdst++ = kATNetDhcpMsgType_DHCPNAK;
		} else {
			// looks good... assign the address and set up a DHCPACK
			lease.mbValid = true;
			lease.mXid = pkt.mXid;
			lease.mAddr = srcHwAddr;

			reply.mPkt.mYiaddr = ipAddr;

			// set up message type option (required by RFC2131)
			*optdst++ = 53;
			*optdst++ = 1;
			*optdst++ = kATNetDhcpMsgType_DHCPACK;

			// set up server identifier option (required)
			*optdst++ = 54;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, 0x100 + leaseIdx);
			optdst += 4;

			// set up lease time option (required)
			*optdst++ = 51;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, 86400);
			optdst += 4;

			// set up subnet mask (optional)
			*optdst++ = 1;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpNetMask());
			optdst += 4;

			// set up router (optional)
			*optdst++ = 3;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpAddress());
			optdst += 4;

			// set up DNS option (optional)
			*optdst++ = 6;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpAddress());
			optdst += 4;

			// set up DNS option (optional)
			*optdst++ = 6;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpAddress());
			optdst += 4;
		}
	} else if (messageType == kATNetDhcpMsgType_DHCPDECLINE || messageType == kATNetDhcpMsgType_DHCPRELEASE) {
		// these messages require a server identifier -- so make sure it's
		// a valid one
		if (serverId >= 0x100 && serverId < 0x100 + vdcountof(mLeases)) {
			// release the lease
			mLeases[serverId - 0x100].mbValid = false;
		}

		// exit without replying
		return;
	} else if (messageType == kATNetDhcpMsgType_DHCPDISCOVER || messageType == kATNetDhcpMsgType_DHCPINFORM) {
		// find an unused address or one that matches the hardware address
		bool found = false;
		uint32 leaseIdx;

		for(uint32 i=0; i<vdcountof(mLeases); ++i) {
			if (!memcmp(mLeases[i].mAddr.mAddr, pkt.mChaddr, 6)) {
				found = true;
				leaseIdx = i;
				break;
			}
		}

		if (!found) {
			for(uint32 i=0; i<vdcountof(mLeases); ++i) {
				if (++mNextLeaseIdx >= vdcountof(mLeases))
					mNextLeaseIdx = 0;

				if (!mLeases[mNextLeaseIdx].mbValid) {
					found = true;
					leaseIdx = mNextLeaseIdx;
					break;
				}
			}
		}

		if (found) {
			// slot available -- allocate it
			mLeases[leaseIdx].mbValid = true;
			mLeases[leaseIdx].mXid = pkt.mXid;
			memcpy(mLeases[leaseIdx].mAddr.mAddr, pkt.mChaddr, 6);
			
			// send DHCPOFFER
			reply.mPkt.mYiaddr = VDToBE32(VDFromBE32(ipStack->GetIpAddress() & ipStack->GetIpNetMask()) + 100 + leaseIdx);

			// set up message type option (required by RFC2131)
			*optdst++ = 53;
			*optdst++ = 1;
			*optdst++ = kATNetDhcpMsgType_DHCPOFFER;

			// set up server identifier option (required)
			*optdst++ = 54;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, 0x100 + leaseIdx);
			optdst += 4;

			// set up lease time option (required)
			*optdst++ = 51;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, 86400);
			optdst += 4;
			
			// set up subnet mask (optional)
			*optdst++ = 1;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpNetMask());
			optdst += 4;

			// set up router (optional)
			*optdst++ = 3;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpAddress());
			optdst += 4;

			// set up DNS option (optional)
			*optdst++ = 6;
			*optdst++ = 4;
			VDWriteUnalignedU32(optdst, ipStack->GetIpAddress());
			optdst += 4;
		} else {
			// no addresses available -- send DHCPNAK
			reply.mPkt.mSiaddr = 0;

			// set up message type option (required by RFC2131)
			*optdst++ = 53;
			*optdst++ = 1;
			*optdst++ = kATNetDhcpMsgType_DHCPNAK;
		}
	}

	*optdst++ = 255;

	const uint32 replylen = (uint32)((optdst - reply.mOptBuf) + offsetof(Reply, mOptBuf));

	if (reply.mPkt.mGiaddr != 0) {
		// giaddr!=0 => send to the DHCP server port on the relay agent
		mpUdpStack->SendDatagram(mpUdpStack->GetIpStack()->GetIpAddress(), dstPort, reply.mPkt.mGiaddr, 67, &reply, replylen);
	} else if (reply.mPkt.mCiaddr != 0) {
		// giaddr=0, ciaddr!=0 => send to client address
		mpUdpStack->SendDatagram(mpUdpStack->GetIpStack()->GetIpAddress(), dstPort, reply.mPkt.mCiaddr, 68, &reply, replylen);
	} else if (reply.mPkt.mFlags & VDToBE16(0x8000)) {
		// giaddr,ciaddr=0 and broadcast is set => broadcast
		const ATEthernetAddr broadcastHwAddr = {
			(uint8)0xFF,
			(uint8)0xFF,
			(uint8)0xFF,
			(uint8)0xFF,
			(uint8)0xFF,
			(uint8)0xFF
		};

		mpUdpStack->SendDatagram(mpUdpStack->GetIpStack()->GetIpAddress(), dstPort, 0xFFFFFFFFU, 68, broadcastHwAddr, &reply, replylen);
	} else {
		// else => unicast to hardware address
		ATEthernetAddr chaddr;
		memcpy(chaddr.mAddr, reply.mPkt.mChaddr, 6);

		mpUdpStack->SendDatagram(mpUdpStack->GetIpStack()->GetIpAddress(), dstPort, reply.mPkt.mYiaddr, 68, chaddr, &reply, replylen);
	}
}
