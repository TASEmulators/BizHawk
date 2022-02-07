#pragma once

namespace nall {

auto image::loadBMP(const string& filename) -> bool {
  if(!file::exists(filename)) return false;
  auto buffer = file::read(filename);
  return loadBMP(buffer.data(), buffer.size());
}

auto image::loadBMP(const uint8_t* bmpData, unsigned bmpSize) -> bool {
  Decode::BMP source;
  if(!source.load(bmpData, bmpSize)) return false;

  allocate(source.width(), source.height());
  const uint32_t* sp = source.data();
  uint8_t* dp = _data;

  for(unsigned y = 0; y < _height; y++) {
    for(unsigned x = 0; x < _width; x++) {
      uint32_t color = *sp++;
      uint64_t a = normalize((uint8_t)(color >> 24), 8, _alpha.depth());
      uint64_t r = normalize((uint8_t)(color >> 16), 8, _red.depth());
      uint64_t g = normalize((uint8_t)(color >>  8), 8, _green.depth());
      uint64_t b = normalize((uint8_t)(color >>  0), 8, _blue.depth());
      write(dp, (a << _alpha.shift()) | (r << _red.shift()) | (g << _green.shift()) | (b << _blue.shift()));
      dp += stride();
    }
  }

  return true;
}

auto image::loadPNG(const string& filename) -> bool {
  if(!file::exists(filename)) return false;
  auto buffer = file::read(filename);
  return loadPNG(buffer.data(), buffer.size());
}

auto image::loadPNG(const uint8_t* pngData, unsigned pngSize) -> bool {
  Decode::PNG source;
  if(!source.load(pngData, pngSize)) return false;

  allocate(source.info.width, source.info.height);
  const uint8_t* sp = source.data;
  uint8_t* dp = _data;

  auto decode = [&]() -> uint64_t {
    uint64_t p, r, g, b, a;

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

  for(unsigned y = 0; y < _height; y++) {
    for(unsigned x = 0; x < _width; x++) {
      write(dp, decode());
      dp += stride();
    }
  }

  return true;
}

}
