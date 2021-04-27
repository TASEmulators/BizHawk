#pragma once

#include <nall/file.hpp>

namespace nall::vfs::fs {

struct file : vfs::file {
  static auto open(string location_, mode mode_) -> shared_pointer<vfs::file> {
    auto instance = shared_pointer<file>{new file};
    if(!instance->_open(location_, mode_)) return {};
    return instance;
  }

  auto size() const -> uintmax override {
    return _fp.size();
  }

  auto offset() const -> uintmax override {
    return _fp.offset();
  }

  auto seek(intmax offset_, index index_) -> void override {
    _fp.seek(offset_, (uint)index_);
  }

  auto read() -> uint8_t override {
    return _fp.read();
  }

  auto write(uint8_t data_) -> void override {
    _fp.write(data_);
  }

  auto flush() -> void override {
    _fp.flush();
  }

private:
  file() = default;
  file(const file&) = delete;
  auto operator=(const file&) -> file& = delete;

  auto _open(string location_, mode mode_) -> bool {
    if(!_fp.open(location_, (uint)mode_)) return false;
    return true;
  }

  file_buffer _fp;
};

}
