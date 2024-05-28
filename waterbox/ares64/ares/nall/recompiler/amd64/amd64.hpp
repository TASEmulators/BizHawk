#pragma once

namespace nall::recompiler {
  struct amd64 {
    #include "emitter.hpp"
    #include "constants.hpp"
    #include "encoder-instructions.hpp"
    #if defined(PLATFORM_WINDOWS)
      #include "encoder-calls-windows.hpp"
    #else
      #include "encoder-calls-systemv.hpp"
    #endif
  };
}
