auto MCC::serialize(serializer& s) -> void {
  s.array(psram.data(), psram.size());

  s.integer(irq.flag);
  s.integer(irq.enable);

  s.integer(r.mapping);
  s.integer(r.psramEnableLo);
  s.integer(r.psramEnableHi);
  s.integer(r.psramMapping);
  s.integer(r.romEnableLo);
  s.integer(r.romEnableHi);
  s.integer(r.exEnableLo);
  s.integer(r.exEnableHi);
  s.integer(r.exMapping);
  s.integer(r.internallyWritable);
  s.integer(r.externallyWritable);

  s.integer(w.mapping);
  s.integer(w.psramEnableLo);
  s.integer(w.psramEnableHi);
  s.integer(w.psramMapping);
  s.integer(w.romEnableLo);
  s.integer(w.romEnableHi);
  s.integer(w.exEnableLo);
  s.integer(w.exEnableHi);
  s.integer(w.exMapping);
  s.integer(w.internallyWritable);
  s.integer(w.externallyWritable);
}
