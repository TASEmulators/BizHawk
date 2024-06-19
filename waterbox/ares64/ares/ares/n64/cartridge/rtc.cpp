auto Cartridge::RTC::power(bool reset) -> void {
  if(present) run(!status.bit(7));
}

auto Cartridge::RTC::load() -> void {
  if(auto fp = self.pak->read("save.rtc")) {
    ram.allocate(fp->size());
    ram.load(fp);
  
    present = 1;
    n64 timestamp = ram.read<Dual>(24);
    if(!~timestamp) {
      time_t t = (time_t)0;
      struct tm tmm = *gmtime(&t);
      ram.write<Byte>(16, BCD::encode(tmm.tm_sec));
      ram.write<Byte>(17, BCD::encode(tmm.tm_min));
      ram.write<Byte>(18, BCD::encode(tmm.tm_hour) | 0x80);
      ram.write<Byte>(19, BCD::encode(tmm.tm_mday));
      ram.write<Byte>(20, BCD::encode(tmm.tm_wday));
      ram.write<Byte>(21, BCD::encode(tmm.tm_mon + 1));
      ram.write<Byte>(22, BCD::encode(tmm.tm_year % 100));
      ram.write<Byte>(23, BCD::encode(tmm.tm_year / 100));
    }

    timestamp = platform->time() - timestamp;
    advance(timestamp);
  }
}

auto Cartridge::RTC::save() -> void {
#if false
  if(auto fp = self.pak->write("save.rtc")) {
    ram.write<Dual>(24, time(0));
    ram.save(fp);
  }
#endif
}

auto Cartridge::RTC::tick(int nsec) -> void {
  advance(nsec);
  run(true);
}

auto Cartridge::RTC::run(bool run) -> void {
  status.bit(7) = !run;
  queue.remove(Queue::RTC_Tick);
  if(run) queue.insert(Queue::RTC_Tick, 187'500'000);
}

auto Cartridge::RTC::running() -> bool {
  return !status.bit(7);
}

auto Cartridge::RTC::advance(int nsec) -> void {
  auto seconds = BCD::decode(ram.read<Byte>(16));
  auto minutes = BCD::decode(ram.read<Byte>(17));
  auto hours   = BCD::decode(ram.read<Byte>(18) & 0x7f);
  auto day     = BCD::decode(ram.read<Byte>(19));
  auto wday    = BCD::decode(ram.read<Byte>(20));
  auto month   = BCD::decode(ram.read<Byte>(21));
  auto year    = BCD::decode(ram.read<Byte>(22)) + 100 * BCD::decode(ram.read<Byte>(23));

  while(nsec--) {
    if(++seconds == 60) {
      seconds = 0;
      if(++minutes == 60) {
        minutes = 0;
        if(++hours == 24) {
          hours = 0;
          if(++wday == 7) wday = 0;
          if(++day > chrono::daysInMonth(month, year)) {
            day = 1;
            if(++month == 13) {
              month = 1;
              year++;
            }
          }
        }
      }
    }
  }

  ram.write<Byte>(16, BCD::encode(seconds));
  ram.write<Byte>(17, BCD::encode(minutes));
  ram.write<Byte>(18, BCD::encode(hours) | 0x80);
  ram.write<Byte>(19, BCD::encode(day));
  ram.write<Byte>(20, BCD::encode(wday));
  ram.write<Byte>(21, BCD::encode(month));
  ram.write<Byte>(22, BCD::encode(year % 100));
  ram.write<Byte>(23, BCD::encode(year / 100));
}

auto Cartridge::RTC::read(u2 block, n8* data) -> void {
  data[0] = ram.read<Byte>(block*8 + 0);
  data[1] = ram.read<Byte>(block*8 + 1);
  data[2] = ram.read<Byte>(block*8 + 2);
  data[3] = ram.read<Byte>(block*8 + 3);
  data[4] = ram.read<Byte>(block*8 + 4);
  data[5] = ram.read<Byte>(block*8 + 5);
  data[6] = ram.read<Byte>(block*8 + 6);
  data[7] = ram.read<Byte>(block*8 + 7);
}

auto Cartridge::RTC::write(u2 block, n8* data) -> void {
  if (writeLock.bit(block)) return;
  ram.write<Byte>(block*8 + 0, data[0]);
  ram.write<Byte>(block*8 + 1, data[1]);
  ram.write<Byte>(block*8 + 2, data[2]);
  ram.write<Byte>(block*8 + 3, data[3]);
  ram.write<Byte>(block*8 + 4, data[4]);
  ram.write<Byte>(block*8 + 5, data[5]);
  ram.write<Byte>(block*8 + 6, data[6]);
  ram.write<Byte>(block*8 + 7, data[7]);
  if(block == 0) {
    n16 control = ram.read<Half>(0);
    writeLock.bit(1,2) = control.bit(8,9);
    run(!control.bit(2));
  }
}

auto Cartridge::RTC::serialize(serializer& s) -> void {
  s(ram);
  s(status);
  s(writeLock);
}
