#pragma once

namespace nall {

auto image::fill(uint64_t color) -> void {
  for(unsigned y = 0; y < _height; y++) {
    uint8_t* dp = _data + pitch() * y;
    for(unsigned x = 0; x < _width; x++) {
      write(dp, color);
      dp += stride();
    }
  }
}

auto image::gradient(uint64_t a, uint64_t b, uint64_t c, uint64_t d) -> void {
  for(unsigned y = 0; y < _height; y++) {
    uint8_t* dp = _data + pitch() * y;
    double muY = (double)y / (double)_height;
    for(unsigned x = 0; x < _width; x++) {
      double muX = (double)x / (double)_width;
      write(dp, interpolate4f(a, b, c, d, muX, muY));
      dp += stride();
    }
  }
}

auto image::gradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY, function<double (double, double)> callback) -> void {
  for(signed y = 0; y < _height; y++) {
    uint8_t* dp = _data + pitch() * y;
    double py = max(-radiusY, min(+radiusY, y - centerY)) * 1.0 / radiusY;
    for(signed x = 0; x < _width; x++) {
      double px = max(-radiusX, min(+radiusX, x - centerX)) * 1.0 / radiusX;
      double mu = max(0.0, min(1.0, callback(px, py)));
      if(mu != mu) mu = 1.0;  //NaN
      write(dp, interpolate4f(a, b, mu));
      dp += stride();
    }
  }
}

auto image::crossGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    x = fabs(x), y = fabs(y);
    return min(x, y) * min(x, y);
  });
}

auto image::diamondGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    return fabs(x) + fabs(y);
  });
}

auto image::horizontalGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    return fabs(x);
  });
}

auto image::radialGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    return sqrt(x * x + y * y);
  });
}

auto image::sphericalGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    return x * x + y * y;
  });
}

auto image::squareGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    return max(fabs(x), fabs(y));
  });
}

auto image::verticalGradient(uint64_t a, uint64_t b, signed radiusX, signed radiusY, signed centerX, signed centerY) -> void {
  return gradient(a, b, radiusX, radiusY, centerX, centerY, [](double x, double y) -> double {
    return fabs(y);
  });
}

}
