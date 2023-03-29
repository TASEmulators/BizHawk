#include <nall/path.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <shlobj.h>
#endif

namespace nall::Path {

NALL_HEADER_INLINE auto program() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  GetModuleFileName(nullptr, path, PATH_MAX);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  return Path::real(result);
  #else
  Dl_info info;
  dladdr((void*)&program, &info);
  return Path::real(info.dli_fname);
  #endif
}

NALL_HEADER_INLINE auto root() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_WINDOWS | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  return slice(result, 0, 3);
  #else
  return "/";
  #endif
}

NALL_HEADER_INLINE auto user() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_PROFILE | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #else
  struct passwd* userinfo = getpwuid(getuid());
  string result = userinfo->pw_dir;
  #endif
  if(!result) result = ".";
  if(!result.endsWith("/")) result.append("/");
  return result;
}

NALL_HEADER_INLINE auto desktop(string_view name) -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_DESKTOP | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #elif defined(PLATFORM_MACOS)
  string result = {user(), "Desktop/"};
  #else
  string result;
  if(const char *env = getenv("XDG_DESKTOP_DIR")) {
    result = string(env);
  } else {
    result = {user(), "Desktop/"};
  }
  #endif
  if(!result) result = ".";
  if(!result.endsWith("/")) result.append("/");
  return result.append(name);
}

NALL_HEADER_INLINE auto userSettings() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_APPDATA | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #elif defined(PLATFORM_MACOS)
  string result = {Path::user(), "Library/Application Support/"};
  #else
  string result;
  if(const char *env = getenv("XDG_CONFIG_HOME")) {
    result = string(env);
  } else {
    result = {Path::user(), ".config/"};
  }
  #endif
  if(!result) result = ".";
  if(!result.endsWith("/")) result.append("/");
  return result;
}

NALL_HEADER_INLINE auto userData() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #elif defined(PLATFORM_MACOS)
  string result = {Path::user(), "Library/Application Support/"};
  #else
  string result;
  if(const char* env = getenv("XDG_DATA_HOME")) {
    result = string(env);
  } else {
    result = {Path::user(), ".local/share/"};
  }
  #endif
  if(!result) result = ".";
  if(!result.endsWith("/")) result.append("/");
  return result;
}

NALL_HEADER_INLINE auto sharedData() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_COMMON_APPDATA | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #elif defined(PLATFORM_MACOS)
  string result = "/Library/Application Support/";
  #else
  string result = "/usr/share/";
  #endif
  if(!result) result = ".";
  if(!result.endsWith("/")) result.append("/");
  return result;
}

NALL_HEADER_INLINE auto temporary() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  GetTempPathW(PATH_MAX, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #elif defined(P_tmpdir)
  string result = P_tmpdir;
  #else
  string result = "/tmp/";
  #endif
  if(!result.endsWith("/")) result.append("/");
  return result;
}

}
