auto EpsonRTC::serialize(serializer& s) -> void {
  Thread::serialize(s);

  s.integer(clocks);
  s.integer(seconds);

  s.integer(chipselect);
  s.integer((uint&)state);
  s.integer(mdr);
  s.integer(offset);
  s.integer(wait);
  s.integer(ready);
  s.integer(holdtick);

  s.integer(secondlo);
  s.integer(secondhi);
  s.integer(batteryfailure);

  s.integer(minutelo);
  s.integer(minutehi);
  s.integer(resync);

  s.integer(hourlo);
  s.integer(hourhi);
  s.integer(meridian);

  s.integer(daylo);
  s.integer(dayhi);
  s.integer(dayram);

  s.integer(monthlo);
  s.integer(monthhi);
  s.integer(monthram);

  s.integer(yearlo);
  s.integer(yearhi);

  s.integer(weekday);

  s.integer(hold);
  s.integer(calendar);
  s.integer(irqflag);
  s.integer(roundseconds);

  s.integer(irqmask);
  s.integer(irqduty);
  s.integer(irqperiod);

  s.integer(pause);
  s.integer(stop);
  s.integer(atime);
  s.integer(test);
}
