struct SuperFX : Processor::GSU, Thread {
  ReadableMemory rom;
  WritableMemory ram;

  inline auto synchronizing() const -> bool { return scheduler.synchronizing(); }

  //superfx.cpp
  auto synchronizeCPU() -> void;
  static auto Enter() -> void;
  auto main() -> void;
  auto unload() -> void;
  auto power() -> void;

  //bus.cpp
  struct CPUROM : Memory {
    auto data() -> uint8* override;
    auto size() const -> uint override;
    auto read(uint, uint8) -> uint8 override;
    auto write(uint, uint8) -> void override;
  };

  struct CPURAM : Memory {
    auto data() -> uint8* override;
    auto size() const -> uint override;
    auto read(uint, uint8) -> uint8 override;
    auto write(uint, uint8) -> void override;
  };

  //core.cpp
  auto stop() -> void override;
  auto color(uint8 source) -> uint8 override;
  auto plot(uint8 x, uint8 y) -> void override;
  auto rpix(uint8 x, uint8 y) -> uint8 override;

  auto flushPixelCache(PixelCache& cache) -> void;

  //memory.cpp
  auto read(uint addr, uint8 data = 0x00) -> uint8 override;
  auto write(uint addr, uint8 data) -> void override;

  auto readOpcode(uint16 addr) -> uint8;
  alwaysinline auto peekpipe() -> uint8;
  alwaysinline auto pipe() -> uint8 override;

  auto flushCache() -> void override;
  auto readCache(uint16 addr) -> uint8;
  auto writeCache(uint16 addr, uint8 data) -> void;

  //io.cpp
  auto readIO(uint addr, uint8 data) -> uint8;
  auto writeIO(uint addr, uint8 data) -> void;

  //timing.cpp
  auto step(uint clocks) -> void override;

  auto syncROMBuffer() -> void override;
  auto readROMBuffer() -> uint8 override;
  auto updateROMBuffer() -> void;

  auto syncRAMBuffer() -> void override;
  auto readRAMBuffer(uint16 addr) -> uint8 override;
  auto writeRAMBuffer(uint16 addr, uint8 data) -> void override;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  uint Frequency;

  CPUROM cpurom;
  CPURAM cpuram;

private:
  uint romMask;
  uint ramMask;
};

extern SuperFX superfx;
