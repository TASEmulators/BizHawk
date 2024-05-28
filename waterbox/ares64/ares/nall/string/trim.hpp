#pragma once

namespace nall {

inline auto string::trim(string_view lhs, string_view rhs, long limit) -> string& {
  trimRight(rhs, limit);
  trimLeft(lhs, limit);
  return *this;
}

inline auto string::trimLeft(string_view lhs, long limit) -> string& {
  if(lhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    s32 offset = lhs.size() * matches;
    s32 length = (s32)size() - offset;
    if(length < (s32)lhs.size()) break;
    if(memory::compare(data() + offset, lhs.data(), lhs.size()) != 0) break;
    matches++;
  }
  if(matches) remove(0, lhs.size() * matches);
  return *this;
}

inline auto string::trimRight(string_view rhs, long limit) -> string& {
  if(rhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    s32 offset = (s32)size() - rhs.size() * (matches + 1);
    s32 length = (s32)size() - offset;
    if(offset < 0 || length < (s32)rhs.size()) break;
    if(memory::compare(data() + offset, rhs.data(), rhs.size()) != 0) break;
    matches++;
  }
  if(matches) resize(size() - rhs.size() * matches);
  return *this;
}

inline auto string::itrim(string_view lhs, string_view rhs, long limit) -> string& {
  itrimRight(rhs, limit);
  itrimLeft(lhs, limit);
  return *this;
}

inline auto string::itrimLeft(string_view lhs, long limit) -> string& {
  if(lhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    s32 offset = lhs.size() * matches;
    s32 length = (s32)size() - offset;
    if(length < (s32)lhs.size()) break;
    if(memory::icompare(data() + offset, lhs.data(), lhs.size()) != 0) break;
    matches++;
  }
  if(matches) remove(0, lhs.size() * matches);
  return *this;
}

inline auto string::itrimRight(string_view rhs, long limit) -> string& {
  if(rhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    s32 offset = (s32)size() - rhs.size() * (matches + 1);
    s32 length = (s32)size() - offset;
    if(offset < 0 || length < (s32)rhs.size()) break;
    if(memory::icompare(data() + offset, rhs.data(), rhs.size()) != 0) break;
    matches++;
  }
  if(matches) resize(size() - rhs.size() * matches);
  return *this;
}

inline auto string::strip() -> string& {
  stripRight();
  stripLeft();
  return *this;
}

inline auto string::stripLeft() -> string& {
  u32 length = 0;
  while(length < size()) {
    char input = operator[](length);
    if(input != ' ' && input != '\t' && input != '\r' && input != '\n') break;
    length++;
  }
  if(length) remove(0, length);
  return *this;
}

inline auto string::stripRight() -> string& {
  u32 length = 0;
  while(length < size()) {
    bool matched = false;
    char input = operator[](size() - length - 1);
    if(input != ' ' && input != '\t' && input != '\r' && input != '\n') break;
    length++;
  }
  if(length) resize(size() - length);
  return *this;
}

}
