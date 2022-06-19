auto PI::serialize(serializer& s) -> void {
  s(io.dmaBusy);
  s(io.ioBusy);
  s(io.error);
  s(io.interrupt);
  s(io.dramAddress);
  s(io.pbusAddress);
  s(io.readLength);
  s(io.writeLength);
  s(io.busLatch);

  s(bsd1.latency);
  s(bsd1.pulseWidth);
  s(bsd1.pageSize);
  s(bsd1.releaseDuration);

  s(bsd2.latency);
  s(bsd2.pulseWidth);
  s(bsd2.pageSize);
  s(bsd2.releaseDuration);
}
