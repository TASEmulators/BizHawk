#pragma once

using uint = unsigned;

namespace nall {
  //UTF-8 to UTF-16
  struct utf16_t {
    utf16_t(const char* s = "") { operator=(s); }
    ~utf16_t() { reset(); }

    utf16_t(const utf16_t&) = delete;
    auto operator=(const utf16_t&) -> utf16_t& = delete;

    auto operator=(const char* s) -> utf16_t& {
      reset();
      if(!s) s = "";
      length = MultiByteToWideChar(CP_UTF8, 0, s, -1, nullptr, 0);
      buffer = new wchar_t[length + 1];
      MultiByteToWideChar(CP_UTF8, 0, s, -1, buffer, length);
      buffer[length] = 0;
      return *this;
    }

    operator wchar_t*() { return buffer; }
    operator const wchar_t*() const { return buffer; }

    auto reset() -> void {
      delete[] buffer;
      length = 0;
    }

    auto data() -> wchar_t* { return buffer; }
    auto data() const -> const wchar_t* { return buffer; }

    auto size() const -> uint { return length; }

  private:
    wchar_t* buffer = nullptr;
    uint length = 0;
  };

  //UTF-16 to UTF-8
  struct utf8_t {
    utf8_t(const wchar_t* s = L"") { operator=(s); }
    ~utf8_t() { reset(); }

    utf8_t(const utf8_t&) = delete;
    auto operator=(const utf8_t&) -> utf8_t& = delete;

    auto operator=(const wchar_t* s) -> utf8_t& {
      reset();
      if(!s) s = L"";
      length = WideCharToMultiByte(CP_UTF8, 0, s, -1, nullptr, 0, nullptr, nullptr);
      buffer = new char[length + 1];
      WideCharToMultiByte(CP_UTF8, 0, s, -1, buffer, length, nullptr, nullptr);
      buffer[length] = 0;
      return *this;
    }

    auto reset() -> void {
      delete[] buffer;
      length = 0;
    }

    operator char*() { return buffer; }
    operator const char*() const { return buffer; }

    auto data() -> char* { return buffer; }
    auto data() const -> const char* { return buffer; }

    auto size() const -> uint { return length; }

  private:
    char* buffer = nullptr;
    uint length = 0;
  };

  inline auto utf8_arguments(int& argc, char**& argv) -> void {
    wchar_t** wargv = CommandLineToArgvW(GetCommandLineW(), &argc);
    argv = new char*[argc + 1]();
    for(uint i = 0; i < argc; i++) {
      argv[i] = new char[PATH_MAX];
      strcpy(argv[i], nall::utf8_t(wargv[i]));
    }
  }
}
