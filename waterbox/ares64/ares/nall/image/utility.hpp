#pragma once

namespace nall {

//scan all four sides of the image for fully transparent pixels, and then crop them
//imagine an icon centered on a transparent background: this function removes the bordering
//this certainly won't win any speed awards, but nall::image is meant to be correct and simple, not fast
inline auto image::shrink(u64 transparentColor) -> void {
  //top
  { u32 padding = 0;
    for(u32 y : range(_height)) {
      const u8* sp = _data + pitch() * y;
      bool found = false;
      for(u32 x : range(_width)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += stride();
      }
      if(found) break;
      padding++;
    }
    crop(0, padding, _width, _height - padding);
  }

  //bottom
  { u32 padding = 0;
    for(u32 y : reverse(range(_height))) {
      const u8* sp = _data + pitch() * y;
      bool found = false;
      for(u32 x : range(_width)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += stride();
      }
      if(found) break;
      padding++;
    }
    crop(0, 0, _width, _height - padding);
  }

  //left
  { u32 padding = 0;
    for(u32 x : range(_width)) {
      const u8* sp = _data + stride() * x;
      bool found = false;
      for(u32 y : range(_height)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += pitch();
      }
      if(found) break;
      padding++;
    }
    crop(padding, 0, _width - padding, _height);
  }

  //right
  { u32 padding = 0;
    for(u32 x : reverse(range(_width))) {
      const u8* sp = _data + stride() * x;
      bool found = false;
      for(u32 y : range(_height)) {
        if(read(sp) != transparentColor) { found = true; break; }
        sp += pitch();
      }
      if(found) break;
      padding++;
    }
    crop(0, 0, _width - padding, _height);
  }
}

inline auto image::crop(u32 outputX, u32 outputY, u32 outputWidth, u32 outputHeight) -> bool {
  if(outputX + outputWidth > _width) return false;
  if(outputY + outputHeight > _height) return false;

  u8* outputData = allocate(outputWidth, outputHeight, stride());
  u32 outputPitch = outputWidth * stride();

  for(u32 y = 0; y < outputHeight; y++) {
    const u8* sp = _data + pitch() * (outputY + y) + stride() * outputX;
    u8* dp = outputData + outputPitch * y;
    for(u32 x = 0; x < outputWidth; x++) {
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

inline auto image::alphaBlend(u64 alphaColor) -> void {
  u64 alphaR = (alphaColor & _red.mask()  ) >> _red.shift();
  u64 alphaG = (alphaColor & _green.mask()) >> _green.shift();
  u64 alphaB = (alphaColor & _blue.mask() ) >> _blue.shift();

  for(u32 y = 0; y < _height; y++) {
    u8* dp = _data + pitch() * y;
    for(u32 x = 0; x < _width; x++) {
      u64 color = read(dp);

      u64 colorA = (color & _alpha.mask()) >> _alpha.shift();
      u64 colorR = (color & _red.mask()  ) >> _red.shift();
      u64 colorG = (color & _green.mask()) >> _green.shift();
      u64 colorB = (color & _blue.mask() ) >> _blue.shift();
      f64 alphaScale = (f64)colorA / (f64)((1 << _alpha.depth()) - 1);

      colorA = (1 << _alpha.depth()) - 1;
      colorR = (colorR * alphaScale) + (alphaR * (1.0 - alphaScale));
      colorG = (colorG * alphaScale) + (alphaG * (1.0 - alphaScale));
      colorB = (colorB * alphaScale) + (alphaB * (1.0 - alphaScale));

      write(dp, (colorA << _alpha.shift()) | (colorR << _red.shift()) | (colorG << _green.shift()) | (colorB << _blue.shift()));
      dp += stride();
    }
  }
}

inline auto image::alphaMultiply() -> void {
  u32 divisor = (1 << _alpha.depth()) - 1;

  for(u32 y = 0; y < _height; y++) {
    u8* dp = _data + pitch() * y;
    for(u32 x = 0; x < _width; x++) {
      u64 color = read(dp);

      u64 colorA = (color & _alpha.mask()) >> _alpha.shift();
      u64 colorR = (color & _red.mask()  ) >> _red.shift();
      u64 colorG = (color & _green.mask()) >> _green.shift();
      u64 colorB = (color & _blue.mask() ) >> _blue.shift();

      colorR = (colorR * colorA) / divisor;
      colorG = (colorG * colorA) / divisor;
      colorB = (colorB * colorA) / divisor;

      write(dp, (colorA << _alpha.shift()) | (colorR << _red.shift()) | (colorG << _green.shift()) | (colorB << _blue.shift()));
      dp += stride();
    }
  }
}

inline auto image::transform(const image& source) -> void {
  return transform(source._endian, source._depth, source._alpha.mask(), source._red.mask(), source._green.mask(), source._blue.mask());
}

inline auto image::transform(bool outputEndian, u32 outputDepth, u64 outputAlphaMask, u64 outputRedMask, u64 outputGreenMask, u64 outputBlueMask) -> void {
  if(_endian == outputEndian && _depth == outputDepth && _alpha.mask() == outputAlphaMask && _red.mask() == outputRedMask && _green.mask() == outputGreenMask && _blue.mask() == outputBlueMask) return;

  image output(outputEndian, outputDepth, outputAlphaMask, outputRedMask, outputGreenMask, outputBlueMask);
  output.allocate(_width, _height);

  for(u32 y = 0; y < _height; y++) {
    const u8* sp = _data + pitch() * y;
    u8* dp = output._data + output.pitch() * y;
    for(u32 x = 0; x < _width; x++) {
      u64 color = read(sp);
      sp += stride();

      u64 a = (color & _alpha.mask()) >> _alpha.shift();
      u64 r = (color & _red.mask()  ) >> _red.shift();
      u64 g = (color & _green.mask()) >> _green.shift();
      u64 b = (color & _blue.mask() ) >> _blue.shift();

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
