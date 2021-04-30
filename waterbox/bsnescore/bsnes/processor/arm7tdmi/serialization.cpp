auto ARM7TDMI::serialize(serializer& s) -> void {
  processor.serialize(s);
  pipeline.serialize(s);
  s.boolean(carry);
  s.boolean(irq);
}

auto ARM7TDMI::Processor::serialize(serializer& s) -> void {
  s.integer(r0.data);
  s.integer(r1.data);
  s.integer(r2.data);
  s.integer(r3.data);
  s.integer(r4.data);
  s.integer(r5.data);
  s.integer(r6.data);
  s.integer(r7.data);
  s.integer(r8.data);
  s.integer(r9.data);
  s.integer(r10.data);
  s.integer(r11.data);
  s.integer(r12.data);
  s.integer(r13.data);
  s.integer(r14.data);
  s.integer(r15.data);
  cpsr.serialize(s);
  s.integer(fiq.r8.data);
  s.integer(fiq.r9.data);
  s.integer(fiq.r10.data);
  s.integer(fiq.r11.data);
  s.integer(fiq.r12.data);
  s.integer(fiq.r13.data);
  s.integer(fiq.r14.data);
  fiq.spsr.serialize(s);
  s.integer(irq.r13.data);
  s.integer(irq.r14.data);
  irq.spsr.serialize(s);
  s.integer(svc.r13.data);
  s.integer(svc.r14.data);
  svc.spsr.serialize(s);
  s.integer(abt.r13.data);
  s.integer(abt.r14.data);
  abt.spsr.serialize(s);
  s.integer(und.r13.data);
  s.integer(und.r14.data);
  und.spsr.serialize(s);
}

auto ARM7TDMI::PSR::serialize(serializer& s) -> void {
  s.integer(m);
  s.boolean(t);
  s.boolean(f);
  s.boolean(i);
  s.boolean(v);
  s.boolean(c);
  s.boolean(z);
  s.boolean(n);
}

auto ARM7TDMI::Pipeline::serialize(serializer& s) -> void {
  s.integer(reload);
  s.integer(nonsequential);
  s.integer(fetch.address);
  s.integer(fetch.instruction);
  s.boolean(fetch.thumb);
  s.integer(decode.address);
  s.integer(decode.instruction);
  s.boolean(decode.thumb);
  s.integer(execute.address);
  s.integer(execute.instruction);
  s.boolean(execute.thumb);
}
