#include <n64/n64.hpp>

namespace ares::Nintendo64 {

CIC cic;
#include "io.cpp"
#include "commands.cpp"
#include "serialization.cpp"

auto CIC::power(bool reset) -> void {
  model = cartridge.node ? cartridge.cic() : dd.cic();
  type = Cartridge;
  challengeAlgo = DummyChallenge;
  if(model == "CIC-NUS-6101") region = NTSC, seed = 0x3f, checksum = 0x45cc73ee317aull;
  if(model == "CIC-NUS-6102") region = NTSC, seed = 0x3f, checksum = 0xa536c0f1d859ull;
  if(model == "CIC-NUS-7101") region = PAL,  seed = 0x3f, checksum = 0xa536c0f1d859ull;
  if(model == "CIC-NUS-7102") region = PAL,  seed = 0x3f, checksum = 0x44160ec5d9afull;
  if(model == "CIC-NUS-6103") region = NTSC, seed = 0x78, checksum = 0x586fd4709867ull;
  if(model == "CIC-NUS-7103") region = PAL,  seed = 0x78, checksum = 0x586fd4709867ull;
  if(model == "CIC-NUS-6105") region = NTSC, seed = 0x91, checksum = 0x8618a45bc2d3ull, challengeAlgo = RealChallenge;
  if(model == "CIC-NUS-7105") region = PAL,  seed = 0x91, checksum = 0x8618a45bc2d3ull, challengeAlgo = RealChallenge;
  if(model == "CIC-NUS-6106") region = NTSC, seed = 0x85, checksum = 0x2bbad4e6eb74ull;
  if(model == "CIC-NUS-7106") region = PAL,  seed = 0x85, checksum = 0x2bbad4e6eb74ull;
  if(model == "CIC-NUS-8303") region = NTSC, seed = 0xdd, checksum = 0x32b294e2ab90ull, type = DD64;
  if(model == "CIC-NUS-8401") region = NTSC, seed = 0xdd, checksum = 0x6ee8d9e84970ull, type = DD64;
  if(model == "CIC-NUS-5167") region = NTSC, seed = 0xdd, checksum = 0x083c6c77e0b1ull;
  if(model == "CIC-NUS-DDUS") region = NTSC, seed = 0xde, checksum = 0x05ba2ef0a5f1ull, type = DD64;
  state = BootRegion;
  fifo.bits.resize(32*4);
}

auto CIC::scramble(n4 *buf, int size) -> void {
  for(int i : range(1,size)) buf[i] += buf[i-1] + 1;
}

auto CIC::poll() -> void {
  if(state == BootRegion) {
    fifo.write(type);
    fifo.write(region == PAL);
    fifo.write(0);
    fifo.write(1);
    state = BootSeed;
    return;
  }

  if(state == BootSeed) {
    n4 buf[6];
    buf[0] = 0xB;
    buf[1] = 0x5;
    buf[2] = seed.bit(4,7);
    buf[3] = seed.bit(0,3);
    buf[4] = seed.bit(4,7);
    buf[5] = seed.bit(0,3);
    for (auto i : range(2)) scramble(buf, 6);
    for (auto i : range(6)) fifo.writeNibble(buf[i]);
    state = BootChecksum;
    return;
  }

  if(state == BootChecksum) {
    n4 buf[16];
    buf[0] = 0x4;  //true random
    buf[1] = 0x7;  //true random
    buf[2] = 0xA;  //true random
    buf[3] = 0x1;  //true random
    for (auto i : range(12)) buf[i+4] = checksum.bit(44-i*4,47-i*4);
    for (auto i : range(4))  scramble(buf, 16);
    for (auto i : range(16)) fifo.writeNibble(buf[i]);
    state = Run;
    return;
  }

  if(state == Run && fifo.size() >= 2) {
    n2 cmd;
    cmd.bit(1) = fifo.read();
    cmd.bit(0) = fifo.read();
    if(cmd == 0b00) return cmdCompare();
    if(cmd == 0b01) return cmdDie();
    if(cmd == 0b10) return cmdChallenge();
    if(cmd == 0b11) return cmdReset();
    return;
  }

  if(state == Challenge) {
    return cmdChallenge();
  }
  
  if(state == Dead) {
    return;
  }
}

}