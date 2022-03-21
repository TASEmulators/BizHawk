struct Mbc {
  explicit Mbc(Memory::Readable& rom_, Memory::Writable& ram_) : rom(rom_), ram(ram_) {}

  virtual auto reset() -> void {};
  virtual auto read(u16 address) -> u8 { return 0xFF; };
  virtual auto write(u16 address, u8 data) -> void {};

protected:
  Memory::Readable& rom;
  Memory::Writable& ram;
};

#include "mbc1.hpp"
#include "mbc3.hpp"
#include "mbc5.hpp"
