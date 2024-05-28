#include <nall/windows/guid.hpp>

#include <combaseapi.h>

namespace nall {

NALL_HEADER_INLINE auto guid() -> string {
  GUID guidInstance;
  CoCreateGuid(&guidInstance);

  wchar_t guidString[39];
  StringFromGUID2(guidInstance, guidString, 39);

  return (char*)utf8_t(guidString);
}

}
