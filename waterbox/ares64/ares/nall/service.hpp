#pragma once

//service model template built on top of shared-memory

#include <nall/shared-memory.hpp>

#if defined(API_POSIX)
  #include <nall/posix/service.hpp>
#endif

#if defined(API_WINDOWS)
  #include <nall/windows/service.hpp>
#endif
