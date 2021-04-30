#pragma once

#include <nall/file.hpp>
#include <nall/stdint.hpp>

#include <stdio.h>
#include <stdlib.h>
#if defined(PLATFORM_WINDOWS)
  #include <nall/windows/utf8.hpp>
#else
  #include <fcntl.h>
  #include <unistd.h>
  #include <sys/mman.h>
  #include <sys/stat.h>
  #include <sys/types.h>
#endif

#if !defined(MAP_NORESERVE)
  //not supported on FreeBSD; flag removed in 11.0
  #define MAP_NORESERVE 0
#endif

namespace nall {

struct file_map {
  struct mode { enum : uint { read, write, modify, append }; };

  file_map(const file_map&) = delete;
  auto operator=(const file_map&) = delete;

  file_map() = default;
  file_map(file_map&& source) { operator=(move(source)); }
  file_map(const string& filename, uint mode) { open(filename, mode); }

  ~file_map() { close(); }

  explicit operator bool() const { return _open; }
  auto size() const -> uint64_t { return _size; }
  auto data() -> uint8_t* { return _data; }
  auto data() const -> const uint8_t* { return _data; }

//auto operator=(file_map&& source) -> file_map&;
//auto open(const string& filename, uint mode) -> bool;
//auto close() -> void;

private:
  bool _open = false;  //zero-byte files return _data = nullptr, _size = 0
  uint8_t* _data = nullptr;
  uint64_t _size = 0;

  #if defined(API_WINDOWS)

  HANDLE _file = INVALID_HANDLE_VALUE;
  HANDLE _map  = INVALID_HANDLE_VALUE;

public:
  auto operator=(file_map&& source) -> file_map& {
    _open = source._open;
    _data = source._data;
    _size = source._size;
    _file = source._file;
    _map = source._map;

    source._open = false;
    source._data = nullptr;
    source._size = 0;
    source._file = INVALID_HANDLE_VALUE;
    source._map = INVALID_HANDLE_VALUE;

    return *this;
  }

  auto open(const string& filename, uint mode_) -> bool {
    close();
    if(file::exists(filename) && file::size(filename) == 0) return _open = true;

    int desiredAccess, creationDisposition, protection, mapAccess;

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
    if(_file == INVALID_HANDLE_VALUE) return false;

    _size = GetFileSize(_file, nullptr);

    _map = CreateFileMapping(_file, nullptr, protection, 0, _size, nullptr);
    if(_map == INVALID_HANDLE_VALUE) {
      CloseHandle(_file);
      _file = INVALID_HANDLE_VALUE;
      return false;
    }

    _data = (uint8_t*)MapViewOfFile(_map, mapAccess, 0, 0, _size);
    return _open = true;
  }

  auto close() -> void {
    if(_data) {
      UnmapViewOfFile(_data);
      _data = nullptr;
    }

    if(_map != INVALID_HANDLE_VALUE) {
      CloseHandle(_map);
      _map = INVALID_HANDLE_VALUE;
    }

    if(_file != INVALID_HANDLE_VALUE) {
      CloseHandle(_file);
      _file = INVALID_HANDLE_VALUE;
    }

    _open = false;
  }

  #else

  int _fd = -1;

public:
  auto operator=(file_map&& source) -> file_map& {
    _open = source._open;
    _data = source._data;
    _size = source._size;
    _fd = source._fd;

    source._open = false;
    source._data = nullptr;
    source._size = 0;
    source._fd = -1;

    return *this;
  }

  auto open(const string& filename, uint mode_) -> bool {
    close();
    if(file::exists(filename) && file::size(filename) == 0) return _open = true;

    int openFlags = 0;
    int mmapFlags = 0;

    switch(mode_) {
    default: return false;
    case mode::read:
      openFlags = O_RDONLY;
      mmapFlags = PROT_READ;
      break;
    case mode::write:
      openFlags = O_RDWR | O_CREAT;  //mmap() requires read access
      mmapFlags = PROT_WRITE;
      break;
    case mode::modify:
      openFlags = O_RDWR;
      mmapFlags = PROT_READ | PROT_WRITE;
      break;
    case mode::append:
      openFlags = O_RDWR | O_CREAT;
      mmapFlags = PROT_READ | PROT_WRITE;
      break;
    }

    _fd = ::open(filename, openFlags, S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP);
    if(_fd < 0) return false;

    struct stat _stat;
    fstat(_fd, &_stat);
    _size = _stat.st_size;

    _data = (uint8_t*)mmap(nullptr, _size, mmapFlags, MAP_SHARED | MAP_NORESERVE, _fd, 0);
    if(_data == MAP_FAILED) {
      _data = nullptr;
      ::close(_fd);
      _fd = -1;
      return false;
    }

    return _open = true;
  }

  auto close() -> void {
    if(_data) {
      munmap(_data, _size);
      _data = nullptr;
    }

    if(_fd >= 0) {
      ::close(_fd);
      _fd = -1;
    }

    _open = false;
  }

  #endif
};

}
