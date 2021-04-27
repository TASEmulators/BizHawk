auto SPC7110::dataromRead(uint addr) -> uint8 {
  uint size = 1 << (r4834 & 3);  //size in MB
  uint mask = 0x100000 * size - 1;
  uint offset = addr & mask;
  if((r4834 & 3) != 3 && (addr & 0x400000)) return 0x00;
  return drom.read(Bus::mirror(offset, drom.size()));
}

auto SPC7110::dataOffset() -> uint { return r4811 | r4812 << 8 | r4813 << 16; }
auto SPC7110::dataAdjust() -> uint { return r4814 | r4815 << 8; }
auto SPC7110::dataStride() -> uint { return r4816 | r4817 << 8; }
auto SPC7110::setDataOffset(uint addr) -> void { r4811 = addr; r4812 = addr >> 8; r4813 = addr >> 16; }
auto SPC7110::setDataAdjust(uint addr) -> void { r4814 = addr; r4815 = addr >> 8; }

auto SPC7110::dataPortRead() -> void {
  uint offset = dataOffset();
  uint adjust = r4818 & 2 ? dataAdjust() : 0;
  if(r4818 & 8) adjust = (int16)adjust;
  r4810 = dataromRead(offset + adjust);
}

auto SPC7110::dataPortIncrement4810() -> void {
  uint offset = dataOffset();
  uint stride = r4818 & 1 ? dataStride() : 1;
  uint adjust = dataAdjust();
  if(r4818 & 4) stride = (int16)stride;
  if(r4818 & 8) adjust = (int16)adjust;
  if((r4818 & 16) == 0) setDataOffset(offset + stride);
  if((r4818 & 16) != 0) setDataAdjust(adjust + stride);
  dataPortRead();
}

auto SPC7110::dataPortIncrement4814() -> void {
  if(r4818 >> 5 != 1) return;
  uint offset = dataOffset();
  uint adjust = dataAdjust();
  if(r4818 & 8) adjust = (int16)adjust;
  setDataOffset(offset + adjust);
  dataPortRead();
}

auto SPC7110::dataPortIncrement4815() -> void {
  if(r4818 >> 5 != 2) return;
  uint offset = dataOffset();
  uint adjust = dataAdjust();
  if(r4818 & 8) adjust = (int16)adjust;
  setDataOffset(offset + adjust);
  dataPortRead();
}

auto SPC7110::dataPortIncrement481a() -> void {
  if(r4818 >> 5 != 3) return;
  uint offset = dataOffset();
  uint adjust = dataAdjust();
  if(r4818 & 8) adjust = (int16)adjust;
  setDataOffset(offset + adjust);
  dataPortRead();
}
