#pragma once

//convert any (supported) type to a const char* without constructing a new nall::string
//this is used inside string{...} to build nall::string values

namespace nall {

//booleans

template<> struct stringify<bool> {
  stringify(bool value) : _value(value) {}
  auto data() const -> const char* { return _value ? "true" : "false"; }
  auto size() const -> uint { return _value ? 4 : 5; }
  bool _value;
};

template<> struct stringify<Boolean> {
  stringify(bool value) : _value(value) {}
  auto data() const -> const char* { return _value ? "true" : "false"; }
  auto size() const -> uint { return _value ? 4 : 5; }
  bool _value;
};

//characters

template<> struct stringify<char> {
  stringify(char source) { _data[0] = source; _data[1] = 0; }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return 1; }
  char _data[2];
};

//signed integers

template<> struct stringify<signed char> {
  stringify(signed char source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(signed char) * 3];
};

template<> struct stringify<signed short> {
  stringify(signed short source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(signed short) * 3];
};

template<> struct stringify<signed int> {
  stringify(signed int source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(signed int) * 3];
};

template<> struct stringify<signed long> {
  stringify(signed long source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(signed long) * 3];
};

template<> struct stringify<signed long long> {
  stringify(signed long long source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(signed long long) * 3];
};

#if defined(__SIZEOF_INT128__)
template<> struct stringify<int128_t> {
  stringify(int128_t source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(int128_t) * 3];
};
#endif

template<uint Bits> struct stringify<Integer<Bits>> {
  stringify(Integer<Bits> source) { fromInteger(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[2 + sizeof(int64_t) * 3];
};

//unsigned integers

template<> struct stringify<unsigned char> {
  stringify(unsigned char source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(unsigned char) * 3];
};

template<> struct stringify<unsigned short> {
  stringify(unsigned short source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(unsigned short) * 3];
};

template<> struct stringify<unsigned int> {
  stringify(unsigned int source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(unsigned int) * 3];
};

template<> struct stringify<unsigned long> {
  stringify(unsigned long source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(unsigned long) * 3];
};

template<> struct stringify<unsigned long long> {
  stringify(unsigned long long source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(unsigned long long) * 3];
};

#if defined(__SIZEOF_INT128__)
template<> struct stringify<uint128_t> {
  stringify(uint128_t source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(uint128_t) * 3];
};
#endif

template<uint Bits> struct stringify<Natural<Bits>> {
  stringify(Natural<Bits> source) { fromNatural(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[1 + sizeof(uint64_t) * 3];
};

//floating-point

template<> struct stringify<float> {
  stringify(float source) { fromReal(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[256];
};

template<> struct stringify<double> {
  stringify(double source) { fromReal(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[256];
};

template<> struct stringify<long double> {
  stringify(long double source) { fromReal(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[256];
};

template<uint Bits> struct stringify<Real<Bits>> {
  stringify(Real<Bits> source) { fromReal(_data, source); }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[256];
};

//arrays

template<> struct stringify<vector<uint8_t>> {
  stringify(const vector<uint8_t>& source) {
    _text.resize(source.size());
    memory::copy(_text.data(), source.data(), source.size());
  }
  auto data() const -> const char* { return _text.data(); }
  auto size() const -> uint { return _text.size(); }
  vector<char> _text;
};

//char arrays

template<> struct stringify<char*> {
  stringify(const char* source) : _data(source ? source : "") {}
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  const char* _data;
};

template<> struct stringify<const char*> {
  stringify(const char* source) : _data(source ? source : "") {}
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  const char* _data;
};

//strings

template<> struct stringify<string> {
  stringify(const string& source) : _text(source) {}
  auto data() const -> const char* { return _text.data(); }
  auto size() const -> uint { return _text.size(); }
  const string& _text;
};

template<> struct stringify<string_view> {
  stringify(const string_view& source) : _view(source) {}
  auto data() const -> const char* { return _view.data(); }
  auto size() const -> uint { return _view.size(); }
  const string_view& _view;
};

template<> struct stringify<array_view<uint8_t>> {
  stringify(const array_view<uint8_t>& source) : _view(source) {}
  auto data() const -> const char* { return _view.data<const char>(); }
  auto size() const -> uint { return _view.size(); }
  const array_view<uint8_t>& _view;
};

template<> struct stringify<string_pascal> {
  stringify(const string_pascal& source) : _text(source) {}
  auto data() const -> const char* { return _text.data(); }
  auto size() const -> uint { return _text.size(); }
  const string_pascal& _text;
};

//pointers

//note: T = char* is matched by stringify<string_view>
template<typename T> struct stringify<T*> {
  stringify(const T* source) {
    if(!source) {
      memory::copy(_data, "(nullptr)", 10);
    } else {
      memory::copy(_data, "0x", 2);
      fromNatural(_data + 2, (uintptr)source);
    }
  }
  auto data() const -> const char* { return _data; }
  auto size() const -> uint { return strlen(_data); }
  char _data[256];
};

//

template<typename T> auto make_string(const T& value) {
  return stringify<std::decay_t<T>>(value);
}

}
