auto PPU::Mosaic::enable() const -> bool {
  if(ppu.bg1.mosaic.enable) return true;
  if(ppu.bg2.mosaic.enable) return true;
  if(ppu.bg3.mosaic.enable) return true;
  if(ppu.bg4.mosaic.enable) return true;
  return false;
}

auto PPU::Mosaic::voffset() const -> uint {
  return size - vcounter;
}

//H = 0
auto PPU::Mosaic::scanline() -> void {
  if(ppu.vcounter() == 1) {
    vcounter = enable() ? size + 1 : 0;
  }
  if(vcounter && !--vcounter) {
    vcounter = enable() ? size + 0 : 0;
  }
}

auto PPU::Mosaic::power() -> void {
  size = (random() & 15) + 1;
  vcounter = 0;
}
