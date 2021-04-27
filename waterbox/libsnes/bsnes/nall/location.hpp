#pragma once

#include <nall/string.hpp>

namespace nall::Location {

// (/parent/child.type/)
// (/parent/child.type/)name.type
inline auto path(string_view self) -> string {
  const char* p = self.data() + self.size() - 1;
  for(int offset = self.size() - 1; offset >= 0; offset--, p--) {
    if(*p == '/') return slice(self, 0, offset + 1);
  }
  return "";  //no path found
}

// /parent/child.type/()
// /parent/child.type/(name.type)
inline auto file(string_view self) -> string {
  const char* p = self.data() + self.size() - 1;
  for(int offset = self.size() - 1; offset >= 0; offset--, p--) {
    if(*p == '/') return slice(self, offset + 1);
  }
  return self;  //no path found
}

// (/parent/)child.type/
// (/parent/child.type/)name.type
inline auto dir(string_view self) -> string {
  const char* p = self.data() + self.size() - 1, *last = p;
  for(int offset = self.size() - 1; offset >= 0; offset--, p--) {
    if(*p == '/' && p == last) continue;
    if(*p == '/') return slice(self, 0, offset + 1);
  }
  return "";  //no path found
}

// /parent/(child.type/)
// /parent/child.type/(name.type)
inline auto base(string_view self) -> string {
  const char* p = self.data() + self.size() - 1, *last = p;
  for(int offset = self.size() - 1; offset >= 0; offset--, p--) {
    if(*p == '/' && p == last) continue;
    if(*p == '/') return slice(self, offset + 1);
  }
  return self;  //no path found
}

// /parent/(child).type/
// /parent/child.type/(name).type
inline auto prefix(string_view self) -> string {
  const char* p = self.data() + self.size() - 1, *last = p;
  for(int offset = self.size() - 1, suffix = -1; offset >= 0; offset--, p--) {
    if(*p == '/' && p == last) continue;
    if(*p == '/') return slice(self, offset + 1, (suffix >= 0 ? suffix : self.size()) - offset - 1).trimRight("/");
    if(*p == '.' && suffix == -1) { suffix = offset; continue; }
    if(offset == 0) return slice(self, offset, suffix).trimRight("/");
  }
  return "";  //no prefix found
}

// /parent/child(.type)/
// /parent/child.type/name(.type)
inline auto suffix(string_view self) -> string {
  const char* p = self.data() + self.size() - 1, *last = p;
  for(int offset = self.size() - 1; offset >= 0; offset--, p--) {
    if(*p == '/' && p == last) continue;
    if(*p == '/') break;
    if(*p == '.') return slice(self, offset).trimRight("/");
  }
  return "";  //no suffix found
}

inline auto notsuffix(string_view self) -> string {
  return {path(self), prefix(self)};
}

}
