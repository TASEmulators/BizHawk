#include <stdafx.h>
#include <vd2/system/binary.h>
#include <at/atnetwork/udp.h>
#include <at/atnetwork/ethernetframe.h>

bool ATUdpDecodeHeader(ATUdpHeaderInfo& udpHdr, const ATIPv4HeaderInfo& iphdr, const uint8 *data, uint32 len) {
	// drop short packets
	if (len < 8)
		return false;

	// extract and check UDP length
	uint32 udpLen = VDReadUnalignedBEU16(data + 4);

	if (len < udpLen)
		return false;

	// check if a checksum was included... it can be omitted for UDP.
	if (VDReadUnalignedU16(data + 6)) {
		// compute checksum
		uint64 sum64 = 0;

		sum64 += iphdr.mSrcAddr;
		sum64 += iphdr.mDstAddr;
		sum64 += VDToBE32(udpLen + 0x110000);

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
	}

	// decode fields
	udpHdr.mSrcPort = VDReadUnalignedBEU16(data + 0);
	udpHdr.mDstPort = VDReadUnalignedBEU16(data + 2);
	udpHdr.mDataOffset = 8;
	udpHdr.mDataLength = udpLen - 8;

	return true;
}
