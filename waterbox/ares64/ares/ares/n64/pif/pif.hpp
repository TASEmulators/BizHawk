//PIF-NUS

struct PIF : Thread, Memory::SI<PIF> {
  enum State : u32 { Init, WaitLockout, WaitGetChecksum, WaitCheckChecksum, WaitTerminateBoot, Run, Error };
  enum IntADir : bool { Read, Write };
  enum IntASize : bool { Size4, Size64 };

  Node::Object node;
  Memory::Readable rom;
  Memory::Writable ram;
  u32 state;

  struct Debugger {
    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 address, u32 data) -> void;

    struct Memory {
      Node::Debugger::Memory ram;
    } memory;
  } debugger;

  struct Intram {
    n8 osInfo[3];
    n8 cpuChecksum[6];
    n8 cicChecksum[6];
    s32 bootTimeout;
    n8 joyAddress[5];
    struct {
      n1 skip;
      n1 reset;
    } joyStatus[5];

    auto serialize(serializer& s) -> void;
  } intram;

  //pif.cpp
  auto load(Node::Object) -> void;
  auto unload() -> void;
  auto main() -> void;
  auto power(bool reset) -> void;
  auto estimateTiming() -> u32;

  //hle.cpp
  auto mainHLE() -> void;
  auto addressCRC(u16 address) const -> n5;
  auto dataCRC(array_view<u8> data) const -> n8;
  auto descramble(n4 *buf, int size) -> void;
  auto ramReadCommand() -> u8;
  auto ramWriteCommand(u8 val) -> void;
  auto memSwap(u32 address, n8 &val) -> void;
  auto memSwapSecrets() -> void;
  auto joyInit() -> void;
  auto joyParse() -> void;
  auto joyRun() -> void;
  auto challenge() -> void;
  auto intA(bool dir, bool size) -> void;

  //io.cpp
  auto readInt(u32 address) -> u32;
  auto writeInt(u32 address, u32 data) -> void;
  auto readWord(u32 address) -> u32;
  auto writeWord(u32 address, u32 data) -> void;
  auto dmaRead(u32 address, u32 ramAddress) -> void;
  auto dmaWrite(u32 address, u32 ramAddress) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  struct IO {
    n1  romLockout;
    n1  resetEnabled;
  } io;
};

extern PIF pif;
