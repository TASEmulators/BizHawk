#pragma once

//burrows-wheeler transform

#include <nall/suffix-array.hpp>

namespace nall::Decode {

inline auto BWT(array_view<u8> input) -> vector<u8> {
  vector<u8> output;

  u32 size = 0;
  for(u32 byte : range(8)) size |= *input++ << byte * 8;
  output.resize(size);

  u32 I = 0;
  for(u32 byte : range(8)) I |= *input++ << byte * 8;

  auto suffixes = SuffixArray(input);

  auto L = input;
  auto F = new u8[size];
  for(u32 offset : range(size)) F[offset] = L[suffixes[offset + 1]];

  u64 K[256] = {};
  auto C = new s32[size];
  for(u32 i : range(size)) {
    C[i] = K[L[i]];
    K[L[i]]++;
  }

  s32 M[256];
  memory::fill<s32>(M, 256, -1);
  for(u32 i : range(size)) {
    if(M[F[i]] == -1) M[F[i]] = i;
  }

  u32 i = I;
  for(u32 j : reverse(range(size))) {
    output[j] = L[i];
    i = C[i] + M[L[i]];
  }

  return output;
}

}
