#pragma once

namespace nall {

template<bool Insensitive, bool Quoted>
auto string::_replace(string_view from, string_view to, long limit) -> string& {
  if(limit <= 0 || from.size() == 0) return *this;

  int size = this->size();
  int matches = 0;
  int quoted = 0;

  //count matches first, so that we only need to reallocate memory once
  //(recording matches would also require memory allocation, so this is not done)
  { const char* p = data();
    for(int n = 0; n <= size - (int)from.size();) {
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

    for(int n = 0, remaining = matches, quoted = 0; n <= size - (int)from.size();) {
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
    int offset = 0;
    int base = 0;

    for(int n = 0, remaining = matches, quoted = 0; n <= size - (int)from.size();) {
      if(Quoted) { if(p[n] == '\"') { quoted ^= 1; n++; continue; } if(quoted) { n++; continue; } }
      if(_compare<Insensitive>(p + n, size - n, from.data(), from.size())) { n++; continue; }

      if(offset) memory::move(p + offset, p + base, n - base);
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

    int offset = this->size();
    int base = size;

    for(int n = size, remaining = matches; n >= (int)from.size();) {  //quoted reused from parent scope since we are iterating backward
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

auto string::replace(string_view from, string_view to, long limit) -> string& { return _replace<0, 0>(from, to, limit); }
auto string::ireplace(string_view from, string_view to, long limit) -> string& { return _replace<1, 0>(from, to, limit); }
auto string::qreplace(string_view from, string_view to, long limit) -> string& { return _replace<0, 1>(from, to, limit); }
auto string::iqreplace(string_view from, string_view to, long limit) -> string& { return _replace<1, 1>(from, to, limit); }

};
