#pragma once

#include <nall/range.hpp>

namespace nall {

//counting sort by powers of two: used to implement radix sort
template<u32 Bits, u32 Shift, typename T>
auto counting_sort(T* output, const T* input, u32 size) -> void {
  static_assert(Bits >= 1 && Bits <= 20, "must be between 1 and 20 bits");
  enum : u32 { Base = 1 << Bits, Mask = Base - 1 };

  u64 count[Base] = {}, last = 0;
  for(u32 n : range(size)) ++count[(input[n] >> Shift) & Mask];
  for(u32 n : range(Base)) last += count[n], count[n] = last - count[n];
  for(u32 n : range(size)) output[count[(input[n] >> Shift) & Mask]++] = input[n];
}

}
