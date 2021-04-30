#pragma once

namespace Emulator {

struct Random {
  enum class Entropy : uint { None, Low, High };

  auto operator()() -> uint64 {
    return random();
  }

  auto entropy(Entropy entropy) -> void {
    _entropy = entropy;
    seed();
  }

  auto seed(maybe<uint32> seed = nothing, maybe<uint32> sequence = nothing) -> void {
    if(!seed) seed = (uint32)clock();
    if(!sequence) sequence = 0;

    _state = 0;
    _increment = sequence() << 1 | 1;
    step();
    _state += seed();
    step();
  }

  auto random() -> uint64 {
    if(_entropy == Entropy::None) return 0;
    return (uint64)step() << 32 | (uint64)step() << 0;
  }

  auto bias(uint64 bias) -> uint64 {
    if(_entropy == Entropy::None) return bias;
    return random();
  }

  auto bound(uint64 bound) -> uint64 {
    uint64 threshold = -bound % bound;
    while(true) {
      uint64 result = random();
      if(result >= threshold) return result % bound;
    }
  }

  auto array(uint8* data, uint32 size) -> void {
    if(_entropy == Entropy::None) {
      memory::fill(data, size);
      return;
    }

    if(_entropy == Entropy::High) {
      for(uint32 address : range(size)) {
        data[address] = random();
      }
      return;
    }

    //Entropy::Low
    uint lobit = random() & 3;
    uint hibit = (lobit + 8 + (random() & 3)) & 15;
    uint lovalue = random() & 255;
    uint hivalue = random() & 255;
    if((random() & 3) == 0) lovalue = 0;
    if((random() & 1) == 0) hivalue = ~lovalue;

    for(uint32 address : range(size)) {
      uint8 value = (address & 1ull << lobit) ? lovalue : hivalue;
      if((address & 1ull << hibit)) value = ~value;
      if((random() &  511) == 0) value ^= 1 << (random() & 7);
      if((random() & 2047) == 0) value ^= 1 << (random() & 7);
      data[address] = value;
    }
  }

  auto serialize(serializer& s) -> void {
    s.integer((uint&)_entropy);
    s.integer(_state);
    s.integer(_increment);
  }

private:
  auto step() -> uint32 {
    uint64 state = _state;
    _state = state * 6364136223846793005ull + _increment;
    uint32 xorshift = (state >> 18 ^ state) >> 27;
    uint32 rotate = state >> 59;
    return xorshift >> rotate | xorshift << (-rotate & 31);
  }

  Entropy _entropy = Entropy::High;
  uint64 _state;
  uint64 _increment;
};

}
