#include <ares/ares.hpp>

namespace ares::Memory {

constexpr u32 fixedBufferSize = 8_MiB;

#if defined(PLATFORM_MACOS)
//dynamic allocation for unsupported platforms
FixedAllocator::FixedAllocator() {
  _allocator.resize(fixedBufferSize, bump_allocator::executable);
}
#else
alignas(4096) u8 fixedBuffer[fixedBufferSize];

FixedAllocator::FixedAllocator() {
  _allocator.resize(sizeof(fixedBuffer), 0, fixedBuffer);
}
#endif

auto FixedAllocator::get() -> bump_allocator& {
  static FixedAllocator allocator;
  return allocator._allocator;
}

}
