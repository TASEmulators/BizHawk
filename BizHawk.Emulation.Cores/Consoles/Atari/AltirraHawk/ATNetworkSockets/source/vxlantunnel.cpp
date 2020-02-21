#include <stdafx.h>
#include <winsock2.h>
#include <windows.h>
#include <tchar.h>
#include <iphlpapi.h>
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/refcount.h>
#include <vd2/system/thunk.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/w32assist.h>
#include <at/atnetwork/ethernetframe.h>
#include <at/atnetworksockets/internal/vxlantunnel.h>

ATNetSockVxlanTunnel::ATNetSockVxlanTunnel() {
}

ATNetSockVxlanTunnel::~ATNetSockVxlanTunnel() {
	Shutdown();
}

bool ATNetSockVxlanTunnel::Init(uint32 tunnelAddr, uint16 tunnelSrcPort, uint16 tunnelTgtPort, IATEthernetSegment *ethSeg, uint32 ethClockIndex) {
	mTunnelAddr = tunnelAddr;
	mTunnelSrcPort = tunnelSrcPort;
	mTunnelTgtPort = tunnelTgtPort;

	mpWndThunk = VDCreateFunctionThunkFromMethod(this, &ATNetSockVxlanTunnel::WndProc, true);
	if (!mpWndThunk) {
		Shutdown();
		return false;
	}

	TCHAR className[64];
	_sntprintf(className, vdcountof(className), _T("ATNetSockVxlanTunnel_%p"), this);

	WNDCLASS wc = {0};
	wc.lpfnWndProc = VDGetThunkFunction<WNDPROC>(mpWndThunk);
	wc.hInstance = VDGetLocalModuleHandleW32();
	wc.lpszClassName = className;

	mWndClass = RegisterClass(&wc);
	if (!mWndClass) {
		Shutdown();
		return false;
	}

	mhwnd = CreateWindowEx(0, MAKEINTATOM(mWndClass), _T(""), WS_POPUP, 0, 0, 0, 0, NULL, NULL, wc.hInstance, NULL);
	if (!mhwnd) {
		Shutdown();
		return false;
	}

	mTunnelSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);

	if (mTunnelSocket != INVALID_SOCKET) {
		WSAAsyncSelect(mTunnelSocket, mhwnd, MYWM_SOCKET, FD_READ);

		sockaddr_in sin = {};
		sin.sin_family = AF_INET;
		sin.sin_port = htons(mTunnelSrcPort);
		sin.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
		if (bind(mTunnelSocket, (const sockaddr *)&sin, sizeof sin) != 0) {
			closesocket(mTunnelSocket);
			mTunnelSocket = INVALID_SOCKET;
		}
	}

	mpEthSegment = ethSeg;
	mEthSource = mpEthSegment->AddEndpoint(this);

	mPacketBuffer.resize(4096);

	return true;
}

void ATNetSockVxlanTunnel::Shutdown() {
	if (mEthSource) {
		mpEthSegment->RemoveEndpoint(mEthSource);
		mEthSource = 0;
	}

	if (mTunnelSocket != INVALID_SOCKET) {
		::closesocket(mTunnelSocket);
		mTunnelSocket = INVALID_SOCKET;
	}

	if (mhwnd) {
		DestroyWindow(mhwnd);
		mhwnd = NULL;
	}

	if (mWndClass) {
		UnregisterClass(MAKEINTATOM(mWndClass), VDGetLocalModuleHandleW32());
		mWndClass = NULL;
	}

	if (mpWndThunk) {
		VDDestroyFunctionThunk(mpWndThunk);
		mpWndThunk = nullptr;
	}
}

void ATNetSockVxlanTunnel::ReceiveFrame(const ATEthernetPacket& packet, ATEthernetFrameDecodedType decType, const void *decInfo) {
	uint32 len = 8 + 14 + packet.mLength;

	if (mPacketBuffer.size() < len)
		mPacketBuffer.resize(len);

	// set VXLAN header to VLAN absent
	memset(mPacketBuffer.data(), 0, 8);
	mPacketBuffer[0] = 0x08;

	// init Ethernet header
	memcpy(&mPacketBuffer[8], &packet.mDstAddr, 6);
	memcpy(&mPacketBuffer[14], &packet.mSrcAddr, 6);
	memcpy(&mPacketBuffer[20], packet.mpData, packet.mLength);

	// send VXLAN packet
	sockaddr_in sin = {};
	sin.sin_family = AF_INET;
	sin.sin_port = htons(mTunnelTgtPort);
	sin.sin_addr.S_un.S_addr = mTunnelAddr;
	sendto(mTunnelSocket, (const char *)mPacketBuffer.data(), len, 0, (const sockaddr *)&sin, sizeof sin);
}

LRESULT ATNetSockVxlanTunnel::WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	if (msg == MYWM_SOCKET) {
		const SOCKET sock = (SOCKET)wParam;
		const UINT event = LOWORD(lParam);

		VDASSERT(sock == mTunnelSocket);

		if (event == FD_READ) {
			sockaddr fromAddr = {};
			int fromAddrLen = sizeof(fromAddr);

			int actual = recvfrom(mTunnelSocket, (char *)mPacketBuffer.data(), (int)mPacketBuffer.size(), 0, &fromAddr, &fromAddrLen);

			if (actual && actual != SOCKET_ERROR)
				OnReadPacket((uint32)actual);
		}

		return 0;
	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

void ATNetSockVxlanTunnel::OnReadPacket(uint32 len) {
	// Okay, next check that we have a valid VXLAN header and ethernet packet after it.
	if (len < 8 + 14)
		return;

	const uint8 *vxlanhdr = mPacketBuffer.data();

	// must be VLAN 0
	if ((vxlanhdr[0] & 0x08) && (VDReadUnalignedBEU32(&vxlanhdr[4]) & 0xFFFFFF00))
		return;

	const uint8 *payload = vxlanhdr + 8;
	const uint32 payloadLen = len - 8;

	if (payloadLen < 14)
		return;

	if (payloadLen > 1502)
		return;

	// forward packet to ethernet segment
	ATEthernetPacket packet = {};
	packet.mClockIndex = mEthClockIndex;
	packet.mTimestamp = mpEthSegment->GetClock(mEthClockIndex)->GetTimestamp(100);
	memcpy(&packet.mSrcAddr, payload + 6, 6);
	memcpy(&packet.mDstAddr, payload, 6);
	packet.mpData = payload + 12;
	packet.mLength = payloadLen - 12;
	mpEthSegment->TransmitFrame(mEthSource, packet);
}

///////////////////////////////////////////////////////////////////////////

void ATCreateNetSockVxlanTunnel(uint32 tunnelAddr, uint16 tunnelSrcPort, uint16 tunnelTgtPort, IATEthernetSegment *ethSeg, uint32 ethClockIndex, IATNetSockVxlanTunnel **pp) {
	ATNetSockVxlanTunnel *p = new ATNetSockVxlanTunnel;

	if (!p->Init(tunnelAddr, tunnelSrcPort, tunnelTgtPort, ethSeg, ethClockIndex)) {
		delete p;
		throw MyMemoryError();
	}

	p->AddRef();
	*pp = p;
}
