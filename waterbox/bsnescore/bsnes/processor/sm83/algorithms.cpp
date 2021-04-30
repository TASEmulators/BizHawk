auto SM83::ADD(uint8 target, uint8 source, bool carry) -> uint8 {
  uint16 x = target + source + carry;
  uint16 y = (uint4)target + (uint4)source + carry;
  CF = x > 0xff;
  HF = y > 0x0f;
  NF = 0;
  ZF = (uint8)x == 0;
  return x;
}

auto SM83::AND(uint8 target, uint8 source) -> uint8 {
  target &= source;
  CF = 0;
  HF = 1;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::BIT(uint3 index, uint8 target) -> void {
  HF = 1;
  NF = 0;
  ZF = bit1(target,index) == 0;
}

auto SM83::CP(uint8 target, uint8 source) -> void {
  uint16 x = target - source;
  uint16 y = (uint4)target - (uint4)source;
  CF = x > 0xff;
  HF = y > 0x0f;
  NF = 1;
  ZF = (uint8)x == 0;
}

auto SM83::DEC(uint8 target) -> uint8 {
  target--;
  HF = (uint4)target == 0x0f;
  NF = 1;
  ZF = target == 0;
  return target;
}

auto SM83::INC(uint8 target) -> uint8 {
  target++;
  HF = (uint4)target == 0x00;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::OR(uint8 target, uint8 source) -> uint8 {
  target |= source;
  CF = 0;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::RL(uint8 target) -> uint8 {
  bool carry = target >> 7;
  target = target << 1 | CF;
  CF = carry;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::RLC(uint8 target) -> uint8 {
  target = target << 1 | target >> 7;
  CF = target & 1;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::RR(uint8 target) -> uint8 {
  bool carry = target & 1;
  target = CF << 7 | target >> 1;
  CF = carry;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::RRC(uint8 target) -> uint8 {
  target = target << 7 | target >> 1;
  CF = target >> 7;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::SLA(uint8 target) -> uint8 {
  bool carry = target >> 7;
  target <<= 1;
  CF = carry;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::SRA(uint8 target) -> uint8 {
  bool carry = target & 1;
  target = (int8)target >> 1;
  CF = carry;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::SRL(uint8 target) -> uint8 {
  bool carry = target & 1;
  target >>= 1;
  CF = carry;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::SUB(uint8 target, uint8 source, bool carry) -> uint8 {
  uint16 x = target - source - carry;
  uint16 y = (uint4)target - (uint4)source - carry;
  CF = x > 0xff;
  HF = y > 0x0f;
  NF = 1;
  ZF = (uint8)x == 0;
  return x;
}

auto SM83::SWAP(uint8 target) -> uint8 {
  target = target << 4 | target >> 4;
  CF = 0;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}

auto SM83::XOR(uint8 target, uint8 source) -> uint8 {
  target ^= source;
  CF = 0;
  HF = 0;
  NF = 0;
  ZF = target == 0;
  return target;
}
