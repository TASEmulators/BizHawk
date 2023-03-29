#pragma once

#include <nall/platform.hpp>
#include <nall/arguments.hpp>
#include <nall/string.hpp>

namespace nall {
  auto main(Arguments arguments) -> void;

  auto main(int argc, char** argv) -> int;
}

#if defined(NALL_HEADER_ONLY)
  #include <nall/main.cpp>
#endif
