auto PPUcounter::serialize(serializer& s) -> void {
  s.integer(time.interlace);
  s.integer(time.field);
  s.integer(time.vperiod);
  s.integer(time.hperiod);
  s.integer(time.vcounter);
  s.integer(time.hcounter);

  s.integer(last.vperiod);
  s.integer(last.hperiod);
}
