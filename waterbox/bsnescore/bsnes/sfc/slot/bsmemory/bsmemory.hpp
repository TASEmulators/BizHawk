//MaskROMs supported:
//  Sharp LH5S4TNI (MaskROM 512K x 8-bit) [BSMC-CR-01: BSMC-ZS5J-JPN, BSMC-YS5J-JPN]
//  Sharp LH534VNF (MaskROM 512K x 8-bit) [BSMC-BR-01: BSMC-ZX3J-JPN]

//Flash chips supported: (16-bit modes unsupported)
//  Sharp LH28F800SUT-ZI (Flash 16 x 65536 x 8-bit) [BSMC-AF-01: BSMC-HM-JPN]
//  Sharp LH28F016SU ??? (Flash 32 x 65536 x 8-bit) [unreleased: experimental]
//  Sharp LH28F032SU ??? (Flash 64 x 65536 x 8-bit) [unreleased: experimental]

//unsupported:
//  Sharp LH28F400SU ??? (Flash 32 x 16384 x 8-bit) [unreleased] {vendor ID: 0x00'b0; device ID: 0x66'21}

//notes:
//timing emulation is only present for block erase commands
//other commands generally complete so quickly that it's unnecessary (eg 70-120ns for writes)
//suspend, resume, abort, ready/busy modes are not supported

struct BSMemory : Thread, Memory {
  uint pathID = 0;
  uint ROM = 1;

  auto writable() const { return pin.writable; }
  auto writable(bool writable) { pin.writable = !ROM && writable; }

  //bsmemory.cpp
  BSMemory();
  auto synchronizeCPU() -> void;
  static auto Enter() -> void;
  auto main() -> void;
  auto step(uint clocks) -> void;

  auto load() -> bool;
  auto unload() -> void;
  auto power() -> void;

  auto data() -> uint8* override;
  auto size() const -> uint override;
  auto read(uint address, uint8 data) -> uint8 override;
  auto write(uint address, uint8 data) -> void override;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  WritableMemory memory;

private:
  struct Pin {
    uint1 writable;  // => /WP
  } pin;

  struct Chip {
    uint16 vendor;
    uint16 device;
    uint48 serial;
  } chip;

  struct Page {
    BSMemory* self = nullptr;

    auto swap() -> void;
    auto read(uint8 address) -> uint8;
    auto write(uint8 address, uint8 data) -> void;

    uint8 buffer[2][256];
  } page;

  struct BlockInformation {
    BSMemory* self = nullptr;

    inline auto bitCount() const -> uint;
    inline auto byteCount() const -> uint;
    inline auto count() const -> uint;
  };

  struct Block : BlockInformation {
    auto read(uint address) -> uint8;
    auto write(uint address, uint8 data) -> void;
    auto erase() -> void;
    auto lock() -> void;
    auto update() -> void;

    uint4  id;
    uint32 erased;
    uint1  locked;
    uint1  erasing;

    struct Status {
      auto operator()() -> uint8;

      uint1 vppLow;
      uint1 queueFull;
      uint1 aborted;
      uint1 failed;
      uint1 locked = 1;
      uint1 ready = 1;
    } status;
  } blocks[64];  //8mbit = 16; 16mbit = 32; 32mbit = 64

  struct Blocks : BlockInformation {
    auto operator()(uint6 id) -> Block&;
  } block;

  struct Compatible {
    struct Status {
      auto operator()() -> uint8;

      uint1 vppLow;
      uint1 writeFailed;
      uint1 eraseFailed;
      uint1 eraseSuspended;
      uint1 ready = 1;
    } status;
  } compatible;

  struct Global {
    struct Status {
      auto operator()() -> uint8;

      uint1 page;
      uint1 pageReady = 1;
      uint1 pageAvailable = 1;
      uint1 queueFull;
      uint1 sleeping;
      uint1 failed;
      uint1 suspended;
      uint1 ready = 1;
    } status;
  } global;

  struct Mode { enum : uint {
    Flash,
    Chip,
    Page,
    CompatibleStatus,
    ExtendedStatus,
  };};
  uint3 mode;

  struct ReadyBusyMode { enum : uint {
    EnableToLevelMode,
    PulseOnWrite,
    PulseOnErase,
    Disable,
  };};
  uint2 readyBusyMode;

  struct Queue {
    auto flush() -> void;
    auto pop() -> void;
    auto push(uint24 address, uint8 data) -> void;
    auto size() -> uint;
    auto address(uint index) -> uint24;
    auto data(uint index) -> uint8;

    //serialization.cpp
    auto serialize(serializer&) -> void;

    struct History {
      uint1  valid;
      uint24 address;
      uint8  data;
    } history[4];
  } queue;

  auto failed() -> void;
};

extern BSMemory bsmemory;
