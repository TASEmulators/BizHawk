#pragma once

#include <new>

#include <nall/array-span.hpp>
#include <nall/array-view.hpp>
#include <nall/bit.hpp>
#include <nall/function.hpp>
#include <nall/iterator.hpp>
#include <nall/maybe.hpp>
#include <nall/memory.hpp>
#include <nall/merge-sort.hpp>
#include <nall/range.hpp>
#include <nall/traits.hpp>
#include <nall/view.hpp>

namespace nall {

template<typename T>
struct vector_base {
  using type = vector_base;

  //core.hpp
  vector_base() = default;
  vector_base(const initializer_list<T>& values);
  vector_base(const type& source);
  vector_base(type&& source);
  ~vector_base();

  explicit operator bool() const;
  operator array_span<T>();
  operator array_view<T>() const;
  template<typename Cast = T> auto capacity() const -> uint64_t;
  template<typename Cast = T> auto size() const -> uint64_t;
  template<typename Cast = T> auto data() -> Cast*;
  template<typename Cast = T> auto data() const -> const Cast*;

  //assign.hpp
  auto operator=(const type& source) -> type&;
  auto operator=(type&& source) -> type&;

  //compare.hpp
  auto operator==(const type& source) const -> bool;
  auto operator!=(const type& source) const -> bool;

  //memory.hpp
  auto reset() -> void;
  auto acquire(const T* data, uint64_t size, uint64_t capacity = 0) -> void;
  auto release() -> T*;

  auto reserveLeft(uint64_t capacity) -> bool;
  auto reserveRight(uint64_t capacity) -> bool;
  auto reserve(uint64_t capacity) -> bool { return reserveRight(capacity); }

  auto reallocateLeft(uint64_t size) -> bool;
  auto reallocateRight(uint64_t size) -> bool;
  auto reallocate(uint64_t size) -> bool { return reallocateRight(size); }

  auto resizeLeft(uint64_t size, const T& value = T()) -> bool;
  auto resizeRight(uint64_t size, const T& value = T()) -> bool;
  auto resize(uint64_t size, const T& value = T()) -> bool { return resizeRight(size, value); }

  //access.hpp
  alwaysinline auto operator[](uint64_t offset) -> T&;
  alwaysinline auto operator[](uint64_t offset) const -> const T&;

  alwaysinline auto operator()(uint64_t offset) -> T&;
  alwaysinline auto operator()(uint64_t offset, const T& value) const -> const T&;

  alwaysinline auto left() -> T&;
  alwaysinline auto first() -> T& { return left(); }
  alwaysinline auto left() const -> const T&;
  alwaysinline auto first() const -> const T& { return left(); }

  alwaysinline auto right() -> T&;
  alwaysinline auto last() -> T& { return right(); }
  alwaysinline auto right() const -> const T&;
  alwaysinline auto last() const -> const T& { return right(); }

  //modify.hpp
  auto prepend(const T& value) -> void;
  auto prepend(T&& value) -> void;
  auto prepend(const type& values) -> void;
  auto prepend(type&& values) -> void;

  auto append(const T& value) -> void;
  auto append(T&& value) -> void;
  auto append(const type& values) -> void;
  auto append(type&& values) -> void;

  auto insert(uint64_t offset, const T& value) -> void;

  auto removeLeft(uint64_t length = 1) -> void;
  auto removeFirst(uint64_t length = 1) -> void { return removeLeft(length); }
  auto removeRight(uint64_t length = 1) -> void;
  auto removeLast(uint64_t length = 1) -> void { return removeRight(length); }
  auto remove(uint64_t offset, uint64_t length = 1) -> void;
  auto removeByIndex(uint64_t offset) -> bool;
  auto removeByValue(const T& value) -> bool;

  auto takeLeft() -> T;
  auto takeFirst() -> T { return move(takeLeft()); }
  auto takeRight() -> T;
  auto takeLast() -> T { return move(takeRight()); }
  auto take(uint64_t offset) -> T;

  //iterator.hpp
  auto begin() -> iterator<T> { return {data(), 0}; }
  auto end() -> iterator<T> { return {data(), size()}; }

  auto begin() const -> iterator_const<T> { return {data(), 0}; }
  auto end() const -> iterator_const<T> { return {data(), size()}; }

  auto rbegin() -> reverse_iterator<T> { return {data(), size() - 1}; }
  auto rend() -> reverse_iterator<T> { return {data(), (uint64_t)-1}; }

  auto rbegin() const -> reverse_iterator_const<T> { return {data(), size() - 1}; }
  auto rend() const -> reverse_iterator_const<T> { return {data(), (uint64_t)-1}; }

  //utility.hpp
  auto fill(const T& value = {}) -> void;
  auto sort(const function<bool (const T& lhs, const T& rhs)>& comparator = [](auto& lhs, auto& rhs) { return lhs < rhs; }) -> void;
  auto reverse() -> void;
  auto find(const function<bool (const T& lhs)>& comparator) -> maybe<uint64_t>;
  auto find(const T& value) const -> maybe<uint64_t>;
  auto findSorted(const T& value) const -> maybe<uint64_t>;
  auto foreach(const function<void (const T&)>& callback) -> void;
  auto foreach(const function<void (uint, const T&)>& callback) -> void;

protected:
  T* _pool = nullptr;   //pointer to first initialized element in pool
  uint64_t _size = 0;   //number of initialized elements in pool
  uint64_t _left = 0;   //number of allocated elements free on the left of pool
  uint64_t _right = 0;  //number of allocated elements free on the right of pool
};

}

#define vector vector_base
#include <nall/vector/core.hpp>
#include <nall/vector/assign.hpp>
#include <nall/vector/compare.hpp>
#include <nall/vector/memory.hpp>
#include <nall/vector/access.hpp>
#include <nall/vector/modify.hpp>
#include <nall/vector/iterator.hpp>
#include <nall/vector/utility.hpp>
#undef vector

namespace nall {
  template<typename T> struct vector : vector_base<T> {
    using vector_base<T>::vector_base;
  };
}

#include <nall/vector/specialization/uint8_t.hpp>
