#pragma once

#include <nall/algorithm.hpp>
#include <nall/stdint.hpp>

namespace nall::memory {
  template<typename T = u8, u32 Align = 0> auto allocate(u32 size) -> T*;
  template<typename T = u8, u32 Align = 0> auto allocate(u32 size, const T& value) -> T*;
  template<typename T = u8, u32 Align = 0> auto resize(void* target, u32 size) -> T*;
  template<typename T = u8, u32 Align = 0> auto free(void* target) -> void;

  template<typename T = u8> auto compare(const void* target, u32 capacity, const void* source, u32 size) -> s32;
  template<typename T = u8> auto compare(const void* target, const void* source, u32 size) -> s32;

  template<typename T = u8> auto icompare(const void* target, u32 capacity, const void* source, u32 size) -> s32;
  template<typename T = u8> auto icompare(const void* target, const void* source, u32 size) -> s32;

  template<typename T = u8> auto copy(void* target, u32 capacity, const void* source, u32 size) -> T*;
  template<typename T = u8> auto copy(void* target, const void* source, u32 size) -> T*;

  template<typename T = u8> auto move(void* target, u32 capacity, const void* source, u32 size) -> T*;
  template<typename T = u8> auto move(void* target, const void* source, u32 size) -> T*;

  template<typename T = u8> auto fill(void* target, u32 capacity, const T& value = {}) -> T*;

  template<typename T> auto assign(T* target) -> void {}
  template<typename T, typename U, typename... P> auto assign(T* target, const U& value, P&&... p) -> void;

  template<u32 size, typename T = u64> auto readl(const void* source) -> T;
  template<u32 size, typename T = u64> auto readm(const void* source) -> T;

  template<u32 size, typename T = u64> auto writel(void* target, T data) -> void;
  template<u32 size, typename T = u64> auto writem(void* target, T data) -> void;

  auto map(u32 size, bool executable) -> void*;
  auto unmap(void* target, u32 size) -> void;
  auto protect(void* target, u32 size, bool executable) -> bool;
  auto jitprotect(bool executable) -> void;
}

namespace nall::memory {

//implementation notes:
//memcmp, memcpy, memmove have terrible performance on small block sizes (FreeBSD 10.0-amd64)
//as this library is used extensively by nall/string, and most strings tend to be small,
//this library hand-codes these functions instead. surprisingly, it's a substantial speedup

template<typename T, u32 Align> auto allocate(u32 size) -> T* {
  if constexpr(Align == 0) {
    return (T*)malloc(size * sizeof(T));
  }
  #if defined(API_WINDOWS)
  return (T*)_aligned_malloc(size * sizeof(T), Align);
  #elif defined(API_POSIX)
  T* result = nullptr;
  posix_memalign((void**)&result, Align, size * sizeof(T));
  return result;
  #else
  return (T*)malloc(size * sizeof(T));
  #endif
}

template<typename T, u32 Align> auto allocate(u32 size, const T& value) -> T* {
  auto result = allocate<T, Align>(size);
  if(result) fill<T>(result, size, value);
  return result;
}

template<typename T, u32 Align> auto resize(void* target, u32 size) -> T* {
  if constexpr(Align == 0) {
    return (T*)realloc(target, size * sizeof(T));
  }
  #if defined(API_WINDOWS)
  return (T*)_aligned_realloc(target, size * sizeof(T), Align);
  #elif defined(API_POSIX)
  //realloc() cannot be used safely with posix_memalign(); a copy is always required
  T* result = allocate<T, Align>(size);
  copy<T>(result, target, size);
  free(target);
  return result;
  #else
  return (T*)realloc(target, size * sizeof(T));
  #endif
}

template<typename T, u32 Align> auto free(void* target) -> void {
  if constexpr(Align == 0) {
    ::free(target);
    return;
  }
  #if defined(API_WINDOWS)
  _aligned_free(target);
  #else
  ::free(target);
  #endif
}

template<typename T> auto compare(const void* target, u32 capacity, const void* source, u32 size) -> s32 {
  auto t = (u8*)target;
  auto s = (u8*)source;
  auto l = min(capacity, size) * sizeof(T);
  while(l--) {
    auto x = *t++;
    auto y = *s++;
    if(x != y) return x - y;
  }
  if(capacity == size) return 0;
  return -(capacity < size);
}

template<typename T> auto compare(const void* target, const void* source, u32 size) -> s32 {
  return compare<T>(target, size, source, size);
}

template<typename T> auto icompare(const void* target, u32 capacity, const void* source, u32 size) -> s32 {
  auto t = (u8*)target;
  auto s = (u8*)source;
  auto l = min(capacity, size) * sizeof(T);
  while(l--) {
    auto x = *t++;
    auto y = *s++;
    if(x - 'A' < 26) x += 32;
    if(y - 'A' < 26) y += 32;
    if(x != y) return x - y;
  }
  return -(capacity < size);
}

template<typename T> auto icompare(const void* target, const void* source, u32 size) -> s32 {
  return icompare<T>(target, size, source, size);
}

template<typename T> auto copy(void* target, u32 capacity, const void* source, u32 size) -> T* {
  auto t = (u8*)target;
  auto s = (u8*)source;
  auto l = min(capacity, size) * sizeof(T);
  while(l--) *t++ = *s++;
  return (T*)target;
}

template<typename T> auto copy(void* target, const void* source, u32 size) -> T* {
  return copy<T>(target, size, source, size);
}

template<typename T> auto move(void* target, u32 capacity, const void* source, u32 size) -> T* {
  auto t = (u8*)target;
  auto s = (u8*)source;
  auto l = min(capacity, size) * sizeof(T);
  if(t < s) {
    while(l--) *t++ = *s++;
  } else {
    t += l;
    s += l;
    while(l--) *--t = *--s;
  }
  return (T*)target;
}

template<typename T> auto move(void* target, const void* source, u32 size) -> T* {
  return move<T>(target, size, source, size);
}

template<typename T> auto fill(void* target, u32 capacity, const T& value) -> T* {
  auto t = (T*)target;
  while(capacity--) *t++ = value;
  return (T*)target;
}

template<typename T, typename U, typename... P> auto assign(T* target, const U& value, P&&... p) -> void {
  *target++ = value;
  assign(target, std::forward<P>(p)...);
}

template<u32 size, typename T> auto readl(const void* source) -> T {
  auto p = (const u8*)source;
  T data = 0;
  for(u32 n = 0; n < size; n++) data |= T(*p++) << n * 8;
  return data;
}

template<u32 size, typename T> auto readm(const void* source) -> T {
  auto p = (const u8*)source;
  T data = 0;
  for(s32 n = size - 1; n >= 0; n--) data |= T(*p++) << n * 8;
  return data;
}

template<u32 size, typename T> auto writel(void* target, T data) -> void {
  auto p = (u8*)target;
  for(u32 n = 0; n < size; n++) *p++ = data >> n * 8;
}

template<u32 size, typename T> auto writem(void* target, T data) -> void {
  auto p = (u8*)target;
  for(s32 n = size - 1; n >= 0; n--) *p++ = data >> n * 8;
}

inline auto jitprotect(bool executable) -> void {
  #if defined(PLATFORM_MACOS)
  if(__builtin_available(macOS 11.0, *)) {
    static thread_local s32 depth = 0;
    if(!executable &&   depth++ == 0
    ||  executable && --depth   == 0) {
      pthread_jit_write_protect_np(executable);
    }
    #if defined(DEBUG)
    struct unmatched_jitprotect {};
    if(depth < 0 || depth > 10) throw unmatched_jitprotect{};
    #endif
  }
  #endif
}

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/memory.cpp>
#endif
