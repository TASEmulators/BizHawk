#pragma once

#include <nall/range.hpp>

namespace nall {

//counting sort by powers of two: used to implement radix sort
template<uint Bits, uint Shift, typename T>
auto counting_sort(T* output, const T* input, uint size) -> void {
  static_assert(Bits >= 1 && Bits <= 20, "must be between 1 and 20 bits");
  enum : uint { Base = 1 << Bits, Mask = Base - 1 };

  uint64_t count[Base] = {}, last = 0;
  for(uint n : range(size)) ++count[(input[n] >> Shift) & Mask];
  for(uint n : range(Base)) last += count[n], count[n] = last - count[n];
  for(uint n : range(size)) output[count[(input[n] >> Shift) & Mask]++] = input[n];
}

}
