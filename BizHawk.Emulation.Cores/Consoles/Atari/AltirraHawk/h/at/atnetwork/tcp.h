#ifndef f_AT_ATNETWORK_TCP_H
#define f_AT_ATNETWORK_TCP_H

struct ATIPv4HeaderInfo;

struct ATTcpHeaderInfo {
	uint16	mSrcPort;
	uint16	mDstPort;
	uint32	mSequenceNo;
	uint32	mAckNo;
	bool	mbURG;
	bool	mbACK;
	bool	mbPSH;
	bool	mbRST;
	bool	mbSYN;
	bool	mbFIN;
	uint16	mWindow;
	uint16	mUrgentPtr;
	uint32	mDataOffset;
	uint32	mDataLength;
};

bool ATTcpDecodeHeader(ATTcpHeaderInfo& hdrInfo, const ATIPv4HeaderInfo& iphdr, const uint8 *data, uint32 len);

#endif
