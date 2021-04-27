#pragma once

#include <nall/service.hpp>
#include <nall/http/role.hpp>

namespace nall::HTTP {

struct Server : Role, service {
  inline auto open(uint port = 8080, const string& serviceName = "", const string& command = "") -> bool;
  inline auto main(const function<Response (Request&)>& function = {}) -> void;
  inline auto scan() -> string;
  inline auto close() -> void;
  ~Server() { close(); }

private:
  function<Response (Request&)> callback;
  std::atomic<int> connections{0};

  int fd4 = -1;
  int fd6 = -1;
  struct sockaddr_in addrin4 = {0};
  struct sockaddr_in6 addrin6 = {0};

  auto ipv4() const -> bool { return fd4 >= 0; }
  auto ipv6() const -> bool { return fd6 >= 0; }

  auto ipv4_close() -> void { if(fd4 >= 0) ::close(fd4); fd4 = -1; }
  auto ipv6_close() -> void { if(fd6 >= 0) ::close(fd6); fd6 = -1; }

  auto ipv4_scan() -> bool;
  auto ipv6_scan() -> bool;
};

auto Server::open(uint port, const string& serviceName, const string& command) -> bool {
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
  int nosigpipe = 1;
  if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_NOSIGPIPE, &nosigpipe, sizeof(int));
  if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_NOSIGPIPE, &nosigpipe, sizeof(int));
  #endif

  #if defined(SO_REUSEADDR)  //BSD, Linux, OSX
  int reuseaddr = 1;
  if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_REUSEADDR, &reuseaddr, sizeof(int));
  if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_REUSEADDR, &reuseaddr, sizeof(int));
  #endif

  #if defined(SO_REUSEPORT)  //BSD, OSX
  int reuseport = 1;
  if(ipv4()) setsockopt(fd4, SOL_SOCKET, SO_REUSEPORT, &reuseport, sizeof(int));
  if(ipv6()) setsockopt(fd6, SOL_SOCKET, SO_REUSEPORT, &reuseport, sizeof(int));
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

auto Server::main(const function<Response (Request&)>& function) -> void {
  callback = function;
}

auto Server::scan() -> string {
  if(auto command = service::receive()) return command;
  if(connections >= settings.connectionLimit) return "busy";
  if(ipv4() && ipv4_scan()) return "ok";
  if(ipv6() && ipv6_scan()) return "ok";
  return "idle";
}

auto Server::ipv4_scan() -> bool {
  struct pollfd query = {0};
  query.fd = fd4;
  query.events = POLLIN;
  poll(&query, 1, 0);

  if(query.fd == fd4 && query.revents & POLLIN) {
    ++connections;

    thread::create([&](uintptr) {
      thread::detach();

      int clientfd = -1;
      struct sockaddr_in settings = {0};
      socklen_t socklen = sizeof(sockaddr_in);

      clientfd = accept(fd4, (struct sockaddr*)&settings, &socklen);
      if(clientfd < 0) return;

      uint32_t ip = ntohl(settings.sin_addr.s_addr);

      Request request;
      request._ipv6 = false;
      request._ip = {
        (uint8_t)(ip >> 24), ".",
        (uint8_t)(ip >> 16), ".",
        (uint8_t)(ip >>  8), ".",
        (uint8_t)(ip >>  0)
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

auto Server::ipv6_scan() -> bool {
  struct pollfd query = {0};
  query.fd = fd6;
  query.events = POLLIN;
  poll(&query, 1, 0);

  if(query.fd == fd6 && query.revents & POLLIN) {
    ++connections;

    thread::create([&](uintptr) {
      thread::detach();

      int clientfd = -1;
      struct sockaddr_in6 settings = {0};
      socklen_t socklen = sizeof(sockaddr_in6);

      clientfd = accept(fd6, (struct sockaddr*)&settings, &socklen);
      if(clientfd < 0) return;

      uint8_t* ip = settings.sin6_addr.s6_addr;
      uint16_t ipSegment[8];
      for(auto n : range(8)) ipSegment[n] = ip[n * 2 + 0] * 256 + ip[n * 2 + 1];

      Request request;
      request._ipv6 = true;
      //RFC5952 IPv6 encoding: the first longest 2+ consecutive zero-sequence is compressed to "::"
      int zeroOffset  = -1;
      int zeroLength  =  0;
      int zeroCounter =  0;
      for(auto n : range(8)) {
        uint16_t value = ipSegment[n];
        if(value == 0) zeroCounter++;
        if(zeroCounter > zeroLength) {
          zeroLength = zeroCounter;
          zeroOffset = 1 + n - zeroLength;
        }
        if(value != 0) zeroCounter = 0;
      }
      if(zeroLength == 1) zeroOffset = -1;
      for(uint n = 0; n < 8;) {
        if(n == zeroOffset) {
          request._ip.append(n == 0 ? "::" : ":");
          n += zeroLength;
        } else {
          uint16_t value = ipSegment[n];
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

auto Server::close() -> void {
  ipv4_close();
  ipv6_close();
}

}
