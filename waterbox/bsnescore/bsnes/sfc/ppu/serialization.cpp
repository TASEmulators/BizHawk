auto PPU::serialize(serializer& s) -> void {
  s.integer(display.interlace);
  s.integer(display.overscan);
  s.integer(display.vdisp);

  if(system.fastPPU()) {
    return ppufast.serialize(s);
  }

  Thread::serialize(s);
  PPUcounter::serialize(s);

  s.integer(vram.mask);
  s.array(vram.data, vram.mask + 1);

  s.integer(ppu1.version);
  s.integer(ppu1.mdr);

  s.integer(ppu2.version);
  s.integer(ppu2.mdr);

  s.integer(latch.vram);
  s.integer(latch.oam);
  s.integer(latch.cgram);
  s.integer(latch.bgofsPPU1);
  s.integer(latch.bgofsPPU2);
  s.integer(latch.mode7);
  s.integer(latch.counters);
  s.integer(latch.hcounter);
  s.integer(latch.vcounter);

  s.integer(latch.oamAddress);
  s.integer(latch.cgramAddress);

  s.integer(io.displayDisable);
  s.integer(io.displayBrightness);

  s.integer(io.oamBaseAddress);
  s.integer(io.oamAddress);
  s.integer(io.oamPriority);

  s.integer(io.bgMode);
  s.integer(io.bgPriority);

  s.integer(io.hoffsetMode7);
  s.integer(io.voffsetMode7);

  s.integer(io.vramIncrementMode);
  s.integer(io.vramMapping);
  s.integer(io.vramIncrementSize);

  s.integer(io.vramAddress);

  s.integer(io.repeatMode7);
  s.integer(io.vflipMode7);
  s.integer(io.hflipMode7);

  s.integer(io.m7a);
  s.integer(io.m7b);
  s.integer(io.m7c);
  s.integer(io.m7d);
  s.integer(io.m7x);
  s.integer(io.m7y);

  s.integer(io.cgramAddress);
  s.integer(io.cgramAddressLatch);

  s.integer(io.extbg);
  s.integer(io.pseudoHires);
  s.integer(io.overscan);
  s.integer(io.interlace);

  s.integer(io.hcounter);
  s.integer(io.vcounter);

  mosaic.serialize(s);
  bg1.serialize(s);
  bg2.serialize(s);
  bg3.serialize(s);
  bg4.serialize(s);
  obj.serialize(s);
  window.serialize(s);
  screen.serialize(s);
}

auto PPU::Mosaic::serialize(serializer& s) -> void {
  s.integer(size);
  s.integer(vcounter);
}

auto PPU::Background::serialize(serializer& s) -> void {
  s.integer(io.tiledataAddress);
  s.integer(io.screenAddress);
  s.integer(io.screenSize);
  s.integer(io.tileSize);
  s.integer(io.mode);
  s.array(io.priority);
  s.integer(io.aboveEnable);
  s.integer(io.belowEnable);
  s.integer(io.hoffset);
  s.integer(io.voffset);

  s.integer(output.above.priority);
  s.integer(output.above.palette);
  s.integer(output.above.paletteGroup);

  s.integer(output.below.priority);
  s.integer(output.below.palette);
  s.integer(output.below.paletteGroup);

  s.integer(mosaic.enable);
  s.integer(mosaic.hcounter);
  s.integer(mosaic.hoffset);

  s.integer(mosaic.pixel.priority);
  s.integer(mosaic.pixel.palette);
  s.integer(mosaic.pixel.paletteGroup);

  s.integer(opt.hoffset);
  s.integer(opt.voffset);

  for(auto& tile : tiles) {
    s.integer(tile.address);
    s.integer(tile.character);
    s.integer(tile.palette);
    s.integer(tile.paletteGroup);
    s.integer(tile.priority);
    s.integer(tile.hmirror);
    s.integer(tile.vmirror);
    s.array(tile.data);
  }

  s.integer(renderingIndex);
  s.integer(pixelCounter);
}

auto PPU::Object::serialize(serializer& s) -> void {
  for(auto& object : oam.object) {
    s.integer(object.x);
    s.integer(object.y);
    s.integer(object.character);
    s.integer(object.nameselect);
    s.integer(object.vflip);
    s.integer(object.hflip);
    s.integer(object.priority);
    s.integer(object.palette);
    s.integer(object.size);
  }

  s.integer(io.aboveEnable);
  s.integer(io.belowEnable);
  s.integer(io.interlace);

  s.integer(io.baseSize);
  s.integer(io.nameselect);
  s.integer(io.tiledataAddress);
  s.integer(io.firstSprite);

  s.array(io.priority);

  s.integer(io.timeOver);
  s.integer(io.rangeOver);

  s.integer(latch.firstSprite);

  s.integer(t.x);
  s.integer(t.y);

  s.integer(t.itemCount);
  s.integer(t.tileCount);

  s.integer(t.active);
  for(auto p : range(2)) {
    for(auto n : range(32)) {
      s.integer(t.item[p][n].valid);
      s.integer(t.item[p][n].index);
    }
    for(auto n : range(34)) {
      s.integer(t.tile[p][n].valid);
      s.integer(t.tile[p][n].x);
      s.integer(t.tile[p][n].priority);
      s.integer(t.tile[p][n].palette);
      s.integer(t.tile[p][n].hflip);
      s.integer(t.tile[p][n].data);
    }
  }

  s.integer(output.above.priority);
  s.integer(output.above.palette);

  s.integer(output.below.priority);
  s.integer(output.below.palette);
}

auto PPU::Window::serialize(serializer& s) -> void {
  s.integer(io.bg1.oneEnable);
  s.integer(io.bg1.oneInvert);
  s.integer(io.bg1.twoEnable);
  s.integer(io.bg1.twoInvert);
  s.integer(io.bg1.mask);
  s.integer(io.bg1.aboveEnable);
  s.integer(io.bg1.belowEnable);

  s.integer(io.bg2.oneEnable);
  s.integer(io.bg2.oneInvert);
  s.integer(io.bg2.twoEnable);
  s.integer(io.bg2.twoInvert);
  s.integer(io.bg2.mask);
  s.integer(io.bg2.aboveEnable);
  s.integer(io.bg2.belowEnable);

  s.integer(io.bg3.oneEnable);
  s.integer(io.bg3.oneInvert);
  s.integer(io.bg3.twoEnable);
  s.integer(io.bg3.twoInvert);
  s.integer(io.bg3.mask);
  s.integer(io.bg3.aboveEnable);
  s.integer(io.bg3.belowEnable);

  s.integer(io.bg4.oneEnable);
  s.integer(io.bg4.oneInvert);
  s.integer(io.bg4.twoEnable);
  s.integer(io.bg4.twoInvert);
  s.integer(io.bg4.mask);
  s.integer(io.bg4.aboveEnable);
  s.integer(io.bg4.belowEnable);

  s.integer(io.obj.oneEnable);
  s.integer(io.obj.oneInvert);
  s.integer(io.obj.twoEnable);
  s.integer(io.obj.twoInvert);
  s.integer(io.obj.mask);
  s.integer(io.obj.aboveEnable);
  s.integer(io.obj.belowEnable);

  s.integer(io.col.oneEnable);
  s.integer(io.col.oneInvert);
  s.integer(io.col.twoEnable);
  s.integer(io.col.twoInvert);
  s.integer(io.col.mask);
  s.integer(io.col.aboveMask);
  s.integer(io.col.belowMask);

  s.integer(io.oneLeft);
  s.integer(io.oneRight);
  s.integer(io.twoLeft);
  s.integer(io.twoRight);

  s.integer(output.above.colorEnable);
  s.integer(output.below.colorEnable);

  s.integer(x);
}

auto PPU::Screen::serialize(serializer& s) -> void {
  s.array(cgram);

  s.integer(io.blendMode);
  s.integer(io.directColor);

  s.integer(io.colorMode);
  s.integer(io.colorHalve);
  s.integer(io.bg1.colorEnable);
  s.integer(io.bg2.colorEnable);
  s.integer(io.bg3.colorEnable);
  s.integer(io.bg4.colorEnable);
  s.integer(io.obj.colorEnable);
  s.integer(io.back.colorEnable);

  s.integer(io.colorBlue);
  s.integer(io.colorGreen);
  s.integer(io.colorRed);

  s.integer(math.above.color);
  s.integer(math.above.colorEnable);
  s.integer(math.below.color);
  s.integer(math.below.colorEnable);
  s.integer(math.transparent);
  s.integer(math.blendMode);
  s.integer(math.colorHalve);
}
