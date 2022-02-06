#pragma once

#include <nall/file-map.hpp>
#include <nall/string.hpp>
#include <nall/vector.hpp>
#include <nall/decode/inflate.hpp>

namespace nall::Decode {

struct ZIP {
  struct File {
    string name;
    const u8* data;
    u32 size;
    u32 csize;
    u32 cmode;  //0 = uncompressed, 8 = deflate
    u32 crc32;
    time_t timestamp;
  };

  ~ZIP() {
    close();
  }

  auto open(const string& filename) -> bool {
    close();
    if(fm.open(filename, file::mode::read) == false) return false;
    if(open(fm.data(), fm.size()) == false) {
      fm.close();
      return false;
    }
    return true;
  }

  auto open(const u8* data, u32 size) -> bool {
    if(size < 22) return false;

    filedata = data;
    filesize = size;

    file.reset();

    const u8* footer = data + size - 22;
    while(true) {
      if(footer <= data + 22) return false;
      if(read(footer, 4) == 0x06054b50) {
        u32 commentlength = read(footer + 20, 2);
        if(footer + 22 + commentlength == data + size) break;
      }
      footer--;
    }
    const u8* directory = data + read(footer + 16, 4);

    while(true) {
      u32 signature = read(directory + 0, 4);
      if(signature != 0x02014b50) break;

      File file;
      file.cmode = read(directory + 10, 2);
      file.crc32 = read(directory + 16, 4);
      file.csize = read(directory + 20, 4);
      file.size  = read(directory + 24, 4);

      u16 dosTime = read(directory + 12, 2);
      u16 dosDate = read(directory + 14, 2);
      tm info = {};
      info.tm_sec  = (dosTime >>  0 &  31) << 1;
      info.tm_min  = (dosTime >>  5 &  63);
      info.tm_hour = (dosTime >> 11 &  31);
      info.tm_mday = (dosDate >>  0 &  31);
      info.tm_mon  = (dosDate >>  5 &  15) - 1;
      info.tm_year = (dosDate >>  9 & 127) + 80;
      info.tm_isdst = -1;
      file.timestamp = mktime(&info);

      u32 namelength = read(directory + 28, 2);
      u32 extralength = read(directory + 30, 2);
      u32 commentlength = read(directory + 32, 2);

      char* filename = new char[namelength + 1];
      memcpy(filename, directory + 46, namelength);
      filename[namelength] = 0;
      file.name = filename;
      delete[] filename;

      u32 offset = read(directory + 42, 4);
      u32 offsetNL = read(data + offset + 26, 2);
      u32 offsetEL = read(data + offset + 28, 2);
      file.data = data + offset + 30 + offsetNL + offsetEL;

      directory += 46 + namelength + extralength + commentlength;

      this->file.append(file);
    }

    return true;
  }

  auto extract(File& file) -> vector<u8> {
    vector<u8> buffer;

    if(file.cmode == 0) {
      buffer.resize(file.size);
      memcpy(buffer.data(), file.data, file.size);
    }

    if(file.cmode == 8) {
      buffer.resize(file.size);
      if(inflate(buffer.data(), buffer.size(), file.data, file.csize) == false) {
        buffer.reset();
      }
    }

    return buffer;
  }

  auto close() -> void {
    if(fm) fm.close();
  }

protected:
  file_map fm;
  const u8* filedata;
  u32 filesize;

  auto read(const u8* data, u32 size) -> u32 {
    u32 result = 0, shift = 0;
    while(size--) { result |= *data++ << shift; shift += 8; }
    return result;
  }

public:
  vector<File> file;
};

}
