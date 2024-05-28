#pragma once

namespace nall {

template<u32 Bits> struct BarrettReduction {
  using type = typename ArithmeticNatural<1 * Bits>::type;
  using pair = typename ArithmeticNatural<2 * Bits>::type;

  explicit BarrettReduction(type modulo) : modulo(modulo), factor(pair(1) + -pair(modulo) / modulo) {}

  //return => value % modulo
  auto operator()(pair value) const -> type {
    pair hi, lo;
    mul(value, factor, hi, lo);
    pair remainder = value - hi * modulo;
    return remainder < modulo ? remainder : remainder - modulo;
  }

private:
  const pair modulo;
  const pair factor;
};

template<typename T, u32 Bits> auto operator%(T value, const BarrettReduction<Bits>& modulo) {
  return modulo(value);
}

}
