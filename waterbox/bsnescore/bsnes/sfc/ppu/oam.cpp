auto PPU::OAM::read(uint10 address) -> uint8 {
  if(!(address & 0x200)) {
    uint n = address >> 2;  //object#
    address &= 3;
    if(address == 0) return object[n].x & 0xff;
    if(address == 1) return object[n].y;
    if(address == 2) return object[n].character;
    return (
      object[n].nameselect << 0
    | object[n].palette    << 1
    | object[n].priority   << 4
    | object[n].hflip      << 6
    | object[n].vflip      << 7
    );
  } else {
    uint n = (address & 0x1f) << 2;  //object#
    return (
      object[n + 0].x >> 8 << 0
    | object[n + 0].size   << 1
    | object[n + 1].x >> 8 << 2
    | object[n + 1].size   << 3
    | object[n + 2].x >> 8 << 4
    | object[n + 2].size   << 5
    | object[n + 3].x >> 8 << 6
    | object[n + 3].size   << 7
    );
  }
}

auto PPU::OAM::write(uint10 address, uint8 data) -> void {
  if(!(address & 0x200)) {
    uint n = address >> 2;  //object#
    address &= 3;
    if(address == 0) { object[n].x = object[n].x & 0x100 | data; return; }
    if(address == 1) { object[n].y = data; return; }
    if(address == 2) { object[n].character = data; return; }
    object[n].nameselect = data >> 0 & 1;
    object[n].palette    = data >> 1 & 7;
    object[n].priority   = data >> 4 & 3;
    object[n].hflip      = data >> 6 & 1;
    object[n].vflip      = data >> 7 & 1;
  } else {
    uint n = (address & 0x1f) << 2;  //object#
    object[n + 0].x    = object[n + 0].x & 0xff | bool(data & 0x01) << 8;
    object[n + 0].size = bool(data & 0x02);
    object[n + 1].x    = object[n + 1].x & 0xff | bool(data & 0x04) << 8;
    object[n + 1].size = bool(data & 0x08);
    object[n + 2].x    = object[n + 2].x & 0xff | bool(data & 0x10) << 8;
    object[n + 2].size = bool(data & 0x20);
    object[n + 3].x    = object[n + 3].x & 0xff | bool(data & 0x40) << 8;
    object[n + 3].size = bool(data & 0x80);
  }
}

auto PPU::OAM::Object::width() const -> uint {
  if(size == 0) {
    static const uint width[] = { 8,  8,  8, 16, 16, 32, 16, 16};
    return width[ppu.obj.io.baseSize];
  } else {
    static const uint width[] = {16, 32, 64, 32, 64, 64, 32, 32};
    return width[ppu.obj.io.baseSize];
  }
}

auto PPU::OAM::Object::height() const -> uint {
  if(size == 0) {
    if(ppu.obj.io.interlace && ppu.obj.io.baseSize >= 6) return 16;  //hardware quirk
    static const uint height[] = { 8,  8,  8, 16, 16, 32, 32, 32};
    return height[ppu.obj.io.baseSize];
  } else {
    static const uint height[] = {16, 32, 64, 32, 64, 64, 64, 32};
    return height[ppu.obj.io.baseSize];
  }
}
