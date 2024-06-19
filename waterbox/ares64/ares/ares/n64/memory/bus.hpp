template<u32 Size>
inline auto Bus::read(u32 address, Thread& thread, const char *peripheral) -> u64 {
  static_assert(Size == Byte || Size == Half || Size == Word || Size == Dual);

  if(address <= 0x03ef'ffff) return rdram.ram.read<Size>(address, peripheral);
  if(address <= 0x03ff'ffff) return rdram.read<Size>(address, thread);
  if(Size == Dual)           return freezeDualRead(address), 0;
  if(address <= 0x0407'ffff) return rsp.read<Size>(address, thread);
  if(address <= 0x040b'ffff) return rsp.status.read<Size>(address, thread);
  if(address <= 0x040f'ffff) return freezeUnmapped(address), 0;
  if(address <= 0x041f'ffff) return rdp.read<Size>(address, thread);
  if(address <= 0x042f'ffff) return rdp.io.read<Size>(address, thread);
  if(address <= 0x043f'ffff) return mi.read<Size>(address, thread);
  if(address <= 0x044f'ffff) return vi.read<Size>(address, thread);
  if(address <= 0x045f'ffff) return ai.read<Size>(address, thread);
  if(address <= 0x046f'ffff) return pi.read<Size>(address, thread);
  if(address <= 0x047f'ffff) return ri.read<Size>(address, thread);
  if(address <= 0x048f'ffff) return si.read<Size>(address, thread);
  if(address <= 0x04ff'ffff) return freezeUnmapped(address), 0;
  if(address <= 0x1fbf'ffff) return pi.read<Size>(address, thread);
  if(address <= 0x1fcf'ffff) return si.read<Size>(address, thread);
  if(address <= 0x7fff'ffff) return pi.read<Size>(address, thread);
  return freezeUnmapped(address), 0;
}

template<u32 Size>
inline auto Bus::readBurst(u32 address, u32 *data, Thread& thread) -> void {
  static_assert(Size == DCache || Size == ICache);

  if(address <= 0x03ef'ffff) return rdram.ram.readBurst<Size>(address, data, "CPU");
  if(address <= 0x03ff'ffff) {
    // FIXME: not hardware validated, no idea of the behavior
    data[0] = rdram.readWord(address | 0x0, thread);
    data[1] = 0;
    data[2] = 0;
    data[3] = 0;
    if constexpr(Size == ICache) {
      data[4] = 0;
      data[5] = 0;
      data[6] = 0;
      data[7] = 0;
    }
    return;
  }

  return freezeUncached(address);
}

template<u32 Size>
inline auto Bus::write(u32 address, u64 data, Thread& thread, const char *peripheral) -> void {
  static_assert(Size == Byte || Size == Half || Size == Word || Size == Dual);
  if constexpr(Accuracy::CPU::Recompiler) {
    cpu.recompiler.invalidate(address + 0); if constexpr(Size == Dual)
    cpu.recompiler.invalidate(address + 4);
  }

  if(address <= 0x03ef'ffff) return rdram.ram.write<Size>(address, data, peripheral);
  if(address <= 0x03ff'ffff) return rdram.write<Size>(address, data, thread);
  if(address <= 0x0407'ffff) return rsp.write<Size>(address, data, thread);
  if(address <= 0x040b'ffff) return rsp.status.write<Size>(address, data, thread);
  if(address <= 0x040f'ffff) return freezeUnmapped(address);
  if(address <= 0x041f'ffff) return rdp.write<Size>(address, data, thread);
  if(address <= 0x042f'ffff) return rdp.io.write<Size>(address, data, thread);
  if(address <= 0x043f'ffff) return mi.write<Size>(address, data, thread);
  if(address <= 0x044f'ffff) return vi.write<Size>(address, data, thread);
  if(address <= 0x045f'ffff) return ai.write<Size>(address, data, thread);
  if(address <= 0x046f'ffff) return pi.write<Size>(address, data, thread);
  if(address <= 0x047f'ffff) return ri.write<Size>(address, data, thread);
  if(address <= 0x048f'ffff) return si.write<Size>(address, data, thread);
  if(address <= 0x04ff'ffff) return freezeUnmapped(address);
  if(address <= 0x1fbf'ffff) return pi.write<Size>(address, data, thread);
  if(address <= 0x1fcf'ffff) return si.write<Size>(address, data, thread);
  if(address <= 0x7fff'ffff) return pi.write<Size>(address, data, thread);
  return freezeUnmapped(address);
}

template<u32 Size>
inline auto Bus::writeBurst(u32 address, u32 *data, Thread& thread) -> void {
  static_assert(Size == DCache || Size == ICache);
  if constexpr(Accuracy::CPU::Recompiler) {
    cpu.recompiler.invalidateRange(address, Size == DCache ? 16 : 32);
  }

  if(address <= 0x03ef'ffff) return rdram.ram.writeBurst<Size>(address, data, "CPU");
  if(address <= 0x03ff'ffff) {
    // FIXME: not hardware validated, but a good guess
    rdram.writeWord(address | 0x0, data[0], thread);
    return;
  }

  return freezeUncached(address);
}

inline auto Bus::freezeUnmapped(u32 address) -> void {
  debug(unusual, "[Bus::freezeUnmapped] CPU frozen because of access to RCP unmapped area: 0x", hex(address, 8L));
  cpu.scc.sysadFrozen = true;
}

inline auto Bus::freezeUncached(u32 address) -> void {
  debug(unusual, "[Bus::freezeUncached] CPU frozen because of cached access to non-RDRAM area: 0x", hex(address, 8L));
  cpu.scc.sysadFrozen = true;
}

inline auto Bus::freezeDualRead(u32 address) -> void {
  debug(unusual, "[Bus::freezeDualRead] CPU frozen because of 64-bit read from non-RDRAM area: 0x ", hex(address, 8L));
  cpu.scc.sysadFrozen = true;
}
