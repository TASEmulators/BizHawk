#pragma once

#include <nall/array.hpp>
#include <nall/counting-sort.hpp>
#include <nall/induced-sort.hpp>
#include <nall/range.hpp>
#include <nall/view.hpp>

namespace nall {

/*

input:
  data = "acaacatat"
  0 "acaacatat"
  1 "caacatat"
  2 "aacatat"
  3 "acatat"
  4 "catat"
  5 "atat"
  6 "tat"
  7 "at"
  8 "t"
  9 ""

suffix_array:
  suffixes = [9,2,0,3,7,5,1,4,8,6] => input + suffixes:
  9 ""
  2 "aacatat"
  0 "acaacatat"
  3 "acatat"
  7 "at"
  5 "atat"
  1 "caacatat"
  4 "catat"
  8 "t"
  6 "tat"

[auxiliary data structures to represent information lost from suffix trees]

suffix_array_invert:
  inverted = [2,6,1,3,7,5,9,4,8,0] => input + suffixes[inverted]:
  2 "acaacatat"
  6 "caacatat"
  1 "aacatat"
  3 "acatat"
  7 "catat"
  5 "atat"
  9 "tat"
  4 "at"
  8 "t"
  0 ""

suffix_array_phi:
  phi = [2,5,9,0,1,7,8,3,4,0]

suffix_array_lcp:
  prefixes = [0,0,1,3,1,2,0,2,0,1] => lcp[n] == lcp(n, n-1)
  ""          0
  "aacatat"   0
  "acaacatat" 1 "a"
  "acatat"    3 "aca"
  "at"        1 "a"
  "atat"      2 "at"
  "caacatat"  0
  "catat"     2 "ca"
  "t"         0
  "tat"       1 "t"

suffix_array_plcp:
  plcp = [1,0,0,3,2,2,1,1,0,0]

suffix_array_lrcp:
  llcp = [0,0,0,3,0,2,0,2,0,0] => llcp[m] == lcp(l, m)
  rlcp = [0,1,1,1,0,0,0,0,0,0] => rlcp[m] == lcp(m, r)

suffix_array_lpf:
  lengths = [0,0,1,3,2,1,0,2,1,0]
  offsets = [0,0,0,0,1,3,4,5,6,2]
  "acaacatat" (0,-)
   "caacatat" (0,-)
    "aacatat" (1,0) at 0, match "a"
     "acatat" (3,0) at 0, match "aca"
      "catat" (2,1) at 1, match "ca"
       "atat" (1,3) at 3, match "a"
        "tat" (0,-)
         "at" (2,5) at 5, match "at"
          "t" (1,6) at 6, match "t"
           "" (0,-)

*/

// suffix array via induced sorting
// O(n)
inline auto suffix_array(array_view<u8> input) -> vector<s32> {
  return induced_sort(input);
}

// inverse
// O(n)
inline auto suffix_array_invert(array_view<s32> sa) -> vector<s32> {
  vector<s32> isa;
  isa.reallocate(sa.size());
  for(s32 i : range(sa.size())) isa[sa[i]] = i;
  return isa;
}

// auxiliary data structure for plcp and lpf computation
// O(n)
inline auto suffix_array_phi(array_view<s32> sa) -> vector<s32> {
  vector<s32> phi;
  phi.reallocate(sa.size());
  phi[sa[0]] = 0;
  for(s32 i : range(1, sa.size())) phi[sa[i]] = sa[i - 1];
  return phi;
}

// longest common prefix: lcp(l, r)
// O(n)
inline auto suffix_array_lcp(s32 l, s32 r, array_view<s32> sa, array_view<u8> input) -> s32 {
  s32 i = sa[l], j = sa[r], k = 0, size = input.size();
  while(i + k < size && j + k < size && input[i + k] == input[j + k]) k++;
  return k;
}

// longest common prefix: lcp(i, j, k)
// O(n)
inline auto suffix_array_lcp(s32 i, s32 j, s32 k, array_view<u8> input) -> s32 {
  s32 size = input.size();
  while(i + k < size && j + k < size && input[i + k] == input[j + k]) k++;
  return k;
}

// longest common prefix: lcp[n] == lcp(n, n-1)
// O(n)
inline auto suffix_array_lcp(array_view<s32> sa, array_view<s32> isa, array_view<u8> input) -> vector<s32> {
  s32 k = 0, size = input.size();
  vector<s32> lcp;
  lcp.reallocate(size + 1);
  for(s32 i : range(size)) {
    if(isa[i] == size) { k = 0; continue; }  //the next substring is empty; ignore it
    s32 j = sa[isa[i] + 1];
    while(i + k < size && j + k < size && input[i + k] == input[j + k]) k++;
    lcp[1 + isa[i]] = k;
    if(k) k--;
  }
  lcp[0] = 0;
  return lcp;
}

// longest common prefix (from permuted longest common prefix)
// O(n)
inline auto suffix_array_lcp(array_view<s32> plcp, array_view<s32> sa) -> vector<s32> {
  vector<s32> lcp;
  lcp.reallocate(plcp.size());
  for(s32 i : range(plcp.size())) lcp[i] = plcp[sa[i]];
  return lcp;
}

// permuted longest common prefix
// O(n)
inline auto suffix_array_plcp(array_view<s32> phi, array_view<u8> input) -> vector<s32> {
  vector<s32> plcp;
  plcp.reallocate(phi.size());
  s32 k = 0, size = input.size();
  for(s32 i : range(size)) {
    s32 j = phi[i];
    while(i + k < size && j + k < size && input[i + k] == input[j + k]) k++;
    plcp[i] = k;
    if(k) k--;
  }
  return plcp;
}

// permuted longest common prefix (from longest common prefix)
// O(n)
inline auto suffix_array_plcp(array_view<s32> lcp, array_view<s32> sa) -> vector<s32> {
  vector<s32> plcp;
  plcp.reallocate(lcp.size());
  for(s32 i : range(lcp.size())) plcp[sa[i]] = lcp[i];
  return plcp;
}

// longest common prefixes - left + right
// llcp[m] == lcp(l, m)
// rlcp[m] == lcp(m, r)
// O(n)
// requires: lcp -or- plcp+sa
inline auto suffix_array_lrcp(vector<s32>& llcp, vector<s32>& rlcp, array_view<s32> lcp, array_view<s32> plcp, array_view<s32> sa, array_view<u8> input) -> void {
  s32 size = input.size();
  llcp.reset(), llcp.reallocate(size + 1);
  rlcp.reset(), rlcp.reallocate(size + 1);

  function<s32 (s32, s32)> recurse = [&](s32 l, s32 r) -> s32 {
    if(l >= r - 1) {
      if(l >= size) return 0;
      if(lcp) return lcp[l];
      return plcp[sa[l]];
    }
    s32 m = l + r >> 1;
    llcp[m - 1] = recurse(l, m);
    rlcp[m - 1] = recurse(m, r);
    return min(llcp[m - 1], rlcp[m - 1]);
  };
  recurse(1, size + 1);

  llcp[0] = 0;
  rlcp[0] = 0;
}

// longest previous factor
// O(n)
// optional: plcp
inline auto suffix_array_lpf(vector<s32>& lengths, vector<s32>& offsets, array_view<s32> phi, array_view<s32> plcp, array_view<u8> input) -> void {
  s32 k = 0, size = input.size();
  lengths.reset(), lengths.resize(size + 1, -1);
  offsets.reset(), offsets.resize(size + 1, -1);

  function<void (s32, s32, s32)> recurse = [&](s32 i, s32 j, s32 k) -> void {
    if(lengths[i] < 0) {
      lengths[i] = k;
      offsets[i] = j;
    } else if(lengths[i] < k) {
      if(offsets[i] > j) {
        recurse(offsets[i], j, lengths[i]);
      } else {
        recurse(j, offsets[i], lengths[i]);
      }
      lengths[i] = k;
      offsets[i] = j;
    } else {
      if(offsets[i] > j) {
        recurse(offsets[i], j, k);
      } else {
        recurse(j, offsets[i], k);
      }
    }
  };

  for(s32 i : range(size)) {
    s32 j = phi[i];
    if(plcp) k = plcp[i];
    else while(i + k < size && j + k < size && input[i + k] == input[j + k]) k++;
    if(i > j) {
      recurse(i, j, k);
    } else {
      recurse(j, i, k);
    }
    if(k) k--;
  }

  lengths[0] = 0;
  offsets[0] = 0;
}

// O(n log m)
inline auto suffix_array_find(s32& length, s32& offset, array_view<s32> sa, array_view<u8> input, array_view<u8> match) -> bool {
  length = 0, offset = 0;
  s32 l = 0, r = input.size();

  while(l < r - 1) {
    s32 m = l + r >> 1;
    s32 s = sa[m];

    s32 k = 0;
    while(k < match.size() && s + k < input.size()) {
      if(match[k] != input[s + k]) break;
      k++;
    }

    if(k > length) {
      length = k;
      offset = s;
      if(k == match.size()) return true;
    }

    if(k == match.size() || s + k == input.size()) k--;

    if(match[k] < input[s + k]) {
      r = m;
    } else {
      l = m;
    }
  }

  return false;
}

// O(n + log m)
inline auto suffix_array_find(s32& length, s32& offset, array_view<s32> llcp, array_view<s32> rlcp, array_view<s32> sa, array_view<u8> input, array_view<u8> match) -> bool {
  length = 0, offset = 0;
  s32 l = 0, r = input.size(), k = 0;

  while(l < r - 1) {
    s32 m = l + r >> 1;
    s32 s = sa[m];

    while(k < match.size() && s + k < input.size()) {
      if(match[k] != input[s + k]) break;
      k++;
    }

    if(k > length) {
      length = k;
      offset = s;
      if(k == match.size()) return true;
    }

    if(k == match.size() || s + k == input.size()) k--;

    if(match[k] < input[s + k]) {
      r = m;
      k = min(k, llcp[m]);
    } else {
      l = m;
      k = min(k, rlcp[m]);
    }
  }

  return false;
}

//

//there are multiple strategies for building the required auxiliary structures for suffix arrays

struct SuffixArray {
  using type = SuffixArray;

  //O(n)
  SuffixArray(array_view<u8> input) : input(input) {
    sa = suffix_array(input);
  }

  //O(n)
  auto lrcp() -> type& {
  //if(!isa) isa = suffix_array_invert(sa);
  //if(!lcp) lcp = suffix_array_lcp(sa, isa, input);
    if(!phi) phi = suffix_array_phi(sa);
    if(!plcp) plcp = suffix_array_plcp(phi, input);
  //if(!lcp) lcp = suffix_array_lcp(plcp, sa);
    if(!llcp || !rlcp) suffix_array_lrcp(llcp, rlcp, lcp, plcp, sa, input);
    return *this;
  }

  //O(n)
  auto lpf() -> type& {
    if(!phi) phi = suffix_array_phi(sa);
  //if(!plcp) plcp = suffix_array_plcp(phi, input);
    if(!lengths || !offsets) suffix_array_lpf(lengths, offsets, phi, plcp, input);
    return *this;
  }

  auto operator[](s32 offset) const -> s32 {
    return sa[offset];
  }

  //O(n log m)
  //O(n + log m) with lrcp()
  auto find(s32& length, s32& offset, array_view<u8> match) -> bool {
    if(!llcp || !rlcp) return suffix_array_find(length, offset, sa, input, match);  //O(n log m)
    return suffix_array_find(length, offset, llcp, rlcp, sa, input, match);  //O(n + log m)
  }

  //O(n) with lpf()
  auto previous(s32& length, s32& offset, s32 address) -> void {
    length = lengths[address];
    offset = offsets[address];
  }

  //non-owning reference: SuffixArray is invalidated if memory is freed
  array_view<u8> input;

  //suffix array and auxiliary data structures
  vector<s32> sa;       //suffix array
  vector<s32> isa;      //inverted suffix array
  vector<s32> phi;      //phi
  vector<s32> plcp;     //permuted longest common prefixes
  vector<s32> lcp;      //longest common prefixes
  vector<s32> llcp;     //longest common prefixes - left
  vector<s32> rlcp;     //longest common prefixes - right
  vector<s32> lengths;  //longest previous factors
  vector<s32> offsets;  //longest previous factors
};

}
