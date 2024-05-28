#include <nall/tcptext/tcp-socket.hpp>

#include <inttypes.h>
#include <memory>
#include <thread>

#if defined(PLATFORM_WINDOWS)
  #include <ws2tcpip.h>
#else
  #include <netinet/tcp.h>
#endif

struct sockaddr_in;
struct sockaddr_in6;

namespace {
  constexpr bool TCP_LOG_MESSAGES = false;

  constexpr u32 TCP_BUFFER_SIZE = 1024 * 16;
  constexpr u32 CLIENT_SLEEP_MS = 10; // ms to sleep while checking for new clients
  constexpr u32 CYCLES_BEFORE_SLEEP = 100; // how often to do a send/receive check before a sleep
  constexpr u32 RECEIVE_TIMEOUT_SEC = 1; // only important for latency of disconnecting clients, reads are blocming anyways

  // A few platform specific socket functions:
  // (In general, windows+linux share the same names, yet they behave differenly)
  auto socketSetBlockingMode(s32 socket, bool isBlocking) -> bool
  {
    if(socket < 0)return false;
    #if defined(O_NONBLOCK) // Linux
      auto oldFlags = fcntl(socket, F_GETFL, 0);
      auto newFlags = isBlocking ? (oldFlags ^ O_NONBLOCK) : (oldFlags | O_NONBLOCK);
      return fcntl(socket, F_SETFL, newFlags) == 0;
    #elif defined(FIONBIO) // Windows
      u_long state = isBlocking ? 0 : 1;
      return ioctlsocket(socket, FIONBIO, &state) == NO_ERROR;
    #endif
  }

  auto socketShutdown(s32 socket) {
    if(socket < 0)return;
    #if defined(SD_BOTH) // Windows
      ::shutdown(socket, SD_BOTH);
    #elif defined(SHUT_RDWR) // Linux, Mac
      ::shutdown(socket, SHUT_RDWR);
    #endif
  }

  auto socketClose(s32 socket) {
    if(socket < 0)return;
    #if defined(PLATFORM_WINDOWS)
      ::closesocket(socket);
    #else
      ::close(socket);
    #endif
  }
}

namespace nall::TCP {

NALL_HEADER_INLINE auto Socket::getURL(u32 port, bool useIPv4) const -> string {
  return {useIPv4 ? "127.0.0.1:" : "[::1]:", port};
}

NALL_HEADER_INLINE auto Socket::open(u32 port, bool useIPv4) -> bool {
  stopServer = false;

  auto url = getURL(port, useIPv4);
  printf("Opening TCP-server on %s\n", url.data());
 
  auto threadServer = std::thread([this, port, useIPv4]() {
    serverRunning = true;

    while (!stopServer) {
      fdServer = socket(useIPv4 ? AF_INET : AF_INET6, SOCK_STREAM, 0);  
      if(fdServer < 0)
        break;

      {
        s32 valueOn = 1;
        #if defined(SO_NOSIGPIPE)  //BSD, OSX
          setsockopt(fdServer, SOL_SOCKET, SO_NOSIGPIPE, &valueOn, sizeof(s32));
        #endif

        #if defined(SO_REUSEADDR)  //BSD, Linux, OSX
          setsockopt(fdServer, SOL_SOCKET, SO_REUSEADDR, &valueOn, sizeof(s32));
        #endif

        #if defined(SO_REUSEPORT)  //BSD, OSX
          setsockopt(fdServer, SOL_SOCKET, SO_REUSEPORT, &valueOn, sizeof(s32));
        #endif

        #if defined(TCP_NODELAY)
          setsockopt(fdServer, IPPROTO_TCP, TCP_NODELAY, &valueOn, sizeof(s32));
        #endif

        if(!socketSetBlockingMode(fdServer, true)) {
          print("TCP: failed to set to blocking mode!\n");
        }

        #if defined(SO_RCVTIMEO)
          #if defined(PLATFORM_WINDOWS)
            DWORD rcvTimeMs = 1000 * RECEIVE_TIMEOUT_SEC;
            setsockopt(fdServer, SOL_SOCKET, SO_RCVTIMEO, &rcvTimeMs, sizeof(rcvTimeMs));
          #else
            struct timeval rcvtimeo;
            rcvtimeo.tv_sec  = RECEIVE_TIMEOUT_SEC;
            rcvtimeo.tv_usec = 0;
            setsockopt(fdServer, SOL_SOCKET, SO_RCVTIMEO, &rcvtimeo, sizeof(rcvtimeo));
          #endif
        #endif
      }

      s32 bindRes;
      if(useIPv4) {
        sockaddr_in serverAddrV4{};
        serverAddrV4.sin_family = AF_INET;
        serverAddrV4.sin_addr.s_addr = htonl(INADDR_ANY);
        serverAddrV4.sin_port = htons(port);

        bindRes = ::bind(fdServer, (sockaddr*)&serverAddrV4, sizeof(serverAddrV4)) < 0;
      } else {
        sockaddr_in6 serverAddrV6{};
        serverAddrV6.sin6_family = AF_INET6;
        serverAddrV6.sin6_addr = in6addr_loopback;
        serverAddrV6.sin6_port = htons(port);

        bindRes = ::bind(fdServer, (sockaddr*)&serverAddrV6, sizeof(serverAddrV6)) < 0;
      }

      if(bindRes < 0 || listen(fdServer, 1) < 0) {
        printf("error binding socket on port %d! (%s)\n", port, strerror(errno));
        break;
      }

      // scan for new connections
      while(fdClient < 0) {
        fdClient = ::accept(fdServer, nullptr, nullptr);
        if(fdClient < 0) {
          if(errno != EAGAIN) {
            if(!stopServer)
              printf("error accepting connection! (%s)\n", strerror(errno));
            break;
          }
          std::this_thread::sleep_for(std::chrono::milliseconds(CLIENT_SLEEP_MS));
        }
      }
      if (fdClient < 0) {
        break;
      }

      // close the server socket, we only want one client
      socketClose(fdServer);
      fdServer = -1;

      while (!stopServer && fdClient >= 0) {
        // Kick client if we need to
        if(wantKickClient) {
          socketClose(fdClient);
          fdClient = -1;
          wantKickClient = false;
          onDisconnect();
          break;
        }

        std::this_thread::sleep_for(std::chrono::milliseconds(CLIENT_SLEEP_MS));
      }
    }
    
    printf("Stopping TCP-server...\n");

    socketClose(fdClient);
    fdClient = -1;

    wantKickClient = false;

    printf("TCP-server stopped\n");
    serverRunning = false;
  });

  auto threadSend = std::thread([this]() 
  {
    vector<u8> localSendBuffer{};
    u32 cycles = 0;

    while(!stopServer) 
    {
      if(fdClient < 0) {
        std::this_thread::sleep_for(std::chrono::milliseconds(CLIENT_SLEEP_MS));
        continue;
      }

      { // copy send-data to minimize lock time
        std::lock_guard guard{sendBufferMutex};
        if(sendBuffer.size() > 0) {
          localSendBuffer = sendBuffer;
          sendBuffer.resize(0);
        }
      }

      // send data
      if(localSendBuffer.size() > 0) {
        auto bytesWritten = send(fdClient, localSendBuffer.data(), localSendBuffer.size(), 0);
        if(bytesWritten < localSendBuffer.size()) {
          printf("Error sending data! (%s)\n", strerror(errno));
        }

        if constexpr(TCP_LOG_MESSAGES) {
          printf("%.4f | TCP >: [%" PRIu64 "]: %.*s\n", (f64)chrono::millisecond() / 1000.0, localSendBuffer.size(), localSendBuffer.size() > 100 ? 100 : (int)localSendBuffer.size(), (char*)localSendBuffer.data());
        }

        localSendBuffer.resize(0);
        cycles = 0; // sending once has a good chance of sending more -> reset sleep timer
      }

      if(cycles++ >= CYCLES_BEFORE_SLEEP) {
        std::this_thread::sleep_for(std::chrono::microseconds(1));
        cycles = 0;
      } 
    }
  });

  auto threadReceive = std::thread([this]() 
  {
    u8 packet[TCP_BUFFER_SIZE]{0};

    while(!stopServer) 
    {
      if(fdClient < 0 || wantKickClient) {
        std::this_thread::sleep_for(std::chrono::milliseconds(CLIENT_SLEEP_MS));
        continue;
      }

      // receive data from connected clients
      s32 length = recv(fdClient, packet, TCP_BUFFER_SIZE, MSG_NOSIGNAL);
      if(length > 0) {
        std::lock_guard guard{receiveBufferMutex};
        auto oldSize = receiveBuffer.size();
        receiveBuffer.resize(oldSize + length);
        memcpy(receiveBuffer.data() + oldSize, packet, length);

        if constexpr(TCP_LOG_MESSAGES) {
          printf("%.4f | TCP <: [%d]: %.*s ([%d]: %.*s)\n", (f64)chrono::millisecond() / 1000.0, length, length, (char*)receiveBuffer.data(), length, length, (char*)packet);
        }
      } else if(length == 0) {
        disconnectClient();
      } else {
        #if defined(PLATFORM_WINDOWS)
        if (WSAGetLastError() != WSAETIMEDOUT) {
        #else
        if (errno != EAGAIN) {
        #endif
          printf("TCP server: error receiving data from client: %s\n", strerror(errno));
          disconnectClient();
        }
      }
    }
  });

  threadServer.detach();
  threadSend.detach();
  threadReceive.detach();

  return true;
}

NALL_HEADER_INLINE auto Socket::close(bool notifyHandler) -> void {
  stopServer = true;

  // we have to forcefully shut it down here, since otherwise accept() would hang causing a UI crash
  socketShutdown(fdServer);
  socketClose(fdClient);
  socketClose(fdServer);
  fdServer = -1;
  fdClient = -1;

  while(serverRunning) {
    std::this_thread::sleep_for(std::chrono::milliseconds(250)); // wait for other threads to stop
  }

  if(notifyHandler) {
    onDisconnect(); // don't call this in destructor, it's virtual
  }
}

NALL_HEADER_INLINE auto Socket::update() -> void {
  vector<u8> data{};
  
  { // local copy, minimize lock time
    std::lock_guard guard{receiveBufferMutex};
    if(receiveBuffer.size() > 0) {
      data = receiveBuffer;
      receiveBuffer.resize(0);
    }
  }

  if(data.size() > 0) {
    onData(data);
  }
}

NALL_HEADER_INLINE auto Socket::disconnectClient() -> void {
  wantKickClient = true;
}

NALL_HEADER_INLINE auto Socket::sendData(const u8* data, u32 size) -> void {
  std::lock_guard guard{sendBufferMutex};
  u32 oldSize = sendBuffer.size();
  sendBuffer.resize(oldSize + size);
  memcpy(sendBuffer.data() + oldSize, data, size);
}

}
