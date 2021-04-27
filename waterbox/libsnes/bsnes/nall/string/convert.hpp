#pragma once

namespace nall {

auto string::downcase() -> string& {
  char* p = get();
  for(uint n = 0; n < size(); n++) {
    if(p[n] >= 'A' && p[n] <= 'Z') p[n] += 0x20;
  }
  return *this;
}

auto string::qdowncase() -> string& {
  char* p = get();
  for(uint n = 0, quoted = 0; n < size(); n++) {
    if(p[n] == '\"') quoted ^= 1;
    if(!quoted && p[n] >= 'A' && p[n] <= 'Z') p[n] += 0x20;
  }
  return *this;
}

auto string::upcase() -> string& {
  char* p = get();
  for(uint n = 0; n < size(); n++) {
    if(p[n] >= 'a' && p[n] <= 'z') p[n] -= 0x20;
  }
  return *this;
}

auto string::qupcase() -> string& {
  char* p = get();
  for(uint n = 0, quoted = 0; n < size(); n++) {
    if(p[n] == '\"') quoted ^= 1;
    if(!quoted && p[n] >= 'a' && p[n] <= 'z') p[n] -= 0x20;
  }
  return *this;
}

auto string::transform(string_view from, string_view to) -> string& {
  if(from.size() != to.size() || from.size() == 0) return *this;  //patterns must be the same length
  char* p = get();
  for(uint n = 0; n < size(); n++) {
    for(uint s = 0; s < from.size(); s++) {
      if(p[n] == from[s]) {
        p[n] = to[s];
        break;
      }
    }
  }
  return *this;
}

}
