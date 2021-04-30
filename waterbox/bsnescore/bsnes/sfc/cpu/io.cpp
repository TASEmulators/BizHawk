auto CPU::readRAM(uint addr, uint8 data) -> uint8 {
  return wram[addr];
}

auto CPU::readAPU(uint addr, uint8 data) -> uint8 {
  synchronizeSMP();
  return smp.portRead(addr & 3);
}

auto CPU::readCPU(uint addr, uint8 data) -> uint8 {
  switch(addr & 0xffff) {
  case 0x2180:  //WMDATA
    return bus.read(0x7e0000 | io.wramAddress++, data);

  //todo: it is not known what happens when reading from this register during auto-joypad polling
  case 0x4016:  //JOYSER0
    data &= 0xfc;
    data |= controllerPort1.device->data();
    return data;

  //todo: it is not known what happens when reading from this register during auto-joypad polling
  case 0x4017:  //JOYSER1
    data &= 0xe0;
    data |= 0x1c;  //pins are connected to GND
    data |= controllerPort2.device->data();
    return data;

  case 0x4210:  //RDNMI
    data &= 0x70;
    data |= rdnmi() << 7;
    data |= (uint4)version;
    return data;

  case 0x4211:  //TIMEUP
    data &= 0x7f;
    data |= timeup() << 7;
    return data;

  case 0x4212:  //HVBJOY
    data &= 0x3e;
    data |= io.autoJoypadPoll && status.autoJoypadCounter < 33;
    data |= (hcounter() <= 2 || hcounter() >= 1096) << 6;  //hblank
    data |= (vcounter() >= ppu.vdisp()) << 7;              //vblank
    return data;

  case 0x4213: return io.pio;  //RDIO

  case 0x4214: return io.rddiv >> 0;  //RDDIVL
  case 0x4215: return io.rddiv >> 8;  //RDDIVH
  case 0x4216: return io.rdmpy >> 0;  //RDMPYL
  case 0x4217: return io.rdmpy >> 8;  //RDMPYH

  //todo: it is not known what happens when reading from these registers during auto-joypad polling
  case 0x4218: return io.joy1 >> 0;   //JOY1L
  case 0x4219: return io.joy1 >> 8;   //JOY1H
  case 0x421a: return io.joy2 >> 0;   //JOY2L
  case 0x421b: return io.joy2 >> 8;   //JOY2H
  case 0x421c: return io.joy3 >> 0;   //JOY3L
  case 0x421d: return io.joy3 >> 8;   //JOY3H
  case 0x421e: return io.joy4 >> 0;   //JOY4L
  case 0x421f: return io.joy4 >> 8;   //JOY4H

  }

  return data;
}

auto CPU::readDMA(uint addr, uint8 data) -> uint8 {
  auto& channel = this->channels[addr >> 4 & 7];

  switch(addr & 0xff8f) {

  case 0x4300:  //DMAPx
    return (
      channel.transferMode    << 0
    | channel.fixedTransfer   << 3
    | channel.reverseTransfer << 4
    | channel.unused          << 5
    | channel.indirect        << 6
    | channel.direction       << 7
    );

  case 0x4301: return channel.targetAddress;       //BBADx
  case 0x4302: return channel.sourceAddress >> 0;  //A1TxL
  case 0x4303: return channel.sourceAddress >> 8;  //A1TxH
  case 0x4304: return channel.sourceBank;          //A1Bx
  case 0x4305: return channel.transferSize >> 0;   //DASxL
  case 0x4306: return channel.transferSize >> 8;   //DASxH
  case 0x4307: return channel.indirectBank;        //DASBx
  case 0x4308: return channel.hdmaAddress >> 0;    //A2AxL
  case 0x4309: return channel.hdmaAddress >> 8;    //A2AxH
  case 0x430a: return channel.lineCounter;         //NTRLx
  case 0x430b: return channel.unknown;             //???x
  case 0x430f: return channel.unknown;             //???x ($43xb mirror)

  }

  return data;
}

auto CPU::writeRAM(uint addr, uint8 data) -> void {
  wram[addr] = data;
}

auto CPU::writeAPU(uint addr, uint8 data) -> void {
  synchronizeSMP();
  return smp.portWrite(addr & 3, data);
}

auto CPU::writeCPU(uint addr, uint8 data) -> void {
  switch(addr & 0xffff) {

  case 0x2180:  //WMDATA
    return bus.write(0x7e0000 | io.wramAddress++, data);

  case 0x2181:  //WMADDL
    io.wramAddress = io.wramAddress & 0x1ff00 | data << 0;
    return;

  case 0x2182:  //WMADDM
    io.wramAddress = io.wramAddress & 0x100ff | data << 8;
    return;

  case 0x2183:  //WMADDH
    io.wramAddress = io.wramAddress & 0x0ffff | (data & 1) << 16;
    return;

  //todo: it is not known what happens when writing to this register during auto-joypad polling
  case 0x4016:  //JOYSER0
    //bit 0 is shared between JOYSER0 and JOYSER1:
    //strobing $4016.d0 affects both controller port latches.
    //$4017 bit 0 writes are ignored.
    controllerPort1.device->latch(data & 1);
    controllerPort2.device->latch(data & 1);
    return;

  case 0x4200:  //NMITIMEN
    io.autoJoypadPoll = data & 1;
    if(!io.autoJoypadPoll) status.autoJoypadCounter = 33; // Disable auto-joypad read
    nmitimenUpdate(data);
    return;

  case 0x4201:  //WRIO
    if((io.pio & 0x80) && !(data & 0x80)) ppu.latchCounters();
    io.pio = data;
    return;

  case 0x4202:  //WRMPYA
    io.wrmpya = data;
    return;

  case 0x4203:  //WRMPYB
    io.rdmpy = 0;
    if(alu.mpyctr || alu.divctr) return;

    io.wrmpyb = data;
    io.rddiv = io.wrmpyb << 8 | io.wrmpya;

    if(!configuration.hacks.cpu.fastMath) {
      alu.mpyctr = 8;  //perform multiplication over the next eight cycles
      alu.shift = io.wrmpyb;
    } else {
      io.rdmpy = io.wrmpya * io.wrmpyb;
    }
    return;

  case 0x4204:  //WRDIVL
    io.wrdiva = io.wrdiva & 0xff00 | data << 0;
    return;

  case 0x4205:  //WRDIVH
    io.wrdiva = io.wrdiva & 0x00ff | data << 8;
    return;

  case 0x4206:  //WRDIVB
    io.rdmpy = io.wrdiva;
    if(alu.mpyctr || alu.divctr) return;

    io.wrdivb = data;

    if(!configuration.hacks.cpu.fastMath) {
      alu.divctr = 16;  //perform division over the next sixteen cycles
      alu.shift = io.wrdivb << 16;
    } else {
      if(io.wrdivb) {
        io.rddiv = io.wrdiva / io.wrdivb;
        io.rdmpy = io.wrdiva % io.wrdivb;
      } else {
        io.rddiv = 0xffff;
        io.rdmpy = io.wrdiva;
      }
    }
    return;

  case 0x4207:  //HTIMEL
    io.htime = (io.htime >> 2) - 1;
    io.htime = io.htime & 0x100 | data << 0;
    io.htime = (io.htime + 1) << 2;
    irqPoll();  //unverified
    return;

  case 0x4208:  //HTIMEH
    io.htime = (io.htime >> 2) - 1;
    io.htime = io.htime & 0x0ff | (data & 1) << 8;
    io.htime = (io.htime + 1) << 2;
    irqPoll();  //unverified
    return;

  case 0x4209:  //VTIMEL
    io.vtime = io.vtime & 0x100 | data << 0;
    irqPoll();  //unverified
    return;

  case 0x420a:  //VTIMEH
    io.vtime = io.vtime & 0x0ff | (data & 1) << 8;
    irqPoll();  //unverified
    return;

  case 0x420b:  //DMAEN
    for(auto n : range(8)) channels[n].dmaEnable = bool(data & 1 << n);
    if(data) status.dmaPending = true;
    return;

  case 0x420c:  //HDMAEN
    for(auto n : range(8)) channels[n].hdmaEnable = bool(data & 1 << n);
    return;

  case 0x420d:  //MEMSEL
    io.fastROM = data & 1;
    return;

  }
}

auto CPU::writeDMA(uint addr, uint8 data) -> void {
  auto& channel = this->channels[addr >> 4 & 7];

  switch(addr & 0xff8f) {

  case 0x4300:  //DMAPx
    channel.transferMode    = data >> 0 & 7;
    channel.fixedTransfer   = data >> 3 & 1;
    channel.reverseTransfer = data >> 4 & 1;
    channel.unused          = data >> 5 & 1;
    channel.indirect        = data >> 6 & 1;
    channel.direction       = data >> 7 & 1;
    return;

  case 0x4301:  //BBADx
    channel.targetAddress = data;
    return;

  case 0x4302:  //A1TxL
    channel.sourceAddress = channel.sourceAddress & 0xff00 | data << 0;
    return;

  case 0x4303:  //A1TxH
    channel.sourceAddress = channel.sourceAddress & 0x00ff | data << 8;
    return;

  case 0x4304:  //A1Bx
    channel.sourceBank = data;
    return;

  case 0x4305:  //DASxL
    channel.transferSize = channel.transferSize & 0xff00 | data << 0;
    return;

  case 0x4306:  //DASxH
    channel.transferSize = channel.transferSize & 0x00ff | data << 8;
    return;

  case 0x4307:  //DASBx
    channel.indirectBank = data;
    return;

  case 0x4308:  //A2AxL
    channel.hdmaAddress = channel.hdmaAddress & 0xff00 | data << 0;
    return;

  case 0x4309:  //A2AxH
    channel.hdmaAddress = channel.hdmaAddress & 0x00ff | data << 8;
    return;

  case 0x430a:  //NTRLx
    channel.lineCounter = data;
    return;

  case 0x430b:  //???x
    channel.unknown = data;
    return;

  case 0x430f:  //???x ($43xb mirror)
    channel.unknown = data;
    return;

  }
}
