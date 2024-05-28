
auto Cartridge::joybusComm(n8 send, n8 recv, n8 input[], n8 output[]) -> n2 {
  n1 valid = 0, over = 0;
  
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
  if(input[0] == 0x06 && send >= 1 && recv >= 3) {
    if(cartridge.rtc.present) {
      output[0] = 0x00;
      output[1] = 0x10;
      output[2] = rtc.status;
      valid = 1;
    }
  }

  //RTC read
  if(input[0] == 0x07 && send >= 2 && recv >= 9) {
    if(cartridge.rtc.present) {
      rtc.read(input[1], &output[0]);
      output[8] = 0x00;
      valid = 1;
    }
  }

  //RTC write
  if(input[0] == 0x08 && send >= 10 && recv >= 1) {
    if(cartridge.rtc.present) {
      rtc.write(input[1], &input[2]);
      output[0] = 0x00;
      valid = 1;
    }
  }

  n2 status;
  status.bit(0) = valid;
  status.bit(1) = over;
  return status;
}
