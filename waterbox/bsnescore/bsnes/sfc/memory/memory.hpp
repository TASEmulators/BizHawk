struct Memory {
  static bool GlobalWriteEnable;

  virtual ~Memory() { reset(); }
  inline explicit operator bool() const { return size() > 0; }

  virtual auto reset() -> void {}
  virtual auto allocate(uint, uint8 = 0xff) -> void {}

  virtual auto data() -> uint8* = 0;
  virtual auto size() const -> uint = 0;

  virtual auto read(uint address, uint8 data = 0) -> uint8 = 0;
  virtual auto write(uint address, uint8 data) -> void = 0;

  uint id = 0;
};

#include "readable.hpp"
#include "writable.hpp"
#include "protectable.hpp"

struct Bus {
  alwaysinline static auto mirror(uint address, uint size) -> uint;
  alwaysinline static auto reduce(uint address, uint mask) -> uint;

  ~Bus();

  alwaysinline auto read(uint address, uint8 data = 0) -> uint8;
  alwaysinline auto write(uint address, uint8 data) -> void;

  auto reset() -> void;
  auto map(
    const function<uint8 (uint, uint8)>& read,
    const function<void (uint, uint8)>& write,
    const string& address, uint size = 0, uint base = 0, uint mask = 0
  ) -> uint;
  auto unmap(const string& address) -> void;

private:
  uint8* lookup = nullptr;
  uint32* target = nullptr;

  function<uint8 (uint, uint8)> reader[256];
  function<void  (uint, uint8)> writer[256];
  uint counter[256];
};

extern Bus bus;
