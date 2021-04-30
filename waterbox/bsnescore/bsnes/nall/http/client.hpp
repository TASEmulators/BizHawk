#pragma once

#include <nall/http/role.hpp>

namespace nall::HTTP {

struct Client : Role {
  inline auto open(const string& hostname, uint port = 80) -> bool;
  inline auto upload(const Request& request) -> bool;
  inline auto download(const Request& request) -> Response;
  inline auto close() -> void;
  ~Client() { close(); }

private:
  int fd = -1;
  addrinfo* info = nullptr;
};

auto Client::open(const string& hostname, uint port) -> bool {
  addrinfo hint = {0};
  hint.ai_family = AF_UNSPEC;
  hint.ai_socktype = SOCK_STREAM;
  hint.ai_flags = AI_ADDRCONFIG;

  if(getaddrinfo(hostname, string{port}, &hint, &info) != 0) return close(), false;

  fd = socket(info->ai_family, info->ai_socktype, info->ai_protocol);
  if(fd < 0) return close(), false;

  if(connect(fd, info->ai_addr, info->ai_addrlen) < 0) return close(), false;
  return true;
}

auto Client::upload(const Request& request) -> bool {
  return Role::upload(fd, request);
}

auto Client::download(const Request& request) -> Response {
  Response response(request);
  Role::download(fd, response);
  return response;
}

auto Client::close() -> void {
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
