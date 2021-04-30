auto NECDSP::firmware() const -> vector<uint8> {
  vector<uint8> buffer;
  if(!cartridge.has.NECDSP) return buffer;
  uint plength = 2048, dlength = 1024;
  if(revision == Revision::uPD96050) plength = 16384, dlength = 2048;
  buffer.reserve(plength * 3 + dlength * 2);

  for(auto n : range(plength)) {
    buffer.append(programROM[n] >>  0);
    buffer.append(programROM[n] >>  8);
    buffer.append(programROM[n] >> 16);
  }

  for(auto n : range(dlength)) {
    buffer.append(dataROM[n] >> 0);
    buffer.append(dataROM[n] >> 8);
  }

  return buffer;
}

auto NECDSP::serialize(serializer& s) -> void {
  uPD96050::serialize(s);
  Thread::serialize(s);
}
