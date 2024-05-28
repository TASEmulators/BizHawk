#pragma once

namespace nall {

template<> struct vector<u8> : vector_base<u8> {
  using type = vector<u8>;
  using vector_base<u8>::vector_base;

  template<typename U> auto appendl(U value, u32 size) -> void {
    for(u32 byte : range(size)) append(u8(value >> byte * 8));
  }

  template<typename U> auto appendm(U value, u32 size) -> void {
    for(u32 byte : nall::reverse(range(size))) append(u8(value >> byte * 8));
  }

  //note: string_view is not declared here yet ...
  auto appends(array_view<u8> memory) -> void {
    for(u8 byte : memory) append(byte);
  }

  template<typename U> auto readl(s32 offset, u32 size) -> U {
    if(offset < 0) offset = this->size() - abs(offset);
    U value = 0;
    for(u32 byte : range(size)) value |= (U)operator[](offset + byte) << byte * 8;
    return value;
  }

  auto view(u32 offset, u32 length) -> array_view<u8> {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(offset + length >= size()) throw out_of_bounds{};
    #endif
    return {data() + offset, length};
  }
};

}
