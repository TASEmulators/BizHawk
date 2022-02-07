#pragma once

#include <nall/array-span.hpp>
#include <nall/array-view.hpp>
#include <nall/range.hpp>
#include <nall/view.hpp>

namespace nall {

template<typename T> struct array;

//usage: int x[256] => array<int[256]> x
template<typename T, uint Size> struct array<T[Size]> {
  array() = default;

  array(const initializer_list<T>& source) {
    uint index = 0;
    for(auto& value : source) {
      operator[](index++) = value;
    }
  }

  operator array_span<T>() {
    return {data(), size()};
  }

  operator array_view<T>() const {
    return {data(), size()};
  }

  alwaysinline auto operator[](uint index) -> T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= Size) throw out_of_bounds{};
    #endif
    return values[index];
  }

  alwaysinline auto operator[](uint index) const -> const T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= Size) throw out_of_bounds{};
    #endif
    return values[index];
  }

  alwaysinline auto operator()(uint index, const T& fallback = {}) const -> const T& {
    if(index >= Size) return fallback;
    return values[index];
  }

  auto fill(const T& fill = {}) -> array& {
    for(auto& value : values) value = fill;
    return *this;
  }

  auto data() -> T* { return values; }
  auto data() const -> const T* { return values; }
  auto size() const -> uint { return Size; }

  auto begin() -> T* { return &values[0]; }
  auto end() -> T* { return &values[Size]; }

  auto begin() const -> const T* { return &values[0]; }
  auto end() const -> const T* { return &values[Size]; }

private:
  T values[Size];
};

template<typename T, T... p> auto from_array(uint index) -> T {
  static const array<T[sizeof...(p)]> table{p...};
  struct out_of_bounds {};
  #if defined(DEBUG)
  if(index >= sizeof...(p)) throw out_of_bounds{};
  #endif
  return table[index];
}

template<int64_t... p> auto from_array(uint index) -> int64_t {
  static const array<int64_t[sizeof...(p)]> table{p...};
  struct out_of_bounds {};
  #if defined(DEBUG)
  if(index >= sizeof...(p)) throw out_of_bounds{};
  #endif
  return table[index];
}

}
