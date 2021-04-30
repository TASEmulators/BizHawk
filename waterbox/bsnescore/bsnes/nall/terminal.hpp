#pragma once

#include <nall/string.hpp>

namespace nall::terminal {

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
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[30m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto blue(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[94m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto green(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[92m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto cyan(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[96m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto red(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[91m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto magenta(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[95m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto yellow(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[93m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto white(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[97m", string{forward<P>(p)...}, "\e[0m"};
}

template<typename... P> inline auto gray(P&&... p) -> string {
  if(!escapable()) return string{forward<P>(p)...};
  return {"\e[37m", string{forward<P>(p)...}, "\e[0m"};
}

}

}
