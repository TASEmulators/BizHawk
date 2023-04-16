#include <nall/inode.hpp>

namespace nall {

NALL_HEADER_INLINE auto inode::hidden(const string& name) -> bool {
  #if defined(PLATFORM_WINDOWS)
  auto attributes = GetFileAttributes(utf16_t(name));
  return attributes & FILE_ATTRIBUTE_HIDDEN;
  #else
  //todo: is this really the best way to do this? stat doesn't have S_ISHIDDEN ...
  return name.split("/").last().beginsWith(".");
  #endif
}

}
