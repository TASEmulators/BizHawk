#pragma once

#include <nall/arithmetic.hpp>
#include <nall/range.hpp>
#include <nall/string.hpp>

//cannot use constructor inheritance due to needing to call virtual reset();
//instead, define a macro to reduce boilerplate code in every Hash subclass
#define nallHash(Name) \
  Name() { reset(); } \
  Name(const void* data, u64 size) : Name() { input(data, size); } \
  Name(const vector<u8>& data) : Name() { input(data); } \
  Name(const string& data) : Name() { input(data); } \
  using Hash::input; \

namespace nall::Hash {

struct Hash {
  virtual auto reset() -> void = 0;
  virtual auto input(u8 data) -> void = 0;
  virtual auto output() const -> vector<u8> = 0;

  auto input(array_view<u8> data) -> void {
    for(auto byte : data) input(byte);
  }

  auto input(const void* data, u64 size) -> void {
    auto p = (const u8*)data;
    while(size--) input(*p++);
  }

  auto input(const vector<u8>& data) -> void {
    for(auto byte : data) input(byte);
  }

  auto input(const string& data) -> void {
    for(auto byte : data) input(byte);
  }

  auto digest() const -> string {
    string result;
    for(auto n : output()) result.append(hex(n, 2L));
    return result;
  }
};

}
