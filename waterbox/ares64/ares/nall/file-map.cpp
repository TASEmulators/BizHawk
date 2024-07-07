#include <nall/file-map.hpp>

namespace nall {

#if defined(API_WINDOWS)

NALL_HEADER_INLINE auto file_map::open(const string& filename, u32 mode_) -> bool {
  close();
  if(file::exists(filename) && file::size(filename) == 0) return _open = true;

  s32 desiredAccess, creationDisposition, protection, mapAccess;

  switch(mode_) {
  default: return false;
  case mode::read:
    desiredAccess = GENERIC_READ;
    creationDisposition = OPEN_EXISTING;
    protection = PAGE_READONLY;
    mapAccess = FILE_MAP_READ;
    break;
  case mode::write:
    //write access requires read access
    desiredAccess = GENERIC_WRITE;
    creationDisposition = CREATE_ALWAYS;
    protection = PAGE_READWRITE;
    mapAccess = FILE_MAP_ALL_ACCESS;
    break;
  case mode::modify:
    desiredAccess = GENERIC_READ | GENERIC_WRITE;
    creationDisposition = OPEN_EXISTING;
    protection = PAGE_READWRITE;
    mapAccess = FILE_MAP_ALL_ACCESS;
    break;
  case mode::append:
    desiredAccess = GENERIC_READ | GENERIC_WRITE;
    creationDisposition = CREATE_NEW;
    protection = PAGE_READWRITE;
    mapAccess = FILE_MAP_ALL_ACCESS;
    break;
  }

  _file = CreateFileW(utf16_t(filename), desiredAccess, FILE_SHARE_READ, nullptr,
    creationDisposition, FILE_ATTRIBUTE_NORMAL, nullptr);
  if(_file == INVALID_HANDLE_VALUE) {
    _file = nullptr;
    return false;
  }

  _size = GetFileSize(_file, nullptr);

  _map = CreateFileMapping(_file, nullptr, protection, 0, _size, nullptr);
  if(_map == nullptr) {
    CloseHandle(_file);
    _file = nullptr;
    return false;
  }

  _data = (u8*)MapViewOfFile(_map, mapAccess, 0, 0, _size);
  return _open = true;
}

NALL_HEADER_INLINE auto file_map::close() -> void {
  if(_data) {
    UnmapViewOfFile(_data);
    _data = nullptr;
  }

  if(_map != nullptr) {
    CloseHandle(_map);
    _map = nullptr;
  }

  if(_file != nullptr) {
    CloseHandle(_file);
    _file = nullptr;
  }

  _open = false;
}

#endif

}
