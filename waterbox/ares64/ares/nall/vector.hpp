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
  template<typename Cast = T> auto capacity() const -> u64;
  template<typename Cast = T> auto size() const -> u64;
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
  auto acquire(T* data, u64 size, u64 capacity = 0) -> void;
  auto release() -> T*;

  auto reserveLeft(u64 capacity) -> bool;
  auto reserveRight(u64 capacity) -> bool;
  auto reserve(u64 capacity) -> bool { return reserveRight(capacity); }

  auto reallocateLeft(u64 size) -> bool;
  auto reallocateRight(u64 size) -> bool;
  auto reallocate(u64 size) -> bool { return reallocateRight(size); }

  auto resizeLeft(u64 size, const T& value = T()) -> bool;
  auto resizeRight(u64 size, const T& value = T()) -> bool;
  auto resize(u64 size, const T& value = T()) -> bool { return resizeRight(size, value); }

  //access.hpp
  auto operator[](u64 offset) -> T&;
  auto operator[](u64 offset) const -> const T&;

  auto operator()(u64 offset) -> T&;
  auto operator()(u64 offset, const T& value) const -> const T&;

  auto left() -> T&;
  auto first() -> T& { return left(); }
  auto left() const -> const T&;
  auto first() const -> const T& { return left(); }

  auto right() -> T&;
  auto last() -> T& { return right(); }
  auto right() const -> const T&;
  auto last() const -> const T& { return right(); }

  //modify.hpp
  auto prepend(const T& value) -> void;
  auto prepend(T&& value) -> void;
  auto prepend(const type& values) -> void;
  auto prepend(type&& values) -> void;

  auto append(const T& value) -> void;
  auto append(T&& value) -> void;
  auto append(const type& values) -> void;
  auto append(type&& values) -> void;

  auto insert(u64 offset, const T& value) -> void;

  auto removeLeft(u64 length = 1) -> void;
  auto removeFirst(u64 length = 1) -> void { return removeLeft(length); }
  auto removeRight(u64 length = 1) -> void;
  auto removeLast(u64 length = 1) -> void { return removeRight(length); }
  auto remove(u64 offset, u64 length = 1) -> void;
  auto removeByIndex(u64 offset) -> bool;
  auto removeByValue(const T& value) -> bool;

  auto takeLeft() -> T;
  auto takeFirst() -> T { return move(takeLeft()); }
  auto takeRight() -> T;
  auto takeLast() -> T { return move(takeRight()); }
  auto take(u64 offset) -> T;

  //iterator.hpp
  auto begin() -> iterator<T> { return {data(), 0}; }
  auto end() -> iterator<T> { return {data(), size()}; }

  auto begin() const -> iterator_const<T> { return {data(), 0}; }
  auto end() const -> iterator_const<T> { return {data(), size()}; }

  auto rbegin() -> reverse_iterator<T> { return {data(), size() - 1}; }
  auto rend() -> reverse_iterator<T> { return {data(), (u64)-1}; }

  auto rbegin() const -> reverse_iterator_const<T> { return {data(), size() - 1}; }
  auto rend() const -> reverse_iterator_const<T> { return {data(), (u64)-1}; }

  //utility.hpp
  auto fill(const T& value = {}) -> void;
  auto sort(const function<bool (const T& lhs, const T& rhs)>& comparator = [](auto& lhs, auto& rhs) { return lhs < rhs; }) -> void;
  auto reverse() -> void;
  auto find(const function<bool (const T& lhs)>& comparator) -> maybe<u64>;
  auto find(const T& value) const -> maybe<u64>;
  auto findSorted(const T& value) const -> maybe<u64>;
  auto foreach(const function<void (const T&)>& callback) -> void;
  auto foreach(const function<void (u64, const T&)>& callback) -> void;

protected:
  T* _pool = nullptr;   //pointer to first initialized element in pool
  u64 _size = 0;   //number of initialized elements in pool
  u64 _left = 0;   //number of allocated elements free on the left of pool
  u64 _right = 0;  //number of allocated elements free on the right of pool
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

#include <nall/vector/specialization/u8.hpp>
