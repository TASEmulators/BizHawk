//deprecated

#pragma once

#include <nall/range.hpp>
#include <nall/traits.hpp>

namespace nall {

template<typename T, uint Capacity>
struct adaptive_array {
  auto capacity() const -> uint { return Capacity; }
  auto size() const -> uint { return _size; }

  auto reset() -> void {
    for(uint n : range(_size)) _pool.t[n].~T();
    _size = 0;
  }

  auto operator[](uint index) -> T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= Capacity) throw out_of_bounds{};
    #endif
    return _pool.t[index];
  }

  auto operator[](uint index) const -> const T& {
    #ifdef DEBUG
    struct out_of_bounds {};
    if(index >= Capacity) throw out_of_bounds{};
    #endif
    return _pool.t[index];
  }

  auto append() -> T& {
    new(_pool.t + _size) T;
    return _pool.t[_size++];
  }

  auto append(const T& value) -> void {
    new(_pool.t + _size++) T(value);
  }

  auto append(T&& value) -> void {
    new(_pool.t + _size++) T(move(value));
  }

  auto begin() { return &_pool.t[0]; }
  auto end() { return &_pool.t[_size]; }

  auto begin() const { return &_pool.t[0]; }
  auto end() const { return &_pool.t[_size]; }

private:
  union U {
    U() {}
    ~U() {}
    T t[Capacity];
  } _pool;
  uint _size = 0;
};

}
