#include <stdafx.h>
#include <vd2/system/binary.h>
#include <at/atnetwork/ethernetframe.h>

bool ATEthernetDecodeArpPacket(ATEthernetArpFrameInfo& dstInfo, const uint8 *data, uint32 len) {
	// length must be at least 28 for IPv4 and Ethernet
	if (len < 28)
		return false;

	// hardware type must be 1 for Ethernet
	if (VDReadUnalignedBEU16(data + 0) != 1)
		return false;

	// protocol must be 0x0800 for IPv4
	if (VDReadUnalignedBEU16(data + 2) != 0x0800)
		return false;

	// hardware address length must be 6 and protocol length must be 4.
	if (data[4] != 6 || data[5] != 4)
		return false;

	// operation must be 1 (request) or 2 (reply)
	const uint16 op = VDReadUnalignedBEU16(data + 6);

	if (op < 1 || op > 2)
		return false;

	// copy over all info
	dstInfo.mOp = (ATEthernetArpFrameInfo::Op)op;
	memcpy(dstInfo.mSenderHardwareAddr.mAddr, data + 8, 6);
	dstInfo.mSenderProtocolAddr = VDReadUnalignedU32(data + 14);
	memcpy(dstInfo.mTargetHardwareAddr.mAddr, data + 18, 6);
	dstInfo.mTargetProtocolAddr = VDReadUnalignedU32(data + 24);

	return true;
}

uint32 ATEthernetEncodeArpPacket(uint8 *dst, uint32 len, const ATEthernetArpFrameInfo& srcInfo) {
	if (len < 30)
		return 0;

	static const uint8 kHeader[]={
		0x08, 0x06,		// EtherType = ARP
		0x00, 0x01,		// hardware type (Ethernet)
		0x08, 0x00,		// protocol (IPv4)
		0x06, 0x04,		// hardware length 6, protocol length 4
	};

	memcpy(dst, kHeader, 8);
	dst[8] = 0;
	dst[9] = (uint8)srcInfo.mOp;
	
	memcpy(dst + 10, srcInfo.mSenderHardwareAddr.mAddr, 6);
	VDWriteUnalignedU32(dst + 16, srcInfo.mSenderProtocolAddr);
	memcpy(dst + 20, srcInfo.mTargetHardwareAddr.mAddr, 6);
	VDWriteUnalignedU32(dst + 26, srcInfo.mTargetProtocolAddr);

	return 30;
}

uint16 ATIPComputeChecksum(uint64 initialSum, const uint8 *data, uint32 dwords) {
	uint64 sum64 = initialSum;

	for(uint32 i=0; i<dwords; ++i)
		sum64 += VDReadUnalignedU32(data + 4*i);

	// fold sum -- note that this can have a carry out
	sum64 = (uint32)sum64 + (sum64 >> 32);

	// fold sum again -- this one can't (max input is 0x1FFFFFFFE)
	uint32 sum32 = (uint32)sum64 + (uint32)(sum64 >> 32);

	// fold to 16-bit
	sum32 = (sum32 & 0xffff) + (sum32 >> 16);

	return (uint16)(~((sum32 & 0xffff) + (sum32 >> 16)));
}

bool ATIPv4DecodeHeader(ATIPv4HeaderInfo& dstInfo, const uint8 *data, uint32 len) {
	// minimum IP header length is 20 bytes (5 dwords)
	if (len < 20)
		return false;

	// check that it's IPv4
	if ((data[0] & 0xf0) != 0x40)
		return false;

	// check that header length is at least 5 dwords
	const uint8 ihl = data[0] & 0x0f;
	if (ihl < 5)
		return false;

	// check that packet is long enough to include header
	if (len < (uint32)(4*ihl))
		return false;

	// check that packet is long enough to include header and data
	const uint16 totalLen = VDReadUnalignedBEU16(data + 2);

	if (len < totalLen)
		return false;

	// check that total length in header is at least as long as the header
	if (totalLen < 4*ihl)
		return false;

	// check the header checksum (note that we must NOT swizzle the checksum!)
	if (ATIPComputeChecksum(0, data, ihl) != 0)
		return false;

	// looks good... decode fields.
	dstInfo.mTOS = data[2];
	dstInfo.mId = VDReadUnalignedBEU16(data + 4);
	dstInfo.mFlags = data[6] >> 5;
	dstInfo.mFragmentOffset = VDReadUnalignedBEU16(data + 6) & 0x1FFF;
	dstInfo.mTTL = data[8];
	dstInfo.mProtocol = data[9];
	dstInfo.mSrcAddr = VDReadUnalignedU32(data + 12);
	dstInfo.mDstAddr = VDReadUnalignedU32(data + 16);
	dstInfo.mDataOffset = 4*ihl;
	dstInfo.mDataLength = totalLen - 4*ihl;

	// TTL = 0 is bogus (RFC1122 3.2.1.7)
	if (dstInfo.mTTL == 0)
		return false;

	return true;
}

uint32 ATIPv4EncodeHeader(uint8 *data, uint32 len, const ATIPv4HeaderInfo& srcInfo) {
	if (len < 22)
		return false;

	data[0] = 0x08;
	data[1] = 0x00;
	data += 2;

	data[0] = 0x45;				// IPv4, header len = 5 dwords
	data[1] = srcInfo.mTOS;
	VDWriteUnalignedBEU16(data + 2, srcInfo.mDataLength + 20);
	VDWriteUnalignedBEU16(data + 4, srcInfo.mId);
	VDWriteUnalignedBEU16(data + 6, (srcInfo.mFlags << 13) + srcInfo.mFragmentOffset);
	data[8] = srcInfo.mTTL;
	data[9] = srcInfo.mProtocol;
	data[10] = 0;
	data[11] = 0;
	VDWriteUnalignedU32(data + 12, srcInfo.mSrcAddr);
	VDWriteUnalignedU32(data + 16, srcInfo.mDstAddr);

	// write checksum
	VDWriteUnalignedU16(data + 10, ATIPComputeChecksum(0, data, 5));

	return 22;
}

bool ATIPv6DecodeHeader(const uint8 *data, uint32 len) {
	// minimum IP header length is 20 bytes (5 dwords)
	if (len < 20)
		return false;

	// check that it's IPv6
	if ((data[0] & 0xf0) != 0x60)
		return false;

	return true;
}
