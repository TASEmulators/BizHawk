#pragma once

#include <nall/array-view.hpp>

namespace nall {

template<typename T> struct array_span : array_view<T> {
  using type = array_span;
  using super = array_view<T>;

  array_span() {
    super::_data = nullptr;
    super::_size = 0;
  }

  array_span(nullptr_t) {
    super::_data = nullptr;
    super::_size = 0;
  }

  array_span(void* data, u64 size) {
    super::_data = (T*)data;
    super::_size = (s32)size;
  }

  template<s32 size> array_span(T (&data)[size]) {
    super::_data = data;
    super::_size = size;
  }

  explicit operator bool() const {
    return super::_data && super::_size > 0;
  }

  explicit operator T*() {
    return (T*)super::_data;
  }

  T& operator*() const {
    return (T&)*super::_data;
  }

  auto operator++() -> type& { super::_data++; super::_size--; return *this; }
  auto operator--() -> type& { super::_data--; super::_size++; return *this; }

  auto operator++(s32) -> type { auto copy = *this; ++(*this); return copy; }
  auto operator--(s32) -> type { auto copy = *this; --(*this); return copy; }

  auto operator[](u32 index) -> T& { return (T&)super::operator[](index); }

  template<typename U = T> auto data() -> U* { return (U*)super::_data; }
  template<typename U = T> auto data() const -> const U* { return (const U*)super::_data; }

  auto begin() -> iterator<T> { return {(T*)super::_data, (u32)0}; }
  auto end() -> iterator<T> { return {(T*)super::_data, (u32)super::_size}; }

  auto rbegin() -> reverse_iterator<T> { return {(T*)super::_data, (u32)super::_size - 1}; }
  auto rend() -> reverse_iterator<T> { return {(T*)super::_data, (u32)-1}; }

  auto write(T value) -> void {
    operator[](0) = value;
    super::_data++;
    super::_size--;
  }

  auto span(u32 offset, u32 length) -> type {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(offset + length >= super::_size) throw out_of_bounds{};
    #endif
    return {(T*)super::_data + offset, length};
  }

  //array_span<u8> specializations
  template<typename U> auto writel(U value, u32 size) -> void;
  template<typename U> auto writem(U value, u32 size) -> void;
  template<typename U> auto writevn(U value, u32 size) -> void;
  template<typename U> auto writevi(U value, u32 size) -> void;
};

//array_span<u8>

template<> inline auto array_span<u8>::write(u8 value) -> void {
  operator[](0) = value;
  _data++;
  _size--;
}

template<> template<typename U> inline auto array_span<u8>::writel(U value, u32 size) -> void {
  for(u32 byte : range(size)) write(value >> byte * 8);
}

template<> template<typename U> inline auto array_span<u8>::writem(U value, u32 size) -> void {
  for(u32 byte : reverse(range(size))) write(value >> byte * 8);
}

template<> template<typename U> inline auto array_span<u8>::writevn(U value, u32 size) -> void {
  while(true) {
    auto byte = value & 0x7f;
    value >>= 7;
    if(value == 0) return write(0x80 | byte);
    write(byte);
    value--;
  }
}

template<> template<typename U> inline auto array_span<u8>::writevi(U value, u32 size) -> void {
  bool negate = value < 0;
  if(negate) value = ~value;
  value = value << 1 | negate;
  writevn(value);
}

}
