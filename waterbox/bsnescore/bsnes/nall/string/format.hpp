#pragma once

namespace nall {

//nall::format is a vector<string> of parameters that can be applied to a string
//each {#} token will be replaced with its appropriate format parameter

auto string::format(const nall::string_format& params) -> type& {
  auto size = (int)this->size();
  auto data = memory::allocate<char>(size);
  memory::copy(data, this->data(), size);

  int x = 0;
  while(x < size - 2) {  //2 = minimum tag length
    if(data[x] != '{') { x++; continue; }

    int y = x + 1;
    while(y < size - 1) {  //-1 avoids going out of bounds on test after this loop
      if(data[y] != '}') { y++; continue; }
      break;
    }

    if(data[y++] != '}') { x++; continue; }

    static auto isNumeric = [](char* s, char* e) -> bool {
      if(s == e) return false;  //ignore empty tags: {}
      while(s < e) {
        if(*s >= '0' && *s <= '9') { s++; continue; }
        return false;
      }
      return true;
    };
    if(!isNumeric(&data[x + 1], &data[y - 1])) { x++; continue; }

    uint index = toNatural(&data[x + 1]);
    if(index >= params.size()) { x++; continue; }

    uint sourceSize = y - x;
    uint targetSize = params[index].size();
    uint remaining = size - x;

    if(sourceSize > targetSize) {
      uint difference = sourceSize - targetSize;
      memory::move(&data[x], &data[x + difference], remaining - difference);
      size -= difference;
    } else if(targetSize > sourceSize) {
      uint difference = targetSize - sourceSize;
      data = (char*)realloc(data, size + difference);
      size += difference;
      memory::move(&data[x + difference], &data[x], remaining);
    }
    memory::copy(&data[x], params[index].data(), targetSize);
    x += targetSize;
  }

  resize(size);
  memory::copy(get(), data, size);
  memory::free(data);
  return *this;
}

template<typename T, typename... P> auto string_format::append(const T& value, P&&... p) -> string_format& {
  vector<string>::append(value);
  return append(forward<P>(p)...);
}

auto string_format::append() -> string_format& {
  return *this;
}

template<typename... P> auto print(P&&... p) -> void {
  string s{forward<P>(p)...};
  fwrite(s.data(), 1, s.size(), stdout);
  fflush(stdout);
}

template<typename... P> auto print(FILE* fp, P&&... p) -> void {
  string s{forward<P>(p)...};
  fwrite(s.data(), 1, s.size(), fp);
  if(fp == stdout || fp == stderr) fflush(fp);
}

template<typename T> auto pad(const T& value, long precision, char padchar) -> string {
  string buffer{value};
  if(precision) buffer.size(precision, padchar);
  return buffer;
}

auto hex(uintmax value, long precision, char padchar) -> string {
  string buffer;
  buffer.resize(sizeof(uintmax) * 2);
  char* p = buffer.get();

  uint size = 0;
  do {
    uint n = value & 15;
    p[size++] = n < 10 ? '0' + n : 'a' + n - 10;
    value >>= 4;
  } while(value);
  buffer.resize(size);
  buffer.reverse();
  if(precision) buffer.size(precision, padchar);
  return buffer;
}

auto octal(uintmax value, long precision, char padchar) -> string {
  string buffer;
  buffer.resize(sizeof(uintmax) * 3);
  char* p = buffer.get();

  uint size = 0;
  do {
    p[size++] = '0' + (value & 7);
    value >>= 3;
  } while(value);
  buffer.resize(size);
  buffer.reverse();
  if(precision) buffer.size(precision, padchar);
  return buffer;
}

auto binary(uintmax value, long precision, char padchar) -> string {
  string buffer;
  buffer.resize(sizeof(uintmax) * 8);
  char* p = buffer.get();

  uint size = 0;
  do {
    p[size++] = '0' + (value & 1);
    value >>= 1;
  } while(value);
  buffer.resize(size);
  buffer.reverse();
  if(precision) buffer.size(precision, padchar);
  return buffer;
}

}
