#pragma once

/*
vector allocator
sizeof(string) == 16 (amd64)

utilizes a raw string pointer
always allocates memory onto the heap when string is not empty

pros:
* potential for in-place resize
* simplicity

cons:
* always allocates heap memory on (capacity > 0)
* pass-by-value requires heap allocation

*/

namespace nall {

template<typename T>
auto string::get() -> T* {
  if(_capacity == 0) reserve(1);
  return (T*)_data;
}

template<typename T>
auto string::data() const -> const T* {
  if(_capacity == 0) return (const T*)"";
  return (const T*)_data;
}

auto string::reset() -> type& {
  if(_data) { memory::free(_data); _data = nullptr; }
  _capacity = 0;
  _size = 0;
  return *this;
}

auto string::reserve(uint capacity) -> type& {
  if(capacity > _capacity) {
    _capacity = bit::round(capacity + 1) - 1;
    _data = memory::resize<char>(_data, _capacity + 1);
    _data[_capacity] = 0;
  }
  return *this;
}

auto string::resize(uint size) -> type& {
  reserve(size);
  get()[_size = size] = 0;
  return *this;
}

auto string::operator=(const string& source) -> type& {
  if(&source == this) return *this;
  reset();
  _data = memory::allocate<char>(source._size + 1);
  _capacity = source._size;
  _size = source._size;
  memory::copy(_data, source.data(), source.size() + 1);
  return *this;
}

auto string::operator=(string&& source) -> type& {
  if(&source == this) return *this;
  reset();
  _data = source._data;
  _capacity = source._capacity;
  _size = source._size;
  source._data = nullptr;
  source._capacity = 0;
  source._size = 0;
  return *this;
}

string::string() {
  _data = nullptr;
  _capacity = 0;
  _size = 0;
}

}
