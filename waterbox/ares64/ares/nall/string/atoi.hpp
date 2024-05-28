#pragma once

namespace nall {

inline auto string::boolean() const -> bool {
  return equals("true");
}

inline auto string::integer() const -> s64 {
  return toInteger(data());
}

inline auto string::natural() const -> u64 {
  return toNatural(data());
}

inline auto string::hex() const -> u64 {
  return toHex(data());
}

inline auto string::real() const -> f64 {
  return toReal(data());
}

}
