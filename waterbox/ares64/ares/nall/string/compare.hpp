#pragma once

namespace nall {

template<bool Insensitive>
inline auto string::_compare(const char* target, u32 capacity, const char* source, u32 size) -> s32 {
  if(Insensitive) return memory::icompare(target, capacity, source, size);
  return memory::compare(target, capacity, source, size);
}

//size() + 1 includes null-terminator; required to properly compare strings of differing lengths
inline auto string::compare(string_view x, string_view y) -> s32 {
  return memory::compare(x.data(), x.size() + 1, y.data(), y.size() + 1);
}

inline auto string::icompare(string_view x, string_view y) -> s32 {
  return memory::icompare(x.data(), x.size() + 1, y.data(), y.size() + 1);
}

inline auto string::compare(string_view source) const -> s32 {
  return memory::compare(data(), size() + 1, source.data(), source.size() + 1);
}

inline auto string::icompare(string_view source) const -> s32 {
  return memory::icompare(data(), size() + 1, source.data(), source.size() + 1);
}

inline auto string::equals(string_view source) const -> bool {
  if(size() != source.size()) return false;
  return memory::compare(data(), source.data(), source.size()) == 0;
}

inline auto string::iequals(string_view source) const -> bool {
  if(size() != source.size()) return false;
  return memory::icompare(data(), source.data(), source.size()) == 0;
}

inline auto string::beginsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::compare(data(), source.data(), source.size()) == 0;
}

inline auto string::ibeginsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::icompare(data(), source.data(), source.size()) == 0;
}

inline auto string::endsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::compare(data() + size() - source.size(), source.data(), source.size()) == 0;
}

inline auto string::iendsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::icompare(data() + size() - source.size(), source.data(), source.size()) == 0;
}

}
