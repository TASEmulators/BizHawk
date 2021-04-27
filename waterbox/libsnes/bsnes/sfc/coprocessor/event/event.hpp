//HLE of the NEC uPD78P214GC processor found on SNES-EVENT PCBs, used by:
//* Campus Challenge '92
//* PowerFest '94

//The NEC uPD78214 family are 8-bit microprocessors containing:
//* UART/CSI serial interface
//* ALU (MUL, DIV, BCD)
//* interrupts (12 internal; 7 external; 2 priority levels)
//* 16384 x 8-bit ROM
//*   512 x 8-bit RAM
//*     4 x timer/counters

//None of the SNES-EVENT games have had their uPD78214 firmware dumped.
//As such, our only option is very basic high-level emulation, provided here.

struct Event : Thread {
  //event.cpp
  auto synchronizeCPU() -> void;
  static auto Enter() -> void;
  auto main() -> void;
  auto step(uint clocks) -> void;
  auto unload() -> void;
  auto power() -> void;

  auto mcuRead(uint addr, uint8) -> uint8;
  auto mcuWrite(uint addr, uint8) -> void;

  auto read(uint addr, uint8 data) -> uint8;
  auto write(uint addr, uint8 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

public:
  ReadableMemory rom[4];

  enum class Board : uint { Unknown, CampusChallenge92, PowerFest94 } board;
  uint timer;

private:
  uint8 status;
  uint8 select;

  bool timerActive;
  bool scoreActive;

  uint timerSecondsRemaining;
  uint scoreSecondsRemaining;
};

extern Event event;
