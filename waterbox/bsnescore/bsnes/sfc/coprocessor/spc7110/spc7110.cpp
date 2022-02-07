#include <sfc/sfc.hpp>

namespace SuperFamicom {

#include "dcu.cpp"
#include "data.cpp"
#include "alu.cpp"
#include "serialization.cpp"
SPC7110 spc7110;

SPC7110::SPC7110() {
  decompressor = new Decompressor(*this);
}

SPC7110::~SPC7110() {
  delete decompressor;
}

auto SPC7110::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}

auto SPC7110::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    spc7110.main();
  }
}

auto SPC7110::main() -> void {
  if(dcuPending) { dcuPending = 0; dcuBeginTransfer(); }
  if(mulPending) { mulPending = 0; aluMultiply(); }
  if(divPending) { divPending = 0; aluDivide(); }
  addClocks(1);
}

auto SPC7110::step(uint clocks) -> void {
  clock += clocks * (uint64_t)cpu.frequency;
}

auto SPC7110::addClocks(uint clocks) -> void {
  step(clocks);
  synchronizeCPU();
}

auto SPC7110::unload() -> void {
  prom.reset();
  drom.reset();
  ram.reset();
}

auto SPC7110::power() -> void {
  create(SPC7110::Enter, 21'477'272);

  r4801 = 0x00;
  r4802 = 0x00;
  r4803 = 0x00;
  r4804 = 0x00;
  r4805 = 0x00;
  r4806 = 0x00;
  r4807 = 0x00;
  r4809 = 0x00;
  r480a = 0x00;
  r480b = 0x00;
  r480c = 0x00;

  dcuPending = 0;
  dcuMode = 0;
  dcuAddress = 0;

  r4810 = 0x00;
  r4811 = 0x00;
  r4812 = 0x00;
  r4813 = 0x00;
  r4814 = 0x00;
  r4815 = 0x00;
  r4816 = 0x00;
  r4817 = 0x00;
  r4818 = 0x00;
  r481a = 0x00;

  r4820 = 0x00;
  r4821 = 0x00;
  r4822 = 0x00;
  r4823 = 0x00;
  r4824 = 0x00;
  r4825 = 0x00;
  r4826 = 0x00;
  r4827 = 0x00;
  r4828 = 0x00;
  r4829 = 0x00;
  r482a = 0x00;
  r482b = 0x00;
  r482c = 0x00;
  r482d = 0x00;
  r482e = 0x00;
  r482f = 0x00;

  mulPending = 0;
  divPending = 0;

  r4830 = 0x00;
  r4831 = 0x00;
  r4832 = 0x01;
  r4833 = 0x02;
  r4834 = 0x00;
}

auto SPC7110::read(uint addr, uint8 data) -> uint8 {
  cpu.synchronizeCoprocessors();
  if((addr & 0xff0000) == 0x500000) addr = 0x4800;  //$50:0000-ffff == $4800
  if((addr & 0xff0000) == 0x580000) addr = 0x4808;  //$58:0000-ffff == $4808
  addr = 0x4800 | (addr & 0x3f);  //$00-3f,80-bf:4800-483f

  switch(addr) {
  //==================
  //decompression unit
  //==================
  case 0x4800: {
    uint16 counter = r4809 | r480a << 8;
    counter--;
    r4809 = counter >> 0;
    r480a = counter >> 8;
    return dcuRead();
  }
  case 0x4801: return r4801;
  case 0x4802: return r4802;
  case 0x4803: return r4803;
  case 0x4804: return r4804;
  case 0x4805: return r4805;
  case 0x4806: return r4806;
  case 0x4807: return r4807;
  case 0x4808: return 0x00;
  case 0x4809: return r4809;
  case 0x480a: return r480a;
  case 0x480b: return r480b;
  case 0x480c: return r480c;

  //==============
  //data port unit
  //==============
  case 0x4810: {
    data = r4810;
    dataPortIncrement4810();
    return data;
  }
  case 0x4811: return r4811;
  case 0x4812: return r4812;
  case 0x4813: return r4813;
  case 0x4814: return r4814;
  case 0x4815: return r4815;
  case 0x4816: return r4816;
  case 0x4817: return r4817;
  case 0x4818: return r4818;
  case 0x481a: {
    dataPortIncrement481a();
    return 0x00;
  }

  //=====================
  //arithmetic logic unit
  //=====================
  case 0x4820: return r4820;
  case 0x4821: return r4821;
  case 0x4822: return r4822;
  case 0x4823: return r4823;
  case 0x4824: return r4824;
  case 0x4825: return r4825;
  case 0x4826: return r4826;
  case 0x4827: return r4827;
  case 0x4828: return r4828;
  case 0x4829: return r4829;
  case 0x482a: return r482a;
  case 0x482b: return r482b;
  case 0x482c: return r482c;
  case 0x482d: return r482d;
  case 0x482e: return r482e;
  case 0x482f: return r482f;

  //===================
  //memory control unit
  //===================
  case 0x4830: return r4830;
  case 0x4831: return r4831;
  case 0x4832: return r4832;
  case 0x4833: return r4833;
  case 0x4834: return r4834;
  }

  return data;
}

auto SPC7110::write(uint addr, uint8 data) -> void {
  cpu.synchronizeCoprocessors();
  if((addr & 0xff0000) == 0x500000) addr = 0x4800;  //$50:0000-ffff == $4800
  if((addr & 0xff0000) == 0x580000) addr = 0x4808;  //$58:0000-ffff == $4808
  addr = 0x4800 | (addr & 0x3f);  //$00-3f,80-bf:4800-483f

  switch(addr) {
  //==================
  //decompression unit
  //==================
  case 0x4801: r4801 = data; break;
  case 0x4802: r4802 = data; break;
  case 0x4803: r4803 = data; break;
  case 0x4804: r4804 = data; dcuLoadAddress(); break;
  case 0x4805: r4805 = data; break;
  case 0x4806: r4806 = data; r480c &= 0x7f; dcuPending = 1; break;
  case 0x4807: r4807 = data; break;
  case 0x4808: break;
  case 0x4809: r4809 = data; break;
  case 0x480a: r480a = data; break;
  case 0x480b: r480b = data & 0x03; break;

  //==============
  //data port unit
  //==============
  case 0x4811: r4811 = data; break;
  case 0x4812: r4812 = data; break;
  case 0x4813: r4813 = data; dataPortRead(); break;
  case 0x4814: r4814 = data; dataPortIncrement4814(); break;
  case 0x4815: r4815 = data; if(r4818 & 2) dataPortRead(); dataPortIncrement4815(); break;
  case 0x4816: r4816 = data; break;
  case 0x4817: r4817 = data; break;
  case 0x4818: r4818 = data & 0x7f; dataPortRead(); break;

  //=====================
  //arithmetic logic unit
  //=====================
  case 0x4820: r4820 = data; break;
  case 0x4821: r4821 = data; break;
  case 0x4822: r4822 = data; break;
  case 0x4823: r4823 = data; break;
  case 0x4824: r4824 = data; break;
  case 0x4825: r4825 = data; r482f |= 0x81; mulPending = 1; break;
  case 0x4826: r4826 = data; break;
  case 0x4827: r4827 = data; r482f |= 0x80; divPending = 1; break;
  case 0x482e: r482e = data & 0x01; break;

  //===================
  //memory control unit
  //===================
  case 0x4830: r4830 = data & 0x87; break;
  case 0x4831: r4831 = data & 0x07; break;
  case 0x4832: r4832 = data & 0x07; break;
  case 0x4833: r4833 = data & 0x07; break;
  case 0x4834: r4834 = data & 0x07; break;
  }
}

//===============
//SPC7110::MCUROM
//===============

//map address=00-3f,80-bf:8000-ffff mask=0x800000 => 00-3f:8000-ffff
//map address=c0-ff:0000-ffff mask=0xc00000 => c0-ff:0000-ffff
auto SPC7110::mcuromRead(uint addr, uint8 data) -> uint8 {
  uint mask = (1 << (r4834 & 3)) - 1;  //8mbit, 16mbit, 32mbit, 64mbit DROM

  if(addr < 0x100000) {  //$00-0f,80-8f:8000-ffff; $c0-cf:0000-ffff
    addr &= 0x0fffff;
    if(prom.size()) {  //8mbit PROM
      return prom.read(bus.mirror(0x000000 + addr, prom.size()));
    }
    addr |= 0x100000 * (r4830 & 7);
    return dataromRead(addr);
  }

  if(addr < 0x200000) {  //$10-1f,90-9f:8000-ffff; $d0-df:0000-ffff
    addr &= 0x0fffff;
    if(r4834 & 4) {  //16mbit PROM
      return prom.read(bus.mirror(0x100000 + addr, prom.size()));
    }
    addr |= 0x100000 * (r4831 & 7);
    return dataromRead(addr);
  }

  if(addr < 0x300000) {  //$20-2f,a0-af:8000-ffff; $e0-ef:0000-ffff
    addr &= 0x0fffff;
    addr |= 0x100000 * (r4832 & 7);
    return dataromRead(addr);
  }

  if(addr < 0x400000) {  //$30-3f,b0-bf:8000-ffff; $f0-ff:0000-ffff
    addr &= 0x0fffff;
    addr |= 0x100000 * (r4833 & 7);
    return dataromRead(addr);
  }

  return data;
}

auto SPC7110::mcuromWrite(uint addr, uint8 data) -> void {
}

//===============
//SPC7110::MCURAM
//===============

//map address=00-3f,80-bf:6000-7fff mask=0x80e000 => 00-07:0000-ffff
auto SPC7110::mcuramRead(uint addr, uint8) -> uint8 {
  if(r4830 & 0x80) {
    addr = bus.mirror(addr, ram.size());
    return ram.read(addr);
  }
  return 0x00;
}

auto SPC7110::mcuramWrite(uint addr, uint8 data) -> void {
  if(r4830 & 0x80) {
    addr = bus.mirror(addr, ram.size());
    ram.write(addr, data);
  }
}

}
