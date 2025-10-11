auto Cartridge::SC64::regReadWord(u32 address) -> u64 {
  n32 data;
  data.bit(0,31) = (address << 16) | (address & 0xFFFF);
  address = (address & 0x1F) >> 2;

  if (lockState != 2) {
    return u64(data);
  }

  if(address == 0) {
    //SCR, command execution and status
    //Mostly unimplemented
    data.bit(31) = busy;
    data.bit(30) = error;
  }

  if(address == 1) {
    //DATA0, command arg/result 0
    data.bit(0,31) = data0;
  }

  if(address == 2) {
    //DATA1, command arg/result 1
    data.bit(0,31) = data1;
  }

  if(address == 3) {
    //IDENTIFIER, flashcart identifier
    //Read-only
    data.bit(0,31) = 0x53'43'76'32; /* SCv2 */
  }

  if(address == 4) {
    //KEY
    //Write-only
  }

  if(address == 5) {
    //IRQ
    //Unimplemented
  }

  if(address == 6) {
    //AUX
    //Unimplemented
  }

  return u64(data);
}

auto Cartridge::SC64::regWriteWord(u32 address, u64 data_) -> void {
  address = (address & 0x1F) >> 2;
  n32 data = data_;

  if(address == 0 && lockState == 2) {
    //SCR, command execution and status
    //Writes initiate a command
    u8 cmd = data.bit(0,7);

    switch (cmd) {
      case 'i': //SD Operation
        if(data1 == 1) //SD Init
          sdIsInit = true;
        break;

      case 'I': //SD Sector Set
        sdAddr = data0 * SC64::SDSectorWords * 4;
        break;

      case 's': //SD Read Sectors
        if(sdIsInit) {
          u32 piAddress = data0;
          u32 numSectors = data1;
          u32 numWords = numSectors * SC64::SDSectorWords;
          //Note: Only data buffer is supported here but real SC64 is more flexible
          for(auto offset : range(numWords))
            buffer.write<Word>(piAddress + offset * 4, sd.read<Word>(sdAddr + offset * 4));
        }
        break;

      case 'S': //SD Write Sectors
        if(sdIsInit) {
          u32 piAddress = data0;
          u32 numSectors = data1;
          u32 numWords = numSectors * SC64::SDSectorWords;
          //Note: Only data buffer is supported here but real SC64 is more flexible
          for(auto offset : range(numWords))
            sd.write<Word>(sdAddr + offset * 4, buffer.read<Word>(piAddress + offset * 4));
        }
        break;

      default: break; //Unrecongized/Unimplemented
    }
  }

  if(address == 1 && lockState == 2) {
    //DATA0, command arg/result 0
    data0 = data.bit(0,31);
  }

  if(address == 2 && lockState == 2) {
    //DATA1, command arg/result 1
    data1 = data.bit(0,31);
  }

  if(address == 3 && lockState == 2) {
    //IDENTIFIER, flashcart identifier
    //Read-only
  }

  if(address == 4) {
    //KEY
    if(data.bit(0,31) == 0x00000000 || data.bit(0,31) == 0xFFFFFFFF)
      lockState = 0;
    else if(data.bit(0,31) == 0x5F554E4C && lockState == 0)
      lockState = 1;
    else if(data.bit(0,31) == 0x4F434B5F && lockState == 1)
      lockState = 2;
  }

  if(address == 5 && lockState == 2) {
    //IRQ
    //Unimplemented
  }

  if(address == 6 && lockState == 2) {
    //AUX
    //Unimplemented
  }
}

auto Cartridge::SC64::serialize(serializer& s) -> void {
  s(enabled);
  s(sd);
  s(buffer);
  s(lockState);
  s(busy);
  s(error);
  s(data0);
  s(data1);
  s(sdIsInit);
  s(sdAddr);
}
