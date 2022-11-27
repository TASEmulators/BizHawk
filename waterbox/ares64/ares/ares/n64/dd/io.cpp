auto DD::readWord(u32 address) -> u32 {
  address = (address & 0x7f) >> 2;
  n32 data;

  //ASIC_DATA
  if(address == 0) {
    data.bit(16,31) = io.data;
  }

  //ASIC_MISC_REG
  if(address == 1) {
  }

  //ASIC_STATUS
  if(address == 2) {
    data.bit(16) = io.status.diskChanged;
    data.bit(17) = io.status.mechaError;
    data.bit(18) = io.status.writeProtect;
    data.bit(19) = io.status.headRetracted;
    data.bit(20) = io.status.spindleMotorStopped;
    data.bit(22) = io.status.resetState;
    data.bit(23) = io.status.busyState;
    data.bit(24) = (bool)disk; //disk present
    data.bit(25) = irq.mecha.line;
    data.bit(26) = irq.bm.line;
    data.bit(27) = io.bm.error;
    data.bit(28) = io.status.requestC2Sector;
    data.bit(30) = io.status.requestUserSector;

    //acknowledge bm interrupt (tested on real hardware)
    if(irq.bm.line) {
      //TODO: proper research into seek and access times
      queue.insert(Queue::DD_BM_Request, 38'000 + (io.currentTrack.bit(0,11) / 15));
      lower(IRQ::BM);
    }
  }

  //ASIC_CUR_TK
  if(address == 3) {
    data.bit(16,31) = io.currentTrack;
  }

  //ASIC_BM_STATUS
  if(address == 4) {
    data.bit(16) = io.bm.c1Error;
    data.bit(21) = io.bm.c1Single;
    data.bit(22) = io.bm.c1Double;
    data.bit(23) = io.bm.c1Correct;
    data.bit(24) = io.bm.blockTransfer;
    data.bit(25) = io.micro.error;
    data.bit(26) = io.bm.error;
    data.bit(31) = io.bm.start;
  }

  //ASIC_ERR_SECTOR
  if(address == 5) {
    data.bit(16,23) = io.error.sector;
    data.bit(24) = io.error.selfStop;
    data.bit(25) = io.error.clockUnlock;
    data.bit(26) = ~(bool)disk; //no disk
    data.bit(27) = io.error.offTrack;
    data.bit(28) = io.error.overrun;
    data.bit(29) = io.error.spindle;
    data.bit(30) = io.micro.error;
    data.bit(31) = io.error.am;
  }

  //ASIC_SEQ_STATUS
  if(address == 6) {
  }

  //ASIC_CUR_SECTOR
  if(address == 7) {
    data.bit(24,31) = io.currentSector;
    data.bit(16,23) = 0xc3;
  }

  //ASIC_HARD_RESET
  if(address == 8) {
  }

  //ASIC_C1_S0
  if(address == 9) {
  }

  //ASIC_HOST_SECBYTE
  if(address == 10) {
    data.bit(16,23) = io.sectorSizeBuf;
  }

  //ASIC_C1_S2
  if(address == 11) {
  }

  //ASIC_SEC_BYTE
  if(address == 12) {
    data.bit(16,23) = io.sectorSize;
    data.bit(24,31) = io.sectorBlock;
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
    data.bit(16,31) = io.id;
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
    io.data = data.bit(16,31);
  }

  //ASIC_MISC_REG
  if(address == 1) {
  }

  //ASIC_CMD
  if(address == 2) {
    command(data.bit(16,31));
  }

  //ASIC_CUR_TK
  if(address == 3) {
  }

  //ASIC_BM_CTL
  if(address == 4) {
    io.bm.reset |= data.bit(28);
    io.bm.readMode = data.bit(30);
    //irq.bm.mask = ~data.bit(29);
    io.bm.disableORcheck = data.bit(27);
    io.bm.disableC1Correction = data.bit(26);
    io.bm.blockTransfer = data.bit(25);
    if (data.bit(24)) {
      //mecha int reset
      lower(IRQ::MECHA);
    }
    io.currentSector = data.bit(16,23);
    if (!data.bit(28) && io.bm.reset) {
      //BM reset
      io.bm.start = 0;
      io.bm.error = 0;
      io.status.requestUserSector = 0;
      io.status.requestC2Sector = 0;
      io.bm.reset = 0;
      lower(IRQ::BM);
    }

    if(data.bit(31) && disk) {
      //start BM
      io.bm.start |= data.bit(31);
      //TODO: proper research into seek and access times
      queue.insert(Queue::DD_BM_Request, 50'000 + (io.currentTrack.bit(0,11) / 15));
    }
  }

  //ASIC_ERR_SECTOR
  if(address == 5) {
  }

  //ASIC_SEQ_CTL
  if(address == 6) {
    io.micro.enable = data.bit(30);
  }

  //ASIC_CUR_SECTOR
  if(address == 7) {
  }

  //ASIC_HARD_RESET
  if(address == 8) {
    if((data >> 16) == 0xAAAA) {
      power(true);
    }
  }

  //ASIC_C1_S0
  if(address == 9) {
  }

  //ASIC_HOST_SECBYTE
  if(address == 10) {
    io.sectorSizeBuf = data.bit(16,23);
  }

  //ASIC_C1_S2
  if(address == 11) {
    io.sectorSize = data.bit(16,23);
    io.sectorBlock = data.bit(24,31);
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
