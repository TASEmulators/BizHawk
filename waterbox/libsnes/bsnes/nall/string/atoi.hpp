#pragma once

namespace nall {

auto string::boolean() const -> bool {
  return equals("true");
}

auto string::integer() const -> intmax {
  return toInteger(data());
}

auto string::natural() const -> uintmax {
  return toNatural(data());
}

auto string::hex() const -> uintmax {
  return toHex(data());
}

auto string::real() const -> double {
  return toReal(data());
}

}
