#pragma once

#include <nall/memory.hpp>
#include <nall/string.hpp>

#if defined(API_POSIX)
  #include <nall/posix/shared-memory.hpp>
#endif

#if defined(API_WINDOWS)
  #include <nall/windows/shared-memory.hpp>
#endif
