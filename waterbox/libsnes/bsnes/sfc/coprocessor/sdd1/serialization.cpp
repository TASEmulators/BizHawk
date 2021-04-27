auto SDD1::serialize(serializer& s) -> void {
  s.integer(r4800);
  s.integer(r4801);
  s.integer(r4804);
  s.integer(r4805);
  s.integer(r4806);
  s.integer(r4807);

  for(auto& channel : dma) {
    s.integer(channel.addr);
    s.integer(channel.size);
  }
  s.integer(dmaReady);

  decompressor.serialize(s);
}

auto SDD1::Decompressor::serialize(serializer& s) -> void {
  im.serialize(s);
  gcd.serialize(s);
  bg0.serialize(s);
  bg1.serialize(s);
  bg2.serialize(s);
  bg3.serialize(s);
  bg4.serialize(s);
  bg5.serialize(s);
  bg6.serialize(s);
  bg7.serialize(s);
  pem.serialize(s);
  cm.serialize(s);
  ol.serialize(s);
}

auto SDD1::Decompressor::IM::serialize(serializer& s) -> void {
  s.integer(offset);
  s.integer(bitCount);
}

auto SDD1::Decompressor::GCD::serialize(serializer& s) -> void {
}

auto SDD1::Decompressor::BG::serialize(serializer& s) -> void {
  s.integer(mpsCount);
  s.integer(lpsIndex);
}

auto SDD1::Decompressor::PEM::serialize(serializer& s) -> void {
  for(auto& info : contextInfo) {
    s.integer(info.status);
    s.integer(info.mps);
  }
}

auto SDD1::Decompressor::CM::serialize(serializer& s) -> void {
  s.integer(bitplanesInfo);
  s.integer(contextBitsInfo);
  s.integer(bitNumber);
  s.integer(currentBitplane);
  s.array(previousBitplaneBits);
}

auto SDD1::Decompressor::OL::serialize(serializer& s) -> void {
  s.integer(bitplanesInfo);
  s.integer(r0);
  s.integer(r1);
  s.integer(r2);
}
