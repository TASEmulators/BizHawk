#pragma once

namespace nall {

#if defined(__AVX2__)
  #define SIMD 256
  #define SIMD_AVX2
  #include <immintrin.h>
#endif

}
