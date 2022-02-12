auto DD::readWord(u32 address) -> u32 {
  address = (address & 0x7f) >> 2;
  n32 data;

  //ASIC_DATA
  if(address == 0) {
  }

  //ASIC_MISC_REG
  if(address == 1) {
  }

  //ASIC_STATUS
  if(address == 2) {
    //required to indicate the 64DD is missing
    data = 0xffff'ffff;
  }

  //ASIC_CUR_TK
  if(address == 3) {
  }

  //ASIC_BM_STATUS
  if(address == 4) {
  }

  //ASIC_ERR_SECTOR
  if(address == 5) {
  }

  //ASIC_SEQ_STATUS
  if(address == 6) {
  }

  //ASIC_CUR_SECTOR
  if(address == 7) {
  }

  //ASIC_HARD_RESET
  if(address == 8) {
  }

  //ASIC_C1_S0
  if(address == 9) {
  }

  //ASIC_HOST_SECBYTE
  if(address == 10) {
  }

  //ASIC_C1_S2
  if(address == 11) {
  }

  //ASIC_SEC_BYTE
  if(address == 12) {
  }

  //ASIC_C1_S4
  if(address == 13) {
  }

  //ASIC_C1_S6
  if(address == 14) {
  }

  //ASIC_CUR_ADDRESS
  if(address == 15) {
  }

  //ASIC_ID_REG
  if(address == 16) {
  }

  //ASIC_TEST_REG
  if(address == 17) {
  }

  //ASIC_TEST_PIN_SEL
  if(address == 18) {
  }

  debugger.io(Read, address, data);
  return data;
}

auto DD::writeWord(u32 address, u32 data_) -> void {
  address = (address & 0x7f) >> 2;
  n32 data = data_;

  //ASIC_DATA
  if(address == 0) {
  }

  //ASIC_MISC_REG
  if(address == 1) {
  }

  //ASIC_CMD
  if(address == 2) {
  }

  //ASIC_CUR_TK
  if(address == 3) {
  }

  //ASIC_BM_CTL
  if(address == 4) {
  }

  //ASIC_ERR_SECTOR
  if(address == 5) {
  }

  //ASIC_SEQ_CTL
  if(address == 6) {
  }

  //ASIC_CUR_SECTOR
  if(address == 7) {
  }

  //ASIC_HARD_RESET
  if(address == 8) {
  }

  //ASIC_C1_S0
  if(address == 9) {
  }

  //ASIC_HOST_SECBYTE
  if(address == 10) {
  }

  //ASIC_C1_S2
  if(address == 11) {
  }

  //ASIC_SEC_BYTE
  if(address == 12) {
  }

  //ASIC_C1_S4
  if(address == 13) {
  }

  //ASIC_C1_S6
  if(address == 14) {
  }

  //ASIC_CUR_ADDRESS
  if(address == 15) {
  }

  //ASIC_ID_REG
  if(address == 16) {
  }

  //ASIC_TEST_REG
  if(address == 17) {
  }

  //ASIC_TEST_PIN_SEL
  if(address == 18) {
  }

  debugger.io(Write, address, data);
}
