auto PPU::serialize(serializer& s) -> void {
  ppubase.Thread::serialize(s);
  PPUcounter::serialize(s);

  latch.serialize(s);
  io.serialize(s);
  s.array(vram);
  s.array(cgram);
  for(auto& object : objects) object.serialize(s);

  Line::start = 0;
  Line::count = 0;
}

auto PPU::Latch::serialize(serializer& s) -> void {
  s.integer(interlace);
  s.integer(overscan);
  s.integer(hires);
  s.integer(hd);
  s.integer(ss);
  s.integer(vram);
  s.integer(oam);
  s.integer(cgram);
  s.integer(oamAddress);
  s.integer(cgramAddress);
  s.integer(mode7);
  s.integer(counters);
  s.integer(hcounter);
  s.integer(vcounter);
  ppu1.serialize(s);
  ppu2.serialize(s);
}

auto PPU::Latch::PPUstate::serialize(serializer& s) -> void {
  s.integer(mdr);
  s.integer(bgofs);
}

auto PPU::IO::serialize(serializer& s) -> void {
  s.integer(displayDisable);
  s.integer(displayBrightness);
  s.integer(oamBaseAddress);
  s.integer(oamAddress);
  s.integer(oamPriority);
  s.integer(bgPriority);
  s.integer(bgMode);
  s.integer(vramIncrementMode);
  s.integer(vramMapping);
  s.integer(vramIncrementSize);
  s.integer(vramAddress);
  s.integer(cgramAddress);
  s.integer(cgramAddressLatch);
  s.integer(hcounter);
  s.integer(vcounter);
  s.integer(interlace);
  s.integer(overscan);
  s.integer(pseudoHires);
  s.integer(extbg);

  mosaic.serialize(s);
  mode7.serialize(s);
  window.serialize(s);
  bg1.serialize(s);
  bg2.serialize(s);
  bg3.serialize(s);
  bg4.serialize(s);
  obj.serialize(s);
  col.serialize(s);
}

auto PPU::IO::Mosaic::serialize(serializer& s) -> void {
  s.integer(size);
  s.integer(counter);
}

auto PPU::IO::Mode7::serialize(serializer& s) -> void {
  s.integer(hflip);
  s.integer(vflip);
  s.integer(repeat);
  s.integer(a);
  s.integer(b);
  s.integer(c);
  s.integer(d);
  s.integer(x);
  s.integer(y);
  s.integer(hoffset);
  s.integer(voffset);
}

auto PPU::IO::Window::serialize(serializer& s) -> void {
  s.integer(oneLeft);
  s.integer(oneRight);
  s.integer(twoLeft);
  s.integer(twoRight);
}

auto PPU::IO::WindowLayer::serialize(serializer& s) -> void {
  s.integer(oneEnable);
  s.integer(oneInvert);
  s.integer(twoEnable);
  s.integer(twoInvert);
  s.integer(mask);
  s.integer(aboveEnable);
  s.integer(belowEnable);
}

auto PPU::IO::WindowColor::serialize(serializer& s) -> void {
  s.integer(oneEnable);
  s.integer(oneInvert);
  s.integer(twoEnable);
  s.integer(twoInvert);
  s.integer(mask);
  s.integer(aboveMask);
  s.integer(belowMask);
}

auto PPU::IO::Background::serialize(serializer& s) -> void {
  window.serialize(s);
  s.integer(aboveEnable);
  s.integer(belowEnable);
  s.integer(mosaicEnable);
  s.integer(tiledataAddress);
  s.integer(screenAddress);
  s.integer(screenSize);
  s.integer(tileSize);
  s.integer(hoffset);
  s.integer(voffset);
  s.integer(tileMode);
  s.array(priority);
}

auto PPU::IO::Object::serialize(serializer& s) -> void {
  window.serialize(s);
  s.integer(aboveEnable);
  s.integer(belowEnable);
  s.integer(interlace);
  s.integer(baseSize);
  s.integer(nameselect);
  s.integer(tiledataAddress);
  s.integer(first);
  s.integer(rangeOver);
  s.integer(timeOver);
  s.array(priority);
}

auto PPU::IO::Color::serialize(serializer& s) -> void {
  window.serialize(s);
  s.array(enable);
  s.integer(directColor);
  s.integer(blendMode);
  s.integer(halve);
  s.integer(mathMode);
  s.integer(fixedColor);
}

auto PPU::Object::serialize(serializer& s) -> void {
  s.integer(x);
  s.integer(y);
  s.integer(character);
  s.integer(nameselect);
  s.integer(vflip);
  s.integer(hflip);
  s.integer(priority);
  s.integer(palette);
  s.integer(size);
}
