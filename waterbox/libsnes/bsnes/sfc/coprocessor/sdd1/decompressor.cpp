//S-DD1 decompression algorithm implementation
//original code written by Andreas Naive (public domain license)
//bsnes port written by byuu

//note: decompression module does not need to be serialized with bsnes
//this is because decompression only runs during DMA, and bsnes will complete
//any pending DMA transfers prior to serialization.

//input manager

auto SDD1::Decompressor::IM::init(uint offset_) -> void {
  offset = offset_;
  bitCount = 4;
}

auto SDD1::Decompressor::IM::getCodeWord(uint8 codeLength) -> uint8 {
  uint8 codeWord;
  uint8 compCount;

  codeWord = sdd1.mmcRead(offset) << bitCount;
  bitCount++;

  if(codeWord & 0x80) {
    codeWord |= sdd1.mmcRead(offset + 1) >> (9 - bitCount);
    bitCount += codeLength;
  }

  if(bitCount & 0x08) {
    offset++;
    bitCount &= 0x07;
  }

  return codeWord;
}

//golomb-code decoder

const uint8 SDD1::Decompressor::GCD::runCount[] = {
  0x00, 0x00, 0x01, 0x00, 0x03, 0x01, 0x02, 0x00,
  0x07, 0x03, 0x05, 0x01, 0x06, 0x02, 0x04, 0x00,
  0x0f, 0x07, 0x0b, 0x03, 0x0d, 0x05, 0x09, 0x01,
  0x0e, 0x06, 0x0a, 0x02, 0x0c, 0x04, 0x08, 0x00,
  0x1f, 0x0f, 0x17, 0x07, 0x1b, 0x0b, 0x13, 0x03,
  0x1d, 0x0d, 0x15, 0x05, 0x19, 0x09, 0x11, 0x01,
  0x1e, 0x0e, 0x16, 0x06, 0x1a, 0x0a, 0x12, 0x02,
  0x1c, 0x0c, 0x14, 0x04, 0x18, 0x08, 0x10, 0x00,
  0x3f, 0x1f, 0x2f, 0x0f, 0x37, 0x17, 0x27, 0x07,
  0x3b, 0x1b, 0x2b, 0x0b, 0x33, 0x13, 0x23, 0x03,
  0x3d, 0x1d, 0x2d, 0x0d, 0x35, 0x15, 0x25, 0x05,
  0x39, 0x19, 0x29, 0x09, 0x31, 0x11, 0x21, 0x01,
  0x3e, 0x1e, 0x2e, 0x0e, 0x36, 0x16, 0x26, 0x06,
  0x3a, 0x1a, 0x2a, 0x0a, 0x32, 0x12, 0x22, 0x02,
  0x3c, 0x1c, 0x2c, 0x0c, 0x34, 0x14, 0x24, 0x04,
  0x38, 0x18, 0x28, 0x08, 0x30, 0x10, 0x20, 0x00,
  0x7f, 0x3f, 0x5f, 0x1f, 0x6f, 0x2f, 0x4f, 0x0f,
  0x77, 0x37, 0x57, 0x17, 0x67, 0x27, 0x47, 0x07,
  0x7b, 0x3b, 0x5b, 0x1b, 0x6b, 0x2b, 0x4b, 0x0b,
  0x73, 0x33, 0x53, 0x13, 0x63, 0x23, 0x43, 0x03,
  0x7d, 0x3d, 0x5d, 0x1d, 0x6d, 0x2d, 0x4d, 0x0d,
  0x75, 0x35, 0x55, 0x15, 0x65, 0x25, 0x45, 0x05,
  0x79, 0x39, 0x59, 0x19, 0x69, 0x29, 0x49, 0x09,
  0x71, 0x31, 0x51, 0x11, 0x61, 0x21, 0x41, 0x01,
  0x7e, 0x3e, 0x5e, 0x1e, 0x6e, 0x2e, 0x4e, 0x0e,
  0x76, 0x36, 0x56, 0x16, 0x66, 0x26, 0x46, 0x06,
  0x7a, 0x3a, 0x5a, 0x1a, 0x6a, 0x2a, 0x4a, 0x0a,
  0x72, 0x32, 0x52, 0x12, 0x62, 0x22, 0x42, 0x02,
  0x7c, 0x3c, 0x5c, 0x1c, 0x6c, 0x2c, 0x4c, 0x0c,
  0x74, 0x34, 0x54, 0x14, 0x64, 0x24, 0x44, 0x04,
  0x78, 0x38, 0x58, 0x18, 0x68, 0x28, 0x48, 0x08,
  0x70, 0x30, 0x50, 0x10, 0x60, 0x20, 0x40, 0x00,
};

auto SDD1::Decompressor::GCD::getRunCount(uint8 codeNumber, uint8& mpsCount, bool& lpsIndex) -> void {
  uint8 codeWord = self.im.getCodeWord(codeNumber);

  if(codeWord & 0x80) {
    lpsIndex = 1;
    mpsCount = runCount[codeWord >> (codeNumber ^ 0x07)];
  } else {
    mpsCount = 1 << codeNumber;
  }
}

//bits generator

auto SDD1::Decompressor::BG::init() -> void {
  mpsCount = 0;
  lpsIndex = 0;
}

auto SDD1::Decompressor::BG::getBit(bool& endOfRun) -> uint8 {
  if(!(mpsCount || lpsIndex)) self.gcd.getRunCount(codeNumber, mpsCount, lpsIndex);

  uint8 bit;
  if(mpsCount) {
    bit = 0;
    mpsCount--;
  } else {
    bit = 1;
    lpsIndex = 0;
  }

  endOfRun = !(mpsCount || lpsIndex);
  return bit;
}

//probability estimation module

const SDD1::Decompressor::PEM::State SDD1::Decompressor::PEM::evolutionTable[33] = {
  {0, 25, 25},
  {0,  2,  1},
  {0,  3,  1},
  {0,  4,  2},
  {0,  5,  3},
  {1,  6,  4},
  {1,  7,  5},
  {1,  8,  6},
  {1,  9,  7},
  {2, 10,  8},
  {2, 11,  9},
  {2, 12, 10},
  {2, 13, 11},
  {3, 14, 12},
  {3, 15, 13},
  {3, 16, 14},
  {3, 17, 15},
  {4, 18, 16},
  {4, 19, 17},
  {5, 20, 18},
  {5, 21, 19},
  {6, 22, 20},
  {6, 23, 21},
  {7, 24, 22},
  {7, 24, 23},
  {0, 26,  1},
  {1, 27,  2},
  {2, 28,  4},
  {3, 29,  8},
  {4, 30, 12},
  {5, 31, 16},
  {6, 32, 18},
  {7, 24, 22},
};

auto SDD1::Decompressor::PEM::init() -> void {
  for(auto n : range(32)) {
    contextInfo[n].status = 0;
    contextInfo[n].mps = 0;
  }
}

auto SDD1::Decompressor::PEM::getBit(uint8 context) -> uint8 {
  ContextInfo& info = contextInfo[context];
  uint8 currentStatus = info.status;
  uint8 currentMps = info.mps;
  const State& s = SDD1::Decompressor::PEM::evolutionTable[currentStatus];

  uint8 bit;
  bool endOfRun;
  switch(s.codeNumber) {
  case 0: bit = self.bg0.getBit(endOfRun); break;
  case 1: bit = self.bg1.getBit(endOfRun); break;
  case 2: bit = self.bg2.getBit(endOfRun); break;
  case 3: bit = self.bg3.getBit(endOfRun); break;
  case 4: bit = self.bg4.getBit(endOfRun); break;
  case 5: bit = self.bg5.getBit(endOfRun); break;
  case 6: bit = self.bg6.getBit(endOfRun); break;
  case 7: bit = self.bg7.getBit(endOfRun); break;
  }

  if(endOfRun) {
    if(bit) {
      if(!(currentStatus & 0xfe)) info.mps ^= 0x01;
      info.status = s.nextIfLps;
    } else {
      info.status = s.nextIfMps;
    }
  }

  return bit ^ currentMps;
}

//context model

auto SDD1::Decompressor::CM::init(uint offset) -> void {
  bitplanesInfo = sdd1.mmcRead(offset) & 0xc0;
  contextBitsInfo = sdd1.mmcRead(offset) & 0x30;
  bitNumber = 0;
  for(auto n : range(8)) previousBitplaneBits[n] = 0;
  switch(bitplanesInfo) {
  case 0x00: currentBitplane = 1; break;
  case 0x40: currentBitplane = 7; break;
  case 0x80: currentBitplane = 3; break;
  }
}

auto SDD1::Decompressor::CM::getBit() -> uint8 {
  switch(bitplanesInfo) {
  case 0x00:
    currentBitplane ^= 0x01;
    break;
  case 0x40:
    currentBitplane ^= 0x01;
    if(!(bitNumber & 0x7f)) currentBitplane = ((currentBitplane + 2) & 0x07);
    break;
  case 0x80:
    currentBitplane ^= 0x01;
    if(!(bitNumber & 0x7f)) currentBitplane ^= 0x02;
    break;
  case 0xc0:
    currentBitplane = bitNumber & 0x07;
    break;
  }

  uint16& contextBits = previousBitplaneBits[currentBitplane];
  uint8 currentContext = (currentBitplane & 0x01) << 4;
  switch(contextBitsInfo) {
  case 0x00: currentContext |= ((contextBits & 0x01c0) >> 5) | (contextBits & 0x0001); break;
  case 0x10: currentContext |= ((contextBits & 0x0180) >> 5) | (contextBits & 0x0001); break;
  case 0x20: currentContext |= ((contextBits & 0x00c0) >> 5) | (contextBits & 0x0001); break;
  case 0x30: currentContext |= ((contextBits & 0x0180) >> 5) | (contextBits & 0x0003); break;
  }

  uint8 bit = self.pem.getBit(currentContext);
  contextBits <<= 1;
  contextBits |= bit;
  bitNumber++;
  return bit;
}

//output logic

auto SDD1::Decompressor::OL::init(uint offset) -> void {
  bitplanesInfo = sdd1.mmcRead(offset) & 0xc0;
  r0 = 0x01;
}

auto SDD1::Decompressor::OL::decompress() -> uint8 {
  switch(bitplanesInfo) {
  case 0x00: case 0x40: case 0x80:
    if(r0 == 0) {
      r0 = ~r0;
      return r2;
    }
    for(r0 = 0x80, r1 = 0, r2 = 0; r0; r0 >>= 1) {
      if(self.cm.getBit()) r1 |= r0;
      if(self.cm.getBit()) r2 |= r0;
    }
    return r1;
  case 0xc0:
    for(r0 = 0x01, r1 = 0; r0; r0 <<= 1) {
      if(self.cm.getBit()) r1 |= r0;
    }
    return r1;
  }

  return 0;  //unreachable?
}

//core

SDD1::Decompressor::Decompressor():
im(*this), gcd(*this),
bg0(*this, 0), bg1(*this, 1), bg2(*this, 2), bg3(*this, 3),
bg4(*this, 4), bg5(*this, 5), bg6(*this, 6), bg7(*this, 7),
pem(*this), cm(*this), ol(*this) {
}

auto SDD1::Decompressor::init(uint offset) -> void {
  im.init(offset);
  bg0.init();
  bg1.init();
  bg2.init();
  bg3.init();
  bg4.init();
  bg5.init();
  bg6.init();
  bg7.init();
  pem.init();
  cm.init(offset);
  ol.init(offset);
}

auto SDD1::Decompressor::read() -> uint8 {
  return ol.decompress();
}
