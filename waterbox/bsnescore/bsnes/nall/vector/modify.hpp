#pragma once

namespace nall {

template<typename T> auto vector<T>::prepend(const T& value) -> void {
  reserveLeft(size() + 1);
  new(--_pool) T(value);
  _left--;
  _size++;
}

template<typename T> auto vector<T>::prepend(T&& value) -> void {
  reserveLeft(size() + 1);
  new(--_pool) T(move(value));
  _left--;
  _size++;
}

template<typename T> auto vector<T>::prepend(const vector<T>& values) -> void {
  reserveLeft(size() + values.size());
  _pool -= values.size();
  for(uint64_t n : range(values)) new(_pool + n) T(values[n]);
  _left -= values.size();
  _size += values.size();
}

template<typename T> auto vector<T>::prepend(vector<T>&& values) -> void {
  reserveLeft(size() + values.size());
  _pool -= values.size();
  for(uint64_t n : range(values)) new(_pool + n) T(move(values[n]));
  _left -= values.size();
  _size += values.size();
}

//

template<typename T> auto vector<T>::append(const T& value) -> void {
  reserveRight(size() + 1);
  new(_pool + _size) T(value);
  _right--;
  _size++;
}

template<typename T> auto vector<T>::append(T&& value) -> void {
  reserveRight(size() + 1);
  new(_pool + _size) T(move(value));
  _right--;
  _size++;
}

template<typename T> auto vector<T>::append(const vector<T>& values) -> void {
  reserveRight(size() + values.size());
  for(uint64_t n : range(values.size())) new(_pool + _size + n) T(values[n]);
  _right -= values.size();
  _size += values.size();
}

template<typename T> auto vector<T>::append(vector<T>&& values) -> void {
  reserveRight(size() + values.size());
  for(uint64_t n : range(values.size())) new(_pool + _size + n) T(move(values[n]));
  _right -= values.size();
  _size += values.size();
}

//

template<typename T> auto vector<T>::insert(uint64_t offset, const T& value) -> void {
  if(offset == 0) return prepend(value);
  if(offset == size() - 1) return append(value);
  reserveRight(size() + 1);
  _size++;
  for(int64_t n = size() - 1; n > offset; n--) {
    _pool[n] = move(_pool[n - 1]);
  }
  new(_pool + offset) T(value);
}

//

template<typename T> auto vector<T>::removeLeft(uint64_t length) -> void {
  if(length > size()) length = size();
  resizeLeft(size() - length);
}

template<typename T> auto vector<T>::removeRight(uint64_t length) -> void {
  if(length > size()) length = size();
  resizeRight(size() - length);
}

template<typename T> auto vector<T>::remove(uint64_t offset, uint64_t length) -> void {
  if(offset == 0) return removeLeft(length);
  if(offset == size() - 1) return removeRight(length);

  for(uint64_t n = offset; n < size(); n++) {
    if(n + length < size()) {
      _pool[n] = move(_pool[n + length]);
    } else {
      _pool[n].~T();
    }
  }
  _size -= length;
}

template<typename T> auto vector<T>::removeByIndex(uint64_t index) -> bool {
  if(index < size()) return remove(index), true;
  return false;
}

template<typename T> auto vector<T>::removeByValue(const T& value) -> bool {
  if(auto index = find(value)) return remove(*index), true;
  return false;
}

//

template<typename T> auto vector<T>::takeLeft() -> T {
  T value = move(_pool[0]);
  removeLeft();
  return value;
}

template<typename T> auto vector<T>::takeRight() -> T {
  T value = move(_pool[size() - 1]);
  removeRight();
  return value;
}

template<typename T> auto vector<T>::take(uint64_t offset) -> T {
  if(offset == 0) return takeLeft();
  if(offset == size() - 1) return takeRight();

  T value = move(_pool[offset]);
  remove(offset);
  return value;
}

}
