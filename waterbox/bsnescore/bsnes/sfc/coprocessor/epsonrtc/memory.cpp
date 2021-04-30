auto EpsonRTC::rtcReset() -> void {
  state = State::Mode;
  offset = 0;

  resync = 0;
  pause = 0;
  test = 0;
}

auto EpsonRTC::rtcRead(uint4 addr) -> uint4 {
  switch(addr) { default:
  case  0: return secondlo;
  case  1: return secondhi | batteryfailure << 3;
  case  2: return minutelo;
  case  3: return minutehi | resync << 3;
  case  4: return hourlo;
  case  5: return hourhi | meridian << 2 | resync << 3;
  case  6: return daylo;
  case  7: return dayhi | dayram << 2 | resync << 3;
  case  8: return monthlo;
  case  9: return monthhi | monthram << 1 | resync << 3;
  case 10: return yearlo;
  case 11: return yearhi;
  case 12: return weekday | resync << 3;
  case 13: {
    uint1 readflag = irqflag & !irqmask;
    irqflag = 0;
    return hold | calendar << 1 | readflag << 2 | roundseconds << 3;
  }
  case 14: return irqmask | irqduty << 1 | irqperiod << 2;
  case 15: return pause | stop << 1 | atime << 2 | test << 3;
  }
}

auto EpsonRTC::rtcWrite(uint4 addr, uint4 data) -> void {
  switch(addr) {
  case 0:
    secondlo = data;
    break;
  case 1:
    secondhi = data;
    batteryfailure = data >> 3;
    break;
  case 2:
    minutelo = data;
    break;
  case 3:
    minutehi = data;
    break;
  case 4:
    hourlo = data;
    break;
  case 5:
    hourhi = data;
    meridian = data >> 2;
    if(atime == 1) meridian = 0;
    if(atime == 0) hourhi &= 1;
    break;
  case 6:
    daylo = data;
    break;
  case 7:
    dayhi = data;
    dayram = data >> 2;
    break;
  case 8:
    monthlo = data;
    break;
  case 9:
    monthhi = data;
    monthram = data >> 1;
    break;
  case 10:
    yearlo = data;
    break;
  case 11:
    yearhi = data;
    break;
  case 12:
    weekday = data;
    break;
  case 13: {
    bool held = hold;
    hold = data;
    calendar = data >> 1;
    roundseconds = data >> 3;
    if(held == 1 && hold == 0 && holdtick == 1) {
      //if a second has passed during hold, increment one second upon resuming
      holdtick = 0;
      tickSecond();
    }
  } break;
  case 14:
    irqmask = data;
    irqduty = data >> 1;
    irqperiod = data >> 2;
    break;
  case 15:
    pause = data;
    stop = data >> 1;
    atime = data >> 2;
    test = data >> 3;
    if(atime == 1) meridian = 0;
    if(atime == 0) hourhi &= 1;
    if(pause) {
      secondlo = 0;
      secondhi = 0;
    }
    break;
  }
}

auto EpsonRTC::load(const uint8* data) -> void {
  secondlo = data[0] >> 0;
  secondhi = data[0] >> 4;
  batteryfailure = data[0] >> 7;

  minutelo = data[1] >> 0;
  minutehi = data[1] >> 4;
  resync = data[1] >> 7;

  hourlo = data[2] >> 0;
  hourhi = data[2] >> 4;
  meridian = data[2] >> 6;

  daylo = data[3] >> 0;
  dayhi = data[3] >> 4;
  dayram = data[3] >> 6;

  monthlo = data[4] >> 0;
  monthhi = data[4] >> 4;
  monthram = data[4] >> 5;

  yearlo = data[5] >> 0;
  yearhi = data[5] >> 4;

  weekday = data[6] >> 0;

  hold = data[6] >> 4;
  calendar = data[6] >> 5;
  irqflag = data[6] >> 6;
  roundseconds = data[6] >> 7;

  irqmask = data[7] >> 0;
  irqduty = data[7] >> 1;
  irqperiod = data[7] >> 2;

  pause = data[7] >> 4;
  stop = data[7] >> 5;
  atime = data[7] >> 6;
  test = data[7] >> 7;

  uint64 timestamp = 0;
  for(auto byte : range(8)) {
    timestamp |= data[8 + byte] << (byte * 8);
  }

  uint64 diff = (uint64)time(0) - timestamp;
  while(diff >= 60 * 60 * 24) { tickDay(); diff -= 60 * 60 * 24; }
  while(diff >= 60 * 60) { tickHour(); diff -= 60 * 60; }
  while(diff >= 60) { tickMinute(); diff -= 60; }
  while(diff--) tickSecond();
}

auto EpsonRTC::save(uint8* data) -> void {
  data[0] = secondlo << 0 | secondhi << 4 | batteryfailure << 7;
  data[1] = minutelo << 0 | minutehi << 4 | resync << 7;
  data[2] = hourlo << 0 | hourhi << 4 | meridian << 6 | resync << 7;
  data[3] = daylo << 0 | dayhi << 4 | dayram << 6 | resync << 7;
  data[4] = monthlo << 0 | monthhi << 4 | monthram << 5 | resync << 7;
  data[5] = yearlo << 0 | yearhi << 4;
  data[6] = weekday << 0 | resync << 3 | hold << 4 | calendar << 5 | irqflag << 6 | roundseconds << 7;
  data[7] = irqmask << 0 | irqduty << 1 | irqperiod << 2 | pause << 4 | stop << 5 | atime << 6 | test << 7;

  uint64 timestamp = (uint64)time(0);
  for(auto byte : range(8)) {
    data[8 + byte] = timestamp;
    timestamp >>= 8;
  }
}
