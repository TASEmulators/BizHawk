struct Register {
  uint16 data = 0;
  bool modified = false;

  inline operator uint() const {
    return data;
  }

  inline auto assign(uint value) -> uint16 {
    modified = true;
    return data = value;
  }

  inline auto operator++() { return assign(data + 1); }
  inline auto operator--() { return assign(data - 1); }
  inline auto operator++(int) { uint r = data; assign(data + 1); return r; }
  inline auto operator--(int) { uint r = data; assign(data - 1); return r; }
  inline auto operator   = (uint i) { return assign(i); }
  inline auto operator  |= (uint i) { return assign(data | i); }
  inline auto operator  ^= (uint i) { return assign(data ^ i); }
  inline auto operator  &= (uint i) { return assign(data & i); }
  inline auto operator <<= (uint i) { return assign(data << i); }
  inline auto operator >>= (uint i) { return assign(data >> i); }
  inline auto operator  += (uint i) { return assign(data + i); }
  inline auto operator  -= (uint i) { return assign(data - i); }
  inline auto operator  *= (uint i) { return assign(data * i); }
  inline auto operator  /= (uint i) { return assign(data / i); }
  inline auto operator  %= (uint i) { return assign(data % i); }

  inline auto operator   = (const Register& value) { return assign(value); }

  Register() = default;
  Register(const Register&) = delete;
};

struct SFR {
  uint16_t data = 0;

  BitField<16, 1> z   {&data};  //zero flag
  BitField<16, 2> cy  {&data};  //carry flag
  BitField<16, 3> s   {&data};  //sign flag
  BitField<16, 4> ov  {&data};  //overflow flag
  BitField<16, 5> g   {&data};  //go flag
  BitField<16, 6> r   {&data};  //ROM r14 flag
  BitField<16, 8> alt1{&data};  //alt1 instruction mode
  BitField<16, 9> alt2{&data};  //alt2 instruction mode
  BitField<16,10> il  {&data};  //immediate lower 8-bit flag
  BitField<16,11> ih  {&data};  //immediate upper 8-bit flag
  BitField<16,12> b   {&data};  //with flag
  BitField<16,15> irq {&data};  //interrupt flag

  BitRange<16,8,9> alt{&data};  //composite instruction mode

  inline operator uint() const { return data & 0x9f7e; }
  inline auto& operator=(const uint value) { return data = value, *this; }
};

struct SCMR {
  uint ht;
  bool ron;
  bool ran;
  uint md;

  operator uint() const {
    return ((ht >> 1) << 5) | (ron << 4) | (ran << 3) | ((ht & 1) << 2) | (md);
  }

  auto& operator=(uint data) {
    ht  = (bool)(data & 0x20) << 1;
    ht |= (bool)(data & 0x04) << 0;
    ron = data & 0x10;
    ran = data & 0x08;
    md  = data & 0x03;
    return *this;
  }
};

struct POR {
  bool obj;
  bool freezehigh;
  bool highnibble;
  bool dither;
  bool transparent;

  operator uint() const {
    return (obj << 4) | (freezehigh << 3) | (highnibble << 2) | (dither << 1) | (transparent);
  }

  auto& operator=(uint data) {
    obj         = data & 0x10;
    freezehigh  = data & 0x08;
    highnibble  = data & 0x04;
    dither      = data & 0x02;
    transparent = data & 0x01;
    return *this;
  }
};

struct CFGR {
  bool irq;
  bool ms0;

  operator uint() const {
    return (irq << 7) | (ms0 << 5);
  }

  auto& operator=(uint data) {
    irq = data & 0x80;
    ms0 = data & 0x20;
    return *this;
  }
};

struct Registers {
  uint8 pipeline;
  uint16 ramaddr;

  Register r[16];   //general purpose registers
  SFR sfr;          //status flag register
  uint8 pbr;        //program bank register
  uint8 rombr;      //game pack ROM bank register
  bool rambr;       //game pack RAM bank register
  uint16 cbr;       //cache base register
  uint8 scbr;       //screen base register
  SCMR scmr;        //screen mode register
  uint8 colr;       //color register
  POR por;          //plot option register
  bool bramr;       //back-up RAM register
  uint8 vcr;        //version code register
  CFGR cfgr;        //config register
  bool clsr;        //clock select register

  uint romcl;       //clock ticks until romdr is valid
  uint8 romdr;      //ROM buffer data register

  uint ramcl;       //clock ticks until ramdr is valid
  uint16 ramar;     //RAM buffer address register
  uint8 ramdr;      //RAM buffer data register

  uint sreg;
  uint dreg;
  auto& sr() { return r[sreg]; }  //source register (from)
  auto& dr() { return r[dreg]; }  //destination register (to)

  auto reset() -> void {
    sfr.b    = 0;
    sfr.alt1 = 0;
    sfr.alt2 = 0;

    sreg = 0;
    dreg = 0;
  }
} regs;

struct Cache {
  uint8 buffer[512];
  bool valid[32];
} cache;

struct PixelCache {
  uint16 offset;
  uint8 bitpend;
  uint8 data[8];
} pixelcache[2];
