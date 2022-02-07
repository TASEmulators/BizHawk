#pragma once

#include <nall/stdint.hpp>

namespace nall {

constexpr inline auto toBinary_(const char* s, uintmax sum = 0) -> uintmax {
  return (
    *s == '0' || *s == '1' ? toBinary_(s + 1, (sum << 1) | *s - '0') :
    *s == '\'' ? toBinary_(s + 1, sum) :
    sum
  );
}

constexpr inline auto toOctal_(const char* s, uintmax sum = 0) -> uintmax {
  return (
    *s >= '0' && *s <= '7' ? toOctal_(s + 1, (sum << 3) | *s - '0') :
    *s == '\'' ? toOctal_(s + 1, sum) :
    sum
  );
}

constexpr inline auto toDecimal_(const char* s, uintmax sum = 0) -> uintmax {
  return (
    *s >= '0' && *s <= '9' ? toDecimal_(s + 1, (sum * 10) + *s - '0') :
    *s == '\'' ? toDecimal_(s + 1, sum) :
    sum
  );
}

constexpr inline auto toHex_(const char* s, uintmax sum = 0) -> uintmax {
  return (
    *s >= 'A' && *s <= 'F' ? toHex_(s + 1, (sum << 4) | *s - 'A' + 10) :
    *s >= 'a' && *s <= 'f' ? toHex_(s + 1, (sum << 4) | *s - 'a' + 10) :
    *s >= '0' && *s <= '9' ? toHex_(s + 1, (sum << 4) | *s - '0') :
    *s == '\'' ? toHex_(s + 1, sum) :
    sum
  );
}

//

constexpr inline auto toBinary(const char* s) -> uintmax {
  return (
    *s == '0' && (*(s + 1) == 'B' || *(s + 1) == 'b') ? toBinary_(s + 2) :
    *s == '%' ? toBinary_(s + 1) : toBinary_(s)
  );
}

constexpr inline auto toOctal(const char* s) -> uintmax {
  return (
    *s == '0' && (*(s + 1) == 'O' || *(s + 1) == 'o') ? toOctal_(s + 2) :
    toOctal_(s)
  );
}

constexpr inline auto toHex(const char* s) -> uintmax {
  return (
    *s == '0' && (*(s + 1) == 'X' || *(s + 1) == 'x') ? toHex_(s + 2) :
    *s == '$' ? toHex_(s + 1) : toHex_(s)
  );
}

//

constexpr inline auto toNatural(const char* s) -> uintmax {
  return (
    *s == '0' && (*(s + 1) == 'B' || *(s + 1) == 'b') ? toBinary_(s + 2) :
    *s == '0' && (*(s + 1) == 'O' || *(s + 1) == 'o') ? toOctal_(s + 2) :
    *s == '0' && (*(s + 1) == 'X' || *(s + 1) == 'x') ? toHex_(s + 2) :
    *s == '%' ? toBinary_(s + 1) : *s == '$' ? toHex_(s + 1) : toDecimal_(s)
  );
}

constexpr inline auto toInteger(const char* s) -> intmax {
  return (
    *s == '+' ? +toNatural(s + 1) : *s == '-' ? -toNatural(s + 1) : toNatural(s)
  );
}

//

inline auto toReal(const char* s) -> double {
  return atof(s);
}

}
