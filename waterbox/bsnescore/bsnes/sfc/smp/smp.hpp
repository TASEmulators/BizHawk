//Sony CXP1100Q-1

struct SMP : Processor::SPC700, Thread {
  inline auto synchronizing() const -> bool override { return scheduler.synchronizing(); }

  //io.cpp
  auto portRead(uint2 port) const -> uint8;
  auto portWrite(uint2 port, uint8 data) -> void;

  //smp.cpp
  auto synchronizeCPU() -> void;
  auto synchronizeDSP() -> void;
  static auto Enter() -> void;
  auto main() -> void;
  auto load() -> bool;
  auto power(bool reset) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  uint8 iplrom[64];

private:
  struct IO {
    //timing
    uint clockCounter = 0;
    uint dspCounter = 0;

    //external
    uint8 apu0 = 0;
    uint8 apu1 = 0;
    uint8 apu2 = 0;
    uint8 apu3 = 0;

    //$00f0
    uint1 timersDisable = 0;
    uint1 ramWritable = 1;
    uint1 ramDisable = 0;
    uint1 timersEnable = 1;
    uint2 externalWaitStates = 0;
    uint2 internalWaitStates = 0;

    //$00f1
    uint1 iplromEnable = 1;

    //$00f2
    uint8 dspAddr = 0;

    //$00f4-00f7
    uint8 cpu0 = 0;
    uint8 cpu1 = 0;
    uint8 cpu2 = 0;
    uint8 cpu3 = 0;

    //$00f8-00f9
    uint8 aux4 = 0;
    uint8 aux5 = 0;
  } io;

  //memory.cpp
  inline auto readRAM(uint16 address) -> uint8;
  inline auto writeRAM(uint16 address, uint8 data) -> void;

  auto idle() -> void override;
  auto read(uint16 address) -> uint8 override;
  auto write(uint16 address, uint8 data) -> void override;

  auto readDisassembler(uint16 address) -> uint8 override;

  //io.cpp
  inline auto readIO(uint16 address) -> uint8;
  inline auto writeIO(uint16 address, uint8 data) -> void;

  //timing.cpp
  template<uint Frequency>
  struct Timer {
    uint8   stage0 = 0;
    uint8   stage1 = 0;
    uint8   stage2 = 0;
    uint4   stage3 = 0;
    boolean line = 0;
    boolean enable = 0;
    uint8   target = 0;

    auto step(uint clocks) -> void;
    auto synchronizeStage1() -> void;
  };

  Timer<128> timer0;
  Timer<128> timer1;
  Timer< 16> timer2;

  inline auto wait(maybe<uint16> address = nothing, bool half = false) -> void;
  inline auto waitIdle(maybe<uint16> address = nothing, bool half = false) -> void;
  inline auto step(uint clocks) -> void;
  inline auto stepIdle(uint clocks) -> void;
  inline auto stepTimers(uint clocks) -> void;
};

extern SMP smp;
