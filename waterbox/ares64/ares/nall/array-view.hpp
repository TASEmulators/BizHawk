#pragma once

#include <nall/iterator.hpp>
#include <nall/range.hpp>
#include <nall/traits.hpp>

namespace nall {

template<typename T> struct array_view {
  using type = array_view;

  array_view() {
    _data = nullptr;
    _size = 0;
  }

  array_view(nullptr_t) {
    _data = nullptr;
    _size = 0;
  }

  array_view(const void* data, u64 size) {
    _data = (const T*)data;
    _size = (s32)size;
  }

  template<s32 size> array_view(const T (&data)[size]) {
    _data = data;
    _size = size;
  }

  explicit operator bool() const {
    return _data && _size > 0;
  }

  explicit operator const T*() const {
    return _data;
  }

  const T& operator*() const {
    return *_data;
  }

  auto operator++() -> type& { _data++; _size--; return *this; }
  auto operator--() -> type& { _data--; _size++; return *this; }

  auto operator++(s32) -> type { auto copy = *this; ++(*this); return copy; }
  auto operator--(s32) -> type { auto copy = *this; --(*this); return copy; }

  auto operator-=(s32 distance) -> type& { _data -= distance; _size += distance; return *this; }
  auto operator+=(s32 distance) -> type& { _data += distance; _size -= distance; return *this; }

  auto operator[](u32 index) const -> const T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= _size) throw out_of_bounds{};
    #endif
    return _data[index];
  }

  auto operator()(u32 index, const T& fallback = {}) const -> T {
    if(index >= _size) return fallback;
    return _data[index];
  }

  template<typename U = T> auto data() const -> const U* { return (const U*)_data; }
  template<typename U = T> auto size() const -> u64 { return _size * sizeof(T) / sizeof(U); }

  auto begin() const -> iterator_const<T> { return {_data, (u32)0}; }
  auto end() const -> iterator_const<T> { return {_data, (u32)_size}; }

  auto rbegin() const -> reverse_iterator_const<T> { return {_data, (u32)_size - 1}; }
  auto rend() const -> reverse_iterator_const<T> { return {_data, (u32)-1}; }

  auto read() -> T {
    auto value = operator[](0);
    _data++;
    _size--;
    return value;
  }

  auto view(u32 offset, u32 length) const -> type {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(offset + length >= _size) throw out_of_bounds{};
    #endif
    return {_data + offset, length};
  }

  //array_view<u8> specializations
  template<typename U> auto readl(U& value, u32 size) -> U;
  template<typename U> auto readm(U& value, u32 size) -> U;
  template<typename U> auto readvn(U& value, u32 size) -> U;
  template<typename U> auto readvi(U& value, u32 size) -> U;

  template<typename U> auto readl(U& value, u32 offset, u32 size) -> U { return view(offset, size).readl(value, size); }

  template<typename U = u64> auto readl(u32 size) -> U { U value; return readl(value, size); }
  template<typename U = u64> auto readm(u32 size) -> U { U value; return readm(value, size); }
  template<typename U = u64> auto readvn(u32 size) -> U { U value; return readvn(value, size); }
  template<typename U = s64> auto readvi(u32 size) -> U { U value; return readvi(value, size); }

  template<typename U = u64> auto readl(u32 offset, u32 size) -> U { U value; return readl(value, offset, size); }

protected:
  const T* _data;
  s32 _size;
};

//array_view<u8>

template<> template<typename U> inline auto array_view<u8>::readl(U& value, u32 size) -> U {
  value = 0;
  for(u32 byte : range(size)) value |= (U)read() << byte * 8;
  return value;
}

template<> template<typename U> inline auto array_view<u8>::readm(U& value, u32 size) -> U {
  value = 0;
  for(u32 byte : reverse(range(size))) value |= (U)read() << byte * 8;
  return value;
}

template<> template<typename U> inline auto array_view<u8>::readvn(U& value, u32 size) -> U {
  value = 0;
  u32 shift = 1;
  while(true) {
    auto byte = read();
    value += (byte & 0x7f) * shift;
    if(byte & 0x80) break;
    shift <<= 7;
    value += shift;
  }
  return value;
}

template<> template<typename U> inline auto array_view<u8>::readvi(U& value, u32 size) -> U {
  value = readvn<U>();
  bool negate = value & 1;
  value >>= 1;
  if(negate) value = ~value;
  return value;
}

}
