#pragma once

#include <nall/file.hpp>
#include <nall/decode/zip.hpp>

namespace nall::vfs {

struct memory : file {
  ~memory() { nall::memory::free(_data); }

  static auto create(u64 size = 0) -> shared_pointer<memory> {
    auto instance = shared_pointer<memory>{new memory};
    instance->_create(size);
    return instance;
  }

  static auto open(array_view<u8> view) -> shared_pointer<memory> {
    auto instance = shared_pointer<memory>{new memory};
    instance->_open(view.data(), view.size());
    return instance;
  }

  auto writable() const -> bool override { return true; }
  auto data() const -> const u8* override { return _data; }
  auto data() -> u8* override { return _data; }
  auto size() const -> u64 override { return _size; }
  auto offset() const -> u64 override { return _offset; }

  auto resize(u64 size) -> bool override {
    _data = nall::memory::resize(_data, size);
    _size = size;
    return true;
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
  memory() = default;
  memory(const file&) = delete;
  auto operator=(const memory&) -> memory& = delete;

  auto _create(u64 size) -> void {
    _size = size;
    _data = nall::memory::allocate<u8>(size, 0x00);
  }

  auto _open(const u8* data, u64 size) -> void {
    _size = size;
    _data = nall::memory::allocate(size);
    nall::memory::copy(_data, data, size);
  }

  u8* _data = nullptr;
  u64 _size = 0;
  u64 _offset = 0;
};

}
