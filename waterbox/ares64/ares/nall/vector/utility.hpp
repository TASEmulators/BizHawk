#pragma once

namespace nall {

template<typename T> auto vector<T>::fill(const T& value) -> void {
  for(u64 n : range(size())) _pool[n] = value;
}

template<typename T> auto vector<T>::sort(const function<bool (const T& lhs, const T& rhs)>& comparator) -> void {
  nall::sort(_pool, _size, comparator);
}

template<typename T> auto vector<T>::reverse() -> void {
  vector<T> reversed;
  for(u64 n : range(size())) reversed.prepend(_pool[n]);
  operator=(move(reversed));
}

template<typename T> auto vector<T>::find(const function<bool (const T& lhs)>& comparator) -> maybe<u64> {
  for(u64 n : range(size())) if(comparator(_pool[n])) return n;
  return nothing;
}

template<typename T> auto vector<T>::find(const T& value) const -> maybe<u64> {
  for(u64 n : range(size())) if(_pool[n] == value) return n;
  return nothing;
}

template<typename T> auto vector<T>::findSorted(const T& value) const -> maybe<u64> {
  s64 l = 0, r = size() - 1;
  while(l <= r) {
    s64 m = l + (r - l >> 1);
    if(value == _pool[m]) return m;
    value < _pool[m] ? r = m - 1 : l = m + 1;
  }
  return nothing;
}

template<typename T> auto vector<T>::foreach(const function<void (const T&)>& callback) -> void {
  for(u64 n : range(size())) callback(_pool[n]);
}

template<typename T> auto vector<T>::foreach(const function<void (u64, const T&)>& callback) -> void {
  for(u64 n : range(size())) callback(n, _pool[n]);
}

}
