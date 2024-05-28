#pragma once

#include <nall/string.hpp>

namespace nall {

auto guid() -> string;

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/windows/guid.cpp>
#endif
