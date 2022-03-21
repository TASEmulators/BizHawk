struct Mbc1 : Mbc {
  explicit Mbc1(Memory::Readable& rom_, Memory::Writable& ram_) : Mbc(rom_, ram_) { reset(); }

  inline auto updateBanks() -> void {
    romBank[0].bit(5,6) = bankReg2 * bankMode;
    romBank[1].bit(0,4) = bankReg1;
    romBank[1].bit(5,6) = bankReg2;
    ramBank = bankReg2 * bankMode;
  }

  auto reset() -> void override {
    bankReg1 = 1;
    bankReg1 = 1;
    bankMode = 0;
    ramBank = 0;
    ramEnable = 0;
    updateBanks();
  }

  auto read(u16 address) -> u8 override {
    static constexpr u8 unmapped = 0xff;
    switch(address) {
      case 0x0000 ... 0x3fff:
        return rom.read<Byte>(romBank[0] * 0x4000 + address);
      case 0x4000 ... 0x7fff:
        return rom.read<Byte>(romBank[1] * 0x4000 + address - 0x4000);
      case 0xa000 ... 0xbfff:
        if(!ramEnable) return unmapped;
        return ram.read<Byte>(ramBank * 0x2000 + address - 0xa000);
      default:
        return unmapped;
    }
  }

  auto write(u16 address, u8 data_) -> void override {
    n8 data = data_;
    switch(address) {
      case 0x0000 ... 0x1fff:
        ramEnable = data.bit(0,3) == 0x0a;
        return;
      case 0x2000 ... 0x3fff:
        bankReg1 = data.bit(0,4);
        if(!bankReg1) bankReg1 = 1;
        return updateBanks();
      case 0x4000 ... 0x5fff:
        bankReg2 = data.bit(0,2);
        return updateBanks();
      case 0x6000 ... 0x7fff:
        bankMode = data.bit(0);
        return updateBanks();
      case 0xa000 ... 0xbfff:
        if(ramEnable) ram.write<Byte>(ramBank * 0x2000 + address - 0xa000, data);
        return;
    }
  }

private:
  n5 bankReg1 = 1;
  n3 bankReg2 = 0;
  n1 bankMode = 0;
  n7 romBank[2];
  n3 ramBank = 0;
  n1 ramEnable = 0;
};
