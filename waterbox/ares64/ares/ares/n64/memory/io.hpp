template<typename T>
struct IO {
  template<u32 Size>
  auto read(u32 address) -> u64 {
    if constexpr(Size == Byte) {
      auto data = ((T*)this)->readWord(address);
      switch(address & 3) {
      case 0: return data >> 24;
      case 1: return data >> 16;
      case 2: return data >>  8;
      case 3: return data >>  0;
      }
    }
    if constexpr(Size == Half) {
      auto data = ((T*)this)->readWord(address);
      switch(address & 2) {
      case 0: return data >> 16;
      case 2: return data >>  0;
      }
    }
    if constexpr(Size == Word) {
      return ((T*)this)->readWord(address);
    }
    if constexpr(Size == Dual) {
      u64 data = ((T*)this)->readWord(address);
      return data << 32 | ((T*)this)->readWord(address + 4);
    }
    unreachable;
  }

  template<u32 Size>
  auto write(u32 address, u64 data) -> void {
    if constexpr(Size == Byte) {
      switch(address & 3) {
      case 0: return ((T*)this)->writeWord(address, data << 24);
      case 1: return ((T*)this)->writeWord(address, data << 16);
      case 2: return ((T*)this)->writeWord(address, data <<  8);
      case 3: return ((T*)this)->writeWord(address, data <<  0);
      }
    }
    if constexpr(Size == Half) {
      switch(address & 2) {
      case 0: return ((T*)this)->writeWord(address, data << 16);
      case 2: return ((T*)this)->writeWord(address, data <<  0);
      }
    }
    if constexpr(Size == Word) {
      ((T*)this)->writeWord(address, data);
    }
    if constexpr(Size == Dual) {
      ((T*)this)->writeWord(address + 0, data >> 32);
      ((T*)this)->writeWord(address + 4, data >>  0);
    }
  }
};
