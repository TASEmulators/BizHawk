//the N64 TLB is 32-bit only: only the 64-bit XTLB exception vector is used.

auto CPU::TLB::load(u32 address) -> Match {
  for(auto& entry : this->entry) {
    if(!entry.globals && entry.addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((u32)(address & entry.addressMaskHi) != (u32)entry.virtualAddress) continue;
    bool lo = address & entry.addressSelect;
    if(!entry.valid[lo]) {
      exception(address);
      self.debugger.tlbLoadInvalid(address);
      self.exception.tlbLoadInvalid();
      return {false};
    }
    physicalAddress = entry.physicalAddress[lo] + (address & entry.addressMaskLo);
    self.debugger.tlbLoad(address, physicalAddress);
    return {true, entry.cacheAlgorithm[lo] != 2, physicalAddress};
  }
  exception(address);
  self.debugger.tlbLoadMiss(address);
  self.exception.tlbLoadMiss();
  return {false};
}

auto CPU::TLB::store(u32 address) -> Match {
  for(auto& entry : this->entry) {
    if(!entry.globals && entry.addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((u32)(address & entry.addressMaskHi) != (u32)entry.virtualAddress) continue;
    bool lo = address & entry.addressSelect;
    if(!entry.valid[lo]) {
      exception(address);
      self.debugger.tlbStoreInvalid(address);
      self.exception.tlbStoreInvalid();
      return {false};
    }
    if(!entry.dirty[lo]) {
      exception(address);
      self.debugger.tlbModification(address);
      self.exception.tlbModification();
      return {false};
    }
    physicalAddress = entry.physicalAddress[lo] + (address & entry.addressMaskLo);
    self.debugger.tlbStore(address, physicalAddress);
    return {true, entry.cacheAlgorithm[lo] != 2, physicalAddress};
  }
  exception(address);
  self.debugger.tlbStoreMiss(address);
  self.exception.tlbStoreMiss();
  return {false};
}

auto CPU::TLB::exception(u32 address) -> void {
  self.scc.badVirtualAddress = address;
  self.scc.tlb.virtualAddress.bit(13,39) = address >> 13;
  self.scc.context.badVirtualAddress = address >> 13;
  self.scc.xcontext.badVirtualAddress = address >> 13;
  self.scc.xcontext.region = 0;
}

auto CPU::TLB::Entry::synchronize() -> void {
  pageMask = pageMask & (0b101010101010 << 13);
  pageMask |= pageMask >> 1;
  globals = global[0] && global[1];
  addressMaskHi = ~(n40)(pageMask | 0x1fff);
  addressMaskLo = (pageMask | 0x1fff) >> 1;
  addressSelect = addressMaskLo + 1;
  physicalAddress[0] &= 0xffff'ffff;
  physicalAddress[1] &= 0xffff'ffff;
  virtualAddress &= addressMaskHi;
  global[0] = global[1] = globals;
}
