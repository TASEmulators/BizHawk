#ifndef f_AT_ATNETWORK_ETHERNETBUS_H
#define f_AT_ATNETWORK_ETHERNETBUS_H

#include <vd2/system/vdstl.h>
#include <at/atnetwork/ethernet.h>

class ATEthernetBus : public IATEthernetSegment, protected IATEthernetClockEventSink {
	ATEthernetBus(const ATEthernetBus&);
	ATEthernetBus& operator=(const ATEthernetBus&);
public:
	ATEthernetBus();
	~ATEthernetBus();

	virtual uint32 AddEndpoint(IATEthernetEndpoint *endpoint);
	virtual void RemoveEndpoint(uint32 endpointId);

	virtual IATEthernetClock *GetClock(uint32 clockId) const;
	virtual uint32 AddClock(IATEthernetClock *clock);
	virtual void RemoveClock(uint32 clockId);

	virtual void ClearPendingFrames();
	virtual void TransmitFrame(uint32 source, const ATEthernetPacket& packet);

protected:
	virtual void OnClockEvent(uint32 eventid, uint32 userid);

protected:
	uint32 mNextEndpoint;
	uint32 mNextPacketId;

	struct Endpoint {
		IATEthernetEndpoint *mpEndpoint;
		uint32 mId;
	};

	typedef vdfastvector<Endpoint> Endpoints;
	Endpoints mEndpoints;

	typedef vdfastvector<IATEthernetClock *> Clocks;
	Clocks mClocks;

	struct QueuedPacket {
		uint32 mSourceId;
		uint32 mClockEventId;
		uint32 mNextPacketId;
		ATEthernetPacket mPacket;
	};

	typedef vdhashmap<uint32, QueuedPacket *> Packets;
	Packets mPackets;

	typedef vdhashmap<uint32, QueuedPacket *> PacketsByTimestamp;
	PacketsByTimestamp mPacketsByTimestamp;
};

#endif
