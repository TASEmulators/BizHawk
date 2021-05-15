#pragma once

//simple circular ring buffer

#include <nall/range.hpp>
#include <nall/serializer.hpp>

namespace nall {

template<typename T>
struct queue {
  queue() = default;
  queue(const queue& source) { operator=(source); }
  queue(queue&& source) { operator=(move(source)); }
  ~queue() { reset(); }

  auto operator=(const queue& source) -> queue& {
    if(this == &source) return *this;
    delete[] _data;
    _data = new T[source._capacity];
    _capacity = source._capacity;
    _size = source._size;
    _read = source._read;
    _write = source._write;
    for(uint n : range(_capacity)) _data[n] = source._data[n];
    return *this;
  }

  auto operator=(queue&& source) -> queue& {
    if(this == &source) return *this;
    _data = source._data;
    _capacity = source._capacity;
    _size = source._size;
    _read = source._read;
    _write = source._write;
    source._data = nullptr;
    source.reset();
    return *this;
  }

  template<typename U = T> auto capacity() const -> uint { return _capacity * sizeof(T) / sizeof(U); }
  template<typename U = T> auto size() const -> uint { return _size * sizeof(T) / sizeof(U); }
  auto empty() const -> bool { return _size == 0; }
  auto pending() const -> bool { return _size > 0; }
  auto full() const -> bool { return _size >= (int)_capacity; }
  auto underflow() const -> bool { return _size < 0; }
  auto overflow() const -> bool { return _size > (int)_capacity; }

  auto data() -> T* { return _data; }
  auto data() const -> const T* { return _data; }

  auto reset() {
    delete[] _data;
    _data = nullptr;
    _capacity = 0;
    _size = 0;
    _read = 0;
    _write = 0;
  }

  auto resize(uint capacity, const T& value = {}) -> void {
    delete[] _data;
    _data = new T[capacity];
    _capacity = capacity;
    _size = 0;
    _read = 0;
    _write = 0;
    for(uint n : range(_capacity)) _data[n] = value;
  }

  auto flush() -> void {
    _size = 0;
    _read = 0;
    _write = 0;
  }

  auto fill(const T& value = {}) -> void {
    _size = 0;
    _read = 0;
    _write = 0;
    for(uint n : range(_capacity)) _data[n] = value;
  }

  auto read() -> T {
    T value = _data[_read++];
    if(_read >= _capacity) _read = 0;
    _size--;
    return value;
  }

  auto write(const T& value) -> void {
    _data[_write++] = value;
    if(_write >= _capacity) _write = 0;
    _size++;
  }

  auto serialize(serializer& s) -> void {
    s.array(_data, _capacity);
    s.integer(_capacity);
    s.integer(_size);
    s.integer(_read);
    s.integer(_write);
  }

private:
  T* _data = nullptr;
  uint _capacity = 0;
  int _size = 0;
  uint _read = 0;
  uint _write = 0;
};

}
