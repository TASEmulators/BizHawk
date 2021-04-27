#pragma once

namespace nall {

string_view::string_view() {
  _string = nullptr;
  _data = "";
  _size = 0;
}

string_view::string_view(const string_view& source) {
  if(this == &source) return;
  _string = nullptr;
  _data = source._data;
  _size = source._size;
}

string_view::string_view(string_view&& source) {
  if(this == &source) return;
  _string = source._string;
  _data = source._data;
  _size = source._size;
  source._string = nullptr;
}

string_view::string_view(const char* data) {
  _string = nullptr;
  _data = data;
  _size = -1;  //defer length calculation, as it is often unnecessary
}

string_view::string_view(const char* data, uint size) {
  _string = nullptr;
  _data = data;
  _size = size;
}

string_view::string_view(const string& source) {
  _string = nullptr;
  _data = source.data();
  _size = source.size();
}

template<typename... P>
string_view::string_view(P&&... p) {
  _string = new string{forward<P>(p)...};
  _data = _string->data();
  _size = _string->size();
}

string_view::~string_view() {
  if(_string) delete _string;
}

auto string_view::operator=(const string_view& source) -> type& {
  if(this == &source) return *this;
  _string = nullptr;
  _data = source._data;
  _size = source._size;
  return *this;
};

auto string_view::operator=(string_view&& source) -> type& {
  if(this == &source) return *this;
  _string = source._string;
  _data = source._data;
  _size = source._size;
  source._string = nullptr;
  return *this;
};

string_view::operator bool() const {
  return _size > 0;
}

string_view::operator const char*() const {
  return _data;
}

auto string_view::data() const -> const char* {
  return _data;
}

auto string_view::size() const -> uint {
  if(_size < 0) _size = strlen(_data);
  return _size;
}

}
