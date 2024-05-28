#include <ares/ares.hpp>

#if !defined(PLATFORM_MACOS)
#define STATIC_ALLOCATION
#endif

namespace ares::Memory {

constexpr u32 fixedBufferSize = 8_MiB;

#if defined(STATIC_ALLOCATION)
u8 fixedBuffer[fixedBufferSize + 64_KiB];
#endif

FixedAllocator::FixedAllocator() {
  u8* buffer = nullptr;

  #if defined(STATIC_ALLOCATION)
  //align to 64 KiB (maximum page size of any supported OS)
  auto offset = -(uintptr)fixedBuffer % 64_KiB;
  //set protection to executable
  if(memory::protect(fixedBuffer + offset, fixedBufferSize, true)) {
    //use static allocation
    buffer = fixedBuffer + offset;
  }
  #endif

  _allocator.resize(fixedBufferSize, bump_allocator::executable, buffer);
}

auto FixedAllocator::get() -> bump_allocator& {
  static FixedAllocator allocator;
  return allocator._allocator;
}

}
