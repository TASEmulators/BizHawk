#ifndef f_ATNETWORK_IPSTACK_H
#define f_ATNETWORK_IPSTACK_H

#include <vd2/system/vdstl_hashmap.h>
#include <at/atnetwork/ethernet.h>
#include <at/atnetwork/socket.h>

struct ATIPv4HeaderInfo;

class ATNetIpStack final : public IATNetIpStack {
	ATNetIpStack(const ATNetIpStack&) = delete;
	ATNetIpStack& operator=(const ATNetIpStack&) = delete;
public:
	ATNetIpStack();
	~ATNetIpStack();

	IATEthernetClock *GetClock() const;
	virtual uint32 GetIpAddress() const { return mIpAddress; }
	virtual uint32 GetIpNetMask() const { return mIpNetMask; }
	virtual bool IsLocalOrBroadcastAddress(uint32 addr) const;

	void Init(const ATEthernetAddr& hwaddr, uint32 ipaddr, uint32 netmask, IATEthernetSegment *segment, uint32 clockId, uint32 endpointId);
	void Shutdown();

	void InitHeader(ATIPv4HeaderInfo& info);

	void ClearArpCache();
	void AddArpEntry(uint32 ipaddr, const ATEthernetAddr& hwaddr, bool pendingOnly);

	void SendFrame(const ATEthernetAddr& dstAddr, const void *data, uint32 len);
	void SendFrame(uint32 dstIpAddr, const void *data, uint32 len);

private:
	void DeletePendingArpEntry(uint32 ipAddr);
	void FlushPendingArpEntry(uint32 ipAddr, const ATEthernetAddr& hwaddr);

	ATEthernetAddr mHwAddress;
	IATEthernetSegment *mpEthSegment;
	uint32	mEthClockId;
	uint32	mEthEndpointId;
	uint32	mIpAddress;
	uint32	mIpNetMask;
	uint16	mIpCounter;

	typedef vdhashmap<uint32, ATEthernetAddr> ArpCache;
	ArpCache mArpCache;

	class PendingArpEntry;

	typedef vdhashmap<uint32, PendingArpEntry *> PendingArpRequests;
	PendingArpRequests mPendingArpRequests;
};

#endif	// f_ATNETWORK_IPSTACK_H
