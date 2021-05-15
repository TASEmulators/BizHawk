#include <sfc/sfc.hpp>

namespace SuperFamicom {

SDD1 sdd1;

#include "decompressor.cpp"
#include "serialization.cpp"

auto SDD1::unload() -> void {
  rom.reset();
}

auto SDD1::power() -> void {
  //hook S-CPU DMA MMIO registers to gather information for struct dma[];
  //buffer address and transfer size information for use in SDD1::mcu_read()
  bus.map({&SDD1::dmaRead, &sdd1}, {&SDD1::dmaWrite, &sdd1}, "00-3f,80-bf:4300-437f");

  r4800 = 0x00;
  r4801 = 0x00;
  r4804 = 0x00;
  r4805 = 0x01;
  r4806 = 0x02;
  r4807 = 0x03;

  for(auto n : range(8)) {
    dma[n].addr = 0;
    dma[n].size = 0;
  }
  dmaReady = false;
}

auto SDD1::ioRead(uint addr, uint8 data) -> uint8 {
  addr = 0x4800 | addr & 0xf;

  switch(addr) {
  case 0x4800: return r4800;
  case 0x4801: return r4801;
  case 0x4804: return r4804;
  case 0x4805: return r4805;
  case 0x4806: return r4806;
  case 0x4807: return r4807;
  }

  //00-3f,80-bf:4802-4803,4808-480f falls through to ROM
  return rom.read(addr);
}

auto SDD1::ioWrite(uint addr, uint8 data) -> void {
  addr = 0x4800 | addr & 0xf;

  switch(addr) {
  case 0x4800: r4800 = data; break;
  case 0x4801: r4801 = data; break;
  case 0x4804: r4804 = data & 0x8f; break;
  case 0x4805: r4805 = data & 0x8f; break;
  case 0x4806: r4806 = data & 0x8f; break;
  case 0x4807: r4807 = data & 0x8f; break;
  }
}

auto SDD1::dmaRead(uint addr, uint8 data) -> uint8 {
  return cpu.readDMA(addr, data);
}

auto SDD1::dmaWrite(uint addr, uint8 data) -> void {
  uint channel = addr >> 4 & 7;
  switch(addr & 15) {
  case 2: dma[channel].addr = dma[channel].addr & 0xffff00 | data <<  0; break;
  case 3: dma[channel].addr = dma[channel].addr & 0xff00ff | data <<  8; break;
  case 4: dma[channel].addr = dma[channel].addr & 0x00ffff | data << 16; break;
  case 5: dma[channel].size = dma[channel].size & 0xff00 | data << 0; break;
  case 6: dma[channel].size = dma[channel].size & 0x00ff | data << 8; break;
  }
  return cpu.writeDMA(addr, data);
}

auto SDD1::mmcRead(uint addr) -> uint8 {
  switch(addr >> 20 & 3) {
  case 0: return rom.read((r4804 & 0xf) << 20 | addr & 0xfffff);  //c0-cf:0000-ffff
  case 1: return rom.read((r4805 & 0xf) << 20 | addr & 0xfffff);  //d0-df:0000-ffff
  case 2: return rom.read((r4806 & 0xf) << 20 | addr & 0xfffff);  //e0-ef:0000-ffff
  case 3: return rom.read((r4807 & 0xf) << 20 | addr & 0xfffff);  //f0-ff:0000-ffff
  }
  unreachable;
}

//map address=00-3f,80-bf:8000-ffff
//map address=c0-ff:0000-ffff
auto SDD1::mcuRead(uint addr, uint8 data) -> uint8 {
  //map address=00-3f,80-bf:8000-ffff
  if(!(addr & 1 << 22)) {
    if(!(addr & 1 << 23) && (addr & 1 << 21) && (r4805 & 0x80)) addr &= ~(1 << 21);  //20-3f:8000-ffff
    if( (addr & 1 << 23) && (addr & 1 << 21) && (r4807 & 0x80)) addr &= ~(1 << 21);  //a0-bf:8000-ffff
    addr = addr >> 1 & 0x1f8000 | addr & 0x7fff;
    return rom.read(addr);
  }

  //map address=c0-ff:0000-ffff
  if(r4800 & r4801) {
    //at least one channel has S-DD1 decompression enabled ...
    for(auto n : range(8)) {
      if((r4800 & 1 << n) && (r4801 & 1 << n)) {
        //S-DD1 always uses fixed transfer mode, so address will not change during transfer
        if(addr == dma[n].addr) {
          if(!dmaReady) {
            //prepare streaming decompression
            decompressor.init(addr);
            dmaReady = true;
          }

          //fetch a decompressed byte; once finished, disable channel and invalidate buffer
          data = decompressor.read();
          if(--dma[n].size == 0) {
            dmaReady = false;
            r4801 &= ~(1 << n);
          }

          return data;
        }  //address matched
      }  //channel enabled
    }  //channel loop
  }  //S-DD1 decompressor enabled

  //S-DD1 decompression mode inactive; return ROM data
  return mmcRead(addr);
}

auto SDD1::mcuWrite(uint addr, uint8 data) -> void {
}

}
