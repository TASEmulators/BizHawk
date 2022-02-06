#pragma once

namespace ares {

struct Random {
  enum class Entropy : u32 { None, Low, High };

  auto operator()() -> n64 {
    return random();
  }

  auto entropy(Entropy entropy) -> void {
    _entropy = entropy;
    seed();
  }

  auto seed(maybe<n32> seed = nothing, maybe<n32> sequence = nothing) -> void {
    if(!seed) seed = (n32)clock();
    if(!sequence) sequence = 0;

    _state = 0;
    _increment = sequence() << 1 | 1;
    step();
    _state += seed();
    step();
  }

  auto random() -> n64 {
    if(_entropy == Entropy::None) return 0;
    return (n64)step() << 32 | (n64)step() << 0;
  }

  auto bias(n64 bias) -> n64 {
    if(_entropy == Entropy::None) return bias;
    return random();
  }

  auto bound(n64 bound) -> n64 {
    n64 threshold = -bound % bound;
    while(true) {
      n64 result = random();
      if(result >= threshold) return result % bound;
    }
  }

  auto array(array_span<n8> buffer) -> void {
    if(_entropy == Entropy::None) {
      memory::fill(buffer.data(), buffer.size());
      return;
    }

    if(_entropy == Entropy::High) {
      for(n32 address : range(buffer.size())) {
        buffer[address] = random();
      }
      return;
    }

    //Entropy::Low
    u32 lobit = random() & 3;
    u32 hibit = (lobit + 8 + (random() & 3)) & 15;
    u32 lovalue = random() & 255;
    u32 hivalue = random() & 255;
    if((random() & 3) == 0) lovalue = 0;
    if((random() & 1) == 0) hivalue = ~lovalue;

    for(n32 address : range(buffer.size())) {
      n8 value = address.bit(lobit) ? lovalue : hivalue;
      if(address.bit(hibit)) value = ~value;
      if((random() &  511) == 0) value.bit(random() & 7) ^= 1;
      if((random() & 2047) == 0) value.bit(random() & 7) ^= 1;
      buffer[address] = value;
    }
  }

  auto serialize(serializer& s) -> void {
    s((u32&)_entropy);
    s(_state);
    s(_increment);
  }

private:
  auto step() -> n32 {
    n64 state = _state;
    _state = state * 6364136223846793005ull + _increment;
    n32 xorshift = (state >> 18 ^ state) >> 27;
    n32 rotate = state >> 59;
    return xorshift >> rotate | xorshift << (-rotate & 31);
  }

  Entropy _entropy = Entropy::High;
  n64 _state;
  n64 _increment;
};

}
