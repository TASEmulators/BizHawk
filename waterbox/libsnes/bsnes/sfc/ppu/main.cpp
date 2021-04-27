auto PPU::main() -> void {
  if(vcounter() == 0) {
    if(display.overscan && !io.overscan) {
      //when disabling overscan, clear the overscan area that won't be rendered to:
      for(uint y = 1; y <= 240; y++) {
        if(y >= 8 && y <= 231) continue;
        auto output = ppu.output + y * 1024;
        memory::fill<uint16>(output, 1024);
      }
    }
    display.interlace = io.interlace;
    display.overscan = io.overscan;
    bg1.frame();
    bg2.frame();
    bg3.frame();
    bg4.frame();
    obj.frame();
  }

  mosaic.scanline();
  bg1.scanline();
  bg2.scanline();
  bg3.scanline();
  bg4.scanline();
  obj.scanline();
  window.scanline();
  screen.scanline();

  if(vcounter() > 240) {
    step(hperiod());
    return;
  }

  #define cycles02(index) cycle<index>()
  #define cycles04(index) cycles02(index); cycles02(index +  2)
  #define cycles08(index) cycles04(index); cycles04(index +  4)
  #define cycles16(index) cycles08(index); cycles08(index +  8)
  #define cycles32(index) cycles16(index); cycles16(index + 16)
  #define cycles64(index) cycles32(index); cycles32(index + 32)
  cycles16(   0);
  cycles04(  16);
  //H =   20
  cycles04(  20);
  cycles04(  24);
  //H =   28
  cycles04(  28);
  cycles32(  32);
  cycles64(  64);
  cycles64( 128);
  cycles64( 192);
  cycles64( 256);
  cycles64( 320);
  cycles64( 384);
  cycles64( 448);
  cycles64( 512);
  cycles64( 576);
  cycles64( 640);
  cycles64( 704);
  cycles64( 768);
  cycles64( 832);
  cycles64( 896);
  cycles64( 960);
  cycles32(1024);
  cycles16(1056);
  cycles08(1072);
  //H = 1080
  obj.fetch();
  //H = 1352 (max)
  step(hperiod() - hcounter());
}

//it would be lovely if we could put these functions inside cycle(),
//but due to the multiple template instantiations, that destroys L1 cache.
//it's a performance penalty of about 25% for the entire(!!) emulator.

auto PPU::cycleObjectEvaluate() -> void {
  obj.evaluate(hcounter() >> 3);
}

template<uint Cycle>
auto PPU::cycleBackgroundFetch() -> void {
  switch(io.bgMode) {
  case 0:
    if constexpr(Cycle == 0) bg4.fetchNameTable();
    if constexpr(Cycle == 1) bg3.fetchNameTable();
    if constexpr(Cycle == 2) bg2.fetchNameTable();
    if constexpr(Cycle == 3) bg1.fetchNameTable();
    if constexpr(Cycle == 4) bg4.fetchCharacter(0);
    if constexpr(Cycle == 5) bg3.fetchCharacter(0);
    if constexpr(Cycle == 6) bg2.fetchCharacter(0);
    if constexpr(Cycle == 7) bg1.fetchCharacter(0);
    break;
  case 1:
    if constexpr(Cycle == 0) bg3.fetchNameTable();
    if constexpr(Cycle == 1) bg2.fetchNameTable();
    if constexpr(Cycle == 2) bg1.fetchNameTable();
    if constexpr(Cycle == 3) bg3.fetchCharacter(0);
    if constexpr(Cycle == 4) bg2.fetchCharacter(0);
    if constexpr(Cycle == 5) bg2.fetchCharacter(1);
    if constexpr(Cycle == 6) bg1.fetchCharacter(0);
    if constexpr(Cycle == 7) bg1.fetchCharacter(1);
    break;
  case 2:
    if constexpr(Cycle == 0) bg2.fetchNameTable();
    if constexpr(Cycle == 1) bg1.fetchNameTable();
    if constexpr(Cycle == 2) bg3.fetchOffset(0);
    if constexpr(Cycle == 3) bg3.fetchOffset(8);
    if constexpr(Cycle == 4) bg2.fetchCharacter(0);
    if constexpr(Cycle == 5) bg2.fetchCharacter(1);
    if constexpr(Cycle == 6) bg1.fetchCharacter(0);
    if constexpr(Cycle == 7) bg1.fetchCharacter(1);
    break;
  case 3:
    if constexpr(Cycle == 0) bg2.fetchNameTable();
    if constexpr(Cycle == 1) bg1.fetchNameTable();
    if constexpr(Cycle == 2) bg2.fetchCharacter(0);
    if constexpr(Cycle == 3) bg2.fetchCharacter(1);
    if constexpr(Cycle == 4) bg1.fetchCharacter(0);
    if constexpr(Cycle == 5) bg1.fetchCharacter(1);
    if constexpr(Cycle == 6) bg1.fetchCharacter(2);
    if constexpr(Cycle == 7) bg1.fetchCharacter(3);
    break;
  case 4:
    if constexpr(Cycle == 0) bg2.fetchNameTable();
    if constexpr(Cycle == 1) bg1.fetchNameTable();
    if constexpr(Cycle == 2) bg3.fetchOffset(0);
    if constexpr(Cycle == 3) bg2.fetchCharacter(0);
    if constexpr(Cycle == 4) bg1.fetchCharacter(0);
    if constexpr(Cycle == 5) bg1.fetchCharacter(1);
    if constexpr(Cycle == 6) bg1.fetchCharacter(2);
    if constexpr(Cycle == 7) bg1.fetchCharacter(3);
    break;
  case 5:
    if constexpr(Cycle == 0) bg2.fetchNameTable();
    if constexpr(Cycle == 1) bg1.fetchNameTable();
    if constexpr(Cycle == 2) bg2.fetchCharacter(0, 0);
    if constexpr(Cycle == 3) bg2.fetchCharacter(0, 1);
    if constexpr(Cycle == 4) bg1.fetchCharacter(0, 0);
    if constexpr(Cycle == 5) bg1.fetchCharacter(1, 0);
    if constexpr(Cycle == 6) bg1.fetchCharacter(0, 1);
    if constexpr(Cycle == 7) bg1.fetchCharacter(1, 1);
    break;
  case 6:
    if constexpr(Cycle == 0) bg2.fetchNameTable();
    if constexpr(Cycle == 1) bg1.fetchNameTable();
    if constexpr(Cycle == 2) bg3.fetchOffset(0);
    if constexpr(Cycle == 3) bg3.fetchOffset(8);
    if constexpr(Cycle == 4) bg1.fetchCharacter(0, 0);
    if constexpr(Cycle == 5) bg1.fetchCharacter(1, 0);
    if constexpr(Cycle == 6) bg1.fetchCharacter(0, 1);
    if constexpr(Cycle == 7) bg1.fetchCharacter(1, 1);
    break;
  case 7:
    //handled separately by mode7.cpp
    break;
  }
}

auto PPU::cycleBackgroundBegin() -> void {
  bg1.begin();
  bg2.begin();
  bg3.begin();
  bg4.begin();
}

auto PPU::cycleBackgroundBelow() -> void {
  bg1.run(1);
  bg2.run(1);
  bg3.run(1);
  bg4.run(1);
}

auto PPU::cycleBackgroundAbove() -> void {
  bg1.run(0);
  bg2.run(0);
  bg3.run(0);
  bg4.run(0);
}

auto PPU::cycleRenderPixel() -> void {
  obj.run();
  window.run();
  screen.run();
}

template<uint Cycle>
auto PPU::cycle() -> void {
  if constexpr(Cycle >=  0 && Cycle <= 1016 && (Cycle -  0) % 8 == 0) cycleObjectEvaluate();
  if constexpr(Cycle >=  0 && Cycle <= 1054 && (Cycle -  0) % 4 == 0) cycleBackgroundFetch<(Cycle - 0) / 4 & 7>();
  if constexpr(Cycle == 56                                          ) cycleBackgroundBegin();
  if constexpr(Cycle >= 56 && Cycle <= 1078 && (Cycle - 56) % 4 == 0) cycleBackgroundBelow();
  if constexpr(Cycle >= 56 && Cycle <= 1078 && (Cycle - 56) % 4 == 2) cycleBackgroundAbove();
  if constexpr(Cycle >= 56 && Cycle <= 1078 && (Cycle - 56) % 4 == 2) cycleRenderPixel();
  step();
}
