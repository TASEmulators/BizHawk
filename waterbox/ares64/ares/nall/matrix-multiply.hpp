#pragma once

//matrix multiplication primitives
//used in: ruby/opengl/quark

namespace nall {

template<typename T> inline auto MatrixMultiply(
T* output,
const T* xdata, u32 xrows, u32 xcols,
const T* ydata, u32 yrows, u32 ycols
) -> void {
  if(xcols != yrows) return;

  for(u32 y : range(xrows)) {
    for(u32 x : range(ycols)) {
      T sum = 0;
      for(u32 z : range(xcols)) {
        sum += xdata[y * xcols + z] * ydata[z * ycols + x];
      }
      *output++ = sum;
    }
  }
}

template<typename T> inline auto MatrixMultiply(
const T* xdata, u32 xrows, u32 xcols,
const T* ydata, u32 yrows, u32 ycols
) -> vector<T> {
  vector<T> output;
  output.resize(xrows * ycols);
  MatrixMultiply(output.data(), xdata, xrows, xcols, ydata, yrows, ycols);
  return output;
}

}
