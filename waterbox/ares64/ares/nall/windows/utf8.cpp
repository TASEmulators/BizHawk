#include <nall/windows/utf8.hpp>

#include <shellapi.h>

namespace nall {

NALL_HEADER_INLINE auto utf16_t::operator=(const char* s) -> utf16_t& {
  reset();
  if(!s) s = "";
  length = MultiByteToWideChar(CP_UTF8, 0, s, -1, nullptr, 0);
  buffer = new wchar_t[length + 1];
  MultiByteToWideChar(CP_UTF8, 0, s, -1, buffer, length);
  buffer[length] = 0;
  return *this;
}

NALL_HEADER_INLINE auto utf8_t::operator=(const wchar_t* s) -> utf8_t& {
  reset();
  if(!s) s = L"";
  length = WideCharToMultiByte(CP_UTF8, 0, s, -1, nullptr, 0, nullptr, nullptr);
  buffer = new char[length + 1];
  WideCharToMultiByte(CP_UTF8, 0, s, -1, buffer, length, nullptr, nullptr);
  buffer[length] = 0;
  return *this;
}

NALL_HEADER_INLINE auto utf8_arguments(int& argc, char**& argv) -> void {
  wchar_t** wargv = CommandLineToArgvW(GetCommandLineW(), &argc);
  argv = new char*[argc + 1]();
  for(u32 i = 0; i < argc; i++) {
    utf8_t arg(wargv[i]);
    argv[i] = new char[arg.size() + 1];
    strcpy(argv[i], arg);
  }
}

}
