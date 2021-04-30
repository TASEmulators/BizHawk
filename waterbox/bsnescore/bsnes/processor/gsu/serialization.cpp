auto GSU::serialize(serializer& s) -> void {
  s.integer(regs.pipeline);
  s.integer(regs.ramaddr);

  for(auto n : range(16)) {
    s.integer(regs.r[n].data);
    s.integer(regs.r[n].modified);
  }

  s.integer(regs.sfr.data);
  s.integer(regs.pbr);
  s.integer(regs.rombr);
  s.integer(regs.rambr);
  s.integer(regs.cbr);
  s.integer(regs.scbr);

  s.integer(regs.scmr.ht);
  s.integer(regs.scmr.ron);
  s.integer(regs.scmr.ran);
  s.integer(regs.scmr.md);

  s.integer(regs.colr);

  s.integer(regs.por.obj);
  s.integer(regs.por.freezehigh);
  s.integer(regs.por.highnibble);
  s.integer(regs.por.dither);
  s.integer(regs.por.transparent);

  s.integer(regs.bramr);
  s.integer(regs.vcr);

  s.integer(regs.cfgr.irq);
  s.integer(regs.cfgr.ms0);

  s.integer(regs.clsr);

  s.integer(regs.romcl);
  s.integer(regs.romdr);

  s.integer(regs.ramcl);
  s.integer(regs.ramar);
  s.integer(regs.ramdr);

  s.integer(regs.sreg);
  s.integer(regs.dreg);

  s.array(cache.buffer);
  s.array(cache.valid);

  for(uint n : range(2)) {
    s.integer(pixelcache[n].offset);
    s.integer(pixelcache[n].bitpend);
    s.array(pixelcache[n].data);
  }
}
