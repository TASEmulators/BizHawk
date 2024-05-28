auto Cartridge::Flash::readByte(u32 address) -> u64 {
  debug(unusual, "[Cartridge::Flash::readByte] mode=", (u32)mode);
  return 0;
}

auto Cartridge::Flash::readHalf(u32 address) -> u64 {
  if(mode == Mode::Read) {
    return Memory::Writable::read<Half>(address);
  }

  if(mode == Mode::Status) {
    switch(address & 6) { default:
    case 0: return status >> 48;
    case 2: return status >> 32;
    case 4: return status >> 16;
    case 6: return status >>  0;
    }
  }

  debug(unusual, "[Cartridge::Flash::readHalf] mode=", (u32)mode);
  return 0;
}

auto Cartridge::Flash::readWord(u32 address) -> u64 {
  switch(address & 4) { default:
  case 0: return status >> 32;
  case 4: return status >>  0;
  }
}

auto Cartridge::Flash::readDual(u32 address) -> u64 {
  debug(unusual, "[Cartridge::Flash::readDual] mode=", (u32)mode);
  return 0;
}

auto Cartridge::Flash::writeByte(u32 address, u64 data) -> void {
  debug(unusual, "[Cartridge::Flash::writeByte] mode=", (u32)mode);
  return;
}

auto Cartridge::Flash::writeHalf(u32 address, u64 data) -> void {
  if(mode == Mode::Write) {
    //writes are deferred until the flash execute command is sent later
    source = pi.io.dramAddress;
    return;
  }

  debug(unusual, "[Cartridge::Flash::writeHalf] mode=", (u32)mode);
  return;
}

auto Cartridge::Flash::writeWord(u32 address, u64 data) -> void {
  address = (address & 0x7ff'ffff) >> 2;

  if(address == 0) {
    debug(unusual, "[Cartridge::Flash::writeWord] ignoring write to status register");
    return;
  }

  u8 command = data >> 24;
  switch(command) {
  case 0x4b:  //set erase offset
    offset = u16(data) * 128;
    return;

  case 0x78:  //erase
    mode = Mode::Erase;
    status = 0x1111'8008'00c2'001dull;
    return;

  case 0xa5:  //set write offset
    offset = u16(data) * 128;
    status = 0x1111'8004'00c2'001dull;
    return;

  case 0xb4:  //write
    mode = Mode::Write;
    return;

  case 0xd2:  //execute
    if(mode == Mode::Erase) {
      for(u32 index = 0; index < 128; index += 2) {
        Memory::Writable::write<Half>(offset + index, 0xffff);
      }
    }
    if(mode == Mode::Write) {
      for(u32 index = 0; index < 128; index += 2) {
        // FIXME: this is obviously wrong, the flash can't access RDRAM
        u16 half = rdram.ram.read<Half>(source + index, "Flash");
        Memory::Writable::write<Half>(offset + index, half);
      }
    }
    return;

  case 0xe1:  //status
    mode = Mode::Status;
    status = 0x1111'8001'00c2'001dull;
    return;

  case 0xf0:  //read
    mode = Mode::Read;
    status = 0x1111'8004'f000'001dull;
    return;

  default:
    debug(unusual, "[Cartridge::Flash::writeWord] command=", hex(command, 2L));
    return;
  }
}

auto Cartridge::Flash::writeDual(u32 address, u64 data) -> void {
  debug(unusual, "[Cartridge::Flash::writeDual] mode=", (u32)mode);
  return;
}
