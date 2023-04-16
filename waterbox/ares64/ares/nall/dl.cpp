#include <nall/dl.hpp>

namespace nall {

#if defined(PLATFORM_WINDOWS)

NALL_HEADER_INLINE auto library::open(const string& name, const string& path) -> bool {
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

NALL_HEADER_INLINE auto library::openAbsolute(const string& name) -> bool {
  if(handle) close();
  handle = (uintptr)LoadLibraryW(utf16_t(name));
  return handle;
}

NALL_HEADER_INLINE auto library::sym(const string& name) -> void* {
  if(!handle) return nullptr;
  return (void*)GetProcAddress((HMODULE)handle, name);
}

NALL_HEADER_INLINE auto library::close() -> void {
  if(!handle) return;
  FreeLibrary((HMODULE)handle);
  handle = 0;
}

#endif

}
