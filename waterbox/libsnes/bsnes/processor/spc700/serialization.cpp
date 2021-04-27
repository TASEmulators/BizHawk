auto SPC700::serialize(serializer& s) -> void {
  s.integer(r.pc.w);
  s.integer(r.ya.w);
  s.integer(r.x);
  s.integer(r.s);
  s.integer(r.p.c);
  s.integer(r.p.z);
  s.integer(r.p.i);
  s.integer(r.p.h);
  s.integer(r.p.b);
  s.integer(r.p.p);
  s.integer(r.p.v);
  s.integer(r.p.n);

  s.integer(r.wait);
  s.integer(r.stop);
}
