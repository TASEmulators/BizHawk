#ifndef f_ATNETWORK_SOCKET_H
#define f_ATNETWORK_SOCKET_H

#include <vd2/system/refcount.h>

struct ATEthernetAddr;

class IATSocket : public IVDRefCount {
public:
	virtual uint32 Read(void *buf, uint32 len) = 0;
	virtual uint32 Write(const void *buf, uint32 len) = 0;
	virtual void Close() = 0;
	virtual void Shutdown() = 0;
};

class IATSocketHandler : public IVDRefCount {
public:
	virtual void OnSocketOpen() = 0;
	virtual void OnSocketReadReady(uint32 len) = 0;
	virtual void OnSocketWriteReady(uint32 len) = 0;
	virtual void OnSocketClose() = 0;
	virtual void OnSocketError() = 0;
};

class IATSocketListener {
public:
	virtual bool OnSocketIncomingConnection(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, IATSocket *socket, IATSocketHandler **handler) = 0;
};

class IATUdpSocketListener {
public:
	virtual void OnUdpDatagram(const ATEthernetAddr& srcHwAddr, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen) = 0;
};

class IATNetIpStack {
public:
	virtual uint32 GetIpAddress() const = 0;
	virtual uint32 GetIpNetMask() const = 0;

	virtual bool IsLocalOrBroadcastAddress(uint32 ip) const = 0;
};

class IATNetUdpStack {
public:
	virtual IATNetIpStack *GetIpStack() const = 0;

	virtual bool Bind(uint16 port, IATUdpSocketListener *listener) = 0;
	virtual uint16 Bind(IATUdpSocketListener *listener) = 0;
	virtual void Unbind(uint16 port, IATUdpSocketListener *listener) = 0;

	virtual void SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen) = 0;
	virtual void SendDatagram(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const ATEthernetAddr& dstHwAddr, const void *data, uint32 dataLen) = 0;
};

class IATNetTcpStack {
public:
	virtual bool Connect(uint32 dstIpAddr, uint16 dstPort, IATSocketHandler *handler, IATSocket **newSocket) = 0;

};

#endif	// f_ATNETWORK_SOCKET_H
