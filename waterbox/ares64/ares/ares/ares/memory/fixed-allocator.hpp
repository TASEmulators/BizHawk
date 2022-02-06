#pragma once

namespace ares::Memory {

struct FixedAllocator {
  static auto get() -> bump_allocator&;

private:
  FixedAllocator();

  bump_allocator _allocator;
};

}
