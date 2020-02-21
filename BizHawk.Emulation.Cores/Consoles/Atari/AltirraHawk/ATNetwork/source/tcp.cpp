#include <stdafx.h>
#include <vd2/system/binary.h>
#include <at/atnetwork/tcp.h>
#include <at/atnetwork/ethernetframe.h>

bool ATTcpDecodeHeader(ATTcpHeaderInfo& tcpHdr, const ATIPv4HeaderInfo& iphdr, const uint8 *data, uint32 len) {
	// drop short packets
	if (len < 20)
		return false;

	// check if data offset fits
	// minimum TCP header size is 5 dwords
	const uint8 dataOffset = data[12] >> 4;
	if (dataOffset < 5 || len < (uint32)(dataOffset * 4))
		return false;

	// compute checksum
	uint64 sum64 = 0;

	sum64 += iphdr.mSrcAddr;
	sum64 += iphdr.mDstAddr;
	sum64 += VDToBE32(len + 0x60000);

	uint32 len4 = len >> 2;
	const uint8 *chksrc = data;
	for(uint32 i=0; i<len4; ++i) {
		sum64 += VDReadUnalignedU32(chksrc);
		chksrc += 4;
	}

	// pick up stragglers
	if (len & 3) {
		if (len & 2) {
			sum64 += VDReadUnalignedU16(chksrc);
			chksrc += 2;
		}

		if (len & 1)
			sum64 += VDFromLE16(*chksrc);
	}

	// fold sum -- note that this can have a carry out
	sum64 = (uint32)sum64 + (sum64 >> 32);

	// fold sum again -- this one can't (max input is 0x1FFFFFFFE)
	uint32 sum32 = (uint32)sum64 + (uint32)(sum64 >> 32);

	// fold to 16-bit
	sum32 = (sum32 & 0xffff) + (sum32 >> 16);

	const uint16 sum16 = (uint16)(~((sum32 & 0xffff) + (sum32 >> 16)));

	// check header checksum; note that we must NOT swizzle this
	if (sum16 != 0)
		return false;

	// decode fields
	tcpHdr.mSrcPort = VDReadUnalignedBEU16(data + 0);
	tcpHdr.mDstPort = VDReadUnalignedBEU16(data + 2);
	tcpHdr.mSequenceNo = VDReadUnalignedBEU32(data + 4);
	tcpHdr.mAckNo = VDReadUnalignedBEU32(data + 8);
	tcpHdr.mbURG = (data[13] & 0x20) != 0;
	tcpHdr.mbACK = (data[13] & 0x10) != 0;
	tcpHdr.mbPSH = (data[13] & 0x08) != 0;
	tcpHdr.mbRST = (data[13] & 0x04) != 0;
	tcpHdr.mbSYN = (data[13] & 0x02) != 0;
	tcpHdr.mbFIN = (data[13] & 0x01) != 0;
	tcpHdr.mWindow = VDReadUnalignedBEU16(data + 14);
	tcpHdr.mUrgentPtr = VDReadUnalignedBEU16(data + 18);
	tcpHdr.mDataOffset = dataOffset*4;
	tcpHdr.mDataLength = len - dataOffset*4;

	return true;
}
