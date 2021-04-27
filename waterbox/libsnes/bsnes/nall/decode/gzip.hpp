#pragma once

#include <nall/file.hpp>
#include <nall/decode/inflate.hpp>

namespace nall::Decode {

struct GZIP {
  inline ~GZIP();

  inline auto decompress(const string& filename) -> bool;
  inline auto decompress(const uint8_t* data, uint size) -> bool;

  string filename;
  uint8_t* data = nullptr;
  uint size = 0;
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

auto GZIP::decompress(const uint8_t* data, uint size) -> bool {
  if(size < 18) return false;
  if(data[0] != 0x1f) return false;
  if(data[1] != 0x8b) return false;
  uint cm = data[2];
  uint flg = data[3];
  uint mtime = data[4];
  mtime |= data[5] << 8;
  mtime |= data[6] << 16;
  mtime |= data[7] << 24;
  uint xfl = data[8];
  uint os = data[9];
  uint p = 10;
  uint isize = data[size - 4];
  isize |= data[size - 3] << 8;
  isize |= data[size - 2] << 16;
  isize |= data[size - 1] << 24;
  filename = "";

  if(flg & 0x04) {  //FEXTRA
    uint xlen = data[p + 0];
    xlen |= data[p + 1] << 8;
    p += 2 + xlen;
  }

  if(flg & 0x08) {  //FNAME
    char buffer[PATH_MAX];
    for(uint n = 0; n < PATH_MAX; n++, p++) {
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
  this->data = new uint8_t[this->size];
  return inflate(this->data, this->size, data + p, size - p - 8);
}

}
