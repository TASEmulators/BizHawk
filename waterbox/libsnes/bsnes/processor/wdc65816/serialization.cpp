auto WDC65816::serialize(serializer& s) -> void {
  s.integer(r.pc.d);

  s.integer(r.a.w);
  s.integer(r.x.w);
  s.integer(r.y.w);
  s.integer(r.z.w);
  s.integer(r.s.w);
  s.integer(r.d.w);
  s.integer(r.b);

  s.integer(r.p.c);
  s.integer(r.p.z);
  s.integer(r.p.i);
  s.integer(r.p.d);
  s.integer(r.p.x);
  s.integer(r.p.m);
  s.integer(r.p.v);
  s.integer(r.p.n);

  s.integer(r.e);
  s.integer(r.irq);
  s.integer(r.wai);
  s.integer(r.stp);

  s.integer(r.vector);
  s.integer(r.mar);
  s.integer(r.mdr);

  s.integer(r.u.d);
  s.integer(r.v.d);
  s.integer(r.w.d);
}
