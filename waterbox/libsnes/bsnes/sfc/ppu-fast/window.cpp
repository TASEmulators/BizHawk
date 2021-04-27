auto PPU::Line::renderWindow(PPU::IO::WindowLayer& self, bool enable, bool output[256]) -> void {
  if(!enable || (!self.oneEnable && !self.twoEnable)) {
    memory::fill<bool>(output, 256, 0);
    return;
  }

  if(self.oneEnable && !self.twoEnable) {
    bool set = 1 ^ self.oneInvert, clear = !set;
    for(uint x : range(256)) {
      output[x] = x >= io.window.oneLeft && x <= io.window.oneRight ? set : clear;
    }
    return;
  }

  if(self.twoEnable && !self.oneEnable) {
    bool set = 1 ^ self.twoInvert, clear = !set;
    for(uint x : range(256)) {
      output[x] = x >= io.window.twoLeft && x <= io.window.twoRight ? set : clear;
    }
    return;
  }

  for(uint x : range(256)) {
    bool oneMask = (x >= io.window.oneLeft && x <= io.window.oneRight) ^ self.oneInvert;
    bool twoMask = (x >= io.window.twoLeft && x <= io.window.twoRight) ^ self.twoInvert;
    switch(self.mask) {
    case 0: output[x] = (oneMask | twoMask) == 1; break;
    case 1: output[x] = (oneMask & twoMask) == 1; break;
    case 2: output[x] = (oneMask ^ twoMask) == 1; break;
    case 3: output[x] = (oneMask ^ twoMask) == 0; break;
    }
  }
}

auto PPU::Line::renderWindow(PPU::IO::WindowColor& self, uint mask, bool output[256]) -> void {
  bool set, clear;
  switch(mask) {
  case 0: memory::fill<bool>(output, 256, 1); return;  //always
  case 1: set = 1, clear = 0; break;  //inside
  case 2: set = 0, clear = 1; break;  //outside
  case 3: memory::fill<bool>(output, 256, 0); return;  //never
  }

  if(!self.oneEnable && !self.twoEnable) {
    memory::fill<bool>(output, 256, clear);
    return;
  }

  if(self.oneEnable && !self.twoEnable) {
    if(self.oneInvert) set ^= 1, clear ^= 1;
    for(uint x : range(256)) {
      output[x] = x >= io.window.oneLeft && x <= io.window.oneRight ? set : clear;
    }
    return;
  }

  if(self.twoEnable && !self.oneEnable) {
    if(self.twoInvert) set ^= 1, clear ^= 1;
    for(uint x : range(256)) {
      output[x] = x >= io.window.twoLeft && x <= io.window.twoRight ? set : clear;
    }
    return;
  }

  for(uint x : range(256)) {
    bool oneMask = (x >= io.window.oneLeft && x <= io.window.oneRight) ^ self.oneInvert;
    bool twoMask = (x >= io.window.twoLeft && x <= io.window.twoRight) ^ self.twoInvert;
    switch(self.mask) {
    case 0: output[x] = (oneMask | twoMask) == 1 ? set : clear; break;
    case 1: output[x] = (oneMask & twoMask) == 1 ? set : clear; break;
    case 2: output[x] = (oneMask ^ twoMask) == 1 ? set : clear; break;
    case 3: output[x] = (oneMask ^ twoMask) == 0 ? set : clear; break;
    }
  }
}
