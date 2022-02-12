struct Cartridge {
  Node::Peripheral node;
  VFS::Pak pak;
  Memory::Readable rom;
  Memory::Writable ram;
  Memory::Writable eeprom;
  struct Flash : Memory::Writable {
    template<u32 Size>
    auto read(u32 address) -> u64 {
      if constexpr(Size == Byte) return readByte(address);
      if constexpr(Size == Half) return readHalf(address);
      if constexpr(Size == Word) return readWord(address);
      if constexpr(Size == Dual) return readDual(address);
      unreachable;
    }

    template<u32 Size>
    auto write(u32 address, u64 data) -> void {
      if constexpr(Size == Byte) return writeByte(address, data);
      if constexpr(Size == Half) return writeHalf(address, data);
      if constexpr(Size == Word) return writeWord(address, data);
      if constexpr(Size == Dual) return writeDual(address, data);
    }

    //flash.cpp
    auto readByte(u32 adddres) -> u64;
    auto readHalf(u32 address) -> u64;
    auto readWord(u32 address) -> u64;
    auto readDual(u32 address) -> u64;
    auto writeByte(u32 address, u64 data) -> void;
    auto writeHalf(u32 address, u64 data) -> void;
    auto writeWord(u32 address, u64 data) -> void;
    auto writeDual(u32 address, u64 data) -> void;

    enum class Mode : u32 { Idle, Erase, Write, Read, Status };
    Mode mode = Mode::Idle;
    u64  status = 0;
    u32  source = 0;
    u32  offset = 0;
  } flash;
  struct ISViewer : Memory::IO<ISViewer> {
    Memory::Writable ram;  //unserialized

    //isviewer.cpp
    auto readWord(u32 address) -> u32;
    auto writeWord(u32 address, u32 data) -> void;
  } isviewer;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto unload(Node::Object) -> void;

    struct Memory {
      Node::Debugger::Memory rom;
      Node::Debugger::Memory ram;
      Node::Debugger::Memory eeprom;
      Node::Debugger::Memory flash;
    } memory;
  } debugger;

  auto title() const -> string { return information.title; }
  auto region() const -> string { return information.region; }
  auto cic() const -> string { return information.cic; }

  //cartridge.cpp
  auto allocate(Node::Port) -> Node::Peripheral;
  auto connect() -> void;
  auto disconnect() -> void;

  auto save() -> void;
  auto power(bool reset) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

private:
  struct Information {
    string title;
    string region;
    string cic;
  } information;
};

#include "slot.hpp"
extern Cartridge& cartridge;
