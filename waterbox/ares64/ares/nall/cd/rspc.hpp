#pragma once

//reed-solomon product code

namespace nall::CD::RSPC {

inline auto encodeP(array_view<u8> input, array_span<u8> parity) -> bool {
  ReedSolomon<26,24> s;
  u32 lo = 0, hi = 43 * 2;
  for(u32 x : range(43)) {
    for(u32 w : range(2)) {  //16-bit words
      u32 z = 0;
      for(u32 y : range(24)) {
        s[z++] = input[(y * 43 + x) * 2 + w];
      }
      s.generateParity();
      parity[lo++] = s[z++];
      parity[hi++] = s[z++];
    }
  }
  return true;
}

inline auto encodeQ(array_view<u8> input, array_span<u8> parity) -> bool {
  ReedSolomon<45,43> s;
  u32 lo = 0, hi = 26 * 2;
  for(u32 y : range(26)) {
    for(u32 w : range(2)) {
      u32 z = 0;
      for(u32 x : range(43)) {
        s[z++] = input[((x * 44 + y * 43) * 2 + w) % (26 * 43 * 2)];
      }
      s.generateParity();
      parity[lo++] = s[z++];
      parity[hi++] = s[z++];
    }
  }
  return true;
}

inline auto encodeMode1(array_span<u8> sector) -> bool {
  if(sector.size() != 2352) return false;
  if(!encodeP({sector.data() + 12, 2064}, {sector.data() + 2076, 172})) return false;
  if(!encodeQ({sector.data() + 12, 2236}, {sector.data() + 2248, 104})) return false;
  return true;
}

//

inline auto decodeP(array_span<u8> input, array_span<u8> parity) -> s32 {
  bool success = false;
  bool failure = false;
  ReedSolomon<26,24> s;
  u32 lo = 0, hi = 43 * 2;
  for(u32 x : range(43)) {
    for(u32 w : range(2)) {
      u32 z = 0;
      for(u32 y : range(24)) {
        s[z++] = input[(y * 43 + x) * 2 + w];
      }
      s[z++] = parity[lo++];
      s[z++] = parity[hi++];
      auto count = s.correctErrors();
      if(count < 0) {
        failure = true;
      }
      if(count > 0) {
        success = true;
        z = 0;
        for(u32 y : range(24)) {
          input[(y * 43 + x) * 2 + w] = s[z++];
        }
        parity[lo - 1] = s[z++];
        parity[hi - 1] = s[z++];
      }
    }
  }
  if(!success && !failure) return 0;  //no errors remaining
  return success ? 1 : -1;  //return success even if there are some failures
}

inline auto decodeQ(array_span<u8> input, array_span<u8> parity) -> s32 {
  bool success = false;
  bool failure = false;
  ReedSolomon<45,43> s;
  u32 lo = 0, hi = 26 * 2;
  for(u32 y : range(26)) {
    for(u32 w : range(2)) {
      u32 z = 0;
      for(u32 x : range(43)) {
        s[z++] = input[((x * 44 + y * 43) * 2 + w) % (26 * 43 * 2)];
      }
      s[z++] = parity[lo++];
      s[z++] = parity[hi++];
      auto count = s.correctErrors();
      if(count < 0) {
        failure = true;
      }
      if(count > 0) {
        success = true;
        z = 0;
        for(u32 x : range(43)) {
          input[((x * 44 + y * 43) * 2 + w) % (26 * 43 * 2)] = s[z++];
        }
        parity[lo - 1] = s[z++];
        parity[hi - 1] = s[z++];
      }
    }
  }
  if(!success && !failure) return 0;
  return success ? 1 : -1;
}

inline auto decodeMode1(array_span<u8> sector) -> bool {
  if(sector.size() != 2352) return false;
  //P corrections can allow Q corrections that previously failed to succeed, and vice versa.
  //the more iterations, the more chances to correct errors, but the more computationally expensive it is.
  //there must be a limit on the amount of retries, or this function may get stuck in an infinite loop.
  for(u32 attempt : range(4)) {
    auto p = decodeP({sector.data() + 12, 2064}, {sector.data() + 2076, 172});
    auto q = decodeQ({sector.data() + 12, 2236}, {sector.data() + 2248, 104});
    if(p == 0 && q == 0) return true;   //no errors remaining
    if(p <  0 && q <  0) return false;  //no more errors correctable
  }
  return false;  //exhausted all retries with errors remaining
}

}
