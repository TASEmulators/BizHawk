//MCC - Memory Controller Chip
//Custom logic chip inside the BS-X Satellaview base cartridge

struct MCC {
  ReadableMemory rom;
  WritableMemory psram;

  //mcc.cpp
  auto unload() -> void;
  auto power() -> void;
  auto commit() -> void;

  auto read(uint address, uint8 data) -> uint8;
  auto write(uint address, uint8 data) -> void;

  auto mcuRead(uint address, uint8 data) -> uint8;
  auto mcuWrite(uint address, uint8 data) -> void;

  auto mcuAccess(bool mode, uint address, uint8 data) -> uint8;
  auto romAccess(bool mode, uint address, uint8 data) -> uint8;
  auto psramAccess(bool mode, uint address, uint8 data) -> uint8;
  auto exAccess(bool mode, uint address, uint8 data) -> uint8;
  auto bsAccess(bool mode, uint address, uint8 data) -> uint8;

  //serialization.cpp
  auto serialize(serializer&) -> void;

private:
  struct IRQ {
    uint1 flag;    //bit 0
    uint1 enable;  //bit 1
  } irq;

  struct Registers {
    uint1 mapping;             //bit  2 (0 = ignore A15; 1 = use A15)
    uint1 psramEnableLo;       //bit  3
    uint1 psramEnableHi;       //bit  4
    uint2 psramMapping;        //bits 5-6
    uint1 romEnableLo;         //bit  7
    uint1 romEnableHi;         //bit  8
    uint1 exEnableLo;          //bit  9
    uint1 exEnableHi;          //bit 10
    uint1 exMapping;           //bit 11
    uint1 internallyWritable;  //bit 12 (1 = MCC allows writes to BS Memory Cassette)
    uint1 externallyWritable;  //bit 13 (1 = BS Memory Cassette allows writes to flash memory)
  } r, w;

  //bit 14 = commit
  //bit 15 = unknown (test register interface?)
};

extern MCC mcc;
