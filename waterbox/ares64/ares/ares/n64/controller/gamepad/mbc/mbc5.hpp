struct Mbc5 : Mbc {
  explicit Mbc5(Memory::Readable& rom_, Memory::Writable& ram_) : Mbc(rom_, ram_) { reset(); }

  auto reset() -> void override {
    romBank = 1;
    ramBank = 0;
    ramEnable = 0;
  }

  auto read(u16 address) -> u8 override {
    static constexpr u8 unmapped = 0xff;
    switch(address) {
      case 0x0000 ... 0x3fff:
        return rom.read<Byte>(address);
      case 0x4000 ... 0x7fff:
        return rom.read<Byte>(romBank * 0x4000 + address - 0x4000);
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
        ramEnable = data == 0x0a;
        return;
      case 0x2000 ... 0x2fff:
        romBank.bit(0,7) = data;
        return;
      case 0x3000 ... 0x3fff:
        romBank.bit(8) = data.bit(0);
        return;
      case 0x4000 ... 0x5fff:
        ramBank = data.bit(0,3);
        return;
      case 0xa000 ... 0xbfff:
        if(ramEnable) ram.write<Byte>(ramBank * 0x2000 + address - 0xa000, data);
        return;
    }
  }

private:
  n9 romBank;
  n4 ramBank;
  n1 ramEnable;
};
