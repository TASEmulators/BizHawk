#pragma once

#include <algorithm>

#include <nall/file-map.hpp>
#include <nall/interpolation.hpp>
#include <nall/stdint.hpp>
#include <nall/decode/bmp.hpp>
#include <nall/decode/png.hpp>

namespace nall {

struct image {
  enum class blend : u32 {
    add,
    sourceAlpha,  //color = sourceColor * sourceAlpha + targetColor * (1 - sourceAlpha)
    sourceColor,  //color = sourceColor
    targetAlpha,  //color = targetColor * targetAlpha + sourceColor * (1 - targetAlpha)
    targetColor,  //color = targetColor
  };

  struct channel {
    channel(u64 mask, u32 depth, u32 shift) : _mask(mask), _depth(depth), _shift(shift) {
    }

    auto operator==(const channel& source) const -> bool {
      return _mask == source._mask && _depth == source._depth && _shift == source._shift;
    }

    auto operator!=(const channel& source) const -> bool {
      return !operator==(source);
    }

    auto mask() const { return _mask; }
    auto depth() const { return _depth; }
    auto shift() const { return _shift; }

  private:
    u64 _mask;
    u32 _depth;
    u32 _shift;
  };

  //core.hpp
  image(const image& source);
  image(image&& source);
  image(bool endian, u32 depth, u64 alphaMask, u64 redMask, u64 greenMask, u64 blueMask);
  image(const string& filename);
  image(const void* data, u32 size);
  image(const vector<u8>& buffer);
  template<u32 Size> image(const u8 (&Name)[Size]);
  image();
  ~image();

  auto operator=(const image& source) -> image&;
  auto operator=(image&& source) -> image&;

  explicit operator bool() const;
  auto operator==(const image& source) const -> bool;
  auto operator!=(const image& source) const -> bool;

  auto read(const u8* data) const -> u64;
  auto write(u8* data, u64 value) const -> void;

  auto free() -> void;
  auto load(const string& filename) -> bool;
  auto copy(const void* data, u32 pitch, u32 width, u32 height) -> void;
  auto allocate(u32 width, u32 height) -> void;

  //fill.hpp
  auto fill(u64 color = 0) -> void;
  auto gradient(u64 a, u64 b, u64 c, u64 d) -> void;
  auto gradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY, function<f64 (f64, f64)> callback) -> void;
  auto crossGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;
  auto diamondGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;
  auto horizontalGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;
  auto radialGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;
  auto sphericalGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;
  auto squareGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;
  auto verticalGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void;

  //scale.hpp
  auto scale(u32 width, u32 height, bool linear = true) -> void;

  //blend.hpp
  auto impose(blend mode, u32 targetX, u32 targetY, image source, u32 x, u32 y, u32 width, u32 height) -> void;

  //utility.hpp
  auto shrink(u64 transparentColor = 0) -> void;
  auto crop(u32 x, u32 y, u32 width, u32 height) -> bool;
  auto alphaBlend(u64 alphaColor) -> void;
  auto alphaMultiply() -> void;
  auto transform(const image& source = {}) -> void;
  auto transform(bool endian, u32 depth, u64 alphaMask, u64 redMask, u64 greenMask, u64 blueMask) -> void;

  //static.hpp
  static auto bitDepth(u64 color) -> u32;
  static auto bitShift(u64 color) -> u32;
  static auto normalize(u64 color, u32 sourceDepth, u32 targetDepth) -> u64;

  //access
  auto data() { return _data; }
  auto data() const { return _data; }
  auto width() const { return _width; }
  auto height() const { return _height; }

  auto endian() const { return _endian; }
  auto depth() const { return _depth; }
  auto stride() const { return (_depth + 7) >> 3; }

  auto pitch() const { return _width * stride(); }
  auto size() const { return _height * pitch(); }

  auto alpha() const { return _alpha; }
  auto red() const { return _red; }
  auto green() const { return _green; }
  auto blue() const { return _blue; }

private:
  //core.hpp
  auto allocate(u32 width, u32 height, u32 stride) -> u8*;

  //scale.hpp
  auto scaleLinearWidth(u32 width) -> void;
  auto scaleLinearHeight(u32 height) -> void;
  auto scaleLinear(u32 width, u32 height) -> void;
  auto scaleNearest(u32 width, u32 height) -> void;

  //load.hpp
  auto loadBMP(const string& filename) -> bool;
  auto loadBMP(const u8* data, u32 size) -> bool;
  auto loadPNG(const string& filename) -> bool;
  auto loadPNG(const u8* data, u32 size) -> bool;

  //interpolation.hpp
  auto isplit(u64* component, u64 color) -> void;
  auto imerge(const u64* component) -> u64;
  auto interpolate1f(u64 a, u64 b, f64 x) -> u64;
  auto interpolate1f(u64 a, u64 b, u64 c, u64 d, f64 x, f64 y) -> u64;
  auto interpolate1i(s64 a, s64 b, u32 x) -> u64;
  auto interpolate1i(s64 a, s64 b, s64 c, s64 d, u32 x, u32 y) -> u64;
  auto interpolate4f(u64 a, u64 b, f64 x) -> u64;
  auto interpolate4f(u64 a, u64 b, u64 c, u64 d, f64 x, f64 y) -> u64;
  auto interpolate4i(u64 a, u64 b, u32 x) -> u64;
  auto interpolate4i(u64 a, u64 b, u64 c, u64 d, u32 x, u32 y) -> u64;

  u8* _data   = nullptr;
  u32 _width  = 0;
  u32 _height = 0;

  bool _endian =  0;  //0 = lsb, 1 = msb
  u32 _depth  = 32;

  channel _alpha{255u << 24, 8, 24};
  channel _red  {255u << 16, 8, 16};
  channel _green{255u <<  8, 8,  8};
  channel _blue {255u <<  0, 8,  0};
};

struct multiFactorImage : public image {
    using image::image;

    multiFactorImage(const multiFactorImage& source);
    multiFactorImage(multiFactorImage&& source);
    multiFactorImage(const image& lowDPI, const image& highDPI);
    multiFactorImage(const image& source);
    multiFactorImage(image&& source);
    multiFactorImage();
    ~multiFactorImage();
    
    auto operator=(const multiFactorImage& source) -> multiFactorImage&;
    auto operator=(multiFactorImage&& source) -> multiFactorImage&;
    
    auto operator==(const multiFactorImage& source) const -> bool;
    auto operator!=(const multiFactorImage& source) const -> bool;

    const image& lowDPI() const { return *this; }
    const image& highDPI() const { return _highDPI; }
    
private:
    image _highDPI;
};

}

#include <nall/image/static.hpp>
#include <nall/image/core.hpp>
#include <nall/image/load.hpp>
#include <nall/image/interpolation.hpp>
#include <nall/image/fill.hpp>
#include <nall/image/scale.hpp>
#include <nall/image/blend.hpp>
#include <nall/image/utility.hpp>
#include <nall/image/multifactor.hpp>
