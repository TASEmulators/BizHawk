struct Readable {
  explicit operator bool() const {
    return size > 0;
  }

  auto reset() -> void {
    memory::free<u8, 64_KiB>(data);
    data = nullptr;
    size = 0;
    maskByte = 0;
    maskHalf = 0;
    maskWord = 0;
    maskDual = 0;
  }

  auto allocate(u32 capacity, u32 fillWith = ~0) -> void {
    reset();
    size = capacity & ~7;
    u32 mask = bit::round(size) - 1;
    maskByte = mask & ~0;
    maskHalf = mask & ~1;
    maskWord = mask & ~3;
    maskDual = mask & ~7;
    data = memory::allocate<u8, 64_KiB>(mask + 1);
    fill(fillWith);
  }

  auto fill(u32 value = 0) -> void {
    for(u32 address = 0; address < size; address += 4) {
      *(u32*)&data[address & maskWord] = value;
    }
  }

  auto load(VFS::File fp) -> void {
    if(!size) allocate(fp->size());
    for(u32 address = 0; address < min(size, fp->size()); address += 4) {
      *(u32*)&data[address & maskWord] = fp->readm(4L);
    }
  }

  auto save(VFS::File fp) -> void {
    for(u32 address = 0; address < min(size, fp->size()); address += 4) {
      fp->writem(*(u32*)&data[address & maskWord], 4L);
    }
  }

  template<u32 Size>
  auto read(u32 address) -> u64 {
    if constexpr(Size == Byte) return *(u8* )&data[address & maskByte ^ 3];
    if constexpr(Size == Half) return *(u16*)&data[address & maskHalf ^ 2];
    if constexpr(Size == Word) return *(u32*)&data[address & maskWord ^ 0];
    if constexpr(Size == Dual) {
      u64 upper = read<Word>(address + 0);
      u64 lower = read<Word>(address + 4);
      return upper << 32 | lower << 0;
    }
    unreachable;
  }

  template<u32 Size>
  auto write(u32 address, u64 value) -> void {
  }

  template<u32 Size>
  auto readUnaligned(u32 address) -> u64 {
    static_assert(Size == Half || Size == Word || Size == Dual);
    if constexpr(Size == Half) {
      u16 upper = read<Byte>(address + 0);
      u16 lower = read<Byte>(address + 1);
      return upper << 8 | lower << 0;
    }
    if constexpr(Size == Word) {
      u32 upper = readUnaligned<Half>(address + 0);
      u32 lower = readUnaligned<Half>(address + 2);
      return upper << 16 | lower << 0;
    }
    if constexpr(Size == Dual) {
      u64 upper = readUnaligned<Word>(address + 0);
      u64 lower = readUnaligned<Word>(address + 4);
      return upper << 32 | lower << 0;
    }
    unreachable;
  }

  template<u32 Size>
  auto writeUnaligned(u32 address, u64 value) -> void {
    static_assert(Size == Half || Size == Word || Size == Dual);
  }

  auto serialize(serializer& s) -> void {
  //s(array_span<u8>{data, size});
  }

//private:
  u8* data = nullptr;
  u32 size = 0;
  u32 maskByte = 0;
  u32 maskHalf = 0;
  u32 maskWord = 0;
  u32 maskDual = 0;
};
