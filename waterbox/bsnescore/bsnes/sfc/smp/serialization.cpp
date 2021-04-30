auto SMP::serialize(serializer& s) -> void {
  SPC700::serialize(s);
  Thread::serialize(s);

  s.integer(io.clockCounter);
  s.integer(io.dspCounter);

  s.integer(io.apu0);
  s.integer(io.apu1);
  s.integer(io.apu2);
  s.integer(io.apu3);

  s.integer(io.timersDisable);
  s.integer(io.ramWritable);
  s.integer(io.ramDisable);
  s.integer(io.timersEnable);
  s.integer(io.externalWaitStates);
  s.integer(io.internalWaitStates);

  s.integer(io.iplromEnable);

  s.integer(io.dspAddr);

  s.integer(io.cpu0);
  s.integer(io.cpu1);
  s.integer(io.cpu2);
  s.integer(io.cpu3);

  s.integer(io.aux4);
  s.integer(io.aux5);

  s.integer(timer0.stage0);
  s.integer(timer0.stage1);
  s.integer(timer0.stage2);
  s.integer(timer0.stage3);
  s.boolean(timer0.line);
  s.boolean(timer0.enable);
  s.integer(timer0.target);

  s.integer(timer1.stage0);
  s.integer(timer1.stage1);
  s.integer(timer1.stage2);
  s.integer(timer1.stage3);
  s.boolean(timer1.line);
  s.boolean(timer1.enable);
  s.integer(timer1.target);

  s.integer(timer2.stage0);
  s.integer(timer2.stage1);
  s.integer(timer2.stage2);
  s.integer(timer2.stage3);
  s.boolean(timer2.line);
  s.boolean(timer2.enable);
  s.integer(timer2.target);
}
