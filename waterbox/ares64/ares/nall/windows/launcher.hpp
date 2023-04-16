#pragma once

#include <nall/stdint.hpp>

namespace nall {

//launch a new process and inject specified DLL into it

auto launch(const char* applicationName, const char* libraryName, u32 entryPoint) -> bool;

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/windows/launcher.cpp>
#endif
