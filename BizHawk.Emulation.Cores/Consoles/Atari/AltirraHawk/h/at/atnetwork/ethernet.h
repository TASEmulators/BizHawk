#ifndef f_AT_ATNETWORK_ETHERNET_H
#define f_AT_ATNETWORK_ETHERNET_H

struct ATEthernetAddr {
	uint8 mAddr[6];
};

struct ATEthernetPacket {
	uint32 mClockIndex;
	uint32 mTimestamp;
	ATEthernetAddr mSrcAddr;
	ATEthernetAddr mDstAddr;
	const uint8 *mpData;
	uint32 mLength;
};

class IATEthernetClockEventSink {
public:
	// Called when a clock event fires. userid and eventid are the values that were supplied
	// to and returned from AddClockEvent(), respectively. When this call occurs, the eventid
	// is already invalidated and must not be passed to RemoveClockEvent().
	virtual void OnClockEvent(uint32 eventid, uint32 userid) = 0;
};

class IATEthernetClock {
public:
	virtual uint32 GetTimestamp(sint32 offsetMS) = 0;
	virtual sint32 SubtractTimestamps(uint32 t1, uint32 t2) = 0;
	virtual uint32 AddClockEvent(uint32 timestamp, IATEthernetClockEventSink *sink, uint32 userid) = 0;
	virtual void RemoveClockEvent(uint32 eventid) = 0;
};

enum ATEthernetFrameDecodedType {
	kATEthernetFrameDecodedType_None,
	kATEthernetFrameDecodedType_ARP,
	kATEthernetFrameDecodedType_IPv4,
	kATEthernetFrameDecodedType_IPv6
};

class IATEthernetEndpoint {
public:
	virtual void ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo) = 0;
};

class IATEthernetSegment {
public:
	virtual uint32 AddEndpoint(IATEthernetEndpoint *endpoint) = 0;
	virtual void RemoveEndpoint(uint32 endpointId) = 0;

	virtual IATEthernetClock *GetClock(uint32 clockId) const = 0;
	virtual uint32 AddClock(IATEthernetClock *clock) = 0;
	virtual void RemoveClock(uint32 clockId) = 0;

	virtual void TransmitFrame(uint32 source, const ATEthernetPacket& packet) = 0;
};

#endif
