auto SPC700::algorithmADC(uint8 x, uint8 y) -> uint8 {
  int z = x + y + CF;
  CF = z > 0xff;
  ZF = (uint8)z == 0;
  HF = (x ^ y ^ z) & 0x10;
  VF = ~(x ^ y) & (x ^ z) & 0x80;
  NF = z & 0x80;
  return z;
}

auto SPC700::algorithmAND(uint8 x, uint8 y) -> uint8 {
  x &= y;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmASL(uint8 x) -> uint8 {
  CF = x & 0x80;
  x <<= 1;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmCMP(uint8 x, uint8 y) -> uint8 {
  int z = x - y;
  CF = z >= 0;
  ZF = (uint8)z == 0;
  NF = z & 0x80;
  return x;
}

auto SPC700::algorithmDEC(uint8 x) -> uint8 {
  x--;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmEOR(uint8 x, uint8 y) -> uint8 {
  x ^= y;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmINC(uint8 x) -> uint8 {
  x++;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmLD(uint8 x, uint8 y) -> uint8 {
  ZF = y == 0;
  NF = y & 0x80;
  return y;
}

auto SPC700::algorithmLSR(uint8 x) -> uint8 {
  CF = x & 0x01;
  x >>= 1;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmOR(uint8 x, uint8 y) -> uint8 {
  x |= y;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmROL(uint8 x) -> uint8 {
  bool carry = CF;
  CF = x & 0x80;
  x = x << 1 | carry;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmROR(uint8 x) -> uint8 {
  bool carry = CF;
  CF = x & 0x01;
  x = carry << 7 | x >> 1;
  ZF = x == 0;
  NF = x & 0x80;
  return x;
}

auto SPC700::algorithmSBC(uint8 x, uint8 y) -> uint8 {
  return algorithmADC(x, ~y);
}

//

auto SPC700::algorithmADW(uint16 x, uint16 y) -> uint16 {
  uint16 z;
  CF = 0;
  z  = algorithmADC(x, y);
  z |= algorithmADC(x >> 8, y >> 8) << 8;
  ZF = z == 0;
  return z;
}

auto SPC700::algorithmCPW(uint16 x, uint16 y) -> uint16 {
  int z = x - y;
  CF = z >= 0;
  ZF = (uint16)z == 0;
  NF = z & 0x8000;
  return x;
}

auto SPC700::algorithmLDW(uint16 x, uint16 y) -> uint16 {
  ZF = y == 0;
  NF = y & 0x8000;
  return y;
}

auto SPC700::algorithmSBW(uint16 x, uint16 y) -> uint16 {
  uint16 z;
  CF = 1;
  z  = algorithmSBC(x, y);
  z |= algorithmSBC(x >> 8, y >> 8) << 8;
  ZF = z == 0;
  return z;
}
