//PIF-NUS

struct PIF : Memory::IO<PIF> {
  Node::Object node;
  Memory::Readable rom;
  Memory::Writable ram;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Memory {
      Node::Debugger::Memory ram;
    } memory;

  } debugger;

  //pif.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;
  auto addressCRC(u16 address) const -> n5;
  auto dataCRC(array_view<u8> data) const -> n8;
  auto run() -> void;
  auto scan() -> void;
  auto challenge() -> void;
  auto power(bool reset) -> void;

  //io.cpp
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct IO {
    n1  romLockout;
  } io;
};

extern PIF pif;
