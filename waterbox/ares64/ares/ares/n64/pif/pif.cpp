#include <n64/n64.hpp>

namespace ares::Nintendo64 {

PIF pif;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto PIF::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("PIF");
  rom.allocate(0x7c0);
  ram.allocate(0x040);

  debugger.load(node);
}

auto PIF::unload() -> void {
  debugger = {};
  rom.reset();
  ram.reset();
  node.reset();
}

auto PIF::addressCRC(u16 address) const -> n5 {
  n5 crc = 0;
  for(u32 i : range(16)) {
    n5 xor = crc & 0x10 ? 0x15 : 0x00;
    crc <<= 1;
    if(address & 0x8000) crc |= 1;
    address <<= 1;
    crc ^= xor;
  }
  return crc;
}

auto PIF::dataCRC(array_view<u8> data) const -> n8 {
  n8 crc = 0;
  for(u32 i : range(33)) {
    for(u32 j : reverse(range(8))) {
      n8 xor = crc & 0x80 ? 0x85 : 0x00;
      crc <<= 1;
      if(i < 32) {
        if(data[i] & 1 << j) crc |= 1;
      }
      crc ^= xor;
    }
  }
  return crc;
}

auto PIF::run() -> void {
  auto flags = ram.read<Byte>(0x3f);

  //controller polling
  if(flags & 0x01) {
  //todo: this flag is supposed to be cleared, but doing so breaks inputs
  //flags &= ~0x01;
    scan();
  }

  //CIC-NUS-6105 challenge/response
  if(flags & 0x02) {
    flags &= ~0x02;
    challenge();
  }

  //unknown purpose
  if(flags & 0x04) {
    flags &= ~0x04;
    debug(unimplemented, "[SI::main] flags & 0x04");
  }

  //must be sent within 5s of the console booting, or SM5 will lock the N64
  if(flags & 0x08) {
    flags &= ~0x08;
  }

  //PIF ROM lockout
  if(flags & 0x10) {
    flags &= ~0x10;
    io.romLockout = 1;
  }

  //initialization
  if(flags & 0x20) {
    flags &= ~0x20;
    flags |=  0x80;  //set completion flag
  }

  //clear PIF RAM
  if(flags & 0x40) {
    flags &= ~0x40;
    ram.fill();
  }

  ram.write<Byte>(0x3f, flags);
}

auto PIF::scan() -> void {
  ControllerPort* controllers[4] = {
    &controllerPort1,
    &controllerPort2,
    &controllerPort3,
    &controllerPort4,
  };

  static constexpr bool Debug = 0;

  if constexpr(Debug) {
    print("{\n");
    for(u32 y : range(8)) {
      print("  ");
      for(u32 x : range(8)) {
        print(hex(ram.read<Byte>(y * 8 + x), 2L), " ");
      }
      print("\n");
    }
    print("}\n");
  }

  n3 channel = 0;  //0-5
  for(u32 offset = 0; offset < 64;) {
    n8 send = ram.read<Byte>(offset++);
    if(send == 0x00) { channel++; continue; }
    if(send == 0xfd) continue;  //channel reset
    if(send == 0xfe) break;     //end of packets
    if(send == 0xff) continue;  //alignment padding
    n8 recvOffset = offset;
    n8 recv = ram.read<Byte>(offset++);
    if(recv == 0xfe) break;     //end of packets

    //clear flags from lengths
    send &= 0x3f;
    recv &= 0x3f;

    n8 input[64];
    for(u32 index : range(send)) {
      input[index] = ram.read<Byte>(offset++);
    }
    n8 output[64];
    b1 valid = 0;
    b1 over = 0;

    //controller port communication
    if (channel < 4 && controllers[channel]->device) {
      n2 status = controllers[channel]->device->comm(send, recv, input, output);
      valid = status.bit(0);
      over = status.bit(1);
    }
    
    if (channel >= 4) {
      //status
      if(input[0] == 0x00 || input[0] == 0xff) {
        //cartridge EEPROM (4kbit)
        if(cartridge.eeprom.size == 512) {
          output[0] = 0x00;
          output[1] = 0x80;
          output[2] = 0x00;
          valid = 1;
        }

        //cartridge EEPROM (16kbit)
        if(cartridge.eeprom.size == 2048) {
          output[0] = 0x00;
          output[1] = 0xc0;
          output[2] = 0x00;
          valid = 1;
        }
      }

      //read EEPROM
      if(input[0] == 0x04 && send >= 2) {
        u32 address = input[1] * 8;
        for(u32 index : range(recv)) {
          output[index] = cartridge.eeprom.read<Byte>(address++);
        }
        valid = 1;
      }

      //write EEPROM
      if(input[0] == 0x05 && send >= 2 && recv >= 1) {
        u32 address = input[1] * 8;
        for(u32 index : range(send - 2)) {
          cartridge.eeprom.write<Byte>(address++, input[2 + index]);
        }
        output[0] = 0x00;
        valid = 1;
      }

      //RTC status
      if(input[0] == 0x06) {
        debug(unimplemented, "[SI::main] RTC status");
      }

      //RTC read
      if(input[0] == 0x07) {
        debug(unimplemented, "[SI::main] RTC read");
      }

      //RTC write
      if(input[0] == 0x08) {
        debug(unimplemented, "[SI::main] RTC write");
      }
    }

    if(!valid) {
      ram.write<Byte>(recvOffset, 0x80 | recv & 0x3f);
    }
    if(over) {
      ram.write<Byte>(recvOffset, 0x40 | recv & 0x3f);
    }

    if (valid) {
      for(u32 index : range(recv)) {
        ram.write<Byte>(offset++, output[index]);
      }
    }
    channel++;
  }

  if constexpr(Debug) {
    print("[\n");
    for(u32 y : range(8)) {
      print("  ");
      for(u32 x : range(8)) {
        print(hex(ram.read<Byte>(y * 8 + x), 2L), " ");
      }
      print("\n");
    }
    print("]\n");
  }
}

//CIC-NUS-6105 anti-piracy challenge/response
auto PIF::challenge() -> void {
  static n4 lut[32] = {
    0x4, 0x7, 0xa, 0x7, 0xe, 0x5, 0xe, 0x1,
    0xc, 0xf, 0x8, 0xf, 0x6, 0x3, 0x6, 0x9,
    0x4, 0x1, 0xa, 0x7, 0xe, 0x5, 0xe, 0x1,
    0xc, 0x9, 0x8, 0x5, 0x6, 0x3, 0xc, 0x9,
  };

  n4 challenge[30];
  n4 response[30];

  //15 bytes -> 30 nibbles
  for(u32 address : range(15)) {
    auto data = ram.read<Byte>(0x30 + address);
    challenge[address << 1 | 0] = data >> 4;
    challenge[address << 1 | 1] = data >> 0;
  }

  n4 key = 0xb;
  n1 sel = 0;
  for(u32 address : range(30)) {
    n4 data = key + 5 * challenge[address];
    response[address] = data;
    key = lut[sel << 4 | data];
    n1 mod = data >> 3;
    n3 mag = data >> 0;
    if(mod) mag = ~mag;
    if(mag % 3 != 1) mod = !mod;
    if(sel) {
      if(data == 0x1 || data == 0x9) mod = 1;
      if(data == 0xb || data == 0xe) mod = 0;
    }
    sel = mod;
  }

  //30 nibbles -> 15 bytes
  for(u32 address : range(15)) {
    n8 data = 0;
    data |= response[address << 1 | 0] << 4;
    data |= response[address << 1 | 1] << 0;
    ram.write<Byte>(0x30 + address, data);
  }
}

auto PIF::power(bool reset) -> void {
  string pifrom = Region::PAL() ? "pif.pal.rom" : "pif.ntsc.rom";
  if(auto fp = system.pak->read(pifrom)) {
    rom.load(fp);
  }

  ram.fill();
  io = {};

  //write CIC seeds into PIF RAM so that cartridge checksum function passes
  string cic = cartridge.node ? cartridge.cic() : dd.cic();
  n8 seed = 0x3f;
  n1 version = 0;
  n1 type = 0;
  if(cic == "CIC-NUS-6101" || cic == "CIC-NUS-7102") seed = 0x3f, version = 1;
  if(cic == "CIC-NUS-6102" || cic == "CIC-NUS-7101") seed = 0x3f;
  if(cic == "CIC-NUS-6103" || cic == "CIC-NUS-7103") seed = 0x78;
  if(cic == "CIC-NUS-6105" || cic == "CIC-NUS-7105") seed = 0x91;
  if(cic == "CIC-NUS-6106" || cic == "CIC-NUS-7106") seed = 0x85;
  if(cic == "CIC-NUS-8303" || cic == "CIC-NUS-8401") seed = 0xdd, type = 1;
  if(cic == "CIC-NUS-DDUS") seed = 0xde, type = 1;

  n32 data;
  data.bit(0, 7) = 0x3f;     //CIC IPL2 seed
  data.bit(8,15) = seed;     //CIC IPL3 seed
  data.bit(17)   = reset;    //osResetType (0 = power; 1 = reset (NMI))
  data.bit(18)   = version;  //osVersion
  data.bit(19)   = type;     //osRomType (0 = Gamepak; 1 = 64DD)
  ram.write<Word>(0x24, data);
}

}
