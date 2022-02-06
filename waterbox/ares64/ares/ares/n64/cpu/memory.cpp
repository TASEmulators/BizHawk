//32-bit segments

auto CPU::kernelSegment32(u32 address) const -> Context::Segment {
  if(address <= 0x7fff'ffff) return Context::Segment::Mapped;  //kuseg
  if(address <= 0x9fff'ffff) return Context::Segment::Cached;  //kseg0
  if(address <= 0xbfff'ffff) return Context::Segment::Direct;  //kseg1
  if(address <= 0xdfff'ffff) return Context::Segment::Mapped;  //ksseg
  if(address <= 0xffff'ffff) return Context::Segment::Mapped;  //kseg3
  unreachable;
}

auto CPU::supervisorSegment32(u32 address) const -> Context::Segment {
  if(address <= 0x7fff'ffff) return Context::Segment::Mapped;  //suseg
  if(address <= 0xbfff'ffff) return Context::Segment::Unused;
  if(address <= 0xdfff'ffff) return Context::Segment::Mapped;  //sseg
  if(address <= 0xffff'ffff) return Context::Segment::Unused;
  unreachable;
}

auto CPU::userSegment32(u32 address) const -> Context::Segment {
  if(address <= 0x7fff'ffff) return Context::Segment::Mapped;  //useg
  if(address <= 0xffff'ffff) return Context::Segment::Unused;
  unreachable;
}

//64-bit segments

auto CPU::kernelSegment64(u64 address) const -> Context::Segment {
  if(address <= 0x0000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xkuseg
  if(address <= 0x3fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0x4000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xksseg
  if(address <= 0x7fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0x8000'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0x87ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0x8800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0x8fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0x9000'0000'ffff'ffffull) return Context::Segment::Direct;  //xkphys*
  if(address <= 0x97ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0x9800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0x9fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0xa000'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0xa7ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0xa800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0xafff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0xb000'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0xb7ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0xb800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(address <= 0xbfff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0xc000'00ff'7fff'ffffull) return Context::Segment::Mapped;  //xkseg
  if(address <= 0xffff'ffff'7fff'ffffull) return Context::Segment::Unused;
  if(address <= 0xffff'ffff'9fff'ffffull) return Context::Segment::Cached;  //ckseg0
  if(address <= 0xffff'ffff'bfff'ffffull) return Context::Segment::Direct;  //ckseg1
  if(address <= 0xffff'ffff'dfff'ffffull) return Context::Segment::Mapped;  //ckseg2
  if(address <= 0xffff'ffff'ffff'ffffull) return Context::Segment::Mapped;  //ckseg3
  unreachable;
}

auto CPU::supervisorSegment64(u64 address) const -> Context::Segment {
  if(address <= 0x0000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xsuseg
  if(address <= 0x3fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(address <= 0x4000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xsseg
  if(address <= 0xffff'ffff'bfff'ffffull) return Context::Segment::Unused;
  if(address <= 0xffff'ffff'dfff'ffffull) return Context::Segment::Mapped;  //csseg
  if(address <= 0xffff'ffff'ffff'ffffull) return Context::Segment::Unused;
  unreachable;
}

auto CPU::userSegment64(u64 address) const -> Context::Segment {
  if(address <= 0x0000'00ff'ffff'ffffull) return Context::Segment::Mapped;  //xuseg
  if(address <= 0xffff'ffff'ffff'ffffull) return Context::Segment::Unused;
  unreachable;
}

//

auto CPU::segment(u64 address) -> Context::Segment {
  auto segment = context.segment[address >> 29 & 7];
//if(likely(context.bits == 32))
  return (Context::Segment)segment;
  switch(segment) {
  case Context::Segment::Kernel64:
    return kernelSegment64(address);
  case Context::Segment::Supervisor64:
    return supervisorSegment64(address);
  case Context::Segment::User64:
    return userSegment64(address);
  }
  unreachable;
}

auto CPU::devirtualize(u64 address) -> maybe<u64> {
  switch(context.segment[address >> 29 & 7]) {
  case Context::Segment::Unused:
    exception.addressLoad();
    return nothing;
  case Context::Segment::Mapped:
    if(auto match = tlb.load(address)) return match.address;
    tlb.exception(address);
    return nothing;
  case Context::Segment::Cached:
  case Context::Segment::Direct:
    return address;
  }
  unreachable;
}

auto CPU::fetch(u64 address) -> u32 {
  switch(segment(address)) {
  case Context::Segment::Unused:
    step(1);
    exception.addressLoad();
    return 0;  //nop
  case Context::Segment::Mapped:
    if(auto match = tlb.load(address)) {
      if(match.cache) return icache.fetch(match.address);
      step(1);
      return bus.read<Word>(match.address);
    }
    step(1);
    tlb.exception(address);
    return 0;  //nop
  case Context::Segment::Cached:
    return icache.fetch(address);
  case Context::Segment::Direct:
    step(1);
    return bus.read<Word>(address);
  }

  unreachable;
}

template<u32 Size>
auto CPU::read(u64 address) -> maybe<u64> {
  if constexpr(Accuracy::CPU::AddressErrors) {
    if(unlikely(address & Size - 1)) {
      step(1);
      exception.addressLoad();
      return nothing;
    }
  }

  switch(segment(address)) {
  case Context::Segment::Unused:
    step(1);
    exception.addressLoad();
    return nothing;
  case Context::Segment::Mapped:
    if(auto match = tlb.load(address)) {
      if(match.cache) return dcache.read<Size>(match.address);
      step(1);
      return bus.read<Size>(match.address);
    }
    step(1);
    tlb.exception(address);
    return nothing;
  case Context::Segment::Cached:
    return dcache.read<Size>(address);
  case Context::Segment::Direct:
    step(1);
    return bus.read<Size>(address);
  }

  unreachable;
}

template<u32 Size>
auto CPU::write(u64 address, u64 data) -> bool {
  if constexpr(Accuracy::CPU::AddressErrors) {
    if(unlikely(address & Size - 1)) {
      step(1);
      exception.addressStore();
      return false;
    }
  }

  switch(segment(address)) {
  case Context::Segment::Unused:
    step(1);
    exception.addressStore();
    return false;
  case Context::Segment::Mapped:
    if(auto match = tlb.store(address)) {
      if(match.cache) return dcache.write<Size>(match.address, data), true;
      step(1);
      return bus.write<Size>(match.address, data), true;
    }
    step(1);
    tlb.exception(address);
    return false;
  case Context::Segment::Cached:
    return dcache.write<Size>(address, data), true;
  case Context::Segment::Direct:
    step(1);
    return bus.write<Size>(address, data), true;
  }

  unreachable;
}
