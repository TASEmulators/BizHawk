#pragma once

#include <nall/http/role.hpp>

struct addrinfo;

namespace nall::HTTP {

struct Client : Role {
  auto open(const string& hostname, u16 port = 80) -> bool;
  auto upload(const Request& request) -> bool;
  auto download(const Request& request) -> Response;
  auto close() -> void;
  ~Client() { close(); }

private:
  s32 fd = -1;
  addrinfo* info = nullptr;
};

inline auto Client::upload(const Request& request) -> bool {
  return Role::upload(fd, request);
}

inline auto Client::download(const Request& request) -> Response {
  Response response(request);
  Role::download(fd, response);
  return response;
}

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/http/client.cpp>
#endif
