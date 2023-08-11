
auto CPU::TLB::load(u64 vaddr, const Entry& entry) -> Match {
  bool lo = vaddr & entry.addressSelect;
  if(!entry.valid[lo]) {
    self.addressException(vaddr);
    self.debugger.tlbLoadInvalid(vaddr);
    self.exception.tlbLoadInvalid();
    return {false};
  }
  physicalAddress = entry.physicalAddress[lo] + (vaddr & entry.addressMaskLo);
  self.debugger.tlbLoad(vaddr, physicalAddress);
  return {true, entry.cacheAlgorithm[lo] != 2, physicalAddress};
}

auto CPU::TLB::load(u64 vaddr) -> Match {
  for(auto& entry : this->tlbCache.entry) {
    if(!entry.entry) continue;
    if(!entry.entry->globals && entry.entry->addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((vaddr & entry.entry->addressMaskHi) != entry.entry->virtualAddress) continue;
    if(vaddr >> 62 != entry.entry->region) continue;
    if(auto match = load(vaddr, *entry.entry)) {
      entry.frequency++;
      return match;
    }
  }

  for(auto& entry : this->entry) {
    if(!entry.globals && entry.addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((vaddr & entry.addressMaskHi) != entry.virtualAddress) continue;
    if(vaddr >> 62 != entry.region) continue;
    if(auto match = load(vaddr, entry)) {
      this->tlbCache.insert(entry);
      return match;
    }
  }

  self.addressException(vaddr);
  self.debugger.tlbLoadMiss(vaddr);
  self.exception.tlbLoadMiss();
  return {false};
}

// Fast(er) version of load for recompiler icache lookups
// avoids exceptions/debug checks
auto CPU::TLB::loadFast(u64 vaddr) -> Match {
  for(auto& entry : this->entry) {
    if(!entry.globals && entry.addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((vaddr & entry.addressMaskHi) != entry.virtualAddress) continue;
    if(vaddr >> 62 != entry.region) continue;
    bool lo = vaddr & entry.addressSelect;
    if(!entry.valid[lo]) return { false, 0, 0 };
    physicalAddress = entry.physicalAddress[lo] + (vaddr & entry.addressMaskLo);
    return {true, entry.cacheAlgorithm[lo] != 2, physicalAddress};
  }

  return {false, 0, 0};
}

auto CPU::TLB::store(u64 vaddr, const Entry& entry) -> Match {
  bool lo = vaddr & entry.addressSelect;
  if(!entry.valid[lo]) {
    self.addressException(vaddr);
    self.debugger.tlbStoreInvalid(vaddr);
    self.exception.tlbStoreInvalid();
    return {false};
  }
  if(!entry.dirty[lo]) {
    self.addressException(vaddr);
    self.debugger.tlbModification(vaddr);
    self.exception.tlbModification();
    return {false};
  }
  physicalAddress = entry.physicalAddress[lo] + (vaddr & entry.addressMaskLo);
  self.debugger.tlbStore(vaddr, physicalAddress);
  return {true, entry.cacheAlgorithm[lo] != 2, physicalAddress};
}

auto CPU::TLB::store(u64 vaddr) -> Match {
  for(auto& entry : this->tlbCache.entry) {
    if(!entry.entry) continue;
    if(!entry.entry->globals && entry.entry->addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((vaddr & entry.entry->addressMaskHi) != entry.entry->virtualAddress) continue;
    if(vaddr >> 62 != entry.entry->region) continue;

    if(auto match = store(vaddr, *entry.entry)) {
      entry.frequency++;
      return match;
    }
  }

  for(auto& entry : this->entry) {
    if(!entry.globals && entry.addressSpaceID != self.scc.tlb.addressSpaceID) continue;
    if((vaddr & entry.addressMaskHi) != entry.virtualAddress) continue;
    if(vaddr >> 62 != entry.region) continue;

    if(auto match = store(vaddr, entry)) {
      this->tlbCache.insert(entry);
      return match;
    }
  }

  self.addressException(vaddr);
  self.debugger.tlbStoreMiss(vaddr);
  self.exception.tlbStoreMiss();
  return {false};
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
