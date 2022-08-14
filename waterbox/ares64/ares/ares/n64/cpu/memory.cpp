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
  if(vaddr <= 0x8000'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(vaddr <= 0x87ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x8800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(vaddr <= 0x8fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x9000'0000'ffff'ffffull) return Context::Segment::Direct;  //xkphys*
  if(vaddr <= 0x97ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0x9800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(vaddr <= 0x9fff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xa000'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(vaddr <= 0xa7ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xa800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(vaddr <= 0xafff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xb000'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
  if(vaddr <= 0xb7ff'ffff'ffff'ffffull) return Context::Segment::Unused;
  if(vaddr <= 0xb800'0000'ffff'ffffull) return Context::Segment::Cached;  //xkphys*
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
    return vaddr & context.physMask;
  }
  unreachable;
}

auto CPU::fetch(u64 vaddr) -> u32 {
  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    step(1);
    addressException(vaddr);
    exception.addressLoad();
    return 0;  //nop
  case Context::Segment::Mapped:
    if(auto match = tlb.load(vaddr)) {
      if(match.cache) return icache.fetch(match.address & context.physMask, cpu);
      step(1);
      return bus.read<Word>(match.address & context.physMask);
    }
    step(1);
    addressException(vaddr);
    return 0;  //nop
  case Context::Segment::Cached:
    return icache.fetch(vaddr & context.physMask, cpu);
  case Context::Segment::Direct:
    step(1);
    return bus.read<Word>(vaddr & context.physMask);
  }

  unreachable;
}

template<u32 Size>
auto CPU::read(u64 vaddr) -> maybe<u64> {
  if constexpr(Accuracy::CPU::AddressErrors) {
    if(unlikely(vaddr & Size - 1)) {
      step(1);
      addressException(vaddr);
      exception.addressLoad();
      return nothing;
    }
    if (context.bits == 32 && unlikely((s32)vaddr != vaddr)) {
      step(1);
      addressException(vaddr);
      exception.addressLoad();
      return nothing;      
    }
  }

  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    step(1);
    addressException(vaddr);
    exception.addressLoad();
    return nothing;
  case Context::Segment::Mapped:
    if(auto match = tlb.load(vaddr)) {
      if(match.cache) return dcache.read<Size>(match.address & context.physMask);
      step(1);
      return bus.read<Size>(match.address & context.physMask);
    }
    step(1);
    addressException(vaddr);
    return nothing;
  case Context::Segment::Cached:
    return dcache.read<Size>(vaddr & context.physMask);
  case Context::Segment::Direct:
    step(1);
    return bus.read<Size>(vaddr & context.physMask);
  }

  unreachable;
}

template<u32 Size>
auto CPU::write(u64 vaddr, u64 data) -> bool {
  if constexpr(Accuracy::CPU::AddressErrors) {
    if(unlikely(vaddr & Size - 1)) {
      step(1);
      addressException(vaddr);
      exception.addressStore();
      return false;
    }
    if (context.bits == 32 && unlikely((s32)vaddr != vaddr)) {
      step(1);
      addressException(vaddr);
      exception.addressStore();
      return false;
    }
  }

  switch(segment(vaddr)) {
  case Context::Segment::Unused:
    step(1);
    addressException(vaddr);
    exception.addressStore();
    return false;
  case Context::Segment::Mapped:
    if(auto match = tlb.store(vaddr)) {
      if(match.cache) return dcache.write<Size>(match.address & context.physMask, data), true;
      step(1);
      return bus.write<Size>(match.address & context.physMask, data), true;
    }
    step(1);
    addressException(vaddr);
    return false;
  case Context::Segment::Cached:
    return dcache.write<Size>(vaddr & context.physMask, data), true;
  case Context::Segment::Direct:
    step(1);
    return bus.write<Size>(vaddr & context.physMask, data), true;
  }

  unreachable;
}

auto CPU::addressException(u64 vaddr) -> void {
  scc.badVirtualAddress = vaddr;
  scc.tlb.virtualAddress.bit(13,39) = vaddr >> 13;
  scc.tlb.region = vaddr >> 62;
  scc.context.badVirtualAddress = vaddr >> 13;
  scc.xcontext.badVirtualAddress = vaddr >> 13;
  scc.xcontext.region = vaddr >> 62;
}
