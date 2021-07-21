#include "mode7.cpp"

auto PPU::Background::hires() const -> bool {
  return ppu.io.bgMode == 5 || ppu.io.bgMode == 6;
}

//V = 0, H = 0
auto PPU::Background::frame() -> void {
}

//H = 0
auto PPU::Background::scanline() -> void {
  mosaic.hcounter = ppu.mosaic.size;
  mosaic.hoffset = 0;

  renderingIndex = 0;
  pixelCounter = (io.hoffset & 7) << hires();

  opt.hoffset = 0;
  opt.voffset = 0;
}

//H = 56
auto PPU::Background::begin() -> void {
  //remove partial tile columns that have been scrolled offscreen
  for(auto& data : tiles[0].data) data >>= pixelCounter << 1;
}

auto PPU::Background::fetchNameTable() -> void {
  if(ppu.vcounter() == 0) return;

  uint nameTableIndex = ppu.hcounter() >> 5 << hires();
  int x = (ppu.hcounter() & ~31) >> 2;

  uint hpixel = x << hires();
  uint vpixel = ppu.vcounter();
  uint hscroll = io.hoffset;
  uint vscroll = io.voffset;

  if(hires()) {
    hscroll <<= 1;
    if(ppu.io.interlace) vpixel = vpixel << 1 | (ppu.field() && !mosaic.enable);
  }
  if(mosaic.enable) {
    vpixel -= ppu.mosaic.voffset() << (hires() && ppu.io.interlace);
  }

  bool repeated = false;
  repeat:

  uint hoffset = hpixel + hscroll;
  uint voffset = vpixel + vscroll;

  if(ppu.io.bgMode == 2 || ppu.io.bgMode == 4 || ppu.io.bgMode == 6) {
    auto hlookup = ppu.bg3.opt.hoffset;
    auto vlookup = ppu.bg3.opt.voffset;
    uint valid = 1 << 13 + id;

    if(ppu.io.bgMode == 4) {
      if(hlookup & valid) {
        if(!(hlookup & 0x8000)) {
          hoffset = hpixel + (hlookup & ~7) + (hscroll & 7);
        } else {
          voffset = vpixel + (vlookup);
        }
      }
    } else {
      if(hlookup & valid) hoffset = hpixel + (hlookup & ~7) + (hscroll & 7);
      if(vlookup & valid) voffset = vpixel + (vlookup);
    }
  }

  uint width = 256 << hires();
  uint hsize = width << io.tileSize << io.screenSize.bit(0);
  uint vsize = width << io.tileSize << io.screenSize.bit(1);

  hoffset &= hsize - 1;
  voffset &= vsize - 1;

  uint vtiles = 3 + io.tileSize;
  uint htiles = !hires() ? vtiles : 4;

  uint htile = hoffset >> htiles;
  uint vtile = voffset >> vtiles;

  uint hscreen = io.screenSize.bit(0) ? 32 << 5 : 0;
  uint vscreen = io.screenSize.bit(1) ? 32 << 5 + io.screenSize.bit(0) : 0;

  uint16 offset = (uint5)htile << 0 | (uint5)vtile << 5;
  if(htile & 0x20) offset += hscreen;
  if(vtile & 0x20) offset += vscreen;

  uint16 address = io.screenAddress + offset;
  uint16 attributes = ppu.vram[address];

  auto& tile = tiles[nameTableIndex];
  tile.character = attributes & 0x03ff;
  tile.paletteGroup = attributes >> 10 & 7;
  tile.priority = io.priority[attributes >> 13 & 1];
  tile.hmirror = bool(attributes & 0x4000);
  tile.vmirror = bool(attributes & 0x8000);

  if(htiles == 4 && bool(hoffset & 8) != tile.hmirror) tile.character +=  1;
  if(vtiles == 4 && bool(voffset & 8) != tile.vmirror) tile.character += 16;

  uint characterMask = ppu.vram.mask >> 3 + io.mode;
  uint characterIndex = io.tiledataAddress >> 3 + io.mode;
  uint16 origin = tile.character + characterIndex & characterMask;

  if(tile.vmirror) voffset ^= 7;
  tile.address = (origin << 3 + io.mode) + (voffset & 7);

  uint paletteOffset = ppu.io.bgMode == 0 ? id << 5 : 0;
  uint paletteSize = 2 << io.mode;
  tile.palette = paletteOffset + (tile.paletteGroup << paletteSize);

  nameTableIndex++;
  if(hires() && !repeated) {
    repeated = true;
    hpixel += 8;
    goto repeat;
  }
}

auto PPU::Background::fetchOffset(uint y) -> void {
  if(ppu.vcounter() == 0) return;

  uint characterIndex = ppu.hcounter() >> 5 << hires();
  uint x = characterIndex << 3;

  uint hoffset = x + (io.hoffset & ~7);
  uint voffset = y + (io.voffset);

  uint vtiles = 3 + io.tileSize;
  uint htiles = !hires() ? vtiles : 4;

  uint htile = hoffset >> htiles;
  uint vtile = voffset >> vtiles;

  uint hscreen = io.screenSize.bit(0) ? 32 << 5 : 0;
  uint vscreen = io.screenSize.bit(1) ? 32 << 5 + io.screenSize.bit(0) : 0;

  uint16 offset = (uint5)htile << 0 | (uint5)vtile << 5;
  if(htile & 0x20) offset += hscreen;
  if(vtile & 0x20) offset += vscreen;

  uint16 address = io.screenAddress + offset;
  if(y == 0) opt.hoffset = ppu.vram[address];
  if(y == 8) opt.voffset = ppu.vram[address];
}

auto PPU::Background::fetchCharacter(uint index, bool half) -> void {
  if(ppu.vcounter() == 0) return;

  uint characterIndex = (ppu.hcounter() >> 5 << hires()) + half;

  auto& tile = tiles[characterIndex];
  uint16 data = ppu.vram[tile.address + (index << 3)];

  //reverse bits so that the lowest bit is the left-most pixel
  if(!tile.hmirror) {
    data = data >> 4 & 0x0f0f | data << 4 & 0xf0f0;
    data = data >> 2 & 0x3333 | data << 2 & 0xcccc;
    data = data >> 1 & 0x5555 | data << 1 & 0xaaaa;
  }

  //interleave two bitplanes for faster planar decoding later
  tile.data[index] = (
    ((uint8(data >> 0) * 0x0101010101010101ull & 0x8040201008040201ull) * 0x0102040810204081ull >> 49) & 0x5555
  | ((uint8(data >> 8) * 0x0101010101010101ull & 0x8040201008040201ull) * 0x0102040810204081ull >> 48) & 0xaaaa
  );
}

auto PPU::Background::run(bool screen) -> void {
  if(ppu.vcounter() == 0) return;

  if(screen == Screen::Below) {
    output.above.priority = 0;
    output.below.priority = 0;
    if(!hires()) return;
  }

  if(io.mode == Mode::Mode7) return runMode7();

  auto& tile = tiles[renderingIndex];
  uint8 color = 0;
  if(io.mode >= Mode::BPP2) color |= (tile.data[0] & 3) << 0; tile.data[0] >>= 2;
  if(io.mode >= Mode::BPP4) color |= (tile.data[1] & 3) << 2; tile.data[1] >>= 2;
  if(io.mode >= Mode::BPP8) color |= (tile.data[2] & 3) << 4; tile.data[2] >>= 2;
  if(io.mode >= Mode::BPP8) color |= (tile.data[3] & 3) << 6; tile.data[3] >>= 2;

  Pixel pixel;
  pixel.priority = tile.priority;
  pixel.palette = color ? uint(tile.palette + color) : 0;
  pixel.paletteGroup = tile.paletteGroup;
  if(++pixelCounter == 0) renderingIndex++;

  uint x = ppu.hcounter() - 56 >> 2;

  if(x == 0 && (!hires() || screen == Screen::Below)) {
    mosaic.hcounter = ppu.mosaic.size;
    mosaic.pixel = pixel;
  } else if((!hires() || screen == Screen::Below) && --mosaic.hcounter == 0) {
    mosaic.hcounter = ppu.mosaic.size;
    mosaic.pixel = pixel;
  } else if(mosaic.enable) {
    pixel = mosaic.pixel;
  }
  if(screen == Screen::Above) x++;
  if(pixel.palette == 0) return;

  if(!hires() || screen == Screen::Above) if(io.aboveEnable) output.above = pixel;
  if(!hires() || screen == Screen::Below) if(io.belowEnable) output.below = pixel;
}

auto PPU::Background::power() -> void {
  io = {};
  io.tiledataAddress = (random() & 0x0f) << 12;
  io.screenAddress = (random() & 0xfc) << 8;
  io.screenSize = random();
  io.tileSize = random();
  io.aboveEnable = random();
  io.belowEnable = random();
  io.hoffset = random();
  io.voffset = random();

  output.above = {};
  output.below = {};

  mosaic = {};
  mosaic.enable = random();
}
