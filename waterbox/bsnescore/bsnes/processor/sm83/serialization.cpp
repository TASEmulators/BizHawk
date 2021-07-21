auto SM83::serialize(serializer& s) -> void {
  s.integer(r.af.word);
  s.integer(r.bc.word);
  s.integer(r.de.word);
  s.integer(r.hl.word);
  s.integer(r.sp.word);
  s.integer(r.pc.word);
  s.integer(r.ei);
  s.integer(r.halt);
  s.integer(r.stop);
  s.integer(r.ime);
}
