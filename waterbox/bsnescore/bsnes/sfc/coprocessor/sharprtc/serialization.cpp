auto SharpRTC::serialize(serializer& s) -> void {
  Thread::serialize(s);

  s.integer((uint&)state);
  s.integer(index);

  s.integer(second);
  s.integer(minute);
  s.integer(hour);
  s.integer(day);
  s.integer(month);
  s.integer(year);
  s.integer(weekday);
}
