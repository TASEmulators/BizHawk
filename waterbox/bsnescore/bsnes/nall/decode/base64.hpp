#pragma once

namespace nall::Decode {

inline auto Base64(const string& text) -> vector<uint8_t> {
  static bool initialized = false;
  static uint8_t lookup[256] = {0};
  if(!initialized) {
    initialized = true;
    for(uint n : range(26)) lookup['A' + n] = n;
    for(uint n : range(26)) lookup['a' + n] = n + 26;
    for(uint n : range(10)) lookup['0' + n] = n + 52;
    lookup['+'] = lookup['-'] = 62;
    lookup['/'] = lookup['_'] = 63;
  }

  vector<uint8_t> result;
  uint8_t buffer, output;
  for(uint n : range(text.size())) {
    uint8_t buffer = lookup[text[n]];

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
