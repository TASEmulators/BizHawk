#pragma once

namespace nall {

struct Boolean {
  static constexpr auto bits() -> u32 { return 1; }
  using btype = bool;

  Boolean() : data(false) {}
  template<typename T> Boolean(const T& value) : data(value) {}
  explicit Boolean(const char* value) { data = !strcmp(value, "true"); }

  operator bool() const { return data; }
  template<typename T> auto& operator=(const T& value) { data = value; return *this; }

  auto flip() { return data ^= 1; }
  auto raise() { return data == 0 ? data = 1, true : false; }
  auto lower() { return data == 1 ? data = 0, true : false; }

  auto flip(bool value) { return data != value ? (data = value, true) : false; }
  auto raise(bool value) { return !data && value ? (data = value, true) : (data = value, false); }
  auto lower(bool value) { return data && !value ? (data = value, true) : (data = value, false); }

  auto serialize(serializer& s) { s(data); }

private:
  btype data;
};

}
