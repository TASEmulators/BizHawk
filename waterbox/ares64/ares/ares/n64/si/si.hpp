//Serial Interface

struct SI : Memory::IO<SI> {
  Node::Object node;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Tracer {
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  //si.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;
  auto addressCRC(u16 address) const -> n5;
  auto dataCRC(array_view<u8> data) const -> n8;
  auto run() -> void;
  auto scan() -> void;
  auto challenge() -> void;
  auto power(bool reset) -> void;

  //dma.cpp
  auto dmaRead() -> void;
  auto dmaWrite() -> void;

  //io.cpp
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct IO {
    n24 dramAddress;
    n32 readAddress;
    n32 writeAddress;
    n1  dmaBusy;
    n1  ioBusy;
    n1  readPending;
    n4  pchState;
    n4  dmaState;
    n1  dmaError;
    n1  interrupt;
  } io;
};

extern SI si;
