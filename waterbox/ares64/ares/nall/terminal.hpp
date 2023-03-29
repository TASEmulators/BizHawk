#pragma once

#include <nall/string.hpp>

namespace nall::terminal {

//control sequence introducer
constexpr char csi[] = "\x1b[";

auto redirectStdioToTerminal(bool create) -> void;

inline auto escapable() -> bool {
  #if defined(PLATFORM_WINDOWS)
  //todo: colors are supported by Windows 10+ and with alternate terminals (eg msys)
  //disabled for now for compatibility with Windows 7 and 8.1's cmd.exe
  return false;
  #endif
  return true;
}

namespace color {

template<typename... P> inline auto black(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "30m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto blue(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "94m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto green(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "92m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto cyan(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "96m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto red(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "91m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto magenta(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "95m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto yellow(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "93m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto white(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "97m", string{std::forward<P>(p)...}, csi, "0m"};
}

template<typename... P> inline auto gray(P&&... p) -> string {
  if(!escapable()) return string{std::forward<P>(p)...};
  return {csi, "37m", string{std::forward<P>(p)...}, csi, "0m"};
}

}

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/terminal.cpp>
#endif
