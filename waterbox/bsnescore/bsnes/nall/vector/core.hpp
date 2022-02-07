#pragma once

namespace nall {

template<typename T> vector<T>::vector(const initializer_list<T>& values) {
  reserveRight(values.size());
  for(auto& value : values) append(value);
}

template<typename T> vector<T>::vector(const vector<T>& source) {
  operator=(source);
}

template<typename T> vector<T>::vector(vector<T>&& source) {
  operator=(move(source));
}

template<typename T> vector<T>::~vector() {
  reset();
}

template<typename T> vector<T>::operator bool() const {
  return _size;
}

template<typename T> vector<T>::operator array_span<T>() {
  return {data(), size()};
}

template<typename T> vector<T>::operator array_view<T>() const {
  return {data(), size()};
}

template<typename T> template<typename Cast> auto vector<T>::capacity() const -> uint64_t {
  return (_left + _size + _right) * sizeof(T) / sizeof(Cast);
}

template<typename T> template<typename Cast> auto vector<T>::size() const -> uint64_t {
  return _size * sizeof(T) / sizeof(Cast);
}

template<typename T> template<typename Cast> auto vector<T>::data() -> Cast* {
  return (Cast*)_pool;
}

template<typename T> template<typename Cast> auto vector<T>::data() const -> const Cast* {
  return (const Cast*)_pool;
}

}
