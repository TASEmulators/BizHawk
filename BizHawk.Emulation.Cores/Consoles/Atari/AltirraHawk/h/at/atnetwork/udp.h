#ifndef f_AT_ATNETWORK_UDP_H
#define f_AT_ATNETWORK_UDP_H

struct ATIPv4HeaderInfo;

struct ATUdpHeaderInfo {
	uint16	mSrcPort;
	uint16	mDstPort;
	uint32	mDataOffset;
	uint32	mDataLength;
};

bool ATUdpDecodeHeader(ATUdpHeaderInfo& hdrInfo, const ATIPv4HeaderInfo& iphdr, const uint8 *data, uint32 len);

#endif
