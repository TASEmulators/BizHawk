#pragma once

namespace nall {

struct string_pascal {
  using type = string_pascal;

  string_pascal(const char* text = nullptr) {
    if(text && *text) {
      uint size = strlen(text);
      _data = memory::allocate<char>(sizeof(uint) + size + 1);
      ((uint*)_data)[0] = size;
      memory::copy(_data + sizeof(uint), text, size);
      _data[sizeof(uint) + size] = 0;
    }
  }

  string_pascal(const string& text) {
    if(text.size()) {
      _data = memory::allocate<char>(sizeof(uint) + text.size() + 1);
      ((uint*)_data)[0] = text.size();
      memory::copy(_data + sizeof(uint), text.data(), text.size());
      _data[sizeof(uint) + text.size()] = 0;
    }
  }

  string_pascal(const string_pascal& source) { operator=(source); }
  string_pascal(string_pascal&& source) { operator=(move(source)); }

  ~string_pascal() {
    if(_data) memory::free(_data);
  }

  explicit operator bool() const { return _data; }
  operator const char*() const { return _data ? _data + sizeof(uint) : nullptr; }
  operator string() const { return _data ? string{_data + sizeof(uint)} : ""; }

  auto operator=(const string_pascal& source) -> type& {
    if(this == &source) return *this;
    if(_data) { memory::free(_data); _data = nullptr; }
    if(source._data) {
      uint size = source.size();
      _data = memory::allocate<char>(sizeof(uint) + size);
      memory::copy(_data, source._data, sizeof(uint) + size);
    }
    return *this;
  }

  auto operator=(string_pascal&& source) -> type& {
    if(this == &source) return *this;
    if(_data) memory::free(_data);
    _data = source._data;
    source._data = nullptr;
    return *this;
  }

  auto operator==(string_view source) const -> bool {
    return size() == source.size() && memory::compare(data(), source.data(), size()) == 0;
  }

  auto operator!=(string_view source) const -> bool {
    return size() != source.size() || memory::compare(data(), source.data(), size()) != 0;
  }

  auto data() const -> char* {
    if(!_data) return nullptr;
    return _data + sizeof(uint);
  }

  auto size() const -> uint {
    if(!_data) return 0;
    return ((uint*)_data)[0];
  }

protected:
  char* _data = nullptr;
};

}
