namespace Memory {
  #include "lsb/readable.hpp"
  #include "lsb/writable.hpp"
  #include "io.hpp"
}

struct Bus {
  //bus.hpp
  template<u32 Size> auto read(u32 address) -> u64;
  template<u32 Size> auto write(u32 address, u64 data) -> void;
};

extern Bus bus;
