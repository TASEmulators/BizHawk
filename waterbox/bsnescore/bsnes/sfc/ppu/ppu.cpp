#include <sfc/sfc.hpp>

namespace SuperFamicom {

PPU ppu;
#include "main.cpp"
#include "io.cpp"
#include "mosaic.cpp"
#include "background.cpp"
#include "object.cpp"
#include "window.cpp"
#include "screen.cpp"
#include "serialization.cpp"
#include "counter/serialization.cpp"

PPU::PPU() :
bg1(Background::ID::BG1),
bg2(Background::ID::BG2),
bg3(Background::ID::BG3),
bg4(Background::ID::BG4) {
  ppu1.version = 1;  //allowed values: 1
  ppu2.version = 3;  //allowed values: 1, 2, 3

  for(uint l = 0; l < 16; l++) {
    for(uint r = 0; r < 32; r++) {
      for(uint g = 0; g < 32; g++) {
        for(uint b = 0; b < 32; b++) {
          double luma = (double)l / 15.0;
          uint ar = (luma * r + 0.5);
          uint ag = (luma * g + 0.5);
          uint ab = (luma * b + 0.5);
          lightTable[l][(r << 10) + (g << 5) + b] = (ab << 10) + (ag << 5) + ar;
        }
      }
    }
  }
}

PPU::~PPU() {
}

auto PPU::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}

auto PPU::step() -> void {
  tick(2);
  clock += 2;
  synchronizeCPU();
}

auto PPU::step(uint clocks) -> void {
  clocks >>= 1;
  while(clocks--) {
    tick(2);
    clock += 2;
    synchronizeCPU();
  }
}

auto PPU::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    ppu.main();
  }
}

auto PPU::load() -> bool {
  ppu1.version = max(1, min(1, configuration.system.ppu1.version));
  ppu2.version = max(1, min(3, configuration.system.ppu2.version));
  vram.mask = configuration.system.ppu1.vram.size / sizeof(uint16) - 1;
  if(vram.mask != 0xffff) vram.mask = 0x7fff;
  return true && ppufast.load();
}

auto PPU::power(bool reset) -> void {
  if(system.fastPPU()) {
    create(PPUfast::Enter, system.cpuFrequency());
    ppufast.power(reset);
    return;
  }

  create(Enter, system.cpuFrequency());
  PPUcounter::reset();
  memory::fill<uint16>(output, 512 * 480);

  function<uint8 (uint, uint8)> reader{&PPU::readIO, this};
  function<void  (uint, uint8)> writer{&PPU::writeIO, this};
  bus.map(reader, writer, "00-3f,80-bf:2100-213f");

  if(!reset) random.array((uint8*)vram.data, sizeof(vram.data));

  ppu1.mdr = random.bias(0xff);
  ppu2.mdr = random.bias(0xff);

  latch.vram = random();
  latch.oam = random();
  latch.cgram = random();
  latch.bgofsPPU1 = random();
  latch.bgofsPPU2 = random();
  latch.mode7 = random();
  latch.counters = false;
  latch.hcounter = 0;
  latch.vcounter = 0;

  latch.oamAddress = 0x0000;
  latch.cgramAddress = 0x00;

  //$2100  INIDISP
  io.displayDisable = true;
  io.displayBrightness = 0;

  //$2102  OAMADDL
  //$2103  OAMADDH
  io.oamBaseAddress = random() & ~1;
  io.oamAddress = random();
  io.oamPriority = random();

  //$2105  BGMODE
  io.bgPriority = false;
  io.bgMode = 0;

  //$210d  BG1HOFS
  io.hoffsetMode7 = random();

  //$210e  BG1VOFS
  io.voffsetMode7 = random();

  //$2115  VMAIN
  io.vramIncrementMode = random.bias(1);
  io.vramMapping = random();
  io.vramIncrementSize = 1;

  //$2116  VMADDL
  //$2117  VMADDH
  io.vramAddress = random();

  //$211a  M7SEL
  io.repeatMode7 = random();
  io.vflipMode7 = random();
  io.hflipMode7 = random();

  //$211b  M7A
  io.m7a = random();

  //$211c  M7B
  io.m7b = random();

  //$211d  M7C
  io.m7c = random();

  //$211e  M7D
  io.m7d = random();

  //$211f  M7X
  io.m7x = random();

  //$2120  M7Y
  io.m7y = random();

  //$2121  CGADD
  io.cgramAddress = random();
  io.cgramAddressLatch = random();

  //$2133  SETINI
  io.extbg = random();
  io.pseudoHires = random();
  io.overscan = false;
  io.interlace = false;

  //$213c  OPHCT
  io.hcounter = 0;

  //$213d  OPVCT
  io.vcounter = 0;

  mosaic.power();
  bg1.power();
  bg2.power();
  bg3.power();
  bg4.power();
  obj.power();
  window.power();
  screen.power();

  updateVideoMode();
}

auto PPU::refresh() -> void {
  if(system.fastPPU()) {
    return ppufast.refresh();
  }

  if(system.runAhead) return;

  auto output = this->output;
  auto pitch  = 512;
  auto width  = 512;
  auto height = 480;
  if(configuration.video.blurEmulation) {
    for(uint y : range(height)) {
      auto data = output + y * pitch;
      for(uint x : range(width - 1)) {
        auto a = data[x + 0];
        auto b = data[x + 1];
        data[x] = (a + b - ((a ^ b) & 0x0421)) >> 1;
      }
    }
  }
  if(auto device = controllerPort2.device) device->draw(output, pitch * sizeof(uint16), width, height);
  platform->videoFrame(output, pitch * sizeof(uint16), width, height, /* scale = */ 1);
}

}
