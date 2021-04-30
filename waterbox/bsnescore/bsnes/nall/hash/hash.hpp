#pragma once

#include <nall/arithmetic.hpp>
#include <nall/range.hpp>
#include <nall/string.hpp>

//cannot use constructor inheritance due to needing to call virtual reset();
//instead, define a macro to reduce boilerplate code in every Hash subclass
#define nallHash(Name) \
  Name() { reset(); } \
  Name(const void* data, uint64_t size) : Name() { input(data, size); } \
  Name(const vector<uint8_t>& data) : Name() { input(data); } \
  Name(const string& data) : Name() { input(data); } \
  using Hash::input; \

namespace nall::Hash {

struct Hash {
  virtual auto reset() -> void = 0;
  virtual auto input(uint8_t data) -> void = 0;
  virtual auto output() const -> vector<uint8_t> = 0;

  auto input(array_view<uint8_t> data) -> void {
    for(auto byte : data) input(byte);
  }

  auto input(const void* data, uint64_t size) -> void {
    auto p = (const uint8_t*)data;
    while(size--) input(*p++);
  }

  auto input(const vector<uint8_t>& data) -> void {
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
