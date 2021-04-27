#pragma once

//burrows-wheeler transform

#include <nall/suffix-array.hpp>

namespace nall::Decode {

inline auto BWT(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;

  uint size = 0;
  for(uint byte : range(8)) size |= *input++ << byte * 8;
  output.resize(size);

  uint I = 0;
  for(uint byte : range(8)) I |= *input++ << byte * 8;

  auto suffixes = SuffixArray(input);

  auto L = input;
  auto F = new uint8_t[size];
  for(uint offset : range(size)) F[offset] = L[suffixes[offset + 1]];

  uint64_t K[256] = {};
  auto C = new int[size];
  for(uint i : range(size)) {
    C[i] = K[L[i]];
    K[L[i]]++;
  }

  int M[256];
  memory::fill<int>(M, 256, -1);
  for(uint i : range(size)) {
    if(M[F[i]] == -1) M[F[i]] = i;
  }

  uint i = I;
  for(uint j : reverse(range(size))) {
    output[j] = L[i];
    i = C[i] + M[L[i]];
  }

  return output;
}

}
