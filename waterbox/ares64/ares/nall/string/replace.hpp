#pragma once

namespace nall {

template<bool Insensitive, bool Quoted>
inline auto string::_replace(string_view from, string_view to, long limit) -> string& {
  if(limit <= 0 || from.size() == 0) return *this;

  s32 size = this->size();
  s32 matches = 0;
  s32 quoted = 0;

  //count matches first, so that we only need to reallocate memory once
  //(recording matches would also require memory allocation, so this is not done)
  { const char* p = data();
    for(s32 n = 0; n <= size - (s32)from.size();) {
      if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
      if(_compare<Insensitive>(p + n, size - n, from.data(), from.size())) { n++; continue; }

      if(++matches >= limit) break;
      n += from.size();
    }
  }
  if(matches == 0) return *this;

  //in-place overwrite
  if(to.size() == from.size()) {
    char* p = get();

    for(s32 n = 0, remaining = matches, quoted = 0; n <= size - (s32)from.size();) {
      if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
      if(_compare<Insensitive>(p + n, size - n, from.data(), from.size())) { n++; continue; }

      memory::copy(p + n, to.data(), to.size());

      if(!--remaining) break;
      n += from.size();
    }
  }

  //left-to-right shrink
  else if(to.size() < from.size()) {
    char* p = get();
    s32 offset = 0;
    s32 base = 0;

    for(s32 n = 0, remaining = matches, quoted = 0; n <= size - (s32)from.size();) {
      if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
      if(_compare<Insensitive>(p + n, size - n, from.data(), from.size())) { n++; continue; }

      if(base) memory::move(p + offset, p + base, n - base);
      memory::copy(p + offset + (n - base), to.data(), to.size());
      offset += (n - base) + to.size();

      n += from.size();
      base = n;
      if(!--remaining) break;
    }

    memory::move(p + offset, p + base, size - base);
    resize(size - matches * (from.size() - to.size()));
  }

  //right-to-left expand
  else if(to.size() > from.size()) {
    resize(size + matches * (to.size() - from.size()));
    char* p = get();

    s32 offset = this->size();
    s32 base = size;

    for(s32 n = size, remaining = matches; n >= (s32)from.size();) {  //quoted reused from parent scope since we are iterating backward
      if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n--; continue; } if(quoted) { n--; continue; } }
      if(_compare<Insensitive>(p + n - from.size(), size - n + from.size(), from.data(), from.size())) { n--; continue; }

      memory::move(p + offset - (base - n), p + base - (base - n), base - n);
      memory::copy(p + offset - (base - n) - to.size(), to.data(), to.size());
      offset -= (base - n) + to.size();

      if(!--remaining) break;
      n -= from.size();
      base = n;
    }
  }

  return *this;
}

inline auto string::replace(string_view from, string_view to, long limit) -> string& { return _replace<0, 0>(from, to, limit); }
inline auto string::ireplace(string_view from, string_view to, long limit) -> string& { return _replace<1, 0>(from, to, limit); }
inline auto string::qreplace(string_view from, string_view to, long limit) -> string& { return _replace<0, 1>(from, to, limit); }
inline auto string::iqreplace(string_view from, string_view to, long limit) -> string& { return _replace<1, 1>(from, to, limit); }

};
