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
#include <at/atnetwork/socket.h>
#include <at/atnetworksockets/internal/worker.h>

#pragma comment(lib, "ws2_32.lib")
#pragma comment(lib, "iphlpapi.lib")

ATNetSockBridgeHandler::ATNetSockBridgeHandler(ATNetSockWorker *parent, SOCKET s, IATSocket *s2, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort)
	: mSrcIpAddr(srcIpAddr)
	, mSrcPort(srcPort)
	, mDstIpAddr(dstIpAddr)
	, mDstPort(dstPort)
	, mpParent(parent)
	, mNativeSocket(s)
	, mpLocalSocket(s2)
	, mbLocalClosed(false)
	, mbNativeConnected(false)
	, mbNativeClosed(false)
	, mLocalReadAvail(0)
	, mLocalWriteAvail(0)
	, mRecvBase(0)
	, mRecvLimit(0)
	, mSendBase(0)
	, mSendLimit(0)
{
	if (!s2)
		mbNativeConnected = true;
}

void ATNetSockBridgeHandler::Shutdown() {
	AddRef();

	if (mNativeSocket != INVALID_SOCKET) {
		mpParent->DeleteConnection(mNativeSocket);
		closesocket(mNativeSocket);
		mNativeSocket = INVALID_SOCKET;
	}

	mbNativeConnected = false;

	if (mpLocalSocket) {
		mpLocalSocket->Shutdown();
		mpLocalSocket = NULL;
	}

	Release();
}

void ATNetSockBridgeHandler::SetLocalSocket(IATSocket *s2) {
	mpLocalSocket = s2;
}

void ATNetSockBridgeHandler::OnNativeSocketConnect() {
	mbNativeConnected = true;
	TryCopyToNative();
	TryCopyFromNative();
}

void ATNetSockBridgeHandler::OnNativeSocketReadReady() {
	TryCopyFromNative();
}

void ATNetSockBridgeHandler::OnNativeSocketWriteReady() {
	TryCopyToNative();
}

void ATNetSockBridgeHandler::OnNativeSocketClose() {
	AddRef();

	mbNativeClosed = true;

	TryCopyFromNative();

	if (mRecvLimit == mRecvBase)
		mpLocalSocket->Close();

	if (mbLocalClosed)
		Shutdown();

	Release();
}

void ATNetSockBridgeHandler::OnNativeSocketError() {
	Shutdown();
}

void ATNetSockBridgeHandler::OnSocketOpen() {
}

void ATNetSockBridgeHandler::OnSocketReadReady(uint32 len) {
	mLocalReadAvail = len;

	TryCopyToNative();
}

void ATNetSockBridgeHandler::OnSocketWriteReady(uint32 len) {
	mLocalWriteAvail = len;

	TryCopyFromNative();
}

void ATNetSockBridgeHandler::OnSocketClose() {
	mbLocalClosed = true;

	shutdown(mNativeSocket, SD_SEND);

	if (mbNativeClosed)
		Shutdown();
}

void ATNetSockBridgeHandler::OnSocketError() {
	Shutdown();
}

void ATNetSockBridgeHandler::TryCopyToNative() {
	if (!mbNativeConnected)
		return;

	for(;;) {
		if (mSendBase == mSendLimit) {
			if (mbLocalClosed)
				break;

			uint32 actual = mpLocalSocket->Read(mSendBuf, sizeof mSendBuf);

			if (!actual)
				break;

			mSendBase = 0;
			mSendLimit = actual;
		}

		int actual2 = ::send(mNativeSocket, mSendBuf + mSendBase, mSendLimit - mSendBase, 0);

		if (actual2 == 0)
			break;

		if (actual2 == SOCKET_ERROR)
			break;

		mSendBase += actual2;
	}
}

void ATNetSockBridgeHandler::TryCopyFromNative() {
	if (!mbNativeConnected || mbNativeClosed)
		return;

	for(;;) {
		if (mRecvBase == mRecvLimit) {
			int actual = ::recv(mNativeSocket, mRecvBuf, sizeof mRecvBuf, 0);

			if (actual == 0) {
				if (mbNativeClosed)
					mpLocalSocket->Close();

				break;
			}

			if (actual == SOCKET_ERROR)		// includes WSAEWOULDBLOCK, which means no data
				break;

			mRecvBase = 0;
			mRecvLimit = actual;
		}

		uint32 actual2 = mpLocalSocket->Write(mRecvBuf + mRecvBase, mRecvLimit - mRecvBase);
		if (!actual2)
			break;

		mRecvBase += actual2;
	}
}

///////////////////////////////////////////////////////////////////////////

ATNetSockWorker::ATNetSockWorker() {
}

ATNetSockWorker::~ATNetSockWorker() {
	Shutdown();
}

bool ATNetSockWorker::Init(IATNetUdpStack *udp, IATNetTcpStack *tcp, bool externalAccess, uint32 forwardingAddr, uint16 forwardingPort) {
	mpUdpStack = udp;
	mpTcpStack = tcp;
	mbAllowExternalAccess = externalAccess;

	// Bind DNS directly on the gateway, as we have to redirect it to the host's gateway.
	udp->Bind(53, this);

	mpWndThunk = VDCreateFunctionThunkFromMethod(this, &ATNetSockWorker::WndProc, true);
	if (!mpWndThunk) {
		Shutdown();
		return false;
	}

	TCHAR className[64];
	_sntprintf(className, vdcountof(className), _T("ATNetSockWorker_%p"), this);

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

	mForwardingAddr = forwardingAddr;
	mForwardingPort = forwardingPort;

	if (mForwardingAddr) {
		// Create the TCP listening socket.
		SOCKET s = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
		if (s != INVALID_SOCKET) {
			::WSAAsyncSelect(s, mhwnd, MYWM_TCP_LISTEN_SOCKET, FD_ACCEPT);

			sockaddr_in sin = {};
			sin.sin_family = AF_INET;
			sin.sin_port = htons(mForwardingPort);
			sin.sin_addr.S_un.S_addr = htonl(INADDR_ANY);

			if (bind(s, (const sockaddr *)&sin, sizeof sin) == 0) {
				if (listen(s, SOMAXCONN) == 0) {
					mTcpListeningSocket = s;
					s = INVALID_SOCKET;
				}
			}

			if (s != INVALID_SOCKET)
				closesocket(s);
		}
	}

	return true;
}

void ATNetSockWorker::Shutdown() {
	ResetAllConnections();

	if (mTcpListeningSocket != INVALID_SOCKET) {
		::closesocket(mTcpListeningSocket);
		mTcpListeningSocket = INVALID_SOCKET;
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
		mpWndThunk = NULL;
	}

	if (mpUdpStack) {
		mpUdpStack->Unbind(53, this);
		mpUdpStack = NULL;
	}
}

void ATNetSockWorker::ResetAllConnections() {
	while(!mTcpConnections.empty()) {
		ATNetSockBridgeHandler *h = mTcpConnections.begin()->second;

		h->Shutdown();
	}

	for(UdpSocketMap::const_iterator it = mUdpSocketMap.begin(), itEnd = mUdpSocketMap.end();
		it != itEnd;
		++it)
	{
		::closesocket(it->first);
	}

	mUdpSocketMap.clear();
	mUdpSourceMap.clear();

	if (mForwardingAddr) {
		// Create and bind a UDP socket, and set up forwarding.
		CreateUdpConnection(mForwardingAddr, mForwardingPort, 0, mForwardingPort, false);
	}
}

bool ATNetSockWorker::GetHostAddressForLocalAddress(bool tcp, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, uint32& hostIp, uint16& hostPort) {
	SOCKET s = INVALID_SOCKET;

	if (tcp) {
		for(TcpConnections::const_iterator it = mTcpConnections.begin(), itEnd = mTcpConnections.end();
			it != itEnd;
			++it)
		{
			ATNetSockBridgeHandler *h = it->second;

			if (h->GetSrcIpAddr() == srcIpAddr && h->GetSrcPort() == srcPort &&
				h->GetDstIpAddr() == dstIpAddr && h->GetDstPort() == dstPort)
			{
				s = it->first;
				break;
			}
		}
	} else {
		const UdpConnection conn = { srcIpAddr, dstIpAddr, srcPort, dstPort };

		UdpSourceMap::const_iterator it = mUdpSourceMap.find(conn);

		if (it != mUdpSourceMap.end())
			s = it->second;
	}

	if (s != INVALID_SOCKET) {
		sockaddr_in sa = {};
		int sa_len = sizeof sa;

		if (0 == getsockname(s, (sockaddr *)&sa, &sa_len)) {
			hostIp = sa.sin_addr.S_un.S_addr;
			hostPort = ntohs(sa.sin_port);
			return true;
		}
	}

	return false;
}

bool ATNetSockWorker::OnSocketIncomingConnection(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, IATSocket *socket, IATSocketHandler **handler) {
	// check if we are allowed to do this connection
	uint32 redirectedDstIpAddr = dstIpAddr;
	if (mpUdpStack->GetIpStack()->IsLocalOrBroadcastAddress(dstIpAddr)) {
		redirectedDstIpAddr = VDToBE32(0x7F000001);		// 127.0.0.1
	} else if (!mbAllowExternalAccess)
		return false;

	SOCKET s = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

	if (s == INVALID_SOCKET)
		return false;

	::WSAAsyncSelect(s, mhwnd, MYWM_TCP_SOCKET, FD_CONNECT | FD_READ | FD_WRITE | FD_CLOSE);

	sockaddr_in addr = {};
	addr.sin_family = AF_INET;
	addr.sin_port = htons(dstPort);
	addr.sin_addr.S_un.S_addr = redirectedDstIpAddr;
	if (SOCKET_ERROR == ::connect(s, (const sockaddr *)&addr, sizeof addr)) {
		if (WSAGetLastError() != WSAEWOULDBLOCK)
			return false;
	}

	vdrefptr<ATNetSockBridgeHandler> h(new_nothrow ATNetSockBridgeHandler(this, s, socket, srcIpAddr, srcPort, dstIpAddr, dstPort));
	if (!h) {
		closesocket(s);
		return false;
	}

	mTcpConnections[s] = h;
	h->AddRef();

	*handler = h.release();
	return true;
}

void ATNetSockWorker::OnUdpDatagram(const ATEthernetAddr& srcHwAddr, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, const void *data, uint32 dataLen) {
	// Check if this is a DNS packet. If so, we intercept these and redirect them to the
	// host gateway.
	uint32 redirectedDstIpAddr = dstIpAddr;
	bool redirected = false;

	if (dstPort == 53) {
		// Yes, it is DNS -- first, check if it is allowed.
		if (!mbAllowExternalAccess)
			return;
		
		// Look up a gateway.
		ULONG outLen = 0;
		if (ERROR_BUFFER_OVERFLOW != ::GetAdaptersInfo(NULL, &outLen))
			return;

		vdblock<char> buf(outLen);
		if (ERROR_SUCCESS != ::GetAdaptersInfo((IP_ADAPTER_INFO *)buf.data(), &outLen))
			return;

		// find the first valid gateway
		for(const IP_ADAPTER_INFO *info = (const IP_ADAPTER_INFO *)buf.data();
			info;
			info = info->Next)
		{
			if (!info->GatewayList.IpAddress.String[0])
				continue;

			uint32 addr = inet_addr(info->GatewayList.IpAddress.String);

			// The address string will normally be 0.0.0.0 if there is no gateway.
			if (addr && addr != INADDR_NONE) {
				redirectedDstIpAddr = addr;
				redirected = true;
				break;
			}
		}
	} else {
		// if this connection is aimed at the gateway, spoof the dest address to localhost
		if (mpUdpStack->GetIpStack()->IsLocalOrBroadcastAddress(dstIpAddr)) {
			redirectedDstIpAddr = VDToBE32(0x7F000001);		// 127.0.0.1
			redirected = true;
		}
	}

	SOCKET sock = CreateUdpConnection(srcIpAddr, srcPort, dstIpAddr, dstPort, redirected);

	sockaddr_in dstAddr = {0};
	dstAddr.sin_family = AF_INET;
	dstAddr.sin_port = htons(dstPort);
	dstAddr.sin_addr.S_un.S_addr = redirectedDstIpAddr;

	::sendto(sock, (const char *)data, dataLen, 0, (const sockaddr *)&dstAddr, sizeof dstAddr);
}

SOCKET ATNetSockWorker::CreateUdpConnection(uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort, bool redirected) {
	// see if we already have a socket set up for this source address
	UdpConnection conn = {0};
	conn.mSrcIpAddr = srcIpAddr;
	conn.mSrcPort = srcPort;

	if (redirected) {
		// if we're redirecting the connection, we must retain the original destination so
		// we can spoof it on the reply
		conn.mDstIpAddr = dstIpAddr;
		conn.mDstPort = dstPort;
	}

	UdpSourceMap::insert_return_type r = mUdpSourceMap.insert(conn);

	if (r.second) {
		// Nope -- establish a new socket.
		SOCKET s = ::socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (s == INVALID_SOCKET) {
			mUdpSourceMap.erase(r.first);
			return s;
		}

		sockaddr_in bindAddr = {0};
		bindAddr.sin_family = AF_INET;
		bindAddr.sin_port = !dstIpAddr && dstPort ? htons(dstPort) : htons(0);
		bindAddr.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
		bind(s, (const sockaddr *)&bindAddr, sizeof bindAddr);

		::WSAAsyncSelect(s, mhwnd, MYWM_UDP_SOCKET, FD_READ);

		r.first->second = s;

		mUdpSocketMap[s] = conn;
	}

	return r.first->second;
}

void ATNetSockWorker::DeleteConnection(SOCKET s) {
	TcpConnections::iterator it = mTcpConnections.find(s);

	if (it != mTcpConnections.end()) {
		ATNetSockBridgeHandler *h = it->second;

		mTcpConnections.erase(it);

		h->Release();
	}
}

LRESULT ATNetSockWorker::WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	if (msg == MYWM_TCP_SOCKET) {
		const SOCKET sock = (SOCKET)wParam;
		const UINT event = LOWORD(lParam);
		const UINT error = HIWORD(lParam);

		TcpConnections::iterator it = mTcpConnections.find(sock);

		if (it == mTcpConnections.end()) {
			VDASSERT(!"Received message for invalid connection.");
		} else {
			ATNetSockBridgeHandler *h = it->second;

			switch(event) {
				case FD_CONNECT:
					h->OnNativeSocketConnect();
					break;

				case FD_READ:
					h->OnNativeSocketReadReady();
					break;
					
				case FD_WRITE:
					h->OnNativeSocketWriteReady();
					break;

				case FD_CLOSE:
					if (error)
						h->OnNativeSocketError();
					else
						h->OnNativeSocketClose();
					break;
			}
		}

		return 0;
	} else if (msg == MYWM_UDP_SOCKET) {
		const SOCKET sock = (SOCKET)wParam;

		UdpSocketMap::iterator it = mUdpSocketMap.find(sock);

		if (it == mUdpSocketMap.end()) {
			VDASSERT(!"Received message for invalid connection.");
		} else {
			const UdpConnection& conn = it->second;

			// The connection info is for the original outbound datagram that bound the socket,
			// so we must reverse it for the incoming datagram we're handling here.
			ProcessUdpDatagram(it->first, conn.mDstIpAddr, conn.mDstPort, conn.mSrcIpAddr, conn.mSrcPort);
		}

		return 0;
	} else if (msg == MYWM_TCP_LISTEN_SOCKET) {
		const SOCKET sock = (SOCKET)wParam;
		const UINT event = LOWORD(lParam);

		VDASSERT(sock == mTcpListeningSocket);

		if (event == FD_ACCEPT) {
			sockaddr addr = {};
			int addrlen = sizeof(addr);
			
			SOCKET newSocket = accept(sock, &addr, &addrlen);

			if (newSocket != INVALID_SOCKET) {
				::WSAAsyncSelect(newSocket, mhwnd, MYWM_TCP_SOCKET, FD_CONNECT | FD_READ | FD_WRITE | FD_CLOSE);

				if (addr.sa_family != AF_INET) {
					closesocket(newSocket);
					return 0;
				}

				// hey, we've got a new incoming connection -- bind it to a new NAT connection and attempt
				// to connect to the forwarding target
				const sockaddr_in *addr4 = (const sockaddr_in *)&addr;
				vdrefptr<ATNetSockBridgeHandler> h(new_nothrow ATNetSockBridgeHandler(this, newSocket, nullptr, addr4->sin_addr.S_un.S_addr, ntohs(addr4->sin_port), mForwardingAddr, mForwardingPort));
				if (!h) {
					closesocket(newSocket);
					return 0;
				}

				vdrefptr<IATSocket> emuSocket;
				if (!mpTcpStack->Connect(mForwardingAddr, mForwardingPort, h, ~emuSocket)) {
					closesocket(newSocket);
					return 0;
				}

				h->SetLocalSocket(emuSocket);

				mTcpConnections[newSocket] = h;
				h->AddRef();
	
				h->OnNativeSocketConnect();
			}
		}

		return 0;
	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

void ATNetSockWorker::ProcessUdpDatagram(SOCKET s, uint32 srcIpAddr, uint16 srcPort, uint32 dstIpAddr, uint16 dstPort) {
	char buf[4096];
	sockaddr_in from;
	int fromlen = sizeof from;

	int len = ::recvfrom(s, buf, sizeof buf, 0, (sockaddr *)&from, &fromlen);

	if (len && len != SOCKET_ERROR)
		mpUdpStack->SendDatagram(srcIpAddr ? srcIpAddr : from.sin_addr.S_un.S_addr, srcPort ? srcPort : ntohs(from.sin_port), dstIpAddr, dstPort, buf, len);
}

///////////////////////////////////////////////////////////////////////////

void ATCreateNetSockWorker(IATNetUdpStack *udp, IATNetTcpStack *tcp, bool externalAccess, uint32 forwardingAddr, uint16 forwardingPort, IATNetSockWorker **pp) {
	ATNetSockWorker *p = new ATNetSockWorker;

	if (!p->Init(udp, tcp, externalAccess, forwardingAddr, forwardingPort)) {
		delete p;
		throw MyMemoryError();
	}

	p->AddRef();
	*pp = p;
}
