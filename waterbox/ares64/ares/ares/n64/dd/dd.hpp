//Disk Drive

struct DD : Memory::IO<DD> {
  Node::Object node;
  VFS::Pak pak;
  Memory::Readable iplrom;
  Memory::Writable c2s;
  Memory::Writable ds;
  Memory::Writable ms;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Tracer {
      Node::Debugger::Tracer::Notification io;
    } tracer;
  } debugger;

  //dd.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;
  auto power(bool reset) -> void;

  //io.cpp
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;
};

extern DD dd;
