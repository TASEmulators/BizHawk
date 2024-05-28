#pragma once

#include <nall/memory.hpp>

namespace nall {

struct bump_allocator {
  static constexpr u32 executable = 1 << 0;
  static constexpr u32 zero_fill  = 1 << 1;

  ~bump_allocator() {
    reset();
  }

  explicit operator bool() const {
    return _memory;
  }

  auto reset() -> void {
    if(_owner) memory::unmap(_memory, _capacity);
    _memory = nullptr;
    _capacity = 0;
    _offset = 0;
    _owner = false;
  }

  auto resize(u32 capacity, u32 flags = 0, u8* buffer = nullptr) -> bool {
    reset();

    if(buffer) {
      if(flags & zero_fill) {
        memset(buffer, 0x00, capacity);
      }
    } else {
      buffer = (u8*)memory::map(capacity, flags & executable);
      if(!buffer) return false;
      _owner = true;
    }
    _memory = buffer;
    _capacity = capacity;

    return true;
  }

  //release all acquired memory
  auto release(u32 flags = 0) -> void {
    _offset = 0;
    if(flags & zero_fill) memset(_memory, 0x00, _capacity);
  }

  auto capacity() const -> u32 {
    return _capacity;
  }

  auto available() const -> u32 {
    return _capacity - _offset;
  }

  //for allocating blocks of known size
  auto acquire(u32 size) -> u8* {
    #ifdef DEBUG
    struct out_of_memory {};
    if((nextOffset(size)) > _capacity) throw out_of_memory{};
    #endif
    auto memory = _memory + _offset;
    _offset = nextOffset(size);  //alignment
    return memory;
  }

  //for allocating blocks of unknown size (eg for a dynamic recompiler code block)
  auto acquire() -> u8* {
    #ifdef DEBUG
    struct out_of_memory {};
    if(_offset > _capacity) throw out_of_memory{};
    #endif
    return _memory + _offset;
  }

  //size can be reserved once the block size is known
  auto reserve(u32 size) -> void {
    #ifdef DEBUG
    struct out_of_memory {};
    if((nextOffset(size)) > _capacity) throw out_of_memory{};
    #endif
    _offset = nextOffset(size);  //alignment
  }

  auto tryAcquire(u32 size, bool reserve = true) -> u8* {
    if((nextOffset(size)) > _capacity) return nullptr;
    return reserve ? acquire(size) : acquire();
  }

private:
  auto nextOffset(u32 size) const -> u32 {
    return _offset + size + 15 & ~15;
  }

  u8* _memory = nullptr;
  u32 _capacity = 0;
  u32 _offset = 0;
  bool _owner = false;
};

}
