#pragma once

namespace nall {

inline auto image::scale(u32 outputWidth, u32 outputHeight, bool linear) -> void {
  if(!_data) return;
  if(_width == outputWidth && _height == outputHeight) return;  //no scaling necessary
  if(linear == false) return scaleNearest(outputWidth, outputHeight);

  if(_width  == outputWidth ) return scaleLinearHeight(outputHeight);
  if(_height == outputHeight) return scaleLinearWidth(outputWidth);

  //find fastest scaling method, based on number of interpolation operations required
  //magnification usually benefits from two-pass linear interpolation
  //minification usually benefits from one-pass bilinear interpolation
  u32 d1wh = ((_width  * outputWidth ) + (outputWidth * outputHeight)) * 1;
  u32 d1hw = ((_height * outputHeight) + (outputWidth * outputHeight)) * 1;
  u32 d2wh = (outputWidth * outputHeight) * 3;

  if(d1wh <= d1hw && d1wh <= d2wh) return scaleLinearWidth(outputWidth), scaleLinearHeight(outputHeight);
  if(d1hw <= d2wh) return scaleLinearHeight(outputHeight), scaleLinearWidth(outputWidth);
  return scaleLinear(outputWidth, outputHeight);
}

inline auto image::scaleLinearWidth(u32 outputWidth) -> void {
  u8* outputData = allocate(outputWidth, _height, stride());
  u32 outputPitch = outputWidth * stride();
  u64 xstride = ((u64)(_width - 1) << 32) / max(1u, outputWidth - 1);

  for(u32 y = 0; y < _height; y++) {
    u64 xfraction = 0;

    const u8* sp = _data + pitch() * y;
    u8* dp = outputData + outputPitch * y;

    u64 a = read(sp);
    u64 b = read(sp + stride());
    sp += stride();

    u32 x = 0;
    while(true) {
      while(xfraction < 0x100000000 && x++ < outputWidth) {
        write(dp, interpolate4i(a, b, xfraction));
        dp += stride();
        xfraction += xstride;
      }
      if(x >= outputWidth) break;

      sp += stride();
      a = b;
      b = read(sp);
      xfraction -= 0x100000000;
    }
  }

  free();
  _data = outputData;
  _width = outputWidth;
}

inline auto image::scaleLinearHeight(u32 outputHeight) -> void {
  u8* outputData = allocate(_width, outputHeight, stride());
  u64 ystride = ((u64)(_height - 1) << 32) / max(1u, outputHeight - 1);

  for(u32 x = 0; x < _width; x++) {
    u64 yfraction = 0;

    const u8* sp = _data + stride() * x;
    u8* dp = outputData + stride() * x;

    u64 a = read(sp);
    u64 b = read(sp + pitch());
    sp += pitch();

    u32 y = 0;
    while(true) {
      while(yfraction < 0x100000000 && y++ < outputHeight) {
        write(dp, interpolate4i(a, b, yfraction));
        dp += pitch();
        yfraction += ystride;
      }
      if(y >= outputHeight) break;

      sp += pitch();
      a = b;
      b = read(sp);
      yfraction -= 0x100000000;
    }
  }

  free();
  _data = outputData;
  _height = outputHeight;
}

inline auto image::scaleLinear(u32 outputWidth, u32 outputHeight) -> void {
  u8* outputData = allocate(outputWidth, outputHeight, stride());
  u32 outputPitch = outputWidth * stride();

  u64 xstride = ((u64)(_width  - 1) << 32) / max(1u, outputWidth  - 1);
  u64 ystride = ((u64)(_height - 1) << 32) / max(1u, outputHeight - 1);

  for(u32 y = 0; y < outputHeight; y++) {
    u64 yfraction = ystride * y;
    u64 xfraction = 0;

    const u8* sp = _data + pitch() * (yfraction >> 32);
    u8* dp = outputData + outputPitch * y;

    u64 a = read(sp);
    u64 b = read(sp + stride());
    u64 c = read(sp + pitch());
    u64 d = read(sp + pitch() + stride());
    sp += stride();

    u32 x = 0;
    while(true) {
      while(xfraction < 0x100000000 && x++ < outputWidth) {
        write(dp, interpolate4i(a, b, c, d, xfraction, yfraction));
        dp += stride();
        xfraction += xstride;
      }
      if(x >= outputWidth) break;

      sp += stride();
      a = b;
      c = d;
      b = read(sp);
      d = read(sp + pitch());
      xfraction -= 0x100000000;
    }
  }

  free();
  _data = outputData;
  _width = outputWidth;
  _height = outputHeight;
}

inline auto image::scaleNearest(u32 outputWidth, u32 outputHeight) -> void {
  u8* outputData = allocate(outputWidth, outputHeight, stride());
  u32 outputPitch = outputWidth * stride();

  u64 xstride = ((u64)_width  << 32) / outputWidth;
  u64 ystride = ((u64)_height << 32) / outputHeight;

  for(u32 y = 0; y < outputHeight; y++) {
    u64 yfraction = ystride * y;
    u64 xfraction = 0;

    const u8* sp = _data + pitch() * (yfraction >> 32);
    u8* dp = outputData + outputPitch * y;

    u64 a = read(sp);

    u32 x = 0;
    while(true) {
      while(xfraction < 0x100000000 && x++ < outputWidth) {
        write(dp, a);
        dp += stride();
        xfraction += xstride;
      }
      if(x >= outputWidth) break;

      sp += stride();
      a = read(sp);
      xfraction -= 0x100000000;
    }
  }

  free();
  _data = outputData;
  _width = outputWidth;
  _height = outputHeight;
}

}
