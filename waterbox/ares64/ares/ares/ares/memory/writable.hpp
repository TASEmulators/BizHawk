#pragma once

#include <ares/memory/memory.hpp>

namespace ares::Memory {

template<typename T>
struct Writable {
  ~Writable() { reset(); }

  auto reset() -> void {
    delete[] self.data;
    self.data = nullptr;
    self.size = 0;
    self.mask = 0;
  }

  auto allocate(u32 size, T fill = (T)~0ull) -> void {
    if(!size) return reset();
    delete[] self.data;
    self.size = size;
    self.mask = bit::round(self.size) - 1;
    self.data = new T[self.mask + 1];
    memory::fill<T>(self.data, self.mask + 1, fill);
  }

  auto fill(T fill = ~0ull) -> void {
    for(u32 address : range(self.size)) {
      self.data[address] = fill;
    }
  }

  auto load(VFS::File fp) -> void {
    if(!self.size) allocate(fp->size());
    fp->read({self.data, min(fp->size(), self.size * sizeof(T))});
    for(u32 address = self.size; address <= self.mask; address++) {
      self.data[address] = self.data[mirror(address, self.size)];
    }
  }

  auto save(VFS::File fp) -> void {
    fp->write({self.data, min(fp->size(), self.size * sizeof(T))});
  }

  explicit operator bool() const { return (bool)self.data; }
  auto data() -> T* { return self.data; }
  auto data() const -> const T* { return self.data; }
  auto size() const -> u32 { return self.size; }
  auto mask() const -> u32 { return self.mask; }

  auto operator[](u32 address) -> T& { return self.data[address & self.mask]; }
  auto operator[](u32 address) const -> T { return self.data[address & self.mask]; }
  auto read(u32 address) const -> T { return self.data[address & self.mask]; }
  auto write(u32 address, T data) -> void { self.data[address & self.mask] = data; }
  auto program(u32 address, T data) -> void { self.data[address & self.mask] = data; }

  auto begin() -> T* { return &self.data[0]; }
  auto end() -> T* { return &self.data[self.size]; }

  auto begin() const -> const T* { return &self.data[0]; }
  auto end() const -> const T* { return &self.data[self.size]; }

  auto serialize(serializer& s) -> void {
    s(array_span<T>{self.data, self.size});
  }

private:
  struct {
    T* data = nullptr;
    u32 size = 0;
    u32 mask = 0;
  } self;
};

}
