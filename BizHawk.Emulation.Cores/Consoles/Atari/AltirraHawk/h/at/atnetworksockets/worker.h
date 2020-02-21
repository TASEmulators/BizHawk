#ifndef f_AT_ATNETWORKSOCKETS_WORKER_H
#define f_AT_ATNETWORKSOCKETS_WORKER_H

class IATNetUdpStack;
class IATNetTcpStack;

class IATNetSockWorker : public IVDRefCount {
public:
	virtual IATSocketListener *AsSocketListener() = 0;
	virtual IATUdpSocketListener *AsUdpListener() = 0;

	virtual void ResetAllConnections() = 0;

	virtual bool GetHostAddressForLocalAddress(bool tcp, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, uint32& hostIp, uint16& hostPort) = 0;
};

void ATCreateNetSockWorker(IATNetUdpStack *udp, IATNetTcpStack *tcp, bool externalAccess, uint32 forwardingAddr, uint16 forwardingPort, IATNetSockWorker **pp);

#endif
