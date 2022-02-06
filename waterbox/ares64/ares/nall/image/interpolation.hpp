#pragma once

namespace nall {

inline auto image::isplit(u64 c[4], u64 color) -> void {
  c[0] = (color & _alpha.mask()) >> _alpha.shift();
  c[1] = (color & _red.mask()  ) >> _red.shift();
  c[2] = (color & _green.mask()) >> _green.shift();
  c[3] = (color & _blue.mask() ) >> _blue.shift();
}

inline auto image::imerge(const u64 c[4]) -> u64 {
  return c[0] << _alpha.shift() | c[1] << _red.shift() | c[2] << _green.shift() | c[3] << _blue.shift();
}

inline auto image::interpolate1f(u64 a, u64 b, f64 x) -> u64 {
  return a * (1.0 - x) + b * x;
}

inline auto image::interpolate1f(u64 a, u64 b, u64 c, u64 d, f64 x, f64 y) -> u64 {
  return a * (1.0 - x) * (1.0 - y) + b * x * (1.0 - y) + c * (1.0 - x) * y + d * x * y;
}

inline auto image::interpolate1i(s64 a, s64 b, u32 x) -> u64 {
  return a + (((b - a) * x) >> 32);  //a + (b - a) * x
}

inline auto image::interpolate1i(s64 a, s64 b, s64 c, s64 d, u32 x, u32 y) -> u64 {
  a = a + (((b - a) * x) >> 32);     //a + (b - a) * x
  c = c + (((d - c) * x) >> 32);     //c + (d - c) * x
  return a + (((c - a) * y) >> 32);  //a + (c - a) * y
}

inline auto image::interpolate4f(u64 a, u64 b, f64 x) -> u64 {
  u64 o[4], pa[4], pb[4];
  isplit(pa, a), isplit(pb, b);
  for(u32 n = 0; n < 4; n++) o[n] = interpolate1f(pa[n], pb[n], x);
  return imerge(o);
}

inline auto image::interpolate4f(u64 a, u64 b, u64 c, u64 d, f64 x, f64 y) -> u64 {
  u64 o[4], pa[4], pb[4], pc[4], pd[4];
  isplit(pa, a), isplit(pb, b), isplit(pc, c), isplit(pd, d);
  for(u32 n = 0; n < 4; n++) o[n] = interpolate1f(pa[n], pb[n], pc[n], pd[n], x, y);
  return imerge(o);
}

inline auto image::interpolate4i(u64 a, u64 b, u32 x) -> u64 {
  u64 o[4], pa[4], pb[4];
  isplit(pa, a), isplit(pb, b);
  for(u32 n = 0; n < 4; n++) o[n] = interpolate1i(pa[n], pb[n], x);
  return imerge(o);
}

inline auto image::interpolate4i(u64 a, u64 b, u64 c, u64 d, u32 x, u32 y) -> u64 {
  u64 o[4], pa[4], pb[4], pc[4], pd[4];
  isplit(pa, a), isplit(pb, b), isplit(pc, c), isplit(pd, d);
  for(u32 n = 0; n < 4; n++) o[n] = interpolate1i(pa[n], pb[n], pc[n], pd[n], x, y);
  return imerge(o);
}

}
