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

#ifndef f_AT_CS8900A_H
#define f_AT_CS8900A_H

#include <at/atnetwork/ethernet.h>

class ATCS8900AEmulator : public IATEthernetEndpoint {
	ATCS8900AEmulator(const ATCS8900AEmulator&);
	ATCS8900AEmulator& operator=(const ATCS8900AEmulator&);
public:
	ATCS8900AEmulator();
	~ATCS8900AEmulator();

	void Init(IATEthernetSegment *ethSeg, uint32 ethClockIndex);
	void Shutdown();

	void ColdReset();
	void WarmReset();

	uint8 DebugReadByte(uint8 address);
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

public:
	virtual void ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo);

protected:
	void ReceiveNextFrame();
	void ReadFromReceiveBuffer(void *data, uint32 len);
	void WriteToReceiveBuffer(const void *data, uint32 len);

	uint8 ReadPacketPage(uint32 address);
	void WritePacketPage(uint32 address, uint8 value);

	void UpdateTransmitBufferStatus();
	void TransmitFrame();

	uint16	mReceiveReadPtr;
	uint16	mReceiveReadLen;
	uint16	mTransmitWritePtr;
	uint16	mTransmitWriteLevel;
	uint16	mTransmitWriteLen;
	uint16	mPacketPtr;
	uint8	mPacketPtrAutoInc;

	enum TransmitState {
		kTransmitState_WaitForCmd,
		kTransmitState_WaitForLength,
		kTransmitState_WaitForData,
		kTransmitState_Transmitting
	} mTransmitState;

	enum ReceiveState {
		kReceiveState_WaitForFrame,
		kReceiveState_ReadingFrame
	} mReceiveState;

	uint32	mTransmitBufferReadPtr;
	uint32	mTransmitBufferWritePtr;
	uint32	mTransmitBufferLevel;

	uint32	mReceiveBufferReadPtr;
	uint32	mReceiveBufferWritePtr;
	uint32	mReceiveBufferLevel;

	IATEthernetSegment *mpEthernetSegment;
	uint32	mEthernetEndpointId;
	uint32	mEthernetClockIndex;

	VDALIGN(2) uint8	mPacketPage[0x1000];

	uint8	mReceiveBuffer[0x1000];
	uint8	mTransmitBuffer[0x0800];
};

#endif
