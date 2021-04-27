auto PPU::latchCounters(uint hcounter, uint vcounter) -> void {
  io.hcounter = hcounter;
  io.vcounter = vcounter;
  latch.counters = 1;
}

auto PPU::latchCounters() -> void {
  io.hcounter = cpu.hdot();
  io.vcounter = cpu.vcounter();
  latch.counters = 1;
}

auto PPU::vramAddress() const -> uint {
  uint address = io.vramAddress;
  switch(io.vramMapping) {
  case 0: return address & 0x7fff;
  case 1: return address & 0x7f00 | address << 3 & 0x00f8 | address >> 5 & 7;
  case 2: return address & 0x7e00 | address << 3 & 0x01f8 | address >> 6 & 7;
  case 3: return address & 0x7c00 | address << 3 & 0x03f8 | address >> 7 & 7;
  }
  unreachable;
}

auto PPU::readVRAM() -> uint16 {
  if(!io.displayDisable && cpu.vcounter() < vdisp()) return 0x0000;
  auto address = vramAddress();
  return vram[address];
}

template<bool Byte>
auto PPU::writeVRAM(uint8 data) -> void {
  if(!io.displayDisable && cpu.vcounter() < vdisp() && !noVRAMBlocking()) return;
  Line::flush();
  auto address = vramAddress();
  if constexpr(Byte == 0) {
    vram[address] = vram[address] & 0xff00 | data << 0;
  }
  if constexpr(Byte == 1) {
    vram[address] = vram[address] & 0x00ff | data << 8;
  }
}

auto PPU::readOAM(uint10 address) -> uint8 {
  if(!io.displayDisable && cpu.vcounter() < vdisp()) address = latch.oamAddress;
  return readObject(address);
}

auto PPU::writeOAM(uint10 address, uint8 data) -> void {
  Line::flush();
  //0x0218: Uniracers (2-player mode) hack; requires cycle timing for latch.oamAddress to be correct
  if(!io.displayDisable && cpu.vcounter() < vdisp()) address = 0x0218;  //latch.oamAddress;
  return writeObject(address, data);
}

template<bool Byte>
auto PPU::readCGRAM(uint8 address) -> uint8 {
  if(!io.displayDisable
  && cpu.vcounter() > 0 && cpu.vcounter() < vdisp()
  && cpu.hcounter() >= 88 && cpu.hcounter() < 1096
  ) address = latch.cgramAddress;
  if constexpr(Byte == 0) {
    return cgram[address] >> 0;
  }
  if constexpr(Byte == 1) {
    return cgram[address] >> 8;
  }
}

auto PPU::writeCGRAM(uint8 address, uint15 data) -> void {
  if(!io.displayDisable
  && cpu.vcounter() > 0 && cpu.vcounter() < vdisp()
  && cpu.hcounter() >= 88 && cpu.hcounter() < 1096
  ) address = latch.cgramAddress;
  cgram[address] = data;
}

auto PPU::readIO(uint address, uint8 data) -> uint8 {
  cpu.synchronizePPU();

  switch(address & 0xffff) {

  case 0x2104: case 0x2105: case 0x2106: case 0x2108:
  case 0x2109: case 0x210a: case 0x2114: case 0x2115:
  case 0x2116: case 0x2118: case 0x2119: case 0x211a:
  case 0x2124: case 0x2125: case 0x2126: case 0x2128:
  case 0x2129: case 0x212a: {
    return latch.ppu1.mdr;
  }

  case 0x2134: {  //MPYL
    uint result = (int16)io.mode7.a * (int8)(io.mode7.b >> 8);
    return latch.ppu1.mdr = result >> 0;
  }

  case 0x2135: {  //MPYM
    uint result = (int16)io.mode7.a * (int8)(io.mode7.b >> 8);
    return latch.ppu1.mdr = result >> 8;
  }

  case 0x2136: {  //MPYH
    uint result = (int16)io.mode7.a * (int8)(io.mode7.b >> 8);
    return latch.ppu1.mdr = result >> 16;
  }

  case 0x2137: {  //SLHV
    if(cpu.pio() & 0x80) latchCounters();
    return data;  //CPU MDR
  }

  case 0x2138: {  //OAMDATAREAD
    data = readOAM(io.oamAddress);
    io.oamAddress = io.oamAddress + 1 & 0x3ff;
    oamSetFirstObject();
    return latch.ppu1.mdr = data;
  }

  case 0x2139: {  //VMDATALREAD
    data = latch.vram >> 0;
    if(io.vramIncrementMode == 0) {
      latch.vram = readVRAM();
      io.vramAddress += io.vramIncrementSize;
    }
    return latch.ppu1.mdr = data;
  }

  case 0x213a: {  //VMDATAHREAD
    data = latch.vram >> 8;
    if(io.vramIncrementMode == 1) {
      latch.vram = readVRAM();
      io.vramAddress += io.vramIncrementSize;
    }
    return latch.ppu1.mdr = data;
  }

  case 0x213b: {  //CGDATAREAD
    if(io.cgramAddressLatch == 0) {
      io.cgramAddressLatch = 1;
      latch.ppu2.mdr = readCGRAM<0>(io.cgramAddress);
    } else {
      io.cgramAddressLatch = 0;
      latch.ppu2.mdr = readCGRAM<1>(io.cgramAddress++) & 0x7f | latch.ppu2.mdr & 0x80;
    }
    return latch.ppu2.mdr;
  }

  case 0x213c: {  //OPHCT
    if(latch.hcounter == 0) {
      latch.hcounter = 1;
      latch.ppu2.mdr = io.hcounter;
    } else {
      latch.hcounter = 0;
      latch.ppu2.mdr = io.hcounter >> 8 | latch.ppu2.mdr & 0xfe;
    }
    return latch.ppu2.mdr;
  }

  case 0x213d: {  //OPVCT
    if(latch.vcounter == 0) {
      latch.vcounter = 1;
      latch.ppu2.mdr = io.vcounter;
    } else {
      latch.vcounter = 0;
      latch.ppu2.mdr = io.vcounter >> 8 | latch.ppu2.mdr & 0xfe;
    }
    return latch.ppu2.mdr;
  }

  case 0x213e: {  //STAT77
    latch.ppu1.mdr = 0x01 | io.obj.rangeOver << 6 | io.obj.timeOver << 7;
    return latch.ppu1.mdr;
  }

  case 0x213f: {  //STAT78
    latch.hcounter = 0;
    latch.vcounter = 0;
    latch.ppu2.mdr &= 1 << 5;
    latch.ppu2.mdr |= 0x03 | Region::PAL() << 4 | field() << 7;
    if(!(cpu.pio() & 0x80)) {
      latch.ppu2.mdr |= 1 << 6;
    } else {
      latch.ppu2.mdr |= latch.counters << 6;
      latch.counters = 0;
    }
    return latch.ppu2.mdr;
  }

  }

  return data;
}

auto PPU::writeIO(uint address, uint8 data) -> void {
  cpu.synchronizePPU();

  switch(address & 0xffff) {

  case 0x2100: {  //INIDISP
    if(io.displayDisable && cpu.vcounter() == vdisp()) oamAddressReset();
    io.displayBrightness = data >> 0 & 15;
    io.displayDisable    = data >> 7 & 1;
    return;
  }

  case 0x2101: {  //OBSEL
    io.obj.tiledataAddress = data << 13 & 0x6000;
    io.obj.nameselect      = data >> 3 & 3;
    io.obj.baseSize        = data >> 5 & 7;
    return;
  }

  case 0x2102: {  //OAMADDL
    io.oamBaseAddress = (io.oamBaseAddress & 0x0200) | data << 1;
    oamAddressReset();
    return;
  }

  case 0x2103: {  //OAMADDH
    io.oamBaseAddress = (data & 1) << 9 | io.oamBaseAddress & 0x01fe;
    io.oamPriority    = data >> 7 & 1;
    oamAddressReset();
    return;
  }

  case 0x2104: {  //OAMDATA
    bool latchBit = io.oamAddress & 1;
    uint address = io.oamAddress;
    io.oamAddress = io.oamAddress + 1 & 0x3ff;
    if(latchBit == 0) latch.oam = data;
    if(address & 0x200) {
      writeOAM(address, data);
    } else if(latchBit == 1) {
      writeOAM((address & ~1) + 0, latch.oam);
      writeOAM((address & ~1) + 1, data);
    }
    oamSetFirstObject();
    return;
  }

  case 0x2105: {  //BGMODE
    io.bgMode       = data >> 0 & 7;
    io.bgPriority   = data >> 3 & 1;
    io.bg1.tileSize = data >> 4 & 1;
    io.bg2.tileSize = data >> 5 & 1;
    io.bg3.tileSize = data >> 6 & 1;
    io.bg4.tileSize = data >> 7 & 1;
    updateVideoMode();
    return;
  }

  case 0x2106: {  //MOSAIC
    bool mosaicEnable = io.bg1.mosaicEnable || io.bg2.mosaicEnable || io.bg3.mosaicEnable || io.bg4.mosaicEnable;
    io.bg1.mosaicEnable = data >> 0 & 1;
    io.bg2.mosaicEnable = data >> 1 & 1;
    io.bg3.mosaicEnable = data >> 2 & 1;
    io.bg4.mosaicEnable = data >> 3 & 1;
    io.mosaic.size      = (data >> 4 & 15) + 1;
    if(!mosaicEnable && (data >> 0 & 15)) {
      //mosaic vcounter is reloaded when mosaic becomes enabled
      io.mosaic.counter = io.mosaic.size + 1;
    }
    return;
  }

  case 0x2107: {  //BG1SC
    io.bg1.screenSize    = data >> 0 & 3;
    io.bg1.screenAddress = data << 8 & 0x7c00;
    return;
  }

  case 0x2108: {  //BG2SC
    io.bg2.screenSize    = data >> 0 & 3;
    io.bg2.screenAddress = data << 8 & 0x7c00;
    return;
  }

  case 0x2109: {  //BG3SC
    io.bg3.screenSize    = data >> 0 & 3;
    io.bg3.screenAddress = data << 8 & 0x7c00;
    return;
  }

  case 0x210a: {  //BG4SC
    io.bg4.screenSize    = data >> 0 & 3;
    io.bg4.screenAddress = data << 8 & 0x7c00;
    return;
  }

  case 0x210b: {  //BG12NBA
    io.bg1.tiledataAddress = data << 12 & 0x7000;
    io.bg2.tiledataAddress = data <<  8 & 0x7000;
    return;
  }

  case 0x210c: {  //BG34NBA
    io.bg3.tiledataAddress = data << 12 & 0x7000;
    io.bg4.tiledataAddress = data <<  8 & 0x7000;
    return;
  }

  case 0x210d: {  //BG1HOFS
    io.mode7.hoffset = data << 8 | latch.mode7;
    latch.mode7 = data;

    io.bg1.hoffset = data << 8 | (latch.ppu1.bgofs & ~7) | (latch.ppu2.bgofs & 7);
    latch.ppu1.bgofs = data;
    latch.ppu2.bgofs = data;
    return;
  }

  case 0x210e: {  //BG1VOFS
    io.mode7.voffset = data << 8 | latch.mode7;
    latch.mode7 = data;

    io.bg1.voffset = data << 8 | latch.ppu1.bgofs;
    latch.ppu1.bgofs = data;
    return;
  }

  case 0x210f: {  //BG2HOFS
    io.bg2.hoffset = data << 8 | (latch.ppu1.bgofs & ~7) | (latch.ppu2.bgofs & 7);
    latch.ppu1.bgofs = data;
    latch.ppu2.bgofs = data;
    return;
  }

  case 0x2110: {  //BG2VOFS
    io.bg2.voffset = data << 8 | latch.ppu1.bgofs;
    latch.ppu1.bgofs = data;
    return;
  }

  case 0x2111: {  //BG3HOFS
    io.bg3.hoffset = data << 8 | (latch.ppu1.bgofs & ~7) | (latch.ppu2.bgofs & 7);
    latch.ppu1.bgofs = data;
    latch.ppu2.bgofs = data;
    return;
  }

  case 0x2112: {  //BG3VOFS
    io.bg3.voffset = data << 8 | latch.ppu1.bgofs;
    latch.ppu1.bgofs = data;
    return;
  }

  case 0x2113: {  //BG4HOFS
    io.bg4.hoffset = data << 8 | (latch.ppu1.bgofs & ~7) | (latch.ppu2.bgofs & 7);
    latch.ppu1.bgofs = data;
    latch.ppu2.bgofs = data;
    return;
  }

  case 0x2114: {  //BG4VOFS
    io.bg4.voffset = data << 8 | latch.ppu1.bgofs;
    latch.ppu1.bgofs = data;
    return;
  }

  case 0x2115: {  //VMAIN
    static const uint size[4] = {1, 32, 128, 128};
    io.vramIncrementSize = size[data & 3];
    io.vramMapping       = data >> 2 & 3;
    io.vramIncrementMode = data >> 7 & 1;
    return;
  }

  case 0x2116: {  //VMADDL
    io.vramAddress = io.vramAddress & 0xff00 | data << 0;
    latch.vram = readVRAM();
    return;
  }

  case 0x2117: {  //VMADDH
    io.vramAddress = io.vramAddress & 0x00ff | data << 8;
    latch.vram = readVRAM();
    return;
  }

  case 0x2118: {  //VMDATAL
    writeVRAM<0>(data);
    if(io.vramIncrementMode == 0) io.vramAddress += io.vramIncrementSize;
    return;
  }

  case 0x2119: {  //VMDATAH
    writeVRAM<1>(data);
    if(io.vramIncrementMode == 1) io.vramAddress += io.vramIncrementSize;
    return;
  }

  case 0x211a: {  //M7SEL
    io.mode7.hflip  = data >> 0 & 1;
    io.mode7.vflip  = data >> 1 & 1;
    io.mode7.repeat = data >> 6 & 3;
    return;
  }

  case 0x211b: {  //M7A
    io.mode7.a = data << 8 | latch.mode7;
    latch.mode7 = data;
    return;
  }

  case 0x211c: {  //M7B
    io.mode7.b = data << 8 | latch.mode7;
    latch.mode7 = data;
    return;
  }

  case 0x211d: {  //M7C
    io.mode7.c = data << 8 | latch.mode7;
    latch.mode7 = data;
    return;
  }

  case 0x211e: {  //M7D
    io.mode7.d = data << 8 | latch.mode7;
    latch.mode7 = data;
    return;
  }

  case 0x211f: {  //M7X
    io.mode7.x = data << 8 | latch.mode7;
    latch.mode7 = data;
    return;
  }

  case 0x2120: {  //M7Y
    io.mode7.y = data << 8 | latch.mode7;
    latch.mode7 = data;
    return;
  }

  case 0x2121: {  //CGADD
    io.cgramAddress = data;
    io.cgramAddressLatch = 0;
    return;
  }

  case 0x2122: {  //CGDATA
    if(io.cgramAddressLatch == 0) {
      io.cgramAddressLatch = 1;
      latch.cgram = data;
    } else {
      io.cgramAddressLatch = 0;
      writeCGRAM(io.cgramAddress++, (data & 0x7f) << 8 | latch.cgram);
    }
    return;
  }

  case 0x2123: {  //W12SEL
    io.bg1.window.oneInvert = data >> 0 & 1;
    io.bg1.window.oneEnable = data >> 1 & 1;
    io.bg1.window.twoInvert = data >> 2 & 1;
    io.bg1.window.twoEnable = data >> 3 & 1;
    io.bg2.window.oneInvert = data >> 4 & 1;
    io.bg2.window.oneEnable = data >> 5 & 1;
    io.bg2.window.twoInvert = data >> 6 & 1;
    io.bg2.window.twoEnable = data >> 7 & 1;
    return;
  }

  case 0x2124: {  //W34SEL
    io.bg3.window.oneInvert = data >> 0 & 1;
    io.bg3.window.oneEnable = data >> 1 & 1;
    io.bg3.window.twoInvert = data >> 2 & 1;
    io.bg3.window.twoEnable = data >> 3 & 1;
    io.bg4.window.oneInvert = data >> 4 & 1;
    io.bg4.window.oneEnable = data >> 5 & 1;
    io.bg4.window.twoInvert = data >> 6 & 1;
    io.bg4.window.twoEnable = data >> 7 & 1;
    return;
  }

  case 0x2125: {  //WOBJSEL
    io.obj.window.oneInvert = data >> 0 & 1;
    io.obj.window.oneEnable = data >> 1 & 1;
    io.obj.window.twoInvert = data >> 2 & 1;
    io.obj.window.twoEnable = data >> 3 & 1;
    io.col.window.oneInvert = data >> 4 & 1;
    io.col.window.oneEnable = data >> 5 & 1;
    io.col.window.twoInvert = data >> 6 & 1;
    io.col.window.twoEnable = data >> 7 & 1;
    return;
  }

  case 0x2126: {  //WH0
    io.window.oneLeft = data;
    return;
  }

  case 0x2127: {  //WH1
    io.window.oneRight = data;
    return;
  }

  case 0x2128: {  //WH2
    io.window.twoLeft = data;
    return;
  }

  case 0x2129: {  //WH3
    io.window.twoRight = data;
    return;
  }

  case 0x212a: {  //WBGLOG
    io.bg1.window.mask = data >> 0 & 3;
    io.bg2.window.mask = data >> 2 & 3;
    io.bg3.window.mask = data >> 4 & 3;
    io.bg4.window.mask = data >> 6 & 3;
    return;
  }

  case 0x212b: {  //WOBJLOG
    io.obj.window.mask = data >> 0 & 3;
    io.col.window.mask = data >> 2 & 3;
    return;
  }

  case 0x212c: {  //TM
    io.bg1.aboveEnable = data >> 0 & 1;
    io.bg2.aboveEnable = data >> 1 & 1;
    io.bg3.aboveEnable = data >> 2 & 1;
    io.bg4.aboveEnable = data >> 3 & 1;
    io.obj.aboveEnable = data >> 4 & 1;
    return;
  }

  case 0x212d: {  //TS
    io.bg1.belowEnable = data >> 0 & 1;
    io.bg2.belowEnable = data >> 1 & 1;
    io.bg3.belowEnable = data >> 2 & 1;
    io.bg4.belowEnable = data >> 3 & 1;
    io.obj.belowEnable = data >> 4 & 1;
    return;
  }

  case 0x212e: {  //TMW
    io.bg1.window.aboveEnable = data >> 0 & 1;
    io.bg2.window.aboveEnable = data >> 1 & 1;
    io.bg3.window.aboveEnable = data >> 2 & 1;
    io.bg4.window.aboveEnable = data >> 3 & 1;
    io.obj.window.aboveEnable = data >> 4 & 1;
    return;
  }

  case 0x212f: {  //TSW
    io.bg1.window.belowEnable = data >> 0 & 1;
    io.bg2.window.belowEnable = data >> 1 & 1;
    io.bg3.window.belowEnable = data >> 2 & 1;
    io.bg4.window.belowEnable = data >> 3 & 1;
    io.obj.window.belowEnable = data >> 4 & 1;
    return;
  }

  case 0x2130: {  //CGWSEL
    io.col.directColor      = data >> 0 & 1;
    io.col.blendMode        = data >> 1 & 1;
    io.col.window.belowMask = data >> 4 & 3;
    io.col.window.aboveMask = data >> 6 & 3;
    return;
  }

  case 0x2131: {  //CGADDSUB
    io.col.enable[Source::BG1 ] = data >> 0 & 1;
    io.col.enable[Source::BG2 ] = data >> 1 & 1;
    io.col.enable[Source::BG3 ] = data >> 2 & 1;
    io.col.enable[Source::BG4 ] = data >> 3 & 1;
    io.col.enable[Source::OBJ1] = 0;
    io.col.enable[Source::OBJ2] = data >> 4 & 1;
    io.col.enable[Source::COL ] = data >> 5 & 1;
    io.col.halve                = data >> 6 & 1;
    io.col.mathMode             = data >> 7 & 1;
    return;
  }

  case 0x2132: {  //COLDATA
    if(data & 0x20) io.col.fixedColor = io.col.fixedColor & 0b11111'11111'00000 | (data & 31) <<  0;
    if(data & 0x40) io.col.fixedColor = io.col.fixedColor & 0b11111'00000'11111 | (data & 31) <<  5;
    if(data & 0x80) io.col.fixedColor = io.col.fixedColor & 0b00000'11111'11111 | (data & 31) << 10;
    return;
  }

  case 0x2133: {  //SETINI
    io.interlace     = data >> 0 & 1;
    io.obj.interlace = data >> 1 & 1;
    io.overscan      = data >> 2 & 1;
    io.pseudoHires   = data >> 3 & 1;
    io.extbg         = data >> 6 & 1;
    updateVideoMode();
    return;
  }

  }
}

auto PPU::updateVideoMode() -> void {
  ppubase.display.vdisp = !io.overscan ? 225 : 240;

  switch(io.bgMode) {
  case 0:
    io.bg1.tileMode = TileMode::BPP2;
    io.bg2.tileMode = TileMode::BPP2;
    io.bg3.tileMode = TileMode::BPP2;
    io.bg4.tileMode = TileMode::BPP2;
    memory::assign(io.bg1.priority, 8, 11);
    memory::assign(io.bg2.priority, 7, 10);
    memory::assign(io.bg3.priority, 2,  5);
    memory::assign(io.bg4.priority, 1,  4);
    memory::assign(io.obj.priority, 3,  6, 9, 12);
    break;

  case 1:
    io.bg1.tileMode = TileMode::BPP4;
    io.bg2.tileMode = TileMode::BPP4;
    io.bg3.tileMode = TileMode::BPP2;
    io.bg4.tileMode = TileMode::Inactive;
    if(io.bgPriority) {
      memory::assign(io.bg1.priority, 5,  8);
      memory::assign(io.bg2.priority, 4,  7);
      memory::assign(io.bg3.priority, 1, 10);
      memory::assign(io.obj.priority, 2,  3, 6,  9);
    } else {
      memory::assign(io.bg1.priority, 6,  9);
      memory::assign(io.bg2.priority, 5,  8);
      memory::assign(io.bg3.priority, 1,  3);
      memory::assign(io.obj.priority, 2,  4, 7, 10);
    }
    break;

  case 2:
    io.bg1.tileMode = TileMode::BPP4;
    io.bg2.tileMode = TileMode::BPP4;
    io.bg3.tileMode = TileMode::Inactive;
    io.bg4.tileMode = TileMode::Inactive;
    memory::assign(io.bg1.priority, 3, 7);
    memory::assign(io.bg2.priority, 1, 5);
    memory::assign(io.obj.priority, 2, 4, 6, 8);
    break;

  case 3:
    io.bg1.tileMode = TileMode::BPP8;
    io.bg2.tileMode = TileMode::BPP4;
    io.bg3.tileMode = TileMode::Inactive;
    io.bg4.tileMode = TileMode::Inactive;
    memory::assign(io.bg1.priority, 3, 7);
    memory::assign(io.bg2.priority, 1, 5);
    memory::assign(io.obj.priority, 2, 4, 6, 8);
    break;

  case 4:
    io.bg1.tileMode = TileMode::BPP8;
    io.bg2.tileMode = TileMode::BPP2;
    io.bg3.tileMode = TileMode::Inactive;
    io.bg4.tileMode = TileMode::Inactive;
    memory::assign(io.bg1.priority, 3, 7);
    memory::assign(io.bg2.priority, 1, 5);
    memory::assign(io.obj.priority, 2, 4, 6, 8);
    break;

  case 5:
    io.bg1.tileMode = TileMode::BPP4;
    io.bg2.tileMode = TileMode::BPP2;
    io.bg3.tileMode = TileMode::Inactive;
    io.bg4.tileMode = TileMode::Inactive;
    memory::assign(io.bg1.priority, 3, 7);
    memory::assign(io.bg2.priority, 1, 5);
    memory::assign(io.obj.priority, 2, 4, 6, 8);
    break;

  case 6:
    io.bg1.tileMode = TileMode::BPP4;
    io.bg2.tileMode = TileMode::Inactive;
    io.bg3.tileMode = TileMode::Inactive;
    io.bg4.tileMode = TileMode::Inactive;
    memory::assign(io.bg1.priority, 2, 5);
    memory::assign(io.obj.priority, 1, 3, 4, 6);
    break;

  case 7:
    if(!io.extbg) {
      io.bg1.tileMode = TileMode::Mode7;
      io.bg2.tileMode = TileMode::Inactive;
      io.bg3.tileMode = TileMode::Inactive;
      io.bg4.tileMode = TileMode::Inactive;
      memory::assign(io.bg1.priority, 2);
      memory::assign(io.obj.priority, 1, 3, 4, 5);
    } else {
      io.bg1.tileMode = TileMode::Mode7;
      io.bg2.tileMode = TileMode::Mode7;
      io.bg3.tileMode = TileMode::Inactive;
      io.bg4.tileMode = TileMode::Inactive;
      memory::assign(io.bg1.priority, 3);
      memory::assign(io.bg2.priority, 1, 5);
      memory::assign(io.obj.priority, 2, 4, 6, 7);
    }
    break;
  }
}
