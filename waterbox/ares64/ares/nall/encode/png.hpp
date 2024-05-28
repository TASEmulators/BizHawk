#pragma once

#include <nall/file.hpp>
#include <nall/run.hpp>
#include <nall/string.hpp>

namespace nall::Encode {

//this encodes an array of pixels into an uncompressed PNG image file.
//if optipng or pngcrush are installed, the resulting PNG file will be quickly compressed.
//if nall gains a deflate implementation one day, then this can be improved to offer integrated compression.

struct PNG {
  static auto RGB8 (const string& filename, const void* data, u32 pitch, u32 width, u32 height) -> bool;
  static auto RGBA8(const string& filename, const void* data, u32 pitch, u32 width, u32 height) -> bool;

private:
  auto compress(const string& filename) -> bool;
  auto open(const string& filename) -> bool;
  auto close() -> void;
  auto header() -> void;
  auto footer() -> void;
  auto information(u32 width, u32 height, u32 depth, u32 type) -> void;
  auto dataHeader(u32 width, u32 height, u32 bitsPerPixel) -> void;
  auto dataLine(bool lastLine) -> void;
  auto dataFooter() -> void;
  auto write(u8 data) -> void;
  auto adler(u8 data) -> void;

  file_buffer fp;
  Hash::CRC32 crc32;
  u16 adler1 = 1;
  u16 adler2 = 0;
  u16 bytesPerLine = 0;
};

inline auto PNG::RGB8(const string& filename, const void* data, u32 pitch, u32 width, u32 height) -> bool {
  PNG png;
  if(!png.open(filename)) return false;

  png.header();
  png.information(width, height, 8, 2);
  png.dataHeader(width, height, 24);
  for(u32 y : range(height)) {
    const auto input = (const u32*)data + y * (pitch >> 2);
    png.dataLine(y == height - 1);
    for(u32 x : range(width)) {
      auto pixel = input[x];   //RGB
      png.adler(pixel >> 16);  //R
      png.adler(pixel >>  8);  //G
      png.adler(pixel >>  0);  //B
    }
  }
  png.dataFooter();
  png.footer();
  png.close();
  png.compress(filename);
  return true;
}

inline auto PNG::RGBA8(const string& filename, const void* data, u32 pitch, u32 width, u32 height) -> bool {
  PNG png;
  if(!png.open(filename)) return false;

  png.header();
  png.information(width, height, 8, 6);
  png.dataHeader(width, height, 32);
  for(u32 y : range(height)) {
    const auto input = (const u32*)data + y * (pitch >> 2);
    png.dataLine(y == height - 1);
    for(u32 x : range(width)) {
      auto pixel = input[x];   //ARGB
      png.adler(pixel >> 16);  //R
      png.adler(pixel >>  8);  //G
      png.adler(pixel >>  0);  //B
      png.adler(pixel >> 24);  //A
    }
  }
  png.dataFooter();
  png.footer();
  png.close();
  png.compress(filename);
  return true;
}

inline auto PNG::compress(const string& filename) -> bool {
  auto size = file::size(filename);
  execute("optipng", "-o1", filename);
  if(file::size(filename) < size) return true;
  execute("pngcrush", "-ow", "-l", "1", filename);
  if(file::size(filename) < size) return true;
  return false;
}

inline auto PNG::open(const string& filename) -> bool {
  fp = file::open(filename, file::mode::write);
  return (bool)fp;
}

inline auto PNG::close() -> void {
  fp.close();
}

inline auto PNG::header() -> void {
  fp.write(0x89);
  fp.write('P');
  fp.write('N');
  fp.write('G');
  fp.write(0x0d);
  fp.write(0x0a);
  fp.write(0x1a);
  fp.write(0x0a);
}

inline auto PNG::footer() -> void {
  fp.writem(0, 4L);
  crc32.reset();
  write('I');
  write('E');
  write('N');
  write('D');
  fp.writem(crc32.value(), 4L);
}

inline auto PNG::information(u32 width, u32 height, u32 depth, u32 type) -> void {
  fp.writem(13, 4L);
  crc32.reset();
  write('I');
  write('H');
  write('D');
  write('R');
  write(width  >> 24);
  write(width  >> 16);
  write(width  >>  8);
  write(width  >>  0);
  write(height >> 24);
  write(height >> 16);
  write(height >>  8);
  write(height >>  0);
  write(depth);
  write(type);
  write(0x00);  //no compression
  write(0x00);  //no filter
  write(0x00);  //no interlace
  fp.writem(crc32.value(), 4L);
}

inline auto PNG::dataHeader(u32 width, u32 height, u32 bitsPerPixel) -> void {
  bytesPerLine = 1 + width * (bitsPerPixel / 8);
  u32 idatSize = 2 + height * (5 + bytesPerLine) + 4;
  fp.writem(idatSize, 4L);
  crc32.reset();
  write('I');
  write('D');
  write('A');
  write('T');
  write(0x78);
  write(0xda);
}

inline auto PNG::dataLine(bool lastLine) -> void {
  write(lastLine);
  write( bytesPerLine >> 0);
  write( bytesPerLine >> 8);
  write(~bytesPerLine >> 0);
  write(~bytesPerLine >> 8);
  adler(0x00);  //no filter
}

inline auto PNG::dataFooter() -> void {
  write(adler2 >> 8);
  write(adler2 >> 0);
  write(adler1 >> 8);
  write(adler1 >> 0);
  fp.writem(crc32.value(), 4L);
}

inline auto PNG::write(u8 data) -> void {
  fp.write(data);
  crc32.input(data);
}

inline auto PNG::adler(u8 data) -> void {
  write(data);
  adler1 = (adler1 + data  ) % 65521;
  adler2 = (adler2 + adler1) % 65521;
}

}
