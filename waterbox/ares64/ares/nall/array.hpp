#pragma once

#include <nall/array-span.hpp>
#include <nall/array-view.hpp>
#include <nall/range.hpp>
#include <nall/view.hpp>

namespace nall {

template<typename T> struct array;

//usage: s32 x[256] => array<s32[256]> x
template<typename T, u32 Size> struct array<T[Size]> {
  array() = default;

  array(const initializer_list<T>& source) {
    u32 index = 0;
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

  alwaysinline auto operator[](u32 index) -> T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= Size) throw out_of_bounds{};
    #endif
    return values[index];
  }

  alwaysinline auto operator[](u32 index) const -> const T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= Size) throw out_of_bounds{};
    #endif
    return values[index];
  }

  alwaysinline auto operator()(u32 index, const T& fallback = {}) const -> const T& {
    if(index >= Size) return fallback;
    return values[index];
  }

  auto fill(const T& fill = {}) -> array& {
    for(auto& value : values) value = fill;
    return *this;
  }

  auto data() -> T* { return values; }
  auto data() const -> const T* { return values; }
  auto size() const -> u32 { return Size; }

  auto begin() -> T* { return &values[0]; }
  auto end() -> T* { return &values[Size]; }

  auto begin() const -> const T* { return &values[0]; }
  auto end() const -> const T* { return &values[Size]; }

private:
  T values[Size];
};

template<typename T, T... p> inline auto from_array(u32 index) -> T {
  static const array<T[sizeof...(p)]> table{p...};
  struct out_of_bounds {};
  #if defined(DEBUG)
  if(index >= sizeof...(p)) throw out_of_bounds{};
  #endif
  return table[index];
}

template<s64... p> inline auto from_array(u32 index) -> s64 {
  static const array<s64[sizeof...(p)]> table{p...};
  struct out_of_bounds {};
  #if defined(DEBUG)
  if(index >= sizeof...(p)) throw out_of_bounds{};
  #endif
  return table[index];
}

}
