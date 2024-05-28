#pragma once

#include <nall/file.hpp>
#include <nall/decode/inflate.hpp>

namespace nall::Decode {

struct GZIP {
  inline ~GZIP();

  inline auto decompress(const string& filename) -> bool;
  inline auto decompress(const u8* data, u32 size) -> bool;

  string filename;
  u8* data = nullptr;
  u32 size = 0;
};

GZIP::~GZIP() {
  if(data) delete[] data;
}

auto GZIP::decompress(const string& filename) -> bool {
  if(auto memory = file::read(filename)) {
    return decompress(memory.data(), memory.size());
  }
  return false;
}

auto GZIP::decompress(const u8* data, u32 size) -> bool {
  if(size < 18) return false;
  if(data[0] != 0x1f) return false;
  if(data[1] != 0x8b) return false;
  u32 cm = data[2];
  u32 flg = data[3];
  u32 mtime = data[4];
  mtime |= data[5] << 8;
  mtime |= data[6] << 16;
  mtime |= data[7] << 24;
  u32 xfl = data[8];
  u32 os = data[9];
  u32 p = 10;
  u32 isize = data[size - 4];
  isize |= data[size - 3] << 8;
  isize |= data[size - 2] << 16;
  isize |= data[size - 1] << 24;
  filename = "";

  if(flg & 0x04) {  //FEXTRA
    u32 xlen = data[p + 0];
    xlen |= data[p + 1] << 8;
    p += 2 + xlen;
  }

  if(flg & 0x08) {  //FNAME
    char buffer[PATH_MAX];
    for(u32 n = 0; n < PATH_MAX; n++, p++) {
      buffer[n] = data[p];
      if(data[p] == 0) break;
    }
    if(data[p++]) return false;
    filename = buffer;
  }

  if(flg & 0x10) {  //FCOMMENT
    while(data[p++]);
  }

  if(flg & 0x02) {  //FHCRC
    p += 2;
  }

  this->size = isize;
  this->data = new u8[this->size];
  return inflate(this->data, this->size, data + p, size - p - 8);
}

}
