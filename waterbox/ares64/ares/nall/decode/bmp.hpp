#pragma once

namespace nall::Decode {

struct BMP {
  BMP() = default;
  BMP(const string& filename) { load(filename); }
  BMP(const u8* data, u32 size) { load(data, size); }

  explicit operator bool() const { return _data; }

  auto reset() -> void {
    if(_data) { delete[] _data; _data = nullptr; }
  }

  auto data() -> u32* { return _data; }
  auto data() const -> const u32* { return _data; }
  auto width() const -> u32 { return _width; }
  auto height() const -> u32 { return _height; }

  auto load(const string& filename) -> bool {
    auto buffer = file::read(filename);
    return load(buffer.data(), buffer.size());
  }

  auto load(const u8* data, u32 size) -> bool {
    if(size < 0x36) return false;
    const u8* p = data;
    if(read(p, 2) != 0x4d42) return false;  //signature
    read(p, 8);
    u32 offset = read(p, 4);
    if(read(p, 4) != 40) return false;  //DIB size
    s32 width = (s32)read(p, 4);
    if(width < 0) width = -width;
    s32 height = (s32)read(p, 4);
    bool flip = height >= 0;
    if(height < 0) height = -height;
    read(p, 2);
    u32 bitsPerPixel = read(p, 2);
    if(bitsPerPixel != 24 && bitsPerPixel != 32) return false;
    if(read(p, 4) != 0) return false;  //compression type

    _width = width;
    _height = height;
    _data = new u32[width * height];

    u32 bytesPerPixel = bitsPerPixel / 8;
    u32 alignedWidth = width * bytesPerPixel;
    u32 paddingLength = 0;
    while(alignedWidth % 4) alignedWidth++, paddingLength++;

    p = data + offset;
    for(auto y : range(height)) {
      u32* output = flip ? _data + (height - 1 - y) * width : _data + y * width;
      for(auto x : range(width)) {
        *output++ = read(p, bytesPerPixel) | (bitsPerPixel == 24 ? 255u << 24 : 0);
      }
      if(paddingLength) read(p, paddingLength);
    }

    return true;
  }

private:
  u32* _data = nullptr;
  u32 _width = 0;
  u32 _height = 0;

  auto read(const u8*& buffer, u32 length) -> u64 {
    u64 result = 0;
    for(u32 n : range(length)) result |= (u64)*buffer++ << (n << 3);
    return result;
  }
};

}
