#pragma once

namespace nall {

//scan all four sides of the image for fully transparent pixels, and then crop them
//imagine an icon centered on a transparent background: this function removes the bordering
//this certainly won't win any speed awards, but nall::image is meant to be correct and simple, not fast
auto image::shrink(uint64_t transparentColor) -> void {
  //top
  { uint padding = 0;
    for(uint y : range(_height)) {
      const uint8_t* sp = _data + pitch() * y;
      bool found = false;
      for(uint x : range(_width)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += stride();
      }
      if(found) break;
      padding++;
    }
    crop(0, padding, _width, _height - padding);
  }

  //bottom
  { uint padding = 0;
    for(uint y : reverse(range(_height))) {
      const uint8_t* sp = _data + pitch() * y;
      bool found = false;
      for(uint x : range(_width)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += stride();
      }
      if(found) break;
      padding++;
    }
    crop(0, 0, _width, _height - padding);
  }

  //left
  { uint padding = 0;
    for(uint x : range(_width)) {
      const uint8_t* sp = _data + stride() * x;
      bool found = false;
      for(uint y : range(_height)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += pitch();
      }
      if(found) break;
      padding++;
    }
    crop(padding, 0, _width - padding, _height);
  }

  //right
  { uint padding = 0;
    for(uint x : reverse(range(_width))) {
      const uint8_t* sp = _data + stride() * x;
      bool found = false;
      for(uint y : range(_height)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += pitch();
      }
      if(found) break;
      padding++;
    }
    crop(0, 0, _width - padding, _height);
  }
}

auto image::crop(unsigned outputX, unsigned outputY, unsigned outputWidth, unsigned outputHeight) -> bool {
  if(outputX + outputWidth > _width) return false;
  if(outputY + outputHeight > _height) return false;

  uint8_t* outputData = allocate(outputWidth, outputHeight, stride());
  unsigned outputPitch = outputWidth * stride();

  for(unsigned y = 0; y < outputHeight; y++) {
    const uint8_t* sp = _data + pitch() * (outputY + y) + stride() * outputX;
    uint8_t* dp = outputData + outputPitch * y;
    for(unsigned x = 0; x < outputWidth; x++) {
      write(dp, read(sp));
      sp += stride();
      dp += stride();
    }
  }

  delete[] _data;
  _data = outputData;
  _width = outputWidth;
  _height = outputHeight;
  return true;
}

auto image::alphaBlend(uint64_t alphaColor) -> void {
  uint64_t alphaR = (alphaColor & _red.mask()  ) >> _red.shift();
  uint64_t alphaG = (alphaColor & _green.mask()) >> _green.shift();
  uint64_t alphaB = (alphaColor & _blue.mask() ) >> _blue.shift();

  for(unsigned y = 0; y < _height; y++) {
    uint8_t* dp = _data + pitch() * y;
    for(unsigned x = 0; x < _width; x++) {
      uint64_t color = read(dp);

      uint64_t colorA = (color & _alpha.mask()) >> _alpha.shift();
      uint64_t colorR = (color & _red.mask()  ) >> _red.shift();
      uint64_t colorG = (color & _green.mask()) >> _green.shift();
      uint64_t colorB = (color & _blue.mask() ) >> _blue.shift();
      double alphaScale = (double)colorA / (double)((1 << _alpha.depth()) - 1);

      colorA = (1 << _alpha.depth()) - 1;
      colorR = (colorR * alphaScale) + (alphaR * (1.0 - alphaScale));
      colorG = (colorG * alphaScale) + (alphaG * (1.0 - alphaScale));
      colorB = (colorB * alphaScale) + (alphaB * (1.0 - alphaScale));

      write(dp, (colorA << _alpha.shift()) | (colorR << _red.shift()) | (colorG << _green.shift()) | (colorB << _blue.shift()));
      dp += stride();
    }
  }
}

auto image::alphaMultiply() -> void {
  unsigned divisor = (1 << _alpha.depth()) - 1;

  for(unsigned y = 0; y < _height; y++) {
    uint8_t* dp = _data + pitch() * y;
    for(unsigned x = 0; x < _width; x++) {
      uint64_t color = read(dp);

      uint64_t colorA = (color & _alpha.mask()) >> _alpha.shift();
      uint64_t colorR = (color & _red.mask()  ) >> _red.shift();
      uint64_t colorG = (color & _green.mask()) >> _green.shift();
      uint64_t colorB = (color & _blue.mask() ) >> _blue.shift();

      colorR = (colorR * colorA) / divisor;
      colorG = (colorG * colorA) / divisor;
      colorB = (colorB * colorA) / divisor;

      write(dp, (colorA << _alpha.shift()) | (colorR << _red.shift()) | (colorG << _green.shift()) | (colorB << _blue.shift()));
      dp += stride();
    }
  }
}

auto image::transform(const image& source) -> void {
  return transform(source._endian, source._depth, source._alpha.mask(), source._red.mask(), source._green.mask(), source._blue.mask());
}

auto image::transform(bool outputEndian, unsigned outputDepth, uint64_t outputAlphaMask, uint64_t outputRedMask, uint64_t outputGreenMask, uint64_t outputBlueMask) -> void {
  if(_endian == outputEndian && _depth == outputDepth && _alpha.mask() == outputAlphaMask && _red.mask() == outputRedMask && _green.mask() == outputGreenMask && _blue.mask() == outputBlueMask) return;

  image output(outputEndian, outputDepth, outputAlphaMask, outputRedMask, outputGreenMask, outputBlueMask);
  output.allocate(_width, _height);

  for(unsigned y = 0; y < _height; y++) {
    const uint8_t* sp = _data + pitch() * y;
    uint8_t* dp = output._data + output.pitch() * y;
    for(unsigned x = 0; x < _width; x++) {
      uint64_t color = read(sp);
      sp += stride();

      uint64_t a = (color & _alpha.mask()) >> _alpha.shift();
      uint64_t r = (color & _red.mask()  ) >> _red.shift();
      uint64_t g = (color & _green.mask()) >> _green.shift();
      uint64_t b = (color & _blue.mask() ) >> _blue.shift();

      a = normalize(a, _alpha.depth(), output._alpha.depth());
      r = normalize(r, _red.depth(),   output._red.depth());
      g = normalize(g, _green.depth(), output._green.depth());
      b = normalize(b, _blue.depth(),  output._blue.depth());

      output.write(dp, (a << output._alpha.shift()) | (r << output._red.shift()) | (g << output._green.shift()) | (b << output._blue.shift()));
      dp += output.stride();
    }
  }

  operator=(move(output));
}

}
