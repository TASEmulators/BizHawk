#pragma once

namespace nall {

inline auto string::contains(string_view characters) const -> maybe<u32> {
  for(u32 x : range(size())) {
    for(char y : characters) {
      if(operator[](x) == y) return x;
    }
  }
  return nothing;
}

template<bool Insensitive, bool Quoted> inline auto string::_find(s32 offset, string_view source) const -> maybe<u32> {
  if(source.size() == 0) return nothing;
  auto p = data();
  for(u32 n = offset, quoted = 0; n < size();) {
    if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
    if(_compare<Insensitive>(p + n, size() - n, source.data(), source.size())) { n++; continue; }
    return n - offset;
  }
  return nothing;
}

inline auto string::find(string_view source) const -> maybe<u32> { return _find<0, 0>(0, source); }
inline auto string::ifind(string_view source) const -> maybe<u32> { return _find<1, 0>(0, source); }
inline auto string::qfind(string_view source) const -> maybe<u32> { return _find<0, 1>(0, source); }
inline auto string::iqfind(string_view source) const -> maybe<u32> { return _find<1, 1>(0, source); }

inline auto string::findFrom(s32 offset, string_view source) const -> maybe<u32> { return _find<0, 0>(offset, source); }
inline auto string::ifindFrom(s32 offset, string_view source) const -> maybe<u32> { return _find<1, 0>(offset, source); }

inline auto string::findNext(s32 offset, string_view source) const -> maybe<u32> {
  if(source.size() == 0) return nothing;
  for(s32 n = offset + 1; n < size(); n++) {
    if(memory::compare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

inline auto string::ifindNext(s32 offset, string_view source) const -> maybe<u32> {
  if(source.size() == 0) return nothing;
  for(s32 n = offset + 1; n < size(); n++) {
    if(memory::icompare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

inline auto string::findPrevious(s32 offset, string_view source) const -> maybe<u32> {
  if(source.size() == 0) return nothing;
  for(s32 n = offset - 1; n >= 0; n--) {
    if(memory::compare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

inline auto string::ifindPrevious(s32 offset, string_view source) const -> maybe<u32> {
  if(source.size() == 0) return nothing;
  for(s32 n = offset - 1; n >= 0; n--) {
    if(memory::icompare(data() + n, size() - n, source.data(), source.size()) == 0) return n;
  }
  return nothing;
}

}
