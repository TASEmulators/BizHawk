#include <nall/http/server.hpp>
#include <nall/thread.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <ws2tcpip.h>
#endif

namespace nall::HTTP {

NALL_HEADER_INLINE auto Server::open(u16 port, const string& serviceName, const string& command) -> bool {
  if(serviceName) {
    if(!service::command(serviceName, command)) return false;
  }

  fd4 = socket(AF_INET, SOCK_STREAM, 0);
  fd6 = socket(AF_INET6, SOCK_STREAM, 0);
  if(!ipv4() && !ipv6()) return false;

  {
  #if defined(SO_RCVTIMEO)
  if(settings.timeoutReceive) {
    struct timeval rcvtimeo;
    rcvtimeo.tv_sec  = settings.timeoutReceive / 1000;
    rcvtimeo.tv_usec = settings.timeoutReceive % 1000 * 1000;
    if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_RCVTIMEO, &rcvtimeo, sizeof(struct timeval));
    if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_RCVTIMEO, &rcvtimeo, sizeof(struct timeval));
  }
  #endif

  #if defined(SO_SNDTIMEO)
  if(settings.timeoutSend) {
    struct timeval sndtimeo;
    sndtimeo.tv_sec  = settings.timeoutSend / 1000;
    sndtimeo.tv_usec = settings.timeoutSend % 1000 * 1000;
    if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_SNDTIMEO, &sndtimeo, sizeof(struct timeval));
    if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_SNDTIMEO, &sndtimeo, sizeof(struct timeval));
  }
  #endif

  #if defined(SO_NOSIGPIPE)  //BSD, OSX
  s32 nosigpipe = 1;
  if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_NOSIGPIPE, &nosigpipe, sizeof(s32));
  if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_NOSIGPIPE, &nosigpipe, sizeof(s32));
  #endif

  #if defined(SO_REUSEADDR)  //BSD, Linux, OSX
  s32 reuseaddr = 1;
  if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_REUSEADDR, &reuseaddr, sizeof(s32));
  if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_REUSEADDR, &reuseaddr, sizeof(s32));
  #endif

  #if defined(SO_REUSEPORT)  //BSD, OSX
  s32 reuseport = 1;
  if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_REUSEPORT, &reuseport, sizeof(s32));
  if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_REUSEPORT, &reuseport, sizeof(s32));
  #endif
  }

  addrin4.sin_family = AF_INET;
  addrin4.sin_addr.s_addr = htonl(INADDR_ANY);
  addrin4.sin_port = htons(port);

  addrin6.sin6_family = AF_INET6;
  addrin6.sin6_addr = in6addr_any;
  addrin6.sin6_port = htons(port);

  if(bind(fd4, (struct sockaddr*)&addrin4, sizeof(addrin4)) < 0 || listen(fd4, SOMAXCONN) < 0) ipv4_close();
  if(bind(fd6, (struct sockaddr*)&addrin6, sizeof(addrin6)) < 0 || listen(fd6, SOMAXCONN) < 0) ipv6_close();
  return ipv4() || ipv6();
}

NALL_HEADER_INLINE auto Server::ipv4_scan() -> bool {
  struct pollfd query = {0};
  query.fd = fd4;
  query.events = POLLIN;
  poll(&query, 1, 0);

  if(query.fd == fd4 && query.revents & POLLIN) {
    ++connections;

    thread::create([&](uintptr) {
      thread::detach();

      s32 clientfd = -1;
      struct sockaddr_in settings = {0};
      socklen_t socklen = sizeof(sockaddr_in);

      clientfd = accept(fd4, (struct sockaddr*)&settings, &socklen);
      if(clientfd < 0) return;

      u32 ip = ntohl(settings.sin_addr.s_addr);

      Request request;
      request._ipv6 = false;
      request._ip = {
        (u8)(ip >> 24), ".",
        (u8)(ip >> 16), ".",
        (u8)(ip >>  8), ".",
        (u8)(ip >>  0)
      };

      if(download(clientfd, request) && callback) {
        auto response = callback(request);
        upload(clientfd, response);
      } else {
        upload(clientfd, Response());  //"501 Not Implemented"
      }

      ::close(clientfd);
      --connections;
    }, 0, settings.threadStackSize);

    return true;
  }

  return false;
}

NALL_HEADER_INLINE auto Server::ipv6_scan() -> bool {
  struct pollfd query = {0};
  query.fd = fd6;
  query.events = POLLIN;
  poll(&query, 1, 0);

  if(query.fd == fd6 && query.revents & POLLIN) {
    ++connections;

    thread::create([&](uintptr) {
      thread::detach();

      s32 clientfd = -1;
      struct sockaddr_in6 settings = {0};
      socklen_t socklen = sizeof(sockaddr_in6);

      clientfd = accept(fd6, (struct sockaddr*)&settings, &socklen);
      if(clientfd < 0) return;

      u8* ip = settings.sin6_addr.s6_addr;
      u16 ipSegment[8];
      for(auto n : range(8)) ipSegment[n] = ip[n * 2 + 0] * 256 + ip[n * 2 + 1];

      Request request;
      request._ipv6 = true;
      //RFC5952 IPv6 encoding: the first longest 2+ consecutive zero-sequence is compressed to "::"
      s32 zeroOffset  = -1;
      s32 zeroLength  =  0;
      s32 zeroCounter =  0;
      for(auto n : range(8)) {
        u16 value = ipSegment[n];
        if(value == 0) zeroCounter++;
        if(zeroCounter > zeroLength) {
          zeroLength = zeroCounter;
          zeroOffset = 1 + n - zeroLength;
        }
        if(value != 0) zeroCounter = 0;
      }
      if(zeroLength == 1) zeroOffset = -1;
      for(u32 n = 0; n < 8;) {
        if(n == zeroOffset) {
          request._ip.append(n == 0 ? "::" : ":");
          n += zeroLength;
        } else {
          u16 value = ipSegment[n];
          request._ip.append(hex(value), n++ != 7 ? ":" : "");
        }
      }

      if(download(clientfd, request) && callback) {
        auto response = callback(request);
        upload(clientfd, response);
      } else {
        upload(clientfd, Response());  //"501 Not Implemented"
      }

      ::close(clientfd);
      --connections;
    }, 0, settings.threadStackSize);

    return true;
  }

  return false;
}

}
