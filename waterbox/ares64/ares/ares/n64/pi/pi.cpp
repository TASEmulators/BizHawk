#include <n64/n64.hpp>

namespace ares::Nintendo64 {

PI pi;
#include "dma.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto PI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("PI");
  rom.allocate(0x7c0);
  ram.allocate(0x040);

  debugger.load(node);
}

auto PI::unload() -> void {
  debugger = {};
  rom.reset();
  ram.reset();
  node.reset();
}

auto PI::power(bool reset) -> void {
  string pifrom = cartridge.region() == "NTSC" ? "pif.ntsc.rom" : "pif.pal.rom";
  if(auto fp = system.pak->read(pifrom)) {
    rom.load(fp);
  }

  ram.fill();
  io = {};
  bsd1 = {};
  bsd2 = {};

  //write CIC seeds into PIF RAM so that cartridge checksum function passes
  string cic = cartridge.cic();
  n8 seed = 0x3f;
  n1 version = 0;
  if(cic == "CIC-NUS-6101" || cic == "CIC-NUS-7102") seed = 0x3f, version = 1;
  if(cic == "CIC-NUS-6102" || cic == "CIC-NUS-7101") seed = 0x3f;
  if(cic == "CIC-NUS-6103" || cic == "CIC-NUS-7103") seed = 0x78;
  if(cic == "CIC-NUS-6105" || cic == "CIC-NUS-7105") seed = 0x91;
  if(cic == "CIC-NUS-6106" || cic == "CIC-NUS-7106") seed = 0x85;

  n32 data;
  data.bit(0, 7) = 0x3f;     //CIC IPL2 seed
  data.bit(8,15) = seed;     //CIC IPL3 seed
  data.bit(17)   = reset;    //osResetType (0 = power; 1 = reset (NMI))
  data.bit(18)   = version;  //osVersion
  data.bit(19)   = 0;        //osRomType (0 = Gamepak; 1 = 64DD)
  ram.write<Word>(0x24, data);
}

}
