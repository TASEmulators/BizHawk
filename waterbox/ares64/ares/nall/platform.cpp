#include <nall/platform.hpp>

#if defined(PLATFORM_WINDOWS)

#include <winsock2.h>

NALL_HEADER_INLINE auto poll(struct pollfd fds[], unsigned long nfds, int timeout) -> int { return WSAPoll(fds, nfds, timeout); }

namespace nall {

NALL_HEADER_INLINE auto recv(int socket, void* buffer, size_t length, int flags) -> ssize_t {
  return ::recv(socket, (char*)buffer, length, flags);
}

NALL_HEADER_INLINE auto send(int socket, const void* buffer, size_t length, int flags) -> ssize_t {
  return ::send(socket, (const char*)buffer, length, flags);
}

NALL_HEADER_INLINE auto setsockopt(int socket, int level, int option_name, const void* option_value, int option_len) -> int {
  return ::setsockopt(socket, level, option_name, (const char*)option_value, option_len);
}

NALL_HEADER_INLINE auto usleep(unsigned int us) -> int {
  if(us != 0) {
    Sleep(us / 1000);
  }

  return 0;
}

}

#endif
