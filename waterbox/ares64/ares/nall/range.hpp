#pragma once

#include <nall/maybe.hpp>

namespace nall {

template<typename T = s64>
struct range_t {
  struct iterator {
    iterator(T position, T step = 0) : position(position), step(step) {}
    auto operator*() const -> T { return position; }
    auto operator!=(const iterator& source) const -> bool { return step > 0 ? position < source.position : position > source.position; }
    auto operator++() -> iterator& { position += step; return *this; }

  private:
    T position;
    const T step;
  };

  struct reverse_iterator {
    reverse_iterator(T position, T step = 0) : position(position), step(step) {}
    auto operator*() const -> T { return position; }
    auto operator!=(const reverse_iterator& source) const -> bool { return step > 0 ? position > source.position : position < source.position; }
    auto operator++() -> reverse_iterator& { position -= step; return *this; }

  private:
    T position;
    const T step;
  };

  auto begin() const -> iterator { return {origin, stride}; }
  auto end() const -> iterator { return {target}; }

  auto rbegin() const -> reverse_iterator { return {target - stride, stride}; }
  auto rend() const -> reverse_iterator { return {origin - stride}; }

  T origin;
  T target;
  T stride;
};

template<typename T = s64>
inline auto range(s64 size) {
  return range_t<T>{0, size, 1};
}

template<typename T = s64>
inline auto range(s64 offset, s64 size) {
  return range_t<T>{offset, size, 1};
}

template<typename T = s64>
inline auto range(s64 offset, s64 size, s64 step) {
  return range_t<T>{offset, size, step};
}

//returns true if {offset ... offset+length-1} is within {min ... max} in range {lo ... hi}
template<s64 lo, s64 hi>
inline auto within(s64 offset, s64 length, s64 min, s64 max) -> bool {
  static_assert(lo <= hi);
  static constexpr s64 range = hi - lo + 1;
  s64 lhs = (offset - lo) % range;
  s64 rhs = (offset + length - 1) % range;
  min = (min - lo) % range;
  max = (max - lo) % range;
  if(rhs < lhs) {
    return lhs <= max || rhs >= min;
  } else {
    return max >= lhs && min <= rhs;
  }
}

//returns index of target within {offset ... offset+length-1} in range {lo ... hi}
template<s64 lo, s64 hi>
inline auto within(s64 offset, s64 length, s64 target) -> maybe<u64> {
  static_assert(lo <= hi);
  static constexpr s64 range = hi - lo + 1;
  s64 start = (offset - lo) % range;
  s64 index = (target - lo) % range - start;
  if(index < 0) index += range;
  if(index < length) return index;
  return {};
}

}
