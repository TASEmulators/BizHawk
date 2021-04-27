#pragma once

//only allocators may access _data or modify _size and _capacity
//all other functions must use data(), size(), capacity()

#if defined(NALL_STRING_ALLOCATOR_ADAPTIVE)
  #include <nall/string/allocator/adaptive.hpp>
#elif defined(NALL_STRING_ALLOCATOR_COPY_ON_WRITE)
  #include <nall/string/allocator/copy-on-write.hpp>
#elif defined(NALL_STRING_ALLOCATOR_SMALL_STRING_OPTIMIZATION)
  #include <nall/string/allocator/small-string-optimization.hpp>
#elif defined(NALL_STRING_ALLOCATOR_VECTOR)
  #include <nall/string/allocator/vector.hpp>
#endif

namespace nall {

auto string::operator[](uint position) const -> const char& {
  #ifdef DEBUG
  struct out_of_bounds {};
  if(position >= size() + 1) throw out_of_bounds{};
  #endif
  return data()[position];
}

auto string::operator()(uint position, char fallback) const -> char {
  if(position >= size() + 1) return fallback;
  return data()[position];
}

template<typename... P> auto string::assign(P&&... p) -> string& {
  resize(0);
  return append(forward<P>(p)...);
}

template<typename T, typename... P> auto string::prepend(const T& value, P&&... p) -> string& {
  if constexpr(sizeof...(p)) prepend(forward<P>(p)...);
  return _prepend(make_string(value));
}

template<typename... P> auto string::prepend(const nall::string_format& value, P&&... p) -> string& {
  if constexpr(sizeof...(p)) prepend(forward<P>(p)...);
  return format(value);
}

template<typename T> auto string::_prepend(const stringify<T>& source) -> string& {
  resize(source.size() + size());
  memory::move(get() + source.size(), get(), size() - source.size());
  memory::copy(get(), source.data(), source.size());
  return *this;
}

template<typename T, typename... P> auto string::append(const T& value, P&&... p) -> string& {
  _append(make_string(value));
  if constexpr(sizeof...(p) > 0) append(forward<P>(p)...);
  return *this;
}

template<typename... P> auto string::append(const nall::string_format& value, P&&... p) -> string& {
  format(value);
  if constexpr(sizeof...(p)) append(forward<P>(p)...);
  return *this;
}

template<typename T> auto string::_append(const stringify<T>& source) -> string& {
  resize(size() + source.size());
  memory::copy(get() + size() - source.size(), source.data(), source.size());
  return *this;
}

auto string::length() const -> uint {
  return strlen(data());
}

}
