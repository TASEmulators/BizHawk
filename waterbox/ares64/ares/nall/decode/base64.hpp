#pragma once

namespace nall::Decode {

inline auto Base64(const string& text) -> vector<u8> {
  static bool initialized = false;
  static u8 lookup[256] = {};
  if(!initialized) {
    initialized = true;
    for(u32 n : range(26)) lookup['A' + n] = n;
    for(u32 n : range(26)) lookup['a' + n] = n + 26;
    for(u32 n : range(10)) lookup['0' + n] = n + 52;
    lookup['+'] = lookup['-'] = 62;
    lookup['/'] = lookup['_'] = 63;
  }

  vector<u8> result;
  u8 buffer = 0;
  u8 output = 0;
  for(u32 n : range(text.size())) {
    u8 buffer = lookup[text[n]];

    switch(n & 3) {
    case 0:
      output = buffer << 2;
      break;

    case 1:
      result.append(output | buffer >> 4);
      output = (buffer & 15) << 4;
      break;

    case 2:
      result.append(output | buffer >> 2);
      output = (buffer & 3) << 6;
      break;

    case 3:
      result.append(output | buffer);
      break;
    }
  }

  if(text.size() & 3) result.append(output | buffer);
  return result;
}

}
