//Audio Interface

struct AI : Thread, Memory::RCP<AI> {
  Node::Object node;
  Node::Audio::Stream stream;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Tracer {
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  //ai.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;
  auto main() -> void;
  auto sample(f64& left, f64& right) -> void;
  auto power(bool reset) -> void;

  //io.cpp
  auto readWord(u32 address, Thread& thread) -> u32;
  auto writeWord(u32 address, u32 data, Thread& thread) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct FIFO {
    n24 address;
  } fifo[2];

  struct IO {
    n1  dmaEnable;
    n24 dmaAddress[2];
    n1  dmaAddressCarry;
    n18 dmaLength[2];
    n2  dmaCount;
    u64 dmaOriginPc[2];
    n14 dacRate;
    n4  bitRate;
  } io;

  struct DAC {
    u32 frequency;
    u32 precision;
    u32 period;
  } dac;
};

extern AI ai;
