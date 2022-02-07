auto PPUcounter::tick() -> void {
  time.hcounter += 2;  //increment by smallest unit of time.
  if(time.hcounter == hperiod()) {
    last.hperiod = hperiod();
    time.hcounter = 0;
    tickScanline();
  }
}

auto PPUcounter::tick(uint clocks) -> void {
  time.hcounter += clocks;
  if(time.hcounter >= hperiod()) {
    last.hperiod = hperiod();
    time.hcounter -= hperiod();
    tickScanline();
  }
}

auto PPUcounter::tickScanline() -> void {
  if(++time.vcounter == 128) {
    //it's not important when this is captured: it is only needed at V=240 or V=311.
    time.interlace = ppu.interlace();
    time.vperiod += interlace() && !field();
  }

  if(vcounter() == vperiod()) {
    last.vperiod = vperiod();
    //this may be off by one until V=128, hence why vperiod() is a private function.
    time.vperiod = Region::NTSC() ? 262 : 312;
    time.vcounter = 0;
    time.field ^= 1;
  }

  time.hperiod = 1364;
  //NTSC and PAL scanline rates would not match up with color clocks if every scanline were 1364 clocks.
  //to offset for this error, NTSC has one short scanline, and PAL has one long scanline.
  if(Region::NTSC() && interlace() == 0 && field() == 1 && vcounter() == 240) time.hperiod -= 4;
  if(Region::PAL()  && interlace() == 1 && field() == 1 && vcounter() == 311) time.hperiod += 4;
  if(scanline) scanline();
}

auto PPUcounter::interlace() const -> bool { return time.interlace; }
auto PPUcounter::field() const -> bool { return time.field; }
auto PPUcounter::vcounter() const -> uint { return time.vcounter; }
auto PPUcounter::hcounter() const -> uint { return time.hcounter; }
auto PPUcounter::vperiod() const -> uint { return time.vperiod; }
auto PPUcounter::hperiod() const -> uint { return time.hperiod; }

auto PPUcounter::vcounter(uint offset) const -> uint {
  if(offset <= hcounter()) return vcounter();
  if(vcounter() > 0) return vcounter() - 1;
  return last.vperiod - 1;
}

auto PPUcounter::hcounter(uint offset) const -> uint {
  if(offset <= hcounter()) return hcounter() - offset;
  return hcounter() + last.hperiod - offset;
}

//one PPU dot = 4 CPU clocks.
//
//PPU dots 323 and 327 are 6 CPU clocks long.
//this does not apply to NTSC non-interlace scanline 240 on odd fields. this is
//because the PPU skips one dot to alter the color burst phase of the video signal.
//it is not known what happens for PAL 1368 clock scanlines.
//
//dot 323 range = {1292, 1294, 1296}
//dot 327 range = {1310, 1312, 1314}

auto PPUcounter::hdot() const -> uint {
  if(hperiod() == 1360) {
    return hcounter() >> 2;
  } else {
    return hcounter() - ((hcounter() > 1292) << 1) - ((hcounter() > 1310) << 1) >> 2;
  }
}

auto PPUcounter::reset() -> void {
  time = {};
  last = {};

  time.vperiod = last.vperiod = Region::NTSC() ? 262 : 312;
  time.hperiod = last.hperiod = 1364;
}
