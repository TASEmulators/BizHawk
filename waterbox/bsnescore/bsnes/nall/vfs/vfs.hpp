#pragma once

#include <nall/range.hpp>
#include <nall/shared-pointer.hpp>

namespace nall::vfs {

struct file {
  enum class mode : uint { read, write, modify, create };
  enum class index : uint { absolute, relative };

  virtual ~file() = default;

  virtual auto size() const -> uintmax = 0;
  virtual auto offset() const -> uintmax = 0;

  virtual auto seek(intmax offset, index = index::absolute) -> void = 0;
  virtual auto read() -> uint8_t = 0;
  virtual auto write(uint8_t data) -> void = 0;
  virtual auto flush() -> void {}

  auto end() const -> bool {
    return offset() >= size();
  }

  auto read(void* vdata, uintmax bytes) -> void {
    auto data = (uint8_t*)vdata;
    while(bytes--) *data++ = read();
  }

  auto readl(uint bytes) -> uintmax {
    uintmax data = 0;
    for(auto n : range(bytes)) data |= (uintmax)read() << n * 8;
    return data;
  }

  auto readm(uint bytes) -> uintmax {
    uintmax data = 0;
    for(auto n : range(bytes)) data = data << 8 | read();
    return data;
  }

  auto reads() -> string {
    string s;
    s.resize(size());
    read(s.get<uint8_t>(), s.size());
    return s;
  }

  auto write(const void* vdata, uintmax bytes) -> void {
    auto data = (const uint8_t*)vdata;
    while(bytes--) write(*data++);
  }

  auto writel(uintmax data, uint bytes) -> void {
    for(auto n : range(bytes)) write(data), data >>= 8;
  }

  auto writem(uintmax data, uint bytes) -> void {
    for(auto n : reverse(range(bytes))) write(data >> n * 8);
  }

  auto writes(const string& s) -> void {
    write(s.data<uint8_t>(), s.size());
  }
};

}

#include <nall/vfs/fs/file.hpp>
#include <nall/vfs/memory/file.hpp>
