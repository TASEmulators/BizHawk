#pragma once

namespace nall {

inline string_view::string_view() {
  _string = nullptr;
  _data = "";
  _size = 0;
}

inline string_view::string_view(const string_view& source) {
  if(this == &source) return;
  _string = nullptr;
  _data = source._data;
  _size = source._size;
}

inline string_view::string_view(string_view&& source) {
  if(this == &source) return;
  _string = source._string;
  _data = source._data;
  _size = source._size;
  source._string = nullptr;
}

inline string_view::string_view(const char* data) {
  _string = nullptr;
  _data = data;
  _size = -1;  //defer length calculation, as it is often unnecessary
}

//todo: this collides with eg: {"value: ", (u32)0}
inline string_view::string_view(const char* data, u32 size) {
  _string = nullptr;
  _data = data;
  _size = size;
}

inline string_view::string_view(const string& source) {
  _string = nullptr;
  _data = source.data();
  _size = source.size();
}

template<typename... P>
inline string_view::string_view(P&&... p) {
  _string = new string{forward<P>(p)...};
  _data = _string->data();
  _size = _string->size();
}

inline string_view::~string_view() {
  if(_string) delete _string;
}

inline auto string_view::operator=(const string_view& source) -> type& {
  if(this == &source) return *this;
  _string = nullptr;
  _data = source._data;
  _size = source._size;
  return *this;
}

inline auto string_view::operator=(string_view&& source) -> type& {
  if(this == &source) return *this;
  _string = source._string;
  _data = source._data;
  _size = source._size;
  source._string = nullptr;
  return *this;
}

inline string_view::operator bool() const {
  return _size > 0;
}

inline string_view::operator const char*() const {
  return _data;
}

inline auto string_view::data() const -> const char* {
  return _data;
}

inline auto string_view::size() const -> u32 {
  if(_size < 0) _size = strlen(_data);
  return _size;
}

}
