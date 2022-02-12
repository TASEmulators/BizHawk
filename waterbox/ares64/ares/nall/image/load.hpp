#pragma once

namespace nall {

inline auto image::loadBMP(const string& filename) -> bool {
  if(!file::exists(filename)) return false;
  auto buffer = file::read(filename);
  return loadBMP(buffer.data(), buffer.size());
}

inline auto image::loadBMP(const u8* bmpData, u32 bmpSize) -> bool {
  Decode::BMP source;
  if(!source.load(bmpData, bmpSize)) return false;

  allocate(source.width(), source.height());
  const u32* sp = source.data();
  u8* dp = _data;

  for(u32 y = 0; y < _height; y++) {
    for(u32 x = 0; x < _width; x++) {
      u32 color = *sp++;
      u64 a = normalize((u8)(color >> 24), 8, _alpha.depth());
      u64 r = normalize((u8)(color >> 16), 8, _red.depth());
      u64 g = normalize((u8)(color >>  8), 8, _green.depth());
      u64 b = normalize((u8)(color >>  0), 8, _blue.depth());
      write(dp, (a << _alpha.shift()) | (r << _red.shift()) | (g << _green.shift()) | (b << _blue.shift()));
      dp += stride();
    }
  }

  return true;
}

inline auto image::loadPNG(const string& filename) -> bool {
  if(!file::exists(filename)) return false;
  auto buffer = file::read(filename);
  return loadPNG(buffer.data(), buffer.size());
}

inline auto image::loadPNG(const u8* pngData, u32 pngSize) -> bool {
  Decode::PNG source;
  if(!source.load(pngData, pngSize)) return false;

  allocate(source.info.width, source.info.height);
  const u8* sp = source.data;
  u8* dp = _data;

  auto decode = [&]() -> u64 {
    u64 p = 0, r = 0, g = 0, b = 0, a = 0;

    switch(source.info.colorType) {
    case 0:  //L
      r = g = b = source.readbits(sp);
      a = (1 << source.info.bitDepth) - 1;
      break;
    case 2:  //R,G,B
      r = source.readbits(sp);
      g = source.readbits(sp);
      b = source.readbits(sp);
      a = (1 << source.info.bitDepth) - 1;
      break;
    case 3:  //P
      p = source.readbits(sp);
      r = source.info.palette[p][0];
      g = source.info.palette[p][1];
      b = source.info.palette[p][2];
      a = (1 << source.info.bitDepth) - 1;
      break;
    case 4:  //L,A
      r = g = b = source.readbits(sp);
      a = source.readbits(sp);
      break;
    case 6:  //R,G,B,A
      r = source.readbits(sp);
      g = source.readbits(sp);
      b = source.readbits(sp);
      a = source.readbits(sp);
      break;
    }

    a = normalize(a, source.info.bitDepth, _alpha.depth());
    r = normalize(r, source.info.bitDepth, _red.depth());
    g = normalize(g, source.info.bitDepth, _green.depth());
    b = normalize(b, source.info.bitDepth, _blue.depth());

    return (a << _alpha.shift()) | (r << _red.shift()) | (g << _green.shift()) | (b << _blue.shift());
  };

  for(u32 y = 0; y < _height; y++) {
    for(u32 x = 0; x < _width; x++) {
      write(dp, decode());
      dp += stride();
    }
  }

  return true;
}

}
