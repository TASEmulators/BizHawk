#ifndef f_ATNETWORK_UDPSTACK_H
#define f_ATNETWORK_UDPSTACK_H

#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_hashmap.h>
#include <at/atnetwork/ethernet.h>
#include <at/atnetwork/socket.h>

struct ATIPv4HeaderInfo;
struct ATUdpHeaderInfo;
class ATNetIpStack;

struct ATNetUdpListeningSocket {
	IATUdpSocketListener *mpHandler;
};


class ATNetUdpStack final : public IATNetUdpStack {
public:
	ATNetUdpStack();

	IATEthernetClock *GetClock() const { return mpClock; }
	uint32 GetIpAddress() const;
	uint32 GetIpNetMask() const;

	void Init(ATNetIpStack *ipStack);
	void Shutdown();

	void SetBridgeListener(IATUdpSocketListener *p);

public:
	virtual IATNetIpStack *GetIpStack() const { return mpIpStack; }
	virtual bool Bind(uint16 port, IATUdpSocketListener *listener);
	virtual uint16 Bind(IATUdpSocketListener *listener);
	virtual void Unbind(uint16 port, IATUdpSocketListener *listener);

	virtual void SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen);
	virtual void SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const ATEthernetAddr& dstHwAddr, const void *data, uint32 dataLen);

public:
	void OnPacket(const ATEthernetPacket& packet, const ATIPv4HeaderInfo& iphdr, const uint8 *data, const uint32 len);

protected:
	void SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const ATEthernetAddr *dstHwAddr, const void *data, uint32 dataLen);

	ATNetIpStack *mpIpStack;
	IATEthernetClock *mpClock;
	uint16 mNextPort;

	IATUdpSocketListener *mpBridgeListener;

	typedef vdhashmap<uint32, ATNetUdpListeningSocket> ListeningSockets;
	ListeningSockets mListeningSockets;
};

#endif	// F_ATNETWORK_UDPSTACK_H
