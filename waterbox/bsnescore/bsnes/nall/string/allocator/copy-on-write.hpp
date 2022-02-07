#pragma once

namespace nall {

string::string() : _data(nullptr), _refs(nullptr), _capacity(0), _size(0) {
}

template<typename T>
auto string::get() -> T* {
  static char _null[] = "";
  if(!_data) return (T*)_null;
  if(*_refs > 1) _data = _copy();  //make unique for write operations
  return (T*)_data;
}

template<typename T>
auto string::data() const -> const T* {
  static const char _null[] = "";
  if(!_data) return (const T*)_null;
  return (const T*)_data;
}

auto string::reset() -> type& {
  if(_data && !--*_refs) {
    memory::free(_data);
    _data = nullptr;  //_refs = nullptr; is unnecessary
  }
  _capacity = 0;
  _size = 0;
  return *this;
}

auto string::reserve(uint capacity) -> type& {
  if(capacity > _capacity) {
    _capacity = bit::round(max(31u, capacity) + 1) - 1;
    _data = _data ? _copy() : _allocate();
  }
  return *this;
}

auto string::resize(uint size) -> type& {
  reserve(size);
  get()[_size = size] = 0;
  return *this;
}

auto string::operator=(const string& source) -> string& {
  if(&source == this) return *this;
  reset();
  if(source._data) {
    _data = source._data;
    _refs = source._refs;
    _capacity = source._capacity;
    _size = source._size;
    ++*_refs;
  }
  return *this;
}

auto string::operator=(string&& source) -> string& {
  if(&source == this) return *this;
  reset();
  _data = source._data;
  _refs = source._refs;
  _capacity = source._capacity;
  _size = source._size;
  source._data = nullptr;
  source._refs = nullptr;
  source._capacity = 0;
  source._size = 0;
  return *this;
}

auto string::_allocate() -> char* {
  auto _temp = memory::allocate<char>(_capacity + 1 + sizeof(uint));
  *_temp = 0;
  _refs = (uint*)(_temp + _capacity + 1);  //this will always be aligned by 32 via reserve()
  *_refs = 1;
  return _temp;
}

auto string::_copy() -> char* {
  auto _temp = memory::allocate<char>(_capacity + 1 + sizeof(uint));
  memory::copy(_temp, _data, _size = min(_capacity, _size));
  _temp[_size] = 0;
  --*_refs;
  _refs = (uint*)(_temp + _capacity + 1);
  *_refs = 1;
  return _temp;
}

}
