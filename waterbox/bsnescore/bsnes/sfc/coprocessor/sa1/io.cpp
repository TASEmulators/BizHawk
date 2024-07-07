auto SA1::readIOCPU(uint address, uint8 data) -> uint8 {
  cpu.synchronizeCoprocessors();

  switch(0x2200 | address & 0x1ff) {

  //(SFR) S-CPU flag read
  case 0x2300: {
    uint8 data;
    data  = mmio.cpu_irqfl   << 7;
    data |= mmio.cpu_ivsw    << 6;
    data |= mmio.chdma_irqfl << 5;
    data |= mmio.cpu_nvsw    << 4;
    data |= mmio.cmeg;
    return data;
  }

  //(VC) version code register
  case 0x230e: {
    break;  //does not actually exist on real hardware ... always returns open bus
  }

  }

  return data;
}

auto SA1::readIOSA1(uint address, uint8) -> uint8 {
  synchronizeCPU();

  switch(0x2200 | address & 0x1ff) {

  //(CFR) SA-1 flag read
  case 0x2301: {
    uint8 data;
    data  = mmio.sa1_irqfl   << 7;
    data |= mmio.timer_irqfl << 6;
    data |= mmio.dma_irqfl   << 5;
    data |= mmio.sa1_nmifl   << 4;
    data |= mmio.smeg;
    return data;
  }

  //(HCR) hcounter read
  case 0x2302: {
    //latch counters
    mmio.hcr = status.hcounter >> 2;
    mmio.vcr = status.vcounter;
    return mmio.hcr >> 0;
  }

  case 0x2303: {
    return mmio.hcr >> 8;
  }

  //(VCR) vcounter read
  case 0x2304: return mmio.vcr >> 0;
  case 0x2305: return mmio.vcr >> 8;

  //(MR) arithmetic result
  case 0x2306: return mmio.mr >>  0;
  case 0x2307: return mmio.mr >>  8;
  case 0x2308: return mmio.mr >> 16;
  case 0x2309: return mmio.mr >> 24;
  case 0x230a: return mmio.mr >> 32;

  //(OF) arithmetic overflow flag
  case 0x230b: return mmio.overflow << 7;

  //(VDPL) variable-length data read port low
  case 0x230c: {
    uint24 data;
    data.byte(0) = readVBR(mmio.va + 0);
    data.byte(1) = readVBR(mmio.va + 1);
    data.byte(2) = readVBR(mmio.va + 2);
    data >>= mmio.vbit;

    return data >> 0;
  }

  //(VDPH) variable-length data read port high
  case 0x230d: {
    uint24 data;
    data.byte(0) = readVBR(mmio.va + 0);
    data.byte(1) = readVBR(mmio.va + 1);
    data.byte(2) = readVBR(mmio.va + 2);
    data >>= mmio.vbit;

    if(mmio.hl == 1) {
      //auto-increment mode
      mmio.vbit += mmio.vb;
      mmio.va += (mmio.vbit >> 3);
      mmio.vbit &= 7;
    }

    return data >> 8;
  }

  }

  return 0xff;
}

auto SA1::writeIOCPU(uint address, uint8 data) -> void {
  cpu.synchronizeCoprocessors();

  switch(0x2200 | address & 0x1ff) {

  //(CCNT) SA-1 control
  case 0x2200: {
    if(mmio.sa1_resb && !(data & 0x20)) {
      //reset SA-1 CPU (PC bank and data bank set to 0x00, clear STP status)
      r.pc.d = mmio.crv;
      r.b    = 0x00;
      r.stp  = false;
      //todo: probably needs a SA-1 CPU reset
      //reset r.s, r.e, r.wai ...

      //reset io status
      //todo: reset timing is unknown, CIWP is set to 0 at reset
      mmio.ciwp = 0x00;
    }

    mmio.sa1_irq  = (data & 0x80);
    mmio.sa1_rdyb = (data & 0x40);
    mmio.sa1_resb = (data & 0x20);
    mmio.sa1_nmi  = (data & 0x10);
    mmio.smeg     = (data & 0x0f);

    if(mmio.sa1_irq) {
      mmio.sa1_irqfl = true;
      if(mmio.sa1_irqen) mmio.sa1_irqcl = 0;
    }

    if(mmio.sa1_nmi) {
      mmio.sa1_nmifl = true;
      if(mmio.sa1_nmien) mmio.sa1_nmicl = 0;
    }

    return;
  }

  //(SIE) S-CPU interrupt enable
  case 0x2201: {
    if(!mmio.cpu_irqen && (data & 0x80)) {
      if(mmio.cpu_irqfl) {
        mmio.cpu_irqcl = 0;
        cpu.irq(1);
      }
    }

    if(!mmio.chdma_irqen && (data & 0x20)) {
      if(mmio.chdma_irqfl) {
        mmio.chdma_irqcl = 0;
        cpu.irq(1);
      }
    }

    mmio.cpu_irqen   = (data & 0x80);
    mmio.chdma_irqen = (data & 0x20);
    return;
  }

  //(SIC) S-CPU interrupt clear
  case 0x2202: {
    mmio.cpu_irqcl   = (data & 0x80);
    mmio.chdma_irqcl = (data & 0x20);

    if(mmio.cpu_irqcl  ) mmio.cpu_irqfl   = false;
    if(mmio.chdma_irqcl) mmio.chdma_irqfl = false;

    if(!mmio.cpu_irqfl && !mmio.chdma_irqfl) cpu.irq(0);
    return;
  }

  //(CRV) SA-1 reset vector
  case 0x2203: { mmio.crv = (mmio.crv & 0xff00) | data; return; }
  case 0x2204: { mmio.crv = (data << 8) | (mmio.crv & 0xff); return; }

  //(CNV) SA-1 NMI vector
  case 0x2205: { mmio.cnv = (mmio.cnv & 0xff00) | data; return; }
  case 0x2206: { mmio.cnv = (data << 8) | (mmio.cnv & 0xff); return; }

  //(CIV) SA-1 IRQ vector
  case 0x2207: { mmio.civ = (mmio.civ & 0xff00) | data; return; }
  case 0x2208: { mmio.civ = (data << 8) | (mmio.civ & 0xff); return; }

  //(CXB) Super MMC bank C
  case 0x2220: {
    mmio.cbmode = (data & 0x80);
    mmio.cb     = (data & 0x07);
    return;
  }

  //(DXB) Super MMC bank D
  case 0x2221: {
    mmio.dbmode = (data & 0x80);
    mmio.db     = (data & 0x07);
    return;
  }

  //(EXB) Super MMC bank E
  case 0x2222: {
    mmio.ebmode = (data & 0x80);
    mmio.eb     = (data & 0x07);
    return;
  }

  //(FXB) Super MMC bank F
  case 0x2223: {
    mmio.fbmode = (data & 0x80);
    mmio.fb     = (data & 0x07);
    return;
  }

  //(BMAPS) S-CPU BW-RAM address mapping
  case 0x2224: {
    mmio.sbm = (data & 0x1f);
    return;
  }

  //(SWBE) S-CPU BW-RAM write enable
  case 0x2226: {
    mmio.swen = (data & 0x80);
    return;
  }

  //(BWPA) BW-RAM write-protected area
  case 0x2228: {
    mmio.bwp = (data & 0x0f);
    return;
  }

  //(SIWP) S-CPU I-RAM write protection
  case 0x2229: {
    mmio.siwp = data;
    return;
  }

  case 0x2231: case 0x2232: case 0x2233: case 0x2234: case 0x2235: case 0x2236: case 0x2237: {
    return writeIOShared(address, data);
  }

  }
}

auto SA1::writeIOSA1(uint address, uint8 data) -> void {
  synchronizeCPU();

  switch(0x2200 | address & 0x1ff) {

  //(SCNT) S-CPU control
  case 0x2209: {
    mmio.cpu_irq  = (data & 0x80);
    mmio.cpu_ivsw = (data & 0x40);
    mmio.cpu_nvsw = (data & 0x10);
    mmio.cmeg     = (data & 0x0f);

    if(mmio.cpu_irq) {
      mmio.cpu_irqfl = true;
      if(mmio.cpu_irqen) {
        mmio.cpu_irqcl = 0;
        cpu.irq(1);
      }
    }

    return;
  }

  //(CIE) SA-1 interrupt enable
  case 0x220a: {
    if(!mmio.sa1_irqen   && (data & 0x80) && mmio.sa1_irqfl  ) mmio.sa1_irqcl   = 0;
    if(!mmio.timer_irqen && (data & 0x40) && mmio.timer_irqfl) mmio.timer_irqcl = 0;
    if(!mmio.dma_irqen   && (data & 0x20) && mmio.dma_irqfl  ) mmio.dma_irqcl   = 0;
    if(!mmio.sa1_nmien   && (data & 0x10) && mmio.sa1_nmifl  ) mmio.sa1_nmicl   = 0;

    mmio.sa1_irqen   = (data & 0x80);
    mmio.timer_irqen = (data & 0x40);
    mmio.dma_irqen   = (data & 0x20);
    mmio.sa1_nmien   = (data & 0x10);
    return;
  }

  //(CIC) SA-1 interrupt clear
  case 0x220b: {
    mmio.sa1_irqcl   = (data & 0x80);
    mmio.timer_irqcl = (data & 0x40);
    mmio.dma_irqcl   = (data & 0x20);
    mmio.sa1_nmicl   = (data & 0x10);

    if(mmio.sa1_irqcl)   mmio.sa1_irqfl   = false;
    if(mmio.timer_irqcl) mmio.timer_irqfl = false;
    if(mmio.dma_irqcl)   mmio.dma_irqfl   = false;
    if(mmio.sa1_nmicl)   mmio.sa1_nmifl   = false;
    return;
  }

  //(SNV) S-CPU NMI vector
  case 0x220c: { mmio.snv = (mmio.snv & 0xff00) | data; return; }
  case 0x220d: { mmio.snv = (data << 8) | (mmio.snv & 0xff); return; }

  //(SIV) S-CPU IRQ vector
  case 0x220e: { mmio.siv = (mmio.siv & 0xff00) | data; return; }
  case 0x220f: { mmio.siv = (data << 8) | (mmio.siv & 0xff); return; }

  //(TMC) H/V timer control
  case 0x2210: {
    mmio.hvselb = (data & 0x80);
    mmio.ven    = (data & 0x02);
    mmio.hen    = (data & 0x01);
    return;
  }

  //(CTR) SA-1 timer restart
  case 0x2211: {
    status.vcounter = 0;
    status.hcounter = 0;
    return;
  }

  //(HCNT) H-count
  case 0x2212: { mmio.hcnt = (mmio.hcnt & 0xff00) | (data << 0); return; }
  case 0x2213: { mmio.hcnt = (mmio.hcnt & 0x00ff) | (data << 8); return; }

  //(VCNT) V-count
  case 0x2214: { mmio.vcnt = (mmio.vcnt & 0xff00) | (data << 0); return; }
  case 0x2215: { mmio.vcnt = (mmio.vcnt & 0x00ff) | (data << 8); return; }

  //(BMAP) SA-1 BW-RAM address mapping
  case 0x2225: {
    mmio.sw46 = (data & 0x80);
    mmio.cbm  = (data & 0x7f);
    return;
  }

  //(CWBE) SA-1 BW-RAM write enable
  case 0x2227: {
    mmio.cwen = (data & 0x80);
    return;
  }

  //(CIWP) SA-1 I-RAM write protection
  case 0x222a: {
    mmio.ciwp = data;
    return;
  }

  //(DCNT) DMA control
  case 0x2230: {
    mmio.dmaen = (data & 0x80);
    mmio.dprio = (data & 0x40);
    mmio.cden  = (data & 0x20);
    mmio.cdsel = (data & 0x10);
    mmio.dd    = (data & 0x04);
    mmio.sd    = (data & 0x03);

    if(mmio.dmaen == 0) dma.line = 0;
    return;
  }

  case 0x2231: case 0x2232: case 0x2233: case 0x2234: case 0x2235: case 0x2236: case 0x2237: {
    return writeIOShared(address, data);
  }

  //(DTC) DMA terminal counter
  case 0x2238: { mmio.dtc = (mmio.dtc & 0xff00) | (data << 0); return; }
  case 0x2239: { mmio.dtc = (mmio.dtc & 0x00ff) | (data << 8); return; }

  //(BBF) BW-RAM bitmap format
  case 0x223f: { mmio.bbf = (data & 0x80); return; }

  //(BRF) bitmap register files
  case 0x2240: { mmio.brf[ 0] = data; return; }
  case 0x2241: { mmio.brf[ 1] = data; return; }
  case 0x2242: { mmio.brf[ 2] = data; return; }
  case 0x2243: { mmio.brf[ 3] = data; return; }
  case 0x2244: { mmio.brf[ 4] = data; return; }
  case 0x2245: { mmio.brf[ 5] = data; return; }
  case 0x2246: { mmio.brf[ 6] = data; return; }
  case 0x2247: { mmio.brf[ 7] = data;
    if(mmio.dmaen) {
      if(mmio.cden == 1 && mmio.cdsel == 0) {
        dmaCC2();
      }
    }
    return;
  }
  case 0x2248: { mmio.brf[ 8] = data; return; }
  case 0x2249: { mmio.brf[ 9] = data; return; }
  case 0x224a: { mmio.brf[10] = data; return; }
  case 0x224b: { mmio.brf[11] = data; return; }
  case 0x224c: { mmio.brf[12] = data; return; }
  case 0x224d: { mmio.brf[13] = data; return; }
  case 0x224e: { mmio.brf[14] = data; return; }
  case 0x224f: { mmio.brf[15] = data;
    if(mmio.dmaen) {
      if(mmio.cden == 1 && mmio.cdsel == 0) {
        dmaCC2();
      }
    }
    return;
  }

  //(MCNT) arithmetic control
  case 0x2250: {
    mmio.acm = (data & 0x02);
    mmio.md  = (data & 0x01);

    if(mmio.acm) mmio.mr = 0;
    return;
  }

  //(MAL) multiplicand / dividend low
  case 0x2251: {
    mmio.ma = mmio.ma & ~0x00ff | data << 0;
    return;
  }

  //(MAH) multiplicand / dividend high
  case 0x2252: {
    mmio.ma = mmio.ma & ~0xff00 | data << 8;
    return;
  }

  //(MBL) multiplier / divisor low
  case 0x2253: {
    mmio.mb = mmio.mb & ~0x00ff | data << 0;
    return;
  }

  //(MBH) multiplier / divisor high
  //multiplication / cumulative sum only resets MB
  //division resets both MA and MB
  case 0x2254: {
    mmio.mb = mmio.mb & ~0xff00 | data << 8;

    if(mmio.acm == 0) {
      if(mmio.md == 0) {
        //signed multiplication
        mmio.mr = (uint32)((int16)mmio.ma * (int16)mmio.mb);
        mmio.mb = 0;
      } else {
        //signed division
        if(mmio.mb == 0) {
          mmio.mr = 0;
        } else {
          int16 dividend = mmio.ma;
          uint16 divisor = mmio.mb;
          //sa1 division rounds toward negative infinity, but C division rounds toward zero
          //adding divisor*65536 ensures it rounds down
          uint32 dividend_ext = dividend + (uint32)divisor*65536;
          uint16 remainder = dividend_ext % divisor;
          uint16 quotient = dividend_ext / divisor - 65536;
          mmio.mr = remainder << 16 | quotient;
        }
        mmio.ma = 0;
        mmio.mb = 0;
      }
    } else {
      //sigma (accumulative multiplication)
      mmio.mr += (int16)mmio.ma * (int16)mmio.mb;
      mmio.overflow = mmio.mr >> 40;
      mmio.mr = (uint40)mmio.mr;
      mmio.mb = 0;
    }
    return;
  }

  //(VBD) variable-length bit processing
  case 0x2258: {
    mmio.hl = (data & 0x80);
    mmio.vb = (data & 0x0f);
    if(mmio.vb == 0) mmio.vb = 16;

    if(mmio.hl == 0) {
      //fixed mode
      mmio.vbit += mmio.vb;
      mmio.va += (mmio.vbit >> 3);
      mmio.vbit &= 7;
    }
    return;
  }

  //(VDA) variable-length bit game pak ROM start address
  case 0x2259: { mmio.va = (mmio.va & 0xffff00) | (data <<  0); return; }
  case 0x225a: { mmio.va = (mmio.va & 0xff00ff) | (data <<  8); return; }
  case 0x225b: { mmio.va = (mmio.va & 0x00ffff) | (data << 16); mmio.vbit = 0; return; }

  }
}

auto SA1::writeIOShared(uint address, uint8 data) -> void {
  switch(0x2200 | address & 0x1ff) {

  //(CDMA) character conversion DMA parameters
  case 0x2231: {
    mmio.chdend  = (data & 0x80);
    mmio.dmasize = (data >> 2) & 7;
    mmio.dmacb   = (data & 0x03);

    if(mmio.chdend) bwram.dma = false;
    if(mmio.dmasize > 5) mmio.dmasize = 5;
    if(mmio.dmacb   > 2) mmio.dmacb   = 2;
    return;
  }

  //(SDA) DMA source device start address
  case 0x2232: { mmio.dsa = (mmio.dsa & 0xffff00) | (data <<  0); return; }
  case 0x2233: { mmio.dsa = (mmio.dsa & 0xff00ff) | (data <<  8); return; }
  case 0x2234: { mmio.dsa = (mmio.dsa & 0x00ffff) | (data << 16); return; }

  //(DDA) DMA destination start address
  case 0x2235: { mmio.dda = (mmio.dda & 0xffff00) | (data <<  0); return; }
  case 0x2236: { mmio.dda = (mmio.dda & 0xff00ff) | (data <<  8);
    if(mmio.dmaen) {
      if(mmio.cden == 0 && mmio.dd == DMA::DestIRAM) {
        dmaNormal();
      } else if(mmio.cden == 1 && mmio.cdsel == 1) {
        dmaCC1();
      }
    }
    return;
  }
  case 0x2237: { mmio.dda = (mmio.dda & 0x00ffff) | (data << 16);
    if(mmio.dmaen) {
      if(mmio.cden == 0 && mmio.dd == DMA::DestBWRAM) {
        dmaNormal();
      }
    }
    return;
  }

  }
}
