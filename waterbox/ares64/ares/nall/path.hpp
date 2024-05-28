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

auto program() -> string;

// program()
// ./ares.app/Contents/Resources/
auto resources() -> string;

// /
// c:/
auto root() -> string;

// /home/username/
// c:/users/username/
auto user() -> string;

// /home/username/Desktop/
// c:/users/username/Desktop/
auto desktop(string_view name = {}) -> string;

//todo: MacOS uses the same location for userData() and userSettings()
//... is there a better option here?

// /home/username/.config/
// ~/Library/Application Support/
// c:/users/username/appdata/roaming/
auto userSettings() -> string;

// /home/username/.local/share/
// ~/Library/Application Support/
// c:/users/username/appdata/local/
auto userData() -> string;

// /usr/share
// /Library/Application Support/
// c:/ProgramData/
auto sharedData() -> string;

// /tmp
// c:/users/username/AppData/Local/Temp/
auto temporary() -> string;

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/path.cpp>
#endif
