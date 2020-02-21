#ifndef f_AT_ATNETWORKSOCKETS_INTERNAL_VXLANTUNNEL_H
#define f_AT_ATNETWORKSOCKETS_INTERNAL_VXLANTUNNEL_H

#include <vd2/system/vdstl.h>
#include <at/atnetwork/ethernet.h>
#include <at/atnetworksockets/vxlantunnel.h>

class ATNetSockVxlanTunnel final : public vdrefcounted<IATNetSockVxlanTunnel>, public IATEthernetEndpoint {
public:
	ATNetSockVxlanTunnel();
	~ATNetSockVxlanTunnel();

	bool Init(uint32 tunnelAddr, uint16 tunnelSrcPort, uint16 tunnelTgtPort, IATEthernetSegment *ethSeg, uint32 ethClockIndex);
	void Shutdown();

public:
	void ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo) override;

private:
	enum {
		MYWM_SOCKET = WM_USER
	};

	LRESULT WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
	void OnReadPacket(uint32 len);

	VDFunctionThunkInfo *mpWndThunk = nullptr;
	ATOM mWndClass = 0;
	HWND mhwnd = nullptr;

	SOCKET mTunnelSocket = INVALID_SOCKET;
	uint32 mTunnelAddr = 0;
	uint16 mTunnelSrcPort = 0;
	uint16 mTunnelTgtPort = 0;

	IATEthernetSegment *mpEthSegment = nullptr;
	uint32 mEthSource = 0;
	uint32 mEthClockIndex = 0;

	vdblock<uint8> mPacketBuffer;
};

#endif
