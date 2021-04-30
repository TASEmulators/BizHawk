struct NECDSP : Processor::uPD96050, Thread {
  auto synchronizeCPU() -> void;
  static auto Enter() -> void;
  auto main() -> void;
  auto step(uint clocks) -> void;

  auto read(uint addr, uint8 data) -> uint8;
  auto write(uint addr, uint8 data) -> void;

  auto readRAM(uint addr, uint8 data) -> uint8;
  auto writeRAM(uint addr, uint8 data) -> void;

  auto power() -> void;

  auto firmware() const -> vector<uint8>;
  auto serialize(serializer&) -> void;

  uint Frequency = 0;
};

extern NECDSP necdsp;
