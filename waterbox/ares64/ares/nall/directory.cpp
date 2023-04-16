#include <nall/directory.hpp>

namespace nall {

#if defined(PLATFORM_WINDOWS)

NALL_HEADER_INLINE auto directory::exists(const string& pathname) -> bool {
  if(!pathname) return false;
  string name = pathname;
  name.trim("\"", "\"");
  DWORD result = GetFileAttributes(utf16_t(name));
  if(result == INVALID_FILE_ATTRIBUTES) return false;
  return (result & FILE_ATTRIBUTE_DIRECTORY);
}

NALL_HEADER_INLINE auto directory::ufolders(const string& pathname, const string& pattern) -> vector<string> {
  if(!pathname) {
    //special root pseudo-folder (return list of drives)
    wchar_t drives[PATH_MAX] = {0};
    GetLogicalDriveStrings(PATH_MAX, drives);
    wchar_t* p = drives;
    while(*p || *(p + 1)) {
      if(!*p) *p = ';';
      p++;
    }
    return string{(const char*)utf8_t(drives)}.replace("\\", "/").split(";");
  }

  vector<string> list;
  string path = pathname;
  path.transform("/", "\\");
  if(!path.endsWith("\\")) path.append("\\");
  path.append("*");
  HANDLE handle;
  WIN32_FIND_DATA data;
  handle = FindFirstFile(utf16_t(path), &data);
  if(handle != INVALID_HANDLE_VALUE) {
    if(wcscmp(data.cFileName, L".") && wcscmp(data.cFileName, L"..")) {
      if(data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
        string name = (const char*)utf8_t(data.cFileName);
        if(name.match(pattern)) list.append(name);
      }
    }
    while(FindNextFile(handle, &data) != false) {
      if(wcscmp(data.cFileName, L".") && wcscmp(data.cFileName, L"..")) {
        if(data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
          string name = (const char*)utf8_t(data.cFileName);
          if(name.match(pattern)) list.append(name);
        }
      }
    }
    FindClose(handle);
  }
  return list;
}

NALL_HEADER_INLINE auto directory::ufiles(const string& pathname, const string& pattern) -> vector<string> {
  if(!pathname) return {};

  vector<string> list;
  string path = pathname;
  path.transform("/", "\\");
  if(!path.endsWith("\\")) path.append("\\");
  path.append("*");
  HANDLE handle;
  WIN32_FIND_DATA data;
  handle = FindFirstFile(utf16_t(path), &data);
  if(handle != INVALID_HANDLE_VALUE) {
    if((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0) {
      string name = (const char*)utf8_t(data.cFileName);
      if(name.match(pattern)) list.append(name);
    }
    while(FindNextFile(handle, &data) != false) {
      if((data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0) {
        string name = (const char*)utf8_t(data.cFileName);
        if(name.match(pattern)) list.append(name);
      }
    }
    FindClose(handle);
  }
  return list;
}

#endif

}
