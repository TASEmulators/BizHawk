auto HitachiDSP::firmware() const -> vector<uint8> {
  vector<uint8> buffer;
  if(!cartridge.has.HitachiDSP) return buffer;
  buffer.reserve(1024 * 3);
  for(auto n : range(1024)) {
    buffer.append(dataROM[n] >>  0);
    buffer.append(dataROM[n] >>  8);
    buffer.append(dataROM[n] >> 16);
  }
  return buffer;
}

auto HitachiDSP::serialize(serializer& s) -> void {
  HG51B::serialize(s);
  Thread::serialize(s);
}
