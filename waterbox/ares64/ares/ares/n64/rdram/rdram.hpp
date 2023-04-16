//RAMBUS RAM

struct RDRAM : Memory::IO<RDRAM> {
  Node::Object node;
  Memory::Writable ram;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 chipID, u32 address, u32 data) -> void;

    struct Memory {
      Node::Debugger::Memory ram;
    } memory;

    struct Tracer {
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  //rdram.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;
  auto power(bool reset) -> void;

  //io.cpp
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct Chip {
    n32 deviceType;
    n32 deviceID;
    n32 delay;
    n32 mode;
    n32 refreshInterval;
    n32 refreshRow;
    n32 rasInterval;
    n32 minInterval;
    n32 addressSelect;
    n32 deviceManufacturer;
    n32 currentControl;
  } chips[4];
};

extern RDRAM rdram;
