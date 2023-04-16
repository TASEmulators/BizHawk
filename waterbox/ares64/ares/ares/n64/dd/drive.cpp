auto DD::seekTrack() -> n1 {
  n16 trackCalc = io.currentTrack.bit(0,11);
  n1 headCalc = io.currentTrack.bit(12);
  u16 trackPhysicalTable[] = {0x09E, 0x13C, 0x1D1, 0x266, 0x2FB, 0x390, 0x425};
  n8 pzone = 0;
  for(u32 n : range(7)) {
    if(trackCalc >= trackPhysicalTable[n]) pzone++;
  }
  pzone += headCalc;

  //return 1 if ROM area, return 0 if RAM area
  if (pzone <= (ctl.diskType + 2)) return 1;
  return 0;
}

auto DD::seekSector(n8 sector) -> u32 {
  n1 blockCalc = (sector >= 0x5A) ? 1 : 0;
  n8 sectorCalc = sector % 0x5A;
  n16 trackCalc = io.currentTrack.bit(0,11);
  n1 headCalc = io.currentTrack.bit(12);

  u32 startOffsetTable[16] = {0x0,0x5F15E0,0xB79D00,0x10801A0,0x1523720,0x1963D80,0x1D414C0,0x20BBCE0,
                              0x23196E0,0x28A1E00,0x2DF5DC0,0x3299340,0x36D99A0,0x3AB70E0,0x3E31900,0x4149200};
  
  u16 trackPhysicalTable[] = {0x000, 0x09E, 0x13C, 0x1D1, 0x266, 0x2FB, 0x390, 0x425};
  u16 blockSizeTable[] = {0x4D08, 0x47B8, 0x4510, 0x3FC0, 0x3A70, 0x3520, 0x2FD0, 0x2A80, 
                          0x47B8, 0x4510, 0x3FC0, 0x3A70, 0x3520, 0x2FD0, 0x2A80, 0x2530};
  n8 pzone = 0;
  for(u32 n : range(7)) {
    if(trackCalc >= trackPhysicalTable[n + 1]) pzone++;
  }
  trackCalc -= trackPhysicalTable[pzone];
  pzone += (headCalc) ? 8 : 0;

  u32 offsetCalc = startOffsetTable[pzone];
  offsetCalc += blockSizeTable[pzone] * 2 * trackCalc;
  offsetCalc += blockCalc * blockSizeTable[pzone];
  offsetCalc += sectorCalc * (io.sectorSizeBuf + 1);

  //return disk data offset
  return offsetCalc;
}

auto DD::bmRequest() -> void {
  //if BM not started make sure to not do anything
  if(!io.bm.start) {
    queue.remove(Queue::DD_BM_Request);
    lower(IRQ::BM);
    return;
  }

  //reset register state
  io.status.requestUserSector = 0;
  io.status.requestC2Sector = 0;
  io.micro.error = 0;
  io.bm.error = 0;
  io.bm.c1Single = 0;
  io.bm.c1Double = 0;
  io.error.am = 0;
  io.error.clockUnlock = 0;
  io.error.offTrack = 0;
  io.error.overrun = 0;
  io.error.selfStop = 0;
  io.error.spindle = 0;
  io.error.sector = 0;

  n1  blockCalc  = (io.currentSector >= 0x5A) ? 1 : 0;
  n8  sectorCalc = io.currentSector - (blockCalc * 0x5A);
  n16 trackCalc  = io.currentTrack.bit(0,11);
  n1  headCalc   = io.currentTrack.bit(12);

  n32 errorCalc  = (headCalc * (1175*2)) + (trackCalc * 2) + blockCalc;

  if(io.bm.readMode) {
    //read mode
    if(error.read<Byte>(errorCalc) != 0) {
      //copy protection (C1 fail all over, retail disk only)
      io.bm.c1Single = 1;
      io.bm.c1Double = 1;
    }

    if(sectorCalc < 0x55) {
      //user sector
      auto offsetCalc = seekSector(io.currentSector);
      for(u32 n : range(io.sectorSizeBuf + 1)) {
        ds.write<Byte>(n, disk.read<Byte>(offsetCalc + n));
      }
      io.status.requestUserSector = 1;
      io.currentSector++;
    } else if (sectorCalc < 0x58) {
      //c2 sector
      io.currentSector++;
    } else if (sectorCalc == 0x58) {
      //last c2 sector
      io.status.requestC2Sector = 1;
      if (io.bm.blockTransfer) {
        //wrap
        io.bm.blockTransfer = 0;
        sectorCalc = 0;
        blockCalc = 1 - blockCalc;
        io.currentSector = sectorCalc + (blockCalc * 0x5A);
      } else {
        //stop
        io.bm.start = 0;
      }
    }
  } else {
    //write mode
    //first interrupt: bm interrupt, request data, don't write anything to disk, wait
    //next interrupt:  assume sector data to be written is on buffer, write to disk
    //                 and request next sector data
    //therefore take into account writing previous sector
    //no need to write C1/C2 data, drive handles it automatically
    if(sectorCalc <= 0x55) {
      if (sectorCalc > 0) {
        auto offsetCalc = seekSector(io.currentSector - 1);
        for(u32 n : range(io.sectorSizeBuf + 1)) {
          disk.write<Byte>(offsetCalc + n, ds.read<Byte>(n));
        }
      }
      io.status.requestUserSector = 1;
    }

    //manage next sector
    if (sectorCalc >= 0x55) {
      //if next sector is on the other block, wrap around the track
      if (io.bm.blockTransfer) {
        io.bm.blockTransfer = 0;
        sectorCalc = 0;
        blockCalc = 1 - blockCalc;
        io.currentSector = sectorCalc + (blockCalc * 0x5A);
      } else {
        //last interrupt is basically acknowledge sector write, don't request data, stop afterwards
        io.status.requestUserSector = 0;
        io.bm.start = 0;
      }
    }

    io.currentSector++;
  }
  
  raise(IRQ::BM);
}

auto DD::motorActive() -> void {
  queue.remove(Queue::DD_Motor_Mode);
  io.status.headRetracted = 0;
  io.status.spindleMotorStopped = 0;
  if(!ctl.standbyDelayDisable)
    queue.insert(Queue::DD_Motor_Mode, (187'500'000 / 0x17) * ctl.standbyDelay);
}

auto DD::motorStandby() -> void {
  queue.remove(Queue::DD_Motor_Mode);
  io.status.headRetracted = 1;
  io.status.spindleMotorStopped = 0;
  if(!ctl.sleepDelayDisable)
      queue.insert(Queue::DD_Motor_Mode, (187'500'000 / 0x17) * ctl.sleepDelay);
}

auto DD::motorStop() -> void {
  queue.remove(Queue::DD_Motor_Mode);
  io.status.headRetracted = 1;
  io.status.spindleMotorStopped = 1;
}

auto DD::motorChange() -> void {
  //to sleep mode
  if(io.status.headRetracted && !io.status.spindleMotorStopped) {
    motorStop();
  }

  //to standby mode
  if(!io.status.headRetracted && !io.status.spindleMotorStopped) {
    motorStandby();
  }
}
