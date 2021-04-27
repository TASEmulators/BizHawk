#pragma once

#include <nall/atoi.hpp>
#include <nall/serializer.hpp>
#include <nall/traits.hpp>

namespace nall {
  struct Boolean;
  template<uint Precision = 64> struct Natural;
  template<uint Precision = 64> struct Integer;
  template<uint Precision = 64> struct Real;
}

#include <nall/primitives/bit-field.hpp>
#include <nall/primitives/bit-range.hpp>
#include <nall/primitives/boolean.hpp>
#include <nall/primitives/natural.hpp>
#include <nall/primitives/integer.hpp>
#include <nall/primitives/real.hpp>
#include <nall/primitives/types.hpp>
#include <nall/primitives/literals.hpp>

namespace nall {
  template<uint Bits> auto Natural<Bits>::integer() const -> Integer<Bits> { return Integer<Bits>(*this); }
  template<uint Bits> auto Integer<Bits>::natural() const -> Natural<Bits> { return Natural<Bits>(*this); }
}
