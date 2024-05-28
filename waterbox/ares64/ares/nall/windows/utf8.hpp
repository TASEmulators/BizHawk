#pragma once

#include <nall/stdint.hpp>

namespace nall {
  //UTF-8 to UTF-16
  struct utf16_t {
    utf16_t(const char* s = "") { operator=(s); }
    ~utf16_t() { reset(); }

    utf16_t(const utf16_t&) = delete;
    auto operator=(const utf16_t&) -> utf16_t& = delete;

    auto operator=(const char* s) -> utf16_t&;

    operator wchar_t*() { return buffer; }
    operator const wchar_t*() const { return buffer; }

    auto reset() -> void {
      delete[] buffer;
      length = 0;
    }

    auto data() -> wchar_t* { return buffer; }
    auto data() const -> const wchar_t* { return buffer; }

    auto size() const -> u32 { return length; }

  private:
    wchar_t* buffer = nullptr;
    u32 length = 0;
  };

  //UTF-16 to UTF-8
  struct utf8_t {
    utf8_t(const wchar_t* s = L"") { operator=(s); }
    ~utf8_t() { reset(); }

    utf8_t(const utf8_t&) = delete;
    auto operator=(const utf8_t&) -> utf8_t& = delete;

    auto operator=(const wchar_t* s) -> utf8_t&;

    auto reset() -> void {
      delete[] buffer;
      length = 0;
    }

    operator char*() { return buffer; }
    operator const char*() const { return buffer; }

    auto data() -> char* { return buffer; }
    auto data() const -> const char* { return buffer; }

    auto size() const -> u32 { return length; }

  private:
    char* buffer = nullptr;
    u32 length = 0;
  };

  auto utf8_arguments(int& argc, char**& argv) -> void;

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/windows/utf8.cpp>
#endif
