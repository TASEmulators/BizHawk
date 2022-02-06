#pragma once

#include <nall/string.hpp>

namespace nall {

inline auto guid() -> string {
  GUID guidInstance;
  CoCreateGuid(&guidInstance);

  wchar_t guidString[39];
  StringFromGUID2(guidInstance, guidString, 39);

  return (char*)utf8_t(guidString);
}

}
