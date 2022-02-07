#pragma once

namespace nall {

template<bool Insensitive>
auto string::_compare(const char* target, uint capacity, const char* source, uint size) -> int {
  if(Insensitive) return memory::icompare(target, capacity, source, size);
  return memory::compare(target, capacity, source, size);
}

//size() + 1 includes null-terminator; required to properly compare strings of differing lengths
auto string::compare(string_view x, string_view y) -> int {
  return memory::compare(x.data(), x.size() + 1, y.data(), y.size() + 1);
}

auto string::icompare(string_view x, string_view y) -> int {
  return memory::icompare(x.data(), x.size() + 1, y.data(), y.size() + 1);
}

auto string::compare(string_view source) const -> int {
  return memory::compare(data(), size() + 1, source.data(), source.size() + 1);
}

auto string::icompare(string_view source) const -> int {
  return memory::icompare(data(), size() + 1, source.data(), source.size() + 1);
}

auto string::equals(string_view source) const -> bool {
  if(size() != source.size()) return false;
  return memory::compare(data(), source.data(), source.size()) == 0;
}

auto string::iequals(string_view source) const -> bool {
  if(size() != source.size()) return false;
  return memory::icompare(data(), source.data(), source.size()) == 0;
}

auto string::beginsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::compare(data(), source.data(), source.size()) == 0;
}

auto string::ibeginsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::icompare(data(), source.data(), source.size()) == 0;
}

auto string::endsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::compare(data() + size() - source.size(), source.data(), source.size()) == 0;
}

auto string::iendsWith(string_view source) const -> bool {
  if(source.size() > size()) return false;
  return memory::icompare(data() + size() - source.size(), source.data(), source.size()) == 0;
}

}
