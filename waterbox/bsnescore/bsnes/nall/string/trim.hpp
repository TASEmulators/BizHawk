#pragma once

namespace nall {

auto string::trim(string_view lhs, string_view rhs, long limit) -> string& {
  trimRight(rhs, limit);
  trimLeft(lhs, limit);
  return *this;
}

auto string::trimLeft(string_view lhs, long limit) -> string& {
  if(lhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    int offset = lhs.size() * matches;
    int length = (int)size() - offset;
    if(length < (int)lhs.size()) break;
    if(memory::compare(data() + offset, lhs.data(), lhs.size()) != 0) break;
    matches++;
  }
  if(matches) remove(0, lhs.size() * matches);
  return *this;
}

auto string::trimRight(string_view rhs, long limit) -> string& {
  if(rhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    int offset = (int)size() - rhs.size() * (matches + 1);
    int length = (int)size() - offset;
    if(offset < 0 || length < (int)rhs.size()) break;
    if(memory::compare(data() + offset, rhs.data(), rhs.size()) != 0) break;
    matches++;
  }
  if(matches) resize(size() - rhs.size() * matches);
  return *this;
}

auto string::itrim(string_view lhs, string_view rhs, long limit) -> string& {
  itrimRight(rhs, limit);
  itrimLeft(lhs, limit);
  return *this;
}

auto string::itrimLeft(string_view lhs, long limit) -> string& {
  if(lhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    int offset = lhs.size() * matches;
    int length = (int)size() - offset;
    if(length < (int)lhs.size()) break;
    if(memory::icompare(data() + offset, lhs.data(), lhs.size()) != 0) break;
    matches++;
  }
  if(matches) remove(0, lhs.size() * matches);
  return *this;
}

auto string::itrimRight(string_view rhs, long limit) -> string& {
  if(rhs.size() == 0) return *this;
  long matches = 0;
  while(matches < limit) {
    int offset = (int)size() - rhs.size() * (matches + 1);
    int length = (int)size() - offset;
    if(offset < 0 || length < (int)rhs.size()) break;
    if(memory::icompare(data() + offset, rhs.data(), rhs.size()) != 0) break;
    matches++;
  }
  if(matches) resize(size() - rhs.size() * matches);
  return *this;
}

auto string::strip() -> string& {
  stripRight();
  stripLeft();
  return *this;
}

auto string::stripLeft() -> string& {
  uint length = 0;
  while(length < size()) {
    char input = operator[](length);
    if(input != ' ' && input != '\t' && input != '\r' && input != '\n') break;
    length++;
  }
  if(length) remove(0, length);
  return *this;
}

auto string::stripRight() -> string& {
  uint length = 0;
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
