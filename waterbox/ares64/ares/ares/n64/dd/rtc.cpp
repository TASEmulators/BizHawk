auto DD::RTC::load() -> void {
  ram.allocate(0x10);
#if false
  if(auto fp = system.pak->read("time.rtc")) {
    ram.load(fp);
  }
#endif

  //byte 0 to 7 = raw rtc time (last updated, only 6 bytes are used)
  n64 check = 0;
  for(auto n : range(8)) check.byte(n) = ram.read<Byte>(n);
  if(!~check) return;  //new save file

  //check for invalid time info, if invalid, set time info to something invalid and ignore the rest
  if (!valid()) {
    for(auto n : range(8)) ram.write<Byte>(n, 0xff);
    return;
  }

  //byte 8 to 15 = timestamp of when the last save was made
  n64 timestamp = 0;
  for(auto n : range(8)) timestamp.byte(n) = ram.read<Byte>(8 + n);
  if(!~timestamp) return;  //new save file

  //update based on the amount of time that has passed since the last save
  timestamp = platform->time() - timestamp;
  while(timestamp--) tickSecond();
}

auto DD::RTC::reset() -> void {
  ram.reset();
}

auto DD::RTC::save() -> void {
#if false
  n64 timestamp = platform->time();
  for(auto n : range(8)) ram.write<Byte>(8 + n, timestamp.byte(n));

  if(auto fp = system.pak->write("time.rtc")) {
    ram.save(fp);
  }
#endif
}

auto DD::RTC::serialize(serializer& s) -> void {
  s(ram);
}

auto DD::RTC::tick(u32 offset) -> void {
  u8 n = ram.read<Byte>(offset);
  if((++n & 0xf) > 9) n = (n & 0xf0) + 0x10;
  if((n & 0xf0) > 0x90) n = 0;
  ram.write<Byte>(offset, n);
}

auto DD::RTC::tickClock() -> void {
  tickSecond();
  queue.remove(Queue::DD_Clock_Tick);
  queue.insert(Queue::DD_Clock_Tick, 187'500'000);
}

auto DD::RTC::tickSecond() -> void {
  if (!valid()) return;

  //second
  tick(5);
  if(ram.read<Byte>(5) < 0x60) return;
  ram.write<Byte>(5, 0);

  //minute
  tick(4);
  if(ram.read<Byte>(4) < 0x60) return;
  ram.write<Byte>(4, 0);

  //hour
  tick(3);
  if(ram.read<Byte>(3) < 0x24) return;
  ram.write<Byte>(3, 0);

  //day
  tick(2);
  if(ram.read<Byte>(2) <= BCD::encode(chrono::daysInMonth(BCD::decode(ram.read<Byte>(1)), BCD::decode(ram.read<Byte>(0))))) return;
  ram.write<Byte>(2, 1);

  //month
  tick(1);
  if(ram.read<Byte>(1) <= 0x12) return;
  ram.write<Byte>(1, 1);

  //year
  tick(0);
}

auto DD::RTC::valid() -> bool {
  //check validity of ram rtc data (if it's BCD valid or not)
  for(auto n : range(6)) {
    if((ram.read<Byte>(n) & 0x0f) >= 0x0a) return false;
  }

  //check for valid values of each byte
  //year
  if(ram.read<Byte>(0) >= 0xa0) return false;
  //second
  if(ram.read<Byte>(5) >= 0x60) return false;
  //minute
  if(ram.read<Byte>(4) >= 0x60) return false;
  //hour
  if(ram.read<Byte>(3) >= 0x24) return false;
  //month
  if(ram.read<Byte>(1) > 0x12) return false;
  if(ram.read<Byte>(1) < 1) return false;
  //day
  if(ram.read<Byte>(2) < 1) return false;
  if(ram.read<Byte>(2) > BCD::encode(chrono::daysInMonth(BCD::decode(ram.read<Byte>(1)), BCD::decode(ram.read<Byte>(0))))) return false;

  //everything is valid
  return true;
}

