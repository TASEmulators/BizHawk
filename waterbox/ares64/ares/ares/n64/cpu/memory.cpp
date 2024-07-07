//32-bit segments

auto CPU::kernelSegment32(u32 vaddr) const -> Context::Segment {
  if(vaddr <= 0x7fff'ffff) return Context::Segment::Mapped;  //kuseg
  if(vaddr <= 0x9fff'ffff) return Context::Segment::Cached;  //kseg0
  if(vaddr <= 0xbfff'ffff) return Context::Segment::Direct;  //kseg1
  if(vaddr <= 0xdfff'ffff) return Context::Segment::Mapped;  //ksseg
  if(vaddr <= 0xffff'ffff) return Context::Segment::Mapped;  //kseg3
  unreachable;
}

auto CPU::supervisorSegment32(u32 vaddr) const -> Context::Segment {
  if(vaddr <= 0x7fff'ffff) return Context::Segment::Mapped;  //suseg
  if(vaddr <= 0xbfff'ffff) return Context::Segment::Unused;
  if(vaddr <= 0xdfff'ffff) return Context::Segment::Mapped;  //sseg
  if(vaddr <= 0xffff'ffff) return Context::Segment::Unused;
  unreachable;
}

auto CPU::userSegment32(u32 vaddr) const -> Context::Segment {
  if(vaddr <= 0x7fff'ffff) return Context::Segment::Mapped;  //useg
  if(vaddr <= 0xffff'ffff) return Context::Segment::Unused;
  unreachable;
}

//64-bit segments

auto CPU::kernelSegment64(u64 vaddr) const -> Context::Segment {
  if(vaddr <= 0x0000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xkuseg
  if(vaddr <= 0x3fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x4000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xksseg
  if(vaddr <= 0x7fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x8000'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0x87ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x8800'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0x8fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x9000'0000'ffff'ffffull) return Context::Segment::Direct32;  //xkphys*
  if(vaddr <= 0x97ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x9800'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0x9fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xa000'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0xa7ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xa800'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0xafff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xb000'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0xb7ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xb800'0000'ffff'ffffull) return Context::Segment::Cached32;  //xkphys*
  if(vaddr <= 0xbfff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xc000'00ff'7fff'ffffull) return Context::Segment::Mapped;  //xkseg
  if(vaddr <= 0xffff'ffff'7fff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xffff'ffff'9fff'ffffull) return Context::Segment::Cached;  //ckseg0
  if(vaddr <= 0xffff'ffff'bfff'ffffull) return Context::Segment::Direct;  //ckseg1
  if(vaddr <= 0xffff'ffff'dfff'ffffull) return Context::Segment::Mapped;  //ckseg2
  if(vaddr <= 0xffff'ffff'ffff'ffffull) return Context::Segment::Mapped;  //ckseg3
  unreachable;
}

auto CPU::supervisorSegment64(u64 vaddr) const -> Context::Segment {
  if(vaddr <= 0x0000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xsuseg
  if(vaddr <= 0x3fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x4000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xsseg
  if(vaddr <= 0xffff'ffff'bfff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xffff'ffff'dfff'ffffull) return Context::Segment::Mapped;  //csseg
  if(vaddr <= 0xffff'ffff'ffff'ffffull) return Context::Segment::Unused;
  unreachable;
}

auto CPU::userSegment64(u64 vaddr) const -> Context::Segment {
  if(vaddr <= 0x0000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xuseg
  if(vaddr <= 0xffff'ffff'ffff'ffffull) return Context::Segment::Unused;
  unreachable;
}

//

auto CPU::segment(u64 vaddr) -> Context::Segment {
  auto segment = context.segment[vaddr >> 29 & 7];
  if(likely(context.bits == 32))
    return (Context::Segment)segment;
  switch(segment) {
  case Context::Segment::Kernel64:
    return kernelSegment64(vaddr);
  case Context::Segment::Supervisor64:
    return supervisorSegment64(vaddr);
  case Context::Segment::User64:
    return userSegment64(vaddr);
  }
  unreachable;
}

auto CPU::devirtualize(u64 vaddr) -> maybe<u64> {
  if(vaddrAlignedError<Word>(vaddr, false)) return nothing;
  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    addressException(vaddr);
    exception.addressLoad();
    return nothing;
  case Context::Segment::Mapped:
    if(auto match = tlb.load(vaddr)) return match.address & context.physMask;
    addressException(vaddr);
    return nothing;
  case Context::Segment::Cached:
  case Context::Segment::Direct:
    return vaddr & 0x1fff'ffff;
  case Context::Segment::Cached32:
  case Context::Segment::Direct32:
    return vaddr & 0xffff'ffff;
  }
  unreachable;
}

// Fast(er) version of devirtualize for icache lookups
// avoids handling unmapped regions/exceptions as these should have already
// been handled by instruction fetch, also ignores tlb match failure
auto CPU::devirtualizeFast(u64 vaddr) -> u64 {
  // Assume address space is mapped into pages that are 4kb in size
  // If we have a cached physical address for this page, use it
  // This cache is purged on any writes to the TLB so should never become stale
  auto vbase = vaddr >> 12;
  if(devirtualizeCache.vbase == vbase && devirtualizeCache.pbase) {
    auto offset = vaddr & 0xfff;
    return (devirtualizeCache.pbase & ~0xfff) + offset;
  }

  // Cache the physical address of this page for the next call
  devirtualizeCache.vbase = vaddr >> 12;

  switch(segment(vaddr)) {
  case Context::Segment::Mapped: {
    auto match = tlb.loadFast(vaddr);
    return devirtualizeCache.pbase = match.address & context.physMask;
  }
  case Context::Segment::Cached:
  case Context::Segment::Direct:
    return devirtualizeCache.pbase =  vaddr & 0x1fff'ffff;
  case Context::Segment::Cached32:
  case Context::Segment::Direct32:
    return devirtualizeCache.pbase =  vaddr & 0xffff'ffff;
  }
  return devirtualizeCache.pbase = 0;
}

auto CPU::devirtualizeDebug(u64 vaddr) -> u64 {
  return devirtualizeFast(vaddr); // this wrapper preserves the inlining of 'devirtualizeFast'
}

template<u32 Size>
inline auto CPU::busWrite(u32 address, u64 data) -> void {
  bus.write<Size>(address, data, *this, "CPU");
}

template<u32 Size>
inline auto CPU::busWriteBurst(u32 address, u32 *data) -> void {
  bus.writeBurst<Size>(address, data, *this);
}

template<u32 Size>
inline auto CPU::busRead(u32 address) -> u64 {
  return bus.read<Size>(address, *this, "CPU");
}

template<u32 Size>
inline auto CPU::busReadBurst(u32 address, u32 *data) -> void {
  return bus.readBurst<Size>(address, data, *this);
}

auto CPU::fetch(u64 vaddr) -> maybe<u32> {
  if(vaddrAlignedError<Word>(vaddr, false)) return nothing;
  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    step(1 * 2);
    addressException(vaddr);
    exception.addressLoad();
    return nothing;
  case Context::Segment::Mapped:
    if(auto match = tlb.load(vaddr)) {
      if(match.cache) return icache.fetch(vaddr, match.address & context.physMask, cpu);
      step(1 * 2);
      return busRead<Word>(match.address & context.physMask);
    }
    step(1 * 2);
    addressException(vaddr);
    return nothing;
  case Context::Segment::Cached:
    return icache.fetch(vaddr, vaddr & 0x1fff'ffff, cpu);
  case Context::Segment::Cached32:
    return icache.fetch(vaddr, vaddr & 0xffff'ffff, cpu);
  case Context::Segment::Direct:
    step(1 * 2);
    return busRead<Word>(vaddr & 0x1fff'ffff);
  case Context::Segment::Direct32:
    step(1 * 2);
    return busRead<Word>(vaddr & 0xffff'ffff);
  }

  unreachable;
}

template<u32 Size>
auto CPU::read(u64 vaddr) -> maybe<u64> {
  if(vaddrAlignedError<Size>(vaddr, false)) return nothing;
  GDB::server.reportMemRead(vaddr, Size);
  
  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    step(1 * 2);
    addressException(vaddr);
    exception.addressLoad();
    return nothing;
  case Context::Segment::Mapped:
    if(auto match = tlb.load(vaddr)) {
      if(match.cache) return dcache.read<Size>(vaddr, match.address & context.physMask);
      step(1 * 2);
      return busRead<Size>(match.address & context.physMask);
    }
    step(1 * 2);
    addressException(vaddr);
    return nothing;
  case Context::Segment::Cached:
    return dcache.read<Size>(vaddr, vaddr & 0x1fff'ffff);
  case Context::Segment::Cached32:
    return dcache.read<Size>(vaddr, vaddr & 0xffff'ffff);
  case Context::Segment::Direct:
    step(1 * 2);
    return busRead<Size>(vaddr & 0x1fff'ffff);
  case Context::Segment::Direct32:
    step(1 * 2);
    return busRead<Size>(vaddr & 0xffff'ffff);
  }

  unreachable;
}

auto CPU::readDebug(u64 vaddr) -> u8 {
  Thread dummyThread{};

  switch(segment(vaddr)) {
    case Context::Segment::Unused: return 0;
    case Context::Segment::Mapped:
      if(auto match = tlb.load(vaddr, true)) {
        if(match.cache) return dcache.readDebug(vaddr, match.address & context.physMask);
        return bus.read<Byte>(match.address & context.physMask, dummyThread, "Ares Debugger");
      }
      return 0;
    case Context::Segment::Cached:
      return dcache.readDebug(vaddr, vaddr & 0x1fff'ffff);
    case Context::Segment::Cached32:
      return dcache.readDebug(vaddr, vaddr & 0xffff'ffff);
    case Context::Segment::Direct:
      return bus.read<Byte>(vaddr & 0x1fff'ffff, dummyThread, "Ares Debugger");
    case Context::Segment::Direct32:
      return bus.read<Byte>(vaddr & 0xffff'ffff, dummyThread, "Ares Debugger");
  }

  unreachable;
}

template<u32 Size>
auto CPU::write(u64 vaddr0, u64 data, bool alignedError) -> bool {
  if(alignedError && vaddrAlignedError<Size>(vaddr0, true)) return false;
  u64 vaddr = vaddr0 & ~((u64)Size - 1);

  GDB::server.reportMemWrite(vaddr0, Size);

  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    step(1 * 2);
    addressException(vaddr0);
    exception.addressStore();
    return false;
  case Context::Segment::Mapped:
    if(auto match = tlb.store(vaddr)) {
      if(match.cache) return dcache.write<Size>(vaddr, match.address & context.physMask, data), true;
      step(1 * 2);
      return busWrite<Size>(match.address & context.physMask, data), true;
    }
    step(1 * 2);
    addressException(vaddr0);
    return false;
  case Context::Segment::Cached:
    return dcache.write<Size>(vaddr, vaddr & 0x1fff'ffff, data), true;
  case Context::Segment::Cached32:
    return dcache.write<Size>(vaddr, vaddr & 0xffff'ffff, data), true;
  case Context::Segment::Direct:
    step(1 * 2);
    return busWrite<Size>(vaddr & 0x1fff'ffff, data), true;
  case Context::Segment::Direct32:
    step(1 * 2);
    return busWrite<Size>(vaddr & 0xffff'ffff, data), true;
  }

  unreachable;
}

template<u32 Size>
auto CPU::vaddrAlignedError(u64 vaddr, bool write) -> bool {
  if constexpr(Accuracy::CPU::AddressErrors) {
    if(unlikely(vaddr & Size - 1)) {
      step(1 * 2);
      addressException(vaddr);
      if(write) exception.addressStore();
      else exception.addressLoad();
      return true;
    }
    if (context.bits == 32 && unlikely((s32)vaddr != vaddr)) {
      step(1 * 2);
      addressException(vaddr);
      if(write) exception.addressStore();
      else exception.addressLoad();
      return true;
    }
  }
  return false;
}

auto CPU::addressException(u64 vaddr) -> void {
  scc.badVirtualAddress = vaddr;
  scc.tlb.virtualAddress.bit(13,39) = vaddr >> 13;
  scc.tlb.region = vaddr >> 62;
  scc.context.badVirtualAddress = vaddr >> 13;
  scc.xcontext.badVirtualAddress = vaddr >> 13;
  scc.xcontext.region = vaddr >> 62;
}
