#pragma once

#include <nall/service.hpp>
#include <nall/http/role.hpp>

struct sockaddr_in;
struct sockaddr_in6;

namespace nall::HTTP {

struct Server : Role, service {
  auto open(u16 port = 8080, const string& serviceName = "", const string& command = "") -> bool;
  auto main(const function<Response (Request&)>& function = {}) -> void;
  auto scan() -> string;
  auto close() -> void;
  ~Server() { close(); }

private:
  function<Response (Request&)> callback;
  std::atomic<s32> connections{0};

  s32 fd4 = -1;
  s32 fd6 = -1;
  u64 addrin4_storage[16] = {0};  //sizeof(sockaddr_storage) = 128
  u64 addrin6_storage[16] = {0};
  sockaddr_in& addrin4 = (sockaddr_in&)addrin4_storage;
  sockaddr_in6& addrin6 = (sockaddr_in6&)addrin6_storage;

  auto ipv4() const -> bool { return fd4 >= 0; }
  auto ipv6() const -> bool { return fd6 >= 0; }

  auto ipv4_close() -> void { if(fd4 >= 0) ::close(fd4); fd4 = -1; }
  auto ipv6_close() -> void { if(fd6 >= 0) ::close(fd6); fd6 = -1; }

  auto ipv4_scan() -> bool;
  auto ipv6_scan() -> bool;
};

inline auto Server::main(const function<Response (Request&)>& function) -> void {
  callback = function;
}

inline auto Server::scan() -> string {
  if(auto command = service::receive()) return command;
  if(connections >= settings.connectionLimit) return "busy";
  if(ipv4() && ipv4_scan()) return "ok";
  if(ipv6() && ipv6_scan()) return "ok";
  return "idle";
}

inline auto Server::close() -> void {
  ipv4_close();
  ipv6_close();
}

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/http/server.cpp>
#endif
