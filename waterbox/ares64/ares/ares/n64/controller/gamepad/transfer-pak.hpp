#include "mbc/mbc.hpp"

struct TransferPak {
  Memory::Readable rom;
  Memory::Writable ram;
  unique_pointer<Mbc> mbc;
  bool active;

  explicit operator bool() const {
    return active;
  }

  auto canSave() -> bool {
    switch(rom.read<Byte>(0x147)) {
    case 0x03:
    case 0x06:
    case 0x09:
    case 0x0f:
    case 0x10:
    case 0x13:
    case 0x1b:
    case 0x1e:
    case 0xfe:
    case 0xff:
      return (bool)ram;
    }
    return false;
  }

  auto allocate(VFS::File romFp, VFS::File ramFp) -> void {
    active = true;
    if(!romFp) {
      mbc = new Mbc(rom, ram);
      return;
    }
    rom.allocate(max(32_KiB, romFp->size()));
    rom.load(romFp);
    u8 cartType = rom.read<Byte>(0x147);
    u8 ramBanks;
    switch(rom.read<Byte>(0x148))
    {
      case 0: ramBanks = 0; break;
      case 1: case 2: ramBanks = 1; break;
      case 3: ramBanks = 4; break;
      case 4: ramBanks = 16; break;
      case 5: ramBanks = 8; break;
      default: ramBanks = 4; break;
    }
    // todo: deal with mbcs that lie about ram size in header
    ram.allocate(ramBanks * 0x2000);
    if(ramFp) {
      ram.load(ramFp);
    }
    switch(cartType)
    {
      //case 0x00: case 0x08: case 0x09: mbc = new Mbc0(rom, ram); break;
      case 0x01: case 0x02: case 0x03: mbc = new Mbc1(rom, ram); break;
      //case 0x05: case 0x06: mbc = new Mbc2(rom, ram); break;
      //case 0x0b: case 0x0c: case 0x0d: mbc = new Mmm01(rom, ram); break;
      case 0x0f: case 0x10: case 0x11: case 0x12: case 0x13: mbc = new Mbc3(rom, ram, cartType <= 0x10); break;
      case 0x19: case 0x1a: case 0x1b: case 0x1c: case 0x1d: case 0x1e: mbc = new Mbc5(rom, ram); break;
      //case 0x20: mbc = new Mbc6(rom, ram); break;
      //case 0x22: mbc = new Mbc7(rom, ram); break;
      //case 0xfc: mbc = new PocketCamera(rom, ram); break;
      //case 0xfd: mbc = new Tama5(rom, ram); break;
      //case 0xfe: mbc = new Huc3(rom, ram); break;
      //case 0xff: mbc = new Huc1(rom, ram); break;
      default: mbc = new Mbc5(rom, ram); break;
    }
  }

  auto reset() -> void {
    rom.reset();
    ram.reset();
    mbc.reset();
    active = false;
  }

  auto read(u16 address) -> u8 {
    static constexpr u8 unmapped = 0;
    address &= 0x7fff;

    if (!pakEnable) return unmapped;
    if(address <= 0x1fff) return 0x84;
    if(address <= 0x2fff) return addressBank;
    if(address <= 0x3fff) {
      n8 status;
      status.bit(0)   = cartEnable;
      status.bit(1)   = 0;
      status.bit(2,3) = resetState;
      status.bit(4,5) = 0;
      status.bit(6)   = !(bool)rom;
      status.bit(7)   = pakEnable;
      if (cartEnable && resetState == 3) resetState = 2;
      else if (!cartEnable && resetState == 2) resetState = 1;
      else if (!cartEnable && resetState == 1) resetState = 0;
      return status;
    }
    if (!cartEnable) return unmapped;
    return mbc->read(0x4000 * addressBank + address - 0x4000);
  }

  auto write(u16 address, u8 data_) -> void {
    address &= 0x7fff;
    n8 data = data_;

    if(address <= 0x1fff) {
      bool wasEnabled = pakEnable;
      if(data == 0x84) pakEnable = 1;
      if(data == 0xfe) pakEnable = 0;
      if (!wasEnabled && pakEnable) {
        addressBank = 3;
        cartEnable = 0;
        resetState = 0;
      }
      return;
    }
    if (!pakEnable) return;
    if(address <= 0x2fff) {
      addressBank = data;
      if(data > 3) addressBank = 0;
      return;
    }
    if(address <= 0x3fff) {
      bool wasEnabled = cartEnable;
      cartEnable = data.bit(0);
      if (!wasEnabled && cartEnable) {
        resetState = 3;
        mbc->reset();
      }
      return;
    }
    if (!cartEnable) return;
    return mbc->write(0x4000 * addressBank + address - 0x4000, data_);
  }

private:
  n2 addressBank;
  n1 cartEnable;
  n2 resetState;
  n1 pakEnable = 0;
};
