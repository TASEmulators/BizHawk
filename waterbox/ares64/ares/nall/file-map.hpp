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
  struct mode { enum : u32 { read, write, modify, append }; };

  file_map(const file_map&) = delete;
  auto operator=(const file_map&) = delete;

  file_map() = default;
  file_map(file_map&& source) { operator=(std::move(source)); }
  file_map(const string& filename, u32 mode) { open(filename, mode); }

  ~file_map() { close(); }

  explicit operator bool() const { return _open; }
  auto size() const -> u64 { return _size; }
  auto data() -> u8* { return _data; }
  auto data() const -> const u8* { return _data; }

//auto operator=(file_map&& source) -> file_map&;
//auto open(const string& filename, u32 mode) -> bool;
//auto close() -> void;

private:
  bool _open = false;  //zero-byte files return _data = nullptr, _size = 0
  u8* _data = nullptr;
  u64 _size = 0;

  #if defined(API_WINDOWS)

  HANDLE _file = nullptr;
  HANDLE _map  = nullptr;

public:
  auto operator=(file_map&& source) -> file_map& {
    if(this == &source) return *this;
    close();

    _open = source._open;
    _data = source._data;
    _size = source._size;
    _file = source._file;
    _map = source._map;

    source._open = false;
    source._data = nullptr;
    source._size = 0;
    source._file = nullptr;
    source._map = nullptr;

    return *this;
  }

  auto open(const string& filename, u32 mode_) -> bool;

  auto close() -> void;

  #else

  s32 _fd = -1;

public:
  auto operator=(file_map&& source) -> file_map& {
    if(this == &source) return *this;
    close();

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

  auto open(const string& filename, u32 mode_) -> bool {
    close();
    if(file::exists(filename) && file::size(filename) == 0) return _open = true;

    s32 openFlags = 0;
    s32 mmapFlags = 0;

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

    _data = (u8*)mmap(nullptr, _size, mmapFlags, MAP_SHARED | MAP_NORESERVE, _fd, 0);
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

#if defined(NALL_HEADER_ONLY)
  #include <nall/file-map.cpp>
#endif
