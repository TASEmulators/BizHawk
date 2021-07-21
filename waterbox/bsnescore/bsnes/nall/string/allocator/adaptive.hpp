#pragma once

/*****
  adaptive allocator
  sizeof(string) == SSO + 8

  aggressively tries to avoid heap allocations
  small strings are stored on the stack
  large strings are shared via copy-on-write

  SSO alone is very slow on large strings due to copying
  SSO alone is very slightly faster than this allocator on small strings

  COW alone is very slow on small strings due to heap allocations
  COW alone is very slightly faster than this allocator on large strings

  adaptive is thus very fast for all string sizes
*****/

namespace nall {

string::string() : _data(nullptr), _capacity(SSO - 1), _size(0) {
}

template<typename T>
auto string::get() -> T* {
  if(_capacity < SSO) return (T*)_text;
  if(*_refs > 1) _copy();
  return (T*)_data;
}

template<typename T>
auto string::data() const -> const T* {
  if(_capacity < SSO) return (const T*)_text;
  return (const T*)_data;
}

auto string::reset() -> type& {
  if(_capacity >= SSO && !--*_refs) memory::free(_data);
  _data = nullptr;
  _capacity = SSO - 1;
  _size = 0;
  return *this;
}

auto string::reserve(uint capacity) -> type& {
  if(capacity <= _capacity) return *this;
  capacity = bit::round(capacity + 1) - 1;
  if(_capacity < SSO) {
    _capacity = capacity;
    _allocate();
  } else if(*_refs > 1) {
    _capacity = capacity;
    _copy();
  } else {
    _capacity = capacity;
    _resize();
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
  if(source._capacity >= SSO) {
    _data = source._data;
    _refs = source._refs;
    _capacity = source._capacity;
    _size = source._size;
    ++*_refs;
  } else {
    memory::copy(_text, source._text, SSO);
    _capacity = source._capacity;
    _size = source._size;
  }
  return *this;
}

auto string::operator=(string&& source) -> type& {
  if(&source == this) return *this;
  reset();
  memory::copy(this, &source, sizeof(string));
  source._data = nullptr;
  source._capacity = SSO - 1;
  source._size = 0;
  return *this;
}

//SSO -> COW
auto string::_allocate() -> void {
  char _temp[SSO];
  memory::copy(_temp, _text, SSO);
  _data = memory::allocate<char>(_capacity + 1 + sizeof(uint));
  memory::copy(_data, _temp, SSO);
  _refs = (uint*)(_data + _capacity + 1);  //always aligned by 32 via reserve()
  *_refs = 1;
}

//COW -> Unique
auto string::_copy() -> void {
  auto _temp = memory::allocate<char>(_capacity + 1 + sizeof(uint));
  memory::copy(_temp, _data, _size = min(_capacity, _size));
  _temp[_size] = 0;
  --*_refs;
  _data = _temp;
  _refs = (uint*)(_data + _capacity + 1);
  *_refs = 1;
}

//COW -> Resize
auto string::_resize() -> void {
  _data = memory::resize<char>(_data, _capacity + 1 + sizeof(uint));
  _refs = (uint*)(_data + _capacity + 1);
  *_refs = 1;
}

}
