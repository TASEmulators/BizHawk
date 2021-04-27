#pragma once

namespace nall {

auto string::contains(string_view characters) const -> maybe<uint> {
  for(uint x : range(size())) {
    for(char y : characters) {
      if(operator[](x) == y) return x;
    }
  }
  return nothing;
}

template<bool Insensitive, bool Quoted> auto string::_find(int offset, string_view source) const -> maybe<uint> {
  if(source.size() == 0) return nothing;
  auto p = data();
  for(uint n = offset, quoted = 0; n < size();) {
    if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
    if(_compare<Insensitive>(p + n, size() - n, source.data(), source.size())) { n++; continue; }
    return n - offset;
  }
  return nothing;
}

auto string::find(string_view source) const -> maybe<uint> { return _find<0, 0>(0, source); }
auto string::ifind(string_view source) const -> maybe<uint> { return _find<1, 0>(0, source); }
auto string::qfind(string_view source) const -> maybe<uint> { return _find<0, 1>(0, source); }
auto string::iqfind(string_view source) const -> maybe<uint> { return _find<1, 1>(0, source); }

auto string::findFrom(int offset, string_view source) const -> maybe<uint> { return _find<0, 0>(offset, source); }
auto string::ifindFrom(int offset, string_view source) const -> maybe<uint> { return _find<1, 0>(offset, source); }

auto string::findNext(int offset, string_view source) const -> maybe<uint> {
  if(source.size() == 0) return nothing;
  for(int n = offset + 1; n < size(); n++) {
    if(memory::compare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

auto string::ifindNext(int offset, string_view source) const -> maybe<uint> {
  if(source.size() == 0) return nothing;
  for(int n = offset + 1; n < size(); n++) {
    if(memory::icompare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

auto string::findPrevious(int offset, string_view source) const -> maybe<uint> {
  if(source.size() == 0) return nothing;
  for(int n = offset - 1; n >= 0; n--) {
    if(memory::compare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

auto string::ifindPrevious(int offset, string_view source) const -> maybe<uint> {
  if(source.size() == 0) return nothing;
  for(int n = offset - 1; n >= 0; n--) {
    if(memory::icompare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

}
