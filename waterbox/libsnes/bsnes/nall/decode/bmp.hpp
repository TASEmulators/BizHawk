#pragma once

namespace nall::Decode {

struct BMP {
  BMP() = default;
  BMP(const string& filename) { load(filename); }
  BMP(const uint8_t* data, uint size) { load(data, size); }

  explicit operator bool() const { return _data; }

  auto reset() -> void {
    if(_data) { delete[] _data; _data = nullptr; }
  }

  auto data() -> uint32_t* { return _data; }
  auto data() const -> const uint32_t* { return _data; }
  auto width() const -> uint { return _width; }
  auto height() const -> uint { return _height; }

  auto load(const string& filename) -> bool {
    auto buffer = file::read(filename);
    return load(buffer.data(), buffer.size());
  }

  auto load(const uint8_t* data, uint size) -> bool {
    if(size < 0x36) return false;
    const uint8_t* p = data;
    if(read(p, 2) != 0x4d42) return false;  //signature
    read(p, 8);
    uint offset = read(p, 4);
    if(read(p, 4) != 40) return false;  //DIB size
    int width = read(p, 4);
    if(width < 0) return false;
    int height = read(p, 4);
    bool flip = height < 0;
    if(flip) height = -height;
    read(p, 2);
    uint bitsPerPixel = read(p, 2);
    if(bitsPerPixel != 24 && bitsPerPixel != 32) return false;
    if(read(p, 4) != 0) return false;  //compression type

    _width = width;
    _height = height;
    _data = new uint32_t[width * height];

    uint bytesPerPixel = bitsPerPixel / 8;
    uint alignedWidth = width * bytesPerPixel;
    uint paddingLength = 0;
    while(alignedWidth % 4) alignedWidth++, paddingLength++;

    p = data + offset;
    for(auto y : range(height)) {
      uint32_t* output = flip ? _data + (height - 1 - y) * width : _data + y * width;
      for(auto x : range(width)) {
        *output++ = read(p, bytesPerPixel) | (bitsPerPixel == 24 ? 255u << 24 : 0);
      }
      if(paddingLength) read(p, paddingLength);
    }

    return true;
  }

private:
  uint32_t* _data = nullptr;
  uint _width = 0;
  uint _height = 0;

  auto read(const uint8_t*& buffer, uint length) -> uintmax {
    uintmax result = 0;
    for(auto n : range(length)) result |= (uintmax)*buffer++ << (n << 3);
    return result;
  }
};

}
