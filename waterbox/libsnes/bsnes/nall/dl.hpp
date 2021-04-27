#pragma once

//dynamic linking support

#include <nall/intrinsics.hpp>
#include <nall/path.hpp>
#include <nall/stdint.hpp>
#include <nall/string.hpp>
#include <nall/utility.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <nall/windows/utf8.hpp>
#else
  #include <dlfcn.h>
#endif

namespace nall {

struct library {
  library() = default;
  ~library() { close(); }

  library& operator=(const library&) = delete;
  library(const library&) = delete;

  explicit operator bool() const { return open(); }
  auto open() const -> bool { return handle; }
  auto open(const string&, const string& = "") -> bool;
  auto openAbsolute(const string&) -> bool;
  auto sym(const string&) -> void*;
  auto close() -> void;

private:
  uintptr handle = 0;
};

#if defined(PLATFORM_LINUX) || defined(PLATFORM_BSD)
inline auto library::open(const string& name, const string& path) -> bool {
  if(handle) close();
  if(path) handle = (uintptr)dlopen(string(path, "lib", name, ".so"), RTLD_LAZY);
  if(!handle) handle = (uintptr)dlopen(string(Path::user(), ".local/lib/lib", name, ".so"), RTLD_LAZY);
  if(!handle) handle = (uintptr)dlopen(string("/usr/local/lib/lib", name, ".so"), RTLD_LAZY);
  if(!handle) handle = (uintptr)dlopen(string("lib", name, ".so"), RTLD_LAZY);
  return handle;
}

inline auto library::openAbsolute(const string& name) -> bool {
  if(handle) close();
  handle = (uintptr)dlopen(name, RTLD_LAZY);
  return handle;
}

inline auto library::sym(const string& name) -> void* {
  if(!handle) return nullptr;
  return dlsym((void*)handle, name);
}

inline auto library::close() -> void {
  if(!handle) return;
  dlclose((void*)handle);
  handle = 0;
}
#elif defined(PLATFORM_MACOS)
inline auto library::open(const string& name, const string& path) -> bool {
  if(handle) close();
  if(path) handle = (uintptr)dlopen(string(path, "lib", name, ".dylib"), RTLD_LAZY);
  if(!handle) handle = (uintptr)dlopen(string(Path::user(), ".local/lib/lib", name, ".dylib"), RTLD_LAZY);
  if(!handle) handle = (uintptr)dlopen(string("/usr/local/lib/lib", name, ".dylib"), RTLD_LAZY);
  if(!handle) handle = (uintptr)dlopen(string("lib", name, ".dylib"), RTLD_LAZY);
  return handle;
}

inline auto library::openAbsolute(const string& name) -> bool {
  if(handle) close();
  handle = (uintptr)dlopen(name, RTLD_LAZY);
  return handle;
}

inline auto library::sym(const string& name) -> void* {
  if(!handle) return nullptr;
  return dlsym((void*)handle, name);
}

inline auto library::close() -> void {
  if(!handle) return;
  dlclose((void*)handle);
  handle = 0;
}
#elif defined(PLATFORM_WINDOWS)
inline auto library::open(const string& name, const string& path) -> bool {
  if(handle) close();
  if(path) {
    string filepath = {path, name, ".dll"};
    handle = (uintptr)LoadLibraryW(utf16_t(filepath));
  }
  if(!handle) {
    string filepath = {name, ".dll"};
    handle = (uintptr)LoadLibraryW(utf16_t(filepath));
  }
  return handle;
}

inline auto library::openAbsolute(const string& name) -> bool {
  if(handle) close();
  handle = (uintptr)LoadLibraryW(utf16_t(name));
  return handle;
}

inline auto library::sym(const string& name) -> void* {
  if(!handle) return nullptr;
  return (void*)GetProcAddress((HMODULE)handle, name);
}

inline auto library::close() -> void {
  if(!handle) return;
  FreeLibrary((HMODULE)handle);
  handle = 0;
}
#else
inline auto library::open(const string&, const string&) -> bool { return false; }
inline auto library::openAbsolute(const string&) -> bool { return false; }
inline auto library::sym(const string&) -> void* { return nullptr; }
inline auto library::close() -> void {}
#endif

}
