#include "decompressor.cpp"

auto SPC7110::dcuLoadAddress() -> void {
  uint table = r4801 | r4802 << 8 | r4803 << 16;
  uint index = r4804 << 2;

  uint address = table + index;
  dcuMode     = dataromRead(address + 0);
  dcuAddress  = dataromRead(address + 1) << 16;
  dcuAddress |= dataromRead(address + 2) <<  8;
  dcuAddress |= dataromRead(address + 3) <<  0;
}

auto SPC7110::dcuBeginTransfer() -> void {
  if(dcuMode == 3) return;  //invalid mode

  addClocks(20);
  decompressor->initialize(dcuMode, dcuAddress);
  decompressor->decode();

  uint seek = r480b & 2 ? r4805 | r4806 << 8 : 0;
  while(seek--) decompressor->decode();

  r480c |= 0x80;
  dcuOffset = 0;
}

auto SPC7110::dcuRead() -> uint8 {
  if((r480c & 0x80) == 0) return 0x00;

  if(dcuOffset == 0) {
    for(auto row : range(8)) {
      switch(decompressor->bpp) {
      case 1:
        dcuTile[row] = decompressor->result;
        break;
      case 2:
        dcuTile[row * 2 + 0] = decompressor->result >> 0;
        dcuTile[row * 2 + 1] = decompressor->result >> 8;
        break;
      case 4:
        dcuTile[row * 2 +  0] = decompressor->result >>  0;
        dcuTile[row * 2 +  1] = decompressor->result >>  8;
        dcuTile[row * 2 + 16] = decompressor->result >> 16;
        dcuTile[row * 2 + 17] = decompressor->result >> 24;
        break;
      }

      uint seek = r480b & 1 ? r4807 : (uint8)1;
      while(seek--) decompressor->decode();
    }
  }

  uint8 data = dcuTile[dcuOffset++];
  dcuOffset &= 8 * decompressor->bpp - 1;
  return data;
}
