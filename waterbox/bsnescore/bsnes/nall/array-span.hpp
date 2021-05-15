#pragma once

#include <nall/array-view.hpp>

namespace nall {

template<typename T> struct array_span : array_view<T> {
  using type = array_span;
  using super = array_view<T>;

  inline array_span() {
    super::_data = nullptr;
    super::_size = 0;
  }

  inline array_span(nullptr_t) {
    super::_data = nullptr;
    super::_size = 0;
  }

  inline array_span(void* data, uint64_t size) {
    super::_data = (T*)data;
    super::_size = (int)size;
  }

  inline operator T*() { return (T*)super::operator const T*(); }

  inline auto operator[](uint index) -> T& { return (T&)super::operator[](index); }

  template<typename U = T> inline auto data() -> U* { return (U*)super::_data; }

  inline auto begin() -> iterator<T> { return {(T*)super::_data, (uint)0}; }
  inline auto end() -> iterator<T> { return {(T*)super::_data, (uint)super::_size}; }

  inline auto rbegin() -> reverse_iterator<T> { return {(T*)super::_data, (uint)super::_size - 1}; }
  inline auto rend() -> reverse_iterator<T> { return {(T*)super::_data, (uint)-1}; }

  auto write(T value) -> void {
    operator[](0) = value;
    super::_data++;
    super::_size--;
  }

  auto span(uint offset, uint length) const -> type {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(offset + length >= super::_size) throw out_of_bounds{};
    #endif
    return {super::_data + offset, length};
  }

  //array_span<uint8_t> specializations
  template<typename U> auto writel(U value, uint size) -> void;
  template<typename U> auto writem(U value, uint size) -> void;
  template<typename U> auto writevn(U value, uint size) -> void;
  template<typename U> auto writevi(U value, uint size) -> void;
};

//array_span<uint8_t>

template<> inline auto array_span<uint8_t>::write(uint8_t value) -> void {
  operator[](0) = value;
  _data++;
  _size--;
}

template<> template<typename U> inline auto array_span<uint8_t>::writel(U value, uint size) -> void {
  for(uint byte : range(size)) write(value >> byte * 8);
}

template<> template<typename U> inline auto array_span<uint8_t>::writem(U value, uint size) -> void {
  for(uint byte : reverse(range(size))) write(value >> byte * 8);
}

template<> template<typename U> inline auto array_span<uint8_t>::writevn(U value, uint size) -> void {
  while(true) {
    auto byte = value & 0x7f;
    value >>= 7;
    if(value == 0) return write(0x80 | byte);
    write(byte);
    value--;
  }
}

template<> template<typename U> inline auto array_span<uint8_t>::writevi(U value, uint size) -> void {
  bool negate = value < 0;
  if(negate) value = ~value;
  value = value << 1 | negate;
  writevn(value);
}

}
