auto AI::serialize(serializer& s) -> void {
  Thread::serialize(s);

  s(fifo[0].address);
  s(fifo[1].address);

  s(io.dmaEnable);
  s(io.dmaAddress);
  s(io.dmaLength);
  s(io.dmaCount);
  s(io.dacRate);
  s(io.bitRate);

  s(dac.frequency);
  s(dac.precision);
  s(dac.period);
}
