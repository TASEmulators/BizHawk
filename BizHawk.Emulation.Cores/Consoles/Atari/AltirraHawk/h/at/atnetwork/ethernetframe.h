#ifndef f_AT_ATNETWORK_ETHERNETFRAME_H
#define f_AT_ATNETWORK_ETHERNETFRAME_H

#include <at/atnetwork/ethernet.h>

enum ATEthernetFrameType {
	kATEthernetFrameType_IP = 0x0800,
	kATEthernetFrameType_ARP = 0x0806,
	kATEthernetFrameType_TransparentEthernet = 0x6558		// [NVGRE]
};

struct ATEthernetArpFrameInfo {
	enum Op {
		kOpRequest = 1,
		kOpReply = 2
	};

	Op	mOp;
	ATEthernetAddr	mSenderHardwareAddr;
	ATEthernetAddr	mTargetHardwareAddr;
	uint32			mSenderProtocolAddr;
	uint32			mTargetProtocolAddr;
};

typedef uint8 ATEthernetArpFrameBuffer[30];

struct ATIPv4HeaderInfo {
	uint32	mSrcAddr;
	uint32	mDstAddr;
	uint8	mProtocol;
	uint8	mFlags;
	uint8	mTTL;
	uint8	mTOS;
	uint16	mId;
	uint16	mFragmentOffset;
	uint32	mDataOffset;
	uint32	mDataLength;
};

bool ATEthernetDecodeArpPacket(ATEthernetArpFrameInfo& dstInfo, const uint8 *data, uint32 len);
uint32 ATEthernetEncodeArpPacket(uint8 *data, uint32 len, const ATEthernetArpFrameInfo& srcInfo);

uint16 ATIPComputeChecksum(uint64 initialSum, const uint8 *data, uint32 dwords);

bool ATIPv4DecodeHeader(ATIPv4HeaderInfo& dstInfo, const uint8 *data, uint32 len);
uint32 ATIPv4EncodeHeader(uint8 *data, uint32 len, const ATIPv4HeaderInfo& srcInfo);

bool ATIPv6DecodeHeader(const uint8 *data, uint32 len);

inline ATEthernetAddr ATEthernetGetBroadcastAddr() {
	return ATEthernetAddr { { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };
}

inline bool ATEthernetIsBroadcastAddr(const ATEthernetAddr& addr) {
	return (addr.mAddr[0] & addr.mAddr[1] & addr.mAddr[2] & addr.mAddr[3] & addr.mAddr[4] & addr.mAddr[5]) == 0xFF;
}

#endif
