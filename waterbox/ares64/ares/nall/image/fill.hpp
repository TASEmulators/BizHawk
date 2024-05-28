#pragma once

namespace nall {

inline auto image::fill(u64 color) -> void {
  for(u32 y = 0; y < _height; y++) {
    u8* dp = _data + pitch() * y;
    for(u32 x = 0; x < _width; x++) {
      write(dp, color);
      dp += stride();
    }
  }
}

inline auto image::gradient(u64 a, u64 b, u64 c, u64 d) -> void {
  for(u32 y = 0; y < _height; y++) {
    u8* dp = _data + pitch() * y;
    f64 muY = (f64)y / (f64)_height;
    for(u32 x = 0; x < _width; x++) {
      f64 muX = (f64)x / (f64)_width;
      write(dp, interpolate4f(a, b, c, d, muX, muY));
      dp += stride();
    }
  }
}

inline auto image::gradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY, function<f64 (f64, f64)> callback) -> void {
  for(s32 y = 0; y < _height; y++) {
    u8* dp = _data + pitch() * y;
    f64 py = max(-radiusY, min(+radiusY, y - centerY)) * 1.0 / radiusY;
    for(s32 x = 0; x < _width; x++) {
      f64 px = max(-radiusX, min(+radiusX, x - centerX)) * 1.0 / radiusX;
      f64 mu = max(0.0, min(1.0, callback(px, py)));
      if(mu != mu) mu = 1.0;  //NaN
      write(dp, interpolate4f(a, b, mu));
      dp += stride();
    }
  }
}

inline auto image::crossGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    x = fabs(x), y = fabs(y);
    return min(x, y) * min(x, y);
  });
}

inline auto image::diamondGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    return fabs(x) + fabs(y);
  });
}

inline auto image::horizontalGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    return fabs(x);
  });
}

inline auto image::radialGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    return sqrt(x * x + y * y);
  });
}

inline auto image::sphericalGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    return x * x + y * y;
  });
}

inline auto image::squareGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    return max(fabs(x), fabs(y));
  });
}

inline auto image::verticalGradient(u64 a, u64 b, s32 radiusX, s32 radiusY, s32 centerX, s32 centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](f64 x, f64 y) -> f64 {
    return fabs(y);
  });
}

}
