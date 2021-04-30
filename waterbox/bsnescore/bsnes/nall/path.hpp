#pragma once

#include <nall/string.hpp>

namespace nall::Path {

inline auto active() -> string {
  char path[PATH_MAX] = "";
  (void)getcwd(path, PATH_MAX);
  string result = path;
  if(!result) result = ".";
  result.transform("\\", "/");
  if(!result.endsWith("/")) result.append("/");
  return result;
}

inline auto real(string_view name) -> string {
  string result;
  char path[PATH_MAX] = "";
  if(::realpath(name, path)) result = Location::path(string{path}.transform("\\", "/"));
  if(!result) return active();
  result.transform("\\", "/");
  if(!result.endsWith("/")) result.append("/");
  return result;
}

inline auto program() -> string {
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

// /
// c:/
inline auto root() -> string {
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

// /home/username/
// c:/users/username/
inline auto user() -> string {
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

// /home/username/Desktop/
// c:/users/username/Desktop/
inline auto desktop(string_view name = {}) -> string {
  return {user(), "Desktop/", name};
}

//todo: MacOS uses the same location for userData() and userSettings()
//... is there a better option here?

// /home/username/.config/
// ~/Library/Application Support/
// c:/users/username/appdata/roaming/
inline auto userSettings() -> string {
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

// /home/username/.local/share/
// ~/Library/Application Support/
// c:/users/username/appdata/local/
inline auto userData() -> string {
  #if defined(PLATFORM_WINDOWS)
  wchar_t path[PATH_MAX] = L"";
  SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA | CSIDL_FLAG_CREATE, nullptr, 0, path);
  string result = (const char*)utf8_t(path);
  result.transform("\\", "/");
  #elif defined(PLATFORM_MACOS)
  string result = {Path::user(), "Library/Application Support/"};
  #else
  string result;
  if(const char *env = getenv("XDG_DATA_HOME")) {
    result = string(env);
  } else {
    result = {Path::user(), ".local/share/"};
  }
  #endif
  if(!result) result = ".";
  if(!result.endsWith("/")) result.append("/");
  return result;
}

// /usr/share
// /Library/Application Support/
// c:/ProgramData/
inline auto sharedData() -> string {
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

// /tmp
// c:/users/username/AppData/Local/Temp/
inline auto temporary() -> string {
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
