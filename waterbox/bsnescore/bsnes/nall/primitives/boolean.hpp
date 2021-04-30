#pragma once

namespace nall {

struct Boolean {
  static inline constexpr auto bits() -> uint { return 1; }
  using btype = bool;

  inline Boolean() : data(false) {}
  template<typename T> inline Boolean(const T& value) : data(value) {}
  explicit inline Boolean(const char* value) { data = !strcmp(value, "true"); }

  inline operator bool() const { return data; }
  template<typename T> inline auto& operator=(const T& value) { data = value; return *this; }

  inline auto flip() { return data ^= 1; }
  inline auto raise() { return data == 0 ? data = 1, true : false; }
  inline auto lower() { return data == 1 ? data = 0, true : false; }

  inline auto flip(bool value) { return data != value ? (data = value, true) : false; }
  inline auto raise(bool value) { return !data && value ? (data = value, true) : (data = value, false); }
  inline auto lower(bool value) { return data && !value ? (data = value, true) : (data = value, false); }

  inline auto serialize(serializer& s) { s(data); }

private:
  btype data;
};

}
