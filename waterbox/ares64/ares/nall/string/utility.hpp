#pragma once

namespace nall {

inline auto string::read(string_view filename) -> string {
  #if !defined(_WIN32)
  FILE* fp = fopen(filename, "rb");
  #else
  FILE* fp = _wfopen(utf16_t(filename), L"rb");
  #endif

  string result;
  if(!fp) return result;

  fseek(fp, 0, SEEK_END);
  s32 filesize = ftell(fp);
  if(filesize < 0) return fclose(fp), result;

  rewind(fp);
  result.resize(filesize);
  (void)fread(result.get(), 1, filesize, fp);
  return fclose(fp), result;
}

inline auto string::repeat(string_view pattern, u32 times) -> string {
  string result;
  while(times--) result.append(pattern.data());
  return result;
}

inline auto string::fill(char fill) -> string& {
  memory::fill(get(), size(), fill);
  return *this;
}

inline auto string::hash() const -> u32 {
  const char* p = data();
  u32 length = size();
  u32 result = 5381;
  while(length--) result = (result << 5) + result + *p++;
  return result;
}

inline auto string::remove(u32 offset, u32 length) -> string& {
  char* p = get();
  length = min(length, size());
  memory::move(p + offset, p + offset + length, size() - length);
  return resize(size() - length);
}

inline auto string::reverse() -> string& {
  char* p = get();
  u32 length = size();
  u32 pivot = length >> 1;
  for(s32 x = 0, y = length - 1; x < pivot && y >= 0; x++, y--) std::swap(p[x], p[y]);
  return *this;
}

//+length => insert/delete from start (right justify)
//-length => insert/delete from end (left justify)
inline auto string::size(s32 length, char fill) -> string& {
  u32 size = this->size();
  if(size == length) return *this;

  bool right = length >= 0;
  length = abs(length);

  if(size < length) {  //expand
    resize(length);
    char* p = get();
    u32 displacement = length - size;
    if(right) memory::move(p + displacement, p, size);
    else p += size;
    while(displacement--) *p++ = fill;
  } else {  //shrink
    char* p = get();
    u32 displacement = size - length;
    if(right) memory::move(p, p + displacement, length);
    resize(length);
  }

  return *this;
}

inline auto slice(string_view self, s32 offset, s32 length) -> string {
  string result;
  if(offset < 0) offset = self.size() - abs(offset);
  if(offset >= 0 && offset < self.size()) {
    if(length < 0) length = self.size() - offset;
    if(length >= 0) {
      result.resize(length);
      memory::copy(result.get(), self.data() + offset, length);
    }
  }
  return result;
}

inline auto string::slice(s32 offset, s32 length) const -> string {
  return nall::slice(*this, offset, length);
}

template<typename T> inline auto fromInteger(char* result, T value) -> char* {
  bool negative = value < 0;
  if(!negative) value = -value;  //negate positive integers to support eg INT_MIN

  char buffer[1 + sizeof(T) * 3];
  u32 size = 0;

  do {
    s32 n = value % 10;  //-0 to -9
    buffer[size++] = '0' - n;  //'0' to '9'
    value /= 10;
  } while(value);
  if(negative) buffer[size++] = '-';

  for(s32 x = size - 1, y = 0; x >= 0 && y < size; x--, y++) result[x] = buffer[y];
  result[size] = 0;
  return result;
}

template<typename T> inline auto fromNatural(char* result, T value) -> char* {
  char buffer[1 + sizeof(T) * 3];
  u32 size = 0;

  do {
    u32 n = value % 10;
    buffer[size++] = '0' + n;
    value /= 10;
  } while(value);

  for(s32 x = size - 1, y = 0; x >= 0 && y < size; x--, y++) result[x] = buffer[y];
  result[size] = 0;
  return result;
}

template<typename T> inline auto fromHex(char* result, T value) -> char* {
  char buffer[1 + sizeof(T) * 2];
  u32 size = 0;

  do {
    u32 n = value & 15;
    if(n <= 9) {
      buffer[size++] = '0' + n;
    } else {
      buffer[size++] = 'a' + n - 10;
    }
    value >>= 4;
  } while(value);

  for(s32 x = size - 1, y = 0; x >= 0 && y < size; x--, y++) result[x] = buffer[y];
  result[size] = 0;
  return result;
}

//using sprintf is certainly not the most ideal method to convert
//a double to a string ... but attempting to parse a double by
//hand, digit-by-digit, results in subtle rounding errors.
template<typename T> inline auto fromReal(char* result, T value) -> u32 {
  char buffer[256];
  #ifdef _WIN32
  //Windows C-runtime does not support long double via sprintf()
  sprintf(buffer, "%f", (double)value);
  #else
  sprintf(buffer, "%Lf", (long double)value);
  #endif

  //remove excess 0's in fraction (2.500000 -> 2.5)
  for(char* p = buffer; *p; p++) {
    if(*p == '.') {
      char* p = buffer + strlen(buffer) - 1;
      while(*p == '0') {
        if(*(p - 1) != '.') *p = 0;  //... but not for eg 1.0 -> 1.
        p--;
      }
      break;
    }
  }

  u32 length = strlen(buffer);
  if(result) strcpy(result, buffer);
  return length + 1;
}

}
