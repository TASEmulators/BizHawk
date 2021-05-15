#pragma once

//burrows-wheeler transform

#include <nall/suffix-array.hpp>

namespace nall::Encode {

/*
  A standard suffix array cannot produce a proper burrows-wheeler transform, due to rotations.

  Take the input string, "nall", this gives us:
    nall
    alln
    llna
    lnal

  If we suffix sort this, we produce:
    all  => alln
    l    => lnal
    ll   => llna
    nall => nall

  If we sort this, we produce:
    alln
    llna
    lnal
    nall

  Thus, suffix sorting gives us "nlal" as the last column instead of "nall".
  This is because BWT rotates the input string, whereas suffix arrays sort the input string.

  Adding a 256th character terminator before sorting will not produce the desired result, either.
  A more complicated string such as "mississippi" will sort as "ssmppissiii" with terminator=256,
  and as "ipssmpissii" with terminator=0, alphabet=1..256, whereas we want "pssmipissii".

  Performing a merge sort to use a specialized comparison function that wraps suffixes is too slow at O(n log n).

  Producing a custom induced sort to handle rotations would be incredibly complicated,
  owing to the recursive nature of induced sorting, among other things.

  So instead, a temporary array is produced that contains the input suffix twice.
  This is then fed into the suffix array sort, and the doubled matches are filtered out.
  After this point, suffixes are sorted in their mirrored form, and the correct result can be derived

  The result of this is an O(2n) algorithm, which vastly outperforms a naive O(n log n) algorithm,
  but is still far from ideal. However, this will have to do until a better solution is devised.

  Although to be fair, BWT is inferior to the bijective BWT anyway, so it may not be worth the effort.
*/

inline auto BWT(array_view<uint8_t> input) -> vector<uint8_t> {
  auto size = input.size();
  vector<uint8_t> output;
  output.reserve(8 + 8 + size);
  for(uint byte : range(8)) output.append(size >> byte * 8);
  for(uint byte : range(8)) output.append(0x00);

  vector<uint8_t> buffer;
  buffer.reserve(2 * size);
  for(uint offset : range(size)) buffer.append(input[offset]);
  for(uint offset : range(size)) buffer.append(input[offset]);

  auto suffixes = SuffixArray(buffer);

  vector<int> prefixes;
  prefixes.reserve(size);

  for(uint offset : range(2 * size + 1)) {
    uint suffix = suffixes[offset];
    if(suffix >= size) continue;  //beyond the bounds of the original input string
    prefixes.append(suffix);
  }

  uint64_t root = 0;
  for(uint offset : range(size)) {
    uint suffix = prefixes[offset];
    if(suffix == 0) root = offset, suffix = size;
    output.append(input[--suffix]);
  }
  for(uint byte : range(8)) output[8 + byte] = root >> byte * 8;

  return output;
}

}
