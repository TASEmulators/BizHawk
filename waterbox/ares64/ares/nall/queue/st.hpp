#pragma once

//simple circular ring buffer (single-threaded)

namespace nall {

template<typename T> struct queue;

template<typename T, u32 Size>
struct queue<T[Size]> {
  auto flush() -> void {
    _read  = 0;
    _write = 2 * Size;
  }

  auto size() const -> u32 {
    return (_write - _read) % (2 * Size);
  }

  auto capacity() const -> u32 {
    return Size;
  }

  auto empty() const -> bool {
    return size() == 0;
  }

  auto full() const -> bool {
    return size() == Size;
  }

  auto peek(u32 index = 0) const -> T {
    return _data[(_read + index) % Size];
  }

  auto read() -> maybe<T> {
    if(empty()) return nothing;
    auto value = _data[_read % Size];
    _read = _read + 1 < 2 * Size ? _read + 1 : 0;
    return value;
  }

  auto read(const T& fallback) -> T {
    if(empty()) return fallback;
    auto value = _data[_read % Size];
    _read = _read + 1 < 2 * Size ? _read + 1 : 0;
    return value;
  }

  auto write(const T& value) -> bool {
    if(full()) return false;
    _data[_write % Size] = value;
    _write = _write + 1 < 4 * Size ? _write + 1 : 2 * Size;
    return true;
  }

  struct iterator_const {
    iterator_const(const queue& self, u64 offset) : self(self), offset(offset) {}
    auto operator*() -> T { return self.peek(offset); }
    auto operator!=(const iterator_const& source) const -> bool { return offset != source.offset; }
    auto operator++() -> iterator_const& { return offset++, *this; }

    const queue& self;
    u64 offset;
  };

  auto begin() const -> iterator_const { return {*this, 0}; }
  auto end() const -> iterator_const { return {*this, size()}; }

  auto serialize(serializer& s) -> void {
    s(_data);
    s(_read);
    s(_write);
  }

private:
  T _data[Size];
  u32 _read  = 0;
  u32 _write = 2 * Size;
};

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
    for(u32 n : range(_capacity)) _data[n] = source._data[n];
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

  template<typename U = T> auto capacity() const -> u32 { return _capacity * sizeof(T) / sizeof(U); }
  template<typename U = T> auto size() const -> u32 { return _size * sizeof(T) / sizeof(U); }
  auto empty() const -> bool { return _size == 0; }
  auto pending() const -> bool { return _size > 0; }
  auto full() const -> bool { return _size >= (s32)_capacity; }
  auto underflow() const -> bool { return _size < 0; }
  auto overflow() const -> bool { return _size > (s32)_capacity; }

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

  auto resize(u32 capacity, const T& value = {}) -> void {
    delete[] _data;
    _data = new T[capacity];
    _capacity = capacity;
    _size = 0;
    _read = 0;
    _write = 0;
    for(u32 n : range(_capacity)) _data[n] = value;
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
    for(u32 n : range(_capacity)) _data[n] = value;
  }

  auto peek(u32 index = 0) const -> T {
    return _data[(_read + index) % _capacity];
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
    s(array_span<T>{_data, _capacity});
    s(_read);
    s(_write);
  }

private:
  T* _data = nullptr;
  u32 _capacity = 0;
  s32 _size = 0;
  u32 _read = 0;
  u32 _write = 0;
};

}
