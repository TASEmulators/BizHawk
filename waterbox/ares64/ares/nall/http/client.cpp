#include <nall/http/client.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <ws2tcpip.h>
#endif

namespace nall::HTTP {

NALL_HEADER_INLINE auto Client::open(const string& hostname, u16 port) -> bool {
  addrinfo hint = {};
  hint.ai_family = AF_UNSPEC;
  hint.ai_socktype = SOCK_STREAM;
  hint.ai_flags = AI_ADDRCONFIG;

  if(getaddrinfo(hostname, string{port}, &hint, &info) != 0) return close(), false;

  fd = socket(info->ai_family, info->ai_socktype, info->ai_protocol);
  if(fd < 0) return close(), false;

  if(connect(fd, info->ai_addr, info->ai_addrlen) < 0) return close(), false;
  return true;
}

NALL_HEADER_INLINE auto Client::close() -> void {
  if(fd) {
    ::close(fd);
    fd = -1;
  }

  if(info) {
    freeaddrinfo(info);
    info = nullptr;
  }
}

}
