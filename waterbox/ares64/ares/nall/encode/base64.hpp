#pragma once

namespace nall::Encode {

inline auto Base64(const void* vdata, u32 size, const string& format = "MIME") -> string {
  static bool initialized = false;
  static char lookup[65] = {};
  if(!initialized) {
    initialized = true;
    for(u32 n : range(26)) lookup[n +  0] = 'A' + n;
    for(u32 n : range(26)) lookup[n + 26] = 'a' + n;
    for(u32 n : range(10)) lookup[n + 52] = '0' + n;
  }

  if(format == "MIME") {
    lookup[62] = '+';
    lookup[63] = '/';
    lookup[64] = '=';
  } else if(format == "URI") {
    lookup[62] = '-';
    lookup[63] = '_';
    lookup[64] = 0;
  } else return "";

  auto data = (const u8*)vdata;
  u32 overflow = (3 - (size % 3)) % 3;  //bytes to round to nearest multiple of 3
  string result;
  u8 buffer = 0;
  for(u32 n : range(size)) {
    switch(n % 3) {
    case 0:
      buffer = data[n] >> 2;
      result.append(lookup[buffer]);
      buffer = (data[n] & 3) << 4;
      break;

    case 1:
      buffer |= data[n] >> 4;
      result.append(lookup[buffer]);
      buffer = (data[n] & 15) << 2;
      break;

    case 2:
      buffer |= data[n] >> 6;
      result.append(lookup[buffer]);
      buffer = (data[n] & 63);
      result.append(lookup[buffer]);
      break;
    }
  }

  if(overflow) result.append(lookup[buffer]);
  if(lookup[64]) {
    while(result.size() % 4) result.append(lookup[64]);
  }

  return result;
}

inline auto Base64(const vector<u8>& buffer, const string& format = "MIME") -> string {
  return Base64(buffer.data(), buffer.size(), format);
}

inline auto Base64(const string& text, const string& format = "MIME") -> string {
  return Base64(text.data(), text.size(), format);
}

}
