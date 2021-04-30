#pragma once

namespace nall {

struct range_t {
  struct iterator {
    iterator(int64_t position, int64_t step = 0) : position(position), step(step) {}
    auto operator*() const -> int64_t { return position; }
    auto operator!=(const iterator& source) const -> bool { return step > 0 ? position < source.position : position > source.position; }
    auto operator++() -> iterator& { position += step; return *this; }

  private:
    int64_t position;
    const int64_t step;
  };

  struct reverse_iterator {
    reverse_iterator(int64_t position, int64_t step = 0) : position(position), step(step) {}
    auto operator*() const -> int64_t { return position; }
    auto operator!=(const reverse_iterator& source) const -> bool { return step > 0 ? position > source.position : position < source.position; }
    auto operator++() -> reverse_iterator& { position -= step; return *this; }

  private:
    int64_t position;
    const int64_t step;
  };

  auto begin() const -> iterator { return {origin, stride}; }
  auto end() const -> iterator { return {target}; }

  auto rbegin() const -> reverse_iterator { return {target - stride, stride}; }
  auto rend() const -> reverse_iterator { return {origin - stride}; }

  int64_t origin;
  int64_t target;
  int64_t stride;
};

inline auto range(int64_t size) {
  return range_t{0, size, 1};
}

inline auto range(int64_t offset, int64_t size) {
  return range_t{offset, size, 1};
}

inline auto range(int64_t offset, int64_t size, int64_t step) {
  return range_t{offset, size, step};
}

}
