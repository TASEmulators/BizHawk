struct Mbc3 : Mbc {
  explicit Mbc3(Memory::Readable& rom_, Memory::Writable& ram_, bool rtc) : Mbc(rom_, ram_), hasRtc(rtc) { reset(); }

  inline auto rtcUpdate() -> void {
    u64 diff = rtcCallback() - lastTime;
    lastTime += diff;
    if(!rtcHalt) {
      s8 seconds = rtcSeconds;
      if(seconds > 60) seconds -= 0x40;
      s8 minutes = rtcMinutes;
      if(minutes > 60) minutes -= 0x40;
      s8 hours = rtcHours;
      if(hours > 24) hours -= 0x20;
      n64 days = rtcDays;

      seconds += diff % 60;
      if(seconds >= 60) {
        minutes++;
        seconds -= 60;
      }
      diff /= 60;
      minutes += diff % 60;
      if(minutes >= 60) {
        hours++;
        minutes -= 60;
      }
      diff /= 60;
      hours += diff % 24;
      if(hours >= 24) {
        days++;
        hours -= 24;
      }
      days += diff / 24;

      if(seconds < 0) seconds += 0x40;
      rtcSeconds = seconds;
      if(minutes < 0) minutes += 0x40;
      rtcMinutes = minutes;
      if(hours < 0) hours += 0x20;
      rtcHours = hours;
      rtcDays = days;
      rtcOverflow = rtcOverflow || days >= 512;
    }
  }

  inline auto rtcWriteReg(n8 data) -> void {
    rtcUpdate();
    switch(ramBank) {
      case 0x08:
        rtcSeconds = data;
        return;
      case 0x09:
        rtcMinutes = data;
        return;
      case 0x0a:
        rtcHours = data;
        return;
      case 0x0b:
        rtcDays.bit(0,7) = data;
        return;
      case 0x0c:
        rtcDays.bit(8) = data.bit(0);
        rtcHalt = data.bit(6);
        rtcOverflow = data.bit(7);
        return;
    }
  }

  inline auto rtcLatchRegs() -> void {
    rtcUpdate();
    rtcLatches[0] = rtcSeconds;
    rtcLatches[1] = rtcMinutes;
    rtcLatches[2] = rtcHours;
    rtcLatches[3] = rtcDays;
    rtcLatches[4].bit(0) = rtcDays.bit(8);
    rtcLatches[4].bit(6) = rtcHalt;
    rtcLatches[4].bit(7) = rtcOverflow;
  }

  inline auto rtcLatchActive() -> n8 {
    switch(ramBank) {
      case 0x08 ... 0x0c: return rtcLatches[ramBank - 0x08];
      default: return 0xff;
    }
  }

  auto reset() -> void override {
    romBank = 1;
    ramBank = 0;
    ramEnable = 0;
  }

  auto read(u16 address) -> u8 override {
    static constexpr u8 unmapped = 0xff;
    switch(address) {
      case 0x0000 ... 0x3fff:
        return rom.read<Byte>(address);
      case 0x4000 ... 0x7fff:
        return rom.read<Byte>(romBank * 0x4000 + address - 0x4000);
      case 0xa000 ... 0xbfff:
        if(!ramEnable) return unmapped;
        if(hasRtc && ramBank > 0x03) return rtcLatchActive();
        return ram.read<Byte>(ramBank * 0x2000 + address - 0xa000);
      default:
        return unmapped;
    }
  }

  auto write(u16 address, u8 data_) -> void override {
    n8 data = data_;
    switch(address) {
      case 0x0000 ... 0x1fff:
        ramEnable = data.bit(0,3) == 0x0a;
        return;
      case 0x2000 ... 0x3fff:
        romBank = data.bit(0,6);
        if(!romBank) romBank = 1;
        return;
      case 0x4000 ... 0x5fff:
        ramBank = data.bit(0, 2 + hasRtc);
        return;
      case 0x6000 ... 0x7fff:
        if(hasRtc) rtcLatchRegs();
        return;
      case 0xa000 ... 0xbfff:
        if(ramEnable) {
          if(hasRtc && ramBank > 0x03) rtcWriteReg(data);
          else ram.write<Byte>(ramBank * 0x2000 + address - 0xa000, data);
        }
        return;
    }
  }

private:
  n7 romBank;
  n4 ramBank;
  n1 ramEnable;
  b1 hasRtc;

  n1 rtcOverflow = 0;
  n1 rtcHalt = 0;
  n9 rtcDays = 0;
  n5 rtcHours = 0;
  n6 rtcMinutes = 0;
  n6 rtcSeconds = 0;
  n8 rtcLatches[5] = {};

  u64 lastTime = 0;
public:
  std::function<u64()> rtcCallback = []() { return 0; };
};
