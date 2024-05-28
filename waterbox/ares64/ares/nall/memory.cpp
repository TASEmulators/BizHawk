#include <nall/memory.hpp>

namespace nall::memory {

NALL_HEADER_INLINE auto map(u32 size, bool executable) -> void* {
  #if defined(API_WINDOWS)
  DWORD protect = executable ? PAGE_EXECUTE_READWRITE : PAGE_READWRITE;
  return VirtualAlloc(nullptr, size, MEM_RESERVE | MEM_COMMIT, protect);
  #elif defined(API_POSIX)
  int prot = PROT_READ | PROT_WRITE;
  int flags = MAP_ANON | MAP_PRIVATE;
  if(executable) {
    prot |= PROT_EXEC;
    #if defined(PLATFORM_MACOS)
    flags |= MAP_JIT;
    #endif
  }
  return mmap(nullptr, size, prot, flags, -1, 0);
  #else
  return nullptr;
  #endif
}

NALL_HEADER_INLINE auto unmap(void* target, u32 size) -> void {
  #if defined(API_WINDOWS)
  VirtualFree(target, 0, MEM_RELEASE);
  #elif defined(API_POSIX)
  munmap(target, size);
  #endif
}

NALL_HEADER_INLINE auto protect(void* target, u32 size, bool executable) -> bool {
  #if defined(API_WINDOWS)
  DWORD protect = executable ? PAGE_EXECUTE_READWRITE : PAGE_READWRITE;
  DWORD oldProtect;
  return VirtualProtect(target, size, protect, &oldProtect);
  #elif defined(API_POSIX)
  int prot = PROT_READ | PROT_WRITE;
  if(executable) {
    prot |= PROT_EXEC;
  }
  return !mprotect(target, size, prot);
  #endif
}

}
