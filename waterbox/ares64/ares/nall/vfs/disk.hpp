#pragma once

#include <nall/file.hpp>

namespace nall::vfs {

struct disk : file {
  static auto open(string location_, mode mode_) -> shared_pointer<disk> {
    auto instance = shared_pointer<disk>{new disk};
    if(!instance->_open(location_, mode_)) return {};
    return instance;
  }

  auto writable() const -> bool override { return _writable; }
  auto data() const -> const u8* override { return _data; }
  auto data() -> u8* override { return _data; }
  auto size() const -> u64 override { return _size; }
  auto offset() const -> u64 override { return _offset; }

  auto resize(u64 size) -> bool override {
    return false;  //todo
  }

  auto seek(s64 offset, index mode = index::absolute) -> void override {
    if(mode == index::absolute) _offset  = (u64)offset;
    if(mode == index::relative) _offset += (s64)offset;
  }

  auto read() -> u8 override {
    if(_offset >= _size) return 0x00;
    return _data[_offset++];
  }

  auto write(u8 data) -> void override {
    if(_offset >= _size) return;
    _data[_offset++] = data;
  }

private:
  disk() = default;
  disk(const disk&) = delete;
  auto operator=(const disk&) -> disk& = delete;

  auto _open(string location_, mode mode_) -> bool {
    if(!_fp.open(location_, (u32)mode_)) return false;
    _data = _fp.data();
    _size = _fp.size();
    _writable = mode_ == mode::write;
    return true;
  }

  file_map _fp;
  u8* _data = nullptr;
  u64 _size = 0;
  u64 _offset = 0;
  bool _writable = false;
};

}
