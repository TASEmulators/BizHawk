namespace Memory {
  #include "lsb/readable.hpp"
  #include "lsb/writable.hpp"
  #include "io.hpp"

  struct Readable16 : Memory::Readable {
    template<u32 Size>
    auto read(u32 address) -> u64 {
      if constexpr(Size == Dual) return (read<Word>(address) << 32) | read<Word>(address+4);
      if constexpr(Size == Word) return (read<Half>(address) << 16) | read<Half>(address+2);
      return Memory::Readable::read<Size>(address);
    }
  };

  struct Writable16 : Memory::Writable {
    template<u32 Size>
    auto read(u32 address) -> u64 {
      if constexpr(Size == Dual) return (read<Word>(address) << 32) | read<Word>(address+4);
      if constexpr(Size == Word) return (read<Half>(address) << 16) | read<Half>(address+2);
      return Memory::Writable::read<Size>(address);
    }

    template<u32 Size>
    auto write(u32 address, u64 data) -> void {
      if constexpr(Size == Dual) return write<Word>(address, data >> 32), write<Word>(address+4, data);
      if constexpr(Size == Word) return write<Half>(address, data >> 16), write<Half>(address+2, data);
      return Memory::Writable::write<Size>(address, data);
    }
  };
}

struct Bus {
  //bus.hpp
  template<u32 Size> auto read(u32 address, Thread& thread, const char *peripheral) -> u64;
  template<u32 Size> auto write(u32 address, u64 data, Thread& thread, const char *peripheral) -> void;

  template<u32 Size> auto readBurst(u32 address, u32* data, Thread& thread) -> void;
  template<u32 Size> auto writeBurst(u32 address, u32* data, Thread& thread) -> void;

  auto freezeUnmapped(u32 address) -> void;
  auto freezeUncached(u32 address) -> void;
  auto freezeDualRead(u32 address) -> void;
};

extern Bus bus;
