#pragma once

#include <nall/arithmetic.hpp>

namespace nall::Decode {

template<uint Bits, typename T> inline auto Base(const string& value) -> T {
  static const string format =
    Bits ==  2 ? "01"
  : Bits ==  8 ? "01234567"
  : Bits == 10 ? "0123456789"
  : Bits == 16 ? "0123456789abcdef"
  : Bits == 32 ? "0123456789abcdefghijklmnopqrstuv"
  : Bits == 34 ? "023456789abcdefghijkmnopqrstuvwxyz"  //1l
  : Bits == 36 ? "0123456789abcdefghijklmnopqrstuvwxyz"
  : Bits == 57 ? "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"  //01IOl
  : Bits == 62 ? "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
  : Bits == 64 ? "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz{}"
  : Bits == 85 ? "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%()+,-.:;=@[]^_`{|}~"  //\ "&'*/<>?
  : "";
  static bool initialized = false;
  static uint8_t lookup[256] = {0};
  if(!initialized) {
    initialized = true;
    for(uint n : range(format.size())) {
      lookup[format[n]] = n;
    }
  }

  T result = 0;
  for(auto byte : value) {
    result = result * Bits + lookup[byte];
  }
  return result;
}

}
