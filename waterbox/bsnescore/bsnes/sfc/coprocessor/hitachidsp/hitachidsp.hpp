struct HitachiDSP : Processor::HG51B, Thread {
  ReadableMemory rom;
  WritableMemory ram;

  //hitachidsp.cpp
  auto synchronizeCPU() -> void;
  static auto Enter() -> void;
  auto step(uint clocks) -> void override;
  auto halt() -> void override;

  auto unload() -> void;
  auto power() -> void;

  auto isROM(uint address) -> bool override;
  auto isRAM(uint address) -> bool override;

  //HG51B read/write
  auto read(uint address) -> uint8 override;
  auto write(uint address, uint8 data) -> void override;

  //CPU ROM read/write
  auto addressROM(uint address) const -> maybe<uint>;
  auto readROM(uint address, uint8 data = 0) -> uint8;
  auto writeROM(uint address, uint8 data) -> void;

  //CPU RAM read/write
  auto addressRAM(uint address) const -> maybe<uint>;
  auto readRAM(uint address, uint8 data = 0) -> uint8;
  auto writeRAM(uint address, uint8 data) -> void;

  //HG51B data RAM read/write
  auto addressDRAM(uint address) const -> maybe<uint>;
  auto readDRAM(uint address, uint8 data = 0) -> uint8;
  auto writeDRAM(uint address, uint8 data) -> void;

  //CPU IO read/write
  auto addressIO(uint address) const -> maybe<uint>;
  auto readIO(uint address, uint8 data = 0) -> uint8;
  auto writeIO(uint address, uint8 data) -> void;

  auto firmware() const -> vector<uint8>;
  auto serialize(serializer&) -> void;

  uint Frequency;
  uint Roms;
  bool Mapping;

  //data-rom.cpp
  static const uint8_t staticDataROM[3072];
};

extern HitachiDSP hitachidsp;
