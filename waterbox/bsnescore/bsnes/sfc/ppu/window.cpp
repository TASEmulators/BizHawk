auto PPU::Window::scanline() -> void {
  x = 0;
}

auto PPU::Window::run() -> void {
  bool one = (x >= io.oneLeft && x <= io.oneRight);
  bool two = (x >= io.twoLeft && x <= io.twoRight);
  x++;

  if(test(io.bg1.oneEnable, one ^ io.bg1.oneInvert, io.bg1.twoEnable, two ^ io.bg1.twoInvert, io.bg1.mask)) {
    if(io.bg1.aboveEnable) ppu.bg1.output.above.priority = 0;
    if(io.bg1.belowEnable) ppu.bg1.output.below.priority = 0;
  }

  if(test(io.bg2.oneEnable, one ^ io.bg2.oneInvert, io.bg2.twoEnable, two ^ io.bg2.twoInvert, io.bg2.mask)) {
    if(io.bg2.aboveEnable) ppu.bg2.output.above.priority = 0;
    if(io.bg2.belowEnable) ppu.bg2.output.below.priority = 0;
  }

  if(test(io.bg3.oneEnable, one ^ io.bg3.oneInvert, io.bg3.twoEnable, two ^ io.bg3.twoInvert, io.bg3.mask)) {
    if(io.bg3.aboveEnable) ppu.bg3.output.above.priority = 0;
    if(io.bg3.belowEnable) ppu.bg3.output.below.priority = 0;
  }

  if(test(io.bg4.oneEnable, one ^ io.bg4.oneInvert, io.bg4.twoEnable, two ^ io.bg4.twoInvert, io.bg4.mask)) {
    if(io.bg4.aboveEnable) ppu.bg4.output.above.priority = 0;
    if(io.bg4.belowEnable) ppu.bg4.output.below.priority = 0;
  }

  if(test(io.obj.oneEnable, one ^ io.obj.oneInvert, io.obj.twoEnable, two ^ io.obj.twoInvert, io.obj.mask)) {
    if(io.obj.aboveEnable) ppu.obj.output.above.priority = 0;
    if(io.obj.belowEnable) ppu.obj.output.below.priority = 0;
  }

  bool value = test(io.col.oneEnable, one ^ io.col.oneInvert, io.col.twoEnable, two ^ io.col.twoInvert, io.col.mask);
  bool array[] = {true, value, !value, false};
  output.above.colorEnable = array[io.col.aboveMask];
  output.below.colorEnable = array[io.col.belowMask];
}

auto PPU::Window::test(bool oneEnable, bool one, bool twoEnable, bool two, uint mask) -> bool {
  if(!oneEnable) return two && twoEnable;
  if(!twoEnable) return one;
  if(mask == 0) return (one | two);
  if(mask == 1) return (one & two);
                return (one ^ two) == 3 - mask;
}

auto PPU::Window::power() -> void {
  io.bg1.oneEnable = random();
  io.bg1.oneInvert = random();
  io.bg1.twoEnable = random();
  io.bg1.twoInvert = random();
  io.bg1.mask = random();
  io.bg1.aboveEnable = random();
  io.bg1.belowEnable = random();

  io.bg2.oneEnable = random();
  io.bg2.oneInvert = random();
  io.bg2.twoEnable = random();
  io.bg2.twoInvert = random();
  io.bg2.mask = random();
  io.bg2.aboveEnable = random();
  io.bg2.belowEnable = random();

  io.bg3.oneEnable = random();
  io.bg3.oneInvert = random();
  io.bg3.twoEnable = random();
  io.bg3.twoInvert = random();
  io.bg3.mask = random();
  io.bg3.aboveEnable = random();
  io.bg3.belowEnable = random();

  io.bg4.oneEnable = random();
  io.bg4.oneInvert = random();
  io.bg4.twoEnable = random();
  io.bg4.twoInvert = random();
  io.bg4.mask = random();
  io.bg4.aboveEnable = random();
  io.bg4.belowEnable = random();

  io.obj.oneEnable = random();
  io.obj.oneInvert = random();
  io.obj.twoEnable = random();
  io.obj.twoInvert = random();
  io.obj.mask = random();
  io.obj.aboveEnable = random();
  io.obj.belowEnable = random();

  io.col.oneEnable = random();
  io.col.oneInvert = random();
  io.col.twoEnable = random();
  io.col.twoInvert = random();
  io.col.mask = random();
  io.col.aboveMask = random();
  io.col.belowMask = random();

  io.oneLeft = random();
  io.oneRight = random();
  io.twoLeft = random();
  io.twoRight = random();

  output.above.colorEnable = 0;
  output.below.colorEnable = 0;

  x = 0;
}
