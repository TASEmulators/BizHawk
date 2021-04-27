struct SDD1 {
  auto unload() -> void;
  auto power() -> void;

  auto ioRead(uint addr, uint8 data) -> uint8;
  auto ioWrite(uint addr, uint8 data) -> void;

  auto dmaRead(uint addr, uint8 data) -> uint8;
  auto dmaWrite(uint addr, uint8 data) -> void;

  auto mmcRead(uint addr) -> uint8;

  auto mcuRead(uint addr, uint8 data) -> uint8;
  auto mcuWrite(uint addr, uint8 data) -> void;

  auto serialize(serializer&) -> void;

  ReadableMemory rom;

private:
  uint8 r4800;  //hard enable
  uint8 r4801;  //soft enable
  uint8 r4804;  //MMC bank 0
  uint8 r4805;  //MMC bank 1
  uint8 r4806;  //MMC bank 2
  uint8 r4807;  //MMC bank 3

  struct DMA {
    uint24 addr;  //$43x2-$43x4 -- DMA transfer address
    uint16 size;  //$43x5-$43x6 -- DMA transfer size
  } dma[8];
  bool dmaReady;  //used to initialize decompression module

public:
  #include "decompressor.hpp"
  Decompressor decompressor;
};

extern SDD1 sdd1;
