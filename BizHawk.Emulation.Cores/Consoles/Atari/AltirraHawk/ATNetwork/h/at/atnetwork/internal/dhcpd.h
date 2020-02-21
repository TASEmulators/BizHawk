#ifndef f_AT_ATNETWORK_INTERNAL_DHCPD_H
#define f_AT_ATNETWORK_INTERNAL_DHCPD_H

#include <at/atnetwork/socket.h>
#include <at/atnetwork/ethernet.h>

class IATNetUdpStack;

class ATNetDhcpDaemon : public IATUdpSocketListener {
public:
	ATNetDhcpDaemon();

	void Init(IATNetUdpStack *ip);
	void Shutdown();

	void Reset();

	void OnUdpDatagram(const ATEthernetAddr& srcHwAddr, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen);

protected:
	IATNetUdpStack *mpUdpStack;
	uint32 mNextLeaseIdx;

	struct Lease {
		bool mbValid;
		uint32 mXid;
		ATEthernetAddr mAddr;
	};

	Lease mLeases[100];
};

#endif	// f_AT_ATNETWORK_INTERNAL_DHCPD_H
