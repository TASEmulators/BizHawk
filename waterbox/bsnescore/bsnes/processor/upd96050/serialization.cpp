auto uPD96050::serialize(serializer& s) -> void {
  s.array(dataRAM);
  regs.serialize(s);
  flags.a.serialize(s);
  flags.b.serialize(s);
}

auto uPD96050::Flag::serialize(serializer& s) -> void {
  s.boolean(ov0);
  s.boolean(ov1);
  s.boolean(z);
  s.boolean(c);
  s.boolean(s0);
  s.boolean(s1);
}

auto uPD96050::Status::serialize(serializer& s) -> void {
  s.boolean(p0);
  s.boolean(p1);
  s.boolean(ei);
  s.boolean(sic);
  s.boolean(soc);
  s.boolean(drc);
  s.boolean(dma);
  s.boolean(drs);
  s.boolean(usf0);
  s.boolean(usf1);
  s.boolean(rqm);
  s.boolean(siack);
  s.boolean(soack);
}

auto uPD96050::Registers::serialize(serializer& s) -> void {
  s.array(stack);
  s.integer(pc);
  s.integer(rp);
  s.integer(dp);
  s.integer(sp);
  s.integer(si);
  s.integer(so);
  s.integer(k);
  s.integer(l);
  s.integer(m);
  s.integer(n);
  s.integer(a);
  s.integer(b);
  s.integer(tr);
  s.integer(trb);
  s.integer(dr);
  sr.serialize(s);
}
