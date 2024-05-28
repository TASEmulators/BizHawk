auto MI::serialize(serializer& s) -> void {
  s(irq.sp.line);
  s(irq.sp.mask);
  s(irq.si.line);
  s(irq.si.mask);
  s(irq.ai.line);
  s(irq.ai.mask);
  s(irq.vi.line);
  s(irq.vi.mask);
  s(irq.pi.line);
  s(irq.pi.mask);
  s(irq.dp.line);
  s(irq.dp.mask);

  s(io.initializeLength);
  s(io.initializeMode);
  s(io.ebusTestMode);
  s(io.rdramRegisterSelect);
}
