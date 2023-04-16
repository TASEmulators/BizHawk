#pragma once

#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <algorithm>
#include <initializer_list>
#include <memory>

#include <nall/platform.hpp>
#include <nall/array-view.hpp>
#include <nall/atoi.hpp>
#include <nall/function.hpp>
#include <nall/intrinsics.hpp>
#include <nall/memory.hpp>
#include <nall/primitives.hpp>
#include <nall/shared-pointer.hpp>
#include <nall/stdint.hpp>
#include <nall/unique-pointer.hpp>
#include <nall/utility.hpp>
#include <nall/varint.hpp>
#include <nall/vector.hpp>
#include <nall/view.hpp>

namespace nall {

struct string;
struct string_format;

struct string_view {
  using type = string_view;

  //view.hpp
  string_view();
  string_view(const string_view& source);
  string_view(string_view&& source);
  string_view(const char* data);
  string_view(const char* data, u32 size);
  string_view(const string& source);
  template<typename... P> string_view(P&&... p);
  ~string_view();

  auto operator=(const string_view& source) -> type&;
  auto operator=(string_view&& source) -> type&;

  auto operator==(const char* source) const -> bool { return strcmp(data(), source) == 0; }
  auto operator!=(const char* source) const -> bool { return strcmp(data(), source) != 0; }

  explicit operator bool() const;
  operator const char*() const;
  auto data() const -> const char*;
  auto size() const -> u32;

  auto begin() const { return &_data[0]; }
  auto end() const { return &_data[size()]; }

protected:
  string* _string;
  const char* _data;
  mutable s32 _size;
};

//adaptive (SSO + COW) is by far the best choice, the others exist solely to:
//1) demonstrate the performance benefit of combining SSO + COW
//2) rule out allocator bugs by trying different allocators when needed
#define NALL_STRING_ALLOCATOR_ADAPTIVE
//#define NALL_STRING_ALLOCATOR_COPY_ON_WRITE
//#define NALL_STRING_ALLOCATOR_SMALL_STRING_OPTIMIZATION
//#define NALL_STRING_ALLOCATOR_VECTOR

//cast.hpp
template<typename T> struct stringify;

//format.hpp
template<typename... P> auto print(P&&...) -> void;
template<typename... P> auto print(FILE*, P&&...) -> void;
template<typename T> auto pad(const T& value, long precision = 0, char padchar = ' ') -> string;
template<typename T> auto hex(T value, long precision = 0, char padchar = '0') -> string;
template<typename T> auto octal(T value, long precision = 0, char padchar = '0') -> string;
template<typename T> auto binary(T value, long precision = 0, char padchar = '0') -> string;

//match.hpp
auto tokenize(const char* s, const char* p) -> bool;
auto tokenize(vector<string>& list, const char* s, const char* p) -> bool;

//utf8.hpp
auto characters(string_view self, s32 offset = 0, s32 length = -1) -> u32;

//utility.hpp
auto slice(string_view self, s32 offset = 0, s32 length = -1) -> string;
template<typename T> auto fromInteger(char* result, T value) -> char*;
template<typename T> auto fromNatural(char* result, T value) -> char*;
template<typename T> auto fromHex(char* result, T value) -> char*;
template<typename T> auto fromReal(char* str, T value) -> u32;

struct string {
  using type = string;

protected:
  #if defined(NALL_STRING_ALLOCATOR_ADAPTIVE)
  enum : u32 { SSO = 24 };
  union {
    struct {  //copy-on-write
      char* _data;
      u32* _refs;
    };
    struct {  //small-string-optimization
      char _text[SSO];
    };
  };
  auto _allocate() -> void;
  auto _copy() -> void;
  auto _resize() -> void;
  #endif

  #if defined(NALL_STRING_ALLOCATOR_COPY_ON_WRITE)
  char* _data;
  mutable u32* _refs;
  auto _allocate() -> char*;
  auto _copy() -> char*;
  #endif

  #if defined(NALL_STRING_ALLOCATOR_SMALL_STRING_OPTIMIZATION)
  enum : u32 { SSO = 24 };
  union {
    char* _data;
    char _text[SSO];
  };
  #endif

  #if defined(NALL_STRING_ALLOCATOR_VECTOR)
  char* _data;
  #endif

  u32 _capacity;
  u32 _size;

public:
  string();
  string(string& source) : string() { operator=(source); }
  string(const string& source) : string() { operator=(source); }
  string(string&& source) : string() { operator=(move(source)); }
  template<typename T = char> auto get() -> T*;
  template<typename T = char> auto data() const -> const T*;
  template<typename T = char> auto size() const -> u32 { return _size / sizeof(T); }
  template<typename T = char> auto capacity() const -> u32 { return _capacity / sizeof(T); }
  auto reset() -> type&;
  auto reserve(u32) -> type&;
  auto resize(u32) -> type&;
  auto operator=(const string&) -> type&;
  auto operator=(string&&) -> type&;

  template<typename T, typename... P> string(T&& s, P&&... p) : string() {
    append(forward<T>(s), forward<P>(p)...);
  }
  ~string() { reset(); }

  explicit operator bool() const { return _size; }
  operator const char*() const { return (const char*)data(); }
  operator array_span<char>() { return {(char*)get(), size()}; }
  operator array_view<char>() const { return {(const char*)data(), size()}; }
  operator array_span<u8>() { return {(u8*)get(), size()}; }
  operator array_view<u8>() const { return {(const u8*)data(), size()}; }

  auto operator==(const string& source) const -> bool {
    return size() == source.size() && memory::compare(data(), source.data(), size()) == 0;
  }
  auto operator!=(const string& source) const -> bool {
    return size() != source.size() || memory::compare(data(), source.data(), size()) != 0;
  }

  auto operator==(const char* source) const -> bool { return strcmp(data(), source) == 0; }
  auto operator!=(const char* source) const -> bool { return strcmp(data(), source) != 0; }

  auto operator==(string_view source) const -> bool { return compare(source) == 0; }
  auto operator!=(string_view source) const -> bool { return compare(source) != 0; }
  auto operator< (string_view source) const -> bool { return compare(source) <  0; }
  auto operator<=(string_view source) const -> bool { return compare(source) <= 0; }
  auto operator> (string_view source) const -> bool { return compare(source) >  0; }
  auto operator>=(string_view source) const -> bool { return compare(source) >= 0; }

  auto begin() -> char* { return &get()[0]; }
  auto end() -> char* { return &get()[size()]; }
  auto begin() const -> const char* { return &data()[0]; }
  auto end() const -> const char* { return &data()[size()]; }

  //atoi.hpp
  auto boolean() const -> bool;
  auto integer() const -> s64;
  auto natural() const -> u64;
  auto hex() const -> u64;
  auto real() const -> f64;

  //core.hpp
  auto operator[](u32) const -> const char&;
  auto operator()(u32, char = 0) const -> char;
  template<typename... P> auto assign(P&&...) -> type&;
  template<typename T, typename... P> auto prepend(const T&, P&&...) -> type&;
  template<typename... P> auto prepend(const nall::string_format&, P&&...) -> type&;
  template<typename T> auto _prepend(const stringify<T>&) -> type&;
  template<typename T, typename... P> auto append(const T&, P&&...) -> type&;
  template<typename... P> auto append(const nall::string_format&, P&&...) -> type&;
  template<typename T> auto _append(const stringify<T>&) -> type&;
  auto length() const -> u32;

  //find.hpp
  auto contains(string_view characters) const -> maybe<u32>;

  template<bool, bool> auto _find(s32, string_view) const -> maybe<u32>;

  auto find(string_view source) const -> maybe<u32>;
  auto ifind(string_view source) const -> maybe<u32>;
  auto qfind(string_view source) const -> maybe<u32>;
  auto iqfind(string_view source) const -> maybe<u32>;

  auto findFrom(s32 offset, string_view source) const -> maybe<u32>;
  auto ifindFrom(s32 offset, string_view source) const -> maybe<u32>;

  auto findNext(s32 offset, string_view source) const -> maybe<u32>;
  auto ifindNext(s32 offset, string_view source) const -> maybe<u32>;

  auto findPrevious(s32 offset, string_view source) const -> maybe<u32>;
  auto ifindPrevious(s32 offset, string_view source) const -> maybe<u32>;

  //format.hpp
  auto format(const nall::string_format& params) -> type&;

  //compare.hpp
  template<bool> static auto _compare(const char*, u32, const char*, u32) -> s32;

  static auto compare(string_view, string_view) -> s32;
  static auto icompare(string_view, string_view) -> s32;

  auto compare(string_view source) const -> s32;
  auto icompare(string_view source) const -> s32;

  auto equals(string_view source) const -> bool;
  auto iequals(string_view source) const -> bool;

  auto beginsWith(string_view source) const -> bool;
  auto ibeginsWith(string_view source) const -> bool;

  auto endsWith(string_view source) const -> bool;
  auto iendsWith(string_view source) const -> bool;

  //convert.hpp
  auto downcase() -> type&;
  auto upcase() -> type&;

  auto qdowncase() -> type&;
  auto qupcase() -> type&;

  auto transform(string_view from, string_view to) -> type&;

  //match.hpp
  auto match(string_view source) const -> bool;
  auto imatch(string_view source) const -> bool;

  //replace.hpp
  template<bool, bool> auto _replace(string_view, string_view, long) -> type&;
  auto replace(string_view from, string_view to, long limit = LONG_MAX) -> type&;
  auto ireplace(string_view from, string_view to, long limit = LONG_MAX) -> type&;
  auto qreplace(string_view from, string_view to, long limit = LONG_MAX) -> type&;
  auto iqreplace(string_view from, string_view to, long limit = LONG_MAX) -> type&;

  //split.hpp
  auto split(string_view key, long limit = LONG_MAX) const -> vector<string>;
  auto isplit(string_view key, long limit = LONG_MAX) const -> vector<string>;
  auto qsplit(string_view key, long limit = LONG_MAX) const -> vector<string>;
  auto iqsplit(string_view key, long limit = LONG_MAX) const -> vector<string>;

  //trim.hpp
  auto trim(string_view lhs, string_view rhs, long limit = LONG_MAX) -> type&;
  auto trimLeft(string_view lhs, long limit = LONG_MAX) -> type&;
  auto trimRight(string_view rhs, long limit = LONG_MAX) -> type&;

  auto itrim(string_view lhs, string_view rhs, long limit = LONG_MAX) -> type&;
  auto itrimLeft(string_view lhs, long limit = LONG_MAX) -> type&;
  auto itrimRight(string_view rhs, long limit = LONG_MAX) -> type&;

  auto strip() -> type&;
  auto stripLeft() -> type&;
  auto stripRight() -> type&;

  //utf8.hpp
  auto characters(s32 offset = 0, s32 length = -1) const -> u32;

  //utility.hpp
  static auto read(string_view filename) -> string;
  static auto repeat(string_view pattern, u32 times) -> string;
  auto fill(char fill = ' ') -> type&;
  auto hash() const -> u32;
  auto remove(u32 offset, u32 length) -> type&;
  auto reverse() -> type&;
  auto size(s32 length, char fill = ' ') -> type&;
  auto slice(s32 offset = 0, s32 length = -1) const -> string;
};

template<> struct vector<string> : vector_base<string> {
  using type = vector<string>;
  using vector_base<string>::vector_base;

  vector(const vector& source) { vector_base::operator=(source); }
  vector(vector& source) { vector_base::operator=(source); }
  vector(vector&& source) { vector_base::operator=(move(source)); }
  template<typename... P> vector(P&&... p) { append(forward<P>(p)...); }

  auto operator=(const vector& source) -> type& { return vector_base::operator=(source), *this; }
  auto operator=(vector& source) -> type& { return vector_base::operator=(source), *this; }
  auto operator=(vector&& source) -> type& { return vector_base::operator=(move(source)), *this; }

  //vector.hpp
  template<typename... P> auto append(const string&, P&&...) -> type&;
  auto append() -> type&;

  auto isort() -> type&;
  auto find(string_view source) const -> maybe<u32>;
  auto ifind(string_view source) const -> maybe<u32>;
  auto match(string_view pattern) const -> vector<string>;
  auto merge(string_view separator = "") const -> string;
  auto strip() -> type&;

  //split.hpp
  template<bool, bool> auto _split(string_view, string_view, long) -> type&;
};

struct string_format : vector<string> {
  using type = string_format;

  template<typename... P> string_format(P&&... p) { reserve(sizeof...(p)); append(forward<P>(p)...); }
  template<typename T, typename... P> auto append(const T&, P&&... p) -> type&;
  auto append() -> type&;
};

inline auto operator"" _s(const char* value, std::size_t) -> string { return {value}; }

}

#include <nall/string/view.hpp>
#include <nall/string/pascal.hpp>

#include <nall/string/atoi.hpp>
#include <nall/string/cast.hpp>
#include <nall/string/compare.hpp>
#include <nall/string/convert.hpp>
#include <nall/string/core.hpp>
#include <nall/string/find.hpp>
#include <nall/string/format.hpp>
#include <nall/string/match.hpp>
#include <nall/string/replace.hpp>
#include <nall/string/split.hpp>
#include <nall/string/trim.hpp>
#include <nall/string/utf8.hpp>
#include <nall/string/utility.hpp>
#include <nall/string/vector.hpp>

#include <nall/string/eval/node.hpp>
#include <nall/string/eval/literal.hpp>
#include <nall/string/eval/parser.hpp>
#include <nall/string/eval/evaluator.hpp>

#include <nall/string/markup/node.hpp>
#include <nall/string/markup/find.hpp>
#include <nall/string/markup/bml.hpp>
#include <nall/string/markup/xml.hpp>

#include <nall/string/transform/cml.hpp>
#include <nall/string/transform/dml.hpp>
