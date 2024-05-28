auto SI::dmaRead() -> void {
  pif.dmaRead(io.readAddress, io.dramAddress);
  io.dmaBusy = 0;
  io.pchState = 0;
  io.dmaState = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::SI);
}

auto SI::dmaWrite() -> void {
  pif.dmaWrite(io.writeAddress, io.dramAddress);
  io.dmaBusy = 0;
  io.pchState = 0;
  io.dmaState = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::SI);
}
