#pragma once

namespace nall {

template<> struct vector<uint8_t> : vector_base<uint8_t> {
  using type = vector<uint8_t>;
  using vector_base<uint8_t>::vector_base;

  template<typename U> auto appendl(U value, uint size) -> void {
    for(uint byte : range(size)) append(uint8_t(value >> byte * 8));
  }

  template<typename U> auto appendm(U value, uint size) -> void {
    for(uint byte : nall::reverse(range(size))) append(uint8_t(value >> byte * 8));
  }

  //note: string_view is not declared here yet ...
  auto appends(array_view<uint8_t> memory) -> void {
    for(uint8_t byte : memory) append(byte);
  }

  template<typename U> auto readl(int offset, uint size) -> U {
    if(offset < 0) offset = this->size() - abs(offset);
    U value = 0;
    for(uint byte : range(size)) value |= (U)operator[](offset + byte) << byte * 8;
    return value;
  }

  auto view(uint offset, uint length) -> array_view<uint8_t> {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(offset + length >= size()) throw out_of_bounds{};
    #endif
    return {data() + offset, length};
  }
};

}
