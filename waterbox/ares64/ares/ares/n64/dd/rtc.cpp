auto DD::rtcLoad() -> void {
#if false
  if(auto fp = system.pak->read("time.rtc")) {
    rtc.load(fp);
  }
#endif

  n64 check = 0;
  for(auto n : range(8)) check.byte(n) = rtc.read<Byte>(n);
  if(!~check) return;  //new save file

  n64 timestamp = 0;
  for(auto n : range(8)) timestamp.byte(n) = rtc.read<Byte>(8 + n);
  if(!~timestamp) return;  //new save file

  timestamp = platform->time() - timestamp;
  while(timestamp--) rtcTickSecond();
}

auto DD::rtcSave() -> void {
  n64 timestamp = platform->time();
  for(auto n : range(8)) rtc.write<Byte>(8 + n, timestamp.byte(n));

#if false
  if(auto fp = system.pak->write("time.rtc")) {
    rtc.save(fp);
  }
#endif
}

auto DD::rtcTick(u32 offset) -> void {
  u8 n = rtc.read<Byte>(offset);
  if((++n & 0xf) > 9) n = (n & 0xf0) + 0x10;
  if((n & 0xf0) > 0x90) n = 0;
  rtc.write<Byte>(offset, n);
}

auto DD::rtcTickClock() -> void {
  rtcTickSecond();
  queue.remove(Queue::DD_Clock_Tick);
  queue.insert(Queue::DD_Clock_Tick, 187'500'000);
}

auto DD::rtcTickSecond() -> void {
  //second
  rtcTick(5);
  if(rtc.read<Byte>(5) < 0x60) return;
  rtc.write<Byte>(5, 0);

  //minute
  rtcTick(4);
  if(rtc.read<Byte>(4) < 0x60) return;
  rtc.write<Byte>(4, 0);

  //hour
  rtcTick(3);
  if(rtc.read<Byte>(3) < 0x24) return;
  rtc.write<Byte>(3, 0);

  //day
  u32 daysInMonth[12] = {0x31, 0x28, 0x31, 0x30, 0x31, 0x30, 0x31, 0x31, 0x30, 0x31, 0x30, 0x31};
  if(rtc.read<Byte>(0) && !(BCD::decode(rtc.read<Byte>(0)) % 4)) daysInMonth[1]++;

  rtcTick(2);
  if(rtc.read<Byte>(2) <= daysInMonth[BCD::decode(rtc.read<Byte>(1))-1]) return;
  rtc.write<Byte>(2, 1);

  //month
  rtcTick(1);
  if(rtc.read<Byte>(1) < 0x12) return;
  rtc.write<Byte>(1, 1);

  //year
  rtcTick(0);
}
