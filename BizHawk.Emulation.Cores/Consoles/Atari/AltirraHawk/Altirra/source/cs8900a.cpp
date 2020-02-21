//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2013 Avery Lee
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
#include <vd2/system/VDString.h>
#include <at/atnetwork/ethernet.h>
#include <at/atnetwork/ethernetframe.h>
#include <at/atnetwork/tcp.h>
#include "cs8900a.h"
#include "debuggerlog.h"

ATDebuggerLogChannel g_ATLCCS8900AW(true, false, "CS8900AW", "CS8900A writes");
ATDebuggerLogChannel g_ATLCCS8900AR(false, false, "CS8900AR", "CS8900A reads");
ATDebuggerLogChannel g_ATLCCS8900AN(true, false, "CS8900AN", "CS8900A network transmit/receive");
ATDebuggerLogChannel g_ATLCCS8900AD(false, false, "CS8900AD", "CS8900A network data (requires CS8900AN channel)");

ATCS8900AEmulator::ATCS8900AEmulator()
	: mpEthernetSegment(NULL)
	, mEthernetEndpointId(0)
{
	ColdReset();
}

ATCS8900AEmulator::~ATCS8900AEmulator() {
}

void ATCS8900AEmulator::Init(IATEthernetSegment *ethSeg, uint32 ethClockIndex) {
	mpEthernetSegment = ethSeg;
	mEthernetEndpointId = mpEthernetSegment->AddEndpoint(this);
	mEthernetClockIndex = ethClockIndex;
}

void ATCS8900AEmulator::Shutdown() {
	if (mpEthernetSegment) {
		if (mEthernetEndpointId) {
			mpEthernetSegment->RemoveEndpoint(mEthernetEndpointId);
			mEthernetEndpointId = 0;
		}

		mpEthernetSegment = NULL;
	}
}

void ATCS8900AEmulator::ColdReset() {
	mReceiveReadPtr = 0x0404;
	mReceiveReadLen = 0;
	mTransmitWritePtr = 0x0A00;
	mTransmitWriteLevel = 0;
	mTransmitWriteLen = 0;
	mPacketPtr = 0;
	mPacketPtrAutoInc = 0;

	mReceiveState = kReceiveState_WaitForFrame;
	mTransmitState = kTransmitState_WaitForCmd;

	mTransmitBufferReadPtr = 0;
	mTransmitBufferWritePtr = 0;
	mTransmitBufferLevel = 0;

	mReceiveBufferReadPtr = 0;
	mReceiveBufferWritePtr = 0;
	mReceiveBufferLevel = 0;

	memset(mPacketPage, 0, sizeof mPacketPage);

	// product ID code (base + 0000h)
	mPacketPage[0x000] = 0x0E;
	mPacketPage[0x001] = 0x63;
	mPacketPage[0x002] = 0x00;
	mPacketPage[0x003] = 0x0A;	// rev F

	// I/O base address (base + 0020h)
	mPacketPage[0x020] = 0x00;
	mPacketPage[0x021] = 0x03;

	// interrupt number (base + 0022h)
	mPacketPage[0x022] = 0x04;		// no interrupt
	mPacketPage[0x023] = 0x00;

	// DMA channel (base + 0024h)
	mPacketPage[0x024] = 0x03;		// no DMA
	mPacketPage[0x025] = 0x00;

	// DMA start of frame (base + 0026h)
	mPacketPage[0x026] = 0x00;
	mPacketPage[0x027] = 0x00;
	
	// DMA frame count (base + 0028h)
	mPacketPage[0x028] = 0x00;
	mPacketPage[0x029] = 0x00;

	// RxDMA byte count (base + 002Ah)
	mPacketPage[0x02A] = 0x00;
	mPacketPage[0x02B] = 0x00;

	// Memory base address (base + 002Ch)
	mPacketPage[0x02C] = 0x00;
	mPacketPage[0x02D] = 0x00;
	mPacketPage[0x02E] = 0x00;
	mPacketPage[0x02F] = 0x00;

	// Boot PROM base address (base + 0030h)
	mPacketPage[0x030] = 0x00;
	mPacketPage[0x031] = 0x00;
	mPacketPage[0x032] = 0x00;
	mPacketPage[0x033] = 0x00;

	// Boot PROM address mask (base + 0034h)
	mPacketPage[0x034] = 0x00;
	mPacketPage[0x035] = 0x00;
	mPacketPage[0x036] = 0x00;
	mPacketPage[0x037] = 0x00;

	// Receiver configuration (RW, base + 0102h)
	mPacketPage[0x102] = 0x03;
	mPacketPage[0x103] = 0x00;

	// Receiver control (RW, base + 0104h)
	mPacketPage[0x104] = 0x05;
	mPacketPage[0x105] = 0x00;

	// Transmit configuration (RW, base + 0106h)
	mPacketPage[0x106] = 0x07;
	mPacketPage[0x107] = 0x00;

	// Transmit command status (RO, base + 0108h)
	mPacketPage[0x108] = 0x09;
	mPacketPage[0x109] = 0x00;

	// Buffer configuration (RW, base + 010Ah)
	mPacketPage[0x10A] = 0x0B;
	mPacketPage[0x10B] = 0x00;

	// Line control (RW, base + 0112h)
	mPacketPage[0x112] = 0x23;
	mPacketPage[0x113] = 0x00;

	// Self control (RW, base + 0114h)
	mPacketPage[0x112] = 0x15;
	mPacketPage[0x113] = 0x00;

	// Bus control (RW, base + 0116h)
	mPacketPage[0x116] = 0x16;
	mPacketPage[0x117] = 0x00;

	// Test control (RW, base + 0118h)
	mPacketPage[0x118] = 0x19;
	mPacketPage[0x119] = 0x00;

	// Interrupt status queue (RO, base + 0120h)
	mPacketPage[0x120] = 0x00;
	mPacketPage[0x121] = 0x00;

	// Transmitter event (RO, base + 0128h)
	mPacketPage[0x128] = 0x08;
	mPacketPage[0x129] = 0x00;

	// Transmitter event (RO, base + 012Ch)
	mPacketPage[0x12C] = 0x0C;
	mPacketPage[0x12D] = 0x00;

	// Receiver miss counter (RO, base + 0130h)
	mPacketPage[0x130] = 0x20;
	mPacketPage[0x131] = 0x00;

	// Transmit collision counter (RO, base + 0132h)
	mPacketPage[0x132] = 0x22;
	mPacketPage[0x133] = 0x00;

	// Line status (RO, base + 0134h)
	mPacketPage[0x134] = 0x24;
	mPacketPage[0x135] = 0x00;

	// Self status (RO, base + 0136h)
	mPacketPage[0x136] = 0x16;
	mPacketPage[0x137] = 0x00;

	// Bus status (RO, base + 0138h)
	mPacketPage[0x138] = 0x18;
	mPacketPage[0x139] = 0x00;

	// AUI time domain reflectometer (RO, base + 013Ch)
	mPacketPage[0x13C] = 0x1C;
	mPacketPage[0x13D] = 0x00;

	// Transmit command request (WO, base + 0144h)
	mPacketPage[0x144] = 0x09;
	mPacketPage[0x145] = 0x00;

	// Transmit length (WO, base + 0146h)
	mPacketPage[0x146] = 0x00;
	mPacketPage[0x147] = 0x00;

	// Logical address filter (hash table) (RW, base + 0150h)
	mPacketPage[0x150] = 0x00;
	mPacketPage[0x151] = 0x00;
	mPacketPage[0x152] = 0x00;
	mPacketPage[0x153] = 0x00;
	mPacketPage[0x154] = 0x00;
	mPacketPage[0x155] = 0x00;
	mPacketPage[0x156] = 0x00;
	mPacketPage[0x157] = 0x00;

	// Individual address (IEEE address) (RW, base + 0158h)
	mPacketPage[0x158] = 0x00;
	mPacketPage[0x159] = 0x00;
	mPacketPage[0x15A] = 0x00;
	mPacketPage[0x15B] = 0x00;
	mPacketPage[0x15C] = 0x00;
	mPacketPage[0x15D] = 0x00;
}

void ATCS8900AEmulator::WarmReset() {
}

uint8 ATCS8900AEmulator::DebugReadByte(uint8 address) {
	switch(address & 0x0f) {
		case 0x00:	// receive/transmit data port 0, low byte
		case 0x02:	// receive/transmit data port 1, low byte
			return mReceiveReadLen > 0 ? mPacketPage[mReceiveReadPtr] : 0;

		case 0x01:	// receive/transmit data port 0, high byte
		case 0x03:	// receive/transmit data port 1, high byte
			return mReceiveReadLen > 0 ? mPacketPage[mReceiveReadPtr + 1] : 0;

		case 0x04:	// transmit command, low byte (write only)
		case 0x05:	// transmit command, high byte (write only)
		case 0x06:	// transmit length, low byte (write only)
		case 0x07:	// transmit length, high byte (write only)
			return 0x00;

		case 0x08:	// interrupt status queue, low byte
		case 0x09:	// interrupt status queue, high byte
			return 0x00;

		case 0x0A:	// PacketPage pointer, low byte
			return (uint8)mPacketPtr;

		case 0x0B:	// PacketPage pointer, high byte
			return (uint8)(((mPacketPtr >> 8) & 0x8F) + 0x60);

		case 0x0C:	// PacketPage data port 0, low byte
		case 0x0E:	// PacketPage data port 1, low byte
			return mPacketPage[mPacketPtr & 0xfff];

		case 0x0D:	// PacketPage data port 0, high byte
		case 0x0F:	// PacketPage data port 1, high byte
			return mPacketPage[(mPacketPtr + 1) & 0xfff];

		default:
			return 0x00;
	}
}

uint8 ATCS8900AEmulator::ReadByte(uint8 address) {
	uint8 value = 0;

	switch(address & 0x0f) {
		case 0x00:	// receive/transmit data port 0, low byte
		case 0x02:	// receive/transmit data port 1, low byte
			if (mReceiveReadLen > 0) {
				value = mPacketPage[mReceiveReadPtr];
				g_ATLCCS8900AR("Read receive port PP[$%03X] = $%02X\n", mReceiveReadPtr, value);

				// RxStatus and RxLength need to be read MSB first, per 8-bit mode doc
				if (mReceiveReadPtr < 0x0404) {
					mReceiveReadPtr += 2;
					if (mReceiveReadPtr >= 0x0A00)
						mReceiveReadPtr = 0x0400;

					--mReceiveReadLen;

					if (!mReceiveReadLen) {
						ReceiveNextFrame();
					}
				}
			}
			break;

		case 0x01:	// receive/transmit data port 0, high byte
		case 0x03:	// receive/transmit data port 1, high byte
			if (mReceiveReadLen > 0) {
				value = mPacketPage[mReceiveReadPtr + 1];
				g_ATLCCS8900AR("Read receive port PP[$%03X] = $%02X\n", mReceiveReadPtr+1, value);

				// frame data needs to be read LSB first, per 8-bit mode doc
				if (mReceiveReadPtr >= 0x0404) {
					mReceiveReadPtr += 2;
					if (mReceiveReadPtr >= 0x0A00)
						mReceiveReadPtr = 0x0400;

					--mReceiveReadLen;

					if (!mReceiveReadLen) {
						ReceiveNextFrame();
					}
				}
			}
			break;

		case 0x04:	// transmit command, low byte (write only)
		case 0x05:	// transmit command, high byte (write only)
		case 0x06:	// transmit length, low byte (write only)
		case 0x07:	// transmit length, high byte (write only)
			break;

		case 0x08:	// interrupt status queue, low byte
			value = ReadPacketPage(0x120);
			break;

		case 0x09:	// interrupt status queue, high byte
			value = ReadPacketPage(0x121);
			break;

		case 0x0A:	// PacketPage pointer, low byte
			return (uint8)mPacketPtr;

		case 0x0B:	// PacketPage pointer, high byte
			return (uint8)((mPacketPtr >> 8) + 0x60 + mPacketPtrAutoInc);

		case 0x0C:	// PacketPage data port 0, low byte
		case 0x0E:	// PacketPage data port 1, low byte
			value = ReadPacketPage(mPacketPtr & 0xfff);
			g_ATLCCS8900AR("Read PP[$%03X] = $%02X\n", mPacketPtr & 0xfff, value);
			if (mPacketPtrAutoInc)
				mPacketPtr += 2;
			break;

		case 0x0D:	// PacketPage data port 0, high byte
		case 0x0F:	// PacketPage data port 1, high byte
			value = ReadPacketPage((mPacketPtr + 1) & 0xfff);
			g_ATLCCS8900AR("Read PP[$%03X] = $%02X\n", (mPacketPtr + 1) & 0xfff, value);
			if (mPacketPtrAutoInc)
				mPacketPtr += 2;
			break;
	}

	return value;
}

void ATCS8900AEmulator::WriteByte(uint8 address, uint8 value) {
	switch(address & 0x0f) {
		case 0x00:	// receive/transmit data port 0, low byte
		case 0x01:	// receive/transmit data port 0, high byte
		case 0x02:	// receive/transmit data port 1, low byte
		case 0x03:	// receive/transmit data port 1, high byte
			if (mTransmitWriteLevel < mTransmitWriteLen) {
				WritePacketPage(mTransmitWritePtr, value);

				if (++mTransmitWritePtr >= 0x1000)
					mTransmitWritePtr = 0xA00;
			}
			break;

		case 0x08:	// interrupt status queue, low byte
		case 0x09:	// interrupt status queue, high byte
			break;

		case 0x04:	// transmit command, low byte (write only)
			g_ATLCCS8900AW("Transmit command lo = $%02X\n", value);
			WritePacketPage(0x0144, value);
			break;
		case 0x05:	// transmit command, high byte (write only)
			g_ATLCCS8900AW("Transmit command hi = $%02X\n", value);
			WritePacketPage(0x0145, value);
			break;
		case 0x06:	// transmit length, low byte (write only)
			g_ATLCCS8900AW("Transmit length lo = $%02X\n", value);
			WritePacketPage(0x0146, value);
			break;
		case 0x07:	// transmit length, high byte (write only)
			g_ATLCCS8900AW("Transmit length hi = $%02X\n", value);
			WritePacketPage(0x0147, value);
			break;

		case 0x0A:	// PacketPage pointer, low byte
			mPacketPtr = (mPacketPtr & 0xf00) + value;
			break;

		case 0x0B:	// PacketPage pointer, high byte
			mPacketPtr = (mPacketPtr & 0x0ff) + ((value & 0xf) << 8);
			mPacketPtrAutoInc = (value & 0x80);
			break;

		case 0x0C:	// PacketPage data port 0, low byte
		case 0x0E:	// PacketPage data port 1, low byte
			g_ATLCCS8900AW("Write PP[$%03X] = $%02X\n", mPacketPtr & 0xfff, value);
			WritePacketPage(mPacketPtr, value);
			if (mPacketPtrAutoInc)
				++mPacketPtr;
			break;

		case 0x0D:	// PacketPage data port 0, high byte
		case 0x0F:	// PacketPage data port 1, high byte
			g_ATLCCS8900AW("Write PP[$%03X] = $%02X\n", (mPacketPtr + 1) & 0xfff, value);
			WritePacketPage(mPacketPtr + 1, value);
			if (mPacketPtrAutoInc)
				++mPacketPtr;
			break;
	}
}

namespace {
	void DescribeFrame(VDStringA& s, ATEthernetFrameDecodedType decType, const void *decInfo, const void *packetData) {
		if (decType == kATEthernetFrameDecodedType_ARP) {
			const ATEthernetArpFrameInfo& arpInfo = *(const ATEthernetArpFrameInfo *)decInfo;

			s += " | arp";

			switch(arpInfo.mOp) {
				case ATEthernetArpFrameInfo::kOpRequest:
					{
						uint8 target[4];
						VDWriteUnalignedU32(target, arpInfo.mTargetProtocolAddr);

						s.append_sprintf(" where is %u.%u.%u.%u"
							, target[0]
							, target[1]
							, target[2]
							, target[3]);
					}
					break;

				case ATEthernetArpFrameInfo::kOpReply:
					{
						uint8 sender[4];
						VDWriteUnalignedU32(sender, arpInfo.mSenderProtocolAddr);

						s.append_sprintf(" %u.%u.%u.%u is at %02X:%02X:%02X:%02X:%02X:%02X"
							, sender[0]
							, sender[1]
							, sender[2]
							, sender[3]
							, arpInfo.mSenderHardwareAddr.mAddr[0]
							, arpInfo.mSenderHardwareAddr.mAddr[1]
							, arpInfo.mSenderHardwareAddr.mAddr[2]
							, arpInfo.mSenderHardwareAddr.mAddr[3]
							, arpInfo.mSenderHardwareAddr.mAddr[4]
							, arpInfo.mSenderHardwareAddr.mAddr[5]
						);
					}
					break;
			}
		} else if (decType == kATEthernetFrameDecodedType_IPv4) {
			const ATIPv4HeaderInfo& iphdr = *(const ATIPv4HeaderInfo *)decInfo;
			const uint8 *ippkt = (const uint8 *)packetData + 2;

			const uint8 *protodata = ippkt + iphdr.mDataOffset;
			const uint8 protocol = iphdr.mProtocol;
			const bool hasPorts = iphdr.mDataLength > 4 && (protocol == 17 || protocol == 6);

			s.append_sprintf(" | ipv4 %u.%u.%u.%u"
				, ippkt[12]
				, ippkt[13]
				, ippkt[14]
				, ippkt[15]);

			if (hasPorts)
				s.append_sprintf(":%u", VDReadUnalignedBEU16(&protodata[0]));

			s.append_sprintf(" > %u.%u.%u.%u"
				, ippkt[16]
				, ippkt[17]
				, ippkt[18]
				, ippkt[19]);

			if (hasPorts)
				s.append_sprintf(":%u", VDReadUnalignedBEU16(&protodata[2]));

			// check if it is UDP, TCP, or ICMP
			switch(ippkt[9]) {
				case 1:		// ICMP
					s.append_sprintf(" icmp");
					break;

				case 2:		// IGMP
					s.append_sprintf(" igmp");
					break;

				case 6:		// TCP
					s.append_sprintf(" tcp");

					{
						ATTcpHeaderInfo tcpHdr;

						if (ATTcpDecodeHeader(tcpHdr, iphdr, protodata, iphdr.mDataLength)) {

							s += ' ';

							if (tcpHdr.mbPSH || tcpHdr.mbSYN || tcpHdr.mbFIN || tcpHdr.mbRST) {
								if (tcpHdr.mbSYN) s += 'S';
								if (tcpHdr.mbPSH) s += 'P';
								if (tcpHdr.mbFIN) s += 'F';
								if (tcpHdr.mbRST) s += 'R';
							} else {
								s += '.';
							}

							s.append_sprintf(" %u:%u(%u)", tcpHdr.mSequenceNo, tcpHdr.mSequenceNo+tcpHdr.mDataLength, tcpHdr.mDataLength);

							if (tcpHdr.mbACK) s.append_sprintf(" ack %u", tcpHdr.mAckNo);

							s.append_sprintf(" win %u", tcpHdr.mWindow);
						} else {
							s += "?!";
						}
					}
					break;

				case 17:	// UDP
					s.append_sprintf(" udp len %u", VDReadUnalignedBEU16(&protodata[4]));
					break;
			}
		} else if (decType == kATEthernetFrameDecodedType_IPv6) {
			s.append(" | ipv6");
		}
	}

	void DumpData(const uint8 *data, uint32 len) {
		uint32 offset = 0;
		VDStringA s;

		while(len) {
			uint32 tc = len > 16 ? 16 : len;
			len -= tc;

			s.sprintf("%04X:", offset);

			for(uint32 i=0; i<tc; ++i)
				s.append_sprintf("%c%02X", i==8 ? '-' : ' ', data[i]);

			for(uint32 i=tc; i<16; ++i) {
				s += (i == 8 ? '-' : ' ');
				s += ' ';
				s += ' ';
			}

			s += " |";

			for(uint32 i=0; i<tc; ++i) {
				uint8 c = data[i];

				if (c >= 0x20 && c < 0x7F)
					s += (char)c;
				else
					s += '.';
			}

			for(uint32 i=tc; i<16; ++i)
				s += ' ';

			s += "|\n";
			g_ATLCCS8900AD <<= s.c_str();

			offset += tc;
			data += tc;
		}
	}
}

void ATCS8900AEmulator::ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo) {
	if (g_ATLCCS8900AN.IsEnabled()) {
		VDStringA s;

		s.sprintf("Receiving %u byte frame: %02X:%02X:%02X:%02X:%02X:%02X > %02X:%02X:%02X:%02X:%02X:%02X"
			, packet.mLength
			, packet.mSrcAddr.mAddr[0]
			, packet.mSrcAddr.mAddr[1]
			, packet.mSrcAddr.mAddr[2]
			, packet.mSrcAddr.mAddr[3]
			, packet.mSrcAddr.mAddr[4]
			, packet.mSrcAddr.mAddr[5]
			, packet.mDstAddr.mAddr[0]
			, packet.mDstAddr.mAddr[1]
			, packet.mDstAddr.mAddr[2]
			, packet.mDstAddr.mAddr[3]
			, packet.mDstAddr.mAddr[4]
			, packet.mDstAddr.mAddr[5]);

		DescribeFrame(s, decType, decInfo, packet.mpData);

		s += '\n';

		g_ATLCCS8900AN <<= s.c_str();

		if (g_ATLCCS8900AD.IsEnabled())
			DumpData(packet.mpData, packet.mLength);
	}

	// discard packet if not for us
	const bool isBroadcast = ATEthernetIsBroadcastAddr(packet.mDstAddr);
	if (!isBroadcast && memcmp(packet.mDstAddr.mAddr, &mPacketPage[0x158], 6))
		return;

	// check if we have room in the receive buffer
	uint32 reqLen = packet.mLength + 16;
	if (mReceiveBufferLevel + reqLen > sizeof mReceiveBuffer) {
		g_ATLCCS8900AN("Dropping packet due to lack of space!\n");
		return;
	}

	// copy packet to receive buffer
	uint8 rxheader[4];
	uint32 pktlen = packet.mLength;
	uint32 padlen = 0;

	// minimum packet size is 64 bytes, from MAC addresses to CRC
	if (pktlen < 48) {
		padlen = 48 - pktlen;
		pktlen = 48;
	}

	VDWriteUnalignedLEU16(&rxheader[0], isBroadcast ? 0x0904 : 0x0504);
	VDWriteUnalignedLEU16(&rxheader[2], pktlen + 12);

	WriteToReceiveBuffer(rxheader, 4);
	WriteToReceiveBuffer(packet.mSrcAddr.mAddr, 6);
	WriteToReceiveBuffer(packet.mDstAddr.mAddr, 6);
	WriteToReceiveBuffer(packet.mpData, packet.mLength);

	if (padlen)
		WriteToReceiveBuffer(NULL, padlen);

	if (mReceiveState == kReceiveState_WaitForFrame)
		ReceiveNextFrame();
}

void ATCS8900AEmulator::ReceiveNextFrame() {
	if (mReceiveBufferLevel) {
		mReceiveState = kReceiveState_ReadingFrame;

		// read RxStatus and RxLength
		ReadFromReceiveBuffer(&mPacketPage[0x0400], 4);

		// read the rest of the packet
		const uint16 rxlen = VDReadUnalignedLEU16(&mPacketPage[0x0402]);

		VDASSERT(rxlen <= 0x5FC);

		ReadFromReceiveBuffer(&mPacketPage[0x0404], rxlen);

		// copy RxStatus to RxEvent
		mPacketPage[0x0124] = mPacketPage[0x0400];
		mPacketPage[0x0125] = mPacketPage[0x0401];

		// reset host read pointers
		mReceiveReadPtr = 0x0400;
		mReceiveReadLen = (rxlen + 5) >> 1;
	} else {
		mReceiveState = kReceiveState_WaitForFrame;
		
		// reset RxEvent
		mPacketPage[0x0124] &= 0x3f;
		mPacketPage[0x0125] = 0;
	}
}

void ATCS8900AEmulator::ReadFromReceiveBuffer(void *data, uint32 len) {
	VDASSERT(len <= mReceiveBufferLevel);

	mReceiveBufferLevel -= len;

	const uint32 len1 = (sizeof mReceiveBuffer) - mReceiveBufferReadPtr;
	if (len1 < len) {
		memcpy(data, mReceiveBuffer + mReceiveBufferReadPtr, len1);
		
		mReceiveBufferReadPtr = 0;
		data = (char *)data + len1;
		len -= len1;
	}

	memcpy(data, mReceiveBuffer + mReceiveBufferReadPtr, len);
	mReceiveBufferReadPtr += len;
}

void ATCS8900AEmulator::WriteToReceiveBuffer(const void *data, uint32 len) {
	VDASSERT(mReceiveBufferLevel + len <= sizeof mReceiveBuffer);

	mReceiveBufferLevel += len;

	const uint32 len1 = (sizeof mReceiveBuffer) - mReceiveBufferWritePtr;
	if (len1 < len) {
		if (data) {
			memcpy(mReceiveBuffer + mReceiveBufferWritePtr, data, len1);
			data = (const char *)data + len1;
		} else
			memset(mReceiveBuffer + mReceiveBufferWritePtr, 0, len1);
		
		mReceiveBufferWritePtr = 0;
		len -= len1;
	}

	if (data)
		memcpy(mReceiveBuffer + mReceiveBufferWritePtr, data, len);
	else
		memset(mReceiveBuffer + mReceiveBufferWritePtr, 0, len);

	mReceiveBufferWritePtr += len;
}

uint8 ATCS8900AEmulator::ReadPacketPage(uint32 address) {
	address &= 0xfff;

	uint8 value = mPacketPage[address];

	// Reading the low order byte clears temporal bits. This means that event registers
	// must be read high byte first.
	switch(address) {
		case 0x0124:	// RxEvent
			mPacketPage[0x124] &= 0x3f;
			mPacketPage[0x125] = 0;

			// Reading RxEvent while a frame is in progress causes an implicit skip (5.2.5/3).
			ReceiveNextFrame();
			break;

		case 0x0128:	// TxEvent
		case 0x012C:	// BufEvent
			mPacketPage[address + 1] &= 0x3f;
			mPacketPage[address + 1] = 0;
			break;
	}

	return value;
}

void ATCS8900AEmulator::WritePacketPage(uint32 address, uint8 value) {
	address &= 0xfff;

	if (address >= 0x020 && address <= 0x025)
		mPacketPage[address] = value;
	else if (address >= 0x02c && address <= 0x037)
		mPacketPage[address] = value;
	else if (address >= 0x040 && address <= 0x043)
		mPacketPage[address] = value;
	else if (address >= 0x100 && address <= 0x11f) {
		// TxCMD (base + 0108h) is special - RO
		if (address == 0x0108 || address == 0x0109)
			return;

		// RxCFG bit 6, SelfCTL bit 6, BusCTL bit 6, and BufCFG bit 6
		// are act-once bits.
		switch(address) {
			case 0x0102:	// RxCFG
				if (value & 0x40) {		// Skip_1
					value -= 0x40;

					ReceiveNextFrame();
				}
				break;
			case 0x010A:	// BufCFG
				if (value & 0x40) {
					value -= 0x40;
				}
				break;
			case 0x0114:	// SelfCTL
				if (value & 0x40) {
					value -= 0x40;
				}
				break;
			case 0x0116:	// BusCTL
				if (value & 0x40) {
					// ResetRxDMA
					value -= 0x40;
				}
				break;
		}

		if (address & 1)
			mPacketPage[address] = value;
		else
			mPacketPage[address] = (mPacketPage[address] & 0x3f) + (value & 0xc0);
	} else if (address >= 0x144 && address <= 0x147) {
		mPacketPage[address] = value;

		switch(address) {
			case 0x0145:	// TxCMD high
				mTransmitState = kTransmitState_WaitForLength;
				break;

			case 0x0147:	// TxLEN high
				mTransmitState = kTransmitState_WaitForData;
				mTransmitWritePtr = 0x0A00;
				mTransmitWriteLen = VDReadUnalignedLEU16(&mPacketPage[0x0146]);
				mTransmitWriteLevel = 0;
				UpdateTransmitBufferStatus();
			break;
		}
	} else if (address >= 0x150 && address <= 0x15d) {
		mPacketPage[address] = value;
	} else if (address >= 0xa00) {
		mPacketPage[address] = value;

		if (mTransmitWriteLevel < mTransmitWriteLen) {
			++mTransmitWriteLevel;

			if (mTransmitWriteLevel >= mTransmitWriteLen)
				TransmitFrame();
		}
	}
}

void ATCS8900AEmulator::UpdateTransmitBufferStatus() {
	// set or clear Rdy4TxNOW bit
	if (mTransmitState == kTransmitState_WaitForData && mTransmitWriteLen + mTransmitBufferLevel > sizeof mTransmitBuffer)
		mPacketPage[0x0139] &= 0xfe;
	else
		mPacketPage[0x0139] |= 0x01;
}

void ATCS8900AEmulator::TransmitFrame() {

	if (g_ATLCCS8900AN.IsEnabled()) {
		VDStringA s;

		s.sprintf("Sending %u byte frame: %02X:%02X:%02X:%02X:%02X:%02X > %02X:%02X:%02X:%02X:%02X:%02X"
			, mTransmitWriteLen
			, mPacketPage[0xA06]
			, mPacketPage[0xA07]
			, mPacketPage[0xA08]
			, mPacketPage[0xA09]
			, mPacketPage[0xA0A]
			, mPacketPage[0xA0B]
			, mPacketPage[0xA00]
			, mPacketPage[0xA01]
			, mPacketPage[0xA02]
			, mPacketPage[0xA03]
			, mPacketPage[0xA04]
			, mPacketPage[0xA05]
			);

		union {
			ATEthernetArpFrameInfo arpInfo;
			ATIPv4HeaderInfo ipv4Info;
		} dec;

		ATEthernetFrameDecodedType decType = kATEthernetFrameDecodedType_None;
		const void *decInfo = NULL;

		if (mTransmitWriteLen >= 14) {
			const uint8 *data = &mPacketPage[0xA0C];

			switch(VDReadUnalignedBEU16(data)) {
				case kATEthernetFrameType_ARP:
					if (ATEthernetDecodeArpPacket(dec.arpInfo, data + 2, mTransmitWriteLen - 14)) {
						decInfo = &dec.arpInfo;
						decType = kATEthernetFrameDecodedType_ARP;
					}
					break;

				case kATEthernetFrameType_IP:
					if (ATIPv4DecodeHeader(dec.ipv4Info, data + 2, mTransmitWriteLen - 14)) {
						decInfo = &dec.ipv4Info;
						decType = kATEthernetFrameDecodedType_IPv4;
					} else if (ATIPv6DecodeHeader(data + 2, mTransmitWriteLen - 14)) {
						decType = kATEthernetFrameDecodedType_IPv6;
					}
					break;
			}
		}

		DescribeFrame(s, decType, decInfo, &mPacketPage[0x0A0C]);

		s += '\n';

		g_ATLCCS8900AN <<= s.c_str();

		if (g_ATLCCS8900AD.IsEnabled() && mTransmitWriteLen > 12)
			DumpData(&mPacketPage[0xA0C], mTransmitWriteLen - 12);
	}

	// send packet across the Ethernet bus
	if (mTransmitWriteLen >= 12) {
		ATEthernetPacket pkt;
		pkt.mTimestamp = 100;
		pkt.mClockIndex = mEthernetClockIndex;
		memcpy(pkt.mSrcAddr.mAddr, &mPacketPage[0xA06], 6);
		memcpy(pkt.mDstAddr.mAddr, &mPacketPage[0xA00], 6);
		pkt.mpData = &mPacketPage[0xA0C];
		pkt.mLength = mTransmitWriteLen - 12;
		mpEthernetSegment->TransmitFrame(mEthernetEndpointId, pkt);
	}

	mTransmitState = kTransmitState_WaitForCmd;
	mTransmitWriteLen = 0;
	mTransmitWriteLevel = 0;

	UpdateTransmitBufferStatus();
}
