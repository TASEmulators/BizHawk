//RAMBUS RAM

struct RDRAM : Memory::RCP<RDRAM> {
  Node::Object node;

  struct Writable : public Memory::Writable {
    RDRAM& self;

    Writable(RDRAM& self) : self(self) {}

    template<u32 Size>
    auto read(u32 address, const char *peripheral) -> u64 {
      if (address >= size) return 0;
      if (peripheral && system.homebrewMode) {
        self.debugger.readWord(address, Size, peripheral);
      }
      return Memory::Writable::read<Size>(address);
    }

    template<u32 Size>
    auto write(u32 address, u64 value, const char *peripheral) -> void {
      if (address >= size) return;
      if (peripheral && system.homebrewMode) {
        self.debugger.writeWord(address, Size, value, peripheral);
      }
      Memory::Writable::write<Size>(address, value);
    }

    template<u32 Size>
    auto writeBurst(u32 address, u32 *value, const char *peripheral) -> void {
      if (address >= size) return;
      Memory::Writable::write<Word>(address | 0x00, value[0]);
      Memory::Writable::write<Word>(address | 0x04, value[1]);
      Memory::Writable::write<Word>(address | 0x08, value[2]);
      Memory::Writable::write<Word>(address | 0x0c, value[3]);
      if (Size == ICache) {
        Memory::Writable::write<Word>(address | 0x10, value[4]);
        Memory::Writable::write<Word>(address | 0x14, value[5]);
        Memory::Writable::write<Word>(address | 0x18, value[6]);
        Memory::Writable::write<Word>(address | 0x1c, value[7]);
      }
    }

    template<u32 Size>
    auto readBurst(u32 address, u32 *value, const char *peripheral) -> void {
      if (address >= size) {
        value[0] = value[1] = value[2] = value[3] = 0;
        if (Size == ICache)
          value[4] = value[5] = value[6] = value[7] = 0;
        return;
      }
      value[0] = Memory::Writable::read<Word>(address | 0x00);
      value[1] = Memory::Writable::read<Word>(address | 0x04);
      value[2] = Memory::Writable::read<Word>(address | 0x08);
      value[3] = Memory::Writable::read<Word>(address | 0x0c);
      if (Size == ICache) {
        value[4] = Memory::Writable::read<Word>(address | 0x10);
        value[5] = Memory::Writable::read<Word>(address | 0x14);
        value[6] = Memory::Writable::read<Word>(address | 0x18);
        value[7] = Memory::Writable::read<Word>(address | 0x1c);
      }
    }

  } ram{*this};

  struct Debugger {
    u32 lastReadCacheline    = 0xffff'ffff;
    u32 lastWrittenCacheline = 0xffff'ffff;

    //debugger.cpp
    auto load(Node::Object) -> void;
    auto io(bool mode, u32 chipID, u32 address, u32 data) -> void;
    auto readWord(u32 address, int size, const char *peripheral) -> void;
    auto writeWord(u32 address, int size, u64 value, const char *peripheral) -> void;
    auto cacheErrorContext(string peripheral) -> string;

    struct Memory {
      Node::Debugger::Memory ram;
      Node::Debugger::Memory dcache;
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
  auto readWord(u32 address, Thread& thread) -> u32;
  auto writeWord(u32 address, u32 data, Thread& thread) -> void;

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
